namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>DefaultJobSettings</c> establishes default values for JobSettings' parameters.
    /// </summary>
    internal class DefaultJobSettings : JobSettings
    {
        public DefaultJobSettings() : base()
        {
            GematriaPath = "GematriaPrimus.txt";
            NgramWidth = 3;
            NgramStatisticsPath = $"ngrams{IOTools.PathSeparator}Trigrams.txt";
            IsShiftMode = false;
            Attempts = new string[] { "p i prime 1 - -", "p" };

            DatafileDirectory = $"sections{IOTools.PathSeparator}";
        }
    }
}