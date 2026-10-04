namespace Game.Editor.Inspectors
{
    using System.Linq;
    using Game.Shared;
    using Game.Shared.Data;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ActorSpellcaster))]
    public class ActorSpellcasterInspector : Editor
    {
        private bool showSpells = true;
        private Vector2 scrollPosition;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var actorSpellcaster = (ActorSpellcaster)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Spells", EditorStyles.boldLabel);

            showSpells = EditorGUILayout.Foldout(showSpells, $"Spells ({actorSpellcaster.Spells.Count})", true);
            if (!showSpells) return;

            if (actorSpellcaster.Spells.Count == 0)
            {
                EditorGUILayout.HelpBox("No spells are available.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));
            EditorGUI.indentLevel++;

            foreach (var kvp in actorSpellcaster.Spells.OrderBy(x => x.Key))
            {
                if (kvp.Value == null) continue;

                string spellID = kvp.Key;
                SpellOverride spellOverride = kvp.Value;
                SpellDefinition definition = spellOverride.BaseDefinition;

                string spellName = definition != null ? definition.SpellName : "(Unknown)";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Spell ID: {spellID}", EditorStyles.miniLabel, GUILayout.Width(100));
                if (definition != null && definition.Icon != null)
                {
                    var iconRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    GUI.DrawTexture(iconRect, definition.Icon.texture, ScaleMode.ScaleToFit);
                }
                EditorGUILayout.LabelField($"{spellName}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;

                // Show the current override values
                EditorGUILayout.LabelField("Range", spellOverride.Range.ToString());
                EditorGUILayout.LabelField("Cooldown", spellOverride.Cooldown.ToString());
                EditorGUILayout.LabelField("Cast Time", spellOverride.CastTime.ToString());
                EditorGUILayout.LabelField("Total Ticks", spellOverride.TotalTicks.ToString());

                // Show the components and their effects
                if (spellOverride.Components.Count > 0)
                {
                    EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);
                    foreach (var component in spellOverride.Components)
                    {
                        string targetType = component.TargetDefinition != null ? component.TargetDefinition.GetType().Name.Split("_")[0] : "Unknown";
                        string effectType = component.EffectDefinition != null ? component.EffectDefinition.GetType().Name.Split("_")[0] : "Unknown";
                        EditorGUILayout.LabelField($"- Target: {targetType}, Effect: {effectType}");

                        if (component.EffectDefinition is SpellEffect_Damage damageEffect)
                        {
                            string damageSchools = string.Join(", ", damageEffect.DamageSchools.Select(ds => ds.DisplayName));
                            EditorGUILayout.LabelField($"  Damage Schools: {damageSchools}");
                            EditorGUILayout.LabelField($"  Base Damage: {damageEffect.BaseDamage}");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No spell components.");
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
