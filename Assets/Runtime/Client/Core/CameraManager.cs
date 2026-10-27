using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        private AsyncOperationHandle<IList<GameObject>> _cameraLoadHandle;
        private bool _cameraLoadHandleOwned;
        private bool _cameraLoadPending;
        private int _cameraLoadGeneration;
        private GameObject _playerCameraInstance;
        private bool _isDestroying;

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
            ClientWorldManager.Instance.onWorldPresentationEnding += EndWorldPresentation;
        }

        private void OnEnteredWorld()
        {
            if (!ClientWorldManager.Initialized || !ClientWorldManager.Instance.IsWorldPresentationActive)
            {
                Debug.Error("Cannot initialize the PlayerCamera outside an active world presentation.");
                return;
            }

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
            if (_isDestroying
                || !_worldEntered
                || !ClientWorldManager.Initialized
                || !ClientWorldManager.Instance.IsWorldPresentationActive
                || _pendingActor == null
                || _cameraInitialized
                || _cameraLoadPending
                || _cameraLoadHandleOwned)
                return;

            var actor = _pendingActor;
            var actorTransform = actor.transform;
            var generation = ClientWorldManager.Instance.WorldPresentationGeneration;
            var handle = Addressables.LoadAssetsAsync<GameObject>("PlayerCamera", null);
            _cameraLoadHandle = handle;
            _cameraLoadHandleOwned = true;
            _cameraLoadPending = true;
            _cameraLoadGeneration = generation;
            RecordCameraHandleCount();

            handle.Completed += loadHandle =>
            {
                if (!_cameraLoadHandleOwned
                    || !_cameraLoadPending
                    || !loadHandle.Equals(_cameraLoadHandle)
                    || _cameraLoadGeneration != generation)
                {
                    Debug.Error("[CameraManager] PlayerCamera completion did not match its owned request.");
                    return;
                }

                _cameraLoadPending = false;

                if (_isDestroying
                    || !ClientWorldManager.Initialized
                    || !ClientWorldManager.Instance.IsWorldPresentationActive
                    || ClientWorldManager.Instance.WorldPresentationGeneration != generation
                    || !_worldEntered)
                {
                    ReleaseCameraInstanceAndHandle();

                    bool canRetry = !_isDestroying
                        && ClientWorldManager.Initialized
                        && ClientWorldManager.Instance.IsWorldPresentationActive
                        && _worldEntered
                        && _pendingActor != null
                        && !_cameraInitialized
                        && !_cameraLoadPending
                        && !_cameraLoadHandleOwned
                        && (ClientWorldManager.Instance.WorldPresentationGeneration != generation
                            || !ReferenceEquals(_pendingActor, actor));
                    if (canRetry)
                        TryInitializeCamera();
                    return;
                }

                if (loadHandle.Status != AsyncOperationStatus.Succeeded
                    || loadHandle.Result == null
                    || loadHandle.Result.Count == 0
                    || loadHandle.Result[0] == null)
                {
                    Debug.Error("Failed to load a valid PlayerCamera prefab from Addressables.");
                    ReleaseCameraInstanceAndHandle();
                    return;
                }

                if (actor == null
                    || actorTransform == null
                    || !ReferenceEquals(_pendingActor, actor))
                {
                    ReleaseCameraInstanceAndHandle();

                    bool canRetry = !_isDestroying
                        && ClientWorldManager.Initialized
                        && ClientWorldManager.Instance.IsWorldPresentationActive
                        && _worldEntered
                        && _pendingActor != null
                        && !_cameraInitialized
                        && !_cameraLoadPending
                        && !_cameraLoadHandleOwned
                        && !ReferenceEquals(_pendingActor, actor);
                    if (canRetry)
                        TryInitializeCamera();
                    return;
                }

                var cameraPrefab = loadHandle.Result[0];
                var playerCamera = Instantiate(cameraPrefab, actorTransform);
                playerCamera.name = "PlayerCamera";

                var cinemachineCamera = playerCamera.GetComponentInChildren<CinemachineCamera>();
                if (cinemachineCamera == null)
                {
                    Destroy(playerCamera);
                    Debug.Error("PlayerCamera prefab is missing a CinemachineCamera component.");
                    ReleaseCameraInstanceAndHandle();
                    return;
                }

                cinemachineCamera.Prioritize();
                cinemachineCamera.Follow = actorTransform;
                _playerCameraInstance = playerCamera;
                CameraPivot = _playerCameraInstance.transform;
                _cameraInitialized = true;
                Debug.Log("Player camera instantiated and parented to player actor.");
            };
        }

        private void EndWorldPresentation()
        {
            if (!_worldEntered
                && _pendingActor == null
                && !_cameraInitialized
                && !_cameraLoadHandleOwned
                && _playerCameraInstance == null)
                return;

            _worldEntered = false;
            _pendingActor = null;
            CameraPivot = null;

            if (_playerCameraInstance != null)
            {
                Destroy(_playerCameraInstance);
                _playerCameraInstance = null;
            }

            if (_cameraLoadHandleOwned && !_cameraLoadPending)
                ReleaseCameraInstanceAndHandle();
            else
            {
                _cameraInitialized = false;
                RecordCameraHandleCount();
            }
        }

        private void ReleaseCameraInstanceAndHandle()
        {
            if (_cameraLoadPending)
                throw new InvalidOperationException("Cannot release a pending PlayerCamera load.");

            if (_playerCameraInstance != null)
            {
                if (CameraPivot == _playerCameraInstance.transform)
                    CameraPivot = null;
                Destroy(_playerCameraInstance);
                _playerCameraInstance = null;
            }

            if (_cameraLoadHandleOwned)
            {
                if (_cameraLoadHandle.IsValid())
                {
                    Addressables.Release(_cameraLoadHandle);
                    if (PerformanceTelemetry.IsEnabled)
                        PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                }

                _cameraLoadHandle = default;
                _cameraLoadHandleOwned = false;
            }

            _cameraInitialized = false;
            RecordCameraHandleCount();
        }

        private void RecordCameraHandleCount()
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    ClientPerformanceMetrics.CAMERA_LIVE_ADDRESSABLE_HANDLE_COUNT,
                    _cameraLoadHandleOwned ? 1 : 0);
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            _isDestroying = true;
            Game.Shared.Networking.AccountEndpoint.onEnteredWorld -= OnEnteredWorld;
            Game.Shared.Networking.ActorEndpoint.onActorOwnershipTaken -= OnActorOwnershipTaken;
            if (ClientWorldManager.Initialized)
                ClientWorldManager.Instance.onWorldPresentationEnding -= EndWorldPresentation;

            _worldEntered = false;
            _pendingActor = null;
            CameraPivot = null;

            if (_playerCameraInstance != null)
            {
                Destroy(_playerCameraInstance);
                _playerCameraInstance = null;
            }

            if (_cameraLoadHandleOwned && !_cameraLoadPending)
                ReleaseCameraInstanceAndHandle();
            else
            {
                _cameraInitialized = false;
                RecordCameraHandleCount();
            }

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
