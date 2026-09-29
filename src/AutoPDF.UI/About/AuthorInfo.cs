using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.About
{
    /// <summary>The author's photo (embedded in AutoPDF.UI.dll) and a round avatar element.</summary>
    public static class AuthorInfo
    {
        public const string PhotoResource = "AutoPDF.UI.Assets.author.jpg";

        private static BitmapSource _photo;
        private static bool _loaded;

        public static string Name => AppInfo.Author;

        /// <summary>"KO" for "Kushmuratov Oybek" — shown if the photo cannot be loaded.</summary>
        public static string Initials =>
            new string(Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])).ToArray());

        public static BitmapSource Photo
        {
            get
            {
                if (_loaded) return _photo;
                _loaded = true;
                try
                {
                    using (Stream stream = typeof(AuthorInfo).Assembly.GetManifestResourceStream(PhotoResource))
                    {
                        if (stream == null) return null;
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        _photo = image;
                    }
                }
                catch
                {
                    _photo = null;
                }
                return _photo;
            }
        }

        /// <summary>Round photo with a coloured ring (initials on a blue disc if the photo is missing).</summary>
        public static FrameworkElement Avatar(double size, double ring, Brush ringBrush, bool shadow = false)
        {
            var grid = new Grid { Width = size, Height = size, SnapsToDevicePixels = true };
            BitmapSource photo = Photo;
            if (photo != null)
            {
                var circle = new Ellipse
                {
                    Fill = new ImageBrush(photo) { Stretch = Stretch.UniformToFill },
                    Stroke = ringBrush,
                    StrokeThickness = ring
                };
                RenderOptions.SetBitmapScalingMode(circle, BitmapScalingMode.HighQuality);
                grid.Children.Add(circle);
            }
            else
            {
                grid.Children.Add(new Ellipse { Fill = Palette.Primary, Stroke = ringBrush, StrokeThickness = ring });
                grid.Children.Add(new TextBlock
                {
                    Text = Initials,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = Math.Max(10, size * 0.36),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            if (shadow)
            {
                grid.Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.28 };
            }
            return grid;
        }
    }
}
