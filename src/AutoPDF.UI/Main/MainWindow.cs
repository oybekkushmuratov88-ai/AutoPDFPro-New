using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Services;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Main
{
    /// <summary>
    /// Main AutoPDF Pro window: detection method, sorting, start number, output folder,
    /// detected sheets table and the Scan / Check / Create PDF / Settings buttons.
    /// </summary>
    public sealed class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(MainViewModel vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, Uz.AppTitle, 1080, 700);
            MinWidth = 900;
            MinHeight = 560;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            vm.CloseRequested += _ => Close();
            vm.PickFolder = current => FolderPicker.Pick(new WindowInteropHelper(this).Handle, Uz.OutputFolder, current);
            vm.ShowMessage = message => UzDialog.Show(this, message, Uz.AppTitle, UzDialogKind.Warning);

            var root = new DockPanel();

            var version = new TextBlock { Foreground = Palette.Hex("#DBEAFE"), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
            version.SetBinding(TextBlock.TextProperty, new Binding(nameof(MainViewModel.VersionText)) { Mode = BindingMode.OneWay });
            Border header = Ui.Header(Uz.AppTitle, Uz.AppSubtitle, version);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            Border footer = Ui.Footer(BuildFooter());
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var body = new Grid { Margin = new Thickness(12, 12, 12, 2) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
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

        private StackPanel BuildLeftColumn()
        {
            var column = new StackPanel();

            // Detection method
            var detection = new StackPanel();
            detection.Children.Add(Ui.Radio(Uz.DetectManual, nameof(MainViewModel.IsModeManual), "mode",
                "Ramkalarni o‘zingiz tanlaysiz (bittalab yoki ramka bilan o‘rab)."));
            detection.Children.Add(Ui.Radio(Uz.DetectBlockName, nameof(MainViewModel.IsModeBlockName), "mode",
                "Model fazosidagi shu nomli barcha bloklar list deb olinadi."));
            var blockNames = Ui.Input(nameof(MainViewModel.BlockNames), tooltip: "Masalan: Ramka_A3, Format*, SHTAMP?");
            blockNames.Margin = new Thickness(22, 2, 0, 4);
            blockNames.SetBinding(IsEnabledProperty, new Binding(nameof(MainViewModel.IsModeBlockName)) { Mode = BindingMode.OneWay });
            var blockHint = Ui.Hint(Uz.BlockNamesLabel, 0);
            blockHint.Margin = new Thickness(22, 0, 0, 0);
            detection.Children.Add(blockHint);
            detection.Children.Add(blockNames);
            detection.Children.Add(Ui.Radio(Uz.DetectSample, nameof(MainViewModel.IsModeSample), "mode",
                "Bitta ramkani ko‘rsatasiz — chizmadagi xuddi shunday (shu qatlamdagi) barcha ramkalar topiladi."));
            detection.Children.Add(Ui.Radio(Uz.DetectSampleRange, nameof(MainViewModel.IsModeSampleRange), "mode",
                "Bitta ramkani ko‘rsatasiz, so‘ng hududni tanlaysiz — faqat shu hududdagi o‘xshash ramkalar olinadi."));
            column.Children.Add(Ui.Card(Uz.DetectionMethod, detection));

            // Sorting
            var sorting = new StackPanel();
            sorting.Children.Add(Ui.Radio(Uz.SortLeftToRight, nameof(MainViewModel.IsSortLeftToRight), "sort", Uz.SortLeftToRightHint));
            sorting.Children.Add(Ui.Radio(Uz.SortTopToBottom, nameof(MainViewModel.IsSortTopToBottom), "sort", Uz.SortTopToBottomHint));
            column.Children.Add(Ui.Card(Uz.SortDirection, sorting));

            // Start number
            var start = new StackPanel();
            var startBox = Ui.Input(nameof(MainViewModel.StartNumberText));
            startBox.Width = 90;
            startBox.HorizontalAlignment = HorizontalAlignment.Left;
            start.Children.Add(startBox);
            var preview = Ui.BoundText(nameof(MainViewModel.FileNamePreview), Palette.Muted, 11.5);
            preview.Margin = new Thickness(0, 6, 0, 0);
            start.Children.Add(preview);
            column.Children.Add(Ui.Card(Uz.StartNumber, start));

            // Output folder
            var folder = new StackPanel();
            var folderBox = Ui.Input(nameof(MainViewModel.OutputFolder), readOnly: true);
            folderBox.SetBinding(ToolTipProperty, new Binding(nameof(MainViewModel.OutputFolder)) { Mode = BindingMode.OneWay });
            folder.Children.Add(folderBox);
            var folderButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var choose = Ui.Button(Uz.Choose, _vm.ChooseFolderCommand);
            choose.Margin = new Thickness(0);
            folderButtons.Children.Add(choose);
            folderButtons.Children.Add(Ui.Button(Uz.Open, _vm.OpenFolderCommand));
            folder.Children.Add(folderButtons);
            column.Children.Add(Ui.Card(Uz.OutputFolder, folder));

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
                Text = Uz.NoSheetsYet,
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

            grid.Columns.Add(TextColumn(Uz.ColNumber, nameof(SheetRowViewModel.Number), new DataGridLength(48), TextAlignment.Right));
            grid.Columns.Add(TextColumn(Uz.ColBlock, nameof(SheetRowViewModel.BlockName), new DataGridLength(1.3, DataGridLengthUnitType.Star)));
            grid.Columns.Add(TextColumn(Uz.ColWidth, nameof(SheetRowViewModel.WidthText), new DataGridLength(80), TextAlignment.Right));
            grid.Columns.Add(TextColumn(Uz.ColHeight, nameof(SheetRowViewModel.HeightText), new DataGridLength(80), TextAlignment.Right));
            grid.Columns.Add(TextColumn(Uz.ColPaper, nameof(SheetRowViewModel.PaperText), new DataGridLength(1.4, DataGridLengthUnitType.Star)));
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = Uz.ColStatus,
                Width = new DataGridLength(150),
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

            var settings = Ui.Button("⚙  " + Uz.Settings, _vm.SettingsCommand, ButtonKind.Secondary, "Plotter, CTB, masshtab, qog‘oz o‘lchamlari va boshqa sozlamalar");
            settings.Margin = new Thickness(0);
            DockPanel.SetDock(settings, Dock.Left);
            dock.Children.Add(settings);

            var actions = new StackPanel { Orientation = Orientation.Horizontal };
            actions.Children.Add(Ui.Button(Uz.Scan, _vm.ScanCommand, ButtonKind.Secondary, Uz.ScanHint));
            actions.Children.Add(Ui.Button(Uz.Check, _vm.CheckCommand, ButtonKind.Secondary, Uz.CheckHint));
            var create = Ui.Button(Uz.CreatePdf, _vm.CreatePdfCommand, ButtonKind.Primary, Uz.CreatePdfHint);
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
