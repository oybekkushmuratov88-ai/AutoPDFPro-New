using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Registration
{
    /// <summary>
    /// First-run registration: name, e-mail, optional phone, a panel showing exactly what is sent to the
    /// author, and a consent tick. Registration is required, so the window returns Registered or Cancelled.
    /// </summary>
    public sealed class RegistrationWindow : Window
    {
        private readonly RegistrationViewModel _vm;

        public RegistrationWindow(RegistrationViewModel vm)
        {
            _vm = vm;
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, L.RegTitle + " — " + L.AppTitle, 560, 600);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            vm.RequestClose += Close;
            // Don't let the window be closed while a submission is in flight (keeps the gateway alive).
            Closing += (s, e) => { if (_vm.Busy) e.Cancel = true; };

            var root = new DockPanel();

            Border header = Ui.Header(L.RegTitle, L.RegSubtitle);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            root.Children.Add(BuildFooter());   // Dock.Bottom is set inside
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

            var form = Ui.FormGrid(160);
            Ui.AddRow(form, L.RegName, Field(Ui.Input("FullName")));
            Ui.AddRow(form, L.RegEmail, Field(Ui.Input("Email")));
            Ui.AddRow(form, L.RegPhone, Field(Ui.Input("Phone")));
            body.Children.Add(form);

            body.Children.Add(WhatIsSentCard());

            CheckBox consent = Ui.Check(L.RegConsent, "Consent");
            consent.Margin = new Thickness(0, 12, 0, 2);
            consent.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            body.Children.Add(consent);

            // Error message (red), shown only when there is one.
            var error = new TextBlock
            {
                Foreground = Palette.Danger,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };
            error.SetBinding(TextBlock.TextProperty, Bind("Error"));
            error.SetBinding(VisibilityProperty, VisibilityOf("Error"));
            body.Children.Add(error);

            // "Send later and continue" explanation, shown alongside the offline button.
            var offlineNote = new TextBlock
            {
                Foreground = Palette.Muted,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            };
            offlineNote.SetBinding(TextBlock.TextProperty, Bind("OfflineNote"));
            offlineNote.SetBinding(VisibilityProperty, VisibilityOf("ShowOfflineOption"));
            body.Children.Add(offlineNote);

            // Busy row: an indeterminate bar plus "Sending…".
            var busy = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            busy.SetBinding(VisibilityProperty, VisibilityOf("Busy"));
            busy.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 120, Height = 6, VerticalAlignment = VerticalAlignment.Center });
            busy.Children.Add(new TextBlock { Text = L.RegSending, Foreground = Palette.Muted, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            body.Children.Add(busy);

            return body;
        }

        private Border WhatIsSentCard()
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = L.RegWhatIsSent,
                Foreground = Palette.Muted,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            });
            content.Children.Add(FactLine(L.RegFieldComputer, _vm.Computer));
            content.Children.Add(FactLine(L.RegFieldUser, _vm.WindowsUser));
            content.Children.Add(FactLine(L.RegFieldVersion, _vm.Version));

            return new Border
            {
                Background = Palette.NeutralBack,
                BorderBrush = Palette.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 14, 0, 0),
                Child = content
            };
        }

        private static UIElement FactLine(string label, string value)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            line.Children.Add(new TextBlock { Text = label + ": ", Foreground = Palette.Muted, FontSize = 12 });
            line.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(value) ? "—" : value, Foreground = Palette.Text, FontSize = 12, FontWeight = FontWeights.SemiBold });
            return line;
        }

        private UIElement BuildFooter()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button cancel = Ui.Button(L.Cancel, _vm.CancelCommand, ButtonKind.Secondary);
            cancel.IsCancel = true;
            cancel.HorizontalAlignment = HorizontalAlignment.Left;
            cancel.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            grid.Children.Add(cancel);

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            Button offline = Ui.Button(L.RegOfflineContinue, _vm.ContinueOfflineCommand, ButtonKind.Secondary);
            offline.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            offline.SetBinding(VisibilityProperty, VisibilityOf("ShowOfflineOption"));
            right.Children.Add(offline);

            Button register = Ui.Button(L.RegSubmit, _vm.RegisterCommand, ButtonKind.Primary);
            register.IsDefault = true;
            register.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            right.Children.Add(register);

            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            Border footer = Ui.Footer(grid);
            DockPanel.SetDock(footer, Dock.Bottom);
            return footer;
        }

        private static TextBox Field(TextBox input)
        {
            input.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            return input;
        }

        private static Binding Bind(string path) => new Binding(path) { Mode = BindingMode.OneWay };

        private static Binding VisibilityOf(string path) => new Binding(path)
        {
            Mode = BindingMode.OneWay,
            Converter = TruthyToVisibilityConverter.Instance
        };
    }
}
