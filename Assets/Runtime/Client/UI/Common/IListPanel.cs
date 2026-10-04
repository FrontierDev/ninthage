using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    public interface IListPanel
    {
        GameObject contentContainer { get; }
        GameObject entryPrefab { get; }

        List<UI_ListEntry> currentEntries { get; set; }

        void PopulateList(bool forceClear = false);

        void ClearList()
        {
            foreach (Transform child in contentContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

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

        void ResetFocus()
        {
            foreach (var entry in currentEntries)
            {
                entry.Unfocus();
            }
        }
    }
}