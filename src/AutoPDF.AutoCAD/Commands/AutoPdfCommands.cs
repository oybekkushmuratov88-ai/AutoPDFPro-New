using System;
using System.Collections.Generic;
using System.Linq;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.AutoCAD.Plotting;
using AutoPDF.AutoCAD.Workflow;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Settings;
using AutoPDF.UI.Services;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoPDF.AutoCAD.Commands
{
    /// <summary>
    /// AutoCAD commands. Global (English) names; in the Russian AutoCAD they are typed the same way
    /// (or with the "_" prefix: _AUTOPDF).
    /// </summary>
    public sealed class AutoPdfCommands
    {
        /// <summary>Main command: opens the AutoPDF Pro window.</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDF", CommandFlags.Modal)]
        public void AutoPdf()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                new AutoPdfController(doc).Run();
            }
            catch (System.Exception ex)
            {
                AutoPdfApp.Log.Error("AUTOPDF kutilmagan xato", ex);
                doc.Editor.WriteMessage("\nAutoPDF XATO: " + AcadErrors.Describe(ex) + "\nJurnal: " + AutoPdfApp.Log.LogFilePath + "\n");
                try
                {
                    UzDialog.Show(IntPtr.Zero, "Kutilmagan xato: " + AcadErrors.Describe(ex) + "\n\nJurnal: " + AutoPdfApp.Log.LogFilePath,
                        Uz.AppTitle, UzDialogKind.Error);
                }
                catch
                {
                    // command line message is enough
                }
            }
        }

        /// <summary>Diagnostics: plotter papers, the paper chosen for typical sheet sizes, plot style status.</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDFMEDIA", CommandFlags.Modal)]
        public void AutoPdfMedia()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var log = AutoPdfApp.Log;
                PluginSettings settings = new SettingsService(SettingsService.DefaultFilePath, log).Load();
                CustomPaperSizeManager.ClearCache();
                var media = new CustomPaperSizeManager(log);
                var plotSettings = new PlotSettingsService(log);

                var sizes = new List<PaperSize>
                {
                    new PaperSize(210, 297), new PaperSize(297, 210), new PaperSize(297, 420), new PaperSize(420, 297),
                    new PaperSize(420, 594), new PaperSize(594, 841), new PaperSize(841, 1189),
                    new PaperSize(630, 297), new PaperSize(891, 420), new PaperSize(1260, 594)
                };
                foreach (PaperFormat f in PaperFormatCatalog.ParseCustom(settings.CustomFormats))
                {
                    PaperSize p = f.Oriented(true);
                    if (!sizes.Any(s => s.ApproximatelyEquals(p, 0.1))) sizes.Add(p);
                }

                string text = "\n===== AutoPDF Pro — plotter diagnostikasi =====\n";
                text += "Plotterlar: " + string.Join(", ", media.GetDevices()) + "\n";
                if (media.DeviceExists(settings.Plotter))
                {
                    text += media.Describe(media.CanonicalDeviceName(settings.Plotter), sizes, settings);
                }
                else
                {
                    text += "✖ “" + settings.Plotter + "” plotteri topilmadi.\n";
                }

                if (!string.IsNullOrWhiteSpace(settings.CustomSizesPlotter) && media.DeviceExists(settings.CustomSizesPlotter))
                {
                    text += "Maxsus o‘lchamlar plotteri mavjud: " + settings.CustomSizesPlotter + "\n";
                }
                else
                {
                    text += "Maxsus o‘lchamlar plotteri (“" + settings.CustomSizesPlotter + "”) o‘rnatilmagan — nostandart listlar kesish usulida chiqariladi.\n";
                }

                foreach (string note in new PageSetupService(doc, log).CheckEnvironment(settings, media, plotSettings))
                {
                    text += note + "\n";
                }

                doc.Editor.WriteMessage(text);
                log.Info(text.Replace("\n", Environment.NewLine));
                doc.Editor.WriteMessage("Batafsil: " + log.LogFilePath + "\n");
            }
            catch (System.Exception ex)
            {
                AutoPdfApp.Log.Error("AUTOPDFMEDIA xato", ex);
                doc.Editor.WriteMessage("\nAUTOPDFMEDIA xato: " + AcadErrors.Describe(ex) + "\n");
            }
        }

        /// <summary>Opens the log folder.</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDFLOG", CommandFlags.Modal)]
        public void AutoPdfLog()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            string dir = AutoPdfApp.Log.LogDirectory;
            System.IO.Directory.CreateDirectory(dir);
            doc?.Editor.WriteMessage("\nAutoPDF jurnali: " + AutoPdfApp.Log.LogFilePath + "\n");
            Shell.OpenFolder(dir, out _);
        }
    }
}
