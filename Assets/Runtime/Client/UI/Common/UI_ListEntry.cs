using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ListEntry</c> implementation.
    /// </summary>
    public abstract class UI_ListEntry : UI_Button
    {
        /// <summary>
        /// Stores the <c>parentPanel</c> value.
        /// </summary>
        public IListPanel parentPanel;
        /// <summary>
        /// Stores the <c>isFocused</c> value.
        /// </summary>
        public bool isFocused;

        /// <summary>
        /// Executes the client-side <c>Register</c> operation.
        /// </summary>
        public virtual void Register(IListPanel _parentPanel)
        {
            parentPanel = _parentPanel;
            parentPanel.currentEntries.Add(this);
        }

        /// <summary>
        /// Executes the client-side <c>Destroy</c> operation.
        /// </summary>
        public virtual void Destroy()
        {
            parentPanel.currentEntries.Remove(this);
            GameObject.Destroy(this.gameObject);
        }

        /// <summary>
        /// Executes the client-side <c>Focus</c> operation.
        /// </summary>
        public virtual void Focus() { }
        /// <summary>
        /// Executes the client-side <c>Unfocus</c> operation.
        /// </summary>
        public virtual void Unfocus() { }
        /// <summary>
        /// Executes the client-side <c>Highlight</c> operation.
        /// </summary>
        public virtual void Highlight() { }
        /// <summary>
        /// Executes the client-side <c>ClearHighlight</c> operation.
        /// </summary>
        public virtual void ClearHighlight() { }
    }

    /// <summary>
    /// Provides the client-side <c>UI_ListEntry</c> implementation.
    /// </summary>
    public abstract class UI_ListEntry<T> : UI_ListEntry
    {
        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public abstract void Initialize(T data, int index);
    }
}