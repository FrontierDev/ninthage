using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ActionBarButton</c> implementation.
    /// </summary>
    public sealed class UI_ActionBarButton : UI_Button, IPointerClickHandler
    {
        [Header("UI References - Action Bar Button")]
        [FormerlySerializedAs("keybindText")]
        [SerializeField] private TMP_Text _keybindText;
        [FormerlySerializedAs("spellIconImage")]
        [SerializeField] private Image _spellIconImage;
        [FormerlySerializedAs("spellID")]
        [SerializeField] private string _spellID;
        [FormerlySerializedAs("spellRank")]
        [SerializeField] private int _spellRank;
        [FormerlySerializedAs("contextMenuEntryPrefab")]
        [SerializeField] private GameObject _contextMenuEntryPrefab;

        [Header("Keybindings")]
        [FormerlySerializedAs("key")]
        [SerializeField] private Key _key;
        private Material _materialInstance;
        private float _pressedAmount;

        private void Awake()
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (_spellIconImage != null && _spellIconImage.material != null)
            {
                _materialInstance = new Material(_spellIconImage.material);
                _spellIconImage.material = _materialInstance;
            }

            RefreshAssignmentPresentation();
            ActorEndpoint.onStartGlobalCooldown += OnStartGlobalCooldown;
        }

        /// <summary>
        /// Sets the value managed by <c>SetKeybindText</c>.
        /// </summary>
        public void SetKeybindText(string text)
        {
            if (_keybindText != null)
                _keybindText.text = text;
        }

        void OnDestroy()
        {
            StopCoroutine(FadeOutPressedState());
            ActorEndpoint.onStartGlobalCooldown -= OnStartGlobalCooldown;

            if (_materialInstance != null)
                Destroy(_materialInstance);
        }

        private IEnumerator FadeOutPressedState()
        {
            while (_pressedAmount > 0f)
            {
                _pressedAmount = Mathf.Max(_pressedAmount - Time.deltaTime * 5f, 0f);
                UIShaderProperties.SetPressedAmount(_spellIconImage, _pressedAmount);
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

            UIShaderProperties.SetDisabled(_spellIconImage, false);
        }

        private void OnStartGlobalCooldown()
        {
            if (_spellIconImage != null)
                UIShaderProperties.SetDisabled(_spellIconImage, true);
            else return;

            StartCoroutine(GlobalCooldownRoutine(GameConfigurationManager.Config.GlobalCooldownDuration));
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = default)
        {
            if (eventData == null) return;

            if (!string.IsNullOrEmpty(_spellID))
            {
                _pressedAmount = 1f;
                UIShaderProperties.SetPressedAmount(_spellIconImage, _pressedAmount);
                StartCoroutine(FadeOutPressedState());
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!string.IsNullOrEmpty(_spellID) && ClientCombatManager.Initialized)
                    ClientCombatManager.Instance.CastSpell(_spellID, _spellRank);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OpenSpellContextMenu(eventData.position);
            }

            base.OnClick(eventData);
        }

        /// <summary>
        /// Handles the <c>OnPointerClick</c> callback.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick(eventData);
        }

        /// <summary>
        /// Sets the value managed by <c>SetSpell</c>.
        /// </summary>
        public void SetSpell(string newSpellID, int newSpellRank, Sprite newSpellIcon)
        {
            _spellID = newSpellID;
            _spellRank = newSpellRank;

            UIShaderProperties.SetIcon(_spellIconImage, newSpellIcon != null ? newSpellIcon.texture : null);
            UIShaderProperties.SetIconVisible(_spellIconImage, newSpellIcon != null);
            RefreshAssignmentPresentation();
        }

        private void RefreshAssignmentPresentation()
        {
            if (_spellIconImage == null) return;

            _spellIconImage.gameObject.SetActive(!string.IsNullOrEmpty(_spellID));
        }

        private void OpenSpellContextMenu(Vector2 screenPosition)
        {
            var spellcaster = ClientAccountManager.PlayerActor.GetComponent<ActorSpellcaster>();
            var spellLibrary = SpellDefinitionLibrary.Instance;

            var buttonRect = GetComponent<RectTransform>();
            Vector3 topLeft = buttonRect.TransformPoint(buttonRect.rect.xMin, buttonRect.rect.yMax, 0f);

            UI_ContextMenuWindow.Instance.Show(topLeft, ContextMenuPivot.BottomLeft, _contextMenuEntryPrefab, panel =>
            {
                foreach (var knownSpellID in spellcaster.KnownSpellIDs)
                {
                    var spellDef = spellLibrary.GetDefinition(knownSpellID);
                    if (spellDef == null) continue;

                    var entryObj = Object.Instantiate(_contextMenuEntryPrefab, panel.ContentRoot);
                    var entry = entryObj.GetComponent<UI_ContextMenuEntry>();
                    entry.Initialize(spellDef.DisplayName, spellDef.Icon, () =>
                    {
                        SetSpell(spellDef.DefinitionID, 1, spellDef.Icon);
                        UI_ContextMenuWindow.Instance.Hide();
                    });
                    entry.Register(panel);
                }
            });
        }
    }
}
