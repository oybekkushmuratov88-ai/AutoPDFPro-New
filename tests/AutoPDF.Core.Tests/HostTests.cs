using AutoPDF.Core.Hosting;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public class HostTests
    {
        [Theory]
        [InlineData(24, 0, 2021)]
        [InlineData(24, 1, 2022)]
        [InlineData(24, 2, 2023)]
        [InlineData(24, 3, 2024)]
        [InlineData(25, 0, 2025)]
        [InlineData(25, 1, 2026)]
        [InlineData(23, 1, 0)]
        [InlineData(26, 0, 0)]
        public void AutoCadReleaseToYear(int major, int minor, int year) =>
            Assert.Equal(year, HostCatalog.AutoCadYear(major, minor));

        [Theory]
        [InlineData(24, 2024)]
        [InlineData(25, 2025)]
        [InlineData(26, 2026)]
        [InlineData(1, 0)]
        public void ZwcadApiVersionToYear(int major, int year) => Assert.Equal(year, HostCatalog.ZwcadYear(major));

        [Theory]
        [InlineData("24.3s (LMS Tech)", 24, 3)]
        [InlineData("25.0s (LMS Tech)", 25, 0)]
        [InlineData("R25.1", 25, 1)]
        public void ParsesAcadver(string text, int major, int minor)
        {
            Assert.True(HostCatalog.TryParseRelease(text, out int ma, out int mi));
            Assert.Equal(major, ma);
            Assert.Equal(minor, mi);
        }

        [Fact]
        public void UnparsableAcadver_IsRejected() => Assert.False(HostCatalog.TryParseRelease("unknown", out _, out _));

        private static readonly CadBuildTarget R24 = new CadBuildTarget(CadProduct.AutoCAD, 2021, 2024, "R24");
        private static readonly CadBuildTarget R25 = new CadBuildTarget(CadProduct.AutoCAD, 2025, 2026, "R25");
        private static readonly CadBuildTarget Zw2025 = new CadBuildTarget(CadProduct.ZWCAD, 2025, 2025, "ZW2025");

        [Theory]
        [InlineData(CadProduct.AutoCAD, 2021, "R24", HostCompatibility.Supported)]
        [InlineData(CadProduct.AutoCAD, 2024, "R24", HostCompatibility.Supported)]
        [InlineData(CadProduct.AutoCAD, 2025, "R24", HostCompatibility.WrongBuild)]
        [InlineData(CadProduct.AutoCAD, 2026, "R25", HostCompatibility.Supported)]
        [InlineData(CadProduct.AutoCAD, 2024, "R25", HostCompatibility.WrongBuild)]
        [InlineData(CadProduct.ZWCAD, 2025, "ZW2025", HostCompatibility.Supported)]
        [InlineData(CadProduct.ZWCAD, 2024, "ZW2025", HostCompatibility.WrongBuild)]
        [InlineData(CadProduct.AutoCAD, 2025, "ZW2025", HostCompatibility.WrongBuild)]
        [InlineData(CadProduct.ZWCAD, 0, "ZW2025", HostCompatibility.Unverified)]
        public void BuildMustMatchHost(CadProduct product, int year, string build, HostCompatibility expected)
        {
            CadBuildTarget target = build == "R24" ? R24 : build == "R25" ? R25 : Zw2025;
            Assert.Equal(expected, HostCatalog.Check(new HostInfo { Product = product, Year = year }, target));
        }

        [Fact]
        public void DisplayName() =>
            Assert.Equal("ZWCAD 2026", new HostInfo { Product = CadProduct.ZWCAD, Year = 2026 }.DisplayName);
    }
}
