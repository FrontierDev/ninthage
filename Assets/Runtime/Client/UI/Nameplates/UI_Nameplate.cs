using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_Nameplate</c> implementation.
    /// </summary>
    public class UI_Nameplate : MonoBehaviour
    {
        private bool initialized = false;

        [SerializeField] private float heightOffset = 2f; // Height above actor's head
        [SerializeField] private float screenSize = 0.02f; // Fraction of screen height

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject barsContainer;
        [SerializeField] private UI_NameplateHealthBar healthBar;
        [SerializeField] private UI_NameplateCastBar castBar;

        private Actor actor;
        private RectTransform rectTransform;
        private Canvas canvas;
        private bool _isCasting;

        /// <summary>
        /// Gets the <c>SortingDistance</c> value.
        /// </summary>
        public float SortingDistance { get; set; }
        /// <summary>
        /// Gets the <c>Actor</c> value.
        /// </summary>
        public Actor Actor => actor;
        /// <summary>
        /// Gets the <c>RectTransform</c> value.
        /// </summary>
        public RectTransform RectTransform => rectTransform;
        /// <summary>
        /// Gets the <c>HeightOffset</c> value.
        /// </summary>
        public float HeightOffset => heightOffset;
        /// <summary>
        /// Gets the <c>StackOffset</c> value.
        /// </summary>
        public float StackOffset { get; set; }

        private void Awake()
        {
            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            if (!initialized)
            {
                var relation = actor.GetFactionRelation(playerActor.GetComponent<PlayerReputation>());
                healthBar.SetRelation(relation);
                OnStatsUpdated(actor, actor.GetComponent<ActorStatContainer>());
                initialized = true;
            }
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(Actor target, Canvas worldCanvas)
        {
            actor = target;
            canvas = worldCanvas;
            rectTransform = GetComponent<RectTransform>();

            // Display the actor's name
            if (nameText != null)
                nameText.text = actor.GetName();

            healthBar.SetName(actor.GetName());
            healthBar.SetLevel(actor.GetLevel());

            var playerActor = ClientAccountManager.PlayerActor;
            if (playerActor != null)
            {
                var relation = actor.GetFactionRelation(playerActor.GetComponent<PlayerReputation>());
                healthBar.SetRelation(relation);
                OnStatsUpdated(actor, actor.GetComponent<ActorStatContainer>());
                initialized = true;
            }

            EvaluateHealthBarVisibility();
            EvaluateNameVisibility();
            EvaluateCastBarVisibility();
        }

        private void Update()
        {
            if (actor == null)
            {
                Destroy(gameObject);
                return;
            }

            // Position nameplate above actor's head in world space
            Vector3 targetPos = actor.transform.position + Vector3.up * (heightOffset + StackOffset);
            transform.position = targetPos;

            // Face camera, but use camera's up so text stays screen-aligned
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.rotation = mainCam.transform.rotation;

                // Scale to maintain constant screen size regardless of distance
                float dist = Vector3.Distance(mainCam.transform.position, targetPos);
                float frustumHeight = 2f * dist * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float scale = frustumHeight * screenSize;
                transform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>
        /// Handles the <c>OnStatsUpdated</c> callback.
        /// </summary>
        public void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            if (this.actor != actor)
                return;

            if (statContainer.TryGetStat("health", out var healthStat))
            {
                float healthPercent = healthStat.CurrentValue / healthStat.EffectiveMaximum;
                healthBar.SetHealthPercent(healthPercent);
            }
        }

        /// <summary>
        /// Executes the client-side <c>EvaluateNameVisibility</c> operation.
        /// </summary>
        public void EvaluateNameVisibility()
        {
            EvaluateVisibility();
        }

        /// <summary>
        /// Executes the client-side <c>EvaluateHealthBarVisibility</c> operation.
        /// </summary>
        public void EvaluateHealthBarVisibility()
        {
            EvaluateVisibility();
        }

        /// <summary>
        /// Executes the client-side <c>EvaluateCastBarVisibility</c> operation.
        /// </summary>
        public void EvaluateCastBarVisibility()
        {
            EvaluateVisibility();
        }

        private void EvaluateVisibility()
        {
            var manager = UI_NameplateManager.Instance;
            bool isHovered = manager.hoveredActor == actor;
            bool isTargeted = ClientAccountManager.PlayerActor != null && ClientAccountManager.PlayerActor.Target == actor;

            // Determine health bar visibility
            bool showHealthBar = false;
            switch (manager.healthBarDisplayMode)
            {
                case NameplateHealthBarDisplayMode.Always:
                    showHealthBar = true;
                    break;
                case NameplateHealthBarDisplayMode.InCombat:
                    break;
                case NameplateHealthBarDisplayMode.OnHover:
                    showHealthBar = isHovered;
                    break;
                case NameplateHealthBarDisplayMode.Never:
                    break;
            }

            if (isTargeted && manager.healthBarDisplayMode != NameplateHealthBarDisplayMode.Never)
                showHealthBar = true;

            // Determine cast bar visibility
            bool showCastBar = false;
            if (_isCasting)
            {
                switch (manager.castBarDisplayMode)
                {
                    case NameplateCastBarDisplayMode.Always:
                        showCastBar = true;
                        break;
                    case NameplateCastBarDisplayMode.OnHover:
                        showCastBar = isHovered;
                        break;
                    case NameplateCastBarDisplayMode.Never:
                        break;
                }

                if (isTargeted && manager.castBarDisplayMode != NameplateCastBarDisplayMode.Never)
                    showCastBar = true;
            }

            // Cast bar requires the health bar to be visible so it sits directly beneath it
            if (showCastBar)
                showHealthBar = true;

            // Determine name visibility
            bool showName = false;
            switch (manager.nameDisplayMode)
            {
                case NameplateNameDisplayMode.Always:
                    showName = true;
                    break;
                case NameplateNameDisplayMode.OnHover:
                    showName = isHovered;
                    break;
                case NameplateNameDisplayMode.Never:
                    break;
            }

            if (isTargeted && manager.nameDisplayMode != NameplateNameDisplayMode.Never)
                showName = true;

            // Bars container and name are mutually exclusive; bars take priority
            if (showHealthBar)
            {
                barsContainer.SetActive(true);
                nameText.gameObject.SetActive(false);
            }
            else
            {
                barsContainer.SetActive(false);
                nameText.gameObject.SetActive(showName);
            }

            if (showCastBar)
                castBar.Show();
            else
                castBar.Hide();
        }

        /// <summary>
        /// Sets the value managed by <c>SetTargetedIndicatorVisibility</c>.
        /// </summary>
        public void SetTargetedIndicatorVisibility(bool isVisible)
        {
            float amount = isVisible ? 1f : 0f;
            UIShaderProperties.SetFloat(healthBar.graphics, "_SelectedAmount", amount);
        }

        /// <summary>
        /// Executes the client-side <c>StartCast</c> operation.
        /// </summary>
        public void StartCast(float duration, bool reverse)
        {
            _isCasting = true;
            castBar.StartCast(duration, reverse);
            EvaluateCastBarVisibility();
        }

        /// <summary>
        /// Executes the client-side <c>InterruptCast</c> operation.
        /// </summary>
        public void InterruptCast()
        {
            _isCasting = false;
            castBar.InterruptCast();
            EvaluateCastBarVisibility();
        }

        /// <summary>
        /// Executes the client-side <c>FinishCast</c> operation.
        /// </summary>
        public void FinishCast()
        {
            _isCasting = false;
            castBar.FinishCast();
            EvaluateCastBarVisibility();
        }
    }
}
