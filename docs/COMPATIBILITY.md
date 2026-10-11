# CAD compatibility

## Matrix

| Application | Release | .NET runtime of the host | Assembly (project) | Compiled against | Loaded by |
|---|---|---|---|---|---|
| AutoCAD 2021 | R24.0 | .NET Framework 4.8 | `AutoPDF.AutoCAD.dll` (`src/AutoPDF.AutoCAD`) | `AutoCAD.NET` 24.0.0 (= 2021 SDK) | autoloader bundle, component *R24* |
| AutoCAD 2022 | R24.1 | .NET Framework 4.8 | same | same | same |
| AutoCAD 2023 | R24.2 | .NET Framework 4.8 | same | same | same |
| AutoCAD 2024 | R24.3 | .NET Framework 4.8 | same | same | same |
| AutoCAD 2025 | R25.0 | .NET 8 | `AutoPDF.AutoCAD.R25.dll` (`src/AutoPDF.AutoCAD.R25`) | `AutoCAD.NET` 25.0.1 | autoloader bundle, component *R25* |
| AutoCAD 2026 | R25.1 | .NET 8 | same | same | same |
| ZWCAD 2024 | API 24.0 | .NET Framework | `AutoPDF.ZWCAD.2024.dll` (`src/AutoPDF.ZWCAD.2024`) | `ZWCAD.NetApi` 20.24.0 (ZwManaged/ZwDatabaseMgd 24.0) | registry start-up entry |
| ZWCAD 2025 | API 25.0 | .NET Framework | `AutoPDF.ZWCAD.2025.dll` | `ZWCAD.NetApi` 20.25.0 (25.0) | same |
| ZWCAD 2026 | API 26.0 | .NET Framework | `AutoPDF.ZWCAD.2026.dll` | `ZWCAD.NetApi` 20.26.0 (26.0) | same |

AutoCAD verticals (Architecture, Mechanical, Electrical, Civil 3D …) of the same releases load the same
assemblies (`Platform="AutoCAD*"` in the bundle).

Why these builds:

* **R24.0–R24.3** are binary compatible; compiling against the *oldest* SDK (2021) guarantees that only API
  present in all four is used.
* **AutoCAD 2025** moved to **.NET 8**; .NET Framework assemblies cannot be loaded there, so the same source is
  compiled for `net8.0-windows` against the 2025 SDK (2026 is binary compatible with 2025).
* **ZWCAD** has its own .NET API (`ZwSoft.ZwCAD.*`, `ZwManaged.dll`, `ZwDatabaseMgd.dll`). AutoCAD's DLLs are
  never loaded into ZWCAD; the shared source is compiled against each ZWCAD release separately (its API
  assemblies change version every year).

## Source layout

```
src/AutoPDF.Core            CAD-independent logic: paper sizes, media choice, sorting, settings, logging,
                            localisation, host model (HostInfo), licensing client          net48 + net8.0
src/AutoPDF.Pdf             shared PDF pipeline (SheetPdfPipeline) + PdfSharp page tools,
                            behind ISheetPlotter / IMediaPlanner                           net48 + net8.0
src/AutoPDF.UI              WPF windows (no CAD references)                                net48 + net8.0-windows
src/AutoPDF.Cad             CAD-API source shared by every host build (not a project):
                              CadApi.cs              the only file naming vendor namespaces (#if ZWCAD)
                              CadHost.targets        common build logic, staging of the outputs
                              ZwcadHost.targets      common part of the three ZWCAD projects
src/AutoPDF.AutoCAD         host project: AutoCAD 2021–2024
src/AutoPDF.AutoCAD.R25     host project: AutoCAD 2025–2026
src/AutoPDF.ZWCAD.2024/.2025/.2026   host projects: ZWCAD
server/licensing            Google Apps Script licence server (Code.gs) + Node tests
```

What is CAD-specific (in `src/AutoPDF.Cad`) and what is shared:

| CAD-specific (per host build) | Shared (`AutoPDF.Core` / `AutoPDF.Pdf` / `AutoPDF.UI`) |
|---|---|
| frame detection on the database, selection prompts | paper size/scale resolution, filtering, sorting, numbering |
| plot API: `PlotSettingsValidator`, `PlotEngine`, progress dialog (`PlotService`) | media/rotation choice (`MediaSelector`), plan description |
| plotter paper catalogue (`CustomPaperSizeManager`) | the per-sheet pipeline: verify file → verify page size/orientation → one re-plot with the other rotation → crop → final check |
| `-PLOT` fallback (AutoCAD only, `CommandLinePlotter`) | PDF reading/cropping (PdfSharp) |
| transient highlighting, system variables, command line | all windows, texts (uz/ru/en), settings, logging |
| host detection, licence gate, command registration | licensing client (state machine, signatures, HTTP) |

## Run-time host detection

At start-up the plug-in logs, for example:

```
AutoPDF Pro 3.0.0 loaded: C:\…\ZWCAD\2025 | ZWCAD 2025 (25.0, ZWCAD, .NET Framework 4.8.9290.0), … | build: AutoPDF.ZWCAD.2025 for ZWCAD 2025
```

* AutoCAD: release from `ACADVER` (`24.3s (LMS Tech)` → R24.3 → 2024), fallback the version of
  `acmgd`/`accoremgd` as loaded by AutoCAD.
* ZWCAD: the version of `ZwManaged.dll` as loaded by ZWCAD (24.x → 2024, 25.x → 2025, 26.x → 2026).
* The detected host is compared with the build: a ZWCAD 2025 build loaded into ZWCAD 2024 (or an AutoCAD
  build into ZWCAD) prints a clear message and `AUTOPDF` refuses to run instead of failing later.
* The host name is shown in the window subtitle and sent with licence requests.

## Plotting: API first

All hosts plot with the plot API (`PlotFactory.CreatePublishEngine`, `PlotInfoValidator`,
`PlotSettingsValidator`) — no prompt sequences, independent of the CAD language. The `-PLOT` command-line
sequence (`PlotCommandBuilder`) is AutoCAD-specific and is used only as a fallback on AutoCAD when the API
fails (setting *Plot method = Auto*), or on request (*-PLOT only*, diagnostics). ZWCAD always uses the API.

## AutoCAD loading

`AutoPDFPro.bundle/PackageContents.xml` has two `<Components>` with `RuntimeRequirements`:
`SeriesMin="R24.0" SeriesMax="R24.3"` → `Contents/Windows/R24/AutoPDF.AutoCAD.dll`, and
`SeriesMin="R25.0" SeriesMax="R25.1"` → `Contents/Windows/R25/AutoPDF.AutoCAD.R25.dll`. Setup installs the
bundle to `%ProgramFiles%\Autodesk\ApplicationPlugins` (a trusted location for `SECURELOAD`). A future AutoCAD
release (R25.2 / R26) needs a tested build and a new component entry.

## ZWCAD loading

ZWCAD does not read Autodesk bundles. Setup copies each selected ZWCAD build to
`C:\Program Files\AutoPDF Pro\ZWCAD\<year>\` and registers it in every language profile of that ZWCAD:

```
HKLM\SOFTWARE\ZWSOFT\ZWCAD\<year>\<language>\Applications\AutoPDFPro   (also HKCU)
    LOADER     = C:\Program Files\AutoPDF Pro\ZWCAD\<year>\AutoPDF.ZWCAD.<year>.dll
    LOADCTRLS  = 2        (load at start-up)
    MANAGED    = 1        (.NET assembly)
    DESCRIPTION
```

This follows ZWCAD's documented demand-load registry layout (the same scheme as AutoCAD's `Applications`
key). It could not be verified on a real ZWCAD installation while building 3.0.0 — if `AUTOPDF` is unknown
after starting ZWCAD:

1. `APPLOAD` → *Startup Suite* → *Contents…* → *Add…* → `C:\Program Files\AutoPDF Pro\ZWCAD\<year>\AutoPDF.ZWCAD.<year>.dll`
   → restart ZWCAD (loads it at every start), **or**
2. type `NETLOAD` and choose the same DLL (this session only).

Use the DLL of **your** ZWCAD year: a different year's build prints "this module is built for ZWCAD …".

## Building against the official SDKs

* AutoCAD: the `AutoCAD.NET` NuGet packages are published by Autodesk (reference assemblies only).
* ZWCAD: by default the ZWSOFT-signed reference assemblies from the `ZWCAD.NetApi` NuGet package are used
  (compile-time only; `ExcludeAssets="runtime"`, nothing is copied to the output). To use the official ZWCAD
  SDK or an installed ZWCAD instead:

  ```
  dotnet build src/AutoPDF.ZWCAD.2025 -c Release -p:ZwcadSdkDir="C:\Program Files\ZWSOFT\ZWCAD 2025"
  ```

  (the folder that contains `ZwManaged.dll` and `ZwDatabaseMgd.dll`).

## Adding a future version

* ZWCAD 2027: copy `src/AutoPDF.ZWCAD.2026` to `.2027`, set `ZwcadYear`/`ZwcadApiVersion`, add
  `ZWCAD_2027` to `HostEnvironment.Build`, add the section to `installer/AutoPDFPro.nsi`, run the checklist.
* AutoCAD R25.2: widen `SeriesMax` of the R25 component and `HostCatalog.AutoCadYear` after testing; a new
  binary-incompatible release (R26) gets its own host project like R25.
