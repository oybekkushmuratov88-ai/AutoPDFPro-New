// AutoPDF Pro - licence / approval server (Google Apps Script, bound to a Google Sheet).
//
// Paste this whole file into Extensions > Apps Script of your sheet, run setup() once, then
// Deploy > Web app (Execute as: Me, Who has access: Anyone). Step-by-step: server/licensing/README.md.
//
// What it does
//   * The plug-in POSTs registrations and status checks here over HTTPS.
//   * Every new request becomes a row in the "Requests" sheet with status "pending", and the administrator
//     gets an e-mail with signed, expiring Approve / Reject links.
//   * The administrator decides with those links (a confirmation page; nothing changes on a plain visit),
//     with the AutoPDF menu, or by editing the Status column. The user is e-mailed the decision.
//   * Approved installations receive a licence token signed with the RSA private key kept in this script's
//     properties. The plug-in only has the PUBLIC key, so it can verify approvals but never create them.
//
// Secrets (signing key, link secret) live only in Script Properties of YOUR Google account; nothing secret
// is in the plug-in. This file is plain ASCII on purpose (safe to copy and paste).

var DEFAULT_ADMIN_EMAIL = "Oybekkushmuratov88@gmail.com";

var CFG = {
  licenseTtlDays: 14,        // offline grace: an approved computer works this long without reaching the server
  linkTtlDays: 7,            // e-mailed Approve / Reject links expire after this many days
  maxRegisterPerInstall: 5,  // per installation per hour
  maxRegisterPerEmail: 5,    // per e-mail address per hour
  maxNewPerHour: 60,         // new requests (rows) per hour, all users together
  maxStatusPerInstall: 120,  // status checks per installation per hour
  maxLogRows: 5000
};

var SHEET_REQUESTS = "Requests";
var SHEET_LOG = "Log";
var HEADERS = ["RequestId", "Status", "Name", "Email", "InstallId", "Host", "AppVersion", "Computer",
               "WindowsUser", "OS", "Language", "Machine", "ClientKeyHash", "SubmittedAt", "UpdatedAt",
               "DecidedAt", "DecidedBy", "LastSeenAt", "Notes"];
var COL = {};
for (var hi = 0; hi < HEADERS.length; hi++) COL[HEADERS[hi]] = hi + 1;
var STATUSES = ["pending", "approved", "rejected"];

var RE = {
  installId: /^APDF(-[A-HJ-NP-Z2-9]{4}){4}$/,
  clientKey: /^[A-Za-z0-9_-]{43}$/,
  machine: /^[0-9a-f]{64}$/,
  nonce: /^[0-9a-f]{32}$/,
  email: /^[^@\s]+@[^@\s]+\.[^@\s]{2,}$/,
  uuid: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/,
  hex64: /^[0-9a-f]{64}$/
};

// ======================================================================================================
// Web app entry points
// ======================================================================================================

function doPost(e) {
  rememberUrl_();
  var p = (e && e.parameter) || {};
  if (!p.v) return legacyRegister_(p);          // AutoPDF Pro 2.x clients (no approval step)
  try {
    if (p.v !== "1") return reply_({ ok: "0", error: "bad_request", message: "Unsupported protocol version." });
    if (p.action === "register") return handleRegister_(p);
    if (p.action === "status") return handleStatus_(p);
    return reply_({ ok: "0", error: "bad_request", message: "Unknown action." });
  } catch (err) {
    log_("error", p.installId, "", "exception", String(err));
    return reply_({ ok: "0", error: "server_error", message: "Server error." });
  }
}

function doGet(e) {
  rememberUrl_();
  var p = (e && e.parameter) || {};
  if (p.a === "review") return reviewPage_(p);
  return HtmlService.createHtmlOutput(page_("AutoPDF Pro", "<p>AutoPDF Pro licence server is running.</p>"))
    .setTitle("AutoPDF Pro");
}

// ======================================================================================================
// Client requests
// ======================================================================================================

function handleRegister_(p) {
  var bad = validateCommon_(p);
  if (bad) return bad;
  var name = clean_(p.name, 100);
  var email = clean_(p.email, 254).toLowerCase();
  if (name.length < 2) return invalid_("name");
  if (!RE.email.test(email)) return invalid_("email");
  if (p.consent !== "1") return invalid_("consent");
  if (seenNonce_(p.nonce)) return reply_({ ok: "0", error: "replay", message: "Duplicate request." });

  if (rateLimited_("reg:" + p.installId, CFG.maxRegisterPerInstall, 3600) ||
      rateLimited_("regmail:" + hash_(email).substring(0, 32), CFG.maxRegisterPerEmail, 3600)) {
    return reply_({ ok: "0", error: "rate_limited", message: "Too many requests.", checkAfter: "900" });
  }

  var lock = LockService.getScriptLock();
  lock.waitLock(20000);
  var notify = null;
  var row;
  try {
    var sh = requests_();
    row = findRow_(sh, COL.InstallId, p.installId);
    var now = new Date();
    if (row) {
      var r = readRow_(sh, row);
      if (r.ClientKeyHash !== hash_(p.clientKey)) return mismatch_("client_mismatch", p);
      if (r.Machine !== p.machine) return mismatch_("machine_mismatch", p);
      if (r.Status === "pending") {
        // Corrections while waiting (e.g. a mistyped e-mail) are allowed.
        setCells_(sh, row, { Name: safeCell_(name), Email: safeCell_(email), UpdatedAt: now });
      }
      setCells_(sh, row, clientFacts_(p, now));
      log_("register-again", p.installId, r.RequestId, r.Status, "");
    } else {
      if (rateLimited_("new", CFG.maxNewPerHour, 3600)) {
        return reply_({ ok: "0", error: "rate_limited", message: "Too many new requests.", checkAfter: "1800" });
      }
      var requestId = Utilities.getUuid().toLowerCase();
      var values = {};
      values.RequestId = requestId;
      values.Status = "pending";
      values.Name = safeCell_(name);
      values.Email = safeCell_(email);
      values.InstallId = p.installId;
      values.Machine = p.machine;
      values.ClientKeyHash = hash_(p.clientKey);
      values.SubmittedAt = now;
      values.UpdatedAt = now;
      values.Computer = safeCell_(clean_(p.computer, 100));
      values.WindowsUser = safeCell_(clean_(p.winUser, 100));
      values.OS = safeCell_(clean_(p.os, 150));
      var facts = clientFacts_(p, now);
      for (var k in facts) values[k] = facts[k];
      sh.appendRow(HEADERS.map(function (h) { return values[h] === undefined ? "" : values[h]; }));
      row = sh.getLastRow();
      notify = requestId;
      log_("register", p.installId, requestId, "pending", email);
    }
    SpreadsheetApp.flush();
  } finally {
    lock.releaseLock();
  }

  if (notify) notifyAdmin_(notify);
  return statusReply_(requests_(), row, p);
}

function handleStatus_(p) {
  var bad = validateCommon_(p);
  if (bad) return bad;
  if (seenNonce_(p.nonce)) return reply_({ ok: "0", error: "replay", message: "Duplicate request." });
  if (rateLimited_("st:" + p.installId, CFG.maxStatusPerInstall, 3600)) {
    return reply_({ ok: "0", error: "rate_limited", message: "Too many status checks.", checkAfter: "600" });
  }
  var sh = requests_();
  var row = findRow_(sh, COL.InstallId, p.installId);
  if (!row) return reply_({ ok: "0", error: "unknown_install", message: "No registration for this installation.", installId: p.installId });
  var r = readRow_(sh, row);
  if (r.ClientKeyHash !== hash_(p.clientKey)) return mismatch_("client_mismatch", p);
  if (r.Machine !== p.machine) return mismatch_("machine_mismatch", p);
  try {
    setCells_(sh, row, clientFacts_(p, new Date()));
  } catch (err) {
    // last-seen is informative only
  }
  return statusReply_(sh, row, p);
}

function statusReply_(sh, row, p) {
  var r = readRow_(sh, row);
  var status = STATUSES.indexOf(r.Status) >= 0 ? r.Status : "pending";
  var now = new Date();
  var out = {
    ok: "1",
    status: status,
    installId: p.installId,
    requestId: r.RequestId,
    serverTime: String(unix_(now)),
    message: status === "approved" ? "Approved." : status === "rejected" ? "Not approved." : "Waiting for the administrator."
  };
  var keys = signingKeys_();
  if (keys) {
    out.publicKey = keys.publicKey;
    out.keyId = keys.keyId;
  }
  if (status === "approved") {
    if (!keys) {
      out.message = "The server has no signing key yet (AutoPDF menu > Generate signing keys).";
    } else {
      var expires = new Date(now.getTime() + CFG.licenseTtlDays * 86400000);
      var token = ["AUTOPDF-LIC", "1", p.installId, r.Machine, "approved", r.RequestId,
                   String(unix_(now)), String(unix_(expires))].join("|");
      out.token = token;
      out.sig = Utilities.base64Encode(Utilities.computeRsaSha256Signature(token, keys.privateKey));
    }
  }
  return reply_(out);
}

function validateCommon_(p) {
  if (!RE.installId.test(p.installId || "")) return invalid_("installId");
  if (!RE.clientKey.test(p.clientKey || "")) return invalid_("clientKey");
  if (!RE.machine.test(p.machine || "")) return invalid_("machine");
  if (!RE.nonce.test(p.nonce || "")) return invalid_("nonce");
  return null;
}

function clientFacts_(p, now) {
  return {
    Host: safeCell_(clean_(p.host, 60)),
    AppVersion: safeCell_(clean_(p.app, 30)),
    Language: safeCell_(clean_(p.lang, 20)),
    LastSeenAt: now
  };
}

function invalid_(field) {
  return reply_({ ok: "0", error: "invalid_field:" + field, message: "Invalid " + field + "." });
}

function mismatch_(code, p) {
  log_(code, p.installId, "", "refused", "");
  return reply_({ ok: "0", error: code, message: "This installation is registered on another computer or profile.", installId: p.installId });
}

function reply_(fields) {
  var lines = ["apdf=1"];
  for (var k in fields) {
    if (fields[k] === undefined || fields[k] === null) continue;
    lines.push(k + "=" + encodeURIComponent(String(fields[k])));
  }
  return ContentService.createTextOutput(lines.join("\n") + "\n").setMimeType(ContentService.MimeType.TEXT);
}

// AutoPDF Pro 2.x sent a plain registration without approval; keep accepting it into the old sheet.
function legacyRegister_(p) {
  try {
    if (p.action === "register" && p.email) {
      var ss = spreadsheet_();
      var sh = ss.getSheetByName("Users") || ss.insertSheet("Users");
      sh.appendRow([new Date(), safeCell_(clean_(p.name, 100)), safeCell_(clean_(p.email, 254)), safeCell_(clean_(p.phone, 40)),
                    safeCell_(clean_(p.computer, 100)), safeCell_(clean_(p.user, 100)), safeCell_(clean_(p.version, 30))]);
    }
  } catch (err) {
    // old clients only look for "OK"
  }
  return ContentService.createTextOutput("OK").setMimeType(ContentService.MimeType.TEXT);
}

// ======================================================================================================
// Administrator decisions
// ======================================================================================================

/** Signed, expiring link to the confirmation page of one request. */
function reviewLink_(requestId, op) {
  var exp = unix_(new Date()) + CFG.linkTtlDays * 86400;
  var sig = linkSignature_(requestId, op, exp);
  return webAppUrl_() + "?a=review&rid=" + encodeURIComponent(requestId) + "&op=" + op + "&exp=" + exp + "&sig=" + sig;
}

function linkSignature_(requestId, op, exp) {
  var secret = PropertiesService.getScriptProperties().getProperty("LINK_SECRET");
  if (!secret) throw new Error("Run setup() first (LINK_SECRET missing).");
  return hex_(Utilities.computeHmacSha256Signature("review|" + requestId + "|" + op + "|" + exp, secret));
}

/** Returns null when the link is genuine and not expired, else the reason. */
function checkLink_(rid, op, exp, sig) {
  if (!RE.uuid.test(String(rid || ""))) return "bad link";
  if (op !== "approve" && op !== "reject") return "bad link";
  if (!/^\d{9,11}$/.test(String(exp || ""))) return "bad link";
  if (!RE.hex64.test(String(sig || ""))) return "bad link";
  if (!safeEquals_(linkSignature_(rid, op, Number(exp)), String(sig))) return "bad signature";
  if (unix_(new Date()) > Number(exp)) return "expired";
  return null;
}

/** GET of an e-mailed link: shows the request and a confirm button; never changes anything by itself. */
function reviewPage_(p) {
  var problem = checkLink_(p.rid, p.op, p.exp, p.sig);
  if (problem) {
    return HtmlService.createHtmlOutput(page_("AutoPDF Pro",
      "<p class='err'>This link is not valid (" + esc_(problem) + ").</p><p>Use the Requests sheet or the AutoPDF menu instead.</p>"))
      .setTitle("AutoPDF Pro");
  }
  var sh = requests_();
  var row = findRow_(sh, COL.RequestId, p.rid);
  if (!row) return HtmlService.createHtmlOutput(page_("AutoPDF Pro", "<p class='err'>Request not found.</p>")).setTitle("AutoPDF Pro");
  var r = readRow_(sh, row);
  var approve = p.op === "approve";
  var html =
    "<table>" +
    tr_("Name", r.Name) + tr_("E-mail", r.Email) + tr_("Installation ID", r.InstallId) + tr_("Computer", r.Computer) +
    tr_("Windows user", r.WindowsUser) + tr_("CAD", r.Host) + tr_("Version", r.AppVersion) +
    tr_("Submitted", fmt_(r.SubmittedAt)) + tr_("Current status", r.Status) +
    "</table>" +
    "<p><button id='go' class='" + (approve ? "ok" : "no") + "' onclick='decide()'>" +
    (approve ? "Approve (Ruxsat berish)" : "Reject (Rad etish)") + "</button></p>" +
    "<p id='result'></p>" +
    "<script>" +
    "function decide(){var b=document.getElementById('go');b.disabled=true;" +
    "google.script.run.withSuccessHandler(function(m){document.getElementById('result').textContent=m;})" +
    ".withFailureHandler(function(e){b.disabled=false;document.getElementById('result').textContent='Error: '+e.message;})" +
    ".reviewDecide(" + JSON.stringify(String(p.rid)) + "," + JSON.stringify(String(p.op)) + "," +
    JSON.stringify(String(p.exp)) + "," + JSON.stringify(String(p.sig)) + ");}" +
    "</script>";
  return HtmlService.createHtmlOutput(page_("AutoPDF Pro: " + (approve ? "approve" : "reject") + " request", html))
    .setTitle("AutoPDF Pro");
}

/** Called from the confirmation page (google.script.run). Authorised only by the link signature. */
function reviewDecide(rid, op, exp, sig) {
  var problem = checkLink_(rid, op, exp, sig);
  if (problem) throw new Error("Link not valid (" + problem + ").");
  var sh = requests_();
  var row = findRow_(sh, COL.RequestId, rid);
  if (!row) throw new Error("Request not found.");
  return applyDecision_(sh, row, op === "approve" ? "approved" : "rejected", "e-mail link");
}

/** Sets the status of one row (under the lock), records who/when, and e-mails the user. */
function applyDecision_(sh, row, status, by) {
  if (STATUSES.indexOf(status) < 0) throw new Error("Unknown status " + status);
  var lock = LockService.getScriptLock();
  lock.waitLock(20000);
  var r, previous;
  try {
    r = readRow_(sh, row);
    previous = r.Status;
    if (previous === status) return r.Name + ": already " + status + ".";
    var now = new Date();
    setCells_(sh, row, { Status: status, UpdatedAt: now, DecidedAt: now, DecidedBy: safeCell_(by) });
    SpreadsheetApp.flush();
  } finally {
    lock.releaseLock();
  }
  log_("decision", r.InstallId, r.RequestId, status, by + " (was " + previous + ")");
  if (status !== "pending") notifyUser_(r, status);
  return r.Name + " (" + r.Email + "): " + status + ".";
}

/** Installable trigger: the administrator changed the Status column directly in the sheet. */
function onEditInstalled(e) {
  // Real trigger events carry Range objects; anything else (e.g. a forged call) is ignored.
  if (!e || !e.range || typeof e.range.getSheet !== "function") return;
  var sh = e.range.getSheet();
  if (sh.getName() !== SHEET_REQUESTS) return;
  var first = e.range.getRow(), last = e.range.getLastRow();
  var c1 = e.range.getColumn(), c2 = e.range.getLastColumn();
  if (COL.Status < c1 || COL.Status > c2) return;
  var by = "sheet" + (e.user && e.user.getEmail && e.user.getEmail() ? " (" + e.user.getEmail() + ")" : "");
  for (var row = Math.max(first, 2); row <= last; row++) {
    var value = String(sh.getRange(row, COL.Status).getValue()).trim().toLowerCase();
    if (STATUSES.indexOf(value) < 0) {
      sh.getRange(row, COL.Status).setValue(first === last && e.oldValue ? e.oldValue : "pending");
      continue;
    }
    sh.getRange(row, COL.Status).setValue(value);
    var r = readRow_(sh, row);
    var now = new Date();
    setCells_(sh, row, { UpdatedAt: now, DecidedAt: now, DecidedBy: safeCell_(by) });
    log_("decision", r.InstallId, r.RequestId, value, by);
    if (value !== "pending" && (first !== last || e.oldValue !== value)) notifyUser_(r, value);
  }
}

// ======================================================================================================
// Sheet menu (owner only)
// ======================================================================================================

function onOpen() {
  SpreadsheetApp.getUi().createMenu("AutoPDF")
    .addItem("Approve selected rows", "menuApprove")
    .addItem("Reject selected rows", "menuReject")
    .addItem("Set selected rows back to pending", "menuPending")
    .addSeparator()
    .addItem("Generate signing keys", "menuKeys")
    .addItem("Show public key", "menuShowKey")
    .addItem("Set administrator e-mail", "menuAdminEmail")
    .addItem("Check configuration", "menuCheck")
    .addToUi();
}

function menuApprove() { decideSelected_("approved"); }
function menuReject() { decideSelected_("rejected"); }
function menuPending() { decideSelected_("pending"); }

function decideSelected_(status) {
  requireOwner_();
  var sh = SpreadsheetApp.getActiveSheet();
  var ui = SpreadsheetApp.getUi();
  if (sh.getName() !== SHEET_REQUESTS) { ui.alert("Select rows in the \"" + SHEET_REQUESTS + "\" sheet first."); return; }
  var range = sh.getActiveRange();
  var messages = [];
  for (var row = Math.max(range.getRow(), 2); row <= range.getLastRow(); row++) {
    if (!sh.getRange(row, COL.RequestId).getValue()) continue;
    messages.push(applyDecision_(sh, row, status, "menu (" + Session.getActiveUser().getEmail() + ")"));
  }
  ui.alert(messages.length ? messages.join("\n") : "No request rows selected.");
}

function menuKeys() {
  requireOwner_();
  var html = [
    "<div style='font:14px Arial'>",
    "<p>Creates a new RSA-2048 signing key <b>in your browser</b> and stores it in this script's properties.",
    " Approved computers switch to the new key at their next online check.</p>",
    "<button id='b' onclick='gen()'>Generate and save</button>",
    "<pre id='out' style='white-space:pre-wrap;font-size:11px'></pre></div>",
    "<script>",
    "function b64(buf){var s='',a=new Uint8Array(buf);for(var i=0;i<a.length;i++)s+=String.fromCharCode(a[i]);return btoa(s);}",
    "async function gen(){var o=document.getElementById('out');document.getElementById('b').disabled=true;",
    "try{var kp=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},true,['sign','verify']);",
    "var p8=b64(await crypto.subtle.exportKey('pkcs8',kp.privateKey));",
    "var pem='-----BEGIN PRIVATE KEY-----\\n'+p8.match(/.{1,64}/g).join('\\n')+'\\n-----END PRIVATE KEY-----\\n';",
    "o.textContent='Saving...';",
    "google.script.run.withSuccessHandler(function(m){o.textContent=m;}).withFailureHandler(function(e){o.textContent='Error: '+e.message;}).saveSigningKey(pem);",
    "}catch(e){o.textContent='This browser cannot create keys here: '+e+'. See README (openssl).';}}",
    "</script>"
  ].join("");
  SpreadsheetApp.getUi().showModalDialog(HtmlService.createHtmlOutput(html).setWidth(560).setHeight(380), "AutoPDF: signing key");
}

/** Stores a PKCS#8 private key (PEM) and the public key derived from it. Owner only. */
function saveSigningKey(privatePem) {
  requireOwner_();
  var der = pemToBytes_(privatePem);
  var spki = spkiFromPkcs8_(der);
  var pem = "-----BEGIN PRIVATE KEY-----\n" + Utilities.base64Encode(der).replace(/(.{64})/g, "$1\n").replace(/\n$/, "") + "\n-----END PRIVATE KEY-----\n";
  Utilities.computeRsaSha256Signature("test", pem); // throws when the key is unusable
  var publicB64 = Utilities.base64Encode(spki);
  PropertiesService.getScriptProperties().setProperties({ PRIVATE_KEY: pem, PUBLIC_KEY: publicB64 });
  log_("keys", "", "", "saved", keyId_(spki));
  return "Saved. Key ID " + keyId_(spki) + "\n\nPublic key (safe to share; optional built-in copy for the plug-in):\n" +
         "-----BEGIN PUBLIC KEY-----\n" + publicB64.replace(/(.{64})/g, "$1\n").replace(/\n$/, "") + "\n-----END PUBLIC KEY-----";
}

function menuShowKey() {
  requireOwner_();
  var keys = signingKeys_();
  var ui = SpreadsheetApp.getUi();
  if (!keys) { ui.alert("No signing key yet: AutoPDF > Generate signing keys."); return; }
  ui.alert("Key ID " + keys.keyId + "\n\n-----BEGIN PUBLIC KEY-----\n" +
           keys.publicKey.replace(/(.{64})/g, "$1\n").replace(/\n$/, "") + "\n-----END PUBLIC KEY-----");
}

function menuAdminEmail() {
  requireOwner_();
  var ui = SpreadsheetApp.getUi();
  var res = ui.prompt("Administrator e-mail", "New requests are sent to (now: " + adminEmail_() + "):", ui.ButtonSet.OK_CANCEL);
  if (res.getSelectedButton() !== ui.Button.OK) return;
  var email = res.getResponseText().trim();
  if (!RE.email.test(email)) { ui.alert("Not an e-mail address."); return; }
  PropertiesService.getScriptProperties().setProperty("ADMIN_EMAIL", email);
  ui.alert("Saved: " + email);
}

function menuCheck() {
  requireOwner_();
  SpreadsheetApp.getUi().alert(configurationReport_().join("\n"));
}

function configurationReport_() {
  var props = PropertiesService.getScriptProperties();
  var keys = signingKeys_();
  var triggers = ScriptApp.getProjectTriggers().filter(function (t) { return t.getHandlerFunction() === "onEditInstalled"; });
  return [
    (keys ? "OK  " : "--  ") + "Signing key: " + (keys ? keys.keyId : "missing (AutoPDF > Generate signing keys)"),
    (props.getProperty("LINK_SECRET") ? "OK  " : "--  ") + "Link secret" + (props.getProperty("LINK_SECRET") ? "" : ": missing (run setup)"),
    (props.getProperty("WEBAPP_URL") ? "OK  " : "--  ") + "Web app URL: " + (props.getProperty("WEBAPP_URL") || "unknown (deploy, then open the /exec URL once)"),
    "OK  Administrator e-mail: " + adminEmail_(),
    (triggers.length ? "OK  " : "--  ") + "Status-column trigger" + (triggers.length ? "" : ": missing (run setup)"),
    "    E-mails left today: " + MailApp.getRemainingDailyQuota()
  ];
}

/** Run once from the editor: sheets, link secret, triggers. Safe to run again. */
function setup() {
  requireOwner_();
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  var props = PropertiesService.getScriptProperties();
  props.setProperty("SPREADSHEET_ID", ss.getId());
  if (!props.getProperty("LINK_SECRET")) {
    var seed = Utilities.getUuid() + Utilities.getUuid() + Utilities.getUuid() + new Date().getTime();
    props.setProperty("LINK_SECRET", hex_(Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, seed)));
  }
  if (!props.getProperty("ADMIN_EMAIL")) props.setProperty("ADMIN_EMAIL", DEFAULT_ADMIN_EMAIL);
  requests_();
  logSheet_();
  ScriptApp.getProjectTriggers().forEach(function (t) {
    if (t.getHandlerFunction() === "onEditInstalled") ScriptApp.deleteTrigger(t);
  });
  ScriptApp.newTrigger("onEditInstalled").forSpreadsheet(ss).onEdit().create();
  Logger.log(configurationReport_().join("\n"));
}

/** Functions callable from a web page must be guarded: only the script owner may run them. */
function requireOwner_() {
  var active = "";
  var owner = "";
  try { active = Session.getActiveUser().getEmail(); } catch (err) { active = ""; }
  try { owner = Session.getEffectiveUser().getEmail(); } catch (err) { owner = ""; }
  if (!active || !owner || active.toLowerCase() !== owner.toLowerCase()) {
    throw new Error("Only the owner of this script can do this.");
  }
}

// ======================================================================================================
// E-mail
// ======================================================================================================

function notifyAdmin_(requestId) {
  var sh = requests_();
  var row = findRow_(sh, COL.RequestId, requestId);
  if (!row) return;
  var r = readRow_(sh, row);
  if (MailApp.getRemainingDailyQuota() < 2) { log_("mail-skipped", r.InstallId, requestId, "quota", "admin"); return; }
  var approve = reviewLink_(requestId, "approve");
  var reject = reviewLink_(requestId, "reject");
  var details = [
    "Ism / Name: " + r.Name,
    "E-pochta / E-mail: " + r.Email,
    "Kompyuter / Computer: " + r.Computer + " (" + r.WindowsUser + ")",
    "CAD: " + r.Host + ", AutoPDF " + r.AppVersion,
    "Installation ID: " + r.InstallId,
    "Request ID: " + requestId
  ];
  var body = "AutoPDF Pro: yangi ruxsat so'rovi / new access request\n\n" + details.join("\n") +
             "\n\nRuxsat berish / Approve:\n" + approve + "\n\nRad etish / Reject:\n" + reject +
             "\n\nJadval / Sheet: " + spreadsheet_().getUrl() + "\n";
  var html = "<p><b>AutoPDF Pro</b>: yangi ruxsat so'rovi / new access request</p><p>" +
             details.map(esc_).join("<br>") + "</p><p>" +
             "<a href='" + esc_(approve) + "' style='background:#1a7f37;color:#fff;padding:8px 14px;text-decoration:none;border-radius:4px'>Ruxsat berish / Approve</a> &nbsp; " +
             "<a href='" + esc_(reject) + "' style='background:#b42318;color:#fff;padding:8px 14px;text-decoration:none;border-radius:4px'>Rad etish / Reject</a></p>" +
             "<p style='color:#666'>Havolalar " + CFG.linkTtlDays + " kun amal qiladi; tasdiqlash sahifasi ochiladi. / Links expire in " +
             CFG.linkTtlDays + " days and open a confirmation page.<br><a href='" + esc_(spreadsheet_().getUrl()) + "'>Requests sheet</a></p>";
  MailApp.sendEmail({ to: adminEmail_(), subject: "AutoPDF Pro: " + r.Name + " - ruxsat so'rovi", body: body, htmlBody: html, name: "AutoPDF Pro" });
}

function notifyUser_(r, status) {
  if (!RE.email.test(String(r.Email || "").replace(/^'/, ""))) return;
  if (MailApp.getRemainingDailyQuota() < 2) { log_("mail-skipped", r.InstallId, r.RequestId, "quota", "user"); return; }
  var t = userTexts_(r.Language, status === "approved");
  try {
    MailApp.sendEmail({ to: String(r.Email).replace(/^'/, ""), subject: t.subject, body: t.body.replace("{name}", String(r.Name).replace(/^'/, "")), name: "AutoPDF Pro" });
  } catch (err) {
    log_("mail-failed", r.InstallId, r.RequestId, status, String(err));
  }
}

function userTexts_(language, approved) {
  var lang = String(language || "").toLowerCase();
  if (lang.indexOf("rus") === 0) {
    return approved
      ? { subject: "AutoPDF Pro: \u0434\u043e\u0441\u0442\u0443\u043f \u0440\u0430\u0437\u0440\u0435\u0448\u0451\u043d", body: "\u0417\u0434\u0440\u0430\u0432\u0441\u0442\u0432\u0443\u0439\u0442\u0435, {name}!\n\n\u0412\u0430\u0448 \u0437\u0430\u043f\u0440\u043e\u0441 \u043d\u0430 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435 AutoPDF Pro \u043e\u0434\u043e\u0431\u0440\u0435\u043d. \u0417\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u0435 \u043a\u043e\u043c\u0430\u043d\u0434\u0443 AUTOPDF \u0432 AutoCAD \u0438\u043b\u0438 ZWCAD.\n" }
      : { subject: "AutoPDF Pro: \u0437\u0430\u043f\u0440\u043e\u0441 \u043e\u0442\u043a\u043b\u043e\u043d\u0451\u043d", body: "\u0417\u0434\u0440\u0430\u0432\u0441\u0442\u0432\u0443\u0439\u0442\u0435, {name}!\n\n\u041a \u0441\u043e\u0436\u0430\u043b\u0435\u043d\u0438\u044e, \u0432\u0430\u0448 \u0437\u0430\u043f\u0440\u043e\u0441 \u043d\u0430 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435 AutoPDF Pro \u043d\u0435 \u043e\u0434\u043e\u0431\u0440\u0435\u043d.\n" };
  }
  if (lang.indexOf("eng") === 0) {
    return approved
      ? { subject: "AutoPDF Pro: access approved", body: "Hello {name},\n\nYour request to use AutoPDF Pro has been approved. Run the AUTOPDF command in AutoCAD or ZWCAD.\n" }
      : { subject: "AutoPDF Pro: request not approved", body: "Hello {name},\n\nUnfortunately your request to use AutoPDF Pro was not approved.\n" };
  }
  return approved
    ? { subject: "AutoPDF Pro: ruxsat berildi", body: "Assalomu alaykum, {name}!\n\nAutoPDF Pro dan foydalanish so'rovingiz tasdiqlandi. AutoCAD yoki ZWCAD da AUTOPDF buyrug'ini ishga tushiring.\n" }
    : { subject: "AutoPDF Pro: so'rov rad etildi", body: "Assalomu alaykum, {name}!\n\nAfsuski, AutoPDF Pro dan foydalanish so'rovingiz tasdiqlanmadi.\n" };
}

// ======================================================================================================
// Storage helpers
// ======================================================================================================

function spreadsheet_() {
  var id = PropertiesService.getScriptProperties().getProperty("SPREADSHEET_ID");
  return id ? SpreadsheetApp.openById(id) : SpreadsheetApp.getActiveSpreadsheet();
}

function requests_() {
  var ss = spreadsheet_();
  var sh = ss.getSheetByName(SHEET_REQUESTS);
  if (!sh) {
    sh = ss.insertSheet(SHEET_REQUESTS);
    sh.getRange(1, 1, 1, HEADERS.length).setValues([HEADERS]).setFontWeight("bold");
    sh.setFrozenRows(1);
    sh.getRange(1, 1, sh.getMaxRows(), COL.ClientKeyHash).setNumberFormat("@");
    sh.getRange(1, COL.Notes, sh.getMaxRows(), 1).setNumberFormat("@");
    var rule = SpreadsheetApp.newDataValidation().requireValueInList(STATUSES, true).setAllowInvalid(false).build();
    sh.getRange(2, COL.Status, 999, 1).setDataValidation(rule);
  }
  return sh;
}

function logSheet_() {
  var ss = spreadsheet_();
  var sh = ss.getSheetByName(SHEET_LOG);
  if (!sh) {
    sh = ss.insertSheet(SHEET_LOG);
    sh.getRange(1, 1, 1, 6).setValues([["Time", "Action", "InstallId", "RequestId", "Result", "Detail"]]).setFontWeight("bold");
    sh.setFrozenRows(1);
  }
  return sh;
}

function log_(action, installId, requestId, result, detail) {
  try {
    var sh = logSheet_();
    sh.appendRow([new Date(), action, safeCell_(installId || ""), safeCell_(requestId || ""), safeCell_(result || ""), safeCell_(clean_(detail, 300))]);
    if (sh.getLastRow() > CFG.maxLogRows + 500) sh.deleteRows(2, 500);
  } catch (err) {
    // logging must never break a request
  }
}

function findRow_(sh, column, value) {
  if (!value || sh.getLastRow() < 2) return 0;
  var cell = sh.getRange(2, column, sh.getLastRow() - 1, 1).createTextFinder(String(value)).matchEntireCell(true).findNext();
  return cell ? cell.getRow() : 0;
}

function readRow_(sh, row) {
  var values = sh.getRange(row, 1, 1, HEADERS.length).getValues()[0];
  var r = {};
  for (var i = 0; i < HEADERS.length; i++) r[HEADERS[i]] = values[i];
  r.Status = String(r.Status || "").trim().toLowerCase();
  r.Machine = String(r.Machine || "");
  r.ClientKeyHash = String(r.ClientKeyHash || "");
  r.RequestId = String(r.RequestId || "");
  return r;
}

function setCells_(sh, row, values) {
  for (var k in values) {
    if (COL[k]) sh.getRange(row, COL[k]).setValue(values[k]);
  }
}

function signingKeys_() {
  var props = PropertiesService.getScriptProperties();
  var priv = props.getProperty("PRIVATE_KEY");
  var pub = props.getProperty("PUBLIC_KEY");
  if (!priv || !pub) return null;
  return { privateKey: priv, publicKey: pub, keyId: keyId_(Utilities.base64Decode(pub)) };
}

function adminEmail_() {
  return PropertiesService.getScriptProperties().getProperty("ADMIN_EMAIL") || DEFAULT_ADMIN_EMAIL;
}

function webAppUrl_() {
  var url = PropertiesService.getScriptProperties().getProperty("WEBAPP_URL");
  if (url) return url;
  try { url = ScriptApp.getService().getUrl(); } catch (err) { url = ""; }
  return url || "";
}

function rememberUrl_() {
  try {
    var url = ScriptApp.getService().getUrl();
    if (url && /\/exec$/.test(url)) {
      var props = PropertiesService.getScriptProperties();
      if (props.getProperty("WEBAPP_URL") !== url) props.setProperty("WEBAPP_URL", url);
    }
  } catch (err) {
    // not in a web app context
  }
}

// ======================================================================================================
// Throttling and replay protection (script cache)
// ======================================================================================================

function rateLimited_(key, max, windowSeconds) {
  var cache = CacheService.getScriptCache();
  var k = "rl:" + key;
  var n = Number(cache.get(k) || 0);
  if (n >= max) return true;
  cache.put(k, String(n + 1), windowSeconds);
  return false;
}

function seenNonce_(nonce) {
  var cache = CacheService.getScriptCache();
  var k = "nonce:" + nonce;
  if (cache.get(k)) return true;
  cache.put(k, "1", 21600);
  return false;
}

// ======================================================================================================
// Small utilities
// ======================================================================================================

function clean_(value, max) {
  var s = String(value === undefined || value === null ? "" : value).replace(/[\u0000-\u001f\u007f]/g, " ").trim();
  return s.length > max ? s.substring(0, max) : s;
}

/** Text that a spreadsheet would treat as a formula is stored as plain text. */
function safeCell_(value) {
  var s = String(value === undefined || value === null ? "" : value);
  return /^[=+\-@]/.test(s) ? "'" + s : s;
}

function hash_(text) {
  return hex_(Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, String(text), Utilities.Charset.UTF_8));
}

function hex_(bytes) {
  var s = "";
  for (var i = 0; i < bytes.length; i++) {
    var b = bytes[i] < 0 ? bytes[i] + 256 : bytes[i];
    s += (b < 16 ? "0" : "") + b.toString(16);
  }
  return s;
}

function keyId_(spkiBytes) {
  return hex_(Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, spkiBytes)).substring(0, 16);
}

function safeEquals_(a, b) {
  if (a.length !== b.length) return false;
  var diff = 0;
  for (var i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

function unix_(date) {
  return Math.floor(date.getTime() / 1000);
}

function fmt_(value) {
  if (value instanceof Date) return Utilities.formatDate(value, Session.getScriptTimeZone(), "yyyy-MM-dd HH:mm");
  return String(value || "");
}

function esc_(s) {
  return String(s === undefined || s === null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;")
    .replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

function tr_(label, value) {
  return "<tr><th>" + esc_(label) + "</th><td>" + esc_(String(value === undefined ? "" : value).replace(/^'/, "")) + "</td></tr>";
}

function page_(title, body) {
  return "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width'>" +
    "<style>body{font:15px Arial,sans-serif;margin:24px;max-width:640px;color:#1f2328}th{text-align:left;padding:4px 12px 4px 0;color:#57606a;font-weight:normal}" +
    "td{padding:4px 0}button{font-size:16px;padding:10px 18px;border:0;border-radius:6px;color:#fff;cursor:pointer}" +
    ".ok{background:#1a7f37}.no{background:#b42318}button:disabled{opacity:.5}.err{color:#b42318}</style></head><body><h2>" +
    esc_(title) + "</h2>" + body + "</body></html>";
}

// ---- RSA key encoding (PKCS#8 private key -> SubjectPublicKeyInfo) -------------------------------------

function pemToBytes_(pem) {
  var b64 = String(pem || "").replace(/-----[^-]+-----/g, "").replace(/\s+/g, "");
  if (!b64) throw new Error("Empty key.");
  return Utilities.base64Decode(b64);
}

function spkiFromPkcs8_(bytes) {
  var der = bytes.map(function (b) { return b < 0 ? b + 256 : b; });
  var top = derRead_(der, 0);                       // PrivateKeyInfo SEQUENCE
  if (top.tag !== 0x30) throw new Error("Not a PKCS#8 key.");
  var p = top.start;
  var version = derRead_(der, p); p = version.end;  // INTEGER 0
  var alg = derRead_(der, p); p = alg.end;          // AlgorithmIdentifier
  var oid = derRead_(der, alg.start);
  if (der.slice(oid.start, oid.end).join(",") !== "42,134,72,134,247,13,1,1,1") throw new Error("Not an RSA key.");
  var octets = derRead_(der, p);                    // OCTET STRING with RSAPrivateKey
  if (octets.tag !== 0x04) throw new Error("Bad PKCS#8 key.");
  var rsa = derRead_(der, octets.start);
  var q = rsa.start;
  var v = derRead_(der, q); q = v.end;
  var n = derRead_(der, q); q = n.end;
  var e = derRead_(der, q);
  var rsaPublic = derEncode_(0x30, derBytes_(der, n).concat(derBytes_(der, e)));
  var algId = derEncode_(0x30, derEncode_(0x06, [42, 134, 72, 134, 247, 13, 1, 1, 1]).concat([5, 0]));
  var spki = derEncode_(0x30, algId.concat(derEncode_(0x03, [0].concat(rsaPublic))));
  return spki.map(function (b) { return b > 127 ? b - 256 : b; });
}

function derRead_(der, pos) {
  var tag = der[pos];
  var len = der[pos + 1];
  var start = pos + 2;
  if (len & 0x80) {
    var count = len & 0x7f;
    len = 0;
    for (var i = 0; i < count; i++) len = len * 256 + der[start + i];
    start += count;
  }
  if (start + len > der.length) throw new Error("Bad DER.");
  return { tag: tag, head: pos, start: start, end: start + len };
}

/** The whole TLV (header included) of an element read with derRead_. */
function derBytes_(der, element) {
  return der.slice(element.head, element.end);
}

function derEncode_(tag, content) {
  var len = content.length;
  var header = [tag];
  if (len < 128) {
    header.push(len);
  } else {
    var bytes = [];
    while (len > 0) { bytes.unshift(len & 0xff); len = Math.floor(len / 256); }
    header.push(0x80 | bytes.length);
    header = header.concat(bytes);
  }
  return header.concat(content);
}
