using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client.UI
{
    public abstract class UI_AuraBar : UI_Panel, IListPanel
    {
        [SerializeField] protected Actor actor;
        [SerializeField] protected bool showBuffs = true;

        [SerializeField] public GameObject container;
        [SerializeField] public UI_AuraEntry auraEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => auraEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();


        public virtual void PopulateList(bool forceClear = false)
        {
            throw new System.NotImplementedException();
        }
    }
}