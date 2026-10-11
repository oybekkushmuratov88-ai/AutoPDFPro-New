using System;
using System.Globalization;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Plotting;
using AutoPDF.Pdf;

namespace AutoPDF.Cad.Plotting
{
    /// <summary>
    /// Plots one sheet to a PDF file with the CAD plot API (PlotEngine) — no command-line prompts, so it
    /// does not depend on the CAD language or on the host's -PLOT prompt sequence. The same code runs on
    /// AutoCAD and ZWCAD (each compiled against its own SDK). Every stage is named, so a failure reports
    /// e.g. "[plot:set-window-area] eInvalidInput".
    /// </summary>
    public sealed class PlotService : ISheetPlotter
    {
        private readonly Document _doc;
        private readonly PlotSettingsService _plotSettings;
        private readonly LoggingService _log;
        private readonly ObjectId _modelLayoutId;
        private readonly Matrix3d _wcsToDcs;

        public PlotService(Document doc, PlotSettingsService plotSettings, LoggingService log, ObjectId modelLayoutId, Matrix3d wcsToDcs)
        {
            _doc = doc;
            _plotSettings = plotSettings;
            _log = log;
            _modelLayoutId = modelLayoutId;
            _wcsToDcs = wcsToDcs;
        }

        public string Method => "API";

        public PlotRunInfo Plot(PlotRequest req, string outputPath, string progressTitle)
        {
            string step = "plot:check-state";
            int sheet = req.SheetNumber;

            void Step(string s)
            {
                step = s;
                _log.SheetDebug(sheet, s, "…");
            }

            try
            {
                if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                {
                    throw new PlotStepException(step, L.PlotBusy);
                }

                using (PlotSettings ps = _plotSettings.Create(req, PlotSettingsService.ToDcs(req.WindowWcs, _wcsToDcs), Step))
                using (var info = new PlotInfo())
                using (var validator = new PlotInfoValidator())
                {
                    Step("plot:validate");
                    info.Layout = _modelLayoutId;
                    info.OverrideSettings = ps;
                    validator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                    validator.Validate(info);

                    var result = new PlotRunInfo();
                    PlotSettings validated = info.ValidatedSettings;
                    if (validated != null)
                    {
                        result.ValidatedMedia = validated.CanonicalMediaName;
                        _log.SheetDebug(sheet, step, string.Format(CultureInfo.InvariantCulture, L.ValidatedFormat,
                            validated.PlotConfigurationName, validated.CanonicalMediaName,
                            validated.PlotPaperSize.X, validated.PlotPaperSize.Y, validated.PlotRotation,
                            validated.CustomPrintScale.Numerator, validated.CustomPrintScale.Denominator,
                            PlotSettingsService.Describe(validated.PlotWindowArea), validated.CurrentStyleSheet));
                    }

                    RunEngine(info, outputPath, progressTitle, result, Step);
                    return result;
                }
            }
            catch (PlotStepException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PlotStepException(step, CadErrors.Describe(ex), ex);
            }
        }

        private void RunEngine(PlotInfo info, string outputPath, string title, PlotRunInfo result, Action<string> step)
        {
            bool inPlot = false, inDocument = false, inPage = false;
            step("plot:engine-create");
            using (PlotEngine engine = PlotFactory.CreatePublishEngine())
            using (var progress = new PlotProgressDialog(false, 1, true))
            {
                try
                {
                    progress.set_PlotMsgString(PlotMessageIndex.DialogTitle, title);
                    progress.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, L.ProgressCancelJob);
                    progress.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, L.ProgressCancelSheet);
                    progress.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, L.ProgressJob);
                    progress.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, L.Sheet);
                    progress.LowerPlotProgressRange = 0;
                    progress.UpperPlotProgressRange = 100;
                    progress.PlotProgressPos = 0;
                    progress.OnBeginPlot();
                    progress.IsVisible = true;

                    step("plot:engine-begin-plot");
                    engine.BeginPlot(progress, null);
                    inPlot = true;

                    step("plot:engine-begin-document");
                    engine.BeginDocument(info, _doc.Name, null, 1, true, outputPath);
                    inDocument = true;

                    progress.OnBeginSheet();
                    progress.LowerSheetProgressRange = 0;
                    progress.UpperSheetProgressRange = 100;
                    progress.SheetProgressPos = 0;

                    using (var pageInfo = new PlotPageInfo())
                    {
                        step("plot:engine-begin-page");
                        engine.BeginPage(pageInfo, info, true, null);
                        inPage = true;

                        step("plot:engine-generate-graphics");
                        engine.BeginGenerateGraphics(null);
                        progress.SheetProgressPos = 50;
                        engine.EndGenerateGraphics(null);
                        try { result.EntityCount = pageInfo.EntityCount; } catch { /* informative only */ }

                        step("plot:engine-end-page");
                        engine.EndPage(null);
                        inPage = false;
                    }

                    progress.SheetProgressPos = 100;
                    progress.OnEndSheet();

                    step("plot:engine-end-document");
                    engine.EndDocument(null);
                    inDocument = false;

                    progress.PlotProgressPos = 100;
                    progress.OnEndPlot();

                    step("plot:engine-end-plot");
                    engine.EndPlot(null);
                    inPlot = false;

                    result.UserCancelled = progress.IsPlotCancelled;
                }
                catch (Exception ex) when (IsCancelled(progress))
                {
                    throw new PlotStepException(PlotSteps.Cancelled, L.PlotCancelledByUser, ex);
                }
                finally
                {
                    // A half-finished engine blocks every later plot of the session: always close it.
                    if (inPage) TryQuiet(() => engine.EndPage(null));
                    if (inDocument) TryQuiet(() => engine.EndDocument(null));
                    if (inPlot) TryQuiet(() => engine.EndPlot(null));
                }
            }
        }

        private static bool IsCancelled(PlotProgressDialog progress)
        {
            try { return progress.IsPlotCancelled || progress.IsSheetCancelled; }
            catch { return false; }
        }

        private static void TryQuiet(Action a)
        {
            try { a(); } catch { /* cleanup */ }
        }
    }
}
