namespace AutoPDF.Core.Registration
{
    /// <summary>One row of the author's user list (returned by the web app for the admin view).</summary>
    public sealed class RegisteredUser
    {
        public int Number { get; set; }
        public string Date { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Computer { get; set; }
        public string WindowsUser { get; set; }
        public string Version { get; set; }
    }
}
