using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing NPC prefabs.
    /// Operates directly on prefab assets that contain an NPCActor component.
    /// </summary>
    public class NPCEditorWindow : EditorWindow
    {
        // --- Left panel ---
        private Vector2 leftScrollPosition;
        private List<GameObject> npcPrefabs = new();
        private GameObject selected;
        private string searchFilter = "";
        private string scanFolder = "Assets";

        // --- Right panel ---
        private Vector2 rightScrollPosition;
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Combat", "Stats", "Spells" };

        // Cached serialized objects (invalidated when selection changes)
        private GameObject cachedPrefab;
        private SerializedObject actorSO;
        private SerializedObject behaviorSO;
        private SerializedObject statProfileSO;
        private SerializedObject spellcasterSO;

        // Reorderable lists (nullified when selection changes)
        private ReorderableList lootTableList;
        private ReorderableList baseStatsList;
        private ReorderableList knownSpellsList;

        // Parallel list of SpellDefinition assets representing knownSpellIDs
        private List<SpellDefinition> cachedSpellDefinitions = new();

        [MenuItem("Data/NPC Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<NPCEditorWindow>("NPC Editor");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPrefabs();
        }

        private void RefreshPrefabs()
        {
            npcPrefabs.Clear();

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { scanFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.TryGetComponent<NPCActor>(out _))
                    npcPrefabs.Add(go);
            }

            npcPrefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // -------------------------------------------------------------------------
        // Left panel
        // -------------------------------------------------------------------------

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNPCPrefab();

            GUI.enabled = selected != null;
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50)))
                DeleteSelected();
            GUI.enabled = true;

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshPrefabs();
            EditorGUILayout.EndHorizontal();

            // Search
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Search:", EditorStyles.miniLabel, GUILayout.Width(45));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField);
            EditorGUILayout.EndHorizontal();

            // Scan folder
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Folder:", EditorStyles.miniLabel, GUILayout.Width(45));
            var newFolder = EditorGUILayout.TextField(scanFolder, EditorStyles.toolbarTextField);
            if (newFolder != scanFolder)
            {
                scanFolder = newFolder;
                RefreshPrefabs();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            var filtered = GetFiltered();
            EditorGUILayout.LabelField($"Total NPCs: {npcPrefabs.Count}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(searchFilter))
                EditorGUILayout.LabelField($"Showing: {filtered.Count}", EditorStyles.miniLabel);

            EditorGUILayout.Space();

            leftScrollPosition = EditorGUILayout.BeginScrollView(leftScrollPosition);

            if (filtered.Count > 0)
            {
                foreach (var prefab in filtered)
                {
                    if (prefab == null) continue;

                    bool isSelected = selected == prefab;

                    var buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                    if (isSelected)
                        buttonStyle.normal.background = Texture2D.grayTexture;

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(prefab.name, buttonStyle, GUILayout.Height(25)))
                        SelectPrefab(prefab);
                    EditorGUILayout.EndHorizontal();

                    if (isSelected)
                    {
                        EditorGUI.indentLevel++;
                        var npcActor = prefab.GetComponent<NPCActor>();
                        string npcId = npcActor != null && !string.IsNullOrEmpty(npcActor.NpcID)
                            ? npcActor.NpcID
                            : "(no ID)";
                        EditorGUILayout.LabelField($"ID: {npcId}", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No NPC prefabs found", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField($"Prefabs must have an NPCActor component", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private List<GameObject> GetFiltered()
        {
            if (string.IsNullOrEmpty(searchFilter))
                return new List<GameObject>(npcPrefabs);

            string lower = searchFilter.ToLower();
            return npcPrefabs.Where(p =>
            {
                if (p == null) return false;

                if (p.name.ToLower().Contains(lower)) return true;
                var actor = p.GetComponent<NPCActor>();

                if (actor != null && actor.NpcID != null && actor.NpcID.ToLower().Contains(lower)) return true;
                var profile = p.GetComponent<NPCActor>();

                if (profile != null && profile.Name != null && profile.Name.ToLower().Contains(lower)) return true;
                return false;
            }).ToList();
        }

        private void SelectPrefab(GameObject prefab)
        {
            selected = prefab;
            cachedPrefab = null;
        }

        // -------------------------------------------------------------------------
        // Right panel
        // -------------------------------------------------------------------------

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (selected != null)
            {
                EnsureSerializedObjects();

                // Update all serialized objects before drawing
                actorSO?.Update();
                behaviorSO?.Update();
                statProfileSO?.Update();
                spellcasterSO?.Update();

                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                EditorGUILayout.Space();

                rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);

                switch (selectedTab)
                {
                    case 0: DrawGeneralTab(); break;
                    case 1: DrawCombatTab(); break;
                    case 2: DrawStatsTab(); break;
                    case 3: DrawSpellsTab(); break;
                }

                EditorGUILayout.EndScrollView();

                // Apply all modified properties after drawing
                ApplyAllModifiedProperties();

                // Prefab info footer
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Prefab Information", EditorStyles.boldLabel);
                string assetPath = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                    EditorGUILayout.SelectableLabel(assetPath, EditorStyles.miniLabel,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = selected;
                        EditorGUIUtility.PingObject(selected);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select an NPC prefab to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // -------------------------------------------------------------------------
        // Serialized object cache
        // -------------------------------------------------------------------------

        private void EnsureSerializedObjects()
        {
            if (cachedPrefab == selected && actorSO != null)
                return;

            cachedPrefab = selected;

            var actor = selected.GetComponent<NPCActor>();
            actorSO = actor != null ? new SerializedObject(actor) : null;

            var behavior = selected.GetComponent<NPCBehavior>();
            behaviorSO = behavior != null ? new SerializedObject(behavior) : null;

            var statProfile = selected.GetComponent<NPCStatProfile>();
            statProfileSO = statProfile != null ? new SerializedObject(statProfile) : null;

            var spellcaster = selected.GetComponent<ActorSpellcaster>();
            spellcasterSO = spellcaster != null ? new SerializedObject(spellcaster) : null;

            // Invalidate reorderable lists
            lootTableList = null;
            baseStatsList = null;
            knownSpellsList = null;

            AutoSetNpcID(selected);
            LoadCachedSpellDefinitions();
        }

        /// <summary>
        /// Assigns a stable GUID to NpcID if one has not been set yet.
        /// </summary>
        private void AutoSetNpcID(GameObject prefab)
        {
            if (actorSO == null) return;

            var idProp = actorSO.FindProperty("NpcID");
            if (!string.IsNullOrEmpty(idProp.stringValue)) return;

            idProp.stringValue = System.Guid.NewGuid().ToString();
            actorSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab.GetComponent<NPCActor>());
            PrefabUtility.SavePrefabAsset(prefab);
        }

        /// <summary>
        /// Builds cachedSpellDefinitions from the spellcaster's knownSpellIDs by looking
        /// up SpellDefinition assets in the project.
        /// </summary>
        private void LoadCachedSpellDefinitions()
        {
            cachedSpellDefinitions = new List<SpellDefinition>();
            var spellcaster = selected.GetComponent<ActorSpellcaster>();
            if (spellcaster == null) return;

            var allSpells = AssetDatabase.FindAssets("t:SpellDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<SpellDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .ToDictionary(s => s.DefinitionId, s => s);

            foreach (var id in spellcaster.KnownSpellIDs)
            {
                allSpells.TryGetValue(id, out var spellDef);
                cachedSpellDefinitions.Add(spellDef);
            }
        }

        /// <summary>
        /// Writes cachedSpellDefinitions back to knownSpellIDs on the ActorSpellcaster.
        /// Null entries (unset slots) are skipped.
        /// </summary>
        private void SyncSpellIDsToComponent()
        {
            if (spellcasterSO == null) return;

            var prop = spellcasterSO.FindProperty("knownSpellIDs");
            prop.ClearArray();

            foreach (var def in cachedSpellDefinitions)
            {
                if (def == null) continue;
                prop.InsertArrayElementAtIndex(prop.arraySize);
                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = def.DefinitionId;
            }

            spellcasterSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(selected.GetComponent<ActorSpellcaster>());
        }

        private void ApplyAllModifiedProperties()
        {
            ApplyIfModified(actorSO, selected.GetComponent<NPCActor>());
            ApplyIfModified(behaviorSO, selected.GetComponent<NPCBehavior>());
            ApplyIfModified(statProfileSO, selected.GetComponent<NPCStatProfile>());
            ApplyIfModified(spellcasterSO, selected.GetComponent<ActorSpellcaster>());
        }

        private static void ApplyIfModified(SerializedObject so, Component comp)
        {
            if (so == null || comp == null) return;
            if (so.hasModifiedProperties)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
            }
        }

        // -------------------------------------------------------------------------
        // General tab
        // -------------------------------------------------------------------------

        private void DrawGeneralTab()
        {
            EditorGUILayout.LabelField("NPC Identity", EditorStyles.boldLabel);

            if (actorSO != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NPC ID:", GUILayout.Width(130));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(actorSO.FindProperty("NpcID").stringValue);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Regenerate", GUILayout.Width(90)))
                {
                    if (EditorUtility.DisplayDialog("Regenerate NPC ID",
                        "Generating a new GUID will break any existing references to this NPC ID. Continue?",
                        "Regenerate", "Cancel"))
                    {
                        var idProp = actorSO.FindProperty("NpcID");
                        idProp.stringValue = System.Guid.NewGuid().ToString();
                        actorSO.ApplyModifiedProperties();
                        var actor = selected.GetComponent<NPCActor>();
                        EditorUtility.SetDirty(actor);
                        PrefabUtility.SavePrefabAsset(selected);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("NPCActor component not found on this prefab.", MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);

            if (statProfileSO != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Display Name:", GUILayout.Width(130));
                EditorGUILayout.PropertyField(statProfileSO.FindProperty("npcName"), GUIContent.none);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Level:", GUILayout.Width(130));
                EditorGUILayout.PropertyField(statProfileSO.FindProperty("npcLevel"), GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("NPCStatProfile component not found on this prefab.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset:", GUILayout.Width(130));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(selected, typeof(GameObject), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        // -------------------------------------------------------------------------
        // Combat tab
        // -------------------------------------------------------------------------

        private void DrawCombatTab()
        {
            if (behaviorSO == null)
            {
                EditorGUILayout.HelpBox("NPCBehavior component not found on this prefab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Experience", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Experience:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(behaviorSO.FindProperty("baseExperience"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Aggro", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Aggro Range:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(behaviorSO.FindProperty("aggroRange"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Aggro Delay:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(behaviorSO.FindProperty("aggroDelay"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Faction & Dialogue", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Combat Faction:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(behaviorSO.FindProperty("combatFaction"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Dialogue:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(behaviorSO.FindProperty("dialogue"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Loot Table", EditorStyles.boldLabel);

            GetLootTableList().DoLayoutList();
        }

        private ReorderableList GetLootTableList()
        {
            if (lootTableList != null) return lootTableList;

            var prop = behaviorSO.FindProperty("lootTable");
            lootTableList = new ReorderableList(behaviorSO, prop, true, true, true, true);
            lootTableList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Loot Entries");
            lootTableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = prop.GetArrayElementAtIndex(index);
                float half = rect.width * 0.65f;
                float padding = 4f;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + 2, half - padding, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("lootTable"), GUIContent.none);

                EditorGUI.LabelField(
                    new Rect(rect.x + half, rect.y + 2, 40f, EditorGUIUtility.singleLineHeight),
                    "Rolls", EditorStyles.miniLabel);

                EditorGUI.PropertyField(
                    new Rect(rect.x + half + 44f, rect.y + 2, rect.width - half - 44f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("rolls"), GUIContent.none);
            };
            lootTableList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            return lootTableList;
        }

        // -------------------------------------------------------------------------
        // Stats tab
        // -------------------------------------------------------------------------

        private void DrawStatsTab()
        {
            if (statProfileSO == null)
            {
                EditorGUILayout.HelpBox("NPCStatProfile component not found on this prefab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Weapon Stats", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Main-Hand DPS:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(statProfileSO.FindProperty("mainHandDPS"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Off-Hand DPS:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(statProfileSO.FindProperty("offHandDPS"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Main-Hand Swing:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(statProfileSO.FindProperty("mainHandSwingSpeed"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Off-Hand Swing:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(statProfileSO.FindProperty("offHandSwingSpeed"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);

            GetBaseStatsList().DoLayoutList();
        }

        private ReorderableList GetBaseStatsList()
        {
            if (baseStatsList != null) return baseStatsList;

            var prop = statProfileSO.FindProperty("baseStats");
            baseStatsList = new ReorderableList(statProfileSO, prop, true, true, true, true);
            baseStatsList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Base Stats");
            baseStatsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = prop.GetArrayElementAtIndex(index);
                float half = rect.width * 0.6f;
                float padding = 4f;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + 2, half - padding, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("definition"), GUIContent.none);

                EditorGUI.LabelField(
                    new Rect(rect.x + half, rect.y + 2, 36f, EditorGUIUtility.singleLineHeight),
                    "Base", EditorStyles.miniLabel);

                EditorGUI.PropertyField(
                    new Rect(rect.x + half + 40f, rect.y + 2, rect.width - half - 40f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("baseValue"), GUIContent.none);
            };
            baseStatsList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            return baseStatsList;
        }

        // -------------------------------------------------------------------------
        // Spells tab
        // -------------------------------------------------------------------------

        private void DrawSpellsTab()
        {
            if (spellcasterSO == null)
            {
                EditorGUILayout.HelpBox("ActorSpellcaster component not found on this prefab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Spellcaster Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Effect Spawn Point:", GUILayout.Width(150));
            EditorGUILayout.PropertyField(spellcasterSO.FindProperty("spellEffectSpawnPoint"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Known Spells", EditorStyles.boldLabel);

            GetKnownSpellsList().DoLayoutList();
        }

        private ReorderableList GetKnownSpellsList()
        {
            if (knownSpellsList != null) return knownSpellsList;

            knownSpellsList = new ReorderableList(cachedSpellDefinitions, typeof(SpellDefinition), true, true, true, true);
            knownSpellsList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Known Spells");
            knownSpellsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= cachedSpellDefinitions.Count) return;

                var prev = cachedSpellDefinitions[index];
                var next = (SpellDefinition)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
                    prev, typeof(SpellDefinition), false);

                if (next != prev)
                {
                    cachedSpellDefinitions[index] = next;
                    SyncSpellIDsToComponent();
                }
            };
            knownSpellsList.onAddCallback = _ =>
                cachedSpellDefinitions.Add(null);
            knownSpellsList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < cachedSpellDefinitions.Count)
                    cachedSpellDefinitions.RemoveAt(list.index);
                SyncSpellIDsToComponent();
            };
            knownSpellsList.onReorderCallbackWithDetails = (_, _, _) =>
                SyncSpellIDsToComponent();
            knownSpellsList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            return knownSpellsList;
        }

        // -------------------------------------------------------------------------
        // Create / Delete
        // -------------------------------------------------------------------------

        private void CreateNPCPrefab()
        {
            string defaultFolder = scanFolder.StartsWith("Assets") ? scanFolder : "Assets";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create NPC Prefab",
                "NPC_New",
                "prefab",
                "Choose where to save the new NPC prefab",
                defaultFolder);

            if (string.IsNullOrEmpty(path)) return;

            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            go.AddComponent<NPCActor>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            if (prefab != null)
            {
                AssetDatabase.Refresh();
                RefreshPrefabs();
                SelectPrefab(prefab);
            }
        }

        private void DeleteSelected()
        {
            if (selected == null) return;

            if (!EditorUtility.DisplayDialog(
                "Delete NPC Prefab",
                $"Are you sure you want to delete '{selected.name}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel"))
                return;

            string assetPath = AssetDatabase.GetAssetPath(selected);
            selected = null;
            cachedPrefab = null;
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshPrefabs();
        }
    }
}
