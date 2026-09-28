using System;
using System.Xml.Serialization;
using AutoPDF.Core.Models;

namespace AutoPDF.Core.Settings
{
    public enum PlotMethod
    {
        /// <summary>AutoCAD plot API (PlotEngine); on failure falls back to the -PLOT command.</summary>
        Auto,
        /// <summary>Only the plot API.</summary>
        ApiOnly,
        /// <summary>Only the -PLOT command (diagnostics).</summary>
        CommandLineOnly
    }

    public enum OverwritePolicy
    {
        Ask,
        Overwrite,
        Rename,
        Skip
    }

    public enum ErrorPolicy
    {
        /// <summary>Log the failed sheet and continue with the next one.</summary>
        Continue,
        /// <summary>Stop the batch at the first failed sheet.</summary>
        Stop
    }

    /// <summary>User settings, persisted as XML in %APPDATA%\AutoPDFPro\settings.xml.</summary>
    [XmlRoot("AutoPdfSettings")]
    public sealed class PluginSettings
    {
        public const string DefaultPlotter = "DWG To PDF.pc3";
        public const string DefaultCustomSizesPlotter = "AutoPDFPro PDF.pc3";
        public const string DefaultStyleTable = "monochrome.ctb";
        public const string DefaultCustomFormats = "630x297; 891x420; 1260x594";

        // ---- Main window -------------------------------------------------------------------
        public string OutputFolder { get; set; } = string.Empty;
        public DetectionMode DetectionMode { get; set; } = DetectionMode.Sample;
        public string BlockNames { get; set; } = string.Empty;
        public SortDirection SortDirection { get; set; } = SortDirection.LeftToRight;
        public int StartNumber { get; set; } = 1;

        // ---- Plotting ----------------------------------------------------------------------
        public string Plotter { get; set; } = DefaultPlotter;

        /// <summary>
        /// Optional second PC3 (a copy of DWG To PDF.pc3 with user-defined paper sizes). Used only for
        /// sheets whose exact size the main plotter does not have. Ignored when not installed.
        /// </summary>
        public string CustomSizesPlotter { get; set; } = DefaultCustomSizesPlotter;

        public string PlotStyleTable { get; set; } = DefaultStyleTable;

        /// <summary>Apply <see cref="PlotStyleTable"/> (monochrome.ctb by default).</summary>
        public bool UseMonochrome { get; set; } = true;

        public bool PlotWithPlotStyles { get; set; } = true;
        public bool PlotLineweights { get; set; } = true;
        public bool ScaleLineweights { get; set; }
        public bool CenterPlot { get; set; } = true;

        /// <summary>N of 1:N; 0 = detect automatically.</summary>
        public double FixedScale { get; set; }

        /// <summary>
        /// When the plotter has no paper of the exact size: plot on the smallest larger full-bleed sheet
        /// (centred, exact scale) and crop the PDF page to the exact size.
        /// </summary>
        public bool AllowCropForNonStandard { get; set; } = true;

        public PlotMethod PlotMethod { get; set; } = PlotMethod.Auto;

        // ---- Detection ---------------------------------------------------------------------
        public double SnapToleranceMm { get; set; } = 0.5;
        public double MinSheetSizeMm { get; set; } = 100;
        public string CustomFormats { get; set; } = DefaultCustomFormats;
        public bool SampleMatchLayer { get; set; } = true;
        public bool SampleMatchSize { get; set; }

        // ---- Export ------------------------------------------------------------------------
        public string FileNamePattern { get; set; } = "{N}";
        public OverwritePolicy OverwritePolicy { get; set; } = OverwritePolicy.Ask;
        public ErrorPolicy ErrorPolicy { get; set; } = ErrorPolicy.Continue;

        /// <summary>Show a preview after each PDF until the user chooses "confirm and process the rest".</summary>
        public bool PreviewEachSheet { get; set; } = true;

        public bool OpenFolderWhenDone { get; set; }

        /// <summary>Allowed difference (mm) between the produced PDF page and the expected paper.</summary>
        public double PageSizeToleranceMm { get; set; } = 1.5;

        // ---- Diagnostics -------------------------------------------------------------------
        public bool VerboseLog { get; set; } = true;

        /// <summary>Clamp values and fill blanks so a hand-edited or old file can never break the plugin.</summary>
        public PluginSettings Normalize()
        {
            if (OutputFolder == null) OutputFolder = string.Empty;
            if (BlockNames == null) BlockNames = string.Empty;
            if (StartNumber < 0) StartNumber = 1;
            if (StartNumber > 99999) StartNumber = 99999;
            if (string.IsNullOrWhiteSpace(Plotter)) Plotter = DefaultPlotter;
            if (CustomSizesPlotter == null) CustomSizesPlotter = string.Empty;
            if (string.IsNullOrWhiteSpace(PlotStyleTable)) PlotStyleTable = DefaultStyleTable;
            if (!(FixedScale >= 0) || FixedScale > 100000) FixedScale = 0;
            SnapToleranceMm = Clamp(SnapToleranceMm, 0.05, 10, 0.5);
            MinSheetSizeMm = Clamp(MinSheetSizeMm, 10, 1000, 100);
            PageSizeToleranceMm = Clamp(PageSizeToleranceMm, 0.1, 20, 1.5);
            if (CustomFormats == null) CustomFormats = string.Empty;
            if (string.IsNullOrWhiteSpace(FileNamePattern)) FileNamePattern = "{N}";
            if (!Enum.IsDefined(typeof(DetectionMode), DetectionMode)) DetectionMode = DetectionMode.Sample;
            if (!Enum.IsDefined(typeof(SortDirection), SortDirection)) SortDirection = SortDirection.LeftToRight;
            if (!Enum.IsDefined(typeof(PlotMethod), PlotMethod)) PlotMethod = PlotMethod.Auto;
            if (!Enum.IsDefined(typeof(OverwritePolicy), OverwritePolicy)) OverwritePolicy = OverwritePolicy.Ask;
            if (!Enum.IsDefined(typeof(ErrorPolicy), ErrorPolicy)) ErrorPolicy = ErrorPolicy.Continue;
            return this;
        }

        public PluginSettings Clone() => (PluginSettings)MemberwiseClone();

        private static double Clamp(double value, double min, double max, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return fallback;
            return value < min ? min : value > max ? max : value;
        }
    }
}
