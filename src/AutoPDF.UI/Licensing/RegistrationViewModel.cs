using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoPDF.Core.Licensing;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;

namespace AutoPDF.UI.Licensing
{
    /// <summary>
    /// The first-launch registration form: name, e-mail, the generated installation ID and the consent tick.
    /// The host injects <see cref="Submit"/> (sends the request over HTTPS and returns the server's answer);
    /// nothing is granted here — a successful submission ends in "pending approval". Without internet the
    /// form stays open with an error (no offline registration).
    /// </summary>
    public sealed class RegistrationViewModel : ObservableObject
    {
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _installId;
        private bool _consent;
        private bool _busy;
        private string _error;

        public RegistrationViewModel(string installId)
        {
            _installId = installId ?? string.Empty;
            RegisterCommand = new RelayCommand(() => { if (!Busy) _ = RegisterAsync(); });
            CancelCommand = new RelayCommand(() => { if (!Busy) RequestClose?.Invoke(); });
            CopyIdCommand = new RelayCommand(() => ClipboardText.TrySet(InstallId));
        }

        /// <summary>Sends (name, e-mail) to the licence server; runs on a background thread.</summary>
        public Func<string, string, LicenseStatus> Submit { get; set; }

        public string FullName { get => _fullName; set { if (Set(ref _fullName, value)) ClearError(); } }
        public string Email { get => _email; set { if (Set(ref _email, value)) ClearError(); } }
        public bool Consent { get => _consent; set { if (Set(ref _consent, value)) ClearError(); } }
        public string InstallId { get => _installId; private set => Set(ref _installId, value); }

        public bool Busy
        {
            get => _busy;
            private set { if (Set(ref _busy, value)) Raise(nameof(NotBusy)); }
        }

        public bool NotBusy => !_busy;

        public string Error { get => _error; private set => Set(ref _error, value); }

        /// <summary>The server's answer after a successful submission (normally Pending); null when cancelled.</summary>
        public LicenseStatus Result { get; private set; }

        public event Action RequestClose;

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CopyIdCommand { get; }

        private async Task RegisterAsync()
        {
            string problem = Validate();
            if (problem != null) { Error = problem; return; }
            if (Submit == null) { Error = L.LicServerError("not_configured"); return; }

            Busy = true;
            Error = null;
            string name = FullName.Trim();
            string email = Email.Trim();
            LicenseStatus status;
            try
            {
                status = await Task.Run(() => Submit(name, email));
            }
            catch (Exception ex)
            {
                status = new LicenseStatus { State = LicenseState.NotRegistered, NetworkError = ex.Message };
            }
            Busy = false;

            if (!string.IsNullOrEmpty(status.InstallId)) InstallId = status.InstallId;

            if (!status.CheckedOnline)
            {
                Error = status.ServerError == "not_configured"
                    ? L.LicServerError("not_configured")
                    : L.RegNoInternet + (string.IsNullOrEmpty(status.NetworkError) ? string.Empty : "\n(" + status.NetworkError + ")");
                return;
            }
            if (!string.IsNullOrEmpty(status.ServerError) || status.State == LicenseState.NotRegistered)
            {
                Error = L.LicServerError(status.ServerError ?? "unknown");
                return;
            }

            Result = status;
            RequestClose?.Invoke();
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length < 2) return L.RegNeedName;
            if (string.IsNullOrWhiteSpace(Email)) return L.RegNeedEmail;
            if (!EmailValidator.IsValid(Email.Trim())) return L.RegBadEmail;
            if (!Consent) return L.RegNeedConsent;
            return null;
        }

        private void ClearError()
        {
            if (!string.IsNullOrEmpty(Error)) Error = null;
        }
    }

    internal static class ClipboardText
    {
        public static void TrySet(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text)) System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                // the clipboard can be locked by another program; copying is a convenience
            }
        }
    }
}
