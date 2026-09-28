using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using AutoPDF.AutoCAD.Detection;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.AutoCAD.Plotting;
using AutoPDF.AutoCAD.Transient;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Models;
using AutoPDF.Core.Naming;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Sorting;
using AutoPDF.UI.Main;
using AutoPDF.UI.Preview;
using AutoPDF.UI.Services;
using AutoPDF.UI.Settings;
using AutoPDF.UI.Summary;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoPDF.AutoCAD.Workflow
{
    /// <summary>
    /// Runs the AUTOPDF command. The main window is modal; when the user presses Scan, Check or
    /// Create PDF it closes, the plug-in works in the drawing (selection prompts, plotting — always from
    /// the command context, which the plot engine and -PLOT require) and the window opens again with the
    /// updated table.
    /// </summary>
    public sealed class AutoPdfController
    {
        private readonly Document _doc;
        private readonly LoggingService _log;
        private readonly SettingsService _settingsService;
        private readonly PluginSettings _settings;
        private readonly CustomPaperSizeManager _media;
        private readonly PlotSettingsService _plotSettings;
        private readonly PageSetupService _pageSetup;
        private List<SheetModel> _sheets = new List<SheetModel>();

        public AutoPdfController(Document doc)
        {
            _doc = doc;
            _log = AutoPdfApp.Log;
            _settingsService = new SettingsService(SettingsService.DefaultFilePath, _log);
            _settings = _settingsService.Load();
            _log.Verbose = _settings.VerboseLog;
            CustomPaperSizeManager.ClearCache(); // pick up PC3 files edited since the last run
            _media = new CustomPaperSizeManager(_log);
            _plotSettings = new PlotSettingsService(_log);
            _pageSetup = new PageSetupService(doc, _log);
        }

        private static IntPtr AcadWindow
        {
            get
            {
                try { return AcApp.MainWindow.Handle; }
                catch { return IntPtr.Zero; }
            }
        }

        public void Run()
        {
            _log.Info("===== AUTOPDF: " + _doc.Name + " =====");
            string previousLayout = _pageSetup.EnsureModelSpace();
            string status = "Chizma birliklari: " + DrawingUnits.Describe((int)_doc.Database.Insunits) + ". " + Uz.NoSheetsYet;

            using (new EditorEcho(_log, _doc))
            {
                try
                {
                    while (true)
                    {
                        MainAction action = ShowMain(status);
                        _settingsService.Save(_settings);

                        switch (action)
                        {
                            case MainAction.Scan:
                                status = Scan();
                                break;
                            case MainAction.Check:
                                status = Check();
                                break;
                            case MainAction.CreatePdf:
                                status = Export();
                                break;
                            default:
                                return;
                        }
                    }
                }
                finally
                {
                    _pageSetup.RestoreLayout(previousLayout);
                    _log.Info("===== AUTOPDF tugadi =====");
                }
            }
        }

        // ---- Main window ---------------------------------------------------------------------

        private MainAction ShowMain(string status)
        {
            var vm = new MainViewModel(_settings, _sheets, "v" + AutoPdfApp.Version, status);
            var window = new MainWindow(vm);

            vm.OpenFolderRequested += folder =>
            {
                if (!Shell.OpenFolder(folder, out string error)) UzDialog.Show(window, error, Uz.AppTitle, UzDialogKind.Warning);
            };
            vm.OpenSettingsRequested += () =>
            {
                var svm = new SettingsViewModel(_settings, _media.GetDevices(), StyleSheetList(), _log.LogDirectory);
                var sw = new SettingsWindow(svm) { Owner = window };
                sw.ShowDialog();
                if (!svm.Saved) return;

                _settingsService.Save(_settings);
                _log.Verbose = _settings.VerboseLog;
                ReResolvePapers();
                vm.RefreshRows();
                vm.StatusMessage = "Sozlamalar saqlandi.";
            };

            AcApp.ShowModalWindow(AcadWindow, window, false);
            _sheets = vm.SheetModels.ToList();
            return vm.Action;
        }

        private List<string> StyleSheetList()
        {
            try
            {
                return PlotSettingsValidator.Current.GetPlotStyleSheetList().Cast<string>().ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        // ---- Scan ----------------------------------------------------------------------------

        private string Scan()
        {
            DetectionOutcome outcome;
            try
            {
                outcome = new SheetDetector(_doc, _settings, _log).Detect(_settings.DetectionMode);
            }
            catch (Exception ex)
            {
                _log.Error("Aniqlashda xato", ex);
                return "Aniqlashda xato: " + AcadErrors.Describe(ex);
            }

            if (outcome.Cancelled) return outcome.Message ?? "Bekor qilindi.";

            _sheets = SheetSorter.Sort(outcome.Sheets, _settings.SortDirection, _settings.StartNumber).ToList();
            UpdatePlans(_sheets);
            return _sheets.Count == 0 ? Uz.NoSheetsFound + " " + outcome.Message : outcome.Message;
        }

        /// <summary>Re-run paper detection after settings changed (scale, custom formats, tolerance …).</summary>
        private void ReResolvePapers()
        {
            if (_sheets.Count == 0) return;
            PaperSizeResolver resolver = SheetDetector.CreateResolver(_doc.Database, _settings);
            foreach (SheetModel s in _sheets)
            {
                s.Paper = resolver.Resolve(s.Width, s.Height);
                s.Status = SheetStatus.Ready;
                s.StatusDetail = null;
            }
            UpdatePlans(_sheets);
        }

        /// <summary>Paper plan per sheet → status Ready / Ready (crop) / Error.</summary>
        private void UpdatePlans(IEnumerable<SheetModel> sheets)
        {
            foreach (SheetModel s in sheets)
            {
                s.OutputPath = null;
                if (s.Paper == null || !s.Paper.IsValid)
                {
                    s.Status = SheetStatus.Error;
                    s.StatusDetail = s.Paper?.Problem ?? "Qog‘oz o‘lchami aniqlanmadi.";
                    s.PlanDescription = null;
                    continue;
                }

                MediaPlan plan;
                string problem;
                try
                {
                    plan = _media.Plan(s.Paper.Paper, _settings, out problem);
                }
                catch (Exception ex)
                {
                    plan = null;
                    problem = AcadErrors.Describe(ex);
                }

                if (plan == null)
                {
                    s.Status = SheetStatus.Error;
                    s.StatusDetail = problem;
                    s.PlanDescription = null;
                }
                else
                {
                    s.Status = plan.Fit == MediaFit.Crop ? SheetStatus.ReadyWithCrop : SheetStatus.Ready;
                    s.PlanDescription = plan.Describe();
                    s.StatusDetail = s.Warnings.Count > 0 ? string.Join("; ", s.Warnings) : null;
                    if (s.Warnings.Count > 0 && s.Status == SheetStatus.Ready) s.Status = SheetStatus.Warning;
                }
            }
        }

        // ---- Check ---------------------------------------------------------------------------

        private string Check()
        {
            List<string> notes = _pageSetup.CheckEnvironment(_settings, _media, _plotSettings);
            foreach (string n in notes) _log.Info("Tekshiruv: " + n);
            UpdatePlans(_sheets);

            int ready = _sheets.Count(s => s.Status == SheetStatus.Ready);
            int crop = _sheets.Count(s => s.Status == SheetStatus.ReadyWithCrop);
            int warn = _sheets.Count(s => s.Status == SheetStatus.Warning);
            int err = _sheets.Count(s => s.Status == SheetStatus.Error);

            if (notes.Any(n => n.StartsWith("✖", StringComparison.Ordinal)))
            {
                UzDialog.Show(AcadWindow, string.Join("\n", notes), Uz.Check, UzDialogKind.Error);
            }

            using (var highlighter = new SheetHighlighter())
            {
                try
                {
                    highlighter.Show(_sheets);
                    SheetHighlighter.ZoomTo(_doc.Editor, _sheets, _pageSetup.GetWcsToDcs());
                    _doc.Editor.UpdateScreen();
                    _doc.Editor.GetString(new PromptStringOptions(Uz.PromptReview) { AllowSpaces = true });
                }
                catch (Exception ex)
                {
                    _log.Warn("Listlarni chizmada ko‘rsatib bo‘lmadi: " + AcadErrors.Describe(ex));
                }
            }
            try { _doc.Editor.UpdateScreen(); } catch { /* transients already erased */ }

            return string.Format(CultureInfo.InvariantCulture,
                "Tekshiruv: {0} tayyor, {1} kesish bilan, {2} ogohlantirish, {3} xato. {4}",
                ready, crop, warn, err, string.Join("  ", notes.Where(n => !n.StartsWith("ℹ", StringComparison.Ordinal))));
        }

        // ---- Export --------------------------------------------------------------------------

        private string Export()
        {
            if (_sheets.Count == 0) return Uz.ScanFirst;

            string folder = _settings.OutputFolder;
            if (!EnsureFolder(folder, out string folderError))
            {
                UzDialog.Show(AcadWindow, Uz.FolderNotWritable + " " + folder + "\n" + folderError, Uz.AppTitle, UzDialogKind.Error);
                return Uz.FolderNotWritable + " " + folder;
            }

            // Output names: 01.pdf, 02.pdf … (unique inside the batch).
            string dwgName = Path.GetFileNameWithoutExtension(_doc.Name);
            int pad = OutputFileNamer.PadWidth(_sheets.Max(s => s.Number));
            var planned = new Dictionary<SheetModel, string>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SheetModel s in _sheets)
            {
                string name = OutputFileNamer.BuildFileName(_settings.FileNamePattern, s.Number, pad, dwgName, s.Paper?.FormatName);
                string path = OutputFileNamer.MakeUnique(Path.Combine(folder, name), used.Contains);
                used.Add(path);
                planned[s] = path;
            }

            // Existing files.
            var skip = new HashSet<SheetModel>();
            List<SheetModel> existing = _sheets.Where(s => File.Exists(planned[s])).ToList();
            if (existing.Count > 0)
            {
                OverwritePolicy policy = _settings.OverwritePolicy;
                if (policy == OverwritePolicy.Ask)
                {
                    UzDialogResult answer = UzDialog.Show(AcadWindow,
                        string.Format(CultureInfo.InvariantCulture, Uz.ExistingFilesQuestion, existing.Count),
                        Uz.AppTitle, UzDialogKind.YesNoCancel);
                    if (answer == UzDialogResult.Cancel) return "Eksport bekor qilindi.";
                    policy = answer == UzDialogResult.Yes ? OverwritePolicy.Overwrite : OverwritePolicy.Rename;
                }

                foreach (SheetModel s in existing)
                {
                    if (policy == OverwritePolicy.Skip) skip.Add(s);
                    else if (policy == OverwritePolicy.Rename)
                    {
                        string unique = OutputFileNamer.MakeUnique(planned[s], p => File.Exists(p) || used.Contains(p));
                        used.Add(unique);
                        planned[s] = unique;
                    }
                }
                _log.Info("Mavjud fayllar: " + existing.Count + " — siyosat: " + policy + ".");
            }

            _log.ResetSession();
            var summary = new ExportSummary { OutputFolder = folder, LogFile = _log.LogFilePath, Started = DateTime.Now };
            bool automatic = !_settings.PreviewEachSheet;
            int total = _sheets.Count;

            foreach (SheetModel s in _sheets)
            {
                s.Status = SheetStatus.Waiting;
                s.StatusDetail = null;
                s.OutputPath = planned[s];
            }

            _log.Info(string.Format(CultureInfo.InvariantCulture, "Eksport: {0} ta list → {1} (plotter {2}, uslub {3}, usul {4}).",
                total, folder, _settings.Plotter, _settings.UseMonochrome ? _settings.PlotStyleTable : "yo‘q", _settings.PlotMethod));

            try
            {
                using (_pageSetup.BeginPlotSession())
                {
                    var exporter = new PdfExportService(_doc, _settings, _log, _media, _plotSettings, _pageSetup);

                    for (int i = 0; i < total; i++)
                    {
                        SheetModel sheet = _sheets[i];

                        if (skip.Contains(sheet))
                        {
                            sheet.Status = SheetStatus.Skipped;
                            summary.Results.Add(new SheetResult { Number = sheet.Number, OutputPath = planned[sheet], Outcome = SheetOutcome.Skipped, Error = "fayl mavjud" });
                            continue;
                        }

                        if (Autodesk.AutoCAD.DatabaseServices.HostApplicationServices.Current.UserBreak())
                        {
                            _log.Warn("Foydalanuvchi ESC bosdi — eksport to‘xtatildi.");
                            CancelRemaining(summary, i, planned);
                            break;
                        }

                        sheet.Status = SheetStatus.Plotting;
                        SheetResult result = exporter.Export(sheet, planned[sheet], i, total);
                        summary.Results.Add(result);
                        ApplyResult(sheet, result);

                        if (result.UserCancelled)
                        {
                            _log.Warn("Chop etish oynasida bekor qilindi — eksport to‘xtatildi.");
                            CancelRemaining(summary, i + 1, planned);
                            break;
                        }

                        if (!result.IsSuccess && _settings.ErrorPolicy == ErrorPolicy.Stop)
                        {
                            _log.Warn("Xatolik siyosati: to‘xtatish — qolgan listlar chiqarilmadi.");
                            CancelRemaining(summary, i + 1, planned);
                            break;
                        }

                        if (!automatic && result.IsSuccess)
                        {
                            PreviewChoice choice;
                            try
                            {
                                choice = ShowPreview(sheet, result, i, total);
                            }
                            catch (Exception ex)
                            {
                                // The preview is a convenience; it must never stop the export.
                                _log.Warn("Ko‘rib chiqish oynasini ochib bo‘lmadi (" + AcadErrors.Describe(ex) + ") — qolgan listlar avtomatik yaratiladi.");
                                choice = PreviewChoice.ConfirmAll;
                            }
                            if (choice == PreviewChoice.Cancel)
                            {
                                _log.Info("Ko‘rib chiqishda bekor qilindi (" + Path.GetFileName(result.OutputPath) + " dan keyin).");
                                CancelRemaining(summary, i + 1, planned);
                                break;
                            }
                            if (choice == PreviewChoice.ConfirmAll)
                            {
                                automatic = true;
                                _log.Info("Tasdiqlandi — qolgan listlar avtomatik yaratiladi.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Something outside a single sheet (e.g. the view could not be read) — report, never crash AutoCAD.
                _log.Error("Eksport to‘xtadi", ex);
                UzDialog.Show(AcadWindow, "Eksport to‘xtadi: " + AcadErrors.Describe(ex) + "\n\nJurnal: " + _log.LogFilePath, Uz.AppTitle, UzDialogKind.Error);
            }

            summary.Finished = DateTime.Now;
            _log.Info(string.Format(CultureInfo.InvariantCulture, "Eksport yakuni: jami {0}, muvaffaqiyatli {1}, xato {2}, o‘tkazildi {3}, bekor {4} ({5:0.0} s).",
                summary.Total, summary.Succeeded, summary.Failed, summary.Skipped, summary.Cancelled, (summary.Finished - summary.Started).TotalSeconds));

            AcApp.ShowModalWindow(AcadWindow, new SummaryWindow(summary), false);
            if (_settings.OpenFolderWhenDone) Shell.OpenFolder(folder, out _);

            return string.Format(CultureInfo.InvariantCulture, "Eksport: {0} ta PDF yaratildi, {1} ta xato. Papka: {2}", summary.Succeeded, summary.Failed, folder);
        }

        private PreviewChoice ShowPreview(SheetModel sheet, SheetResult result, int index, int total)
        {
            var checks = new List<string>
            {
                "✓ Fayl yaratildi va bo‘sh emas (" + PreviewViewModel.FormatBytes(result.FileBytes) + ")",
                result.PageSize.HasValue && result.PageSize.Value.ApproximatelyEquals(sheet.Paper.Paper, _settings.PageSizeToleranceMm)
                    ? "✓ Sahifa o‘lchami to‘g‘ri: " + result.PageSize
                    : "⚠ Sahifa o‘lchami: " + (result.PageSize?.ToString() ?? "noma’lum")
            };
            checks.AddRange(result.Warnings.Select(w => "⚠ " + w));

            var vm = new PreviewViewModel(result.OutputPath)
            {
                HeaderText = string.Format(CultureInfo.InvariantCulture, "List {0} / {1} — {2}", index + 1, total, Path.GetFileName(result.OutputPath)),
                PaperText = sheet.Paper.DisplayText + (sheet.Paper.FormatName != null ? " (" + sheet.Paper.FormatName + ")" : string.Empty),
                PageText = result.PageSize?.ToString(),
                FileSizeText = PreviewViewModel.FormatBytes(result.FileBytes),
                MethodText = result.Method == "API" ? "AutoCAD plot API" : "-PLOT buyrug‘i",
                PlanText = result.PlanDescription,
                Checks = checks,
                HasRemaining = index < total - 1
            };
            vm.LoadImage();

            AcApp.ShowModalWindow(AcadWindow, new PreviewWindow(vm), false);
            return vm.Choice;
        }

        private void ApplyResult(SheetModel sheet, SheetResult r)
        {
            switch (r.Outcome)
            {
                case SheetOutcome.Success:
                    sheet.Status = SheetStatus.Done;
                    sheet.StatusDetail = r.Method + ", " + r.PageSize;
                    break;
                case SheetOutcome.SuccessWithWarning:
                    sheet.Status = SheetStatus.DoneWithWarning;
                    sheet.StatusDetail = string.Join("; ", r.Warnings);
                    break;
                case SheetOutcome.Cancelled:
                    sheet.Status = SheetStatus.Cancelled;
                    sheet.StatusDetail = r.Error;
                    break;
                default:
                    sheet.Status = SheetStatus.Failed;
                    sheet.StatusDetail = "[" + r.FailedStep + "] " + r.Error;
                    break;
            }
        }

        private void CancelRemaining(ExportSummary summary, int fromIndex, Dictionary<SheetModel, string> planned)
        {
            for (int j = fromIndex; j < _sheets.Count; j++)
            {
                SheetModel s = _sheets[j];
                if (summary.Results.Any(r => r.Number == s.Number)) continue;
                s.Status = SheetStatus.Cancelled;
                summary.Results.Add(new SheetResult { Number = s.Number, OutputPath = planned[s], Outcome = SheetOutcome.Cancelled });
            }
        }

        private static bool EnsureFolder(string folder, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(folder))
            {
                error = Uz.ChooseFolderFirst;
                return false;
            }
            try
            {
                Directory.CreateDirectory(folder);
                string probe = Path.Combine(folder, ".autopdf-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
