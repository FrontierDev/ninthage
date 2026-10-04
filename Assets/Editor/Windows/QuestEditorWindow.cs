using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Shared;
using Game.Shared.Data;
using Game.Editor.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing quest definitions.
    /// </summary>
    public class QuestEditorWindow : DataDefinitionEditorWindow<QuestDefinition, QuestDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Rewards", "Objectives & Requirements", "Settings" };

        private ReorderableList objectiveList;
        private ReorderableList requirementList;
        private ReorderableList itemRewardList;

        private SerializedObject cachedSerializedObject;
        private QuestDefinition cachedDefinition;

        private bool pendingTooltip;
        private string pendingTooltipText;
        private Color pendingTooltipAccentColor;

        private Vector2 descriptionScrollPosition;

        protected override string DefinitionTypeName => "Quest";
        protected override string AssetDirectoryPath => "Assets/Data/QuestDefinitions";
        protected override string AssetPrefix => "Quest";

        [MenuItem("Data/Quest Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<QuestEditorWindow>("Quest Definitions");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        protected override QuestDefinitionLibrary GetLibraryInstance()
        {
            return QuestDefinitionLibrary.Instance;
        }

        protected override void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (Selected != null)
            {
                var serializedDef = GetOrCreateSerializedObject(Selected);

                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                EditorGUILayout.Space();

                rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);

                switch (selectedTab)
                {
                    case 0: DrawGeneralTab(serializedDef); break;
                    case 1: DrawRewardsTab(serializedDef); break;
                    case 2: DrawObjectivesAndRequirementsTab(serializedDef); break;
                    case 3: DrawSettingsTab(serializedDef); break;
                }

                if (serializedDef.hasModifiedProperties)
                {
                    serializedDef.ApplyModifiedProperties();
                    EditorUtility.SetDirty(Selected);
                }

                EditorGUILayout.EndScrollView();

                // Draw deferred tooltip on top of everything
                if (pendingTooltip && Event.current.type == EventType.Repaint)
                {
                    DrawDeferredTooltip();
                    pendingTooltip = false;
                }
            }
            else
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select a quest to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quest ID:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definitionId"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Level:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("level"), GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);

            var descRect = EditorGUILayout.GetControlRect(GUILayout.Height(200));
            descriptionScrollPosition = GUI.BeginScrollView(descRect, descriptionScrollPosition, new Rect(0, 0, descRect.width - 16, 500), false, true);

            var descriptionProp = serializedObject.FindProperty("description");
            var descStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };

            string descValue = EditorGUI.TextArea(new Rect(0, 0, descRect.width - 16, 500), descriptionProp.stringValue, descStyle);
            if (descValue != descriptionProp.stringValue)
            {
                descriptionProp.stringValue = descValue;
            }

            GUI.EndScrollView();

            EditorGUILayout.Space();

            // Asset path
            EditorGUILayout.LabelField("Asset Information", EditorStyles.boldLabel);
            string assetPath = AssetDatabase.GetAssetPath(Selected);
            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(assetPath, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = Selected;
                    EditorGUIUtility.PingObject(Selected);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRewardsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Experience & Currency", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Gold Reward:", GUILayout.Width(120));
            var goldProp = serializedObject.FindProperty("goldReward");
            if (goldProp != null)
                EditorGUILayout.PropertyField(goldProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Experience Reward:", GUILayout.Width(120));
            var expProp = serializedObject.FindProperty("experienceReward");
            if (expProp != null)
                EditorGUILayout.PropertyField(expProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Faction Reputation", EditorStyles.boldLabel);
            var repProp = serializedObject.FindProperty("reputationReward");
            if (repProp != null)
            {
                EditorGUILayout.PropertyField(repProp, new GUIContent("Reputation Reward"), true);
            }

            EditorGUILayout.Space();

            var itemRewardsProp = serializedObject.FindProperty("itemRewards");
            if (itemRewardsProp != null)
                DrawItemRewardList(itemRewardsProp);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Give All Items:", GUILayout.Width(120));
            var giveAllProp = serializedObject.FindProperty("giveAllItems");
            if (giveAllProp != null)
                EditorGUILayout.PropertyField(giveAllProp, GUIContent.none, GUILayout.Width(50));
            EditorGUILayout.LabelField("(Give all or choose)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectivesAndRequirementsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Quest Objectives", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GetObjectiveList(serializedObject).DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Quest Requirements", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GetRequirementList(serializedObject).DoLayoutList();
        }

        private void DrawSettingsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Quest Parameters", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Recommended Players:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recommendedPlayers"), GUIContent.none, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Timer (seconds):", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timerSeconds"), GUIContent.none, GUILayout.Width(80));
            EditorGUILayout.LabelField("(0 = no timer)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Repeatability", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Is Repeatable:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isRepeatable"), GUIContent.none, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            if (serializedObject.FindProperty("isRepeatable").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cooldown:", GUILayout.Width(120));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"), GUIContent.none);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
        }

        // --- Inline array drawing for item rewards ---

        private void DrawItemRewardList(SerializedProperty arrayProp)
        {
            if (arrayProp == null)
                return;

            DrawInlineArrayHeader("Item Rewards", arrayProp);

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();

                var itemProp = element.FindPropertyRelative("item");
                var quantityProp = element.FindPropertyRelative("quantity");

                if (itemProp != null)
                    EditorGUILayout.PropertyField(itemProp, new GUIContent("Item"));
                if (quantityProp != null)
                    EditorGUILayout.PropertyField(quantityProp, new GUIContent("Quantity"));

                EditorGUILayout.EndVertical();

                if (DrawInlineRemoveButton())
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        // --- Shared inline array helpers ---

        private void DrawInlineArrayHeader(string label, SerializedProperty arrayProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label} ({arrayProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18)))
            {
                arrayProp.arraySize++;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInlineArrayEmpty()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);
            EditorGUI.indentLevel--;
        }

        private bool DrawInlineRemoveButton()
        {
            return GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18));
        }

        private void DrawInlineSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        // --- Preview & Tooltip ---

        private void DrawQuestPreview()
        {
            if (Selected == null) return;

            Color accentColor = new Color(1f, 0.84f, 0f, 1f); // Gold accent for quests
            float iconSize = 64;

            EditorGUILayout.LabelField("Quest Preview", EditorStyles.boldLabel);

            // Outer container with dark background
            var outerRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(outerRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Icon with colored border
            var iconRect = GUILayoutUtility.GetRect(iconSize + 4, iconSize + 4, GUILayout.Width(iconSize + 4));
            EditorGUI.DrawRect(iconRect, accentColor);
            var innerIconRect = new Rect(iconRect.x + 2, iconRect.y + 2, iconSize, iconSize);
            EditorGUI.DrawRect(innerIconRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            if (Selected.Icon != null)
            {
                Texture2D iconTex = AssetPreview.GetAssetPreview(Selected.Icon);
                if (iconTex != null)
                    GUI.DrawTexture(innerIconRect, iconTex, ScaleMode.ScaleToFit);
                else
                    GUI.DrawTexture(innerIconRect, Selected.Icon.texture, ScaleMode.ScaleToFit);
            }

            GUILayout.Space(10);

            // Name and quest info
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8);

            var nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = accentColor },
                wordWrap = false
            };
            EditorGUILayout.LabelField(Selected.DisplayName ?? "(No Name)", nameStyle);

            var infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = accentColor }
            };
            EditorGUILayout.LabelField($"Level {Selected.Level} - {Selected.Type}", infoStyle);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            // Schedule tooltip for deferred drawing (on top of everything)
            if (outerRect.width > 1 && outerRect.Contains(Event.current.mousePosition))
            {
                pendingTooltip = true;
                pendingTooltipText = BuildQuestTooltip();
                pendingTooltipAccentColor = accentColor;
                Repaint();
            }
        }

        private void DrawDeferredTooltip()
        {
            var tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            Vector2 tooltipSize = tooltipStyle.CalcSize(new GUIContent(pendingTooltipText));
            tooltipSize.x = Mathf.Min(tooltipSize.x, 300);
            tooltipSize.y = tooltipStyle.CalcHeight(new GUIContent(pendingTooltipText), tooltipSize.x);

            Vector2 mousePos = Event.current.mousePosition;
            Rect tooltipRect = new Rect(mousePos.x + 16, mousePos.y + 16, tooltipSize.x, tooltipSize.y);

            if (tooltipRect.xMax > position.width)
                tooltipRect.x = mousePos.x - tooltipSize.x - 8;
            if (tooltipRect.yMax > position.height)
                tooltipRect.y = mousePos.y - tooltipSize.y - 8;

            EditorGUI.DrawRect(tooltipRect, new Color(0.12f, 0.12f, 0.12f, 0.95f));
            EditorGUI.DrawRect(new Rect(tooltipRect.x, tooltipRect.y, tooltipRect.width, 2), pendingTooltipAccentColor);
            GUI.Label(tooltipRect, pendingTooltipText, tooltipStyle);
        }

        private string BuildQuestTooltip()
        {
            if (Selected == null) return string.Empty;

            var sb = new StringBuilder();
            Color accentColor = new Color(1f, 0.84f, 0f, 1f);
            string hexColor = ColorUtility.ToHtmlStringRGB(accentColor);

            sb.AppendLine($"<color=#{hexColor}><b>{Selected.DisplayName ?? "(No Name)"}</b></color>");
            sb.AppendLine($"<color=#{hexColor}>{Selected.Type} Quest • Level {Selected.Level}</color>");
            sb.AppendLine();

            // Recommended party size
            if (Selected.RecommendedPlayers > 0)
                sb.AppendLine($"Recommended Players: {Selected.RecommendedPlayers}");

            // Rewards
            if (Selected.GoldReward > 0 || Selected.ExperienceReward > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#FFD700>Rewards:</color>");
                if (Selected.GoldReward > 0)
                    sb.AppendLine($"  Gold: {Selected.GoldReward}");
                if (Selected.ExperienceReward > 0)
                    sb.AppendLine($"  Experience: {Selected.ExperienceReward}");
            }

            // Reputation
            if (Selected.ReputationReward != null)
                sb.AppendLine($"  Reputation: {Selected.ReputationReward.ReputationPoints}");

            // Items
            if (Selected.ItemRewards.Count > 0)
            {
                sb.AppendLine($"  Items: {Selected.ItemRewards.Count}");
            }

            // Repeatability
            if (Selected.IsRepeatable)
                sb.AppendLine($"\n<color=#00FF00>Repeatable ({Selected.Cooldown})</color>");

            // Description
            if (!string.IsNullOrEmpty(Selected.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FFD100>\"{Selected.Description}\"</color>");
            }

            return sb.ToString().TrimEnd();
        }

        // --- Reorderable Lists ---

        private SerializedObject GetOrCreateSerializedObject(QuestDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
                objectiveList = null;
                requirementList = null;
                itemRewardList = null;
            }
            return cachedSerializedObject;
        }

        private ReorderableList GetObjectiveList(SerializedObject serializedObject)
        {
            if (objectiveList == null)
            {
                var prop = serializedObject.FindProperty("objectives");
                objectiveList = new ReorderableList(serializedObject, prop, true, true, true, true)
                {
                    drawHeaderCallback = rect =>
                        EditorGUI.LabelField(rect, "Objectives"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        rect.height -= 4;
                        EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                    },
                    elementHeightCallback = index =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element, true) + 4;
                    },
                    onAddDropdownCallback = (rect, list) =>
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Kill Objective"), false, () =>
                        {
                            prop.arraySize++;
                            prop.GetArrayElementAtIndex(prop.arraySize - 1).managedReferenceValue =
                                new Game.Shared.Data.Quest_KillObjective();
                            serializedObject.ApplyModifiedProperties();
                            objectiveList = null;
                        });
                        menu.AddItem(new GUIContent("Item Objective"), false, () =>
                        {
                            prop.arraySize++;
                            prop.GetArrayElementAtIndex(prop.arraySize - 1).managedReferenceValue =
                                new Game.Shared.Data.Quest_ItemObjective();
                            serializedObject.ApplyModifiedProperties();
                            objectiveList = null;
                        });
                        menu.ShowAsContext();
                    }
                };
            }
            return objectiveList;
        }

        private ReorderableList GetRequirementList(SerializedObject serializedObject)
        {
            if (requirementList == null)
            {
                var prop = serializedObject.FindProperty("requirements");
                requirementList = new ReorderableList(serializedObject, prop, true, false, true, true)
                {
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        EditorGUI.PropertyField(rect, element, new GUIContent($"Requirement {index + 1}"), true);
                    },
                    elementHeightCallback = (index) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element, true) + 4;
                    }
                };
            }
            return requirementList;
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool MatchesSearchFilter(QuestDefinition definition, string lowerFilter)
        {
            if (definition.Type.ToString().ToLower().Contains(lowerFilter))
                return true;

            if (definition.Level.ToString().Contains(lowerFilter))
                return true;

            return false;
        }

        protected override void DrawListItem(QuestDefinition def, bool isSelected)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button);
            if (isSelected)
            {
                buttonStyle.normal.background = Texture2D.grayTexture;
            }
            buttonStyle.alignment = TextAnchor.MiddleLeft;

            if (GUILayout.Button($"{def.DisplayName} (Lvl {def.Level} - {def.Type})", buttonStyle, GUILayout.Height(25)))
            {
                Selected = def;
            }
        }
    }
}
