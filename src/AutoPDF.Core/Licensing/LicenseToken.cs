using System;
using System.Globalization;

namespace AutoPDF.Core.Licensing
{
    /// <summary>
    /// The server's signed statement about one installation:
    /// <c>AUTOPDF-LIC|1|installId|machine|status|requestId|issuedAt|expiresAt</c> (Unix seconds), signed with
    /// RSASSA-PKCS1-v1_5 / SHA-256 by the administrator's Apps Script. Because it is signed, a cached licence
    /// cannot be created or extended by editing a local file.
    /// </summary>
    public sealed class LicenseToken
    {
        public const string Prefix = "AUTOPDF-LIC";
        public const string FormatVersion = "1";

        public string InstallId { get; private set; }
        public string MachineHash { get; private set; }
        public RemoteStatus Status { get; private set; }
        public string RequestId { get; private set; }
        public DateTime IssuedUtc { get; private set; }
        public DateTime ExpiresUtc { get; private set; }
        public string Payload { get; private set; }

        public static string Format(string installId, string machine, RemoteStatus status, string requestId, DateTime issuedUtc, DateTime expiresUtc) =>
            string.Join("|", Prefix, FormatVersion, installId, machine, StatusText(status), requestId,
                ToUnix(issuedUtc).ToString(CultureInfo.InvariantCulture), ToUnix(expiresUtc).ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string payload, out LicenseToken token)
        {
            token = null;
            if (string.IsNullOrEmpty(payload) || payload.Length > 512) return false;
            string[] f = payload.Split('|');
            if (f.Length != 8 || f[0] != Prefix || f[1] != FormatVersion) return false;
            if (!TryParseStatus(f[4], out RemoteStatus status)) return false;
            if (!long.TryParse(f[6], NumberStyles.None, CultureInfo.InvariantCulture, out long issued)) return false;
            if (!long.TryParse(f[7], NumberStyles.None, CultureInfo.InvariantCulture, out long expires)) return false;
            if (expires < issued) return false;

            token = new LicenseToken
            {
                InstallId = f[2],
                MachineHash = f[3],
                Status = status,
                RequestId = f[5],
                IssuedUtc = FromUnix(issued),
                ExpiresUtc = FromUnix(expires),
                Payload = payload
            };
            return true;
        }

        public static string StatusText(RemoteStatus s)
        {
            switch (s)
            {
                case RemoteStatus.Approved: return "approved";
                case RemoteStatus.Rejected: return "rejected";
                case RemoteStatus.Pending: return "pending";
                default: return "none";
            }
        }

        public static bool TryParseStatus(string text, out RemoteStatus status)
        {
            switch ((text ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "approved": status = RemoteStatus.Approved; return true;
                case "rejected": status = RemoteStatus.Rejected; return true;
                case "pending": status = RemoteStatus.Pending; return true;
                case "none": status = RemoteStatus.None; return true;
                default: status = RemoteStatus.None; return false;
            }
        }

        public static long ToUnix(DateTime utc) => (long)(utc.ToUniversalTime() - Epoch).TotalSeconds;

        public static DateTime FromUnix(long seconds) => Epoch.AddSeconds(seconds);

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>The registration status as stored on the server.</summary>
    public enum RemoteStatus
    {
        None,
        Pending,
        Approved,
        Rejected
    }
}
