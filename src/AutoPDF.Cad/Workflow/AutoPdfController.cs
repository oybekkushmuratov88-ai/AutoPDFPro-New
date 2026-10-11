using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoPDF.Cad.Detection;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Cad.Plotting;
using AutoPDF.Cad.Transient;
using AutoPDF.Core.Localization;
using AutoPDF.Pdf;
using AutoPDF.Core.Logging;
using AutoPDF.Core.Models;
using AutoPDF.Core.Naming;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Plotting;
using AutoPDF.Core.Registration;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Sorting;
using AutoPDF.UI.Main;
using AutoPDF.UI.Preview;
using AutoPDF.UI.Registration;
using AutoPDF.UI.Services;
using AutoPDF.UI.Settings;
using AutoPDF.UI.Summary;

namespace AutoPDF.Cad.Workflow
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

        /// <summary>Status line for the next main window after it was reloaded (language change).</summary>
        private string _statusAfterReload;

        public AutoPdfController(Document doc)
        {
            _doc = doc;
            _log = AutoPdfApp.Log;
            _settingsService = new SettingsService(SettingsService.DefaultFilePath, _log);
            _settings = _settingsService.Load();
            L.Current = _settings.Language;
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
                try { return CadApp.MainWindow.Handle; }
                catch { return IntPtr.Zero; }
            }
        }

        public void Run()
        {
            _log.Info("===== AUTOPDF: " + _doc.Name + " =====");
            if (!EnsureRegistered()) return;
            string previousLayout = _pageSetup.EnsureModelSpace();
            string status = L.UnitsStatus(DrawingUnits.Describe((int)_doc.Database.Insunits)) + L.NoSheetsYet;

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
                            case MainAction.Reload:
                                status = _statusAfterReload ?? status;
                                break;
                            default:
                                return;
                        }
                    }
                }
                finally
                {
                    _pageSetup.RestoreLayout(previousLayout);
                    _log.Info(L.RunFinished);
                }
            }
        }

        // ---- Registration (required before the plug-in can be used) --------------------------

        /// <summary>
        /// Shows the registration window on the first use and blocks AUTOPDF until the user registers.
        /// Disabled unless a web-app URL is configured (<see cref="RegistrationConfig.IsConfigured"/>), so a
        /// build without the URL behaves exactly as before. A registration that could not reach the server is
        /// re-sent quietly on later runs.
        /// </summary>
        private bool EnsureRegistered()
        {
            if (!RegistrationConfig.IsConfigured) return true;

            var store = new RegistrationStore(RegistrationStore.DefaultFilePath, _log);

            // Registered during installation (Setup.exe writes a machine-wide marker) → AUTOPDF just works.
            MachineRegistration machine = MachineRegistration.Read();
            if (machine != null && machine.Registered)
            {
                if (!machine.Delivered) EnsureMachineDelivered(store, machine);
                return true;
            }

            RegistrationInfo existing = store.Load();
            if (existing != null && existing.HasCompleted)
            {
                if (!existing.Delivered) ResendInBackground(store, existing);
                return true;
            }

            var vm = new RegistrationViewModel(AutoPdfApp.Version);
            using (var gateway = new HttpRegistrationGateway(RegistrationConfig.Endpoint, AutoPdfApp.Version))
            {
                vm.Submit = info => gateway.Submit(info);
                vm.Persist = (info, delivered) =>
                {
                    store.Save(info);
                    _log.Info(L.RegSavedLog(info.Email, delivered));
                };

                CadApp.ShowModalWindow(AcadWindow, new RegistrationWindow(vm), false);
            }

            if (vm.Outcome == RegistrationOutcome.Registered) return true;

            MessageDialog.Show(AcadWindow, L.RegMandatoryAbort, L.AppTitle, DialogKind.Info);
            _log.Info(L.RegAbortedLog);
            return false;
        }

        /// <summary>
        /// The installer registered the machine but could not reach the server (installed offline). Re-send
        /// once from the plug-in and remember, per user, that it was delivered so the author is not e-mailed
        /// again on every run.
        /// </summary>
        private void EnsureMachineDelivered(RegistrationStore store, MachineRegistration machine)
        {
            RegistrationInfo local = store.Load();
            if (local != null && local.Delivered) return; // this user already re-sent it

            var info = new RegistrationInfo
            {
                FullName = machine.Name,
                Email = machine.Email,
                Phone = machine.Phone,
                Computer = MachineFacts.Computer,
                WindowsUser = MachineFacts.WindowsUser,
                MachineId = MachineFacts.MachineId(),
                Os = MachineFacts.Os,
                Version = AutoPdfApp.Version,
                Language = L.Current.ToString(),
                RegisteredAtUtc = string.IsNullOrEmpty(machine.AtUtc)
                    ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                    : machine.AtUtc,
                Delivered = false
            };
            store.Save(info);
            ResendInBackground(store, info);
        }

        /// <summary>Re-send a registration that reached this machine but not the server yet. Best effort.</summary>
        private void ResendInBackground(RegistrationStore store, RegistrationInfo info)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var gateway = new HttpRegistrationGateway(RegistrationConfig.Endpoint, AutoPdfApp.Version))
                    {
                        info.Attempts++;
                        SubmitResult result = gateway.Submit(info);
                        if (result.Ok)
                        {
                            info.Delivered = true;
                            info.LastError = string.Empty;
                            store.Save(info);
                            _log.Info(L.RegResentLog(info.Email));
                        }
                        else
                        {
                            info.LastError = result.Error;
                            store.Save(info);
                            _log.Warn(L.RegResendFailedLog(result.Error ?? string.Empty));
                        }
                    }
                }
                catch
                {
                    // Ignore — it will be retried the next time AUTOPDF runs.
                }
            });
        }

        // ---- Main window ---------------------------------------------------------------------

        private MainAction ShowMain(string status)
        {
            var vm = new MainViewModel(_settings, _sheets, "v" + AutoPdfApp.Version, status);
            var window = new MainWindow(vm);

            vm.OpenFolderRequested += folder =>
            {
                if (!Shell.OpenFolder(folder, out string error)) MessageDialog.Show(window, error, L.AppTitle, DialogKind.Warning);
            };
            vm.OpenSettingsRequested += () =>
            {
                var svm = new SettingsViewModel(_settings, _media.GetDevices(), StyleSheetList(), _log.LogDirectory);
                var sw = new SettingsWindow(svm) { Owner = window };
                sw.ShowDialog();
                if (!svm.Saved) return;

                _settingsService.Save(_settings);
                _log.Verbose = _settings.VerboseLog;
                if (svm.LanguageChanged)
                {
                    // Every text is built when a window is created: switch the language and rebuild the window.
                    L.Current = _settings.Language;
                    ReResolvePapers();
                    _statusAfterReload = L.SettingsSaved;
                    vm.RequestReload();
                    return;
                }
                ReResolvePapers();
                vm.RefreshRows();
                vm.StatusMessage = L.SettingsSaved;
            };

            CadApp.ShowModalWindow(AcadWindow, window, false);
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
                _log.Error(L.DetectionFailed, ex);
                return L.DetectionFailed + ": " + CadErrors.Describe(ex);
            }

            if (outcome.Cancelled) return outcome.Message ?? L.Cancelled;

            _sheets = SheetSorter.Sort(outcome.Sheets, _settings.SortDirection, _settings.StartNumber).ToList();
            UpdatePlans(_sheets);
            return _sheets.Count == 0 ? L.NoSheetsFound + " " + outcome.Message : outcome.Message;
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
                    s.StatusDetail = s.Paper?.Problem ?? L.PaperUnknown;
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
                    problem = CadErrors.Describe(ex);
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
            foreach (string n in notes) _log.Info(L.CheckLog(n));
            UpdatePlans(_sheets);

            int ready = _sheets.Count(s => s.Status == SheetStatus.Ready);
            int crop = _sheets.Count(s => s.Status == SheetStatus.ReadyWithCrop);
            int warn = _sheets.Count(s => s.Status == SheetStatus.Warning);
            int err = _sheets.Count(s => s.Status == SheetStatus.Error);

            if (notes.Any(n => n.StartsWith("✖", StringComparison.Ordinal)))
            {
                MessageDialog.Show(AcadWindow, string.Join("\n", notes), L.Check, DialogKind.Error);
            }

            using (var highlighter = new SheetHighlighter())
            {
                try
                {
                    highlighter.Show(_sheets);
                    SheetHighlighter.ZoomTo(_doc.Editor, _sheets, _pageSetup.GetWcsToDcs());
                    _doc.Editor.UpdateScreen();
                    _doc.Editor.GetString(new PromptStringOptions(L.PromptReview) { AllowSpaces = true });
                }
                catch (Exception ex)
                {
                    _log.Warn(L.HighlightFailed(CadErrors.Describe(ex)));
                }
            }
            try { _doc.Editor.UpdateScreen(); } catch { /* transients already erased */ }

            return L.CheckSummary(ready, crop, warn, err, string.Join("  ", notes.Where(n => !n.StartsWith("ℹ", StringComparison.Ordinal))));
        }

        // ---- Export --------------------------------------------------------------------------

        private string Export()
        {
            if (_sheets.Count == 0) return L.ScanFirst;

            string folder = _settings.OutputFolder;
            if (!EnsureFolder(folder, out string folderError))
            {
                MessageDialog.Show(AcadWindow, L.FolderNotWritable + " " + folder + "\n" + folderError, L.AppTitle, DialogKind.Error);
                return L.FolderNotWritable + " " + folder;
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
                    DialogAnswer answer = MessageDialog.Show(AcadWindow, L.ExistingFilesQuestion(existing.Count), L.AppTitle, DialogKind.YesNoCancel);
                    if (answer == DialogAnswer.Cancel) return L.ExportCancelled;
                    policy = answer == DialogAnswer.Yes ? OverwritePolicy.Overwrite : OverwritePolicy.Rename;
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
                _log.Info(L.ExistingPolicy(existing.Count, policy));
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

            _log.Info(L.ExportStart(total, folder, _settings.Plotter, _settings.UseMonochrome ? _settings.PlotStyleTable : L.NoneWord, _settings.PlotMethod));

            try
            {
                using (_pageSetup.BeginPlotSession())
                {
                    SheetPdfPipeline exporter = PdfExportFactory.Create(_doc, _settings, _log, _media, _plotSettings, _pageSetup, out _);
                    HostEnvironment.ResetUserBreak();

                    for (int i = 0; i < total; i++)
                    {
                        SheetModel sheet = _sheets[i];

                        if (skip.Contains(sheet))
                        {
                            sheet.Status = SheetStatus.Skipped;
                            summary.Results.Add(new SheetResult { Number = sheet.Number, OutputPath = planned[sheet], Outcome = SheetOutcome.Skipped, Error = L.FileExists });
                            continue;
                        }

                        if (HostEnvironment.UserBreak())
                        {
                            _log.Warn(L.EscPressed);
                            CancelRemaining(summary, i, planned);
                            break;
                        }

                        sheet.Status = SheetStatus.Plotting;
                        SheetResult result = exporter.Export(sheet, planned[sheet], i, total);
                        summary.Results.Add(result);
                        ApplyResult(sheet, result);

                        if (result.UserCancelled)
                        {
                            _log.Warn(L.CancelledInProgress);
                            CancelRemaining(summary, i + 1, planned);
                            break;
                        }

                        if (!result.IsSuccess && _settings.ErrorPolicy == ErrorPolicy.Stop)
                        {
                            _log.Warn(L.StopPolicyApplied);
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
                                _log.Warn(L.PreviewFailed(CadErrors.Describe(ex)));
                                choice = PreviewChoice.ConfirmAll;
                            }
                            if (choice == PreviewChoice.Cancel)
                            {
                                _log.Info(L.PreviewCancelledAfter(Path.GetFileName(result.OutputPath)));
                                CancelRemaining(summary, i + 1, planned);
                                break;
                            }
                            if (choice == PreviewChoice.ConfirmAll)
                            {
                                automatic = true;
                                _log.Info(L.ConfirmedAll);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Something outside a single sheet (e.g. the view could not be read) — report, never crash AutoCAD.
                _log.Error(L.ExportStopped, ex);
                MessageDialog.Show(AcadWindow, L.ExportStopped + ": " + CadErrors.Describe(ex) + "\n\n" + L.Log + ": " + _log.LogFilePath, L.AppTitle, DialogKind.Error);
            }

            summary.Finished = DateTime.Now;
            _log.Info(L.ExportTotals(summary.Total, summary.Succeeded, summary.Failed, summary.Skipped, summary.Cancelled, (summary.Finished - summary.Started).TotalSeconds));

            CadApp.ShowModalWindow(AcadWindow, new SummaryWindow(summary), false);
            if (_settings.OpenFolderWhenDone) Shell.OpenFolder(folder, out _);

            return L.ExportStatus(summary.Succeeded, summary.Failed, folder);
        }

        private PreviewChoice ShowPreview(SheetModel sheet, SheetResult result, int index, int total)
        {
            var checks = new List<string>
            {
                L.CheckFileOk(PreviewViewModel.FormatBytes(result.FileBytes)),
                result.PageSize.HasValue && result.PageSize.Value.ApproximatelyEquals(sheet.Paper.Paper, _settings.PageSizeToleranceMm)
                    ? L.CheckPageOk(result.PageSize)
                    : L.CheckPageWarn(result.PageSize?.ToString() ?? L.Unknown)
            };
            checks.AddRange(result.Warnings.Select(w => "⚠ " + w));

            var vm = new PreviewViewModel(result.OutputPath)
            {
                HeaderText = L.PreviewHeader(index + 1, total, Path.GetFileName(result.OutputPath)),
                PaperText = sheet.Paper.DisplayText + (sheet.Paper.FormatName != null ? " (" + sheet.Paper.FormatName + ")" : string.Empty),
                PageText = result.PageSize?.ToString(),
                FileSizeText = PreviewViewModel.FormatBytes(result.FileBytes),
                MethodText = result.Method == "API" ? L.MethodApi : L.MethodCommand,
                PlanText = result.PlanDescription,
                Checks = checks,
                HasRemaining = index < total - 1
            };
            vm.LoadImage();

            CadApp.ShowModalWindow(AcadWindow, new PreviewWindow(vm), false);
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
                error = L.ChooseFolderFirst;
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
