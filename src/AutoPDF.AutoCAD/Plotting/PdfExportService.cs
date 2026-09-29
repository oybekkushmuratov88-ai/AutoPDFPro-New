using System;
using System.Globalization;
using System.IO;
using System.Threading;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.AutoCAD.Pdf;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Pdf;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Localization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoPDF.AutoCAD.Plotting
{
    /// <summary>
    /// Exports one sheet: choose paper → plot (API, fallback -PLOT) → verify the file → verify the page
    /// size/orientation (re-plot once with the other rotation if it came out turned) → crop non-standard
    /// sizes → verify again. Any failure is returned as a <see cref="SheetResult"/> naming the exact step;
    /// it never throws, so one bad sheet does not stop the batch.
    /// </summary>
    public sealed class PdfExportService
    {
        private readonly Document _doc;
        private readonly PluginSettings _settings;
        private readonly LoggingService _log;
        private readonly CustomPaperSizeManager _media;
        private readonly PlotSettingsService _plotSettings;
        private readonly PlotService _api;
        private readonly CommandLinePlotter _cmd;
        private readonly ObjectId _modelLayoutId;
        private readonly Matrix3d _wcsToDcs;
        private readonly string _styleSheet;

        public PdfExportService(Document doc, PluginSettings settings, LoggingService log, CustomPaperSizeManager media,
            PlotSettingsService plotSettings, PageSetupService pageSetup)
        {
            _doc = doc;
            _settings = settings;
            _log = log;
            _media = media;
            _plotSettings = plotSettings;
            _api = new PlotService(doc, plotSettings, log);
            _cmd = new CommandLinePlotter(doc, log);
            _modelLayoutId = pageSetup.GetModelLayoutId();
            _wcsToDcs = pageSetup.GetWcsToDcs();
            _styleSheet = plotSettings.ResolveStyleSheet(doc.Database, settings, out string warning);
            if (warning != null) _log.Warn(warning);
            StyleWarning = warning;
        }

        public string StyleWarning { get; }

        public SheetResult Export(SheetModel sheet, string outputPath, int index, int total)
        {
            var result = new SheetResult { Number = sheet.Number, OutputPath = outputPath };
            string step = "prepare";
            int n = sheet.Number;
            string label = string.Format(CultureInfo.InvariantCulture, "AutoPDF Pro — {0}/{1}: {2}", index + 1, total, Path.GetFileName(outputPath));

            try
            {
                _log.SheetInfo(n, "start", Path.GetFileName(outputPath) + " ← " + sheet.BlockName + " " + sheet.Bounds + ", " + sheet.Paper?.DisplayText);

                step = "prepare:paper";
                if (sheet.Paper == null || !sheet.Paper.IsValid)
                {
                    throw new PlotStepException(step, sheet.Paper?.Problem ?? L.PaperUnknown);
                }

                step = "prepare:output-file";
                PrepareOutputFile(outputPath);

                step = "media:select";
                MediaPlan plan = _media.Plan(sheet.Paper.Paper, _settings, out string problem);
                if (plan == null) throw new PlotStepException(step, problem);
                result.PlanDescription = plan.Describe();
                _log.SheetInfo(n, step, L.PlanChosen(plan.Describe(), plan.Fit == MediaFit.Exact));

                step = "plot";
                PlotAndVerify(sheet, plan, outputPath, label, result, allowOrientationRetry: true);

                result.Outcome = result.Warnings.Count > 0 ? SheetOutcome.SuccessWithWarning : SheetOutcome.Success;
                _log.SheetInfo(n, "done", "✓ " + Path.GetFileName(outputPath) + " (" + result.PageSize + ", " +
                                          (result.FileBytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB, " + result.Method + ")");
            }
            catch (PlotStepException ex) when (ex.Step == PlotService.CancelledStep)
            {
                result.Outcome = SheetOutcome.Cancelled;
                result.UserCancelled = true;
                result.FailedStep = ex.Step;
                result.Error = ex.Message;
                _log.SheetWarn(n, ex.Step, ex.Message);
            }
            catch (PlotStepException ex)
            {
                Fail(result, ex.Step, ex.Message, ex.InnerException ?? ex);
            }
            catch (Exception ex)
            {
                Fail(result, step, AcadErrors.Describe(ex), ex);
            }

            return result;
        }

        private void Fail(SheetResult result, string step, string message, Exception ex)
        {
            result.Outcome = SheetOutcome.Failed;
            result.FailedStep = step;
            result.Error = message;
            _log.SheetError(result.Number, step, message, ex is PlotStepException ? null : ex);
        }

        private void PrepareOutputFile(string outputPath)
        {
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(outputPath)) return;

            if (!PdfFileVerifier.IsWritable(outputPath, out string problem))
            {
                throw new PlotStepException("prepare:output-file", L.FileLocked(outputPath, problem));
            }
            File.Delete(outputPath);
        }

        private void PlotAndVerify(SheetModel sheet, MediaPlan plan, string path, string label, SheetResult result, bool allowOrientationRetry)
        {
            int n = sheet.Number;
            PlotRequest req = BuildRequest(sheet, plan);

            PlotOnce(req, path, label, result);

            string step = "verify:file";
            PdfFileCheck file = WaitForFile(path);
            if (!file.IsValid) throw new PlotStepException(step, file.Problem);

            step = "verify:page";
            PdfPageBox box;
            int pages;
            try
            {
                box = PdfPageService.ReadFirstPage(path, out pages);
            }
            catch (Exception ex)
            {
                // The file is structurally fine; we just cannot measure it. Keep it, but say so.
                result.Warnings.Add(plan.Fit == MediaFit.Crop ? L.PageUnreadableCrop(ex.Message, plan.ExpectedPage) : L.PageUnreadable(ex.Message));
                _log.SheetWarn(n, step, result.Warnings[result.Warnings.Count - 1]);
                result.FileBytes = new FileInfo(path).Length;
                return;
            }

            if (pages != 1) result.Warnings.Add(L.PageCount(pages));

            PaperSize displayed = box.DisplayedSizeMm;
            double tol = _settings.PageSizeToleranceMm;
            PageOrientationCheck check = PdfPageGeometry.Compare(displayed, plan.ExpectedPage, tol);
            _log.SheetDebug(n, step, string.Format(CultureInfo.InvariantCulture, L.PageDebugFormat,
                displayed, box.X2 - box.X1, box.Y2 - box.Y1, box.Rotate, plan.ExpectedPage, check));

            if (check == PageOrientationCheck.Swapped && allowOrientationRetry)
            {
                // This device places the drawing the other way round for this paper: learn it and re-plot once.
                _log.SheetWarn(n, step, L.OrientationSwapped(displayed, plan.ExpectedPage));
                bool other = !plan.Rotate90;
                _media.LearnRotation(plan, other);
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    throw new PlotStepException("verify:page-retry", L.DeleteSwappedFailed(ex.Message), ex);
                }
                PlotAndVerify(sheet, plan.WithRotation(other), path, label, result, allowOrientationRetry: false);
                return;
            }

            if (check != PageOrientationCheck.Match)
            {
                string msg = L.PageMismatch(displayed, plan.ExpectedPage);
                if (plan.Fit == MediaFit.Exact || !PdfPageGeometry.Fits(displayed, plan.TargetPage, tol))
                {
                    throw new PlotStepException(step, msg + L.WrongPaperOrOrientation);
                }
                result.Warnings.Add(msg);
                _log.SheetWarn(n, step, msg);
            }

            if (plan.Fit == MediaFit.Crop)
            {
                step = "crop";
                PdfPageBox crop = PdfPageGeometry.CenteredCropBox(box, plan.TargetPage);
                try
                {
                    PdfPageService.Crop(path, crop);
                }
                catch (Exception ex)
                {
                    throw new PlotStepException(step, L.CropFailed(plan.TargetPage, ex.Message), ex);
                }

                step = "verify:cropped-page";
                try
                {
                    box = PdfPageService.ReadFirstPage(path, out _);
                }
                catch (Exception ex)
                {
                    throw new PlotStepException(step, L.CroppedUnreadable(ex.Message), ex);
                }
                displayed = box.DisplayedSizeMm;
                _log.SheetDebug(n, step, L.CroppedPage(displayed));
            }

            step = "verify:final-size";
            if (!displayed.ApproximatelyEquals(sheet.Paper.Paper, tol))
            {
                result.Warnings.Add(L.FinalSizeWarning(displayed, sheet.Paper.Paper));
                _log.SheetWarn(n, step, result.Warnings[result.Warnings.Count - 1]);
            }

            result.PageSize = displayed;
            result.FileBytes = new FileInfo(path).Length;
        }

        private void PlotOnce(PlotRequest req, string path, string label, SheetResult result)
        {
            int n = req.SheetNumber;
            if (_settings.PlotMethod != PlotMethod.CommandLineOnly)
            {
                try
                {
                    PlotRunInfo run = _api.Plot(req, _modelLayoutId, path, label);
                    result.Method = "API";
                    if (run.UserCancelled) result.UserCancelled = true;
                    if (run.EntityCount == 0)
                    {
                        result.Warnings.Add(L.EmptyPage);
                        _log.SheetWarn(n, "plot:content", result.Warnings[result.Warnings.Count - 1]);
                    }
                    return;
                }
                catch (PlotStepException ex) when (_settings.PlotMethod == PlotMethod.Auto && ex.Step != PlotService.CancelledStep)
                {
                    _log.SheetWarn(n, ex.Step, L.ApiFailedFallback(ex.Message));
                    result.Warnings.Add(L.ApiFailedWarning(ex.Step, ex.Message));
                    TryDelete(path);
                }
            }

            _cmd.Plot(req, path);
            result.Method = "-PLOT";
        }

        private PlotRequest BuildRequest(SheetModel sheet, MediaPlan plan)
        {
            return new PlotRequest
            {
                SheetNumber = sheet.Number,
                Plan = plan,
                WindowWcs = sheet.Bounds,
                WindowDcs = PlotSettingsService.ToDcs(sheet.Bounds, _wcsToDcs),
                DrawingUnitsPerMm = sheet.Paper.DrawingUnitsPerMm,
                Center = _settings.CenterPlot,
                StyleSheet = _styleSheet,
                PlotStyles = _settings.PlotWithPlotStyles,
                Lineweights = _settings.PlotLineweights,
                ScaleLineweights = _settings.ScaleLineweights
            };
        }

        /// <summary>The PDF driver may still be flushing the file: poll briefly before judging it.</summary>
        private static PdfFileCheck WaitForFile(string path)
        {
            PdfFileCheck check = PdfFileVerifier.Verify(path);
            for (int i = 0; i < 20 && !check.IsValid; i++)
            {
                Thread.Sleep(150);
                check = PdfFileVerifier.Verify(path);
            }
            return check;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // PrepareOutputFile of the next attempt reports a locked file
            }
        }
    }
}
