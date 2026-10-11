using System;
using System.Collections.Generic;
using System.Linq;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Cad.Plotting;
using AutoPDF.Cad.Workflow;
using AutoPDF.Core.Hosting;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Paper;
using AutoPDF.Core.Registration;
using AutoPDF.Core.Settings;
using AutoPDF.UI.Registration;
using AutoPDF.UI.Services;
#if ZWCAD
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif

namespace AutoPDF.Cad.Commands
{
    /// <summary>
    /// The plug-in's commands — the same names in AutoCAD and ZWCAD. Global (English) names; in a localized
    /// CAD they are typed the same way (or with the "_" prefix: _AUTOPDF).
    /// </summary>
    public sealed class AutoPdfCommands
    {
        private static IntPtr AcadWindow
        {
            get
            {
                try { return CadApp.MainWindow.Handle; }
                catch { return IntPtr.Zero; }
            }
        }

        /// <summary>Main command: opens the AutoPDF Pro window.</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDF", CommandFlags.Modal)]
        public void AutoPdf()
        {
            Document doc = CadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            if (HostEnvironment.Compatibility == HostCompatibility.WrongBuild)
            {
                doc.Editor.WriteMessage(L.HostWrongBuild(HostEnvironment.Build.Describe(), HostEnvironment.Host.DisplayName));
                return;
            }

            try
            {
                new AutoPdfController(doc).Run();
            }
            catch (System.Exception ex)
            {
                AutoPdfApp.Log.Error("AUTOPDF: " + L.UnexpectedError, ex);
                doc.Editor.WriteMessage("\n" + L.EchoError + CadErrors.Describe(ex) + "\n" + L.Log + ": " + AutoPdfApp.Log.LogFilePath + "\n");
                try
                {
                    MessageDialog.Show(IntPtr.Zero, L.UnexpectedError + ": " + CadErrors.Describe(ex) + "\n\n" + L.Log + ": " + AutoPdfApp.Log.LogFilePath,
                        L.AppTitle, DialogKind.Error);
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
            Document doc = CadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var log = AutoPdfApp.Log;
                PluginSettings settings = new SettingsService(SettingsService.DefaultFilePath, log).Load();
                L.Current = settings.Language;
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

                string text = "\n===== " + L.MediaDiagTitle + " =====\n";
                text += L.Plotters + ": " + string.Join(", ", media.GetDevices()) + "\n";
                if (media.DeviceExists(settings.Plotter))
                {
                    text += media.Describe(media.CanonicalDeviceName(settings.Plotter), sizes, settings);
                }
                else
                {
                    text += "✖ " + L.PlotterNotFound(settings.Plotter) + "\n";
                }

                if (!string.IsNullOrWhiteSpace(settings.CustomSizesPlotter) && media.DeviceExists(settings.CustomSizesPlotter))
                {
                    text += L.CustomPlotterPresent(settings.CustomSizesPlotter) + "\n";
                }
                else
                {
                    text += L.CustomPlotterMissing(settings.CustomSizesPlotter) + "\n";
                }

                foreach (string note in new PageSetupService(doc, log).CheckEnvironment(settings, media, plotSettings))
                {
                    text += note + "\n";
                }

                doc.Editor.WriteMessage(text);
                log.Info(text.Replace("\n", Environment.NewLine));
                doc.Editor.WriteMessage(L.Details(log.LogFilePath) + "\n");
            }
            catch (System.Exception ex)
            {
                AutoPdfApp.Log.Error(L.MediaCommandError, ex);
                doc.Editor.WriteMessage("\n" + L.MediaCommandError + CadErrors.Describe(ex) + "\n");
            }
        }

        /// <summary>Admin-only: the list of registered users (protected by the admin key, checked on the server).</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDFADMIN", CommandFlags.Modal)]
        public void AutoPdfAdmin()
        {
            Document doc = CadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var log = AutoPdfApp.Log;
                PluginSettings settings = new SettingsService(SettingsService.DefaultFilePath, log).Load();
                L.Current = settings.Language;

                if (!RegistrationConfig.IsConfigured)
                {
                    MessageDialog.Show(AcadWindow, L.AdminNotConfigured, L.AppTitle, DialogKind.Info);
                    return;
                }

                using (var gateway = new HttpRegistrationGateway(RegistrationConfig.Endpoint, AutoPdfApp.Version))
                {
                    var vm = new UsersViewModel(gateway, new AdminKeyStore(AdminKeyStore.DefaultFilePath));
                    CadApp.ShowModalWindow(AcadWindow, new UsersWindow(vm), false);
                }
            }
            catch (System.Exception ex)
            {
                AutoPdfApp.Log.Error("AUTOPDFADMIN", ex);
                doc.Editor.WriteMessage("\n" + L.EchoError + CadErrors.Describe(ex) + "\n");
            }
        }

        /// <summary>Opens the log folder.</summary>
        [CommandMethod("AUTOPDFPRO", "AUTOPDFLOG", CommandFlags.Modal)]
        public void AutoPdfLog()
        {
            Document doc = CadApp.DocumentManager.MdiActiveDocument;
            string dir = AutoPdfApp.Log.LogDirectory;
            System.IO.Directory.CreateDirectory(dir);
            doc?.Editor.WriteMessage("\n" + L.LogFileIs(AutoPdfApp.Log.LogFilePath) + "\n");
            Shell.OpenFolder(dir, out _);
        }
    }
}
