using AutoPDF.Core.Paper;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;

namespace AutoPDF.Pdf
{
    /// <summary>
    /// Produces the PDF file of one sheet. Implemented by each CAD host with that host's own API
    /// (AutoCAD: Autodesk.AutoCAD.PlottingServices, ZWCAD: ZwSoft.ZwCAD.PlottingServices).
    /// </summary>
    public interface ISheetPlotter
    {
        /// <summary>Short name for logs and the summary, e.g. "API" or "-PLOT".</summary>
        string Method { get; }

        /// <summary>Plots <paramref name="request"/> to <paramref name="outputPath"/>. Throws <see cref="PlotStepException"/>.</summary>
        PlotRunInfo Plot(PlotRequest request, string outputPath, string progressTitle);
    }

    /// <summary>Knows the papers of the host's plotters and chooses one for a sheet.</summary>
    public interface IMediaPlanner
    {
        /// <summary>Device + paper + rotation for a sheet, or null with <paramref name="problem"/>.</summary>
        MediaPlan Plan(PaperSize paper, PluginSettings settings, out string problem);

        /// <summary>The device placed the drawing the other way round: remember the rotation that works.</summary>
        void LearnRotation(MediaPlan plan, bool rotate90ThatWorks);
    }
}
