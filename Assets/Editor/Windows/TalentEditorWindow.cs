using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Shared.Data;
using System;
using System.Linq;
using Game.Shared;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing talent definitions.
    /// </summary>
    public class TalentEditorWindow : DataDefinitionEditorWindow<TalentDefinition, TalentDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Modifiers" };

        private ReorderableList componentList;
        private SerializedObject cachedSerializedObject;
        private TalentDefinition cachedDefinition;

        private ReorderableList modifierList;
        private static Type[] cachedModifierTypes;
        private static string[] cachedModifierNames;


        private bool pendingTooltip;
        private string pendingTooltipText;
        private Color pendingTooltipAccentColor;

        protected override string DefinitionTypeName => "Talent";
        protected override string AssetDirectoryPath => "Assets/Data/TalentDefinitions";
        protected override string AssetPrefix => "Talent";

        [MenuItem("Data/Talent Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<TalentEditorWindow>("Talent Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override TalentDefinitionLibrary GetLibraryInstance()
        {
            return TalentDefinitionLibrary.Instance;
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
                    case 1: DrawModifiersTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select a talent to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Talent ID:", GUILayout.Width(120));
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRank"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Talent Behaviour", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("talentBehaviour"), GUIContent.none);

            EditorGUILayout.Space();

            DrawTalentPreview();

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

        private void DrawModifiersTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Talent Modifiers", EditorStyles.boldLabel);
            var prop = serializedObject.FindProperty("modifiers");
            if (prop == null)
            {
                EditorGUILayout.HelpBox("`modifiers` is not a managed-reference field. Mark the field with [SerializeReference] in TalentDefinition.", MessageType.Warning);
                return;
            }
            GetModifierList(serializedObject).DoLayoutList();
        }


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

        private void DrawTalentPreview()
        {
            if (Selected == null) return;

            Color accentColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue accent for talents
            float iconSize = 64;

            EditorGUILayout.LabelField("Talent Preview", EditorStyles.boldLabel);

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
            EditorGUILayout.LabelField("Talent", infoStyle);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            // Schedule tooltip for deferred drawing (on top of everything)
            if (outerRect.width > 1 && outerRect.Contains(Event.current.mousePosition))
            {
                pendingTooltip = true;
                pendingTooltipText = BuildTalentTooltip();
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

        private string BuildTalentTooltip()
        {
            if (Selected == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);
            string hexColor = ColorUtility.ToHtmlStringRGB(accentColor);

            sb.AppendLine($"<color=#{hexColor}><b>{Selected.DisplayName ?? "(No Name)"}</b></color>");
            sb.AppendLine($"<color=#{hexColor}>Talent</color>");
            sb.AppendLine();

            // Casting info
            sb.AppendLine($"Max Rank: {Selected.MaxRank}");

            // Description
            if (!string.IsNullOrEmpty(Selected.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FFD100>\"{Selected.Description}\"</color>");
            }

            return sb.ToString().TrimEnd();
        }

        private SerializedObject GetOrCreateSerializedObject(TalentDefinition definition)
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


        private ReorderableList GetModifierList(SerializedObject serializedObject)
        {
            if (modifierList != null) return modifierList;

            var prop = serializedObject.FindProperty("modifiers");
            modifierList = new ReorderableList(serializedObject, prop, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Modifiers"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, true);
                },
                elementHeightCallback = index =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, true) + 4;
                },
                onAddCallback = list =>
                {
                    prop.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                    var newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                    var modProp = newElem.FindPropertyRelative("Modifier");
                    if (modProp != null)
                        modProp.managedReferenceValue = null; // user will pick type
                },
                onRemoveCallback = list =>
                {
                    prop.DeleteArrayElementAtIndex(list.index);
                }
            };

            return modifierList;
        }

        private void EnsureCachedModifierTypes()
        {
            if (cachedModifierTypes != null) return;
            cachedModifierTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsAbstract && typeof(ISpellModifier).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();
            cachedModifierNames = cachedModifierTypes.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool SupportsTags => true;

        protected override IReadOnlyList<string> GetTags(TalentDefinition definition)
        {
            return definition.Tags;
        }

        protected override bool MatchesSearchFilter(TalentDefinition definition, string lowerFilter)
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
