namespace Game.Editor.Suite
{
    /// <summary>
    /// Defines the UI-only contract for a module rendered by the Editor Suite.
    /// </summary>
    internal interface IEditorSuiteModule
    {
        /// <summary>
        /// Gets the static title shown in the Suite navigation.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Draws this module's content inside the Suite's right-hand panel.
        /// </summary>
        void Draw();
    }
}
