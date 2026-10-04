namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ActorSpawnPoint))]
    public class ActorSpawnPointInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ActorSpawnPoint spawnPoint = (ActorSpawnPoint)target;

            EditorGUILayout.LabelField("GUID", spawnPoint.Guid.ToString());
        }
    }
}