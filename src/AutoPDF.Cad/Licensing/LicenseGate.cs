using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AutoPDF.Cad.Infrastructure;
using AutoPDF.Core.Licensing;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Logging;
using AutoPDF.UI.Licensing;

namespace AutoPDF.Cad.Licensing
{
    /// <summary>
    /// The licence check of the CAD application (AutoCAD and ZWCAD alike):
    ///  • AUTOPDF runs only with a valid, server-signed approval for this installation and computer;
    ///  • first launch → registration form; afterwards → the persistent status window until approved;
    ///  • re-checked on every AUTOPDF (in the background when already approved, so a revocation takes effect)
    ///    and every few minutes while the application runs; the user is notified once of a decision.
    /// All throttling and offline rules live in <see cref="LicenseManager"/>.
    /// </summary>
    public static class LicenseGate
    {
        private static readonly object InitLock = new object();
        private static LicenseManager _manager;
        private static HttpLicenseServer _server;
        private static DispatcherTimer _timer;
        private static int _backgroundRunning;

        private static LoggingService Log => AutoPdfApp.Log;

        public static LicenseManager Manager
        {
            get
            {
                lock (InitLock)
                {
                    if (_manager == null)
                    {
                        _server = LicenseConfig.IsConfigured ? new HttpLicenseServer(LicenseConfig.Endpoint, AutoPdfApp.Version) : null;
                        var options = new LicenseOptions { PinnedPublicKey = string.IsNullOrWhiteSpace(LicenseConfig.PinnedPublicKey) ? null : LicenseConfig.PinnedPublicKey };
                        _manager = new LicenseManager(_server, new LicenseStore(LicenseStore.DefaultFilePath), new MachineIdentity(), options);
                    }
                    return _manager;
                }
            }
        }

        private static ClientInfo Client => new ClientInfo
        {
            AppVersion = AutoPdfApp.Version,
            Host = HostEnvironment.Host.DisplayName,
            Language = L.Current.ToString()
        };

        /// <summary>Called by AUTOPDF. True when PDF export may run now.</summary>
        public static bool EnsureLicensed(Document doc, IntPtr owner)
        {
            if (!LicenseConfig.IsConfigured)
            {
                doc.Editor.WriteMessage("\n" + L.LicServerError("not_configured") + "\n");
                return false;
            }

            LicenseStatus status = Manager.Evaluate();
            Log.Info("Licence: " + status.State + " (" + status.InstallId + ")");

            if (status.State == LicenseState.Approved)
            {
                Notify(status);
                CheckInBackground(CheckTrigger.Command);
                return true;
            }

            if (status.State == LicenseState.NotRegistered)
            {
                status = ShowRegistration(status, owner);
                if (status == null) return false;
                if (status.CanExport) return true; // this installation had already been approved
                return ShowStatus(status, doc, owner, checkOnOpen: false);
            }

            return ShowStatus(status, doc, owner, checkOnOpen: true);
        }

        /// <summary>AUTOPDFLICENSE: shows the installation ID and the current status (registration if needed).</summary>
        public static void ShowLicenseInfo(Document doc, IntPtr owner)
        {
            if (!LicenseConfig.IsConfigured)
            {
                doc.Editor.WriteMessage("\n" + L.LicServerError("not_configured") + "\n");
                return;
            }
            LicenseStatus status = Manager.Evaluate();
            if (status.State == LicenseState.NotRegistered)
            {
                status = ShowRegistration(status, owner);
                if (status == null) return;
                ShowStatus(status, doc, owner, checkOnOpen: false);
                return;
            }
            ShowStatus(status, doc, owner, checkOnOpen: true);
        }

        /// <summary>From the plug-in start-up (UI thread): one check soon after start, then every 5 minutes if due.</summary>
        public static void StartPeriodicChecks()
        {
            if (!LicenseConfig.IsConfigured || _timer != null) return;
            try
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(5) };
                _timer.Tick += (s, e) => CheckInBackground(CheckTrigger.Periodic);
                _timer.Start();
                CheckInBackground(CheckTrigger.Periodic);
            }
            catch (Exception ex)
            {
                Log.Warn("Licence timer: " + ex.Message);
            }
        }

        public static void Stop()
        {
            try { _timer?.Stop(); } catch { /* shutting down */ }
        }

        private static LicenseStatus ShowRegistration(LicenseStatus status, IntPtr owner)
        {
            var vm = new RegistrationViewModel(status.InstallId)
            {
                Submit = (name, email) => Manager.Register(name, email, Client)
            };
            CadApp.ShowModalWindow(owner, new RegistrationWindow(vm), false);
            if (vm.Result == null)
            {
                Log.Info(L.RegAbortedLog);
                return null;
            }
            Log.Info("Licence: registration sent, " + vm.Result.State + " (" + vm.Result.InstallId + ", request " + vm.Result.RequestId + ")");
            return vm.Result;
        }

        private static bool ShowStatus(LicenseStatus status, Document doc, IntPtr owner, bool checkOnOpen)
        {
            var vm = new LicenseStatusViewModel(status) { Check = trigger => Manager.Check(Client, trigger) };
            // The window itself shows a decision, so it counts as the notification.
            vm.Checked += s =>
            {
                Log.Info("Licence check: " + s.State + (s.NetworkError != null ? " (offline: " + s.NetworkError + ")" : string.Empty));
                if (s.ChangeToNotify != RemoteStatus.None) Manager.MarkNotified(s.ChangeToNotify);
            };
            CadApp.ShowModalWindow(owner, new LicenseStatusWindow(vm, checkOnOpen), false);

            LicenseStatus final = vm.Status;
            if (final.CanExport && vm.Continued) return true;
            if (!final.CanExport) doc.Editor.WriteMessage(L.LicBlockedEcho(final.State));
            return false;
        }

        /// <summary>Asks the server on a worker thread when a check is due; reports a decision on the UI thread.</summary>
        private static void CheckInBackground(CheckTrigger trigger)
        {
            LicenseManager manager;
            try
            {
                manager = Manager;
                if (!manager.IsCheckDue(trigger)) return;
            }
            catch (Exception ex)
            {
                Log.Warn("Licence: " + ex.Message);
                return;
            }
            if (Interlocked.Exchange(ref _backgroundRunning, 1) == 1) return;

            Dispatcher ui = Dispatcher.CurrentDispatcher;
            ClientInfo client = Client;
            Task.Run(() => manager.Check(client, trigger)).ContinueWith(t =>
            {
                Interlocked.Exchange(ref _backgroundRunning, 0);
                if (t.IsFaulted)
                {
                    Log.Warn("Licence check failed: " + t.Exception?.GetBaseException().Message);
                    return;
                }
                LicenseStatus s = t.Result;
                if (s.NetworkError != null) Log.Debug("Licence check offline: " + s.NetworkError);
                if (s.ChangeToNotify != RemoteStatus.None) ui.BeginInvoke(new Action(() => Notify(s)));
            }, TaskScheduler.Default);
        }

        /// <summary>Tells the user once about an approval or a rejection/revocation (toast + command line).</summary>
        private static void Notify(LicenseStatus s)
        {
            if (s.ChangeToNotify == RemoteStatus.None) return;
            bool approved = s.ChangeToNotify == RemoteStatus.Approved;
            string text = approved ? L.LicToastApproved : L.LicToastRejected;
            Log.Info("Licence decision: " + s.ChangeToNotify);
            LicenseToast.Show(text, approved);
            try
            {
                CadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + text + "\n");
            }
            catch
            {
                // no document open
            }
            Manager.MarkNotified(s.ChangeToNotify);
        }
    }
}
