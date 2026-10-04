using UnityEditor;
using UnityEngine;
using Game.Shared.Data;

namespace Game.Editor.Inspectors
{
    [CustomEditor(typeof(AuraBehaviour), true)]
    public class AuraBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var behaviour = target as AuraBehaviour;
            if (behaviour != null && !string.IsNullOrEmpty(behaviour.Description))
            {
                EditorGUILayout.HelpBox(behaviour.Description, MessageType.Info);
                EditorGUILayout.Space();
            }

            DrawDefaultInspector();
        }
    }
}
