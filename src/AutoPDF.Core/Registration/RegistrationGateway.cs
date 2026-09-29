using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace AutoPDF.Core.Registration
{
    public sealed class SubmitResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public bool NetworkProblem { get; set; }
    }

    public sealed class UsersResult
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public List<RegisteredUser> Users { get; set; } = new List<RegisteredUser>();
    }

    /// <summary>Sends a registration to the web app and (for the admin) reads back the list of users.</summary>
    public interface IRegistrationGateway
    {
        SubmitResult Submit(RegistrationInfo info);
        UsersResult FetchUsers(string adminKey);
    }

    /// <summary>
    /// Talks to the Google Apps Script web app. The registration is a plain form POST (so the script reads it
    /// from <c>e.parameter</c>); the admin list is a GET whose reply is one "OK" line followed by tab-separated
    /// rows. No credentials are held here — the URL is the only configuration, like a form address.
    /// </summary>
    public sealed class HttpRegistrationGateway : IRegistrationGateway, IDisposable
    {
        private readonly string _endpoint;
        private readonly HttpClient _http;

        static HttpRegistrationGateway()
        {
            // Make sure TLS 1.2 is allowed (older .NET Framework defaults could omit it); Google requires it.
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
            catch { /* platform already negotiates a modern protocol */ }
        }

        public HttpRegistrationGateway(string endpoint, string userAgentVersion = null, TimeSpan? timeout = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _http = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "AutoPDFPro/" + (userAgentVersion ?? "?"));
        }

        public SubmitResult Submit(RegistrationInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var fields = new Dictionary<string, string>
            {
                ["action"] = "register",
                ["name"] = info.FullName,
                ["email"] = info.Email,
                ["phone"] = info.Phone,
                ["computer"] = info.Computer,
                ["user"] = info.WindowsUser,
                ["machineId"] = info.MachineId,
                ["os"] = info.Os,
                ["version"] = info.Version,
                ["lang"] = info.Language
            };

            try
            {
                using (var content = new StringContent(FormEncode(fields), Encoding.UTF8, "application/x-www-form-urlencoded"))
                using (HttpResponseMessage response = _http.PostAsync(_endpoint, content).GetAwaiter().GetResult())
                {
                    string body = ReadBody(response);
                    if (response.IsSuccessStatusCode && StartsWithOk(body))
                    {
                        return new SubmitResult { Ok = true };
                    }
                    return new SubmitResult
                    {
                        Ok = false,
                        Error = response.IsSuccessStatusCode
                            ? Trim(body, 300)
                            : "HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase
                    };
                }
            }
            catch (Exception ex)
            {
                return new SubmitResult { Ok = false, NetworkProblem = true, Error = Innermost(ex).Message };
            }
        }

        public UsersResult FetchUsers(string adminKey)
        {
            string url = _endpoint + (_endpoint.Contains("?") ? "&" : "?") +
                         "action=list&key=" + Uri.EscapeDataString(adminKey ?? string.Empty);
            try
            {
                using (HttpResponseMessage response = _http.GetAsync(url).GetAwaiter().GetResult())
                {
                    string body = ReadBody(response);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new UsersResult { Ok = false, Error = "HTTP " + (int)response.StatusCode };
                    }
                    if (!StartsWithOk(body))
                    {
                        return new UsersResult { Ok = false, Error = Trim(body, 200) };
                    }
                    return new UsersResult { Ok = true, Users = ParseUsers(body) };
                }
            }
            catch (Exception ex)
            {
                return new UsersResult { Ok = false, Error = Innermost(ex).Message };
            }
        }

        internal static List<RegisteredUser> ParseUsers(string body)
        {
            var users = new List<RegisteredUser>();
            string[] lines = (body ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            int number = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                // Skip the leading "OK" status line and any blank lines.
                if (line.Length == 0) continue;
                if (i == 0 && StartsWithOk(line)) continue;
                if (line.Equals("OK", StringComparison.OrdinalIgnoreCase)) continue;

                string[] c = line.Split('\t');
                users.Add(new RegisteredUser
                {
                    Number = ++number,
                    Date = Field(c, 0),
                    FullName = Field(c, 1),
                    Email = Field(c, 2),
                    Phone = Field(c, 3),
                    Computer = Field(c, 4),
                    WindowsUser = Field(c, 5),
                    Version = Field(c, 6)
                });
            }
            return users;
        }

        private static string Field(string[] cells, int index) =>
            index < cells.Length ? cells[index].Trim() : string.Empty;

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

        private static string ReadBody(HttpResponseMessage response)
        {
            try { return response.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static bool StartsWithOk(string body) =>
            body != null && body.TrimStart().StartsWith("OK", StringComparison.OrdinalIgnoreCase);

        private static string Trim(string s, int max)
        {
            s = (s ?? string.Empty).Trim();
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        private static Exception Innermost(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex;
        }

        public void Dispose() => _http.Dispose();
    }
}
