using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AutoPDF.Core.Logging;

namespace AutoPDF.Core.Registration
{
    /// <summary>Loads and saves the machine's <see cref="RegistrationInfo"/> (XML, next to settings.xml).</summary>
    public sealed class RegistrationStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(RegistrationInfo));
        private readonly LoggingService _log;

        public RegistrationStore(string filePath, LoggingService log = null)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _log = log;
        }

        public string FilePath { get; }

        public static string DefaultFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoPDFPro",
                "registration.xml");

        /// <summary>The stored registration, or null when the file is missing or unreadable.</summary>
        public RegistrationInfo Load()
        {
            if (!File.Exists(FilePath)) return null;
            try
            {
                using (FileStream stream = File.OpenRead(FilePath))
                {
                    return (RegistrationInfo)Serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                _log?.Warn("registration.xml could not be read: " + ex.Message);
                return null;
            }
        }

        public void Save(RegistrationInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string temp = FilePath + ".tmp";
                var xmlSettings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
                using (FileStream stream = File.Create(temp))
                using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                {
                    Serializer.Serialize(writer, info);
                }

                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception ex)
            {
                _log?.Warn("registration.xml could not be saved: " + ex.Message);
            }
        }
    }
}
