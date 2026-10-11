using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoPDF.Core.Licensing;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public sealed class LicensingTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "autopdf-lic-" + Guid.NewGuid().ToString("N"));
        private readonly FakeMachine _machine = new FakeMachine();
        private readonly FakeServer _server = new FakeServer();
        private DateTime _now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { /* temp */ }
            _server.Dispose();
        }

        private LicenseStore Store => new LicenseStore(Path.Combine(_dir, "license.txt"));

        private LicenseManager Manager(LicenseOptions options = null, ILicenseServer server = null) =>
            new LicenseManager(server ?? _server, Store, _machine, options, () => _now);

        private static readonly ClientInfo Client = new ClientInfo { AppVersion = "3.0.0", Host = "ZWCAD 2025", Language = "Uzbek" };

        // ---- identity ------------------------------------------------------------------------------------

        [Fact]
        public void InstallIds_AreWellFormedAndUnique()
        {
            var ids = Enumerable.Range(0, 200).Select(_ => InstallationIdentity.NewInstallId()).ToList();
            Assert.All(ids, id => Assert.True(InstallationIdentity.IsValidInstallId(id), id));
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Matches("^APDF-....-....-....-....$", ids[0]);
            Assert.True(InstallationIdentity.IsValidClientKey(InstallationIdentity.NewClientKey()));
            Assert.False(InstallationIdentity.IsValidInstallId("APDF-IIII-OOOO-0000-1111"));
        }

        [Fact]
        public void FirstLaunch_IsNotRegistered_AndKeepsItsInstallationId()
        {
            LicenseStatus s = Manager().Evaluate();
            Assert.Equal(LicenseState.NotRegistered, s.State);
            Assert.False(s.CanExport);
            Assert.True(InstallationIdentity.IsValidInstallId(s.InstallId));
            Assert.Equal(s.InstallId, Manager().InstallId); // persisted
        }

        // ---- keys and tokens ----------------------------------------------------------------------------

        [Fact]
        public void PublicKey_ParsesPemBase64AndPkcs1_AndVerifies()
        {
            using (RSA rsa = RSA.Create(2048))
            {
                byte[] spki = rsa.ExportSubjectPublicKeyInfo();
                string pem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(spki, Base64FormattingOptions.InsertLineBreaks) + "\n-----END PUBLIC KEY-----\n";
                byte[] sig = rsa.SignData(Encoding.UTF8.GetBytes("hello"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                foreach (string text in new[] { pem, Convert.ToBase64String(spki), Convert.ToBase64String(rsa.ExportRSAPublicKey()) })
                {
                    RsaPublicKey key = RsaPublicKey.Parse(text);
                    Assert.Equal(2048, key.KeySizeBits);
                    Assert.True(key.Verify("hello", Convert.ToBase64String(sig)));
                    Assert.False(key.Verify("hellO", Convert.ToBase64String(sig)));
                    Assert.False(key.Verify("hello", "not base64!"));
                }

                using (SHA256 sha = SHA256.Create())
                {
                    string expected = string.Concat(sha.ComputeHash(spki).Take(8).Select(b => b.ToString("x2")));
                    Assert.Equal(expected, RsaPublicKey.Parse(pem).KeyId);
                }
            }
        }

        [Fact]
        public void PublicKey_RejectsGarbageAndSmallKeys()
        {
            Assert.False(RsaPublicKey.TryParse("", out _));
            Assert.False(RsaPublicKey.TryParse("-----BEGIN PUBLIC KEY-----\nAAAA\n-----END PUBLIC KEY-----", out _));
            using (RSA small = RSA.Create(512))
            {
                Assert.False(RsaPublicKey.TryParse(Convert.ToBase64String(small.ExportSubjectPublicKeyInfo()), out _));
            }
        }

        [Fact]
        public void Token_RoundTrips_AndRejectsMalformed()
        {
            string payload = LicenseToken.Format("APDF-AAAA-BBBB-CCCC-DDDD", "abc", RemoteStatus.Approved, "req-1", _now, _now.AddDays(14));
            Assert.True(LicenseToken.TryParse(payload, out LicenseToken t));
            Assert.Equal(RemoteStatus.Approved, t.Status);
            Assert.Equal(_now.AddDays(14), t.ExpiresUtc);
            Assert.False(LicenseToken.TryParse(payload.Replace("AUTOPDF-LIC", "OTHER"), out _));
            Assert.False(LicenseToken.TryParse(payload + "|extra", out _));
            Assert.False(LicenseToken.TryParse("AUTOPDF-LIC|1|a|b|approved|r|200|100", out _)); // expires before issue
        }

        // ---- registration and approval -------------------------------------------------------------------

        [Fact]
        public void Register_SendsIdentity_AndIsPending()
        {
            LicenseManager m = Manager();
            LicenseStatus s = m.Register("Ali Valiyev", "ali@example.com", Client);

            Assert.Equal(LicenseState.Pending, s.State);
            Assert.False(s.CanExport);
            Assert.True(s.CheckedOnline);
            FakeServer.Entry e = _server.Entries[s.InstallId];
            Assert.Equal("ali@example.com", e.Email);
            Assert.Equal(_machine.MachineHash, e.Machine);
            Assert.Equal("ZWCAD 2025", _server.LastRequest.Host);
            Assert.NotNull(_server.LastRequest.ClientKey);
            Assert.Equal(e.RequestId, s.RequestId);
        }

        [Fact]
        public void Register_Offline_GrantsNothing()
        {
            _server.Offline = true;
            LicenseStatus s = Manager().Register("Ali", "ali@example.com", Client);
            Assert.Equal(LicenseState.NotRegistered, s.State);
            Assert.NotNull(s.NetworkError);
            Assert.False(s.CanExport);
        }

        [Fact]
        public void Register_ValidationErrorFromServer_IsReported()
        {
            _server.ForceError = "invalid_field:email";
            LicenseStatus s = Manager().Register("Ali", "ali@example.com", Client);
            Assert.Equal(LicenseState.NotRegistered, s.State);
            Assert.Equal("invalid_field:email", s.ServerError);
        }

        [Fact]
        public void Approval_IsAcceptedOnlyWithValidSignature_AndNotifiedOnce()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");

            LicenseStatus s = m.Check(Client, CheckTrigger.Manual, force: true);
            Assert.Equal(LicenseState.Approved, s.State);
            Assert.True(s.CanExport);
            Assert.Equal(RemoteStatus.Approved, s.ChangeToNotify);
            Assert.Equal(_now.AddDays(14), s.ExpiresUtc);

            m.MarkNotified(RemoteStatus.Approved);
            Assert.Equal(RemoteStatus.None, m.Evaluate().ChangeToNotify);
        }

        [Theory]
        [InlineData("signature")]
        [InlineData("machine")]
        [InlineData("installation")]
        [InlineData("status")]
        public void ForgedOrForeignApproval_IsInvalid(string defect)
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            _server.TokenDefect = defect;

            LicenseStatus s = m.Check(Client, CheckTrigger.Manual, force: true);
            Assert.Equal(LicenseState.Invalid, s.State);
            Assert.False(s.CanExport);
        }

        [Fact]
        public void EditingTheLocalFile_CannotGrantALicence()
        {
            LicenseManager m = Manager();
            m.Register("Ali", "ali@example.com", Client);
            string path = Store.FilePath;
            File.WriteAllText(path, File.ReadAllText(path).Replace("status=pending", "status=approved"));

            Assert.Equal(LicenseState.Invalid, m.Evaluate().State);
        }

        [Fact]
        public void CopiedLicenceFile_DoesNotWorkOnAnotherComputer()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            Assert.True(m.Check(Client, CheckTrigger.Manual, true).CanExport);

            var other = new LicenseManager(_server, Store, new FakeMachine { MachineHash = "other" }, null, () => _now);
            Assert.Equal(LicenseState.Invalid, other.Evaluate().State);
        }

        [Fact]
        public void Offline_ApprovalWorksUntilSignedExpiry_ThenNeedsTheServer()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            m.Check(Client, CheckTrigger.Manual, true);
            _server.Offline = true;

            _now = _now.AddDays(13);
            LicenseStatus s = m.Check(Client, CheckTrigger.Command);
            Assert.Equal(LicenseState.Approved, s.State);
            Assert.NotNull(s.NetworkError);

            _now = _now.AddDays(2);
            s = m.Check(Client, CheckTrigger.Command);
            Assert.Equal(LicenseState.Expired, s.State);
            Assert.False(s.CanExport);

            _server.Offline = false;
            _now = _now.AddMinutes(1);
            Assert.Equal(LicenseState.Approved, m.Check(Client, CheckTrigger.Command).State);
        }

        [Fact]
        public void ClockTurnedBack_DistrustsTheCachedApproval()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            m.Check(Client, CheckTrigger.Manual, true);
            _now = _now.AddDays(3);
            Assert.Equal(LicenseState.Approved, m.Evaluate().State); // remembers "last seen"

            _now = _now.AddDays(-2);
            LicenseStatus s = m.Evaluate();
            Assert.Equal(LicenseState.Expired, s.State);
            Assert.Equal("clock", s.Detail);
        }

        [Fact]
        public void PendingStaysDisabled_WhenOffline()
        {
            LicenseManager m = Manager();
            m.Register("Ali", "ali@example.com", Client);
            _server.Offline = true;
            _now = _now.AddMinutes(5);
            LicenseStatus s = m.Check(Client, CheckTrigger.Command);
            Assert.Equal(LicenseState.Pending, s.State);
            Assert.NotNull(s.NetworkError);
        }

        [Fact]
        public void Rejection_AndRevocation_DisableExport()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            Assert.True(m.Check(Client, CheckTrigger.Manual, true).CanExport);
            m.MarkNotified(RemoteStatus.Approved);

            _server.Decide(id, "rejected");
            _now = _now.AddHours(1);
            LicenseStatus s = m.Check(Client, CheckTrigger.Periodic);
            Assert.Equal(LicenseState.Rejected, s.State);
            Assert.Equal(RemoteStatus.Rejected, s.ChangeToNotify);
            Assert.Null(Store.Load().Token); // the old approval is gone, also offline

            _server.Offline = true;
            Assert.Equal(LicenseState.Rejected, m.Evaluate().State);
        }

        [Fact]
        public void UnknownInstallation_GoesBackToRegistration()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Entries.Remove(id);
            LicenseStatus s = m.Check(Client, CheckTrigger.Manual, true);
            Assert.Equal(LicenseState.NotRegistered, s.State);
            Assert.Equal(id, s.InstallId);
        }

        [Fact]
        public void ClientKeyMismatch_StartsANewIdentity()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Entries[id].ClientKeyHash = "someone-else";
            LicenseStatus s = m.Check(Client, CheckTrigger.Manual, true);
            Assert.Equal(LicenseState.NotRegistered, s.State);
            Assert.NotEqual(id, Manager().InstallId);
        }

        // ---- throttling ------------------------------------------------------------------------------------

        [Fact]
        public void Checks_AreThrottled()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            int calls = _server.StatusCalls;

            m.Check(Client, CheckTrigger.Command); // pending: < 20 s since registration
            Assert.Equal(calls, _server.StatusCalls);
            _now = _now.AddSeconds(25);
            m.Check(Client, CheckTrigger.Command);
            Assert.Equal(calls + 1, _server.StatusCalls);

            _server.Decide(id, "approved");
            m.Check(Client, CheckTrigger.Manual, true);
            calls = _server.StatusCalls;
            _now = _now.AddMinutes(10);
            m.Check(Client, CheckTrigger.Command); // approved: 30 min
            m.Check(Client, CheckTrigger.Periodic);
            Assert.Equal(calls, _server.StatusCalls);
            _now = _now.AddMinutes(21);
            m.Check(Client, CheckTrigger.Command);
            Assert.Equal(calls + 1, _server.StatusCalls);
        }

        [Fact]
        public void ServerCheckAfterHint_IsHonoured()
        {
            LicenseManager m = Manager();
            m.Register("Ali", "ali@example.com", Client);
            _server.ForceError = "rate_limited";
            _server.CheckAfter = 600;
            _now = _now.AddMinutes(1);
            m.Check(Client, CheckTrigger.Command);
            int calls = _server.StatusCalls;

            _now = _now.AddMinutes(5);
            m.Check(Client, CheckTrigger.Manual);
            Assert.Equal(calls, _server.StatusCalls);
            _now = _now.AddMinutes(6);
            m.Check(Client, CheckTrigger.Manual);
            Assert.Equal(calls + 1, _server.StatusCalls);
        }

        // ---- key trust ---------------------------------------------------------------------------------------

        [Fact]
        public void PinnedKey_IsTheOnlyKeyAccepted()
        {
            using (var other = new FakeServer())
            {
                // Built-in key = other server's key; this server signs with its own key → not accepted.
                var options = new LicenseOptions { PinnedPublicKey = other.PublicKeyBase64 };
                LicenseManager m = Manager(options);
                string id = m.Register("Ali", "ali@example.com", Client).InstallId;
                _server.Decide(id, "approved");
                Assert.Equal(LicenseState.Invalid, m.Check(Client, CheckTrigger.Manual, true).State);

                // The server that owns the pinned key is accepted.
                _server.UseKeyOf(other);
                _now = _now.AddMinutes(1);
                Assert.Equal(LicenseState.Approved, m.Check(Client, CheckTrigger.Manual, true).State);
            }
        }

        [Fact]
        public void ServerKeyWithWrongKeyId_IsIgnored()
        {
            _server.KeyIdOverride = "0000000000000000";
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            _server.Decide(id, "approved");
            Assert.Equal(LicenseState.Invalid, m.Check(Client, CheckTrigger.Manual, true).State);
        }

        // ---- store -------------------------------------------------------------------------------------------

        [Fact]
        public void Store_RoundTrips_AndSurvivesDamage()
        {
            var rec = new LicenseRecord
            {
                InstallId = InstallationIdentity.NewInstallId(),
                ClientKey = InstallationIdentity.NewClientKey(),
                Name = "Ali = Vali & Co\nnewline",
                Email = "ali@example.com",
                RequestId = "r1",
                Status = RemoteStatus.Pending,
                SubmittedUtc = _now,
                ClockOffsetSeconds = -42
            };
            Store.Save(rec);
            LicenseRecord back = Store.Load();
            Assert.Equal(rec.Name, back.Name);
            Assert.Equal(RemoteStatus.Pending, back.Status);
            Assert.Equal(_now, back.SubmittedUtc);
            Assert.Equal(-42, back.ClockOffsetSeconds);

            File.WriteAllText(Store.FilePath, "installId=broken\n");
            Assert.Null(Store.Load().InstallId);
        }

        [Fact]
        public void Store_FileBeingWrittenByAnotherProcess_IsNeverMistakenForNoLicence()
        {
            LicenseManager m = Manager();
            string id = m.Register("Ali", "ali@example.com", Client).InstallId;
            using (new FileStream(Store.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.ThrowsAny<IOException>(() => m.Evaluate());
            }
            Assert.Equal(id, m.Evaluate().InstallId); // identity and registration kept
            Assert.Equal(LicenseState.Pending, m.Evaluate().State);
        }

        // ---- HTTP client -------------------------------------------------------------------------------------

        [Fact]
        public void Http_PostsFormAndParsesReply()
        {
            string received = null;
            using (var listener = Listener(out string url, ctx =>
                   {
                       using (var r = new StreamReader(ctx.Request.InputStream)) received = r.ReadToEnd();
                       return "apdf=1\nok=1\nstatus=pending\nrequestId=req%2D1\nmessage=Kutilmoqda%20%E2%9C%93\nserverTime=1790000000\ncheckAfter=30\n";
                   }))
            using (var http = new HttpLicenseServer(url, "3.0.0"))
            {
                LicenseReply reply = http.Register(new LicenseRequest
                {
                    InstallId = "APDF-AAAA-BBBB-CCCC-DDDD", ClientKey = "k", MachineHash = "m", Name = "Ali Vali", Email = "a@b.uz", Host = "AutoCAD 2024"
                });
                Assert.True(reply.Reached);
                Assert.True(reply.Ok);
                Assert.Equal(RemoteStatus.Pending, reply.Status);
                Assert.Equal("req-1", reply.RequestId);
                Assert.Equal("Kutilmoqda ✓", reply.Message);
                Assert.Equal(30, reply.CheckAfterSeconds);
                Assert.Contains("action=register", received);
                Assert.Contains("name=Ali%20Vali", received);
                Assert.Contains("consent=1", received);
                Assert.Contains("host=AutoCAD%202024", received);
            }
        }

        [Fact]
        public void Http_NonProtocolReplyOrErrorStatus_IsUnreachable()
        {
            using (var listener = Listener(out string url, ctx => "<html>Sign in - Google Accounts</html>"))
            using (var http = new HttpLicenseServer(url, "3.0.0"))
            {
                LicenseReply reply = http.Status(new LicenseRequest { InstallId = "x" });
                Assert.False(reply.Reached);
                Assert.Contains("Unexpected reply", reply.NetworkError);
            }

            using (var listener = Listener(out string url, ctx => { ctx.Response.StatusCode = 500; return "boom"; }))
            using (var http = new HttpLicenseServer(url, "3.0.0"))
            {
                Assert.False(http.Status(new LicenseRequest()).Reached);
            }
        }

        [Fact]
        public void Http_RequiresHttpsExceptLoopback()
        {
            Assert.Throws<ArgumentException>(() => new HttpLicenseServer("http://example.com/exec", "1"));
            Assert.True(HttpLicenseServer.IsAcceptableEndpoint("https://script.google.com/macros/s/x/exec", out _));
            Assert.True(LicenseConfig.IsConfigured);
            Assert.StartsWith("https://", LicenseConfig.Endpoint);
        }

        private static HttpListener Listener(out string url, Func<HttpListenerContext, string> handler)
        {
            int port = FreePort();
            url = "http://127.0.0.1:" + port + "/exec/";
            var listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Task.Run(() =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        HttpListenerContext ctx = listener.GetContext();
                        string body = handler(ctx);
                        byte[] bytes = Encoding.UTF8.GetBytes(body);
                        ctx.Response.ContentType = "text/plain; charset=utf-8";
                        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        ctx.Response.Close();
                    }
                }
                catch
                {
                    // stopped
                }
            });
            return listener;
        }

        private static int FreePort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        // ---- fakes -------------------------------------------------------------------------------------------

        private sealed class FakeMachine : IMachineIdentity
        {
            public string MachineHash { get; set; } = "9f2c" + new string('a', 60);
            public string ComputerName => "PC-01";
            public string WindowsUser => "ali";
            public string OsDescription => "Windows 11";
        }

        /// <summary>Behaves like server/licensing/Code.gs (register, status, decisions, signed tokens).</summary>
        private sealed class FakeServer : ILicenseServer, IDisposable
        {
            public sealed class Entry
            {
                public string ClientKeyHash, Machine, Email, RequestId, Status = "pending";
            }

            private RSA _rsa = RSA.Create(2048);

            public Dictionary<string, Entry> Entries { get; } = new Dictionary<string, Entry>();
            public bool Offline { get; set; }
            public string ForceError { get; set; }
            public int CheckAfter { get; set; }
            public string TokenDefect { get; set; }
            public string KeyIdOverride { get; set; }
            public int StatusCalls { get; private set; }
            public LicenseRequest LastRequest { get; private set; }
            public DateTime Now { get; set; } = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

            public string PublicKeyBase64 => Convert.ToBase64String(_rsa.ExportSubjectPublicKeyInfo());

            public void UseKeyOf(FakeServer other) => _rsa = other._rsa;

            public void Decide(string installId, string status) => Entries[installId].Status = status;

            public LicenseReply Register(LicenseRequest r)
            {
                LastRequest = r;
                if (Offline) return LicenseReply.Unreachable("No such host is known.");
                if (ForceError != null) return Error(ForceError);
                if (!Entries.TryGetValue(r.InstallId, out Entry e))
                {
                    e = new Entry { ClientKeyHash = InstallationIdentity.Sha256Hex(r.ClientKey), RequestId = Guid.NewGuid().ToString() };
                    Entries[r.InstallId] = e;
                }
                else if (e.ClientKeyHash != InstallationIdentity.Sha256Hex(r.ClientKey))
                {
                    return Error("client_mismatch");
                }
                e.Machine = r.MachineHash;
                e.Email = r.Email;
                return Reply(r, e);
            }

            public LicenseReply Status(LicenseRequest r)
            {
                StatusCalls++;
                LastRequest = r;
                if (Offline) return LicenseReply.Unreachable("The operation has timed out.");
                if (ForceError != null) return Error(ForceError);
                if (!Entries.TryGetValue(r.InstallId, out Entry e)) return Error("unknown_install");
                if (e.ClientKeyHash != InstallationIdentity.Sha256Hex(r.ClientKey)) return Error("client_mismatch");
                return Reply(r, e);
            }

            private LicenseReply Error(string code) =>
                new LicenseReply { Reached = true, Ok = false, Error = code, Message = code, CheckAfterSeconds = CheckAfter };

            private LicenseReply Reply(LicenseRequest r, Entry e)
            {
                LicenseToken.TryParseStatus(e.Status, out RemoteStatus status);
                var reply = new LicenseReply
                {
                    Reached = true,
                    Ok = true,
                    Status = status,
                    InstallId = r.InstallId,
                    RequestId = e.RequestId,
                    ServerTimeUtc = Now,
                    PublicKey = PublicKeyBase64,
                    KeyId = KeyIdOverride ?? RsaPublicKey.ComputeKeyId(_rsa.ExportSubjectPublicKeyInfo())
                };
                if (status == RemoteStatus.Approved)
                {
                    string machine = TokenDefect == "machine" ? "other-machine" : e.Machine;
                    string install = TokenDefect == "installation" ? "APDF-ZZZZ-ZZZZ-ZZZZ-ZZZZ" : r.InstallId;
                    RemoteStatus st = TokenDefect == "status" ? RemoteStatus.Pending : RemoteStatus.Approved;
                    reply.Token = LicenseToken.Format(install, machine, st, e.RequestId, Now, Now.AddDays(14));
                    string signed = TokenDefect == "signature" ? reply.Token + "x" : reply.Token;
                    reply.Signature = Convert.ToBase64String(_rsa.SignData(Encoding.UTF8.GetBytes(signed), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                }
                return reply;
            }

            public void Dispose() => _rsa.Dispose();
        }
    }
}
