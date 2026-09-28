using System;
using System.Diagnostics;
using System.IO;

namespace AutoPDF.UI.Services
{
    /// <summary>Opens files and folders with the Windows shell; never throws.</summary>
    public static class Shell
    {
        public static bool OpenFolder(string folder, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    error = "Papka topilmadi: " + folder;
                    return false;
                }
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool SelectFile(string file, out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(file)) return OpenFolder(Path.GetDirectoryName(file), out error);
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + file + "\"") { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool OpenFile(string file, out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(file))
                {
                    error = "Fayl topilmadi: " + file;
                    return false;
                }
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
