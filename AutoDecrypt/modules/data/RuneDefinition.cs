using System;
using System.Reflection;

namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>RuneDefinition</c> provides a definition for each Gematria Primus character and stores its associated values.
    /// </summary>
    internal class RuneDefinition
    {
        public char Rune { get; protected set; }
        public int Index { get; protected set; }
        public int Prime { get; protected set; }
        public string Plaintext { get; protected set; }

        public RuneDefinition(params object[] args)
        {
            PropertyInfo[] properties = GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i].SetValue(this, Convert.ChangeType(args[i], properties[i].PropertyType));
            }
        }
    }
}