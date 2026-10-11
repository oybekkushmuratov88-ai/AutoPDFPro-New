# Testing

## Automated (run on every build)

| Suite | Command | What it covers |
|---|---|---|
| .NET unit tests | `dotnet test tests/AutoPDF.Core.Tests` | paper sizes and scales, media choice, sorting, naming, `-PLOT` answers, localisation (every text in 3 languages), host detection rules, licensing client, **PDF pipeline** |
| PDF pipeline | `PdfPipelineTests` | a fake plotter writes real PDFs: exact **page size** and **orientation** (A4 portrait, A3/A0 landscape), **plot window** = the frame in WCS, **scale** (1:1, 1:50, 1:100), centring, **output folder** created, existing file replaced, page turned → one re-plot with the other rotation (and remembered), still turned → error, **non-standard size cropped** (630×297 from A1), wrong paper → error, API → `-PLOT` fallback rules, no fallback on ZWCAD, cancel, empty plot, missing file |
| Licensing client | `LicensingTests` | installation IDs, key parsing and RSA verification, tokens, register/pending/approve/reject/revoke, forged/foreign/copied licences, offline grace and expiry, clock rollback, throttling, server "check after", pinned key, HTTP client |
| Licence server | `node --test server/licensing/test/*.test.js` | the real `Code.gs` with mocked Google services: field validation, unique request IDs, replayed nonces, throttling, signed expiring links (tampered/expired/swapped), confirmation page does not change state, sheet edits and menu, owner-only functions, formula injection, licence signature verified with Node's crypto |
| End to end | `LicensingEndToEndTests` (needs Node.js; `AUTOPDF_E2E=1` makes it mandatory) | the C# client over HTTP against `Code.gs`: register → pending → approve → signature verifies in .NET → copied licence refused → revoke |
| Installer | Wine (manual, see below) | components preselected from the registry, files, ZWCAD registry entries, backup of old bundles, uninstall cleanup |

`build/build.sh` and `build\build.ps1` run the .NET tests and, when Node.js is installed, the server tests.

## In-CAD checklist (once per application and version)

Open [`tests/fixtures/AutoPDF-test-sheets.dxf`](../tests/fixtures/AutoPDF-test-sheets.dxf) (regenerate with
`python3 tests/fixtures/make_test_drawing.py`). It has six frames on layer `RAMKA`, each with an inner
border that must be ignored, a title block, diagonals and the text `TOP n` near the top edge:

| # | Frame | Expected PDF page | Notes |
|---|---|---|---|
| 01 | 210 × 297 | A4 portrait 210 × 297 mm | |
| 02 | 420 × 297 | A3 landscape 420 × 297 mm | |
| 03 | 841 × 594 | A1 landscape 841 × 594 mm | |
| 04 | 630 × 297 | 630 × 297 mm | plotted on a larger sheet and cropped (or exact on a custom PC3) |
| 05 | 420 × 594 | A2 portrait 420 × 594 mm | |
| 06 | 42000 × 29700 | A3 landscape 420 × 297 mm | scale **1:100** detected |

Steps — tick each for AutoCAD 2021 · 2022 · 2023 · 2024 · 2025 · 2026 · ZWCAD 2024 · 2025 · 2026:

1. **Loading** — the command line shows `AutoPDF Pro v3.0.1 …`; `AUTOPDFLOG` → the log's first line names the
   right host and build (e.g. `ZWCAD 2025 … build: AutoPDF.ZWCAD.2025 for ZWCAD 2025`).
2. **Licence** — first `AUTOPDF` shows the registration form with an installation ID; after sending, the
   *Pending approval* window; approve in the sheet → within a minute the window turns green → *Continue*.
   `AUTOPDFLICENSE` shows *Approved*. Disconnect the network: `AUTOPDF` still works (grace period).
3. **Detection** — *Namuna ramka bo‘yicha*, pick frame 1 → **6** sheets (no inner borders), formats as in the
   table, sheet 6 at 1:100.
4. **Check** — numbers 1…6 left to right on the drawing.
5. **Output folder** — choose a new, not yet existing folder → it is created; files `01.pdf` … `06.pdf`.
6. **PDF output** — every file opens; page sizes as in the table (Acrobat: *File ▸ Properties ▸ Page size*);
   `TOP n` is at the **top** of each page (orientation); the frame lines lie on the page edges (**plot
   area** and centring); sheet 06 fills an A3 page exactly, i.e. 100 drawing units = 1 mm (**scale**).
7. **Summary** — 6 succeeded (04 may say *cropped*); *Papkani ochish* opens the folder.
8. **Run again** with *mavjud fayllar: almashtirish* → files are replaced; with *qayta nomlash* → `01 (2).pdf` …
9. **Revocation** — set the request to `rejected` in the sheet; run `AUTOPDFLICENSE` → *Check now* →
   *Rejected*; `AUTOPDF` is blocked.

Please send `AUTOPDFLOG` output if any step differs; every failure is logged with the sheet number and the
exact step (e.g. `[03] plot:set-media`).

## Installer under Wine (Linux)

```
makensis "-XTarget amd64-unicode" -DOUTFILE=/tmp/Setup-test.exe installer/AutoPDFPro.nsi
wine64 reg add 'HKLM\SOFTWARE\Autodesk\AutoCAD\R24.3\ACAD-7101:409' /f      # pretend AutoCAD 2024
wine64 reg add 'HKLM\SOFTWARE\ZWSOFT\ZWCAD\2025\en-US' /f                  # pretend ZWCAD 2025
wine64 /tmp/Setup-test.exe /S
```

Expected: `Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle` with `R24` and `R25`;
`Program Files\AutoPDF Pro\ZWCAD\2025` only; `…\ZWCAD\2025\en-US\Applications\AutoPDFPro` (HKLM and HKCU) with
`LOADER`, `LOADCTRLS=2`, `MANAGED=1`; `Uninstall.exe /S` removes all of it. (The amd64 stub is only for Wine
prefixes without 32-bit support; the shipped Setup.exe is the standard x86 NSIS stub.)
