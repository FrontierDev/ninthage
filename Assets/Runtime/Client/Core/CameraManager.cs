using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Runtime.Actors;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>CameraManager</c> implementation.
    /// </summary>
    public sealed class CameraManager : MonoBehaviour
    {
        private static CameraManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static CameraManager Instance => _instance;

        /// <summary>
        /// The root transform of the instantiated PlayerCamera prefab.
        /// Exposed so other systems (e.g. Test_CharacterController) can rotate it
        /// without relying on a fragile Transform.Find call.
        /// </summary>
        public static Transform CameraPivot { get; private set; }

        private bool _worldEntered;
        private bool _cameraInitialized;
        private Actor _pendingActor;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("CameraManager is already initialized!");
                return;
            }

            _instance = this;

            Game.Shared.Networking.AccountEndpoint.onEnteredWorld += OnEnteredWorld;
            Game.Shared.Networking.ActorEndpoint.onActorOwnershipTaken += OnActorOwnershipTaken;
        }

        private void OnEnteredWorld()
        {
            _worldEntered = true;
            TryInitializeCamera();
        }

        private void OnActorOwnershipTaken(Actor actor)
        {
            _pendingActor = actor;
            TryInitializeCamera();
        }

        private void TryInitializeCamera()
        {
            if (_cameraInitialized || !_worldEntered) return;
            if (_pendingActor == null) return;

            _cameraInitialized = true;
            var actorObject = _pendingActor.gameObject;
            Addressables.LoadAssetsAsync<GameObject>("PlayerCamera", null).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    var cameraPrefab = handle.Result[0];
                    var playerCamera = Instantiate(cameraPrefab, actorObject.transform);
                    playerCamera.name = "PlayerCamera";

                    var cinemachineCamera = playerCamera.GetComponentInChildren<CinemachineCamera>();
                    cinemachineCamera.Prioritize();
                    cinemachineCamera.Follow = actorObject.transform;

                    CameraPivot = playerCamera.transform;
                    Debug.Log("Player camera instantiated and parented to player actor.");
                }
                else
                {
                    Debug.Error("Failed to load PlayerCamera prefab from Addressables.");
                }
            };
        }
    }
}
