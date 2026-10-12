using Game.Shared.Runtime;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterStatTypeButton</c> implementation.
    /// </summary>
    [Obsolete]
    [ToDo("Remove this as it is obsolete.")]
    public sealed class UI_CharacterStatTypeButton : UI_Button
    {
        [FormerlySerializedAs("statTypeTag")]
        [SerializeField] private string _statTypeTag;

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            // Do nothing.
        }
    }
}
