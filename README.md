# AutoPDF Pro 2.2 — AutoCAD 2024 plug-in

**Author: Kushmuratov Oybek** · © 2026 Kushmuratov Oybek. All rights reserved.

Finds drawing-sheet frames in model space and exports **each sheet to its own PDF** (`01.pdf`, `02.pdf`, …)
with the correct paper size, orientation, scale and plot style. User interface in **O‘zbekcha, Русский
and English** (switch in *Sozlamalar → Til · Язык · Language*); works in the Russian (and any other)
AutoCAD 2024.

* Command: **`AUTOPDF`** (also `AUTOPDFMEDIA` — plotter diagnostics, `AUTOPDFLOG` — open the log folder,
  `AUTOPDFADMIN` — the author's list of registered users)
* AutoCAD 2024 (R24.3), Windows 10/11 64-bit, .NET Framework 4.8
* Clean new implementation — see [docs/ANALYSIS.md](docs/ANALYSIS.md) for why the old version failed with
  `SetPlotType(copyFrom) -> InvalidInput` and `-PLOT buyrug'i PDF yaratmadi`, and how this one avoids it.

## Features

| | |
|---|---|
| Sheet detection | Closed 4-vertex rectangular LWPOLYLINE (no arcs, axis-aligned) or title-block **blocks**. Modes: manual selection · block name (wildcards) · sample frame (whole drawing) · sample frame + range. Inner borders, duplicates and "boundary" rectangles are filtered out. |
| Paper sizes | ISO A0–A5, ГОСТ extended formats (A4x3 = 297×630, A3x3 = 420×891, A2x3 = 594×1261 …) and your own list (default `630x297; 891x420; 1260x594`). Scale detected automatically (1:1, 1:100 …) or fixed; INSUNITS respected. |
| Sorting | Left → right (row by row) or top → bottom (column by column); any start number. |
| Table | `# · Blok · Eni · Bo‘yi · Qog‘oz · Holat`, e.g. `1 · Rectangle · 210 · 297 · 210 x 297 mm · Tayyor`. |
| Plotting | AutoCAD plot API (no prompt sequences): DWG To PDF.pc3, exact paper, millimetres, window, orientation, centred, exact scale, monochrome.ctb, plot styles, lineweights. Robust localized `-PLOT` fallback. Your drawing's page setups are **never modified**. |
| Custom sizes | Exact paper if a plotter provides it, otherwise plotted on a larger full-bleed sheet and the PDF is cropped to the exact size ([details](docs/CUSTOM_PAPER_SIZES.md)). |
| Verification | File exists, not empty, valid PDF, page size/orientation checked (auto-corrected once if turned). |
| Preview | After each PDF: **Bekor qilish** (cancel) · **Tasdiqlash** (confirm) · **Tasdiqlash va qolganlarini avtomatik yaratish** (confirm and do the rest). |
| Errors | Logged with sheet number and exact step; the batch continues (or stops, if configured); final report. |
| Languages | O‘zbekcha (default), Русский, English — every window, message, command-line prompt and log line. |
| About | The author's photo and name in the top-right corner of the main window; click it for the *About* window. |
| Registration | Optional, author-configured: the user registers once (name + e-mail) and the author is e-mailed and sees every user. Off unless the author sets it up — see [Registration](#registration--royxatdan-otish). |

## Installation

### Setup.exe (recommended)

1. Close AutoCAD.
2. Run **`AutoPDFPro-2.2.0-Setup.exe`** → choose the language (O‘zbekcha / Русский / English) →
   *O‘rnatish* / *Установить* / *Install*. Windows asks for administrator rights.
3. Start AutoCAD 2024 and type **`AUTOPDF`**.

Setup installs `C:\Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle` for all users of the computer,
moves other AutoPDF bundles (the old plug-in, per-user copies) to `%LOCALAPPDATA%\AutoPDFPro\Backup\<date-time>`
so the `AUTOPDF` command is not defined twice, and on a first installation starts the plug-in in the language
chosen for Setup. It waits until AutoCAD is closed (AutoCAD locks the DLLs) and warns when AutoCAD 2024 is not
installed.

Setup.exe is not code-signed, so Windows SmartScreen may show *“Windows protected your PC”* /
*«Система Windows защитила ваш компьютер»*: click *More info → Run anyway* / *Подробнее → Выполнить в любом
случае*.

Silent installation: `AutoPDFPro-2.2.0-Setup.exe /S`.

### From the release zip (`AutoPDFPro-2.2.0.zip`)

1. Close AutoCAD.
2. Unzip, right-click **`install.bat`** → *Run* (asks for administrator rights; installs for all users to
   `C:\Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle`).
   Without admin rights use **`install-peruser.bat`** (installs to `%APPDATA%\Autodesk\ApplicationPlugins`).
3. Start AutoCAD 2024 → the command line shows `AutoPDF Pro v2.2.0 yuklandi` → type **`AUTOPDF`**.

The script unblocks the DLLs and moves an older AutoPDF Pro bundle to
`%LOCALAPPDATA%\AutoPDFPro\Backup\…` so the `AUTOPDF` command is not defined twice.

### Manual

Copy the folder `AutoPDFPro.bundle` to `C:\Program Files\Autodesk\ApplicationPlugins\` (or
`%APPDATA%\Autodesk\ApplicationPlugins\`), then *Properties → Unblock* the DLLs in
`Contents\Windows\2024` if they came from the internet. Restart AutoCAD.

### Uninstall

* Installed with Setup.exe: *Settings → Apps* (or *Control Panel → Programs and Features*) →
  **AutoPDF Pro (AutoCAD 2024)** → *Uninstall*. Settings and logs are kept unless you answer *Yes* to the last
  question. Silent: `"C:\Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle\Uninstall.exe" /S`.
* Installed from the zip: `uninstall.bat` (add `-Purge` to also delete settings and logs).

## Usage (O‘zbekcha qisqacha)

1. **`AUTOPDF`** buyrug‘ini kiriting.
2. **Aniqlash usuli**ni tanlang:
   * *Listlarni qo‘lda tanlash* — ramkalarni o‘zingiz belgilaysiz;
   * *Blok nomi bo‘yicha* — masalan `Ramka_A3, Format*`;
   * *Namuna ramka bo‘yicha* — bitta ramkani bosasiz, chizmadagi shu qatlamdagi barcha ramkalar topiladi;
   * *Namuna + diapazon* — namuna, so‘ng hududni tanlaysiz.
3. **Skanerlash** → jadvalda listlar paydo bo‘ladi (format, masshtab, holat).
4. **Saralash yo‘nalishi** va **Boshlang‘ich raqam**ni belgilang (jadval darhol qayta raqamlanadi).
5. **Tekshirish** — listlar chizmada raqamlari bilan ko‘rsatiladi; ENTER bilan qaytasiz.
6. **Saqlash papkasi**ni tanlang → **PDF yaratish**.
7. Birinchi PDF ko‘rsatiladi: *Tasdiqlash* / *Tasdiqlash va qolganlarini avtomatik yaratish* / *Bekor qilish*.
8. Oxirida hisobot: nechta yaratildi, qaysi list qaysi bosqichda xato berdi; *Papkani ochish*, *Jurnalni ochish*.

**Sozlamalar**: interfeys tili (O‘zbekcha / Русский / English), plotter (DWG To PDF.pc3), plot uslubi (monochrome.ctb), chiziq qalinliklari, masshtab
(avto yoki qat’iy), qo‘shimcha formatlar, o‘lcham tolerantligi, fayl nomi shabloni (`{N}`, `{DWG}`,
`{FORMAT}`), mavjud fayllar siyosati, xatolik siyosati (davom etish / to‘xtatish), ko‘rib chiqish,
plot usuli (API / -PLOT).

Settings are stored in `%APPDATA%\AutoPDFPro\settings.xml`, logs in `%LOCALAPPDATA%\AutoPDFPro\Logs`.

## Building from source

Requirements: Visual Studio 2022 (".NET desktop development") or the .NET 8 SDK. AutoCAD does **not**
need to be installed to build — the AutoCAD 2024 reference assemblies come from the official
`AutoCAD.NET 24.3.0` NuGet package, the .NET 4.8 reference assemblies from NuGet as well.

```powershell
# everything: build, unit tests, bundle, zip and Setup.exe (Setup.exe needs NSIS 3: winget install NSIS.NSIS)
powershell -ExecutionPolicy Bypass -File build\build.ps1

# or just
dotnet build AutoPDFPro.sln -c Release
dotnet test  tests\AutoPDF.Core.Tests
```

On Linux or macOS `build/build.sh` builds, tests and compiles Setup.exe (`apt install nsis` /
`brew install makensis`).

Output: `artifacts\AutoPDFPro.bundle` (ready to copy), `dist\AutoPDFPro-2.2.0.zip` and
`dist\AutoPDFPro-2.2.0-Setup.exe`. The installer is described by `installer\AutoPDFPro.nsi`; its pictures are
made by `installer\art\make_art.py`.

## Documentation

* [docs/ANALYSIS.md](docs/ANALYSIS.md) — analysis of the old DLLs, root causes of the reported errors
* [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — projects, classes, command flow, plotting pipeline
* [docs/CUSTOM_PAPER_SIZES.md](docs/CUSTOM_PAPER_SIZES.md) — 630×297, 891×420, 1260×594 …
* [docs/DEBUGGING.md](docs/DEBUGGING.md) — logs, step names, `AUTOPDFMEDIA`, Visual Studio debugging
* [docs/REGISTRATION.md](docs/REGISTRATION.md) — the registration flow, the web app, `AUTOPDFADMIN`
* [installer/registration/README.md](installer/registration/README.md) — deploy the Google Apps Script (5 min)

## Registration / Ro‘yxatdan o‘tish

Optional and **off by default**. When the author sets it up, the user registers once (name + e-mail) the
first time they run `AUTOPDF`; the details, plus this computer's name, the Windows user and the version, are
shown to the user, sent on consent to a Google Apps Script the author deploys, and the author is e-mailed and
sees every user in a Google Sheet (or in AutoCAD via `AUTOPDFADMIN` + the admin key).

* No password or secret ships in the plug-in — the e-mail is sent by the author's own Google account; the
  plug-in only holds the public web-app URL.
* The user's Google password is never requested; they type their e-mail and give consent.
* A workstation with restricted internet is not locked out: the registration is stored locally and re-sent
  automatically once the machine is online.

To switch it on, deploy [installer/registration/Code.gs](installer/registration/Code.gs) (steps in
[installer/registration/README.md](installer/registration/README.md)) and put its `/exec` URL in
`RegistrationConfig.Endpoint`. While the URL is the placeholder, registration is disabled and the plug-in
works exactly as before.

## Languages / Tillar / Языки

*Sozlamalar → Til · Язык · Language* → **O‘zbekcha**, **Русский** or **English** → *Saqlash*. The window
reopens in the chosen language; the choice is remembered (settings file) and also used for the
command-line prompts and the log. All texts live in `src/AutoPDF.Core/Localization/L*.cs`, each text with
its three translations side by side — to correct a translation, edit it there and rebuild.

## Changelog

* **2.2.0** — Optional, author-configured **registration**: a first-run window (name + e-mail + consent,
  showing exactly what is sent), a Google Apps Script back end that e-mails the author and lists users in a
  Google Sheet, mandatory-registration gate with offline re-send, and the `AUTOPDFADMIN` user list. Disabled
  until the author sets the web-app URL, so the plug-in is unchanged for everyone else.
* **2.1.0** — Single-file installer `AutoPDFPro-2.1.0-Setup.exe` (O‘zbekcha / Русский / English, AutoCAD
  checks, moves older versions aside, uninstaller in *Apps*). Interface in three languages (O‘zbekcha / Русский / English) with a language switch in
  Settings; author badge with photo in the main window and an *About* window (Kushmuratov Oybek); author
  and copyright in the DLL properties, the bundle manifest and the installer; wider left panel and
  wrapping labels (the “Namuna ramka + diapazon” option was cut off).
* **2.0.0** — First version of the new implementation.

## Status / known limitations

* Version 2.0.0 was confirmed working in AutoCAD 2024 by the author. 2.1.0 adds the languages, the author
  badge/About window and the language setting; the detection and plotting logic is unchanged — only its
  messages are now translated.
* Sheets are searched in **model space**. Paper-space layouts are not exported by this command.
* Rotated (non-orthogonal) frames are plotted by their axis-aligned bounding box (a warning is shown).
* If *DWG To PDF → PDF Options → Open in PDF viewer when done* is enabled, turn it off for batch export.
