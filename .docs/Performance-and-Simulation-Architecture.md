# Ninth Age — Performance and Simulation Architecture

**Status:** Proposed architecture contract — requires manual validation of [Issue #41](https://github.com/FrontierDev/ninthage/issues/41)  
**Scope:** Phase 1.0 runtime ownership and integration boundaries; **documentation only**  
**Code baseline reviewed:** `dev` at `f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f`  
**Review date:** 2026-10-13  
**Unity version:** 6000.3.10f1

## 1. Purpose, authority and interpretation

This document records **what the existing runtime does** and separately defines **the intended performance-oriented ownership contracts** for Phase 1. It does not create classes, methods, gameplay rules, supported population limits, implementation permission or a claim that proposed systems already exist.

Authority follows [MMORPG Master PDD](MMORPG-Master-PDD.md): the Master PDD defines project-wide principles; subsystem PDDs own player-facing behaviour; this document owns technical integration decisions subordinate to those PDDs; the [Project Roadmap](Plans/Project-Roadmap.md) and [Phase 1 Implementation Plan](Plans/Phase-1-Implementation-Plan.md) own sequence and delivery. Follow [C# Architecture](CSharp-Architecture.md), [C# Style Conventions](CSharp-Style-Conventions.md), [GitHub Issue and Workflow Guide](GitHub-Issue-and-Workflow-Guide.md) and `AGENTS.md` for all implementation work. **Third-party source, namespaces, packages and vendor assets are read-only without specific user authorisation.** Project-owned PurrNet integrations may evolve; PurrNet itself may not be modified under this contract.

**Authoritative product references:**

- [World Runtime and Instancing PDD](pdd/World-Runtime-and-Instancing-PDD.md) — world/map/instance identity, double precision, 100 m origin cells, 256 m world tiles, streamed scenes, actor interest, persistence across scene unloads.
- [Movement and Traversal PDD](pdd/Movement-and-Traversal-PDD.md) — responsive locally controlled movement, server validation and authoritative world position.
- [Account, Character and Persistence PDD](pdd/Account-Character-and-Persistence-PDD.md) — identity/session separation, location recovery, minimum disconnect grace, timed-state continuity and persistence.
- [AI and Encounter Behaviour PDD](pdd/AI-and-Encounter-Behaviour-PDD.md) — server authority, NPC activation and scalable decision frequency.
- [Combat System PDD](pdd/Combat-System-PDD.md) — cast, aura, cooldown, combat and spatial execution semantics.
- [Graphical Approach PDD](pdd/Graphical-Approach-PDD.md) — URP-facing rendering, LOD and art performance requirements.
- [Live Content and Versioning PDD](pdd/Live-Content-and-Versioning-PDD.md) — immutable compatible release/content state and controlled Addressables ownership.

**Terminology:** **Current** means verified in project-owned source at the reviewed revision, not confirmed by a running Unity build. **Required contract** means the technical invariant later stages must meet. **Deferred** means the named Stage 1 issue owns implementation. **Open** means the relevant PDD or implementation decision does not yet fix a value or method; a benchmark or engineering issue must not silently invent player-facing behaviour.

### Baseline qualification

The current [C# Architecture](CSharp-Architecture.md) is a descriptive snapshot at source revision `3232b1cfc1a889d1f8be7665714bf145e8329a4b`; this document additionally reviews the later `dev` revision above. The [Phase 0.40 issue](https://github.com/FrontierDev/ninthage/issues/40) is closed, but its prescribed `.docs/Phase-0-Stable-Foundation-Baseline.md` is **not present in the reviewed `dev` tree**. That discrepancy must be reconciled before treating the Phase 0 acceptance artifact as independently verified. It does **not** justify changing Phase 0 records within Issue #41.

## 2. Actual current-runtime boundaries

| Area | Current source and behaviour | Contract gap (not implemented by this document) |
| --- | --- | --- |
| Process selection | `Assets/Runtime/Core/GameBootstrapper.cs::Awake` selects `ServerInitialization.Begin` for batch mode/no graphics/forced mode, otherwise `ClientInitialization.Begin`. | Dedicated benchmark modes and unambiguous test-client selection are Stage 1.0 tooling work. |
| ECS clock | `ServerECSManager.ServerInitializeECSWorlds` creates a Unity Entities `World`, registers seven systems in `SimulationSystemGroup` and appends it to the player loop. Spell, aura, cooldown and ticker systems use `UnityEngine.Time.deltaTime`; `ServerInitialization.ServerStartSimulation` is a placeholder. | No single configured authoritative simulation tick/order/catch-up contract (Stage 1.1). |
| GameObject actor and ECS state | `Actor` is a PurrNet `NetworkBehaviour`; `ServerActorManager.OnActorSpawned` creates its `ActorComponent` and buffers. `ServerActorManager.Update` synchronises client-owner transforms into ECS and `LateUpdate` synchronises NPC positions conditionally. | No demonstrated single writer for double-precision spatial state, stable actor–Entity reverse mapping/lifecycle, or job-safe bridge (Stage 1.3). |
| Movement | Owner-controlled PurrNet transform updates feed server transform; `ValidatePlayerPositions` samples float positions and may send correction; `SyncPlayerECS` converts server float transform to `double3`. | The PDD-required absolute-position reconstruction/validation and automatic spatial-interest update are incomplete (Stages 1.1–1.3). |
| World content | `ServerWorldManager.EnsureChunkLoaded` loads `World_X_Y` via PurrNet Addressable additive scenes; `UnloadChunk` unloads. `ServerInterestManager` uses a 3×3 neighbouring scene set. | Origin-cell, streamed world-tile and instance qualifications are not yet separate in all implementation paths (Stages 1.2 and 1.6). |
| Actor interest | `ChunkSubscriptions` indexes actors/NetworkIdentities by scene-name strings; `ChunkVisibilityRule.CanSee` checks subscribed actors; changed-chunk identity re-evaluation is targeted in `ServerInterestManager.OnInterestRequest`. | Does not implement general instance-aware double-precision distance relevance or spatial queries (Stages 1.2 and 1.5). |
| Scene and actor lifetime | `ServerSpawnManager` instantiates runtime actors into the persistent `global_actors` scene; `Actor.OnDestroy` emits `onServerActorDespawned`; `ServerSpawnManager` observes that event for spawn-point bookkeeping. | Complete actor ECS/index/replication cleanup and autonomous persistence lifetime must be defined/implemented (Stages 1.3–1.4 and 1.8). |
| Reconnect / persistence | `ServerConnectionManager.OnPlayerLeft` currently saves a JSON account snapshot and despawns the network identity, then removes connection/session maps. `PlayerDatabase` persists JSON account data. | This does not meet the Persistence PDD's 30-second minimum unexpected-disconnect actor continuity (Stages 1.7–1.8). |
| Client placement | `ClientPositionManager.SetOriginChunk` shifts loaded scene roots and `global_actors`; `ClientWorldManager` tracks delivered world scenes and uses `global_world`. | Precise separation from world tiles, automatic position-driven updates and measured resource lifetimes remain incomplete (Stages 1.2 and 1.6). |

These observations are **not** an instruction to change the named implementation now. They identify which existing pathways the later architecture must reconcile.

## 3. Authoritative time and simulation scheduling

### Current

The Unity player loop drives the server Entities simulation; several ECS timers use frame delta, and several server managers use `MonoBehaviour.Update` or `LateUpdate`. Client `Actor.Update` advances presentation-side aura objects. A declared engine fixed timestep is not evidence that gameplay systems already execute as one fixed authoritative tick.

### Required contract — Stage 1.1

1. **The server owns gameplay time.** Exactly one server simulation-clock authority provides the simulation tick identity, delta and ordered phase boundaries. Neither render frame rate, remote client time, nor independently chosen per-manager clocks may determine authoritative combat outcomes.
2. **The scheduler owns when work executes.** Movement validation, ECS updates, combat actions, cast/periodic events, AI decisions and lifecycle maintenance must consume agreed scheduling phases and frequencies. A system may use an event/deadline or lower-rate schedule, but must not invent independent inconsistent elapsed-time semantics.
3. **The event boundary owns ordering.** Main-thread game state and ECS buffers must not be mutated unsafely during iteration or from Jobs. Commands/callbacks cross through a documented ordering/commit boundary; preserve product-defined combat/timer effects.
4. **Overload is explicit.** Bounded catch-up, skipped presentation work, deadline misses and backlogs are measured and classified. Authoritative events may not be silently discarded solely to sustain apparent frame rate.
5. **Wall-clock time has a separate owner.** Authoritative UTC/server timestamps support offline expiry, disconnect grace, persistence and scheduled live content; simulation delta cannot stand in for durable elapsed real time, and the client clock never grants expiry privileges.
6. **No bitwise determinism claim.** Stable within-server ordering and reproducible benchmark workloads do not imply deterministic physics or bit-identical simulations across machines.

**Not yet selected:** Fixed tick frequency, maximum catch-up work, AI decision intervals and server overload policy. Stage 1.1 must choose implementation parameters against Stage 1.0 workload measurements while preserving PDD-defined timing behaviour.

## 4. Actor, gameplay and network identity

An actor's identities have distinct owners and lifetimes:

| Identity | Authority and lifetime contract | Current evidence / limitation |
| --- | --- | --- |
| AccountID / CharacterID | Persistent account/character identity; database is the eventual durable authority. Account, session and selected character must remain distinct. | Persistence PDD requires these; current `PlayerDatabase` and `PlayerAccountData` do not yet constitute the final SQL identity model. |
| Gameplay session | Server-owned login/character-control continuity, potentially surviving transport disconnection. | `ServerConnectionManager` currently indexes player sessions by PurrNet `PlayerID` and removes them on leave. |
| Network connection / `PlayerID` | PurrNet connection identity; **not** a durable character key and never the sole key for continued combat state. | Used in connection and actor-owner maps today. |
| Runtime actor ID | Per-spawn/session game-world actor identity; server owns uniqueness and actor lifecycle. | `Actor.ID` is `System.Guid`, assigned/initialised in `Actor.Initialize`; it is not automatically a persistent `CharacterID`. |
| ECS `Entity` | Process-local handle for server simulation representation; never persisted as a character identity or sent as a durable ID. | Created in `ServerActorManager.OnActorSpawned`; no complete reverse lifecycle/reconciliation path is established. |
| PurrNet `NetworkIdentity` | Transport-visible replicated representation, with PurrNet spawn/despawn/ownership rules. Its presence does not define whether gameplay state exists. | Instantiated in `ServerSpawnManager.Update` and located in `global_actors`. |
| Spawn point `GUID` | Authoring/runtime respawn association, not actor lifetime or character identity. | `ServerSpawnManager` maintains a spawn-point/actor map. |
| PurrNet `SceneID` / Unity `Scene` | Loaded content/transport scene instance identity, not logical world, map or gameplay instance identity. | `ActorComponent.CurrentScene` currently stores a `SceneID`. |

**Required contract:** One server actor-lifecycle authority reconciles its gameplay identity, ECS representation and PurrNet identity through spawn, ownership transfer, visibility change, dormancy, reconnect, despawn and shutdown. Exactly one authoritative runtime actor may represent a selected character in one gameplay session. Representation destruction must not implicitly erase persistent or still-live authoritative state.

**Stage 1.3 implementation decision left explicit:** The exact physical owner of the actor-ID-to-Entity mapping and the authoritative data store for per-actor position (ECS component versus server-owned spatial/domain state) is to be resolved by Stage 1.3. Whichever is chosen must be a **single writable authority**, not parallel independent stores. Defining that ownership now does not require introducing a new class or demanding a full ECS rewrite.

## 5. Position ownership and synchronisation

### 5.1 Required state ownership matrix

| State | Current writer / reader | Intended writer of authority and synchronisation direction | Lifetime and invariant |
| --- | --- | --- | --- |
| `Actor.WorldPos` (`WorldPosition` doubles) | `ServerSpawnManager.Update` initialises it; `ServerActorManager.OnActorSpawned` reads it. Other runtime code may also consume it; no continuous one-writer rule is established. | Server spatial-authority path owns the **absolute** value; where `Actor.WorldPos` remains as an exposed view, it is updated **from the accepted authoritative position**, never from an unrelated client/local-origin transform. | Survives origin/scene changes; where required, saved with logical world/map/instance context. Must match authoritative ECS view after the agreed commit boundary. |
| `ActorComponent.WorldPositionD` (`double3`) | Initially written from `Actor.WorldPos`; `ServerActorManager.SyncPlayerECS` copies player `Transform` coordinates; `LateUpdate` copies NavMeshAgent NPC transforms; `ActorUpdateSystem.OnUpdate` writes ECS motion state. | Exactly one server-approved motion/position update commits the double-precision authoritative value for each actor. ECS is either that canonical storage or a synchronised projection, chosen explicitly in Stage 1.3; **never both independent authorities**. | Process-local entity lifecycle; removed/recreated with entity. Not a durable persistence identifier; invariant under client origin shift. |
| Server `Actor.transform.position` (`Vector3`) | PurrNet owner-authoritative movement updates player transform; enabled NPC `NavMeshAgent` drives its transform; ECS gravity writes other NPC transforms in `ServerActorManager.LateUpdate`. | Presentation/physics/NavMesh or received movement **candidate** in server-local coordinates; server validation/reconstruction precedes committing authoritative world state. After accepted movement, project canonical position back to Unity space where needed. | Exists only while GameObject is instantiated; float transform must not be treated as globally authoritative, particularly across wide-world positions. |
| PurrNet owner movement / `NetworkTransform` state | Client sends movement according to owner authority; server reads resulting transform and may issue `MovementEndpoint.Client_ServerCorrection`. | Client remains responsive and can propose/predict motion; **server validation owns legality**. Accepted movement updates authoritative spatial state, then relevant network state/interest; corrections originate on server. Use supported PurrNet facilities, not a second competing transport. | Connection/visibility lifetime. A rejected candidate does not modify committed authoritative position, gameplay range or cross-instance membership. |
| Client `Transform.position` / floating origin | `ClientPositionManager.SetOriginChunk`, `ShiftSceneRoots` and `NetworkToLocal` manage shifted local positions. | Client-local projection from absolute authoritative coordinate and that client's origin. Outgoing movement must carry enough origin/cell context for absolute reconstruction; client shifts never modify server absolute position. | Scene/presentation lifetime; a local origin shift is not gameplay motion or a teleport. |

The **authority** in the table is the *server-accepted absolute spatial state*, not an assertion that the present `WorldPositionD` writes all satisfy it. The current server transform-to-`double3` bridge can lose intended absolute-coordinate semantics after origin/cell changes; Stage 1.3 must reconcile storage ownership, and Stage 1.2 must consume that authoritative output.

### 5.2 Direction by actor/path

| Path | Intended information flow | Validation and ownership constraint |
| --- | --- | --- |
| Player input | Client local simulation → owner movement proposal with spatial context → server legal-movement check → committed absolute position → ECS/spatial index/replication projections. | Client authoritative over responsiveness, **not** server gameplay truth; speed, mode, spatial context, status and world/instance changes validated server-side. |
| NavMesh-driven NPC | Server navigation result → authorised absolute-position commit → ECS view, spatial index and replicated transform. | Unity NavMesh/Physics is a motion provider, not a separate gameplay-distance authority. |
| ECS-driven NPC | Scheduled server ECS motion → absolute-position commit → GameObject/replication where materialised. | No second `LateUpdate` writer may overwrite newer authoritative ECS results. |
| Teleport / forced movement / world transfer | Authoritative action validates destination and context → atomic logical-context and position transition → update spatial memberships/visibility and relevant scene/actor representations. | No intermediate externally visible state in an invalid or old instance; PDD owns valid fallback behaviour. |
| Client floating-origin correction | Absolute position + new local origin → derived client scene/actor `Vector3` shifts only. | No effect on authoritative distance, ownership, timers, simulation or membership. |

## 6. Spatial coordinate and runtime-context model

**These concepts are not interchangeable:**

| Concept | Owner and meaning | Current value / future requirement |
| --- | --- | --- |
| Absolute world position | Server-authoritative `WorldPosition`/double-precision coordinates within an identified logical map/world context. | Already represented by `WorldPosition` and ECS `double3`; coordinate-use correctness incomplete. |
| **Origin cell** | Position encoding and client floating-origin projection. | World Runtime PDD: **100 × 100 m** horizontally; `WorldPosition.Chunk` currently uses `GameConfigurationManager.Config.ChunkSize`. |
| **World tile** | Streamed environment/terrain authored region, independent of origin cell. | World Runtime PDD: **256 × 256 m** terrain tiles; some current runtime `World_X_Y` chunk handling still conflates concepts. |
| **Spatial-index cell** | Internal broad-phase search bucket for combat, perception and network relevance. | **Not implemented as the shared authoritative index.** Cell size/configuration Stage 1.2, independent of both 100 m/256 m grids. |
| Authored scene asset | Reusable `.unity` scene/content from Addressables. | Existing `World_X_Y` and `global_actors` names are asset/runtime conventions, not instance identities. |
| Loaded additive scene / `SceneID` | Specific Unity/PurrNet loaded scene, delivering content and/or holding representations. | Currently used by `ServerWorldManager` and scene subscriptions; may be reused across logical contexts only with correct separate identity. |
| **World** | Explicit persistent player-selectable world (`World 32` in PDD example). | World Runtime PDD defines concept; not equivalent to a process or scene. |
| **Map** | Independent spatial coordinate space within world/runtime context. | Map ID must not be inferred solely from numerical coordinates. |
| **Runtime instance** | Isolated gameplay simulation context sharing authored assets if appropriate. | Instance ID and isolation not completed; not inferred from `SceneID` or scene asset name. |

**Required query/context rule:** Every spatially relevant actor/query/replication operation first checks compatible **world + map + runtime instance** according to the World Runtime PDD, then local spatial candidates, then an exact authoritative double-precision range/line-of-sight check where required. Identical coordinates in two private instances never imply visibility, targeting, aggro or combat interaction. A player may share authored scene assets with players in other contexts without sharing simulation state.

**Spatial indexing authority (Stage 1.2):** A server-owned spatial membership service maintains actor current region and context; publishes insertion, accepted movement, removal, teleport/transfer and context teardown to consumers. AI, combat, interaction and replication must use the same qualified spatial source rather than independent global scans or a spawn-scene string. World/environment tile subscriptions remain a separate delivery mechanism. The existing `ServerInterestManager` changed-chunk targeted reevaluation is a useful retained starting point; do not falsely characterise it as an all-actor scan every interest update.

**Open implementation values:** Spatial index structure/cell size, actor visibility range, enter/exit hysteresis values and independent environment scene radii. The PDD fixes neither actor range nor index dimensions; Stage 1.2 must select/tune implementations without treating 100 m or 256 m as mandatory index-cell dimensions.

## 7. Replication and client/server authority

- **Server/gameplay** owns whether an actor or action exists, which simulation instance it belongs to, what effects resolve, and which client is eligible to receive its authoritative state.
- **PurrNet** owns transport connection and network-identity lifecycle; the project-owned integration determines relevance within PurrNet's supported visibility/replication APIs. Do not implement a parallel networking stack.
- **Client** owns presentation, rendering, UI, local prediction and movement proposal, never authoritative ability targets, aura/timer expiry, persistent state or visibility permissions.
- **Interest** uses context compatibility → regional candidate filtering → precise range → gameplay exceptions defined by their owning PDDs. Environment scene membership alone is insufficient for final actor visibility.
- **Relevance changes** must update subscriptions/identity visibility without stale recipients or cross-instance leakage. Entry/exit hysteresis may be used when tuned (World Runtime PDD).
- **Phase 1.5** specifies update-priority/frequency, available PurrNet bandwidth counters and replication-budget behaviour. This document establishes ownership **only**; it does not claim prioritised replication or network LOD currently exists.

Existing `ChunkVisibilityRule.CanSee` currently tests `CurrentChunk` scene-subscription membership, and `ServerInterestManager.OnInterestRequest` processes client-provided centre-chunk requests. Neither is by itself a guarantee of authoritative absolute position or logical-instance isolation. Normal movement must eventually drive server-approved world-tile and actor-interest updates (Movement PDD).

## 8. Lifecycle contracts and sequence diagrams

The following are **logical responsibility diagrams**, not existing call-graph or new API declarations. Each is labelled with the observed current path and the missing stage contract.

### 8.1 Spawn and first representation

```text
[CURRENT, observed]
PurrNet additive scene becomes loaded
  -> ServerSpawnManager.OnPostSceneLoaded finds authored ActorSpawnPoint
  -> WorldSpawnSystem emits SpawnCommand
  -> ServerSpawnManager.Update instantiates via PurrNet in global_actors
  -> Actor gets initial WorldPos, ID, Owner, CurrentChunk
  -> ChunkSubscriptions registers NetworkIdentity / player association
  -> onServerActorSpawned
  -> ServerActorManager.OnActorSpawned creates ECS ActorComponent + buffers

[REQUIRED, later stages 1.2-1.4]
Authoritative spawn request + resolved world/map/instance/context
  -> validate definition, spawn ownership, location and instance
  -> create ONE actor identity + authoritative state
  -> insert spatial membership and ECS representation as required
  -> materialise GameObject/NetworkIdentity when relevant
  -> expose to authorised observers only after state is complete
```

**Invariant:** Spawning the same actor through multiple paths cannot create duplicate authoritative state. A spawn point is not actor identity. Authored environment scenes may define spawn locations but need not own active actor lifetime.

### 8.2 Normal player movement and region change

```text
[CURRENT, observed]
Client movement -> owner-authoritative PurrNet NetworkTransform
  -> server Actor.transform.position
  -> ServerActorManager.ValidatePlayerPositions (periodic float speed check)
  -> possible Client_ServerCorrection RPC
  -> ServerActorManager.SyncPlayerECS (float Transform -> double3)

Client-side chunk interest request
  -> MovementEndpoint.Server_RequestChunkInterest
  -> ServerInterestManager.OnInterestRequest
  -> PurrNet scene subscriptions / chunk visibility re-evaluation

[REQUIRED, stages 1.1-1.3 / 1.5]
Client movement + absolute position context
  -> server validates and accepts/rejects movement
  -> one absolute double-precision position commit
  -> ECS representation + spatial membership update
  -> world-content interest update (only if tile relevance changed)
  -> actor replication interest update (independent of scene interest)
  -> visible client representations / correction where needed
```

**Invariant:** Local floating-origin changes and unrelated streamed-tile boundaries cannot make an actor move, change logical instance or bypass range validation.

### 8.3 Environment scene unload with continuing actors

```text
[CURRENT, observed]
Player interest region changes or player unsubscribes
  -> ServerInterestManager updates subscriptions
  -> ServerWorldManager.UnloadChunk -> PurrNet scene unload
  -> actors instantiated in global_actors do not reside in that tile scene

[REQUIRED, stages 1.2 / 1.4 / 1.6]
Last content subscriber removed
  -> settle/abort pending tile scene load where possible
  -> unload environment scene and release its resource handles
  -> preserve authoritative actors independent of scene residency
  -> independently evaluate actor activity and replication relevance
  -> clean content scene/identity registries without orphan references
```

**Invariant:** Unloading an environment tile cannot alone delete an active persistent actor or its durable combat/session obligations. PDD leaves NPC despawn/respawn policy to the relevant NPC/AI system.

### 8.4 Actor despawn versus session termination

```text
[CURRENT, observed]
ServerConnectionManager.OnPlayerLeft
  -> save account JSON snapshot
  -> NetworkIdentity.Despawn
  -> remove session/connection maps
Actor.OnDestroy (server)
  -> ActorEndpoint.onServerActorDespawned
  -> ServerSpawnManager clears matching spawn-point association

[REQUIRED, stages 1.3 / 1.4 / 1.7 / 1.8]
Authoritative lifecycle decision (not simply loss of network connection)
  -> resolve combat/timers/persistence ownership
  -> remove actor from observer interest and spatial index
  -> remove/retire ECS state and stale actor-ID mappings
  -> despawn PurrNet identity / release presentation object if present
  -> checkpoint durable state at defined safe boundary
  -> release subscriptions, event handlers, handles and caches
```

**Current gap:** `ServerActorManager` subscribes to spawn/owner/interest events, not `onServerActorDespawned`; no complete ECS/actor-map cleanup is shown by that manager. This is a lifecycle correctness gap for the later stages, **not** a change authorised by Issue #41. Identity destruction cannot be used to infer that all ECS/interest state was safely removed.

### 8.5 Enter or leave a private runtime instance

```text
[REQUIRED ONLY — logical instance transitions are not complete today]
Authoritative transition request or world boundary reached
  -> validate destination world/map/instance and entry eligibility
  -> ensure required simulation context and streamed destination ready
  -> commit logical context + double position together
  -> leave old spatial/replication audiences
  -> enter destination spatial/replication audiences
  -> deliver content/actor state through PurrNet scene/identity handling
  -> unload old content only when no longer required
Failure before commit -> remain in valid previous context / PDD fallback
```

**Invariant:** Equivalent coordinates or identical authored Unity scenes must not mix two private instance populations. Destination admission must not partially complete when content or simulation initialisation fails. Instance reuse/return and transition-specific player-facing policy stay with the World Runtime and Persistence PDDs.

### 8.6 Unexpected disconnect and reconnect

```text
[CURRENT, observed]
PurrNet OnPlayerLeft
  -> account saved
  -> player NetworkIdentity despawned
  -> player session removed
Reconnect creates a subsequent login/world-entry path

[REQUIRED BY PERSISTENCE PDD — stages 1.7 / 1.8]
Network connection lost
  -> gameplay session + authoritative actor retained (>= 30 seconds)
  -> combat, damage, auras, cooldowns and death continue as required
  -> reconnecting account proves session/CharacterID ownership
  -> if actor still exists: rebind connection/NetworkIdentity authority
     to SAME authoritative actor, send current state, no duplicate
  -> if actor legitimately saved/despawned: reconstruct saved state
  -> unresolved combat cannot be escaped by waiting out grace
  -> server restart uses durable checkpoint and elapsed-time policy
```

**Invariant:** PurrNet `PlayerID` is not the persistent actor identity. The PDD explicitly requires a **minimum 30-second** unexpected-disconnect grace period and continuing combat beyond that minimum where necessary; no implementation value should weaken it.

## 9. Runtime resource ownership and cleanup

| Resource | Ownership contract | Acquisition / release triggers | Current baseline or deferred work |
| --- | --- | --- | --- |
| Server ECS `World` and systems | Server runtime composition owns creation, update attachment, orderly disposal and job completion. | Process/world initialise → shutdown/reinitialise; no duplicate registration or leaked player-loop attachment. | `ServerECSManager` creates/attaches world; complete shutdown and scheduler lifecycle Stage 1.1/1.3. |
| ECS actor components/buffers | Server actor-lifecycle authority owns entity creation/retirement in step with actor identity. | Spawn, materialisation, activity transition, teardown. | `ServerActorManager.OnActorSpawned` creates; corresponding complete despawn path not established. |
| Server actor state, session and persistent character | Authoritative game session/actor and persistence boundaries; network disconnection alone cannot erase state. | Login/spawn → safe logout/despawn/restart; reconnect may retain same actor. | `ServerConnectionManager` immediate player cleanup and `PlayerDatabase` JSON snapshot are provisional. |
| PurrNet `NetworkIdentity` and owner | Supported PurrNet lifecycle; project-owned integration tracks logical actor and audience. | Actor becomes network-visible → despawn/no longer represented; reclaim for reconnect where allowed. | Created by `ServerSpawnManager`; Stage 1.5/1.8 relevance and reconnection. |
| Spatial subscriptions/index membership | Server spatial authority and project PurrNet visibility adapter; no stale membership after movement/despawn/context transfer. | Spawn, accepted move, teleports, instance transition, actor retirement. | `ChunkSubscriptions` and `ServerInterestManager` track scene interest; shared index Stage 1.2. |
| Environment additive scenes | `ServerWorldManager` / PurrNet scene module own load/reference/scene membership; client `ClientWorldManager` tracks delivered scenes. | Subscriber/streaming relevance enters/leaves; async cancellation and teardown accounted for. | `EnsureChunkLoaded` / `UnloadChunk` exist; robust in-flight cleanup Stage 1.6. |
| Client floating-origin scene roots | Client position/world managers own local transform shifts, not absolute simulation state. | Origin change, newly loaded scene, scene release. | `ClientPositionManager` and `ClientWorldManager` implemented in prototype form. |
| Addressables loads, catalogs and asset handles | The initiating project-owned resource consumer owns each handle or a documented shared-ownership count; PurrNet-owned scene handles obey supported PurrNet APIs. | Acquire/load → final consumer gone/unload; no leak or premature release. | Actual handle lifecycle must be audited and instrumented in Stages 1.0 and 1.6, not assumed correct now. |
| Effects, UI, audio, pooled representations | Client presentation owner manages allocations, subscriptions, reuse/reset and destruction. | Visible/relevant use → completion/teardown/logout. | Resource budgets and pooling policy Stage 1.6. |
| Persistent timestamps and data writes | Authoritative server clock and persistence component; never client clock. | State mutation/checkpoint/logout/offline progression and release transitions. | Database and timed-state migration Stage 1.7–1.8. |

**Disposal rule:** Each lifetime must have one explicit owner and a verifiable release path; **GC or scene destruction are not substitutes for releasing persistent subscriptions, entity data or Addressables handles**. Shared resources require explicit ownership accounting. Errors fail observably rather than silently placing actor or scene state into an invalid partial lifecycle.

## 10. Stage hand-offs and expected integration surfaces

This table is a **responsibility specification**, not a new named C# API declaration.

| Stage | Intended integration responsibilities | Not presently claimed |
| --- | --- | --- |
| **1.0** | Define ownership/context contracts, stress profiles, measurable invariants, baseline instrumentation/evidence. | No gameplay optimisation or benchmark result is implied by this document. |
| **1.1** | One authoritative clock and phase scheduler feeding existing ECS systems and server managers; simulated vs UTC time semantics; overload/catch-up contracts. | No fixed-tick authoritative scheduler yet. |
| **1.2** | World/map/instance-qualified double-precision spatial membership, query and visibility candidate interface; event-driven insert/move/remove/context transfer; independent world-content streaming interest. | `ChunkSubscriptions` is not a fully general spatial index; no range-qualified instance isolation yet. |
| **1.3** | Exact single-writer per-field ownership, actor ID ↔ ECS Entity mapping, batched managed/ECS bridge, job/main-thread boundaries, safe teardown. | GameObject/ECS mirror is not yet governed by this contract. |
| **1.4** | Relevance/activity states, scheduling of NPC perception and decisions, sleep/wake continuity, representation demand and activation spikes. | Dormant/near/engaged simulation tiers not currently established. |
| **1.5** | PurrNet-supported observer policy, prioritised update frequency, bandwidth/accounting and correct instance/range observer set. | Existing chunk visibility is not a network budget/priority implementation. |
| **1.6** | Content streaming and renderer LOD/culling contracts, resource/Addressables lifetime budgets, client pooling policies and origin-shift performance. | No guarantees of GPU/object throughput without measurement. |
| **1.7–1.8** | Transactional persistent actor/character identity, timestamps, session restoration and reconnect-safe actor lifetime. | Current JSON and immediate disconnect/despawn do not meet PDD continuity requirements. |
| **1.9–1.11** | Immutable content/version compatibility, operational metrics/CI and integrated stress sign-off on documented hardware. | No production capacity or performance guarantee is established here. |

Implementations should provide a direct project-owned integration against existing Managers/Endpoints where practical; do not create a generic message bus, new network transport or speculative multi-process sharding framework. If a future measurement demonstrates a different internal owner is necessary, document and validate the change **without altering PDD-owned player behaviour**.

## 11. Measurable invariants and benchmark evidence

Stage 1.0 issue [#42](https://github.com/FrontierDev/ninthage/issues/42) and subsequent benchmark issues must turn these invariants into measurable tests. They are **required outcomes**, not claimed current test results:

| Invariant | Evidence/metric |
| --- | --- |
| Single logical actor and one legal network owner per active character/session | Actor IDs versus CharacterIDs, session/connection counts, duplicate detection during spawn/reconnect. |
| Consistent authoritative absolute position | Error between committed double-position views; accepted/rejected movement events; negative-coordinate and boundary crossings. |
| Floating-origin and world-tile independence | Origin shift count, distinct tile load count, stable absolute position/instance and unchanged combat range during shifts. |
| Instance isolation | Cross-instance candidates, visibility recipients and forbidden target/damage events = **zero** under overlapping coordinates. |
| Bounded relevance queries | Eligible local candidate count, examined candidates and query time versus total server population across density sweeps. |
| Scheduled authoritative work correctness | Expiry/tick/command counts, update order and missed/deferred event counts, with backlog explicitly **unavailable until Stage 1.1**. |
| Dormancy without state exploits | Active/near/dormant population and wake-up state/timing; **unavailable until Stage 1.4**. |
| Actor/scene/handle teardown | Spawn/despawn balance, ECS entity counts, stale memberships, memory retention after unload/churn, Addressables handle release evidence where observable. |
| Legitimate reconnect continuity | Same authoritative actor/character and continuing combat/timers during required grace; current implementation is known to lack the PDD behaviour. |
| Network relevance and limits | Visibility decisions by world/map/instance/range, bytes/messages per relevant observer where supported, explicitly unavailable transport counters otherwise. |
| Streaming/renderer stability | Client p50/p95/p99 frame time, activation/unload stalls, draw/batch/GPU samples on specified hardware, memory/GC trends. |

**Measurement policy:** Every result must identify Git commit, Unity/PurrNet/package versions, build mode, graphics settings, hardware, workload definition/seed, achieved player/actor counts, sample count, test duration and whether each metric is measured, estimated, not applicable or unavailable. Instrumentation must not fabricate zeros or equate architecture-readiness with tested production capacity.

## 12. Open decisions, omissions and validation gate

The following **remain open on purpose**:

- **Stage 1.1:** Tick rate, simulation budget, detailed ordering, overload/catch-up policy. Persistent/offline timers remain governed by the Account/Persistence PDD.
- **Stage 1.2:** Spatial-index strategy/cell size, range/hysteresis thresholds, concrete world/map/instance identifiers, membership event API, scene interest radius. These are not fixed by the World Runtime PDD.
- **Stage 1.3:** Canonical physical storage for accepted absolute position, per-actor ECS mapping implementation, managed/Jobs/Burst boundaries, entity representation under dormancy.
- **Stage 1.4:** AI decision frequencies and simplified simulation detail; AI PDD explicitly leaves numeric update frequencies unspecified.
- **Stage 1.5–1.6:** PurrNet-supported replication features, per-connection budgets, asset LOD thresholds, streaming concurrency and pooling strategy.
- **Stages 1.7–1.8:** Database decomposition/checkpoint cadence, session duplicate-login UX, exact cleanup sequencing and valid instance-reentry policy as assigned to the relevant PDDs.
- **Other product decisions:** Combat tuning, instance scope, NPC respawn policy, housing/dungeon UX and invalid-location recovery must follow their owning PDDs. Technical implementation cannot silently decide them.

### Acceptance for Issue #41

- [ ] Architecture owner reviews and approves the present/proposed boundaries and identity matrix.
- [ ] Cross-check current-source references at the implementation head of `dev` (source may advance).
- [ ] Verify 100 m origin cells and 256 m world tiles remain separate; no scene is used as world/map/instance identity.
- [ ] Verify spawn, movement, scene unload, actor despawn, instance transfer and reconnect diagrams explicitly distinguish current from required behaviour.
- [ ] Resolve/document the missing Phase 0.40 baseline artifact before treating Phase 0 documentary acceptance as verified.
- [ ] Review this sole Markdown addition and ensure **no runtime code, tests, scene/prefab assets, Unity `.meta`, vendor files, configuration or PDDs changed**.
- [ ] Only after manual approval remove `needs-validation` and permit dependent Stage 1.0 implementation issues.

**No Unity compile, Play Mode, dedicated-server run, automated regression run or performance measurement is asserted by this documentation change.**
