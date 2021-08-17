using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDecrypt.modules.language
{
    /// <summary>
    /// Class <c>StringExtension</c> provides string-cleaning utilities for decryption scoring.
    /// </summary>
    internal static class StringScorerExtension
    {
        private static readonly Dictionary<char, string> _replacements = new();

        static StringScorerExtension()
        {
            foreach (char c in $"[] {Environment.NewLine}abcdefghijklmnopqrstuvwxyz0123456789")
            {
                _replacements[c] = string.Empty;
            }
        }

        public static string SanitiseAsScorerInput(this string s)
        {
            StringBuilder builder = new(s.Length);

            foreach (char c in s)
            {
                builder.Append(_replacements.ContainsKey(c) ? _replacements[c] : c);
            }

            return builder.ToString();
        }
    }
}