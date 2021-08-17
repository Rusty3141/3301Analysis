namespace AutoDecrypt.modules.data
{
    /// <summary>
    /// Class <c>OperationReference</c> stores information about operations supported by the postfix grammar to allow the user to view
    /// and choose which of them to be included in decryption attempt generations.
    /// </summary>
    internal class OperationReference : ISelectable
    {
        public string Category { get; }
        public string Operation { get; }
        public int Index { get; }
        public bool Selected { get; set; }

        public OperationReference(string category, string operation, int index, bool selected = true)
        {
            Category = category;
            Operation = operation;
            Index = index;
            Selected = selected;
        }
    }
}