/************************************************************************************************
 * AutoPDF Pro — registration back end (Google Apps Script)
 *
 * Runs inside the AUTHOR's own Google account. It receives a registration from the plug-in, e-mails
 * the author and appends the user to the bound Google Sheet. Nothing here is shipped inside the
 * plug-in, so the author's account stays private; the plug-in only knows the public /exec URL.
 *
 * Setup (see installer/registration/README.md):
 *   1. Create a Google Sheet, open Extensions ▸ Apps Script and paste this file.
 *   2. Change NOTIFY_EMAIL and ADMIN_KEY below.
 *   3. Deploy ▸ New deployment ▸ Web app ▸ Execute as: Me ▸ Who has access: Anyone ▸ Deploy.
 *   4. Copy the /exec URL and put it in RegistrationConfig.Endpoint (or send it to be built in).
 ************************************************************************************************/

// ---- Settings (change these two) --------------------------------------------------------------

/** Where the "new user" e-mail is sent. */
var NOTIFY_EMAIL = 'Oybekkushmuratov88@gmail.com';

/** A secret word you invent. It is asked for by AUTOPDFADMIN before the user list is shown. */
var ADMIN_KEY = 'CHANGE-ME';

/** Sheet tab that stores the users. */
var SHEET_NAME = 'Users';

var HEADER = ['Date', 'Name', 'Email', 'Phone', 'Computer', 'Windows user', 'Machine id', 'OS', 'Version', 'Language'];

// ---- Registration (POST from the plug-in) -----------------------------------------------------

function doPost(e) {
  try {
    var p = (e && e.parameter) ? e.parameter : {};
    if (p.action && p.action !== 'register') return text('ERROR: unknown action');

    var email = trim(p.email);
    if (!email) return text('ERROR: no email');

    var row = {
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

    appendRow(row);
    sendMail(row);
    return text('OK');
  } catch (err) {
    return text('ERROR: ' + err);
  }
}

// ---- User list (GET ?action=list&key=… from AUTOPDFADMIN) --------------------------------------

function doGet(e) {
  var p = (e && e.parameter) ? e.parameter : {};
  if (p.action === 'list') {
    if (trim(p.key) !== ADMIN_KEY) return text('FORBIDDEN');
    return text(listUsers());
  }
  return text('OK'); // health check
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

function appendRow(r) {
  sheet().appendRow([r.date, r.name, r.email, r.phone, r.computer, r.user, r.machineId, r.os, r.version, r.lang]);
}

function sendMail(r) {
  var when = Utilities.formatDate(r.date, Session.getScriptTimeZone(), 'yyyy-MM-dd HH:mm');
  var subject = 'AutoPDF Pro — yangi foydalanuvchi: ' + (r.name || r.email);
  var body =
    'AutoPDF Pro plaginidan yangi foydalanuvchi ro‘yxatdan o‘tdi.\n\n' +
    'Ism: ' + r.name + '\n' +
    'E-pochta: ' + r.email + '\n' +
    'Telefon: ' + (r.phone || '-') + '\n' +
    'Kompyuter: ' + r.computer + '\n' +
    'Windows foydalanuvchisi: ' + r.user + '\n' +
    'Versiya: ' + r.version + '\n' +
    'Til: ' + r.lang + '\n' +
    'Sana: ' + when + '\n\n' +
    'Ya’ni "' + (r.name || r.email) + '" foydalanuvchisi "' + r.computer + '" kompyuteridan kirdi.';
  MailApp.sendEmail(NOTIFY_EMAIL, subject, body);
}

function listUsers() {
  var sh = sheet();
  var last = sh.getLastRow();
  var out = ['OK'];
  if (last > 1) {
    // Columns: Date, Name, Email, Phone, Computer, Windows user, …  → return the first six + Version.
    var values = sh.getRange(2, 1, last - 1, HEADER.length).getValues();
    for (var i = 0; i < values.length; i++) {
      var v = values[i];
      var date = (v[0] instanceof Date)
        ? Utilities.formatDate(v[0], Session.getScriptTimeZone(), 'yyyy-MM-dd HH:mm')
        : String(v[0]);
      out.push([date, cell(v[1]), cell(v[2]), cell(v[3]), cell(v[4]), cell(v[5]), cell(v[8])].join('\t'));
    }
  }
  return out.join('\n');
}

/** Remove tabs/newlines so a field never breaks the tab-separated reply. */
function cell(value) {
  return String(value == null ? '' : value).replace(/[\t\r\n]+/g, ' ').trim();
}

function trim(value) {
  return (value == null ? '' : String(value)).trim();
}

function text(s) {
  return ContentService.createTextOutput(s).setMimeType(ContentService.MimeType.TEXT);
}
