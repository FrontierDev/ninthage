using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.Client.UI
{
    public abstract class UI_ListEntry : UI_Button
    {
        public IListPanel parentPanel;
        public bool isFocused;

        public virtual void Register(IListPanel _parentPanel)
        {
            parentPanel = _parentPanel;
            parentPanel.currentEntries.Add(this);
        }

        public virtual void Destroy()
        {
            parentPanel.currentEntries.Remove(this);
            GameObject.Destroy(this.gameObject);
        }

        public virtual void Focus() { }
        public virtual void Unfocus() { }
        public virtual void Highlight() { }
        public virtual void ClearHighlight() { }
    }

    public abstract class UI_ListEntry<T> : UI_ListEntry
    {
        public abstract void Initialize(T data, int index);
    }
}