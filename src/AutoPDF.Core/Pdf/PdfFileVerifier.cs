using System;
using System.IO;
using System.Text;

namespace AutoPDF.Core.Pdf
{
    public sealed class PdfFileCheck
    {
        public bool Exists { get; set; }
        public long Length { get; set; }
        public bool HasHeader { get; set; }
        public bool HasEndMarker { get; set; }
        public bool IsValid => Exists && Length > 0 && HasHeader && HasEndMarker;
        public string Problem { get; set; }
    }

    /// <summary>Structural checks of a produced PDF: exists, non-empty, "%PDF-" header, "%%EOF" trailer.</summary>
    public static class PdfFileVerifier
    {
        private const int TailBytes = 2048;

        public static PdfFileCheck Verify(string path)
        {
            var check = new PdfFileCheck();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                check.Problem = "PDF fayl yaratilmadi: " + path;
                return check;
            }

            check.Exists = true;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    check.Length = stream.Length;
                    if (check.Length == 0)
                    {
                        check.Problem = "PDF fayl bo‘sh (0 bayt).";
                        return check;
                    }

                    var head = new byte[Math.Min(1024, (int)Math.Min(int.MaxValue, check.Length))];
                    int read = stream.Read(head, 0, head.Length);
                    // The header must be within the first 1024 bytes (PDF spec, implementation note).
                    check.HasHeader = Encoding.ASCII.GetString(head, 0, read).Contains("%PDF-");

                    long tailStart = Math.Max(0, check.Length - TailBytes);
                    stream.Seek(tailStart, SeekOrigin.Begin);
                    var tail = new byte[check.Length - tailStart];
                    int tailRead = stream.Read(tail, 0, tail.Length);
                    check.HasEndMarker = Encoding.ASCII.GetString(tail, 0, tailRead).Contains("%%EOF");
                }
            }
            catch (Exception ex)
            {
                check.Problem = "PDF faylni o‘qib bo‘lmadi: " + ex.Message;
                return check;
            }

            if (!check.HasHeader) check.Problem = "Fayl PDF emas (%PDF- sarlavhasi yo‘q).";
            else if (!check.HasEndMarker) check.Problem = "PDF fayl to‘liq yozilmagan (%%EOF yo‘q).";
            return check;
        }

        /// <summary>True when the file can be opened exclusively (not open in a PDF viewer).</summary>
        public static bool IsWritable(string path, out string problem)
        {
            problem = null;
            if (!File.Exists(path)) return true;
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                problem = ex.Message;
                return false;
            }
        }
    }
}
