using System;

namespace AutoPDF.Core.Registration
{
    /// <summary>
    /// How the plug-in registers a user. The registration is sent to a Google Apps Script web app that the
    /// author deploys (installer/registration/Code.gs); that script — running inside the author's own Google
    /// account — e-mails the author and appends the user to a Google Sheet. No password or secret is ever
    /// shipped inside the plug-in: the only value here is the public web-app URL (like a form address).
    /// </summary>
    public static class RegistrationConfig
    {
        /// <summary>
        /// The Apps Script web-app URL (…/exec). Deploy installer/registration/Code.gs, then put its /exec
        /// URL here and rebuild. While it is the placeholder below, registration is DISABLED and the plug-in
        /// works exactly as before — so a build without the URL is still fully usable.
        /// </summary>
        public const string Endpoint = "https://script.google.com/macros/s/AKfycbxLq4SmB2WQX-0CnlGJO20ICEyisTERPpscwuFZAY-ENsPPIaP7-uR_G9QCQ8JAu7r4jw/exec";

        /// <summary>Registration is required before AUTOPDF can be used (once <see cref="Endpoint"/> is set).</summary>
        public const bool Mandatory = true;

        /// <summary>
        /// When the web app cannot be reached, let the user continue and re-send the registration on a later
        /// start, so a restricted-internet workstation is not permanently locked out. The author still
        /// receives the registration as soon as the machine is online. Set to false for strict online-only.
        /// </summary>
        public const bool AllowOfflineContinue = true;

        /// <summary>
        /// Recognised as the administrator (only used to hint that AUTOPDFADMIN is available). This is not a
        /// security boundary: the user list is protected by the admin key that the server checks.
        /// </summary>
        public static readonly string[] AdminEmails = { "oybekkushmuratov88@gmail.com" };

        /// <summary>True once a real https web-app URL has been set (i.e. registration is switched on).</summary>
        public static bool IsConfigured => IsHttpsUrl(Endpoint);

        public static bool IsAdmin(string email) =>
            !string.IsNullOrWhiteSpace(email) &&
            Array.Exists(AdminEmails, a => string.Equals(a.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));

        internal static bool IsHttpsUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out Uri u) && u.Scheme == Uri.UriSchemeHttps;
    }
}
