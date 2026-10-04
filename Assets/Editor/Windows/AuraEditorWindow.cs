using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    public class AuraEditorWindow : DataDefinitionEditorWindow<AuraDefinition, AuraDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Timing", "Components" };

        private ReorderableList componentList;
        private SerializedObject cachedSerializedObject;
        private AuraDefinition cachedDefinition;

        private bool pendingTooltip;
        private string pendingTooltipText;
        private Color pendingTooltipAccentColor;

        protected override string DefinitionTypeName => "Aura";
        protected override string AssetDirectoryPath => "Assets/Data/AuraDefinitions";
        protected override string AssetPrefix => "Aura";

        [MenuItem("Data/Aura Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<AuraEditorWindow>("Aura Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override AuraDefinitionLibrary GetLibraryInstance()
        {
            return AuraDefinitionLibrary.Instance;
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
                    case 1: DrawTimingTab(serializedDef); break;
                    case 2: DrawComponentsTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select an aura to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Aura ID:", GUILayout.Width(120));
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

            EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stackBehavior"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStacks"));

            EditorGUILayout.Space();

            DrawAuraPreview();

            EditorGUILayout.Space();

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

        private void DrawTimingTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Aura Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseTickInterval"));
        }

        private void DrawComponentsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Aura Components", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GetComponentList(serializedObject).DoLayoutList();
        }

        private void DrawAuraPreview()
        {
            if (Selected == null) return;

            Color accentColor = new Color(1f, 0.6f, 0.2f, 1f); // Orange accent for auras
            float iconSize = 64;

            EditorGUILayout.LabelField("Aura Preview", EditorStyles.boldLabel);

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

            // Name and aura info
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
            EditorGUILayout.LabelField("Aura", infoStyle);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            // Schedule tooltip for deferred drawing (on top of everything)
            if (outerRect.width > 1 && outerRect.Contains(Event.current.mousePosition))
            {
                pendingTooltip = true;
                pendingTooltipText = BuildAuraTooltip();
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

        private string BuildAuraTooltip()
        {
            if (Selected == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            Color accentColor = new Color(1f, 0.6f, 0.2f, 1f);
            string hexColor = ColorUtility.ToHtmlStringRGB(accentColor);

            sb.AppendLine($"<color=#{hexColor}><b>{Selected.DisplayName ?? "(No Name)"}</b></color>");
            sb.AppendLine($"<color=#{hexColor}>Aura</color>");
            sb.AppendLine();

            // Timing info
            if (Selected.BaseDuration > 0)
                sb.AppendLine($"Duration: {Selected.BaseDuration:F2}s");
            if (Selected.BaseTickInterval > 0)
                sb.AppendLine($"Tick Interval: {Selected.BaseTickInterval:F2}s");

            // Stacking info
            sb.AppendLine($"<color=#FFD100>{Selected.StackBehavior}</color>");
            if (Selected.MaxStacks > 0)
                sb.AppendLine($"Max Stacks: {Selected.MaxStacks}");

            // Description
            if (!string.IsNullOrEmpty(Selected.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FFD100>\"{Selected.Description}\"</color>");
            }

            return sb.ToString().TrimEnd();
        }

        private SerializedObject GetOrCreateSerializedObject(AuraDefinition definition)
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
                var prop = serializedObject.FindProperty("components");
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

        protected override IReadOnlyList<string> GetTags(AuraDefinition definition)
        {
            return definition.Tags;
        }

        protected override bool MatchesSearchFilter(AuraDefinition definition, string lowerFilter)
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
