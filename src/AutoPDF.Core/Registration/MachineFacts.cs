using System;
using System.Security.Cryptography;
using System.Text;

namespace AutoPDF.Core.Registration
{
    /// <summary>
    /// Non-personal facts about this computer that the registration reports so the author can see
    /// "which machine" a user installed on. Everything is read from <see cref="Environment"/>; nothing here
    /// touches the network or any private data.
    /// </summary>
    public static class MachineFacts
    {
        public static string Computer => Safe(() => Environment.MachineName);

        public static string WindowsUser => Safe(() => Environment.UserName);

        public static string Os => Safe(() => Environment.OSVersion.VersionString);

        /// <summary>A short, stable id for this computer+user (hash), so duplicate installs are recognisable.</summary>
        public static string MachineId()
        {
            string basis = (Computer + "|" + WindowsUser).ToLowerInvariant();
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(basis));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static string Safe(Func<string> read)
        {
            try { return read() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
