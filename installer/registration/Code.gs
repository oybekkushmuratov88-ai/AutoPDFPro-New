/**
 * AutoPDF Pro - registration back end (Google Apps Script). Plain ASCII, paste as one whole file.
 *
 * 1) Paste this entire file into the Apps Script editor (replace the default myFunction).
 * 2) Set NOTIFY_EMAIL and ADMIN_KEY just below.
 * 3) Deploy as a Web app: Execute as = Me, Who has access = Anyone.
 * See installer/registration/README.md for the step-by-step.
 */

// ---- Settings (change these two) --------------------------------------------------------------
var NOTIFY_EMAIL = "Oybekkushmuratov88@gmail.com"; // where the "new user" e-mail is sent
var ADMIN_KEY    = "CHANGE-ME";                     // a secret word you invent (asked by AUTOPDFADMIN)
var SHEET_NAME   = "Users";

var HEADER = ["Date", "Name", "Email", "Phone", "Computer", "Windows user", "Machine id", "OS", "Version", "Language"];

// ---- Registration (POST from the plug-in) -----------------------------------------------------
function doPost(e) {
  try {
    var p = (e && e.parameter) ? e.parameter : {};
    if (p.action && p.action !== "register") return text("ERROR: unknown action");

    var email = trim(p.email);
    if (!email) return text("ERROR: no email");

    var r = {
      date: new Date(),
      name: trim(p.name),
      email: email,
      phone: trim(p.phone),
      computer: trim(p.computer),
      user: trim(p.user),
      machineId: trim(p.machineId),
      os: trim(p.os),
      version: trim(p.version),
      lang: trim(p.lang)
    };

    sheet().appendRow([r.date, r.name, r.email, r.phone, r.computer, r.user, r.machineId, r.os, r.version, r.lang]);
    sendMail(r);
    return text("OK");
  } catch (err) {
    return text("ERROR: " + err);
  }
}

// ---- User list (GET ?action=list&key=... from AUTOPDFADMIN) ------------------------------------
function doGet(e) {
  var p = (e && e.parameter) ? e.parameter : {};
  if (p.action === "list") {
    if (trim(p.key) !== ADMIN_KEY) return text("FORBIDDEN");
    return text(listUsers());
  }
  return text("OK"); // health check
}

// ---- Helpers ----------------------------------------------------------------------------------
function sheet() {
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  var sh = ss.getSheetByName(SHEET_NAME);
  if (!sh) {
    sh = ss.insertSheet(SHEET_NAME);
    sh.appendRow(HEADER);
    sh.setFrozenRows(1);
  }
  return sh;
}

function sendMail(r) {
  var when = Utilities.formatDate(r.date, Session.getScriptTimeZone(), "yyyy-MM-dd HH:mm");
  var subject = "AutoPDF Pro - yangi foydalanuvchi: " + (r.name || r.email);
  var body =
    "AutoPDF Pro: yangi foydalanuvchi royxatdan otdi.\n\n" +
    "Ism: " + r.name + "\n" +
    "E-pochta: " + r.email + "\n" +
    "Telefon: " + (r.phone || "-") + "\n" +
    "Kompyuter: " + r.computer + "\n" +
    "Windows: " + r.user + "\n" +
    "Versiya: " + r.version + "\n" +
    "Til: " + r.lang + "\n" +
    "Sana: " + when + "\n\n" +
    (r.name || r.email) + " (" + r.email + ") - " + r.computer + " kompyuteridan kirdi.";
  MailApp.sendEmail(NOTIFY_EMAIL, subject, body);
}

function listUsers() {
  var sh = sheet();
  var last = sh.getLastRow();
  var out = ["OK"];
  if (last > 1) {
    var values = sh.getRange(2, 1, last - 1, HEADER.length).getValues();
    for (var i = 0; i < values.length; i++) {
      var v = values[i];
      var date = (v[0] instanceof Date)
        ? Utilities.formatDate(v[0], Session.getScriptTimeZone(), "yyyy-MM-dd HH:mm")
        : String(v[0]);
      out.push([date, cell(v[1]), cell(v[2]), cell(v[3]), cell(v[4]), cell(v[5]), cell(v[8])].join("\t"));
    }
  }
  return out.join("\n");
}

function cell(v) {
  return String(v == null ? "" : v).replace(/[\t\r\n]+/g, " ").trim();
}

function trim(v) {
  return (v == null ? "" : String(v)).trim();
}

function text(s) {
  return ContentService.createTextOutput(s).setMimeType(ContentService.MimeType.TEXT);
}
