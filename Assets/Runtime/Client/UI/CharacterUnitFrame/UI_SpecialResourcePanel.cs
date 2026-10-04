using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;

namespace Game.Client.UI
{
    public interface ISpecialResourceBar
    {
        void Initialize(Actor playerActor);
    }

    public sealed class UI_SpecialResourcePanel : UI_Panel
    {
        private static UI_SpecialResourcePanel _instance;
        public static UI_SpecialResourcePanel Instance => _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        public void OnPlayerActorAssigned(Actor playerActor)
        {
            if (Game.Shared.Runtime.IsServer()) return; // This UI should not be active on the server.

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