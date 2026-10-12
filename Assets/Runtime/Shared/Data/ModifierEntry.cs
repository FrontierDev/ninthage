using Game.Shared;
using System;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Wraps a polymorphic spell modifier for serialization.</summary>
    public sealed class ModifierEntry
    {
        /// <summary>Gets or sets the spell modifier.</summary>
        [SerializeReference] public ISpellModifier Modifier;
    }
}
