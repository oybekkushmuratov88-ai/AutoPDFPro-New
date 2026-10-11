// Tests of server/licensing/Code.gs with mocked Google services.  Run: node --test server/licensing/test
"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const { createContext } = require("./gas-mock");

const ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
const rnd = (n) => Array.from(crypto.randomBytes(n), (b) => ALPHABET[b & 31]).join("");
const newInstallId = () => "APDF-" + [rnd(4), rnd(4), rnd(4), rnd(4)].join("-");
const newClientKey = () => crypto.randomBytes(32).toString("base64url");
const hex = (n) => crypto.randomBytes(n).toString("hex");

function parse(text) {
  const out = {};
  for (const line of text.split("\n")) {
    const i = line.indexOf("=");
    if (i > 0) out[line.substring(0, i)] = decodeURIComponent(line.substring(i + 1));
  }
  return out;
}

function server({ keys = true } = {}) {
  const ctx = createContext();
  ctx.gas.setup();
  ctx.gas.onOpen();
  if (keys) {
    const { privateKey, publicKey } = crypto.generateKeyPairSync("rsa", { modulusLength: 2048 });
    ctx.gas.saveSigningKey(privateKey.export({ type: "pkcs8", format: "pem" }));
    ctx.publicKey = publicKey;
  }
  ctx.post = (fields) => parse(ctx.gas.doPost({ parameter: fields }).getContent());
  ctx.client = () => ({ installId: newInstallId(), clientKey: newClientKey(), machine: hex(32) });
  ctx.register = (client, extra = {}) =>
    ctx.post(Object.assign({ v: "1", action: "register", nonce: hex(16), name: "Ali Valiyev", email: "ali@example.com",
      consent: "1", host: "ZWCAD 2025", app: "3.0.0", lang: "Russian", computer: "PC-01", winUser: "ali", os: "Windows 11" }, client, extra));
  ctx.status = (client, extra = {}) => ctx.post(Object.assign({ v: "1", action: "status", nonce: hex(16), host: "AutoCAD 2024", app: "3.0.0" }, client, extra));
  ctx.requests = () => ctx.ss.getSheetByName("Requests");
  ctx.rowOf = (installId) => {
    const sh = ctx.requests();
    for (let r = 2; r <= sh.getLastRow(); r++) if (sh.cell(r, 5) === installId) return r;
    return 0;
  };
  ctx.cell = (installId, header) => ctx.requests().cell(ctx.rowOf(installId), ctx.gas.COL[header]);
  ctx.linkParams = (mail, op) => {
    const m = mail.body.match(new RegExp("(https://\\S+op=" + op + "\\S+)"));
    return Object.fromEntries(new URL(m[1]).searchParams);
  };
  return ctx;
}

test("Code.gs is plain ASCII (safe to paste)", () => {
  const text = fs.readFileSync(path.join(__dirname, "..", "Code.gs"));
  assert.ok(text.every((b) => b < 128), "non-ASCII byte found");
});

test("register: validation of every field", () => {
  const s = server();
  const c = s.client();
  assert.equal(s.register({ ...c, installId: "APDF-1111-AAAA-BBBB-CCCC" }).error, "invalid_field:installId");
  assert.equal(s.register({ ...c, clientKey: "short" }).error, "invalid_field:clientKey");
  assert.equal(s.register({ ...c, machine: "XYZ" }).error, "invalid_field:machine");
  assert.equal(s.register(c, { nonce: "1" }).error, "invalid_field:nonce");
  assert.equal(s.register(c, { email: "not-an-email" }).error, "invalid_field:email");
  assert.equal(s.register(c, { name: " " }).error, "invalid_field:name");
  assert.equal(s.register(c, { consent: "0" }).error, "invalid_field:consent");
  assert.equal(s.post({ v: "2", action: "register" }).error, "bad_request");
  assert.equal(s.post({ v: "1", action: "drop" }).error, "bad_request");
  assert.equal(s.requests().getLastRow(), 1, "nothing stored");
});

test("register: pending row, unique request id, admin e-mail with signed links", () => {
  const s = server();
  const c = s.client();
  const reply = s.register(c);
  assert.equal(reply.apdf, "1");
  assert.equal(reply.ok, "1");
  assert.equal(reply.status, "pending");
  assert.match(reply.requestId, /^[0-9a-f-]{36}$/);
  assert.equal(reply.installId, c.installId);
  assert.ok(reply.publicKey && reply.keyId, "public key is announced");
  assert.equal(reply.token, undefined, "no licence while pending");
  assert.equal(s.cell(c.installId, "Status"), "pending");
  assert.equal(s.cell(c.installId, "ClientKeyHash"), crypto.createHash("sha256").update(c.clientKey).digest("hex"));
  assert.notEqual(s.cell(c.installId, "ClientKeyHash"), c.clientKey, "the client key itself is never stored");

  const other = s.register(s.client());
  assert.notEqual(other.requestId, reply.requestId);

  const mail = s.state.mails.find((m) => m.to === "Oybekkushmuratov88@gmail.com" && m.body.includes(c.installId));
  assert.ok(mail, "administrator notified");
  assert.match(mail.body, /a=review&rid=.+&op=approve&exp=\d+&sig=[0-9a-f]{64}/);
  assert.match(mail.body, /op=reject/);
});

test("register again: same installation keeps one row; pending details can be corrected", () => {
  const s = server();
  const c = s.client();
  s.register(c);
  const mails = s.state.mails.length;
  const again = s.register(c, { email: "fixed@example.com" });
  assert.equal(again.ok, "1");
  assert.equal(s.requests().getLastRow(), 2);
  assert.equal(s.cell(c.installId, "Email"), "fixed@example.com");
  assert.equal(s.state.mails.length, mails, "no second admin e-mail");
});

test("status: unknown, wrong client key, other computer", () => {
  const s = server();
  const c = s.client();
  assert.equal(s.status(c).error, "unknown_install");
  s.register(c);
  assert.equal(s.status({ ...c, clientKey: newClientKey() }).error, "client_mismatch");
  assert.equal(s.status({ ...c, machine: hex(32) }).error, "machine_mismatch");
  assert.equal(s.register({ ...c, clientKey: newClientKey() }).error, "client_mismatch");
  assert.equal(s.status(c).status, "pending");
});

test("replayed request id (nonce) is refused", () => {
  const s = server();
  const c = s.client();
  s.register(c);
  const nonce = hex(16);
  assert.equal(s.status(c, { nonce }).ok, "1");
  assert.equal(s.status(c, { nonce }).error, "replay");
});

test("throttling: per installation, per e-mail and new requests per hour", () => {
  const s = server();
  const c = s.client();
  for (let i = 0; i < 5; i++) assert.equal(s.register(c).ok, "1");
  const limited = s.register(c);
  assert.equal(limited.error, "rate_limited");
  assert.ok(Number(limited.checkAfter) > 0);

  for (let i = 0; i < 4; i++) s.register(s.client(), { email: "same@example.com" });
  s.register(s.client(), { email: "same@example.com" });
  assert.equal(s.register(s.client(), { email: "same@example.com" }).error, "rate_limited");

  for (let i = 0; i < 120; i++) s.status(c);
  assert.equal(s.status(c).error, "rate_limited");

  s.state.now += 3601 * 1000; // a new hour
  assert.equal(s.status(c).ok, "1");
});

test("cap on new requests per hour protects the administrator's mailbox", () => {
  const s = server();
  let created = 0;
  for (let i = 0; i < 70; i++) if (s.register(s.client(), { email: "u" + i + "@example.com" }).ok === "1") created++;
  assert.equal(created, 60);
});

test("approval link: GET only shows a confirmation page; the button approves; signed licence verifies", () => {
  const s = server();
  const c = s.client();
  s.register(c);
  const mail = s.state.mails.find((m) => m.body.includes(c.installId));
  const p = s.linkParams(mail, "approve");

  const page = s.gas.doGet({ parameter: p }).getContent();
  assert.match(page, /Approve/);
  assert.match(page, /reviewDecide/);
  assert.equal(s.cell(c.installId, "Status"), "pending", "visiting the link changes nothing");

  const result = s.gas.reviewDecide(p.rid, p.op, p.exp, p.sig);
  assert.match(result, /approved/);
  assert.equal(s.cell(c.installId, "Status"), "approved");
  assert.equal(s.cell(c.installId, "DecidedBy"), "e-mail link");
  const userMail = s.state.mails.find((m) => m.to === "ali@example.com");
  assert.ok(userMail, "user notified");
  assert.match(userMail.subject, /доступ/, "in the user's language (Russian)");

  const reply = s.status(c);
  assert.equal(reply.status, "approved");
  const f = reply.token.split("|");
  assert.deepEqual(f.slice(0, 6), ["AUTOPDF-LIC", "1", c.installId, c.machine, "approved", reply.requestId]);
  assert.equal(Number(f[7]) - Number(f[6]), 14 * 86400);
  assert.equal(Number(f[6]), Number(reply.serverTime));

  const key = crypto.createPublicKey({ key: Buffer.from(reply.publicKey, "base64"), format: "der", type: "spki" });
  assert.ok(crypto.verify("sha256", Buffer.from(reply.token), key, Buffer.from(reply.sig, "base64")));
  assert.ok(!crypto.verify("sha256", Buffer.from(reply.token + "x"), key, Buffer.from(reply.sig, "base64")));
  assert.equal(reply.keyId, crypto.createHash("sha256").update(Buffer.from(reply.publicKey, "base64")).digest("hex").substring(0, 16));
  assert.equal(key.export({ type: "spki", format: "der" }).toString("base64"),
    s.publicKey.export({ type: "spki", format: "der" }).toString("base64"), "public key derived from the private key");
});

test("approval links: tampering, swapped operation and expiry are refused", () => {
  const s = server();
  const c = s.client();
  s.register(c);
  const mail = s.state.mails.find((m) => m.body.includes(c.installId));
  const a = s.linkParams(mail, "approve");
  const r = s.linkParams(mail, "reject");

  assert.throws(() => s.gas.reviewDecide(a.rid, "approve", a.exp, r.sig), /bad signature/);
  assert.throws(() => s.gas.reviewDecide(a.rid, "approve", String(Number(a.exp) + 999), a.sig), /bad signature/);
  assert.throws(() => s.gas.reviewDecide(a.rid, "approve", a.exp, "0".repeat(64)), /bad signature/);
  assert.throws(() => s.gas.reviewDecide("not-a-uuid", "approve", a.exp, a.sig), /bad link/);
  assert.match(s.gas.doGet({ parameter: { ...a, sig: "f".repeat(64) } }).getContent(), /not valid/);

  s.state.now += 8 * 86400 * 1000;
  assert.throws(() => s.gas.reviewDecide(a.rid, a.op, a.exp, a.sig), /expired/);
  assert.equal(s.cell(c.installId, "Status"), "pending");
});

test("reject and revoke: status column edits and the menu (owner only)", () => {
  const s = server();
  const c = s.client();
  s.register(c);
  const sh = s.requests();
  const row = s.rowOf(c.installId);

  // Edit in the sheet (installable trigger with real Range objects).
  sh.set(row, s.gas.COL.Status, "approved");
  s.gas.onEditInstalled({ range: sh.getRange(row, s.gas.COL.Status), oldValue: "pending", value: "approved" });
  assert.equal(s.status(c).status, "approved");
  assert.ok(s.cell(c.installId, "DecidedAt"));

  sh.set(row, s.gas.COL.Status, "maybe");
  s.gas.onEditInstalled({ range: sh.getRange(row, s.gas.COL.Status), oldValue: "approved", value: "maybe" });
  assert.equal(s.cell(c.installId, "Status"), "approved", "invalid values are reverted");

  // A forged "event" (plain data, e.g. via google.script.run) is ignored.
  s.gas.onEditInstalled({ range: { getRow: 1 }, value: "rejected" });
  assert.equal(s.cell(c.installId, "Status"), "approved");

  // Menu: owner rejects the selected row → revoked.
  s.state.activeSheet = sh;
  sh.setActiveRange(sh.getRange(row, 1));
  s.gas.menuReject();
  const reply = s.status(c);
  assert.equal(reply.status, "rejected");
  assert.equal(reply.token, undefined);

  // Anyone else (anonymous web visitor) cannot use the menu functions.
  s.state.activeUser = "";
  assert.throws(() => s.gas.menuApprove(), /Only the owner/);
  assert.equal(s.cell(c.installId, "Status"), "rejected");
});

test("every public function is either an entry point or owner-only", () => {
  const s = server();
  s.state.activeUser = ""; // anonymous visitor of the web app
  const allowed = new Set(["doGet", "doPost", "reviewDecide", "onEditInstalled", "onOpen"]);
  const source = fs.readFileSync(path.join(__dirname, "..", "Code.gs"), "utf8");
  const publics = [...source.matchAll(/^function ([A-Za-z0-9]+)\(/gm)].map((m) => m[1]).filter((n) => !n.endsWith("_"));
  assert.ok(publics.includes("saveSigningKey") && publics.includes("setup"));
  for (const name of publics) {
    if (allowed.has(name)) continue;
    assert.throws(() => s.gas[name]("x"), /Only the owner/, name + " must call requireOwner_()");
  }
  s.state.activeUser = "intruder@example.com";
  assert.throws(() => s.gas.saveSigningKey("x"), /Only the owner/);
});

test("approved without a signing key: no licence is issued", () => {
  const s = server({ keys: false });
  const c = s.client();
  s.register(c);
  const sh = s.requests();
  const row = s.rowOf(c.installId);
  sh.set(row, s.gas.COL.Status, "approved");
  const reply = s.status(c);
  assert.equal(reply.status, "approved");
  assert.equal(reply.token, undefined);
  assert.match(reply.message, /signing key/);
});

test("sheet safety: formulas are stored as text, control characters removed", () => {
  const s = server();
  const c = s.client();
  s.register(c, { name: "=HYPERLINK(\"http://evil\",\"x\")", host: "@cmd\u0007" });
  // The mock strips the apostrophe exactly like Sheets; check what was written instead.
  const written = [];
  const sh = s.requests();
  const orig = sh.set.bind(sh);
  sh.set = (r, col, v) => { written.push(v); orig(r, col, v); };
  s.register(s.client(), { name: "+1+1" });
  assert.ok(written.includes("'+1+1"));
  assert.equal(s.cell(c.installId, "Host"), "@cmd");
});

test("legacy 2.x clients still get OK", () => {
  const s = server();
  const out = s.gas.doPost({ parameter: { action: "register", name: "Old", email: "old@example.com" } }).getContent();
  assert.equal(out, "OK");
  assert.equal(s.ss.getSheetByName("Users").cell(1, 2), "Old");
});

test("setup is idempotent and installs one trigger; configuration report", () => {
  const s = server();
  s.gas.setup();
  s.gas.setup();
  assert.equal(s.state.triggers.length, 1);
  s.gas.doGet({ parameter: {} });
  s.gas.menuCheck();
  const report = s.state.alerts.pop();
  assert.match(report, /OK {2}Signing key/);
  assert.match(report, /OK {2}Web app URL: https:\/\/script.google.com\/macros\/s\/DEPLOYMENT\/exec/);
  assert.match(report, /OK {2}Status-column trigger/);
});

test("signing key dialog: WebCrypto-generated key saved via the owner-only function", () => {
  const s = server({ keys: false });
  s.gas.menuKeys();
  assert.match(s.state.dialog.html, /crypto\.subtle\.generateKey/);
  assert.match(s.state.dialog.html, /saveSigningKey/);
  const { privateKey } = crypto.generateKeyPairSync("rsa", { modulusLength: 3072 });
  const msg = s.gas.saveSigningKey(privateKey.export({ type: "pkcs8", format: "pem" }));
  assert.match(msg, /Key ID [0-9a-f]{16}/);
  assert.throws(() => s.gas.saveSigningKey("-----BEGIN PRIVATE KEY-----\nAAAA\n-----END PRIVATE KEY-----"));
});
