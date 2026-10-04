namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PlayerActor))]
    public class PlayerActorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var playerActor = (PlayerActor)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Player Character Data (SyncVars)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Character Name", playerActor.characterName.value ?? "(null)");
                EditorGUILayout.IntField("Level", playerActor.level.value);
                EditorGUILayout.TextField("Class ID", playerActor.classID.value ?? "(null)");
                EditorGUILayout.TextField("Race ID", playerActor.raceID.value ?? "(null)");
                EditorGUILayout.TextField("PvP Faction", playerActor.pvpFactionID.value ?? "(null)");
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Raw CharacterData", EditorStyles.boldLabel);

                var data = playerActor.CharacterData;
                if (data != null)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Name", data.Name ?? "(null)");
                        EditorGUILayout.IntField("Level", data.Level);
                        EditorGUILayout.TextField("Class ID", data.ClassID ?? "(null)");
                        EditorGUILayout.TextField("Race ID", data.RaceID ?? "(null)");
                        EditorGUILayout.TextField("PvP Faction", data.PvPFactionID ?? "(null)");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("CharacterData is null — not yet assigned by server.", MessageType.Info);
                }

                // Force repaint so values update live during play mode.
                Repaint();
            }
        }
    }
}
