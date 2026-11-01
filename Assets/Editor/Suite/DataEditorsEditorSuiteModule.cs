using Game.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Suite
{
    /// <summary>
    /// Provides shortcuts to the existing data authoring editor windows.
    /// </summary>
    internal sealed class DataEditorsEditorSuiteModule : IEditorSuiteModule
    {
        /// <inheritdoc />
        public string Label => "Data Editors";

        /// <inheritdoc />
        public void Draw()
        {
            EditorGUILayout.LabelField(Label, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Opens existing editor in a separate window.",
                MessageType.Info);

            if (GUILayout.Button("Open Actor Stats"))
                ActorStatEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Auras"))
                AuraEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Classes"))
                ClassEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Damage Schools"))
                DamageSchoolEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Factions"))
                FactionEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Items"))
                ItemEditorWindow.OpenWindow();

            if (GUILayout.Button("Open NPCs"))
                NPCEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Quests"))
                QuestEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Races"))
                RaceEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Spells"))
                SpellEditorWindow.OpenWindow();

            if (GUILayout.Button("Open Talents"))
                TalentEditorWindow.OpenWindow();

            EditorGUILayout.HelpBox(
                "Embedded editing and additional authoring types are separate future work.",
                MessageType.Info);
        }
    }
}
