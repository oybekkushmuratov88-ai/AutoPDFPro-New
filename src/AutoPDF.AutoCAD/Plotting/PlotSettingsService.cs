using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Localization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoPDF.AutoCAD.Plotting
{
    /// <summary>Everything needed to plot one sheet.</summary>
    public sealed class PlotRequest
    {
        public int SheetNumber { get; set; }
        public MediaPlan Plan { get; set; }

        /// <summary>Frame in WCS.</summary>
        public Bounds2d WindowWcs { get; set; }

        /// <summary>Frame in the display coordinate system of the model-space view — what the plot API expects.</summary>
        public Extents2d WindowDcs { get; set; }

        public double DrawingUnitsPerMm { get; set; } = 1;
        public bool Center { get; set; } = true;

        /// <summary>Resolved plot style table, or null for none.</summary>
        public string StyleSheet { get; set; }

        public bool PlotStyles { get; set; } = true;
        public bool Lineweights { get; set; } = true;
        public bool ScaleLineweights { get; set; }
    }

    /// <summary>
    /// Builds the plot settings for one sheet (DWG To PDF.pc3, paper, millimetres, window, rotation, centred,
    /// exact scale, monochrome.ctb, plot styles, lineweights). The settings are an in-memory override passed
    /// to the plot engine; the drawing's own layouts and page setups are never modified.
    ///
    /// The call order matters. The previous AutoPDF Pro called SetPlotType(Window) before a window area
    /// existed, which AutoCAD rejects with eInvalidInput ("SetPlotType(copyFrom) -> InvalidInput"); here the
    /// window area is always set first.
    /// </summary>
    public sealed class PlotSettingsService
    {
        private readonly LoggingService _log;

        public PlotSettingsService(LoggingService log)
        {
            _log = log;
        }

        public PlotSettings Create(PlotRequest req, Action<string> step)
        {
            if (req?.Plan?.Media == null) throw new ArgumentException("Plot request has no media plan.", nameof(req));
            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            var ps = new PlotSettings(true);
            try
            {
                step("plot:set-device");
                psv.SetPlotConfigurationName(ps, req.Plan.DeviceName, null);
                step("plot:refresh-lists");
                psv.RefreshLists(ps);
                step("plot:set-media");
                psv.SetPlotConfigurationName(ps, req.Plan.DeviceName, req.Plan.Media.CanonicalName);
                step("plot:set-paper-units");
                psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);

                step("plot:set-window-area");
                psv.SetPlotWindowArea(ps, req.WindowDcs);
                step("plot:set-plot-type");
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);

                step("plot:set-scale");
                psv.SetUseStandardScale(ps, false);
                psv.SetCustomPrintScale(ps, new CustomScale(1.0, req.DrawingUnitsPerMm));

                step("plot:set-rotation");
                psv.SetPlotRotation(ps, req.Plan.Rotate90 ? PlotRotation.Degrees090 : PlotRotation.Degrees000);

                step("plot:set-center");
                if (req.Center)
                {
                    psv.SetPlotCentered(ps, true);
                }
                else
                {
                    psv.SetPlotCentered(ps, false);
                    psv.SetPlotOrigin(ps, new Point2d(0, 0));
                }

                if (!string.IsNullOrEmpty(req.StyleSheet))
                {
                    step("plot:set-style-sheet");
                    psv.SetCurrentStyleSheet(ps, req.StyleSheet);
                }

                step("plot:set-flags");
                ps.PlotPlotStyles = req.PlotStyles;
                ps.PrintLineweights = req.Lineweights;
                ps.ScaleLineweights = req.ScaleLineweights;
                ps.PlotHidden = false;

                return ps;
            }
            catch
            {
                ps.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The plot style table to use, or null. Colour-dependent drawings (PSTYLEMODE=1) take .ctb files,
        /// named-style drawings take .stb — assigning a .ctb to an STB drawing fails with eInvalidInput,
        /// so the extension is adapted ("monochrome.ctb" → "monochrome.stb").
        /// </summary>
        public string ResolveStyleSheet(Database db, PluginSettings settings, out string warning)
        {
            warning = null;
            if (!settings.UseMonochrome || string.IsNullOrWhiteSpace(settings.PlotStyleTable)) return null;

            bool colorDependent = db.PlotStyleMode;
            string wanted = settings.PlotStyleTable.Trim();
            string ext = colorDependent ? ".ctb" : ".stb";
            if (!wanted.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                string adapted = Path.ChangeExtension(wanted, ext);
                _log.Info(L.StyleAdapted(colorDependent, wanted, adapted));
                wanted = adapted;
            }

            StringCollection available;
            try
            {
                available = PlotSettingsValidator.Current.GetPlotStyleSheetList();
            }
            catch (Exception ex)
            {
                warning = L.StyleListFailed(AcadErrors.Describe(ex));
                return null;
            }

            string match = available.Cast<string>().FirstOrDefault(s => string.Equals(s, wanted, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                warning = L.StyleNotFound(wanted);
                return null;
            }
            return match;
        }

        /// <summary>World → display coordinates of a model-space view (formula of the plot window transform).</summary>
        public static Matrix3d WcsToDcs(AbstractViewTableRecord view)
        {
            Matrix3d m = Matrix3d.PlaneToWorld(view.ViewDirection);
            m = Matrix3d.Displacement(view.Target - Point3d.Origin) * m;
            m = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * m;
            return m.Inverse();
        }

        public static Extents2d ToDcs(Bounds2d wcs, Matrix3d wcsToDcs)
        {
            var corners = new[]
            {
                new Point3d(wcs.MinX, wcs.MinY, 0).TransformBy(wcsToDcs),
                new Point3d(wcs.MaxX, wcs.MinY, 0).TransformBy(wcsToDcs),
                new Point3d(wcs.MaxX, wcs.MaxY, 0).TransformBy(wcsToDcs),
                new Point3d(wcs.MinX, wcs.MaxY, 0).TransformBy(wcsToDcs),
            };
            return new Extents2d(
                corners.Min(p => p.X), corners.Min(p => p.Y),
                corners.Max(p => p.X), corners.Max(p => p.Y));
        }

        public static string Describe(Extents2d e) =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###})-({2:0.###},{3:0.###})",
                e.MinPoint.X, e.MinPoint.Y, e.MaxPoint.X, e.MaxPoint.Y);
    }
}
