using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Settings;
using AutoPDF.UI.Main;
using AutoPDF.UI.Preview;
using AutoPDF.UI.Services;
using AutoPDF.UI.Settings;
using AutoPDF.UI.Summary;

namespace AutoPDF.UiDemo
{
    /// <summary>
    /// Shows the AutoPDF Pro windows with sample data, without AutoCAD:
    ///   AutoPDF.UiDemo.exe                → main window (+ settings)
    ///   AutoPDF.UiDemo.exe some.pdf       → preview window for that PDF, then the summary window
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            if (args.Length > 0 && File.Exists(args[0]))
            {
                ShowPreview(args[0]);
            }
            else
            {
                ShowMain();
            }

            app.Shutdown();
        }

        private static void ShowMain()
        {
            var settings = new PluginSettings { OutputFolder = Path.Combine(Path.GetTempPath(), "AutoPDF-demo") };
            var resolver = new PaperSizeResolver(new PaperResolverOptions { CustomFormats = PaperFormatCatalog.ParseCustom(settings.CustomFormats) });
            var sheets = new List<SheetModel>();
            double x = 0;
            var sizes = new[] { (210.0, 297.0), (297.0, 420.0), (420.0, 297.0), (630.0, 297.0), (891.0, 420.0), (1260.0, 594.0), (594.0, 841.0) };
            int i = 0;
            foreach (var (w, h) in sizes)
            {
                sheets.Add(new SheetModel
                {
                    Handle = (0x2A0 + i).ToString("X"),
                    BlockName = i % 3 == 2 ? "Ramka_GOST" : SheetModel.RectangleName,
                    Layer = "Ramka",
                    Bounds = new Bounds2d(x, 0, x + w, h),
                    Paper = resolver.Resolve(w, h),
                    Status = w > 600 && h < 600 ? SheetStatus.ReadyWithCrop : SheetStatus.Ready
                });
                x += w + 50;
                i++;
            }

            while (true)
            {
                var vm = new MainViewModel(settings, sheets, "v2.0.0 (demo)", "Demo rejimi — AutoCAD siz.");
                var window = new MainWindow(vm);
                vm.OpenFolderRequested += f => Shell.OpenFolder(f, out _);
                vm.OpenSettingsRequested += () =>
                {
                    var svm = new SettingsViewModel(settings, new[] { "DWG To PDF.pc3", "AutoCAD PDF (High Quality Print).pc3" },
                        new[] { "monochrome.ctb", "acad.ctb", "Grayscale.ctb" }, Path.GetTempPath());
                    new SettingsWindow(svm) { Owner = window }.ShowDialog();
                };
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.ShowDialog();

                if (vm.Action == MainAction.Close) return;
                UzDialog.Show(IntPtr.Zero, "Tanlangan amal: " + vm.Action + "\n(AutoCAD ichida bu yerda chizma bilan ishlanadi.)");
                sheets = new List<SheetModel>(vm.SheetModels);
            }
        }

        private static void ShowPreview(string pdf)
        {
            var vm = new PreviewViewModel(pdf)
            {
                HeaderText = "List 1 / 3 — " + Path.GetFileName(pdf),
                PaperText = "210 x 297 mm (A4)",
                PageText = "210 x 297 mm",
                FileSizeText = PreviewViewModel.FormatBytes(new FileInfo(pdf).Length),
                MethodText = "AutoCAD plot API",
                PlanText = "DWG To PDF.pc3 · ISO full bleed A4 (210.00 x 297.00 MM) · 0°",
                Checks = new List<string> { "✓ Fayl yaratildi va bo‘sh emas", "✓ Sahifa o‘lchami to‘g‘ri: 210 x 297 mm" },
                HasRemaining = true
            };
            vm.LoadImage();
            new PreviewWindow(vm) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();

            var summary = new ExportSummary { OutputFolder = Path.GetDirectoryName(Path.GetFullPath(pdf)), LogFile = "", Started = DateTime.Now, Finished = DateTime.Now };
            summary.Results.Add(new SheetResult { Number = 1, OutputPath = pdf, Outcome = SheetOutcome.Success });
            summary.Results.Add(new SheetResult { Number = 2, OutputPath = "02.pdf", Outcome = SheetOutcome.Failed, FailedStep = "plot:set-media", Error = "eInvalidInput — demo" });
            var warn = new SheetResult { Number = 3, OutputPath = "03.pdf", Outcome = SheetOutcome.SuccessWithWarning };
            warn.Warnings.Add("Plot API xatosi — -PLOT bilan yaratildi (demo).");
            summary.Results.Add(warn);
            new SummaryWindow(summary) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();
        }
    }
}
