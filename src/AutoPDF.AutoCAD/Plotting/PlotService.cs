using System;
using System.Globalization;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.Core.Logging;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;

namespace AutoPDF.AutoCAD.Plotting
{
    public sealed class PlotRunInfo
    {
        public int EntityCount { get; set; } = -1;
        public bool UserCancelled { get; set; }
        public string ValidatedMedia { get; set; }
    }

    /// <summary>
    /// Plots one sheet to a PDF file with the AutoCAD plot API (PlotEngine) — no command-line prompts, so it
    /// does not depend on the AutoCAD language. Every stage is named, so a failure reports e.g.
    /// "[plot:set-window-area] eInvalidInput".
    /// </summary>
    public sealed class PlotService
    {
        /// <summary>Step name used when the user pressed Cancel in the plot progress dialog.</summary>
        public const string CancelledStep = "plot:cancelled-by-user";

        private readonly Document _doc;
        private readonly PlotSettingsService _plotSettings;
        private readonly LoggingService _log;

        public PlotService(Document doc, PlotSettingsService plotSettings, LoggingService log)
        {
            _doc = doc;
            _plotSettings = plotSettings;
            _log = log;
        }

        public PlotRunInfo Plot(PlotRequest req, ObjectId modelLayoutId, string outputPath, string progressTitle)
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
                    throw new PlotStepException(step, "AutoCAD boshqa chop etish jarayonini bajaryapti (fon rejimidagi plot tugashini kuting).");
                }

                using (PlotSettings ps = _plotSettings.Create(req, Step))
                using (var info = new PlotInfo())
                using (var validator = new PlotInfoValidator())
                {
                    Step("plot:validate");
                    info.Layout = modelLayoutId;
                    info.OverrideSettings = ps;
                    validator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                    validator.Validate(info);

                    var result = new PlotRunInfo();
                    PlotSettings validated = info.ValidatedSettings;
                    if (validated != null)
                    {
                        result.ValidatedMedia = validated.CanonicalMediaName;
                        _log.SheetDebug(sheet, step, string.Format(CultureInfo.InvariantCulture,
                            "Tasdiqlangan: qurilma={0}, qog‘oz={1} ({2:0.##} x {3:0.##} mm), burish={4}, masshtab={5}:{6}, oyna(DCS)={7}, uslub={8}",
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
                throw new PlotStepException(step, AcadErrors.Describe(ex), ex);
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
                    progress.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Bekor qilish");
                    progress.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Listni bekor qilish");
                    progress.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Jarayon");
                    progress.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "List");
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
                    throw new PlotStepException(CancelledStep, "Chop etish oynasida bekor qilindi.", ex);
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
