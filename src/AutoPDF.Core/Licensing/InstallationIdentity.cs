using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoPDF.Core.Licensing
{
    /// <summary>
    /// Identifiers of this installation. The installation ID is random (not derived from personal data), shown
    /// to the user and the administrator, and sent with every request. The client key is a second random
    /// value known only to this installation and the server (which stores just its SHA-256), so nobody who
    /// merely knows an installation ID can query or take over its licence.
    /// </summary>
    public static class InstallationIdentity
    {
        // 32 symbols without the look-alikes I, O, 0, 1 → 5 bits each; 16 symbols = 80 random bits.
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private static readonly Regex InstallIdPattern =
            new Regex("^APDF(-[A-HJ-NP-Z2-9]{4}){4}$", RegexOptions.CultureInvariant);

        private static readonly Regex ClientKeyPattern =
            new Regex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant);

        /// <summary>E.g. "APDF-7K3M-Q9TX-2FHC-WN4R".</summary>
        public static string NewInstallId()
        {
            byte[] random = RandomBytes(16);
            var sb = new StringBuilder("APDF");
            for (int i = 0; i < 16; i++)
            {
                if (i % 4 == 0) sb.Append('-');
                sb.Append(Alphabet[random[i] & 31]);
            }
            return sb.ToString();
        }

        /// <summary>256 random bits, base64url without padding (43 characters).</summary>
        public static string NewClientKey() => Base64Url(RandomBytes(32));

        /// <summary>Unique id of one request (idempotency / audit on the server).</summary>
        public static string NewNonce() => Guid.NewGuid().ToString("N");

        public static bool IsValidInstallId(string id) => id != null && InstallIdPattern.IsMatch(id);

        public static bool IsValidClientKey(string key) => key != null && ClientKeyPattern.IsMatch(key);

        public static string Sha256Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                var sb = new StringBuilder(64);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return bytes;
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Binds a licence to one computer + Windows user (a copied licence file does not work elsewhere).</summary>
    public interface IMachineIdentity
    {
        /// <summary>SHA-256 hex of stable machine facts; never the raw values.</summary>
        string MachineHash { get; }

        string ComputerName { get; }
        string WindowsUser { get; }
        string OsDescription { get; }
    }

    public sealed class MachineIdentity : IMachineIdentity
    {
        private string _hash;

        public string ComputerName => Safe(() => Environment.MachineName);
        public string WindowsUser => Safe(() => Environment.UserName);
        public string OsDescription => Safe(() => Environment.OSVersion.VersionString);

        public string MachineHash => _hash ?? (_hash = InstallationIdentity.Sha256Hex(
            "autopdf|" + ReadMachineGuid() + "|" + Safe(() => Environment.UserDomainName) + "\\" + WindowsUser).ToLowerInvariant());

        /// <summary>Windows' per-installation MachineGuid (64-bit view); the computer name when unavailable.</summary>
        private string ReadMachineGuid()
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
#pragma warning disable CA1416 // guarded by the platform check above
                    using (var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64))
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                    {
                        string guid = key?.GetValue("MachineGuid") as string;
                        if (!string.IsNullOrWhiteSpace(guid)) return guid.Trim().ToLowerInvariant();
                    }
#pragma warning restore CA1416
                }
            }
            catch
            {
                // fall through
            }
            return ComputerName.ToLowerInvariant();
        }

        private static string Safe(Func<string> read)
        {
            try { return read() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
