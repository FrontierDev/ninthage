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
    /// Editor window for viewing and managing item definitions.
    /// </summary>
    public class ItemEditorWindow : DataDefinitionEditorWindow<ItemDefinition, ItemDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Equipment", "Trading", "Stats & Conditions" };
        private int selectedTemplateIndex;
        private int previewLevel = 60;

        private SerializedObject cachedSerializedObject;
        private ItemDefinition cachedDefinition;
        private GameConfiguration cachedGameConfig;
        private bool pendingTooltip;
        private string pendingTooltipText;
        private Color pendingTooltipAccentColor;

        private GameConfiguration GetGameConfig()
        {
            if (cachedGameConfig == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameConfiguration");
                if (guids.Length > 0)
                    cachedGameConfig = AssetDatabase.LoadAssetAtPath<GameConfiguration>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return cachedGameConfig;
        }

        private Color GetQualityColor(ItemQuality quality)
        {
            var config = GetGameConfig();
            if (config == null)
                return Color.white;

            return quality switch
            {
                ItemQuality.Poor => config.PoorColor,
                ItemQuality.Common => config.CommonColor,
                ItemQuality.Uncommon => config.UncommonColor,
                ItemQuality.Rare => config.RareColor,
                ItemQuality.Epic => config.EpicColor,
                ItemQuality.Legendary => config.LegendaryColor,
                _ => Color.white
            };
        }

        protected override string DefinitionTypeName => "Item";
        protected override string AssetDirectoryPath => "Assets/Data/ItemDefinitions";
        protected override string AssetPrefix => "Item";

        [MenuItem("Data/Item Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<ItemEditorWindow>("Item Definitions");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        protected override ItemDefinitionLibrary GetLibraryInstance()
        {
            return ItemDefinitionLibrary.Instance;
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
                    case 1: DrawEquipmentTab(serializedDef); break;
                    case 2: DrawTradingTab(serializedDef); break;
                    case 3: DrawStatsTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select an item to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item ID:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definitionId"), GUIContent.none);
            if (GUILayout.Button("Generate", GUILayout.Width(70)))
            {
                GenerateItemId(serializedObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quality:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("quality"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), GUIContent.none, GUILayout.Height(80));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item Set Key:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemSetKey"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            DrawItemPreview();

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

        private void DrawItemPreview()
        {
            if (Selected == null) return;

            Color qualityColor = GetQualityColor(Selected.Quality);
            float iconSize = 64;

            EditorGUILayout.LabelField("Item Preview", EditorStyles.boldLabel);

            // Outer container with dark background
            var outerRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(outerRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Icon with colored border
            var iconRect = GUILayoutUtility.GetRect(iconSize + 4, iconSize + 4, GUILayout.Width(iconSize + 4));
            EditorGUI.DrawRect(iconRect, qualityColor);
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

            // Name and quality text
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8);

            var nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = qualityColor },
                wordWrap = false
            };
            EditorGUILayout.LabelField(Selected.DisplayName ?? "(No Name)", nameStyle);

            var qualityStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = qualityColor }
            };
            EditorGUILayout.LabelField(Selected.Quality.ToString(), qualityStyle);

            if (Selected.ValidSlots != null && Selected.ValidSlots.Count > 0)
            {
                var slotNames = new List<string>();
                foreach (var slot in Selected.ValidSlots)
                    slotNames.Add(slot.ToString());
                EditorGUILayout.LabelField(string.Join(", ", slotNames), EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            // Schedule tooltip for deferred drawing (on top of everything)
            if (outerRect.width > 1 && outerRect.Contains(Event.current.mousePosition))
            {
                pendingTooltip = true;
                pendingTooltipText = BuildItemTooltip();
                pendingTooltipAccentColor = qualityColor;
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

        private string BuildItemTooltip()
        {
            if (Selected == null) return string.Empty;

            var sb = new StringBuilder();
            Color qualityColor = GetQualityColor(Selected.Quality);
            string hexColor = ColorUtility.ToHtmlStringRGB(qualityColor);

            // Simulate ItemInstance access pattern
            // var instance = new ItemInstance(guid, itemId, modifications);
            // var baseItem = instance.BaseItem;

            sb.AppendLine($"<color=#{hexColor}><b>{Selected.DisplayName ?? "(No Name)"}</b></color>");
            sb.AppendLine($"<color=#{hexColor}>{Selected.Quality}</color>");
            sb.AppendLine();

            // Slot info
            if (Selected.ValidSlots != null && Selected.ValidSlots.Count > 0)
            {
                var slotNames = new List<string>();
                foreach (var slot in Selected.ValidSlots)
                    slotNames.Add(slot.ToString());
                sb.AppendLine(string.Join(", ", slotNames));
            }

            // Item type info
            if (Selected.ItemType != ItemType.None)
            {
                sb.AppendLine();
                if (Selected.ItemType == ItemType.Weapon && Selected.WeaponType != ItemWeaponType.None)
                {
                    sb.AppendLine($"<color=#FF8800>{Selected.WeaponType} Weapon</color>");
                    if (Selected.DamagePerSecond > 0 && Selected.SwingTimer > 0)
                    {
                        float minDamage = Selected.DamagePerSecond * 0.9f * Selected.SwingTimer;
                        float maxDamage = Selected.DamagePerSecond * 1.1f * Selected.SwingTimer;
                        sb.AppendLine($"Damage: {minDamage:F0} - {maxDamage:F0}");
                    }
                    if (Selected.SwingTimer > 0)
                        sb.AppendLine($"Speed (s): {Selected.SwingTimer:F2}");
                    if (Selected.IsTwoHanded)
                        sb.AppendLine("<color=#FF4444>Two-Handed</color>");
                }
                else if (Selected.ItemType == ItemType.Armor && Selected.ArmorWeight != ItemArmorWeightClass.Cosmetic)
                {
                    sb.AppendLine($"<color=#0099FF>{Selected.ArmorWeight} Armor</color>");
                }
            }

            // Stats
            if (Selected.Stats != null && Selected.Stats.Count > 0)
            {
                sb.AppendLine();
                foreach (var stat in Selected.Stats)
                {
                    if (stat.Stat != null)
                        sb.AppendLine($"<color=#00FF00>+{stat.Value:F0} {stat.Stat.DisplayName}</color>");
                }
            }

            // Description
            if (!string.IsNullOrEmpty(Selected.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#FFD100>\"{Selected.Description}\"</color>");
            }

            // Binding info
            var def = Selected;
            if (def.UniqueFlag != ItemUniqueFlag.None)
                sb.AppendLine($"\n<color=#FF4444>{def.UniqueFlag}</color>");

            // Sell price
            if (def.CanSell)
                sb.AppendLine($"\nSell Price: {def.SellPrice}");

            // Item set
            if (!string.IsNullOrEmpty(def.ItemSetKey))
                sb.AppendLine($"\n<color=#00FFAA>Set: {def.ItemSetKey}</color>");

            return sb.ToString().TrimEnd();
        }

        private void DrawEquipmentTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Equipment Configuration", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            DrawInlineList("Valid Equipment Slots", serializedObject.FindProperty("validSlots"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Type", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemType"), new GUIContent("Type"));

            var itemTypeProp = serializedObject.FindProperty("itemType");
            if (itemTypeProp != null)
            {
                ItemType itemType = (ItemType)itemTypeProp.enumValueIndex;

                if (itemType == ItemType.Weapon)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponType"), new GUIContent("Weapon Type"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("isTwoHanded"), new GUIContent("Two-Handed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("damagePerSecond"), new GUIContent("Damage Per Second"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("swingTimer"), new GUIContent("Swing Timer (s)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("damageSchool"), new GUIContent("Damage School"));
                    EditorGUI.indentLevel--;
                }
                else if (itemType == ItemType.Armor)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("armorWeight"), new GUIContent("Armor Weight"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unique Restrictions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uniqueFlag"), new GUIContent("Unique Flag"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bindingFlag"), new GUIContent("Binding Flag"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Soulbinding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bindingFlag"), new GUIContent("Binding Flag"));
        }

        private void DrawTradingTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canStack"), new GUIContent("Can Stack"));
            if (serializedObject.FindProperty("canStack").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStackSize"), new GUIContent("Max Stack Size"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Trading Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canTrade"), new GUIContent("Can Trade"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canSell"), new GUIContent("Can Sell"));
            if (serializedObject.FindProperty("canSell").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sellPrice"), new GUIContent("Sell Price"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Modifications", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canDisenchant"), new GUIContent("Can Disenchant"));
        }

        private void DrawStatsTab(SerializedObject serializedObject)
        {
            DrawStatTemplatePanel(serializedObject);

            EditorGUILayout.Space(10);
            DrawStatsList("Stats", serializedObject.FindProperty("stats"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equip Conditions", EditorStyles.boldLabel);
            DrawConditionsList(serializedObject.FindProperty("conditions"));

            EditorGUILayout.Space(10);
            DrawStatPreview(serializedObject);
        }

        private void DrawStatTemplatePanel(SerializedObject serializedObject)
        {
            var library = ItemStatTemplateLibrary.Instance;
            if (library == null)
            {
                EditorGUILayout.HelpBox("No ItemStatTemplateLibrary found. Create one via NinthAge/Editor/Item Stat Template Library.", MessageType.Info);
                return;
            }

            var templates = library.Templates;
            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox("No stat templates in the library. Add templates to the ItemStatTemplateLibrary asset.", MessageType.Info);
                return;
            }

            string[] templateNames = new string[templates.Count];
            for (int i = 0; i < templates.Count; i++)
            {
                templateNames[i] = templates[i] != null
                    ? (!string.IsNullOrEmpty(templates[i].TemplateName) ? templates[i].TemplateName : templates[i].name)
                    : "(Missing)";
            }

            EditorGUILayout.LabelField("Stat Template", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            selectedTemplateIndex = EditorGUILayout.Popup(selectedTemplateIndex, templateNames);

            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (selectedTemplateIndex >= 0 && selectedTemplateIndex < templates.Count && templates[selectedTemplateIndex] != null)
                {
                    ApplyStatTemplate(serializedObject, templates[selectedTemplateIndex]);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyStatTemplate(SerializedObject serializedObject, ItemStatTemplate template)
        {
            var statsProp = serializedObject.FindProperty("stats");
            if (statsProp == null)
                return;

            statsProp.arraySize = template.Stats.Count;

            for (int i = 0; i < template.Stats.Count; i++)
            {
                var entry = template.Stats[i];
                var elementProp = statsProp.GetArrayElementAtIndex(i);

                var statFieldProp = elementProp.FindPropertyRelative("stat");
                var valueFieldProp = elementProp.FindPropertyRelative("value");

                if (statFieldProp != null && valueFieldProp != null)
                {
                    statFieldProp.objectReferenceValue = entry.stat;
                    valueFieldProp.floatValue = entry.value;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private Dictionary<ActorStatDefinition, List<DerivedStatInfo>> derivedStatsMap;

        private struct DerivedStatInfo
        {
            public string label;
            public float ratingPerPercent;
            public bool isRating;
            public float multiplier;
        }

        private void DrawStatPreview(SerializedObject serializedObject)
        {
            var statsProp = serializedObject.FindProperty("stats");
            if (statsProp == null || statsProp.arraySize == 0)
                return;

            if (derivedStatsMap == null)
                BuildDerivedStatsMap();

            EditorGUILayout.LabelField("Stat Preview", EditorStyles.boldLabel);

            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            EditorGUILayout.BeginHorizontal();
            previewLevel = EditorGUILayout.IntSlider("Character Level", previewLevel, 1, 60);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                derivedStatsMap = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            for (int i = 0; i < statsProp.arraySize; i++)
            {
                var element = statsProp.GetArrayElementAtIndex(i);
                var statRef = element.FindPropertyRelative("stat");
                var valueProp = element.FindPropertyRelative("value");

                if (statRef == null || statRef.objectReferenceValue == null || valueProp == null)
                    continue;

                var statDef = (ActorStatDefinition)statRef.objectReferenceValue;
                float rawValue = valueProp.floatValue;

                EditorGUILayout.BeginHorizontal();

                // Column 1: Stat name
                EditorGUILayout.LabelField(statDef.DisplayName, GUILayout.Width(150));

                // Column 2: Raw value
                EditorGUILayout.LabelField($"+{rawValue:F0}", GUILayout.Width(60));

                // Column 3: Derived stats
                if (derivedStatsMap.TryGetValue(statDef, out var derivedList) && derivedList.Count > 0)
                {
                    EditorGUILayout.BeginVertical();
                    foreach (var derived in derivedList)
                    {
                        if (derived.isRating)
                        {
                            // If this derived entry has a multiplier, the intermediate value is rawValue * multiplier
                            float ratingValue = derived.multiplier > 0 ? rawValue * derived.multiplier : rawValue;
                            float percent = CombatRatingHelper.RatingToPercent(ratingValue, previewLevel, derived.ratingPerPercent);
                            EditorGUILayout.LabelField($"({percent:F2}% {derived.label})", EditorStyles.miniLabel);
                        }
                        else
                        {
                            float derivedValue = rawValue * derived.multiplier;
                            EditorGUILayout.LabelField($"(+{derivedValue:F1} {derived.label})", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();

                if (i < statsProp.arraySize - 1)
                {
                    var sepRect = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(sepRect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void BuildDerivedStatsMap()
        {
            derivedStatsMap = new Dictionary<ActorStatDefinition, List<DerivedStatInfo>>();

            // First pass: load all stat definitions and index by source
            var allStats = new List<ActorStatDefinition>();
            var statGuids = AssetDatabase.FindAssets("t:ActorStatDefinition");
            foreach (var guid in statGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var stat = AssetDatabase.LoadAssetAtPath<ActorStatDefinition>(path);
                if (stat != null)
                    allStats.Add(stat);
            }

            // Build a map of rating stat -> its chance stat (Rating mode stats that reference it)
            var ratingToChance = new Dictionary<ActorStatDefinition, ActorStatDefinition>();
            foreach (var stat in allStats)
            {
                if (stat.BaseValueMode == ActorStatBaseValueMode.Rating && stat.SourceStat != null)
                {
                    ratingToChance[stat.SourceStat] = stat;
                }
            }

            // Second pass: build derived stats map
            foreach (var stat in allStats)
            {
                if (stat.SourceStat == null)
                    continue;

                if (!derivedStatsMap.TryGetValue(stat.SourceStat, out var list))
                {
                    list = new List<DerivedStatInfo>();
                    derivedStatsMap[stat.SourceStat] = list;
                }

                if (stat.BaseValueMode == ActorStatBaseValueMode.Rating)
                {
                    // This is a chance stat derived from a rating - show as rating percentage
                    list.Add(new DerivedStatInfo
                    {
                        label = stat.DisplayName,
                        ratingPerPercent = stat.Multiplier,
                        isRating = true
                    });
                }
                else if (stat.BaseValueMode == ActorStatBaseValueMode.Derived)
                {
                    // If this derived stat is itself a rating stat with its own chance stat,
                    // show the chance stat percentage instead
                    if (ratingToChance.TryGetValue(stat, out var chanceStat))
                    {
                        list.Add(new DerivedStatInfo
                        {
                            label = chanceStat.DisplayName,
                            ratingPerPercent = chanceStat.Multiplier,
                            isRating = true,
                            multiplier = stat.Multiplier
                        });
                    }
                    else
                    {
                        list.Add(new DerivedStatInfo
                        {
                            label = stat.DisplayName,
                            multiplier = stat.Multiplier,
                            isRating = false
                        });
                    }
                }
            }

            // Find damage school mitigations
            var schoolGuids = AssetDatabase.FindAssets("t:DamageSchoolDefinition");
            foreach (var guid in schoolGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var school = AssetDatabase.LoadAssetAtPath<DamageSchoolDefinition>(path);

                if (school == null || school.MitigationRatingStat == null)
                    continue;

                if (!derivedStatsMap.TryGetValue(school.MitigationRatingStat, out var list))
                {
                    list = new List<DerivedStatInfo>();
                    derivedStatsMap[school.MitigationRatingStat] = list;
                }

                list.Add(new DerivedStatInfo
                {
                    label = $"{school.DisplayName} Mitigation",
                    ratingPerPercent = school.RatingPerPercent,
                    isRating = true
                });
            }
        }

        private void DrawConditionsList(SerializedProperty arrayProp)
        {
            if (arrayProp == null)
            {
                EditorGUILayout.HelpBox("Conditions field not found", MessageType.Warning);
                return;
            }

            DrawInlineArrayHeader("Conditions", arrayProp);

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
                EditorGUILayout.PropertyField(element, new GUIContent($"Condition {i + 1}"));

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

        private void DrawStatsList(string label, SerializedProperty arrayProp)
        {
            if (arrayProp == null)
            {
                EditorGUILayout.HelpBox($"Property '{label}' not found", MessageType.Warning);
                return;
            }

            DrawInlineArrayHeader(label, arrayProp);

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
                var statProp = element.FindPropertyRelative("stat");
                var valueProp = element.FindPropertyRelative("value");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(statProp, GUIContent.none, GUILayout.MinWidth(150));
                EditorGUILayout.PropertyField(valueProp, GUIContent.none, GUILayout.Width(60));

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

        private void DrawInlineList(string label, SerializedProperty arrayProp)
        {
            if (arrayProp == null)
            {
                EditorGUILayout.HelpBox($"Property '{label}' not found", MessageType.Warning);
                return;
            }

            DrawInlineArrayHeader(label, arrayProp);

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
                EditorGUILayout.PropertyField(element, GUIContent.none, false);

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

        // --- Reorderable List helpers ---

        private SerializedObject GetOrCreateSerializedObject(ItemDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
            }
            return cachedSerializedObject;
        }

        private void GenerateItemId(SerializedObject serializedObject)
        {
            var displayNameProp = serializedObject.FindProperty("displayName");
            if (displayNameProp == null || string.IsNullOrEmpty(displayNameProp.stringValue))
            {
                EditorUtility.DisplayDialog("Generate Item ID", "Please enter a Display Name first", "OK");
                return;
            }

            // Convert display name to snake_case ID
            string displayName = displayNameProp.stringValue;
            string itemId = ConvertToItemId(displayName);

            var idProp = serializedObject.FindProperty("definitionId");
            if (idProp != null)
            {
                idProp.stringValue = itemId;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Item ID Generated", $"Generated ID: {itemId}", "OK");
            }
        }

        private string ConvertToItemId(string displayName)
        {
            const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const int idLength = 10;

            // Use display name hash as seed for deterministic randomness
            int seed = displayName.GetHashCode();
            System.Random random = new System.Random(seed);

            string id = "";
            for (int i = 0; i < idLength; i++)
            {
                id += alphanumeric[random.Next(alphanumeric.Length)];
            }

            return id;
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool MatchesSearchFilter(ItemDefinition definition, string lowerFilter)
        {
            if (definition.ItemSetKey != null && definition.ItemSetKey.ToLower().Contains(lowerFilter))
                return true;

            if (definition.Quality.ToString().ToLower().Contains(lowerFilter))
                return true;

            return false;
        }

        protected override void DrawListItem(ItemDefinition def, bool isSelected)
        {
            bool isValid = def.Validate();

            var buttonStyle = new GUIStyle(GUI.skin.button);
            if (isSelected)
            {
                buttonStyle.normal.background = Texture2D.grayTexture;
            }
            buttonStyle.alignment = TextAnchor.MiddleLeft;

            // Apply yellow text if invalid
            if (!isValid)
            {
                buttonStyle.normal.textColor = new Color(1f, 0.9f, 0.2f); // Yellow
            }

            if (GUILayout.Button(def.DisplayName, buttonStyle, GUILayout.Height(25)))
            {
                Selected = def;
            }

            // Warning icon if invalid
            if (!isValid)
            {
                EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.warnicon.sml"), GUILayout.Width(20), GUILayout.Height(20));
            }
        }
    }
}
