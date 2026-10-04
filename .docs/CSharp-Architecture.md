# Ninth Age — C# Architecture Design

**Status:** Current architecture reference  
**Repository:** `FrontierDev/ninthage`  
**Branch:** `main`  
**Source revision:** `a793ded37d152783d29b71a7849fda083a33086d`  
**Snapshot date:** 2026-10-04  
**Unity version:** 6000.3.10f1

## 1. Purpose and Scope

This document describes the current C# architecture of the Ninth Age Unity project. It records how the codebase is organised, how the principal assemblies relate to one another, how client and server execution are selected and initialised, and how gameplay data, runtime actors, networking, server simulation, client presentation, world streaming, persistence, and editor tooling are divided across the project.

This is a descriptive design reference for the code that exists at the source revision above. It does not define a future refactor and does not prescribe architectural changes.

## 2. Architectural Overview

Ninth Age is implemented as a single Unity project that can run in two principal execution modes:

- a graphical game client;
- a headless authoritative server.

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

## 3. Source Layout

At this revision, the project contains 386 C# files under `Assets`.

| Area | C# files | Role |
|---|---:|---|
| `Assets/Runtime/Client` | 139 | Client execution, presentation, input, UI and client world state |
| `Assets/Runtime/Server` | 37 | Authoritative server managers, services, ECS systems and persistence |
| `Assets/Runtime/Shared` | 130 | Shared definitions, runtime objects, networking and utilities |
| `Assets/Runtime/Core` | 1 | Process bootstrap and client/server routing |
| `Assets/Editor` | 79 | Unity editor extensions and world/data authoring tools |
| **Total** | **386** | |

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
│   ├── Editor.asmdef
│   └── ToDoGenerator.cs
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

- `Game.Client.ClientInitialization.Begin(...)`; or
- `Game.Server.ServerInitialization.Begin(...)`.

The server path is selected when Unity is running in batch mode or when no graphics device is present. The bootstrapper also exposes an editor-oriented `forceServerMode` switch for server testing.

`Game.Core` references the shared, client, and server assemblies so that it can dispatch into either runtime.

### 4.2 `Game.Shared`

`Game.Shared` contains the C# types that form the common language of the game. Its responsibilities include:

- authorable game definitions;
- runtime actor and item state;
- spell and aura runtime contexts;
- common persistence DTOs;
- network services and RPC contracts;
- world-position and chunk-related types;
- shared movement mathematics;
- common calculations and utilities.

Both `Game.Client` and `Game.Server` build on the types in this assembly.

### 4.3 `Game.Client`

`Game.Client` contains code that only participates in the graphical client runtime. It owns:

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

`Game.Server` contains authoritative runtime responsibilities, including:

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

`Game.Client.ClientInitialization` performs client startup as a coroutine. The current sequence is:

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

`Game.Server.ServerInitialization` performs server startup as a coroutine. The current sequence is:

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
Initialise server RPC/event hooks
    ↓
Initialise player database/auth service
    ↓
Create ServerWorldManager
    ↓
Load global_actors scene
    ↓
Create interest, position, spawn and gameplay managers
```

The gameplay/world managers attached after world setup are currently:

- `ServerInterestManager`;
- `ServerPositionManager`;
- `ServerSpawnManager`;
- `ServerActorManager`;
- `ServerStatManager`;
- `ServerSpellCastManager`;
- `ServerAuraManager`;
- `ServerTickerManager`;
- `ServerCooldownManager`;
- `ServerAutoAttackManager`.

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

`Game.Shared.Actor` is the central runtime actor type. It derives from PurrNet `NetworkBehaviour` and represents a network-visible gameplay actor.

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

This area includes:

- account service;
- actor service;
- character service;
- movement service;
- stat service;
- login/authentication payloads;
- player-auth interface;
- chunk subscriptions and visibility rules;
- latency simulation;
- network packing structures.

### 10.1 Service/event pattern

Several shared networking services act as cross-assembly event hubs. `ActorService` is the clearest example. It exposes callbacks for client and server actor lifecycle, targeting, combat state, spell casting, aura state, VFX/SFX, cooldowns, tickers and auto-attacks.

This pattern allows:

- shared/network code to expose a common event surface;
- server managers to subscribe to authoritative events;
- client managers/UI to subscribe to presentation events;
- server ECS systems to publish events back toward MonoBehaviour-based gameplay managers without directly depending on `Game.Server` types from `Game.Shared`.

### 10.2 RPC-facing services

Shared service classes also contain PurrNet RPC methods where a network operation belongs to both sides of the contract.

For example, `MovementService` includes:

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

Small networking hook/session types are kept under `Server/Networking`.

### 11.1 Server managers

The main server managers are MonoBehaviour-based, process-wide components created during server startup.

`ServerConnectionManager` manages connected-player/session state.

`ServerWorldManager` owns server-side chunk scene loading and unloading.

`ServerInterestManager` owns player interest in world chunks and coordinates subscriptions with `ServerWorldManager`.

`ServerPositionManager` owns server-side position-related processing.

`ServerSpawnManager` owns spawning flows.

`ServerActorManager` maintains the authoritative relationship between networked `Actor` GameObjects and their server ECS entities, along with player validation and NPC-related actor processing.

`ServerStatManager`, `ServerSpellCastManager`, `ServerAuraManager`, `ServerTickerManager`, `ServerCooldownManager`, and `ServerAutoAttackManager` own authoritative gameplay subsystems of the same names.

`ServerHooksManager` initialises server-side hooks used to connect shared network/RPC surfaces to server implementations.

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

`Server/Networking` currently contains supporting hook/session types including:

- `AccountServiceHooks`;
- `ActorDeathHooks`;
- `PlayerSessionData`.

These types connect shared networking services and actor events to server state.

## 12. Server ECS Architecture

### 12.1 ECS world

`ServerECSManager` constructs a dedicated Unity Entities `World` named `Default`, creates a `SimulationSystemGroup`, explicitly creates the project systems, adds them to the update list, and appends the world to Unity's current player loop.

The current server systems registered during startup are:

- `WorldSpawnSystem`;
- `ActorUpdateSystem`;
- `ResourceRegenSystem`;
- `SpellCastSystem`;
- `CooldownTickSystem`;
- `AuraTickSystem`;
- `ActorTickerSystem`.

The server code also defines chunk-spawn component types used by the world-spawn path.

### 12.2 Actor ECS state

`ActorComponents.cs` defines the core ECS state used for server simulation.

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
- NPCs using `NavMeshAgent` keep NavMesh movement on the GameObject side and update ECS position from the transform.

ECS systems also communicate completion/tick events back to the MonoBehaviour/domain layer through shared service events. `SpellCastSystem`, for example, decrements cast state in ECS and raises `ActorService` events for cast ticks and cast completion.

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

`GameConfiguration.ChunkSize` supplies the shared physical chunk size.

`WorldPosition` stores authoritative coordinates using double precision (`double x/y/z`) and derives:

- the chunk coordinate;
- the local position within a chunk;
- conversion to local coordinates relative to an arbitrary origin.

### 15.2 Server world scenes

`ServerWorldManager` is authoritative for world chunk scene loading/unloading. It maintains a mapping from chunk coordinates to PurrNet `SceneID` values and tracks asynchronous in-flight chunk loads.

World chunks are loaded through the PurrNet scene module as addressable additive scenes. The manager also loads a persistent `global_actors` scene used for actor GameObjects.

This creates a distinction between:

- terrain/world chunk scenes, which can be loaded and unloaded according to interest;
- `global_actors`, which acts as the persistent actor scene.

### 15.3 Interest management

`ServerInterestManager` receives chunk-interest requests from the shared `MovementService`.

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

`ClientPositionManager` maintains an `OriginChunk`. Unity scene objects are positioned relative to this origin using normal `Vector3` values.

When the origin chunk changes:

1. the delta between old and new origin chunks is calculated;
2. the corresponding world-space shift is calculated from `ChunkSize`;
3. root objects in loaded world scenes are shifted;
4. root objects in `global_actors` are shifted;
5. the new origin chunk becomes the client's reference origin.

The manager also provides conversions between:

- network chunk + local position;
- Unity local-space position;
- chunk-relative coordinates for data sent back toward the server.

This separates large-scale authoritative coordinates from the float coordinate space used by Unity rendering and normal GameObject transforms.

## 17. Movement Architecture

Movement contains both shared mathematics and side-specific ownership/validation.

`Shared/Movement/MovementSimulation` contains movement and gravity calculations intended to be usable by both client and server. The functions operate on input/yaw/velocity values rather than directly depending on a Transform.

The current actor/server architecture treats player movement differently from NPC movement:

- player movement is owner/client-driven at the network-transform level;
- the server tracks and periodically validates player positions;
- server correction is available through `MovementService` when validation detects an illegal position;
- NPC movement may be driven by `NavMeshAgent` or server ECS gravity depending on the actor setup.

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
- resource regeneration.

Corresponding systems tick this state each update. When a timed operation reaches a gameplay boundary, the ECS system raises shared service events so the authoritative manager/runtime layer can resolve the associated game action.

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

### 19.2 Server storage

`PlayerDatabase` is the current server storage implementation. It maintains player account data in memory and serializes the database to a JSON file beneath `Application.persistentDataPath`.

The configured filename comes from `GameConfiguration.PlayerDataFilePath`.

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
Game.Client
Game.Client.UI
Game.Server
Game.Server.ECS
Game.Server.Persistence
Game.Server.Networking
Game.Shared
Game.Shared.Data
Game.Shared.Networking
Game.Shared.Persistence
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
Shared network service or networked Actor component
      ↓ PurrNet RPC / replicated state
Server manager or service
      ↓
Shared runtime definitions/state
      ↓
Server ECS timer/system when applicable
      ↓
Shared service event
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
| Actor runtime model | `Game.Shared.Runtime/Actors` |
| Spell/aura runtime contexts | `Game.Shared.Runtime/Spellcasting` |
| Common network contracts/RPC services | `Game.Shared.Networking` || Shared persistence DTOs | `Game.Shared.Persistence` |
| Authoritative world loading | `Game.Server.ServerWorldManager` |
| Player chunk interest | `Game.Server.ServerInterestManager` |
| Server actor/GameObject-ECS bridge | `Game.Server.ServerActorManager` |
| Server time-based simulation | `Game.Server.ECS` systems |
| Authentication/account implementation | `Game.Server.Services` / `Game.Server.Persistence` |
| Client world scene tracking | `Game.Client.ClientWorldManager` |
| Client floating origin | `Game.Client.ClientPositionManager` |
| Client input/camera | `Game.Client.Core` controllers/managers |
| Client combat presentation | `ClientCombatManager`, UI/VFX/audio layers |
| Game UI | `Game.Client.UI` |
| Definition authoring | `Game.Editor.Windows`, drawers, inspectors |
| World/terrain authoring | `Game.Editor.WorldEditor` |

## 24. Current Architectural Summary

The current Ninth Age C# architecture is centred on five responsibility domains:

1. **Core bootstrap** selects client or headless-server execution.
2. **Shared code** defines the game's common data model, runtime actor concepts, persistence records, network contracts and utility mathematics.
3. **Server code** owns authoritative world state, gameplay resolution, persistence and the server ECS simulation layer.
4. **Client code** owns input, cameras, scene presentation, floating-origin behaviour, VFX/audio and the full UI layer.
5. **Editor code** provides authoring environments for gameplay definitions and the large-world terrain pipeline.

The server runtime combines networked GameObject actors with a Unity Entities simulation layer. Shared actor objects provide the network-visible gameplay identity, while server ECS entities and dynamic buffers hold selected simulation state such as positions, movement state, spell casts, cooldowns, auras, tickers and regeneration. `ServerActorManager` and shared service events bridge these two representations.

The world layer uses addressable additive chunk scenes, server-controlled scene interest, a persistent global actor scene, double-precision shared world coordinates and a client-side floating origin. This allows the world-management architecture, actor architecture and networking architecture to share the same chunk-coordinate model.

The authored RPG rules are represented as shared `ScriptableObject` definitions and definition libraries, with specialised effects/targets/conditions/behaviours represented as smaller typed components. The editor assembly provides dedicated authoring tools over those same shared definition types.

This document is intended to remain the reference description of the C# architecture represented by the source revision stated at the top of the file.