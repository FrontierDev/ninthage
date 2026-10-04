using UnityEditor;
using UnityEngine;
using Game.Shared.Data;

[CustomEditor(typeof(DialogueDefinition))]
public class DialogueDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Open in Dialogue Editor"))
        {
            var def = (DialogueDefinition)target;
            Game.Editor.Dialogue.DialogueEditorWindow.OpenWith(def);
        }
    }
}
