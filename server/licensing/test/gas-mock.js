// In-memory stand-ins for the Google Apps Script services used by Code.gs, so the real file can be run
// and tested with Node (node --test server/licensing/test). Only what Code.gs uses is implemented.
"use strict";

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const toSigned = (buf) => Array.from(buf, (b) => (b > 127 ? b - 256 : b));
const toBuffer = (bytes) => (typeof bytes === "string" ? Buffer.from(bytes, "utf8") : Buffer.from(bytes.map((b) => (b < 0 ? b + 256 : b))));

class Range {
  constructor(sheet, row, col, rows, cols) {
    Object.assign(this, { sheet, row, col, rows, cols });
  }
  getSheet() { return this.sheet; }
  getRow() { return this.row; }
  getLastRow() { return this.row + this.rows - 1; }
  getColumn() { return this.col; }
  getLastColumn() { return this.col + this.cols - 1; }
  getValues() {
    const out = [];
    for (let r = 0; r < this.rows; r++) {
      const line = [];
      for (let c = 0; c < this.cols; c++) line.push(this.sheet.cell(this.row + r, this.col + c));
      out.push(line);
    }
    return out;
  }
  getValue() { return this.sheet.cell(this.row, this.col); }
  setValues(values) {
    values.forEach((line, r) => line.forEach((v, c) => this.sheet.set(this.row + r, this.col + c, v)));
    return this;
  }
  setValue(v) { this.sheet.set(this.row, this.col, v); return this; }
  setFontWeight() { return this; }
  setNumberFormat() { return this; }
  setDataValidation() { return this; }
  createTextFinder(text) {
    const range = this;
    let whole = false;
    return {
      matchEntireCell(b) { whole = b; return this; },
      findNext() {
        for (let r = 0; r < range.rows; r++) {
          for (let c = 0; c < range.cols; c++) {
            const v = String(range.sheet.cell(range.row + r, range.col + c));
            if (whole ? v === text : v.includes(text)) return new Range(range.sheet, range.row + r, range.col + c, 1, 1);
          }
        }
        return null;
      }
    };
  }
}

class Sheet {
  constructor(ss, name) {
    this.ss = ss;
    this.name = name;
    this.data = []; // rows of values, 1-based in the API
    this.active = null;
  }
  getName() { return this.name; }
  cell(r, c) { return (this.data[r - 1] || [])[c - 1] ?? ""; }
  set(r, c, v) {
    while (this.data.length < r) this.data.push([]);
    const row = this.data[r - 1];
    while (row.length < c) row.push("");
    // Like Sheets: a leading apostrophe only marks text.
    row[c - 1] = typeof v === "string" && v.startsWith("'") ? v.substring(1) : v;
  }
  getRange(row, col, rows = 1, cols = 1) {
    if (typeof row === "string") throw new Error("A1 notation not supported in the mock");
    return new Range(this, row, col, rows, cols);
  }
  appendRow(values) { this.getRange(this.getLastRow() + 1, 1, 1, values.length).setValues([values]); }
  getLastRow() {
    for (let i = this.data.length; i > 0; i--) if (this.data[i - 1].some((v) => v !== "")) return i;
    return 0;
  }
  getMaxRows() { return 1000; }
  setFrozenRows() {}
  deleteRows(start, count) { this.data.splice(start - 1, count); }
  getActiveRange() { return this.active; }
  setActiveRange(r) { this.active = r; }
}

function createContext(options = {}) {
  const state = {
    now: options.now || Date.parse("2026-10-01T12:00:00Z"),
    activeUser: "owner@example.com",
    owner: "owner@example.com",
    mails: [],
    alerts: [],
    promptAnswer: null,
    props: {},
    cache: new Map(),
    triggers: [],
    logs: []
  };

  const ss = {
    sheets: [],
    getSheetByName(n) { return this.sheets.find((s) => s.name === n) || null; },
    insertSheet(n) { const s = new Sheet(this, n); this.sheets.push(s); return s; },
    getId() { return "SHEET-ID"; },
    getUrl() { return "https://docs.google.com/spreadsheets/d/SHEET-ID/edit"; }
  };
  state.ss = ss;

  const RealDate = Date;
  class FakeDate extends RealDate {
    constructor(...args) { if (args.length === 0) super(state.now); else super(...args); }
    static now() { return state.now; }
  }

  const ui = {
    alert(m) { state.alerts.push(String(m)); },
    prompt() {
      return { getSelectedButton: () => "OK", getResponseText: () => state.promptAnswer || "" };
    },
    Button: { OK: "OK" },
    ButtonSet: { OK_CANCEL: "OK_CANCEL" },
    createMenu() { const m = { addItem: () => m, addSeparator: () => m, addToUi: () => { state.menu = true; } }; return m; },
    showModalDialog(out, title) { state.dialog = { html: out.getContent(), title }; }
  };

  const output = (content) => {
    const o = {
      content,
      getContent() { return this.content; },
      setMimeType() { return this; },
      setTitle(t) { this.title = t; return this; },
      setWidth() { return this; },
      setHeight() { return this; }
    };
    return o;
  };

  const context = {
    Date: FakeDate,
    console,
    Logger: { log: (m) => state.logs.push(String(m)) },
    SpreadsheetApp: {
      getActiveSpreadsheet: () => ss,
      openById: (id) => { if (id !== "SHEET-ID") throw new Error("no such sheet"); return ss; },
      getActiveSheet: () => state.activeSheet || ss.sheets[0],
      getUi: () => ui,
      flush: () => {},
      newDataValidation: () => {
        const b = { requireValueInList: () => b, setAllowInvalid: () => b, build: () => ({}) };
        return b;
      }
    },
    PropertiesService: {
      getScriptProperties: () => ({
        getProperty: (k) => (k in state.props ? state.props[k] : null),
        setProperty: (k, v) => { state.props[k] = String(v); },
        setProperties: (o) => { for (const k in o) state.props[k] = String(o[k]); }
      })
    },
    CacheService: {
      getScriptCache: () => ({
        get: (k) => {
          const e = state.cache.get(k);
          return e && e.until > state.now ? e.value : null;
        },
        put: (k, v, seconds) => { state.cache.set(k, { value: String(v), until: state.now + seconds * 1000 }); }
      })
    },
    LockService: { getScriptLock: () => ({ waitLock: () => {}, tryLock: () => true, releaseLock: () => {} }) },
    MailApp: {
      sendEmail: (m) => { state.mails.push(m); },
      getRemainingDailyQuota: () => (state.quota === undefined ? 100 : state.quota)
    },
    Session: {
      getActiveUser: () => ({ getEmail: () => state.activeUser }),
      getEffectiveUser: () => ({ getEmail: () => state.owner }),
      getScriptTimeZone: () => "Asia/Tashkent"
    },
    ScriptApp: {
      getService: () => ({ getUrl: () => "https://script.google.com/macros/s/DEPLOYMENT/exec" }),
      getProjectTriggers: () => state.triggers,
      deleteTrigger: (t) => { state.triggers = state.triggers.filter((x) => x !== t); },
      newTrigger: (fn) => ({
        forSpreadsheet: () => ({ onEdit: () => ({ create: () => { const t = { getHandlerFunction: () => fn }; state.triggers.push(t); return t; } }) })
      })
    },
    ContentService: { createTextOutput: (t) => output(t), MimeType: { TEXT: "TEXT" } },
    HtmlService: { createHtmlOutput: (h) => output(h), XFrameOptionsMode: { DEFAULT: "DEFAULT" } },
    Utilities: {
      DigestAlgorithm: { SHA_256: "sha256" },
      Charset: { UTF_8: "utf8" },
      getUuid: () => crypto.randomUUID(),
      base64Encode: (bytes) => toBuffer(bytes).toString("base64"),
      base64Decode: (s) => toSigned(Buffer.from(s, "base64")),
      computeDigest: (alg, value) => toSigned(crypto.createHash("sha256").update(toBuffer(value)).digest()),
      computeHmacSha256Signature: (value, key) => toSigned(crypto.createHmac("sha256", Buffer.from(key, "utf8")).update(Buffer.from(value, "utf8")).digest()),
      computeRsaSha256Signature: (value, pem) => toSigned(crypto.sign("sha256", Buffer.from(value, "utf8"), pem)),
      formatDate: (d) => new RealDate(d).toISOString().substring(0, 16).replace("T", " ")
    }
  };

  vm.createContext(context);
  const source = fs.readFileSync(path.join(__dirname, "..", "Code.gs"), "utf8");
  vm.runInContext(source, context, { filename: "Code.gs" });
  return { gas: context, state, ss, Sheet, Range };
}

module.exports = { createContext, toSigned, toBuffer };
