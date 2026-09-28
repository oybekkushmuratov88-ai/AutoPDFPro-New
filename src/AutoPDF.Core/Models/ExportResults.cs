using System;
using System.Collections.Generic;
using System.Linq;
using AutoPDF.Core.Paper;

namespace AutoPDF.Core.Models
{
    public enum SheetOutcome
    {
        Success,
        SuccessWithWarning,
        Failed,
        Skipped,
        Cancelled
    }

    /// <summary>Result of exporting one sheet.</summary>
    public sealed class SheetResult
    {
        public int Number { get; set; }
        public string OutputPath { get; set; }
        public SheetOutcome Outcome { get; set; }

        /// <summary>The operation that failed, e.g. "plot:set-window-area" or "verify:page-size".</summary>
        public string FailedStep { get; set; }

        public string Error { get; set; }
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>"API" or "-PLOT".</summary>
        public string Method { get; set; }

        public string PlanDescription { get; set; }
        public long FileBytes { get; set; }
        public PaperSize? PageSize { get; set; }
        public bool UserCancelled { get; set; }

        public bool IsSuccess => Outcome == SheetOutcome.Success || Outcome == SheetOutcome.SuccessWithWarning;
    }

    public sealed class ExportSummary
    {
        public string OutputFolder { get; set; }
        public string LogFile { get; set; }
        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }
        public List<SheetResult> Results { get; } = new List<SheetResult>();

        public int Total => Results.Count;
        public int Succeeded => Results.Count(r => r.IsSuccess);
        public int WithWarnings => Results.Count(r => r.Outcome == SheetOutcome.SuccessWithWarning);
        public int Failed => Results.Count(r => r.Outcome == SheetOutcome.Failed);
        public int Skipped => Results.Count(r => r.Outcome == SheetOutcome.Skipped);
        public int Cancelled => Results.Count(r => r.Outcome == SheetOutcome.Cancelled);
    }
}
