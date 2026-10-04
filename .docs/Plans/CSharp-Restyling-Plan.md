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

- Serialized fields: `description` → `_description`, `icon` → `_icon`, `isPlayable` → `_isPlayable`, `defaultPVPFaction` → `_defaultPVPFaction`, `startingReputations` → `_startingReputations`, `baseStats` → `_baseStats`; add `FormerlySerializedAs`.
- XML documentation: add docs to `PlayerStartingReputation`, `Faction`, `Reputation`, `DefaultPVPFaction`, `StartingReputations`, and `BaseStats`.

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
- Serialized fields: `instabilityDamageSchool` → `_instabilityDamageSchool`, `resourcePrefab` → `_resourcePrefab`; add `FormerlySerializedAs`.- Acronym casing: nested `RiftData.VFXId` → `VFXID`; parameter `vfxId` → `vfxID`.
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

#### `Assets/Runtime/Shared/Networking/AccountService.cs`

- Alphabetize `using` directives.
- Parameter acronym: `characterGuid` → `characterGUID` in `Server_RequestEnterWorld`.
- XML documentation: add docs to callbacks `onCharacterListReceived`, `onEnteringWorld`, `onPlayerActorAssigned`, `onEnteredWorld`, `onCharacterCreationRequest`, `onCharacterCreated`, `onEnterWorldRequest`; and RPC methods `Client_UpdateCharacterList`, `Client_SetPlayerActor`, `Server_TryCreateCharacter`, `Client_NotifyCharacterCreationResult`, `Server_RequestEnterWorld`, `Client_EnterWorld`.
- Do **not** rename `actorIdentity`; `Identity` is a word/type concept, not an `ID` suffix.

#### `Assets/Runtime/Shared/Networking/ActorService.cs`

- Alphabetize `using` directives.
- XML documentation: add docs to every currently undocumented public callback field: `onActorDeath`, `onActorSpawned`, `onActorOwnerChanged`, `onActorOwnershipTaken`, `onPlayerActorAssigned`, `onClientHoveredActorChanged`, `onClientTargetedActorChanged`, `onClientLateTargetedActorChanged`, `onClientEnteredCombat`, `onClientExitedCombat`, `onSpellCastStarted`, `onSpellCastInterrupted`, `onSpellCastCompleted`, `onCombatLogEntryReceived`, `onStartGlobalCooldown`, `onStartCooldown`, `onClientApplyAura`, `onClientTickAura`, `onClientUpdateAura`, `onClientExpireAura`, `onClientDispelAura`, `onClientSpawnVFXAtPosition`, `onClientSpawnProjectile`, `onClientTriggerEffect`, `onClientPlaySFX`, `onClientPlaySFXOnAudioSource`, `onServerActorSpawned`, `onServerActorDespawned`, `onServerActorOwnerChanged`, `onServerActorInterestChanged`, `onServerSpellCastStarted`, `onServerSpellCastTick`, `onServerSpellCastInterrupted`, `onServerSpellCastCompleted`, `onServerSpellCastDelayedAction`, `onServerRequestSpellCastCancel`, `onServerStartCooldown`, `onServerApplyAura`, `onServerAuraApplied`, `onServerAuraTick`, `onServerAuraExpired`, `onServerAuraDispelled`, `onServerActorTick`, `onServerActorTickerExpired`, `onServerAddTicker`, `onServerRemoveTickerByTag`, `onServerStartAutoAttack`, `onServerStopAutoAttack`.
- Callback names themselves already conform to the selected `on...` convention.

#### `Assets/Runtime/Shared/Networking/AuthenticationPayload.cs`

- Namespace: `Game.Shared.Authentication` → `Game.Shared.Networking`.
- XML documentation: add docs to both constructors.
- Safety: update all references, especially `LoginAuthenticator` aliases/usings in Client and Server.

#### `Assets/Runtime/Shared/Networking/CharacterService.cs`

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

#### `Assets/Runtime/Shared/Networking/MovementService.cs`
- Alphabetize `using` directives.
- XML documentation: add docs to `MovementService`, `onInterestUpdated`, `onClientInterestRequest`, `onInterestRequest`, `onServerCorrection`, `Server_RequestChunkInterest`, and `Client_UpdateInterest`.
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

#### `Assets/Runtime/Shared/Networking/StatService.cs`

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