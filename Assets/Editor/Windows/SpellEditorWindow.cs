using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing spell definitions.
    /// </summary>
    public class SpellEditorWindow : DataDefinitionEditorWindow<SpellDefinition, SpellDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Casting", "Components", "Racial Variants" };

        private ReorderableList componentList;
        private SerializedObject cachedSerializedObject;
        private SpellDefinition cachedDefinition;

        private bool pendingTooltip;
        private string pendingTooltipText;
        private Color pendingTooltipAccentColor;

        protected override string DefinitionTypeName => "Spell";
        protected override string AssetDirectoryPath => "Assets/Data/SpellDefinitions";
        protected override string AssetPrefix => "Spell";

        [MenuItem("Data/Spell Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<SpellEditorWindow>("Spell Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override SpellDefinitionLibrary GetLibraryInstance()
        {
            return SpellDefinitionLibrary.Instance;
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
                    case 1: DrawCastingTab(serializedDef); break;
                    case 2: DrawComponentsTab(serializedDef); break;
                    case 3: DrawRacialVariantsTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select a spell to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spell ID:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definitionId"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), GUIContent.none, GUILayout.Height(60));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);

            EditorGUILayout.Space();

            DrawSpellPreview();

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

        private void DrawCastingTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Cast Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseCastTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canMoveWhileCasting"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Cooldown Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownScalesWithHaste"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useCooldownCharges"));
            if (serializedObject.FindProperty("useCooldownCharges").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseCharges"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Channeling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isChanneled"));
            if (serializedObject.FindProperty("isChanneled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseTotalTicks"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            DrawResourceCostList(serializedObject.FindProperty("baseResourceCosts"));
        }

        private void DrawComponentsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Spell Components", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GetComponentList(serializedObject).DoLayoutList();
        }

        private void DrawRacialVariantsTab(SerializedObject serializedObject)
        {
            DrawRacialVariantList(serializedObject.FindProperty("racialVariants"));
        }

        // --- Inline array drawing (layout-based, matching SpellComponentDrawer style) ---

        private void DrawResourceCostList(SerializedProperty arrayProp)
        {
            DrawInlineArrayHeader("Resource Costs", arrayProp);

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

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element.FindPropertyRelative("ResourceType"), GUIContent.none);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Amount"), GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element.FindPropertyRelative("CastPhase"), GUIContent.none);
                EditorGUILayout.LabelField("Refund %", GUILayout.Width(60));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("RefundOnInterrupt"), GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

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

        private void DrawRacialVariantList(SerializedProperty arrayProp)
        {
            DrawInlineArrayHeader("Racial Name Variants", arrayProp);

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

                EditorGUILayout.PropertyField(element.FindPropertyRelative("Race"), new GUIContent("Race"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("NameOverride"), new GUIContent("Name"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("DescriptionOverride"), new GUIContent("Description"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("IconOverride"), new GUIContent("Icon"));

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

        // --- Existing helpers ---

        private void DrawSpellPreview()
        {
            if (Selected == null) return;

            Color accentColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue accent for spells
            float iconSize = 64;

            EditorGUILayout.LabelField("Spell Preview", EditorStyles.boldLabel);

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

            // Name and spell info
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
            EditorGUILayout.LabelField("Spell", infoStyle);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            // Schedule tooltip for deferred drawing (on top of everything)
            if (outerRect.width > 1 && outerRect.Contains(Event.current.mousePosition))
            {
                pendingTooltip = true;
                pendingTooltipText = BuildSpellTooltip();
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
            tooltipSize.x = Mathf.Min(tooltipSize.x, 280);
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

        private string BuildSpellTooltip()
        {
            if (Selected == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);
            string hexColor = ColorUtility.ToHtmlStringRGB(accentColor);

            sb.AppendLine($"<color=#{hexColor}><b>{Selected.DisplayName ?? "(No Name)"}</b></color>");
            sb.AppendLine($"<color=#{hexColor}>Spell</color>");
            sb.AppendLine();

            // Casting info
            if (Selected.BaseCastTime > 0)
                sb.AppendLine($"Cast Time: {Selected.BaseCastTime:F2}s");
            if (Selected.BaseCooldown > 0)
                sb.AppendLine($"Cooldown: {Selected.BaseCooldown:F2}s");
            if (Selected.BaseRange > 0)
                sb.AppendLine($"Range: {Selected.BaseRange:F0}m");

            // Channeling info
            if (Selected.IsChanneled)
                sb.AppendLine($"<color=#FFD100>Channeled ({Selected.BaseTotalTicks} ticks)</color>");

            // Description
            if (!string.IsNullOrEmpty(Selected.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FFD100>\"{Selected.Description}\"</color>");
            }

            return sb.ToString().TrimEnd();
        }

        private SerializedObject GetOrCreateSerializedObject(SpellDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
                componentList = null;
            }
            return cachedSerializedObject;
        }

        private ReorderableList GetComponentList(SerializedObject serializedObject)
        {
            if (componentList == null)
            {
                var prop = serializedObject.FindProperty("baseComponents");
                componentList = new ReorderableList(serializedObject, prop, true, false, true, true)
                {
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        EditorGUI.PropertyField(rect, element, true);
                    },
                    elementHeightCallback = (index) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element, true) + 4;
                    }
                };
            }
            return componentList;
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool SupportsTags => true;

        protected override IReadOnlyList<string> GetTags(SpellDefinition definition)
        {
            return definition.Tags;
        }

        protected override bool MatchesSearchFilter(SpellDefinition definition, string lowerFilter)
        {
            if (definition.Tags != null)
            {
                for (int i = 0; i < definition.Tags.Count; i++)
                {
                    if (definition.Tags[i] != null && definition.Tags[i].ToLower().Contains(lowerFilter))
                        return true;
                }
            }
            return false;
        }
    }
}
