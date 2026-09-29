// AutoPDF Pro - registration back end (Google Apps Script). Paste this whole file.
// Change the two values below, then Deploy as a Web app (Execute as: Me, Access: Anyone).

var NOTIFY_EMAIL = "Oybekkushmuratov88@gmail.com";
var ADMIN_KEY = "CHANGE-ME";

function sheet_() {
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  return ss.getSheetByName("Users") || ss.insertSheet("Users");
}

function out_(s) {
  return ContentService.createTextOutput(s).setMimeType(ContentService.MimeType.TEXT);
}

function doPost(e) {
  try {
    var p = e.parameter;
    sheet_().appendRow([new Date(), p.name, p.email, p.phone, p.computer, p.user, p.version]);
    var body = p.name + " (" + p.email + ") - " + p.computer + " kompyuteridan kirdi.";
    MailApp.sendEmail(NOTIFY_EMAIL, "AutoPDF Pro: yangi foydalanuvchi", body);
    return out_("OK");
  } catch (err) {
    return out_("ERROR: " + err);
  }
}

function doGet(e) {
  if (e.parameter.action != "list") return out_("OK");
  if (e.parameter.key != ADMIN_KEY) return out_("FORBIDDEN");
  var rows = sheet_().getDataRange().getValues();
  var result = "OK";
  for (var i = 0; i < rows.length; i++) {
    var r = rows[i];
    var d = (r[0] instanceof Date) ? Utilities.formatDate(r[0], Session.getScriptTimeZone(), "yyyy-MM-dd HH:mm") : r[0];
    result += "\n" + [d, r[1], r[2], r[3], r[4], r[5], r[6]].join("\t");
  }
  return out_(result);
}
