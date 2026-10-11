using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;

namespace AutoPDF.Core.Licensing
{
    /// <summary>Fields sent with every request (no personal data except in registration).</summary>
    public sealed class LicenseRequest
    {
        public string InstallId { get; set; }
        public string ClientKey { get; set; }
        public string MachineHash { get; set; }
        public string AppVersion { get; set; }
        public string Host { get; set; }
        public string Language { get; set; }

        // Registration only.
        public string Name { get; set; }
        public string Email { get; set; }
        public string Computer { get; set; }
        public string WindowsUser { get; set; }
        public string Os { get; set; }
    }

    /// <summary>The server's answer. <see cref="Reached"/> is false for network failures (no answer at all).</summary>
    public sealed class LicenseReply
    {
        public bool Reached { get; set; }
        public bool Ok { get; set; }
        public RemoteStatus Status { get; set; }
        public string RequestId { get; set; }
        public string InstallId { get; set; }

        /// <summary>Machine-readable error, e.g. "rate_limited", "unknown_install", "invalid_field:email".</summary>
        public string Error { get; set; }

        /// <summary>Human-readable text from the server (shown as is).</summary>
        public string Message { get; set; }

        public DateTime? ServerTimeUtc { get; set; }
        public string PublicKey { get; set; }
        public string KeyId { get; set; }
        public string Token { get; set; }
        public string Signature { get; set; }

        /// <summary>The server asks not to check again before this many seconds.</summary>
        public int CheckAfterSeconds { get; set; }

        /// <summary>Network/protocol problem description when <see cref="Reached"/> is false.</summary>
        public string NetworkError { get; set; }

        public static LicenseReply Unreachable(string why) => new LicenseReply { Reached = false, NetworkError = why };
    }

    /// <summary>The licence server (Google Apps Script web app).</summary>
    public interface ILicenseServer
    {
        LicenseReply Register(LicenseRequest request);
        LicenseReply Status(LicenseRequest request);
    }

    /// <summary>
    /// HTTPS client of the Apps Script web app: form POST, reply as "key=value" lines (values URL-encoded,
    /// first line "apdf=1"). TLS certificates are validated by .NET as usual; plain http is refused except for
    /// loopback test servers. No credential of the administrator is involved — the URL is public.
    /// </summary>
    public sealed class HttpLicenseServer : ILicenseServer, IDisposable
    {
        public const string ProtocolVersion = "1";

        private readonly Uri _endpoint;
        private readonly HttpClient _http;

        static HttpLicenseServer()
        {
            // .NET Framework 4.8 may still default to older protocols on some systems; Google requires TLS 1.2+.
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { /* the platform negotiates a modern protocol itself */ }
        }

        public HttpLicenseServer(string endpoint, string appVersion, TimeSpan? timeout = null)
        {
            if (!IsAcceptableEndpoint(endpoint, out Uri uri)) throw new ArgumentException("The licence server must be an https:// URL.", nameof(endpoint));
            _endpoint = uri;
            _http = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AutoPDFPro/" + (appVersion ?? "?"));
        }

        public static bool IsAcceptableEndpoint(string endpoint, out Uri uri)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        }

        public LicenseReply Register(LicenseRequest r)
        {
            Dictionary<string, string> f = Common("register", r);
            f["name"] = r.Name;
            f["email"] = r.Email;
            f["computer"] = r.Computer;
            f["winUser"] = r.WindowsUser;
            f["os"] = r.Os;
            f["consent"] = "1";
            return Post(f);
        }

        public LicenseReply Status(LicenseRequest r) => Post(Common("status", r));

        private static Dictionary<string, string> Common(string action, LicenseRequest r) => new Dictionary<string, string>
        {
            ["v"] = ProtocolVersion,
            ["action"] = action,
            ["nonce"] = InstallationIdentity.NewNonce(),
            ["installId"] = r.InstallId,
            ["clientKey"] = r.ClientKey,
            ["machine"] = r.MachineHash,
            ["app"] = r.AppVersion,
            ["host"] = r.Host,
            ["lang"] = r.Language
        };

        private LicenseReply Post(Dictionary<string, string> fields)
        {
            try
            {
                using (var content = new StringContent(FormEncode(fields), Encoding.UTF8, "application/x-www-form-urlencoded"))
                using (HttpResponseMessage response = _http.PostAsync(_endpoint, content).GetAwaiter().GetResult())
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
                    if (!response.IsSuccessStatusCode)
                    {
                        return LicenseReply.Unreachable("HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase);
                    }
                    return ParseReply(body);
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                return LicenseReply.Unreachable(ex.Message);
            }
        }

        /// <summary>Parses the "key=value" reply; anything else (e.g. a Google sign-in page) is a protocol error.</summary>
        public static LicenseReply ParseReply(string body)
        {
            Dictionary<string, string> v = LicenseStore.Parse(body);
            if (!v.TryGetValue("apdf", out string marker) || marker != ProtocolVersion)
            {
                string start = (body ?? string.Empty).Trim();
                if (start.Length > 120) start = start.Substring(0, 120) + "…";
                return LicenseReply.Unreachable("Unexpected reply from the licence server: " + start);
            }

            var reply = new LicenseReply
            {
                Reached = true,
                Ok = Get(v, "ok") == "1",
                RequestId = Get(v, "requestId"),
                InstallId = Get(v, "installId"),
                Error = Get(v, "error"),
                Message = Get(v, "message"),
                PublicKey = Get(v, "publicKey"),
                KeyId = Get(v, "keyId"),
                Token = Get(v, "token"),
                Signature = Get(v, "sig")
            };
            if (LicenseToken.TryParseStatus(Get(v, "status"), out RemoteStatus status)) reply.Status = status;
            if (long.TryParse(Get(v, "serverTime"), NumberStyles.None, CultureInfo.InvariantCulture, out long t)) reply.ServerTimeUtc = LicenseToken.FromUnix(t);
            if (int.TryParse(Get(v, "checkAfter"), NumberStyles.None, CultureInfo.InvariantCulture, out int after)) reply.CheckAfterSeconds = after;
            return reply;
        }

        private static string Get(Dictionary<string, string> v, string key) => v.TryGetValue(key, out string s) ? s : null;

        private static string FormEncode(IEnumerable<KeyValuePair<string, string>> fields)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> f in fields)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(f.Key)).Append('=').Append(Uri.EscapeDataString(f.Value ?? string.Empty));
            }
            return sb.ToString();
        }

        public void Dispose() => _http.Dispose();
    }
}
