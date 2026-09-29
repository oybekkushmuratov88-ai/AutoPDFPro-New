using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using AutoPDF.Core.Geometry;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Registration;
using AutoPDF.Core.Settings;
using AutoPDF.UI.Main;
using AutoPDF.UI.Preview;
using AutoPDF.UI.Registration;
using AutoPDF.UI.Services;
using AutoPDF.UI.Settings;
using AutoPDF.UI.Summary;

namespace AutoPDF.UiDemo
{
    /// <summary>
    /// Shows the AutoPDF Pro windows with sample data, without AutoCAD:
    ///   AutoPDF.UiDemo.exe                  → main window (+ settings, about)
    ///   AutoPDF.UiDemo.exe --lang ru        → same in Russian (uz / ru / en)
    ///   AutoPDF.UiDemo.exe some.pdf         → preview window for that PDF, then the summary window
    ///   AutoPDF.UiDemo.exe --demo register  → the first-run registration window (add --fail to test offline)
    ///   AutoPDF.UiDemo.exe --demo users     → the admin user list (any key works in the demo)
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var settings = new PluginSettings { OutputFolder = Path.Combine(Path.GetTempPath(), "AutoPDF-demo") };

            int langIndex = Array.FindIndex(args, a => string.Equals(a, "--lang", StringComparison.OrdinalIgnoreCase));
            if (langIndex >= 0 && langIndex + 1 < args.Length)
            {
                switch (args[langIndex + 1].ToLowerInvariant())
                {
                    case "ru": settings.Language = UiLanguage.Russian; break;
                    case "en": settings.Language = UiLanguage.English; break;
                    default: settings.Language = UiLanguage.Uzbek; break;
                }
            }
            L.Current = settings.Language;

            int demoIndex = Array.FindIndex(args, a => string.Equals(a, "--demo", StringComparison.OrdinalIgnoreCase));
            string demo = demoIndex >= 0 && demoIndex + 1 < args.Length ? args[demoIndex + 1].ToLowerInvariant() : null;
            string pdf = args.FirstOrDefault(a => a.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(a));

            if (demo == "register") ShowRegister(args.Any(a => string.Equals(a, "--fail", StringComparison.OrdinalIgnoreCase)));
            else if (demo == "users") ShowUsers();
            else if (pdf != null) ShowPreview(pdf);
            else ShowMain(settings);

            app.Shutdown();
        }

        private static void ShowRegister(bool simulateOffline)
        {
            var vm = new RegistrationViewModel("2.2.0 (demo)")
            {
                Submit = _ =>
                {
                    Thread.Sleep(900); // show the busy state
                    return simulateOffline
                        ? new SubmitResult { Ok = false, NetworkProblem = true, Error = "demo offline" }
                        : new SubmitResult { Ok = true };
                },
                Persist = (_, __) => { }
            };
            new RegistrationWindow(vm) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();
            MessageDialog.Show(IntPtr.Zero, "Outcome: " + vm.Outcome);
        }

        private static void ShowUsers()
        {
            var vm = new UsersViewModel(new DemoGateway(), new AdminKeyStore(Path.Combine(Path.GetTempPath(), "autopdf-demo-admin.txt")));
            new UsersWindow(vm) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();
        }

        /// <summary>Returns sample users for the admin-window demo (any key is accepted).</summary>
        private sealed class DemoGateway : IRegistrationGateway
        {
            public SubmitResult Submit(RegistrationInfo info) => new SubmitResult { Ok = true };

            public UsersResult FetchUsers(string adminKey)
            {
                Thread.Sleep(500);
                return new UsersResult
                {
                    Ok = true,
                    Users = new List<RegisteredUser>
                    {
                        new RegisteredUser { Number = 1, Date = "2026-09-29 09:12", FullName = "Kushmuratov Oybek", Email = "oybek@example.com", Phone = "+998 90 111 22 33", Computer = "OYBEK-PC", WindowsUser = "oybek", Version = "2.1.0" },
                        new RegisteredUser { Number = 2, Date = "2026-09-29 10:03", FullName = "Ali Valiyev", Email = "ali@example.uz", Phone = "", Computer = "WS-ARCH-02", WindowsUser = "ali", Version = "2.1.0" },
                        new RegisteredUser { Number = 3, Date = "2026-09-30 14:40", FullName = "Nigora Karimova", Email = "nigora@example.uz", Phone = "+998 91 555 66 77", Computer = "DESIGN-05", WindowsUser = "nigora", Version = "2.1.0" }
                    }
                };
            }
        }

        private static void ShowMain(PluginSettings settings)
        {
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

            string status = "Demo — AutoCAD siz / без AutoCAD / without AutoCAD.";
            while (true)
            {
                var vm = new MainViewModel(settings, sheets, "v2.2.0 (demo)", status);
                var window = new MainWindow(vm);
                vm.OpenFolderRequested += f => Shell.OpenFolder(f, out _);
                vm.OpenSettingsRequested += () =>
                {
                    var svm = new SettingsViewModel(settings, new[] { "DWG To PDF.pc3", "AutoCAD PDF (High Quality Print).pc3" },
                        new[] { "monochrome.ctb", "acad.ctb", "Grayscale.ctb" }, Path.GetTempPath());
                    new SettingsWindow(svm) { Owner = window }.ShowDialog();
                    if (svm.LanguageChanged)
                    {
                        L.Current = settings.Language;
                        vm.RequestReload();
                    }
                };
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.ShowDialog();

                sheets = new List<SheetModel>(vm.SheetModels);
                if (vm.Action == MainAction.Close) return;
                if (vm.Action == MainAction.Reload)
                {
                    status = L.SettingsSaved;
                    continue;
                }
                MessageDialog.Show(IntPtr.Zero, vm.Action + " → AutoCAD");
            }
        }

        private static void ShowPreview(string pdf)
        {
            var vm = new PreviewViewModel(pdf)
            {
                HeaderText = L.PreviewHeader(1, 3, Path.GetFileName(pdf)),
                PaperText = "210 x 297 mm (A4)",
                PageText = "210 x 297 mm",
                FileSizeText = PreviewViewModel.FormatBytes(new FileInfo(pdf).Length),
                MethodText = L.MethodApi,
                PlanText = "DWG To PDF.pc3 · ISO full bleed A4 (210.00 x 297.00 MM) · 0°",
                Checks = new List<string> { L.CheckFileOk("120 KB"), L.CheckPageOk("210 x 297 mm") },
                HasRemaining = true
            };
            vm.LoadImage();
            new PreviewWindow(vm) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();

            var summary = new ExportSummary { OutputFolder = Path.GetDirectoryName(Path.GetFullPath(pdf)), LogFile = "", Started = DateTime.Now, Finished = DateTime.Now };
            summary.Results.Add(new SheetResult { Number = 1, OutputPath = pdf, Outcome = SheetOutcome.Success });
            summary.Results.Add(new SheetResult { Number = 2, OutputPath = "02.pdf", Outcome = SheetOutcome.Failed, FailedStep = "plot:set-media", Error = "eInvalidInput — demo" });
            var warn = new SheetResult { Number = 3, OutputPath = "03.pdf", Outcome = SheetOutcome.SuccessWithWarning };
            warn.Warnings.Add(L.ApiFailedWarning("plot:engine-begin-document", "demo"));
            summary.Results.Add(warn);
            new SummaryWindow(summary) { WindowStartupLocation = WindowStartupLocation.CenterScreen }.ShowDialog();
        }
    }
}
