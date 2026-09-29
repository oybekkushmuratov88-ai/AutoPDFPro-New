using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoPDF.UI.Mvvm
{
    /// <summary>
    /// Shows an element when the bound value is "truthy": true, a non-empty string, or any non-null object.
    /// Use <c>ConverterParameter=Hidden</c> to reserve the space instead of collapsing.
    /// </summary>
    public sealed class TruthyToVisibilityConverter : IValueConverter
    {
        public static readonly TruthyToVisibilityConverter Instance = new TruthyToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool truthy;
            switch (value)
            {
                case null: truthy = false; break;
                case bool b: truthy = b; break;
                case string s: truthy = !string.IsNullOrEmpty(s); break;
                default: truthy = true; break;
            }

            if (truthy) return Visibility.Visible;
            return string.Equals(parameter as string, "Hidden", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Hidden
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
