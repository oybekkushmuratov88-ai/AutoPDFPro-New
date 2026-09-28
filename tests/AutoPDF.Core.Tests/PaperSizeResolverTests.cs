using AutoPDF.Core.Paper;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public class PaperSizeResolverTests
    {
        private static PaperSizeResolver Resolver(double fixedScale = 0, double mmPerUnit = 1) =>
            new PaperSizeResolver(new PaperResolverOptions
            {
                FixedScale = fixedScale,
                MillimetersPerUnit = mmPerUnit,
                CustomFormats = PaperFormatCatalog.ParseCustom("630x297; 891x420; 1260x594")
            });

        [Theory]
        [InlineData(210, 297, "A4")]
        [InlineData(297, 420, "A3")]
        [InlineData(420, 594, "A2")]
        [InlineData(594, 841, "A1")]
        [InlineData(841, 1189, "A0")]
        [InlineData(420, 297, "A3")]
        [InlineData(630, 297, "A4x3")]
        [InlineData(891, 420, "A3x3")]
        [InlineData(1260, 594, "1260x594")]
        public void RequirementExamples_AtFullScale(double w, double h, string expectedName)
        {
            PaperResolution r = Resolver().Resolve(w, h);

            Assert.True(r.IsValid);
            Assert.Equal(expectedName, r.FormatName);
            Assert.Equal(1, r.ScaleDenominator);
            Assert.Equal(w, r.Paper.WidthMm, 3);
            Assert.Equal(h, r.Paper.HeightMm, 3);
        }

        [Fact]
        public void DisplayText_MatchesTableExample()
        {
            Assert.Equal("210 x 297 mm", Resolver().Resolve(210, 297).DisplayText);
        }

        [Fact]
        public void KeepsOrientationOfFrame()
        {
            PaperResolution r = Resolver().Resolve(420, 297);
            Assert.True(r.Paper.IsLandscape);
            Assert.Equal("420 x 297 mm", r.Paper.ToString());
        }

        [Fact]
        public void DetectsScale100()
        {
            PaperResolution r = Resolver().Resolve(29700, 42000);
            Assert.Equal("A3", r.FormatName);
            Assert.Equal(100, r.ScaleDenominator);
            Assert.Equal(100, r.DrawingUnitsPerMm);
            Assert.Equal("297 x 420 mm · 1:100", r.DisplayText);
        }

        [Fact]
        public void A3AtScale100_IsNotMistakenForA1At50()
        {
            // At 1:50 the frame is 594 x 840 which is 1 mm off A1 – must lose against the exact A3 at 1:100.
            PaperResolution r = Resolver().Resolve(42000, 29700);
            Assert.Equal("A3", r.FormatName);
            Assert.Equal(100, r.ScaleDenominator);
        }

        [Fact]
        public void SnapsSmallDrawingInaccuracy()
        {
            PaperResolution r = Resolver().Resolve(209.8, 297.1);
            Assert.Equal("A4", r.FormatName);
            Assert.Equal(210, r.Paper.WidthMm);
            Assert.Equal(297, r.Paper.HeightMm);
        }

        [Fact]
        public void DoesNotRoundUpNoise()
        {
            // The old implementation used Math.Ceiling: 210.01 became 211.
            PaperResolution r = Resolver().Resolve(210.01, 297.01);
            Assert.Equal(210, r.Paper.WidthMm);
            Assert.Equal(297, r.Paper.HeightMm);
        }

        [Fact]
        public void UnknownSize_IsCustomAndRoundedToTenthMm()
        {
            PaperResolution r = Resolver().Resolve(700.04, 350.02);
            Assert.True(r.IsValid);
            Assert.False(r.IsKnownFormat);
            Assert.Null(r.FormatName);
            Assert.Equal(700, r.Paper.WidthMm);
            Assert.Equal(350, r.Paper.HeightMm);
        }

        [Fact]
        public void UnknownSizeAtScale100_UsesStandardShortEdgeEvidence()
        {
            var resolver = new PaperSizeResolver(new PaperResolverOptions()); // no custom formats
            // 1000 x 594 is no known format; at 1:100 the short side is exactly the A2/A1 edge 594.
            PaperResolution r = resolver.Resolve(100000, 59400);
            Assert.Equal(100, r.ScaleDenominator);
            Assert.Equal("1000 x 594 mm · 1:100", r.DisplayText);
        }

        [Fact]
        public void AmbiguousScaledFrame_IsResolvedByUserFormatList()
        {
            // 126000 x 59400 is A4x3 at 1:200 *and* 1260 x 594 at 1:100; the user list decides.
            PaperResolution r = Resolver().Resolve(126000, 59400);
            Assert.Equal(100, r.ScaleDenominator);
            Assert.Equal("1260x594", r.FormatName);
        }

        [Fact]
        public void TinyRectangle_IsInvalid()
        {
            PaperResolution r = Resolver(fixedScale: 1).Resolve(185, 55);
            Assert.False(r.IsValid);
            Assert.NotNull(r.Problem);
        }

        [Fact]
        public void FixedScaleIsRespected()
        {
            PaperResolution r = Resolver(fixedScale: 50).Resolve(10500, 14850);
            Assert.Equal("A4", r.FormatName);
            Assert.Equal(50, r.ScaleDenominator);
        }

        [Fact]
        public void MetreDrawing_ConvertsUnits()
        {
            PaperResolution r = Resolver(mmPerUnit: 1000).Resolve(0.297, 0.210);
            Assert.Equal("A4", r.FormatName);
            Assert.Equal(1, r.ScaleDenominator);
            Assert.Equal(0.001, r.DrawingUnitsPerMm, 9);
        }

        [Fact]
        public void Custom1261_PrefersGostFormat_1260_PrefersUserFormat()
        {
            Assert.Equal("A2x3", Resolver().Resolve(1261, 594).FormatName);
            Assert.Equal("1260x594", Resolver().Resolve(1260, 594).FormatName);
        }

        [Fact]
        public void ParseCustom_AcceptsSeparatorsAndCyrillicX()
        {
            var list = PaperFormatCatalog.ParseCustom("630x297, 891×420;1260 х 594\n700,5*300");
            Assert.Equal(4, list.Count);
            Assert.Equal(700.5, list[3].LongMm);
        }
    }
}
