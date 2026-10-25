# Ninth Age — C# Style and Naming Conventions

**Status:** Active project standard  
**Applies to:** Ninth Age-authored C# source code only; third-party code is excluded  
**Repository:** `FrontierDev/ninthage`  
**Established:** 2026-10-04

## 1. Purpose

This document defines the C# styling and naming conventions for the Ninth Age project. These conventions are normative for new C# code and should be followed when existing C# files are modified.

The purpose of the standard is to keep code visually consistent across the Client, Server, Shared, Core, and Editor assemblies while retaining project-specific naming patterns such as `UI_`, `SpellEffect_`, and directional RPC prefixes.

Where this document does not define a convention, no additional convention is implied.

### 1.1 Third-party exclusion — non-negotiable

**Mandatory third-party boundary:** Third-party code is read-only unless the user explicitly requests a change to the specific third-party code. This includes imported packages, plugins, SDKs, tools, and externally authored source anywhere in the repository (including `Assets/` and `Assets/Editor/`). **Never modify third-party namespaces**, identifiers, files, assembly definitions, metadata, or vendor-owned assets. Do not restyle, rename, relocate, patch, reformat, or regenerate them. Follow `AGENTS.md`; if the task requires a third-party change, stop and request explicit permission. GitHub issues, plans, general refactor requests, and coding standards do not grant that permission.

Ninth Age style and namespace conventions **must never** be applied to third-party code, even when that code is stored inside the Unity project or does not follow our naming rules.

## 2. Private Fields

Private instance and static fields use `_camelCase`.

```csharp
private int _maxSlots;
private Actor _currentTarget;
private static ServerActorManager _instance;
private readonly Dictionary<GUID, Actor> _actors = new();
```

This rule also applies to private fields that are serialized by Unity.

```csharp
[SerializeField] private CanvasGroup _canvasGroup;
[SerializeField] private float _fadeDuration = 0.3f;
[SerializeField] private List<UI_Window> _windows = new();
```

## 3. Type Naming

Classes, structs, enums, interfaces, delegates, and other named types use PascalCase unless they belong to an established type family that uses an underscore separator.

Standard types:

```csharp
ServerActorManager
CharacterCreationService
WorldPosition
ActorComponent
IInteractable
```

Established type-family prefixes retain the underscore convention:

```csharp
UI_Window
UI_ActionBarButton
SpellEffect_Damage
SpellTarget_Single
AuraEffect_Heal
AuraTarget_Caster
AuraBehaviour_DamageHostOnHit
Condition_HasAura
Quest_KillObjective
TalentBehaviour_DamageOnHit
VFX_ImpactEffect
```

The underscore separates the family name from the concrete implementation name. Existing families should remain internally consistent.

## 4. Acronyms and Identifiers

Acronyms and identifier abbreviations use uppercase letters within member and type names.

```csharp
ActorID
ClassID
RaceID
QuestID
GUID
PVPFaction
NPCActor
ClientECSManager
ClientVFXManager
ActorSFXController
```

This applies to identifier names, not framework type names. For example, `System.Guid` remains `Guid` because that is the framework type name.

Local variables and parameters follow the same acronym casing within camelCase names:

```csharp
var actorID = actor.ActorID;
var classID = character.ClassID;
var npcGUID = npc.GUID;
```

## 5. Constants

Constants use `SCREAMING_SNAKE_CASE`.

```csharp
private const float MAX_ALLOWED_SPEED = 15f;
private const int VIOLATION_THRESHOLD = 3;
private const float DEFAULT_FADE_TIME = 0.3f;
```

## 6. Static Readonly Fields

Static readonly fields also use `SCREAMING_SNAKE_CASE`.

```csharp
private static readonly string SETTINGS_PATH = "Assets/Editor/WorldEditor/Settings.json";
private static readonly Vector3 DEFAULT_OFFSET = Vector3.zero;
```

This intentionally gives immutable static values the same visual treatment as constants.

## 7. Test and Prototype Types

Test and prototype types use the `Test_` prefix.

```csharp
Test_CharacterController
Test_NPCPathfinder
Test_ClientSystem
```

Their filenames should match the type name:

```text
Test_CharacterController.cs
Test_NPCPathfinder.cs
Test_ClientSystem.cs
```

## 8. Local Variable Typing

Use `var` when the type is obvious from the right-hand side or surrounding context.

```csharp
var networkManager = GetComponent<NetworkManager>();
var actor = GetActor(actorID);
var changes = new List<InventorySlotChange>();
```

Explicit types remain appropriate where the type is not obvious or where writing the type materially improves readability.

```csharp
PlayerID owner = actor.Owner.Value;
IReadOnlyList<Actor> targets = ResolveTargets();
```

## 9. Object and Collection Construction

Prefer target-typed `new()` when the declared type is already explicit.

```csharp
private readonly List<Actor> _actors = new();
private readonly Dictionary<GUID, Actor> _actorsByID = new();

List<string> tags = new();
```

Use an explicit constructed type when target typing is unavailable or would make the expression unclear.

## 10. `using` Directive Ordering

`using` directives are ordered alphabetically.

```csharp
using Game.Shared;
using Game.Shared.Data;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;
```

Aliases participate in the same alphabetical ordering.

```csharp
using Debug = Game.Shared.FormattedDebug;
```

## 11. Namespace Structure

Namespaces mirror the C# folder hierarchy.

For example:

```text
Assets/Runtime/Client/UI/Inventory/UI_InventoryWindow.cs
```

uses:

```csharp
namespace Game.Client.UI.Inventory
```

Likewise:

```text
Assets/Runtime/Server/Services/CharacterCreationService.cs
```

uses:

```csharp
namespace Game.Server.Services
```

and:

```text
Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_Damage.cs
```

uses:

```csharp
namespace Game.Shared.Data.SpellEffectDefinitions
```

The namespace should therefore communicate both the architectural assembly and the feature/subsystem directory containing the source file.

## 12. RPC Naming

RPC methods retain an explicit direction prefix separated from the method name by an underscore.

```csharp
[ServerRpc]
public static void Server_RequestChunkInterest(...)
{
}

[TargetRpc]
public static void Client_UpdateInterest(...)
{
}

[ObserversRpc]
public static void Observers_SetTarget(...)
{
}
```

The prefixes `Server_`, `Client_`, and `Observers_` are intentional markers for network direction and should not be collapsed into ordinary PascalCase.

Non-RPC methods use normal PascalCase.

```csharp
LoadGlobalActorsScene();
SubscribePlayerToChunk();
ValidatePlayerPositions();
```

## 13. Callbacks and Events

Callbacks and event-like members use `on...` camelCase naming.

```csharp
public static Action<Actor> onActorDeath;
public static Action<Actor> onServerActorSpawned;
public static Action<Actor> onClientTargetedActorChanged;
public static Action<Vector2Int> onClientInterestRequest;
```

Scope or direction follows `on` where applicable:

```text
onServer...
onClient...
onActor...
```

Handler methods use normal PascalCase `On...` naming.

```csharp
private void OnActorSpawned(Actor actor)
{
}

private void OnClientConnectionStateChanged(ConnectionState state)
{
}
```

## 14. Control-Flow Braces

Braces may be omitted for a single statement.

```csharp
if (actor == null)
    return;

if (remaining <= 0) break;
```

Braces are required when a control-flow body contains multiple statements.

```csharp
if (actor != null)
{
    RegisterActor(actor);
    RefreshActor(actor);
}
```

Either braced or unbraced form is acceptable for a single statement; consistency within the immediate block should be maintained where practical.

## 15. Expression-Bodied Members

Expression-bodied syntax is allowed for properties and short methods.

```csharp
public bool Initialized => _initialized;
public int ItemCount => _items.Count;

private void OnHit(SpellContext context, Actor target) =>
    _damageEffect?.Execute(context, target);
```

Longer or multi-step methods use ordinary block bodies.

## 16. `#region` Usage

`#region` directives may be used to organise larger classes into meaningful sections.

```csharp
#region Lifecycle

private void Awake()
{
}

private void OnDestroy()
{
}

#endregion
```

Region names should describe coherent groups such as lifecycle, networking, window management, spellcasting, serialization, or editor functionality.

## 17. XML Documentation and Comments

Public APIs use XML documentation.

```csharp
/// <summary>
/// Loads the persistent global actor scene used for networked actors.
/// </summary>
public void LoadGlobalActorsScene()
{
}
```

Public properties, methods, types, and other externally consumed members should be documented sufficiently to explain their purpose and contract.

Implementation comments are used where they add useful context, explain non-obvious behaviour, or document an important sequencing or architectural requirement.

```csharp
// Subscribe before loading the scene so the initial scene callbacks are not missed.
_networkManager.sceneModule.onSceneLoaded += OnSceneLoaded;
```

Comments should explain intent or constraints rather than restating self-explanatory code.

## 18. Serialized Unity Fields

Private serialized Unity fields follow the same `_camelCase` convention as all other private fields.

```csharp
[SerializeField] private CanvasGroup _canvasGroup;
[SerializeField] private Sprite _icon;
[SerializeField] private float _baseCooldown;
[SerializeField] private List<SpellComponent> _baseComponents = new();
```

Read-only public accessors use PascalCase when required.

```csharp
public Sprite Icon => _icon;
public float BaseCooldown => _baseCooldown;
public IReadOnlyList<SpellComponent> BaseComponents => _baseComponents;
```

## 19. Public DTO and ECS Fields

Public PascalCase fields are allowed for DTOs, serialized data containers, network payloads, and ECS components.

```csharp
public struct ActorComponent : IComponentData
{
    public GUID ActorID;
    public PlayerID Owner;
    public double3 WorldPositionD;
}
```

```csharp
[Serializable]
public sealed class CharacterData
{
    public string Name;
    public string GUID;
    public int Level;
    public string ClassID;
    public string RaceID;
}
```

Behavioural classes may still expose properties when encapsulation is appropriate; this rule specifically permits public fields for data-oriented types.

## 20. Filename and Primary-Type Correspondence

A C# filename must match its primary type exactly, including casing.

```text
ServerActorManager.cs       -> ServerActorManager
UI_InventoryWindow.cs       -> UI_InventoryWindow
SpellEffect_Damage.cs       -> SpellEffect_Damage
Test_NPCPathfinder.cs       -> Test_NPCPathfinder
```

Duplicate extensions, copy suffixes, misspellings, or filenames that do not match the primary type are not part of the convention.

## 21. Partial-Class Filenames

Partial-class files use an underscore between the type name and section name.

```text
WorldEditorWindow_Base.cs
WorldEditorWindow_Input.cs
WorldEditorWindow_UI.cs
WorldEditorWindow_Terrain.cs
WorldEditorWindow_Erosion.cs
```

Each file still contains the same partial type:

```csharp
public partial class WorldEditorWindow : EditorWindow
{
}
```

## 22. Representative Example

```csharp
using Game.Shared;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI.Inventory
{
    /// <summary>
    /// Displays and manages the player's inventory window.
    /// </summary>
    public sealed class UI_InventoryWindow : UI_Window
    {
        private const float DEFAULT_FADE_TIME = 0.3f;
        private static readonly string DEFAULT_LAYOUT_PATH = "Inventory/Default";

        [SerializeField] private CanvasGroup _canvasGroup;

        private readonly List<ItemInstance> _items = new();

        /// <summary>
        /// Gets the number of items currently displayed by the window.
        /// </summary>
        public int ItemCount => _items.Count;

        #region Inventory

        /// <summary>
        /// Refreshes the displayed inventory from the current player state.
        /// </summary>
        public void Refresh()
        {
            if (_items.Count == 0)
                return;

            var inventory = GetComponent<PlayerInventory>();
            Populate(inventory);
        }

        #endregion

        /// <summary>
        /// Requests that the server move an item between inventory slots.
        /// </summary>
        [ServerRpc]
        public static void Server_RequestMoveItem(int fromSlot, int toSlot)
        {
            // Server-side inventory handling.
        }
    }
}
```

## 23. Minimal Implementation and Review Discipline

These rules apply **when changing existing code**, not just when choosing names and formatting. Code quality includes the amount of state, branching, and maintenance burden introduced.

### 23.1 Prefer the smallest correct change

- Make the smallest change that demonstrably satisfies the issue's observable acceptance criteria. Do not redesign adjacent subsystems to pre-empt hypothetical future needs.
- **Prefer existing owners, data and interfaces.** Before adding a manager, event bus, registry, cache, queue, callback layer, helper type, or parallel source of truth, identify the concrete invariant that existing code cannot enforce and explain why a local change cannot satisfy it.
- Do not duplicate authoritative state across registries unless synchronisation and invalidation are explicit and indispensable. A new dictionary/queue introduces its own lifetime, cancellation and failure paths.
- Avoid defensive fallbacks that hide invalid states or convert failed operations into apparent success. Surface the actual failure; preserve already-defined behaviour.
- Avoid abstractions built for only one speculative use case. A small amount of duplication is preferable to an unproven framework, but do not knowingly duplicate existing project-owned helpers.
- Limit behavioural modifications to the issue's exact operation and boundary. Do not rename, restyle, reformat, or optimise neighbouring code while implementing a fix.

### 23.2 Size and complexity expectations

These are **review triggers, not arbitrary hard limits**: ordinarily aim for **one behavioural invariant**, **one primary production subsystem**, **one to three production source files**, and **roughly 150 or fewer net new production lines** per implementation issue. Changes that add a new state owner, involve multiple authoritative managers, or require hundreds of lines should be treated as high-risk even if their net line count is small.

If a proposed change exceeds those targets, **stop before implementing** and either:
1. split it into smaller, individually verifiable changes with explicit dependencies and unchanged public contracts; or
2. document the smallest viable reason it cannot be split, enumerate the extra state/paths introduced, and obtain explicit owner approval for the larger scope.

Do not game the guideline by compressing code, omitting comments, or making many tightly coupled mini-commits that cannot be validated independently. Judge semantic complexity and reviewability first.

### 23.3 Evidence-based correctness review

Every requested corrective change must be classified as:

- **Confirmed defect:** a reproducible failing test, direct code-level proof of incorrect behaviour, or an explicit contradiction of an approved contract. Fix it in a narrowly scoped production issue.
- **Potential defect:** a plausible scenario without established incorrect behaviour. Write a focused failing regression test or investigate the contract first; do not automatically add production state or force another implementation revision.
- **Future concern:** a possible scalability or architectural improvement outside current acceptance criteria. Record it separately only when actionable; do not expand the current issue.

A reviewer should cite the exact path/method, entry conditions, observable incorrect state or callback sequence, and smallest correction. If expected semantics are not settled, request a design decision rather than presenting an assumption as a bug.

### 23.4 Validate boundaries without speculative machinery

For simulation, ECS, networking, and persistence changes, validate the specific lifecycle and ordering guarantee being modified (including teardown, reentrancy, and failure paths **when those are part of the affected contract**). Prefer deterministic tests at the smallest useful layer. Regression tests and production implementation remain **separate issues**, as defined in the GitHub workflow guide.

Compilation and static review do not imply runtime correctness. Do not claim an edge case is fixed without test or proof; likewise, do not repeatedly request code changes for unproven edge cases.

### 23.5 Establish an invariant once, at its owner

Every important invariant should have one clear owner and one boundary where it is established. Code downstream of that boundary should normally trust it.

Examples:

- server startup validates configuration and constructs managers in a defined order;
- actor registration establishes actor identity, spatial context, ECS entity mapping and required ECS components/buffers;
- a world/scene registry establishes that a registered key refers to owned runtime state;
- a spell/aura/ticker/cooldown manager validates authored duration or generation data when it creates the timed state;
- a coordinate-conversion helper owns the numeric range needed by that conversion;
- a public/network/request boundary validates external input before it enters trusted internal state.

Do **not** repeatedly re-check the same invariant in every internal caller, hot loop or helper. In particular:

- do not poll predecessor-manager `IsInitialized` state during ordinary gameplay when startup order already guarantees it;
- do not revalidate actor IDs, ECS component shape, context validity or finite canonical state after registration solely as defensive programming;
- do not perform a dictionary lookup and then re-prove that the returned object's ID matches the lookup key when the dictionary owner establishes that relationship;
- do not replace a removed redundant check with `Debug.Assert`, another helper guard or a second fallback unless a distinct runtime failure remains possible.

Impossible internal states may fail through the owned dictionary, ECS or API operation naturally. Add a bespoke failure only where it improves a real ownership boundary.

When reviewing or changing invariant-heavy code, write the invariant explicitly:

```text
invariant
→ owner that establishes it
→ point where it may legitimately change
→ consumers that may trust it
```

If no point exists where the truth can change between establishment and use, repeated validation is usually unnecessary work.

### 23.6 Distinguish immutable guarantees from dynamic runtime state

A check is required when the state can legitimately change before it is consumed. A check is not justified merely because a bad value can be imagined.

**Normally trust once established:**

- startup configuration and predecessor-manager availability;
- registered actor ID ↔ ECS entity relationships;
- required ECS component/buffer shape created during actor registration;
- immutable scene/runtime keys held by an owned registry;
- authored timing fields after their owning manager has validated and converted them;
- canonical accepted positions handed directly from the authority that committed them.

**Continue to validate/revalidate dynamic state:**

- network ownership and player/session replacement;
- actor/target liveness;
- current cast, aura or ticker generation;
- logical context after an operation that can transfer it;
- scene loaded/unloaded/pending state;
- NavMesh availability and enabled-agent state;
- asynchronous completion after an `await`, callback, Addressables/PurrNet completion, or other suspension point;
- data that external/network/user-authored input can still supply invalidly;
- collection membership when callbacks can re-enter and mutate that collection.

The rule is temporal: **can this state change between the point that proved it and this use?** If yes, revalidate at the use or resume boundary. If no, trust the owner.

### 23.7 Lifecycle transitions are ordered contracts

Construction, publication, retirement and destruction are architectural state transitions. Their order must be explicit.

For process-wide/singleton managers:

- initialise owned fields and subscriptions before publishing the singleton as ready;
- duplicate construction fails fast; do not log-and-return and leave a half-live component;
- teardown runs only on the exact instance that owns the singleton;
- an unregistered duplicate must never unsubscribe callbacks, clear shared state or reset the live singleton;
- teardown that may run after partial startup remains idempotent only where partial startup is a legitimate lifecycle state.

For authoritative actors:

- **authoritative gameplay retirement happens before network despawn**;
- once `NetworkIdentity.Despawn()` begins, the actor must no longer remain in gameplay registries capable of generating ticks, effects, stat replication, combat logs or other RPCs;
- explicit retirement may happen before Unity destroys the GameObject;
- `OnDestroy` may remain a fallback, but the retirement publication must be exactly-once/idempotent so explicit retirement followed by destruction does not run gameplay teardown twice.

Do not fix lifecycle-order bugs by scattering `isSpawned`, null or manager-readiness guards through RPC/effect code. Correct the owner transition.

Rollback follows the same rule: retain rollback only for mutations that can genuinely fail after earlier state has changed. Do not maintain transaction code around a synchronous operation whose precondition has already been proved and which has no reentrant/suspending step before completion.

### 23.8 Hot paths trust owners and avoid rediscovery

Per-tick, per-actor, per-candidate and per-event code must not repeatedly rediscover state already indexed by its owner.

Prefer:

- direct `ActorID -> Entity` or other owner mappings over full ECS/query scans for a known ID;
- registered component references/state over repeated `GetComponent` calls;
- one dictionary lookup over `ContainsKey` followed by another lookup;
- one query/buffer access over querying a buffer and then calling `GetBuffer` again;
- one computation of derived values such as chunk/cell coordinates per operation;
- immutable configuration cached at initialization when it is genuinely server-lifetime configuration.

Do not add a new reverse index, cache or duplicate registry merely to eliminate a scan unless:
1. the scan is material or on a proven hot path;
2. no existing owner already provides the lookup;
3. update/removal ownership for the new state is explicit.

The first optimisation question is therefore **"which existing owner already knows this?"**, not **"what should we cache?"**.

### 23.9 Allocation and snapshot discipline

Avoid allocation churn in authoritative loops, but preserve mutation/reentrancy isolation.

- Reuse private scratch lists/buffers when one owner controls their lifetime and clears them deterministically.
- Do not call `ToArray()` merely to obtain a stable snapshot when a reusable owned list provides the same isolation.
- Do not allocate temporary one-element collections when direct execution has identical semantics.
- Do not allocate an ECB or other temporary command container on an idle tick when no command will be produced.
- Preserve a real snapshot boundary when callbacks may enqueue, cancel or otherwise mutate the live source collection during iteration.
- All queues that must observe the same mutation boundary must be snapshotted before any of them begin committing; allocation removal must not weaken that semantic boundary.

A reusable buffer is temporary storage, **not a second source of truth**. It must not acquire independent lifecycle or authority semantics.

### 23.10 Failure semantics must be explicit

An operation that claims to perform work must either perform it or surface failure according to its contract.

- A void command/queue method must not log an error and return as though the operation was accepted when nothing was queued.
- Do not silently substitute an invalid prefab, scene, entity, context or registration with a fallback that violates the caller's requested operation.
- Do not self-heal internal registries unless recovery is an explicit subsystem responsibility.
- Idempotent absence is appropriate only for cleanup/cancellation APIs whose contract explicitly permits already-removed state.
- Fail fast at the boundary that owns malformed authored/configuration/input data; do not defer the failure into a hot simulation loop.
- Preserve graceful handling for external or asynchronous outcomes that are expected runtime possibilities.

The project preference is **one clear failure at the responsible boundary**, not many defensive branches downstream.


## 24. Numeric Types and Precision

Choose numeric types according to **value semantics, range, required precision, and storage/transfer cost**. Do not routinely select the smallest or largest type, and do not change an existing serialized, networked, persisted, or gameplay-facing value type without an explicitly scoped compatibility review.

| Type | Size | Preferred use |
|---|---:|---|
| `int` | 4 bytes | Exact discrete counts, array indices, stack counts, and integer gameplay quantities where the existing contract uses whole units. |
| `long` | 8 bytes | Exact simulation tick IDs, due-tick deadlines, and counters that may exceed `int` range. |
| `float` | 4 bytes | Local movement/physics/rendering values, fractional combat modifiers, and authored gameplay durations when the existing data contract is single-precision. Approximately 6–7 significant decimal digits. |
| `double` | 8 bytes | Authoritative absolute world coordinates and elapsed-time calculations that genuinely require additional precision. Approximately 15–16 significant decimal digits. |
| `decimal` | 16 bytes | Explicitly required exact **decimal** quantities (for example, monetary accounting); not a default for combat, ECS simulation, or graphics. |

**Simulation time:** Use `long` tick IDs and integer due-tick deadlines for authoritative scheduling, not accumulated `float` or `double` countdowns. Authorable seconds may remain `float` as required by existing definitions; convert to ticks once using the approved non-shortening rounding contract from [Performance and Simulation Architecture](Performance-and-Simulation-Architecture.md#33-one-clock-owner-identity-lifecycle-and-configuration--proposed). Binary floating-point cannot represent many simple decimal fractions exactly: test nominal tick multiples and values on either side of deadlines; do not silently invent a conversion epsilon or change gameplay durations.

**Coordinates:** Preserve `double`/`double3` for server-authoritative absolute world positions and `float`/`float3`/`Vector3` for Unity-local physics and rendering, as defined in [C# Architecture](CSharp-Architecture.md#16-floating-origin-and-position-architecture). Do not make large-world positions authoritative in local `float` space.

**Costs:** `int` and `float` each occupy 4 bytes; `long` and `double` each occupy 8 bytes. This matters for dense ECS data, cache use, network payloads, and memory bandwidth. Both `float` and `double` are ordinarily efficient on modern CPUs, but `double` generally costs more storage and can be substantially more expensive than `float` on graphics hardware. Choose for correctness first; benchmark hotspots before type-based performance rewrites. Use checked integer arithmetic where tick/deadline overflow could violate scheduling correctness.

## 25. Convention Summary

| Category | Standard |
|---|---|
| Private fields | `_camelCase` |
| Serialized private fields | `_camelCase` |
| Normal types | `PascalCase` |
| Type families | Preserve underscore families such as `UI_` and `SpellEffect_` |
| Acronyms | Uppercase, e.g. `ID`, `GUID`, `PVP`, `NPC`, `ECS` |
| Constants | `SCREAMING_SNAKE_CASE` |
| Static readonly fields | `SCREAMING_SNAKE_CASE` |
| Tests/prototypes | `Test_Foo` |
| Local typing | Prefer `var` when obvious |
| Construction | Prefer target-typed `new()` |
| Numeric types | Choose for exactness, precision, range and data-layout cost; authoritative deadlines use `long` ticks |
| `using` directives | Alphabetical |
| Namespaces | Mirror folders |
| RPCs | `Server_Foo`, `Client_Foo`, `Observers_Foo` |
| Callbacks/events | `on...` camelCase |
| Event handlers | `On...` PascalCase |
| Single-statement braces | May be omitted |
| Expression-bodied members | Allowed for properties and short methods |
| `#region` | Allowed for meaningful organisation |
| Public API documentation | XML documentation required |
| Invariants | Establish once at the owning boundary; stable internal consumers trust them |
| Dynamic/async state | Revalidate only where state can legitimately change or after suspension/reentrancy |
| Singleton lifecycle | Publish after initialization; duplicate creation fails; only the registered instance tears down |
| Actor teardown | Retire authoritative gameplay state before network despawn; `OnDestroy` is a one-shot fallback |
| Hot-path lookup | Use existing owner mappings/references; avoid scans and repeated rediscovery |
| Scratch/snapshots | Reuse owned temporary buffers where safe; preserve real mutation/reentrancy snapshot boundaries |
| Failure semantics | Fail at the responsible boundary; no silent success/fallback for an operation that did not occur |
| DTO/ECS fields | Public PascalCase fields allowed |
| Normal filenames | Exact primary-type match |
| Partial filenames | `TypeName_Section.cs` |