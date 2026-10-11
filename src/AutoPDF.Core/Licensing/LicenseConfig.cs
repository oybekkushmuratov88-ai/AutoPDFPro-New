using System.Text.RegularExpressions;

namespace AutoPDF.Core.Licensing
{
    /// <summary>
    /// Where the licence server is. The client holds no administrator secret: the URL is public (anyone may
    /// submit a registration request, which only the administrator can approve) and the optional key below is
    /// a PUBLIC key that can only verify signatures.
    /// </summary>
    public static class LicenseConfig
    {
        /// <summary>
        /// The Google Apps Script web app (…/exec) running server/licensing/Code.gs in the administrator's
        /// Google account. Redeploying a new version of the same deployment keeps this URL.
        /// </summary>
        public const string Endpoint = "https://script.google.com/macros/s/AKfycbxLq4SmB2WQX-0CnlGJO20ICEyisTERPpscwuFZAY-ENsPPIaP7-uR_G9QCQ8JAu7r4jw/exec";

        /// <summary>
        /// Optional built-in copy of the server's signing key ("-----BEGIN PUBLIC KEY-----…", shown by the sheet
        /// menu AutoPDF ▸ Show public key). Empty: the key is learnt from the server over HTTPS at registration.
        /// Set: only this key is ever accepted (strict mode; a changed server key then requires a new build).
        /// </summary>
        public const string PinnedPublicKey = "";

        public static bool IsConfigured => HttpLicenseServer.IsAcceptableEndpoint(Endpoint, out _);
    }

    /// <summary>A lenient e-mail check — enough to catch typos, never stricter than real addresses.</summary>
    public static class EmailValidator
    {
        private static readonly Regex Pattern =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsValid(string email) =>
            !string.IsNullOrWhiteSpace(email) && email.Length <= 254 && Pattern.IsMatch(email.Trim());
    }
}
