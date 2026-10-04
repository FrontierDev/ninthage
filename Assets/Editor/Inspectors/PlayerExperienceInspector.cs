namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using Game.Shared.Utility;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PlayerExperience))]
    public class PlayerExperienceInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var playerExperience = (PlayerExperience)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Experience (Runtime)", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Level", playerExperience.Level);
                    EditorGUILayout.IntField("Experience", playerExperience.Experience);
                    EditorGUILayout.IntField("Next Lv. @", ExperienceCalculator.GetExpForLevel(playerExperience.Level));

                    EditorGUILayout.LabelField("Weapon Skills", EditorStyles.boldLabel);
                    foreach (var weaponSkill in playerExperience.WeaponSkills)
                    {
                        EditorGUILayout.IntField($"{weaponSkill.WeaponType} - Level {weaponSkill.Level}", weaponSkill.Experience);
                    }
                }

                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view experience data.", MessageType.Info);
            }
        }
    }
}
