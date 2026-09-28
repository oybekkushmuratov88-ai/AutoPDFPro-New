# Debugging AutoPDF Pro

## 1. Logs (first place to look)

* File: `%LOCALAPPDATA%\AutoPDFPro\Logs\AutoPDFPro_YYYY-MM-DD.log` — command `AUTOPDFLOG` opens the folder;
  *Sozlamalar → Jurnal papkasini ochish* does the same.
* Every line carries the sheet and the exact step:

  ```text
  2026-09-28 14:03:11.204 [ERROR] [List 03] [plot:set-media] eInvalidInput — …
  2026-09-28 14:03:11.310 [WARN ] [List 03] [plot:engine-begin-document] Plot API xatosi: … → -PLOT buyrug‘i bilan urinilmoqda.
  2026-09-28 14:03:12.020 [INFO ] [List 03] [cmd-plot] -PLOT javoblari: _.-PLOT | _Yes | <Enter> | DWG To PDF.pc3 | …
  ```

* *Batafsil jurnal* (on by default) adds DEBUG lines: every plot step, the validated plot settings
  (device, paper, rotation, scale, DCS window, style table), the PDF page box and /Rotate, crop boxes.
* The command line shows INFO/WARN/ERROR lines (prefix `AutoPDF:`) while the command runs.

### Step names

| Step | Meaning |
|---|---|
| `prepare:paper` | Frame has no valid paper size (too small/large). |
| `prepare:output-file` | Folder not writable or the PDF is open in a viewer. |
| `media:select` | No suitable paper on the plotter (see `AUTOPDFMEDIA`). |
| `plot:set-device` … `plot:set-flags` | Individual `PlotSettingsValidator` calls. |
| `plot:validate` | `PlotInfoValidator.Validate`. |
| `plot:engine-*` | PlotEngine stages (begin-plot, begin-document, begin-page, generate-graphics …). |
| `cmd-plot:*` | `-PLOT` fallback (`command` = AutoCAD error, `no-file` = sequence did not finish). |
| `verify:file`, `verify:page`, `crop`, `verify:final-size` | PDF checks after plotting. |

## 2. Diagnostic command `AUTOPDFMEDIA`

Prints (and logs) the plotter list, every full-bleed paper of the configured plotter with the size AutoCAD
reports and its localized name, the plan chosen for the typical sizes (210×297 … 1260×594 + your custom
list), the plot style status and the drawing units. Send this output together with the log when
reporting a problem.

## 3. Comparing with a manual -PLOT

If the log shows the `-PLOT` fallback failing, press **F2** (text window) and compare the prompts AutoCAD
printed with the logged answer list (`-PLOT javoblari: …`). The answer list follows the model-space
sequence: detailed = Yes → layout (Enter) → device → paper (locale name) → mm → Portrait/Landscape →
upside down = No → Window → 2 corners → scale `1=N` → Center → plot styles = Yes → table (`.` = none) →
lineweights → shade = As displayed → [write to file = Yes, only for devices that ask] → file name →
save page setup = No → proceed = Yes.

Setting *Plot usuli → Faqat -PLOT buyrug‘i* forces the fallback for testing; *Faqat API* disables it.

## 4. Debugging the code in Visual Studio 2022

1. Open `AutoPDFPro.sln`, configuration **Debug | Any CPU** (projects are x64).
2. Install the Debug bundle so AutoCAD loads your build: build, then run (as admin)
   `installer\install.ps1 -Source artifacts\AutoPDFPro.bundle`.
   For quicker iterations create a junction once instead of copying:
   ```powershell
   New-Item -ItemType Junction -Path "$env:APPDATA\Autodesk\ApplicationPlugins\AutoPDFPro.bundle" `
            -Target "C:\src\AutoPDFPro-New\artifacts\AutoPDFPro.bundle"
   ```
   (close AutoCAD before rebuilding — it locks the DLLs).
3. Set **AutoPDF.AutoCAD** as start-up project and pick the launch profile *AutoCAD 2024*
   (`Properties\launchSettings.json` starts `acad.exe /nologo`). Adjust the path if AutoCAD is installed
   elsewhere.
   Alternative without the bundle: profile *AutoCAD 2024 (NETLOAD, no bundle)* runs
   `tools\netload-debug.scr` (edit the path in it) — uninstall the bundle first to avoid a duplicate
   command.
4. F5 → AutoCAD starts → open a drawing → `AUTOPDF`. Breakpoints in all three projects are hit.
   * *Debug → Windows → Exception Settings*: tick **Common Language Runtime Exceptions** to stop at the
     first AutoCAD `Autodesk.AutoCAD.Runtime.Exception`.
   * Disable *Just My Code* if you want to step into PdfSharp.
5. Attach instead of launch: *Debug → Attach to Process* → `acad.exe`, code type **Managed (.NET 4.x)**.

The UI can be developed without AutoCAD: set **AutoPDF.UiDemo** as start-up project
(argument = path to any PDF opens the preview and summary windows).

## 5. Unit tests

```powershell
dotnet test tests\AutoPDF.Core.Tests
```

They cover paper-size/scale detection (all requirement examples), rectangle recognition, nested/container
filtering, sorting, media selection, crop/orientation geometry, the `-PLOT` answer builder, naming,
settings persistence and PDF file checks. They run on Windows, Linux or CI.

## 6. Loading problems

| Symptom | Fix |
|---|---|
| `AUTOPDF` unknown | Check that `…\ApplicationPlugins\AutoPDFPro.bundle\PackageContents.xml` exists; `APPLOAD` → *Startup Suite* is not needed. Restart AutoCAD. Bundles are only read at start-up. |
| Security dialog "Загрузить исполняемый файл?" | Choose *Always load*, or install to `%ProgramFiles%\Autodesk\ApplicationPlugins` (trusted), or add the folder to *Options → Files → Trusted Locations*. |
| `FileLoadException` / "operation is not supported" | The DLLs were blocked after download: run `install.ps1` (it unblocks) or *Properties → Unblock* on each DLL. |
| Command defined twice / old UI appears | The old AutoPDF Pro bundle is still installed; `install.ps1` moves it to `%LOCALAPPDATA%\AutoPDFPro\Backup`. |
| Assembly binding issues | Enable Fusion logging (`fuslogvw.exe`, "Log bind failures") and restart AutoCAD. |

## 7. PDF viewer settings

If *DWG To PDF → PDF Options → "Open in PDF viewer when done"* is enabled, a viewer opens after every
sheet and may lock the file while the next sheet is written. Turn it off for batch export.
