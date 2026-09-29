using System;
using System.IO;

namespace AutoPDF.Core.Registration
{
    /// <summary>
    /// Optionally remembers the admin key on the author's own computer (a single line in
    /// %APPDATA%\AutoPDFPro\admin.txt) so the user list can be opened without retyping it. The key is only
    /// ever entered by the author and checked on the server; it is never part of the distributed plug-in.
    /// </summary>
    public sealed class AdminKeyStore
    {
        public AdminKeyStore(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public string FilePath { get; }

        public static string DefaultFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoPDFPro",
                "admin.txt");

        public string Load()
        {
            try { return File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : string.Empty; }
            catch { return string.Empty; }
        }

        public void Save(string key)
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, (key ?? string.Empty).Trim());
            }
            catch { /* remembering the key is a convenience; ignore failures */ }
        }

        public void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch { /* ignore */ }
        }
    }
}
