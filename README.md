# AutoPDF Pro 2.0 — AutoCAD 2024 plug-in

Finds drawing-sheet frames in model space and exports **each sheet to its own PDF** (`01.pdf`, `02.pdf`, …)
with the correct paper size, orientation, scale and plot style. Uzbek user interface, works in the
Russian (and any other) AutoCAD 2024.

* Command: **`AUTOPDF`** (also `AUTOPDFMEDIA` — plotter diagnostics, `AUTOPDFLOG` — open the log folder)
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

## Installation

### From the release zip (`AutoPDFPro-2.0.0.zip`)

1. Close AutoCAD.
2. Unzip, right-click **`install.bat`** → *Run* (asks for administrator rights; installs for all users to
   `C:\Program Files\Autodesk\ApplicationPlugins\AutoPDFPro.bundle`).
   Without admin rights use **`install-peruser.bat`** (installs to `%APPDATA%\Autodesk\ApplicationPlugins`).
3. Start AutoCAD 2024 → the command line shows `AutoPDF Pro v2.0.0 yuklandi` → type **`AUTOPDF`**.

The installer unblocks the DLLs and moves an older AutoPDF Pro bundle to
`%LOCALAPPDATA%\AutoPDFPro\Backup\…` so the `AUTOPDF` command is not defined twice.

### Manual

Copy the folder `AutoPDFPro.bundle` to `C:\Program Files\Autodesk\ApplicationPlugins\` (or
`%APPDATA%\Autodesk\ApplicationPlugins\`), then *Properties → Unblock* the DLLs in
`Contents\Windows\2024` if they came from the internet. Restart AutoCAD.

### Setup.exe

`build\build.ps1` also compiles `installer\AutoPDFPro.iss` when Inno Setup 6 is installed.

### Uninstall

`uninstall.bat` (add `-Purge` to also delete settings and logs).

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

**Sozlamalar**: plotter (DWG To PDF.pc3), plot uslubi (monochrome.ctb), chiziq qalinliklari, masshtab
(avto yoki qat’iy), qo‘shimcha formatlar, o‘lcham tolerantligi, fayl nomi shabloni (`{N}`, `{DWG}`,
`{FORMAT}`), mavjud fayllar siyosati, xatolik siyosati (davom etish / to‘xtatish), ko‘rib chiqish,
plot usuli (API / -PLOT).

Settings are stored in `%APPDATA%\AutoPDFPro\settings.xml`, logs in `%LOCALAPPDATA%\AutoPDFPro\Logs`.

## Building from source

Requirements: Visual Studio 2022 (".NET desktop development") or the .NET 8 SDK. AutoCAD does **not**
need to be installed to build — the AutoCAD 2024 reference assemblies come from the official
`AutoCAD.NET 24.3.0` NuGet package, the .NET 4.8 reference assemblies from NuGet as well.

```powershell
# everything: build, unit tests, bundle, zip (+ Setup.exe if Inno Setup is installed)
powershell -ExecutionPolicy Bypass -File build\build.ps1

# or just
dotnet build AutoPDFPro.sln -c Release
dotnet test  tests\AutoPDF.Core.Tests
```

Output: `artifacts\AutoPDFPro.bundle` (ready to copy) and `dist\AutoPDFPro-2.0.0.zip`.

## Documentation

* [docs/ANALYSIS.md](docs/ANALYSIS.md) — analysis of the old DLLs, root causes of the reported errors
* [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — projects, classes, command flow, plotting pipeline
* [docs/CUSTOM_PAPER_SIZES.md](docs/CUSTOM_PAPER_SIZES.md) — 630×297, 891×420, 1260×594 …
* [docs/DEBUGGING.md](docs/DEBUGGING.md) — logs, step names, `AUTOPDFMEDIA`, Visual Studio debugging

## Status / known limitations

* Built and unit-tested (Core logic) without AutoCAD; the AutoCAD-facing code compiles against the
  official AutoCAD 2024 reference assemblies but must be verified in AutoCAD 2024 with a real drawing.
  Please run `AUTOPDFMEDIA` and one export on the target PC and send the log if anything fails.
* Sheets are searched in **model space**. Paper-space layouts are not exported by this command.
* Rotated (non-orthogonal) frames are plotted by their axis-aligned bounding box (a warning is shown).
* If *DWG To PDF → PDF Options → Open in PDF viewer when done* is enabled, turn it off for batch export.
