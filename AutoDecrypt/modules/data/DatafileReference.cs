namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>DatafileReference</c> stores information about files in the users selected directory which enables specific pages/sections
    /// to be chosen for a decryption job.
    /// </summary>
    internal class DatafileReference : ISelectable
    {
        public string Name { get; }
        public int Index { get; }
        public bool Selected { get; set; }

        public DatafileReference(string name, int index, bool selected = true)
        {
            Name = name;
            Index = index;
            Selected = selected;
        }
    }
}