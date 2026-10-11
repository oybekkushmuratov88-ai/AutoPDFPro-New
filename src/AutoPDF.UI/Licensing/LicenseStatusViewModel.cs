using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using AutoPDF.Core.Licensing;
using AutoPDF.Core.Localization;
using AutoPDF.UI.Mvvm;
using AutoPDF.UI.Theme;

namespace AutoPDF.UI.Licensing
{
    /// <summary>
    /// The licence status screen ("Pending approval", "Rejected", "Needs verification" …). While it is open it
    /// re-checks with the server (the host injects <see cref="Check"/>, which applies the client's throttling);
    /// when the approval arrives it turns green and offers "Continue".
    /// </summary>
    public sealed class LicenseStatusViewModel : ObservableObject
    {
        private LicenseStatus _status;
        private bool _busy;
        private string _message;

        public LicenseStatusViewModel(LicenseStatus status)
        {
            _status = status ?? new LicenseStatus();
            CheckNowCommand = new RelayCommand(() => { if (!Busy) _ = CheckAsync(CheckTrigger.Manual); });
            ContinueCommand = new RelayCommand(() => { if (CanContinue) { Continued = true; RequestClose?.Invoke(); } });
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
            CopyIdCommand = new RelayCommand(() => ClipboardText.TrySet(InstallId));
            Message = status?.NetworkError != null ? L.LicOffline(status.NetworkError) : null;
        }

        /// <summary>Asks the licence server (background thread).</summary>
        public Func<CheckTrigger, LicenseStatus> Check { get; set; }

        /// <summary>Raised on the UI thread after every check (the host uses it for notifications).</summary>
        public event Action<LicenseStatus> Checked;

        public LicenseStatus Status
        {
            get => _status;
            private set
            {
                _status = value;
                foreach (string p in new[] { nameof(State), nameof(StateName), nameof(Headline), nameof(Explanation), nameof(Name), nameof(Email),
                                             nameof(InstallId), nameof(RequestId), nameof(Submitted), nameof(LastOnline), nameof(ValidUntil),
                                             nameof(CanContinue), nameof(BadgeBack), nameof(BadgeText), nameof(Waiting) })
                {
                    Raise(p);
                }
            }
        }

        public LicenseState State => _status.State;
        public string StateName => L.LicStateName(_status.State);
        public string Headline => L.LicHeadline(_status.State);
        public string Explanation => L.LicExplanation(_status.State);
        public string Name => _status.Name ?? string.Empty;
        public string Email => _status.Email ?? string.Empty;
        public string InstallId => _status.InstallId ?? string.Empty;
        public string RequestId => _status.RequestId ?? string.Empty;
        public string Submitted => Local(_status.SubmittedUtc);
        public string LastOnline => Local(_status.LastOnlineUtc);
        public string ValidUntil => _status.State == LicenseState.Approved && _status.ExpiresUtc.HasValue ? L.LicValidOffline(Local(_status.ExpiresUtc)) : null;

        public bool CanContinue => _status.State == LicenseState.Approved;

        /// <summary>Still waiting for a decision (the window keeps checking).</summary>
        public bool Waiting => _status.State != LicenseState.Approved && _status.State != LicenseState.NotRegistered;

        public Brush BadgeBack
        {
            get
            {
                switch (_status.State)
                {
                    case LicenseState.Approved: return Palette.SuccessBack;
                    case LicenseState.Pending: return Palette.InfoBack;
                    case LicenseState.Expired: return Palette.WarningBack;
                    default: return Palette.DangerBack;
                }
            }
        }

        public Brush BadgeText
        {
            get
            {
                switch (_status.State)
                {
                    case LicenseState.Approved: return Palette.SuccessText;
                    case LicenseState.Pending: return Palette.InfoText;
                    case LicenseState.Expired: return Palette.WarningText;
                    default: return Palette.DangerText;
                }
            }
        }

        public bool Busy
        {
            get => _busy;
            private set { if (Set(ref _busy, value)) Raise(nameof(NotBusy)); }
        }

        public bool NotBusy => !_busy;

        /// <summary>Network problem or server remark, shown under the status.</summary>
        public string Message { get => _message; private set => Set(ref _message, value); }

        /// <summary>The user pressed Continue after the approval.</summary>
        public bool Continued { get; private set; }

        public event Action RequestClose;

        public ICommand CheckNowCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand CopyIdCommand { get; }

        /// <summary>Checks with the server unless a check is already running.</summary>
        public async Task CheckAsync(CheckTrigger trigger)
        {
            if (Busy || Check == null) return;
            Busy = true;
            LicenseStatus result;
            try
            {
                result = await Task.Run(() => Check(trigger));
            }
            catch (Exception ex)
            {
                result = null;
                Message = L.LicOffline(ex.Message);
            }
            Busy = false;
            if (result == null) return;

            Status = result;
            if (result.NetworkError != null) Message = L.LicOffline(result.NetworkError);
            else if (!string.IsNullOrEmpty(result.ServerError)) Message = L.LicServerError(result.ServerError);
            else Message = null;
            Checked?.Invoke(result);
        }

        private static string Local(DateTime? utc) =>
            utc.HasValue ? utc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : L.LicNever;
    }
}
