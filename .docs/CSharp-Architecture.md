# Ninth Age — C# Architecture Design

**Status:** Current architecture reference  
**Repository:** `FrontierDev/ninthage`  
**Branch:** `dev`
**Source revision:** `7afed7bd70be84a170a2e6694874c793a016b3c3`
**Snapshot date:** 2026-10-27
**Documentation contract updated:** 2026-10-27 (Stage 1.5 networking-foundation ownership and pressure-query boundary)
**Unity version:** 6000.3.10f1

## 1. Purpose and Scope

This document describes the current C# architecture of the Ninth Age Unity project. It records how the codebase is organised, how the principal assemblies relate to one another, how client and server execution are selected and initialised, and how gameplay data, runtime actors, networking, server simulation, client presentation, world streaming, persistence, and editor tooling are divided across the project.

This is the current design reference for the code that exists at the source revision above. It is descriptive of the implementation, but the ownership, authority, lifecycle and simulation-boundary contracts recorded here are normative for changes to that implementation until explicitly revised. It does not by itself authorize a future refactor.

**Mandatory third-party boundary:** Third-party code is read-only unless the user explicitly requests a change to the specific third-party code. This includes imported packages, plugins, SDKs, tools, and externally authored source anywhere in the repository (including `Assets/` and `Assets/Editor/`). **Never modify third-party namespaces**, identifiers, files, assembly definitions, metadata, or vendor-owned assets. Do not restyle, rename, relocate, patch, reformat, or regenerate them. Follow `AGENTS.md`; if the task requires a third-party change, stop and request explicit permission. GitHub issues, plans, general refactor requests, and coding standards do not grant that permission.

References to third-party dependencies in this architecture are descriptive only and do not authorize editing their implementation or namespace.

## 2. Architectural Overview

Ninth Age is currently implemented as a single Unity project that can run in two principal execution modes:

- a graphical game client;
- a headless authoritative World Server.

**Approved future deployment contract (not claimed as implemented at this source revision):** each persistent world runs in exactly one World Server process on one physical machine, with all maps and public/private instances inside that process. Distinct worlds run on distinct World Server machines. A separate Master Server service owns global world registration, account/character persistence, authentication, session/admission and inter-world coordination, but performs no gameplay simulation. The current JSON/local-session prototype does not yet provide that service; Stages 1.7–1.8 establish its foundation and Phase 3 implements multiple concurrent world machines.

The C# code is separated into client, server, shared, core bootstrap, and editor concerns. The high-level dependency flow is:

```text
                         Game.Core
                      GameBootstrapper
                       /           \
                      /             \
             Game.Client         Game.Server
            client runtime       server runtime
            input / UI           authority / simulation
            presentation         persistence
                 \                 /
                  \               /
                    Game.Shared
               game definitions
               runtime actor model
               networking contracts
               shared utilities

                    Game.Editor
               data authoring tools
                 world authoring
```

The project is therefore neither a completely duplicated client/server codebase nor a server-only simulation with a thin presentation shell. Common game concepts are represented in `Game.Shared`; the two runtime sides build different responsibilities around those shared concepts.

The runtime also uses a hybrid GameObject/MonoBehaviour and Unity Entities model. Network-visible actors are represented by PurrNet `NetworkBehaviour` GameObjects, while selected authoritative simulation state is mirrored into server ECS entities and buffers for system-driven ticking and processing.

### 2.1 Numeric Representation Boundaries

The project-wide numeric-type selection guidance (including size, precision, and memory costs) is defined in [C# Style and Naming Conventions](CSharp-Style-Conventions.md#24-numeric-types-and-precision). The architectural boundaries are:

- **Authoritative world positions:** use double precision (`double`/`double3`) for absolute coordinates; use float precision (`float`/`float3`/`Vector3`) for local Unity rendering and physics. See Sections 15–16.
- **Authoritative time:** represent simulation identity and due deadlines as integer `long` ticks; use the server clock's fixed delta and higher-precision elapsed-time calculations only where needed. Authored `float` durations are converted once to tick deadlines under the approved policy; see [Performance and Simulation Architecture](Performance-and-Simulation-Architecture.md#33-one-clock-owner-identity-lifecycle-and-configuration--proposed).
- **Discrete and fractional gameplay values:** prefer exact integer types (`int` or `long` as range requires) for counts and IDs, and `float` for fractional quantities where the existing gameplay schema defines them. Do not silently change resource/stat units or balancing formulas.

These are type-selection boundaries, not a mandate to rewrite existing serialized fields, ECS components, persistence formats, or network contracts.

## 3. Source Layout

At this revision, the project contains 451 C# files under `Assets`.

| Area | C# files | Role |
|---|---:|---|
| `Assets/Runtime/Client` | 140 | Client execution, presentation, input, UI and client world state |
| `Assets/Runtime/Server` | 50 | Authoritative server managers, services, ECS systems, persistence and server test helpers |
| `Assets/Runtime/Shared` | 137 | Shared definitions, runtime objects, networking and utilities |
| `Assets/Runtime/Core` | 1 | Process bootstrap and client/server routing |
| `Assets/Editor` | 79 | Unity editor extensions and world/data authoring tools |
| `Assets/Tests` | 44 | Project-owned EditMode/PlayMode/performance test infrastructure |
| **Total** | **451** | |

The principal source layout is:

```text
Assets/
├── Editor/
│   ├── Components/
│   ├── Data/
│   ├── DialogueEditor/
│   ├── Drawers/
│   ├── EDEN_ErosionTools/
│   ├── Inspectors/
│   ├── Windows/
│   ├── WorldEditor/
│   └── Editor.asmdef
│
├── Tests/
│   ├── EditMode/
│   ├── PlayMode/
│   └── Performance/
│
└── Runtime/
    ├── Client/
    │   ├── Core/
    │   ├── Shaders/
    │   ├── Systems/
    │   ├── UI/
    │   ├── Utility/
    │   ├── VFX/
    │   └── Client.asmdef
    │
    ├── Core/
    │   ├── GameBootstrapper.cs
    │   └── Core.asmdef
    │
    ├── Server/
    │   ├── Core/
    │   ├── Networking/
    │   ├── Persistence/
    │   ├── Services/
    │   ├── Systems/
    │   ├── Test/
    │   └── Server.asmdef
    │
    ├── Shared/
    │   ├── Components/
    │   ├── Data/
    │   ├── Movement/
    │   ├── Networking/
    │   ├── Persistence/
    │   ├── Runtime/
    │   ├── Terrain/
    │   ├── Utility/
    │   └── Shared.asmdef
    │
    └── Runtime.asmdef
```

## 4. Assembly Architecture

### 4.1 `Game.Core`

`Game.Core` is the bootstrap assembly. Its current C# implementation consists of `GameBootstrapper.cs`.

Its role is to determine the execution context and transfer control to the appropriate runtime side. The bootstrapper persists across scene changes and chooses between:

- `Game.Client.Core.ClientInitialization.Begin(...)`; or
- `Game.Server.Core.ServerInitialization.Begin(...)`.

The server path is selected when Unity is running in batch mode or when no graphics device is present. The bootstrapper also exposes an editor-oriented `forceServerMode` switch for server testing.

`Game.Core` references the shared, client, and server assemblies so that it can dispatch into either runtime.

### 4.2 `Game.Shared`

`Game.Shared` contains the C# types that form the common language of the game. Its responsibilities include:

- authorable game definitions;
- runtime actor and item state;
- spell and aura runtime contexts;
- common persistence DTOs;
- endpoint/callback surfaces and RPC contracts;
- world-position and chunk-related types;
- shared movement mathematics;
- common calculations and utilities.

The `Shared` source tree uses folder-derived namespaces under `Game.Shared`, including `Game.Shared.Data`, `Game.Shared.Networking`, `Game.Shared.Persistence`, `Game.Shared.Runtime.*`, `Game.Shared.Terrain` and `Game.Shared.Utility`.

Both `Game.Client` and `Game.Server` build on the types in this assembly.

### 4.3 `Game.Client`

`Game.Client` contains code that only participates in the graphical client runtime. Its folder-derived namespaces include `Game.Client.Core`, `Game.Client.UI.<Feature>`, `Game.Client.Systems`, `Game.Client.Utility`, `Game.Client.VFX` and `Game.Client.Shaders`. It owns:

- client networking setup;
- client world and scene handling;
- input;
- camera control;
- client combat/event presentation;
- VFX and audio presentation;
- character selection and creation controllers;
- the client UI framework and feature windows;
- client-side floating-origin management;
- the current client ECS world.

### 4.4 `Game.Server`

`Game.Server` contains authoritative runtime responsibilities, with folder-derived namespaces such as `Game.Server.Core`, `Game.Server.Networking`, `Game.Server.Systems`, `Game.Server.Persistence`, `Game.Server.Services` and `Game.Server.Test`, including:

- connection/session handling;
- authoritative actor tracking;
- world and chunk scene loading;
- interest management;
- spawning;
- stats, spell casting, auras, cooldowns, tickers and auto-attacks;
- server services for authentication and character operations;
- server-side persistence;
- the server ECS world and systems.

### 4.5 `Game.Editor`

`Game.Editor` is restricted to the Unity Editor. It contains custom inspectors, property drawers, data-definition authoring windows, dialogue tooling, terrain/world tooling, erosion tooling, and other editor-only utilities.

The assembly uses the shared data/runtime definitions so that editor tools operate on the same asset types consumed by the game.

### 4.6 `Game.Runtime`

`Assets/Runtime/Runtime.asmdef` defines the parent runtime assembly boundary. The substantive source files beneath `Client`, `Core`, `Server`, and `Shared` are captured by their own nested assembly definitions.

## 5. External C# Dependencies

The Unity project version is 6000.3.10f1. Packages that materially shape the current C# architecture include:

- Unity Addressables 2.9.1;
- Unity Entities 1.4.5;
- Unity AI Navigation 2.0.10;
- Unity Input System 1.18.0;
- Cinemachine 3.1.6;
- Universal Render Pipeline 17.3.0;
- Unity Mathematics 1.3.3;
- Unity UI/uGUI 2.0.0;
- Newtonsoft JSON package support.

Networking code uses PurrNet types throughout the runtime, including `NetworkManager`, `NetworkBehaviour`, `NetworkIdentity`, RPC attributes, player IDs, scene IDs, scene modules and transport classes.

## 6. Runtime Bootstrap and Initialisation

### 6.1 Common entry point

`GameBootstrapper` is the common runtime entry point. On `Awake` it:

1. enforces a single persistent bootstrapper instance;
2. marks the bootstrap object as `DontDestroyOnLoad`;
3. detects whether the process is a headless server;
4. starts either server or client initialisation.

The two runtime modes then follow separate initialisation pipelines.

### 6.2 Client initialisation

`Game.Client.Core.ClientInitialization` performs client startup as a coroutine. The current sequence is:

```text
GameBootstrapper
    ↓
ClientInitialization.Begin
    ↓
Wait for LoadingScreen
    ↓
Initialise Addressables
    ↓
Load GameConfiguration
    ↓
Create/configure PurrNet NetworkManager for client use
    ↓
Create client ECS World
    ↓
Create ClientWorldManager and client runtime managers/controllers
    ↓
Load MainMenu additively
    ↓
Unload Bootstrapper scene
```

During the manager stage, the client creates a persistent world-manager object and attaches the following components:

- `ClientWorldManager`;
- `ClientPositionManager`;
- `CameraManager`;
- `ClientInputController`;
- `ClientCombatManager`;
- `ClientVFXManager`;
- `ClientAudioManager`.

`ClientConnectionManager` is attached to the instantiated network-manager object.

### 6.3 Server initialisation

`Game.Server.Core.ServerInitialization` performs server startup as a coroutine. The current sequence is:

```text
GameBootstrapper
    ↓
ServerInitialization.Begin
    ↓
Initialise Addressables
    ↓
Load GameConfiguration
    ↓
Create/configure PurrNet NetworkManager
    ↓
Start PurrNet server
    ↓
Create server ECS World and systems
    ↓
Initialise server bindings
    ↓
Initialise player database/auth service
    ↓
Create ServerWorldManager
    ↓
Load global_actors scene
    ↓
Create interest, position, physics, spawn and gameplay managers
```

The gameplay/world manager architecture is:

- `ServerInterestManager`;
- `ServerPositionManager`;
- `ServerPhysicsManager` (continuous server physical-coordinate, physics-query and streamed NavMesh owner);
- `ServerSpawnManager`;
- `ServerActorManager`;
- `ServerStatManager`;
- `ServerSpellCastManager`;
- `ServerAuraManager`;
- `ServerTickerManager`;
- `ServerCooldownManager`;
- `ServerAutoAttackManager`.

### 6.4 Server shutdown ownership

`ServerControlEndpoint` is attached only for the ordinary dedicated-server path and is the local operational request boundary: it binds the loopback control port, parses the bounded shutdown request and hands it to `ServerShutdownController`. It does not own teardown or process state.

`ServerShutdownController` is attached to the persistent `GameBootstrapper` host by `ServerInitialization`. It owns thread-safe request arbitration and the Unity main-thread handoff to one graceful shutdown coroutine. `ServerInitialization` owns authoritative subsystem teardown ordering: it stops the simulation clock, drains PurrNet with a bounded wait, disposes the NetworkManager/ECS/world state and then finalises the profiler. `ServerPerformanceProfiler` owns final report finalisation only. Fatal shutdown uses the same ordered cleanup boundary with a non-zero process exit; Editor forced-server mode tears down without quitting the Editor.

The stable local World Server control contract for the future `ninthage-master` caller is `POST http://127.0.0.1:<control-port>/control/shutdown`, returning `202 Accepted` with `{"status":"shutdown_requested"}`. The Master should launch each managed World Server with an explicit unique control port, retain that port with the child process, use this endpoint for the Worlds dashboard Stop action, wait a bounded time for natural exit, and use `Process.Kill(entireProcessTree: true)` only as the timeout/error fallback. No alternate World Server shutdown protocol should be introduced.

## 7. Shared Configuration Layer

`GameConfiguration` is the master shared `ScriptableObject` for core runtime configuration. It currently contains settings for:

- persistence;
- world chunk size;
- physics and ground detection;
- movement/fall behaviour;
- spell hit/crit configuration;
- global cooldown duration;
- item-quality colours;
- auto-attack spell selection;
- server address and port.

`GameConfigurationManager` owns the process-wide loaded configuration and exposes it through `GameConfigurationManager.Config`. Both client and server initialise this before the major runtime managers are started.

This gives shared code a single configuration source for values such as chunk size, gravity, combat constants and connection settings.

## 8. Shared Data-Definition Architecture

### 8.1 Definition model

The game's authored gameplay data is centred on `Game.Shared.Data.DataDefinition`, an abstract `ScriptableObject` base class. A definition provides common identity fields:

- `DefinitionId`;
- `DisplayName`.

Concrete definitions extend this base type and add the fields needed for a particular game concept.

The current `Shared/Data` area contains definitions for major RPG concepts including:

- actor stats;
- auras;
- classes;
- damage schools;
- factions;
- items;
- loot tables;
- quests;
- races;
- spells;
- talents.

### 8.2 Definition libraries

`DataDefinitionLibrary<T>` is the common typed collection abstraction for definition assets. It provides:

- a serialized list of definitions;
- lookup by definition ID;
- access to all definitions;
- ID existence checks;
- library validation.

Concrete library types exist alongside the relevant definitions, such as spell, item, class, faction, race, talent and aura libraries.

### 8.3 Composable definition subtypes

Several systems represent behaviour through smaller specialised definition classes rather than a single monolithic asset type. Current groups include:

```text
Data/
├── AuraBehaviours/
├── AuraEffectDefinitions/
├── AuraTargetDefinitions/
├── ClassBehaviours/
├── Conditions/
├── Dialogue/
├── QuestObjectives/
├── SpellEffectDefinitions/
├── SpellTargetDefinitions/
└── TalentBehaviours/
```

Examples include separate spell effect definitions for damage, healing, resource effects, aura application and rift placement, and separate target definitions for single-target, cone and ground-location targeting.

The result is a shared asset model in which spell, aura, quest and talent definitions refer to typed behaviour/effect objects that are interpreted by runtime code on the appropriate side.

## 9. Shared Runtime Model

The `Shared/Data` layer describes authored game content. The `Shared/Runtime` layer represents instantiated game state.

```text
Shared/Data
    ↓ definitions/configuration
Shared/Runtime
    ↓ live objects and state
Client / Server systems
```

### 9.1 Actor hierarchy

`Game.Shared.Runtime.Actors.Actor` is the central runtime actor type. It derives from PurrNet `NetworkBehaviour` and represents a network-visible gameplay actor.

Its state and responsibilities include:

- stable actor GUID;
- spawn-point GUID;
- actor name;
- network owner;
- current logical chunk;
- double-precision authoritative `WorldPosition`;
- target selection;
- hitbox/range calculations;
- active aura tracking on the client-facing runtime object;
- faction relationship queries;
- weapon damage/swing information;
- target replication through RPCs;
- actor lifecycle events through the shared networking service layer.

Concrete actor/runtime types in the same area include:

- `PlayerActor`;
- `NPCActor`;
- `NPCBehavior`;
- `NPCStatProfile`;
- `ActorMotor`;
- `ActorSpellcaster`;
- `ActorStatContainer`;
- `ActorSFXController`;
- `ActorVFXController`;
- `PlayerEquipment`;
- `PlayerInventory`;
- `PlayerExperience`;
- `PlayerQuests`;
- `PlayerReputation`;
- `PlayerTalents`.

The player runtime is therefore composed from the shared `PlayerActor` plus specialised components for equipment, inventory, progression, quest, reputation and talent state.

### 9.2 Spellcasting runtime

`ActorSpellcaster` is the shared networked spellcasting component. Spell execution uses shared definition objects together with runtime context types.

`Shared/Runtime/Spellcasting` contains types such as:

- `SpellContext`;
- `AuraContext`;
- `AuraInstance`;
- `CombatLogEntry`;
- `ISpellModifier`;
- `SpellComponentOverrides`;
- `SpellProjectile`;
- concrete spell modifier implementations.

`SpellOverride` represents a runtime-adjusted view of a base `SpellDefinition`, carrying values such as range, cooldown, cast time, tick count, target position and component overrides.

This permits authored spell definitions to act as the base data while runtime systems work with a mutable cast-specific representation.

### 9.3 Item runtime

`Shared/Runtime/Items/ItemInstance.cs` represents instantiated item state separately from authored `ItemDefinition` assets. Inventory and equipment components on player actors operate on these runtime instances.

## 10. Shared Networking Architecture

Networking-facing types used by both runtime sides are grouped under `Game.Shared.Networking`.

The shared RPC and callback surfaces are the following endpoint types:

- `AccountEndpoint`;
- `ActorEndpoint`;
- `CharacterEndpoint`;
- `MovementEndpoint`;
- `StatEndpoint`.

These are Shared Networking endpoint/callback surfaces, not channels. `Channel` remains PurrNet transport/RPC-delivery terminology and is not used as the name of these project endpoint types.

The same namespace also contains login/authentication payloads, the `IPlayerAuthService` abstraction, chunk subscriptions and visibility rules, latency simulation and network-packing structures. `IPlayerAuthService` remains a genuine service interface because it abstracts the server authentication/persistence implementation; `PlayerAuthService` remains its server-side implementation in `Game.Server.Services`.

### 10.1 Endpoint/callback pattern

The endpoint types act as cross-assembly RPC and event surfaces. `ActorEndpoint` is the broadest example. It exposes callbacks for client and server actor lifecycle, targeting, combat state, spell casting, aura state, VFX/SFX, cooldowns, tickers and auto-attacks.

This pattern allows:

- shared/network code to expose a common endpoint and callback surface;
- server managers to subscribe to authoritative events;
- client managers/UI to subscribe to presentation events;
- server ECS systems to publish events back toward MonoBehaviour-based gameplay managers without directly depending on `Game.Server` types from `Game.Shared`.

Managers may subscribe directly to the endpoint for the subsystem they own. Server bindings are used for cross-layer registration that needs a named binding lifecycle; a `*Bindings` type is not required around every endpoint subscription.

### 10.2 RPC-facing endpoints

Shared endpoint classes also contain PurrNet RPC methods where a network operation belongs to both sides of the contract.

For example, `MovementEndpoint` includes:

- a server RPC for requesting chunk interest;
- a targeted client RPC for updated interest;
- a targeted client RPC for authoritative position correction.

The shared service invokes client- or server-specific callbacks, with the owning runtime layer handling the resulting behaviour.

### 10.3 Network packing

Compact network representations are separated into dedicated types, including:

- `PackGuid`;
- `PackWorldPosition`;
- `PackStatSnapshot`;
- `PackCombatLogEntry`.

The shared networking layer therefore contains both higher-level RPC/event contracts and lower-level value representations used during serialization.

## 11. Server Architecture

The server code is divided into four principal runtime roles:

```text
Server/Core          long-lived authoritative managers
Server/Services      request/domain operations
Server/Systems       ECS simulation systems/components
Server/Persistence   server-owned persisted data
```

Small networking binding/session support types are kept under `Server/Networking`.

### 11.1 Server managers

The main server managers are MonoBehaviour-based, process-wide components created during server startup.

`ServerConnectionManager` manages connected-player/session state.

`ServerWorldManager` owns context-qualified server environment scene loading/unloading, PurrNet scene identity and terminal asynchronous scene lifecycle.

`ServerInterestManager` owns player interest in world chunks and coordinates subscriptions with `ServerWorldManager`.

`ServerPositionManager` owns chunk/local position and network transport conversion. It does not own active physical state, colliders, NavMesh lifetime or physics queries.

`ServerPhysicsManager` owns the current continuous server physical/NavMesh representation. `ToPhysicalPosition(WorldPosition)` and `ToWorldPosition(Vector3)` convert between canonical absolute positions and the single Unity physical frame; `RaycastGround(...)` executes continuous physical queries; and context-qualified environment navigation roots keyed by `WorldChunkKey` feed one streamed continuous NavMesh representation. It also owns navigation-aware environment unload preparation/cancellation. It does **not** own `ActorComponent.WorldPositionD`, an actor-position cache, actor physical-region membership, environment scene loading, gameplay target selection or future **in-process** simulation-region policy.

`ServerSpawnManager` owns spawning flows.

`ServerActorManager` maintains the authoritative relationship between networked `Actor` GameObjects and their server ECS entities, along with player validation and NPC-related actor processing. Its `ServerActorManager_NPCMovement` partial (not a separate runtime manager) owns enabled-`NavMeshAgent` lifecycle, direct pursuit/destination updates in the continuous physical space, the pre-ECS enabled-agent GameObject-to-canonical-ECS movement bridge, and post-ECS ECS-to-GameObject projection for ECS-driven NPC movement. Navigation code does not own gameplay target clearing. There is no region-hop, seam-search or Warp state in the current NPC navigation path.

`ServerStatManager`, `ServerSpellCastManager`, `ServerAuraManager`, `ServerTickerManager`, `ServerCooldownManager`, and `ServerAutoAttackManager` own authoritative gameplay subsystems of the same names. `ServerAutoAttackManager` manages auto-attack registration, attack-range/readiness/cooldown checks and attack execution only; it does not own NPC navigation state or movement.

`ServerBindings` performs the one-time server binding registration. It registers `AccountBindings` for account endpoint requests and `ActorDeathBindings` for the actor-death callback. Other managers subscribe directly to the endpoint they own; bindings are not required around every subscription.

### 11.2 Server service layer

`Server/Services` contains domain operations that are invoked by network/account/gameplay flows. Current services are:

- `PlayerAuthService`;
- `CharacterCreationService`;
- `EnterWorldService`;
- `CharacterExperienceService`;
- `CharacterReputationService`;
- `CharacterWeaponSkillService`;
- `PartyLootService`.

These services operate on shared data contracts while remaining server-owned implementations.

### 11.3 Server networking support

`Server/Networking` currently contains supporting binding/session types including:

- `AccountBindings`;
- `ActorDeathBindings`;
- `PlayerSessionData`.

`AccountBindings` connects `AccountEndpoint` requests to the character-creation and enter-world services. `ActorDeathBindings` connects `ActorEndpoint.onActorDeath` to server-side experience, loot and quest handling. These are explicit binding responsibilities, not generic `*Hooks` managers.

These types connect shared endpoint callbacks and session state to server state.

Stage 1.5 network-pressure ownership is deliberately narrow:

- `ServerConnectionManager` is the sole owner of `Connection`-keyed outbound rate windows, pressure trackers, thresholds and hysteresis. Its one-second window remains active during ordinary server operation; profiling additionally owns report samples and the UDP-native statistics window.
- `INetworkPressureProvider` is a Shared read-only interface only. `NetworkPressureQuery` stores only the active provider reference and delegates `PlayerID` queries; it is not a generic service locator and owns no cache, thresholds or duplicate pressure dictionary.
- `ServerConnectionManager.IsPlayerPressured(PlayerID)` resolves the current PurrNet `playerModule` `Connection` mapping and delegates to the existing Connection pressure state. Shared code does not import `Game.Server`.
- The provider is registered after the server connection manager has its active network fields and unregistered at the existing server unsubscribe boundary. The query currently has no Stage 1.5 replication-shedding consumer.

The pressure classifier is observability/state, not a bandwidth cap, capacity claim, generic PurrNet backlog measurement or traffic-shedding policy. No authoritative, control or current-state network delivery is silently dropped by this boundary.

### 11.4 Server invariant and lifecycle contracts

Server managers are not independent collections of defensive checks. Each authoritative subsystem establishes specific invariants that downstream project-owned code may trust.

#### 11.4.1 Startup and manager publication

Server startup order is an architectural guarantee.

- Configuration is loaded and validated before the server ECS world and gameplay/world managers consume it.
- A manager created after another required manager may trust that predecessor during ordinary synchronous gameplay instead of repeatedly polling its `IsInitialized` state.
- A process-wide manager publishes its singleton only after its required fields/subscriptions are ready. Duplicate manager construction is an invalid startup state and fails immediately.
- Manager teardown only acts when the component being destroyed is the registered singleton. An unregistered duplicate must not unsubscribe shared callbacks, clear global registries or reset the live singleton.

There is one deliberate startup asymmetry in the simulation path: the authoritative simulation clock can run after the ECS world is ready but before all gameplay managers have been created. Therefore `ServerECSManager` retains nullable gameplay-manager calls during that startup interval. This is a real lifecycle condition, not a generic argument for repeated readiness polling elsewhere.

#### 11.4.2 Actor and ECS authority

`ServerActorManager` owns the authoritative relationship between:

```text
ActorID
  ↕
networked Actor GameObject
  ↕
server ECS Entity
```

Successful actor registration establishes, as one transaction:

- the ActorID → Actor mapping;
- the ActorID → Entity mapping;
- valid actor spatial context and spatial-index membership;
- the authoritative `ActorComponent`;
- the spell-cast component;
- cooldown, aura and ticker buffers;
- movement components where applicable.

Internal consumers resolving a known ActorID therefore use the actor manager's direct mapping. They do not scan every `ActorComponent` entity and do not repeatedly re-prove component ActorID equality or required-buffer existence.

The authoritative ECS entity generation remains important for deferred events: `ServerSimulationEvent.SourceEntity` is compared with the actor manager's current ActorID → Entity mapping so an event from an earlier actor incarnation cannot be delivered to a replacement actor. The source entity is an identity token at this boundary, not a reason to rediscover and validate the entity's complete component shape during event dispatch.

#### 11.4.3 Validation ownership

Validation is performed where data crosses from untrusted or mutable state into an owned invariant.

Examples include:

- network/client request validation at the server authority boundary;
- authored spell/aura/ticker/cooldown duration validation when the owning manager creates timed state;
- coordinate representability at the conversion/index boundary that requires it;
- scene/Addressables/PurrNet completion checks when asynchronous work resumes;
- cast/aura/ticker generation checks when callbacks can replace or retire the current operation.

Once those boundaries have established stable internal state, normal hot paths trust it.

A check is retained when its truth can legitimately change between proof and use: actor/target liveness, player ownership, logical context transfer, scene loading/unloading, NavMesh state, callback reentrancy, pending mutation generation and asynchronous completion are all examples. Startup configuration, registered component shape and immutable registry keys are not revalidated merely because a theoretically bad value can be imagined.

#### 11.4.4 Authoritative actor retirement precedes network despawn

Network lifetime and gameplay authority are separate concerns.

For an explicit player logout, teardown order is:

```text
persist required character/account state
    ↓
stop periodic stat tracking
    ↓
Actor.RetireFromServer()
    ↓
publish onServerActorDespawned exactly once
    ↓
remove combat / interest / simulation / timed-state / ECS / spatial registrations
    ↓
NetworkIdentity.Despawn()
    ↓
Unity object destruction
```

`Actor.RetireFromServer()` is idempotent. `Actor.OnDestroy()` calls it as a fallback for destruction paths that did not retire explicitly, but a player logout that already retired the actor does not publish server-despawn twice.

The architectural invariant is:

> Once `NetworkIdentity.Despawn()` begins, the actor is no longer authoritative gameplay state capable of generating combat, stat, spell, aura, ticker or other RPC-producing callbacks.

RPC-side `isSpawned` guards are not the normal remedy for teardown-order bugs. If unspawned actors are still executing authoritative gameplay, the owner lifecycle is incorrect and must be fixed at retirement.

#### 11.4.5 Asynchronous state is the exception to internal trust

World streaming and network scene state deliberately keep stronger revalidation than ordinary synchronous manager code. `ServerWorldManager`, `ServerInterestManager` and `ServerPhysicsManager` can span PurrNet callbacks, Addressables completion, scene unloads and cancellation/rollback. State that could change while work is pending must be checked again when the operation resumes.

The project therefore distinguishes:

- **owned synchronous invariants**, established once and trusted;
- **dynamic/reentrant state**, checked at the point of use;
- **asynchronous state**, revalidated after suspension/completion;
- **external input**, validated at its authority boundary.

This distinction is the basis for removing defensive work without weakening real lifecycle safety.

## 12. Server ECS Architecture

### 12.1 ECS world

`ServerECSManager` constructs a dedicated Unity Entities `World` named `Default`, creates a `SimulationSystemGroup`, explicitly creates the project systems, and adds them to the update list. `ServerSimulationClock` manually drives this group once per authoritative fixed tick; the dedicated world is not added to Unity's player loop.

The current server systems registered during startup are:

- `WorldSpawnSystem`;
- `ActorUpdateSystem`;
- `SpellCastSystem`;
- `CooldownTickSystem`;
- `AuraTickSystem`;
- `ActorTickerSystem`.

`ResourceRegenSystem` and `ResourceRegenElement` remain defined placeholders, but
`ResourceRegenSystem` is not scheduled until resource regeneration behavior is implemented.

The server code also defines chunk-spawn component types used by the world-spawn path.

### 12.2 Actor ECS state

`ActorComponent.cs` defines the core ECS state used for server simulation.

`ActorComponent` contains:

- actor GUID;
- actor name;
- owner player ID;
- current scene ID;
- double-precision world position.

Additional components/buffers include:

- `MotionStateComponent`;
- `GroundDetectionComponent`;
- `ResourceRegenElement`;
- enableable `SpellCastComponent`;
- `CooldownElement`;
- `AuraElement`;
- `TickerElement`.

### 12.3 GameObject/ECS bridge

The authoritative actor model is hybrid rather than purely ECS.

When a shared `Actor` is spawned on the server, `ServerActorManager` creates an ECS entity and associates it with the actor GUID. It keeps a dictionary from GUID to the corresponding `Actor` MonoBehaviour/NetworkBehaviour.

Depending on actor type and movement ownership, state can move in either direction between the GameObject and ECS representations. For example:

- player-owned actor movement is tracked from the networked actor and synchronised into ECS for authoritative state/validation;
- non-player actors without NavMesh ownership can have vertical motion simulated in ECS and applied back to their GameObject transform;
- for enabled-`NavMeshAgent` NPCs, `ServerActorManager_NPCMovement` resolves the live target's canonical accepted `WorldPositionD`, projects it through `ServerPhysicsManager.ToPhysicalPosition(...)`, and submits that continuous-space destination to `NavMeshAgent.SetDestination(...)`. The pre-ECS bridge reconstructs the agent's physical transform through `ServerPhysicsManager.ToWorldPosition(...)` into `ActorComponent.WorldPositionD`. The GameObject transform is a physical representation, not an independent global position authority; there is no seam/hop/Warp handoff path.

ECS systems communicate completion/tick state back to the MonoBehaviour/domain layer through the simulation event boundary rather than invoking gameplay callbacks during ECS traversal. Systems such as `SpellCastSystem`, `AuraTickSystem` and `ActorTickerSystem` queue immutable `ServerSimulationEvent` records. `ServerSimulationEventBuffer` stages and deterministically orders them, and `ServerECSManager` dispatches them to the appropriate `ActorEndpoint` callbacks after ECS structural work and managed mutation commits are complete.

### 12.4 Simulation and timed-state boundary contracts

The server simulation uses explicit ownership boundaries rather than treating ECS and managed gameplay as freely reentrant layers.

#### Tick ownership

`ServerSimulationClock` owns authoritative tick identity and cadence. `ServerECSManager` owns the body of one tick:

```text
BeginTick
    ↓
prepare + commit pending managed mutations
    ↓
actor pre-ECS bridge
    ↓
set ECS world time
    ↓
SimulationSystemGroup.Update
    ↓
actor post-ECS bridge
    ↓
commit spawn/domain structural work
    ↓
dispatch buffered simulation events
    ↓
EndTick (or Clear on failure)
```

The event buffer does not independently own the simulation lifetime. It owns event staging, ordering, cancellation and dispatch isolation.

#### Mutation snapshots

Spell, aura, ticker and cooldown callbacks can enqueue mutations while another subsystem is committing or while buffered events are being dispatched. To preserve one deterministic boundary:

1. all four manager mutation queues are prepared/snapshotted before any manager begins committing;
2. commit reads only the prepared snapshots;
3. mutations created during a commit remain pending for the next boundary.

Reusable scratch/snapshot lists may remove allocation, but they do not weaken this two-phase rule or become a second source of authority.

#### Timed-state validation

Timed gameplay managers own conversion from authored/runtime durations to coherent ECS timing state. They validate their subsystem-specific input once, calculate the authoritative tick deadlines/intervals and write the ECS component/buffer state.

The ECS tick systems then trust those fields and perform deterministic progression. They do not revalidate global simulation configuration or every timing invariant for every entry on every tick. Dynamic generation checks remain in the managed owners when a cast, aura or ticker can be replaced or removed between event creation and event delivery.

#### Hot-path lookup

Known actors are resolved through `ServerActorManager`'s direct mappings rather than by full ECS actor scans. ECS systems use the buffers/components supplied by their queries rather than querying the same entity a second time. Immutable server-lifetime configuration may be cached at system creation where appropriate.

This does not imply adding speculative caches or active-state indexes. Additional state is introduced only when an existing owner cannot answer the lookup and measured/architectural need justifies another lifecycle to maintain.

#### Main-thread and job boundaries

The current hybrid simulation intentionally retains managed/main-thread boundaries where systems call Unity Physics or enqueue managed simulation events. Jobs/Burst are appropriate only after the relevant data path is separated from UnityEngine objects, managed callbacks and structural mutation. The architecture does not add Burst annotations around code that remains fundamentally managed.

## 13. Client Architecture

The client C# source is organised into:

```text
Client/
├── Core/       runtime managers and controllers
├── Systems/    current client ECS system(s)
├── UI/         complete user-interface layer
├── VFX/        procedural/effect behaviours
├── Utility/    client-only utility types
└── Shaders/    shader-facing C# property helpers
```

These folders use folder-derived namespaces: `Core/` maps to `Game.Client.Core`, its `Controllers/`, `Managers/` and `Collections/` subfolders map to `Game.Client.Core.Controllers`, `Game.Client.Core.Managers` and `Game.Client.Core.Collections`, UI feature folders map to `Game.Client.UI.<Feature>`, and the remaining folders map to `Game.Client.Systems`, `Game.Client.VFX`, `Game.Client.Utility` and `Game.Client.Shaders`.

### 13.1 Client managers and controllers

`Client/Core` contains the high-level runtime components used after startup. Major classes include:

- `ClientConnectionManager`;
- `ClientWorldManager`;
- `ClientPositionManager`;
- `ClientCombatManager`;
- `ClientAudioManager`;
- `ClientVFXManager`;
- `ClientSettingsManager`;
- `ClientAccountManager`;
- `CameraManager`;
- `ClientInputController`;
- `CharacterCameraController`;
- `CharacterCreationManager`;
- `CharacterSelectionManager`.

These components consume shared network events and actor/data types and convert them into client behaviour, scene state, presentation, input and UI-facing state.

### 13.2 Client ECS

`ClientECSManager` currently creates a Unity Entities `World` and a `SimulationSystemGroup`, then registers `TestClientSystem`.

The main production client gameplay/presentation model is presently represented through GameObjects, managers and UI components, with the ECS world existing as a separate client-side system boundary.

## 14. Client UI Architecture

The UI is the largest single C# area in the client assembly, with 111 files. It is organised primarily by game feature.

Current UI feature folders are:

```text
UI/
├── ActionBars/
├── CastBars/
├── CharacterCreation/
├── CharacterSelection/
├── CharacterUnitFrame/
├── CharacterWindow/
├── Common/
├── ContextMenu/
├── Dialogue/
├── ExperienceBar/
├── FloatingCombatText/
├── Inventory/
├── LoadingScreen/
├── MainMenu/
├── Nameplates/
├── QuestLog/
├── ReputationWindow/
├── TalentWindow/
├── TargetWindow/
├── Tooltip/
└── VFX/
```

### 14.1 Common UI layer

`Client/UI/Common` contains reusable UI building blocks. Important types include:

- `UI_Manager`;
- `UI_Window`;
- `UI_Panel`;
- `UI_Button`;
- `UI_ListEntry`;
- `IListPanel`;
- `IDraggableSlot`;
- `UI_DragGhost`;
- `UI_ProgressBar`;
- `UI_AuraBar` and `UI_AuraEntry`;
- world-space UI manager/components.

`UI_Window` provides common show/hide/toggle behaviour, canvas interaction control, fade transitions, and child-panel discovery. Windows notify `UI_Manager` when their visibility changes.

Feature UI classes compose or derive from these common types and subscribe to the relevant shared/client events.

### 14.2 Feature-oriented UI

Feature areas generally separate their visual responsibilities into windows, panels, list containers and entries. Examples include:

- character creation stages, race/class lists and finalisation panels;
- character selection list and selected-character panels;
- inventory grid, slot entries and footer;
- character statistics/equipment/skills panels;
- quest list, objective and reward panels;
- talent tree viewer, tree panel and nodes;
- target information, resource, aura and cast bars;
- nameplate manager, health bar and cast bar;
- tooltip window, panel and line types.

The UI therefore mirrors gameplay domains rather than being arranged as a single monolithic screen-controller layer.

## 15. World and Chunk Architecture

The current world architecture uses chunk-addressable scenes with server-managed interest and a client floating origin.

### 15.1 Chunk identity

World chunk scenes use names in the form:

```text
World_<x>_<y>
```

`GameConfiguration.ChunkSize` supplies the shared 256 m origin/network/environment-tile grid size.

`WorldPosition` stores authoritative coordinates using double precision (`double x/y/z`) and derives:

- the chunk coordinate;
- the local position within a chunk;
- conversion to local coordinates relative to an arbitrary origin.

`ActorSpatialContext` carries authoritative logical world/map/instance identity independently of physical coordinates. Server environment identity uses `WorldChunkKey`, which combines an `ActorSpatialContext` with an environment-tile coordinate so equal tile coordinates in different runtime contexts do not share logical identity.

### 15.2 Server world scenes

`ServerWorldManager` is authoritative for context-qualified world chunk scene loading/unloading. It maps logical context + environment tile identity to PurrNet `SceneID` values and tracks asynchronous in-flight chunk loads.

World chunks are loaded through the PurrNet scene module as addressable additive scenes. The manager also loads a persistent `global_actors` scene used for actor GameObjects.

Physical simulation is a separate responsibility. `ServerWorldManager` does not own physical-coordinate conversion, collider/NavMesh binding or physics queries. When an environment scene reaches the loaded state, `ServerPhysicsManager.BindEnvironmentScene(...)` registers its existing scene roots as context-qualified NavMesh sources without moving those roots into another physical frame. Navigation-aware unload preparation removes a tile from the continuous NavMesh before terminal scene unload and can be cancelled if the unload is aborted.

This creates a distinction between:

- terrain/world chunk scenes, whose scene lifetime belongs to `ServerWorldManager`;
- continuous physical/NavMesh representation, whose conversion/query/navigation lifetime belongs to `ServerPhysicsManager`;
- `global_actors`, which acts as the persistent actor scene.

### 15.3 Interest management

`ServerInterestManager` receives chunk-interest requests from the shared `MovementEndpoint`.

For a player actor it computes a grid of required chunk scenes, compares that set with the actor's previous subscription set, and coordinates:

- chunk loading;
- player-to-scene subscription;
- chunk unsubscription;
- chunk unloading when no players remain;
- network-visibility re-evaluation for changed chunks;
- actor current-chunk updates.

`Shared.Networking.ChunkSubscriptions` provides common mappings for:

- scene name → actors;
- player ID → actor;
- scene name → network identities.

### 15.4 Client world loading

`ClientWorldManager` listens for the character entering the world, establishes the initial origin chunk, hooks PurrNet scene load/unload notifications, loads the client `global_world` scene additively, and tracks loaded world chunk scenes.

New chunk scenes are offset immediately to the active floating origin when they arrive.

## 16. Floating-Origin and Position Architecture

The shared `WorldPosition` struct is the high-precision representation for world coordinates. The client does not render directly in those large double-precision coordinates.

The horizontal origin/network grid and authored environment-tile grid share the configured `GameConfiguration.ChunkSize` of 256 m, but the integer coordinate identifies the **centre** of the cell/tile rather than its minimum corner.

For one horizontal axis with cell size `S` and integer coordinate `c`:

```text
cell centre = c * S
cell footprint = [c * S - S/2, c * S + S/2)
cell coordinate = floor((absolute + S/2) / S)
cell-local offset = absolute - c * S
absolute = c * S + cell-local offset
```

Therefore, with `S = 256`, `World_0_0` / origin cell `(0,0)` occupies X/Z `[-128, 128)`, and its chunk-local X/Z offsets are also `[-128, 128)`. Absolute world origin `(0,0)` is the centre of `World_0_0`, not the intersection of four cells.

`ClientPositionManager` maintains an `OriginChunk`. Unity scene objects are positioned relative to the centre of that origin cell using normal `Vector3` values. Conceptually, client-local X/Z is:

```text
local = absolute - OriginChunk * ChunkSize
```

When the origin chunk changes:

1. the delta between old and new origin chunks is calculated;
2. the corresponding world-space shift is calculated from `ChunkSize`;
3. root objects in loaded world scenes are shifted;
4. networked actors and the locally owned player are reprojected into the new local frame without changing their absolute `WorldPosition`;
5. the new origin chunk becomes the client's reference origin.

Scene membership is not an authority for whether the locally owned player must move with a rebase. If shifting scene roots does not guarantee the owner actor is shifted, the owner actor must be explicitly reprojected from its pre-rebase absolute position.

The manager also provides conversions between:

- centred network chunk + local position;
- Unity local-space position;
- chunk-relative coordinates for data sent back toward the server.

Normal floating-origin selection is driven by the player's actual position crossing a centred origin-cell boundary. Environment streaming/interest may use the same integer grid coordinate, but interest requests do not own or directly force the client floating origin.

The authoritative server does not mirror the client's floating-origin shifts. Canonical accepted actor position remains double precision in `ActorComponent.WorldPositionD`. For the current tested world scale, `ServerPhysicsManager` projects finite canonical coordinates directly into one continuous Unity `Vector3` physical frame and reconstructs physical results back into `WorldPosition`. A streamed 256 m environment/origin-cell boundary therefore does not create a server physical frame boundary.

This direct-float server representation is the current implementation, not a claim that one Unity process can represent an arbitrarily large future world indefinitely. Any later physical partitioning remains internal to the **single World Server process** and must preserve one-machine ownership of all its maps, instances and actors. Off-process simulation workers or sharding one world across machines are not approved; future coarse in-process regions are not implied by the 256 m streaming/FPO grid.

This separates large-scale authoritative coordinates from the float coordinate space used by Unity rendering and normal GameObject transforms.

## 17. Movement Architecture

Movement contains both shared mathematics and side-specific ownership/validation.

`Shared/Movement/MovementSimulation` contains movement and gravity calculations intended to be usable by both client and server. The functions operate on input/yaw/velocity values rather than directly depending on a Transform.

The current actor/server architecture treats player movement differently from NPC movement:

- player movement uses the existing client-owned position proposal, authoritative server validation and correction path through `MovementEndpoint`;
- `ServerActorManager_NPCMovement` orchestrates enabled-`NavMeshAgent` NPC pursuit, direct continuous-space destination refresh and the pre-ECS movement bridge;
- disabled/absent-agent NPC vertical movement is owned by server ECS and projected back to the GameObject after ECS simulation;
- `ServerPhysicsManager` owns continuous physical conversion, physical queries and streamed NavMesh binding/lifetime; streamed tile crossings do not cause region lookup, Warp or handoff;
- `ServerAutoAttackManager` handles auto-attack combat execution, not NPC locomotion/navigation.

Chunk transitions are coupled to movement through interest requests rather than by directly making the movement simulation responsible for scene loading.

## 18. Combat, Spell, Aura and Timed-State Architecture

Combat-related code is split across shared definitions/runtime state and authoritative server managers/ECS systems.

### 18.1 Definitions

Shared data assets describe spells, spell effects, targets, auras, aura effects, damage schools, actor stats, talents and other combat-facing game rules.

### 18.2 Runtime actor layer

`ActorSpellcaster`, `ActorStatContainer`, player equipment and associated shared runtime classes expose the live actor-facing view of combat state.

### 18.3 Server manager layer

Authoritative operations are coordinated by managers including:

- `ServerSpellCastManager`;
- `ServerAuraManager`;
- `ServerCooldownManager`;
- `ServerTickerManager`;
- `ServerAutoAttackManager`;
- `ServerStatManager`.

### 18.4 ECS timing layer

Time-dependent state that benefits from system iteration is represented in server ECS components/buffers:

- active cast timing;
- cooldown timing;
- aura timing;
- generic ticker timing;
- resource regeneration data reserved for a future implementation.

The active cast, cooldown, aura and ticker systems tick this state each authoritative update.
Resource regeneration remains a defined future ECS timing responsibility. When a timed operation
reaches a gameplay boundary, the ECS system raises shared endpoint callbacks so the authoritative
manager/runtime layer can resolve the associated game action.

## 19. Persistence Architecture

Persistence data contracts that are meaningful across the codebase live in `Shared/Persistence`, while storage belongs to `Server/Persistence`.

### 19.1 Shared character data

`CharacterData` is a serializable character persistence model. It includes identity and progression data plus collections for:

- known spells;
- inventory;
- equipment;
- weapon skills;
- talents;
- reputations;
- quests;
- current resource values.

Supporting saved-entry structs represent the individual persisted records.

The Phase 0 compatibility requirement is that pre-restyle `CharacterData` JSON names remain readable. The current loader therefore keeps the public DTO names stable and `PlayerDatabase` migrates the legacy version-0 names used by existing files (for example `Guid`, `PvPFactionID`, `ItemGuid`, `ItemId` and `StatId`) into the current `CharacterData` and saved-entry fields. A C# naming/restyling change must not rely on Unity `FormerlySerializedAs` alone for these `JsonUtility` fields.

### 19.2 Server storage

`PlayerDatabase` is the current prototype server storage implementation. It maintains player account data in an in-memory dictionary and serializes the database to a versioned JSON file beneath `Application.persistentDataPath`, including a legacy version-0 loader/migration.

The configured filename comes from `GameConfiguration.PlayerDataFilePath`.

Relational persistence is not implemented by this architecture revision. The current boundary is the shared `CharacterData`/saved-entry DTOs plus the server-local `PlayerDatabase`; database-backed relational character, item and session storage remains future infrastructure rather than an existing implementation claim.

Server authentication is exposed to the shared login authenticator through `PlayerAuthService`, keeping the concrete account/database implementation inside the server assembly.

## 20. Editor Architecture

The editor assembly contains two broad tool families: gameplay-data authoring and world authoring.

### 20.1 Gameplay-data authoring

`Editor/Windows` contains dedicated editor windows for game definition types, including:

- actor stats;
- auras;
- classes;
- damage schools;
- factions;
- items;
- NPCs;
- quests;
- races;
- spells;
- talents.

`DataDefinitionEditorWindow<TDefinition, TLibrary>` provides a generic two-panel authoring framework with definition listing, filtering, sorting, asset selection, creation/cloning/deletion and a detail panel for serialized properties.

Concrete windows inherit this framework and supply type-specific drawing and metadata.

Custom property drawers and inspectors in `Editor/Drawers` and `Editor/Inspectors` provide specialised editing for nested gameplay structures and shared runtime components.

### 20.2 Dialogue tooling

`Editor/DialogueEditor` contains the dialogue editor window and associated utilities/editors for authored dialogue definitions.

### 20.3 World editor

`Editor/WorldEditor` contains the bespoke terrain/world-authoring pipeline. Its code is separated into functional areas for:

- terrain/chunk generation;
- heightmap processing;
- tiled heightmap streaming;
- terrain LOD generation;
- terrain painting and texture-array support;
- erosion processing;
- editor UI and interaction.

The main `WorldEditorWindow` is a `partial` class split by concern:

```text
WorldEditorWindow.Base.cs
WorldEditorWindow.Erosion.cs
WorldEditorWindow.Input.cs
WorldEditorWindow.Modules.cs
WorldEditorWindow.Overlay.cs
WorldEditorWindow.Terrain.cs
WorldEditorWindow.UI.cs
```

`WorldEditorWindow.Base` owns shared editor state and lifecycle. Other partial files add the specific terrain, erosion, overlay, input, module and UI behaviours.

The erosion subsystem is itself divided into interfaces, modes, processing, local erosion operations and multi-tile processing.

## 21. Namespace and Type Organisation

The main namespaces mirror the assembly and responsibility boundaries:

```text
Game.Core
Game.Client.Core
Game.Client.Core.Collections
Game.Client.Core.Controllers
Game.Client.Core.Managers
Game.Client.Systems
Game.Client.UI.<Feature>
Game.Client.Shaders
Game.Client.Utility
Game.Client.VFX
Game.Server.Core
Game.Server.Persistence
Game.Server.Services
Game.Server.Systems
Game.Server.Test
Game.Server.Networking
Game.Shared
Game.Shared.Components
Game.Shared.Data
Game.Shared.Movement
Game.Shared.Networking
Game.Shared.Persistence
Game.Shared.Runtime.*
Game.Shared.Terrain
Game.Shared.Utility
Game.Editor
Game.Editor.Windows
Game.Editor.WorldEditor
```

The codebase commonly uses role prefixes to make runtime ownership explicit:

- `Client...` for client managers/controllers;
- `Server...` for server managers;
- `UI_...` for UI components;
- `SpellEffect_...`, `SpellTarget_...`, `AuraEffect_...`, etc. for definition subtypes;
- `Pack...` for network-packed values;
- `Saved...` for persisted record structures.

Managers are generally persistent or process-level coordinators, while domain data and runtime objects are represented as concrete shared types.

## 22. Event and Dependency Flow

A typical cross-layer gameplay flow follows this shape:

```text
Client input / UI
      ↓
Shared endpoint/callback surface or networked Actor component
      ↓ PurrNet RPC / replicated state
Server manager or service
      ↓
Shared runtime definitions/state
      ↓
Server ECS timer/system when applicable
      ↓
Shared endpoint callback
      ↓
Authoritative server resolution
      ↓ PurrNet replication / target RPC / observers RPC
Shared client callback
      ↓
Client manager / actor component
      ↓
UI / VFX / audio presentation
```

Not every feature uses every stage, but this captures the principal direction of ownership: the shared assembly defines the common model and communication surfaces, the server resolves authoritative game state, and the client turns received state/events into local interaction and presentation.

## 23. Architectural Responsibility Map

| Concern | Primary C# ownership |
|---|---|
| Process/client-server selection | `Game.Core` |
| Game configuration | `Game.Shared` |
| Gameplay definitions | `Game.Shared.Data` |
| Actor runtime model | `Game.Shared.Runtime.Actors` |
| Spell/aura runtime contexts | `Game.Shared.Runtime.Spellcasting` |
| Common network endpoint/callback contracts | `Game.Shared.Networking` |
| Shared persistence DTOs | `Game.Shared.Persistence` |
| Authoritative world loading | `Game.Server.Core.ServerWorldManager` |
| Player chunk interest | `Game.Server.Core.ServerInterestManager` |
| Position/network coordinate conversion | `Game.Server.Core.ServerPositionManager` |
| Continuous server physics/NavMesh representation | `Game.Server.Core.ServerPhysicsManager` |
| Server actor/GameObject-ECS bridge | `Game.Server.Core.ServerActorManager` |
| Authoritative actor retirement | `Actor.RetireFromServer()` publication + `onServerActorDespawned` subsystem cleanup |
| Server simulation tick identity/cadence | `Game.Server.Core.ServerSimulationClock` |
| Server tick/mutation/event boundary | `Game.Server.Core.ServerECSManager` + `ServerSimulationEventBuffer` |
| Server time-based ECS progression | `Game.Server.Systems` systems |
| Timed gameplay creation/generation state | `ServerSpellCastManager`, `ServerAuraManager`, `ServerTickerManager`, `ServerCooldownManager` |
| Authentication/account implementation | `Game.Server.Services` / `Game.Server.Persistence` |
| Client world scene tracking | `Game.Client.Core.ClientWorldManager` |
| Client floating origin | `Game.Client.Core.ClientPositionManager` |
| Client input/camera | `Game.Client.Core` controllers/managers |
| Client combat presentation | `Game.Client.Core.ClientCombatManager`, UI/VFX/audio layers |
| Game UI | `Game.Client.UI.<Feature>` |
| Definition authoring | `Game.Editor.Windows`, drawers, inspectors |
| World/terrain authoring | `Game.Editor.WorldEditor` |

## 24. Current Architectural Summary

The current Ninth Age C# architecture is centred on five responsibility domains:

1. **Core bootstrap** selects client or headless-server execution.
2. **Shared code** defines the game's common data model, runtime actor concepts, persistence records, network contracts and utility mathematics.
3. **Server code** owns authoritative world state, gameplay resolution, persistence and the server ECS simulation layer.
4. **Client code** owns input, cameras, scene presentation, floating-origin behaviour, VFX/audio and the full UI layer.
5. **Editor code** provides authoring environments for gameplay definitions and the large-world terrain pipeline.

The server runtime combines networked GameObject actors with a Unity Entities simulation layer. Shared actor objects provide the network-visible gameplay identity, while server ECS entities and dynamic buffers hold selected simulation state such as positions, movement state, spell casts, cooldowns, auras, tickers and regeneration. `Game.Server.Core.ServerActorManager` and `Game.Shared.Networking.ActorEndpoint` callbacks bridge these two representations.

Stage 1.2 is complete. The server foundation now uses canonical accepted positions, context-qualified spatial indexing and world/interest state, current visibility relevance, observer replication from authoritative position, and atomic logical-context transfer. The pre-Stage 1.3 cleanup also established explicit invariant ownership: stable synchronous state is validated once at its owner, dynamic/asynchronous state is revalidated where it can change, hot paths use existing direct mappings rather than rediscovery, and authoritative actor retirement occurs before network despawn.

The world layer uses context-qualified addressable additive environment scenes, server-controlled scene interest, a persistent global actor scene, double-precision canonical actor positions, one continuous server physical/NavMesh space for the current runtime phase, and a client-side floating origin. The shared centred 256 m grid identifies client origin/network cells and authored environment tiles without partitioning server physics.

The authored RPG rules are represented as shared `ScriptableObject` definitions and definition libraries, with specialised effects/targets/conditions/behaviours represented as smaller typed components. The editor assembly provides dedicated authoring tools over those same shared definition types.

This document is intended to remain the reference description of the C# architecture represented by the source revision stated at the top of the file.
