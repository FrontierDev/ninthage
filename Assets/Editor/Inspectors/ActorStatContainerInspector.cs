namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using UnityEditor;
    using UnityEngine;
    using PurrNet;

    [CustomEditor(typeof(ActorStatContainer))]
    public class ActorStatContainerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ActorStatContainer statContainer = (ActorStatContainer)target;

            // Display current stat values in the inspector.
            EditorGUILayout.LabelField("Current Stats:");
            if (statContainer.All != null)
            {
                foreach (var stat in statContainer.All)
                {
                    EditorGUILayout.LabelField($"{stat.Key}: {stat.Value.CurrentValue} / {stat.Value.EffectiveMaximum} (+ {stat.Value.FlatModifier})");
                }
            }
        }
    }
}