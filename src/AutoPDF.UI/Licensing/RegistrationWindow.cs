using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Licensing
{
    /// <summary>
    /// First-launch registration: name, e-mail, the installation ID (generated, read-only, copyable) and the
    /// consent tick. Sending the form requests approval; it does not unlock anything by itself.
    /// </summary>
    public sealed class RegistrationWindow : Window
    {
        private readonly RegistrationViewModel _vm;

        public RegistrationWindow(RegistrationViewModel vm)
        {
            _vm = vm;
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, L.RegTitle + " — " + L.AppTitle, 580, 600);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            vm.RequestClose += Close;
            // Keep the window (and the request in flight) open until the server has answered.
            Closing += (s, e) => { if (_vm.Busy) e.Cancel = true; };

            var root = new DockPanel();
            Border header = Ui.Header(L.RegTitle, L.RegSubtitle);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            root.Children.Add(BuildFooter());
            root.Children.Add(BuildBody());
            Content = root;
        }

        private UIElement BuildBody()
        {
            var body = new StackPanel { Margin = new Thickness(24, 18, 24, 8) };
            body.Children.Add(new TextBlock
            {
                Text = L.RegIntro,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Palette.Text,
                Margin = new Thickness(0, 0, 0, 14)
            });

            var form = Ui.FormGrid(150);
            Ui.AddRow(form, L.RegName, Enabled(Ui.Input("FullName")));
            Ui.AddRow(form, L.RegEmail, Enabled(Ui.Input("Email")));
            Ui.AddRow(form, L.LicInstallId, InstallIdRow(_vm.CopyIdCommand));
            body.Children.Add(form);

            CheckBox consent = Ui.Check(L.RegConsent, "Consent");
            consent.Margin = new Thickness(0, 14, 0, 2);
            consent.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            body.Children.Add(consent);

            var error = new TextBlock { Foreground = Palette.Danger, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
            error.SetBinding(TextBlock.TextProperty, Bind("Error"));
            error.SetBinding(VisibilityProperty, VisibilityOf("Error"));
            body.Children.Add(error);

            var busy = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            busy.SetBinding(VisibilityProperty, VisibilityOf("Busy"));
            busy.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 120, Height = 6, VerticalAlignment = VerticalAlignment.Center });
            busy.Children.Add(new TextBlock { Text = L.RegSending, Foreground = Palette.Muted, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            body.Children.Add(busy);
            return body;
        }

        /// <summary>Read-only installation ID with a Copy button (also used by the status window).</summary>
        internal static UIElement InstallIdRow(System.Windows.Input.ICommand copy)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBox id = Ui.Input("InstallId", readOnly: true);
            id.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            grid.Children.Add(id);
            Button button = Ui.Button(L.LicCopy, copy);
            button.MinWidth = 0;
            button.Padding = new Thickness(10, 4, 10, 4);
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);
            return grid;
        }

        private UIElement BuildFooter()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button cancel = Ui.Button(L.Cancel, _vm.CancelCommand);
            cancel.IsCancel = true;
            cancel.HorizontalAlignment = HorizontalAlignment.Left;
            cancel.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            grid.Children.Add(cancel);

            Button register = Ui.Button(L.RegSubmit, _vm.RegisterCommand, ButtonKind.Primary);
            register.IsDefault = true;
            register.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            Grid.SetColumn(register, 1);
            grid.Children.Add(register);

            Border footer = Ui.Footer(grid);
            DockPanel.SetDock(footer, Dock.Bottom);
            return footer;
        }

        private static TextBox Enabled(TextBox input)
        {
            input.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            return input;
        }

        internal static Binding Bind(string path) => new Binding(path) { Mode = BindingMode.OneWay };

        internal static Binding VisibilityOf(string path) => new Binding(path)
        {
            Mode = BindingMode.OneWay,
            Converter = TruthyToVisibilityConverter.Instance
        };
    }
}
