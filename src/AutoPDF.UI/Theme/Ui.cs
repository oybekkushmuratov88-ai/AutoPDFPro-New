using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoPDF.UI.Theme
{
    /// <summary>Colours of the AutoPDF Pro UI.</summary>
    public static class Palette
    {
        public static readonly Brush WindowBackground = Hex("#F3F5F8");
        public static readonly Brush Card = Brushes.White;
        public static readonly Brush CardBorder = Hex("#DDE3EA");
        public static readonly Brush Text = Hex("#1F2937");
        public static readonly Brush Muted = Hex("#6B7280");
        public static readonly Brush Divider = Hex("#EEF1F5");

        public static readonly Brush Primary = Hex("#2563EB");
        public static readonly Brush PrimaryHover = Hex("#1D4ED8");
        public static readonly Brush PrimaryPressed = Hex("#1E40AF");

        public static readonly Brush Secondary = Hex("#FFFFFF");
        public static readonly Brush SecondaryHover = Hex("#F1F5F9");
        public static readonly Brush SecondaryPressed = Hex("#E2E8F0");
        public static readonly Brush SecondaryBorder = Hex("#CBD5E1");

        public static readonly Brush Danger = Hex("#DC2626");
        public static readonly Brush DangerHover = Hex("#FEF2F2");

        public static readonly Brush HeaderStart = Hex("#1E3A5F");
        public static readonly Brush HeaderEnd = Hex("#2563EB");

        public static readonly Brush SuccessText = Hex("#15803D");
        public static readonly Brush SuccessBack = Hex("#DCFCE7");
        public static readonly Brush WarningText = Hex("#B45309");
        public static readonly Brush WarningBack = Hex("#FEF3C7");
        public static readonly Brush DangerText = Hex("#B91C1C");
        public static readonly Brush DangerBack = Hex("#FEE2E2");
        public static readonly Brush InfoText = Hex("#1D4ED8");
        public static readonly Brush InfoBack = Hex("#DBEAFE");
        public static readonly Brush NeutralText = Hex("#4B5563");
        public static readonly Brush NeutralBack = Hex("#F3F4F6");

        public static Brush Hex(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        public static Brush HeaderGradient()
        {
            var b = new LinearGradientBrush(((SolidColorBrush)HeaderStart).Color, ((SolidColorBrush)HeaderEnd).Color, 0);
            b.Freeze();
            return b;
        }
    }

    public enum ButtonKind
    {
        Primary,
        Secondary,
        Danger
    }

    /// <summary>Small factory for consistently styled controls (the UI is composed in code).</summary>
    public static class Ui
    {
        public static readonly FontFamily Font = new FontFamily("Segoe UI");

        public static void ApplyWindowDefaults(Window w, string title, double width, double height)
        {
            w.Title = title;
            w.Width = width;
            w.Height = height;
            w.Background = Palette.WindowBackground;
            w.FontFamily = Font;
            w.FontSize = 13;
            w.Foreground = Palette.Text;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowInTaskbar = false;
            w.UseLayoutRounding = true;
            w.SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(w, TextFormattingMode.Display);
        }

        public static Button Button(string text, ICommand command, ButtonKind kind = ButtonKind.Secondary, string tooltip = null)
        {
            var b = new Button
            {
                Content = text,
                Command = command,
                Padding = new Thickness(16, 7, 16, 7),
                Margin = new Thickness(6, 0, 0, 0),
                MinWidth = 96,
                Cursor = Cursors.Hand,
                FontWeight = kind == ButtonKind.Primary ? FontWeights.SemiBold : FontWeights.Normal,
                Focusable = true
            };
            if (!string.IsNullOrEmpty(tooltip)) b.ToolTip = tooltip;

            switch (kind)
            {
                case ButtonKind.Primary:
                    b.Foreground = Brushes.White;
                    b.Template = ButtonTemplate(Palette.Primary, Palette.PrimaryHover, Palette.PrimaryPressed, Palette.Primary);
                    break;
                case ButtonKind.Danger:
                    b.Foreground = Palette.Danger;
                    b.Template = ButtonTemplate(Palette.Secondary, Palette.DangerHover, Palette.DangerBack, Palette.Danger);
                    break;
                default:
                    b.Foreground = Palette.Text;
                    b.Template = ButtonTemplate(Palette.Secondary, Palette.SecondaryHover, Palette.SecondaryPressed, Palette.SecondaryBorder);
                    break;
            }
            return b;
        }

        private static ControlTemplate ButtonTemplate(Brush background, Brush hover, Brush pressed, Brush border)
        {
            var template = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border), "bd");
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            bd.SetValue(Border.BackgroundProperty, background);
            bd.SetValue(Border.BorderBrushProperty, border);
            bd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            bd.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            bd.AppendChild(content);
            template.VisualTree = bd;

            var over = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            over.Setters.Add(new Setter(Border.BackgroundProperty, hover, "bd"));
            template.Triggers.Add(over);

            var press = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            press.Setters.Add(new Setter(Border.BackgroundProperty, pressed, "bd"));
            template.Triggers.Add(press);

            var focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
            focus.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "bd"));
            template.Triggers.Add(focus);

            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabled);

            return template;
        }

        /// <summary>White rounded panel with a bold title.</summary>
        public static Border Card(object header, UIElement content, Thickness? margin = null)
        {
            var stack = new DockPanel { LastChildFill = true };
            if (header != null)
            {
                UIElement headerElement = header as UIElement ?? SectionTitle(header.ToString());
                DockPanel.SetDock(headerElement, Dock.Top);
                stack.Children.Add(headerElement);
            }
            stack.Children.Add(content);

            return new Border
            {
                Background = Palette.Card,
                BorderBrush = Palette.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = margin ?? new Thickness(0, 0, 0, 10),
                Child = stack
            };
        }

        public static TextBlock SectionTitle(string text) => new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13.5,
            Foreground = Palette.Text,
            Margin = new Thickness(0, 0, 0, 8)
        };

        public static TextBlock Label(string text, double top = 0) => new TextBlock
        {
            Text = text,
            Foreground = Palette.Text,
            Margin = new Thickness(0, top, 0, 4),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        public static TextBlock Hint(string text, double top = 4) => new TextBlock
        {
            Text = text,
            Foreground = Palette.Muted,
            FontSize = 11.5,
            Margin = new Thickness(0, top, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };

        public static TextBlock BoundText(string path, Brush foreground = null, double fontSize = 13, FontWeight? weight = null)
        {
            var tb = new TextBlock
            {
                Foreground = foreground ?? Palette.Text,
                FontSize = fontSize,
                FontWeight = weight ?? FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.SetBinding(TextBlock.TextProperty, new Binding(path) { Mode = BindingMode.OneWay });
            return tb;
        }

        public static RadioButton Radio(string text, string path, string group, string tooltip = null)
        {
            var r = new RadioButton
            {
                Content = text,
                GroupName = group,
                Margin = new Thickness(0, 3, 0, 3),
                Foreground = Palette.Text,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            if (tooltip != null) r.ToolTip = tooltip;
            r.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Mode = BindingMode.TwoWay });
            return r;
        }

        public static CheckBox Check(string text, string path, string tooltip = null)
        {
            var c = new CheckBox
            {
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                Margin = new Thickness(0, 4, 0, 4),
                Foreground = Palette.Text,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            if (tooltip != null) c.ToolTip = tooltip;
            c.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Mode = BindingMode.TwoWay });
            return c;
        }

        public static TextBox Input(string path, bool readOnly = false, string tooltip = null)
        {
            var t = new TextBox
            {
                Padding = new Thickness(6, 4, 6, 4),
                BorderBrush = Palette.SecondaryBorder,
                IsReadOnly = readOnly,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = readOnly ? Palette.NeutralBack : Brushes.White
            };
            if (tooltip != null) t.ToolTip = tooltip;
            t.SetBinding(TextBox.TextProperty, new Binding(path)
            {
                Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            return t;
        }

        /// <summary>Combo box over OptionItem values: ItemsSource = <paramref name="itemsPath"/>, SelectedValue = <paramref name="valuePath"/>.</summary>
        public static ComboBox OptionCombo(string itemsPath, string valuePath)
        {
            var c = new ComboBox
            {
                DisplayMemberPath = "Text",
                SelectedValuePath = "Value",
                Padding = new Thickness(6, 4, 6, 4),
                MinWidth = 180
            };
            c.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath) { Mode = BindingMode.OneWay });
            c.SetBinding(Selector.SelectedValueProperty, new Binding(valuePath) { Mode = BindingMode.TwoWay });
            return c;
        }

        /// <summary>Editable combo (free text + suggestions), e.g. plotter or CTB names.</summary>
        public static ComboBox EditableCombo(string itemsPath, string textPath)
        {
            var c = new ComboBox { IsEditable = true, IsTextSearchEnabled = true, Padding = new Thickness(6, 4, 6, 4), MinWidth = 180 };
            c.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath) { Mode = BindingMode.OneWay });
            c.SetBinding(ComboBox.TextProperty, new Binding(textPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return c;
        }

        /// <summary>Two-column "label: control" grid row helper.</summary>
        public static void AddRow(Grid grid, string label, UIElement control, string hint = null)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var l = Label(label);
            l.Margin = new Thickness(0, 6, 12, 6);
            Grid.SetRow(l, row);
            Grid.SetColumn(l, 0);
            grid.Children.Add(l);

            FrameworkElement content = control as FrameworkElement;
            if (hint != null)
            {
                var sp = new StackPanel();
                sp.Children.Add(control);
                sp.Children.Add(Hint(hint, 2));
                content = sp;
            }
            if (content != null) content.Margin = new Thickness(0, 4, 0, 4);
            Grid.SetRow(content ?? control, row);
            Grid.SetColumn(content ?? control, 1);
            grid.Children.Add(content ?? control);
        }

        public static Grid FormGrid(double labelWidth = 230)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return g;
        }

        public static Border Header(string title, string subtitle, UIElement right = null)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titles.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.SemiBold });
            if (!string.IsNullOrEmpty(subtitle))
                titles.Children.Add(new TextBlock { Text = subtitle, Foreground = Palette.Hex("#DBEAFE"), FontSize = 12 });
            grid.Children.Add(titles);

            if (right != null)
            {
                Grid.SetColumn(right, 1);
                grid.Children.Add(right);
            }

            return new Border
            {
                Background = Palette.HeaderGradient(),
                Padding = new Thickness(20, 10, 20, 10),
                MinHeight = 60,
                Child = grid
            };
        }

        public static Border Footer(UIElement content) => new Border
        {
            Background = Brushes.White,
            BorderBrush = Palette.CardBorder,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(14, 10, 14, 10),
            Child = content
        };
    }
}
