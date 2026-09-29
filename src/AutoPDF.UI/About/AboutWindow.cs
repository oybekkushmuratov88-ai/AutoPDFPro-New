using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.About
{
    /// <summary>"About" window: author photo, name, role, product description, version and copyright.</summary>
    public sealed class AboutWindow : Window
    {
        public AboutWindow(string version)
        {
            Ui.ApplyWindowDefaults(this, L.About + " — " + L.AppTitle, 500, 560);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;

            var root = new DockPanel();

            var close = Ui.Button(L.Close, new RelayCommand(Close), ButtonKind.Primary);
            close.IsDefault = true;
            close.IsCancel = true;
            var footer = Ui.Footer(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { close } });
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            // Coloured band; the photo overlaps its lower edge.
            var band = new Border
            {
                Background = Palette.HeaderGradient(),
                Height = 104,
                Child = new TextBlock
                {
                    Text = L.AppTitle,
                    Foreground = Brushes.White,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 16, 0, 0)
                }
            };
            DockPanel.SetDock(band, Dock.Top);
            root.Children.Add(band);

            var body = new StackPanel { Margin = new Thickness(32, 0, 32, 22), HorizontalAlignment = HorizontalAlignment.Stretch };

            FrameworkElement avatar = AuthorInfo.Avatar(136, 4, Brushes.White, shadow: true);
            avatar.Margin = new Thickness(0, -58, 0, 0);
            avatar.HorizontalAlignment = HorizontalAlignment.Center;
            body.Children.Add(avatar);

            body.Children.Add(Centered(AuthorInfo.Name, 22, FontWeights.SemiBold, Palette.Text, new Thickness(0, 14, 0, 2)));
            body.Children.Add(Centered(L.AuthorRole, 13, FontWeights.Normal, Palette.Muted, new Thickness(0, 0, 0, 0)));

            body.Children.Add(new Border { Height = 1, Background = Palette.Divider, Margin = new Thickness(0, 18, 0, 16) });

            body.Children.Add(Centered(L.AboutDescription, 13, FontWeights.Normal, Palette.Text, new Thickness(0, 0, 0, 14)));
            body.Children.Add(Centered(L.AppTitle + " · " + L.VersionText(version ?? string.Empty), 12.5, FontWeights.SemiBold, Palette.Text, new Thickness(0, 0, 0, 2)));
            body.Children.Add(Centered(L.Compatibility, 12, FontWeights.Normal, Palette.Muted, new Thickness(0, 0, 0, 12)));
            body.Children.Add(Centered(L.Copyright, 11.5, FontWeights.Normal, Palette.Muted, new Thickness(0)));

            root.Children.Add(body);
            Content = root;
        }

        private static TextBlock Centered(string text, double size, FontWeight weight, Brush brush, Thickness margin) => new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = brush,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = margin
        };
    }
}
