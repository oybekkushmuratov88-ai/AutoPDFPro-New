using System.Collections.Generic;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Paper;

namespace AutoPDF.Core.Models
{
    public enum DetectionMode
    {
        /// <summary>User selects the frames (rectangles or blocks) directly.</summary>
        Manual,

        /// <summary>All block references whose effective name matches the configured list.</summary>
        BlockName,

        /// <summary>User picks one frame; all similar frames in model space are found.</summary>
        Sample,

        /// <summary>User picks one frame, then selects an area; similar frames inside it are found.</summary>
        SampleRange
    }

    public enum SortDirection
    {
        /// <summary>Row by row (top row first), each row left → right.</summary>
        LeftToRight,

        /// <summary>Column by column (left column first), each column top → bottom.</summary>
        TopToBottom
    }

    public enum SheetStatus
    {
        Ready,
        ReadyWithCrop,
        Warning,
        Error,
        Waiting,
        Plotting,
        Done,
        DoneWithWarning,
        Failed,
        Skipped,
        Cancelled
    }

    public enum SheetSourceKind
    {
        Rectangle,
        Block
    }

    /// <summary>One detected sheet frame.</summary>
    public sealed class SheetModel
    {
        public const string RectangleName = "Rectangle";

        /// <summary>AutoCAD handle of the frame entity (hex string).</summary>
        public string Handle { get; set; }

        public SheetSourceKind SourceKind { get; set; }

        /// <summary>"Rectangle" for polylines, the effective block name for blocks.</summary>
        public string BlockName { get; set; } = RectangleName;

        public string Layer { get; set; }

        /// <summary>Frame extents in WCS drawing units.</summary>
        public Bounds2d Bounds { get; set; }

        public double Width => Bounds.Width;
        public double Height => Bounds.Height;

        public PaperResolution Paper { get; set; }

        public int Number { get; set; }

        public SheetStatus Status { get; set; } = SheetStatus.Ready;

        /// <summary>Short explanation shown next to the status (error text, media used …).</summary>
        public string StatusDetail { get; set; }

        /// <summary>Media / plotter plan description, e.g. "DWG To PDF.pc3 · ISO_full_bleed_A4_(210.00_x_297.00_MM)".</summary>
        public string PlanDescription { get; set; }

        public string OutputPath { get; set; }

        public List<string> Warnings { get; } = new List<string>();

        public override string ToString() =>
            "#" + Number + " " + BlockName + " " + Bounds + " → " + (Paper != null ? Paper.DisplayText : "?");
    }
}
