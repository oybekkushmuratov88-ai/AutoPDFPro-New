using System;
using System.Collections.Generic;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Localization;

namespace AutoPDF.Cad.Plotting
{
    /// <summary>
    /// Prepares the CAD session for plotting without changing the drawing: model space active,
    /// foreground plotting (BACKGROUNDPLOT = 0), everything restored afterwards. Also checks the plotting
    /// environment (plotter present, plot style table, STB/CTB mode, units).
    /// </summary>
    public sealed class PageSetupService
    {
        private readonly Document _doc;
        private readonly LoggingService _log;

        public PageSetupService(Document doc, LoggingService log)
        {
            _doc = doc;
            _log = log;
        }

        /// <summary>Switches to model space (TILEMODE = 1); returns the layout to go back to, or null.</summary>
        public string EnsureModelSpace()
        {
            if (SysVarScope.GetInt("TILEMODE", 1) == 1) return null;

            string previous = null;
            try { previous = LayoutManager.Current.CurrentLayout; } catch { /* ignore */ }

            CadCoreApp.SetSystemVariable("TILEMODE", (short)1);
            _log.Info(L.SwitchedToModel);
            return previous;
        }

        public void RestoreLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName)) return;
            try
            {
                LayoutManager.Current.CurrentLayout = layoutName;
            }
            catch (Exception ex)
            {
                _log.Debug(L.LayoutRestoreFailed(CadErrors.Describe(ex)));
            }
        }

        /// <summary>Foreground plotting for the batch; restored when disposed.</summary>
        public IDisposable BeginPlotSession()
        {
            var scope = new SysVarScope(_log);
            scope.Set("BACKGROUNDPLOT", (short)0);
            return scope;
        }

        public Matrix3d GetWcsToDcs()
        {
            using (ViewTableRecord view = _doc.Editor.GetCurrentView())
            {
                return PlotSettingsService.WcsToDcs(view);
            }
        }

        public ObjectId GetModelLayoutId()
        {
            using (Transaction tr = _doc.Database.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_doc.Database), OpenMode.ForRead);
                ObjectId id = ms.LayoutId;
                tr.Commit();
                return id;
            }
        }

        /// <summary>Problems that would make every sheet fail, plus informative notes.</summary>
        public List<string> CheckEnvironment(PluginSettings settings, CustomPaperSizeManager media, PlotSettingsService plotSettings)
        {
            var notes = new List<string>();

            if (!media.DeviceExists(settings.Plotter))
            {
                notes.Add(L.EnvPlotterMissing(settings.Plotter));
            }
            else
            {
                try
                {
                    int count = media.GetMedia(media.CanonicalDeviceName(settings.Plotter)).Count;
                    notes.Add(L.EnvPlotterOk(settings.Plotter, count));
                }
                catch (Exception ex)
                {
                    notes.Add(L.EnvPlotterMediaFailed(CadErrors.Describe(ex)));
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.CustomSizesPlotter) && media.DeviceExists(settings.CustomSizesPlotter))
            {
                notes.Add(L.EnvCustomPlotter(settings.CustomSizesPlotter));
            }

            string style = plotSettings.ResolveStyleSheet(_doc.Database, settings, out string warning);
            if (warning != null) notes.Add("⚠ " + warning);
            else if (style != null) notes.Add(L.EnvStyle(style));

            int insunits = (int)_doc.Database.Insunits;
            notes.Add(L.EnvUnits(DrawingUnits.Describe(insunits)));
            return notes;
        }
    }
}
