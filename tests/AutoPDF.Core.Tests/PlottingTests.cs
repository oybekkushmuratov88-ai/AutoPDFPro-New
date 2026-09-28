using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoPDF.Core.Naming;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Pdf;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Text;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public class MediaSelectorTests
    {
        // A subset of what DWG To PDF.pc3 reports (sizes for PlotRotation 0°).
        private static readonly List<MediaInfo> DwgToPdf = new List<MediaInfo>
        {
            new MediaInfo("ISO_A4_(210.00_x_297.00_MM)", null, 210, 297, 5.79, 17.8, 5.79, 17.8),
            new MediaInfo("ISO_full_bleed_A4_(210.00_x_297.00_MM)", null, 210, 297),
            new MediaInfo("ISO_full_bleed_A4_(297.00_x_210.00_MM)", null, 297, 210),
            new MediaInfo("ISO_full_bleed_A3_(297.00_x_420.00_MM)", null, 297, 420),
            new MediaInfo("ISO_full_bleed_A3_(420.00_x_297.00_MM)", null, 420, 297),
            new MediaInfo("ISO_full_bleed_A2_(420.00_x_594.00_MM)", null, 420, 594),
            new MediaInfo("ISO_full_bleed_A2_(594.00_x_420.00_MM)", null, 594, 420),
            new MediaInfo("ISO_full_bleed_A1_(594.00_x_841.00_MM)", null, 594, 841),
            new MediaInfo("ISO_full_bleed_A1_(841.00_x_594.00_MM)", null, 841, 594),
            new MediaInfo("ISO_full_bleed_A0_(841.00_x_1189.00_MM)", null, 841, 1189),
            new MediaInfo("ISO_full_bleed_A0_(1189.00_x_841.00_MM)", null, 1189, 841),
            new MediaInfo("ISO_full_bleed_2A0_(1189.00_x_1682.00_MM)", null, 1189, 1682),
        };

        [Theory]
        [InlineData(210, 297, "ISO_full_bleed_A4_(210.00_x_297.00_MM)")]
        [InlineData(297, 210, "ISO_full_bleed_A4_(297.00_x_210.00_MM)")]
        [InlineData(420, 297, "ISO_full_bleed_A3_(420.00_x_297.00_MM)")]
        [InlineData(841, 1189, "ISO_full_bleed_A0_(841.00_x_1189.00_MM)")]
        public void StandardSizes_UseExactFullBleedPaperWithoutRotation(double w, double h, string expected)
        {
            MediaPlan plan = MediaSelector.Select("DWG To PDF.pc3", DwgToPdf, new PaperSize(w, h));
            Assert.Equal(expected, plan.Media.CanonicalName);
            Assert.Equal(MediaFit.Exact, plan.Fit);
            Assert.False(plan.Rotate90);
        }

        [Fact]
        public void PaperWithMargins_IsNeverChosen()
        {
            var onlyMargins = DwgToPdf.Where(m => !m.IsFullBleed()).ToList();
            Assert.Null(MediaSelector.Select("d", onlyMargins, new PaperSize(210, 297)));
        }

        [Fact]
        public void A4x3_IsCroppedFromA1Landscape()
        {
            MediaPlan plan = MediaSelector.Select("d", DwgToPdf, new PaperSize(630, 297));
            Assert.Equal(MediaFit.Crop, plan.Fit);
            Assert.Equal("ISO_full_bleed_A1_(841.00_x_594.00_MM)", plan.Media.CanonicalName);
            Assert.False(plan.Rotate90);
            Assert.Equal(new PaperSize(630, 297), plan.TargetPage);
        }

        [Fact]
        public void A3x3_IsCroppedFromA0Landscape()
        {
            MediaPlan plan = MediaSelector.Select("d", DwgToPdf, new PaperSize(891, 420));
            Assert.Equal(MediaFit.Crop, plan.Fit);
            Assert.Equal("ISO_full_bleed_A0_(1189.00_x_841.00_MM)", plan.Media.CanonicalName);
        }

        [Fact]
        public void TooWideSheet_UsesRotatedLargerPaper()
        {
            MediaPlan plan = MediaSelector.Select("d", DwgToPdf, new PaperSize(1260, 594));
            Assert.Equal(MediaFit.Crop, plan.Fit);
            Assert.True(plan.Rotate90);
            Assert.Equal("ISO_full_bleed_2A0_(1189.00_x_1682.00_MM)", plan.Media.CanonicalName);
            Assert.Equal(new PaperSize(1682, 1189), plan.ExpectedPage);
        }

        [Fact]
        public void CustomExactPaper_WinsOverCrop()
        {
            var withCustom = new List<MediaInfo>(DwgToPdf)
            {
                new MediaInfo("UserDefinedMetric_(1260.00_x_594.00MM)", "Пользовательский (1260.00 x 594.00 мм)", 1260, 594)
            };
            MediaPlan plan = MediaSelector.Select("d", withCustom, new PaperSize(1260, 594));
            Assert.Equal(MediaFit.Exact, plan.Fit);
            Assert.False(plan.Rotate90);
        }

        [Fact]
        public void NoCropAllowed_ReturnsNullForNonStandard()
        {
            Assert.Null(MediaSelector.Select("d", DwgToPdf, new PaperSize(630, 297), new MediaSelectionOptions { AllowCrop = false }));
        }
    }

    public class PdfGeometryTests
    {
        private const double K = PdfPageBox.PointsPerMm;

        [Fact]
        public void DisplayedSize_HonoursRotate()
        {
            var box = new PdfPageBox(0, 0, 297 * K, 210 * K, 90);
            Assert.True(box.DisplayedSizeMm.ApproximatelyEquals(new PaperSize(210, 297), 0.01));
        }

        [Fact]
        public void CropBox_IsCentred()
        {
            var page = new PdfPageBox(0, 0, 841 * K, 594 * K, 0);
            PdfPageBox crop = PdfPageGeometry.CenteredCropBox(page, new PaperSize(630, 297));
            Assert.Equal((841 - 630) / 2.0 * K, crop.X1, 6);
            Assert.Equal((594 - 297) / 2.0 * K, crop.Y1, 6);
            Assert.True(crop.DisplayedSizeMm.ApproximatelyEquals(new PaperSize(630, 297), 0.01));
        }

        [Fact]
        public void CropBox_OnQuarterTurnedPage_SwapsTarget()
        {
            var page = new PdfPageBox(0, 0, 1189 * K, 1682 * K, 90); // displayed 1682 x 1189
            PdfPageBox crop = PdfPageGeometry.CenteredCropBox(page, new PaperSize(1260, 594));
            Assert.True(crop.DisplayedSizeMm.ApproximatelyEquals(new PaperSize(1260, 594), 0.01));
        }

        [Fact]
        public void OrientationCheck()
        {
            Assert.Equal(PageOrientationCheck.Match, PdfPageGeometry.Compare(new PaperSize(420.3, 297), new PaperSize(420, 297), 1));
            Assert.Equal(PageOrientationCheck.Swapped, PdfPageGeometry.Compare(new PaperSize(297, 420), new PaperSize(420, 297), 1));
            Assert.Equal(PageOrientationCheck.Mismatch, PdfPageGeometry.Compare(new PaperSize(210, 297), new PaperSize(420, 297), 1));
        }
    }

    public class PlotCommandBuilderTests
    {
        [Fact]
        public void UsesLocaleMediaNameGlobalKeywordsAndEnterForLayout()
        {
            var tokens = PlotCommandBuilder.Build(new PlotCommandInput
            {
                DeviceName = "DWG To PDF.pc3",
                LocaleMediaName = "ISO без полей A4 (210.00 x 297.00 мм)",
                Landscape = false,
                LowerLeft = new PlotPoint(0, 0),
                UpperRight = new PlotPoint(210, 297),
                DrawingUnitsPerMm = 1,
                StyleTable = "monochrome.ctb",
                OutputPath = @"C:\out\01.pdf"
            });

            Assert.Equal("_.-PLOT", tokens[0]);
            Assert.Equal("", tokens[2]);
            Assert.Equal("ISO без полей A4 (210.00 x 297.00 мм)", tokens[4]);
            Assert.Equal("_Portrait", tokens[6]);
            Assert.IsType<PlotPoint>(tokens[9]);
            Assert.Equal("1=1", tokens[11]);
            Assert.Equal("monochrome.ctb", tokens[14]);
            Assert.Equal(@"C:\out\01.pdf", tokens[tokens.Count - 3]);
            Assert.Equal("_Yes", tokens[tokens.Count - 1]);
            // Every keyword answer is a global ("_") keyword so it works in the Russian AutoCAD.
            var keywordAnswers = new[] { 1, 5, 6, 7, 8, 12, 13, 15, 16, tokens.Count - 2, tokens.Count - 1 };
            Assert.All(keywordAnswers, i => Assert.StartsWith("_", (string)tokens[i]));
        }

        [Fact]
        public void NoStyleTable_UsesDot_AndWriteToFileVariantAddsAnswer()
        {
            var tokens = PlotCommandBuilder.Build(new PlotCommandInput
            {
                DeviceName = "X.pc3", LocaleMediaName = "A", OutputPath = "o.pdf", DeviceAsksWriteToFile = true,
                DrawingUnitsPerMm = 100
            });
            Assert.Contains(".", tokens.OfType<string>());
            Assert.Equal("1=100", tokens[11]);
            Assert.Equal("_Yes", tokens[tokens.Count - 4]);
        }
    }

    public class NamingSettingsAndPdfTests
    {
        [Theory]
        [InlineData(1, 12, "01.pdf")]
        [InlineData(9, 99, "09.pdf")]
        [InlineData(7, 120, "007.pdf")]
        public void FileNames_AreZeroPadded(int n, int max, string expected)
        {
            Assert.Equal(expected, OutputFileNamer.BuildFileName("{N}", n, OutputFileNamer.PadWidth(max)));
        }

        [Fact]
        public void Pattern_TokensAndSanitizing()
        {
            Assert.Equal("Plan_1_03_A4.pdf", OutputFileNamer.BuildFileName("{DWG}_{N}_{FORMAT}", 3, 2, "Plan:1", "A4"));
        }

        [Fact]
        public void MakeUnique_AddsSuffix()
        {
            var existing = new HashSet<string> { Path.Combine("d", "01.pdf"), Path.Combine("d", "01_1.pdf") };
            Assert.Equal(Path.Combine("d", "01_2.pdf"), OutputFileNamer.MakeUnique(Path.Combine("d", "01.pdf"), existing.Contains));
        }

        [Fact]
        public void Wildcards()
        {
            var m = new WildcardMatcher("Ramka*, A#_list, format?");
            Assert.True(m.IsMatch("RAMKA_A3"));
            Assert.True(m.IsMatch("A4_list"));
            Assert.True(m.IsMatch("formatX"));
            Assert.False(m.IsMatch("A_list"));
        }

        [Fact]
        public void Settings_RoundTripAndRecoverFromCorruptFile()
        {
            string dir = Path.Combine(Path.GetTempPath(), "apdf-test-" + System.Guid.NewGuid());
            string file = Path.Combine(dir, "settings.xml");
            var svc = new SettingsService(file);

            var s = new PluginSettings { OutputFolder = @"C:\PDF", StartNumber = 5, FixedScale = 100 };
            svc.Save(s);
            var loaded = svc.Load();
            Assert.Equal(@"C:\PDF", loaded.OutputFolder);
            Assert.Equal(5, loaded.StartNumber);
            Assert.Equal(100, loaded.FixedScale);

            File.WriteAllText(file, "<not xml");
            var recovered = svc.Load();
            Assert.Equal(PluginSettings.DefaultPlotter, recovered.Plotter);
            Directory.Delete(dir, true);
        }

        [Fact]
        public void PdfVerifier_DetectsMissingEmptyAndValidFiles()
        {
            string dir = Path.Combine(Path.GetTempPath(), "apdf-pdf-" + System.Guid.NewGuid());
            Directory.CreateDirectory(dir);

            Assert.False(PdfFileVerifier.Verify(Path.Combine(dir, "none.pdf")).IsValid);

            string empty = Path.Combine(dir, "empty.pdf");
            File.WriteAllBytes(empty, new byte[0]);
            Assert.False(PdfFileVerifier.Verify(empty).IsValid);

            string ok = Path.Combine(dir, "ok.pdf");
            File.WriteAllText(ok, "%PDF-1.6\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");
            Assert.True(PdfFileVerifier.Verify(ok).IsValid);

            Directory.Delete(dir, true);
        }
    }
}
