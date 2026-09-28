using System.Globalization;
using System.Text;
using System.Windows.Media;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Models;
using AutoPDF.Core.Paper;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Main
{
    /// <summary>One row of the "detected sheets" table: # | Blok | Eni | Bo‘yi | Qog‘oz | Holat.</summary>
    public sealed class SheetRowViewModel : ObservableObject
    {
        public SheetRowViewModel(SheetModel model)
        {
            Model = model;
        }

        public SheetModel Model { get; }

        public int Number => Model.Number;
        public string BlockName => Model.BlockName;
        public string WidthText => FormatUnits(Model.Width);
        public string HeightText => FormatUnits(Model.Height);
        public string PaperText => Model.Paper != null ? Model.Paper.DisplayText : "?";
        public string StatusText => Uz.StatusText(Model.Status);

        public Brush StatusForeground
        {
            get
            {
                switch (Model.Status)
                {
                    case SheetStatus.Ready:
                    case SheetStatus.Done: return Palette.SuccessText;
                    case SheetStatus.ReadyWithCrop:
                    case SheetStatus.Plotting:
                    case SheetStatus.Waiting: return Palette.InfoText;
                    case SheetStatus.Warning:
                    case SheetStatus.DoneWithWarning: return Palette.WarningText;
                    case SheetStatus.Error:
                    case SheetStatus.Failed: return Palette.DangerText;
                    default: return Palette.NeutralText;
                }
            }
        }

        public Brush StatusBackground
        {
            get
            {
                switch (Model.Status)
                {
                    case SheetStatus.Ready:
                    case SheetStatus.Done: return Palette.SuccessBack;
                    case SheetStatus.ReadyWithCrop:
                    case SheetStatus.Plotting:
                    case SheetStatus.Waiting: return Palette.InfoBack;
                    case SheetStatus.Warning:
                    case SheetStatus.DoneWithWarning: return Palette.WarningBack;
                    case SheetStatus.Error:
                    case SheetStatus.Failed: return Palette.DangerBack;
                    default: return Palette.NeutralBack;
                }
            }
        }

        public string ToolTipText
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("List ").Append(Model.Number.ToString(CultureInfo.InvariantCulture));
                if (Model.Paper != null)
                {
                    sb.Append("\nFormat: ").Append(Model.Paper.FormatName ?? "nostandart");
                    sb.Append("\nQog‘oz: ").Append(Model.Paper.Paper);
                    sb.Append("\nMasshtab: ").Append(Model.Paper.ScaleText);
                    if (!Model.Paper.IsValid && !string.IsNullOrEmpty(Model.Paper.Problem)) sb.Append("\n").Append(Model.Paper.Problem);
                }
                if (!string.IsNullOrEmpty(Model.Layer)) sb.Append("\nQatlam: ").Append(Model.Layer);
                if (!string.IsNullOrEmpty(Model.PlanDescription)) sb.Append("\nPlot: ").Append(Model.PlanDescription);
                if (!string.IsNullOrEmpty(Model.StatusDetail)) sb.Append("\n").Append(Model.StatusDetail);
                if (!string.IsNullOrEmpty(Model.OutputPath)) sb.Append("\nFayl: ").Append(Model.OutputPath);
                sb.Append("\nHandle: ").Append(Model.Handle);
                return sb.ToString();
            }
        }

        public void Refresh() => RaiseAll();

        private static string FormatUnits(double value) => PaperSize.FormatMm(value);
    }
}
