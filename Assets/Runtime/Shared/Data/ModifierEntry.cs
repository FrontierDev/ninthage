using System;
using UnityEngine;
using Game.Shared;

namespace Game.Shared.Data
{
    [Serializable]
    public sealed class ModifierEntry
    {
        [SerializeReference] public ISpellModifier Modifier;
    }
}
