using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_Button</c> implementation.
    /// </summary>
    public abstract class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Button button;
        [SerializeField] protected string sfxAddress;

        /// <summary>
        /// Executes the client-side <c>Start</c> operation.
        /// </summary>
        public virtual void Start()
        {
            if (Game.Shared.Runtime.Runtime.IsServer()) return;
            if (button != null) button.onClick.AddListener(() => OnClick());
        }

        /// <summary>
        /// Handles the <c>OnClick</c> callback.
        /// </summary>
        public virtual void OnClick(PointerEventData eventData = default)
        {
            if (!string.IsNullOrEmpty(sfxAddress))
            {
                ClientAudioManager.Instance.PlaySFX(sfxAddress);
            }
        }

        // Optional: Override these methods in derived classes to add hover effects or other interactions.  
        /// <summary>
        /// Handles the <c>OnPointerEnter</c> callback.
        /// </summary>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null) button.OnPointerEnter(eventData);
        }

        /// <summary>
        /// Handles the <c>OnPointerExit</c> callback.
        /// </summary>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (button != null) button.OnPointerExit(eventData);
        }

        /// <summary>
        /// Executes the client-side <c>EnableButton</c> operation.
        /// </summary>
        public virtual void EnableButton()
        {
            if (button != null) button.enabled = true;
        }

        /// <summary>
        /// Executes the client-side <c>DisableButton</c> operation.
        /// </summary>
        public virtual void DisableButton()
        {
            if (button != null) button.enabled = false;
        }

        /// <summary>
        /// Executes the client-side <c>IsVisible</c> operation.
        /// </summary>
        public virtual bool IsVisible()
        {
            return canvasGroup.alpha > 0f;
        }

        /// <summary>
        /// Executes the client-side <c>Show</c> operation.
        /// </summary>
        public virtual void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Executes the client-side <c>Hide</c> operation.
        /// </summary>
        public virtual void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
