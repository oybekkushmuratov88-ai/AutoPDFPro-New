using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Settings;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Services;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Settings
{
    /// <summary>Edits a copy of the settings; <see cref="Saved"/> tells whether the copy was written back.</summary>
    public sealed class SettingsViewModel : ObservableObject
    {
        private readonly PluginSettings _target;
        private readonly PluginSettings _edit;
        private readonly string _logFolder;

        public SettingsViewModel(PluginSettings target, IEnumerable<string> devices, IEnumerable<string> styleTables, string logFolder)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _edit = target.Clone();
            _logFolder = logFolder;

            Devices = (devices ?? Enumerable.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!Devices.Contains(PluginSettings.DefaultPlotter, StringComparer.OrdinalIgnoreCase)) Devices.Insert(0, PluginSettings.DefaultPlotter);
            StyleTables = (styleTables ?? Enumerable.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!StyleTables.Contains(PluginSettings.DefaultStyleTable, StringComparer.OrdinalIgnoreCase)) StyleTables.Insert(0, PluginSettings.DefaultStyleTable);

            ScaleOptions = new List<OptionItem<double>> { new OptionItem<double>(0, Uz.ScaleAuto) };
            foreach (double s in new double[] { 1, 2, 2.5, 4, 5, 10, 20, 25, 50, 75, 100, 200, 250, 500, 1000 })
                ScaleOptions.Add(new OptionItem<double>(s, "1:" + s.ToString("0.##", CultureInfo.InvariantCulture)));
            if (_edit.FixedScale > 0 && ScaleOptions.All(o => Math.Abs(o.Value - _edit.FixedScale) > 1e-9))
                ScaleOptions.Add(new OptionItem<double>(_edit.FixedScale, "1:" + _edit.FixedScale.ToString("0.##", CultureInfo.InvariantCulture)));

            PlotMethods = new List<OptionItem<PlotMethod>>
            {
                new OptionItem<PlotMethod>(PlotMethod.Auto, Uz.PlotMethodAuto),
                new OptionItem<PlotMethod>(PlotMethod.ApiOnly, Uz.PlotMethodApi),
                new OptionItem<PlotMethod>(PlotMethod.CommandLineOnly, Uz.PlotMethodCmd),
            };
            OverwriteOptions = new List<OptionItem<OverwritePolicy>>
            {
                new OptionItem<OverwritePolicy>(OverwritePolicy.Ask, Uz.OverwriteAsk),
                new OptionItem<OverwritePolicy>(OverwritePolicy.Overwrite, Uz.OverwriteReplace),
                new OptionItem<OverwritePolicy>(OverwritePolicy.Rename, Uz.OverwriteRename),
                new OptionItem<OverwritePolicy>(OverwritePolicy.Skip, Uz.OverwriteSkip),
            };
            ErrorOptions = new List<OptionItem<ErrorPolicy>>
            {
                new OptionItem<ErrorPolicy>(ErrorPolicy.Continue, Uz.OnErrorContinue),
                new OptionItem<ErrorPolicy>(ErrorPolicy.Stop, Uz.OnErrorStop),
            };

            SnapToleranceText = Format(_edit.SnapToleranceMm);
            MinSheetSizeText = Format(_edit.MinSheetSizeMm);
            PageToleranceText = Format(_edit.PageSizeToleranceMm);

            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
            OpenLogFolderCommand = new RelayCommand(() => Shell.OpenFolder(_logFolder, out _));
        }

        public event Action CloseRequested;
        public Action<string> ShowError { get; set; }
        public bool Saved { get; private set; }

        public List<string> Devices { get; }
        public List<string> StyleTables { get; }
        public List<OptionItem<double>> ScaleOptions { get; }
        public List<OptionItem<PlotMethod>> PlotMethods { get; }
        public List<OptionItem<OverwritePolicy>> OverwriteOptions { get; }
        public List<OptionItem<ErrorPolicy>> ErrorOptions { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenLogFolderCommand { get; }

        public string Plotter { get => _edit.Plotter; set { _edit.Plotter = value; Raise(nameof(Plotter)); } }
        public string CustomSizesPlotter { get => _edit.CustomSizesPlotter; set { _edit.CustomSizesPlotter = value; Raise(nameof(CustomSizesPlotter)); } }
        public string PlotStyleTable { get => _edit.PlotStyleTable; set { _edit.PlotStyleTable = value; Raise(nameof(PlotStyleTable)); } }
        public bool UseMonochrome { get => _edit.UseMonochrome; set { _edit.UseMonochrome = value; Raise(nameof(UseMonochrome)); } }
        public bool PlotWithPlotStyles { get => _edit.PlotWithPlotStyles; set { _edit.PlotWithPlotStyles = value; Raise(nameof(PlotWithPlotStyles)); } }
        public bool PlotLineweights { get => _edit.PlotLineweights; set { _edit.PlotLineweights = value; Raise(nameof(PlotLineweights)); } }
        public bool ScaleLineweights { get => _edit.ScaleLineweights; set { _edit.ScaleLineweights = value; Raise(nameof(ScaleLineweights)); } }
        public bool CenterPlot { get => _edit.CenterPlot; set { _edit.CenterPlot = value; Raise(nameof(CenterPlot)); } }
        public double FixedScale { get => _edit.FixedScale; set { _edit.FixedScale = value; Raise(nameof(FixedScale)); } }
        public bool AllowCropForNonStandard { get => _edit.AllowCropForNonStandard; set { _edit.AllowCropForNonStandard = value; Raise(nameof(AllowCropForNonStandard)); } }
        public PlotMethod PlotMethod { get => _edit.PlotMethod; set { _edit.PlotMethod = value; Raise(nameof(PlotMethod)); } }
        public string CustomFormats { get => _edit.CustomFormats; set { _edit.CustomFormats = value; Raise(nameof(CustomFormats)); } }
        public bool SampleMatchLayer { get => _edit.SampleMatchLayer; set { _edit.SampleMatchLayer = value; Raise(nameof(SampleMatchLayer)); } }
        public bool SampleMatchSize { get => _edit.SampleMatchSize; set { _edit.SampleMatchSize = value; Raise(nameof(SampleMatchSize)); } }
        public string FileNamePattern { get => _edit.FileNamePattern; set { _edit.FileNamePattern = value; Raise(nameof(FileNamePattern)); } }
        public OverwritePolicy OverwritePolicy { get => _edit.OverwritePolicy; set { _edit.OverwritePolicy = value; Raise(nameof(OverwritePolicy)); } }
        public ErrorPolicy ErrorPolicy { get => _edit.ErrorPolicy; set { _edit.ErrorPolicy = value; Raise(nameof(ErrorPolicy)); } }
        public bool PreviewEachSheet { get => _edit.PreviewEachSheet; set { _edit.PreviewEachSheet = value; Raise(nameof(PreviewEachSheet)); } }
        public bool OpenFolderWhenDone { get => _edit.OpenFolderWhenDone; set { _edit.OpenFolderWhenDone = value; Raise(nameof(OpenFolderWhenDone)); } }
        public bool VerboseLog { get => _edit.VerboseLog; set { _edit.VerboseLog = value; Raise(nameof(VerboseLog)); } }

        public string SnapToleranceText { get; set; }
        public string MinSheetSizeText { get; set; }
        public string PageToleranceText { get; set; }

        private void OnSave()
        {
            if (!TryParse(SnapToleranceText, out double snap) || !TryParse(MinSheetSizeText, out double minSize) || !TryParse(PageToleranceText, out double pageTol))
            {
                ShowError?.Invoke("Son qiymatlari noto‘g‘ri kiritilgan (masalan: 0.5).");
                return;
            }

            _edit.SnapToleranceMm = snap;
            _edit.MinSheetSizeMm = minSize;
            _edit.PageSizeToleranceMm = pageTol;
            _edit.Normalize();

            CopyInto(_edit, _target);
            Saved = true;
            CloseRequested?.Invoke();
        }

        /// <summary>Copies the settings-window fields (main-window fields stay as they are).</summary>
        private static void CopyInto(PluginSettings from, PluginSettings to)
        {
            to.Plotter = from.Plotter;
            to.CustomSizesPlotter = from.CustomSizesPlotter;
            to.PlotStyleTable = from.PlotStyleTable;
            to.UseMonochrome = from.UseMonochrome;
            to.PlotWithPlotStyles = from.PlotWithPlotStyles;
            to.PlotLineweights = from.PlotLineweights;
            to.ScaleLineweights = from.ScaleLineweights;
            to.CenterPlot = from.CenterPlot;
            to.FixedScale = from.FixedScale;
            to.AllowCropForNonStandard = from.AllowCropForNonStandard;
            to.PlotMethod = from.PlotMethod;
            to.SnapToleranceMm = from.SnapToleranceMm;
            to.MinSheetSizeMm = from.MinSheetSizeMm;
            to.CustomFormats = from.CustomFormats;
            to.SampleMatchLayer = from.SampleMatchLayer;
            to.SampleMatchSize = from.SampleMatchSize;
            to.FileNamePattern = from.FileNamePattern;
            to.OverwritePolicy = from.OverwritePolicy;
            to.ErrorPolicy = from.ErrorPolicy;
            to.PreviewEachSheet = from.PreviewEachSheet;
            to.OpenFolderWhenDone = from.OpenFolderWhenDone;
            to.PageSizeToleranceMm = from.PageSizeToleranceMm;
            to.VerboseLog = from.VerboseLog;
        }

        private static string Format(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool TryParse(string text, out double value) =>
            double.TryParse((text ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public sealed class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm)
        {
            DataContext = vm ?? throw new ArgumentNullException(nameof(vm));
            Ui.ApplyWindowDefaults(this, Uz.SettingsTitle, 780, 760);
            MinWidth = 640;
            MinHeight = 480;
            vm.CloseRequested += Close;
            vm.ShowError = msg => UzDialog.Show(this, msg, Uz.SettingsTitle, UzDialogKind.Warning);

            var root = new DockPanel();
            var header = Ui.Header(Uz.SettingsTitle, "Plot, aniqlash va eksport parametrlari");
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            var buttons = new DockPanel();
            var log = Ui.Button(Uz.OpenLogFolder, vm.OpenLogFolderCommand);
            log.Margin = new Thickness(0);
            DockPanel.SetDock(log, Dock.Left);
            buttons.Children.Add(log);
            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = Ui.Button(Uz.Cancel, vm.CancelCommand);
            cancel.IsCancel = true;
            right.Children.Add(cancel);
            var save = Ui.Button(Uz.Save, vm.SaveCommand, ButtonKind.Primary);
            save.IsDefault = true;
            right.Children.Add(save);
            buttons.Children.Add(right);
            var footer = Ui.Footer(buttons);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var stack = new StackPanel { Margin = new Thickness(12) };

            // Plot
            var plot = Ui.FormGrid();
            Ui.AddRow(plot, Uz.Plotter, Ui.EditableCombo(nameof(SettingsViewModel.Devices), nameof(SettingsViewModel.Plotter)));
            Ui.AddRow(plot, Uz.CustomSizesPlotter, Ui.EditableCombo(nameof(SettingsViewModel.Devices), nameof(SettingsViewModel.CustomSizesPlotter)),
                "DWG To PDF.pc3 nusxasi, unga 630x297 kabi maxsus o‘lchamlar qo‘shilgan bo‘lsa — o‘sha o‘lchamlar aniq chiqariladi (README ga qarang).");
            Ui.AddRow(plot, Uz.StyleTable, Ui.EditableCombo(nameof(SettingsViewModel.StyleTables), nameof(SettingsViewModel.PlotStyleTable)));
            var plotChecks = new StackPanel();
            plotChecks.Children.Add(Ui.Check(Uz.UseMonochrome, nameof(SettingsViewModel.UseMonochrome)));
            plotChecks.Children.Add(Ui.Check(Uz.PlotWithStyles, nameof(SettingsViewModel.PlotWithPlotStyles)));
            plotChecks.Children.Add(Ui.Check(Uz.PlotLineweights, nameof(SettingsViewModel.PlotLineweights)));
            plotChecks.Children.Add(Ui.Check(Uz.ScaleLineweights, nameof(SettingsViewModel.ScaleLineweights)));
            plotChecks.Children.Add(Ui.Check(Uz.CenterPlot, nameof(SettingsViewModel.CenterPlot)));
            plotChecks.Children.Add(Ui.Check(Uz.AllowCrop, nameof(SettingsViewModel.AllowCropForNonStandard)));
            Ui.AddRow(plot, string.Empty, plotChecks);
            Ui.AddRow(plot, Uz.Scale, Ui.OptionCombo(nameof(SettingsViewModel.ScaleOptions), nameof(SettingsViewModel.FixedScale)),
                "Avtomatik: ramka o‘lchamidan aniqlanadi (1:1, 1:100 …).");
            Ui.AddRow(plot, Uz.PlotMethod, Ui.OptionCombo(nameof(SettingsViewModel.PlotMethods), nameof(SettingsViewModel.PlotMethod)));
            stack.Children.Add(Ui.Card(Uz.GroupPlot, plot));

            // Detection
            var detection = Ui.FormGrid();
            Ui.AddRow(detection, Uz.CustomFormats, Ui.Input(nameof(SettingsViewModel.CustomFormats)), Uz.CustomFormatsHint);
            Ui.AddRow(detection, Uz.SnapTolerance, Narrow(Ui.Input(nameof(SettingsViewModel.SnapToleranceText))));
            Ui.AddRow(detection, Uz.MinSheetSize, Narrow(Ui.Input(nameof(SettingsViewModel.MinSheetSizeText))));
            var detChecks = new StackPanel();
            detChecks.Children.Add(Ui.Check(Uz.SampleMatchLayer, nameof(SettingsViewModel.SampleMatchLayer)));
            detChecks.Children.Add(Ui.Check(Uz.SampleMatchSize, nameof(SettingsViewModel.SampleMatchSize)));
            Ui.AddRow(detection, string.Empty, detChecks);
            stack.Children.Add(Ui.Card(Uz.GroupDetection, detection));

            // Export
            var export = Ui.FormGrid();
            Ui.AddRow(export, Uz.FileNamePattern, Ui.Input(nameof(SettingsViewModel.FileNamePattern)), Uz.FileNamePatternHint);
            Ui.AddRow(export, Uz.Overwrite, Ui.OptionCombo(nameof(SettingsViewModel.OverwriteOptions), nameof(SettingsViewModel.OverwritePolicy)));
            Ui.AddRow(export, Uz.OnError, Ui.OptionCombo(nameof(SettingsViewModel.ErrorOptions), nameof(SettingsViewModel.ErrorPolicy)));
            Ui.AddRow(export, Uz.PageTolerance, Narrow(Ui.Input(nameof(SettingsViewModel.PageToleranceText))));
            var exportChecks = new StackPanel();
            exportChecks.Children.Add(Ui.Check(Uz.PreviewEachSheet, nameof(SettingsViewModel.PreviewEachSheet)));
            exportChecks.Children.Add(Ui.Check(Uz.OpenFolderWhenDone, nameof(SettingsViewModel.OpenFolderWhenDone)));
            Ui.AddRow(export, string.Empty, exportChecks);
            stack.Children.Add(Ui.Card(Uz.GroupExport, export));

            // Diagnostics
            var diagnostics = new StackPanel();
            diagnostics.Children.Add(Ui.Check(Uz.VerboseLog, nameof(SettingsViewModel.VerboseLog)));
            diagnostics.Children.Add(Ui.Hint("Diagnostika buyrug‘i: AUTOPDFMEDIA — plotter qog‘ozlari va listlar uchun tanlangan qog‘ozni ko‘rsatadi."));
            stack.Children.Add(Ui.Card(Uz.GroupDiagnostics, diagnostics));

            root.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Content = root;
        }

        private static TextBox Narrow(TextBox box)
        {
            box.Width = 100;
            box.HorizontalAlignment = HorizontalAlignment.Left;
            return box;
        }
    }
}
