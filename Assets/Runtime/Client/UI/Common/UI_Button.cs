using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public abstract class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Button button;
        [SerializeField] protected string sfxAddress;

        public virtual void Start()
        {
            if (Game.Shared.Runtime.IsServer()) return;
            if (button != null) button.onClick.AddListener(() => OnClick());
        }

        public virtual void OnClick(PointerEventData eventData = default)
        {
            if (!string.IsNullOrEmpty(sfxAddress))
            {
                ClientAudioManager.Instance.PlaySFX(sfxAddress);
            }
        }

        // Optional: Override these methods in derived classes to add hover effects or other interactions.  
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null) button.OnPointerEnter(eventData);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (button != null) button.OnPointerExit(eventData);
        }

        public virtual void EnableButton()
        {
            if (button != null) button.enabled = true;
        }

        public virtual void DisableButton()
        {
            if (button != null) button.enabled = false;
        }

        public virtual bool IsVisible()
        {
            return canvasGroup.alpha > 0f;
        }

        public virtual void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public virtual void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}