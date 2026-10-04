using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    [Obsolete]
    [ToDo("Remove this as it is obsolete.")]
    public sealed class UI_CharacterStatTypeButton : UI_Button
    {
        [SerializeField] private string statTypeTag;

        public override void OnClick(PointerEventData eventData)
        {
            // Do nothing.
        }
    }
}