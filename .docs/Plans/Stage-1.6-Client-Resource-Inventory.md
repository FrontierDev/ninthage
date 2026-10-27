# Stage 1.6 Client Resource Inventory

This inventory is the Stage 1.6 resource-lifetime census for the current `dev`
client implementation. It records ownership and observable boundaries before
the owner-specific probes are implemented. This document does not claim that
the listed missing release paths or future probes have been fixed.

## Invariant and scope

Every entry below has one stated owner. Project-owned Addressables handles are
distinct from PurrNet-owned streamed-scene handles. The three new live-handle
metrics each describe one owner only:

- `client.audio.live_addressable_handle_count` — `ClientAudioManager` handles;
- `client.camera.live_addressable_handle_count` — `CameraManager` handles;
- `client.vfx.live_addressable_handle_count` — `ClientVFXManager` handles.

The world cleanup metric belongs to the project-owned client world-presentation
boundary. It does not measure a PurrNet request duration.

## Ownership table

| Acquisition call | Concrete owner | Resource/handle kind | Lifetime class | Current release site | Success/failure/late-completion boundary | Planned fixing issue | PurrNet/project ownership |
|---|---|---|---|---|---|---|---|
| `ClientInitialization.InitializeAddressables()` → `Addressables.InitializeAsync()` | `ClientInitialization` | Addressables initialization `AsyncOperationHandle<IResourceLocator>` | Process/bootstrap | Addressables' default initialization semantics: this overload uses automatic handle release on completion; **no project `Addressables.Release` call** | `ClientInitialization` waits for `IsDone` and continues without checking `Status`. The default handle is not retained by the project. | This inventory/declaration issue; preserve the Addressables initialization-handle contract and audit it separately from asset ownership. | Addressables-owned initialization operation; project-owned bootstrap caller |
| `ClientInitialization.UnloadInitializationScene()` → `SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive)` | `ClientInitialization` / main-menu UI scope | Unity `Scene` plus `AsyncOperation` | Connection/UI additive scene | **missing** for the loaded `MainMenu` scene in the inspected client paths; `Bootstrapper` is unloaded separately by the same method | Load completion is awaited before `Bootstrapper` unload. There is no explicit load failure or abandoned-completion settlement in this method. | Connection/UI scene-lifecycle follow-up; not a world-presentation cleanup claim. | Project-owned Unity scene; not an Addressables handle and not PurrNet-owned |
| `ClientInitialization.UnloadInitializationScene()` → `SceneManager.UnloadSceneAsync("Bootstrapper")` | `ClientInitialization` | Unity `Scene` plus `AsyncOperation` | Process/bootstrap | The same `UnloadInitializationScene()` call is the current release/unload site | The unload operation is awaited after the `MainMenu` load; no explicit failure result is checked. | This inventory/declaration issue; no runtime change. | Project-owned Unity scene |
| `ClientWorldManager.OnEnteringWorld()` → `SceneManager.LoadSceneAsync("global_world", LoadSceneMode.Additive)` | `ClientWorldManager` | Project Unity `Scene` plus `AsyncOperation` | World presentation session | **missing**; no project-owned `UnloadSceneAsync("global_world")` path is present in the inspected client code, including `ClientConnectionManager.OnClientConnectionStateChanged()` | The load is awaited and then made active. `global_world` is not an Addressables acquisition. Entry failure, abandoned coroutine and late completion have no explicit generation boundary in the current implementation. | Stage 1.6 Step 2 — client world-presentation cleanup | Project-owned Unity scene; not a PurrNet-delivered scene |
| `ClientConnectionManager.ConnectionProcess()` → `SceneManager.LoadSceneAsync("CharacterSelection", LoadSceneMode.Additive)` | `ClientConnectionManager` with character-selection UI scope | Unity `Scene` plus `AsyncOperation` | Connection/UI additive scene | `CharacterSelectionManager.OnEnteringWorld()`, `UI_CreateCharacterButton.OnClick()` | The load is started after character-list receipt and is not awaited. The current release calls are also not awaited; no world-cleanup ownership is implied. | Connection/UI scene-lifecycle follow-up; keep separate from world presentation. | Project-owned Unity scene |
| `ClientConnectionManager.ConnectionProcess()` → `SceneManager.LoadSceneAsync("CharacterCreation", LoadSceneMode.Additive)` | `ClientConnectionManager` with character-creation UI scope | Unity `Scene` plus `AsyncOperation` | Connection/UI additive scene | `CharacterCreationManager.OnCharacterCreated()`, `UI_CreateCharacterButton.OnClick()` transitions from selection | The load is started after an empty character list and is not awaited. The current release/transition calls are not awaited; no world-cleanup ownership is implied. | Connection/UI scene-lifecycle follow-up; keep separate from world presentation. | Project-owned Unity scene |
| `CharacterCreationManager.OnCharacterCreated()` → `SceneManager.LoadSceneAsync("CharacterSelection", LoadSceneMode.Additive)` | `CharacterCreationManager` / character-selection UI scope | Unity `Scene` plus `AsyncOperation` | Connection/UI additive scene | `CharacterSelectionManager.OnEnteringWorld()` or `UI_CreateCharacterButton.OnClick()` | Started after character creation; not awaited. It is a UI transition and does not establish ownership of `global_world` or streamed scenes. | Connection/UI scene-lifecycle follow-up; keep separate from world presentation. | Project-owned Unity scene |
| `PurrNet.Modules.ScenesModule` delivers a `World_*` scene through its client scene action; observed by `ClientWorldManager.OnSceneLoaded(...)` and `OnSceneUnloaded(...)` | PurrNet client `ScenesModule`; `ClientWorldManager` owns only logical presentation tracking | PurrNet `SceneID`, `SceneState`, Addressables `AsyncOperationHandle<SceneInstance>` internally, and Unity `Scene` | PurrNet streamed scene | PurrNet scene-module unload/cleanup path (`TryRemoveAddressableScene` / supported module lifecycle); **no project `Addressables.Release`** | PurrNet owns the load completion, scene registration, failure settlement and handle release/unload. Project callbacks observe a loaded/unloaded scene; an observed callback is not a true PurrNet request duration. | Stage 1.6 Step 2 — integrate world cleanup through the supported PurrNet lifecycle | **PurrNet-owned handle**; project-owned callback observation and `_loadedScenes` presentation record |
| `ClientAudioManager.PlaySFX(string)` → `Addressables.LoadAssetAsync<AudioClip>(addressablePath)` | `ClientAudioManager` | `AsyncOperationHandle<AudioClip>`; successful result is placed in the audio cache and played on `_sfxSource` | Cached asset; transient playback depends on the cached clip | **missing** for a successful cached handle when `EvictCacheIfNeeded()` removes the cache entry; `OnDestroy()` releases pending handles only | Handle is added to `_pendingLoads` immediately. Completion removes it from pending, then success stores only `Result`; failure logs and returns without a project release. There is no owner/session generation check before late publication. | Stage 1.6 Step 3 — audio Addressables lifetime | Project-owned Addressables asset handle |
| `ClientAudioManager.PreloadSFX(string)` → `Addressables.LoadAssetAsync<AudioClip>(addressablePath)` | `ClientAudioManager` | `AsyncOperationHandle<AudioClip>` retained conceptually by the bounded cache but not currently stored with the `AudioClip` | Cached asset | **missing** for a successful cached handle on LRU eviction; pending handles are released in `OnDestroy()` | Same pending/completion boundary as `PlaySFX`; success stores only the clip, failure logs and returns, and a late completion can still populate the cache. | Stage 1.6 Step 3 — audio Addressables lifetime | Project-owned Addressables asset handle |
| `ClientAudioManager.PlaySFXOnAudioSource(AudioSource, string)` → `Addressables.LoadAssetAsync<AudioClip>(addressablePath)` | `ClientAudioManager` | `AsyncOperationHandle<AudioClip>`; direct-source request currently also enters the shared audio cache | Cached asset after success; transient-source request boundary before cache publication | `OnDestroy()` releases pending handles; if the source is destroyed before completion, the completion callback calls `Addressables.Release(loadHandle)`; **missing** for successful cache insertion followed by eviction | Handle is added to `_pendingLoads` immediately. Completion removes it from pending, then failure logs; a null source releases the completed handle, otherwise success stores only the clip and plays it. No late owner/session boundary exists. | Stage 1.6 Step 3 — audio Addressables lifetime | Project-owned Addressables asset handle |
| `CameraManager.TryInitializeCamera()` → `Addressables.LoadAssetsAsync<GameObject>("PlayerCamera", null)` | `CameraManager` | Collection `AsyncOperationHandle<IList<GameObject>>` and instantiated camera `GameObject` | World presentation session | **missing**; the inspected `CameraManager` has no `OnDestroy`, handle release, or explicit camera destruction path | Completion success instantiates the first result and parents it to the actor; failure only logs. `_cameraInitialized` prevents a second request, but there is no session generation or late-completion check. | Stage 1.6 Step 3 — camera Addressables/world lifetime | Project-owned Addressables collection handle and camera instance |
| `ClientVFXManager.OnClientSpawnVFXAtPosition(...)` → `Addressables.LoadAssetAsync<GameObject>(addressablePath)` | `ClientVFXManager` | `AsyncOperationHandle<GameObject>` plus instantiated VFX `GameObject` | Active transient VFX | **missing**; `TrackVFX` records a spawn but does not own a release path, `OnDestroy()` does not release handles, and the loaded handle is not released after instance creation or effect completion | Pending count is changed only while telemetry is enabled. Completion decrements the local pending counter for the matching telemetry session, then success can instantiate/publish; failure logs. There is no owner/session generation boundary for the resource or late completion. | Stage 1.6 Step 3 — VFX Addressables/active lifetime | Project-owned Addressables asset handle and VFX instance |
| `ClientVFXManager.OnClientSpawnProjectile(...)` → `Addressables.LoadAssetAsync<GameObject>(addressablePath)` | `ClientVFXManager` | `AsyncOperationHandle<GameObject>` plus instantiated projectile `GameObject` / `SpellProjectile` | Active transient projectile | **missing**; projectile destruction is not connected to release of the loaded handle | Pending/completion behavior matches the VFX path. Missing target or component logs and destroys the instance where applicable, but does not release the successful load handle. | Stage 1.6 Step 3 — VFX Addressables/active lifetime | Project-owned Addressables asset handle and projectile instance |
| `UI_FCTManager.OnExperienceUpdated()` and `OnCombatLogEntryReceived()` → `Instantiate(_fctPrefab, ...)` | `UI_FCTManager` / `UI_FloatingCombatText` | Project serialized prefab instance; no Addressables handle | Active transient UI | `UI_FloatingCombatText.Update()` calls `Destroy(gameObject)` at completion; `UI_FloatingCombatText.OnDestroy()` raises `Released` to the manager | `UI_FCTManager` tracks the instance for telemetry only. Completion is a Unity destruction boundary; there is no pool or reset contract yet. | Stage 1.6 Step 4 — bounded FCT pooling (separate follow-up) | Project-owned presentation object |
| `UI_NameplateManager.OnActorSpawned()` → `Instantiate(nameplatePrefab, ...)` | `UI_NameplateManager` keyed by `Actor` | Project serialized prefab instance; no Addressables handle | Active actor presentation | `UI_NameplateManager.RemoveNameplateForActor()` calls `Destroy(nameplate.gameObject)`; stale actor/nameplate cleanup also routes there | Actor spawn creates one presentation entry; actor despawn, stale detection or nameplate destruction removes it. The `Actor` key is not pooled and the current object has no reusable reset contract. | Stage 1.6 Step 4 — measured bounded nameplate pooling (separate follow-up) | Project-owned presentation object; actor identity remains separately owned |
| PurrNet-delivered actor `GameObject` / `NetworkIdentity` associated with `global_actors` and streamed scenes | PurrNet actor/network lifecycle; client managers consume the `Actor` identity | Networked actor object and `NetworkIdentity`; not an Addressables asset acquisition by the client code | Network actor representation | PurrNet despawn/destroy lifecycle; client UI/VFX/audio consumers must not release the actor identity | Actor publication/despawn callbacks define the observable boundary. `UI_NameplateManager` and other presentation systems may destroy their own views, never the network actor. | No owner-fixing issue in this declaration ticket; network actor pooling is a Stage 1.6 non-goal | **PurrNet-owned actor identity**; project-owned presentation references only |

The client bootstrap also loads `Resources/NetworkManager` and instantiates a
persistent project-side network-manager `GameObject` in
`ClientInitialization.ClientInitializeNetworking()`. Its lifetime is process
bootstrap and is not an Addressables acquisition; the inspected code does not
destroy it on disconnect. It is recorded here to keep the non-Addressables
bootstrap boundary distinct from PurrNet-delivered world-scene handles.

Other feature UI panels instantiate serialized, non-Addressables prefabs for
their local list/row content. Those instances remain owned and destroyed by
their feature UI managers or parent scene cleanup; they are outside the three
owner-specific Addressables gauges and the Stage 1.6 handle census above.

## Metric contract

The following declarations are intentionally registered now, before their
owner-specific probes exist:

| Metric ID | Owner | Unit/subsystem/scope | Sampling contract in later owner issue |
|---|---|---|---|
| `client.audio.live_addressable_handle_count` | `ClientAudioManager` only | `Count` / `addressables` / `client_process` | Count each audio handle once from acquisition through release, including pending operations; report transitions and a zero after completed cleanup. |
| `client.camera.live_addressable_handle_count` | `CameraManager` only | `Count` / `addressables` / `client_process` | Count each camera collection/asset handle once from acquisition through release; do not aggregate another manager. |
| `client.vfx.live_addressable_handle_count` | `ClientVFXManager` only | `Count` / `addressables` / `client_process` | Count each VFX/projectile handle once from acquisition through terminal release; do not use spawn count as a substitute. |
| `client.world.presentation_cleanup_duration` | `ClientWorldManager` project-owned world-presentation cleanup boundary | `Milliseconds` / `scene` / `scene_operation` | Measure the project world-presentation cleanup operation when that later boundary exists. A PurrNet callback observation is not a PurrNet request duration. |

For both the pending-handle contract and the three live-handle gauges, an
acquired pending operation remains included until its handle is released. A
handle is counted exactly once even if the same asset key is requested more
than once. The later owner-specific implementation reports only on real
ownership state transitions and reports zero after completed cleanup. A
telemetry session reset does not reset the resource owner or silently create a
zero sample; the subsequent transition must reflect the actual live count.

`CLIENT_ADDRESSABLE_PENDING_COUNT` and
`CLIENT_ADDRESSABLE_RELEASE_COUNT` retain their existing IDs and registration
for historical report compatibility. Their current writers are not additive or
global: audio and VFX independently write the pending metric, and release
writes are emitted from individual paths. This declarations-only issue does not
repair or redefine those semantics.

No runtime owner, release call, aggregator, or telemetry sample is added by
this inventory and metric-declaration change.

