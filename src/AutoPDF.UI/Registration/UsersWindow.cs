using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Registration
{
    /// <summary>Admin window showing the registered users returned by the web app.</summary>
    public sealed class UsersWindow : Window
    {
        public UsersWindow(UsersViewModel vm)
        {
            DataContext = vm;
            Ui.ApplyWindowDefaults(this, L.AdminUsersTitle + " — " + L.AppTitle, 940, 560);
            MinWidth = 720;
            MinHeight = 420;
            ResizeMode = ResizeMode.CanResize;
            Closing += (s, e) => { if (vm.Busy) e.Cancel = true; };

            var root = new DockPanel();

            Border header = Ui.Header(L.AdminUsersTitle, L.AdminUsersSubtitle);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            root.Children.Add(BuildFooter(vm));   // Dock.Bottom
            root.Children.Add(BuildQueryBar(vm));  // Dock.Top
            root.Children.Add(BuildList());        // fills the rest

            Content = root;
        }

        private static UIElement BuildQueryBar(UsersViewModel vm)
        {
            var bar = new Grid { Margin = new Thickness(16, 12, 16, 6) };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = Ui.Label(L.AdminKey + ": ");
            label.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(label, 0);
            bar.Children.Add(label);

            TextBox key = Ui.Input("Key");
            key.MinWidth = 220;
            Grid.SetColumn(key, 1);
            bar.Children.Add(key);

            CheckBox remember = Ui.Check(L.AdminRemember, "Remember");
            remember.Margin = new Thickness(12, 0, 12, 0);
            Grid.SetColumn(remember, 2);
            bar.Children.Add(remember);

            Button show = Ui.Button(L.AdminShow, vm.ShowCommand, ButtonKind.Primary);
            show.IsDefault = true;
            show.SetBinding(IsEnabledProperty, new Binding("NotBusy") { Mode = BindingMode.OneWay });
            Grid.SetColumn(show, 3);
            bar.Children.Add(show);

            DockPanel.SetDock(bar, Dock.Top);
            return bar;
        }

        private static UIElement BuildList()
        {
            var grid = new GridView();
            grid.Columns.Add(Column(L.ColNo, "Number", 44));
            grid.Columns.Add(Column(L.ColDate, "Date", 140));
            grid.Columns.Add(Column(L.ColName, "FullName", 170));
            grid.Columns.Add(Column(L.ColEmail, "Email", 200));
            grid.Columns.Add(Column(L.ColPhone, "Phone", 130));
            grid.Columns.Add(Column(L.ColComputer, "Computer", 130));
            grid.Columns.Add(Column(L.ColWindows, "WindowsUser", 110));
            grid.Columns.Add(Column(L.ColVersion, "Version", 70));

            var list = new ListView
            {
                View = grid,
                Margin = new Thickness(16, 0, 16, 8),
                BorderBrush = Palette.CardBorder,
                Background = Palette.Card
            };
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Users") { Mode = BindingMode.OneWay });
            return list;
        }

        private static GridViewColumn Column(string header, string path, double width) => new GridViewColumn
        {
            Header = header,
            DisplayMemberBinding = new Binding(path),
            Width = width
        };

        private UIElement BuildFooter(UsersViewModel vm)
        {
            var status = new TextBlock { Foreground = Palette.Muted, VerticalAlignment = VerticalAlignment.Center };
            status.SetBinding(TextBlock.TextProperty, new Binding("Status") { Mode = BindingMode.OneWay });

            Button close = Ui.Button(L.Close, new RelayCommand(Close), ButtonKind.Secondary);
            close.IsCancel = true;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(status);
            Grid.SetColumn(close, 1);
            grid.Children.Add(close);

            Border footer = Ui.Footer(grid);
            DockPanel.SetDock(footer, Dock.Bottom);
            return footer;
        }
    }
}
