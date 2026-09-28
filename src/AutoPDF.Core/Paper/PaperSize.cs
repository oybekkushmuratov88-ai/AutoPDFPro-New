using System;
using System.Globalization;

namespace AutoPDF.Core.Paper
{
    /// <summary>Paper size in millimetres, in the orientation the sheet is drawn (width = X extent).</summary>
    public readonly struct PaperSize : IEquatable<PaperSize>
    {
        public PaperSize(double widthMm, double heightMm)
        {
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public double WidthMm { get; }
        public double HeightMm { get; }

        public bool IsLandscape => WidthMm > HeightMm;
        public double ShortSideMm => Math.Min(WidthMm, HeightMm);
        public double LongSideMm => Math.Max(WidthMm, HeightMm);

        /// <summary>Same paper turned by 90°.</summary>
        public PaperSize Rotated() => new PaperSize(HeightMm, WidthMm);

        public bool ApproximatelyEquals(PaperSize other, double toleranceMm) =>
            Math.Abs(WidthMm - other.WidthMm) <= toleranceMm &&
            Math.Abs(HeightMm - other.HeightMm) <= toleranceMm;

        /// <summary>Invariant key such as "210x297".</summary>
        public string Key => FormatMm(WidthMm) + "x" + FormatMm(HeightMm);

        /// <summary>Display text such as "210 x 297 mm".</summary>
        public override string ToString() => FormatMm(WidthMm) + " x " + FormatMm(HeightMm) + " mm";

        public static string FormatMm(double value)
        {
            double rounded = Math.Round(value);
            return Math.Abs(value - rounded) < 0.05
                ? rounded.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        public bool Equals(PaperSize other) => Key == other.Key;
        public override bool Equals(object obj) => obj is PaperSize p && Equals(p);
        public override int GetHashCode() => Key.GetHashCode();
    }
}
