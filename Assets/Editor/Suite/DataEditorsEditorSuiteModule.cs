using Game.Editor.Suite.DataEditors;
using Game.Editor.Windows;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Suite
{
    /// <summary>
    /// Identifies one of the embedded definition editor panels.
    /// </summary>
    internal enum DataEditorKind
    {
        ActorStats = 0,
        Auras = 1,
        Classes = 2,
        DamageSchools = 3,
        Factions = 4,
        Items = 5,
        Quests = 6,
        Races = 7,
        Spells = 8,
        Talents = 9
    }

    /// <summary>
    /// Owns selection and lifetime for the embedded definition editor panels.
    /// </summary>
    internal sealed class DataEditorsEditorSuiteModule : IEditorSuiteModule
    {
        private static readonly string[] DATA_EDITOR_LABELS =
        {
            "Actor Stats",
            "Auras",
            "Classes",
            "Damage Schools",
            "Factions",
            "Items",
            "Quests",
            "Races",
            "Spells",
            "Talents"
        };

        private readonly Action _requestRepaint;
        private readonly IDataDefinitionEditorPanel[] _panels = new IDataDefinitionEditorPanel[10];
        private DataEditorKind _selectedEditor = DataEditorKind.Items;

        /// <summary>
        /// Creates the data editor module with the Suite repaint callback.
        /// </summary>
        internal DataEditorsEditorSuiteModule(Action requestRepaint)
        {
            _requestRepaint = requestRepaint
                ?? throw new ArgumentNullException(nameof(requestRepaint));
        }

        /// <inheritdoc />
        public string Label => "Data Editors";

        /// <inheritdoc />
        public void Draw()
        {
            EditorGUILayout.LabelField(Label, EditorStyles.boldLabel);

            int selectedIndex = EditorGUILayout.Popup(
                "Editor",
                (int)_selectedEditor,
                DATA_EDITOR_LABELS);
            if (selectedIndex != (int)_selectedEditor)
                Select((DataEditorKind)selectedIndex);

            Select(_selectedEditor);

            var contentRect = GUILayoutUtility.GetRect(
                0f,
                0f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            GUILayout.BeginArea(contentRect);
            try
            {
                _panels[(int)_selectedEditor].Draw(
                    new Rect(0f, 0f, contentRect.width, contentRect.height),
                    _requestRepaint);
            }
            finally
            {
                GUILayout.EndArea();
            }

            if (GUILayout.Button("Open NPC Editor (standalone)"))
                NPCEditorWindow.OpenWindow();
        }

        /// <summary>
        /// Selects a definition editor and lazily initializes its panel.
        /// </summary>
        internal void Select(DataEditorKind kind)
        {
            if (!Enum.IsDefined(typeof(DataEditorKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown data editor kind.");

            _selectedEditor = kind;
            int panelIndex = (int)kind;
            if (_panels[panelIndex] != null)
                return;

            IDataDefinitionEditorPanel panel;
            switch (kind)
            {
                case DataEditorKind.ActorStats:
                    panel = new ActorStatEditorPanel();
                    break;
                case DataEditorKind.Auras:
                    panel = new AuraEditorPanel();
                    break;
                case DataEditorKind.Classes:
                    panel = new ClassEditorPanel();
                    break;
                case DataEditorKind.DamageSchools:
                    panel = new DamageSchoolEditorPanel();
                    break;
                case DataEditorKind.Factions:
                    panel = new FactionEditorPanel();
                    break;
                case DataEditorKind.Items:
                    panel = new ItemEditorPanel();
                    break;
                case DataEditorKind.Quests:
                    panel = new QuestEditorPanel();
                    break;
                case DataEditorKind.Races:
                    panel = new RaceEditorPanel();
                    break;
                case DataEditorKind.Spells:
                    panel = new SpellEditorPanel();
                    break;
                case DataEditorKind.Talents:
                    panel = new TalentEditorPanel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown data editor kind.");
            }

            panel.Initialize();
            _panels[panelIndex] = panel;
        }

        /// <summary>
        /// Releases the in-memory panel instances owned by this module.
        /// </summary>
        internal void Reset()
        {
            Array.Clear(_panels, 0, _panels.Length);
        }
    }
}
