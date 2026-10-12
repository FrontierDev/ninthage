namespace Game.Client.UI
{
    /// <summary>
    /// Defines the client-side <c>ITooltipLine</c> contract.
    /// </summary>
    public interface ITooltipLine
    {
        /// <summary>
        /// Sets the value managed by <c>Set</c>.
        /// </summary>
        public void Set(string[] text);
    }
}