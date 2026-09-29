using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace AutoPDF.Core.Registration
{
    /// <summary>
    /// One machine's registration, stored as XML in %APPDATA%\AutoPDFPro\registration.xml. It holds what the
    /// user typed (name, e-mail, optional phone) and the facts about this computer that are sent to the author,
    /// plus whether the server has already received it (<see cref="Delivered"/>).
    /// </summary>
    [XmlRoot("AutoPdfRegistration")]
    public sealed class RegistrationInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string Computer { get; set; } = string.Empty;
        public string WindowsUser { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;

        /// <summary>UTC time the user completed the form, ISO 8601 (e.g. 2026-09-29T05:12:00Z).</summary>
        public string RegisteredAtUtc { get; set; } = string.Empty;

        /// <summary>The server has confirmed receipt at least once.</summary>
        public bool Delivered { get; set; }

        /// <summary>How many times sending was attempted (for the log; not a hard limit).</summary>
        public int Attempts { get; set; }

        public string LastError { get; set; } = string.Empty;

        /// <summary>The user has finished the form (valid e-mail + a completion time). Used to gate AUTOPDF.</summary>
        [XmlIgnore]
        public bool HasCompleted =>
            EmailValidator.IsValid(Email) && !string.IsNullOrWhiteSpace(RegisteredAtUtc);
    }

    /// <summary>A lenient e-mail check — enough to catch typos, never stricter than real addresses.</summary>
    public static class EmailValidator
    {
        private static readonly Regex Pattern =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsValid(string email) =>
            !string.IsNullOrWhiteSpace(email) && email.Length <= 254 && Pattern.IsMatch(email.Trim());
    }
}
