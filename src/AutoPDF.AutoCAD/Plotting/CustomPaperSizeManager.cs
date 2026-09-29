using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Localization;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutoPDF.AutoCAD.Plotting
{
    /// <summary>
    /// Knows which papers ("media") each plotter really has and picks the paper for a sheet.
    ///
    /// Custom sizes such as 630 × 297, 891 × 420 or 1260 × 594 are handled without touching the user's
    /// plotter configuration:
    ///   1. exact paper on the main plotter (DWG To PDF.pc3 has every ISO A size in both orientations);
    ///   2. exact paper on the optional "custom sizes" plotter — a copy of DWG To PDF.pc3 to which the user
    ///      added the sizes (see README). It is used only when it is installed;
    ///   3. otherwise the smallest full-bleed paper that holds the sheet: plotted centred at the exact scale,
    ///      then the PDF page is cropped to the exact size (PdfExportService).
    /// AutoCAD has no API to add paper sizes to a PC3/PMP, and editing those binary files programmatically
    /// risks breaking the user's plotters — so nothing is ever written to them.
    ///
    /// Sizes are read from AutoCAD (PlotPaperSize / PlotPaperMargins for each canonical media name), not
    /// parsed from the names, so user-defined papers with any naming are recognised.
    /// </summary>
    public sealed class CustomPaperSizeManager
    {
        private static readonly Dictionary<string, List<MediaInfo>> CatalogCache =
            new Dictionary<string, List<MediaInfo>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Learned rotation per (device, media, expected orientation) — see <see cref="LearnRotation"/>.</summary>
        private static readonly Dictionary<string, bool> LearnedRotation = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private readonly LoggingService _log;
        private List<string> _devices;

        public CustomPaperSizeManager(LoggingService log)
        {
            _log = log;
        }

        public static void ClearCache()
        {
            CatalogCache.Clear();
        }

        public IReadOnlyList<string> GetDevices()
        {
            if (_devices != null) return _devices;
            _devices = new List<string>();
            try
            {
                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                using (var ps = new PlotSettings(true))
                {
                    psv.RefreshLists(ps);
                    StringCollection list = psv.GetPlotDeviceList();
                    foreach (string d in list) _devices.Add(d);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(L.DeviceListFailed(AcadErrors.Describe(ex)));
            }
            return _devices;
        }

        public bool DeviceExists(string device) =>
            !string.IsNullOrWhiteSpace(device) && GetDevices().Any(d => string.Equals(d, device, StringComparison.OrdinalIgnoreCase));

        /// <summary>Exact device name as AutoCAD lists it (case may differ from the settings).</summary>
        public string CanonicalDeviceName(string device) =>
            GetDevices().FirstOrDefault(d => string.Equals(d, device, StringComparison.OrdinalIgnoreCase)) ?? device;

        public IReadOnlyList<MediaInfo> GetMedia(string device)
        {
            if (CatalogCache.TryGetValue(device, out List<MediaInfo> cached)) return cached;

            var list = new List<MediaInfo>();
            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            using (var ps = new PlotSettings(true))
            {
                try
                {
                    psv.SetPlotConfigurationName(ps, device, null);
                    psv.RefreshLists(ps);
                }
                catch (Exception ex)
                {
                    throw new PlotStepException("media:open-device", L.DeviceOpenFailed(device, AcadErrors.Describe(ex)), ex);
                }

                StringCollection names = psv.GetCanonicalMediaNameList(ps);
                int failed = 0;
                foreach (string name in names)
                {
                    try
                    {
                        psv.SetCanonicalMediaName(ps, name);
                        var size = ps.PlotPaperSize;
                        var margins = ps.PlotPaperMargins;
                        string locale = SafeLocaleName(psv, ps, name);
                        list.Add(new MediaInfo(name, locale, size.X, size.Y,
                            margins.MinPoint.X, margins.MinPoint.Y, margins.MaxPoint.X, margins.MaxPoint.Y));
                    }
                    catch
                    {
                        failed++;
                    }
                }

                _log.Info(L.MediaCatalog(device, list.Count, list.Count(m => m.IsFullBleed()), failed));
            }

            CatalogCache[device] = list;
            return list;
        }

        private static string SafeLocaleName(PlotSettingsValidator psv, PlotSettings ps, string canonical)
        {
            try { return psv.GetLocaleMediaName(ps, canonical); }
            catch { return canonical; }
        }

        /// <summary>Chooses device + paper + rotation for a sheet, or returns null with <paramref name="problem"/>.</summary>
        public MediaPlan Plan(PaperSize paper, PluginSettings settings, out string problem)
        {
            problem = null;
            var options = new MediaSelectionOptions { AllowCrop = settings.AllowCropForNonStandard };

            string main = CanonicalDeviceName(settings.Plotter);
            if (!DeviceExists(main))
            {
                problem = L.PlotterNotFoundSettings(settings.Plotter);
                return null;
            }

            IReadOnlyList<MediaInfo> mainMedia;
            try
            {
                mainMedia = GetMedia(main);
            }
            catch (Exception ex)
            {
                problem = AcadErrors.Describe(ex);
                return null;
            }

            MediaPlan plan = MediaSelector.SelectExact(main, mainMedia, paper, options);

            if (plan == null && !string.IsNullOrWhiteSpace(settings.CustomSizesPlotter) &&
                !string.Equals(settings.CustomSizesPlotter, main, StringComparison.OrdinalIgnoreCase) &&
                DeviceExists(settings.CustomSizesPlotter))
            {
                string custom = CanonicalDeviceName(settings.CustomSizesPlotter);
                try
                {
                    plan = MediaSelector.SelectExact(custom, GetMedia(custom), paper, options);
                }
                catch (Exception ex)
                {
                    _log.Warn(L.CustomPlotterReadFailed(AcadErrors.Describe(ex)));
                }
            }

            if (plan == null && options.AllowCrop)
            {
                plan = MediaSelector.Select(main, mainMedia, paper, options);
            }

            if (plan == null)
            {
                problem = options.AllowCrop ? L.NoMediaForCrop(main, paper) : L.NoMediaExact(main, paper);
                return null;
            }

            if (LearnedRotation.TryGetValue(RotationKey(plan), out bool rotate) && rotate != plan.Rotate90)
            {
                plan = plan.WithRotation(rotate);
            }
            return plan;
        }

        /// <summary>
        /// The PDF came out with width and height exchanged, i.e. this device places the drawing the other way
        /// round for this paper. Remember the rotation that gives <see cref="MediaPlan.ExpectedPage"/>.
        /// </summary>
        public void LearnRotation(MediaPlan plan, bool rotate90ThatWorks)
        {
            LearnedRotation[RotationKey(plan)] = rotate90ThatWorks;
        }

        private static string RotationKey(MediaPlan plan) =>
            plan.DeviceName + "|" + plan.Media.CanonicalName + "|" + (plan.ExpectedPage.IsLandscape ? "L" : "P");

        /// <summary>Human-readable dump for the AUTOPDFMEDIA command.</summary>
        public string Describe(string device, IEnumerable<PaperSize> testSizes, PluginSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Plotter: " + device);
            IReadOnlyList<MediaInfo> media = GetMedia(device);
            foreach (MediaInfo m in media.Where(x => x.IsFullBleed()).OrderBy(x => x.Area))
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  [{0}] {1,8:0.##} x {2,-8:0.##}  {3}  |  {4}",
                    L.DiagFullBleed, m.WidthMm, m.HeightMm, m.CanonicalName, m.LocaleName));
            }
            sb.AppendLine("  " + L.DiagWithMargins(media.Count(x => !x.IsFullBleed())));
            sb.AppendLine(L.DiagChoices);
            foreach (PaperSize size in testSizes)
            {
                MediaPlan plan = Plan(size, settings, out string problem);
                sb.AppendLine("  " + size + " → " + (plan != null ? plan.Describe() + (plan.Fit == MediaFit.Exact ? " " + L.DiagExact : string.Empty) : L.DiagNone(problem)));
            }
            return sb.ToString();
        }
    }
}
