using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Licensing
{
    /// <summary>
    /// A small notification in the bottom-right corner ("access approved" / "not approved") that does not take
    /// the focus from the drawing and disappears after a few seconds (or on click).
    /// </summary>
    public sealed class LicenseToast : Window
    {
        private LicenseToast(string text, bool good)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowActivated = false;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            FontFamily = Ui.Font;
            FontSize = 13;

            var stack = new StackPanel { MaxWidth = 380 };
            stack.Children.Add(new TextBlock { Text = L.AppTitle, FontWeight = FontWeights.SemiBold, Foreground = good ? Palette.SuccessText : Palette.DangerText });
            stack.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Palette.Text, Margin = new Thickness(0, 4, 0, 0) });

            Content = new Border
            {
                Background = good ? Palette.SuccessBack : Palette.DangerBack,
                BorderBrush = good ? Palette.SuccessText : Palette.DangerText,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(8),
                Child = stack
            };

            MouseLeftButtonUp += (s, e) => Close();
            Loaded += (s, e) =>
            {
                Rect area = SystemParameters.WorkArea;
                Left = area.Right - ActualWidth - 12;
                Top = area.Bottom - ActualHeight - 12;
            };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
            timer.Tick += (s, e) => { timer.Stop(); Close(); };
            timer.Start();
        }

        /// <summary>Shows the notification; never throws (a notification must not disturb the CAD session).</summary>
        public static void Show(string text, bool good)
        {
            try
            {
                new LicenseToast(text, good).Show();
            }
            catch
            {
                // the command line message is enough
            }
        }
    }
}
