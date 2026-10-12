using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Data.ClassBehaviours;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Linq;

namespace Game.Client.UI
{
    /// <summary>
    /// Defines the client-side <c>ISpecialResourceBar</c> contract.
    /// </summary>
    public interface ISpecialResourceBar
    {
        /// <summary>
        /// Initializes the special-resource bar for the specified player actor.
        /// </summary>
        void Initialize(Actor playerActor);
    }

    /// <summary>
    /// Provides the client-side <c>UI_SpecialResourcePanel</c> implementation.
    /// </summary>
    public sealed class UI_SpecialResourcePanel : UI_Panel
    {
        private static UI_SpecialResourcePanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_SpecialResourcePanel Instance => _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        /// <summary>
        /// Handles the <c>OnPlayerActorAssigned</c> callback.
        /// </summary>
        public void OnPlayerActorAssigned(Actor playerActor)
        {
            if (Game.Shared.Runtime.Runtime.IsServer()) return; // This UI should not be active on the server.

            var classId = ClientAccountManager.ActiveCharacter.ClassID;
            var def = ClassDefinitionLibrary.Instance.GetDefinition(classId);
            var trackedStat = def.BaseStats.FirstOrDefault(x => x.definition.Tags.Any(tag => tag == "special"))?.definition;
            var resourcePrefab = def.ClassBehaviour != null ? def.ClassBehaviour.ResourcePrefab : null;

            if (trackedStat != null && resourcePrefab != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;

                // Instantiate the resource bar prefab as a child of this panel.
                var resourceBarObj = Instantiate(resourcePrefab, transform);
                resourceBarObj.transform.SetSiblingIndex(0); // Ensure it is behind the other UI
                resourceBarObj.GetComponent<ISpecialResourceBar>()?.Initialize(playerActor);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }
    }
}
