using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Registration;
using AutoPDF.UI.Mvvm;

namespace AutoPDF.UI.Registration
{
    /// <summary>Admin-only user list: type the admin key, fetch the list from the web app, show the rows.</summary>
    public sealed class UsersViewModel : ObservableObject
    {
        private readonly IRegistrationGateway _gateway;
        private readonly AdminKeyStore _keyStore;
        private string _key = string.Empty;
        private string _status = string.Empty;
        private bool _busy;
        private bool _remember;

        public UsersViewModel(IRegistrationGateway gateway, AdminKeyStore keyStore)
        {
            _gateway = gateway;
            _keyStore = keyStore;

            string saved = keyStore?.Load() ?? string.Empty;
            _key = saved;
            _remember = !string.IsNullOrEmpty(saved);

            ShowCommand = new RelayCommand(() => { if (!Busy) _ = LoadAsync(); });
        }

        public ObservableCollection<RegisteredUser> Users { get; } = new ObservableCollection<RegisteredUser>();

        public string Key { get => _key; set => Set(ref _key, value); }
        public bool Remember { get => _remember; set => Set(ref _remember, value); }
        public string Status { get => _status; private set => Set(ref _status, value); }

        public bool Busy
        {
            get => _busy;
            private set { if (Set(ref _busy, value)) Raise(nameof(NotBusy)); }
        }

        public bool NotBusy => !_busy;

        public ICommand ShowCommand { get; }

        private async Task LoadAsync()
        {
            string key = (Key ?? string.Empty).Trim();
            if (key.Length == 0) { Status = L.AdminForbidden; return; }

            Busy = true;
            Status = L.AdminLoading;
            Users.Clear();

            UsersResult result;
            try
            {
                result = await Task.Run(() => _gateway.FetchUsers(key));
            }
            catch (Exception ex)
            {
                result = new UsersResult { Ok = false, Error = ex.Message };
            }

            Busy = false;

            if (result.Ok)
            {
                foreach (RegisteredUser user in result.Users) Users.Add(user);
                Status = Users.Count == 0 ? L.AdminNoUsers : L.AdminCount(Users.Count);
                if (Remember) _keyStore?.Save(key); else _keyStore?.Clear();
            }
            else
            {
                bool forbidden = !string.IsNullOrEmpty(result.Error) &&
                                 result.Error.IndexOf("FORBIDDEN", StringComparison.OrdinalIgnoreCase) >= 0;
                Status = forbidden ? L.AdminForbidden : (result.Error ?? L.AdminForbidden);
            }
        }
    }
}
