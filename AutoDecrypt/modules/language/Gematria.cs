using AutoDecrypt.modules.data;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoDecrypt.modules.language
{
    /// <summary>
    /// Class <c>Gematria</c> provides translation ability between runic, Latin, index and prime-based forms.
    /// </summary>
    internal class Gematria
    {
        public Dictionary<char, RuneDefinition> RuneLookup { get; protected set; } = new Dictionary<char, RuneDefinition>();
        public Dictionary<int, RuneDefinition> IndexLookup { get; protected set; } = new Dictionary<int, RuneDefinition>();

        public Gematria(string gematriaPath)
        {
            foreach (string r in File.ReadAllLines(gematriaPath, Encoding.UTF8))
            {
                if (!r.StartsWith(IOTools.CommentDeliminator))
                {
                    RuneDefinition newDefinition = new(r.Split(' '));
                    RuneLookup[newDefinition.Rune] = IndexLookup[newDefinition.Index] = newDefinition;
                }
            }
        }

        public bool RuneExists(char runeToCheck)
        {
            return RuneLookup.ContainsKey(runeToCheck);
        }
    }
}