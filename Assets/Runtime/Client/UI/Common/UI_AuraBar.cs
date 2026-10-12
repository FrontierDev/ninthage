using Game.Shared;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_AuraBar</c> implementation.
    /// </summary>
    public abstract class UI_AuraBar : UI_Panel, IListPanel
    {
        [SerializeField] protected Actor actor;
        [SerializeField] protected bool showBuffs = true;

        [SerializeField] public GameObject container;
        [SerializeField] public UI_AuraEntry auraEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => auraEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();


        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public virtual void PopulateList(bool forceClear = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
