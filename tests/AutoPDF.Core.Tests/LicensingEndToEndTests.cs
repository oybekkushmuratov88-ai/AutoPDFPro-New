using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using AutoPDF.Core.Licensing;
using Xunit;
using Xunit.Abstractions;

namespace AutoPDF.Core.Tests
{
    /// <summary>
    /// The real C# client against the real server/licensing/Code.gs (run by Node with mocked Google services,
    /// see server/licensing/test/serve.js): registration → pending → administrator approves → a licence signed
    /// by the script verifies in .NET → revocation → a copied licence file on another computer.
    /// Needs Node.js on PATH; set AUTOPDF_E2E=1 to make a missing Node a failure instead of a no-op.
    /// </summary>
    public sealed class LicensingEndToEndTests : IDisposable
    {
        private readonly ITestOutputHelper _out;
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "autopdf-e2e-" + Guid.NewGuid().ToString("N"));
        private Process _node;

        public LicensingEndToEndTests(ITestOutputHelper output)
        {
            _out = output;
        }

        public void Dispose()
        {
            try { if (_node != null && !_node.HasExited) _node.Kill(); } catch { /* gone */ }
            try { Directory.Delete(_dir, true); } catch { /* temp */ }
        }

        [Fact]
        public void ClientAndAppsScript_AgreeOnTheWholeApprovalFlow()
        {
            string url = StartServer();
            if (url == null) return;

            var client = new ClientInfo { AppVersion = "3.0.0", Host = "AutoCAD 2026", Language = "English" };
            var store = new LicenseStore(Path.Combine(_dir, "license.txt"));
            var machine = new MachineIdentity();
            using (var http = new HttpLicenseServer(url, "3.0.0"))
            {
                var manager = new LicenseManager(http, store, machine);
                Assert.Equal(LicenseState.NotRegistered, manager.Evaluate().State);
                LicenseStatus s = manager.Register("E2E User", "e2e@example.com", client);
                Assert.True(s.CheckedOnline, s.NetworkError + s.ServerError);
                Assert.Equal(LicenseState.Pending, s.State);
                Assert.False(s.CanExport);
                Assert.Equal(LicenseState.Pending, manager.Check(client, CheckTrigger.Manual, force: true).State);

                Admin(url, s.InstallId, "approved");
                s = manager.Check(client, CheckTrigger.Manual, force: true);
                Assert.Equal(LicenseState.Approved, s.State);
                Assert.True(s.CanExport);
                Assert.Equal(RemoteStatus.Approved, s.ChangeToNotify);
                Assert.InRange((s.ExpiresUtc.Value - DateTime.UtcNow).TotalDays, 13.9, 14.1);

                // Strict mode: the same key built into the client is accepted.
                var pinned = new LicenseManager(http, store, machine, new LicenseOptions { PinnedPublicKey = store.Load().ServerPublicKey });
                Assert.Equal(LicenseState.Approved, pinned.Evaluate().State);

                // A copy of the licence file on another computer is refused by the server and the client.
                string copy = Path.Combine(_dir, "copy.txt");
                File.Copy(store.FilePath, copy);
                var elsewhere = new LicenseManager(http, new LicenseStore(copy), new OtherMachine());
                Assert.Equal(LicenseState.Invalid, elsewhere.Evaluate().State);
                LicenseStatus moved = elsewhere.Check(client, CheckTrigger.Manual, force: true);
                Assert.Equal("machine_mismatch", moved.ServerError);
                Assert.Equal(LicenseState.NotRegistered, moved.State);

                Admin(url, s.InstallId, "rejected");
                s = manager.Check(client, CheckTrigger.Manual, force: true);
                Assert.Equal(LicenseState.Rejected, s.State);
                Assert.False(s.CanExport);
            }
        }

        private static void Admin(string url, string installId, string status)
        {
            using (var http = new HttpClient())
            {
                var admin = new Uri(new Uri(url), "/__admin?installId=" + Uri.EscapeDataString(installId) + "&status=" + status);
                HttpResponseMessage r = http.PostAsync(admin, new StringContent(string.Empty)).GetAwaiter().GetResult();
                Assert.True(r.IsSuccessStatusCode, r.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }

        private string StartServer()
        {
            string script = FindUp(Path.Combine("server", "licensing", "test", "serve.js"));
            string node = FindNode();
            bool required = Environment.GetEnvironmentVariable("AUTOPDF_E2E") == "1";
            if (script == null || node == null)
            {
                Assert.False(required, "AUTOPDF_E2E=1 but Node.js or serve.js was not found.");
                _out.WriteLine("Node.js not found: end-to-end licensing test not run.");
                return null;
            }

            var psi = new ProcessStartInfo(node)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add(script);
            psi.ArgumentList.Add("0");
            _node = Process.Start(psi);
            var read = System.Threading.Tasks.Task.Run(() => _node.StandardOutput.ReadLine());
            // stdin is redirected too: an inherited test-host stdin can keep Node from starting.
            Assert.True(read.Wait(TimeSpan.FromSeconds(20)), "serve.js did not report READY within 20 s.");
            string line = read.Result;
            Assert.True(line != null && line.StartsWith("READY "), "serve.js did not start: " + line);
            return line.Substring("READY ".Length).Trim();
        }

        private static string FindUp(string relative)
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            {
                string candidate = Path.Combine(d.FullName, relative);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static string FindNode()
        {
            string exe = Environment.OSVersion.Platform == PlatformID.Win32NT ? "node.exe" : "node";
            return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .Where(p => p.Length > 0)
                .Select(p => Path.Combine(p, exe))
                .FirstOrDefault(File.Exists);
        }

        private sealed class OtherMachine : IMachineIdentity
        {
            public string MachineHash => new string('b', 64);
            public string ComputerName => "OTHER-PC";
            public string WindowsUser => "someone";
            public string OsDescription => "Windows 10";
        }
    }
}
