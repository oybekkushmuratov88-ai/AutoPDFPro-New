using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AutoPDF.Core.Logging;

namespace AutoPDF.Core.Settings
{
    /// <summary>Loads and saves <see cref="PluginSettings"/>. A damaged file is backed up and replaced by defaults.</summary>
    public sealed class SettingsService
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(PluginSettings));
        private readonly LoggingService _log;

        public SettingsService(string filePath, LoggingService log = null)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _log = log;
        }

        public string FilePath { get; }

        public static string DefaultFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoPDFPro",
                "settings.xml");

        public PluginSettings Load()
        {
            if (!File.Exists(FilePath))
            {
                _log?.Info("Sozlamalar fayli yo‘q — standart sozlamalar ishlatiladi: " + FilePath);
                return new PluginSettings().Normalize();
            }

            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var settings = (PluginSettings)Serializer.Deserialize(stream);
                    return (settings ?? new PluginSettings()).Normalize();
                }
            }
            catch (Exception ex)
            {
                string backup = FilePath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                try { File.Copy(FilePath, backup, true); } catch { /* best effort */ }
                _log?.Warn("Sozlamalar fayli o‘qilmadi (" + ex.Message + "). Standart sozlamalar olindi, eski fayl: " + backup);
                return new PluginSettings().Normalize();
            }
        }

        public void Save(PluginSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Normalize();
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string temp = FilePath + ".tmp";
                var xmlSettings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
                using (var stream = File.Create(temp))
                using (var writer = XmlWriter.Create(stream, xmlSettings))
                {
                    Serializer.Serialize(writer, settings);
                }

                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception ex)
            {
                _log?.Warn("Sozlamalarni saqlab bo‘lmadi: " + ex.Message);
            }
        }
    }
}
