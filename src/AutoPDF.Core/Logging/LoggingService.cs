using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoPDF.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public sealed class LogEntry
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        /// <summary>Sheet number the entry refers to, or null.</summary>
        public int? Sheet { get; set; }

        /// <summary>Exact operation, e.g. "plot:set-window-area".</summary>
        public string Step { get; set; }

        public string Format()
        {
            var sb = new StringBuilder();
            sb.Append(Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            sb.Append(' ').Append(LevelText(Level));
            if (Sheet.HasValue) sb.Append(" [").Append(Localization.L.Sheet).Append(' ').Append(Sheet.Value.ToString("00", CultureInfo.InvariantCulture)).Append(']');
            if (!string.IsNullOrEmpty(Step)) sb.Append(" [").Append(Step).Append(']');
            sb.Append(' ').Append(Message);
            return sb.ToString();
        }

        private static string LevelText(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return "[DEBUG]";
                case LogLevel.Info: return "[INFO ]";
                case LogLevel.Warning: return "[WARN ]";
                default: return "[ERROR]";
            }
        }
    }

    /// <summary>
    /// File log (one file per day in %LOCALAPPDATA%\AutoPDFPro\Logs) plus an event that the AutoCAD
    /// command line subscribes to. Every sheet error is written with its sheet number and the exact step.
    /// </summary>
    public sealed class LoggingService
    {
        private const int KeepDays = 30;
        private readonly object _sync = new object();
        private readonly List<LogEntry> _sessionProblems = new List<LogEntry>();

        public LoggingService(string logDirectory, bool verbose = true)
        {
            LogDirectory = logDirectory;
            Verbose = verbose;
            LogFilePath = string.IsNullOrEmpty(logDirectory)
                ? null
                : Path.Combine(logDirectory, "AutoPDFPro_" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
            TryCleanup();
        }

        public static string DefaultLogDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoPDFPro",
                "Logs");

        public string LogDirectory { get; }
        public string LogFilePath { get; }
        public bool Verbose { get; set; }

        /// <summary>Raised for every written entry (Debug entries only when <see cref="Verbose"/>).</summary>
        public event Action<LogEntry> EntryWritten;

        /// <summary>Warnings and errors since <see cref="ResetSession"/>.</summary>
        public IReadOnlyList<LogEntry> SessionProblems
        {
            get { lock (_sync) return _sessionProblems.ToList(); }
        }

        public void ResetSession()
        {
            lock (_sync) _sessionProblems.Clear();
        }

        public void Debug(string message) => Write(LogLevel.Debug, message, null, null);
        public void Info(string message) => Write(LogLevel.Info, message, null, null);
        public void Warn(string message) => Write(LogLevel.Warning, message, null, null);
        public void Error(string message, Exception ex = null) => Write(LogLevel.Error, WithException(message, ex), null, null);

        public void SheetDebug(int sheet, string step, string message) => Write(LogLevel.Debug, message, sheet, step);
        public void SheetInfo(int sheet, string step, string message) => Write(LogLevel.Info, message, sheet, step);
        public void SheetWarn(int sheet, string step, string message) => Write(LogLevel.Warning, message, sheet, step);
        public void SheetError(int sheet, string step, string message, Exception ex = null) =>
            Write(LogLevel.Error, WithException(message, ex), sheet, step);

        public void Write(LogLevel level, string message, int? sheet, string step)
        {
            if (level == LogLevel.Debug && !Verbose) return;

            var entry = new LogEntry { Time = DateTime.Now, Level = level, Message = message ?? string.Empty, Sheet = sheet, Step = step };
            lock (_sync)
            {
                if (level >= LogLevel.Warning) _sessionProblems.Add(entry);
                AppendToFile(entry.Format());
            }

            try { EntryWritten?.Invoke(entry); }
            catch { /* a broken sink must never break plotting */ }
        }

        private static string WithException(string message, Exception ex)
        {
            if (ex == null) return message;
            var sb = new StringBuilder(message);
            sb.Append(" | ").Append(ex.GetType().FullName).Append(": ").Append(ex.Message);
            for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
            {
                sb.Append(" | inner ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
            }
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine().Append(ex.StackTrace);
            }
            return sb.ToString();
        }

        private void AppendToFile(string line)
        {
            if (LogFilePath == null) return;
            try
            {
                Directory.CreateDirectory(LogDirectory);
                using (var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
                // Logging must never throw into AutoCAD.
            }
        }

        private void TryCleanup()
        {
            if (string.IsNullOrEmpty(LogDirectory)) return;
            try
            {
                if (!Directory.Exists(LogDirectory)) return;
                DateTime limit = DateTime.Now.AddDays(-KeepDays);
                foreach (string file in Directory.GetFiles(LogDirectory, "AutoPDFPro_*.log"))
                {
                    if (File.GetLastWriteTime(file) < limit) File.Delete(file);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
