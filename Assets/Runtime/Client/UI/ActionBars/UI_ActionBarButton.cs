using System.Collections;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public sealed class UI_ActionBarButton : UI_Button, IPointerClickHandler
    {
        [Header("UI References - Action Bar Button")]
        [SerializeField] private TMP_Text keybindText;
        [SerializeField] private Image spellIconImage;
        [SerializeField] private string spellID;
        [SerializeField] private int spellRank;
        [SerializeField] private GameObject contextMenuEntryPrefab;

        [Header("Keybindings")]
        [SerializeField] private Key key;

        private void Awake()
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (spellIconImage != null && spellIconImage.material != null)
                spellIconImage.material = new Material(spellIconImage.material);

            ActorService.onStartGlobalCooldown += OnStartGlobalCooldown;
        }

        public void SetKeybindText(string text)
        {
            if (keybindText != null)
                keybindText.text = text;
        }

        void OnDestroy()
        {
            StopCoroutine(FadeOutPressedState());
        }

        private IEnumerator FadeOutPressedState()
        {
            if (spellIconImage == null || spellIconImage.material == null) yield break;

            float pressed = spellIconImage.material.GetFloat("_PressedAmount");
            while (pressed > 0f)
            {
                pressed -= Time.deltaTime * 5f; // Fade out over 0.2 seconds
                spellIconImage.material.SetFloat("_PressedAmount", Mathf.Max(pressed, 0f));
                yield return null;
            }
        }

        private IEnumerator GlobalCooldownRoutine(float gcdDuration)
        {
            float elapsed = 0f;
            while (elapsed < gcdDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            UIShaderProperties.SetFloat(spellIconImage, "_DisabledAmount", 0f);
        }

        private void OnStartGlobalCooldown()
        {
            if (spellIconImage != null)
                UIShaderProperties.SetFloat(spellIconImage, "_DisabledAmount", 1f);
            else return;

            StartCoroutine(GlobalCooldownRoutine(GameConfigurationManager.Config.GlobalCooldownDuration));
        }

        public override void OnClick(PointerEventData eventData = default)
        {
            if (eventData == null) return;

            UIShaderProperties.SetFloat(spellIconImage, "_PressedAmount", 1f);
            StartCoroutine(FadeOutPressedState());

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!string.IsNullOrEmpty(spellID) && ClientCombatManager.Initialized)
                    ClientCombatManager.Instance.CastSpell(spellID, spellRank);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OpenSpellContextMenu(eventData.position);
            }

            base.OnClick(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick(eventData);
        }

        public void SetSpell(string newSpellID, int newSpellRank, Sprite newSpellIcon)
        {
            spellID = newSpellID;
            spellRank = newSpellRank;

            UIShaderProperties.SetTexture(spellIconImage, "_IconTex", newSpellIcon != null ? newSpellIcon.texture : Texture2D.whiteTexture);
            if (newSpellIcon != null)
                UIShaderProperties.SetFloat(spellIconImage, "_IconAmount", 1f);
            else
                UIShaderProperties.SetFloat(spellIconImage, "_IconAmount", 0f);

        }

        private void OpenSpellContextMenu(Vector2 screenPosition)
        {
            var spellcaster = ClientAccountManager.PlayerActor.GetComponent<ActorSpellcaster>();
            var spellLibrary = SpellDefinitionLibrary.Instance;

            var buttonRect = GetComponent<RectTransform>();
            Vector3 topLeft = buttonRect.TransformPoint(buttonRect.rect.xMin, buttonRect.rect.yMax, 0f);

            UI_ContextMenuWindow.Instance.Show(topLeft, ContextMenuPivot.BottomLeft, contextMenuEntryPrefab, panel =>
            {
                foreach (var knownSpellID in spellcaster.KnownSpellIDs)
                {
                    var spellDef = spellLibrary.GetDefinition(knownSpellID);
                    if (spellDef == null) continue;

                    var entryObj = Object.Instantiate(contextMenuEntryPrefab, panel.ContentRoot);
                    var entry = entryObj.GetComponent<UI_ContextMenuEntry>();
                    entry.Initialize(spellDef.DisplayName, spellDef.Icon, () =>
                    {
                        SetSpell(spellDef.DefinitionId, 1, spellDef.Icon);
                        UI_ContextMenuWindow.Instance.Hide();
                    });
                    entry.Register(panel);
                }
            });
        }
    }
}