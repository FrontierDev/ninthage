namespace Game.Editor.Inspectors
{
    using Game.Shared;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PlayerInventory))]
    public class PlayerInventoryInspector : Editor
    {
        private bool showInventory = true;
        private Vector2 scrollPosition;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var playerInventory = (PlayerInventory)target;
            var inventory = playerInventory.Inventory;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Inventory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Slots Used: {inventory.Count} / {playerInventory.MaxSlots}");

            showInventory = EditorGUILayout.Foldout(showInventory, $"Items ({inventory.Count})", true);
            if (!showInventory) return;

            if (inventory.Count == 0)
            {
                EditorGUILayout.HelpBox("Inventory is empty.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));
            EditorGUI.indentLevel++;

            foreach (var kvp in inventory)
            {
                int slot = kvp.Key;
                var item = kvp.Value;
                var definition = item.BaseItem;
                string itemName = definition != null ? definition.DisplayName : "(Unknown)";
                string stackInfo = definition != null && definition.CanStack
                    ? $" x{item.CurrentStackSize}/{definition.MaxStackSize}"
                    : "";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Slot {slot}", EditorStyles.miniLabel, GUILayout.Width(50));
                if (definition != null && definition.Icon != null)
                {
                    var iconRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    GUI.DrawTexture(iconRect, definition.Icon.texture, ScaleMode.ScaleToFit);
                }
                EditorGUILayout.LabelField($"{itemName}{stackInfo}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                if (definition != null)
                {
                    EditorGUILayout.LabelField("Quality", definition.Quality.ToString());
                    EditorGUILayout.LabelField("Type", definition.ItemType.ToString());
                }
                EditorGUILayout.LabelField("Durability", $"{item.Durability}/100");
                EditorGUILayout.LabelField("GUID", item.GUID, EditorStyles.miniLabel);

                if (item.Modifications != null && item.Modifications.Count > 0)
                {
                    EditorGUILayout.LabelField($"Modifications ({item.Modifications.Count}):");
                    EditorGUI.indentLevel++;
                    foreach (var mod in item.Modifications)
                        EditorGUILayout.LabelField(mod, EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Refresh"))
                    Repaint();
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;
    }
}
