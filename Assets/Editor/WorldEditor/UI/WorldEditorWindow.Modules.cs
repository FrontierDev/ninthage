namespace Game.Editor.WorldEditor
{
    public interface IWorldEditorWindowModule
    {
        string TabName { get; }
        void Draw(WorldEditorWindow window);
    }

    public partial class WorldEditorWindow
    {
        private sealed class WorldParametersModule : IWorldEditorWindowModule
        {
            public string TabName => "World";

            public void Draw(WorldEditorWindow window)
            {
                window.DrawWorldParametersModule();
            }
        }

        private sealed class ErosionModule : IWorldEditorWindowModule
        {
            public string TabName => "Global Erosion";

            public void Draw(WorldEditorWindow window)
            {
                window.DrawErosionModule();
            }
        }


    }
}
