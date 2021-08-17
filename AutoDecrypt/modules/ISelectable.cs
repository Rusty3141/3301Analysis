namespace AutoDecrypt.modules
{
    /// <summary>
    /// Interface <c>ISelectable</c> represents objects that can be selected and deselected by the user when making job configurations.
    /// </summary>
    internal interface ISelectable
    {
        public bool Selected { get; set; }
    }
}