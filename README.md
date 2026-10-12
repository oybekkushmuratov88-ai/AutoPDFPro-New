# AutoPDF Pro 3.0 — AutoCAD 2021–2026 and ZWCAD 2024–2026 plug-in

**Author: Kushmuratov Oybek** · © 2026 Kushmuratov Oybek. All rights reserved.

Finds drawing-sheet frames in model space and exports **each sheet to its own PDF** (`01.pdf`, `02.pdf`, …)
with the correct paper size, orientation, scale and plot style. User interface in **O‘zbekcha, Русский
and English** (switch in *Sozlamalar → Til · Язык · Language*); works in localized CAD versions.

* Command: **`AUTOPDF`** — the same in every supported application. Also `AUTOPDFLICENSE` (installation ID,
  approval status, "check now"), `AUTOPDFMEDIA` (plotter diagnostics), `AUTOPDFLOG` (open the log folder).
* Use requires the **administrator's approval** (registration on the first `AUTOPDF`, see
  [Licensing](#licensing--litsenziya)).
* Windows 10/11 64-bit.

| Application | Versions | Plug-in assembly | Runtime |
|---|---|---|---|
| AutoCAD (and verticals) | 2021, 2022, 2023, 2024 (R24.0–R24.3) | `AutoPDF.AutoCAD.dll` | .NET Framework 4.8 |
| AutoCAD (and verticals) | 2025, 2026 (R25.0–R25.1) | `AutoPDF.AutoCAD.R25.dll` | .NET 8 |
| ZWCAD | 2024 / 2025 / 2026 | `AutoPDF.ZWCAD.2024.dll` / `.2025` / `.2026` | .NET Framework |

Every application gets an assembly compiled against **its own** SDK; AutoCAD DLLs are never loaded into
ZWCAD. Details, loading mechanisms and limits: [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).

## Features

| | |
|---|---|
| Sheet detection | Closed 4-vertex rectangular polylines (no arcs, axis-aligned) or title-block **blocks**. Modes: manual selection · block name (wildcards) · sample frame (whole drawing) · sample frame + range. Inner borders, duplicates and "boundary" rectangles are filtered out. |
| Paper sizes | ISO A0–A5, ГОСТ extended formats (A4x3 = 297×630, A3x3 = 420×891, A2x3 = 594×1261 …) and your own list (default `630x297; 891x420; 1260x594`). Scale detected automatically (1:1, 1:100 …) or fixed; INSUNITS respected. |
| Sorting | Left → right (row by row) or top → bottom (column by column); any start number. |
| Plotting | The CAD **plot API** (PlotEngine — no prompt sequences): DWG To PDF.pc3, exact paper, millimetres, window, orientation, centred, exact scale, monochrome.ctb, plot styles, lineweights. On AutoCAD only, a localized `-PLOT` fallback if the API fails. Your drawing's page setups are **never modified**. |
| Shared PDF pipeline | The same verification in every application: file exists and is a valid PDF, page size and orientation checked (re-plotted once with the other rotation if it came out turned), non-standard sizes cropped to the exact size, final size checked. |
| Custom sizes | Exact paper if a plotter provides it, otherwise plotted on a larger full-bleed sheet and the PDF is cropped ([details](docs/CUSTOM_PAPER_SIZES.md)). |
| Preview | After each PDF: **Bekor qilish** (cancel) · **Tasdiqlash** (confirm) · **Tasdiqlash va qolganlarini avtomatik yaratish** (confirm and do the rest). |
| Errors | Logged with sheet number and exact step; the batch continues (or stops, if configured); final report. |
| Host detection | Product, version, release and runtime are detected at start-up and logged; a build loaded into the wrong application refuses to run and says why. |
| Licensing | Registration (name, e-mail, generated installation ID) → administrator approves or rejects (e-mail links, the Google Sheet or its menu) → signed licence. PDF export stays disabled until approved. |

## Installation

1. Close AutoCAD and ZWCAD.
2. Run **`AutoPDFPro-3.0.2-Setup.exe`** → language (O‘zbekcha / Русский / English) → the components page
   preselects the applications found on the computer (*AutoCAD 2021–2026*, *ZWCAD 2024*, *2025*, *2026*) →
   *O‘rnatish* / *Установить* / *Install*. Windows asks for administrator rights.
3. Start AutoCAD or ZWCAD and type **`AUTOPDF`**. The first time, fill in the registration form; PDF
   creation works once the administrator approves (you also get an e-mail).

What Setup installs:

* AutoCAD: `C:\Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle` (one bundle with the R24 and
  R25 builds; AutoCAD's autoloader picks the right one).
* ZWCAD: `C:\Program Files\AutoPDF Pro\ZWCAD\<year>\`, registered to load at ZWCAD start-up
  (`HKLM`/`HKCU\SOFTWARE\ZWSOFT\ZWCAD\<year>\<language>\Applications\AutoPDFPro`). If your ZWCAD does not
  load it, add the DLL once with `APPLOAD` → *Startup Suite* (or type `NETLOAD`) — see
  [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md#zwcad-loading).
* Other AutoPDF bundles are moved to `%LOCALAPPDATA%\AutoPDFPro\Backup\<date-time>` so `AUTOPDF` is defined
  only once.

Setup.exe is not code-signed, so Windows SmartScreen may show *“Windows protected your PC”*: click *More info →
Run anyway*. Silent installation: `AutoPDFPro-3.0.2-Setup.exe /S` (installs for the detected applications).

**Uninstall:** *Settings → Apps* → **AutoPDF Pro (AutoCAD, ZWCAD)** → *Uninstall*
(silent: `"C:\Program Files\AutoPDF Pro\Uninstall.exe" /S`). Settings, logs and the licence state are kept
unless you answer *Yes* to the last question.

## Usage (O‘zbekcha qisqacha)

1. **`AUTOPDF`** buyrug‘ini kiriting. Birinchi marta — ro‘yxatdan o‘tish oynasi (ism, e-pochta, o‘rnatish ID,
   rozilik). Administrator ruxsat bergunicha «Tasdiq kutilmoqda» oynasi ko‘rinadi.
2. **Aniqlash usuli**ni tanlang:
   * *Listlarni qo‘lda tanlash* — ramkalarni o‘zingiz belgilaysiz;
   * *Blok nomi bo‘yicha* — masalan `Ramka_A3, Format*`;
   * *Namuna ramka bo‘yicha* — bitta ramkani bosasiz, chizmadagi shu qatlamdagi barcha ramkalar topiladi;
   * *Namuna + diapazon* — namuna, so‘ng hududni tanlaysiz.
3. **Skanerlash** → jadvalda listlar paydo bo‘ladi (format, masshtab, holat).
4. **Saralash yo‘nalishi** va **Boshlang‘ich raqam**ni belgilang.
5. **Tekshirish** — listlar chizmada raqamlari bilan ko‘rsatiladi; ENTER bilan qaytasiz.
6. **Saqlash papkasi**ni tanlang → **PDF yaratish**.
7. Birinchi PDF ko‘rsatiladi: *Tasdiqlash* / *Tasdiqlash va qolganlarini avtomatik yaratish* / *Bekor qilish*.
8. Oxirida hisobot: nechta yaratildi, qaysi list qaysi bosqichda xato berdi; *Papkani ochish*, *Jurnalni ochish*.

**Sozlamalar**: interfeys tili, plotter (DWG To PDF.pc3), plot uslubi (monochrome.ctb), chiziq qalinliklari,
masshtab (avto yoki qat’iy), qo‘shimcha formatlar, o‘lcham tolerantligi, fayl nomi shabloni (`{N}`, `{DWG}`,
`{FORMAT}`), mavjud fayllar siyosati, xatolik siyosati, ko‘rib chiqish, plot usuli (API / -PLOT — faqat AutoCAD).

Settings: `%APPDATA%\AutoPDFPro\settings.xml`; logs: `%LOCALAPPDATA%\AutoPDFPro\Logs`; licence state:
`%LOCALAPPDATA%\AutoPDFPro\license.txt` (shared by all CAD applications of the Windows user).

## Licensing / Litsenziya

* First `AUTOPDF` → registration form: **name, e-mail, the generated installation ID** (e.g.
  `APDF-7K3M-Q9TX-2FHC-WN4R`) and a consent tick → sent over **HTTPS** to the administrator's Google Apps
  Script; a row appears in the Google Sheet (status *pending*) and the administrator gets an **e-mail** with
  *Approve* / *Reject* links.
* Until approval a persistent **“Pending approval”** window is shown on `AUTOPDF` (it re-checks every minute);
  all PDF creation is disabled.
* The administrator approves or rejects with the e-mail links (a confirmation page, signed and expiring
  links), the sheet's *AutoPDF* menu, or the Status column. The user is e-mailed and notified in the CAD
  application; a later rejection revokes access.
* The status is re-checked on every `AUTOPDF` and every few minutes while the CAD application runs.
* No administrator secret is in the plug-in: approvals are **signed** by a private key that never leaves the
  administrator's Google account; the plug-in only verifies them. Without internet nothing new is granted; an
  existing approval keeps working offline for the grace period (14 days by default).

Administrator setup (≈ 5 min): [server/licensing/README.md](server/licensing/README.md). Design and
security: [docs/LICENSING.md](docs/LICENSING.md).

## Building from source

Requirements: the .NET 8 SDK (or Visual Studio 2022 with ".NET desktop development"). No CAD application is
needed to build: the reference assemblies come from NuGet (`AutoCAD.NET` 24.0.0 and 25.0.1,
`ZWCAD.NetApi` 20.24.0 / 20.25.0 / 20.26.0, compile-time only — never shipped). The official ZWCAD SDK can be
used instead: `-p:ZwcadSdkDir="C:\Program Files\ZWSOFT\ZWCAD 2025"`.

```powershell
# everything: build all CAD builds, tests, zip and Setup.exe (Setup.exe needs NSIS 3: winget install NSIS.NSIS)
powershell -ExecutionPolicy Bypass -File build\build.ps1
```

On Linux or macOS `build/build.sh` builds, runs the .NET tests and the licence-server tests (Node.js) and
compiles Setup.exe (`apt install nsis` / `brew install makensis`).

Output: `artifacts\AutoPDFPro.bundle` (AutoCAD), `artifacts\ZWCAD\<year>` (ZWCAD) and
`dist\AutoPDFPro-3.0.2-Setup.exe`. The installer is `installer\AutoPDFPro.nsi`; its pictures are made by
`installer\art\make_art.py`.

## Documentation

* [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) — supported versions, project layout, how each application loads the plug-in
* [docs/LICENSING.md](docs/LICENSING.md) — registration and approval: protocol, states, security, offline rules
* [server/licensing/README.md](server/licensing/README.md) — deploy the Apps Script back end, set the administrator e-mail
* [docs/TESTING.md](docs/TESTING.md) — automated tests and the in-CAD checklist (test drawing included)
* [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — projects, classes, command flow, plotting pipeline
* [docs/CUSTOM_PAPER_SIZES.md](docs/CUSTOM_PAPER_SIZES.md) — 630×297, 891×420, 1260×594 …
* [docs/DEBUGGING.md](docs/DEBUGGING.md) — logs, step names, `AUTOPDFMEDIA`, debugging in Visual Studio
* [docs/ANALYSIS.md](docs/ANALYSIS.md) — analysis of the original plug-in's failures

## Changelog

* **3.0.2** — Contact phone for requests and suggestions (+998 93 109 12 28) in the *About* window, the installer picture and the *Apps* entry.
* **3.0.1** — The licence server moved to the web app of the *AutoPDF Pro таблица* sheet (new `/exec` URL); the server set-up no longer depends on Google returning the owner's e-mail.
* **3.0.0** — **Multi-CAD**: AutoCAD 2021–2024 (R24), AutoCAD 2025–2026 (R25, .NET 8) and ZWCAD
  2024/2025/2026, each with its own assembly; shared, CAD-independent PDF pipeline; run-time host detection;
  one Setup.exe with a components page and ZWCAD registration. **Licence approval system**: first-launch
  registration with installation ID, Google Sheets + Apps Script back end, administrator e-mail with signed
  approve/reject links, signed licences, pending/rejected screens, periodic re-checks, offline grace,
  throttling. Replaces the 2.2 installer-time registration and `AUTOPDFADMIN`.
* **2.2.0** — Registration during installation (name + e-mail), Google Apps Script notification.
* **2.1.0** — Single-file installer; interface in three languages; author badge and *About* window.
* **2.0.0** — First version of the new implementation.

## Status / known limitations

* The plug-in compiles against every listed SDK and all automated tests pass (see
  [docs/TESTING.md](docs/TESTING.md)); 2.x was confirmed working in AutoCAD 2024. AutoCAD 2021–2023, 2025,
  2026 and ZWCAD have not yet been run on real installations — please use the in-CAD checklist and send the
  log (`AUTOPDFLOG`) if something differs.
* ZWCAD has no `-PLOT` fallback (the API is used; its prompts differ from AutoCAD's) and no managed
  "Esc pressed" API — Esc between sheets is read from the keyboard; *Cancel* in the plot progress dialog
  works everywhere.
* Sheets are searched in **model space**. Paper-space layouts are not exported by this command.
* Rotated (non-orthogonal) frames are plotted by their axis-aligned bounding box (a warning is shown).
* If *DWG To PDF → PDF Options → Open in PDF viewer when done* is enabled, turn it off for batch export.
