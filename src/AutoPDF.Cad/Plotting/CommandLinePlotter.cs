using System;
using System.Collections.Generic;
using System.IO;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Localization;
using AutoPDF.Pdf;

#if !ZWCAD // ZWCAD's -PLOT prompts differ; it plots with the API only (PlotService).
namespace AutoPDF.Cad.Plotting
{
    /// <summary>
    /// AutoCAD only. Fallback: plots with the "-PLOT" command. Used only if the plot API fails (or when chosen
    /// in the settings for diagnostics) — the API (PlotService) is always tried first. Made robust for a
    /// localized AutoCAD:
    ///  • paper = the *locale* media name from AutoCAD itself (the old code guessed it from the canonical
    ///    name, which does not match in the Russian AutoCAD — the reason "-PLOT did not create a PDF");
    ///  • every keyword in its global "_" form; the layout name answered with Enter;
    ///  • running object snaps disabled so the window corners are not snapped elsewhere;
    ///  • FILEDIA = 0; the extra "Write the plot to a file?" prompt answered only for devices that ask it;
    ///  • the exact answer list is written to the log, the result file is checked afterwards.
    /// </summary>
    public sealed class CommandLinePlotter : ISheetPlotter
    {
        private readonly Document _doc;
        private readonly LoggingService _log;
        private readonly Dictionary<string, bool> _asksWriteToFile = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public CommandLinePlotter(Document doc, LoggingService log)
        {
            _doc = doc;
            _log = log;
        }

        public string Method => "-PLOT";

        public PlotRunInfo Plot(PlotRequest req, string outputPath, string progressTitle)
        {
            int sheet = req.SheetNumber;
            Editor ed = _doc.Editor;
            Matrix3d wcsToUcs = ed.CurrentUserCoordinateSystem.Inverse();
            Point3d ll = new Point3d(req.WindowWcs.MinX, req.WindowWcs.MinY, 0).TransformBy(wcsToUcs);
            Point3d ur = new Point3d(req.WindowWcs.MaxX, req.WindowWcs.MaxY, 0).TransformBy(wcsToUcs);

            var input = new PlotCommandInput
            {
                DeviceName = req.Plan.DeviceName,
                LocaleMediaName = req.Plan.Media.LocaleName,
                Landscape = req.Plan.Rotate90,
                LowerLeft = new PlotPoint(ll.X, ll.Y),
                UpperRight = new PlotPoint(ur.X, ur.Y),
                DrawingUnitsPerMm = req.DrawingUnitsPerMm,
                Center = req.Center,
                StyleTable = req.StyleSheet,
                Lineweights = req.Lineweights,
                DeviceAsksWriteToFile = AsksWriteToFile(req.Plan.DeviceName),
                OutputPath = outputPath
            };

            IReadOnlyList<object> tokens = PlotCommandBuilder.Build(input);
            _log.SheetInfo(sheet, "cmd-plot", L.CmdAnswers(PlotCommandBuilder.Describe(tokens)));

            var args = new object[tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                args[i] = tokens[i] is PlotPoint p ? (object)new Point3d(p.X, p.Y, 0) : tokens[i];
            }

            using (var vars = new SysVarScope(_log))
            {
                vars.Set("FILEDIA", (short)0);
                vars.Set("CMDECHO", (short)1);
                int osmode = SysVarScope.GetInt("OSMODE");
                vars.Set("OSMODE", (short)(osmode | 16384));

                try
                {
                    ed.Command(args);
                }
                catch (Exception ex)
                {
                    throw new PlotStepException("cmd-plot:command", L.CmdFailed(CadErrors.Describe(ex)), ex);
                }
            }

            if (!File.Exists(outputPath))
            {
                throw new PlotStepException("cmd-plot:no-file", L.CmdNoFile);
            }
            return new PlotRunInfo();
        }

        /// <summary>Devices that may also print on paper ask "Write the plot to a file?"; PDF devices do not.</summary>
        private bool AsksWriteToFile(string device)
        {
            if (_asksWriteToFile.TryGetValue(device, out bool cached)) return cached;

            bool asks = false;
            string previous = null;
            try
            {
                try { previous = PlotConfigManager.CurrentConfig?.DeviceName; } catch { /* none */ }
                PlotConfig config = PlotConfigManager.SetCurrentConfig(device);
                asks = config.PlotToFileCapability == PlotToFileCapability.PlotToFileAllowed;
            }
            catch (Exception ex)
            {
                _log.Debug(L.CmdCapabilityUnknown(CadErrors.Describe(ex)));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous) && !string.Equals(previous, device, StringComparison.OrdinalIgnoreCase))
                {
                    try { PlotConfigManager.SetCurrentConfig(previous); } catch { /* ignore */ }
                }
            }

            _asksWriteToFile[device] = asks;
            return asks;
        }
    }
}
#endif
