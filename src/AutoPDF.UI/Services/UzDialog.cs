using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Services
{
    public enum UzDialogKind
    {
        Info,
        Warning,
        Error,
        YesNo,
        YesNoCancel
    }

    public enum UzDialogResult
    {
        Ok,
        Yes,
        No,
        Cancel
    }

    /// <summary>
    /// Message box with Uzbek button captions (the Windows MessageBox would show "Да/Нет" on a Russian
    /// Windows). Can be owned by a WPF window or by a native handle such as AutoCAD's main window.
    /// </summary>
    public sealed class UzDialog : Window
    {
        private UzDialogResult _result = UzDialogResult.Cancel;

        private UzDialog(string message, string title, UzDialogKind kind)
        {
            Ui.ApplyWindowDefaults(this, string.IsNullOrEmpty(title) ? Uz.AppTitle : title, 520, 240);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            MinHeight = 180;

            string glyph;
            Brush glyphBrush;
            switch (kind)
            {
                case UzDialogKind.Error: glyph = "✖"; glyphBrush = Palette.Danger; break;
                case UzDialogKind.Warning: glyph = "⚠"; glyphBrush = Palette.WarningText; break;
                case UzDialogKind.YesNo:
                case UzDialogKind.YesNoCancel: glyph = "?"; glyphBrush = Palette.Primary; break;
                default: glyph = "ℹ"; glyphBrush = Palette.Primary; break;
            }

            var body = new DockPanel { Margin = new Thickness(20, 18, 20, 18) };
            var icon = new TextBlock
            {
                Text = glyph,
                FontSize = 28,
                Foreground = glyphBrush,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            DockPanel.SetDock(icon, Dock.Left);
            body.Children.Add(icon);
            body.Children.Add(new TextBox
            {
                Text = message ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = Palette.Text,
                VerticalAlignment = VerticalAlignment.Center,
                MaxHeight = 420,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            switch (kind)
            {
                case UzDialogKind.YesNo:
                    buttons.Children.Add(MakeButton(Uz.Yes, UzDialogResult.Yes, ButtonKind.Primary, isDefault: true));
                    buttons.Children.Add(MakeButton(Uz.No, UzDialogResult.No, ButtonKind.Secondary, isCancel: true));
                    break;
                case UzDialogKind.YesNoCancel:
                    buttons.Children.Add(MakeButton(Uz.Yes, UzDialogResult.Yes, ButtonKind.Primary, isDefault: true));
                    buttons.Children.Add(MakeButton(Uz.No, UzDialogResult.No, ButtonKind.Secondary));
                    buttons.Children.Add(MakeButton(Uz.Cancel, UzDialogResult.Cancel, ButtonKind.Secondary, isCancel: true));
                    break;
                default:
                    buttons.Children.Add(MakeButton(Uz.Ok, UzDialogResult.Ok, ButtonKind.Primary, isDefault: true, isCancel: true));
                    break;
            }

            var root = new DockPanel();
            var footer = Ui.Footer(buttons);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);
            root.Children.Add(body);
            Content = root;
        }

        private Button MakeButton(string text, UzDialogResult result, ButtonKind kind, bool isDefault = false, bool isCancel = false)
        {
            var b = Ui.Button(text, new RelayCommand(() =>
            {
                _result = result;
                Close();
            }), kind);
            b.IsDefault = isDefault;
            b.IsCancel = isCancel;
            return b;
        }

        public static UzDialogResult Show(Window owner, string message, string title = null, UzDialogKind kind = UzDialogKind.Info)
        {
            var dlg = new UzDialog(message, title, kind);
            if (owner != null && owner.IsLoaded) dlg.Owner = owner;
            else dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dlg.ShowDialog();
            return dlg._result;
        }

        public static UzDialogResult Show(IntPtr owner, string message, string title = null, UzDialogKind kind = UzDialogKind.Info)
        {
            var dlg = new UzDialog(message, title, kind);
            if (owner != IntPtr.Zero)
            {
                new WindowInteropHelper(dlg).Owner = owner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            dlg.ShowDialog();
            return dlg._result;
        }
    }
}
