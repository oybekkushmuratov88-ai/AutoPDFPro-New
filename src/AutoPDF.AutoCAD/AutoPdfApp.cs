using System;
using System.IO;
using System.Reflection;
using AutoPDF.AutoCAD.Infrastructure;
using AutoPDF.Core.Logging;
using Autodesk.AutoCAD.Runtime;
using AcCoreApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(AutoPDF.AutoCAD.AutoPdfApp))]
[assembly: CommandClass(typeof(AutoPDF.AutoCAD.Commands.AutoPdfCommands))]

namespace AutoPDF.AutoCAD
{
    /// <summary>Plug-in entry point: sets up logging and dependency loading from the bundle folder.</summary>
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
                Log.Info("AutoPDF Pro " + Version + " yuklandi: " + PluginDirectory +
                         " | AutoCAD " + SafeSysVar("ACADVER") + ", til " + SafeSysVar("LOCALE") +
                         " | .NET " + Environment.Version);

                var doc = AcCoreApp.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage("\nAutoPDF Pro v" + Version + " yuklandi. Buyruq: AUTOPDF  (diagnostika: AUTOPDFMEDIA, jurnal: AUTOPDFLOG)\n");
            }
            catch
            {
                // Never let initialisation fail the load of the plug-in.
            }
        }

        public void Terminate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        /// <summary>
        /// AutoCAD loads the plug-in with LoadFrom; this makes sure our own dependencies (Core, UI, PdfSharp)
        /// always come from the bundle folder, even if another add-in has loaded a different copy.
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name;
                if (!name.StartsWith("AutoPDF.", StringComparison.OrdinalIgnoreCase) &&
                    !name.Equals("PdfSharp", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

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
