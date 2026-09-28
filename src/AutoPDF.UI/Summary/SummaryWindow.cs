using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Models;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Services;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Summary
{
    /// <summary>Final report: totals, every failed sheet with the exact step, open folder / log.</summary>
    public sealed class SummaryWindow : Window
    {
        public SummaryWindow(ExportSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            Ui.ApplyWindowDefaults(this, Uz.SummaryTitle, 760, 560);
            MinWidth = 560;
            MinHeight = 400;

            var root = new DockPanel();

            string subtitle = summary.Failed == 0 && summary.Cancelled == 0
                ? "Barcha listlar muvaffaqiyatli yaratildi."
                : "Ba’zi listlar yaratilmadi — quyidagi ro‘yxatni ko‘ring.";
            var header = Ui.Header(Uz.SummaryTitle, subtitle);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(Ui.Button(Uz.OpenLog, new RelayCommand(() => Shell.OpenFile(summary.LogFile, out _), () => File.Exists(summary.LogFile ?? string.Empty))));
            buttons.Children.Add(Ui.Button(Uz.OpenFolder, new RelayCommand(() => Shell.OpenFolder(summary.OutputFolder, out _), () => Directory.Exists(summary.OutputFolder ?? string.Empty))));
            var close = Ui.Button(Uz.Close, new RelayCommand(Close), ButtonKind.Primary);
            close.IsDefault = true;
            close.IsCancel = true;
            buttons.Children.Add(close);
            var footer = Ui.Footer(buttons);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var body = new DockPanel { Margin = new Thickness(12) };

            var tiles = new UniformGrid6();
            tiles.Add(Uz.SummaryTotal, summary.Total, Palette.NeutralBack, Palette.Text);
            tiles.Add(Uz.SummarySuccess, summary.Succeeded, Palette.SuccessBack, Palette.SuccessText);
            tiles.Add(Uz.SummaryWarnings, summary.WithWarnings, Palette.WarningBack, Palette.WarningText);
            tiles.Add(Uz.SummaryFailed, summary.Failed, Palette.DangerBack, Palette.DangerText);
            tiles.Add(Uz.SummarySkipped, summary.Skipped, Palette.NeutralBack, Palette.NeutralText);
            tiles.Add(Uz.SummaryCancelled, summary.Cancelled, Palette.NeutralBack, Palette.NeutralText);
            DockPanel.SetDock(tiles.Panel, Dock.Top);
            body.Children.Add(tiles.Panel);

            var folderText = Ui.Hint("Papka: " + summary.OutputFolder + "\nJurnal: " + summary.LogFile, 8);
            DockPanel.SetDock(folderText, Dock.Top);
            body.Children.Add(folderText);

            var list = new ListBox
            {
                Margin = new Thickness(0, 10, 0, 0),
                BorderBrush = Palette.CardBorder,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 12
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
            foreach (string line in ProblemLines(summary)) list.Items.Add(line);
            if (list.Items.Count == 0) list.Items.Add("✓ Xato yo‘q.");
            body.Children.Add(Ui.Card(Uz.SummaryErrors, list, new Thickness(0, 10, 0, 0)));

            root.Children.Add(body);
            Content = root;
        }

        public static IEnumerable<string> ProblemLines(ExportSummary summary)
        {
            foreach (SheetResult r in summary.Results.OrderBy(r => r.Number))
            {
                string name = string.IsNullOrEmpty(r.OutputPath) ? string.Empty : " (" + Path.GetFileName(r.OutputPath) + ")";
                string n = r.Number.ToString("00", CultureInfo.InvariantCulture);
                switch (r.Outcome)
                {
                    case SheetOutcome.Failed:
                        yield return "✖ List " + n + name + " — [" + (r.FailedStep ?? "?") + "] " + r.Error;
                        break;
                    case SheetOutcome.Cancelled:
                        yield return "– List " + n + name + " — bekor qilindi";
                        break;
                    case SheetOutcome.Skipped:
                        yield return "– List " + n + name + " — o‘tkazildi" + (string.IsNullOrEmpty(r.Error) ? string.Empty : ": " + r.Error);
                        break;
                }
                foreach (string w in r.Warnings)
                {
                    yield return "⚠ List " + n + name + " — " + w;
                }
            }
        }

        /// <summary>Row of six small number tiles.</summary>
        private sealed class UniformGrid6
        {
            public readonly System.Windows.Controls.Primitives.UniformGrid Panel =
                new System.Windows.Controls.Primitives.UniformGrid { Columns = 6, Rows = 1 };

            public void Add(string label, int value, Brush background, Brush foreground)
            {
                var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stack.Children.Add(new TextBlock
                {
                    Text = value.ToString(CultureInfo.InvariantCulture),
                    FontSize = 24,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = foreground,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stack.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 11.5,
                    Foreground = Palette.Muted,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
                Panel.Children.Add(new Border
                {
                    Background = background,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(6, 8, 6, 8),
                    Margin = new Thickness(3),
                    Child = stack
                });
            }
        }
    }
}
