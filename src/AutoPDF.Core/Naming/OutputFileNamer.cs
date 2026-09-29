using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AutoPDF.Core.Localization;

namespace AutoPDF.Core.Naming
{
    /// <summary>Builds output file names: 01.pdf, 02.pdf … (pattern tokens {N}, {DWG}, {FORMAT}).</summary>
    public static class OutputFileNamer
    {
        public const string DefaultPattern = "{N}";

        /// <summary>At least two digits; more when the last number needs them (100 → 3 digits).</summary>
        public static int PadWidth(int maxNumber) =>
            Math.Max(2, Math.Abs(maxNumber).ToString(CultureInfo.InvariantCulture).Length);

        public static string BuildFileName(string pattern, int number, int padWidth, string drawingName = null, string formatName = null)
        {
            if (string.IsNullOrWhiteSpace(pattern)) pattern = DefaultPattern;
            string n = number.ToString(CultureInfo.InvariantCulture).PadLeft(padWidth, '0');

            string name = pattern
                .Replace("{N}", n)
                .Replace("{n}", n)
                .Replace("{DWG}", drawingName ?? string.Empty)
                .Replace("{dwg}", drawingName ?? string.Empty)
                .Replace("{FORMAT}", formatName ?? string.Empty)
                .Replace("{format}", formatName ?? string.Empty);

            name = Sanitize(name).Trim().TrimEnd('.');
            if (name.Length == 0) name = n;
            if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) name += ".pdf";
            return name;
        }

        public static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            char[] invalid = Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).ToArray();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(invalid.Contains(c) || char.IsControl(c) ? '_' : c);
            }
            return sb.ToString();
        }

        /// <summary>"01.pdf" → "01_1.pdf", "01_2.pdf" … until <paramref name="exists"/> returns false.</summary>
        public static string MakeUnique(string path, Func<string, bool> exists)
        {
            if (!exists(path)) return path;
            string dir = Path.GetDirectoryName(path) ?? string.Empty;
            string stem = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            for (int i = 1; i < 10000; i++)
            {
                string candidate = Path.Combine(dir, stem + "_" + i.ToString(CultureInfo.InvariantCulture) + ext);
                if (!exists(candidate)) return candidate;
            }
            throw new IOException(L.NoFreeFileName(path));
        }
    }
}
