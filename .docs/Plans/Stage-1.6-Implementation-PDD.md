# Ninth Age — Stage 1.6 Implementation PDD

**Stage:** Phase 1, Stage 1.6 — Client rendering, world streaming and resource lifetime foundations  
**Status:** Implementation contract prepared; implementation pending  
**Branch:** `dev`  
**Reviewed source baseline:** `7afed7bd70be84a170a2e6694874c793a016b3c3`  
**Updated:** 2026-10-27

## 1. Purpose and authority

Stage 1.6 establishes the client-side presentation and resource-lifetime foundation that later world, art, combat and content work must use.

This document translates the Stage 1.6 requirements in the Phase 1 Implementation Plan into an implementation contract for the current `dev` codebase. It is not a replacement world-streaming architecture and does not redefine product behaviour owned by the authoritative PDDs.

Read together with:

- `.docs/Plans/Phase-1-Implementation-Plan.md`;
- `.docs/pdd/World-Runtime-and-Instancing-PDD.md`;
- `.docs/pdd/Graphical-Approach-PDD.md`;
- `.docs/Performance-and-Simulation-Architecture.md`;
- `.docs/Plans/Phase-1-Performance-Stress-Profiles.md`;
- `.docs/Plans/Phase-1-Performance-Budgets.md`;
- `.docs/Plans/Phase-1-Benchmark-Specification.md`;
- `.docs/CSharp-Architecture.md`;
- `.docs/CSharp-Style-Conventions.md`;
- `.docs/GitHub-Issue-and-Workflow-Guide.md`;
- repository `AGENTS.md`.

Where this document conflicts with an authoritative product PDD, the product PDD wins. Third-party packages, including PurrNet, remain read-only. Project code must adapt to supported package APIs rather than patching vendor source.

Stage 1.6 has three coupled objectives:

1. make every project-owned client resource lifetime explicit and bounded;
2. make seamless streaming, floating-origin work and presentation churn measurable and stable;
3. establish a predictable LOD/culling/asset-authoring contract for future graphical content.

No Stage 1.6 result is a player-capacity claim or final shipping quality target.

## 2. Locked Stage 1.6 exit guarantee

At Stage 1.6 completion:

- every project-owned Addressables acquisition used by the client has exactly one documented owner and a verifiable release path;
- PurrNet-owned streamed-scene handles remain PurrNet-owned and are never released through a duplicate project handle path;
- disconnect, failed world entry, later world/map transfer and client shutdown have an explicit presentation cleanup boundary rather than relying on scene destruction or process exit;
- repeated world entry/exit and repeated tile traversal do not cause monotonically growing retained world scenes, project-owned Addressables handles, pooled objects or presentation objects;
- normal 256 m world-tile/origin boundary crossings remain visually seamless and do not alter authoritative actor position;
- streamed detailed world content and distant terrain representation remain separate lifetime/LOD concerns, even though both use the shared 256 m world grid;
- transient presentation pooling is limited to measured high-churn, reset-safe objects; network actor identity is not pooled by Stage 1.6;
- significant environment assets have a standard LOD/culling authoring contract, and validation can identify assets that violate it;
- URP Forward+ rendering is benchmarked with representative forest, settlement and crowded-presentation fixtures using real rendered client frames;
- GPU instancing, SRP Batcher compatibility and any later GPU-driven/impostor/HLOD technique are adopted only where compatible and supported by profiling evidence;
- day/night and future weather hooks remain compatible with the rendering contract, but complete weather/environment systems are not implemented here;
- Stage 1.6 closes only with repeatable traversal/rendering evidence and stable retained-memory/resource counts, not with static inspection alone.

## 3. Existing `dev` foundation to preserve

Stage 1.6 builds on the current client managers and does not introduce a second world-streaming or floating-origin system.

### 3.1 World streaming and floating origin

`ClientWorldManager` already:

- subscribes to PurrNet scene load/unload callbacks;
- tracks delivered `World_*` scenes;
- loads `global_world` additively on world entry;
- waits for the initial nearby world-scene set before crossing the entered-world boundary;
- applies the current floating origin to newly delivered world scenes;
- records scene, frame, render and memory telemetry.

`ClientPositionManager` already:

- owns the client floating-origin cell;
- uses `GameConfiguration.ChunkSize`, currently and authoritatively 256 m;
- shifts loaded world scene roots and `global_actors` together;
- preserves absolute player/remote-actor position across rebases;
- converts authoritative `WorldPosition` values into client-local float coordinates;
- records origin-shift duration/count/root-count telemetry.

These ownership boundaries are retained.

The old Stage 1.0 stress-profile wording that refers to independent 100 m origin transitions describes a superseded prototype configuration. Stage 1.6 uses the approved current contract: origin/network cells and authored environment tiles share one 256 m grid through `GameConfiguration.ChunkSize`. Streaming and floating-origin work remain independently owned and may be staggered even when they are triggered by the same geometric boundary.

### 3.2 Existing client telemetry

`ClientPerformanceMetrics` and `ClientWorldManager` already provide useful foundations for:

- rendered/frame interval;
- main-thread, render-thread and GPU frame time;
- frame-spike count;
- scene load/activation/unload callback counts and durations where observable;
- pending project-owned scene operation count;
- origin-shift cost;
- draw calls, batches and triangles;
- total/Mono memory and GC allocation per frame;
- Addressables load/release/pending counters;
- nameplate/UI transient presentation counters.

Do not replace this telemetry with a second profiling framework. Stage 1.6 extends the existing metric catalogue only where the current measurements cannot prove its exit gate.

PurrNet does not expose every streamed-scene request timestamp through the current project integration. Unsupported timing must remain explicitly unavailable rather than being fabricated from unrelated callbacks.

### 3.3 Current resource-lifetime gaps

The reviewed baseline contains concrete Stage 1.6 work:

| Current owner/path | Existing behaviour | Stage 1.6 requirement |
| --- | --- | --- |
| `ClientWorldManager` | Tracks delivered scenes; no complete disconnect/world-exit cleanup path. | Add one explicit client world-presentation cleanup boundary and prove scene tracking returns to baseline. |
| `ClientConnectionManager` | On disconnect destroys account state and logs that scene unloading is not implemented. | Invoke the world-presentation cleanup boundary after connection loss without inventing session authority owned by Stage 1.8. |
| `ClientVFXManager` | Loads Addressable prefabs and directly instantiates them; successful load handles are not released/tracked through effect lifetime. | Give every loaded prefab/instance a balanced lifetime; add active/release telemetry; pool only reset-safe measured classes. |
| `ClientAudioManager` | Bounds cache entry count but stores only `AudioClip` after successful loads; successful cache handles are not retained for later release. | Cache the asset and its owning handle together; release on eviction/shutdown; preserve bounded LRU behaviour. |
| `CameraManager` | Loads the PlayerCamera Addressable collection and instantiates the first prefab without a retained/released operation lifetime. | Give camera asset/instance an explicit world-session lifetime and release path. |
| `UI_NameplateManager` | Instantiates/destroys one nameplate per actor visibility lifetime. | Use a bounded reusable pool if measurement confirms churn benefit; reset all actor-bound state on return. |
| `UI_FCTManager` / `UI_FloatingCombatText` | Instantiates/destroys every floating-combat-text object. | Treat as the initial low-risk UI pooling candidate; reset content, timers, colour, scale and callbacks. |
| streamed PurrNet world scenes | PurrNet owns Addressable scene loading and scene identity. | Keep PurrNet ownership; project code tracks logical/client presentation lifetime and must not duplicate-release vendor handles. |

GC or eventual scene destruction is not an acceptable substitute for a missing Addressables release.

### 3.4 Current rendering/LOD foundation

The Graphical Approach PDD already establishes:

- Unity 6.3 URP Forward+;
- 256 m terrain tiles;
- Unity Terrain for the highest-detail terrain representation;
- mesh-based lower terrain LODs;
- economical mid-poly assets;
- distant vegetation LODs/impostors where appropriate;
- LOD as an asset-authoring requirement rather than a late optimisation pass.

The runtime client does not yet expose one common Stage 1.6 contract for mesh/vegetation/animation/shadow/VFX distance behaviour or an asset validator that enforces that contract. Stage 1.6 adds that contract around native Unity mechanisms; it does not replace the terrain authoring pipeline.

## 4. Client resource ownership contract

### 4.1 Ownership classes

Every client presentation resource belongs to one of these lifetime classes.

| Lifetime class | Examples | Owner | Release boundary |
| --- | --- | --- | --- |
| Process/bootstrap | Addressables initialization, persistent client-manager GameObjects | existing client bootstrap | client shutdown/manager destruction |
| Connection | connection-owned UI/account presentation | existing connection/account managers | disconnect/reconnect boundary |
| World presentation session | `global_world`, camera instance, world-scene tracking and world-bound presentation state | `ClientWorldManager` plus the direct subsystem owner | failed entry, disconnect, later transfer/leave-world, shutdown |
| PurrNet streamed scene | delivered `World_*` environment scenes | PurrNet scene module; `ClientWorldManager` tracks presentation state only | PurrNet-supported unload/cleanup lifecycle |
| Cached asset | audio clips or later measured reusable prefab assets | initiating client manager | eviction, cache clear or owner destruction |
| Active transient | VFX, projectile, floating text | subsystem that spawned it | completion/cancellation/world cleanup |
| Pooled presentation object | FCT/nameplate and later opt-in reset-safe objects | owning presentation manager | returned to bounded pool; pool destroyed on owner/world cleanup as specified |
| Network actor representation | PurrNet actor GameObject/NetworkIdentity | networking/actor lifecycle | existing PurrNet despawn lifecycle |

A resource may not be simultaneously treated as both PurrNet-owned and project-owned.

### 4.2 Client world-presentation session

`ClientWorldManager` owns one explicit world-presentation session lifecycle.

The lifecycle must support:

~~~~text
not in world
    ↓ begin entry
loading global presentation + receiving streamed scenes
    ↓ initial world ready
in world
    ↓ exit/failure/disconnect/transfer
tearing down
    ↓
not in world
~~~~

Required semantics:

- only one active world-presentation session exists at a time;
- each entry receives an internal monotonically changing generation/token so an asynchronous completion from an abandoned entry cannot publish resources into a later session;
- world-ready is false during entry and cleanup;
- cleanup is idempotent;
- new world-bound VFX/camera/presentation acquisition is rejected or ignored once cleanup begins;
- project-owned world resources are released exactly once;
- PurrNet scene ownership is respected throughout;
- callback subscriptions are established once and are not multiplied by repeated entries;
- a later Stage 1.8 reconnect/session implementation may call this boundary without changing its resource semantics.

`ClientConnectionManager` does not become a resource manager. On disconnect it requests the existing world owner to end the presentation session before returning to the menu. Authentication/session authority remains outside Stage 1.6.

### 4.3 Scene ownership and cleanup

Normal connected traversal:

- the server/PurrNet path remains authoritative for which streamed environment scenes are delivered;
- `ClientWorldManager` records loaded scene identity and applies the floating-origin projection after load;
- ordinary unload is consumed from PurrNet's scene-unloaded callback;
- the client does not call `Addressables.Release` on PurrNet's internal scene handles.

Project-owned scenes such as `global_world` retain a project-owned load/unload operation and must be explicitly unloaded when the world presentation session ends.

Disconnect/aborted-entry cleanup must use supported PurrNet/Unity scene cleanup boundaries without reaching into vendor internals. The implementation issue must verify the exact supported PurrNet client scene cleanup API at the installed package version before selecting that call. If the supported package API owns cleanup, use it. Do not bypass it by releasing an unknown PurrNet Addressables handle.

The tracked scene collection must be keyed/cleaned by actual scene lifecycle and must not use a growing historical list.

### 4.3.1 Verified PurrNet client scene cleanup contract

This contract is based on the installed source at `Assets/ThirdParty/PurrNet`, not on
upstream documentation. `Assets/ThirdParty/PurrNet/package.json` identifies the package as
`dev.purrnet.purrnet` version `1.25.0`. The package is vendored under `Assets/ThirdParty`
and is not resolved by `Packages/manifest.json` or `Packages/packages-lock.json`. No Git
revision or commit identifier is exposed by the installed package metadata, so the source
location, package version and concrete type names are the strongest available identity.

The vendor property `NetworkManager.sceneModule` is declared in
`Assets/ThirdParty/PurrNet/Runtime/Managers/NetworkManager.cs` and returns the
`PurrNet.ScenesModule` instance (`_serverSceneModule ?? _clientSceneModule`). The inspected
client scene implementation is
`Assets/ThirdParty/PurrNet/Runtime/CoreModules/Scenes/ScenesModule.cs`, with Addressables
ownership implemented in its sibling `ScenesModule.Addressables.cs`.

#### Normal client disconnect and stop path

For an explicit client stop, vendor `RawNetManager.StopClient()` calls the transport's
`StopClient(...)`; transport loss and remote disconnects converge on the same
`NetworkManager.OnConnectionState(...)` path. For a normal client disconnect, the installed
call chain is:

1. The transport reaches `NetworkManager.OnConnectionState(Disconnected, false)`.
2. `NetworkManager` first calls `_clientModules.OnConnectionState(...)`, then invokes the
   public `onClientConnectionState` event synchronously. `ClientConnectionManager` receives
   this event in `OnClientConnectionStateChanged(...)`. Only after those subscribers return
   does `NetworkManager` set `_isCleaningClient`.
3. On the next `NetworkManager.OnTick`, `_clientModules.Cleanup()` drives the client module
   cleanup. `ScenesModule.Cleanup()` waits for pending scene operations, applies
   `NetworkRules.ShouldCleanupScenesOnDisconnect()` and
   `NetworkRules.SceneCleanupModeOnDisconnect()`, and then unloads the tracked PurrNet
   scenes through its cleanup state machine.
4. `ScenesModule.UnloadAllScenesCleanup(...)` uses
   `TryUnloadAddressableSceneOnCleanup(...)` for an Addressables-backed scene. That method
   calls `Addressables.UnloadSceneAsync(handle, ..., true)` using the handle already owned by
   PurrNet. Non-Addressables scenes are unloaded through the same vendor cleanup path with
   `SceneManager.UnloadSceneAsync(...)`. Pending Addressables operations are discarded by
   `ScenesModule.Disable(...)`; PurrNet releases or unloads their handles according to their
   completion state.
5. The cleanup state machine completes before the client modules are unregistered. This is
   the terminal physical cleanup path for PurrNet-owned client scenes when the installed
   network rules enable scene cleanup. If the rules are set to `SceneCleanupMode.Off`, the
   installed `ScenesModule.Cleanup()` intentionally returns without unloading those scenes;
   Stage 1.6 must not silently compensate by taking ownership of them. The `NetworkManager`
   prefab uses the installed `ServerOwner` `NetworkRules` asset, so any change to that
   cleanup-mode configuration is a separate, explicit configuration decision. The concrete
   reference is `Assets/Resources/NetworkManager.prefab` to
   `Assets/ThirdParty/PurrNet/Defaults/NetworkRules/ServerOwner.asset`.

`ScenesModule.onSceneUnloaded` is not a disconnect-completion callback. During ordinary
scene actions, `RemoveScene(...)` queues `PlayUnloadEventsForScene(...)`, which invokes
`onPreSceneUnloaded`, `onSceneUnloaded` and `onPostSceneUnloaded` in that order. During
disconnect cleanup, `UnloadAllScenesCleanup(...)` unloads the scenes and clears the
module registry directly; it does not call `RemoveScene(...)` or
`PlayUnloadEventsForScene(...)`. Therefore `onSceneUnloaded(SceneID, bool)` is not
guaranteed during disconnect cleanup. The project-observable disconnect boundary is the
synchronous `ConnectionState.Disconnected` delivery to `ClientConnectionManager`, which
occurs before PurrNet starts `_clientModules.Cleanup()`; it is not a guarantee that the
physical unload has completed by the time that callback returns.

#### Abandoned or failed world entry

If the connection terminates after PurrNet has delivered one or more scenes but before the
`ClientWorldManager` entry coroutine finishes, the same `NetworkManager` disconnect path
owns the delivered scenes. Completed scenes are in `ScenesModule`'s registry and follow
`ScenesModule.Cleanup()`. Addressables loads still in flight are handled by
`ScenesModule.Disable(...)` and its pending-operation cleanup; the client must not take over
their handles. On the project's `Disconnected` callback, `ClientWorldManager` must invalidate
the active entry generation, stop publishing the abandoned coroutine's result, and clear its
logical tracking of PurrNet-delivered scenes while PurrNet finishes physical cleanup. It must
not wait for `onSceneUnloaded` as a disconnect completion signal, and it must not issue a
second unload request.

The server-side `ServerWorldManager.LoadGlobalActorsScene()` call confirms that
`global_actors` is loaded through the server `sceneModule`. When that scene is delivered to a
client by PurrNet, its client lifetime follows the same PurrNet ownership rule; the
`global_actors` name does not make it equivalent to the client-loaded `global_world` scene.

#### Verified ownership table

| Scene/resource | Loader | Handle/lifetime owner | Required Stage 1.6 cleanup action |
|---|---|---|---|
| `global_world` | Unity `SceneManager.LoadSceneAsync` in `ClientWorldManager` | Ninth Age client | Explicitly unload through the Unity scene API. |
| PurrNet-delivered `World_*` | PurrNet scene module | PurrNet | Let the installed disconnect cleanup path unload it; observe ordinary connected unloads through PurrNet callbacks. |
| `global_actors` on the client, when delivered by PurrNet | PurrNet scene module | PurrNet | Let the installed disconnect cleanup path unload it; do not treat it as the project-owned `global_world`. |

#### Final prescription — Contract A: disconnect path owns cleanup

The installed PurrNet implementation proves Contract A. The exact owner path is
`NetworkManager.OnConnectionState(Disconnected, false)` setting the client cleanup state,
followed on the next tick by `NetworkManager.OnTick` calling
`_clientModules.Cleanup()` and `ScenesModule.Cleanup()`. Ninth Age observes the disconnect
through `NetworkManager.onClientConnectionState` / `ClientConnectionManager.OnClientConnectionStateChanged`;
that callback is the point at which #314 must end the project-owned world session and clear
logical PurrNet-scene tracking. PurrNet's own cleanup state machine remains responsible for
the physical unload and any Addressables handle release, subject to the configured scene
cleanup mode. #314 must not manually unload or release any PurrNet-owned scene handle, and
must not select or invoke another PurrNet scene-cleanup API.

Ninth Age client code must therefore never:

- call `Addressables.Release` on a PurrNet-owned scene handle;
- manufacture a second Addressables handle solely to unload a PurrNet scene;
- call `SceneManager.UnloadSceneAsync` directly on a PurrNet-owned scene unless a future
  installed PurrNet contract explicitly delegates that ownership;
- patch PurrNet internals;
- infer ownership from the `World_*` name alone; or
- conflate project-owned `global_world` with PurrNet-owned `global_actors` or environment
  scenes.

### 4.4 Addressables asset rule

For every project-owned `LoadAssetAsync` or `LoadAssetsAsync` operation:

1. the initiating manager records the handle ownership immediately;
2. failed/cancelled loads reach a terminal cleanup path;
3. successful loads retain the handle for at least as long as any live object still depends on the loaded asset and its dependencies;
4. eviction/cleanup destroys/releases dependent instances first, then releases the owning handle exactly once;
5. late async completion checks the current owner/session generation before publishing an instance;
6. telemetry increments/decrements live/pending ownership counts from the same state transition that changes the real lifetime;
7. no manager stores only `handle.Result` while discarding the handle required for release.

Shared cache ownership must be explicit. Do not introduce a project-wide generic Addressables service merely to hide these rules; the current number of consumers is small enough for direct subsystem ownership unless later evidence shows a shared cache is required.

### 4.5 Audio cache

`ClientAudioManager` retains its bounded cache policy, but each cache record owns both:

- the loaded `AudioClip`;
- the corresponding valid `AsyncOperationHandle<AudioClip>`.

Eviction removes the LRU record and releases that record's handle. Owner destruction invalidates publication immediately. Completed cached handles are released during owner cleanup. An Addressables operation that is still in flight remains owned by its already-registered completion callback; that callback must observe the destroyed-owner state, publish nothing, and release the terminal handle exactly once. No pending operation is released both from `OnDestroy()` and again from its completion callback. Failed operations are removed from pending state and cleaned up.

The cache remains a presentation cache, not persistent gameplay state.

### 4.6 Camera lifetime

The PlayerCamera Addressable acquisition and instantiated camera belong to the world-presentation session.

The implementation must either:

- retain the loaded prefab handle until the instantiated camera is destroyed, then release it; or
- use a supported Addressables instance API whose release operation owns that dependency lifetime.

Do not load a camera prefab, keep an instance indefinitely and lose the corresponding asset handle.

Repeated entry must never create multiple active player cameras for one client world session.

### 4.7 VFX lifetime

Stage 1.6 first fixes VFX ownership before optimising allocation.

Each spawned Addressable VFX/projectile must have a terminal release signal that allows `ClientVFXManager` to:

- remove the active instance from its ownership set;
- record release/active-count telemetry;
- destroy or return the presentation object through the selected lifecycle;
- release the Addressables dependency lifetime exactly once.

The current shared RPC/endpoint payload remains unchanged. Server authority and VFX publication semantics are not modified for client allocation work.

Initial pooling policy:

- do not pool all VFX globally;
- do not pool an effect unless its component state has an explicit reset contract and repeated creation is demonstrated to be material;
- projectile pooling is deferred until target/reference, elapsed-time, impact/audio and child-effect reset are proven safe;
- one-shot or self-destroying third-party VFX without a project-owned reset contract remain destroy/release lifetimes.

A later VFX pooling issue may add an opt-in project-owned pool policy using measured high-churn effect classes. It must not depend on modifying third-party components.

### 4.8 UI pooling

The initial mandatory pooling candidate is floating combat text because it is project-owned, short-lived and resettable.

`UI_FCTManager` owns a bounded pool. `UI_FloatingCombatText` must expose an explicit activation/reset/completion contract rather than using `Destroy(gameObject)` as its only completion signal.

On reuse, reset at least:

- text content;
- colour/alpha;
- direction;
- elapsed time;
- size multiplier;
- duration multiplier;
- transform position/rotation/scale;
- subscribed completion callback/state.

Nameplates are the second candidate. Pool them only after the FCT pool pattern is validated. A returned nameplate must hold no actor reference, target/hover state, cast state, aura/stat binding or subscribed callback from the previous actor.

Pool capacity is bounded configuration. When a pool is full, excess returned objects are destroyed. A pool may grow to its configured retained capacity but may not grow without bound.

Networked actors and their `NetworkIdentity` are not pooled by Stage 1.6.

## 5. World streaming and floating-origin performance contract

### 5.1 Shared grid, separate work

The 256 m shared grid means one movement boundary can cause both:

- environment-interest/scene churn;
- a client floating-origin shift.

These remain independent operations. Stage 1.6 must not merge them into one state owner.

The implementation should stagger/defer presentation work where supported if simultaneous work produces a measured spike, but must not defer authoritative movement or make world content visibly absent.

### 5.2 Initial nearby scene set

The current 3 × 3 nearby detailed-scene set remains the initial behaviour.

Stage 1.6 must remove client assumptions that infer readiness solely from a permanently hard-coded count of nine. World-entry readiness should be based on the current required initial scene set supplied by the existing streaming contract, so later content-specific radii or instance scene sets do not require rewriting client readiness.

This does not move environment-interest authority to the client.

### 5.3 Streaming telemetry

The existing metrics remain the base. Stage 1.6 adds only the missing measurements needed to prove lifecycle and spikes:

- current tracked streamed-scene count;
- per-session streamed-scene load/unload counts;
- project-owned scene operation count and duration;
- scene-activation callback/main-thread duration;
- origin-shift duration and shifted-root count;
- frame interval/main/render/GPU time around streaming/origin events;
- current/pending project-owned Addressables handle counts by owning subsystem;
- active and retained pool counts by pool;
- active VFX count and release count;
- world-session cleanup duration;
- retained-memory samples before entry, after steady state, after traversal cycles and after cleanup.

Where Unity/PurrNet cannot expose a measurement, register/report it as unavailable and retain the available proxy separately. Do not label a callback duration as full streamed-scene load/unload latency.

### 5.4 Retained-memory stability

Stage 1.6 does not adopt a universal memory-cap number from the existing lightweight rendering baseline.

The validation question is first whether retained state stabilises.

For a repeated identical route/session fixture:

- live scene count returns to the expected steady-state range;
- live project-owned Addressables handle counts return to the expected steady-state range;
- pooled retained counts remain at or below configured capacity;
- after cleanup, world-session resources return to the documented not-in-world baseline;
- memory may fluctuate because of Unity allocator behaviour, but project-owned live-resource counts must not trend upward cycle after cycle;
- any long-run memory slope that cannot be explained by stable project-owned counts remains a leak investigation blocker.

## 6. LOD and culling contract

### 6.1 Native mechanisms first

Stage 1.6 standardises the use of existing Unity/URP mechanisms before adding a custom renderer framework.

Use, where appropriate:

- Unity `LODGroup` for authored GameObject mesh/vegetation distance families;
- existing Unity Terrain/distant terrain representation for terrain;
- camera/frustum/layer culling provided by Unity;
- animator culling modes for nonessential off-screen animation where gameplay presentation permits;
- renderer shadow settings by LOD/category;
- particle/VFX bounds and distance policies that do not keep invisible effects expensive;
- GPU instancing and SRP Batcher-compatible materials where the asset/material structure permits.

Do not create a second spatial visibility system on the client for normal renderer culling.

### 6.2 Asset categories

Stage 1.6 establishes these authoring categories:

| Category | Required distance behaviour |
| --- | --- |
| Terrain | detailed local Terrain plus independently retained lower/distant terrain representation; detailed object-scene lifetime does not define terrain visibility lifetime |
| Large architecture / terrain-kit mesh | LOD family where screen-space reduction is material; preserve silhouette and terrain contact |
| Trees / large vegetation | LOD family with distant card/impostor option where validated; control alpha overdraw and shadow distance |
| Small props / undergrowth | may cull before requiring multiple geometric LODs; repeated assets should use compatible materials/instancing where practical |
| Characters / NPCs | preserve gameplay silhouette; reduce animation/shadow/presentation work before making actors unreadable |
| VFX | effect-specific bounds and maximum useful presentation distance; gameplay telegraph readability takes priority |
| World-space UI/nameplates | distance/visibility policy independent of actor network visibility; do not process invisible/behind-camera presentation unnecessarily |

Exact LOD transition percentages/distances are tuning values, not universal product constants. They are measured per asset category and quality tier.

### 6.3 Asset-authoring metadata and validation

Do not require a new metadata component where Unity's native asset/component state already expresses the contract.

Stage 1.6 adds project-owned editor validation that can report, by category:

- missing required `LODGroup`;
- invalid/non-monotonic LOD transitions;
- missing renderer assignment in an LOD level;
- inappropriate persistent shadow casting on distant LODs according to the approved project authoring rule;
- excessive material-slot count or transparency usage only where an explicitly approved editor review threshold exists;
- vegetation/architecture assets that are marked as requiring distant representation but have none;
- invalid or oversized VFX renderer bounds where detectable;
- missing project category assignment only when native Unity state cannot distinguish the intended rule.

The validator's category rules are editor authoring policy, not runtime player configuration. It may use editor-only labels or equivalent editor metadata where a native Unity component cannot identify the intended asset category. Those labels must not introduce a runtime presentation component or a second visibility/culling system.

Validation initially reports actionable warnings/errors. It must not silently rewrite authored assets.

### 6.4 Graphics settings, authored state and engineering-limit ownership

Stage 1.6 must keep three distinct kinds of presentation values under their existing appropriate owners rather than combining them into a new client-presentation configuration subsystem.

**Player-adjustable graphics preferences** belong to the existing `ClientSettingsManager` and are persisted locally through `PlayerPrefs`. When Stage 1.6 or later work introduces an actual player-adjustable rendering control, such as a quality preset, shadow quality/distance, vegetation/detail distance, VFX quality/distance or animation-quality option, that control must use the existing client-settings path. Do not create a parallel graphics-settings manager or a `ClientPresentationConfiguration` solely to hold those preferences.

**Authored asset presentation state** remains on the relevant Unity assets/components. Examples include `LODGroup` transition values and renderer membership, per-LOD renderer shadow state, material structure and effect/renderer bounds. Project editor tooling may validate that state against the authoring contract, but it does not move those values into `PlayerPrefs` or duplicate them in runtime configuration.

**Manager-specific engineering limits** remain with the runtime owner that enforces the lifetime unless measurement establishes a concrete reason to expose a different contract. Examples include retained pool capacities such as the FCT pool owned by `UI_FCTManager`. These are implementation/resource limits, not user graphics preferences, and must not be moved into `PlayerPrefs` merely because they affect presentation performance.

Benchmark-only review thresholds and editor validation rules remain tooling/evidence policy unless they become a real player-adjustable runtime setting. They are not implicitly runtime configuration.

None of these client-local presentation concerns belong in shared authoritative `GameConfiguration`, and they must not duplicate `GameConfiguration.ChunkSize` or other shared world contracts.

Values that have not yet been measured remain explicitly provisional and are promoted only with Stage 1.6 evidence.

## 7. URP Forward+ optimisation policy

### 7.1 Baseline before optimisation

The approved renderer remains URP Forward+.

For each representative fixture, first capture an unchanged Forward+ baseline with:

- exact Git/build identity;
- hardware and graphics device;
- resolution/display mode;
- URP asset/quality level;
- fixture content hash/counts;
- draw calls/batches/triangles;
- main/render/GPU frame times;
- memory;
- lights/shadow casters;
- visible actors/nameplates/VFX;
- LOD state where measurable.

### 7.2 Techniques to evaluate

Evaluate techniques independently enough to attribute results:

1. SRP Batcher compatibility and material structure;
2. GPU instancing for repeated compatible meshes/materials;
3. authored LOD/culling;
4. vegetation impostors/cards where suitable;
5. only then, if the remaining measured bottleneck justifies it, Unity-supported GPU-driven rendering or an HLOD layer.

Do not enable a technique globally merely because it is theoretically faster. Do not assume SRP Batcher, GPU instancing and a GPU-driven path stack beneficially for every material/renderer combination.

A candidate optimisation is retained only when:

- the fixture remains visually correct;
- floating-origin/streaming behaviour remains correct;
- the relevant CPU/GPU or memory metric improves or a required density becomes viable;
- it does not create a worse tail-frame or asset-authoring burden disproportionate to the gain.

### 7.3 Lighting, day/night and weather

Stage 1.6 preserves the existing graphical requirement for day/night and future weather.

The rendering contract must therefore avoid optimisations that require a permanently static lighting model or make future weather overlays impossible.

Stage 1.6 does not implement:

- the complete day/night gameplay system;
- full weather simulation;
- final biome atmosphere;
- final light-count/art budgets.

Forward+ local lights, shadows and environmental effects remain measurable presentation costs.

## 8. Ordered implementation

Execute production work in this order. Follow the GitHub workflow rule that production implementation and regression-test issues remain separate where required.

### Step 1 — Client resource audit and lifetime telemetry

Create the Stage 1.6 resource inventory from all project-owned client Addressables/scene/presentation acquisition sites.

Extend existing telemetry with live project-owned handle counts, active VFX/release count, pool counts and world-session cleanup observations. Correct stale metric availability comments where current endpoints now provide the required lifecycle.

No pooling or rendering optimisation is performed in this step.

### Step 2 — World-presentation session and cleanup boundary

Add the explicit `ClientWorldManager` world-presentation lifecycle described in §4.2.

Integrate disconnect through `ClientConnectionManager`. Make entry failure and repeated entry safe. Release `global_world` and project-owned world presentation resources through the explicit boundary.

Verify the supported PurrNet scene cleanup path at the installed package version; retain vendor scene-handle ownership.

### Step 3 — Fix project-owned Addressables lifetimes

In focused changesets:

1. fix `ClientAudioManager` cache handle ownership and LRU release;
2. fix `CameraManager` camera asset/instance lifetime;
3. fix `ClientVFXManager` active-instance/asset lifetime and release signal.

Do not introduce pooling as a way to conceal a missing release path.

### Step 4 — Bound high-churn UI allocation

Implement the bounded floating-combat-text pool and its reset contract.

Measure spawn/release/GC behaviour. Then implement nameplate pooling only if the measured fixture shows meaningful creation/destruction churn and the reset contract can be made complete without changing gameplay/UI semantics.

### Step 5 — Streaming readiness and traversal instrumentation

Remove the client assumption that world readiness is permanently equivalent to `_loadedScenes.Count >= 9` while preserving the current 3 × 3 initial behaviour.

Instrument the full project-observable boundary for scene callback/activation, origin shifting, frame tails and retained-resource counts.

Do not move streaming-interest authority to the client.

### Step 6 — LOD/culling policy and editor validation

Add the native-component authoring validation described in §6.3 around Unity's existing `LODGroup`, Renderer, Animator, material and bounds state.

Use editor-only category assignment only where native Unity state cannot distinguish the intended asset class. Keep those authoring rules in editor tooling rather than introducing a runtime presentation-policy/configuration subsystem.

If this step introduces any actual player-adjustable graphics control, route that preference through the existing `ClientSettingsManager` / `PlayerPrefs` path described in §6.4. Do not move manager-owned engineering limits such as FCT pool capacity into player preferences.

Adopt the authoring contract first for representative test assets/fixtures rather than mass-editing unrelated production art.

No third-party assets may be rewritten automatically.

### Step 7 — Representative Forward+ fixture campaign

Run the Stage 1.6 rendering campaign using the intent of `P1-S0-06`:

- outdoor/control;
- forest;
- settlement;
- crowded presentation slice.

Use the documented density progression rather than treating the old lightweight fixture as a production load.

Record the current existing provisional Editor guardrails as comparison references only:

| Metric | Existing provisional Editor review threshold |
| --- | ---: |
| rendered frame p95 | ≤ 6.0 ms |
| rendered frame p99 | ≤ 10.0 ms |
| main-thread p99 | ≤ 5.0 ms |
| render-thread p99 | ≤ 4.0 ms |
| GPU p99 | ≤ 2.5 ms |
| total allocated memory, median-run p50 | ≤ 2,000,000,000 bytes |

These values are not new Stage 1.6 shipping budgets and the existing 26-draw-call / 1,930-triangle fixture signature is not a world-content limit.

A >20% graphical frame/resource increase against a compatible approved baseline remains a regression-review trigger.

### Step 8 — Rapid traversal and repeated-lifetime campaign

Run the current-grid form of `P1-S0-05`:

- a connected legal route across at least six adjacent 256 m tiles;
- repeated positive and negative world coordinates;
- repeated shared 256 m origin/tile boundaries;
- isolated origin-shift work where scenes can be held steady;
- isolated scene churn where practical;
- combined traversal/streaming flow;
- repeated world entry/cleanup cycles.

Measure scene counts, origin-shift cost, frame tails, Addressables ownership counts, pool counts and retained memory.

No numerical streaming-hitch or memory-slope limit is invented before this evidence exists. The first qualified campaign establishes candidate Stage 1.6 review thresholds, which are then recorded in the performance-budget documentation through an explicit owner-approved update.

### Step 9 — Focused regression coverage

Add separate regression-test issues for the critical contracts:

- world-session cleanup is idempotent;
- late async completion from an old session cannot publish into a new session;
- project-owned Addressables handles balance on success, failure, eviction and cleanup;
- audio cache eviction releases the evicted handle;
- camera entry/exit cannot retain duplicate instances/handles;
- VFX completion removes active ownership and releases exactly once;
- FCT pooled reuse fully resets state and remains bounded;
- nameplate reuse, if implemented, retains no previous actor state;
- floating-origin shift preserves absolute actor position;
- newly loaded scene projection uses the current origin;
- repeated scene callbacks do not duplicate tracked scenes.

Tests do not prove GPU performance. Render/traversal campaigns remain required.

### Step 10 — Stage close-out

Stage 1.6 closes only after:

1. production lifetime fixes are accepted;
2. focused regression coverage is accepted;
3. representative Forward+ evidence exists;
4. rapid traversal/repeated-lifetime evidence exists;
5. new measured thresholds or explicitly unresolved hardware-dependent limits are documented;
6. no known unbounded project-owned resource lifetime remains in the Stage 1.6 client scope.

## 9. Required benchmark interpretation

### 9.1 P1-S0-05 update

Use the stress profile's intended traversal/churn workload but apply the current authoritative 256 m shared grid. Do not reintroduce a 100 m origin grid solely to reproduce historical text.

Correctness failures include:

- visible floating-origin teleport;
- changed authoritative absolute position;
- missing terrain/world content on a legal route;
- duplicate scene roots;
- stale tracked scenes;
- presentation objects surviving their owning world session;
- project-owned live Addressables counts increasing on each identical cycle.

### 9.2 P1-S0-06 rendering workload

The forest and settlement fixture remains the primary renderer workload.

The density ladder is exploratory, not an art cap:

- forest D0: 50 trees + 200 undergrowth clusters per tile;
- settlement D0: 15 buildings + 80 props per tile;
- D1/D2/D3: ×2/×4/×8 instance-count probes where the asset fixture supports them.

Capture 1/4/16 dynamic-light variants only where the fixture genuinely implements those lights. Record shadow casters, LOD/impostor policy, terrain settings and transparency/overdraw characteristics rather than inferring them from object count.

A Null graphics device cannot qualify GPU/rendering evidence.

### 9.3 Evidence quality

Use the existing Phase 1 benchmark identity/retention rules.

For graphical comparisons:

- use qualified independent repeats;
- retain per-run p50/p95/p99 and raw maxima;
- preserve failures and outliers rather than deleting them;
- do not pool samples from incompatible revisions;
- record hardware/build/quality context;
- distinguish Editor-only evidence from standalone graphical-client evidence.

Stage 1.6 should add standalone-client evidence when the harness supports it. Editor-only evidence cannot be promoted silently into a production GPU claim.

## 10. Ownership matrix

| Responsibility | Owner after Stage 1.6 |
| --- | --- |
| server environment interest / delivered streamed scenes | existing server interest/world managers + PurrNet |
| client record of delivered world scenes | `ClientWorldManager` |
| client world-presentation session lifecycle | `ClientWorldManager` |
| client floating origin and world→local projection | `ClientPositionManager` |
| authoritative actor position | server/ECS existing ownership; unchanged |
| player camera asset/instance | `CameraManager`, scoped to world presentation |
| audio cache asset handles | `ClientAudioManager` |
| active VFX/projectile resource lifetime | `ClientVFXManager` plus explicit completion signal |
| FCT pool | `UI_FCTManager` |
| nameplate pool, if justified | `UI_NameplateManager` |
| network actor GameObject/NetworkIdentity | existing PurrNet actor lifecycle |
| player-adjustable graphics preferences | `ClientSettingsManager` + local `PlayerPrefs` persistence |
| manager-specific presentation/resource limits | existing owning manager, e.g. FCT capacity in `UI_FCTManager` |
| authored LOD/render/shadow/bounds state | native Unity assets/components |
| LOD/culling asset compliance and category authoring rules | project-owned editor validator |
| frame/render/resource telemetry | existing `PerformanceTelemetry` / `ClientPerformanceMetrics` |
| final content/update catalog versioning | Stage 1.9; not Stage 1.6 |

## 11. Non-goals

Stage 1.6 does not:

- change authoritative server movement, spatial indexing or actor-interest semantics;
- change the approved one-world/one-process/one-machine topology;
- change PurrNet vendor source or create a second networking/scene-loading stack;
- implement world-server transfer/session authority owned by Stages 1.7–1.8 and Phase 3;
- replace Addressables with a bespoke patch/cache system;
- pool PurrNet network actors;
- pool every VFX/object;
- build a generic service locator, message bus or universal resource manager;
- replace the terrain-generation/terrain-authoring pipeline;
- set final per-asset triangle/texture/light budgets without representative evidence;
- enable GPU-driven rendering or HLOD globally before comparison evidence;
- implement complete weather or day/night gameplay;
- treat scene membership as actor visibility authority;
- make client-local float transforms authoritative;
- change the 256 m shared world-grid contract.

## 12. Expected issue decomposition

The implementation should be converted into narrowly scoped GitHub issues after this PDD is approved.

Recommended production sequence:

1. resource-lifetime telemetry and inventory;
2. client world-presentation cleanup;
3. audio Addressables lifetime;
4. camera Addressables/world lifetime;
5. VFX Addressables/active lifetime;
6. FCT pooling;
7. optional measured nameplate pooling;
8. streaming-readiness/instrumentation cleanup;
9. native LOD/culling authoring validator, with player-adjustable graphics controls using `ClientSettingsManager` only when a concrete runtime setting is introduced;
10. Forward+ candidate optimisation slices, each separately measurable;
11. Stage 1.6 rendering campaign;
12. Stage 1.6 traversal/lifetime campaign.

Regression-test issues should be separate from their production issues according to the repository workflow. Benchmark/evidence issues must preserve the exact fixture, build and hardware identity.

Do not combine all Stage 1.6 production work into one large implementation issue.

## 13. Validation order

Validate in this order:

1. static ownership/lifecycle correctness;
2. Addressables acquire/release and asynchronous failure paths;
3. world entry/disconnect/re-entry cleanup;
4. pooled-object reset and bounds;
5. floating-origin/scene-transition correctness;
6. LOD/culling asset validation;
7. representative Forward+ performance;
8. repeated traversal and memory/resource stability.

A visually correct single empty scene is not sufficient Stage 1.6 validation. Conversely, a faster benchmark does not pass if it leaks resources, creates visible LOD/streaming discontinuities or weakens gameplay readability.

## 14. Stage 1.6 hand-off

After Stage 1.6, later work may assume:

- client presentation resources have explicit owners and cleanup;
- streamed world content can be profiled without conflating scene lifetime with actor authority;
- future art has a defined LOD/culling acceptance contract;
- high-churn UI presentation has a bounded pooling pattern;
- renderer optimisation decisions are evidence-based;
- Stage 1.9 can build Addressables release/version compatibility on top of a balanced runtime handle-lifetime foundation.

Stage 1.9 still owns remote catalog/version/content-delivery policy. Stage 1.6 owns the runtime lifetime and rendering behaviour of content once the client uses it.
