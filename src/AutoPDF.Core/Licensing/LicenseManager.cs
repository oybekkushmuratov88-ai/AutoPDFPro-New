using System;

namespace AutoPDF.Core.Licensing
{
    /// <summary>What the plug-in may do right now.</summary>
    public enum LicenseState
    {
        /// <summary>No registration has been accepted by the server yet: show the registration form.</summary>
        NotRegistered,

        /// <summary>Registered, waiting for the administrator.</summary>
        Pending,

        /// <summary>A valid, server-signed approval for this computer that has not expired.</summary>
        Approved,

        /// <summary>The administrator rejected (or revoked) this installation.</summary>
        Rejected,

        /// <summary>Approved, but the offline grace period is over (or the clock was turned back): must reach the server.</summary>
        Expired,

        /// <summary>The stored approval is not validly signed for this computer — treated as not approved.</summary>
        Invalid
    }

    /// <summary>Why a check is being made (decides how often the server is contacted).</summary>
    public enum CheckTrigger
    {
        /// <summary>The user ran AUTOPDF.</summary>
        Command,

        /// <summary>The timer while the CAD application is running.</summary>
        Periodic,

        /// <summary>The user pressed "Check now".</summary>
        Manual
    }

    public sealed class LicenseStatus
    {
        public LicenseState State { get; set; }

        /// <summary>PDF export is allowed only in this case.</summary>
        public bool CanExport => State == LicenseState.Approved;

        public string InstallId { get; set; }
        public string RequestId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime? SubmittedUtc { get; set; }
        public DateTime? LastOnlineUtc { get; set; }

        /// <summary>When the cached approval stops working without a new online check.</summary>
        public DateTime? ExpiresUtc { get; set; }

        /// <summary>This result includes a successful answer from the server.</summary>
        public bool CheckedOnline { get; set; }

        /// <summary>Set when the server could not be reached (the result then comes from the local cache).</summary>
        public string NetworkError { get; set; }

        /// <summary>Error code from the server, e.g. "rate_limited" or "invalid_field:email".</summary>
        public string ServerError { get; set; }

        public string ServerMessage { get; set; }

        /// <summary>Why <see cref="LicenseState.Invalid"/> / <see cref="LicenseState.Expired"/>.</summary>
        public string Detail { get; set; }

        /// <summary>A decision the user has not been told about yet (Approved or Rejected), else None.</summary>
        public RemoteStatus ChangeToNotify { get; set; }
    }

    public sealed class LicenseOptions
    {
        /// <summary>Built-in server key (SubjectPublicKeyInfo PEM/base64). When set, no other key is accepted.</summary>
        public string PinnedPublicKey { get; set; }

        public TimeSpan ApprovedCommandRecheck { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan ApprovedPeriodicRecheck { get; set; } = TimeSpan.FromMinutes(55);
        public TimeSpan PendingCommandRecheck { get; set; } = TimeSpan.FromSeconds(20);
        public TimeSpan PendingPeriodicRecheck { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RejectedRecheck { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan ManualMinInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>The clock may go back this much (NTP corrections) before a cached approval is distrusted.</summary>
        public TimeSpan ClockRollbackTolerance { get; set; } = TimeSpan.FromHours(1);
    }

    /// <summary>Context sent with requests (not personal).</summary>
    public sealed class ClientInfo
    {
        public string AppVersion { get; set; }
        public string Host { get; set; }
        public string Language { get; set; }
    }

    /// <summary>
    /// The licensing rules of the client:
    ///  • first launch → registration (name, e-mail, generated installation ID) sent to the server over HTTPS;
    ///  • nothing is allowed until the server reports "approved" with a signature that verifies for THIS
    ///    installation and computer; a rejection (or later revocation) disables export again;
    ///  • network failures never grant anything: a registration that was not received stays unregistered, a
    ///    pending one stays pending, and an earlier approval keeps working only until its signed expiry
    ///    (the offline grace period set by the server) — and not at all if the clock was turned back;
    ///  • the server is contacted on AUTOPDF and periodically, never more often than the intervals in
    ///    <see cref="LicenseOptions"/> (or the server's own "check after" hint).
    /// Thread-safe; every operation re-reads the store, so several CAD applications on one computer agree.
    /// </summary>
    public sealed class LicenseManager
    {
        private static readonly object Gate = new object();

        private readonly ILicenseServer _server;
        private readonly LicenseStore _store;
        private readonly IMachineIdentity _machine;
        private readonly LicenseOptions _options;
        private readonly Func<DateTime> _utcNow;
        private readonly RsaPublicKey _pinned;

        public LicenseManager(ILicenseServer server, LicenseStore store, IMachineIdentity machine, LicenseOptions options = null, Func<DateTime> utcNow = null)
        {
            _server = server;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _options = options ?? new LicenseOptions();
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(_options.PinnedPublicKey))
            {
                _pinned = RsaPublicKey.Parse(_options.PinnedPublicKey);
            }
        }

        public bool HasServer => _server != null;

        /// <summary>This installation's ID (created on first use).</summary>
        public string InstallId
        {
            get
            {
                lock (Gate) return LoadWithIdentity().InstallId;
            }
        }

        /// <summary>The current state from the local cache only (no network).</summary>
        public LicenseStatus Evaluate()
        {
            lock (Gate)
            {
                LicenseRecord rec = LoadWithIdentity();
                return EvaluateRecord(rec, save: true);
            }
        }

        /// <summary>True when <paramref name="trigger"/> should contact the server now.</summary>
        public bool IsCheckDue(CheckTrigger trigger)
        {
            lock (Gate)
            {
                LicenseRecord rec = LoadWithIdentity();
                return IsDue(rec, EvaluateRecord(rec, save: false).State, trigger);
            }
        }

        /// <summary>Sends the registration. Never grants anything by itself; the result is normally Pending.</summary>
        public LicenseStatus Register(string name, string email, ClientInfo client)
        {
            name = (name ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();
            if (name.Length < 2 || name.Length > 100) throw new ArgumentException("name");
            if (!EmailValidator.IsValid(email)) throw new ArgumentException("email");
            if (_server == null)
            {
                LicenseStatus none = Evaluate();
                none.ServerError = "not_configured";
                return none;
            }

            LicenseRecord rec;
            lock (Gate) rec = LoadWithIdentity();

            LicenseReply reply = _server.Register(BuildRequest(rec, client, name, email));

            lock (Gate)
            {
                rec = LoadWithIdentity();
                DateTime now = _utcNow();
                rec.LastCheckUtc = now;
                if (!reply.Reached)
                {
                    rec.LastMessage = reply.NetworkError;
                    Save(rec);
                    LicenseStatus offline = EvaluateRecord(rec, save: false);
                    offline.NetworkError = reply.NetworkError ?? "network";
                    return offline;
                }

                if (reply.Ok)
                {
                    rec.Name = name;
                    rec.Email = email;
                    rec.SubmittedUtc = now;
                }
                ApplyReply(rec, reply, now);
                Save(rec);
                LicenseStatus status = EvaluateRecord(rec, save: false);
                status.CheckedOnline = true;
                status.ServerError = reply.Ok ? null : reply.Error;
                status.ServerMessage = reply.Message;
                return status;
            }
        }

        /// <summary>
        /// Asks the server for the current status when a check is due for <paramref name="trigger"/> (always for
        /// <paramref name="force"/>); otherwise returns the cached state.
        /// </summary>
        public LicenseStatus Check(ClientInfo client, CheckTrigger trigger, bool force = false)
        {
            LicenseRecord rec;
            lock (Gate)
            {
                rec = LoadWithIdentity();
                LicenseStatus cached = EvaluateRecord(rec, save: true);
                if (_server == null || !rec.HasRegistration) return cached;
                if (!force && !IsDue(rec, cached.State, trigger)) return cached;
            }

            LicenseReply reply = _server.Status(BuildRequest(rec, client, null, null));

            lock (Gate)
            {
                rec = LoadWithIdentity();
                DateTime now = _utcNow();
                rec.LastCheckUtc = now;
                if (!reply.Reached)
                {
                    rec.LastMessage = reply.NetworkError;
                    Save(rec);
                    LicenseStatus offline = EvaluateRecord(rec, save: false);
                    offline.NetworkError = reply.NetworkError ?? "network";
                    return offline;
                }

                ApplyReply(rec, reply, now);
                Save(rec);
                LicenseStatus status = EvaluateRecord(rec, save: false);
                status.CheckedOnline = true;
                status.ServerError = reply.Ok ? null : reply.Error;
                status.ServerMessage = reply.Message;
                return status;
            }
        }

        /// <summary>The user has been told about <paramref name="status"/>.</summary>
        public void MarkNotified(RemoteStatus status)
        {
            lock (Gate)
            {
                LicenseRecord rec = LoadWithIdentity();
                if (rec.NotifiedStatus == status) return;
                rec.NotifiedStatus = status;
                Save(rec);
            }
        }

        // ---- internals -------------------------------------------------------------------------------

        private LicenseRequest BuildRequest(LicenseRecord rec, ClientInfo client, string name, string email) => new LicenseRequest
        {
            InstallId = rec.InstallId,
            ClientKey = rec.ClientKey,
            MachineHash = _machine.MachineHash,
            AppVersion = client?.AppVersion,
            Host = client?.Host,
            Language = client?.Language,
            Name = name,
            Email = email,
            Computer = name == null ? null : _machine.ComputerName,
            WindowsUser = name == null ? null : _machine.WindowsUser,
            Os = name == null ? null : _machine.OsDescription
        };

        private LicenseRecord LoadWithIdentity()
        {
            LicenseRecord rec = _store.Load();
            if (string.IsNullOrEmpty(rec.InstallId))
            {
                rec = new LicenseRecord
                {
                    InstallId = InstallationIdentity.NewInstallId(),
                    ClientKey = InstallationIdentity.NewClientKey()
                };
                Save(rec);
            }
            return rec;
        }

        private void Save(LicenseRecord rec)
        {
            try
            {
                _store.Save(rec);
            }
            catch
            {
                // A read-only profile must not crash the CAD application; the state is re-evaluated next time.
            }
        }

        private void ApplyReply(LicenseRecord rec, LicenseReply reply, DateTime now)
        {
            // The server's throttle hint ("check after N seconds") applies to every trigger.
            rec.NextCheckUtc = reply.CheckAfterSeconds > 0 ? now.AddSeconds(Math.Min(reply.CheckAfterSeconds, 24 * 3600)) : (DateTime?)null;

            if (!reply.Ok)
            {
                switch (reply.Error)
                {
                    case "unknown_install":
                        // The server has no record (deleted by the administrator): register again.
                        ClearRegistration(rec);
                        break;
                    case "client_mismatch":
                    case "machine_mismatch":
                        // This installation ID belongs to another key or computer (e.g. a copied licence file):
                        // start over with a fresh identity, which needs its own approval.
                        ClearRegistration(rec);
                        rec.InstallId = InstallationIdentity.NewInstallId();
                        rec.ClientKey = InstallationIdentity.NewClientKey();
                        break;
                }
                rec.LastMessage = reply.Message ?? reply.Error;
                return;
            }

            if (!string.IsNullOrEmpty(reply.InstallId) && reply.InstallId != rec.InstallId)
            {
                rec.LastMessage = "Reply for another installation ignored.";
                return;
            }

            rec.LastOnlineUtc = now;
            if (reply.ServerTimeUtc.HasValue) rec.ClockOffsetSeconds = (long)(reply.ServerTimeUtc.Value - now).TotalSeconds;
            if (!string.IsNullOrEmpty(reply.RequestId)) rec.RequestId = reply.RequestId;
            AcceptServerKey(rec, reply);
            rec.LastMessage = reply.Message;

            switch (reply.Status)
            {
                case RemoteStatus.Approved:
                    rec.Status = RemoteStatus.Approved;
                    rec.Token = reply.Token;
                    rec.Signature = reply.Signature;
                    break;
                case RemoteStatus.Rejected:
                    rec.Status = RemoteStatus.Rejected;
                    rec.Token = null;
                    rec.Signature = null;
                    break;
                case RemoteStatus.Pending:
                    rec.Status = RemoteStatus.Pending;
                    rec.Token = null;
                    rec.Signature = null;
                    if (rec.NotifiedStatus != RemoteStatus.Pending) rec.NotifiedStatus = RemoteStatus.Pending;
                    break;
                default:
                    ClearRegistration(rec);
                    break;
            }
        }

        private void AcceptServerKey(LicenseRecord rec, LicenseReply reply)
        {
            if (_pinned != null) return; // only the built-in key is ever used
            if (string.IsNullOrEmpty(reply.PublicKey)) return;
            if (!RsaPublicKey.TryParse(reply.PublicKey, out RsaPublicKey key)) return;
            if (!string.IsNullOrEmpty(reply.KeyId) && !string.Equals(reply.KeyId, key.KeyId, StringComparison.OrdinalIgnoreCase)) return;
            // Received over HTTPS from the configured server: that server is the trust anchor.
            rec.ServerPublicKey = key.ToBase64();
        }

        private static void ClearRegistration(LicenseRecord rec)
        {
            rec.RequestId = null;
            rec.Status = RemoteStatus.None;
            rec.Token = null;
            rec.Signature = null;
            rec.SubmittedUtc = null;
            rec.NotifiedStatus = RemoteStatus.None;
        }

        private LicenseStatus EvaluateRecord(LicenseRecord rec, bool save)
        {
            DateTime now = _utcNow();
            var s = new LicenseStatus
            {
                InstallId = rec.InstallId,
                RequestId = rec.RequestId,
                Name = rec.Name,
                Email = rec.Email,
                SubmittedUtc = rec.SubmittedUtc,
                LastOnlineUtc = rec.LastOnlineUtc
            };

            bool rolledBack = rec.LastSeenUtc.HasValue && now < rec.LastSeenUtc.Value - _options.ClockRollbackTolerance;
            if (save && !rolledBack && (!rec.LastSeenUtc.HasValue || now > rec.LastSeenUtc.Value + TimeSpan.FromMinutes(1)))
            {
                rec.LastSeenUtc = now;
                Save(rec);
            }

            if (!rec.HasRegistration)
            {
                s.State = LicenseState.NotRegistered;
                return s;
            }

            switch (rec.Status)
            {
                case RemoteStatus.Rejected:
                    s.State = LicenseState.Rejected;
                    break;

                case RemoteStatus.Approved:
                    if (!TryVerify(rec, out LicenseToken token, out string why))
                    {
                        s.State = LicenseState.Invalid;
                        s.Detail = why;
                        break;
                    }
                    s.ExpiresUtc = token.ExpiresUtc;
                    DateTime serverNow = now.AddSeconds(rec.ClockOffsetSeconds);
                    if (rolledBack)
                    {
                        s.State = LicenseState.Expired;
                        s.Detail = "clock";
                    }
                    else if (serverNow >= token.ExpiresUtc)
                    {
                        s.State = LicenseState.Expired;
                        s.Detail = "expired";
                    }
                    else
                    {
                        s.State = LicenseState.Approved;
                    }
                    break;

                default:
                    s.State = LicenseState.Pending;
                    break;
            }

            if ((rec.Status == RemoteStatus.Approved && s.State == LicenseState.Approved || rec.Status == RemoteStatus.Rejected) &&
                rec.NotifiedStatus != rec.Status)
            {
                s.ChangeToNotify = rec.Status;
            }
            return s;
        }

        private bool TryVerify(LicenseRecord rec, out LicenseToken token, out string why)
        {
            token = null;
            RsaPublicKey key = _pinned;
            if (key == null && !RsaPublicKey.TryParse(rec.ServerPublicKey, out key))
            {
                why = "no-key";
                return false;
            }
            if (!LicenseToken.TryParse(rec.Token, out token))
            {
                why = "no-token";
                return false;
            }
            if (!key.Verify(token.Payload, rec.Signature))
            {
                why = "signature";
                return false;
            }
            if (token.InstallId != rec.InstallId || token.RequestId != rec.RequestId)
            {
                why = "installation";
                return false;
            }
            if (!string.Equals(token.MachineHash, _machine.MachineHash, StringComparison.OrdinalIgnoreCase))
            {
                why = "machine";
                return false;
            }
            if (token.Status != RemoteStatus.Approved)
            {
                why = "status";
                return false;
            }
            why = null;
            return true;
        }

        private bool IsDue(LicenseRecord rec, LicenseState state, CheckTrigger trigger)
        {
            if (!rec.HasRegistration) return false;
            DateTime now = _utcNow();
            if (rec.NextCheckUtc.HasValue && now < rec.NextCheckUtc.Value && rec.NextCheckUtc.Value - now < TimeSpan.FromDays(1)) return false;
            TimeSpan since = rec.LastCheckUtc.HasValue ? now - rec.LastCheckUtc.Value : TimeSpan.MaxValue;
            if (since < TimeSpan.Zero) since = TimeSpan.MaxValue; // clock turned back: allow a check

            if (trigger == CheckTrigger.Manual) return since >= _options.ManualMinInterval;

            switch (state)
            {
                case LicenseState.Approved:
                    return since >= (trigger == CheckTrigger.Command ? _options.ApprovedCommandRecheck : _options.ApprovedPeriodicRecheck);
                case LicenseState.Pending:
                    return since >= (trigger == CheckTrigger.Command ? _options.PendingCommandRecheck : _options.PendingPeriodicRecheck);
                case LicenseState.Rejected:
                    return since >= _options.RejectedRecheck;
                default: // Expired / Invalid: the user is blocked until the server answers
                    return since >= (trigger == CheckTrigger.Command ? _options.ManualMinInterval : _options.PendingPeriodicRecheck);
            }
        }
    }
}
