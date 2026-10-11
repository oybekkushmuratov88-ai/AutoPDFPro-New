using AutoPDF.Cad.Infrastructure;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Settings;
using AutoPDF.Pdf;

namespace AutoPDF.Cad.Plotting
{
    /// <summary>
    /// Wires the host's plot API into the shared <see cref="SheetPdfPipeline"/>: PlotService (API) first and,
    /// on AutoCAD only, the -PLOT command as a fallback. ZWCAD has no fallback because its -PLOT prompt
    /// sequence differs and the API covers everything AutoPDF needs.
    /// </summary>
    public static class PdfExportFactory
    {
        public static SheetPdfPipeline Create(Document doc, PluginSettings settings, LoggingService log,
            CustomPaperSizeManager media, PlotSettingsService plotSettings, PageSetupService pageSetup, out string styleWarning)
        {
            string styleSheet = plotSettings.ResolveStyleSheet(doc.Database, settings, out styleWarning);
            if (styleWarning != null) log.Warn(styleWarning);

            var api = new PlotService(doc, plotSettings, log, pageSetup.GetModelLayoutId(), pageSetup.GetWcsToDcs());
#if ZWCAD
            ISheetPlotter fallback = null;
#else
            ISheetPlotter fallback = new CommandLinePlotter(doc, log);
#endif
            return new SheetPdfPipeline(settings, log, media, api, fallback, styleSheet, CadErrors.Describe);
        }
    }
}
