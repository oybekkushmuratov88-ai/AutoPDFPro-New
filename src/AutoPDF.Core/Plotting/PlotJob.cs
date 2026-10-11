using System;
using AutoPDF.Core.Geometry;

namespace AutoPDF.Core.Plotting
{
    /// <summary>
    /// Everything needed to plot one sheet, in CAD-independent terms. Each CAD host (AutoCAD, ZWCAD) turns it
    /// into its own plot settings; the shared PDF pipeline never sees a CAD type.
    /// </summary>
    public sealed class PlotRequest
    {
        public int SheetNumber { get; set; }
        public MediaPlan Plan { get; set; }

        /// <summary>Frame (plot window) in WCS.</summary>
        public Bounds2d WindowWcs { get; set; }

        public double DrawingUnitsPerMm { get; set; } = 1;
        public bool Center { get; set; } = true;

        /// <summary>Resolved plot style table, or null for none.</summary>
        public string StyleSheet { get; set; }

        public bool PlotStyles { get; set; } = true;
        public bool Lineweights { get; set; } = true;
        public bool ScaleLineweights { get; set; }
    }

    /// <summary>What a plotter reports about one plot.</summary>
    public sealed class PlotRunInfo
    {
        public int EntityCount { get; set; } = -1;
        public bool UserCancelled { get; set; }
        public string ValidatedMedia { get; set; }
    }

    /// <summary>An operation failed; <see cref="Step"/> names it exactly (e.g. "plot:set-window-area").</summary>
    public sealed class PlotStepException : Exception
    {
        public PlotStepException(string step, string message, Exception inner = null)
            : base(message, inner)
        {
            Step = step;
        }

        public string Step { get; }
    }

    public static class PlotSteps
    {
        /// <summary>Step name used when the user pressed Cancel in the plot progress dialog.</summary>
        public const string Cancelled = "plot:cancelled-by-user";
    }
}
