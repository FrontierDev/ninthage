using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Generic definition explorer window that allows searching and selecting definitions from any library.
    /// Can be opened as a modal popup to select a single definition and populate a callback.
    /// </summary>
    public class DefinitionExplorerWindow : EditorWindow
    {
        private ScriptableObject library;
        private Delegate onSelectionCallback;
        private string searchQuery = "";
        private Vector2 scrollPosition;
        private List<DataDefinition> filteredDefinitions = new();
        private System.Type defType;

        private enum SearchMode { ID, Name, Tags, All }
        private SearchMode searchMode = SearchMode.All;

        // Defer selection to avoid layout issues
        private DataDefinition pendingSelection;
        private bool shouldClose;

        /// <summary>
        /// Opens the explorer window as a modal popup that calls the callback when a definition is selected.
        /// </summary>
        public static void Open<T>(DataDefinitionLibrary<T> lib, Action<T> onSelected) where T : DataDefinition
        {
            if (lib == null)
            {
                EditorUtility.DisplayDialog("Error", "Definition library is null", "OK");
                return;
            }

            var window = GetWindow<DefinitionExplorerWindow>(utility: true, title: $"Select {typeof(T).Name}");
            window.InitializeGeneric(lib, onSelected, typeof(T));
            window.minSize = new Vector2(400, 300);
            window.ShowModal();
        }

        private void InitializeGeneric<T>(DataDefinitionLibrary<T> lib, Action<T> onSelected, System.Type type) where T : DataDefinition
        {
            library = lib;
            defType = type;
            // Wrap the generic action in a delegate
            onSelectionCallback = onSelected;
            pendingSelection = null;
            shouldClose = false;
            RefreshList();
        }

        private void OnGUI()
        {
            if (defType != null)
            {
                EditorGUILayout.LabelField($"Select {defType.Name}", EditorStyles.boldLabel);
            }
            EditorGUILayout.Space();

            if (library == null)
            {
                EditorGUILayout.HelpBox("Library not found", MessageType.Error);
                return;
            }

            // Search controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            string newSearchQuery = EditorGUILayout.TextField(searchQuery);
            if (newSearchQuery != searchQuery)
            {
                searchQuery = newSearchQuery;
                RefreshList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mode:", GUILayout.Width(60));
            var newMode = (SearchMode)EditorGUILayout.EnumPopup(searchMode);
            if (newMode != searchMode)
            {
                searchMode = newMode;
                RefreshList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Results list
            EditorGUILayout.LabelField($"Results: {filteredDefinitions.Count}", EditorStyles.miniLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (filteredDefinitions.Count == 0)
            {
                EditorGUILayout.LabelField("No definitions found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < filteredDefinitions.Count; i++)
                {
                    var def = filteredDefinitions[i];
                    if (def == null) continue;

                    var entryRect = EditorGUILayout.GetControlRect(GUILayout.Height(40));

                    // Draw button background
                    EditorGUI.DrawRect(entryRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));

                    // Draw text content
                    EditorGUI.LabelField(new Rect(entryRect.x + 5, entryRect.y + 5, entryRect.width - 10, 20), def.DisplayName, EditorStyles.boldLabel);
                    EditorGUI.LabelField(new Rect(entryRect.x + 5, entryRect.y + 20, entryRect.width - 10, 15), def.DefinitionId, EditorStyles.miniLabel);

                    // Make it clickable
                    if (GUI.Button(entryRect, "", GUIStyle.none))
                    {
                        pendingSelection = def;
                        shouldClose = true;
                    }

                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();

            // Close button
            EditorGUILayout.Space();
            if (GUILayout.Button("Cancel", GUILayout.Height(25)))
            {
                shouldClose = true;
            }

            // Execute deferred selection after layout is complete
            if (shouldClose && Event.current.type == EventType.Repaint)
            {
                if (pendingSelection != null)
                {
                    onSelectionCallback?.DynamicInvoke(pendingSelection);
                    pendingSelection = null;
                }
                shouldClose = false;
                Close();
            }
        }

        private void RefreshList()
        {
            filteredDefinitions.Clear();

            if (library == null || defType == null)
                return;

            // Get GetAllDefinitions method via reflection
            var methodInfo = library.GetType().GetMethod("GetAllDefinitions");
            if (methodInfo == null)
                return;

            var allDefsObj = methodInfo.Invoke(library, null);
            if (allDefsObj is not System.Collections.IEnumerable allDefs)
                return;

            string searchLower = searchQuery.ToLower();

            foreach (var def in allDefs)
            {
                if (def is not DataDefinition dataDef)
                    continue;

                bool matches = false;

                switch (searchMode)
                {
                    case SearchMode.ID:
                        matches = dataDef.DefinitionId.ToLower().Contains(searchLower);
                        break;

                    case SearchMode.Name:
                        matches = dataDef.name.ToLower().Contains(searchLower);
                        break;

                    case SearchMode.Tags:
                        if (dataDef is ITaggedDefinition tagged && tagged.Tags != null)
                        {
                            matches = tagged.Tags.Any(t => t.ToLower().Contains(searchLower));
                        }
                        break;

                    case SearchMode.All:
                    default:
                        matches = dataDef.DefinitionId.ToLower().Contains(searchLower) ||
                                 dataDef.name.ToLower().Contains(searchLower);

                        if (!matches && dataDef is ITaggedDefinition taggedDef && taggedDef.Tags != null)
                        {
                            matches = taggedDef.Tags.Any(t => t.ToLower().Contains(searchLower));
                        }
                        break;
                }

                if (matches)
                {
                    filteredDefinitions.Add(dataDef);
                }
            }
        }
    }

    /// <summary>
    /// Interface for definitions that have tags for searching.
    /// </summary>
    public interface ITaggedDefinition
    {
        List<string> Tags { get; }
    }
}
