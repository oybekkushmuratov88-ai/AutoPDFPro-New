# Analysis of the previous AutoPDF Pro (v1.0.0)

The five supplied assemblies (`AutoPDFPro.AutoCAD.dll`, `.Application.dll`, `.Core.dll`,
`.Infrastructure.dll`, `.UI.dll`) were decompiled with ILSpy and reviewed. They target AutoCAD 2024
(references `acdbmgd`/`acmgd`/`accoremgd` 24.3), PdfSharp 1.50.5147, Docnet.Core 2.6 (PDF preview) and
Newtonsoft.Json 13.

The new plug-in does **not** reuse this code. This document records what the old code did, why it failed,
and what the new implementation does instead.

## 1. Root cause of `SetPlotType(copyFrom) -> InvalidInput`

`PlotService.CreateConfiguredPlot` built the plot settings in this order:

```text
new PlotSettings(model) ; CopyFrom(layout)              (attempt 1)
SetPlotConfigurationName(ps, "DWG To PDF.pc3", null)
RefreshLists(ps)
SetPlotConfigurationName(ps, device, media)
SetPlotType(ps, PlotType.Window)          <-- throws eInvalidInput
...
SetPlotWindowArea(ps, window)             (only later, in PlotOnceViaApi)
```

AutoCAD refuses `PlotType.Window` while the settings have no (or an empty) window area, and answers
`eInvalidInput`. The code caught this, logged *"SetPlotType(copyFrom) xato (InvalidInput)"* and retried
with a *clean* `PlotSettings` — but in exactly the same order, so the retry failed identically. The API
path could therefore never succeed on a drawing whose model layout had no stored plot window.

**New implementation** (`PlotSettingsService.Create`): the window area is always set *before*
`SetPlotType(Window)`. The full order is device → refresh lists → media → millimetres → **window area →
plot type** → custom scale → rotation → centre → plot style table → flags. Each call is a named step, so
a failure is reported as e.g. `[List 03] [plot:set-media] eInvalidInput`.

## 2. Root cause of `[cmd-plot] -PLOT buyrug'i PDF yaratmadi`

After the API failed, the old code ran `PageSetupFixer` and then the `-PLOT` command via `Editor.Command`:

| Problem | Effect |
|---|---|
| Paper size answered with the *canonical* media name with `_` replaced by spaces (`ISO_full_bleed_A4_(210.00_x_297.00_MM)` → `ISO full bleed A4 (210.00 x 297.00 MM)`). | `-PLOT` expects the **locale** name. In a Russian AutoCAD the names differ, the prompt repeats, and every following answer lands on the wrong prompt. No PDF. |
| Orientation chosen as `mediaW >= mediaH ? Landscape : Portrait`, while the API path used rotation 0 for the same media. | Inconsistent with the API path; wrong orientation even when the sequence worked. |
| Window corners passed as `Point3d` while running object snaps were active. | Points can snap to nearby geometry → wrong window. |
| No detection of a device that asks "Write the plot to a file?". | One extra prompt shifts the whole sequence. |
| Temporary UCS switch but WCS points; no restore of `FILEDIA`. | Fragile. |

A manual `-PLOT` works because the user types the paper name exactly as AutoCAD lists it.

**New implementation**: `-PLOT` is only a fallback (the plot API normally works now). When it is used
(`CommandLinePlotter` + `PlotCommandBuilder`): the paper is the locale name returned by
`PlotSettingsValidator.GetLocaleMediaName`; all keywords are global (`_Yes`, `_Millimeters`, `_Window`, …);
the layout is answered with Enter (current layout = Model, whose display name is localized); OSNAP is
disabled (`OSMODE | 16384`) and `FILEDIA = 0` for the call; the "write to file" prompt is answered only for
devices with `PlotToFileCapability.PlotToFileAllowed`; the complete answer list is written to the log.

## 3. Other defects found

| Area | Old behaviour | New behaviour |
|---|---|---|
| Page setups | `PageSetupFixer.FixAllLayouts()` permanently rewrote **every layout** of the user's drawing (device, A0, Extents/Fit, no plot style). | Nothing in the drawing is modified. Plot settings are an in-memory override passed to the plot engine. |
| Monochrome | The API path never called `SetCurrentStyleSheet`; with Monochrome off it forced `PlotPlotStyles = false`. | `monochrome.ctb` is applied when enabled; STB drawings get `monochrome.stb` (a `.ctb` on an STB drawing is another `eInvalidInput`). |
| Paper size | `Math.Ceiling` on frame size / scale: a 210.01 mm frame became 211 mm paper. | Snap to known formats within 0.5 mm, otherwise round to 0.1 mm. |
| Formats | Only ISO short edges considered; default scale 1:100. | ISO A0–A5 **and** ГОСТ 2.301 extended formats (A4x3 = 297×630, A3x3 = 420×891, A2x3 = 594×1261 …) plus a user list (default `630x297; 891x420; 1260x594`). Scale detected from the frame (1:1, 1:100 …) or fixed in settings. INSUNITS respected. |
| Rectangle test | Closed + 4 vertices + no bulge. Parallelograms accepted. | Additionally: every edge parallel to X/Y; a 5-vertex polyline whose last point repeats the first is accepted. |
| Nested frames | Dropped every frame inside another. A boundary rectangle around all sheets would have swallowed them. | Containment tree: inner borders are dropped, but a non-format rectangle containing several sheets is dropped instead of the sheets. |
| Media choice | Parsed sizes from media *names*; accepted media with unprintable margins (edges clipped). | Sizes/margins read from AutoCAD (`PlotPaperSize`, `PlotPaperMargins`); only full-bleed paper is used. |
| Plot window | WCS coordinates passed as the window (a WCS→DCS helper existed but was unused). | Window transformed to DCS of the model view, as the plot API expects. |
| Sorting | "Left to right" = pure X order (multi-row sets interleaved). | Row/column grouping by overlap. |
| Error isolation | One exception type per plot, step names partially tracked. | Every sheet is independent; the result names the exact step; the batch continues unless "stop on error" is set. |

## 4. What was kept conceptually

The UI flow (detection method, sort direction, start number, table `# | Blok | Eni | Bo‘yi | Qog‘oz | Holat`,
Scan / Check / Create PDF / Settings, preview with *Cancel / Confirm / Confirm and do the rest*) and the
Uzbek wording of the old UI were used as the functional reference.

## 5. Material not yet received

The DWG sample, example PDFs, command-line logs, installer and UI screenshots were announced but only the
DLLs and a screenshot of the old project structure were attached. The implementation is therefore based on
the requirements and the decompiled code. The points that would benefit most from the real files:

* the DWG — to confirm frame layers/blocks and scales (use `AUTOPDF` → *Tekshirish*);
* the command-line log of the manual `-PLOT` — to compare with the answer list the fallback logs;
* `AUTOPDFMEDIA` output from the target PC — shows which papers `DWG To PDF.pc3` exposes there.
