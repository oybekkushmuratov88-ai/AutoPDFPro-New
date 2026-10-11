using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace AutoPDF.Core.Tests
{
    /// <summary>
    /// The shared PDF pipeline (the same in AutoCAD and ZWCAD) driven by a fake plotter that writes real PDF
    /// files: page size, orientation (incl. the one-time re-plot), plot window, scale, centring, cropping of
    /// non-standard sizes, the output folder, the API → -PLOT fallback rules and cancellation.
    /// </summary>
    public sealed class PdfPipelineTests : IDisposable
    {
        private static readonly List<MediaInfo> DwgToPdf = new List<MediaInfo>
        {
            new MediaInfo("ISO_full_bleed_A4_(210.00_x_297.00_MM)", null, 210, 297),
            new MediaInfo("ISO_full_bleed_A4_(297.00_x_210.00_MM)", null, 297, 210),
            new MediaInfo("ISO_full_bleed_A3_(297.00_x_420.00_MM)", null, 297, 420),
            new MediaInfo("ISO_full_bleed_A3_(420.00_x_297.00_MM)", null, 420, 297),
            new MediaInfo("ISO_full_bleed_A2_(594.00_x_420.00_MM)", null, 594, 420),
            new MediaInfo("ISO_full_bleed_A1_(841.00_x_594.00_MM)", null, 841, 594),
            new MediaInfo("ISO_full_bleed_A0_(1189.00_x_841.00_MM)", null, 1189, 841),
        };

        private readonly string _dir;
        private readonly LoggingService _log;

        static PdfPipelineTests()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public PdfPipelineTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "autopdf-pipeline-" + Guid.NewGuid().ToString("N"));
            _log = new LoggingService(Path.Combine(_dir, "logs"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { /* temp */ }
        }

        [Theory]
        [InlineData(420, 297, 100)] // A3 landscape, 1:100
        [InlineData(210, 297, 50)]  // A4 portrait, 1:50
        [InlineData(1189, 841, 1)]  // A0 landscape, 1:1
        public void StandardSheet_ExactPageSizeOrientationWindowAndScale(double w, double h, double unitsPerMm)
        {
            var plotter = new FakePlotter();
            SheetModel sheet = Sheet(1, w, h, unitsPerMm, originX: 5000, originY: -300);
            string output = Path.Combine(_dir, "out", "nested", "01.pdf"); // folder does not exist yet

            SheetResult result = Pipeline(plotter).Export(sheet, output, 0, 1);

            Assert.Equal(SheetOutcome.Success, result.Outcome);
            Assert.True(File.Exists(output));
            Assert.Equal("API", result.Method);
            AssertPage(new PaperSize(w, h), result.PageSize.Value);
            AssertPage(new PaperSize(w, h), ReadPage(output));
            Assert.True(new PaperSize(w, h).IsLandscape == ReadPage(output).IsLandscape);

            PlotRequest req = Assert.Single(plotter.Requests);
            Assert.Equal(sheet.Bounds.MinX, req.WindowWcs.MinX);
            Assert.Equal(sheet.Bounds.MaxY, req.WindowWcs.MaxY);
            Assert.Equal(unitsPerMm, req.DrawingUnitsPerMm);
            Assert.True(req.Center);
            Assert.Equal(MediaFit.Exact, req.Plan.Fit);
            Assert.Equal(output, plotter.Paths.Single());
        }

        [Fact]
        public void PageComesOutTurned_ReplotsOnceWithOtherRotation_AndRemembersIt()
        {
            var planner = new FakePlanner();
            bool? firstRotation = null;
            var plotter = new FakePlotter
            {
                // This "device" places the drawing the other way round with the first rotation it is given.
                PageFor = req =>
                {
                    firstRotation = firstRotation ?? req.Plan.Rotate90;
                    return req.Plan.Rotate90 == firstRotation ? req.Plan.ExpectedPage.Rotated() : req.Plan.ExpectedPage;
                }
            };
            string output = Path.Combine(_dir, "turned.pdf");

            SheetResult result = Pipeline(plotter, planner).Export(Sheet(3, 420, 297, 100), output, 0, 1);

            Assert.Equal(SheetOutcome.Success, result.Outcome);
            Assert.Equal(2, plotter.Requests.Count);
            Assert.NotEqual(plotter.Requests[0].Plan.Rotate90, plotter.Requests[1].Plan.Rotate90);
            Assert.Equal(plotter.Requests[1].Plan.Rotate90, planner.Learned.Single().Value);
            AssertPage(new PaperSize(420, 297), ReadPage(output));
        }

        [Fact]
        public void PageTurnedEvenAfterRetry_FailsAtVerifyPage()
        {
            var plotter = new FakePlotter { PageFor = req => req.Plan.ExpectedPage.Rotated() };

            SheetResult result = Pipeline(plotter).Export(Sheet(4, 420, 297, 100), Path.Combine(_dir, "x.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("verify:page", result.FailedStep);
            Assert.Equal(2, plotter.Requests.Count);
        }

        [Fact]
        public void NonStandardSize_IsPlottedOnLargerPaperAndCroppedToExactSize()
        {
            var plotter = new FakePlotter();
            string output = Path.Combine(_dir, "a4x3.pdf");

            SheetResult result = Pipeline(plotter).Export(Sheet(5, 630, 297, 100), output, 0, 1);

            Assert.Equal(SheetOutcome.Success, result.Outcome);
            Assert.Equal(MediaFit.Crop, plotter.Requests.Single().Plan.Fit);
            Assert.True(plotter.Requests.Single().Center); // the crop assumes a centred plot
            AssertPage(new PaperSize(630, 297), ReadPage(output));
            AssertPage(new PaperSize(630, 297), result.PageSize.Value);
        }

        [Fact]
        public void WrongPaper_FailsAtVerifyPage()
        {
            var plotter = new FakePlotter { PageFor = req => new PaperSize(210, 297) };

            SheetResult result = Pipeline(plotter).Export(Sheet(6, 420, 297, 100), Path.Combine(_dir, "y.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("verify:page", result.FailedStep);
        }

        [Fact]
        public void ApiFailure_FallsBackToCommandLine_WhenAuto()
        {
            var api = new FakePlotter { Fail = new PlotStepException("plot:set-window-area", "eInvalidInput") };
            var cmd = new FakePlotter { Method = "-PLOT" };
            string output = Path.Combine(_dir, "fallback.pdf");

            SheetResult result = Pipeline(api, fallback: cmd).Export(Sheet(7, 297, 210, 1), output, 0, 1);

            Assert.Equal(SheetOutcome.SuccessWithWarning, result.Outcome);
            Assert.Equal("-PLOT", result.Method);
            Assert.Contains(result.Warnings, w => w.Contains("plot:set-window-area"));
            Assert.Single(cmd.Requests);
        }

        [Fact]
        public void ApiFailure_IsReported_WhenApiOnly()
        {
            var api = new FakePlotter { Fail = new PlotStepException("plot:validate", "eInvalidInput") };
            var cmd = new FakePlotter { Method = "-PLOT" };

            SheetResult result = Pipeline(api, fallback: cmd, method: PlotMethod.ApiOnly).Export(Sheet(8, 297, 210, 1), Path.Combine(_dir, "z.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("plot:validate", result.FailedStep);
            Assert.Empty(cmd.Requests);
        }

        [Fact]
        public void HostWithoutCommandLinePlotter_UsesApiEvenWhenCommandLineIsChosen()
        {
            // ZWCAD: no -PLOT fallback (its prompts differ); the API is used whatever the setting says.
            var api = new FakePlotter();
            SheetResult result = Pipeline(api, fallback: null, method: PlotMethod.CommandLineOnly)
                .Export(Sheet(9, 210, 297, 1), Path.Combine(_dir, "zw.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Success, result.Outcome);
            Assert.Equal("API", result.Method);
        }

        [Fact]
        public void ApiFailure_WithoutFallback_IsReported()
        {
            var api = new FakePlotter { Fail = new PlotStepException("plot:engine-begin-document", "eNotApplicable") };
            SheetResult result = Pipeline(api, fallback: null).Export(Sheet(10, 210, 297, 1), Path.Combine(_dir, "f.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("plot:engine-begin-document", result.FailedStep);
        }

        [Fact]
        public void CancelInProgressDialog_IsCancelledNotFailed_AndNeverFallsBack()
        {
            var api = new FakePlotter { Fail = new PlotStepException(PlotSteps.Cancelled, "cancelled") };
            var cmd = new FakePlotter { Method = "-PLOT" };

            SheetResult result = Pipeline(api, fallback: cmd).Export(Sheet(11, 210, 297, 1), Path.Combine(_dir, "c.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Cancelled, result.Outcome);
            Assert.True(result.UserCancelled);
            Assert.Empty(cmd.Requests);
        }

        [Fact]
        public void EmptyPlot_IsSuccessWithWarning()
        {
            var api = new FakePlotter { EntityCount = 0 };
            SheetResult result = Pipeline(api).Export(Sheet(12, 210, 297, 1), Path.Combine(_dir, "e.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.SuccessWithWarning, result.Outcome);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void ExistingFile_IsReplaced()
        {
            string output = Path.Combine(_dir, "existing.pdf");
            Directory.CreateDirectory(_dir);
            File.WriteAllText(output, "old");

            SheetResult result = Pipeline(new FakePlotter()).Export(Sheet(13, 210, 297, 1), output, 0, 1);

            Assert.Equal(SheetOutcome.Success, result.Outcome);
            AssertPage(new PaperSize(210, 297), ReadPage(output));
        }

        [Fact]
        public void InvalidPaper_FailsBeforePlotting()
        {
            var plotter = new FakePlotter();
            SheetModel sheet = Sheet(14, 210, 297, 1);
            sheet.Paper.IsValid = false;
            sheet.Paper.Problem = "unknown size";

            SheetResult result = Pipeline(plotter).Export(sheet, Path.Combine(_dir, "p.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("prepare:paper", result.FailedStep);
            Assert.Empty(plotter.Requests);
        }

        [Fact]
        public void MissingFile_FailsAtVerifyFile()
        {
            var plotter = new FakePlotter { WriteFile = false };
            var pipeline = Pipeline(plotter);
            pipeline.FileWaitPolls = 0;

            SheetResult result = pipeline.Export(Sheet(15, 210, 297, 1), Path.Combine(_dir, "none.pdf"), 0, 1);

            Assert.Equal(SheetOutcome.Failed, result.Outcome);
            Assert.Equal("verify:file", result.FailedStep);
        }

        // ---- helpers --------------------------------------------------------------------------------

        private SheetPdfPipeline Pipeline(FakePlotter primary, FakePlanner planner = null, FakePlotter fallback = null, PlotMethod method = PlotMethod.Auto)
        {
            var settings = new PluginSettings { PlotMethod = method, CenterPlot = true, AllowCropForNonStandard = true };
            return new SheetPdfPipeline(settings, _log, planner ?? new FakePlanner(), primary, fallback, "monochrome.ctb");
        }

        private static SheetModel Sheet(int number, double wMm, double hMm, double unitsPerMm, double originX = 0, double originY = 0) =>
            new SheetModel
            {
                Number = number,
                Bounds = new Bounds2d(originX, originY, originX + wMm * unitsPerMm, originY + hMm * unitsPerMm),
                Paper = new PaperResolution
                {
                    Paper = new PaperSize(wMm, hMm),
                    DrawingUnitsPerMm = unitsPerMm,
                    IsValid = true
                }
            };

        private static PaperSize ReadPage(string path) => PdfPageService.ReadFirstPage(path, out _).DisplayedSizeMm;

        private static void AssertPage(PaperSize expected, PaperSize actual) =>
            Assert.True(actual.ApproximatelyEquals(expected, 0.5), "expected " + expected + ", got " + actual);

        private sealed class FakePlanner : IMediaPlanner
        {
            public Dictionary<string, bool> Learned { get; } = new Dictionary<string, bool>();

            public MediaPlan Plan(PaperSize paper, PluginSettings settings, out string problem)
            {
                MediaPlan plan = MediaSelector.Select("DWG To PDF.pc3", DwgToPdf, paper,
                    new MediaSelectionOptions { AllowCrop = settings.AllowCropForNonStandard });
                problem = plan == null ? "no paper" : null;
                if (plan != null && Learned.TryGetValue(plan.Media.CanonicalName, out bool r)) plan = plan.WithRotation(r);
                return plan;
            }

            public void LearnRotation(MediaPlan plan, bool rotate90ThatWorks) => Learned[plan.Media.CanonicalName] = rotate90ThatWorks;
        }

        /// <summary>Writes a one-page PDF of the page size a real PDF driver would produce.</summary>
        private sealed class FakePlotter : ISheetPlotter
        {
            public string Method { get; set; } = "API";
            public Func<PlotRequest, PaperSize> PageFor { get; set; } = req => req.Plan.ExpectedPage;
            public PlotStepException Fail { get; set; }
            public int EntityCount { get; set; } = 42;
            public bool WriteFile { get; set; } = true;
            public List<PlotRequest> Requests { get; } = new List<PlotRequest>();
            public List<string> Paths { get; } = new List<string>();

            public PlotRunInfo Plot(PlotRequest request, string outputPath, string progressTitle)
            {
                Requests.Add(request);
                Paths.Add(outputPath);
                if (Fail != null) throw Fail;
                if (WriteFile)
                {
                    PaperSize page = PageFor(request);
                    using (var doc = new PdfDocument())
                    {
                        PdfPage p = doc.AddPage();
                        p.Width = XUnit.FromMillimeter(page.WidthMm);
                        p.Height = XUnit.FromMillimeter(page.HeightMm);
                        doc.Save(outputPath);
                    }
                }
                return new PlotRunInfo { EntityCount = EntityCount };
            }
        }
    }
}
