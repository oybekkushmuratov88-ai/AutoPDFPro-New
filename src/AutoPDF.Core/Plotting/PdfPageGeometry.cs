using System;
using AutoPDF.Core.Paper;

namespace AutoPDF.Core.Plotting
{
    /// <summary>A PDF page box in points (1/72 inch) plus the page's /Rotate value.</summary>
    public readonly struct PdfPageBox
    {
        public const double PointsPerMm = 72.0 / 25.4;

        public PdfPageBox(double x1, double y1, double x2, double y2, int rotate)
        {
            X1 = Math.Min(x1, x2);
            Y1 = Math.Min(y1, y2);
            X2 = Math.Max(x1, x2);
            Y2 = Math.Max(y1, y2);
            Rotate = ((rotate % 360) + 360) % 360;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public int Rotate { get; }

        public bool IsQuarterTurned => Rotate == 90 || Rotate == 270;

        /// <summary>Page size as a PDF viewer shows it (after /Rotate), in mm.</summary>
        public PaperSize DisplayedSizeMm
        {
            get
            {
                double w = (X2 - X1) / PointsPerMm;
                double h = (Y2 - Y1) / PointsPerMm;
                return IsQuarterTurned ? new PaperSize(h, w) : new PaperSize(w, h);
            }
        }
    }

    public enum PageOrientationCheck
    {
        /// <summary>Displayed page matches the expected size.</summary>
        Match,

        /// <summary>Width and height are exchanged: the drawing was placed with the other rotation.</summary>
        Swapped,

        /// <summary>Anything else (wrong paper).</summary>
        Mismatch
    }

    public static class PdfPageGeometry
    {
        public static PageOrientationCheck Compare(PaperSize actual, PaperSize expected, double toleranceMm)
        {
            if (actual.ApproximatelyEquals(expected, toleranceMm)) return PageOrientationCheck.Match;
            if (actual.ApproximatelyEquals(expected.Rotated(), toleranceMm) && !IsSquare(expected, toleranceMm))
                return PageOrientationCheck.Swapped;
            return PageOrientationCheck.Mismatch;
        }

        /// <summary>
        /// Crop box (in unrotated PDF user space) of <paramref name="targetDisplayed"/> centred on the page.
        /// A centred, exact-scale plot on full-bleed paper puts the sheet frame exactly there.
        /// </summary>
        public static PdfPageBox CenteredCropBox(PdfPageBox page, PaperSize targetDisplayed)
        {
            double tw = targetDisplayed.WidthMm * PdfPageBox.PointsPerMm;
            double th = targetDisplayed.HeightMm * PdfPageBox.PointsPerMm;
            if (page.IsQuarterTurned)
            {
                double t = tw;
                tw = th;
                th = t;
            }

            double cx = (page.X1 + page.X2) / 2.0;
            double cy = (page.Y1 + page.Y2) / 2.0;
            return new PdfPageBox(cx - tw / 2.0, cy - th / 2.0, cx + tw / 2.0, cy + th / 2.0, page.Rotate);
        }

        /// <summary>True when the target fits on the displayed page (the crop does not need area outside the page).</summary>
        public static bool Fits(PaperSize displayedPage, PaperSize target, double toleranceMm) =>
            displayedPage.WidthMm + toleranceMm >= target.WidthMm &&
            displayedPage.HeightMm + toleranceMm >= target.HeightMm;

        private static bool IsSquare(PaperSize size, double tol) => Math.Abs(size.WidthMm - size.HeightMm) <= tol;
    }
}
