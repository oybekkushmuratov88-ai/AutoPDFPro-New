using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoPDF.Core.Paper
{
    public sealed class PaperResolverOptions
    {
        /// <summary>Millimetres per drawing unit (from INSUNITS). 1 for mm drawings.</summary>
        public double MillimetersPerUnit { get; set; } = 1.0;

        /// <summary>Scale denominator N of "1:N" to force. 0 = detect automatically.</summary>
        public double FixedScale { get; set; }

        /// <summary>How far (mm, on paper) a frame may deviate from a known format and still snap to it.</summary>
        public double SnapToleranceMm { get; set; } = 0.5;

        /// <summary>Frames whose short side on paper is smaller than this are not sheets (title-block cells etc.).</summary>
        public double MinShortSideMm { get; set; } = 100;

        /// <summary>Frames whose long side on paper exceeds this are rejected.</summary>
        public double MaxLongSideMm { get; set; } = 5000;

        public IReadOnlyList<PaperFormat> CustomFormats { get; set; } = Array.Empty<PaperFormat>();

        /// <summary>Scales tried in automatic mode, most preferred first.</summary>
        public IReadOnlyList<double> CandidateScales { get; set; } = DefaultScales;

        public static IReadOnlyList<double> DefaultScales { get; } = new double[]
        {
            1, 2, 2.5, 4, 5, 10, 15, 20, 25, 40, 50, 75, 100, 125, 150, 200, 250, 300, 400, 500,
            750, 1000, 2000, 2500, 5000, 10000
        };
    }

    public sealed class PaperResolution
    {
        /// <summary>Paper in the frame's orientation (width = X extent).</summary>
        public PaperSize Paper { get; set; }

        /// <summary>"A4", "A4x3", "1260x594" … or null for an unrecognised custom size.</summary>
        public string FormatName { get; set; }

        public bool IsKnownFormat { get; set; }

        /// <summary>N of the plot scale 1:N.</summary>
        public double ScaleDenominator { get; set; } = 1;

        /// <summary>Drawing units per millimetre of paper — the value passed to CustomScale(1, x).</summary>
        public double DrawingUnitsPerMm { get; set; } = 1;

        public bool IsValid { get; set; }
        public string Problem { get; set; }

        public string ScaleText => "1:" + ScaleDenominator.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>"210 x 297 mm" (+ " · 1:100" when the frame is drawn at a scale).</summary>
        public string DisplayText =>
            Math.Abs(ScaleDenominator - 1) < 1e-9 ? Paper.ToString() : Paper + " · " + ScaleText;
    }

    /// <summary>
    /// Turns a frame's size in drawing units into a paper size in millimetres, detecting the drawing
    /// scale and snapping to known formats (ISO A-series, ГОСТ extended formats, user formats).
    /// </summary>
    public sealed class PaperSizeResolver
    {
        private const double EqualErrorEpsilon = 0.05;

        private readonly PaperResolverOptions _options;
        private readonly List<PaperFormat> _formats;

        public PaperSizeResolver(PaperResolverOptions options)
        {
            _options = options ?? new PaperResolverOptions();
            _formats = new List<PaperFormat>(PaperFormatCatalog.Standard);
            if (_options.CustomFormats != null) _formats.AddRange(_options.CustomFormats);
        }

        public PaperResolution Resolve(double widthUnits, double heightUnits)
        {
            if (!(widthUnits > 0) || !(heightUnits > 0))
            {
                return new PaperResolution
                {
                    Paper = new PaperSize(0, 0),
                    IsValid = false,
                    Problem = "Ramka o‘lchami noto‘g‘ri (nol yoki manfiy)."
                };
            }

            double mmPerUnit = _options.MillimetersPerUnit > 0 ? _options.MillimetersPerUnit : 1.0;
            IReadOnlyList<double> scales = _options.FixedScale > 0
                ? new[] { _options.FixedScale }
                : (_options.CandidateScales ?? PaperResolverOptions.DefaultScales);

            // 1) Snap to a known format. Across all scales take the smallest deviation;
            //    among (practically) equal deviations prefer the smaller scale (the order of the list).
            Match best = null;
            for (int si = 0; si < scales.Count; si++)
            {
                double scale = scales[si];
                if (!(scale > 0)) continue;
                double w = widthUnits * mmPerUnit / scale;
                double h = heightUnits * mmPerUnit / scale;
                bool landscape = w > h;

                foreach (PaperFormat f in _formats)
                {
                    PaperSize oriented = f.Oriented(landscape);
                    double err = Math.Max(Math.Abs(oriented.WidthMm - w), Math.Abs(oriented.HeightMm - h));
                    if (err > _options.SnapToleranceMm) continue;

                    var candidate = new Match(scale, si, f, oriented, err);
                    if (best == null || candidate.IsBetterThan(best)) best = candidate;
                }
            }

            if (best != null)
            {
                return new PaperResolution
                {
                    Paper = best.Paper,
                    FormatName = best.Format.Name,
                    IsKnownFormat = true,
                    ScaleDenominator = best.Scale,
                    DrawingUnitsPerMm = best.Scale / mmPerUnit,
                    IsValid = true
                };
            }

            // 2) Non-standard frame: choose the scale whose short side is closest to a standard edge.
            double chosen = PickScaleForCustom(widthUnits, heightUnits, mmPerUnit, scales);
            double pw = Round01(widthUnits * mmPerUnit / chosen);
            double ph = Round01(heightUnits * mmPerUnit / chosen);
            var paper = new PaperSize(pw, ph);
            var result = new PaperResolution
            {
                Paper = paper,
                FormatName = null,
                IsKnownFormat = false,
                ScaleDenominator = chosen,
                DrawingUnitsPerMm = chosen / mmPerUnit,
                IsValid = true
            };

            if (paper.ShortSideMm < _options.MinShortSideMm)
            {
                result.IsValid = false;
                result.Problem = string.Format(CultureInfo.InvariantCulture,
                    "List juda kichik: {0} (minimal qisqa tomon {1:0} mm).", paper, _options.MinShortSideMm);
            }
            else if (paper.LongSideMm > _options.MaxLongSideMm)
            {
                result.IsValid = false;
                result.Problem = string.Format(CultureInfo.InvariantCulture,
                    "List juda katta: {0} (maksimal uzun tomon {1:0} mm).", paper, _options.MaxLongSideMm);
            }

            return result;
        }

        /// <summary>
        /// Scale for a frame that matches no known format. Strong evidence first: a scale at which the short
        /// side is within 1 % of a standard edge (ГОСТ multiples keep the standard short side, e.g. 594 for
        /// 1260 × 594). Otherwise the smallest scale that gives a plausible paper — normally 1:1.
        /// </summary>
        private double PickScaleForCustom(double widthUnits, double heightUnits, double mmPerUnit, IReadOnlyList<double> scales)
        {
            if (scales.Count == 1) return scales[0];

            const double EdgeEvidence = 0.01;
            double? smallestValid = null;
            double edgeScale = 0;
            double edgeError = double.MaxValue;

            foreach (double scale in scales)
            {
                if (!(scale > 0)) continue;
                double w = widthUnits * mmPerUnit / scale;
                double h = heightUnits * mmPerUnit / scale;
                double shortSide = Math.Min(w, h);
                double longSide = Math.Max(w, h);
                if (shortSide < _options.MinShortSideMm || longSide > _options.MaxLongSideMm) continue;

                if (!smallestValid.HasValue || scale < smallestValid.Value) smallestValid = scale;

                double err = PaperFormatCatalog.StandardEdgesMm.Min(e => Math.Abs(shortSide - e) / e);
                if (err < edgeError - 1e-4 || (Math.Abs(err - edgeError) <= 1e-4 && scale < edgeScale))
                {
                    edgeError = err;
                    edgeScale = scale;
                }
            }

            if (edgeError <= EdgeEvidence) return edgeScale;
            return smallestValid ?? 1.0;
        }

        private static double Round01(double v) => Math.Round(v * 10.0, MidpointRounding.AwayFromZero) / 10.0;

        private sealed class Match
        {
            public Match(double scale, int scaleIndex, PaperFormat format, PaperSize paper, double error)
            {
                Scale = scale;
                ScaleIndex = scaleIndex;
                Format = format;
                Paper = paper;
                Error = error;
            }

            public double Scale { get; }
            public int ScaleIndex { get; }
            public PaperFormat Format { get; }
            public PaperSize Paper { get; }
            public double Error { get; }

            public bool IsBetterThan(Match other)
            {
                if (Error < other.Error - EqualErrorEpsilon) return true;
                if (Error > other.Error + EqualErrorEpsilon) return false;
                if (ScaleIndex != other.ScaleIndex) return ScaleIndex < other.ScaleIndex;
                if (Format.IsCustom != other.Format.IsCustom) return !Format.IsCustom; // standard name wins ties
                return Error < other.Error;
            }
        }
    }
}
