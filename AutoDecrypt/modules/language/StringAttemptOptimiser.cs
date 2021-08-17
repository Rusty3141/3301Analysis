using System.Collections.Generic;
using System.IO;

namespace AutoDecrypt.modules.language
{
    /// <summary>
    /// Class <c>StringAttemptOptimiser</c> provides string-cleaning utilities for optimising decryption attempts by making logical
    /// replacements.
    /// </summary>
    internal class StringAttemptOptimiser
    {
        private readonly Dictionary<string, string> _replacements = new();

        public StringAttemptOptimiser(string pathToReplacements)
        {
            foreach (string c in File.ReadAllLines(pathToReplacements))
            {
                string[] split = c.Split(';');

                _replacements[split[0]] = split[1];
            }
        }

        public string Optimise(string s)
        {
            string result = s;

            foreach (string replacementKey in _replacements.Keys)
            {
                result = result.Replace(replacementKey, _replacements[replacementKey]);
            }

            return result;
        }
    }
}