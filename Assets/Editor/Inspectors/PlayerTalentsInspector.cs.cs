namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using Game.Shared.Data;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PlayerTalents))]
    public class PlayerTalentsInspector : Editor
    {
        private bool showTalents = true;
        private Vector2 scrollPosition;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var playerTalents = (PlayerTalents)target;
            var talents = playerTalents.Talents;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Talents", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Known: {talents.Count}");

            showTalents = EditorGUILayout.Foldout(showTalents, $"Talents ({talents.Count})", true);
            if (!showTalents) return;

            if (talents.Count == 0)
            {
                EditorGUILayout.HelpBox("No talents learned.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));
            EditorGUI.indentLevel++;

            foreach (var kvp in talents)
            {
                var def = kvp.Key;
                int rank = kvp.Value;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                string title = def != null ? def.DisplayName : "(Unknown)";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Rank: {rank}", GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();

                if (def != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("ID", def.DefinitionId, EditorStyles.miniLabel);
                    if (def.Icon != null)
                    {
                        var iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                        GUI.DrawTexture(iconRect, def.Icon.texture, ScaleMode.ScaleToFit);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Refresh"))
                    Repaint();
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;
    }
}
