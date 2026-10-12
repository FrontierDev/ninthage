using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Defines the client-side <c>IListPanel</c> contract.
    /// </summary>
    public interface IListPanel
    {
        /// <summary>Gets the container for list entries.</summary>
        GameObject contentContainer { get; }

        /// <summary>Gets the prefab used for list entries.</summary>
        GameObject entryPrefab { get; }

        /// <summary>Gets or sets the entries currently displayed by the panel.</summary>
        List<UI_ListEntry> currentEntries { get; set; }

        /// <summary>Populates the panel with its current entries.</summary>
        void PopulateList(bool forceClear = false);

        /// <summary>Removes all entries from the panel.</summary>
        void ClearList()
        {
            foreach (Transform child in contentContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        /// <summary>Focuses the entry at the specified index.</summary>
        void FocusAt(int index)
        {
            for (int i = 0; i < currentEntries.Count; i++)
            {
                if (i == index)
                {
                    currentEntries[i].Focus();
                }
                else
                {
                    currentEntries[i].Unfocus();
                }
            }
        }

        /// <summary>Clears focus from every entry in the panel.</summary>
        void ResetFocus()
        {
            foreach (var entry in currentEntries)
            {
                entry.Unfocus();
            }
        }
    }
}
