using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Registration;
using AutoPDF.UI.Mvvm;

namespace AutoPDF.UI.Registration
{
    public enum RegistrationOutcome
    {
        Cancelled,
        Registered
    }

    /// <summary>
    /// Drives the registration window. The controller injects <see cref="Submit"/> (send to the web app) and
    /// <see cref="Persist"/> (save locally); the view model does validation, the busy state and the
    /// "send later and continue" fallback when the network is unavailable.
    /// </summary>
    public sealed class RegistrationViewModel : ObservableObject
    {
        private readonly string _version;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private bool _consent;
        private bool _busy;
        private string _error;
        private bool _showOfflineOption;
        private RegistrationInfo _pending;

        public RegistrationViewModel(string version)
        {
            _version = version ?? string.Empty;
            RegisterCommand = new RelayCommand(() => { if (!Busy) _ = RegisterAsync(); });
            CancelCommand = new RelayCommand(Cancel);
            ContinueOfflineCommand = new RelayCommand(ContinueOffline);
        }

        /// <summary>Sends the registration to the web app (runs on a background thread).</summary>
        public Func<RegistrationInfo, SubmitResult> Submit { get; set; }

        /// <summary>Stores the registration locally (info, delivered).</summary>
        public Action<RegistrationInfo, bool> Persist { get; set; }

        public string FullName { get => _fullName; set { if (Set(ref _fullName, value)) ClearError(); } }
        public string Email { get => _email; set { if (Set(ref _email, value)) ClearError(); } }
        public string Phone { get => _phone; set => Set(ref _phone, value); }
        public bool Consent { get => _consent; set { if (Set(ref _consent, value)) ClearError(); } }

        public bool Busy
        {
            get => _busy;
            private set { if (Set(ref _busy, value)) Raise(nameof(NotBusy)); }
        }

        public bool NotBusy => !_busy;

        public string Error { get => _error; private set => Set(ref _error, value); }

        public bool ShowOfflineOption { get => _showOfflineOption; private set => Set(ref _showOfflineOption, value); }

        public string OfflineNote => L.RegOfflineNote;

        // Read-only facts shown so the user sees exactly what is sent.
        public string Version => _version;
        public string Computer => MachineFacts.Computer;
        public string WindowsUser => MachineFacts.WindowsUser;

        public RegistrationOutcome Outcome { get; private set; } = RegistrationOutcome.Cancelled;

        public event Action RequestClose;

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ContinueOfflineCommand { get; }

        private async Task RegisterAsync()
        {
            string problem = Validate();
            if (problem != null) { Error = problem; return; }
            if (Submit == null) { Error = L.RegServerError("no gateway"); return; }

            Busy = true;
            Error = null;
            ShowOfflineOption = false;

            RegistrationInfo info = BuildInfo();
            SubmitResult result;
            try
            {
                result = await Task.Run(() => Submit(info));
            }
            catch (Exception ex)
            {
                result = new SubmitResult { Ok = false, NetworkProblem = true, Error = ex.Message };
            }

            Busy = false;

            if (result != null && result.Ok)
            {
                Complete(info, delivered: true);
                return;
            }

            // Sending failed. Keep the filled form and offer to continue offline (re-sent on a later start).
            _pending = info;
            Error = result != null && result.NetworkProblem
                ? L.RegNetworkError
                : L.RegServerError(result?.Error ?? string.Empty);
            ShowOfflineOption = RegistrationConfig.AllowOfflineContinue;
        }

        private void ContinueOffline()
        {
            if (_pending == null || Busy) return;
            Complete(_pending, delivered: false);
        }

        private void Cancel()
        {
            if (Busy) return;
            Outcome = RegistrationOutcome.Cancelled;
            RequestClose?.Invoke();
        }

        private void Complete(RegistrationInfo info, bool delivered)
        {
            info.Delivered = delivered;
            try { Persist?.Invoke(info, delivered); }
            catch { /* saving locally is best-effort; the user is still allowed through */ }
            Outcome = RegistrationOutcome.Registered;
            RequestClose?.Invoke();
        }

        private RegistrationInfo BuildInfo() => new RegistrationInfo
        {
            FullName = (FullName ?? string.Empty).Trim(),
            Email = (Email ?? string.Empty).Trim(),
            Phone = (Phone ?? string.Empty).Trim(),
            Computer = MachineFacts.Computer,
            WindowsUser = MachineFacts.WindowsUser,
            MachineId = MachineFacts.MachineId(),
            Os = MachineFacts.Os,
            Version = _version,
            Language = L.Current.ToString(),
            RegisteredAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            Attempts = 1
        };

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(FullName)) return L.RegNeedName;
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
}
