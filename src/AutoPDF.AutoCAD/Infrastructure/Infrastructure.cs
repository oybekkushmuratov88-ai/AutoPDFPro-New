using System;
using System.Collections.Generic;
using System.Globalization;
using AutoPDF.Core.Logging;
using Autodesk.AutoCAD.ApplicationServices;
using AcCoreApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

namespace AutoPDF.AutoCAD.Infrastructure
{
    /// <summary>An operation failed; <see cref="Step"/> names it exactly (e.g. "plot:set-window-area").</summary>
    public sealed class PlotStepException : Exception
    {
        public PlotStepException(string step, string message, Exception inner = null)
            : base(message, inner)
        {
            Step = step;
        }

        public string Step { get; }
    }

    public static class AcadErrors
    {
        /// <summary>"eInvalidInput — Wrong input" for AutoCAD exceptions, the message otherwise.</summary>
        public static string Describe(Exception ex)
        {
            if (ex == null) return string.Empty;
            if (ex is AcException ae)
            {
                string msg = ae.Message;
                string status = ae.ErrorStatus.ToString();
                return string.IsNullOrEmpty(msg) || msg == status ? status : status + " — " + msg;
            }
            if (ex is PlotStepException pse) return pse.Message;
            return ex.GetType().Name + ": " + ex.Message;
        }
    }

    /// <summary>
    /// Sets system variables for the duration of an operation and restores the previous values
    /// (in reverse order) even when the operation fails.
    /// </summary>
    public sealed class SysVarScope : IDisposable
    {
        private readonly LoggingService _log;
        private readonly List<KeyValuePair<string, object>> _saved = new List<KeyValuePair<string, object>>();

        public SysVarScope(LoggingService log)
        {
            _log = log;
        }

        public static object Get(string name)
        {
            try { return AcCoreApp.GetSystemVariable(name); }
            catch { return null; }
        }

        public static int GetInt(string name, int fallback = 0)
        {
            object v = Get(name);
            try { return v == null ? fallback : Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public bool Set(string name, object value)
        {
            try
            {
                object old = AcCoreApp.GetSystemVariable(name);
                if (Equals(old, value)) return true;
                AcCoreApp.SetSystemVariable(name, value);
                _saved.Add(new KeyValuePair<string, object>(name, old));
                _log?.Debug("SysVar " + name + ": " + old + " → " + value);
                return true;
            }
            catch (Exception ex)
            {
                _log?.Warn("SysVar " + name + " o‘rnatilmadi: " + AcadErrors.Describe(ex));
                return false;
            }
        }

        public void Dispose()
        {
            for (int i = _saved.Count - 1; i >= 0; i--)
            {
                try
                {
                    AcCoreApp.SetSystemVariable(_saved[i].Key, _saved[i].Value);
                }
                catch (Exception ex)
                {
                    _log?.Warn("SysVar " + _saved[i].Key + " tiklanmadi: " + AcadErrors.Describe(ex));
                }
            }
            _saved.Clear();
        }
    }

    /// <summary>Writes log entries to the AutoCAD command line of the active document.</summary>
    public sealed class EditorEcho : IDisposable
    {
        private readonly LoggingService _log;
        private readonly Document _doc;

        public EditorEcho(LoggingService log, Document doc)
        {
            _log = log;
            _doc = doc;
            _log.EntryWritten += OnEntry;
        }

        private void OnEntry(LogEntry e)
        {
            if (e.Level == LogLevel.Debug) return;
            try
            {
                string prefix = e.Level == LogLevel.Error ? "AutoPDF XATO: " : e.Level == LogLevel.Warning ? "AutoPDF ogohlantirish: " : "AutoPDF: ";
                string sheet = e.Sheet.HasValue ? "[List " + e.Sheet.Value.ToString("00", CultureInfo.InvariantCulture) + "] " : string.Empty;
                string step = e.Level >= LogLevel.Warning && !string.IsNullOrEmpty(e.Step) ? "[" + e.Step + "] " : string.Empty;
                string message = e.Message ?? string.Empty;
                int newline = message.IndexOf('\n');
                if (newline > 0) message = message.Substring(0, newline); // no stack traces on the command line
                _doc.Editor.WriteMessage("\n" + prefix + sheet + step + message);
            }
            catch
            {
                // the command line is best effort
            }
        }

        public void Dispose() => _log.EntryWritten -= OnEntry;
    }
}
