using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AutoPDF.Core.Licensing;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Theme;
using static AutoPDF.UI.Licensing.RegistrationWindow;

namespace AutoPDF.UI.Licensing
{
    /// <summary>
    /// The persistent licence screen: shown by AUTOPDF for as long as the installation is not approved
    /// (pending, rejected, needs verification). It checks with the server when it opens and then every
    /// minute while it stays open; PDF creation stays disabled until the server's signed approval arrives.
    /// </summary>
    public sealed class LicenseStatusWindow : Window
    {
        private readonly LicenseStatusViewModel _vm;
        private readonly DispatcherTimer _timer;

        /// <param name="checkOnOpen">Ask the server right away (the caller has not just done so).</param>
        public LicenseStatusWindow(LicenseStatusViewModel vm, bool checkOnOpen = true)
        {
            _vm = vm;
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, L.LicTitle + " — " + L.AppTitle, 600, 560);
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            vm.RequestClose += Close;

            var root = new DockPanel();
            Border header = Ui.Header(L.LicTitle, L.AppSubtitle);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            root.Children.Add(BuildFooter());
            root.Children.Add(BuildBody());
            Content = root;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _timer.Tick += (s, e) => { if (_vm.Waiting) _ = _vm.CheckAsync(CheckTrigger.Manual); };
            Loaded += (s, e) =>
            {
                _timer.Start();
                if (checkOnOpen && _vm.Waiting) _ = _vm.CheckAsync(CheckTrigger.Command);
            };
            Closed += (s, e) => _timer.Stop();
        }

        private UIElement BuildBody()
        {
            var body = new StackPanel { Margin = new Thickness(24, 18, 24, 8) };

            var badge = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 14) };
            badge.SetBinding(Border.BackgroundProperty, Bind("BadgeBack"));
            var badgeText = new StackPanel();
            var headline = new TextBlock { FontSize = 17, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            headline.SetBinding(TextBlock.TextProperty, Bind("Headline"));
            headline.SetBinding(TextBlock.ForegroundProperty, Bind("BadgeText"));
            badgeText.Children.Add(headline);
            var explanation = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), Foreground = Palette.Text };
            explanation.SetBinding(TextBlock.TextProperty, Bind("Explanation"));
            badgeText.Children.Add(explanation);
            badge.Child = badgeText;
            body.Children.Add(badge);

            var form = Ui.FormGrid(190);
            Ui.AddRow(form, L.LicStatus, Text("StateName"));
            Ui.AddRow(form, L.RegName, Text("Name"));
            Ui.AddRow(form, L.RegEmail, Text("Email"));
            Ui.AddRow(form, L.LicInstallId, InstallIdRow(_vm.CopyIdCommand));
            Ui.AddRow(form, L.LicSubmitted, Text("Submitted"));
            Ui.AddRow(form, L.LicLastOnline, Text("LastOnline"));
            body.Children.Add(form);

            var valid = new TextBlock { Foreground = Palette.Muted, Margin = new Thickness(0, 8, 0, 0) };
            valid.SetBinding(TextBlock.TextProperty, Bind("ValidUntil"));
            valid.SetBinding(VisibilityProperty, VisibilityOf("ValidUntil"));
            body.Children.Add(valid);

            var message = new TextBlock { Foreground = Palette.WarningText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
            message.SetBinding(TextBlock.TextProperty, Bind("Message"));
            message.SetBinding(VisibilityProperty, VisibilityOf("Message"));
            body.Children.Add(message);

            var busy = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            busy.SetBinding(VisibilityProperty, VisibilityOf("Busy"));
            busy.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 120, Height = 6, VerticalAlignment = VerticalAlignment.Center });
            busy.Children.Add(new TextBlock { Text = L.LicChecking, Foreground = Palette.Muted, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            body.Children.Add(busy);
            return body;
        }

        private static TextBlock Text(string path)
        {
            var t = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4) };
            t.SetBinding(TextBlock.TextProperty, Bind(path));
            return t;
        }

        private UIElement BuildFooter()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button close = Ui.Button(L.Close, _vm.CloseCommand);
            close.IsCancel = true;
            close.HorizontalAlignment = HorizontalAlignment.Left;
            grid.Children.Add(close);

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button check = Ui.Button(L.LicCheckNow, _vm.CheckNowCommand);
            check.SetBinding(IsEnabledProperty, Bind("NotBusy"));
            check.SetBinding(VisibilityProperty, VisibilityOf("Waiting"));
            right.Children.Add(check);
            Button go = Ui.Button(L.LicContinue, _vm.ContinueCommand, ButtonKind.Primary);
            go.IsDefault = true;
            go.SetBinding(VisibilityProperty, VisibilityOf("CanContinue"));
            right.Children.Add(go);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            Border footer = Ui.Footer(grid);
            DockPanel.SetDock(footer, Dock.Bottom);
            return footer;
        }
    }
}
