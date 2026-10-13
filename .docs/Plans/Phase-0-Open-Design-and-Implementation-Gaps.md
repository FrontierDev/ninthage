# Phase 0 — Open Design and Implementation Gaps

**Status:** Needs manual validation

**Scope:** Inventory of unresolved design decisions, incomplete implementation contracts, deferred productionisation work and deliberately retained prototype/testing scaffolding found during the Phase 0.36 audit.

**Dependency:** Phase 0.35 must be implemented and manually validated before this inventory is accepted.

This document is an audit record, not a product-design replacement. It does not approve any current temporary behaviour, resolve an open design question, or make a new gameplay choice. The authoritative subsystem PDDs remain the source of product behaviour; this document records where the current implementation does not yet satisfy them or where the PDDs intentionally leave a decision open.

## 1. Audit method and classification

The audit covered:

- all 12 current runtime `[ToDo(...)]` sites reported by source search;
- runtime `TODO`, `TO DO`, `for now`, `for simplicity`, `prototype`, `obsolete`, temporary and hard-coded-path markers;
- the source locations named by this issue;
- the authoritative PDDs and Phase 1 implementation plan listed in the dependency section below.

Classifications used here are deliberately separate:

- **Design unresolved:** the PDD identifies a question or leaves the final rule/shape open. The inventory must not invent the answer.
- **Implementation missing but design already defined by a PDD:** the PDD already defines the required direction or contract, but the current implementation is incomplete or knowingly contradicts it.
- **Prototype/testing scaffolding deliberately retained:** the code is intentionally useful for the current prototype or tests and is not a production contract.
- **Deferred productionisation:** the current behaviour is a bounded or workable foundation, but production-scale ownership, performance, content or operational requirements remain.
- **Resolved/stale marker:** the implementation already covers the behaviour described by the marker, or the marker is no longer an actionable gap. It is recorded so the marker is not silently mistaken for a new requirement.

“Blocks Phase 1” means that Phase 1 cannot be considered complete while the item remains unresolved, not that implementation must happen in this issue. “No” means the item belongs to a later phase, is a deliberate prototype, or is a tuning/detail decision that does not block the Phase 1 foundation.

## 2. Authority and dependencies

The relevant authority is:

- [Project Roadmap](Project-Roadmap.md), especially the Phase 0 stable-foundation milestone and Phase 1 sequencing;
- [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md), especially stages 1.1–1.9 and the hand-off contracts;
- [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md) for identity, location, world entry, reconnect, actor lifetime and persistence continuity;
- [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md) for persistent worlds, authored spawn locations, streaming, actor scenes, spatial membership and instances;
- [AI and Encounter Behaviour PDD](../pdd/AI-and-Encounter-Behaviour-PDD.md) for server-authoritative perception, aggro, threat and scalable NPC activity;
- [Abilities and Talents PDD](../pdd/Abilities-and-Talents-PDD.md) for class abilities, trainer learning, spell ranks, talent validation and effective known abilities;
- [Items, Equipment and Loot PDD](../pdd/Items-Equipment-and-Loot-PDD.md) for equipment requirements, activation and loot rules;
- [Group and Raid Systems PDD](../pdd/Group-and-Raid-Systems-PDD.md) and [Dungeon and Group Content PDD](../pdd/Dungeon-and-Group-Content-PDD.md) for group ownership, group credit and loot-mode authority;
- [Combat System PDD](../pdd/Combat-System-PDD.md) for cast lifecycle, resource costs, absorption, auras, damage, threat and combat feedback;
- [Class Design PDD](../pdd/Class-Design-PDD.md) for the provisional Sorcerer/Instability direction and unresolved class mechanics;
- [Quest, Narrative and Dialogue PDD](../pdd/Quest-Narrative-and-Dialogue-PDD.md) for conditional dialogue, authored actions and dialogue authority;
- [Audio and Music PDD](../pdd/Audio-and-Music-PDD.md) for Addressable audio, concurrency, cache/lifetime responsibilities and deferred audio budgets;
- [UI and UX PDD](../pdd/UI-and-UX-PDD.md) for dialogue, nameplates, resource bars, aura presentation and icon expectations;
- [Character Stats and Progression PDD](../pdd/Character-Stats-and-Progression-PDD.md) where progression/stat ownership is a dependency.

The C# architecture and style documents were also reviewed. They describe the current code boundary but do not override the PDD ownership above.

## 3. Open design and implementation inventory

### GAP-01 — Player actor component lookup and ownership

- **Source:** `Assets/Runtime/Shared/Runtime/Actors/PlayerActor.cs`, `PlayerActor.SetCharacterData` and related accessors.
- **Current behaviour:** the player actor repeatedly resolves `ActorSpellcaster`, `PlayerInventory`, `PlayerEquipment`, `PlayerExperience`, `PlayerTalents`, `PlayerReputation`, `PlayerQuests` and `ActorEvents` through `GetComponent`. The `[ToDo]` asks for cached component references.
- **Classification:** Deferred productionisation. This is a lookup/ownership and lifecycle concern, not a new gameplay rule.
- **Authority/dependency:** [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) stages 1.3–1.4 require explicit ECS/GameObject ownership and measured hot-path cleanup. The implementation must preserve the current component contracts and PurrNet lifecycle.
- **Blocks Phase 1:** No as a standalone cache change; it belongs in the Phase 1 actor ownership/performance work if profiling shows it is material.

### GAP-02 — Authored spawn and invalid-location recovery

- **Source:** `Assets/Runtime/Server/Core/ServerSpawnManager.cs`, `SpawnPlayerCoroutine`; `Assets/Runtime/Server/Services/EnterWorldService.cs`, world-entry constructor.
- **Current behaviour:** player entry requests chunk `(0, 0)`, loads `PlayerPrefab`, uses position `(0, 5, 0)`, and calls `SpawnAt("World_0_0", ...)`. `EnterWorldService` describes sending the player to a hard-coded scene. Saved character location is not the source of the spawn decision.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md) requires persistent location and an ordered invalid-location recovery policy rather than arbitrary origin spawning. [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md) defines authored spawn locations, persistent actor scenes, explicit world/map/instance identity and Addressable additive world content.
- **Blocks Phase 1:** Yes. Durable location, world entry, reconnect and restart continuity are part of the Phase 1 character/persistence and world-runtime foundation.
- **Open implementation boundary:** the eventual spawn-selection service must decide how authored safe checkpoints, persistent location, runtime-instance recovery and final starting-location fallback compose. This issue does not choose that policy.

### GAP-03 — Server world bootstrap remains a single-scene demonstration

- **Source:** `Assets/Runtime/Server/Core/ServerInitialization.cs`, `ServerLoadWorldScene`.
- **Current behaviour:** initialization adds the world manager, loads `global_actors`, and the comment describes the path as loading a single scene for demonstration. Dynamic chunk loading is then handled by the existing world/interest managers rather than by a complete world selection/instance bootstrap.
- **Classification:** Deferred productionisation; the required world-runtime direction is defined, while the final bootstrap/selection integration is incomplete.
- **Authority/dependency:** [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md) requires explicit persistent worlds, Addressable additive scenes and logical runtime instances independent of Unity scenes. [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md) owns account home-world and entry context.
- **Blocks Phase 1:** Yes for the complete Phase 1 world/session milestone; the existing chunk manager may remain the implementation baseline while the bootstrap contract is completed.

### GAP-04 — Class-spell repair/initial seeding is unconditional

- **Source:** `Assets/Runtime/Server/Core/ServerSpawnManager.cs`, the class-spell repair loop in `SpawnPlayerCoroutine`.
- **Current behaviour:** every entry in `ClassSpellList` is added to `CharacterData.KnownSpellIDs` when absent, without checking level requirement, trainer access, rank prerequisites, cost or the distinction between persistent trainer knowledge and effective specialisation/talent abilities.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Abilities and Talents PDD](../pdd/Abilities-and-Talents-PDD.md) explicitly identifies this current behaviour as incorrect and requires trainer-authoritative learning, rank/level/prerequisite/cost validation, and no automatic spawn-time grant merely because an ability appears in `ClassSpellList`.
- **Blocks Phase 1:** Yes for authoritative progression/known-ability state and safe character restoration. The trainer/content implementation itself may be sequenced after the shared validation and persistence contracts.

### GAP-05 — NPC aggro performs an all-NPC, per-frame physics search

- **Source:** `Assets/Runtime/Server/Core/ServerActorManager.cs`, `UpdateNPCAggro`.
- **Current behaviour:** the server copies all tracked NPC IDs, then every update performs `Physics.OverlapSphere` for each NPC, chooses the nearest player and starts an aggro timer. This allocates a collider result per NPC and does not use player-centric or shared spatial candidate queries.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [AI and Encounter Behaviour PDD](../pdd/AI-and-Encounter-Behaviour-PDD.md) owns server-authoritative perception/aggro and bounded assistance. [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) stages 1.2, 1.3 and 1.4 require instance-aware spatial queries, relevance scheduling and removal of routine per-NPC expensive perception where an equivalent shared path exists.
- **Blocks Phase 1:** Yes. This is an identified hot-path/foundation gap, though the replacement query and scheduling design must preserve target validity, instance isolation and aggro timing.

### GAP-06 — Group loot and death-credit ownership are provisional

- **Source:** `Assets/Runtime/Server/Services/PartyLootService.cs`, `PartyLootService`; `Assets/Runtime/Server/Networking/ActorDeathBindings.cs`, the NPC death handler.
- **Current behaviour:** `PartyLootService` stores one player inventory and distributes the entire pool to that player. The death handler iterates threat-table players and independently rolls/gives loot to each of them. Threat-table membership is also used as a temporary proxy for experience and quest credit.
- **Classification:** Prototype/testing scaffolding deliberately retained, with a later implementation gap.
- **Authority/dependency:** [Group and Raid Systems PDD](../pdd/Group-and-Raid-Systems-PDD.md) explicitly calls the current service and death hooks provisional test code. It assigns group membership/loot-mode authority to the group system and mechanics to [Items, Equipment and Loot PDD](../pdd/Items-Equipment-and-Loot-PDD.md); the [Dungeon and Group Content PDD](../pdd/Dungeon-and-Group-Content-PDD.md) defers detailed distribution to the Items/Loot PDD.
- **Blocks Phase 1:** No. Production party/raid loot is later group/content work, but the prototype must not be treated as the production contract or used as authoritative group eligibility.

### GAP-07 — Equipment eligibility is not enforced before equip

- **Source:** `Assets/Runtime/Shared/Runtime/Actors/PlayerEquipment.cs`, `EquipItem`; related right-click path in `Assets/Runtime/Client/UI/Inventory/UI_InventorySlotEntry.cs`.
- **Current behaviour:** `EquipItem` contains a `TO DO` comment and proceeds to replace the slot/remove the inventory item without evaluating the item’s authored requirements. The inventory UI assumes weapon/armour `ValidSlots` is sufficient to issue the equip request.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Items, Equipment and Loot PDD](../pdd/Items-Equipment-and-Loot-PDD.md) requires the existing polymorphic `ConditionDefinition` framework, initial-equip validation, server authority and later inactive-but-equipped behaviour when requirements become false.
- **Blocks Phase 1:** Yes for authoritative inventory/equipment state and progression safety. The final condition set and item data remain authored by the Items system.

### GAP-08 — Talent RPCs bypass authoritative validation and reconstruction gaps remain

- **Source:** `Assets/Runtime/Shared/Runtime/Actors/PlayerTalents.cs`, `Server_RequestAddTalent`, `Server_RequestRemoveTalent`, `LoadTalents`, `RemoveTalent`.
- **Current behaviour:** the RPCs contain `TODO: Validate first` and immediately apply the requested rank. The current removal path removes the dictionary entry before reading its rank, and loading does not rebuild all spent-point state before derived effects are reconstructed.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Abilities and Talents PDD](../pdd/Abilities-and-Talents-PDD.md) requires server validation of point budget, tree depth, prerequisites and rank caps, safe refund/rebuild behaviour, persistent authoritative state and deterministic effective-ability reconstruction.
- **Blocks Phase 1:** Yes for authoritative progression and persistence continuity. Full respec UX and class breadth can follow later, but the shared server validation boundary cannot be deferred as a client-only concern.

### GAP-09 — Actor despawn lifecycle has no complete shared contract

- **Source:** `Assets/Runtime/Shared/Runtime/Actors/Actor.cs`, `OnDestroy`.
- **Current behaviour:** server destruction raises `onServerActorDespawned`, while the commented client/general `onActorDespawned` path is absent from `ActorEndpoint`. The `[ToDo]` asks broadly for remaining despawn logic, without specifying which systems own cleanup or which client notification is required.
- **Classification:** Design unresolved at the event/lifecycle boundary, with implementation work required after ownership is agreed.
- **Authority/dependency:** [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md) requires disconnect/despawn not to create resets or duplicate actors. [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md) separates persistent actor lifetime from environment-scene lifetime. [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) stages 1.3–1.5 require explicit actor cleanup, stale spatial membership removal and reconnect-safe lifetime transitions.
- **Blocks Phase 1:** Yes. The missing final event/cleanup contract must be resolved before lifecycle, spatial-index and reconnect work can be signed off.
- **Open decision:** enumerate server, owner-client and observer cleanup responsibilities without adding duplicate event paths merely to satisfy the stale comment.

### GAP-10 — Interrupted-cast refund and cast-time modifier components are incomplete

- **Source:** `Assets/Runtime/Server/Core/ServerSpellCastManager.cs`, `OnSpellCastInterrupted` and `OnSpellCastCompleted`.
- **Current behaviour:** resource costs are applied at authored cast phases, but the interrupt path only contains a TODO for refunding costs marked `RefundOnInterrupt`. The completion path applies cast-phase components, while the `Modifier components` TODO indicates that the intended modifier-component handling is not complete.
- **Classification:** Implementation missing but design already defined by a PDD for the cast/resource contract; modifier breadth remains partly open.
- **Authority/dependency:** [Combat System PDD](../pdd/Combat-System-PDD.md) defines cast lifecycle, authored resource-cost phases, interruptibility and authored interrupt refunds, and retains the `SpellCastPhase`/effect-component foundation. [Abilities and Talents PDD](../pdd/Abilities-and-Talents-PDD.md) owns rank/modifier effects and their deterministic reconstruction.
- **Blocks Phase 1:** Yes for authoritative cast/resource consistency; the exact per-spell refund policy remains authored data rather than a global assumption. Modifier breadth beyond the existing foundation is not a reason to invent a new component model in this issue.

### GAP-11 — Observer cast tick is a no-op

- **Source:** `Assets/Runtime/Shared/Runtime/Actors/ActorSpellcaster.cs`, `Observers_TickSpellCast`.
- **Current behaviour:** the observer RPC contains only `// TO DO...`; start, interrupt and finish callbacks exist, but no tick-side client presentation is produced.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Combat System PDD](../pdd/Combat-System-PDD.md) defines cast/channel lifecycle and client combat feedback. [UI and UX PDD](../pdd/UI-and-UX-PDD.md) requires cast bars to communicate progress, completion and interruption where available.
- **Blocks Phase 1:** No as a standalone presentation gap; it belongs to the combat/UI completion work after the authoritative cast contract is established.

### GAP-12 — Damage absorption is not applied

- **Source:** `Assets/Runtime/Shared/Data/SpellEffectDefinitions/SpellEffect_Damage.cs`, damage resolution after mitigation and critical-hit calculation.
- **Current behaviour:** the code contains `// TO DO` where absorption should be subtracted, then applies the full remaining damage directly to Health.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Combat System PDD](../pdd/Combat-System-PDD.md) requires absorption after mitigation, preserving pre-absorb, absorbed and final Health damage for logs/threat/meters, and supports reusable `Absorb` effects.
- **Blocks Phase 1:** Yes for the complete authoritative combat substrate if absorption effects are part of the Phase 1 baseline; otherwise it must be explicitly gated as a known combat-foundation exception rather than silently treated as complete.

### GAP-13 — Sorcerer surge-to-haste interaction is unspecified

- **Source:** `Assets/Runtime/Shared/Data/ClassBehaviours/SorcererClassBehaviour.cs`, `GenerateSurgeFromSpell`.
- **Current behaviour:** surge is generated from mana cost and instability tier, but the `[ToDo]` asks for surge to increase haste; no such conversion is currently applied.
- **Classification:** Design unresolved, not merely a missing line of code.
- **Authority/dependency:** [Class Design PDD](../pdd/Class-Design-PDD.md) treats Sorcerer Instability as a central direction but leaves the risk/reward loop, determinism, maximum behaviour and final specialisation mechanics open. The [Combat System PDD](../pdd/Combat-System-PDD.md) provides reusable stat/cast primitives but does not approve this specific conversion.
- **Blocks Phase 1:** No. This is class-depth design/content work and must not be implemented from the TODO wording alone.

### GAP-14 — Sorcerer instability damage uses a prototype value

- **Source:** `Assets/Runtime/Shared/Data/ClassBehaviours/SorcererClassBehaviour.cs`, `ApplyInstabilityDamage`.
- **Current behaviour:** the method applies the passed damage directly to Health; the `[ToDo]` calls out replacing hard-coded damage assumptions and including damage school/type. The current class code already has an instability damage school reference for combat-log presentation, but the full authored damage contract is not present.
- **Classification:** Design unresolved with implementation missing behind it.
- **Authority/dependency:** [Class Design PDD](../pdd/Class-Design-PDD.md) leaves Instability consequences and determinism open. [Combat System PDD](../pdd/Combat-System-PDD.md) owns damage resolution, schools, mitigation, absorption, combat events and class-mechanic primitives.
- **Blocks Phase 1:** No. The class mechanic must be resolved in a class/combat design pass before production implementation.

### GAP-15 — Dialogue condition wrapper and inversion boundary

- **Source:** `Assets/Runtime/Shared/Data/Dialogue/DialogueCondition.cs`.
- **Current behaviour:** `DialogueCondition` wraps a `ConditionDefinition` and exposes inversion. The `[ToDo]` asks whether the wrapper can be removed in favour of using `ConditionDefinition` directly.
- **Classification:** Design unresolved, but the current wrapper is intentionally retained rather than treated as obsolete.
- **Authority/dependency:** [Quest, Narrative and Dialogue PDD](../pdd/Quest-Narrative-and-Dialogue-PDD.md) says dialogue conditions should reuse the shared condition system and specifically retains inversion until a cleaner general NOT/composite mechanism exists. The PDD also says failed choices must be evaluated and hidden; the current choice-list integration remains incomplete.
- **Blocks Phase 1:** No. Dialogue condition evaluation is a later narrative/UI implementation dependency; the wrapper must not be removed without a replacement inversion contract.

### GAP-16 — Dialogue choice icon is authored but not presented

- **Source:** `Assets/Runtime/Client/UI/Dialogue/UI_DialogueOptionEntry.cs`, `UpdateEntry`.
- **Current behaviour:** `DialogueChoice` supports an icon, but the entry leaves `_choiceIcon` unused and the assignment commented out.
- **Classification:** Implementation missing but design already defined by a PDD.
- **Authority/dependency:** [Quest, Narrative and Dialogue PDD](../pdd/Quest-Narrative-and-Dialogue-PDD.md) retains choice icons in the dialogue model. [UI and UX PDD](../pdd/UI-and-UX-PDD.md) owns the final dialogue presentation and icon treatment.
- **Blocks Phase 1:** No; this is dialogue/UI completion work.

### GAP-17 — Client audio manager needs production resource/lifetime policy

- **Source:** `Assets/Runtime/Client/Core/ClientAudioManager.cs`, `Awake` and the cache/load methods.
- **Current behaviour:** one manager uses a bounded 50-entry LRU-like cache, one non-spatial source and simple Addressable one-shot loads. The `[ToDo]` asks about separate pools or reference counting, but exact budgets and ownership are not established in code.
- **Classification:** Deferred productionisation; exact cache/memory values remain open technical tuning rather than a product decision.
- **Authority/dependency:** [Audio and Music PDD](../pdd/Audio-and-Music-PDD.md) requires Addressable runtime assets, UI/2D and world/spatial responsibilities, priority/concurrency and bounded cache/streaming behaviour. [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) stage 1.6 owns client resource lifetime and measurable load/release behaviour.
- **Blocks Phase 1:** No for choosing separate pools versus reference counting; Yes for the broader Phase 1 client-resource ownership/measurement gate if the current manager remains the only production audio path.

### GAP-18 — Equipment/inventory UI only routes weapon and armour equip

- **Source:** `Assets/Runtime/Client/UI/Inventory/UI_InventorySlotEntry.cs`, right-click handling.
- **Current behaviour:** weapon and armour entries issue an equip request; other item types fall through to `// Other item types... TO DO`.
- **Classification:** Implementation missing but design already defined by a PDD in part; category-specific UI actions remain content-scoped.
- **Authority/dependency:** [Items, Equipment and Loot PDD](../pdd/Items-Equipment-and-Loot-PDD.md) defines item categories, consumables, active effects and equipment requirements. The exact UI action for each future item category belongs to item/content design.
- **Blocks Phase 1:** No; the authoritative inventory/equipment boundary is Phase 1 work, while category-specific client actions can follow the relevant item systems.

### GAP-19 — Resource bar selects the first configured resource

- **Source:** `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_CharacterResourceBar.cs`, `Refresh`.
- **Current behaviour:** the bar iterates configured resource definitions and returns after the first stat it finds. It does not define a multi-resource presentation or a stronger semantic selection contract.
- **Classification:** Implementation missing but the presentation direction is already defined by a PDD; the exact multi-resource treatment remains open.
- **Authority/dependency:** [UI and UX PDD](../pdd/UI-and-UX-PDD.md) requires authored semantic resource presentation and does not approve hard-coded single-resource treatment. Character resource/stat ownership remains in the [Character Stats and Progression PDD](../pdd/Character-Stats-and-Progression-PDD.md).
- **Blocks Phase 1:** No; it is client presentation work, although the resource-definition contract should be settled before broad class/resource UI authoring.

### GAP-20 — Aura UI retains obsolete stack categories and has no click action

- **Source:** `Assets/Runtime/Client/UI/Common/UI_AuraEntry.cs`, `UpdateDisplay` and `OnClick`.
- **Current behaviour:** display logic branches on `Condition_Stack`/`Condition_Extend`, while the combat/UI PDDs identify those categories as obsolete in the final aura model. Clicking currently only invokes the base UI sound behaviour.
- **Classification:** Implementation missing but the aura model is already defined by a PDD; click behaviour is deliberately unassigned UI scope.
- **Authority/dependency:** [Combat System PDD](../pdd/Combat-System-PDD.md) requires authored aura stacking/effect identity rather than the generic condition combat layer. [UI and UX PDD](../pdd/UI-and-UX-PDD.md) requires authored aura icon, stack, duration, filtering and relevance presentation.
- **Blocks Phase 1:** Yes for the shared aura model if these obsolete categories remain authoritative; no for adding an aura-click action, which is not required by the current PDD.

### GAP-21 — Runtime definition libraries use hard-coded `Resources` paths

- **Source:**
  - `Assets/Runtime/Shared/GameConfigurationManager.cs`;
  - `Assets/Runtime/Shared/Data/ClassDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/DamageSchoolDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/FactionDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/ActorStatDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/ItemDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/AuraDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/LootTableDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/QuestDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/RaceDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/SpellDefinitionLibrary.cs`;
  - `Assets/Runtime/Shared/Data/TalentDefinitionLibrary.cs`.
- **Current behaviour:** each singleton loads its definition asset from a fixed `Resources` path such as `GameConfiguration` or `SpellDefinitionLibrary`. This is a workable local-content bootstrap, but it makes the current content authority and delivery boundary implicit and prevents those definitions from participating in a remotely versioned content update without a later integration change.
- **Classification:** Deferred productionisation. The audit does not assume that every library must become remotely hotfixable; content ownership and release policy must first classify which definitions are local executable-bound data and which are Addressable/hotfixable definitions.
- **Authority/dependency:** [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) stage 1.9 requires stable definition IDs and removal of `Resources.Load` authority only for data designated as remotely hotfixable, while preserving local bootstrap/executable-bound content. The relevant subsystem PDDs listed in Section 2 own the semantics of each definition family.
- **Blocks Phase 1:** No for the initial runtime foundation; yes for the Stage 1.9 content-delivery/release gate if any of these libraries are designated remotely hotfixable. No library is changed by this issue.

## 4. Markers reviewed and explicitly excluded from the gap list

These markers were reviewed so they are not silently converted into product requirements:

| Source | Marker/current behaviour | Classification and reason | Phase 1 impact |
|---|---|---|---|
| `Assets/Runtime/Client/UI/Nameplates/UI_NameplateHealthBar.cs`, `SetRelation` | `[ToDo]` asks for a relation method, but `SetRelation` already updates bar, name and percentage colours. | **Resolved/stale marker.** The method named by the marker exists and performs the requested update. The remaining nameplate `InCombat` mode is a separate PDD implementation gap, not a reason to duplicate this method. | No |
| `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterStatTypeButton.cs` | `[Obsolete]` type with `[ToDo] Remove this as it is obsolete.` | **Prototype/testing scaffolding deliberately retained.** Removal requires scene/prefab/reference audit; this issue does not delete production assets or serialized types. | No |
| `Assets/Runtime/Client/Core/Controllers/Test_CharacterController.cs`, `OnInteractInput` | Test controller subscribes to interaction input but has an empty handler. | **Prototype/testing scaffolding deliberately retained.** No approved interaction contract is owned by this test controller. | No |
| `Assets/Runtime/Server/Test/Test_NPCPathfinder.cs` | Prototype movement component is explicitly named and documented as a test. | **Prototype/testing scaffolding deliberately retained.** It is not evidence for production NPC navigation. | No |
| `Assets/Runtime/Client/VFX/ProceduralProjectileMesh.cs`, `AutoBakeMeshToAsset` | Editor-only `Assets/Resources/...` output path under `UNITY_EDITOR`. | **Prototype/editor tooling deliberately retained.** This is an authoring output path, not runtime content authority; no runtime file or asset path is changed here. | No |
| `Assets/Runtime/Client/VFX/RiftLightning.cs` | “Temporary” comments describe local collider/list/renderer lifetimes inside the effect implementation. | **Not an unresolved design marker.** These are implementation-local temporary values, not deferred product behaviour or an authority/path decision. | No |
| `Assets/Runtime/Server/Core/ServerInitialization.cs` and `Assets/Runtime/Client/Core/ClientInitialization.cs` | `Resources/NetworkManager` is a named bootstrap prefab path. | **Intentional bootstrap dependency, not a hotfixable gameplay-definition path.** The Phase 1 plan’s `Resources.Load` removal applies to content designated remotely hotfixable and explicitly preserves local bootstrap/executable-bound content. | No standalone blocker |
| `Assets/Runtime/Client/Core/ClientWorldManager.cs`, character selection/creation managers | Named scenes such as `global_world`, `CharacterSelection` and `CharacterCreation` are used for current bootstrap/UI transitions. | **Existing bootstrap contract.** The audit found no TODO asserting that these strings are a final world-location policy; world-entry location is tracked under GAP-02/GAP-03. | No standalone blocker |
| `Assets/Runtime/Shared/Runtime/ToDoAttribute.cs` and `Assets/Editor/ToDoGenerator.cs` | Defines and reports `[ToDo]` metadata. | **Audit infrastructure, not a product gap.** The generated `todo.txt` is not modified as a substitute for this inventory. | No |

## 5. Phase 1 dependency summary

The items that should remain visible as Phase 1 sign-off dependencies are:

1. authored/persistent world location and invalid-location recovery (GAP-02);
2. world/instance bootstrap and entry context (GAP-03);
3. authoritative class ability knowledge and trainer/spawn separation (GAP-04);
4. player-centric, instance-aware NPC perception and actor spatial/lifecycle ownership (GAP-05 and GAP-09);
5. server-authoritative equipment requirements and talent validation/reconstruction (GAP-07 and GAP-08);
6. cast resource/refund consistency, absorption and any combat foundation explicitly included in the Phase 1 baseline (GAP-10 and GAP-12);
7. final aura model ownership and removal of obsolete generic combat-condition assumptions (GAP-20);
8. definition ownership and Addressables/release policy for any content designated remotely hotfixable (GAP-21).

The group-loot, dialogue, Sorcerer-specific, detailed audio-policy and presentation gaps remain tracked, but they do not become Phase 1 requirements merely because they are present in the current prototype. Their owning PDDs and later implementation stages must be used when those systems are scheduled.

## 6. Manual validation gate

This issue is intentionally **not `ai-safe`**. Before acceptance or the next Phase 0 issue:

1. The user must inspect every entry and confirm that its classification is accurate.
2. The user must confirm that no temporary behaviour has been described as approved final design.
3. The user must inspect the complete diff and confirm that only this documentation file changed.
4. The user must confirm that no runtime source, test, Unity `.meta` file, generated `todo.txt`, third-party file or product configuration changed.
5. The user must keep the issue in `needs-validation` until that review is complete.

No runtime or editor manual test is claimed by this documentation issue. Runtime validation remains the responsibility of the implementation issue that resolves each owned gap.

## 7. Audit completeness

All 12 runtime `[ToDo]` attributes were reviewed:

- Client audio cache/lifetime — GAP-17;
- party loot — GAP-06;
- player spawn — GAP-02;
- actor despawn — GAP-09;
- NPC aggro — GAP-05;
- dialogue choice icon — GAP-16;
- player component references — GAP-01;
- Sorcerer surge/haste — GAP-13;
- Sorcerer instability damage — GAP-14;
- dialogue condition wrapper — GAP-15;
- nameplate relation — explicitly excluded as resolved/stale in Section 4;
- obsolete character stat type button — explicitly excluded as retained scaffolding in Section 4.

Additional runtime TODO/“for now”/prototype markers are covered by GAP-02, GAP-03, GAP-06, GAP-07, GAP-08, GAP-10–GAP-12, GAP-18–GAP-21, or the explicit exclusions in Section 4. No runtime behaviour has been changed by this inventory.
