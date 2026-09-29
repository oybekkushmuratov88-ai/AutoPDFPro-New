using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.UI.About;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Services;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Main
{
    /// <summary>
    /// Main AutoPDF Pro window: detection method, sorting, start number, output folder,
    /// detected sheets table and the Scan / Check / Create PDF / Settings buttons.
    /// The header shows the author (photo, name, version); clicking it opens the About window.
    /// </summary>
    public sealed class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(MainViewModel vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, L.AppTitle, 1120, 760);
            MinWidth = 960;
            MinHeight = 600;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            vm.CloseRequested += _ => Close();
            vm.PickFolder = current => FolderPicker.Pick(new WindowInteropHelper(this).Handle, L.OutputFolder, current);
            vm.ShowMessage = message => MessageDialog.Show(this, message, L.AppTitle, DialogKind.Warning);

            var root = new DockPanel();

            Border header = Ui.Header(L.AppTitle, L.AppSubtitle, BuildAuthorBadge());
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            Border footer = Ui.Footer(BuildFooter());
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var body = new Grid { Margin = new Thickness(12, 12, 12, 2) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = BuildLeftColumn(),
                Margin = new Thickness(0, 0, 12, 0)
            };
            body.Children.Add(left);

            UIElement table = BuildTableCard();
            Grid.SetColumn(table, 1);
            body.Children.Add(table);

            root.Children.Add(body);
            Content = root;
        }

        /// <summary>Author name + version + round photo at the top right; opens the About window.</summary>
        private UIElement BuildAuthorBadge()
        {
            var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            texts.Children.Add(new TextBlock
            {
                Text = AuthorInfo.Name,
                Foreground = Brushes.White,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right
            });
            var version = new TextBlock { Foreground = Palette.Hex("#DBEAFE"), FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right };
            version.SetBinding(TextBlock.TextProperty, new Binding(nameof(MainViewModel.VersionText)) { Mode = BindingMode.OneWay });
            texts.Children.Add(version);

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(texts);
            content.Children.Add(AuthorInfo.Avatar(46, 2, Brushes.White));

            return Ui.FlatButton(content, new RelayCommand(ShowAbout), L.AboutTooltip);
        }

        private void ShowAbout()
        {
            new AboutWindow(_vm.VersionText?.TrimStart('v')) { Owner = this }.ShowDialog();
        }

        private StackPanel BuildLeftColumn()
        {
            var column = new StackPanel();

            // Detection method
            var detection = new StackPanel();
            detection.Children.Add(Ui.Radio(L.DetectManual, nameof(MainViewModel.IsModeManual), "mode", L.DetectManualHint));
            detection.Children.Add(Ui.Radio(L.DetectBlockName, nameof(MainViewModel.IsModeBlockName), "mode", L.DetectBlockNameHint));
            var blockHint = Ui.Hint(L.BlockNamesLabel, 0);
            blockHint.Margin = new Thickness(22, 0, 0, 0);
            detection.Children.Add(blockHint);
            var blockNames = Ui.Input(nameof(MainViewModel.BlockNames), tooltip: L.BlockNamesExample);
            blockNames.Margin = new Thickness(22, 2, 0, 4);
            blockNames.SetBinding(IsEnabledProperty, new Binding(nameof(MainViewModel.IsModeBlockName)) { Mode = BindingMode.OneWay });
            detection.Children.Add(blockNames);
            detection.Children.Add(Ui.Radio(L.DetectSample, nameof(MainViewModel.IsModeSample), "mode", L.DetectSampleHint));
            detection.Children.Add(Ui.Radio(L.DetectSampleRange, nameof(MainViewModel.IsModeSampleRange), "mode", L.DetectSampleRangeHint));
            column.Children.Add(Ui.Card(L.DetectionMethod, detection));

            // Sorting
            var sorting = new StackPanel();
            sorting.Children.Add(Ui.Radio(L.SortLeftToRight, nameof(MainViewModel.IsSortLeftToRight), "sort", L.SortLeftToRightHint));
            sorting.Children.Add(Ui.Radio(L.SortTopToBottom, nameof(MainViewModel.IsSortTopToBottom), "sort", L.SortTopToBottomHint));
            column.Children.Add(Ui.Card(L.SortDirection, sorting));

            // Start number
            var start = new StackPanel();
            var startBox = Ui.Input(nameof(MainViewModel.StartNumberText));
            startBox.Width = 90;
            startBox.HorizontalAlignment = HorizontalAlignment.Left;
            start.Children.Add(startBox);
            var preview = Ui.BoundText(nameof(MainViewModel.FileNamePreview), Palette.Muted, 11.5);
            preview.Margin = new Thickness(0, 6, 0, 0);
            start.Children.Add(preview);
            column.Children.Add(Ui.Card(L.StartNumber, start));

            // Output folder
            var folder = new StackPanel();
            var folderBox = Ui.Input(nameof(MainViewModel.OutputFolder), readOnly: true);
            folderBox.SetBinding(ToolTipProperty, new Binding(nameof(MainViewModel.OutputFolder)) { Mode = BindingMode.OneWay });
            folder.Children.Add(folderBox);
            var folderButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var choose = Ui.Button(L.Choose, _vm.ChooseFolderCommand);
            choose.Margin = new Thickness(0);
            folderButtons.Children.Add(choose);
            folderButtons.Children.Add(Ui.Button(L.Open, _vm.OpenFolderCommand));
            folder.Children.Add(folderButtons);
            column.Children.Add(Ui.Card(L.OutputFolder, folder));

            return column;
        }

        private UIElement BuildTableCard()
        {
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var found = Ui.BoundText(nameof(MainViewModel.FoundText), Palette.Text, 13.5, FontWeights.SemiBold);
            DockPanel.SetDock(found, Dock.Left);
            header.Children.Add(found);
            var summary = Ui.BoundText(nameof(MainViewModel.SummaryText), Palette.Muted, 11.5);
            summary.HorizontalAlignment = HorizontalAlignment.Right;
            summary.TextTrimming = TextTrimming.CharacterEllipsis;
            summary.TextWrapping = TextWrapping.NoWrap;
            summary.Margin = new Thickness(12, 0, 0, 0);
            header.Children.Add(summary);

            var grid = BuildGrid();

            var empty = new TextBlock
            {
                Text = L.NoSheetsYet,
                Foreground = Palette.Muted,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(30),
                IsHitTestVisible = false
            };
            empty.SetBinding(VisibilityProperty, new Binding(nameof(MainViewModel.HasNoSheets))
            {
                Mode = BindingMode.OneWay,
                Converter = new BooleanToVisibilityConverter()
            });

            var host = new Grid();
            host.Children.Add(grid);
            host.Children.Add(empty);

            return Ui.Card(header, host, new Thickness(0, 0, 0, 10));
        }

        private DataGrid BuildGrid()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                CanUserReorderColumns = false,
                CanUserSortColumns = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = Palette.Divider,
                RowHeight = 30,
                Background = Brushes.White,
                RowBackground = Brushes.White,
                AlternatingRowBackground = Palette.Hex("#FAFBFC"),
                BorderThickness = new Thickness(0),
                FontSize = 13
            };
            VirtualizingPanel.SetIsVirtualizing(grid, true);
            grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainViewModel.Sheets)) { Mode = BindingMode.OneWay });

            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(BackgroundProperty, Palette.Hex("#F8FAFC")));
            headerStyle.Setters.Add(new Setter(ForegroundProperty, Palette.Muted));
            headerStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
            headerStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(8, 6, 8, 6)));
            headerStyle.Setters.Add(new Setter(BorderBrushProperty, Palette.CardBorder));
            headerStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            grid.ColumnHeaderStyle = headerStyle;

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(ToolTipProperty, new Binding(nameof(SheetRowViewModel.ToolTipText))));
            grid.RowStyle = rowStyle;

            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(FocusVisualStyleProperty, null));
            var selected = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(BackgroundProperty, Palette.InfoBack));
            selected.Setters.Add(new Setter(ForegroundProperty, Palette.Text));
            cellStyle.Triggers.Add(selected);
            grid.CellStyle = cellStyle;

            grid.Columns.Add(TextColumn(L.ColNumber, nameof(SheetRowViewModel.Number), new DataGridLength(48), TextAlignment.Right));
            grid.Columns.Add(TextColumn(L.ColBlock, nameof(SheetRowViewModel.BlockName), new DataGridLength(1.3, DataGridLengthUnitType.Star)));
            grid.Columns.Add(TextColumn(L.ColWidth, nameof(SheetRowViewModel.WidthText), new DataGridLength(84), TextAlignment.Right));
            grid.Columns.Add(TextColumn(L.ColHeight, nameof(SheetRowViewModel.HeightText), new DataGridLength(84), TextAlignment.Right));
            grid.Columns.Add(TextColumn(L.ColPaper, nameof(SheetRowViewModel.PaperText), new DataGridLength(1.4, DataGridLengthUnitType.Star)));
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = L.ColStatus,
                Width = new DataGridLength(160),
                CellTemplate = StatusTemplate()
            });
            return grid;
        }

        private static DataGridTextColumn TextColumn(string header, string path, DataGridLength width, TextAlignment alignment = TextAlignment.Left)
        {
            var element = new Style(typeof(TextBlock));
            element.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(8, 0, 8, 0)));
            element.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            element.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
            element.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            return new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(path) { Mode = BindingMode.OneWay },
                Width = width,
                ElementStyle = element
            };
        }

        private static DataTemplate StatusTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.PaddingProperty, new Thickness(9, 2, 9, 2));
            border.SetValue(MarginProperty, new Thickness(6, 0, 6, 0));
            border.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
            border.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.SetBinding(Border.BackgroundProperty, new Binding(nameof(SheetRowViewModel.StatusBackground)));

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding(nameof(SheetRowViewModel.StatusText)));
            text.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(SheetRowViewModel.StatusForeground)));
            text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            text.SetValue(TextBlock.FontSizeProperty, 12.0);
            border.AppendChild(text);

            return new DataTemplate { VisualTree = border };
        }

        private UIElement BuildFooter()
        {
            var dock = new DockPanel { LastChildFill = true };

            var settings = Ui.Button("⚙  " + L.Settings, _vm.SettingsCommand, ButtonKind.Secondary, L.SettingsHint);
            settings.Margin = new Thickness(0);
            DockPanel.SetDock(settings, Dock.Left);
            dock.Children.Add(settings);

            var actions = new StackPanel { Orientation = Orientation.Horizontal };
            actions.Children.Add(Ui.Button(L.Scan, _vm.ScanCommand, ButtonKind.Secondary, L.ScanHint));
            actions.Children.Add(Ui.Button(L.Check, _vm.CheckCommand, ButtonKind.Secondary, L.CheckHint));
            var create = Ui.Button(L.CreatePdf, _vm.CreatePdfCommand, ButtonKind.Primary, L.CreatePdfHint);
            create.MinWidth = 150;
            create.IsDefault = true;
            actions.Children.Add(create);
            DockPanel.SetDock(actions, Dock.Right);
            dock.Children.Add(actions);

            var status = Ui.BoundText(nameof(MainViewModel.StatusMessage), Palette.Muted, 12);
            status.Margin = new Thickness(14, 0, 14, 0);
            status.TextTrimming = TextTrimming.CharacterEllipsis;
            status.TextWrapping = TextWrapping.NoWrap;
            status.SetBinding(ToolTipProperty, new Binding(nameof(MainViewModel.StatusMessage)) { Mode = BindingMode.OneWay });
            dock.Children.Add(status);

            return dock;
        }
    }
}
