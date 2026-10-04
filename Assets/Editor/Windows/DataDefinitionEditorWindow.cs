using UnityEngine;
using UnityEditor;
using Game.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Abstract base for definition library editor windows.
    /// Provides a two-panel layout with list, search, toolbar, and serialized property editing.
    /// </summary>
    public abstract class DataDefinitionEditorWindow<TDefinition, TLibrary> : EditorWindow
        where TDefinition : DataDefinition
        where TLibrary : DataDefinitionLibrary<TDefinition>
    {
        private Vector2 leftScrollPosition;
        protected Vector2 rightScrollPosition;
        private TDefinition selected;
        private TLibrary library;
        private List<TDefinition> definitions;
        private string searchFilter = "";
        private bool useInstanceFallback = true;

        // Sorting
        private enum SortMode { NameAsc, NameDesc, IdAsc, IdDesc, TagPriority }
        private SortMode sortMode = SortMode.NameAsc;
        private string tagSortOrder = "";
        private bool showSortSettings;

        /// <summary>
        /// Display name used in UI labels (e.g. "Class", "Race").
        /// </summary>
        protected abstract string DefinitionTypeName { get; }

        /// <summary>
        /// Asset folder path where new definitions are created (e.g. "Assets/Data/ClassDefinitions").
        /// </summary>
        protected abstract string AssetDirectoryPath { get; }

        /// <summary>
        /// File prefix for new assets (e.g. "Class" produces "Class_newclass.asset").
        /// </summary>
        protected abstract string AssetPrefix { get; }

        /// <summary>
        /// Return the singleton library instance for fallback loading.
        /// </summary>
        protected abstract TLibrary GetLibraryInstance();

        /// <summary>
        /// Draw type-specific fields in the right detail panel.
        /// Called after the base fields (ID, Display Name) are drawn.
        /// </summary>
        protected abstract void DrawDefinitionFields(SerializedObject serializedObject);

        /// <summary>
        /// Override to add type-specific search matching beyond ID and display name.
        /// </summary>
        protected virtual bool MatchesSearchFilter(TDefinition definition, string lowerFilter)
        {
            return false;
        }

        /// <summary>
        /// Override to return the tags for a definition. Used for tag filtering and tag-priority sorting.
        /// Return null or empty if the type has no tags.
        /// </summary>
        protected virtual IReadOnlyList<string> GetTags(TDefinition definition)
        {
            return null;
        }

        /// <summary>
        /// Whether this definition type supports tags. Controls visibility of tag UI.
        /// </summary>
        protected virtual bool SupportsTags => false;

        /// <summary>
        /// Customize how list items are drawn. Called for each definition in the filtered list.
        /// Override to customize visual appearance (colors, icons, etc).
        /// </summary>
        protected virtual void DrawListItem(TDefinition def, bool isSelected)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button);
            if (isSelected)
            {
                buttonStyle.normal.background = Texture2D.grayTexture;
            }
            buttonStyle.alignment = TextAnchor.MiddleLeft;

            if (GUILayout.Button(def.DisplayName, buttonStyle, GUILayout.Height(25)))
            {
                Selected = def;
            }
        }

        protected TDefinition Selected
        {
            get => selected;
            set => selected = value;
        }
        protected TLibrary Library => library;

        private void OnEnable()
        {
            RefreshLibrary();
        }

        private void RefreshLibrary()
        {
            if (library == null && useInstanceFallback)
            {
                library = GetLibraryInstance();
            }

            if (library != null)
            {
                definitions = new List<TDefinition>(library.GetAllDefinitions());
            }
            else
            {
                definitions = new List<TDefinition>();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                CreateNew();
            }

            GUI.enabled = selected != null;
            if (GUILayout.Button("Clone", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                CloneSelected();
            }

            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                DeleteSelected();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshLibrary();
            }

            EditorGUILayout.EndHorizontal();

            // Search
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Search:", EditorStyles.miniLabel, GUILayout.Width(45));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField);
            EditorGUILayout.EndHorizontal();

            // Sort row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Sort:", EditorStyles.miniLabel, GUILayout.Width(45));

            string[] sortOptions = SupportsTags
                ? new[] { "Name A-Z", "Name Z-A", "ID A-Z", "ID Z-A", "Tag Priority" }
                : new[] { "Name A-Z", "Name Z-A", "ID A-Z", "ID Z-A" };

            int maxSort = SupportsTags ? 4 : 3;
            int currentSort = (int)sortMode > maxSort ? 0 : (int)sortMode;
            int newSort = EditorGUILayout.Popup(currentSort, sortOptions, EditorStyles.toolbarPopup);
            sortMode = (SortMode)newSort;

            EditorGUILayout.EndHorizontal();

            // Tag priority input (only when Tag Priority sort is active)
            if (SupportsTags && sortMode == SortMode.TagPriority)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField("Tags:", EditorStyles.miniLabel, GUILayout.Width(45));
                tagSortOrder = EditorGUILayout.TextField(tagSortOrder, EditorStyles.toolbarTextField);
                EditorGUILayout.EndHorizontal();
            }

            // Library selector
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Library:", EditorStyles.miniLabel, GUILayout.Width(45));
            var newLibrary = EditorGUILayout.ObjectField(library, typeof(TLibrary), false, GUILayout.ExpandWidth(true)) as TLibrary;
            if (newLibrary != library)
            {
                library = newLibrary;
                useInstanceFallback = (library == null);
                RefreshLibrary();
                selected = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Count
            int totalCount = definitions?.Count ?? 0;
            var filtered = GetFilteredAndSorted();

            if (library == null)
            {
                EditorGUILayout.HelpBox($"Drag a {typeof(TLibrary).Name} asset above, or ensure one exists in Resources.", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(searchFilter))
            {
                EditorGUILayout.LabelField($"Showing {filtered.Count} of {totalCount} {DefinitionTypeName.ToLower()}s");
            }
            else
            {
                EditorGUILayout.LabelField($"Total {DefinitionTypeName.ToLower()}s: {totalCount}");
            }

            EditorGUILayout.Space();

            // Definition list
            leftScrollPosition = EditorGUILayout.BeginScrollView(leftScrollPosition);

            if (definitions != null && definitions.Count > 0)
            {
                foreach (var def in filtered)
                {
                    if (def == null) continue;

                    bool isSelected = selected == def;

                    EditorGUILayout.BeginHorizontal();
                    DrawListItem(def, isSelected);
                    EditorGUILayout.EndHorizontal();

                    if (isSelected)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"ID: {def.DefinitionId}", EditorStyles.miniLabel);

                        if (SupportsTags)
                        {
                            var tags = GetTags(def);
                            if (tags != null && tags.Count > 0)
                            {
                                EditorGUILayout.LabelField($"Tags: {string.Join(", ", tags)}", EditorStyles.miniLabel);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"No {DefinitionTypeName.ToLower()}s found", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
                if (library == null)
                {
                    EditorGUILayout.LabelField($"Drag a {typeof(TLibrary).Name} above", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Library contains no {DefinitionTypeName}Definitions", EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.LabelField($"Add {DefinitionTypeName}Definition assets to the library", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private List<TDefinition> GetFilteredAndSorted()
        {
            if (definitions == null) return new List<TDefinition>();

            List<TDefinition> result;

            if (string.IsNullOrEmpty(searchFilter))
            {
                result = new List<TDefinition>(definitions);
            }
            else
            {
                result = new List<TDefinition>();
                string lowerFilter = searchFilter.ToLower();

                foreach (var def in definitions)
                {
                    if (def == null) continue;

                    bool matches = def.DisplayName.ToLower().Contains(lowerFilter) ||
                                  def.DefinitionId.ToLower().Contains(lowerFilter) ||
                                  MatchesSearchFilter(def, lowerFilter);

                    // Also match against tags
                    if (!matches && SupportsTags)
                    {
                        var tags = GetTags(def);
                        if (tags != null)
                        {
                            for (int i = 0; i < tags.Count; i++)
                            {
                                if (tags[i] != null && tags[i].ToLower().Contains(lowerFilter))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (matches)
                        result.Add(def);
                }
            }

            // Apply sorting
            switch (sortMode)
            {
                case SortMode.NameAsc:
                    result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.NameDesc:
                    result.Sort((a, b) => string.Compare(b.DisplayName, a.DisplayName, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.IdAsc:
                    result.Sort((a, b) => string.Compare(a.DefinitionId, b.DefinitionId, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.IdDesc:
                    result.Sort((a, b) => string.Compare(b.DefinitionId, a.DefinitionId, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.TagPriority:
                    SortByTagPriority(result);
                    break;
            }

            return result;
        }

        private void SortByTagPriority(List<TDefinition> list)
        {
            if (string.IsNullOrWhiteSpace(tagSortOrder))
            {
                // Fall back to name sort if no tags specified
                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                return;
            }

            var priorityTags = tagSortOrder
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .Where(t => t.Length > 0)
                .ToArray();

            list.Sort((a, b) =>
            {
                int pa = GetTagPriority(a, priorityTags);
                int pb = GetTagPriority(b, priorityTags);
                if (pa != pb) return pa.CompareTo(pb);
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private int GetTagPriority(TDefinition def, string[] priorityTags)
        {
            var tags = GetTags(def);
            if (tags == null || tags.Count == 0)
                return priorityTags.Length; // sort last

            int best = priorityTags.Length;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null) continue;
                string lower = tags[i].ToLower();
                for (int p = 0; p < priorityTags.Length; p++)
                {
                    if (lower == priorityTags[p] && p < best)
                    {
                        best = p;
                        break;
                    }
                }
            }
            return best;
        }

        protected virtual void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (selected != null)
            {
                EditorGUILayout.LabelField($"{DefinitionTypeName} Details", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);

                var serializedDef = new SerializedObject(selected);

                // Base fields
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{DefinitionTypeName} ID:", GUILayout.Width(120));
                EditorGUILayout.PropertyField(serializedDef.FindProperty("definitionId"), GUIContent.none);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
                EditorGUILayout.PropertyField(serializedDef.FindProperty("displayName"), GUIContent.none);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Type-specific fields
                DrawDefinitionFields(serializedDef);

                EditorGUILayout.Space();

                // Asset path
                EditorGUILayout.LabelField("Asset Information:", EditorStyles.boldLabel);
                string assetPath = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                    EditorGUILayout.SelectableLabel(assetPath, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = selected;
                        EditorGUIUtility.PingObject(selected);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (serializedDef.hasModifiedProperties)
                {
                    serializedDef.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selected);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Select a {DefinitionTypeName.ToLower()} to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNew()
        {
            if (library == null)
            {
                EditorUtility.DisplayDialog("No Library Selected", $"Please select a {typeof(TLibrary).Name} first.", "OK");
                return;
            }

            string directoryPath = AssetDirectoryPath;
            if (!AssetDatabase.IsValidFolder(directoryPath))
            {
                string parent = System.IO.Path.GetDirectoryName(directoryPath).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(directoryPath);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    string grandParent = System.IO.Path.GetDirectoryName(parent).Replace("\\", "/");
                    string parentFolder = System.IO.Path.GetFileName(parent);
                    AssetDatabase.CreateFolder(grandParent, parentFolder);
                }
                AssetDatabase.CreateFolder(parent, folder);
            }

            string baseName = $"New{DefinitionTypeName}";
            string fileName = baseName;
            int counter = 1;

            while (AssetDatabase.LoadAssetAtPath<TDefinition>($"{directoryPath}/{AssetPrefix}_{fileName}.asset") != null)
            {
                fileName = $"{baseName}{counter}";
                counter++;
            }

            string assetPath = $"{directoryPath}/{AssetPrefix}_{fileName}.asset";

            var newDef = ScriptableObject.CreateInstance<TDefinition>();

            var serializedNew = new SerializedObject(newDef);
            serializedNew.FindProperty("definitionId").stringValue = fileName.ToLower();
            serializedNew.FindProperty("displayName").stringValue = fileName.Replace("_", " ");
            serializedNew.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(newDef, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var serializedLibrary = new SerializedObject(library);
            var definitionsProperty = serializedLibrary.FindProperty("definitions");

            definitionsProperty.arraySize++;
            var newElement = definitionsProperty.GetArrayElementAtIndex(definitionsProperty.arraySize - 1);
            newElement.objectReferenceValue = newDef;

            serializedLibrary.ApplyModifiedProperties();

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            Selection.activeObject = newDef;
            EditorGUIUtility.PingObject(newDef);

            RefreshLibrary();
            selected = newDef;
        }

        private void CloneSelected()
        {
            if (selected == null) return;

            string originalPath = AssetDatabase.GetAssetPath(selected);
            string directory = System.IO.Path.GetDirectoryName(originalPath);

            string newPath = EditorUtility.SaveFilePanelInProject(
                $"Clone {DefinitionTypeName} Definition",
                System.IO.Path.GetFileNameWithoutExtension(originalPath) + "_Copy",
                "asset",
                $"Choose where to save the cloned {DefinitionTypeName} Definition",
                directory);

            if (!string.IsNullOrEmpty(newPath))
            {
                AssetDatabase.CopyAsset(originalPath, newPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var cloned = AssetDatabase.LoadAssetAtPath<TDefinition>(newPath);

                Selection.activeObject = cloned;
                EditorGUIUtility.PingObject(cloned);

                RefreshLibrary();
                selected = cloned;
            }
        }

        private void DeleteSelected()
        {
            if (selected == null) return;

            if (EditorUtility.DisplayDialog(
                $"Delete {DefinitionTypeName} Definition",
                $"Are you sure you want to delete '{selected.DisplayName}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel"))
            {
                string assetPath = AssetDatabase.GetAssetPath(selected);
                selected = null;
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                RefreshLibrary();
            }
        }
    }
}
