using System;
using System.Collections.Generic;
using Game.Shared.Data.Conditions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data.Dialogue
{
    [Serializable]
    /// <summary>Defines a dialogue condition and its inversion setting.</summary>
    public class DialogueCondition
    {
        // Polymorphic condition instance — use SerializeReference to preserve derived types.
        [SerializeReference]
        [FormerlySerializedAs("condition")]
        private ConditionDefinition _condition;
        /// <summary>Gets or sets the polymorphic condition.</summary>
        public ConditionDefinition Condition
        {
            get => _condition;
            set => _condition = value;
        }

        [SerializeField, FormerlySerializedAs("invert")] private bool _invert = false;
        /// <summary>Gets or sets whether the condition result is inverted.</summary>
        public bool Invert
        {
            get => _invert;
            set => _invert = value;
        }
    }
}
