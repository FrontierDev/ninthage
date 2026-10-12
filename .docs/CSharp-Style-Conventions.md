# Ninth Age — C# Style and Naming Conventions

**Status:** Active project standard  
**Applies to:** C# source code in the Ninth Age project  
**Repository:** `FrontierDev/ninthage`  
**Established:** 2026-10-04

## 1. Purpose

This document defines the C# styling and naming conventions for the Ninth Age project. These conventions are normative for new C# code and should be followed when existing C# files are modified.

The purpose of the standard is to keep code visually consistent across the Client, Server, Shared, Core, and Editor assemblies while retaining project-specific naming patterns such as `UI_`, `SpellEffect_`, and directional RPC prefixes.

Where this document does not define a convention, no additional convention is implied.

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

## 23. Convention Summary

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
| `using` directives | Alphabetical |
| Namespaces | Mirror folders |
| RPCs | `Server_Foo`, `Client_Foo`, `Observers_Foo` |
| Callbacks/events | `on...` camelCase |
| Event handlers | `On...` PascalCase |
| Single-statement braces | May be omitted |
| Expression-bodied members | Allowed for properties and short methods |
| `#region` | Allowed for meaningful organisation |
| Public API documentation | XML documentation required |
| DTO/ECS fields | Public PascalCase fields allowed |
| Normal filenames | Exact primary-type match |
| Partial filenames | `TypeName_Section.cs` |