using Game.Shared.Data;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Shared
{
    /// <summary>
    /// Owns the process-lifetime Addressables handles for required game startup content.
    /// </summary>
    public static class GameContentBootstrap
    {
        private const string GAME_CONFIGURATION_ADDRESS = "GameConfiguration";
        private const string NETWORK_MANAGER_ADDRESS = "NetworkManager";

        private static readonly List<AsyncOperationHandle> OWNED_HANDLES = new();

        private static GameObject _networkManagerPrefab;
        private static bool _initializationInProgress;
        private static bool _initialized;

        /// <summary>Gets whether all required local startup assets are loaded and published.</summary>
        public static bool IsInitialized => _initialized;

        /// <summary>Gets the loaded network-manager prefab.</summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when startup content has not been initialized.
        /// </exception>
        public static GameObject NetworkManagerPrefab
        {
            get
            {
                if (!_initialized || _networkManagerPrefab == null)
                    throw new InvalidOperationException(
                        "The NetworkManager prefab was accessed before GameContentBootstrap initialization.");

                return _networkManagerPrefab;
            }
        }

        /// <summary>
        /// Asynchronously loads and publishes all required local bootstrap content.
        /// </summary>
        /// <param name="onFailure">Called exactly once when loading or validation fails.</param>
        public static IEnumerator Initialize(Action<Exception> onFailure)
        {
            if (onFailure == null)
                throw new ArgumentNullException(nameof(onFailure));
            if (_initialized || _initializationInProgress)
                throw new InvalidOperationException("Game content is already initialized or initializing.");
            if (Addressables.ResourceLocators == null
                || !Addressables.ResourceLocators.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException(
                    "GameContentBootstrap requires Addressables initialization to complete first.");
            }

            _initializationInProgress = true;
            return InitializeContent(onFailure);
        }

        private static IEnumerator InitializeContent(Action<Exception> onFailure)
        {
            var stagedHandles = new List<AsyncOperationHandle>();
            GameConfiguration gameConfiguration = null;
            GameObject networkManagerPrefab = null;
            ActorStatDefinitionLibrary actorStatDefinitionLibrary = null;
            AuraDefinitionLibrary auraDefinitionLibrary = null;
            ClassDefinitionLibrary classDefinitionLibrary = null;
            DamageSchoolDefinitionLibrary damageSchoolDefinitionLibrary = null;
            FactionDefinitionLibrary factionDefinitionLibrary = null;
            ItemDefinitionLibrary itemDefinitionLibrary = null;
            QuestDefinitionLibrary questDefinitionLibrary = null;
            RaceDefinitionLibrary raceDefinitionLibrary = null;
            SpellDefinitionLibrary spellDefinitionLibrary = null;
            TalentDefinitionLibrary talentDefinitionLibrary = null;
            WorldLocationDefinitionLibrary worldLocationDefinitionLibrary = null;

            Exception failure = null;
            yield return LoadAsset<GameConfiguration>(
                GAME_CONFIGURATION_ADDRESS,
                stagedHandles,
                value => gameConfiguration = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<GameObject>(
                NETWORK_MANAGER_ADDRESS,
                stagedHandles,
                value => networkManagerPrefab = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<ActorStatDefinitionLibrary>(
                nameof(ActorStatDefinitionLibrary),
                stagedHandles,
                value => actorStatDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<AuraDefinitionLibrary>(
                nameof(AuraDefinitionLibrary),
                stagedHandles,
                value => auraDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<ClassDefinitionLibrary>(
                nameof(ClassDefinitionLibrary),
                stagedHandles,
                value => classDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<DamageSchoolDefinitionLibrary>(
                nameof(DamageSchoolDefinitionLibrary),
                stagedHandles,
                value => damageSchoolDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<FactionDefinitionLibrary>(
                nameof(FactionDefinitionLibrary),
                stagedHandles,
                value => factionDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<ItemDefinitionLibrary>(
                nameof(ItemDefinitionLibrary),
                stagedHandles,
                value => itemDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<QuestDefinitionLibrary>(
                nameof(QuestDefinitionLibrary),
                stagedHandles,
                value => questDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<RaceDefinitionLibrary>(
                nameof(RaceDefinitionLibrary),
                stagedHandles,
                value => raceDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<SpellDefinitionLibrary>(
                nameof(SpellDefinitionLibrary),
                stagedHandles,
                value => spellDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<TalentDefinitionLibrary>(
                nameof(TalentDefinitionLibrary),
                stagedHandles,
                value => talentDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            yield return LoadAsset<WorldLocationDefinitionLibrary>(
                nameof(WorldLocationDefinitionLibrary),
                stagedHandles,
                value => worldLocationDefinitionLibrary = value,
                exception => failure = exception);
            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            try
            {
                ValidateNetworkManagerPrefab(networkManagerPrefab);

                GameConfigurationManager.Initialize(gameConfiguration);
                ActorStatDefinitionLibrary.Initialize(actorStatDefinitionLibrary);
                AuraDefinitionLibrary.Initialize(auraDefinitionLibrary);
                ClassDefinitionLibrary.Initialize(classDefinitionLibrary);
                DamageSchoolDefinitionLibrary.Initialize(damageSchoolDefinitionLibrary);
                FactionDefinitionLibrary.Initialize(factionDefinitionLibrary);
                ItemDefinitionLibrary.Initialize(itemDefinitionLibrary);
                QuestDefinitionLibrary.Initialize(questDefinitionLibrary);
                RaceDefinitionLibrary.Initialize(raceDefinitionLibrary);
                SpellDefinitionLibrary.Initialize(spellDefinitionLibrary);
                TalentDefinitionLibrary.Initialize(talentDefinitionLibrary);
                WorldLocationDefinitionLibrary.Initialize(worldLocationDefinitionLibrary);

                OWNED_HANDLES.AddRange(stagedHandles);
                _networkManagerPrefab = networkManagerPrefab;
                _initialized = true;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (failure != null)
            {
                FailInitialization(stagedHandles, failure, onFailure);
                yield break;
            }

            _initializationInProgress = false;
        }

        private static void FailInitialization(
            List<AsyncOperationHandle> stagedHandles,
            Exception failure,
            Action<Exception> onFailure)
        {
            ResetPublishedContent();
            ReleaseHandles(stagedHandles);
            _initializationInProgress = false;
            onFailure(failure);
        }

        /// <summary>Releases all process-owned Addressables handles and clears published content.</summary>
        public static void Release()
        {
            if (_initializationInProgress)
                throw new InvalidOperationException("Game content cannot be released while initialization is running.");

            ResetPublishedContent();
            ReleaseHandles(OWNED_HANDLES);
        }

        private static IEnumerator LoadAsset<T>(
            string address,
            List<AsyncOperationHandle> stagedHandles,
            Action<T> onLoaded,
            Action<Exception> onFailure)
        {
            AsyncOperationHandle<T> handle = StartAssetLoad<T>(address, out Exception startFailure);
            if (startFailure != null)
            {
                onFailure(new InvalidOperationException(
                    $"Addressables asset '{address}' failed to load.",
                    startFailure));
                yield break;
            }

            while (!handle.IsDone)
                yield return null;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Exception operationException = handle.OperationException;
                if (handle.IsValid())
                    Addressables.Release(handle);

                onFailure(new InvalidOperationException(
                    $"Addressables asset '{address}' failed to load.",
                    operationException));
                yield break;
            }

            stagedHandles.Add(handle);
            onLoaded(handle.Result);
        }

        private static AsyncOperationHandle<T> StartAssetLoad<T>(string address, out Exception failure)
        {
            try
            {
                failure = null;
                return Addressables.LoadAssetAsync<T>(address);
            }
            catch (Exception exception)
            {
                failure = exception;
                return default;
            }
        }

        private static void ValidateNetworkManagerPrefab(GameObject networkManagerPrefab)
        {
            if (networkManagerPrefab.GetComponent<NetworkManager>() == null)
            {
                throw new InvalidOperationException(
                    $"Addressables asset '{NETWORK_MANAGER_ADDRESS}' does not contain a PurrNet NetworkManager.");
            }

            if (networkManagerPrefab.GetComponentInChildren<UDPTransport>(true) == null)
            {
                throw new InvalidOperationException(
                    $"Addressables asset '{NETWORK_MANAGER_ADDRESS}' does not contain a UDPTransport.");
            }
        }

        private static void ResetPublishedContent()
        {
            WorldLocationDefinitionLibrary.ResetInstance();
            TalentDefinitionLibrary.ResetInstance();
            SpellDefinitionLibrary.ResetInstance();
            RaceDefinitionLibrary.ResetInstance();
            QuestDefinitionLibrary.ResetInstance();
            ItemDefinitionLibrary.ResetInstance();
            FactionDefinitionLibrary.ResetInstance();
            DamageSchoolDefinitionLibrary.ResetInstance();
            ClassDefinitionLibrary.ResetInstance();
            AuraDefinitionLibrary.ResetInstance();
            ActorStatDefinitionLibrary.ResetInstance();
            GameConfigurationManager.Reset();
            _networkManagerPrefab = null;
            _initialized = false;
        }

        private static void ReleaseHandles(List<AsyncOperationHandle> handles)
        {
            for (int i = handles.Count - 1; i >= 0; i--)
            {
                if (handles[i].IsValid())
                    Addressables.Release(handles[i]);
            }

            handles.Clear();
        }
    }
}
