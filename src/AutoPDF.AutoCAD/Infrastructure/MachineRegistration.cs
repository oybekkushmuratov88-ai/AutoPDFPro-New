using System;
using Microsoft.Win32;

namespace AutoPDF.AutoCAD.Infrastructure
{
    /// <summary>
    /// The machine-wide registration written by Setup.exe to HKLM\Software\AutoPDFPro. When it is present the
    /// plug-in treats this computer as registered, so AUTOPDF runs straight away without its own registration
    /// window. Read-only here (the plug-in is not elevated); the per-user "delivered" flag lives in
    /// %APPDATA% instead.
    /// </summary>
    public sealed class MachineRegistration
    {
        public const string KeyPath = @"Software\AutoPDFPro";

        public bool Registered { get; private set; }
        public bool Delivered { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string AtUtc { get; private set; } = string.Empty;

        /// <summary>The marker, or null when the installer did not register this machine.</summary>
        public static MachineRegistration Read()
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey key = baseKey.OpenSubKey(KeyPath))
                {
                    if (key == null) return null;
                    if (ToInt(key.GetValue("Registered")) != 1) return null;
                    return new MachineRegistration
                    {
                        Registered = true,
                        Delivered = ToInt(key.GetValue("RegDelivered")) == 1,
                        Email = ToStr(key.GetValue("RegEmail")),
                        Name = ToStr(key.GetValue("RegName")),
                        Phone = ToStr(key.GetValue("RegPhone")),
                        AtUtc = ToStr(key.GetValue("RegAtUtc"))
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        private static int ToInt(object value)
        {
            try { return value == null ? 0 : Convert.ToInt32(value); }
            catch { return 0; }
        }

        private static string ToStr(object value) => Convert.ToString(value) ?? string.Empty;
    }
}
