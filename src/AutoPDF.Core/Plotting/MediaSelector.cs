using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Localization;

namespace AutoPDF.Core.Plotting
{
    /// <summary>One paper ("media") of a plotter, as reported by AutoCAD for PlotRotation = 0°.</summary>
    public sealed class MediaInfo
    {
        public MediaInfo(string canonicalName, string localeName, double widthMm, double heightMm,
            double marginLeft = 0, double marginBottom = 0, double marginRight = 0, double marginTop = 0)
        {
            CanonicalName = canonicalName;
            LocaleName = string.IsNullOrEmpty(localeName) ? canonicalName : localeName;
            WidthMm = widthMm;
            HeightMm = heightMm;
            MarginLeft = marginLeft;
            MarginBottom = marginBottom;
            MarginRight = marginRight;
            MarginTop = marginTop;
        }

        /// <summary>Language-independent name, e.g. "ISO_full_bleed_A4_(210.00_x_297.00_MM)".</summary>
        public string CanonicalName { get; }

        /// <summary>Name shown in the (Russian) UI and expected by the -PLOT command.</summary>
        public string LocaleName { get; }

        public double WidthMm { get; }
        public double HeightMm { get; }
        public double MarginLeft { get; }
        public double MarginBottom { get; }
        public double MarginRight { get; }
        public double MarginTop { get; }

        public double Area => WidthMm * HeightMm;

        /// <summary>No unprintable margins (DWG To PDF "full bleed" papers).</summary>
        public bool IsFullBleed(double toleranceMm = 0.5)
        {
            // AutoCAD reports margins as distances from the edges; be tolerant of an API that reports the
            // printable corner coordinates instead (max corner == paper size).
            bool minZero = Math.Abs(MarginLeft) <= toleranceMm && Math.Abs(MarginBottom) <= toleranceMm;
            bool maxZero = Math.Abs(MarginRight) <= toleranceMm && Math.Abs(MarginTop) <= toleranceMm;
            bool maxIsPaper = Math.Abs(MarginRight - WidthMm) <= toleranceMm && Math.Abs(MarginTop - HeightMm) <= toleranceMm;
            return minZero && (maxZero || maxIsPaper);
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0} [{1:0.##} x {2:0.##} mm]", CanonicalName, WidthMm, HeightMm);
    }

    public enum MediaFit
    {
        /// <summary>The plotter has paper of exactly the sheet size.</summary>
        Exact,

        /// <summary>Plot on a larger full-bleed paper (centred, exact scale) and crop the PDF page afterwards.</summary>
        Crop
    }

    public sealed class MediaPlan
    {
        public string DeviceName { get; set; }
        public MediaInfo Media { get; set; }

        /// <summary>PlotRotation 90° ("Landscape" in -PLOT) instead of 0° ("Portrait").</summary>
        public bool Rotate90 { get; set; }

        public MediaFit Fit { get; set; }

        /// <summary>The page the PDF is expected to have (width × height as displayed) before cropping.</summary>
        public PaperSize ExpectedPage { get; set; }

        /// <summary>Final page size after cropping (= the sheet's paper).</summary>
        public PaperSize TargetPage { get; set; }

        public MediaPlan WithRotation(bool rotate90)
        {
            var plan = (MediaPlan)MemberwiseClone();
            plan.Rotate90 = rotate90;
            return plan;
        }

        public string Describe() =>
            string.Format(CultureInfo.InvariantCulture, "{0} · {1} · {2}{3}",
                DeviceName,
                Media?.LocaleName,
                Rotate90 ? "90°" : "0°",
                Fit == MediaFit.Crop ? " · " + L.PlanCrop + " → " + TargetPage : string.Empty);
    }

    public sealed class MediaSelectionOptions
    {
        /// <summary>Max difference (mm) for a paper to count as "exactly" the sheet size.</summary>
        public double ExactToleranceMm { get; set; } = 0.6;

        public bool AllowCrop { get; set; } = true;

        /// <summary>
        /// A rotated plan is only chosen over a non-rotated one when its paper is at most this share of the
        /// non-rotated paper's area (rotation adds uncertainty; see PdfExportService orientation check).
        /// </summary>
        public double RotatedAreaAdvantage { get; set; } = 0.7;
    }

    /// <summary>
    /// Chooses the plotter paper for a sheet. Order of preference:
    /// 1) full-bleed paper of exactly the sheet size, same orientation (0°);
    /// 2) same paper turned 90°;
    /// 3) smallest full-bleed paper that contains the sheet — the PDF is cropped afterwards.
    /// Papers with unprintable margins are never used: a centred exact-scale plot would lose its edges.
    /// </summary>
    public static class MediaSelector
    {
        public static MediaPlan Select(string deviceName, IReadOnlyList<MediaInfo> media, PaperSize target, MediaSelectionOptions options = null)
        {
            options = options ?? new MediaSelectionOptions();
            if (media == null || media.Count == 0) return null;

            List<MediaInfo> usable = media.Where(m => m.WidthMm > 0 && m.HeightMm > 0 && m.IsFullBleed()).ToList();
            double tol = options.ExactToleranceMm;

            // 1 + 2: exact size.
            MediaInfo exact0 = usable
                .Where(m => Math.Abs(m.WidthMm - target.WidthMm) <= tol && Math.Abs(m.HeightMm - target.HeightMm) <= tol)
                .OrderBy(m => Math.Abs(m.WidthMm - target.WidthMm) + Math.Abs(m.HeightMm - target.HeightMm))
                .ThenBy(m => FullBleedNameRank(m))
                .FirstOrDefault();
            if (exact0 != null)
            {
                return new MediaPlan
                {
                    DeviceName = deviceName, Media = exact0, Rotate90 = false, Fit = MediaFit.Exact,
                    ExpectedPage = new PaperSize(exact0.WidthMm, exact0.HeightMm), TargetPage = target
                };
            }

            MediaInfo exact90 = usable
                .Where(m => Math.Abs(m.WidthMm - target.HeightMm) <= tol && Math.Abs(m.HeightMm - target.WidthMm) <= tol)
                .OrderBy(m => Math.Abs(m.WidthMm - target.HeightMm) + Math.Abs(m.HeightMm - target.WidthMm))
                .ThenBy(m => FullBleedNameRank(m))
                .FirstOrDefault();
            if (exact90 != null)
            {
                return new MediaPlan
                {
                    DeviceName = deviceName, Media = exact90, Rotate90 = true, Fit = MediaFit.Exact,
                    ExpectedPage = new PaperSize(exact90.HeightMm, exact90.WidthMm), TargetPage = target
                };
            }

            if (!options.AllowCrop) return null;

            // 3: smallest containing paper.
            MediaInfo crop0 = usable
                .Where(m => m.WidthMm + tol >= target.WidthMm && m.HeightMm + tol >= target.HeightMm)
                .OrderBy(m => m.Area)
                .ThenBy(m => FullBleedNameRank(m))
                .FirstOrDefault();
            MediaInfo crop90 = usable
                .Where(m => m.WidthMm + tol >= target.HeightMm && m.HeightMm + tol >= target.WidthMm)
                .OrderBy(m => m.Area)
                .ThenBy(m => FullBleedNameRank(m))
                .FirstOrDefault();

            bool useRotated = crop90 != null &&
                              (crop0 == null || crop90.Area <= crop0.Area * options.RotatedAreaAdvantage);

            if (!useRotated && crop0 != null)
            {
                return new MediaPlan
                {
                    DeviceName = deviceName, Media = crop0, Rotate90 = false, Fit = MediaFit.Crop,
                    ExpectedPage = new PaperSize(crop0.WidthMm, crop0.HeightMm), TargetPage = target
                };
            }

            if (crop90 != null)
            {
                return new MediaPlan
                {
                    DeviceName = deviceName, Media = crop90, Rotate90 = true, Fit = MediaFit.Crop,
                    ExpectedPage = new PaperSize(crop90.HeightMm, crop90.WidthMm), TargetPage = target
                };
            }

            return null;
        }

        /// <summary>Only exact-size plans (used to search the optional custom-sizes plotter).</summary>
        public static MediaPlan SelectExact(string deviceName, IReadOnlyList<MediaInfo> media, PaperSize target, MediaSelectionOptions options = null)
        {
            var o = options ?? new MediaSelectionOptions();
            var exactOnly = new MediaSelectionOptions
            {
                ExactToleranceMm = o.ExactToleranceMm,
                AllowCrop = false,
                RotatedAreaAdvantage = o.RotatedAreaAdvantage
            };
            return Select(deviceName, media, target, exactOnly);
        }

        private static int FullBleedNameRank(MediaInfo m) =>
            m.CanonicalName != null && m.CanonicalName.IndexOf("full_bleed", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1;
    }
}
