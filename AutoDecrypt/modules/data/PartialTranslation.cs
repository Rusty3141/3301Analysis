namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>PartialTranslation</c> represents a character in the intermediate stage of decryption that may or may not require
    /// further translation.
    /// </summary>
    internal class PartialTranslation
    {
        public RuneDefinition Rune
        { get; protected set; }

        public string Plain
        { get; protected set; }

        public PartialTranslation(RuneDefinition rune)
        { Rune = rune; }

        public PartialTranslation(string plain)
        { Plain = plain; }
    }
}