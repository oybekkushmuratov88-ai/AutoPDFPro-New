using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Services;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Preview
{
    public enum PreviewChoice
    {
        /// <summary>Stop the export.</summary>
        Cancel,

        /// <summary>Accept this PDF, create the next one and preview it too.</summary>
        Confirm,

        /// <summary>Accept this PDF and create all remaining ones without previews.</summary>
        ConfirmAll
    }

    public sealed class PreviewViewModel : ObservableObject
    {
        public PreviewViewModel(string pdfPath)
        {
            PdfPath = pdfPath;
            CancelCommand = new RelayCommand(() => Finish(PreviewChoice.Cancel));
            ConfirmCommand = new RelayCommand(() => Finish(PreviewChoice.Confirm));
            ConfirmAllCommand = new RelayCommand(() => Finish(PreviewChoice.ConfirmAll), () => HasRemaining);
            OpenExternalCommand = new RelayCommand(OpenExternal, () => File.Exists(PdfPath));
        }

        public event Action<PreviewChoice> CloseRequested;

        public string PdfPath { get; }
        public string HeaderText { get; set; }
        public string PaperText { get; set; }
        public string PageText { get; set; }
        public string FileSizeText { get; set; }
        public string MethodText { get; set; }
        public string PlanText { get; set; }
        public IList<string> Checks { get; set; } = new List<string>();
        public bool HasRemaining { get; set; } = true;
        public PreviewChoice Choice { get; private set; } = PreviewChoice.Cancel;

        public BitmapSource Image { get; private set; }
        public bool HasImage => Image != null;
        public bool HasNoImage => Image == null;
        public string ImageMessage { get; private set; }

        public ICommand CancelCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand ConfirmAllCommand { get; }
        public ICommand OpenExternalCommand { get; }

        public string ConfirmText => HasRemaining ? Uz.PreviewConfirm : Uz.PreviewConfirm + " (oxirgi list)";

        public void LoadImage(int maxPixels = 1600)
        {
            string error;
            try
            {
                Image = PdfPreviewRenderer.TryRender(PdfPath, maxPixels, out error);
            }
            catch (Exception ex)
            {
                // e.g. the Windows PDF component cannot be loaded — the window still works without the picture.
                Image = null;
                error = ex.Message;
            }
            ImageMessage = Image == null ? Uz.PreviewUnavailable + (string.IsNullOrEmpty(error) ? string.Empty : "\n(" + error + ")") : null;
            Raise(nameof(Image));
            Raise(nameof(HasImage));
            Raise(nameof(HasNoImage));
            Raise(nameof(ImageMessage));
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes.ToString(CultureInfo.InvariantCulture) + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }

        private void OpenExternal()
        {
            Shell.OpenFile(PdfPath, out _);
        }

        private void Finish(PreviewChoice choice)
        {
            Choice = choice;
            CloseRequested?.Invoke(choice);
        }
    }

    /// <summary>Shows a freshly created PDF: Cancel / Confirm / Confirm and process the rest automatically.</summary>
    public sealed class PreviewWindow : Window
    {
        public PreviewWindow(PreviewViewModel vm)
        {
            DataContext = vm ?? throw new ArgumentNullException(nameof(vm));
            Ui.ApplyWindowDefaults(this, Uz.PreviewTitle, 1100, 760);
            MinWidth = 760;
            MinHeight = 520;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            vm.CloseRequested += _ => Close();

            var root = new DockPanel();

            var header = Ui.Header(Uz.PreviewTitle, vm.HeaderText);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            var footer = Ui.Footer(BuildButtons(vm));
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var body = new Grid { Margin = new Thickness(12) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Page image
            var imageHost = new Grid();
            var image = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(10) };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image.SetBinding(System.Windows.Controls.Image.SourceProperty, new Binding(nameof(PreviewViewModel.Image)) { Mode = BindingMode.OneWay });
            var page = new Border
            {
                Background = Brushes.White,
                BorderBrush = Palette.CardBorder,
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 1, Opacity = 0.18 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = image
            };
            page.SetBinding(VisibilityProperty, new Binding(nameof(PreviewViewModel.HasImage)) { Mode = BindingMode.OneWay, Converter = new BooleanToVisibilityConverter() });
            imageHost.Children.Add(page);

            var noImage = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var noImageText = Ui.BoundText(nameof(PreviewViewModel.ImageMessage), Palette.Muted);
            noImageText.TextAlignment = TextAlignment.Center;
            noImageText.MaxWidth = 420;
            noImage.Children.Add(noImageText);
            var openBtn = Ui.Button(Uz.PreviewOpenExternal, vm.OpenExternalCommand);
            openBtn.Margin = new Thickness(0, 12, 0, 0);
            openBtn.HorizontalAlignment = HorizontalAlignment.Center;
            noImage.Children.Add(openBtn);
            noImage.SetBinding(VisibilityProperty, new Binding(nameof(PreviewViewModel.HasNoImage)) { Mode = BindingMode.OneWay, Converter = new BooleanToVisibilityConverter() });
            imageHost.Children.Add(noImage);

            var imageCard = new Border
            {
                Background = Palette.Hex("#E5E7EB"),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 12, 0),
                Child = imageHost
            };
            body.Children.Add(imageCard);

            // Information
            var info = new StackPanel();
            AddInfo(info, "Fayl", Path.GetFileName(vm.PdfPath));
            AddInfo(info, "Kutilgan qog‘oz", vm.PaperText);
            AddInfo(info, "PDF sahifasi", vm.PageText);
            AddInfo(info, "Fayl hajmi", vm.FileSizeText);
            AddInfo(info, "Usul", vm.MethodText);
            AddInfo(info, "Plot", vm.PlanText);

            var checksTitle = Ui.SectionTitle("Tekshiruv");
            checksTitle.Margin = new Thickness(0, 12, 0, 6);
            info.Children.Add(checksTitle);
            foreach (string c in vm.Checks)
            {
                info.Children.Add(new TextBlock
                {
                    Text = c,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2),
                    Foreground = c.StartsWith("✓", StringComparison.Ordinal) ? Palette.SuccessText
                        : c.StartsWith("⚠", StringComparison.Ordinal) ? Palette.WarningText
                        : c.StartsWith("✖", StringComparison.Ordinal) ? Palette.DangerText
                        : Palette.Text
                });
            }

            var external = Ui.Button(Uz.PreviewOpenExternal, vm.OpenExternalCommand);
            external.Margin = new Thickness(0, 14, 0, 0);
            external.HorizontalAlignment = HorizontalAlignment.Left;
            info.Children.Add(external);

            var infoCard = Ui.Card("Ma’lumot", new ScrollViewer { Content = info, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, new Thickness(0));
            Grid.SetColumn(infoCard, 1);
            body.Children.Add(infoCard);

            root.Children.Add(body);
            Content = root;

            Loaded += (s, e) => Keyboard.Focus(this);
        }

        private static void AddInfo(Panel panel, string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            panel.Children.Add(new TextBlock { Text = label, Foreground = Palette.Muted, FontSize = 11.5, Margin = new Thickness(0, 6, 0, 0) });
            panel.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Foreground = Palette.Text });
        }

        private static UIElement BuildButtons(PreviewViewModel vm)
        {
            var panel = new DockPanel();

            var cancel = Ui.Button("✕  " + Uz.PreviewCancel, vm.CancelCommand, ButtonKind.Danger, Uz.PreviewCancelHint);
            cancel.Margin = new Thickness(0);
            cancel.IsCancel = true;
            DockPanel.SetDock(cancel, Dock.Left);
            panel.Children.Add(cancel);

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var confirm = Ui.Button("✓  " + vm.ConfirmText, vm.ConfirmCommand, ButtonKind.Secondary, Uz.PreviewConfirmHint);
            right.Children.Add(confirm);
            var all = Ui.Button("✓✓  " + Uz.PreviewConfirmAll, vm.ConfirmAllCommand, ButtonKind.Primary, Uz.PreviewConfirmAllHint);
            all.IsDefault = vm.HasRemaining;
            if (!vm.HasRemaining)
            {
                all.Visibility = Visibility.Collapsed;
                confirm.IsDefault = true;
            }
            right.Children.Add(all);
            panel.Children.Add(right);
            return panel;
        }
    }
}
