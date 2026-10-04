using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Data
{
    /// <summary>
    /// Editor-only stat template for quickly populating item stats.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemStatTemplate_", menuName = "NinthAge/Editor/Item Stat Template")]
    public class ItemStatTemplate : ScriptableObject
    {
        [SerializeField] private string templateName;
        [SerializeField] private List<StatEntry> stats = new List<StatEntry>();

        public string TemplateName => templateName;
        public IReadOnlyList<StatEntry> Stats => stats;

        [System.Serializable]
        public class StatEntry
        {
            public ActorStatDefinition stat;
            public float value;
        }
    }

    /// <summary>
    /// Editor-only library holding all item stat templates. Lives in the Editor folder.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemStatTemplateLibrary", menuName = "NinthAge/Editor/Item Stat Template Library")]
    public class ItemStatTemplateLibrary : ScriptableObject
    {
        [SerializeField] private List<ItemStatTemplate> templates = new List<ItemStatTemplate>();

        public IReadOnlyList<ItemStatTemplate> Templates => templates;

        private static ItemStatTemplateLibrary instance;

        public static ItemStatTemplateLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindLibrary();
                }
                return instance;
            }
        }

        private static ItemStatTemplateLibrary FindLibrary()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(ItemStatTemplateLibrary));
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<ItemStatTemplateLibrary>(path);
                if (candidate != null)
                    return candidate;
            }

            // Fallback: try loading by known path
            return AssetDatabase.LoadAssetAtPath<ItemStatTemplateLibrary>("Assets/Editor/Data/ItemStatTemplateLibrary.asset");
        }

        public static void ResetInstance()
        {
            instance = null;
        }
    }
}
