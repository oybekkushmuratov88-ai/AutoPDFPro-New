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
    public enum DialogKind
    {
        Info,
        Warning,
        Error,
        YesNo,
        YesNoCancel
    }

    public enum DialogAnswer
    {
        Ok,
        Yes,
        No,
        Cancel
    }

    /// <summary>
    /// Message box whose buttons follow the plug-in language (the Windows MessageBox would always show the
    /// Windows language). Can be owned by a WPF window or by a native handle such as AutoCAD's main window.
    /// </summary>
    public sealed class MessageDialog : Window
    {
        private DialogAnswer _answer = DialogAnswer.Cancel;

        private MessageDialog(string message, string title, DialogKind kind)
        {
            Ui.ApplyWindowDefaults(this, string.IsNullOrEmpty(title) ? L.AppTitle : title, 540, 240);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            MinHeight = 180;

            string glyph;
            Brush glyphBrush;
            switch (kind)
            {
                case DialogKind.Error: glyph = "✖"; glyphBrush = Palette.Danger; break;
                case DialogKind.Warning: glyph = "⚠"; glyphBrush = Palette.WarningText; break;
                case DialogKind.YesNo:
                case DialogKind.YesNoCancel: glyph = "?"; glyphBrush = Palette.Primary; break;
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
                case DialogKind.YesNo:
                    buttons.Children.Add(MakeButton(L.Yes, DialogAnswer.Yes, ButtonKind.Primary, isDefault: true));
                    buttons.Children.Add(MakeButton(L.No, DialogAnswer.No, ButtonKind.Secondary, isCancel: true));
                    break;
                case DialogKind.YesNoCancel:
                    buttons.Children.Add(MakeButton(L.Yes, DialogAnswer.Yes, ButtonKind.Primary, isDefault: true));
                    buttons.Children.Add(MakeButton(L.No, DialogAnswer.No, ButtonKind.Secondary));
                    buttons.Children.Add(MakeButton(L.Cancel, DialogAnswer.Cancel, ButtonKind.Secondary, isCancel: true));
                    break;
                default:
                    buttons.Children.Add(MakeButton(L.Ok, DialogAnswer.Ok, ButtonKind.Primary, isDefault: true, isCancel: true));
                    break;
            }

            var root = new DockPanel();
            var footer = Ui.Footer(buttons);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);
            root.Children.Add(body);
            Content = root;
        }

        private Button MakeButton(string text, DialogAnswer answer, ButtonKind kind, bool isDefault = false, bool isCancel = false)
        {
            var b = Ui.Button(text, new RelayCommand(() =>
            {
                _answer = answer;
                Close();
            }), kind);
            b.IsDefault = isDefault;
            b.IsCancel = isCancel;
            return b;
        }

        public static DialogAnswer Show(Window owner, string message, string title = null, DialogKind kind = DialogKind.Info)
        {
            var dlg = new MessageDialog(message, title, kind);
            if (owner != null && owner.IsLoaded) dlg.Owner = owner;
            else dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dlg.ShowDialog();
            return dlg._answer;
        }

        public static DialogAnswer Show(IntPtr owner, string message, string title = null, DialogKind kind = DialogKind.Info)
        {
            var dlg = new MessageDialog(message, title, kind);
            if (owner != IntPtr.Zero)
            {
                new WindowInteropHelper(dlg).Owner = owner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            dlg.ShowDialog();
            return dlg._answer;
        }
    }
}
