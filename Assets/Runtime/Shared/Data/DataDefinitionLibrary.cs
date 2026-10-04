using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared.Data
{
    /// <summary>
    /// Abstract base for all definition library ScriptableObjects.
    /// Provides a typed list, singleton access, lookup, and validation.
    /// </summary>
    public abstract class DataDefinitionLibrary<T> : ScriptableObject where T : DataDefinition
    {
        [SerializeField]
        private List<T> definitions = new List<T>();

        /// <summary>
        /// The serialized list of definitions, accessible to subclasses.
        /// </summary>
        protected List<T> Definitions => definitions;

        /// <summary>
        /// Get a definition by ID.
        /// </summary>
        public T GetDefinition(string definitionId)
        {
            foreach (var def in definitions)
            {
                if (def != null && def.DefinitionId == definitionId)
                    return def;
            }
            Debug.LogWarning($"[{GetType().Name}] Definition '{definitionId}' not found");
            return null;
        }

        /// <summary>
        /// Get all definitions in this library.
        /// </summary>
        public IReadOnlyList<T> GetAllDefinitions()
        {
            return definitions.AsReadOnly();
        }

        /// <summary>
        /// Check if a definition ID exists in this library.
        /// </summary>
        public bool HasDefinition(string definitionId)
        {
            return definitions.Any(d => d != null && d.DefinitionId == definitionId);
        }

        /// <summary>
        /// Validates all definitions in the library. Removes nulls and checks for duplicates.
        /// </summary>
        public bool ValidateLibrary()
        {
            bool isValid = true;
            var seenIds = new HashSet<string>();

            for (int i = definitions.Count - 1; i >= 0; i--)
            {
                if (definitions[i] == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Removing null definition from library");
                    definitions.RemoveAt(i);
                }
            }

            foreach (var def in definitions)
            {
                if (!def.IsValid())
                {
                    isValid = false;
                    continue;
                }

                if (seenIds.Contains(def.DefinitionId))
                {
                    Debug.LogError($"[{GetType().Name}] Duplicate definition ID: {def.DefinitionId}");
                    isValid = false;
                }
                else
                {
                    seenIds.Add(def.DefinitionId);
                }
            }

            return isValid;
        }

        private void OnValidate()
        {
            ValidateLibrary();
        }
    }
}
