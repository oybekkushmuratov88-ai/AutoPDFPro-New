using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoPDF.Core.Hosting;

namespace AutoPDF.Cad.Infrastructure
{
    /// <summary>
    /// Which CAD application and version the plug-in runs in, which one this assembly was built for, and the
    /// few services whose API differs between the hosts.
    /// </summary>
    public static class HostEnvironment
    {
        private static HostInfo _host;

        /// <summary>What this assembly was compiled for (set by the host project's symbols).</summary>
        public static CadBuildTarget Build { get; } =
#if ZWCAD_2024
            new CadBuildTarget(CadProduct.ZWCAD, 2024, 2024, "AutoPDF.ZWCAD.2024");
#elif ZWCAD_2025
            new CadBuildTarget(CadProduct.ZWCAD, 2025, 2025, "AutoPDF.ZWCAD.2025");
#elif ZWCAD_2026
            new CadBuildTarget(CadProduct.ZWCAD, 2026, 2026, "AutoPDF.ZWCAD.2026");
#elif ACAD_R25
            new CadBuildTarget(CadProduct.AutoCAD, 2025, 2026, "AutoPDF.AutoCAD.R25");
#else
            new CadBuildTarget(CadProduct.AutoCAD, 2021, 2024, "AutoPDF.AutoCAD (R24)");
#endif

        /// <summary>The running host, detected once.</summary>
        public static HostInfo Host => _host ?? (_host = Detect());

        public static HostCompatibility Compatibility => HostCatalog.Check(Host, Build);

        private static HostInfo Detect()
        {
            var info = new HostInfo { Runtime = HostCatalog.RuntimeDescription() };
            try { info.ProcessName = Process.GetCurrentProcess().ProcessName; } catch { /* informative only */ }

#if ZWCAD
            info.Product = CadProduct.ZWCAD;
            try
            {
                // ZwManaged.dll as loaded by ZWCAD itself: 24.x = 2024, 25.x = 2025, 26.x = 2026.
                Version api = typeof(CadApp).Assembly.GetName().Version;
                info.Release = api.Major + "." + api.Minor;
                info.Year = HostCatalog.ZwcadYear(api.Major);
            }
            catch
            {
                // unknown version: Compatibility reports Unverified
            }
#else
            info.Product = CadProduct.AutoCAD;
            // ACADVER = "24.3s (LMS Tech)": R24.3 = AutoCAD 2024.
            string acadver = Convert.ToString(SysVarScope.Get("ACADVER"));
            if (!HostCatalog.TryParseRelease(acadver, out int major, out int minor))
            {
                try
                {
                    Version v = typeof(CadCoreApp).Assembly.GetName().Version;
                    major = v.Major;
                    minor = v.Minor;
                }
                catch
                {
                    major = minor = 0;
                }
            }
            if (major > 0)
            {
                info.Release = "R" + major + "." + minor;
                info.Year = HostCatalog.AutoCadYear(major, minor);
            }
#endif
            return info;
        }

        /// <summary>Forgets an Esc pressed before the batch started (ZWCAD reads the key state directly).</summary>
        public static void ResetUserBreak()
        {
#if ZWCAD
            try { GetAsyncKeyState(VkEscape); } catch { /* best effort */ }
#endif
        }

        /// <summary>True when the user pressed Esc (checked between sheets).</summary>
        public static bool UserBreak()
        {
#if ZWCAD
            // ZWCAD's .NET API has no HostApplicationServices.UserBreak(): read Esc from the keyboard, but only
            // while ZWCAD itself is in the foreground. (Cancel in the plot progress dialog works in both hosts.)
            try
            {
                IntPtr fg = GetForegroundWindow();
                if (fg == IntPtr.Zero) return false;
                GetWindowThreadProcessId(fg, out uint pid);
                if (pid != (uint)Process.GetCurrentProcess().Id) return false;
                return (GetAsyncKeyState(VkEscape) & 0x8001) != 0;
            }
            catch
            {
                return false;
            }
#else
            return HostApplicationServices.Current.UserBreak();
#endif
        }

#if ZWCAD
        private const int VkEscape = 0x1B;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
#endif
    }
}
