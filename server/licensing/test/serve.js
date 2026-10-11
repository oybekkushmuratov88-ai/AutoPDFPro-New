// Serves Code.gs (with the mocked Google services) over HTTP for the C# end-to-end test:
//   node server/licensing/test/serve.js [port]      → prints "READY <url>"
//   POST <url>                    the web app (doPost), like https://script.google.com/.../exec
//   POST /__admin?installId=…&status=approved|rejected|pending   the administrator's decision
"use strict";

const http = require("http");
const crypto = require("crypto");
const { createContext } = require("./gas-mock");

const ctx = createContext();
ctx.state.now = Date.now();
ctx.gas.setup();
const { privateKey } = crypto.generateKeyPairSync("rsa", { modulusLength: 2048 });
ctx.gas.saveSigningKey(privateKey.export({ type: "pkcs8", format: "pem" }));
setInterval(() => { ctx.state.now = Date.now(); }, 200).unref();

const server = http.createServer((req, res) => {
  let body = "";
  req.on("data", (c) => { body += c; });
  req.on("end", () => {
    try {
      const url = new URL(req.url, "http://localhost");
      ctx.state.now = Date.now();
      if (url.pathname === "/__admin") {
        const sh = ctx.ss.getSheetByName("Requests");
        let row = 0;
        for (let r = 2; r <= sh.getLastRow(); r++) if (sh.cell(r, ctx.gas.COL.InstallId) === url.searchParams.get("installId")) row = r;
        if (!row) { res.writeHead(404); res.end("no such installation"); return; }
        const msg = ctx.gas.applyDecision_(sh, row, url.searchParams.get("status"), "e2e");
        res.writeHead(200, { "Content-Type": "text/plain" });
        res.end(msg);
        return;
      }
      const parameter = Object.fromEntries(new URLSearchParams(req.method === "POST" ? body : url.search));
      const out = req.method === "POST" ? ctx.gas.doPost({ parameter }) : ctx.gas.doGet({ parameter });
      res.writeHead(200, { "Content-Type": "text/plain; charset=utf-8" });
      res.end(out.getContent());
    } catch (err) {
      res.writeHead(500);
      res.end(String(err));
    }
  });
});

server.listen(Number(process.argv[2] || 0), "127.0.0.1", () => {
  console.log("READY http://127.0.0.1:" + server.address().port + "/exec");
});
