# Ninth Age — C# Restyling Plan

**Status:** In progress  
**Repository:** `FrontierDev/ninthage`  
**Target branch:** `main`  
**Style authority:** `.docs/CSharp-Style-Conventions.md`  
**Current audit scope:** Assembly 1 — `Game.Core`  
**Assembly path:** `Assets/Runtime/Core/`  
**Audit date:** 2026-10-04

## 1. Purpose

This document records the changes required to bring the existing Ninth Age C# codebase into compliance with the project's locked C# style and naming conventions.

The audit is being performed one assembly at a time. This revision covers only the first assembly in the architecture document: `Game.Core`.

All changes in this plan are intended to be behaviour-preserving. Styling work must not be combined with gameplay, networking, persistence, scene, prefab, or architectural changes.

## 2. Safety Requirements

The following rules apply to every implementation phase of this restyling work:

1. Symbol renames must update all references atomically.
2. C# file renames must preserve the associated Unity `.meta` file and therefore the existing asset GUID.
3. Unity-serialized field renames must use `FormerlySerializedAs` where required so existing scene, prefab, and ScriptableObject values are not lost.
4. Persisted DTO/member names must not be changed without an explicit backward-compatible migration path.
5. Namespace/type changes that can affect `SerializeReference` data must be migrated and validated against existing assets.
6. RPC attributes, network payload layouts, and networking behaviour must remain unchanged.
7. Restyling changes must compile in the Unity Editor, client build, and dedicated-server build before the assembly is considered complete.
8. Any asset reserialization caused by a rename must be reviewed rather than accepted automatically.

## 3. Assembly 1 — `Game.Core`

### 3.1 Assembly definition

**File:** `Assets/Runtime/Core/Core.asmdef`

The C# styling conventions do not define formatting or naming rules for `.asmdef` JSON files. No restyling change is required to this file.

### 3.2 C# source inventory

`Game.Core` currently contains one C# source file:

- `Assets/Runtime/Core/GameBootstrapper.cs`

The file already complies with the following locked conventions:

- namespace mirrors the assembly folder: `Game.Core`;
- filename exactly matches the primary type: `GameBootstrapper.cs` / `GameBootstrapper`;
- type name uses PascalCase;
- method names use PascalCase;
- no project-defined acronym casing breaches are present;
- no constants or static readonly fields are present;
- no test/prototype types are present;
- no RPCs or callback/event-like members are present;
- control-flow braces are valid under the selected convention;
- expression-bodied members are not required;
- the `Lifecycle` region is permitted;
- no partial-class filename rule applies.

## 4. Required changes

### `Assets/Runtime/Core/GameBootstrapper.cs`

#### CORE-001 — Alphabetize `using` directives

**Current:**

```csharp
using UnityEngine;
using System.Collections;
using Unity.Entities;
```

**Required:**

```csharp
using System.Collections;
using Unity.Entities;
using UnityEngine;
```

**Reason:** The project convention requires `using` directives to be ordered alphabetically.

**Functional-safety note:** This changes source ordering only and has no runtime effect. Do not remove the currently present directives as part of this restyling item; unused-using cleanup is outside the locked convention set.

---

#### CORE-002 — Rename the private singleton field to `_camelCase`

**Current:**

```csharp
private static GameBootstrapper instance;
```

**Required:**

```csharp
private static GameBootstrapper _instance;
```

Update every reference within `GameBootstrapper`:

```csharp
if (_instance != null && _instance != this)
{
    Destroy(gameObject);
    return;
}

_instance = this;
```

**Reason:** Private instance and static fields use `_camelCase`.

**Functional-safety note:** `instance` is a private static field and is not Unity-serialized. The rename is therefore a compile-time symbol rename only. It must be performed atomically with all references in this class. No `FormerlySerializedAs` attribute is required.

---

#### CORE-003 — Add XML documentation to the public `GameBootstrapper` type

**Current:**

```csharp
public class GameBootstrapper : MonoBehaviour
```

**Required form:**

```csharp
/// <summary>
/// Selects the client or headless-server startup path and bootstraps the
/// corresponding runtime initialization sequence.
/// </summary>
public class GameBootstrapper : MonoBehaviour
```

The exact wording may be adjusted during implementation, but the documentation must accurately describe the existing responsibility and must not imply behaviour that the class does not provide.

**Reason:** Public APIs, including public types, require XML documentation.

**Functional-safety note:** Documentation-only change; no runtime effect.

---

#### CORE-004 — Use `var` for obvious local variable types in `IsHeadlessServer`

**Current:**

```csharp
bool isBatchMode = Application.isBatchMode;
bool hasNoGraphics =
    SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
```

**Required:**

```csharp
var isBatchMode = Application.isBatchMode;
var hasNoGraphics =
    SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
```

**Reason:** The project convention prefers `var` when the type is obvious from the right-hand side or surrounding context.

**Functional-safety note:** Both inferred types remain `bool`; generated behaviour is unchanged.

## 5. Fields deliberately not changed

`GameBootstrapper` currently contains:

```csharp
[SerializeField] protected bool forceServerMode = false;
```

The locked convention specifically requires `_camelCase` for **private** fields and private serialized Unity fields. It does not currently define a naming rule for protected fields.

Therefore `forceServerMode` is **not** listed as a convention breach and must not be renamed as part of the current restyling pass.

This is important because the field is Unity-serialized. Renaming it without an explicit convention requiring that change would introduce unnecessary serialization migration risk.

## 6. Game.Core implementation sequence

Apply the four changes in a single focused restyling change:

1. alphabetize the `using` directives;
2. rename `instance` to `_instance` and update its references;
3. add XML documentation to `GameBootstrapper`;
4. replace the two obvious local `bool` declarations with `var`.

No changes should be made to startup logic, headless-server detection, `forceServerMode`, `Game.Client.ClientInitialization.Begin(...)`, or `Game.Server.ServerInitialization.Begin(...)`.

## 7. Validation

`Game.Core` is complete only when all of the following pass:

- Unity Editor script compilation succeeds;
- client compilation succeeds;
- dedicated-server compilation succeeds;
- entering normal graphical execution still selects `ClientInitialization.Begin(...)`;
- batch/headless execution still selects `ServerInitialization.Begin(...)`;
- the editor `forceServerMode` behaviour remains unchanged;
- no scene or prefab serialized changes are produced by this assembly restyle;
- a re-audit of `GameBootstrapper.cs` reports no remaining breaches of the currently locked conventions.

## 8. Assembly result

| Item | Count |
|---|---:|
| C# files audited | 1 |
| Files requiring changes | 1 |
| Required convention changes | 4 |
| Serialized-field renames | 0 |
| Namespace changes | 0 |
| C# file renames | 0 |
| Type renames | 0 |
| RPC/network changes | 0 |
| Functional changes intended | 0 |

**Game.Core status:** Planned; not yet restyled.