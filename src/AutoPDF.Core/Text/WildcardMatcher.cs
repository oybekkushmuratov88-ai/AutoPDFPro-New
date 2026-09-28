using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoPDF.Core.Text
{
    /// <summary>
    /// Case-insensitive AutoCAD-style wildcard matching for block names:
    /// '*' any sequence, '?' any single character, '#' any digit. Several patterns separated by ',' or ';'.
    /// </summary>
    public sealed class WildcardMatcher
    {
        private readonly List<Regex> _patterns;

        public WildcardMatcher(string patternList)
        {
            _patterns = Split(patternList).Select(ToRegex).ToList();
        }

        public bool IsEmpty => _patterns.Count == 0;

        public bool IsMatch(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _patterns.Any(r => r.IsMatch(name));
        }

        public static IReadOnlyList<string> Split(string patternList)
        {
            if (string.IsNullOrWhiteSpace(patternList)) return Array.Empty<string>();
            return patternList
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Regex ToRegex(string pattern)
        {
            var sb = new StringBuilder("^");
            foreach (char c in pattern)
            {
                switch (c)
                {
                    case '*': sb.Append(".*"); break;
                    case '?': sb.Append('.'); break;
                    case '#': sb.Append("[0-9]"); break;
                    default: sb.Append(Regex.Escape(c.ToString())); break;
                }
            }
            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
