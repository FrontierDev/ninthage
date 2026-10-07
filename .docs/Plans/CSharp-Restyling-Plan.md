# Ninth Age — C# Restyling Plan

**Status:** In progress  
**Repository:** `FrontierDev/ninthage`  
**Target branch:** `main`  
**Style authority:** `.docs/CSharp-Style-Conventions.md`  
**Current audit scope:** Assemblies 1–2 — `Game.Core`, `Game.Shared`  
**Assembly paths:** `Assets/Runtime/Core/`, `Assets/Runtime/Shared/`  
**Audit date:** 2026-10-04

## 1. Purpose

This document records the changes required to bring the existing Ninth Age C# codebase into compliance with the project's locked C# style and naming conventions.

The audit is being performed one assembly at a time. This revision covers the first two assemblies in the architecture document: `Game.Core` and `Game.Shared`.

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

# Assembly 2 — `Game.Shared`

## 9. Shared assembly scope

**Assembly definition:** `Assets/Runtime/Shared/Shared.asmdef`  
**Source root:** `Assets/Runtime/Shared/`  
**Audit baseline:** `f3d503e3dbd168f418040e33af93d37395996094`  
**C# files audited:** 130

The C# styling conventions do not define formatting or naming rules for `.asmdef` JSON files. No restyling change is required to `Shared.asmdef`.

The Shared assembly is the highest-risk restyling pass because it contains serialized ScriptableObject data, `[SerializeReference]` polymorphic data, persistence DTOs, PurrNet RPC/network types, runtime MonoBehaviours, and types referenced from Client, Server, Core, and Editor assemblies.

## 10. Shared-specific compatibility rules

The following safeguards are mandatory while implementing the changes listed below.

1. **Serialized private fields:** for every Unity-serialized field rename, add `UnityEngine.Serialization.FormerlySerializedAsAttribute` with the exact old field name before changing the identifier. Add and alphabetize `using UnityEngine.Serialization;` where required. Retain the migration attribute until existing assets have been loaded/resaved and migration is verified.
2. **Serialized property paths:** `FormerlySerializedAs` does not update Editor code that calls `SerializedProperty.FindProperty(...)`, `FindPropertyRelative(...)`, reflection APIs, or other string-based field paths. Search the entire repository for every renamed serialized field name and update those string references in the same implementation change.
3. **Managed-reference type moves:** namespace or type-name changes affecting classes stored via `[SerializeReference]` must use an explicit Unity type migration, such as `MovedFromAttribute` where applicable, and must be validated against existing assets. This applies particularly to effect, target, condition, quest-objective, talent-behaviour, and spell-modifier hierarchies.
4. **MonoBehaviour/ScriptableObject file moves:** preserve the existing `.meta` file for every C# filename change so the Unity asset GUID is retained.
5. **Persistence DTOs:** changes to fields serialized by `JsonUtility` are not safely migrated by `FormerlySerializedAs` alone. `CharacterData` and its nested saved-entry structs require a backward-compatible loader/migration that accepts the old JSON field names before those identifiers are changed.
6. **Network compatibility:** RPC method renames must preserve the existing PurrNet attribute, parameters, direction, ownership requirements, and runtime behaviour. Client and server must be rebuilt from the same revision. Validate whether PurrNet derives RPC IDs from method names; if it does, mixed-version compatibility must not be assumed.
7. **Cross-assembly namespace updates:** every namespace move in this section requires coordinated updates in Client, Server, Core, Editor, tests, `using` directives, fully-qualified references, generic constraints, attributes, and any string-based type lookup.
8. **Reflection:** `ActorSpellcaster.CloneModifier` currently searches for a field named `"source"`. Any modifier field rename to `_source` must update this reflection path while retaining support for modifier types that still legitimately expose a public `source` field.
9. **Acronyms:** apply uppercase acronym casing only to project-defined identifiers. Framework names such as `System.Guid`, `NetworkIdentity`, and the word `Identity` are not changed.
10. **Preferred syntax rules:** `var` and target-typed `new()` are preference conventions. Apply them only when the inferred/target type is unambiguous and readability is not reduced.

## 11. File-by-file audit

### 11.1 Components

#### `Assets/Runtime/Shared/Components/TerrainTileComponent.cs`

- Namespace: `Game.Runtime.Shared` → `Game.Shared.Components`; update all references atomically.
- Serialized fields: `tileX` → `_tileX`, `tileZ` → `_tileZ`, `hasBeenEroded` → `_hasBeenEroded`, `lastErosionProfileUsed` → `_lastErosionProfileUsed`, `zoneID` → `_zoneID`; add `FormerlySerializedAs` for each old name.
- Acronym casing: `ShowPoiMarker` → `ShowPOIMarker`; update all references. `_showPoiMarker` → `_showPOIMarker` because the acronym occurs after the camel-case prefix word.
- Acronym casing: `PoiLabel` → `POILabel`, `PoiTextSize` → `POITextSize`, `PoiColor` → `POIColor`; update all references. The leading private backing names `_poiLabel`, `_poiTextSize`, `_poiColor` may remain camelCase because the acronym is the first camel-case word.
- Construction: `_poiColor = new Color(...)` → `_poiColor = new(...)`.
- XML documentation: add docs to `ShowPOIMarker`, `POILabel`, `POITextSize`, and `POIColor`.
- Safety: update Editor `SerializedProperty`/reflection paths for the renamed serialized fields in the same change.

### 11.2 Data — root definitions and libraries

#### `Assets/Runtime/Shared/Data/ActorStatDefinition.cs`

- Serialized fields: `description` → `_description`, `icon` → `_icon`, `category` → `_category`, `baseValueMode` → `_baseValueMode`, `baseValue` → `_baseValue`, `sourceStat` → `_sourceStat`, `multiplier` → `_multiplier`, `startsAtZero` → `_startsAtZero`, `regenMode` → `_regenMode`, `regenPerSecond` → `_regenPerSecond`, `regenSourceStat` → `_regenSourceStat`, `regenMultiplier` → `_regenMultiplier`, `replicationMode` → `_replicationMode`, `tags` → `_tags`; add `FormerlySerializedAs` for every old name.
- XML documentation: add docs to `ActorStatCategory`, `ActorStatBaseValueMode`, `ActorStatReplicationMode`, `Icon`, `ReplicationMode`, `Tags`, `RegenMode`, `RegenPerSecond`, `RegenSourceStat`, and `RegenMultiplier`.
- Safety: update all Editor serialized-property names for the renamed fields.

#### `Assets/Runtime/Shared/Data/ActorStatDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/ActorStatScaling.cs`

- Filename: `ActorStatScaling.cs` → `StatScaling.cs`; preserve the existing `.meta` file.
- XML documentation: add docs to `StatScaling`, `Stat`, and `Coefficient`.

#### `Assets/Runtime/Shared/Data/AuraDefinition.cs`

- `AuraComponent.Guid` → `AuraComponent.GUID`; update all code references.
- Serialized fields in `AuraDefinition`: `description` → `_description`, `icon` → `_icon`, `isDebuff` → `_isDebuff`, `baseDuration` → `_baseDuration`, `baseTickInterval` → `_baseTickInterval`, `stackBehavior` → `_stackBehavior`, `maxStacks` → `_maxStacks`, `tags` → `_tags`, `components` → `_components`, `auraBehaviour` → `_auraBehaviour`; add `FormerlySerializedAs` for each.
- XML documentation: add docs to `AuraBehaviour`, `AuraBehaviour.Description`, `AuraBehaviour.OnApplied`, `AuraBehaviour.OnRemoved`, `AuraPhase`, `AuraStackBehavior`, `AuraComponent`, `AuraComponent.GUID`, `CastPhase`, `EffectDefinition`, `TargetDefinition`, and the public `AuraDefinition` properties `AuraBehaviour`, `Description`, `Icon`, `IsDebuff`, `BaseDuration`, `BaseTickInterval`, `StackBehavior`, `MaxStacks`, `Tags`, and `Components`.
- Safety: `AuraComponent` contains `[SerializeReference]` members; validate all existing aura assets after the derived-type namespace migrations listed below.

#### `Assets/Runtime/Shared/Data/AuraDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/AuraEffectDefinition.cs`

- XML documentation: add docs to `IAutoDescription`, its `Description` contract if undocumented, `AuraActorEvent`, `AuraEffectDefinition`, and `AuraEffectDefinition.Execute`.

#### `Assets/Runtime/Shared/Data/AuraTargetDefinition.cs`

- XML documentation: add docs to `AuraTargetDefinition` and `Evaluate`.

#### `Assets/Runtime/Shared/Data/ClassDefinition.cs`

- Serialized fields: `description` → `_description`, `icon` → `_icon`, `statGrowthPerLevel` → `_statGrowthPerLevel`, `baseStats` → `_baseStats`, `classSpellList` → `_classSpellList`, `focusStats` → `_focusStats`, `relevantStats` → `_relevantStats`, `weaponTypes` → `_weaponTypes`, `specialisations` → `_specialisations`, `classBehaviour` → `_classBehaviour`; add `FormerlySerializedAs` for each.
- XML documentation: add docs to `ClassStatGrowth` and its public fields `stat`/`flatPerLevel`; `ClassSpell` and `spell`/`levelRequirement`; `ClassSpecialisation` and `name`/`description`/`icon`/`spells`/`talents`/`color`; and the public properties `ClassSpellList`, `BaseStats`, `FocusStats`, `RelevantStats`, `WeaponTypes`, `Specialisations`, and `ClassBehaviour`.
- Safety: update all Editor serialized-property paths for these fields.

#### `Assets/Runtime/Shared/Data/ClassDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/DamageSchoolDefinition.cs`

- Serialized fields: `icon` → `_icon`, `mitigationRatingStat` → `_mitigationRatingStat`, `ratingPerPercent` → `_ratingPerPercent`; add `FormerlySerializedAs`.
- XML documentation: add docs to `Icon`, `MitigationRatingStat`, `RatingPerPercent`, and `CalculateMitigation`.

#### `Assets/Runtime/Shared/Data/DamageSchoolDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/DataDefinition.cs`

- Serialized field: `definitionId` → `_definitionID`; add `[FormerlySerializedAs("definitionId")]`.
- Serialized field: `displayName` → `_displayName`; add `[FormerlySerializedAs("displayName")]`.
- Public property: `DefinitionId` → `DefinitionID`; update every repository reference.
- Safety: `DefinitionID` is pervasive across data, runtime, server, client, and editor code. Implement as one symbol-wide rename and separately update any string-based Editor/property lookups.

#### `Assets/Runtime/Shared/Data/DataDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Serialized field: `definitions` → `_definitions`; add `[FormerlySerializedAs("definitions")]`.
- Parameters: `definitionId` → `definitionID` in `GetDefinition` and `HasDefinition`; update named arguments.
- Construction: `new List<T>()` → `new()`.
- Preferred local syntax: obvious constructor-created locals such as `seenIds = new HashSet<string>()` may use `var`.
- Safety: update editor tooling that accesses the serialized `definitions` property by string.

#### `Assets/Runtime/Shared/Data/FactionDefinition.cs`

- Serialized fields: `description` → `_description`, `category` → `_category`, `icon` → `_icon`, `color` → `_color`, `isPlayable` → `_isPlayable`, `canWar` → `_canWar`, `maxPoints` → `_maxPoints`, `alliedFactionIDs` → `_alliedFactionIDs`, `enemyFactionIDs` → `_enemyFactionIDs`, `rewardDescriptions` → `_rewardDescriptions`; add `FormerlySerializedAs` for each.
- XML documentation: add docs to `FactionCategory`; `FactionRewardDescriptionEntry` and its fields `threshold`, `isPositive`, `icon`, `title`, `benefits`, `penalties`; and `CanWar`, `MaxPoints`, `AlliedFactionIDs`, `EnemyFactionIDs`, `RewardDescriptions`.

#### `Assets/Runtime/Shared/Data/FactionDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/ItemDefinition.cs`

- Serialized fields in `ItemStat`: `stat` → `_stat`, `value` → `_value`; add `FormerlySerializedAs`.
- Serialized fields in `ItemDefinition`: `quality` → `_quality`, `icon` → `_icon`, `description` → `_description`, `itemSetKey` → `_itemSetKey`, `uniqueFlag` → `_uniqueFlag`, `bindingFlag` → `_bindingFlag`, `canStack` → `_canStack`, `maxStackSize` → `_maxStackSize`, `canTrade` → `_canTrade`, `canSell` → `_canSell`, `sellPrice` → `_sellPrice`, `canDisenchant` → `_canDisenchant`, `itemType` → `_itemType`, `weaponType` → `_weaponType`, `armorWeight` → `_armorWeight`, `isTwoHanded` → `_isTwoHanded`, `damagePerSecond` → `_damagePerSecond`, `swingTimer` → `_swingTimer`, `damageSchool` → `_damageSchool`, `validSlots` → `_validSlots`, `stats` → `_stats`, `conditions` → `_conditions`, `onEquipAction` → `_onEquipAction`, `onUnequipAction` → `_onUnequipAction`; add `FormerlySerializedAs` for every renamed serialized field.
- Construction: `validSlots`, `stats`, and `conditions` explicit constructors → target-typed `new()`.
- XML documentation: add docs to `ItemStat`, `Stat`, `Value`, and all currently undocumented public `ItemDefinition` accessors: `Quality`, `Rarity`, `Icon`, `Description`, `ItemSetKey`, `ItemType`, `WeaponType`, `ArmorWeight`, `IsTwoHanded`, `DamagePerSecond`, `SwingTimer`, `DamageSchool`, `UniqueFlag`, `CanStack`, `MaxStackSize`, `CanTrade`, `CanSell`, `SellPrice`, `CanDisenchant`, `ValidSlots`, `Stats`, `Conditions`.
- Safety: update all custom editor/drawer property paths for the renamed fields.

#### `Assets/Runtime/Shared/Data/ItemDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/LootTableDefinition.cs`

- Serialized fields in `LootTableEntry`: `item` → `_item`, `weight` → `_weight`, `chance` → `_chance`; add `FormerlySerializedAs`.
- Serialized fields in `LootTableDefinition`: `rollChance` → `_rollChance`, `nullRollChance` → `_nullRollChance`, `entryDefinitions` → `_entryDefinitions`; add `FormerlySerializedAs`.
- XML documentation: add docs to `LootTableEntry`, `Item`, `Weight`, `Chance`, `SetDropChance`, `RollChance`, `NullRollChance`, and `EntryDefinitions`.

#### `Assets/Runtime/Shared/Data/LootTableDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/ModifierEntry.cs`

- Alphabetize `using` directives.
- XML documentation: add docs to `ModifierEntry` and `Modifier`.

#### `Assets/Runtime/Shared/Data/QuestDefinition.cs`

- Serialized fields in `QuestReputationReward`: `faction` → `_faction`, `reputationPoints` → `_reputationPoints`.
- Serialized fields in `QuestItemReward`: `item` → `_item`, `quantity` → `_quantity`.
- Serialized fields in `QuestDefinition`: `icon` → `_icon`, `description` → `_description`, `level` → `_level`, `type` → `_type`, `recommendedPlayers` → `_recommendedPlayers`, `timerSeconds` → `_timerSeconds`, `goldReward` → `_goldReward`, `experienceReward` → `_experienceReward`, `reputationReward` → `_reputationReward`, `itemRewards` → `_itemRewards`, `cooldown` → `_cooldown`, `isRepeatable` → `_isRepeatable`, `objectives` → `_objectives`, `requirements` → `_requirements`; add `FormerlySerializedAs` to every old serialized name.
- Construction: explicit `List<QuestItemReward>`, `List<QuestObjective>`, and `List<ConditionDefinition>` initializers → target-typed `new()`.
- XML documentation: add docs to `QuestType`, `QuestCooldown`, `QuestReputationReward`, `Faction`, `ReputationPoints`, both `QuestReputationReward` constructors, `QuestItemReward`, `Item`, `Quantity`, both `QuestItemReward` constructors, and all currently undocumented `QuestDefinition` accessors: `Icon`, `Description`, `Level`, `Type`, `RecommendedPlayers`, `TimerSeconds`, `GoldReward`, `ExperienceReward`, `ReputationReward`, `ItemRewards`, `GiveAllItems`, `Cooldown`, `IsRepeatable`, `Objectives`, `Requirements`.
- Safety: `objectives` and `requirements` contain polymorphic serialized data; update Editor property paths and validate all quest assets.

#### `Assets/Runtime/Shared/Data/QuestDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/QuestObjective.cs`

- Serialized fields: `description` → `_description`, `requiredAmount` → `_requiredAmount`; add `FormerlySerializedAs`.
- XML documentation: add docs to `QuestObjective`, `Description`, `RequiredAmount`, and both constructors.

#### `Assets/Runtime/Shared/Data/RaceDefinition.cs`

- Serialized fields: `description` → `_description`, `icon` → `_icon`, `isPlayable` → `_isPlayable`, `defaultPVPFaction` → `_defaultPVPFaction`, `startingReputations` → `_startingReputations`, `baseStats` → `_baseStats`; add `FormerlySerializedAs`.- XML documentation: add docs to `PlayerStartingReputation`, `Faction`, `Reputation`, `DefaultPVPFaction`, `StartingReputations`, and `BaseStats`.

#### `Assets/Runtime/Shared/Data/RaceDefintionLibrary.cs`

- Filename typo: `RaceDefintionLibrary.cs` → `RaceDefinitionLibrary.cs`; preserve the existing `.meta` file.
- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/SpellDefinition.cs`

- `SpellComponent.Guid` → `SpellComponent.GUID`; update all references.
- Serialized fields in `SpellDefinition`: `spellName` → `_spellName`, `description` → `_description`, `icon` → `_icon`, `tags` → `_tags`, `internalTags` → `_internalTags`, `baseCastTime` → `_baseCastTime`, `baseCooldown` → `_baseCooldown`, `baseCharges` → `_baseCharges`, `useCooldownCharges` → `_useCooldownCharges`, `cooldownScalesWithHaste` → `_cooldownScalesWithHaste`, `triggersGCD` → `_triggersGCD`, `baseRange` → `_baseRange`, `isChanneled` → `_isChanneled`, `canMoveWhileCasting` → `_canMoveWhileCasting`, `baseTotalTicks` → `_baseTotalTicks`, `baseResourceCosts` → `_baseResourceCosts`, `baseComponents` → `_baseComponents`, `racialVariants` → `_racialVariants`, `sfxCastingLoop` → `_sfxCastingLoop`, `associatedClasses` → `_associatedClasses`; add `FormerlySerializedAs` for each.
- Construction inside validation: replace `internalTags = new List<string>()` with target-typed `new()`.
- XML documentation: add docs to `SpellCastPhase`, `SpellHitType`, `SpellDamageType`, `SpellComponent` and its public fields, `SpellResourceCost` and its currently undocumented public fields, `SpellNameVariant` and its fields, all public `SpellDefinition` accessors, and `AddAssociatedClass`.
- Safety: update all Editor serialized-property paths. `SpellComponent` contains `[SerializeReference]` members, so validate every spell asset after effect/target namespace migration.

#### `Assets/Runtime/Shared/Data/SpellDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

#### `Assets/Runtime/Shared/Data/SpellEffectDefinition.cs`

- XML documentation: add docs to `SpellActorEvent`, `SpellActorEventTarget`, `SpellEffectDefinition`, `Clone`, and `Execute`.

#### `Assets/Runtime/Shared/Data/SpellTargetDefinition.cs`

- XML documentation: add docs to `SpellTargetDefinition`, `RequiresTarget`, `Clone`, and `Evaluate`.

#### `Assets/Runtime/Shared/Data/TalentDefinition.cs`

- Serialized fields: `description` → `_description`, `icon` → `_icon`, `maxRank` → `_maxRank`, `minLevel` → `_minLevel`, `prerequisiteTalentIDs` → `_prerequisiteTalentIDs`, `modifiers` → `_modifiers`, `tags` → `_tags`, `associatedSpecialisations` → `_associatedSpecialisations`, `talentBehaviour` → `_talentBehaviour`; add `FormerlySerializedAs`.
- XML documentation: add docs to `TalentBehaviour`, `OnActivate`, `OnDeactivate`, `MinLevel`, `Modifiers`, `Tags`, `AssociatedSpecialisations`, `TalentBehaviour`, and `AddAssociatedSpecialisation`.
- Safety: `modifiers` and behaviour data are polymorphic; update Editor property paths and validate all talent assets after namespace/type moves.

#### `Assets/Runtime/Shared/Data/TalentDefinitionLibrary.cs`

- Alphabetize `using` directives.
- Private field: `instance` → `_instance`.

### 11.3 Data — aura behaviours/effects/targets

#### `Assets/Runtime/Shared/Data/AuraBehaviours/AuraBehaviour_DamageHostOnHit copy.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraBehaviours`.
- Filename: `AuraBehaviour_DamageHostOnHit copy.cs` → `AuraBehaviour_DamageHostOnHit.cs`; preserve `.meta`.
- Constant: `note` → `NOTE`.
- Serialized field: `damageEffect` → `_damageEffect`; add `[FormerlySerializedAs("damageEffect")]`.
- XML documentation: add docs to `AuraBehaviour_DamageHostOnHit`, `Description`, `OnApplied`, and `OnRemoved`.
- Safety: the moved concrete behaviour/effect type must remain deserializable from existing assets.

#### `Assets/Runtime/Shared/Data/AuraBehaviours/AuraBehaviour_HealAttackerOnHit.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraBehaviours`.
- Constant: `note` → `NOTE`.
- Serialized field: `healEffect` → `_healEffect`; add `[FormerlySerializedAs("healEffect")]`.
- XML documentation: add docs to the type, `Description`, `OnApplied`, and `OnRemoved`.

#### `Assets/Runtime/Shared/Data/AuraEffectDefinitions/AuraEffect_Damage.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraEffectDefinitions`.
- Alphabetize `using` directives.
- Constant: `StatHealth` → `STAT_HEALTH`.
- XML documentation: add docs to `AuraEffect_Damage`, `Description`, `BaseDamage`, `StatScaling`, `DamageSchools`, and `Execute`.
- Safety: add managed-reference type migration for the namespace move.

#### `Assets/Runtime/Shared/Data/AuraEffectDefinitions/AuraEffect_Heal.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraEffectDefinitions`.
- Alphabetize `using` directives.
- Constant: `StatHealth` → `STAT_HEALTH`.
- XML documentation: add docs to `AuraEffect_Heal`, `Description`, `BaseHealing`, `StatScaling`, and `Execute`.
- Safety: add managed-reference type migration for the namespace move.

#### `Assets/Runtime/Shared/Data/AuraTargetDefinitions/AuraTarget_Caster.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraTargetDefinitions`.
- XML documentation: add docs to `AuraTarget_Caster` and `Evaluate`.
- Safety: add managed-reference type migration for the namespace move.

#### `Assets/Runtime/Shared/Data/AuraTargetDefinitions/AuraTarget_Target.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.AuraTargetDefinitions`.
- XML documentation: add docs to `AuraTarget_Target` and `Evaluate`.
- Safety: add managed-reference type migration for the namespace move.

### 11.4 Data — class behaviours and conditions

#### `Assets/Runtime/Shared/Data/ClassBehaviours/ClassBehaviour.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.ClassBehaviours`.
- XML documentation: add docs to `IClassBehaviour`, `ResourcePrefab`, `OnActivate`, `OnDeactivate`, `ClassBehaviour`, and its public abstract members.
- Safety: update all references to both `IClassBehaviour` and `ClassBehaviour`.

#### `Assets/Runtime/Shared/Data/ClassBehaviours/SorcererClassBehaviour.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.ClassBehaviours`.
- Alphabetize `using` directives.
- Constants: `UnstableThreshold` → `UNSTABLE_THRESHOLD`, `CriticalThreshold` → `CRITICAL_THRESHOLD`, `UnstableDamagePerTick` → `UNSTABLE_DAMAGE_PER_TICK`, `CriticalDamagePerTick` → `CRITICAL_DAMAGE_PER_TICK`, `DecayDelay` → `DECAY_DELAY`, `DecayPerSecond` → `DECAY_PER_SECOND`, `UnstableSurgeRatio` → `UNSTABLE_SURGE_RATIO`, `CriticalSurgeRatio` → `CRITICAL_SURGE_RATIO`, `SurgeDecayPerSecond` → `SURGE_DECAY_PER_SECOND`.
- Serialized fields: `instabilityDamageSchool` → `_instabilityDamageSchool`, `resourcePrefab` → `_resourcePrefab`; add `FormerlySerializedAs`.
- Acronym casing: nested `RiftData.VFXId` → `VFXID`; parameter `vfxId` → `vfxID`.
- XML documentation: add docs to `SorcererClassBehaviour`, the public `RiftData` fields `Position`, `Radius`, `VFXID`, `ResourcePrefab`, `OnActivate`, and `OnDeactivate`.
- Construction: explicit `new List<DamageSchoolDefinition>()` in combat-log creation → target-typed `new()` where target typing is available.
- Safety: preserve the script `.meta`; validate any serialized class-behaviour references after namespace migration.

#### `Assets/Runtime/Shared/Data/ClassBehaviours/TestClassBehaviour.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.ClassBehaviours`.
- Alphabetize `using` directives.
- Type: `TestClassBehaviour` → `Test_ClassBehaviour`.
- Filename: `TestClassBehaviour.cs` → `Test_ClassBehaviour.cs`; preserve `.meta`.
- Serialized fields: `manaPerHit` → `_manaPerHit`, `manaOnDamageTaken` → `_manaOnDamageTaken`, `resourcePrefab` → `_resourcePrefab`; add `FormerlySerializedAs`.
- XML documentation: add docs to the type, `ResourcePrefab`, `OnActivate`, and `OnDeactivate`.
- Safety: add type migration if existing serialized assets reference the old concrete class name.

#### `Assets/Runtime/Shared/Data/Conditions/ConditionDefinition.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Conditions`.
- XML documentation: add docs to `ConditionDefinition`, `Evaluate`, and `GetTooltipLine`.

#### `Assets/Runtime/Shared/Data/Conditions/Condition_ActorLevel.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Conditions`.
- Serialized fields: `requiredLevel` → `_requiredLevel`, `tooltipFormat` → `_tooltipFormat`; add `FormerlySerializedAs`.
- XML documentation: add docs to `Condition_ActorLevel`, `Evaluate`, and `GetTooltipLine`.
- Safety: add managed-reference type migration for existing condition data.

#### `Assets/Runtime/Shared/Data/Conditions/Condition_PlayerClass.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Conditions`.
- Serialized fields: `requiredClass` → `_requiredClass`, `tooltipFormat` → `_tooltipFormat`; add `FormerlySerializedAs`.
- XML documentation: add docs to `Condition_PlayerClass`, `Evaluate`, and `GetTooltipLine`.
- Safety: add managed-reference type migration for existing condition data.

### 11.5 Data — dialogue

#### `Assets/Runtime/Shared/Data/Dialogue/DialogueChoice.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Dialogue`.
- Serialized fields: `id` → `_id`, `choiceText` → `_choiceText`, `icon` → `_icon`, `conditions` → `_conditions`, `showAlways` → `_showAlways`, `nextNodeId` → `_nextNodeID`; add `FormerlySerializedAs`.
- XML documentation: add docs to `DialogueChoice`, `ID`, `Icon`, `Conditions`, `AddCondition`, `RemoveConditionAt`, `ShowAlways`, and `ResolveNextNode`.
- Safety: update dialogue Editor serialized-property paths.

#### `Assets/Runtime/Shared/Data/Dialogue/DialogueCondition.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Dialogue`.
- Serialized fields: `condition` → `_condition`, `invert` → `_invert`; add `FormerlySerializedAs`.
- XML documentation: add docs to `DialogueCondition`.
- Safety: `condition` contains polymorphic data; verify managed-reference migration and update Editor paths.

#### `Assets/Runtime/Shared/Data/Dialogue/DialogueDefinition.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Dialogue`.
- Serialized field: `nodes` → `_nodes`; add `[FormerlySerializedAs("nodes")]`.
- XML documentation: add docs to `Nodes` and `GetNode`.
- Safety: update dialogue editor property paths.

#### `Assets/Runtime/Shared/Data/Dialogue/DialogueNode.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.Dialogue`.
- Serialized fields: `id` → `_id`, `title` → `_title`, `dialogueText` → `_dialogueText`, `choices` → `_choices`; add `FormerlySerializedAs`.
- XML documentation: add docs to `DialogueNode`, `Choices`, `AddChoice`, and `RemoveChoiceAt`.
- Safety: update dialogue editor property paths.

### 11.6 Data — quest objectives

#### `Assets/Runtime/Shared/Data/QuestObjectives/Quest_ItemObjective.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.QuestObjectives`.
- Serialized field: `targetItemID` → `_targetItemID`; add `[FormerlySerializedAs("targetItemID")]`.
- XML documentation: add docs to `Quest_ItemObjective`, `TargetItemID`, and both constructors.
- Safety: add managed-reference type migration for existing quest assets.

#### `Assets/Runtime/Shared/Data/QuestObjectives/Quest_KillObjective.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.QuestObjectives`.
- Serialized field: `targetNpcID` → `_targetNPCID`; add `[FormerlySerializedAs("targetNpcID")]`.
- Property: `TargetNpcID` → `TargetNPCID`; update references.
- Constructor parameter: `targetNpcID` → `targetNPCID`.
- XML documentation: add docs to `Quest_KillObjective`, `TargetNPCID`, and both constructors.
- Safety: add managed-reference type migration for existing quest assets.

### 11.7 Data — spell effects and targets

#### `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_ApplyAura.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellEffectDefinitions`.
- Alphabetize `using` directives.
- Serialized fields: `casterEvents` → `_casterEvents`, `targetEvents` → `_targetEvents`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpellEffect_ApplyAura`, `Aura`, `Stacks`, `Duration`, `CasterEvents`, `TargetEvents`, `Clone`, and `Execute`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_Damage.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellEffectDefinitions`.
- Alphabetize `using` directives.
- Serialized fields: `casterEvents` → `_casterEvents`, `targetEvents` → `_targetEvents`; add `FormerlySerializedAs`.
- Constants: `StatDodgeChance` → `STAT_DODGE_CHANCE`, `StatParryChance` → `STAT_PARRY_CHANCE`, `StatBlockChance` → `STAT_BLOCK_CHANCE`, `StatSpellResistance` → `STAT_SPELL_RESISTANCE`, `StatSpellExpertise` → `STAT_SPELL_EXPERTISE`, `StatHealth` → `STAT_HEALTH`.
- XML documentation: add docs to `SpellWeaponDamageMode`, `SpellEffect_Damage`, `BaseDamage`, `WeaponDamageMode`, `StatScaling`, `DamageSchools`, `HitType`, `DamageType`, `AlwaysHits`, `UsesProjectile`, `ProjectileAddressablePath`, `ProjectileSpeed`, `ApplyAura`, `Aura`, `AuraStacks`, `CasterEvents`, `TargetEvents`, `Clone`, and `Execute`.
- Construction: clone-time explicit list constructors may use target-typed `new(...)`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_Heal.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellEffectDefinitions`.
- Alphabetize `using` directives.
- Serialized fields: `casterEvents` → `_casterEvents`, `targetEvents` → `_targetEvents`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpellEffect_Heal`, `BaseHealing`, `StatScaling`, `UsesProjectile`, `ProjectileAddressablePath`, `ProjectileSpeed`, `ApplyAura`, `Aura`, `AuraStacks`, `CasterEvents`, `TargetEvents`, `Clone`, and `Execute`.
- Construction: clone-time explicit list constructor → target-typed `new(...)`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_PlaceRift.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellEffectDefinitions`.
- Serialized fields: `casterEvents` → `_casterEvents`, `targetEvents` → `_targetEvents`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpellEffect_PlaceRift`, `ClassBehaviour`, `Radius`, `Duration`, `RiftPrefabAddressablePath`, `CasterEvents`, `TargetEvents`, `Clone`, and `Execute`.
- Safety: update reference to `SorcererClassBehaviour` after its namespace move; add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_Resource.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellEffectDefinitions`.
- Alphabetize `using` directives.
- Serialized fields: `casterEvents` → `_casterEvents`, `targetEvents` → `_targetEvents`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpellEffect_Resource`, `Resource`, `CasterEvents`, `TargetEvents`, `Clone`, and `Execute`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellTargetDefinitions/SpellTarget_Cone.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellTargetDefinitions`.
- Alphabetize `using` directives.
- XML documentation: add docs to `SpellTarget_Cone`, `Range`, `Angle`, its constructor, `Clone`, and `Evaluate`.
- Preferred syntax: local `results = new List<Actor>()` may become `var results = new List<Actor>()`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellTargetDefinitions/SpellTarget_GroundLocation.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellTargetDefinitions`.
- Alphabetize `using` directives.
- XML documentation: add docs to `SpellTarget_GroundLocation`, `Radius`, `GetActorsInRadius`, its constructor, `Clone`, and `Evaluate`.
- Safety: add managed-reference type migration.

#### `Assets/Runtime/Shared/Data/SpellTargetDefinitions/SpellTarget_Single.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.SpellTargetDefinitions`.
- Alphabetize `using` directives.
- XML documentation: add docs to `SpellTarget_Single`, its constructor, `Clone`, and `Evaluate`.
- Safety: add managed-reference type migration.

### 11.8 Data — talent behaviours

#### `Assets/Runtime/Shared/Data/TalentBehaviours/TalentBehaviour_ApplyAuraOnHit.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.TalentBehaviours`.
- Serialized fields: `aura` → `_aura`, `stacks` → `_stacks`; add `FormerlySerializedAs`.
- XML documentation: add docs to `TalentBehaviour_ApplyAuraOnHit`, `OnActivate`, and `OnDeactivate`.
- Safety: add managed-reference type migration if stored as a managed reference.

#### `Assets/Runtime/Shared/Data/TalentBehaviours/TalentBehaviour_DamageOnHit.cs`

- Namespace: `Game.Shared.Data` → `Game.Shared.Data.TalentBehaviours`.
- Alphabetize `using` directives.
- Serialized fields: `damageEffect` → `_damageEffect`, `damageTicks` → `_damageTicks`; add `FormerlySerializedAs`.
- XML documentation: add docs to `TalentBehaviour_DamageOnHit`, `OnActivate`, and `OnDeactivate`.
- Safety: add managed-reference type migration if stored as a managed reference.

### 11.9 Root configuration/runtime

#### `Assets/Runtime/Shared/GameConfiguration.cs`

- Construction: all six item-quality `Color` initializers (`PoorColor`, `CommonColor`, `UncommonColor`, `RareColor`, `EpicColor`, `LegendaryColor`) → target-typed `new(...)`.
- XML documentation: add docs to every currently undocumented public configuration field: `PlayerDataFilePath`, `ClearPlayerDataOnStart`, `ChunkSize`, `Gravity`, `GroundDetectionRadius`, `GroundDetectionMaskName`, `MaxFallSpeed`, `SpellCastRangeTickBuffer`, `SpellCastRangeEndBuffer`, `SpellHitChanceStat`, `MeleeHitChanceStat`, `RangedHitChanceStat`, `BaseMissChance`, `MissChancePerLevelPenalty`, `MaximumMissChance`, `MeleeCritChanceStat`, `RangedCritChanceStat`, `SpellCritChanceStat`, `CritResistanceStat`, `BaseCritChance`, `CritChancePerLevelPenalty`, `CritDamageMultiplier`, `GlobalCooldownDuration`, all six quality colors, `AutoAttackSpell`, `ServerAddress`, and `ServerPort`.
- No private-field rename is required because these configuration fields are public.

#### `Assets/Runtime/Shared/GameConfigurationManager.cs`

- Private field: `config` → `_config`.
- XML documentation: add docs to `GameConfigurationManager` and `IsInitialized` where missing.

#### `Assets/Runtime/Shared/Runtime.cs`

- XML documentation: add docs to `Runtime` and `IsServer`.

### 11.10 Movement

#### `Assets/Runtime/Shared/Movement/MovementSimulation.cs`

- Namespace: `Game.Shared` → `Game.Shared.Movement`; update all references atomically.
- Existing public XML documentation is otherwise retained.

### 11.11 Networking

#### Naming rule — shared networking endpoints

The static Shared Networking types that expose PurrNet RPC entry points and cross-assembly callback surfaces are **endpoints**, not domain services. Standardize the terminology as follows:

| Current type/file | Required type/file |
|---|---|
| `AccountService` / `AccountService.cs` | `AccountEndpoint` / `AccountEndpoint.cs` |
| `ActorService` / `ActorService.cs` | `ActorEndpoint` / `ActorEndpoint.cs` |
| `CharacterService` / `CharacterService.cs` | `CharacterEndpoint` / `CharacterEndpoint.cs` |
| `MovementService` / `MovementService.cs` | `MovementEndpoint` / `MovementEndpoint.cs` |
| `StatService` / `StatService.cs` | `StatEndpoint` / `StatEndpoint.cs` |

Rename each type and file together, preserve its existing `.meta` file/GUID, and update all references atomically. `IPlayerAuthService` is **not** part of this rename: it is a genuine service contract implemented by the server-side `PlayerAuthService`.

Use `Endpoint` rather than `Channel` because PurrNet already uses **Channel** as networking terminology for RPC/transport delivery behaviour. The project naming should not overload that term.


#### `Assets/Runtime/Shared/Networking/AccountEndpoint.cs`

- Rename `AccountService` → `AccountEndpoint` and `AccountService.cs` → `AccountEndpoint.cs`; preserve the existing `.meta` GUID.

- Alphabetize `using` directives.
- Parameter acronym: `characterGuid` → `characterGUID` in `Server_RequestEnterWorld`.
- XML documentation: add docs to callbacks `onCharacterListReceived`, `onEnteringWorld`, `onPlayerActorAssigned`, `onEnteredWorld`, `onCharacterCreationRequest`, `onCharacterCreated`, `onEnterWorldRequest`; and RPC methods `Client_UpdateCharacterList`, `Client_SetPlayerActor`, `Server_TryCreateCharacter`, `Client_NotifyCharacterCreationResult`, `Server_RequestEnterWorld`, `Client_EnterWorld`.
- Do **not** rename `actorIdentity`; `Identity` is a word/type concept, not an `ID` suffix.

#### `Assets/Runtime/Shared/Networking/ActorEndpoint.cs`

- Rename `ActorService` → `ActorEndpoint` and `ActorService.cs` → `ActorEndpoint.cs`; preserve the existing `.meta` GUID.

- Alphabetize `using` directives.
- XML documentation: add docs to every currently undocumented public callback field: `onActorDeath`, `onActorSpawned`, `onActorOwnerChanged`, `onActorOwnershipTaken`, `onPlayerActorAssigned`, `onClientHoveredActorChanged`, `onClientTargetedActorChanged`, `onClientLateTargetedActorChanged`, `onClientEnteredCombat`, `onClientExitedCombat`, `onSpellCastStarted`, `onSpellCastInterrupted`, `onSpellCastCompleted`, `onCombatLogEntryReceived`, `onStartGlobalCooldown`, `onStartCooldown`, `onClientApplyAura`, `onClientTickAura`, `onClientUpdateAura`, `onClientExpireAura`, `onClientDispelAura`, `onClientSpawnVFXAtPosition`, `onClientSpawnProjectile`, `onClientTriggerEffect`, `onClientPlaySFX`, `onClientPlaySFXOnAudioSource`, `onServerActorSpawned`, `onServerActorDespawned`, `onServerActorOwnerChanged`, `onServerActorInterestChanged`, `onServerSpellCastStarted`, `onServerSpellCastTick`, `onServerSpellCastInterrupted`, `onServerSpellCastCompleted`, `onServerSpellCastDelayedAction`, `onServerRequestSpellCastCancel`, `onServerStartCooldown`, `onServerApplyAura`, `onServerAuraApplied`, `onServerAuraTick`, `onServerAuraExpired`, `onServerAuraDispelled`, `onServerActorTick`, `onServerActorTickerExpired`, `onServerAddTicker`, `onServerRemoveTickerByTag`, `onServerStartAutoAttack`, `onServerStopAutoAttack`.
- Callback names themselves already conform to the selected `on...` convention.

#### `Assets/Runtime/Shared/Networking/AuthenticationPayload.cs`

- Namespace: `Game.Shared.Authentication` → `Game.Shared.Networking`.
- XML documentation: add docs to both constructors.
- Safety: update all references, especially `LoginAuthenticator` aliases/usings in Client and Server.

#### `Assets/Runtime/Shared/Networking/CharacterEndpoint.cs`

- Rename `CharacterService` → `CharacterEndpoint` and `CharacterService.cs` → `CharacterEndpoint.cs`; preserve the existing `.meta` GUID.

- Alphabetize `using` directives.
- XML documentation: add docs to callbacks `onClientLearnedSpell`, `onClientKnownSpellsUpdated`, `onClientInventoryUpdated`, `onClientCurrencyUpdated`, `onClientEquipmentUpdated`, `onClientTalentsUpdated`, `onClientExperienceUpdated`, `onClientWeaponSkillUpdated`, `onClientReputationUpdated`, `onClientLevelUpdated`, `onClientQuestUpdated`, `onLearnSpellRequest`, `onUpdateStatsRequest`; and RPCs `Server_RequestLearnSpell`, `Client_LearnSpell`, `Client_ExperienceUpdated`, `Client_WeaponSkillUpdated`, `Client_ReputationUpdated`, `Client_QuestUpdated`.

#### `Assets/Runtime/Shared/Networking/ChunkSubscriptions.cs`

- Alphabetize `using` directives.
- Static readonly fields: `SceneActors` → `SCENE_ACTORS`, `PlayerActors` → `PLAYER_ACTORS`, `SceneIdentities` → `SCENE_IDENTITIES`; update every reference.
- Parameters: `playerId` → `playerID` in `RegisterPlayerActor` and `UnregisterPlayerActor`.
- XML documentation: add docs to `ChunkSubscriptions`, the three static readonly collections, and `AddActorToScene`, `RemoveActorFromScene`, `RegisterPlayerActor`, `UnregisterPlayerActor`, `RegisterIdentity`, `UnregisterIdentity`.
- Do **not** rename `Identity`, `Identities`, `RegisterIdentity`, or `UnregisterIdentity`.

#### `Assets/Runtime/Shared/Networking/ChunkVisibilityRule.cs`

- XML documentation: add docs to `ChunkVisibilityRule`, `complexity`, and `CanSee`.

#### `Assets/Runtime/Shared/Networking/IPlayerAuthService.cs`

- Namespace: `Game.Shared.Authentication` → `Game.Shared.Networking`.
- Existing XML documentation already covers the public interface API; no additional documentation change is required.
- Safety: update every authentication reference atomically.

#### `Assets/Runtime/Shared/Networking/LatencySimulator.cs`

- Namespace: `Game.Shared` → `Game.Shared.Networking`.
- Alphabetize `using` directives.
- Serialized fields: `simulateLatency` → `_simulateLatency`, `minLatencyMs` → `_minLatencyMS`, `maxLatencyMs` → `_maxLatencyMS`, `simulatePacketLoss` → `_simulatePacketLoss`, `packetLossChance` → `_packetLossChance`; add `FormerlySerializedAs` with the exact old names.
- XML documentation: add docs to `LatencySimulator`.
- Safety: preserve inspector values through `FormerlySerializedAs`.

#### `Assets/Runtime/Shared/Networking/LoginAuthenticator.cs`

- Namespace: `Game.Shared.Authentication` → `Game.Shared.Networking`.
- Alphabetize `using` directives.
- Private field: `clientPayload` → `_clientPayload`.
- XML documentation: add docs to `GetNextValidatedUsername` and `GetNextPlayerIsNew`.
- Safety: update all references to the old authentication namespace.

#### `Assets/Runtime/Shared/Networking/MovementEndpoint.cs`

- Rename `MovementService` → `MovementEndpoint` and `MovementService.cs` → `MovementEndpoint.cs`; preserve the existing `.meta` GUID.

- Alphabetize `using` directives.
- XML documentation: add docs to `MovementEndpoint`, `onInterestUpdated`, `onClientInterestRequest`, `onInterestRequest`, `onServerCorrection`, `Server_RequestChunkInterest`, and `Client_UpdateInterest`.
- Existing `Server_`/`Client_` RPC direction naming already conforms.

#### `Assets/Runtime/Shared/Networking/PackCombatLogEntry.cs`

- Alphabetize `using` directives.
- XML documentation: add docs to `PackCombatLogEntry` and both `Write`/`Read` methods.
- Preferred syntax: when constructing `damageTypes` after its type has already been declared, use target-typed `new(damageTypesLength)` rather than repeating `List<DamageSchoolDefinition>`.

#### `Assets/Runtime/Shared/Networking/PackGuid.cs`

- Type: `PackGuid` → `PackGUID`.
- Filename: `PackGuid.cs` → `PackGUID.cs`; preserve `.meta`.
- Alphabetize `using` directives.
- XML documentation: add docs to `PackGUID` and both `Write`/`Read` methods.
- Do not alter the framework `Guid` parameter type.

#### `Assets/Runtime/Shared/Networking/PackStatSnapshot.cs`

- XML documentation: add docs to `PackStatSnapshot` and both `Write`/`Read` overload pairs.

#### `Assets/Runtime/Shared/Networking/PackWorldPosition.cs`

- Alphabetize `using` directives.
- XML documentation: add docs to `PackWorldPosition` and `Write`/`Read`.

#### `Assets/Runtime/Shared/Networking/StatEndpoint.cs`

- Rename `StatService` → `StatEndpoint` and `StatService.cs` → `StatEndpoint.cs`; preserve the existing `.meta` GUID.

- XML documentation: add docs to `onStatsUpdated` where currently undocumented.

#### `Assets/Runtime/Shared/Networking/StatSnapshot.cs`

- Field: `StatSnapshotEntry.StatId` → `StatID`; update every reference.
- XML documentation: add docs to `StatSnapshotEntry`, `StatID`, `EffectiveMax`, `FlatModifier`, `PercentModifier`, `Current`, `StatSnapshot`, and `Stats`.

### 11.12 Persistence

#### `Assets/Runtime/Shared/Persistence/CharacterData.cs`

- Alphabetize `using` directives.
- Persisted identifiers: `Guid` → `GUID`, `PvPFactionID` → `PVPFactionID`, `SavedResourceEntry.StatId` → `StatID`, `SavedInventoryEntry.ItemGuid` → `ItemGUID`, `SavedInventoryEntry.ItemId` → `ItemID`, `SavedEquipmentEntry.ItemGuid` → `ItemGUID`, `SavedEquipmentEntry.ItemId` → `ItemID`; update all code references.
- Construction: all explicit saved-list initializers (`KnownSpellIDs`, `SavedInventory`, `SavedEquipment`, `SavedWeaponSkills`, `SavedTalents`, `SavedReputations`, `SavedQuests`, `SavedResources`) → target-typed `new()`.
- XML documentation: add docs to `CharacterData`; its public fields; both constructors; and every nested persisted struct and public field (`SavedResourceEntry`, `SavedInventoryEntry`, `SavedEquipmentEntry`, `SavedWeaponSkill`, `SavedTalentEntry`, `SavedReputationEntry`, `SavedQuestEntry`).
- **Persistence safety requirement:** before changing the JSON-facing field names, implement a versioned/backward-compatible load migration that reads the existing names (`Guid`, `PvPFactionID`, `StatId`, `ItemGuid`, `ItemId`) and writes the new names. Verify loading a pre-restyle `player_data.json`, then saving and reloading it after migration. Do not perform these field renames without that migration.

#### `Assets/Runtime/Shared/Persistence/NewCharacterData.cs`

- XML documentation: add docs to `NewCharacterData`, `Name`, `ClassID`, and `RaceID`.
### 11.13 Runtime — actors

#### `Assets/Runtime/Shared/Runtime/Actors/Actor.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Identifiers: `Id` → `ID`, `SpawnPointGuid` → `SpawnPointGUID`, `SetSpawnPointGuid` → `SetSpawnPointGUID`, parameter `spawnPointGuid` → `spawnPointGUID`.
- Serialized fields: `target` → `_target`, `projectileTarget` → `_projectileTarget`, `activeAuras` → `_activeAuras`; add `FormerlySerializedAs`.
- Private field: `expiredAurasThisFrame` → `_expiredAurasThisFrame`.
- Construction: explicit `Dictionary<int, AuraInstance>` and `List<int>` member constructors → target-typed `new()`.
- XML documentation: add docs to `Actor`, `ID`, `SpawnPointGUID`, `Name`, `IsPlayer`, `isDead`, `onAutoAttackHit`, `hitLocation`, `Target`, `ProjectileTarget`, `Initialize`, `GetName`, `GetLevel`, `GetCombatFaction`, `GetFactionRelation`, `GetWeaponDamage`, `GetWeaponSwingTimer`, `Server_SetTarget`, `Observers_SetTarget`, `Server_RequestSpellCast`, `Server_StartAutoAttack`, `Server_StopAutoAttack`, `SetOwner`, `SetDisplayName`, `SetSpawnPointGUID`, `Client_ReceiveCombatLogEntry`, aura observer methods, `GetActiveAuras`, `GetRemainingAuraTime`, `OnPointerEnter`, and `OnPointerClick`.
- Safety: namespace move affects almost every runtime/server/client reference; perform atomically.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorEvents.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- XML documentation: add docs to `ActorEvents`, all six public `Action` callback fields, and `LoadClassEvents`.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorMotor.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Private field: `actor` → `_actor`.
- XML documentation: add docs to `ActorMotor`, `verticalVelocity`, `isGrounded`, and `horizontalVelocity`.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorSFXController.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- `[ObserversRpc]` method: `Observer_PlaySFX` → `Observers_PlaySFX`.
- XML documentation: add docs to `ActorSFXController` and the public RPC if currently undocumented.
- Safety: preserve the exact `ObserversRpc` attribute and RPC signature.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorSpawnPoint.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Serialized fields: `guid` → `_guid`, `actor` → `_actor`, `spawnCooldown` → `_spawnCooldown`, `spawnOnStart` → `_spawnOnStart`; add `FormerlySerializedAs`.
- Property: `Guid` → `GUID`; update all references. Framework `Guid` type remains unchanged.
- XML documentation: add docs to `ActorSpawnPoint`, `Actor`, `SpawnCooldown`, `SpawnOnStart`, and `GUID`.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorSpellcaster.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `knownSpellIDs` → `_knownSpellIDs`, `spells` → `_spells`; add `FormerlySerializedAs`.
- Private field: `modifiersBySource` → `_modifiersBySource`, `isCasting` → `_isCasting`.
- `[ObserversRpc]` methods: `Observer_StartSpellCast` → `Observers_StartSpellCast`, `Observer_TickSpellCast` → `Observers_TickSpellCast`, `Observer_InterruptSpellCast` → `Observers_InterruptSpellCast`, `Observer_FinishSpellCast` → `Observers_FinishSpellCast`.
- Target-RPC parameters: `playerId` → `playerID` in `Client_StartGlobalCooldown` and `Client_StartSpellCooldown`.
- XML documentation: add docs to `SpellOverride`, its public fields and constructor; `ActorSpellcaster`, `Spells`, `SpellModifiers`, `IsCasting`, `LearnSpell`, `LoadKnownSpells`, `AddModifier`, `RemoveModifier`, `RecalculateOverrides`, all public spell-cast/RPC methods that are currently undocumented.
- Preferred syntax: local `list = new List<ISpellModifier>()` → target-typed `new()` because `list` has an inferred generic target from `TryGetValue`.
- **Reflection safety:** `CloneModifier` currently calls `GetField("source")`. When `BonusFireDamagePCT.source` is renamed to `_source`, update the reflection logic to support `_source` **and** retain `"source"` support for modifier types such as `FireDamageBonus` whose public `source` field is not required to change. This is mandatory to preserve modifier-source behaviour.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorStatContainer.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Private fields in `StatInstance`: `baseValue` → `_baseValue`, `flatModifier` → `_flatModifier`, `percentModifier` → `_percentModifier`, `effectiveMaximum` → `_effectiveMaximum`, `currentValue` → `_currentValue`.
- Private fields in `ActorStatContainer`: `stats` → `_stats`, `modifiers` → `_modifiers`.
- Identifier: `StatModifier.StatId` → `StatID`.
- Parameters: `statId` → `statID` in the `StatModifier` constructor, `GetStat`, `TryGetStat`, `SetStat`, `AddCurrentValue`, and `TryGetModifiersForStat`; `sourceId` → `sourceID`; `playerId` → `playerID` in `Client_ReceiveSnapshot`.
- XML documentation: add docs to `StatInstance` and its public properties/constructors/mutators; `ActorStatModifierSource`; `StatModifier` and all public fields/constructor; `ActorStatContainer`; `GetStat`, `All`, `TryGetStat`, `SetStat`, `AddCurrentValue`, `AddModifier`, `RemoveModifier`, `TryGetModifiersForStat`, `Client_ReceiveSnapshot`, `Observers_ReceiveSnapshot`, `ApplySnapshot`, and `BuildSnapshot`.
- Preferred syntax: obvious list construction in `BuildSnapshot` may use `var`.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorStatModifier.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- XML documentation: add docs to `ActorStatModifier` and public fields `stat`, `flatBonus`, `percentBonus`.

#### `Assets/Runtime/Shared/Runtime/Actors/ActorVFXController.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `spellEffectSpawnPoint` → `_spellEffectSpawnPoint`, `activeVFX` → `_activeVFX`; add `FormerlySerializedAs`.
- `[ObserversRpc]` methods: `Observer_SpawnEffectAtPosition` → `Observers_SpawnEffectAtPosition`, `Observer_SpawnProjectile` → `Observers_SpawnProjectile`, `Observer_TriggerEffect` → `Observers_TriggerEffect`.
- XML documentation: add docs to `ActorVFXController`, `SpellEffectSpawnPoint`, `ActiveEffects`, and `RegisterEffect`.
- Safety: preserve RPC attributes/signatures.

#### `Assets/Runtime/Shared/Runtime/Actors/ILevelledActor.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- XML documentation: add docs to `ILevelledActor` and `GetLevel`.

#### `Assets/Runtime/Shared/Runtime/Actors/NPCActor.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Public field: `NpcID` → `NPCID`; update references.
- Serialized private field: `DisplayName` → `_displayName`; add `[FormerlySerializedAs("DisplayName")]`.
- XML documentation: add docs to `NPCActor`, `IsPlayer`, `NPCID`, `levelRange`, `level`, `GetName`, and `GetLevel`.

#### `Assets/Runtime/Shared/Runtime/Actors/NPCBehavior.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- `NPCLootTableEntry` serialized fields: `lootTable` → `_lootTable`, `rolls` → `_rolls`; add `FormerlySerializedAs`.
- `NPCBehavior` private field: `threatTable` → `_threatTable`.
- `NPCBehavior` serialized fields: `baseExperience` → `_baseExperience`, `aggroRange` → `_aggroRange`, `aggroDelay` → `_aggroDelay`, `combatFaction` → `_combatFaction`, `dialogue` → `_dialogue`, and the outer `lootTable` collection → `_lootTable`; add `FormerlySerializedAs` to each serialized field.
- Construction: threat dictionary explicit constructor → target-typed `new()`.
- XML documentation: add docs to `NPCLootTableEntry`, `LootTable`, `Rolls`, `NPCBehavior`, `ThreatTable`, `BaseExperience`, `AggroRange`, `AggroDelay`, `CombatFaction`, `Dialogue`, `LootTable`, `onDeath`, `onDamaged`, and `OnDamaged`.
- Safety: the nested and outer `lootTable` identifiers are in different scopes; rename each by symbol, not global text replacement.

#### `Assets/Runtime/Shared/Runtime/Actors/NPCStatProfile.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `baseStats` → `_baseStats`, `mainHandDPS` → `_mainHandDPS`, `offHandDPS` → `_offHandDPS`, `mainHandSwingSpeed` → `_mainHandSwingSpeed`, `offHandSwingSpeed` → `_offHandSwingSpeed`; add `FormerlySerializedAs`.
- XML documentation: add docs to `ActorStatBaseEntry`, `definition`, `baseValue`, `NPCStatProfile`, `BaseStats`, `GetWeaponSwingTimer`, and `GetWeaponDamage`.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerActor.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Private field: `characterData` → `_characterData`.
- Acronym casing: public sync field `pvpFactionID` → `PVPFactionID`; update all references.
- XML documentation: add docs to `PlayerActor`, `IsPlayer`, `CharacterData`, public sync fields `level`, `classID`, `raceID`, `characterName`, `PVPFactionID`, `SetCharacterData`, `GetWeaponDamage`, `GetWeaponSwingTimer`, `GetName`, `GetLevel`, and `GetCombatFaction`.
- Safety: update the `CharacterData.PVPFactionID` migration in the same persistence-compatible phase.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerEquipment.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `equipment` → `_equipment`, `playerInventory` → `_playerInventory`; add `FormerlySerializedAs`.
- Private field: `statContainer` → `_statContainer`.
- Construction: `equipment` dictionary → target-typed `new()`. Obvious `EquipmentDelta delta = new EquipmentDelta(...)` locals should become `var delta = new EquipmentDelta(...)`; nested list construction may use target typing where supported.
- XML documentation: add docs to `EquipmentSlotChangeType`, `EquipmentSlotChange` and its public members/constructor, `EquipmentDelta` and members/constructor, `PlayerEquipment`, `Equipment`, `TryGetItem`, `LoadEquipment`, `EquipItem`, `UnequipItem`, `Server_RequestEquipItem`, `Server_RequestUnequipItem`, and `Client_UpdateEquipment`.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerExperience.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Private fields in `PlayerWeaponSkill`: `weaponType` → `_weaponType`, `level` → `_level`, `experience` → `_experience`.
- Private fields in `PlayerExperience`: `experience` → `_experience`, `level` → `_level`, `weaponSkills` → `_weaponSkills`.
- Construction: `weaponSkills = new List<PlayerWeaponSkill>()` → `new()`.
- XML documentation: add docs to `PlayerWeaponSkill`, its public properties and constructor; `PlayerExperience`, `Experience`, `Level`, `WeaponSkills`, `LoadExperience`, `SetExperience`, and `SetWeaponSkill`.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerInventory.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `inventory` → `_inventory`, `maxSlots` → `_maxSlots`; add `FormerlySerializedAs`.
- Parameter: `itemId` → `itemID` in `RemoveItem`.
- Construction: inventory dictionary → target-typed `new()`.
- XML documentation: add docs to `InventorySlotChangeType`, `InventorySlotChange` and its public members/constructor, `InventoryDelta` and its members/constructor, `PlayerInventory`, `Inventory`, `MaxSlots`, `LoadInventory`, `GetRemainingSlots`, `GetItemAtSlot`, and the public inventory RPCs.
- Preferred local syntax already uses `var` for most obvious collection constructions; retain that pattern.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerQuests.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized field: `activeQuests` → `_activeQuests`; add `[FormerlySerializedAs("activeQuests")]`.
- XML documentation: add docs to `PlayerQuestProgress`, `Quest`, `Progress`, `IsCompleted`, `PlayerQuests`, `ActiveQuests`, `onUpdate`, `OnClientQuestUpdated`, and `GetQuestProgress`.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerReputation.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized fields: `pvpFaction` → `_pvpFaction`, `reputations` → `_reputations`; add `FormerlySerializedAs`.
- Public property: `PvPFaction` → `PVPFaction`; update references.
- XML documentation: add docs to `FactionRelationState`, `PlayerReputation`, `PVPFaction`, `Reputations`, `OnClientReputationUpdated`, `SetReputation`, and `TryGetReputation`.

#### `Assets/Runtime/Shared/Runtime/Actors/PlayerTalents.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Actors`.
- Alphabetize `using` directives.
- Serialized field: `talents` → `_talents`; add `[FormerlySerializedAs("talents")]`.
- Private field: `spentTalentPointsBySpecialisation` → `_spentTalentPointsBySpecialisation`.
- XML documentation: add docs to `PlayerTalents`, `Talents`, `SpentTalentPoints`, `GetUnspentTalentPoints`, `GetMaximumTalentPoints`, `GetSpentTalentPoints`, `LoadTalents`, `AddTalent`, `RemoveTalent`, and the public talent RPCs.

### 11.14 Runtime — interfaces, items and managers

#### `Assets/Runtime/Shared/Runtime/IInteractable.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime`.
- XML documentation: add docs to `IInteractable`, `OnPointerEnter`, and `OnPointerClick`.

#### `Assets/Runtime/Shared/Runtime/ITargettable.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime`.
- XML documentation: add docs to `ITargettable` and `OnTargeted`.

#### `Assets/Runtime/Shared/Runtime/Items/ItemInstance.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Items`.
- Alphabetize `using` directives.
- Serialized fields: `guid` → `_guid`, `itemId` → `_itemID`, `modifications` → `_modifications`, `durability` → `_durability`, `currentStackSize` → `_currentStackSize`; add `FormerlySerializedAs`.
- XML documentation: add docs to `ItemInstance`, `BaseItem`, `GUID`, `Modifications`, and the constructor.
- Construction: in the constructor, `modifications ?? new List<string>()` → `modifications ?? new()` if target typing remains clear after the field/property rename.

#### `Assets/Runtime/Shared/Runtime/Managers/ChunkSpawnData.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Managers`.
- Alphabetize `using` directives.
- Serialized fields: `spawnPoints` → `_spawnPoints`, `prefab` → `_prefab`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpawnPoints` and `Prefab`.

### 11.15 Runtime — spellcasting

#### `Assets/Runtime/Shared/Runtime/Spellcasting/AuraContext.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- Alphabetize `using` directives.
- Parameter: `componentGuid` → `componentGUID` in `GetOrCreateOverrides`.
- XML documentation: add docs to the public `ConditionStack` fields/constructor; `AuraContext`; public context fields; both constructors; `ComponentOverrides`; and `GetOrCreateOverrides`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/AuraInstance.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- Alphabetize `using` directives.
- XML documentation: add docs to `AuraInstance`, its constructor, `Update`, `SetDuration`, and `SetStacks`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/CombatLogEntry.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- Alphabetize `using` directives.
- XML documentation: add docs to `CombatHistoryEntryType`, `CombatLogResultType`, `CombatLogEntry`, all public fields, and its constructor.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/ISpellModifier.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- XML documentation: add docs to `IScalableModifier`, `Scale`, `ISpellModifier`, `Priority`, `Source`, `AppliesTo`, `Apply`, and `Combine`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellComponentOverrides.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- XML documentation: add docs to `SpellComponentOverrides`, `DamageMultiplier`, `HealingMultiplier`, `FlatDamageBonus`, and `FlatHealingBonus`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellContext.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- Alphabetize `using` directives.
- XML documentation: add docs to `SpellContext`, `Phase`, `Spell`, `Caster`, `ActorTarget`, `PositionTarget`, `Targets`, the constructor, and `IsInstantCast`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellProjectile.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting`.
- Alphabetize `using` directives.
- Serialized fields: `impactEffectPrefab` → `_impactEffectPrefab`, `arcHeight` → `_arcHeight`, `horizontalDeviationAmount` → `_horizontalDeviationAmount`; add `FormerlySerializedAs`.
- XML documentation: add docs to `SpellProjectile` and public SFX fields `onCreateSFX`, `onImpactSFX`, and `onLoopSFX`.

### 11.16 Runtime — spell modifiers

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellModifiers/BonusFireDamagePct.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting.SpellModifiers`.
- Alphabetize `using` directives.
- Type: `BonusFireDamagePct` → `BonusFireDamagePCT`.
- Filename: `BonusFireDamagePct.cs` → `BonusFireDamagePCT.cs`; preserve `.meta`.
- Serialized fields: `bonusDamagePct` → `_bonusDamagePCT`, `source` → `_source`; add `FormerlySerializedAs`.
- Property: `BonusDamagePct` → `BonusDamagePCT`.
- Constructor: `BonusFireDamagePct(...)` → `BonusFireDamagePCT(...)`; parameter `bonusDamagePct` → `bonusDamagePCT`.
- XML documentation: add docs to the type, its public properties, constructors, `AppliesTo`, `Apply`, `Scale`, and `Combine`.
- **Reflection safety:** update `ActorSpellcaster.CloneModifier` so it can assign `_source`; retain fallback support for a field named `source`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellModifiers/CastTimeHasteScaling.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting.SpellModifiers`.
- Alphabetize `using` directives.
- XML documentation: add docs to `CastTimeHasteScaling`, `Priority`, `Source`, `AppliesTo`, `Apply`, and `Combine`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellModifiers/CooldownHasteScaling.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting.SpellModifiers`.
- Alphabetize `using` directives.
- XML documentation: add docs to `CooldownHasteScaling`, `Priority`, `Source`, `AppliesTo`, `Apply`, and `Combine`.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellModifiers/FireDamageBonus.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting.SpellModifiers`.
- Alphabetize `using` directives.
- Serialized private field: `bonusDamage` → `_bonusDamage`; add `[FormerlySerializedAs("bonusDamage")]`.
- XML documentation: add docs to `FireDamageBonus`, `BonusDamage`, `Priority`, public field `source`, `Source`, both constructors, `AppliesTo`, `Apply`, `Scale`, and `Combine`.
- Do not rename the public `source` field solely under the private-field convention. `ActorSpellcaster.CloneModifier` must continue to support this field name.

#### `Assets/Runtime/Shared/Runtime/Spellcasting/SpellModifiers/WeaponSwingTimerCooldown.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.Spellcasting.SpellModifiers`.
- Alphabetize `using` directives.
- XML documentation: add docs to `WeaponSwingTimerCooldown`, `Priority`, `Source`, `AppliesTo`, `Apply`, and `Combine`.

### 11.17 Runtime — tests and VFX interfaces

#### `Assets/Runtime/Shared/Runtime/Test/Test_InterestController.cs`

- Namespace: `Game.Shared.Test` → `Game.Shared.Runtime.Test`.
- Alphabetize `using` directives.
- Private field: `onMove` → `_onMove`.
- XML documentation: add docs to `Test_InterestController`, `Instance`, and `GetCurrentTile`.
- Existing `Test_` type naming already conforms.

#### `Assets/Runtime/Shared/Runtime/Test/Test_SpawnedObject.cs`

- Namespace: `Game.Shared.Test` → `Game.Shared.Runtime.Test`.
- Alphabetize `using` directives.
- Existing `Test_` type naming already conforms.
- Add XML documentation to any currently undocumented public type/member in this file during implementation.

#### `Assets/Runtime/Shared/Runtime/ToDoAttribute.cs`

- Namespace: global → `Game.Shared.Runtime`.
- XML documentation: add docs to `ToDoAttribute` and its constructor.

#### `Assets/Runtime/Shared/Runtime/VFX/IVFXFadeIn.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.VFX`.
- Existing XML documentation is retained; no other style change is required.

#### `Assets/Runtime/Shared/Runtime/VFX/IVFXFadeOut.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.VFX`.
- Existing XML documentation is retained; no other style change is required.

#### `Assets/Runtime/Shared/Runtime/VFX/IVFXTrigger.cs`

- Namespace: `Game.Shared` → `Game.Shared.Runtime.VFX`.
- Existing XML documentation is retained; no other style change is required.

### 11.18 Terrain

#### `Assets/Runtime/Shared/Terrain/ZoneDefinition.cs`

- Namespace: `Game.Runtime.Shared` → `Game.Shared.Terrain`.
- XML documentation: add docs to public fields `zoneName`, `albedoArray`, and `normalArray`.
- Safety: update all world-editor/terrain references to the new namespace.

### 11.19 Utility

#### `Assets/Runtime/Shared/Utility/CombatRating.cs`

- Namespace: `Game.Shared` → `Game.Shared.Utility`.
- Filename: `CombatRating.cs` → `CombatRatingHelper.cs`; preserve `.meta`.
- Constants: `ReferenceLevel` → `REFERENCE_LEVEL`, `ScalingExponent` → `SCALING_EXPONENT`.
- XML documentation: add docs to `CombatRatingHelper` where missing.
- Update all references to the renamed constants.

#### `Assets/Runtime/Shared/Utility/ExperienceCalculator.cs`

- Constants: `MinLevel` → `MIN_LEVEL`, `MaxLevel` → `MAX_LEVEL`.
- Static readonly field: `LevelXpRequirements` → `LEVEL_XP_REQUIREMENTS`.
- Construction: `new Dictionary<int, int>(MaxLevel)` → target-typed `new(MAX_LEVEL)`.
- XML documentation: update existing docs that name `LevelXpRequirements`; add docs to `GetExpForLevel` and `GetLevelProgress01`.
- Update every code reference to the renamed constants/static readonly field.

#### `Assets/Runtime/Shared/Utility/FormattedDebug.cs`

- Namespace: `Game.Shared` → `Game.Shared.Utility`.
- XML documentation: add docs to `FormattedDebug`, `Log`, `Warning`, and `Error`.
- Safety: many files currently alias `Game.Shared.FormattedDebug` as `Debug`; update those aliases across the repository to `Game.Shared.Utility.FormattedDebug`.

#### `Assets/Runtime/Shared/Utility/GaussianTable.cs`

- Namespace: global → `Game.Shared.Utility`.
- Static readonly field: `_samples` → `SAMPLES`.
- Constant: `Size` → `SIZE`.
- XML documentation: add docs to `GaussianTable` and `Next`.
- Update all references atomically.

#### `Assets/Runtime/Shared/Utility/WorldPosition.cs`

- XML documentation: add docs to `WorldPosition`, public fields `x`, `y`, `z`, its constructor, `ToLocal`, and both operators where currently undocumented.
- Existing namespace `Game.Shared.Utility` already conforms.

## 12. Game.Shared implementation order

The Shared restyle must be applied in dependency-safe stages rather than as one unreviewed global replacement.

### Stage S1 — Documentation and syntax-only changes

Apply:

- XML documentation;
- alphabetical `using` order;
- target-typed `new()` where unambiguous;
- obvious `var` changes where the right-hand side states the type.

Compile before proceeding.

### Stage S2 — Non-serialized private fields, constants and static readonly fields

Apply `_camelCase`, `SCREAMING_SNAKE_CASE`, and project acronym changes that do not touch Unity serialization or persistence.

This includes singleton/library `instance` fields, immutable constants, `ChunkSubscriptions` static readonly collections, and nonserialized runtime caches.

Update all symbol references atomically and compile.

### Stage S3 — Serialized field renames

For each serialized field:

1. add `FormerlySerializedAs` with the old field name;
2. rename the field;
3. update every C# reference;
4. update all Editor `SerializedProperty`/reflection/property-path strings;
5. open and inspect representative assets/prefabs/scenes;
6. verify no values reset;
7. compile before moving to the next subsystem.

Perform this subsystem-by-subsystem rather than across all Shared data simultaneously.
### Stage S4 — Filename/type/acronym renames

Perform these explicit moves while preserving `.meta` files:

- `ActorStatScaling.cs` → `StatScaling.cs`;
- `AuraBehaviour_DamageHostOnHit copy.cs` → `AuraBehaviour_DamageHostOnHit.cs`;
- `RaceDefintionLibrary.cs` → `RaceDefinitionLibrary.cs`;
- `TestClassBehaviour.cs` / `TestClassBehaviour` → `Test_ClassBehaviour.cs` / `Test_ClassBehaviour`;
- `PackGuid.cs` / `PackGuid` → `PackGUID.cs` / `PackGUID`;
- `BonusFireDamagePct.cs` / `BonusFireDamagePct` → `BonusFireDamagePCT.cs` / `BonusFireDamagePCT`;
- `CombatRating.cs` → `CombatRatingHelper.cs`.

Also perform project-defined member acronym renames (`DefinitionID`, `GUID`, `NPCID`, `PVPFaction`, etc.) as symbol-wide refactors.

Compile after each coherent rename set.

### Stage S5 — Persistence migration

Before changing `CharacterData` JSON-facing field names:

1. capture a representative pre-restyle `player_data.json`;
2. introduce a compatibility deserialization/migration path for the old names;
3. rename the C# fields;
4. load the old file;
5. verify every character, inventory/equipment item, resource, talent, reputation, and quest field;
6. save to the new format;
7. reload the newly saved file and verify equivalence.

Only after that validation may the old compatibility path be scheduled for later removal.

### Stage S6 — Namespace mirroring and managed-reference migration

Apply namespace changes one subtree at a time:

1. `Components`;
2. `Data` subfolders;
3. `Movement`;
4. `Networking`;
5. `Runtime` and its subfolders;
6. `Terrain`;
7. `Utility`.

For each subtree:

- update every repository reference in the same commit;
- add type-migration metadata to managed-reference classes where applicable;
- reopen affected ScriptableObject assets;
- check for missing managed-reference types;
- compile Client, Server, and Editor assemblies before advancing.

### Stage S7 — RPC naming

Rename only the non-conforming observer RPCs:

- `Observer_PlaySFX` → `Observers_PlaySFX`;
- `Observer_StartSpellCast` → `Observers_StartSpellCast`;
- `Observer_TickSpellCast` → `Observers_TickSpellCast`;
- `Observer_InterruptSpellCast` → `Observers_InterruptSpellCast`;
- `Observer_FinishSpellCast` → `Observers_FinishSpellCast`;
- `Observer_SpawnEffectAtPosition` → `Observers_SpawnEffectAtPosition`;
- `Observer_SpawnProjectile` → `Observers_SpawnProjectile`;
- `Observer_TriggerEffect` → `Observers_TriggerEffect`.

Preserve attributes/signatures exactly. Rebuild both client and server and perform an RPC smoke test.

## 13. Game.Shared validation

The Shared assembly restyle is complete only when all of the following pass:

- Unity Editor C# compilation;
- Client C# compilation/build;
- dedicated-server C# compilation/build;
- no missing MonoBehaviour/ScriptableObject scripts;
- no missing `[SerializeReference]` types in spell, aura, condition, quest, talent, or modifier assets;
- existing definition-library assets retain their entries and values;
- representative spell/aura/talent/quest assets retain all serialized values after field renames;
- `CharacterData` can load a pre-restyle persistence file and round-trip it into the new format;
- PurrNet authentication, account entry, actor services, movement interest, stat snapshots, inventory/equipment RPCs, spellcast RPCs, aura RPCs, and VFX/SFX observer RPCs still operate;
- `ActorSpellcaster.CloneModifier` still assigns modifier sources to both `_source`-backed and `source`-backed modifier implementations;
- all Editor custom inspectors/drawers that access renamed Shared fields by serialized-property path still function;
- a fresh convention audit of all 130 Shared C# files reports no remaining hard convention breaches;
- no gameplay values, network semantics, persistence semantics, or asset content are intentionally changed.

## 14. Game.Shared result

| Item | Result |
|---|---:|
| C# files audited | 130 |
| Assembly definition files audited | 1 |
| Namespace-mirroring changes | Required across multiple Shared subtrees |
| Serialized-field migrations | Required across data definitions and runtime components |
| Persistence migration | Required for `CharacterData` acronym field renames |
| Managed-reference migration | Required for polymorphic data namespace/type moves |
| C# file/type moves explicitly identified | 7 |
| Observer RPC renames | 8 |
| Functional changes intended | 0 |

**Game.Shared status:** Planned; not yet restyled.

# Assembly 3 — `Game.Client`

## 15. Client assembly scope

**Assembly definition:** `Assets/Runtime/Client/Client.asmdef`  
**Source root:** `Assets/Runtime/Client/`  
**Audit baseline:** `8850d401e3c8052e793cdfa69492a47094820ee3`  
**C# files audited:** 139

The C# conventions do not define formatting or naming requirements for `.asmdef` JSON. No restyling change is required to `Client.asmdef`.

The Client assembly contains:

| Area | C# files |
|---|---:|
| `Core` | 17 |
| `Shaders` | 1 |
| `Systems` | 1 |
| `UI` | 111 |
| `Utility` | 4 |
| `VFX` | 5 |
| **Total** | **139** |

All 139 files were checked against `.docs/CSharp-Style-Conventions.md`.

## 16. Client-specific safety requirements

The general safety requirements in section 2 continue to apply. The following Client-specific rules are additionally mandatory.

1. **Serialized MonoBehaviour/ScriptableObject fields:** any private field listed for renaming below that is serialized by Unity must receive `[FormerlySerializedAs("<oldName>")]` before the identifier is changed.
2. **Prefab and scene safety:** after serialized field or MonoBehaviour type renames, existing UI prefabs, world-space UI prefabs, character-creation/selection scenes, loading screen objects, nameplates, VFX prefabs, and camera/controller objects must be opened and checked for missing scripts and reset inspector values.
3. **C# file moves:** preserve the existing `.meta` file for every C# filename change so Unity asset GUIDs are retained.
4. **MonoBehaviour type renames:** where the type itself changes, preserve the script `.meta` and validate every prefab/scene containing that component. Use Unity type-migration metadata where required.
5. **Namespace migration:** the UI namespace move is cross-cutting. `Game.Client.UI.Common` contains base types used by nearly every other UI folder, so the entire UI namespace migration and all corresponding `using`/fully-qualified references must be updated atomically.
6. **Shared dependency sequencing:** the Shared restyle should be applied before the final Client namespace/import pass. Client references must use the final Shared namespaces and renamed Shared APIs (`Actor.ID`, `CharacterData.GUID`, `DefinitionID`, `Game.Shared.Utility.FormattedDebug`, etc.) rather than being migrated twice.
7. **Core bootstrap dependency:** moving `ClientInitialization` to `Game.Client.Core` requires `Game.Core.GameBootstrapper` and every other caller to be updated in the same change.
8. **Shader utility dependency:** moving `UIShaderProperties` from `Game.Client.UI` to `Game.Client.Shaders` requires all UI/VFX callers to import the new namespace.
9. **Events/callbacks:** callback-like members continue to use `on...`; the private-field `_camelCase` rule must not be used to convert correctly named callback fields such as `onClick`, `onShowWindow`, or `onHideWindow`.
10. **Unity event methods:** normal Unity lifecycle and pointer-handler methods retain their existing API signatures. Styling work must not alter Unity callback names.
11. **String-based references:** search the complete repository for renamed type/member names, serialized property names, reflection strings, `GetComponent` type names, and UnityEvent method names before closing each rename.
12. **No behavioural changes:** camera behaviour, input mapping, networking calls, combat logic, UI behaviour, world loading, VFX timing, and addressable loading must remain unchanged.

## 17. Namespace migration

The namespace convention requires namespaces to mirror the Client folder tree.

### 17.1 Core

All 17 files under `Assets/Runtime/Client/Core/` currently use flatter Client namespaces and must move to their folder-derived namespaces:

- files directly under `Core/`: `Game.Client` → `Game.Client.Core`;
- `Core/Collections/LoadingScreenCollection.cs`: `Game.Client.Collections` → `Game.Client.Core.Collections`;
- files under `Core/Controllers/`: `Game.Client` → `Game.Client.Core.Controllers`;
- files under `Core/Managers/`: `Game.Client` → `Game.Client.Core.Managers`.

### 17.2 Shaders

`Assets/Runtime/Client/Shaders/UIShaderProperties.cs`:

```text
Game.Client.UI → Game.Client.Shaders
```

### 17.3 Systems

`Assets/Runtime/Client/Systems/TestClientSystem.cs`:

```text
Game.Client → Game.Client.Systems
```

### 17.4 UI

All 111 files under `Assets/Runtime/Client/UI/` currently use `Game.Client.UI` and must move to their direct feature namespaces:

```text
Game.Client.UI.ActionBars
Game.Client.UI.CastBars
Game.Client.UI.CharacterCreation
Game.Client.UI.CharacterSelection
Game.Client.UI.CharacterUnitFrame
Game.Client.UI.CharacterWindow
Game.Client.UI.Common
Game.Client.UI.ContextMenu
Game.Client.UI.Dialogue
Game.Client.UI.ExperienceBar
Game.Client.UI.FloatingCombatText
Game.Client.UI.Inventory
Game.Client.UI.LoadingScreen
Game.Client.UI.MainMenu
Game.Client.UI.Nameplates
Game.Client.UI.QuestLog
Game.Client.UI.ReputationWindow
Game.Client.UI.TalentWindow
Game.Client.UI.TargetWindow
Game.Client.UI.Tooltip
Game.Client.UI.VFX
```

This accounts for 111 namespace moves. Every cross-feature UI reference must be updated with the appropriate `using` directive or fully-qualified type.

### 17.5 Utility and VFX

The four files in `Assets/Runtime/Client/Utility/` already use `Game.Client.Utility`.

The five files in `Assets/Runtime/Client/VFX/` already use `Game.Client.VFX`.

No namespace rename is required for those nine files.

### 17.6 Namespace result

A total of **130 Client C# files require namespace changes**.

## 18. Public API documentation and source syntax

### 18.1 XML documentation

The Client audit found undocumented public APIs throughout Core and UI. During implementation:

- every public type must have XML documentation;
- public properties, fields, events/callbacks, methods, constructors, and public overrides must be documented where currently undocumented;
- interfaces must document their public contracts;
- inherited implementations may use `<inheritdoc/>` when that accurately represents the contract;
- private Unity lifecycle methods do not require public API documentation.

This applies across all 139 audited files, including the many `UI_` types whose current public API is self-explanatory but undocumented.

### 18.2 `using` ordering

Alphabetize `using` directives throughout the Client assembly. The audit identified out-of-order directives in a large proportion of Core, UI, Utility, and VFX source files.

After the Shared namespace migration, aliases such as:

```csharp
using Debug = Game.Shared.FormattedDebug;
```

must also be updated to the final Shared namespace and alphabetized.

### 18.3 `var`

Replace explicit local types with `var` where the right-hand side makes the type obvious. Representative audited cases include:

- `ClientPositionManager` temporary `Vector3` values;
- `UI_ReputationRewardEntry` temporary `Color`;
- `UI_TooltipWindow` `RectTransform` lookup;
- `ProceduralRiftMesh` obvious collection/array construction;
- `ItemTooltipFactory` locally constructed `UI_TooltipLine` values;
- obvious `GetComponent<T>()` and `new T(...)` local assignments throughout Client UI/VFX code.

Do not replace explicit types where the type is not obvious or where it materially improves readability.

### 18.4 Target-typed construction

Use target-typed `new()`/`new(...)` where the declared target type is already explicit. Audited examples occur in:

- `ClientSettingsManager`;
- `ClientWorldManager`;
- `LoadingScreenCollection`;
- character stat/skill/tab panels;
- `UI_Manager`;
- `ItemTooltipFactory`;
- `PlaceholderText`;
- `UIConstants`;
- `ProceduralRiftMesh`;
- `RiftLightning`;
- list/dictionary fields throughout Client UI.

## 19. Filename and type changes

The following filename/type changes are required.

### CLIENT-001 — Client initialization filename

```text
Assets/Runtime/Client/Core/Initialisation.cs
→ Assets/Runtime/Client/Core/ClientInitialization.cs
```

Primary type remains `ClientInitialization`.

Preserve the `.meta` file.

### CLIENT-002 — Client ECS test type

```text
TestClientSystem
→ Test_ClientSystem
```

and:

```text
Assets/Runtime/Client/Systems/TestClientSystem.cs
→ Assets/Runtime/Client/Systems/Test_ClientSystem.cs
```

Preserve the `.meta` file and update all type references.

### CLIENT-003 — Client cast-bar window filename

```text
Assets/Runtime/Client/UI/CastBars/UI_ClientCastBar.cs
→ Assets/Runtime/Client/UI/CastBars/UI_ClientCastBarWindow.cs
```

Primary type remains `UI_ClientCastBarWindow`.

Preserve the `.meta` file.

### CLIENT-004 — Character-creation stage button type

The file already follows the established `UI_CC_` family name:

```text
Assets/Runtime/Client/UI/CharacterCreation/UI_CC_StageButton.cs
```

Rename the primary type:

```text
StageButton
→ UI_CC_StageButton
```

Update every reference. Preserve the script `.meta` and validate prefabs/scenes using the component.

### CLIENT-005 — Tooltip filename typo

```text
Assets/Runtime/Client/UI/Tooltip/UI_TooltipDoubleLIne.cs
→ Assets/Runtime/Client/UI/Tooltip/UI_TooltipDoubleLine.cs
```

Primary type remains `UI_TooltipDoubleLine`.

Preserve the `.meta` file.

### CLIENT-006 — Item tooltip factory filename

```text
Assets/Runtime/Client/Utility/ItemTooltipFactor.cs
→ Assets/Runtime/Client/Utility/ItemTooltipFactory.cs
```

Primary type remains `ItemTooltipFactory`.

Preserve the `.meta` file.

## 20. Immutable and acronym naming

### 20.1 Constants and static readonly fields

Apply the following immutable renames:

| File | Current | Required |
|---|---|---|
| `Core/ClientAudioManager.cs` | `MaxCacheSize` | `MAX_CACHE_SIZE` |
| `Utility/PlaceholderText.cs` | `PlaceholderRegex` | `PLACEHOLDER_REGEX` |
| `Utility/PlaceholderText.cs` | `resolvers` | `RESOLVERS` |
| `Utility/UIConstants.cs` | `PrimaryText` | `PRIMARY_TEXT` |
| `Utility/UIConstants.cs` | `SecondaryText` | `SECONDARY_TEXT` |
| `VFX/VFX_SorcererRift.cs` | `OpenAmountProperty` | `OPEN_AMOUNT_PROPERTY` |
| `VFX/VFX_SorcererRift.cs` | `ShaderProgress` | `SHADER_PROGRESS` |
| `VFX/VFX_SorcererRift.cs` | `ShaderTrailStrength` | `SHADER_TRAIL_STRENGTH` |

`SETTINGS_PREFIX` and `FILL_SPEED` already conform and remain unchanged.

### 20.2 Project-defined acronym identifiers

Apply uppercase acronym casing to Client-owned identifiers. Explicit audit hits include:

- `CharacterCreationManager._selectedClassId` → `_selectedClassID`;
- `CharacterCreationManager._selectedRaceId` → `_selectedRaceID`;
- `CharacterCreationManager.GetCurrentClassId()` → `GetCurrentClassID()`;
- `CharacterCreationManager.GetCurrentRaceId()` → `GetCurrentRaceID()`;
- `ClientWorldManager` parameters `sceneId` → `sceneID`;
- `UI_SpecialResourceCounter` `slotId` identifiers → `slotID`;
- `UI_CharacterEquipmentEntry` `slotId`/`_slotId` identifiers → `slotID`/`_slotID`.

During implementation, perform a final symbol scan for Client-owned `*Id`, `*Guid`, `*Pvp`, `*Npc`, `*Ecs`, `*Vfx`, `*Sfx`, `*Pct`, and `*Dps` identifiers and correct only genuine project abbreviations. Do not alter framework names such as `Guid`, `NetworkIdentity`, or ordinary words containing those letter sequences.

Shared-owned symbols referenced by Client are updated in the Shared migration rather than treated as Client declarations.

### 20.3 Event naming

`Assets/Runtime/Client/VFX/ProceduralRiftMesh.cs` currently declares:

```csharp
public event System.Action OnGenerated;
```

Rename it to:

```csharp
public event System.Action onGenerated;
```

Update subscribers, including `RiftLightning`.

Private callback fields that already use `on...` naming remain unchanged even though ordinary private fields use an underscore.
## 21. Private-field rename manifest

Every private field below must be renamed to `_camelCase`. If the field is Unity-serialized, add `FormerlySerializedAs` with the exact old name before renaming it.

### 21.1 Core

#### `Core/ClientAudioManager.cs`

- `sfxSource` → `_sfxSource`.

#### `Core/ClientCombatManager.cs`

- `inCombat` → `_inCombat`;
- `activeCooldowns` → `_activeCooldowns`;
- `globalCooldownRemaining` → `_globalCooldownRemaining`.

#### `Core/ClientPositionManager.cs`

- `chunkSize` → `_chunkSize`.

#### `Core/ClientSettingsManager.cs`

- `settings` → `_settings`.

#### `Core/ClientWorldManager.cs`

- `loadedScenes` → `_loadedScenes`.

#### `Core/Collections/LoadingScreenCollection.cs`

- `splashImages` → `_splashImages`.

#### `Core/Controllers/CharacterCameraController.cs`

- `cameraInputController` → `_cameraInputController`;
- `orbitalFollow` → `_orbitalFollow`;
- `characterRotationSpeed` → `_characterRotationSpeed`.

#### `Core/Controllers/Test_CharacterController.cs`

- `moveSpeed` → `_moveSpeed`;
- `jumpPower` → `_jumpPower`;
- `actorMotor` → `_actorMotor`.

#### `Core/Managers/CharacterCreationManager.cs`

- `characterCreationWindow` → `_characterCreationWindow`;
- `sceneCamera` → `_sceneCamera`;
- `_selectedClassId` → `_selectedClassID`;
- `_selectedRaceId` → `_selectedRaceID`.

#### `Core/Managers/CharacterSelectionManager.cs`

- `characterSelectionWindow` → `_characterSelectionWindow`;
- `sceneCamera` → `_sceneCamera`.

### 21.2 Action bars and cast bars

#### `UI/ActionBars/UI_ActionBarButton.cs`

- `keybindText` → `_keybindText`;
- `spellIconImage` → `_spellIconImage`;
- `spellID` → `_spellID`;
- `spellRank` → `_spellRank`;
- `contextMenuEntryPrefab` → `_contextMenuEntryPrefab`;
- `key` → `_key`.

#### `UI/ActionBars/UI_ActionBarPanel.cs`

- `actionBarIndex` → `_actionBarIndex`.

#### `UI/CastBars/UI_CastBar.cs`

- `parentWindowName` → `_parentWindowName`;
- `defaultMaterial` → `_defaultMaterial`.

#### `UI/CastBars/UI_ClientCastBarWindow.cs` after CLIENT-003

- `castBar` → `_castBar`;
- `castColor` → `_castColor`;
- `interruptColor` → `_interruptColor`;
- `completeColor` → `_completeColor`.

### 21.3 Character creation

#### `UI/CharacterCreation/UI_CC_ClassInfoPanel.cs`

- `classNameText` → `_classNameText`;
- `classDescriptionText` → `_classDescriptionText`;
- `classIconImage` → `_classIconImage`.

#### `UI/CharacterCreation/UI_CC_ClassListEntry.cs`

- `classIconImage` → `_classIconImage`;
- `index` → `_index`.

#### `UI/CharacterCreation/UI_CC_ClassListPanel.cs`

- `contentContainer` → `_contentContainer`;
- `entryPrefab` → `_entryPrefab`;
- `classDefinitionLibrary` → `_classDefinitionLibrary`.

#### `UI/CharacterCreation/UI_CC_FinalizePanel.cs`

- `createButton` → `_createButton`;
- `nameInput` → `_nameInput`.

#### `UI/CharacterCreation/UI_CC_NextStageButton.cs`

- `currentPanel` → `_currentPanel`;
- `nextPanel` → `_nextPanel`.

The existing public `_instance` field is not covered by the private-field rule and must not be changed merely as part of this item.

#### `UI/CharacterCreation/UI_CC_RaceInfoPanel.cs`

- `raceNameText` → `_raceNameText`;
- `raceDescriptionText` → `_raceDescriptionText`;
- `raceIconImage` → `_raceIconImage`.

#### `UI/CharacterCreation/UI_CC_RaceListEntry.cs`

- `raceIconImage` → `_raceIconImage`;
- `index` → `_index`.

#### `UI/CharacterCreation/UI_CC_RaceListPanel.cs`

- `contentContainer` → `_contentContainer`;
- `entryPrefab` → `_entryPrefab`;
- `raceDefinitionLibrary` → `_raceDefinitionLibrary`.

#### `UI/CharacterCreation/UI_CC_StageButton.cs`

- `openPanels` → `_openPanels`;
- `closePanels` → `_closePanels`.

### 21.4 Character selection

#### `UI/CharacterSelection/UI_CharacterListEntry.cs`

- `classIconImage` → `_classIconImage`;
- `characterNameText` → `_characterNameText`;
- `characterInfoText` → `_characterInfoText`;
- `factionIcon` → `_factionIcon`;
- `index` → `_index`.

#### `UI/CharacterSelection/UI_CharacterListPanel.cs`

- `contentContainer` → `_contentContainer`;
- `entryPrefab` → `_entryPrefab`.

#### `UI/CharacterSelection/UI_SelectedCharacterPanel.cs`

- `enterWorldButton` → `_enterWorldButton`;
- `characterNameText` → `_characterNameText`;
- `characterInfoText` → `_characterInfoText`.

### 21.5 Character unit frame

#### `UI/CharacterUnitFrame/UI_CharacterResourceBar.cs`

- `playerStatContainer` → `_playerStatContainer`;
- `valueText` → `_valueText`;
- `percentageText` → `_percentageText`;
- `trackedResources` → `_trackedResources`.

#### `UI/CharacterUnitFrame/UI_CharacterSpecialBar.cs`

- `playerStatContainer` → `_playerStatContainer`;
- `trackedStat` → `_trackedStat`;
- `container` → `_container`;
- `counterPrefab` → `_counterPrefab`;
- `counters` → `_counters`;
- `currentMax` → `_currentMax`;
- `currentValue` → `_currentValue`.

#### `UI/CharacterUnitFrame/UI_InstabilityBar.cs`

- `playerStatContainer` → `_playerStatContainer`.

#### `UI/CharacterUnitFrame/UI_SpecialResourceCounter.cs`

- `background` → `_background`;
- `icon` → `_icon`;
- `index` → `_index`.

### 21.6 Character window

#### `UI/CharacterWindow/UI_CharacterDataPanel.cs`

- `characterNameText` → `_characterNameText`;
- `characterTitleText` → `_characterTitleText`;
- `characterLevelText` → `_characterLevelText`;
- `characterClassText` → `_characterClassText`;
- `characterRaceText` → `_characterRaceText`;
- `characterClassIcon` → `_characterClassIcon`;
- `characterRaceIcon` → `_characterRaceIcon`;
- `characterClassBackground` → `_characterClassBackground`.

#### `UI/CharacterWindow/UI_CharacterEquipmentEntry.cs`

- `graphics` → `_graphics`;
- `_slotId` → `_slotID` where this private identifier occurs.

#### `UI/CharacterWindow/UI_CharacterEquipmentList.cs`

- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `equipmentSlots` → `_equipmentSlots`.

#### `UI/CharacterWindow/UI_CharacterEquipmentPanel.cs`

- `subPanels` → `_subPanels`.

#### `UI/CharacterWindow/UI_CharacterFocusStatsPanel.cs`

- `container` → `_container`;
- `statEntryPrefab` → `_statEntryPrefab`;
- `focusStatDefinitions` → `_focusStatDefinitions`;
- `currentStats` → `_currentStats`.

#### `UI/CharacterWindow/UI_CharacterSkillsPanel.cs`

- `subPanels` → `_subPanels`.

#### `UI/CharacterWindow/UI_CharacterStatEntry.cs`

- `statNameText` → `_statNameText`;
- `currentValueText` → `_currentValueText`;
- `statIcon` → `_statIcon`;
- `index` → `_index`.

#### `UI/CharacterWindow/UI_CharacterStatList.cs`

- `container` → `_container`;
- `statEntryPrefab` → `_statEntryPrefab`;
- `statDefinitions` → `_statDefinitions`;
- `focusStatDefinitions` → `_focusStatDefinitions`;
- `relevantStatDefinitions` → `_relevantStatDefinitions`;
- `currentStats` → `_currentStats`.

#### `UI/CharacterWindow/UI_CharacterStatTypeButton.cs`

- `statTypeTag` → `_statTypeTag`.

#### `UI/CharacterWindow/UI_CharacterStatsPanel.cs`

- `subPanels` → `_subPanels`.

#### `UI/CharacterWindow/UI_CharacterTabButton.cs`

- `associatedPanel` → `_associatedPanel`;
- `tabText` → `_tabText`;
- `tabGraphics` → `_tabGraphics`;
- `characterTabPanel` → `_characterTabPanel`.

#### `UI/CharacterWindow/UI_CharacterTabPanel.cs`

- `tabButtons` → `_tabButtons`;
- `tabButtonList` → `_tabButtonList`.

#### `UI/CharacterWindow/UI_CharacterWeaponSkillBar.cs`

- `weaponSkillEntry` → `_weaponSkillEntry`.

#### `UI/CharacterWindow/UI_CharacterWeaponSkillEntry.cs`

- `weaponTypeText` → `_weaponTypeText`;
- `levelText` → `_levelText`;
- `weaponSkillBar` → `_weaponSkillBar`;
- `weaponType` → `_weaponType`;
- `index` → `_index`.

#### `UI/CharacterWindow/UI_CharacterWeaponSkillsList.cs`

- `container` → `_container`;
- `skillEntryPrefab` → `_skillEntryPrefab`;
- `trackedWeaponTypes` → `_trackedWeaponTypes`;
- `playerExperience` → `_playerExperience`.

### 21.7 Common UI

#### `UI/Common/UI_AuraEntry.cs`

- `data` → `_data`;
- `graphics` → `_graphics`;
- `durationText` → `_durationText`;
- `index` → `_index`.

#### `UI/Common/UI_DragGhost.cs`

- `iconImage` → `_iconImage`;
- `rectTransform` → `_rectTransform`;
- `rootCanvas` → `_rootCanvas`.

#### `UI/Common/UI_Manager.cs`

- `instance` → `_instance`.

Protected fields such as `root`, `rootCanvasGroup`, `windows`, and `openedWindows` are not covered by the private-field convention and are not renamed in this pass.

#### `UI/Common/UI_ProgressBar.cs`

- `canvasGroup` → `_canvasGroup`.

#### `UI/Common/UI_WorldSpace.cs`

- `instance` → `_instance`.

#### `UI/Common/UI_WorldSpaceUIManager.cs`

- `cornerBuffer` → `_cornerBuffer`.

`UI_Panel.onShowWindow` and `UI_Panel.onHideWindow` remain callback-style names under section 13 of the convention.

### 21.8 Context menu

#### `UI/ContextMenu/UI_ContextMenuEntry.cs`

- `iconImage` → `_iconImage`;
- `nameText` → `_nameText`.

The private callback `onClick` remains `onClick`.

#### `UI/ContextMenu/UI_ContextMenuWindow.cs`

- `contentsPanel` → `_contentsPanel`;
- `backdropBlocker` → `_backdropBlocker`;
- `panelRect` → `_panelRect`.

### 21.9 Dialogue

#### `UI/Dialogue/UI_DialogueBodyPanel.cs`

- `dialogueText` → `_dialogueText`;
- `optionList` → `_optionList`.

#### `UI/Dialogue/UI_DialogueNPCInfoPanel.cs`

- `npcNameText` → `_npcNameText`;
- `npcSubtext` → `_npcSubtext`;
- `npcPortrait` → `_npcPortrait`.

#### `UI/Dialogue/UI_DialogueOptionEntry.cs`

- `choiceText` → `_choiceText`;
- `choiceIcon` → `_choiceIcon`.

#### `UI/Dialogue/UI_DialogueOptionList.cs`

- `currentNode` → `_currentNode`;
- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`.

### 21.10 Floating combat text

#### `UI/FloatingCombatText/UI_FCTManager.cs`

- `instance` → `_instance`;
- `fctPrefab` → `_fctPrefab`;
- `autoHitColor` → `_autoHitColor`;
- `abilityHitColor` → `_abilityHitColor`;
- `petHitColor` → `_petHitColor`;
- `healColor` → `_healColor`;
- `buffColor` → `_buffColor`;
- `debuffColor` → `_debuffColor`;
- `experienceColor` → `_experienceColor`;
- `heightOffset` → `_heightOffset`;
- `randomSpreadX` → `_randomSpreadX`.

#### `UI/FloatingCombatText/UI_FloatingCombatText.cs`

- `text` → `_text`;
- `floatSpeed` → `_floatSpeed`;
- `fadeDuration` → `_fadeDuration`;
- `screenSize` → `_screenSize`;
- `floatDirection` → `_floatDirection`;
- `elapsedTime` → `_elapsedTime`;
- `sizeMultiplier` → `_sizeMultiplier`;
- `durationMultiplier` → `_durationMultiplier`.

### 21.11 Inventory

#### `UI/Inventory/UI_InventoryFooterPanel.cs`

- `capacityText` → `_capacityText`;
- `currencyText` → `_currencyText`.

#### `UI/Inventory/UI_InventoryGridPanel.cs`

- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`.

#### `UI/Inventory/UI_InventorySlotEntry.cs`

- `dragSource` → `_dragSource`;
- `index` → `_index`;
- `itemInstance` → `_itemInstance`;
- `graphics` → `_graphics`;- `stackSizeText` → `_stackSizeText`.

### 21.12 Loading screen and main menu

#### `UI/LoadingScreen/LoadingScreenController.cs`

- `canvasGroup` → `_canvasGroup`;
- `splashImage` → `_splashImage`;
- `loadingIcon` → `_loadingIcon`;
- `stage` → `_stage`;
- `maxStages` → `_maxStages`;
- `targetFillAmount` → `_targetFillAmount`;
- `isShowing` → `_isShowing`.

`FILL_SPEED` already conforms.

#### `UI/MainMenu/UI_MainMenuWindow.cs`

- `usernameInput` → `_usernameInput`;
- `passwordInput` → `_passwordInput`;
- `loginButton` → `_loginButton`.

### 21.13 Nameplates

#### `UI/Nameplates/UI_Nameplate.cs`

- `initialized` → `_initialized`;
- `heightOffset` → `_heightOffset`;
- `screenSize` → `_screenSize`;
- `nameText` → `_nameText`;
- `barsContainer` → `_barsContainer`;
- `healthBar` → `_healthBar`;
- `castBar` → `_castBar`;
- `actor` → `_actor`;
- `rectTransform` → `_rectTransform`;
- `canvas` → `_canvas`.

#### `UI/Nameplates/UI_NameplateHealthBar.cs`

- `nameText` → `_nameText`;
- `percentText` → `_percentText`;
- `levelText` → `_levelText`.

#### `UI/Nameplates/UI_NameplateManager.cs`

- `instance` → `_instance`;
- `targetedActor` → `_targetedActor`;
- `sortedForStacking` → `_sortedForStacking`.

Public configuration/data fields in this behavioural class are not renamed under the locked private-field rule alone.

### 21.14 Quest log

#### `UI/QuestLog/UI_QuestDetailsPanel.cs`

- `instance` → `_instance`;
- `questName` → `_questName`;
- `questSubtext` → `_questSubtext`;
- `questIcon` → `_questIcon`;
- `questDescription` → `_questDescription`;
- `objectivesPanel` → `_objectivesPanel`;
- `rewardsPanel` → `_rewardsPanel`;
- `def` → `_def`.

#### `UI/QuestLog/UI_QuestListEntry.cs`

- `titleText` → `_titleText`;
- `subtext` → `_subtext`;
- `levelText` → `_levelText`;
- `icon` → `_icon`;
- `data` → `_data`;
- `index` → `_index`.

#### `UI/QuestLog/UI_QuestListPanel.cs`

- `instance` → `_instance`;
- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `quests` → `_quests`.

#### `UI/QuestLog/UI_QuestObjectivesEntry.cs`

- `data` → `_data`;
- `index` → `_index`;
- `objectiveText` → `_objectiveText`.

#### `UI/QuestLog/UI_QuestObjectivesPanel.cs`

- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `def` → `_def`.

#### `UI/QuestLog/UI_QuestRewardsEntry.cs`

- `graphics` → `_graphics`;
- `data` → `_data`;
- `index` → `_index`.

#### `UI/QuestLog/UI_QuestRewardsPanel.cs`

- `choiceText` → `_choiceText`;
- `goldReward` → `_goldReward`;
- `goldAmountText` → `_goldAmountText`;
- `xpReward` → `_xpReward`;
- `xpAmountText` → `_xpAmountText`;
- `factionReward` → `_factionReward`;
- `factionIcon` → `_factionIcon`;
- `factionReputationText` → `_factionReputationText`;
- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `def` → `_def`.

### 21.15 Reputation window

#### `UI/ReputationWindow/UI_ReputationBodyPanel.cs`

- `progressBar` → `_progressBar`;
- `factionIcon` → `_factionIcon`;
- `factionNameText` → `_factionNameText`;
- `reputationValueText` → `_reputationValueText`;
- `reputationRankText` → `_reputationRankText`;
- `factionDescriptionText` → `_factionDescriptionText`;
- `currentFaction` → `_currentFaction`;
- `currentProgress` → `_currentProgress`;
- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `markerPrefab` → `_markerPrefab`.

#### `UI/ReputationWindow/UI_ReputationHeaderEntry.cs`

- `progressBar` → `_progressBar`;
- `factionIcon` → `_factionIcon`;
- `data` → `_data`;
- `progress` → `_progress`;
- `index` → `_index`.

#### `UI/ReputationWindow/UI_ReputationHeaderPanel.cs`

- `container` → `_container`;
- `slotEntryPrefab` → `_slotEntryPrefab`;
- `currentCategory` → `_currentCategory`;
- `playerReputation` → `_playerReputation`.

#### `UI/ReputationWindow/UI_ReputationRewardEntry.cs`

- `index` → `_index`;
- `data` → `_data`;
- `header` → `_header`;
- `entryText` → `_entryText`;
- `thresholdText` → `_thresholdText`;
- `thresholdIcon` → `_thresholdIcon`;
- `graphics` → `_graphics`.

### 21.16 Talent window

#### `UI/TalentWindow/UI_TalentHeaderPanel.cs`

- `classIconImage` → `_classIconImage`;
- `classNameText` → `_classNameText`;
- `characterLevelText` → `_characterLevelText`;
- `unspentPointsText` → `_unspentPointsText`;
- `spentPointsText` → `_spentPointsText`.

#### `UI/TalentWindow/UI_TalentTreeNode.cs`

- `graphics` → `_graphics`;
- `talentDefinition` → `_talentDefinition`;
- `rank` → `_rank`.

#### `UI/TalentWindow/UI_TalentTreePanel.cs`

- `graphics` → `_graphics`;
- `specNameText` → `_specNameText`;
- `spentPointsText` → `_spentPointsText`;
- `specIndex` → `_specIndex`;
- `talentNodePanelPrefab` → `_talentNodePanelPrefab`;
- `talentNodePanelContainer` → `_talentNodePanelContainer`;
- `rowSpacing` → `_rowSpacing`;
- `talentNodes` → `_talentNodes`;
- `specialisation` → `_specialisation`.

#### `UI/TalentWindow/UI_TalentTreeViewerPanel.cs`

- `instance` → `_instance`;
- `graphics` → `_graphics`;
- `talentNameText` → `_talentNameText`;
- `talentDescriptionText` → `_talentDescriptionText`.

### 21.17 Target window

#### `UI/TargetWindow/UI_TargetAuraBar.cs`

- `interactable` → `_interactable`.

#### `UI/TargetWindow/UI_TargetCastBar.cs`

- `spellNameText` → `_spellNameText`;
- `spellTimeText` → `_spellTimeText`.

#### `UI/TargetWindow/UI_TargetInfoPanel.cs`

- `actorNameText` → `_actorNameText`;
- `actorInfoText` → `_actorInfoText`.

#### `UI/TargetWindow/UI_TargetResourceBar.cs`

- `trackedResources` → `_trackedResources`;
- `valueText` → `_valueText`;
- `percentageText` → `_percentageText`.

### 21.18 Tooltip

#### `UI/Tooltip/UI_TooltipDoubleLine.cs` after CLIENT-005

- `leftText` → `_leftText`;
- `rightText` → `_rightText`.

#### `UI/Tooltip/UI_TooltipPanel.cs`

- `tooltipHeader` → `_tooltipHeader`;
- `tooltipBody` → `_tooltipBody`;
- `tooltipFooter` → `_tooltipFooter`;
- `tooltipIcon` → `_tooltipIcon`;
- `singleLinePrefab` → `_singleLinePrefab`;
- `doubleLinePrefab` → `_doubleLinePrefab`.

#### `UI/Tooltip/UI_TooltipSingleLine.cs`

- `lineText` → `_lineText`.

### 21.19 UI VFX

#### `UI/VFX/UI_ScrollingFog.cs`

- `mat` → `_mat`;
- `offset` → `_offset`.

### 21.20 Utility

#### `Utility/ItemTooltipFactory.cs` after CLIENT-006

- `cache` → `_cache`.

#### `Utility/PlaceholderText.cs`

No ordinary private-field rename is required after applying the immutable renames `PLACEHOLDER_REGEX` and `RESOLVERS`.

### 21.21 Client VFX

#### `VFX/ProceduralProjectileMesh.cs`

- `shapeType` → `_shapeType`;
- `scale` → `_scale`;
- `segments` → `_segments`;
- `rings` → `_rings`;
- `capsuleLength` → `_capsuleLength`;
- `taperStrength` → `_taperStrength`;
- `cachedMesh` → `_cachedMesh`.

#### `VFX/ProceduralRiftMesh.cs`

- `radius` → `_radius`;
- `height` → `_height`;
- `segments` → `_segments`;
- `minWidthFactor` → `_minWidthFactor`;
- `maxWidthFactor` → `_maxWidthFactor`;
- `edgeJitter` → `_edgeJitter`;
- `centreWobble` → `_centreWobble`;
- `thickness` → `_thickness`;
- `spiralTurns` → `_spiralTurns`;
- `spiralJitter` → `_spiralJitter`;
- `randomiseOnAwake` → `_randomiseOnAwake`;
- `seed` → `_seed`;
- `mesh` → `_mesh`.

Also rename event `OnGenerated` → `onGenerated` as specified in section 20.3.

#### `VFX/RiftLightning.cs`

- `intervalMin` → `_intervalMin`;
- `intervalMax` → `_intervalMax`;
- `range` → `_range`;
- `minTargetDistance` → `_minTargetDistance`;
- `hitLayers` → `_hitLayers`;
- `maxSimultaneous` → `_maxSimultaneous`;
- `boltColor` → `_boltColor`;
- `boltWidth` → `_boltWidth`;
- `boltSegments` → `_boltSegments`;
- `jitter` → `_jitter`;
- `flashDuration` → `_flashDuration`;
- `boltMaterial` → `_boltMaterial`;
- `riftMesh` → `_riftMesh`;
- `activeStrikes` → `_activeStrikes`;
- `runtimeMaterial` → `_runtimeMaterial`;
- `selfColliders` → `_selfColliders`;
- `activeBoltObjects` → `_activeBoltObjects`.

Explicit `HashSet<T>` constructors should become target-typed `new()`.

#### `VFX/VFX_ImpactEffect.cs`

- `lifetime` → `_lifetime`.

#### `VFX/VFX_SorcererRift.cs`

- `openDuration` → `_openDuration`;
- `closeDuration` → `_closeDuration`;
- `lifetime` → `_lifetime`;
- `distortionMaterial` → `_distortionMaterial`;
- `distortionSize` → `_distortionSize`;
- `distortionYOffset` → `_distortionYOffset`;
- `pulseWaveMaterial` → `_pulseWaveMaterial`;
- `pulseMaxRadius` → `_pulseMaxRadius`;
- `pulseWaveHeight` → `_pulseWaveHeight`;
- `pulseTrailStrength` → `_pulseTrailStrength`;
- `pulseDuration` → `_pulseDuration`;
- `meshRenderer` → `_meshRenderer`;
- `activeCoroutine` → `_activeCoroutine`;
- `triggerCoroutine` → `_triggerCoroutine`;
- `closing` → `_closing`;
- `currentOpenAmount` → `_currentOpenAmount`;
- `distortionMat` → `_distortionMat`;
- `distortionTransform` → `_distortionTransform`.

Apply the immutable renames from section 20.1 at the same time.

## 22. Files without additional hard symbol renames

The following audited files require their folder namespace migration and/or documentation/`using`/preferred-syntax cleanup, but no additional hard Client-owned symbol rename was identified beyond the global rules above:

- `Core/CameraManager.cs`;
- `Core/ClientAccountManager.cs` — Client references to Shared `Actor.Id` are updated when Shared becomes `Actor.ID`;
- `Core/ClientConnectionManager.cs`;
- `Core/ClientECSManager.cs`;
- `Core/ClientVFXManager.cs`;
- `Core/Controllers/ClientInputController.cs`;
- `Shaders/UIShaderProperties.cs`;
- `UI/ActionBars/UI_ActionBarWindow.cs`;
- `UI/CharacterCreation/UI_CC_CreateButton.cs`;
- `UI/CharacterCreation/UI_CharacterCreationWindow.cs`;
- `UI/CharacterSelection/UI_CharacterSelectWindow.cs`;
- `UI/CharacterSelection/UI_CreateCharacterButton.cs`;
- `UI/CharacterSelection/UI_EnterWorldButton.cs`;
- `UI/CharacterUnitFrame/UI_CharacterUnitWindow.cs`;
- `UI/CharacterUnitFrame/UI_SpecialResourcePanel.cs`;
- `UI/CharacterWindow/UI_CharacterWindow.cs`;
- `UI/Common/IDraggableSlot.cs`;
- `UI/Common/IListPanel.cs`;
- `UI/Common/UI_AuraBar.cs`;
- `UI/Common/UI_Button.cs`;
- `UI/Common/UI_GenericPanel.cs`;
- `UI/Common/UI_ListEntry.cs`;
- `UI/Common/UI_Panel.cs`;
- `UI/Common/UI_Window.cs`;
- `UI/ContextMenu/UI_ContextMenuPanel.cs`;
- `UI/Dialogue/UI_DialogueWindow.cs`;
- `UI/ExperienceBar/UI_ExperienceBar.cs`;
- `UI/ExperienceBar/UI_ExperienceBarWindow.cs`;
- `UI/Inventory/UI_InventoryWindow.cs`;
- `UI/MainMenu/UI_LoginButton.cs`;
- `UI/Nameplates/UI_NameplateCastBar.cs`;
- `UI/QuestLog/UI_QuestLogWindow.cs`;
- `UI/ReputationWindow/UI_ReputationHeaderProgressBar.cs`;
- `UI/ReputationWindow/UI_ReputationWindow.cs`;
- `UI/TalentWindow/UI_TalentWindow.cs`;
- `UI/TargetWindow/UI_TargetWindow.cs`;
- `UI/Tooltip/ITooltipLine.cs`;
- `UI/Tooltip/UI_TooltipWindow.cs`;
- `Utility/RarityColor.cs`.

`Utility/UIConstants.cs` and `Utility/PlaceholderText.cs` have immutable changes listed separately and therefore are not included above.

## 23. Cross-assembly reference updates

The Client assembly currently references many symbols that are scheduled to move or be renamed in `Game.Shared`. When the Shared and Client plans are implemented in sequence, update Client references to the final Shared API in the same integration branch.

Representative required updates include:

- `Game.Shared.FormattedDebug` → `Game.Shared.Utility.FormattedDebug`;
- flat `Game.Shared` actor/runtime types → their final `Game.Shared.Runtime...` namespaces;
- `Game.Shared.Data` concrete effect/target/condition types → their folder-derived namespaces;
- `Actor.Id` → `Actor.ID`;
- `CharacterData.Guid` → `CharacterData.GUID`;
- `CharacterData.PvPFactionID` → `CharacterData.PVPFactionID`;
- `DataDefinition.DefinitionId` → `DefinitionID`;
- `PlayerReputation.PvPFaction` → `PVPFaction`;
- spell modifier namespace/type changes;
- authentication namespace changes.

This is a reference update only in the Client pass; do not duplicate or diverge from the Shared migration contract.

## 24. Client implementation order

### Stage C1 — Documentation and syntax-only cleanup

Apply:

- XML documentation;
- alphabetical `using` ordering;
- target-typed `new()` where the target type is explicit;- `var` where the type is obvious.

Compile the current namespaces before proceeding.

### Stage C2 — Nonserialized private fields and immutable names

Apply:

- private `_camelCase` renames that do not touch Unity serialization;
- the eight constant/static-readonly renames;
- `onGenerated`;
- safe Client-owned acronym renames.

Update all symbol references atomically and compile.

### Stage C3 — Serialized field renames

Process subsystem-by-subsystem:

1. Core/controllers/managers;
2. action/cast bars;
3. character creation/selection;
4. character unit/character window;
5. common/context/dialogue;
6. floating combat text/inventory/loading/menu;
7. nameplates;
8. quest/reputation/talent/target/tooltip;
9. client VFX.

For every serialized field:

1. add `FormerlySerializedAs`;
2. rename the field;
3. update C# references;
4. update any Editor serialized-property paths;
5. open representative prefabs/scenes;
6. verify values are retained;
7. compile before moving to the next group.

### Stage C4 — Filename/type moves

Apply CLIENT-001 through CLIENT-006 while preserving `.meta` files.

For `UI_CC_StageButton`, validate every prefab/scene component after the type rename.

### Stage C5 — Shared integration

Apply the already-planned Shared namespace/API changes first, then update all Client references to the final Shared names.

Compile Shared + Client before the Client namespace migration.

### Stage C6 — Client namespace migration

Perform as coherent dependency groups:

1. `Game.Client.Core`, `Core.Controllers`, `Core.Managers`, `Core.Collections`;
2. `Game.Client.Shaders`;
3. `Game.Client.Systems`;
4. the entire `Game.Client.UI.*` tree as one coordinated migration.

The UI tree should not be left half-migrated because `UI.Common` base types are consumed across almost every feature folder.

Update `Game.Core.GameBootstrapper` and Editor references in the same change.

### Stage C7 — Final style sweep

Run a final search/audit for:

- private fields without `_camelCase`;
- `const`/`static readonly` names not in `SCREAMING_SNAKE_CASE`;
- Client-owned `*Id`/other acronym-casing breaches;
- `TestFoo` types lacking `Test_`;
- filename/primary-type mismatches;
- non-folder namespaces;
- observer/server/client RPC prefixes if any new RPCs were introduced while implementation was underway;
- public APIs without XML documentation;
- nonalphabetical `using` directives.

## 25. Client validation

The Client restyle is complete only when all of the following pass:

- Unity Editor compilation;
- standalone client compilation/build;
- dedicated-server compilation still succeeds against the Shared/Core integration changes;
- `GameBootstrapper` still enters `ClientInitialization.Begin(...)` in graphical execution;
- client networking still connects/authenticates and enters the world;
- additive world-scene loading and floating-origin behaviour are unchanged;
- camera and input controls are unchanged;
- spellcasting, cooldown UI, auto-attack input, and combat callbacks remain functional;
- character creation and character selection UI retain all inspector references;
- action bars, cast bars, character window, inventory, quest log, reputation, talents, target frame, tooltips, nameplates, and floating combat text load without missing scripts;
- no existing serialized UI values reset after private-field renames;
- `UI_CC_StageButton` components remain attached after the type rename;
- `UI_ClientCastBarWindow`, `UI_TooltipDoubleLine`, `ItemTooltipFactory`, `ClientInitialization`, and `Test_ClientSystem` resolve under their corrected filenames;
- `ProceduralRiftMesh.onGenerated` continues to drive `RiftLightning`;
- VFX prefabs retain all serialized values and effect timing;
- `UIShaderProperties` callers resolve the new `Game.Client.Shaders` namespace;
- all Client references use the final Shared namespaces/API;
- no missing MonoBehaviour scripts appear in scenes or prefabs;
- a fresh audit of all 139 Client C# files reports no remaining hard convention breaches;
- no functional change is intended or accepted as part of the restyle.

## 26. Game.Client result

| Item | Result |
|---|---:|
| C# files audited | 139 |
| Assembly definition files audited | 1 |
| Files requiring namespace migration | 130 |
| UI files requiring coordinated namespace migration | 111 |
| Explicit filename/type corrections | 6 |
| Immutable (`const`/`static readonly`) renames | 8 |
| Event-style rename | 1 (`OnGenerated` → `onGenerated`) |
| Serialized-field migrations | Required across Core, UI, and VFX |
| RPC renames defined in Client | 0 |
| Functional changes intended | 0 |

**Game.Client status:** Planned; not yet restyled.

# Assembly 4 — `Game.Server`

## 27. Server assembly scope

**Assembly definition:** `Assets/Runtime/Server/Server.asmdef`  
**Source root:** `Assets/Runtime/Server/`  
**Audit baseline:** `e6137b25c0e04d667f71e8a8c9715da2d794f1c2`  
**C# files audited:** 37

The C# styling conventions do not define formatting or naming requirements for `.asmdef` JSON files. No restyling change is required to `Server.asmdef`.

| Area | C# files |
|---|---:|
| `Core` | 15 |
| `Networking` | 3 |
| `Persistence` | 2 |
| `Services` | 7 |
| `Systems` | 9 |
| `Test` | 1 |
| **Total** | **37** |

All 37 files were checked against `.docs/CSharp-Style-Conventions.md`.

## 28. Server-specific compatibility rules

### 28.1 Server role terminology

Use the following role names consistently in the Server assembly:

- **Binding** — a small adapter that registers callbacks from Shared `*Endpoint` surfaces into server-owned implementations. Bindings own wiring, not gameplay/domain behaviour.
- **Service** — a server-owned domain capability or implementation. `IPlayerAuthService` / `PlayerAuthService` remain service names.
- **Manager** — a long-lived authoritative runtime subsystem owner.
- A manager may subscribe directly to an endpoint when the event is part of that manager's owned subsystem. Do **not** create a binding solely to wrap every event subscription.

Required binding renames are `AccountServiceHooks` → `AccountBindings`, `ActorDeathHooks` → `ActorDeathBindings`, and `ServerHooksManager` → `ServerBindings`. Their registration methods are simply `Register()`; `ServerBindings.Initialize()` performs the startup registration. Avoid redundant names such as `RegisterBindings`, `RegisterHooks`, or `ServerInitializeRPCBindings`.



1. **No Server-owned serialized private-field migration is required.** No `[SerializeField]` or `[SerializeReference]` declarations were found under `Assets/Runtime/Server/`.
2. **Preserve `.meta` files** for every C# filename change.
3. **Core namespace migration is cross-assembly.** `Game.Core.GameBootstrapper`, Client code, Shared code, Editor code, and any tests that reference `ServerInitialization` or Server manager types must be updated in the same integration change.
4. **ECS namespace migration must be atomic.** The current `Game.Server.ECS` types are referenced extensively from Server Core. Move the Systems namespace and all references together.
5. **ECS field renames are data-layout-neutral but symbol-wide.** Renaming `ActorId` to `ActorID`, for example, must update every system and manager that reads/writes the field without altering field type, order, or ECS semantics.
6. **Persistence must remain compatible.** The Server pass does not rename JSON-facing fields in `PlayerAccountData`; however, `PlayerDatabase` contains Shared `CharacterData`, so the Shared persistence migration defined earlier must be validated through the Server loader.
7. **PurrNet/network semantics must not change.** `PlayerID`, `SceneID`, ownership, scene subscription, spawn, authentication, and account/session behaviour must remain unchanged.
8. **Shared restyle sequencing applies.** Server imports and references should target the final Shared namespaces and Shared symbol names rather than being migrated twice.
9. **Callback-family naming overrides normal member casing.** `TickerRegistration.OnTick` and `OnExpire` are callbacks and therefore become `onTick` and `onExpire`.
10. **Framework names remain unchanged.** `System.Guid`, `PlayerID`, `SceneID`, and framework/library type names are not renamed.
11. **No functional changes are part of this plan.** Combat, spawning, world streaming, persistence, authentication, movement validation, ECS update order, and server simulation behaviour must remain identical.

## 29. Namespace migration

### 29.1 Core

All 15 files under `Assets/Runtime/Server/Core/` must use:

```text
Game.Server.Core
```

Current cases are mostly `Game.Server`, with `ServerECSManager.cs` currently using `Game.Server.ECS`.

### 29.2 Networking

These files already use the correct folder namespace and require no namespace change:

- `Networking/AccountBindings.cs`;
- `Networking/ActorDeathBindings.cs`;
- `Networking/PlayerSessionData.cs`.

### 29.3 Persistence

- `Persistence/PlayerAccountData.cs`: `Game.Server` → `Game.Server.Persistence`.
- `Persistence/PlayerDatabase.cs` already uses `Game.Server.Persistence`.

### 29.4 Services

The following already use `Game.Server.Services`:

- `CharacterCreationService.cs`;
- `CharacterExperienceService.cs`;
- `CharacterReputationService.cs`;
- `CharacterWeaponSkillService.cs`;
- `EnterWorldService.cs`;
- `PartyLootService.cs`.

`PlayerAuthService.cs` currently uses `Game.Server.Persistence` and must move to:

```text
Game.Server.Services
```

### 29.5 Systems

All nine files under `Assets/Runtime/Server/Systems/` currently use `Game.Server.ECS` and must move to:

```text
Game.Server.Systems
```

### 29.6 Test

`Test/Test_NPCPathfinder.cs`:

```text
Game.Server → Game.Server.Test
```

### 29.7 Namespace result

A total of **27 Server C# files require namespace changes**.

## 30. Filename and primary-type corrections

### SERVER-001 — Server initialization

```text
Assets/Runtime/Server/Core/Initialisation.cs
→ Assets/Runtime/Server/Core/ServerInitialization.cs
```

Primary type remains `ServerInitialization`.

### SERVER-002 — Spell-cast manager casing

```text
Assets/Runtime/Server/Core/ServerSpellcastManager.cs
→ Assets/Runtime/Server/Core/ServerSpellCastManager.cs
```

Primary type remains `ServerSpellCastManager`.

### SERVER-003 — Actor ECS component file

`ActorComponents.cs` does not match its primary type.

```text
Assets/Runtime/Server/Systems/ActorComponents.cs
→ Assets/Runtime/Server/Systems/ActorComponent.cs
```

Keep the secondary ECS component/buffer types in the same file; no type split is required by the current convention.

### SERVER-004 — Spawn ECS component file

`ChunkSpawnComponents.cs` does not match its primary type.

```text
Assets/Runtime/Server/Systems/ChunkSpawnComponents.cs
→ Assets/Runtime/Server/Systems/SpawnPointComponent.cs
```

Keep `SpawnCommand` in the same file.

For all four moves, preserve the existing Unity `.meta` file.

## 31. Immutable and callback naming

### 31.1 Constants and static readonly fields

| File | Current | Required |
|---|---|---|
| `Core/ServerActorManager.cs` | `MaxAllowedSpeed` | `MAX_ALLOWED_SPEED` |
| `Core/ServerActorManager.cs` | `ViolationThreshold` | `VIOLATION_THRESHOLD` |
| `Core/ServerActorManager.cs` | `ValidationInterval` | `VALIDATION_INTERVAL` |
| `Core/ServerStatManager.cs` | `RegenTickInterval` | `REGEN_TICK_INTERVAL` |
| `Persistence/PlayerDatabase.cs` | `_filePath` | `FILE_PATH` |
| `Services/PlayerAuthService.cs` | `SaltSize` | `SALT_SIZE` |
| `Services/PlayerAuthService.cs` | `HashSize` | `HASH_SIZE` |
| `Services/PlayerAuthService.cs` | `Iterations` | `ITERATIONS` |

`ServerCooldownManager.GCD_ID` already conforms.

### 31.2 Callback names

In `ServerTickerManager.TickerRegistration`:

```text
OnTick   → onTick
OnExpire → onExpire
```

Update every registration, invocation, and assignment.

## 32. File-by-file audit

### 32.1 Core

#### `Assets/Runtime/Server/Core/Initialisation.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Rename file to `ServerInitialization.cs`.
- Private fields: `host` → `_host`, `forceCleanServer` → `_forceCleanServer`.
- Parameter: `Begin(MonoBehaviour _host)` → `Begin(MonoBehaviour host)`.
- Alphabetize `using` directives.
- Add XML documentation to `ServerInitialization` and `Begin`.
- Update Shared references to the final Shared namespaces after the Shared migration, including `FormattedDebug`, `LoginAuthenticator`, and configuration types.
- Update `Game.Core.GameBootstrapper` to call the final `Game.Server.Core.ServerInitialization`.

#### `Assets/Runtime/Server/Core/ServerActorManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `entityManager` → `_entityManager`, `actorComponentQuery` → `_actorComponentQuery`.
- Constants: apply the three `SCREAMING_SNAKE_CASE` renames from section 31.1.
- Acronym casing: rename Server-owned `actorId` → `actorID`, `playerId` → `playerID`, and `sceneId` → `sceneID` locals/parameters where present.
- Update ECS field references from `ActorId` → `ActorID`.
- Update Shared actor references from `actor.Id` → `actor.ID` as part of Shared integration.
- Add XML documentation to the public API, including `Instance`, `IsInitialized`, and `TryGetActor`.
- Retain existing `NPCAggroState` type naming; its public fields are inside a private nested data type and are not an externally exposed API.

#### `Assets/Runtime/Server/Core/ServerAuraManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `entityManager` → `_entityManager`, `actorComponentQuery` → `_actorComponentQuery`, `_nextInstanceId` → `_nextInstanceID`.
- Acronym casing: `instanceId` → `instanceID`, `targetActorId` → `targetActorID`, `auraDefId` → `auraDefID`, `actorId` → `actorID`, `auraDefinitionId` → `auraDefinitionID`.
- Update ECS `ActorId`, `InstanceId`, and `AuraDefinitionId` references to their final `...ID` forms.
- Add XML documentation to `ServerAuraManager`, `Instance`, `IsInitialized`, `ApplyAura`, `DispelAura`, `RemoveAllAuras`, `HasAura`, and `GetAuraStackCount`.
- Prefer target-typed construction where a declared target type is already explicit.

#### `Assets/Runtime/Server/Core/ServerAutoAttackManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Acronym casing: Server-owned `actorId` locals → `actorID`.
- Add XML documentation to `ServerAutoAttackManager`, `Instance`, and `IsInitialized`.
- No ordinary private-field rename is required; the persistent manager collections already use `_camelCase`.

#### `Assets/Runtime/Server/Core/ServerConnectionManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `networkManager` → `_networkManager`, `connectedPlayers` → `_connectedPlayers`, `sessionData` → `_sessionData`.
- Acronym casing: `playerId` → `playerID`, `characterId` → `characterID`.
- Use target-typed `new()` for the two dictionary fields after renaming.
- Add XML documentation to the public manager API, including `Initialized`, `Unsubscribe`, `SetSessionData`, `SetAccountData`, `GetSessionData`, and `GetAccountData`.
- Update `PlayerSessionData.PlayerId`/`CharacterId` references to `PlayerID`/`CharacterID`.

#### `Assets/Runtime/Server/Core/ServerCooldownManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `entityManager` → `_entityManager`, `actorComponentQuery` → `_actorComponentQuery`.
- Acronym casing: `actorId` → `actorID`, `spellId` → `spellID`.
- Update ECS `ActorId` references to `ActorID`.
- Add XML documentation to `ServerCooldownManager`, `Instance`, `IsInitialized`, `GCD_ID`, `StartCooldown`, `IsOnCooldown`, and `GetRemainingCooldown`.

#### `Assets/Runtime/Server/Core/ServerECSManager.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Add XML documentation to `ServerECSManager`, `Initialized`, `DefaultWorld`, and `ServerInitializeECSWorlds`.
- Update every `ECS.ServerECSManager`/`Game.Server.ECS.ServerECSManager` reference to the final Core namespace.

#### `Assets/Runtime/Server/Core/ServerBindings.cs`

- Rename `ServerHooksManager` → `ServerBindings` and `ServerHooksManager.cs` → `ServerBindings.cs`; preserve the existing `.meta` GUID.
- Rename `ServerInitializeRPCHooks()` → `Initialize()`.

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Add XML documentation to `ServerBindings`, `Initialized`, and `Initialize`.

#### `Assets/Runtime/Server/Core/ServerInterestManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private field: `networkManager` → `_networkManager`.
- Acronym casing: `playerId` → `playerID`; any Server-owned `sceneId` identifier → `sceneID`.
- Add XML documentation to `Instance`, `IsInitialized`, `SubscribePlayerToChunk`, and `UnsubscribePlayerFromChunk`.
- Update all references to the final `ServerWorldManager`, Shared actor/session, and Systems namespaces.

#### `Assets/Runtime/Server/Core/ServerPositionManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private field: `chunkSize` → `_chunkSize`.
- Add XML documentation to `Instance` and `IsInitialized`.

#### `Assets/Runtime/Server/Core/ServerSpawnManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields:
  - `networkManager` → `_networkManager`;
  - `prefabRegistry` → `_prefabRegistry`;
  - `sceneRegistry` → `_sceneRegistry`;
  - `nextSceneIndex` → `_nextSceneIndex`;
  - `spawnPointActorMap` → `_spawnPointActorMap`;
  - `entityManager` → `_entityManager`;
  - `spawnCommandQuery` → `_spawnCommandQuery`.
- Acronym casing across Server-owned identifiers: `playerId` → `playerID`, `characterId` → `characterID`, `spawnPointGuid` → `spawnPointGUID`, `classId` → `classID`, `raceId` → `raceID`, `factionId` → `factionID`, `questId` → `questID`, `spellId` → `spellID`, and `sceneId` → `sceneID`.
- Update `SpawnPointGuid` ECS fields to `SpawnPointGUID`.
- Prefer target-typed construction for collection fields whose target type is explicit.
- Add XML documentation to `ServerSpawnManager`, `Instance`, `IsInitialized`, `Awake`, `SpawnPlayer`, and `SpawnAt`.- Preserve all current spawn sequencing, scene ownership, and session assignment behaviour.

#### `Assets/Runtime/Server/Core/ServerSpellcastManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Rename file to `ServerSpellCastManager.cs`.
- Alphabetize `using` directives.
- Private fields: `entityManager` → `_entityManager`, `actorComponentQuery` → `_actorComponentQuery`.
- Acronym casing: `actorId` → `actorID`; any Server-owned `spellId` → `spellID`.
- Update ECS `ActorId` references to `ActorID`.
- Add XML documentation to `ServerSpellCastManager`, `Instance`, `IsInitialized`, `IsCasting`, and `TryInterruptCast`.

#### `Assets/Runtime/Server/Core/ServerStatManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Constant: `RegenTickInterval` → `REGEN_TICK_INTERVAL`.
- Private fields: `regenTimer` → `_regenTimer`, `trackedActors` → `_trackedActors`, `currentEquipmentModifiers` → `_currentEquipmentModifiers`.
- Method: `InitializeNpcStats` → `InitializeNPCStats`.
- Acronym casing: `statId` → `statID`; Server-owned `classId`/`raceId` → `classID`/`raceID`.
- Add XML documentation to `ServerStatManager`, `Instance`, `IsInitialized`, `RegisterActor`, `UnregisterActor`, `InitializeNPCStats`, `Recalculate`, `AddModifier`, `RemoveModifier`, `SetCurrent`, and `SendSnapshot`.
- Preserve stat calculation and regeneration semantics.

#### `Assets/Runtime/Server/Core/ServerTickerManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `entityManager` → `_entityManager`, `actorComponentQuery` → `_actorComponentQuery`, `_nextTickerId` → `_nextTickerID`, `_actorTickerIds` → `_actorTickerIDs`.
- `TickerRegistration.ActorId` → `ActorID`.
- Callback fields: `TickerRegistration.OnTick` → `onTick`, `OnExpire` → `onExpire`.
- Acronym casing: `tickerId` → `tickerID`, `actorId` → `actorID`.
- Update ECS `ActorId`/`TickerId` references to `ActorID`/`TickerID`.
- Add XML documentation to the externally visible manager API: `ServerTickerManager`, `Instance`, `IsInitialized`, `AddTicker`, `RemoveTicker`, `RemoveTickerByTag`, and `HasTicker`.
- No XML documentation is required solely because members of the private nested `TickerRegistration` struct are declared `public`.

#### `Assets/Runtime/Server/Core/ServerWorldManager.cs`

- Namespace: `Game.Server` → `Game.Server.Core`.
- Alphabetize `using` directives.
- Private fields: `networkManager` → `_networkManager`, `worldSceneSettings` → `_worldSceneSettings`, `loadedChunks` → `_loadedChunks`, `_globalActorsSceneId` → `_globalActorsSceneID`.
- Public property: `GlobalActorsSceneId` → `GlobalActorsSceneID`.
- Method: `TryGetChunkSceneId` → `TryGetChunkSceneID`.
- Acronym casing for Server-owned locals/parameters: `sceneId` → `sceneID`, `loadedSceneId` → `loadedSceneID`, `chunkSceneId` → `chunkSceneID`.
- Update `LoadedChunks` to return `_loadedChunks`.
- Use target-typed `new()` for the loaded-chunk dictionary where appropriate.
- Add XML documentation to `Instance`, `IsInitialized`, `LoadedChunks`, `GlobalActorsSceneID`, `IsGlobalActorsSceneLoaded`, `ParseChunkCoordinate`, `IsChunkLoaded`, `IsChunkLoadPending`, and `TryGetChunkSceneID`.
- Preserve existing additive scene loading/unloading and pending-load callback behaviour.

### 32.2 Networking

#### `Assets/Runtime/Server/Networking/AccountBindings.cs`

- Rename `AccountServiceHooks` → `AccountBindings` and `AccountServiceHooks.cs` → `AccountBindings.cs`; preserve the existing `.meta` GUID.
- Rename `RegisterHooks()` → `Register()`.

- Namespace already conforms.
- Alphabetize `using` directives.
- Acronym casing: `playerId` → `playerID`, `characterGuid` → `characterGUID`.
- Add XML documentation to `AccountBindings` and `Register`.
- Update references to final Server Core/Services and Shared Networking namespaces.

#### `Assets/Runtime/Server/Networking/ActorDeathBindings.cs`

- Rename `ActorDeathHooks` → `ActorDeathBindings` and `ActorDeathHooks.cs` → `ActorDeathBindings.cs`; preserve the existing `.meta` GUID.
- Rename `RegisterHooks()` → `Register()`.

- Namespace already conforms.
- Alphabetize `using` directives.
- Acronym casing: Server-owned `npcId` → `npcID`.
- Add XML documentation to `ActorDeathBindings`/`Register` where currently undocumented.
- Update Shared actor/NPC references to their final Shared runtime namespaces.

#### `Assets/Runtime/Server/Networking/PlayerSessionData.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Public fields: `PlayerId` → `PlayerID`, `CharacterId` → `CharacterID`.
- Constructor parameters: `playerId` → `playerID`, `characterId` → `characterID`.
- Add XML documentation to `PlayerSessionData`, `PlayerID`, `Username`, `CharacterID`, `PlayerActor`, the constructor, and `SetPlayerActor`.
- This is session/runtime state, not the persisted account JSON schema.

### 32.3 Persistence

#### `Assets/Runtime/Server/Persistence/PlayerAccountData.cs`

- Namespace: `Game.Server` → `Game.Server.Persistence`.
- Alphabetize `using` directives.
- Construction: `Characters = new List<CharacterData>()` → `Characters = new()`.
- Add XML documentation to `PlayerAccountData` and the public DTO fields `Username`, `PasswordHash`, `PasswordSalt`, and `Characters`.
- Do not rename the JSON-facing public fields.
- Validate that an existing player-data file still deserializes after the namespace move.

#### `Assets/Runtime/Server/Persistence/PlayerDatabase.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Static readonly field: `_filePath` → `FILE_PATH`.
- Private field: `playerAccounts` → `_playerAccounts`.
- Use target-typed `new()` for `_playerAccounts` and other obvious declared collection constructions.
- Update all internal references to the two renamed fields.
- Add XML documentation to `PlayerDatabase`, `CleanDatabase`, `TryGetPlayerAccount`, `SavePlayerAccount`, and `Accounts` if exposed.
- The nested `PlayerDatabaseWrapper.Accounts` field remains unchanged because it is part of the serialized JSON shape.
- Validate loading and saving a pre-restyle database, including Shared `CharacterData` migration.

### 32.4 Services

#### `Assets/Runtime/Server/Services/CharacterCreationService.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Private fields: `characterData` → `_characterData`, `playerId` → `_playerID`.
- Constructor parameters: `_playerId` → `playerID`, `_characterData` → `characterData`.
- Update all internal references.
- Add XML documentation to `CharacterCreationService`, its public constructor, and the public definition-library cache fields.
- Update Shared `DefinitionId` → `DefinitionID` and final Shared namespaces during integration.
- Preserve validation and account-save behaviour.

#### `Assets/Runtime/Server/Services/CharacterExperienceService.cs`

- Namespace already conforms.
- Private fields: `playerExperience` → `_playerExperience`, `level` → `_level`, `experience` → `_experience`.
- Add XML documentation to `CharacterExperienceService`, its constructor where public, and `AddExperience`.

#### `Assets/Runtime/Server/Services/CharacterReputationService.cs`

- Namespace already conforms.
- Private fields: `player` → `_player`, `playerReputation` → `_playerReputation`, `faction` → `_faction`.
- Add XML documentation to `CharacterReputationService`, its constructor where public, and `AddReputation`.
- Update Shared `PVPFaction`/definition APIs to their final Shared names during integration.

#### `Assets/Runtime/Server/Services/CharacterWeaponSkillService.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Private fields: `playerExperience` → `_playerExperience`, `weaponType` → `_weaponType`, `level` → `_level`, `experience` → `_experience`.
- Add XML documentation to `CharacterWeaponSkillService`, its constructor where public, and `AddWeaponSkillExperience`.

#### `Assets/Runtime/Server/Services/EnterWorldService.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Private fields: `playerId` → `_playerID`, `characterGuid` → `_characterGUID`.
- Constructor parameters: `_playerId` → `playerID`, `_characterGuid` → `characterGUID`.
- Update all internal references.
- Update Shared `CharacterData.Guid` → `CharacterData.GUID`.
- Add XML documentation to `EnterWorldService` and its public constructor.
- Preserve character ownership validation and spawn behaviour.

#### `Assets/Runtime/Server/Services/PartyLootService.cs`

- Namespace already conforms.
- Alphabetize `using` directives.
- Private fields: `playerInventory` → `_playerInventory`, `lootPool` → `_lootPool`.
- Construction: explicit loot-pool `new List<ItemInstance>()` → target-typed `new()`.
- Add XML documentation to `PartyLootService`, its constructor, `AddLootFromSource`, `AddToPool`, and `DistributeLoot`.

#### `Assets/Runtime/Server/Services/PlayerAuthService.cs`

- Namespace: `Game.Server.Persistence` → `Game.Server.Services`.
- Alphabetize `using` directives.
- Constants: `SaltSize` → `SALT_SIZE`, `HashSize` → `HASH_SIZE`, `Iterations` → `ITERATIONS`.
- Use target-typed `new()` for `_validatedUsernamesQueue`, `_newPlayerQueue`, and `_pendingConnections`.
- Update all constant references in salt/hash generation.
- Add XML documentation to currently undocumented public interface implementations: `TrackPendingConnection`, `TryRemovePendingConnection`, `GetNextValidatedUsername`, `GetNextPlayerIsNew`, and `EnqueueNewPlayerFlag`.
- Update `Game.Shared.Authentication` references to the final Shared Networking namespace.
- Preserve PBKDF2 parameters and authentication behaviour exactly.

### 32.5 Systems

#### `Assets/Runtime/Server/Systems/ActorComponents.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Rename file to `ActorComponent.cs`.
- Alphabetize `using` directives.
- Acronym fields:
  - `ActorComponent.ActorId` → `ActorID`;
  - `ResourceRegenElement.ResourceId` → `ResourceID`;
  - `AuraElement.InstanceId` → `InstanceID`;
  - `AuraElement.AuraDefinitionId` → `AuraDefinitionID`;
  - `TickerElement.TickerId` → `TickerID`.
- `SpellID`, `PlayerID`, and `SceneID` already conform.
- Retain the existing lower-camel public ECS fields that do not contain an acronym; the locked convention permits public ECS data fields but does not require a broader field-style rewrite beyond the specified acronym rule.
- Add XML documentation to the public ECS component/buffer types and externally meaningful fields.
- Update every manager/system reference to the renamed fields atomically.

#### `Assets/Runtime/Server/Systems/ActorTickerSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Update `ActorId` → `ActorID` and `TickerId` → `TickerID` references.
- Add XML documentation to `ActorTickerSystem` and its public `OnUpdate` override where currently undocumented.

#### `Assets/Runtime/Server/Systems/ActorUpdateSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Alphabetize `using` directives.
- Private field: `motionQuery` → `_motionQuery`.
- Add XML documentation to `ActorUpdateSystem`, `OnCreate`, `OnUpdate`, and `OnDestroy`.
- Preserve ECS query composition and gravity/movement behaviour.

#### `Assets/Runtime/Server/Systems/AuraTickSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Update `ActorId` → `ActorID`, `InstanceId` → `InstanceID`, and `AuraDefinitionId` → `AuraDefinitionID`.
- Add XML documentation to `AuraTickSystem` and `OnUpdate`.

#### `Assets/Runtime/Server/Systems/ChunkSpawnComponents.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Rename file to `SpawnPointComponent.cs`.
- Alphabetize `using` directives.
- In both `SpawnPointComponent` and `SpawnCommand`: `SpawnPointGuid` → `SpawnPointGUID`.
- Add XML documentation to both public ECS types and their externally meaningful fields.
- Update every Core/System reference atomically.

#### `Assets/Runtime/Server/Systems/CooldownTickSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Add XML documentation to `CooldownTickSystem` and `OnUpdate`.
- Preserve cooldown buffer mutation semantics.

#### `Assets/Runtime/Server/Systems/ResourceRegenSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Alphabetize `using` directives.
- Update `ResourceId` → `ResourceID` references.
- Add XML documentation to `ResourceRegenSystem`, `OnCreate`, `OnUpdate`, and `OnDestroy`.

#### `Assets/Runtime/Server/Systems/SpellCastSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Update `ActorId` → `ActorID` references.
- `SpellID` already conforms.
- Add XML documentation to `SpellCastSystem` and `OnUpdate`.

#### `Assets/Runtime/Server/Systems/WorldSpawnSystem.cs`

- Namespace: `Game.Server.ECS` → `Game.Server.Systems`.
- Alphabetize `using` directives.
- Update `SpawnPointGuid` → `SpawnPointGUID` references.
- Add XML documentation to `WorldSpawnSystem`, `OnCreate`, `OnUpdate`, and `OnDestroy`.
- Preserve spawn-command creation and destruction sequencing.

### 32.6 Test

#### `Assets/Runtime/Server/Test/Test_NPCPathfinder.cs`

- Namespace: `Game.Server` → `Game.Server.Test`.
- `Test_NPCPathfinder` already conforms to the `Test_` naming convention.
- Add XML documentation to the public test/prototype type where currently undocumented.
- No filename change is required.

## 33. Cross-assembly integration

The Server pass must consume the final Shared and Core naming from the earlier assembly plans. Representative coordinated updates include:

- `Game.Shared.FormattedDebug` → `Game.Shared.Utility.FormattedDebug`;
- `Game.Shared.Authentication` → `Game.Shared.Networking`;
- Shared actors/runtime classes → their final `Game.Shared.Runtime...` namespaces;
- `Actor.Id` → `Actor.ID`;
- `CharacterData.Guid` → `CharacterData.GUID`;
- `DataDefinition.DefinitionId` → `DefinitionID`;
- Shared NPC/PVP/acronym changes;
- `Game.Server.ECS` references → `Game.Server.Systems`;
- `Game.Server.ServerInitialization` → `Game.Server.Core.ServerInitialization`.

These are integration references, not duplicate ownership of the Shared/Core renames.

## 34. Server implementation order

### Stage V1 — Documentation and syntax-only cleanup

Apply:

- XML documentation;
- alphabetical `using` ordering;
- target-typed `new()` where unambiguous;
- `var` where the right-hand side clearly states the type.

Compile before proceeding.

### Stage V2 — Private fields, immutable names, callbacks and acronyms

Apply:

- private `_camelCase` field renames;
- the eight immutable-field renames;
- `TickerRegistration.onTick` / `onExpire`;
- Server-owned acronym casing;
- ECS public acronym-field renames.

Update all references atomically and compile.

### Stage V3 — Filename corrections

Apply SERVER-001 through SERVER-004 while preserving `.meta` files.

Compile after each coherent move.

### Stage V4 — Systems namespace migration

Move all nine `Game.Server.ECS` files to `Game.Server.Systems` as one coordinated symbol migration. Update all Core references at the same time.

Compile Server + Shared.

### Stage V5 — Core, Persistence, Services and Test namespace migration

Apply:

1. all 15 Core files → `Game.Server.Core`;
2. `PlayerAccountData` → `Game.Server.Persistence`;
3. `PlayerAuthService` → `Game.Server.Services`;
4. `Test_NPCPathfinder` → `Game.Server.Test`.

Update `GameBootstrapper`, Networking bindings, services, and all cross-assembly callers atomically.

### Stage V6 — Shared integration

Update Server imports/references to the final Shared namespaces and symbol names. Validate existing player-data migration through `PlayerDatabase`.

### Stage V7 — Final convention sweep

Re-audit all 37 Server C# files for:

- private fields without `_camelCase`;
- constants/static readonly fields outside `SCREAMING_SNAKE_CASE`;
- project acronym casing;
- callbacks outside `on...`;
- filename/primary-type mismatches;
- non-folder namespaces;
- public APIs without XML documentation;
- nonalphabetical `using` directives;
- obvious construction/local-typing preference breaches.

## 35. Server validation

The Server restyle is complete only when all of the following pass:

- Unity Editor C# compilation;
- dedicated-server compilation/build;
- standalone client compilation/build against the same Shared/Core revision;
- headless startup still reaches `ServerInitialization.Begin(...)`;
- Addressables initialization and `GameConfiguration` loading still complete;
- PurrNet server startup and connection-state handling are unchanged;
- authentication accepts existing accounts and still creates new test accounts as before;
- a pre-restyle player database loads successfully;
- Shared `CharacterData` migration round-trips through `PlayerDatabase`;
- character creation, character selection/enter-world, session assignment, and player spawning still work;
- global actor scene and world chunk loading/unloading still work;
- interest subscriptions and scene visibility still work;
- player position validation and NPC aggro continue to work;
- ECS actor, cooldown, aura, ticker, resource-regeneration, spell-cast, and world-spawn systems run with the renamed component fields;
- stat initialization, equipment modifiers, resource regeneration, spellcasting, cooldowns, auras, tickers, auto-attacks, reputation, experience, weapon skills, and loot retain existing behaviour;
- no PurrNet ownership/RPC/network semantics change;
- a fresh audit of all 37 Server C# files reports no remaining hard convention breaches;
- no gameplay or persistence behaviour is intentionally changed.

## 36. Game.Server result

| Item | Result |
|---|---:|
| C# files audited | 37 |
| Assembly definition files audited | 1 |
| Files requiring namespace migration | 27 |
| Explicit filename/primary-type corrections | 4 |
| Immutable (`const`/`static readonly`) renames | 8 |
| Callback-field renames | 2 |
| Server-owned Unity serialized-field migrations | 0 |
| RPC renames defined in Server | 0 |
| Functional changes intended | 0 |

**Game.Server status:** Planned; not yet restyled.