using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>
    /// Abstract base for all definition library ScriptableObjects.
    /// Provides a typed list, singleton access, lookup, and validation.
    /// </summary>
    public abstract class DataDefinitionLibrary<T> : ScriptableObject where T : DataDefinition
    {
        [SerializeField]
        [FormerlySerializedAs("definitions")]
        private List<T> _definitions = new();

        /// <summary>
        /// The serialized list of definitions, accessible to subclasses.
        /// </summary>
        protected List<T> Definitions => _definitions;

        /// <summary>
        /// Get a definition by ID.
        /// </summary>
        public T GetDefinition(string definitionID)
        {
            foreach (var def in _definitions)
            {
                if (def != null && def.DefinitionID == definitionID)
                    return def;
            }
            Debug.LogWarning($"[{GetType().Name}] Definition '{definitionID}' not found");
            return null;
        }

        /// <summary>
        /// Get all definitions in this library.
        /// </summary>
        public IReadOnlyList<T> GetAllDefinitions()
        {
            return _definitions.AsReadOnly();
        }

        /// <summary>
        /// Check if a definition ID exists in this library.
        /// </summary>
        public bool HasDefinition(string definitionID)
        {
            return _definitions.Any(d => d != null && d.DefinitionID == definitionID);
        }

        /// <summary>
        /// Validates all definitions in the library. Removes nulls and checks for duplicates.
        /// </summary>
        public bool ValidateLibrary()
        {
            bool isValid = true;
            var seenIds = new HashSet<string>();

            for (int i = _definitions.Count - 1; i >= 0; i--)
            {
                if (_definitions[i] == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] Removing null definition from library");
                    _definitions.RemoveAt(i);
                }
            }

            foreach (var def in _definitions)
            {
                if (!def.IsValid())
                {
                    isValid = false;
                    continue;
                }

                if (seenIds.Contains(def.DefinitionID))
                {
                    Debug.LogError($"[{GetType().Name}] Duplicate definition ID: {def.DefinitionID}");
                    isValid = false;
                }
                else
                {
                    seenIds.Add(def.DefinitionID);
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
