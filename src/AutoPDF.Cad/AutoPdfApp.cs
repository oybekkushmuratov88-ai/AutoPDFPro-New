using System;
using System.IO;
using System.Reflection;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Cad.Licensing;
using AutoPDF.Core.Hosting;
using AutoPDF.Core.Logging;
#if ZWCAD
using ZwSoft.ZwCAD.Runtime;
#else
using Autodesk.AutoCAD.Runtime;
#endif
using AutoPDF.Core.Localization;
using AutoPDF.Core.Settings;

[assembly: ExtensionApplication(typeof(AutoPDF.Cad.AutoPdfApp))]
[assembly: CommandClass(typeof(AutoPDF.Cad.Commands.AutoPdfCommands))]

namespace AutoPDF.Cad
{
    /// <summary>
    /// Plug-in entry point (AutoCAD and ZWCAD): sets up logging and dependency loading from the plug-in folder,
    /// detects the host application and version and checks that this build matches it.
    /// </summary>
    public sealed class AutoPdfApp : IExtensionApplication
    {
        private static LoggingService _log;

        public static string PluginDirectory { get; } =
            Path.GetDirectoryName(typeof(AutoPdfApp).Assembly.Location) ?? string.Empty;

        public static string Version { get; } =
            typeof(AutoPdfApp).Assembly.GetName().Version.ToString(3);

        public static LoggingService Log => _log ?? (_log = new LoggingService(LoggingService.DefaultLogDirectory));

        public void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            try
            {
                // The start-up message already uses the language chosen in the settings.
                L.Current = new SettingsService(SettingsService.DefaultFilePath).Load().Language;
                HostInfo host = HostEnvironment.Host;
                L.HostName = host.DisplayName;
                Log.Info(L.LoadedLog(Version, PluginDirectory) +
                         " | " + host + ", ACADVER " + SafeSysVar("ACADVER") + ", LOCALE " + SafeSysVar("LOCALE") +
                         " | build: " + HostEnvironment.Build.AssemblyLabel + " for " + HostEnvironment.Build.Describe());

                var doc = CadCoreApp.DocumentManager.MdiActiveDocument;
                switch (HostEnvironment.Compatibility)
                {
                    case HostCompatibility.WrongBuild:
                        Log.Error(L.HostWrongBuild(HostEnvironment.Build.Describe(), host.DisplayName).Trim());
                        doc?.Editor.WriteMessage(L.HostWrongBuild(HostEnvironment.Build.Describe(), host.DisplayName));
                        return;
                    case HostCompatibility.Unverified:
                        Log.Warn(L.HostUnverified(host.DisplayName));
                        break;
                }
                doc?.Editor.WriteMessage(L.LoadedMessage(Version));
                LicenseGate.StartPeriodicChecks();
            }
            catch
            {
                // Never let initialisation fail the load of the plug-in.
            }
        }

        public void Terminate()
        {
            LicenseGate.Stop();
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        /// <summary>
        /// The CAD host loads the plug-in with LoadFrom; this makes sure our own dependencies (Core, Pdf, UI,
        /// PdfSharp and, on .NET 8, the Windows SDK projection) come from the plug-in folder, even if another
        /// add-in has loaded a different copy. Only files that exist in our folder are ever resolved.
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(name) || name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;

                string path = Path.Combine(PluginDirectory, name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string SafeSysVar(string name) => Convert.ToString(SysVarScope.Get(name)) ?? "?";
    }
}
