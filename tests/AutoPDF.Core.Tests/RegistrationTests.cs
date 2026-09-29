using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AutoPDF.Core.Registration;
using Xunit;

namespace AutoPDF.Core.Tests
{
    public class RegistrationTests
    {
        // ---- Value logic --------------------------------------------------------------------------

        [Theory]
        [InlineData("a@b.com", true)]
        [InlineData("oybek.kushmuratov@gmail.com", true)]
        [InlineData("user@sub.domain.uz", true)]
        [InlineData("no-at-sign", false)]
        [InlineData("two@@at.com", false)]
        [InlineData("space in@mail.com", false)]
        [InlineData("trailing@dot.", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void EmailValidator_Works(string email, bool expected) =>
            Assert.Equal(expected, EmailValidator.IsValid(email));

        [Fact]
        public void HasCompleted_NeedsEmailAndTime()
        {
            Assert.False(new RegistrationInfo().HasCompleted);
            Assert.False(new RegistrationInfo { Email = "a@b.com" }.HasCompleted);
            Assert.False(new RegistrationInfo { RegisteredAtUtc = "2026-01-01T00:00:00Z" }.HasCompleted);
            Assert.True(new RegistrationInfo { Email = "a@b.com", RegisteredAtUtc = "2026-01-01T00:00:00Z" }.HasCompleted);
        }

        [Fact]
        public void MachineFacts_AreStableAndNonEmpty()
        {
            Assert.False(string.IsNullOrEmpty(MachineFacts.Computer));
            Assert.False(string.IsNullOrEmpty(MachineFacts.WindowsUser));
            string id1 = MachineFacts.MachineId();
            string id2 = MachineFacts.MachineId();
            Assert.Equal(id1, id2);
            Assert.Equal(16, id1.Length);
            Assert.All(id1, c => Assert.True(Uri.IsHexDigit(c)));
        }

        [Fact]
        public void Config_HttpsDetectionAndAdmin()
        {
            Assert.False(RegistrationConfig.IsHttpsUrl("__REGISTRATION_ENDPOINT__")); // the placeholder
            Assert.False(RegistrationConfig.IsHttpsUrl("http://example.com/x"));       // must be https
            Assert.True(RegistrationConfig.IsHttpsUrl("https://script.google.com/macros/s/AAA/exec"));
            Assert.Equal(RegistrationConfig.IsHttpsUrl(RegistrationConfig.Endpoint), RegistrationConfig.IsConfigured);
            Assert.True(RegistrationConfig.IsAdmin("Oybekkushmuratov88@gmail.com"));
            Assert.False(RegistrationConfig.IsAdmin("someone@else.com"));
        }

        [Fact]
        public void Store_RoundTripsAndSurvivesMissingOrBrokenFile()
        {
            string dir = Path.Combine(Path.GetTempPath(), "apdf-reg-" + Guid.NewGuid());
            var store = new RegistrationStore(Path.Combine(dir, "registration.xml"));
            try
            {
                Assert.Null(store.Load()); // missing file

                var info = new RegistrationInfo
                {
                    FullName = "Oybek Kushmuratov",
                    Email = "a@b.com",
                    Phone = "+998 90 123 45 67",
                    Computer = "WS-01",
                    WindowsUser = "oybek",
                    MachineId = "abc123",
                    Version = "2.1.0",
                    Language = "Uzbek",
                    RegisteredAtUtc = "2026-09-29T05:00:00Z",
                    Delivered = false,
                    Attempts = 2
                };
                store.Save(info);

                RegistrationInfo back = store.Load();
                Assert.NotNull(back);
                Assert.Equal("a@b.com", back.Email);
                Assert.Equal("+998 90 123 45 67", back.Phone);
                Assert.False(back.Delivered);
                Assert.Equal(2, back.Attempts);
                Assert.True(back.HasCompleted);

                File.WriteAllText(store.FilePath, "<not-valid-xml>");
                Assert.Null(store.Load()); // broken file → null, no throw
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void ParseUsers_ReadsTsvRowsAndSkipsStatusLine()
        {
            string body = "OK\n" +
                          "2026-09-29 05:10\tOybek\ta@b.com\t+998\tWS-01\toybek\t2.1.0\n" +
                          "2026-09-29 06:00\tAli\tali@c.uz\t\tWS-02\tali\t2.1.0\n";
            List<RegisteredUser> users = HttpRegistrationGateway.ParseUsers(body);
            Assert.Equal(2, users.Count);
            Assert.Equal(1, users[0].Number);
            Assert.Equal("Oybek", users[0].FullName);
            Assert.Equal("a@b.com", users[0].Email);
            Assert.Equal("WS-01", users[0].Computer);
            Assert.Equal(2, users[1].Number);
            Assert.Equal("ali@c.uz", users[1].Email);
            Assert.Equal(string.Empty, users[1].Phone);
        }

        // ---- Gateway over a real local HTTP server ------------------------------------------------

        [Fact]
        public void Submit_PostsFieldsAndSucceedsOnOk()
        {
            IDictionary<string, string> received = null;
            using (var server = new TestServer(ctx =>
            {
                received = ReadForm(ctx.Request);
                Respond(ctx, 200, "OK");
            }))
            using (var gateway = new HttpRegistrationGateway(server.Url, "2.1.0"))
            {
                var info = new RegistrationInfo
                {
                    FullName = "Oʻzbek Ism",
                    Email = "user@mail.com",
                    Phone = "+998901234567",
                    Computer = "WS-01",
                    WindowsUser = "oybek",
                    MachineId = "deadbeef",
                    Os = "Windows 10",
                    Version = "2.1.0",
                    Language = "Uzbek"
                };
                SubmitResult result = gateway.Submit(info);

                Assert.True(result.Ok, result.Error);
                Assert.NotNull(received);
                Assert.Equal("register", received["action"]);
                Assert.Equal("user@mail.com", received["email"]);
                Assert.Equal("Oʻzbek Ism", received["name"]); // Unicode survives UTF-8 form encoding
                Assert.Equal("WS-01", received["computer"]);
                Assert.Equal("2.1.0", received["version"]);
            }
        }

        [Fact]
        public void Submit_FailsOnServerError()
        {
            using (var server = new TestServer(ctx => Respond(ctx, 500, "boom")))
            using (var gateway = new HttpRegistrationGateway(server.Url, "2.1.0"))
            {
                SubmitResult result = gateway.Submit(new RegistrationInfo { Email = "a@b.com" });
                Assert.False(result.Ok);
                Assert.Contains("500", result.Error);
            }
        }

        [Fact]
        public void Submit_FailsGracefullyWhenServerIsUnreachable()
        {
            string deadUrl = "http://127.0.0.1:" + FreePort() + "/reg"; // nothing is listening
            using (var gateway = new HttpRegistrationGateway(deadUrl, "2.1.0", TimeSpan.FromSeconds(3)))
            {
                SubmitResult result = gateway.Submit(new RegistrationInfo { Email = "a@b.com" });
                Assert.False(result.Ok);
                Assert.True(result.NetworkProblem);
            }
        }

        [Fact]
        public void FetchUsers_ParsesListAndRejectsWrongKey()
        {
            using (var server = new TestServer(ctx =>
            {
                string key = ctx.Request.QueryString["key"];
                if (key == "secret-123")
                    Respond(ctx, 200, "OK\n2026-09-29\tOybek\ta@b.com\t\tWS-01\toybek\t2.1.0\n");
                else
                    Respond(ctx, 200, "FORBIDDEN");
            }))
            using (var gateway = new HttpRegistrationGateway(server.Url, "2.1.0"))
            {
                UsersResult ok = gateway.FetchUsers("secret-123");
                Assert.True(ok.Ok, ok.Error);
                Assert.Single(ok.Users);
                Assert.Equal("a@b.com", ok.Users[0].Email);

                UsersResult bad = gateway.FetchUsers("wrong");
                Assert.False(bad.Ok);
                Assert.Contains("FORBIDDEN", bad.Error);
            }
        }

        // ---- Test HTTP server ---------------------------------------------------------------------

        private static int FreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static void Respond(HttpListenerContext ctx, int status, string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "text/plain; charset=utf-8";
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static IDictionary<string, string> ReadForm(HttpListenerRequest request)
        {
            string raw;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                raw = reader.ReadToEnd();

            var dict = new Dictionary<string, string>();
            foreach (string pair in raw.Split('&'))
            {
                if (pair.Length == 0) continue;
                int eq = pair.IndexOf('=');
                string key = eq >= 0 ? pair.Substring(0, eq) : pair;
                string value = eq >= 0 ? pair.Substring(eq + 1) : string.Empty;
                dict[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
            return dict;
        }

        private sealed class TestServer : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();
            private readonly Thread _thread;

            public TestServer(Action<HttpListenerContext> handler)
            {
                int port = FreePort();
                Url = "http://127.0.0.1:" + port + "/reg";
                _listener.Prefixes.Add("http://127.0.0.1:" + port + "/reg/");
                // HttpListener matches on the path prefix; requests to /reg resolve to the /reg/ prefix.
                _listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                _listener.Start();
                _thread = new Thread(() =>
                {
                    try
                    {
                        while (_listener.IsListening)
                        {
                            HttpListenerContext ctx = _listener.GetContext();
                            try { handler(ctx); }
                            catch { try { ctx.Response.Abort(); } catch { /* ignore */ } }
                        }
                    }
                    catch (HttpListenerException) { /* stopped */ }
                    catch (ObjectDisposedException) { /* stopped */ }
                })
                { IsBackground = true };
                _thread.Start();
            }

            public string Url { get; }

            public void Dispose()
            {
                try { _listener.Stop(); } catch { /* ignore */ }
                try { _listener.Close(); } catch { /* ignore */ }
            }
        }
    }
}
