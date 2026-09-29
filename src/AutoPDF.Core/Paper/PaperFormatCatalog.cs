using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AutoPDF.Core.Localization;

namespace AutoPDF.Core.Paper
{
    /// <summary>A named paper format, stored orientation-independent (short × long side).</summary>
    public sealed class PaperFormat
    {
        public PaperFormat(string name, double shortMm, double longMm, bool isCustom = false)
        {
            Name = name;
            ShortMm = Math.Min(shortMm, longMm);
            LongMm = Math.Max(shortMm, longMm);
            IsCustom = isCustom;
        }

        public string Name { get; }
        public double ShortMm { get; }
        public double LongMm { get; }
        public bool IsCustom { get; }

        /// <summary>The format oriented like a sheet of the given orientation.</summary>
        public PaperSize Oriented(bool landscape) =>
            landscape ? new PaperSize(LongMm, ShortMm) : new PaperSize(ShortMm, LongMm);

        public override string ToString() =>
            Name + " (" + PaperSize.FormatMm(ShortMm) + " x " + PaperSize.FormatMm(LongMm) + " mm)";
    }

    /// <summary>
    /// Known sheet formats: ISO 216 A-series plus the ГОСТ 2.301-68 / ЕСКД extended formats
    /// (A4x3 = 297 × 630, A3x3 = 420 × 891, A2x3 = 594 × 1261 …) that are common in drawings
    /// made with Russian-language AutoCAD.
    /// </summary>
    public static class PaperFormatCatalog
    {
        private static readonly Regex CustomItem = new Regex(
            @"^\s*(?<w>\d+(?:[.,]\d+)?)\s*[xX×*х]\s*(?<h>\d+(?:[.,]\d+)?)\s*$",
            RegexOptions.CultureInvariant);

        public static IReadOnlyList<PaperFormat> Standard { get; } = new List<PaperFormat>
        {
            new PaperFormat("A5", 148, 210),
            new PaperFormat("A4", 210, 297),
            new PaperFormat("A3", 297, 420),
            new PaperFormat("A2", 420, 594),
            new PaperFormat("A1", 594, 841),
            new PaperFormat("A0", 841, 1189),

            new PaperFormat("A4x3", 297, 630),
            new PaperFormat("A4x4", 297, 841),
            new PaperFormat("A4x5", 297, 1051),
            new PaperFormat("A4x6", 297, 1261),
            new PaperFormat("A4x7", 297, 1471),
            new PaperFormat("A4x8", 297, 1682),
            new PaperFormat("A4x9", 297, 1892),

            new PaperFormat("A3x3", 420, 891),
            new PaperFormat("A3x4", 420, 1189),
            new PaperFormat("A3x5", 420, 1486),
            new PaperFormat("A3x6", 420, 1783),
            new PaperFormat("A3x7", 420, 2080),

            new PaperFormat("A2x3", 594, 1261),
            new PaperFormat("A2x4", 594, 1682),
            new PaperFormat("A2x5", 594, 2102),

            new PaperFormat("A1x3", 841, 1783),
            new PaperFormat("A1x4", 841, 2378),

            new PaperFormat("A0x2", 1189, 1682),
            new PaperFormat("A0x3", 1189, 2523),
        };

        /// <summary>Short sides of the standard formats, used to guess the scale of non-standard frames.</summary>
        public static IReadOnlyList<double> StandardEdgesMm { get; } = new double[] { 148, 210, 297, 420, 594, 841, 1189 };

        /// <summary>
        /// Parses a user list such as "630x297; 891x420; 1260x594" (separators ; , or new line).
        /// Invalid items are ignored.
        /// </summary>
        public static IReadOnlyList<PaperFormat> ParseCustom(string text)
        {
            var result = new List<PaperFormat>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            foreach (string raw in text.Split(new[] { ';', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // A comma is a list separator unless it is a decimal comma inside a number ("630,5x297").
                foreach (string item in SplitOnListCommas(raw))
                {
                    Match m = CustomItem.Match(item);
                    if (!m.Success) continue;
                    double w = ParseNumber(m.Groups["w"].Value);
                    double h = ParseNumber(m.Groups["h"].Value);
                    if (w <= 0 || h <= 0) continue;
                    string name = PaperSize.FormatMm(w) + "x" + PaperSize.FormatMm(h);
                    if (result.Any(f => f.Name == name)) continue;
                    result.Add(new PaperFormat(name, w, h, isCustom: true));
                }
            }
            return result;
        }

        private static IEnumerable<string> SplitOnListCommas(string raw)
        {
            // "630x297, 891x420" → two items; "630,5x297" → one item.
            var parts = new List<string>();
            int start = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] != ',') continue;
                bool digitBefore = i > 0 && char.IsDigit(raw[i - 1]);
                bool digitAfter = i + 1 < raw.Length && char.IsDigit(raw[i + 1]);
                if (digitBefore && digitAfter) continue; // decimal comma
                parts.Add(raw.Substring(start, i - start));
                start = i + 1;
            }
            parts.Add(raw.Substring(start));
            return parts.Where(p => !string.IsNullOrWhiteSpace(p));
        }

        private static double ParseNumber(string s) =>
            double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
    }

    /// <summary>Conversion of AutoCAD INSUNITS codes to millimetres.</summary>
    public static class DrawingUnits
    {
        public static double MillimetersPerUnit(int insunits)
        {
            switch (insunits)
            {
                case 1: return 25.4;      // inches
                case 2: return 304.8;     // feet
                case 4: return 1.0;       // millimetres
                case 5: return 10.0;      // centimetres
                case 6: return 1000.0;    // metres
                case 14: return 100.0;    // decimetres
                default: return 1.0;      // unitless and everything else: assume millimetres
            }
        }

        public static string Describe(int insunits)
        {
            switch (insunits)
            {
                case 0: return L.UnitsUnitless;
                case 1: return L.UnitsInches;
                case 2: return L.UnitsFeet;
                case 4: return L.UnitsMm;
                case 5: return L.UnitsCm;
                case 6: return L.UnitsM;
                case 14: return L.UnitsDm;
                default: return "INSUNITS=" + insunits.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
