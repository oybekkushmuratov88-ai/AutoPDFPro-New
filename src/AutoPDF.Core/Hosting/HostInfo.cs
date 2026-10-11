using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoPDF.Core.Hosting
{
    public enum CadProduct
    {
        Unknown,
        AutoCAD,
        ZWCAD
    }

    /// <summary>The CAD application and version the plug-in is running in (detected at run time).</summary>
    public sealed class HostInfo
    {
        public CadProduct Product { get; set; }

        /// <summary>Marketing year (2021 … 2026), 0 when unknown.</summary>
        public int Year { get; set; }

        /// <summary>Internal release as reported by the host, e.g. "24.3" (AutoCAD R24.3) or "25.0" (ZWCAD 2025 API).</summary>
        public string Release { get; set; }

        /// <summary>The CAD executable, e.g. "acad" or "ZWCAD".</summary>
        public string ProcessName { get; set; }

        /// <summary>The .NET runtime the host runs on, e.g. ".NET Framework 4.8.9181.0" or ".NET 8.0.11".</summary>
        public string Runtime { get; set; }

        public string DisplayName =>
            (Product == CadProduct.Unknown ? "CAD" : Product.ToString()) + (Year > 0 ? " " + Year.ToString(CultureInfo.InvariantCulture) : string.Empty);

        public override string ToString() =>
            DisplayName + " (" + (string.IsNullOrEmpty(Release) ? "?" : Release) + ", " + (ProcessName ?? "?") + ", " + (Runtime ?? "?") + ")";
    }

    /// <summary>What one plug-in assembly was compiled for (set by the host project).</summary>
    public sealed class CadBuildTarget
    {
        public CadBuildTarget(CadProduct product, int minYear, int maxYear, string assemblyLabel)
        {
            Product = product;
            MinYear = minYear;
            MaxYear = maxYear;
            AssemblyLabel = assemblyLabel;
        }

        public CadProduct Product { get; }
        public int MinYear { get; }
        public int MaxYear { get; }

        /// <summary>E.g. "AutoPDF.AutoCAD (R24)".</summary>
        public string AssemblyLabel { get; }

        public string Describe() =>
            Product + " " + (MinYear == MaxYear
                ? MinYear.ToString(CultureInfo.InvariantCulture)
                : MinYear.ToString(CultureInfo.InvariantCulture) + "–" + MaxYear.ToString(CultureInfo.InvariantCulture));
    }

    public enum HostCompatibility
    {
        Supported,

        /// <summary>The version could not be determined; the plug-in runs and says so in the log.</summary>
        Unverified,

        /// <summary>This build belongs to another CAD product or release (wrong folder / wrong registration).</summary>
        WrongBuild
    }

    /// <summary>Maps host version numbers to marketing years and checks a build against the host.</summary>
    public static class HostCatalog
    {
        /// <summary>
        /// AutoCAD release → year. R24.0 = 2021, R24.1 = 2022, R24.2 = 2023, R24.3 = 2024 (binary compatible,
        /// .NET Framework 4.8); R25.0 = 2025, R25.1 = 2026 (.NET 8).
        /// </summary>
        public static int AutoCadYear(int major, int minor)
        {
            if (major == 24 && minor >= 0 && minor <= 3) return 2021 + minor;
            if (major == 25 && minor >= 0 && minor <= 1) return 2025 + minor;
            return 0;
        }

        /// <summary>ZWCAD's managed API assemblies are versioned by the release: 24.x = 2024, 25.x = 2025, 26.x = 2026.</summary>
        public static int ZwcadYear(int assemblyMajor) =>
            assemblyMajor >= 20 && assemblyMajor <= 40 ? 2000 + assemblyMajor : 0;

        /// <summary>Parses AutoCAD's ACADVER ("24.3s (LMS Tech)") into major/minor.</summary>
        public static bool TryParseRelease(string text, out int major, out int minor)
        {
            major = minor = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            Match m = Regex.Match(text, @"(\d+)\.(\d+)");
            return m.Success &&
                   int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out major) &&
                   int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minor);
        }

        public static HostCompatibility Check(HostInfo host, CadBuildTarget build)
        {
            if (host == null || build == null) return HostCompatibility.Unverified;
            if (host.Product != CadProduct.Unknown && host.Product != build.Product) return HostCompatibility.WrongBuild;
            if (host.Year == 0) return HostCompatibility.Unverified;
            return host.Year >= build.MinYear && host.Year <= build.MaxYear ? HostCompatibility.Supported : HostCompatibility.WrongBuild;
        }

        public static string RuntimeDescription()
        {
            try { return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription; }
            catch { return ".NET " + Environment.Version; }
        }
    }
}
