# Ninth Age — NPC and Creature Design Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** NPC and creature taxonomy, factions and disposition, difficulty classifications, population roles, spawning, variants, reusable unit authoring, interaction capabilities, world populations, rares, bosses and content-authoring requirements  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the player-facing and content-authoring design for **NPCs and creatures in Ninth Age**.

It is authoritative for:

- creature-type taxonomy;
- creature subfamilies and tags;
- NPC faction membership and derived disposition;
- difficulty classifications;
- named, rare, elite and boss distinctions;
- ambient wildlife and non-combat populations;
- NPC population roles;
- spawning and respawning philosophy;
- authored home areas;
- NPC groups and patrol definitions;
- service NPCs;
- reusable NPC/creature definitions;
- inheritance between definitions;
- presets and variants;
- appearance variants;
- interaction capabilities;
- NPC persistence expectations;
- Unity-editor requirements for NPC authoring.

This document does not define:

- detailed aggro, perception, target selection or threat logic — [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- combat resolution or stat formulas — [Combat System PDD](Combat-System-PDD.md) and [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- item and loot-system rules — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- faction reputation progression and political relationships beyond the NPC-facing disposition contract — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- quest logic — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- dungeon encounter structure — [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- exact zone populations and geography — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- technical world streaming and actor replication — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md).

---

## 2. Design Pillars

### 2.1 NPCs should be reusable content definitions

A wolf, guard, merchant or skeleton should not require its entire gameplay definition to be copied every time a variation is needed.

NPC content should be assembled from reusable definitions, inheritance and presets.

### 2.2 Classification dimensions should remain independent

What a creature **is**, how powerful it is, how it behaves and what faction it belongs to are separate concepts.

For example:

```text
Creature Type: Beast
Subfamily: Wolf
Difficulty: Elite
Behaviour: Pack Hunter
Faction: local wolf population / hostile wildlife
```

The data model must not force these concepts into one combined "NPC type".

### 2.3 Faction determines baseline disposition

Friendly, neutral and hostile behaviour should normally emerge from faction relationships rather than being duplicated as an independent authored NPC field.

### 2.4 World populations should serve more than combat

NPCs may exist for:

- combat;
- atmosphere;
- trade;
- narrative;
- transport;
- social context;
- wildlife;
- services;
- world simulation.

An NPC does not need to exist primarily as something for the player to kill.

### 2.5 Empty space is intentional

The World and Zone Design PDD requires every zone to contain some locations deliberately free of routine NPC populations for roleplaying.

NPC population systems must respect those spaces.

### 2.6 Behaviour belongs to behaviour data

NPC definitions should reference behaviour rather than encode large amounts of AI logic directly into every creature definition.

This allows the same creature identity to use different behaviours when appropriate.

---

## 3. Creature Type Taxonomy

Ninth Age initially uses the **D&D 5e creature-type taxonomy** as its broad creature classification.

The supported types are:

- Aberration
- Beast
- Celestial
- Construct
- Dragon
- Elemental
- Fey
- Fiend
- Giant
- Humanoid
- Monstrosity
- Ooze
- Plant
- Undead

This taxonomy is an initial content structure rather than a commitment to reproduce D&D creature mechanics.

Creature Type answers:

> **What kind of being is this at the broadest mechanical level?**

Creature Type may be referenced by:

- abilities;
- items;
- quests;
- professions;
- achievements;
- damage bonuses;
- resistances;
- targeting conditions;
- other content rules.

For example:

> Deals additional damage to Undead.

---

## 4. Creature Subfamilies and Tags

Creature Type is intentionally broad.

More specific classification is represented through **subfamily tags**.

Examples include:

```text
Type: Beast
Tags: Wolf, Canine

Type: Undead
Tags: Skeleton, HumanoidSkeleton

Type: Humanoid
Tags: Goblin

Type: Dragon
Tags: Drake, FireDragon
```

An NPC may have multiple applicable subfamily or taxonomy tags.

This permits mechanics such as:

- Deals additional damage to Wolves.
- Reduced damage from Canines.
- Track Goblins.
- Bonus harvesting from Spiders.
- Quest credit for Skeletons.

Subfamily tags should be data-driven and extensible rather than implemented as a large fixed enum.

A tag should only be introduced when it has a meaningful identity or mechanical/content-authoring use.

---

## 5. Classification Dimensions

NPC and creature classification is separated into at least four major dimensions.

### 5.1 Creature Type

What the creature fundamentally is.

Examples:

- Beast
- Undead
- Humanoid
- Dragon

### 5.2 Subfamily Tags

More specific identities.

Examples:

- Wolf
- Bear
- Skeleton
- Goblin
- Drake

### 5.3 Difficulty

How significant the unit is as combat content.

Initial classifications:

- Critter
- Normal
- Elite
- Boss

### 5.4 Behaviour Archetype

How the NPC generally behaves.

Examples may include:

- Passive Wildlife
- Territorial
- Predator
- Pack Hunter
- Guard
- Patrol
- Melee Combatant
- Ranged Combatant
- Caster
- Healer
- Support
- Civilian
- Merchant

Detailed AI semantics belong to the AI and Encounter Behaviour PDD.

These dimensions are independent.

A wolf does not become Elite because it is a Wolf.

A Humanoid does not become friendly because it is a Humanoid.

A Boss is not defined by belonging to a particular creature family.

---

## 6. Factions and Disposition

### 6.1 Existing faction model

Ninth Age already has a data-driven `FactionDefinition` system.

Faction definitions support:

- faction identity;
- faction categories;
- allied-faction relationships;
- enemy-faction relationships;
- playable/reputation-related metadata.

The current runtime resolves NPC relationship to a player into:

- **Allied**
- **Neutral**
- **Hostile**

using the NPC's combat faction and the player's PvP faction relationship.

This existing model is retained as the baseline.

### 6.2 NPC faction membership

An NPC may reference a faction appropriate to its combat/social identity.

The NPC definition should not normally contain an independent permanent field such as:

```text
Disposition = Hostile
```

when the same information should be derived from faction relationships.

Conceptually:

```text
NPC faction
      +
player faction / relevant relationship context
      ↓
Allied / Neutral / Hostile
```

### 6.3 Contextual relationship changes

Later systems may alter effective disposition because of:

- reputation;
- quest state;
- scripted encounter state;
- temporary hostility;
- disguises;
- war/peace state;
- other explicit gameplay rules.

Those systems should modify or override the relationship through a deliberate relationship mechanism rather than permanently duplicating faction state on every NPC definition.

The detailed reputation and faction rules belong to the Factions and Reputation PDD.

---

## 7. Population Roles

NPCs may fulfil several broad world roles.

These are descriptive roles and do not need to become one rigid enum.

### 7.1 Ambient creatures

Creatures whose primary purpose is environmental life and atmosphere.

Examples:

- birds;
- livestock;
- small wildlife;
- insects;
- harmless animals.

Ambient creatures need not provide meaningful combat rewards.

### 7.2 Ordinary world combatants

The normal hostile or potentially hostile population of the world.

Examples:

- wolves;
- bandits;
- hostile soldiers;
- undead;
- dangerous wildlife.

### 7.3 Quest and narrative NPCs

Characters whose primary purpose is narrative, dialogue or quest interaction.

Being named or narrative-important does not automatically make an NPC stronger in combat.

### 7.4 Service NPCs

Examples include:

- merchants;
- trainers;
- bankers;
- transport operators;
- profession-related NPCs;
- other service providers.

Service NPCs should use the same general actor/NPC framework rather than requiring unrelated bespoke actor architectures.

### 7.5 Guards

Settlement and faction guards are ordinary NPC definitions with appropriate:

- faction;
- equipment;
- behaviour;
- patrol/home data;
- combat capability.

### 7.6 Rare creatures

Uncommon identifiable world encounters.

Rare status is distinct from difficulty.

A rare creature may also be Elite, but **Rare** itself means uncommon/special world occurrence rather than a fixed stat multiplier.

### 7.7 Bosses

Bosses are encounter-significant units.

A Boss should represent more than simply a normal NPC with much higher health.

Boss behaviour, phases and mechanics belong primarily to the AI and Encounter Behaviour and Dungeon/Group Content PDDs.

---

## 8. Difficulty Classification

The initial NPC difficulty structure is:

### 8.1 Critter

Very low-significance creatures.

Critters may exist primarily for ambience and may have little or no meaningful combat capability.

### 8.2 Normal

Standard world and encounter NPCs.

Normal is the baseline difficulty classification.

### 8.3 Elite

Significantly stronger or more demanding than ordinary creatures of comparable level.

Elites may be:

- dangerous world enemies;
- group-oriented enemies;
- stronger guards;
- miniboss-like enemies;
- important encounter units.

### 8.4 Boss

Encounter-defining units intended to require special attention, mechanics or coordinated combat appropriate to their content.

Exact stat scaling for these classifications belongs to the character/combat balance design.

Difficulty must be explicit data and not inferred from creature size, naming or appearance.

---

## 9. Named NPCs

An NPC having a unique name does not automatically imply increased difficulty.

A named NPC may be:

- a civilian;
- quest giver;
- merchant;
- ordinary combatant;
- rare creature;
- elite;
- boss.

Naming, rarity and combat difficulty must therefore remain independent.

---

## 10. Rare Creatures

Rare creatures provide occasional unusual encounters in the world.

A rare creature should normally have:

- a distinct identity;
- a less-common spawn or availability model;
- some reason for players to value finding it.

That value may come from:

- loot;
- profession materials;
- achievements;
- exploration;
- quests;
- reputation;
- other rewards.

The game should avoid designing rares around excessively opaque or frustrating spawn timers.

Exact rare-spawn schedules are content-specific and not fixed here.

---

## 11. Ambient Wildlife

Ambient wildlife should make the world feel inhabited without turning every animal into an aggressive combat encounter.

Wildlife may:

- ignore players;
- flee;
- wander;
- graze;
- gather in groups;
- react defensively;
- become hostile only under particular conditions.

Exact behaviour belongs to the AI PDD.

Ambient populations should also respect the roleplaying spaces defined by the World and Zone Design PDD.

---

## 12. Creature Level

NPCs and creatures may have explicit character/combat levels where the progression system requires them.

The world should not automatically scale every NPC to the observing player.

Geographic progression and differences in regional danger should remain meaningful.

Exact level bands and NPC scaling formulas belong to the Character Stats and Progression PDD.

---

## 13. Spawn Philosophy

NPCs should normally have intentionally authored spatial relationships with the world.

They should not simply appear at arbitrary positions throughout a zone.

A spawn may represent:

- a specific location;
- a small allowed area;
- a group location;
- a patrol origin;
- a home territory;
- a rare spawn;
- a contextual/story spawn.

Spawn authoring must respect:

- terrain;
- settlements;
- encounter composition;
- faction geography;
- roleplaying spaces;
- world-density principles.

---

## 14. Home Areas

Combat-capable NPCs should have a meaningful authored home context.

A home area may be used by later AI systems to determine:

- roaming limits;
- patrol context;
- disengagement;
- return behaviour;
- reset state.

The NPC and Creature system owns the concept that such a home exists.

The exact leash and reset algorithms belong to the AI and Encounter Behaviour PDD.

---

## 15. Respawning

Ordinary open-world NPCs may respawn after death.

Respawning does not need to happen on one universal fixed timer.

Different content may use:

- short respawns;
- longer respawns;
- variable respawn windows;
- conditional respawns;
- group respawning;
- no respawn until an encounter resets.

The system should support some timing variation where useful so populations do not always appear mechanically synchronised.

Quest-critical NPCs should not become unnecessarily frustrating because of long or obscure respawn behaviour.

Exact timings remain content-specific.

---

## 16. NPC Groups

NPC populations may be authored as coherent groups rather than only as unrelated individual spawn points.

Examples include:

- wolf packs;
- bandit camps;
- guard squads;
- military patrols;
- travelling groups;
- encounter packs.

Group authoring may describe:

- members;
- roles;
- spawn arrangement;
- shared home area;
- patrol association;
- encounter identity.

Detailed group combat cooperation belongs to the AI and Encounter Behaviour PDD.

---

## 17. Patrols

NPC patrols are supported as an authored world feature.

A patrol may follow:

- fixed waypoints;
- a route;
- a bounded area;
- another authored movement pattern.

The NPC definition or its behaviour reference may identify that the NPC is suitable for patrol behaviour.

The spawn/world authoring data should specify the actual route or spatial patrol context.

Detailed movement decisions and navigation implementation belong to AI/technical design.

---

## 18. Interaction Capabilities

NPC interaction capabilities should be explicitly authored or referenced.

Potential capabilities include:

- dialogue;
- quest interaction;
- merchant/service interaction;
- combat;
- loot;
- transport interaction;
- profession interaction;
- other specialised interactions.

An NPC's creature type or faction should not implicitly determine all of its available interactions.

A Humanoid is not automatically a dialogue NPC.

A friendly NPC is not automatically a merchant.

---

## 19. Corpses and Defeated NPCs

Defeated combat creatures may leave temporary corpses where required for:

- looting;
- visual continuity;
- harvesting;
- quest interaction;
- other post-death interactions.

The detailed corpse lifetime, loot ownership and harvesting rules belong to their respective gameplay PDDs.

The NPC system must support a clear distinction between:

- alive;
- defeated/dead;
- corpse available;
- despawned.

---

## 20. Persistence

Most ordinary open-world NPCs do not require permanent individual persistence across a full world restart.

Persistent NPC state should be opt-in where required.

Examples that may require persistence include:

- story-significant characters;
- persistent world-state changes;
- ownership/stateful service NPCs;
- unusual rare or event state;
- other explicitly persistent content.

A normal wolf does not require a database record simply because it spawned once.

---

# 21. Reusable NPC/Creature Definitions

NPC content should be authored through reusable definitions rather than requiring a separate complete prefab/data copy for every variant.

The authoring philosophy is adapted from the successful **RPE2 Unit** model while being redesigned for Ninth Age and Unity.

A definition conceptually contains or references:

```text
NPC / Creature Definition
│
├── Identity
├── Parent Definition (optional)
├── Creature Type
├── Subfamily Tags
├── Faction
├── Base Difficulty
├── Base Level / progression data where applicable
├── Base Stats
├── Appearance(s)
├── Equipment
├── Abilities
├── Movement capabilities
├── Behaviour reference
├── Loot reference
├── Interaction capabilities
├── Persistence requirements
│
└── Presets / Variants
```

The exact C# class layout belongs to technical design.

---

## 22. Definition Inheritance

An NPC/creature definition may optionally extend another definition.

Inheritance exists to prevent unnecessary duplication and to allow increasingly specific reusable creature definitions.

Example:

```text
Wolf
    ↓
Dire Wolf
    ↓
Frost Dire Wolf
```

A child definition should store only its meaningful authored changes and additions where practical.

### 22.1 Parent immutability

Resolving a child definition must never modify the parent definition.

### 22.2 Child identity

The child remains its own content identity even when most values are inherited.

### 22.3 Scalar overrides

Where the child explicitly authors a scalar field, the child value replaces the parent value.

Examples may include:

- display name;
- faction;
- creature type;
- difficulty;
- behaviour reference.

### 22.4 Referenced keyed data

Data such as stats or comparable referenced values should resolve by stable definition key/reference so that a child can override a parent's value without duplicating the entire collection.

### 22.5 Additive lists

Lists such as applicable tags or abilities may inherit and extend according to the semantics of that field.

The precise merge semantics must be deterministic and visible in the editor.

### 22.6 Invalid inheritance

The editor and validation system must prevent:

- self-inheritance;
- inheritance cycles;
- missing parent references.

Failures should be explicit rather than silently falling back to partial definitions.

---

## 23. Presets and Variants

Presets represent selectable variants of the same underlying creature definition.

They are overlays rather than complete duplicated NPC definitions.

Example:

```text
Wolf
 ├── Young
 ├── Alpha
 └── Diseased
```

A preset may modify appropriate fields such as:

- difficulty;
- stats;
- resources;
- appearance;
- equipment;
- abilities;
- behaviour archetype/reference;
- other variant-appropriate properties.

The exact list of preset-overridable fields should be defined by technical design with the principle that presets are **lightweight overlays**.

### 23.1 Inheritance versus preset

Use **inheritance** when the result is a reusable creature subtype or independently referenceable definition.

Use a **preset** when the result is a selectable variation of the same underlying creature.

Example:

```text
Wolf
 ├── preset: Young
 ├── preset: Alpha
 └── preset: Diseased

Dire Wolf
 └── extends Wolf
```

This distinction should keep the content hierarchy comprehensible.

---

## 24. Inherited Presets

Child definitions may inherit presets from their parent.

Inherited presets should be available to the resolved child definition.

A child may also contribute additional local presets.

The editor must clearly distinguish:

- **Inherited presets — read-only in this child**
- **Local presets — editable**

Editing an inherited preset must require editing the owning parent definition rather than silently creating hidden parent mutations.

If a later technical design supports explicit preset override/replacement, that operation must be clearly represented rather than implicit.

---

## 25. Appearances

A definition may support multiple valid appearances.

A preset may optionally provide its own appearance set.

This supports:

- visual diversity within one creature;
- regional appearances;
- equipment-based role variants;
- sex/body variants where applicable;
- rare visual variants.

Appearance is distinct from gameplay identity.

Random appearance selection must not silently alter stats or gameplay unless an associated preset explicitly does so.

---

## 26. Unity Editor Authoring

NPC/creature authoring must have a purpose-built Unity Editor workflow.

The workflow should not require designers to manually reason through raw serialized inheritance data.

For an extending definition, the editor should visibly separate:

- inherited values;
- locally authored overrides;
- resolved/effective values.

### 26.1 Effective preview

Designers should be able to inspect the fully resolved result before placing/spawning it.

The effective preview should include important resolved content such as:

- identity;
- type/tags;
- faction;
- difficulty;
- stats;
- abilities;
- appearance;
- equipment;
- behaviour;
- presets.

### 26.2 Explicit overrides

A child value should become an override because the designer explicitly chooses to override it.

Unity's serialized default value must not accidentally become a child override merely because a field exists in a ScriptableObject.

This distinction is important when adapting the RPE2 inheritance concept to Unity serialization.

### 26.3 Preset editor

The editor should support:

- adding/removing/reordering local presets;
- selecting inherited presets for preview;
- preventing direct editing of inherited presets;
- showing effective preset values;
- clearly showing a preset-specific difficulty override where one exists.

---

## 27. Spawn Authoring

The current repository uses `ActorSpawnPoint` components that reference a concrete `Actor` and define a GUID, spawn cooldown and spawn-on-start behaviour.

This is a valid prototype baseline but is not the intended final content-authoring boundary.

Production spawn authoring should reference a reusable NPC/creature definition and, where required:

- preset/variant;
- level or level-resolution rule;
- spawn conditions;
- home area;
- patrol/group reference;
- respawn rule;
- appearance-selection rule.

The spawned runtime actor should be materialised from the resolved definition rather than requiring every spawn variant to exist as a separate manually maintained actor prefab.

The exact relationship between reusable network prefabs and resolved NPC data belongs to technical architecture.

---

## 28. Runtime Identity

A spawned actor must retain enough identity to determine:

- which NPC/creature definition produced it;
- which preset was selected;
- which appearance was selected where relevant;
- its runtime actor identity;
- its current level/difficulty state.

A child definition that extends a parent must retain the child's identity after resolution.

This mirrors the useful RPE2 rule that a resolved extending Unit remains identified as the selected child rather than becoming indistinguishable from its parent.

---

## 29. Current Ninth Age Implementation Baseline

The current repository already establishes several relevant systems:

- `Actor` is the common networked actor foundation;
- `NPCBehavior` contains current NPC combat faction, aggro settings, dialogue, loot references and threat state;
- `NPCStatProfile` provides current authored base NPC stats and weapon values;
- `FactionDefinition` defines allied/enemy relationships;
- `FactionRelationState` currently resolves to Hostile, Neutral or Allied;
- NPC faction relation is currently derived from NPC combat faction versus the player's PvP faction;
- `ActorSpawnPoint` provides scene-authored spawn locations, GUIDs, respawn cooldown and spawn-on-start state;
- spawned actors already participate in the server-authoritative actor/world runtime.

These systems form implementation baseline, not a requirement that all current component boundaries remain permanent.

The content model should evolve toward the reusable definition/inheritance/preset model in this PDD rather than duplicating complete actor prefabs for every creature variant.

---

## 30. Relationship to RPE2 Unit Authoring

The Ninth Age NPC definition model should borrow the following proven concepts from RPE2 Units:

- base reusable unit definitions;
- optional parent-unit inheritance;
- child-owned identity;
- parent immutability;
- deterministic merging;
- lightweight presets;
- preset-specific difficulty override;
- inherited presets available to child units;
- local child presets appended separately;
- inherited presets shown read-only in authoring tools;
- fully resolved runtime variants;
- explicit validation of invalid inheritance.

It should **not** copy RPE2's Lua/data representation directly.

Ninth Age requires a Unity-native editor and runtime architecture suitable for:

- ScriptableObject/data-definition workflows;
- GameObjects/network actors;
- Unity appearances/prefabs;
- NavMesh/movement capabilities;
- server runtime spawning;
- editor previews.

The goal is to preserve the useful authoring semantics, not the implementation language.

---

## 31. Server Authority

The server is authoritative for NPC runtime state, including:

- existence/spawning;
- authoritative position;
- health/resources;
- death;
- faction state;
- selected runtime definition/preset;
- combat state;
- loot eligibility;
- interaction eligibility where security matters.

Clients may display and predict presentation but must not determine that an NPC:

- exists;
- died;
- changed faction;
- became lootable;
- granted rewards.

---

## 32. NPC-Free Roleplaying Spaces

The World and Zone Design PDD requires every zone to contain locations deliberately free from routine NPC population for player roleplaying.

NPC spawning systems and authored population passes must respect this requirement.

These areas should not acquire routine:

- ambient creature spawns;
- patrol routes;
- hostile spawn points;
- service NPCs;

simply because an automated population system considers the area empty.

Explicit event or quest content may use such a space temporarily where deliberately authored, but the default state should preserve its intended social use.

---

## 33. Content Authoring Requirements

A designer should be able to create a new reusable creature without writing gameplay code.

At minimum, authoring must support:

- stable definition identity;
- name/description;
- optional parent definition;
- D&D-style creature type;
- subfamily tags;
- faction;
- base difficulty;
- level/progression reference where required;
- stats/resources;
- appearance set;
- equipment;
- ability references;
- movement capabilities;
- behaviour reference;
- loot reference;
- interaction capabilities;
- persistence settings;
- presets/variants.

Spawn authoring must separately support:

- location;
- chosen definition;
- chosen/default/random preset as applicable;
- level rule;
- respawn;
- grouping;
- home area;
- patrol data;
- spawn conditions.

Definition data should describe **what the NPC is**.

Spawn data should describe **where, when and in what configured form it exists in the world**.

---

## 34. Intentionally Deferred Design

The following are intentionally left to other PDDs or later balancing:

- exact NPC stat curves;
- exact Elite/Boss multipliers;
- level-scaling formula;
- aggro radii;
- perception cones;
- aggro delays;
- threat formulas;
- target selection;
- assist radius;
- pack AI;
- fleeing;
- ranged positioning;
- spell rotations;
- boss phases;
- leash distances;
- exact respawn times;
- rare spawn timers;
- loot probabilities;
- corpse duration;
- exact faction/reputation thresholds;
- exact editor class/schema implementation.

---

## 35. Locked Design Decisions

The following are locked by this PDD:

1. Broad creature classification initially uses the D&D 5e creature types: Aberration, Beast, Celestial, Construct, Dragon, Elemental, Fey, Fiend, Giant, Humanoid, Monstrosity, Ooze, Plant and Undead.
2. More specific creature identities use extensible subfamily/taxonomy tags.
3. Multiple subfamily tags may apply to one creature.
4. Creature Type, subfamily, difficulty, behaviour and faction are independent dimensions.
5. The initial difficulty classifications are Critter, Normal, Elite and Boss.
6. Rare is an orthogonal designation rather than a mandatory difficulty tier.
7. Named NPCs are not automatically stronger than unnamed NPCs.
8. Baseline Allied/Neutral/Hostile disposition is derived from faction relationships rather than duplicated as an independent permanent NPC field.
9. The existing FactionDefinition relationship system remains the baseline faction model.
10. Ambient wildlife may exist primarily for world atmosphere rather than combat.
11. NPC definitions should support explicitly authored interaction capabilities.
12. Service NPCs and guards should use the same general NPC/actor framework.
13. NPCs should normally have authored spawn locations/areas rather than arbitrary zone-wide placement.
14. NPC groups and patrols are supported content concepts.
15. Combat NPCs should have an authored home context for later AI reset/roaming rules.
16. Ordinary world NPCs may respawn, with content-specific timing.
17. Most ordinary NPCs do not require permanent individual persistence.
18. NPC/creature content should use reusable definitions.
19. Definitions may inherit from other definitions.
20. A child definition retains its own identity and must not mutate its parent.
21. Presets are lightweight variants layered on a resolved base definition.
22. Inheritance and presets serve different purposes and must remain distinct.
23. Presets may override difficulty and other explicitly supported variant fields.
24. Child definitions may use inherited presets and add local presets.
25. Inherited presets must be read-only in the child authoring context.
26. Unity authoring must distinguish inherited, local and effective values.
27. The production spawn system should resolve reusable definitions rather than require complete separate NPC prefabs for every variant.
28. Runtime NPC state remains server-authoritative.
29. NPC populations must respect the NPC-free roleplaying spaces required by the World and Zone Design PDD.
30. Detailed AI decisions remain owned by the AI and Encounter Behaviour PDD.

---

## 36. Dependencies

This PDD depends on or constrains:

- **World and Zone Design PDD** — population density, wild regions, faction geography and NPC-free roleplaying spaces;
- **World Runtime and Instancing PDD** — actor lifetime, world positioning, visibility and streamed world scenes;
- **AI and Encounter Behaviour PDD** — perception, aggro, target selection, group behaviour, movement, abilities and encounter logic;
- **Combat System PDD** — NPC combat resolution;
- **Character Stats and Progression PDD** — NPC levels and stat scaling;
- **Factions and Reputation PDD** — faction relationships and reputation-driven disposition;
- **Quest, Narrative and Dialogue PDD** — narrative/service NPC use;
- **Items, Equipment and Loot PDD** — NPC equipment and loot;
- **Dungeon and Group Content PDD** — dungeon populations, elites and bosses;
- **Open-World Events PDD** — event-specific NPC spawning and populations;
- **Movement and Traversal PDD** — creature movement capabilities where relevant.

---

## 37. Validation Criteria

The system satisfies this PDD when:

1. A creature can be classified using one of the approved broad creature types.
2. A creature can carry multiple specific subfamily tags.
3. Gameplay content can target a subfamily such as Wolf without requiring a custom hard-coded Wolf field.
4. Creature type, faction, difficulty and behaviour can vary independently.
5. NPC disposition can be resolved through the faction relationship model.
6. An NPC does not require a duplicate permanent Friendly/Neutral/Hostile field.
7. Critter, Normal, Elite and Boss units can be authored distinctly.
8. Rare status can be applied independently of difficulty.
9. A named NPC can remain Normal or non-combat if desired.
10. Ambient creatures can exist without behaving as normal hostile combat mobs.
11. Service NPCs, guards, civilians and combat NPCs use the common actor/NPC framework.
12. Designers can author groups and patrols without manually scripting every actor.
13. Spawn content can define meaningful home/spawn areas.
14. Ordinary NPCs can respawn under content-specific rules.
15. NPC-free roleplaying spaces remain free from routine automated or authored populations.
16. A reusable creature definition can be created once and referenced by many spawn locations.
17. A child creature definition can inherit from a parent without copying the full parent.
18. Editing or resolving a child does not mutate the parent definition.
19. The child retains its own runtime/content identity.
20. A base creature can provide multiple lightweight presets.
21. A preset can override its effective difficulty when deliberately authored.
22. A child definition can access inherited presets while adding its own.
23. Inherited presets are visibly read-only in the child editor.
24. The Unity editor can distinguish inherited values from local overrides.
25. Designers can inspect the fully resolved effective creature before use.
26. Invalid inheritance, including cycles and missing parents, fails clearly.
27. Spawn authoring can select a definition and optional preset rather than requiring a unique full actor prefab for every variant.
28. Runtime actors preserve definition/preset/appearance identity necessary for gameplay and debugging.
29. NPC existence, death and authoritative gameplay state remain server-controlled.
30. Detailed AI behaviour can be changed independently from the creature's taxonomic identity.
