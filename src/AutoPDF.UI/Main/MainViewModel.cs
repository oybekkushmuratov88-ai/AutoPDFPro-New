using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Models;
using AutoPDF.Core.Naming;
using AutoPDF.Core.Settings;
using AutoPDF.Core.Sorting;
using AutoPDF.UI.Mvvm;

namespace AutoPDF.UI.Main
{
    /// <summary>What the user asked the plugin to do when the main window closed.</summary>
    public enum MainAction
    {
        Close,
        Scan,
        Check,
        CreatePdf,

        /// <summary>Rebuild the window (e.g. after the interface language was changed).</summary>
        Reload
    }

    public sealed class MainViewModel : ObservableObject
    {
        private readonly PluginSettings _settings;
        private List<SheetModel> _sheets;
        private string _startNumberText;
        private string _statusMessage;

        public MainViewModel(PluginSettings settings, IEnumerable<SheetModel> sheets, string versionText, string statusMessage = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sheets = (sheets ?? Enumerable.Empty<SheetModel>()).ToList();
            _startNumberText = _settings.StartNumber.ToString(CultureInfo.InvariantCulture);
            _statusMessage = statusMessage;
            VersionText = versionText;
            Sheets = new ObservableCollection<SheetRowViewModel>();

            ScanCommand = new RelayCommand(() => Finish(MainAction.Scan));
            CheckCommand = new RelayCommand(() => Finish(MainAction.Check), () => HasSheets);
            CreatePdfCommand = new RelayCommand(OnCreatePdf, () => HasSheets);
            SettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke());
            ChooseFolderCommand = new RelayCommand(OnChooseFolder);
            OpenFolderCommand = new RelayCommand(() => OpenFolderRequested?.Invoke(OutputFolder), () => Directory.Exists(OutputFolder ?? string.Empty));

            Resort();
        }

        // ---- Services injected by the window / plugin ---------------------------------------
        public event Action<MainAction> CloseRequested;
        public event Action OpenSettingsRequested;
        public event Action<string> OpenFolderRequested;
        public Func<string, string> PickFolder { get; set; }
        public Action<string> ShowMessage { get; set; }

        public PluginSettings Settings => _settings;
        public MainAction Action { get; private set; } = MainAction.Close;
        public string VersionText { get; }

        public ObservableCollection<SheetRowViewModel> Sheets { get; }

        /// <summary>The sheets in their current order and numbering.</summary>
        public IReadOnlyList<SheetModel> SheetModels => _sheets;

        public ICommand ScanCommand { get; }
        public ICommand CheckCommand { get; }
        public ICommand CreatePdfCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand ChooseFolderCommand { get; }
        public ICommand OpenFolderCommand { get; }

        // ---- Detection mode -----------------------------------------------------------------
        public bool IsModeManual { get => Mode == DetectionMode.Manual; set { if (value) Mode = DetectionMode.Manual; } }
        public bool IsModeBlockName { get => Mode == DetectionMode.BlockName; set { if (value) Mode = DetectionMode.BlockName; } }
        public bool IsModeSample { get => Mode == DetectionMode.Sample; set { if (value) Mode = DetectionMode.Sample; } }
        public bool IsModeSampleRange { get => Mode == DetectionMode.SampleRange; set { if (value) Mode = DetectionMode.SampleRange; } }

        private DetectionMode Mode
        {
            get => _settings.DetectionMode;
            set
            {
                if (_settings.DetectionMode == value) return;
                _settings.DetectionMode = value;
                Raise(nameof(IsModeManual));
                Raise(nameof(IsModeBlockName));
                Raise(nameof(IsModeSample));
                Raise(nameof(IsModeSampleRange));
            }
        }

        public string BlockNames
        {
            get => _settings.BlockNames;
            set
            {
                if (_settings.BlockNames == value) return;
                _settings.BlockNames = value ?? string.Empty;
                Raise(nameof(BlockNames));
            }
        }

        // ---- Sorting ------------------------------------------------------------------------
        public bool IsSortLeftToRight
        {
            get => _settings.SortDirection == SortDirection.LeftToRight;
            set { if (value) SetSort(SortDirection.LeftToRight); }
        }

        public bool IsSortTopToBottom
        {
            get => _settings.SortDirection == SortDirection.TopToBottom;
            set { if (value) SetSort(SortDirection.TopToBottom); }
        }

        private void SetSort(SortDirection direction)
        {
            if (_settings.SortDirection == direction) return;
            _settings.SortDirection = direction;
            Raise(nameof(IsSortLeftToRight));
            Raise(nameof(IsSortTopToBottom));
            Resort();
        }

        public string StartNumberText
        {
            get => _startNumberText;
            set
            {
                if (!Set(ref _startNumberText, value)) return;
                if (int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0 && n <= 99999)
                {
                    _settings.StartNumber = n;
                    Resort();
                }
            }
        }

        // ---- Output folder ------------------------------------------------------------------
        public string OutputFolder
        {
            get => _settings.OutputFolder;
            set
            {
                if (_settings.OutputFolder == value) return;
                _settings.OutputFolder = value ?? string.Empty;
                Raise(nameof(OutputFolder));
            }
        }

        public string FileNamePreview
        {
            get
            {
                if (_sheets.Count == 0)
                {
                    int pad = OutputFileNamer.PadWidth(_settings.StartNumber + 1);
                    return L.FilesPreview(
                        OutputFileNamer.BuildFileName(_settings.FileNamePattern, _settings.StartNumber, pad),
                        OutputFileNamer.BuildFileName(_settings.FileNamePattern, _settings.StartNumber + 1, pad));
                }
                int padWidth = OutputFileNamer.PadWidth(_sheets.Max(s => s.Number));
                string first = OutputFileNamer.BuildFileName(_settings.FileNamePattern, _sheets.First().Number, padWidth);
                string last = OutputFileNamer.BuildFileName(_settings.FileNamePattern, _sheets.Last().Number, padWidth);
                return _sheets.Count == 1 ? L.FileSingle(first) : L.FilesRange(first, last);
            }
        }

        // ---- Table / status ----------------------------------------------------------------
        public bool HasSheets => _sheets.Count > 0;
        public bool HasNoSheets => _sheets.Count == 0;

        public string FoundText => L.FoundSheets(_sheets.Count);

        public string SummaryText
        {
            get
            {
                if (_sheets.Count == 0) return string.Empty;
                int ready = _sheets.Count(s => s.Status == SheetStatus.Ready || s.Status == SheetStatus.Done);
                int crop = _sheets.Count(s => s.Status == SheetStatus.ReadyWithCrop);
                int warn = _sheets.Count(s => s.Status == SheetStatus.Warning || s.Status == SheetStatus.DoneWithWarning);
                int err = _sheets.Count(s => s.Status == SheetStatus.Error || s.Status == SheetStatus.Failed);
                var parts = new List<string>();
                if (ready > 0) parts.Add(L.CountReady(ready));
                if (crop > 0) parts.Add(L.CountCrop(crop));
                if (warn > 0) parts.Add(L.CountWarnings(warn));
                if (err > 0) parts.Add(L.CountErrors(err));
                var formats = _sheets.Where(s => s.Paper != null)
                    .GroupBy(s => s.Paper.FormatName ?? s.Paper.Paper.Key)
                    .Select(g => g.Key + "×" + g.Count());
                return string.Join(" · ", parts) + "   |   " + string.Join(", ", formats);
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public void Resort()
        {
            _sheets = SheetSorter.Sort(_sheets, _settings.SortDirection, _settings.StartNumber).ToList();
            Sheets.Clear();
            foreach (SheetModel s in _sheets) Sheets.Add(new SheetRowViewModel(s));
            RaiseTableProperties();
        }

        public void RefreshRows()
        {
            foreach (SheetRowViewModel row in Sheets) row.Refresh();
            RaiseTableProperties();
        }

        private void RaiseTableProperties()
        {
            Raise(nameof(HasSheets));
            Raise(nameof(HasNoSheets));
            Raise(nameof(FoundText));
            Raise(nameof(SummaryText));
            Raise(nameof(FileNamePreview));
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnChooseFolder()
        {
            string picked = PickFolder?.Invoke(OutputFolder);
            if (!string.IsNullOrEmpty(picked)) OutputFolder = picked;
        }

        private void OnCreatePdf()
        {
            if (!HasSheets)
            {
                ShowMessage?.Invoke(L.ScanFirst);
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputFolder))
            {
                OnChooseFolder();
                if (string.IsNullOrWhiteSpace(OutputFolder))
                {
                    ShowMessage?.Invoke(L.ChooseFolderFirst);
                    return;
                }
            }
            Finish(MainAction.CreatePdf);
        }

        /// <summary>Close the window so the plug-in opens it again (used after a language change).</summary>
        public void RequestReload() => Finish(MainAction.Reload);

        private void Finish(MainAction action)
        {
            Action = action;
            CloseRequested?.Invoke(action);
        }
    }
}
