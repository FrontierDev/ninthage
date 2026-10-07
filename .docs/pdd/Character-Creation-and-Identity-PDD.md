# Ninth Age — Character Creation and Identity Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Character creation flow, race/class selection, names, appearance, sex/body presentation, starting state, starting locations, starting equipment, contextual onboarding tips, character-selection presentation, renaming, appearance changes and permanent identity boundaries  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended character-creation and player-character identity model for **Ninth Age**.

It is authoritative for:

- the character-creation flow;
- race and class selection at creation;
- default race/class compatibility;
- character naming rules;
- appearance customisation;
- sex/body-presentation options;
- starting level;
- starting specialisation/talent state;
- starting equipment and money;
- starting abilities and trainer relationship;
- starting faction/reputation derivation;
- racial starting locations;
- the absence of a handholding tutorial sequence;
- contextual UI tips for onboarding;
- post-creation appearance changes;
- renaming;
- race/class change policy;
- character-selection presentation;
- deleted-character restoration presentation.

This document does not own:

- account slot count, global name uniqueness enforcement, CharacterID or deletion retention — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- class fantasies and specialisations — [Class Design PDD](Class-Design-PDD.md);
- level/stat progression — [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- talent and ability progression — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- item/equipment mechanics — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- faction/reputation semantics — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- detailed world-region design — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- generic UI presentation rules — [UI and UX PDD](UI-and-UX-PDD.md).

Where the current implementation conflicts with this PDD, this document defines intended behaviour.

---

## 2. Design Pillars

### 2.1 Character creation establishes identity, not power optimisation traps

Creation should establish:

- who the character is;
- where they come from;
- how they look;
- which class they play.

Players should not need external spreadsheets to avoid irreversible cosmetic or class-selection traps.

### 2.2 Race defines cultural/racial origin

Race determines:

- racial identity;
- racial gameplay progression/starting contribution;
- default faction relationship;
- starting reputation;
- starting region;
- available racial appearance options.

Race should not arbitrarily prohibit classes unless the setting makes a combination genuinely impossible.

### 2.3 Class defines gameplay identity

Class determines the character's core combat/progression identity.

Class is a permanent ordinary character choice.

The intended route to another class is another character, supported by the account's alt/Legacy systems.

### 2.4 Appearance is presentation

Cosmetic appearance choices do not alter:

- stats;
- hitbox;
- reach;
- movement speed;
- armour;
- resources;
- combat performance.

### 2.5 Start in the real world

Ninth Age does **not** use a tutorial island, tutorial instance or mandatory handholding introduction.

A new character enters the actual persistent world.

Learning comes from:

- world context;
- natural early content;
- appropriately timed/placed UI tips;
- discovery.

### 2.6 UI tips inform; they do not gate

Contextual tips should appear when useful.

They should not:

- lock controls;
- force a scripted sequence;
- require completion to proceed;
- turn ordinary gameplay into a mandatory tutorial.

---

## 3. Character-Slot Dependency

The Account, Character and Persistence PDD defines:

**12 character slots per account.**

This PDD consumes that rule.

Character creation must fail server-side if the account cannot create another active character.

---

## 4. Character-Creation Flow

The intended creation order is:

```text
Race
→ Class
→ Appearance
→ Name
→ Review
→ Create
```

The UI may allow revisiting prior steps before final creation.

The final request is server-authoritative.

---

## 5. Why Race Comes First

Race is selected before class because it establishes:

- character model/presentation family;
- racial appearance options;
- default faction;
- starting reputation;
- racial starting region;
- race-specific stat/progression inputs.

The race-selection UI should communicate relevant identity/lore before confirmation.

---

## 6. Class Selection

After race selection, the player chooses a playable class.

The class-selection UI should communicate at minimum:

- class name;
- class fantasy;
- broad combat role(s);
- relevant class identity;
- appropriate visual iconography.

The Class Design PDD remains authoritative for class content.

---

## 7. Race/Class Compatibility

By default:

> **Every playable race may choose every playable class.**

Race/class restrictions are not the default design.

A race/class combination may be explicitly prohibited only where the setting/content requires it.

The data model should therefore support explicit exceptions rather than requiring every race to maintain a complete allow-list.

Conceptually:

```text
RaceDefinition
- IsPlayable
- DefaultFaction
- StartingReputations
- StartingLocation
- OptionalDisallowedClasses[]
```

An empty restriction list means all playable classes are available.

---

## 8. Restriction Authoring Principle

Do not add race/class restrictions merely because another MMORPG conventionally does so.

A restriction should reflect a real Ninth Age world/lore constraint.

The server must validate any authored restriction during creation.

The client only presents the available options.

---

## 9. Starting Level

Every ordinary newly created character begins at:

**Level 1.**

Character creation does not provide:

- level boosts;
- veteran starts;
- alternate high-level starts;
- tutorial-skip level grants.

Legacy may make later characters more convenient to progress without changing the level-1 starting rule.

---

## 10. Starting Experience

A newly created Level-1 character begins at the ordinary starting XP state for Level 1.

It does not receive pre-filled progress toward Level 2 unless another explicit reward has already been earned after entering the world.

---

## 11. Starting Specialisation

A new character has **no active specialisation**.

The Abilities and Talents PDD defines active specialisation selection at Level 10.

Character creation does not select a specialisation.

---

## 12. Starting Talents

A Level-1 character begins with:

- 0 spent talent points;
- 0 unspent talent points;
- no talent allocation.

Talent-point entitlement begins at Level 10 as defined by Character Stats and Progression.

---

## 13. Starting Abilities

Normal class abilities are not automatically dumped into the character's known-ability list at creation.

The starting character receives only whatever universal/basic actions are necessary for the game to function, such as:

- interaction;
- movement/traversal basics;
- ordinary auto-attack capability where applicable.

Class abilities are learned through the class-trainer system.

---

## 14. First Class Trainer

The racial starting region should provide convenient access to appropriate class trainers.

Initial Level-1 class abilities should have a training cost of:

**0 gold.**

This makes the trainer relationship part of natural world interaction without requiring the character to begin with money merely to become functional.

Later abilities/ranks follow the ordinary trainer/cost rules.

---

## 15. Starting Money

A new character begins with:

**0 gold.**

Early gameplay provides the first ordinary currency.

Creation does not provide a disposable starting wallet that bypasses early economic interactions.

---

## 16. Starting Equipment

Every class receives a small authored Level-1 starter kit.

The kit should include:

- basic appropriate weapon(s);
- minimal clothing/armour appropriate to the class;
- only the equipment needed for the character to begin functioning.

It should not normally include:

- jewellery;
- trinkets;
- gems;
- enchantments;
- high-value consumables;
- economically meaningful resale inventory.

---

## 17. Starter Item Quality

Starter equipment should be equivalent to ordinary:

**Level-1 Standard-quality equipment.**

It should establish class silhouette/fantasy without making early equipment rewards irrelevant.

Exact item definitions are class/content data.

---

## 18. Starter Item Economic Value

Starter equipment should have no meaningful economic exploitation value.

It must not enable:

- repeat-character vendor farming;
- material farming through character creation/deletion;
- other repeatable creation-based economic abuse.

Exact binding/vendor-value implementation belongs to the Items/Economy systems.

---

## 19. Starting Inventory

Characters begin with the ordinary base inventory capacity defined by the Items PDD.

Creation should not fill the inventory with tutorial clutter.

A very small number of basic consumables may be included only where they are genuinely useful content, not as mandatory tutorial props.

---

## 20. Race and Faction

Race determines the character's default faction identity at creation.

The player does not independently choose a faction unless a future race/content design explicitly requires that choice.

The current RaceDefinition concepts of:

- `DefaultPVPFaction`;
- `StartingReputations`

are appropriate foundations.

---

## 21. Starting Reputation

Starting reputation is authored by race.

This allows racial history/culture to affect initial relationships with factions.

Character creation should resolve the authored starting-reputation entries exactly once when creating the character.

They are not repeatedly re-applied on every login.

---

## 22. Starting Region

Each playable race has an authored **starting region/location** within the account's HomeWorld.

Characters do not all need to appear in one generic starter village.

The starting location should reflect:

- race;
- culture;
- faction;
- world geography.

---

## 23. Starting Location Identity

Race content should reference a stable **StartingLocationID** or equivalent authored location definition rather than storing an arbitrary raw spawn transform directly in race data.

That location can resolve:

- world/map;
- authoritative position;
- facing;
- safe fallback;
- related starting context.

This also provides the Persistence system with a final recovery location.

---

## 24. Home World and Starting Location

Initial character location conceptually resolves from:

```text
Account HomeWorldID
+
Race StartingLocationID
=
Initial world position/context
```

HomeWorldID remains account-owned.

StartingLocationID remains race/content-owned.

---

## 25. Starting-Region Services

A racial starting region should provide reasonable early access to the services required by ordinary early play, including where appropriate:

- class trainers;
- basic vendors;
- quest/content NPCs;
- safe logout location;
- routes into the wider world.

It may also expose early crafting/gathering opportunities where the world design supports them.

This is not a requirement for a scripted tutorial route.

---

## 26. No Tutorial Sequence

Ninth Age has **no mandatory tutorial experience**.

Do not create:

- tutorial island;
- tutorial dungeon;
- forced tutorial quest chain;
- mandatory step-by-step control sequence;
- locked tutorial mode;
- artificial tutorial-only map.

A new character begins in the real game world and may immediately act freely.

---

## 27. Contextual UI Tips

Onboarding is handled through contextual UI tips.

Tips should be:

- appropriately timed;
- attached to relevant UI/context;
- concise;
- non-blocking;
- dismissible where appropriate;
- shown when they help explain a newly encountered system.

Examples may include the first time the player:

- opens an inventory;
- encounters a class trainer;
- gains a talent point;
- receives an item;
- discovers a lodestone;
- opens a profession UI.

The exact tip catalogue belongs to UI/content authoring.

---

## 28. Contextual Tips Must Not Handhold

A contextual tip must not require the player to:

- click a highlighted object before continuing;
- follow a fixed route;
- kill a prescribed tutorial enemy;
- complete a tutorial quest;
- press a required sequence of inputs;
- remain in a tutorial state.

Tips explain.

The player decides what to do next.

---

## 29. Tip Persistence

The system should remember enough account/client state to avoid repeatedly showing already-acknowledged introductory tips where repetition would become intrusive.

Whether a specific tip is account-wide or character-specific is authored according to what the tip teaches.

Tips are convenience state, not gameplay progression.

---

## 30. Character Sex

The core character-creation sex options are:

- Male;
- Female.

They affect presentation only.

They do not alter gameplay statistics or progression.

A future race with explicitly different biological/presentation requirements may author an appropriate race-specific option model.

---

## 31. Sex-Dependent Presentation

Where applicable, sex selection may affect:

- body/model;
- face catalogue;
- voice set;
- hairstyles;
- facial hair;
- race-specific cosmetic options.

It must not alter combat geometry or player power.

---

## 32. Appearance Categories

Within the selected race/sex presentation, character creation should support appropriate options such as:

- height;
- body build;
- face;
- skin tone;
- hairstyle;
- hair colour;
- eye colour;
- facial hair where applicable;
- scars;
- markings;
- tattoos/paint where culturally appropriate;
- race-specific features.

Race-specific features may include things such as ears, horns, eye effects or other authored anatomy where relevant.

---

## 33. Height

Characters may vary visibly in height within a race/sex-specific authored range.

The initial target is approximately:

**±5% from the authored race/sex baseline.**

This is a presentation target rather than a hard mathematical requirement for every race.

Cosmetic height must not alter:

- collision advantage;
- hitbox;
- melee reach;
- ranged targeting;
- movement speed;
- camera exploitability.

---

## 34. Body Build

Body build should use a constrained authored model rather than arbitrary skeletal deformation.

An initial content target is approximately five broad presentations such as:

- Slender;
- Lean;
- Average;
- Broad;
- Heavy.

Exact names/count may vary by race/art pipeline.

Build is cosmetic only.

---

## 35. Face Customisation

Prefer authored face presets with limited controlled adjustments rather than unrestricted face sculpting.

An initial art/content target is approximately:

**12–20 face presets per race/sex presentation**, where production scope supports it.

The exact count is not a gameplay contract.

---

## 36. Hair Customisation

An initial art/content target is approximately:

- **15–25 hairstyles** per compatible race/sex presentation;
- a curated set of appropriate hair colours.

Hair colour should use authored swatches unless a race explicitly supports unrestricted/unusual colouration.

---

## 37. Skin and Eye Colours

Skin and eye colours are race-specific authored swatches.

The system should not expose setting-inappropriate colours merely because a shader technically permits them.

Race data/art content owns valid ranges/swatches.

---

## 38. Cosmetic Geometry and Combat

Appearance choices must not alter authoritative gameplay geometry.

The server/runtime combat representation must not grant an advantage to a player who chooses:

- shortest height;
- smallest build;
- particular sex/body model;
- particular hairstyle;
- other cosmetic configuration.

---

## 39. Character Name Structure

Every character has two required name fields:

- **First Name**
- **Last Name**

Neither field is optional.

---

## 40. Name Length

The allowed length is:

- First Name: **2–12 characters**
- Last Name: **2–12 characters**

Validation must be server-authoritative.

---

## 41. Global Full-Name Uniqueness

The normalized complete:

```text
FirstName + LastName
```

combination must be globally unique, as defined by the Account/Persistence PDD.

Individual first names and surnames may repeat.

For example:

```text
Aren Vale
Aren Thorne
Mira Vale
```

may coexist.

A second normalized `Aren Vale` may not.

---

## 42. Name Capitalisation

Valid display capitalisation is preserved for presentation.

Uniqueness comparison is case-insensitive after normalization.

Therefore:

```text
Aren Vale
aren vale
AREN VALE
```

represent the same name for uniqueness purposes.

---

## 43. Allowed Name Characters

Names should support the setting's orthography rather than impose English-only ASCII assumptions.

At minimum the system should be able to support:

- letters;
- appropriate apostrophes;
- appropriate hyphens;
- valid setting/language characters required by authored cultures.

Reject:

- digits;
- control characters;
- leading/trailing punctuation;
- repeated/abusive punctuation patterns;
- excessive/embedded whitespace where not explicitly supported.

The final Unicode/normalization implementation belongs to technical validation.

---

## 44. Name Reservation During Creation

A name is not permanently reserved merely because the player typed it into the client.

Final uniqueness is resolved atomically when the server creates the character.

If another player claims the same normalized full name first, creation fails cleanly and the user remains able to choose another name.

---

## 45. Titles

New characters begin with:

**no title.**

Titles are earned through gameplay.

Character creation does not provide decorative title selection.

---

## 46. Creation Review

Before final creation, the review step should show at minimum:

- full name;
- race;
- class;
- starting faction;
- starting region;
- final character appearance/model.

The player may return to prior creation steps before committing.

---

## 47. Final Creation Transaction

Character creation is one authoritative server-side operation.

The server validates:

- account slot availability;
- name structure;
- global normalized full-name uniqueness;
- race eligibility;
- class eligibility;
- race/class compatibility;
- appearance option validity;
- required identity data.

The server then creates:

- CharacterID;
- Level-1 progression state;
- racial/faction/reputation state;
- starter equipment/inventory;
- starting location;
- ordinary initial persistent records.

Partial creation must not leave orphaned/duplicate value.

---

## 48. Post-Creation Appearance Changes

Players may change ordinary cosmetic appearance after character creation through an in-world appearance service.

The service may support changing:

- hairstyle;
- hair colour;
- facial hair;
- face;
- body build;
- markings;
- scars;
- tattoos/paint;
- other race-appropriate cosmetic options.

Characters should not need to be deleted because the player dislikes a cosmetic choice.

---

## 49. Sex Change

Sex/presentation may be changed through the broader appearance-customisation service where the selected race supports the destination presentation.

This does not change:

- race;
- class;
- stats;
- progression;
- CharacterID.

Exact cost/location/service rules belong to Economy/world content.

---

## 50. Rename

Character rename is supported as a dedicated service.

A rename:

- changes first name and/or last name;
- preserves CharacterID;
- validates the same 2–12 character rules;
- validates global normalized full-name uniqueness;
- updates presentation without creating a new character identity.

Exact cost, cooldown and service availability remain open/economic details.

---

## 51. Race Change

Race change is **not** an ordinary supported character service.

Race is tied to:

- cultural origin;
- racial progression;
- faction identity;
- starting reputation;
- narrative/world context.

If a race-migration system is ever added, it requires an explicit design rather than reusing the appearance editor.

---

## 52. Class Change

Class change is **not** an ordinary supported character service.

Class is fundamental to:

- ability progression;
- talents;
- class stats/resources;
- equipment expectations;
- weapon skills;
- narrative/class identity.

The intended route to playing another class is creating another character.

---

## 53. Faction Change

The player does not ordinarily choose or change the race-derived default faction through the appearance editor.

Any future faction-defection/allegiance system belongs to the Factions/PvP/world narrative design and must explicitly reconcile race, reputation and world-state consequences.

---

## 54. Character Selection Screen

The character-selection experience must support the account's 12 slots.

For each occupied active character, display at minimum:

- full name;
- level;
- race;
- class;
- active specialisation if Level 10+;
- current/last known location;
- character appearance/model.

Detailed stats/inventory are not required on the selection screen.

---

## 55. Empty Character Slots

Unused slots should make creation discoverable.

The selection UI should clearly communicate:

- occupied slots;
- available slots;
- current count versus 12-character account limit.

The server remains authoritative for whether creation is currently allowed.

---

## 56. Deleted Character Restoration UI

Soft-deleted characters should appear in a separate **Restore Characters** view rather than being mixed into the active roster.

The restore view should show:

- full name;
- race;
- class;
- level;
- deletion date;
- remaining recovery period.

The Account/Persistence PDD defines the 30-day soft-deletion window.

---

## 57. Restoration Slot Rule

Restoration must not produce more than 12 active characters.

If no active slot is available, the restoration UI must explain why restoration cannot currently complete.

The underlying restored character retains its original CharacterID.

---

## 58. Appearance Persistence

Persistent character appearance must be stored by stable authored option identifiers/values sufficient to reconstruct the character.

Do not persist scene-object references as durable character identity.

Appearance survives:

- logout;
- reconnect;
- server restart;
- world transfer;
- soft deletion/restoration.

---

## 59. Race/Class Persistence

Race and class are permanent ordinary character identity fields.

They are server-owned persistent definition IDs.

Client selection is only a creation request.

---

## 60. Current Implementation — Foundations to Retain

### 60.1 Playable race flag

RaceDefinition already supports `IsPlayable`.

This is the correct basic content gate.

### 60.2 Race-owned default faction

RaceDefinition already supports `DefaultPVPFaction`.

The race-owned faction relationship is retained.

### 60.3 Starting reputation

RaceDefinition already supports `StartingReputations`.

This is an appropriate foundation for racial starting relationships.

### 60.4 Race base stats

RaceDefinition already contains racial base-stat data.

Its gameplay resolution remains owned by Character Stats and Progression.

### 60.5 Race/class definition libraries

The current client/server definition-library approach already provides authoritative definition identities for race/class validation.

### 60.6 Character-creation scene/UI separation

The existing character-creation UI/manager separation is a usable prototype foundation.

It should be extended rather than treating its current limited fields as final product scope.

---

## 61. Current Implementation — Required Changes

### 61.1 Split Name into FirstName and LastName

Current `NewCharacterData` and `CharacterData` use one `Name` string.

Replace the creation/persistence contract with separate:

- FirstName;
- LastName.

The Account/Persistence migration must preserve existing test data where appropriate.

### 61.2 Replace current 20-character single-name validation

Current CharacterCreationService only checks that one Name is non-empty and no more than 20 characters.

Implement:

- FirstName 2–12;
- LastName 2–12;
- valid character rules;
- normalization;
- global full-name uniqueness.

### 61.3 Enforce global uniqueness in persistence

Current creation code does not check name uniqueness.

The database-backed persistence layer must enforce the normalized pair atomically.

### 61.4 Enforce 12-character slot limit

Current creation code appends characters without enforcing the final account slot limit.

Add server-side transactional enforcement.

### 61.5 Add appearance payload

Current NewCharacterData contains only:

- Name;
- ClassID;
- RaceID.

Extend the creation request to carry validated appearance selections/identifiers.

### 61.6 Add sex/body presentation selection

The current creation model does not persist the final sex/presentation choice.

Add stable presentation state.

### 61.7 Add racial starting location

RaceDefinition currently has no stable StartingLocationID.

Add an authored race-start reference compatible with World Runtime/Persistence.

### 61.8 Add race/class restriction exceptions

The current creation validator checks that race/class definitions exist/playable but has no authored compatibility rule.

Add default-all-compatible support with optional explicit disallowed combinations.

### 61.9 Remove creation-time random/test level mutation

Current ServerSpawnManager contains prototype code that changes the character level to a random value between 11 and 59.

Newly created characters must remain Level 1 unless legitimate progression changes them.

Prototype spawn-time progression mutation must be removed.

### 61.10 Stop spawn-time test-data resets

Current ServerSpawnManager clears/replaces persistent inventory, equipment, talents, weapon skills, reputations and quests with test data.

Character creation/login must load the actual persisted state and must not reset it for testing in production paths.

### 61.11 Apply starting reputation once

Current server-spawn prototype applies racial starting reputation during spawn.

Starting reputation belongs to character creation and should be persisted once.

Repeated login must not append/reapply starting values.

### 61.12 Starter kit creation

Implement class-authored starter equipment/inventory as part of character creation.

Do not recreate starter gear on every spawn.

### 61.13 Starting ability/trainer flow

Remove unconditional spawn-time granting of the class spell catalogue.

Creation starts with only universal/basic actions; class abilities follow the trainer model.

### 61.14 Expand creation UI

Current creation UI primarily supports RaceSelection and class/race manager state.

Add:

- appearance;
- sex/presentation;
- first/last name;
- review;
- validation feedback.

### 61.15 Add character model preview

Creation/review should render the selected appearance/equipment presentation sufficiently for the user to understand the final character.

### 61.16 Add contextual tip framework

Do not implement a tutorial-state machine.

Implement non-blocking contextual tip triggers through the UI system.

---

## 62. Content Authoring Requirements — Race

Playable race authoring should support:

- stable RaceID;
- display name;
- description/lore;
- icon;
- playable flag;
- default faction;
- starting reputations;
- racial stat/progression data owned by Stats;
- StartingLocationID;
- valid appearance catalogues;
- sex/presentation options;
- optional disallowed class IDs;
- race-specific appearance swatches/features.

---

## 63. Content Authoring Requirements — Class Creation Data

Class content should support creation-specific metadata such as:

- display identity;
- summary/fantasy;
- role summary;
- starter equipment kit;
- starting trainer relationship/initial trainer abilities;
- presentation data needed by character creation.

Do not duplicate the full Class Design PDD inside creation data.

---

## 64. Content Authoring Requirements — Appearance

Appearance authoring should support stable IDs/categories for:

- race/sex body model;
- height range;
- build presets;
- faces;
- skin swatches;
- hairstyles;
- hair colours;
- eyes;
- facial hair;
- markings;
- scars;
- tattoos/paint;
- race-specific anatomy/features.

Invalid combinations should be detectable in editor/content validation.

---

## 65. Content Targets

The following are initial production targets rather than permanent hard minimums:

- approximately ±5% cosmetic height range where appropriate;
- approximately 5 body-build presets;
- approximately 12–20 face presets per race/sex presentation;
- approximately 15–25 hairstyles per compatible race/sex presentation;
- curated race-appropriate colour swatches.

A race may legitimately differ where its art direction demands it.

---

## 66. Contextual Tip Authoring

Tips should be data-driven enough to specify:

- stable TipID;
- trigger/context;
- text;
- optional relevant UI anchor;
- whether acknowledgement is account-wide or character-specific;
- repeat/suppression behaviour.

They must not become gameplay prerequisites.

---

## 67. Multiplayer and Server Authority

The server is authoritative for final character creation.

It validates:

- account ownership/session;
- slot availability;
- name validity/uniqueness;
- race;
- class;
- race/class compatibility;
- appearance selections;
- starting persistent state.

The client cannot create an unsupported:

- race;
- class;
- appearance combination;
- name;
- starting level;
- starting inventory;
- currency amount.

---

## 68. Locked Design Decisions

The following are locked:

1. Character creation flow is Race → Class → Appearance → Name → Review/Create.
2. New characters start at Level 1.
3. New characters have no active specialisation.
4. New characters have no talent points/allocation.
5. All playable races may choose all playable classes by default.
6. Race/class restrictions are explicit exceptions, not the default.
7. Race determines default faction.
8. Race determines starting reputation.
9. Race determines an authored starting location/region.
10. Starting location is resolved within the account HomeWorld.
11. New characters begin with 0 gold.
12. Starter equipment is small, authored, Level-1 Standard-quality class-appropriate gear.
13. Starter gear must not provide meaningful character-creation economic farming.
14. Normal class abilities are learned from class trainers.
15. Initial Level-1 class abilities cost 0 gold at the trainer.
16. Character creation does not automatically grant the ordinary class ability catalogue.
17. Ninth Age has no mandatory tutorial sequence/tutorial island/tutorial mode.
18. New characters enter the real persistent world immediately.
19. Onboarding uses appropriately timed/placed non-blocking contextual UI tips.
20. Contextual tips do not gate player actions or progression.
21. Core sex options are Male/Female where appropriate to the race.
22. Sex/presentation is cosmetic only.
23. Appearance is cosmetic only.
24. Cosmetic height/build do not change combat geometry/power.
25. First Name is required and 2–12 characters.
26. Last Name is required and 2–12 characters.
27. The normalized complete FirstName+LastName pair is globally unique.
28. Individual first/surnames may repeat.
29. Character creation does not provide a starting title.
30. Titles are earned through gameplay.
31. Ordinary cosmetic appearance can be changed later through an in-world service.
32. Sex/presentation may be changed later through appropriate appearance service.
33. Race change is not an ordinary supported service.
34. Class change is not an ordinary supported service.
35. Rename is supported while preserving CharacterID.
36. Character selection supports all 12 account slots.
37. Soft-deleted characters use a separate restoration view.
38. Character appearance persists through logout/transfer/restart/deletion restoration.
39. Race and class are persistent server-owned identity definition IDs.
40. Creation is server-authoritative and atomic.

---

## 69. Open / Deferred Details

The core design is complete.

The following remain content/UX/economic details rather than architectural blockers.

### 69.1 Exact appearance-service economics

- location;
- price;
- cooldown;
- whether certain appearance changes require specialist services.

### 69.2 Rename economics

- price;
- cooldown;
- access point.

### 69.3 Exact appearance catalogue

Final:

- face counts;
- hair counts;
- swatches;
- race-specific features.

The content targets in this PDD guide production but are not immutable minimums.

### 69.4 Exact starter items

Owned by class/item content.

### 69.5 Exact racial starting locations

Owned by world/race content.

### 69.6 Tip catalogue

Owned by UI/content authoring.

### 69.7 Voice presentation

Exact voice-selection/voice-set behaviour may be defined later with Audio/UI design.

---

## 70. Validation Criteria

The Character Creation and Identity system satisfies this PDD when all of the following are true.

### 70.1 Creation flow

- Player can select Race, Class, Appearance, Name and review before creation.
- Final creation is one server-authoritative transaction.
- Failure does not leave a partial character/value state.

### 70.2 Race/class

- Every playable race can choose every playable class unless an explicit exception exists.
- Server rejects an explicitly prohibited combination.
- Race determines default faction and starting reputation.
- Starting reputation is applied only once at creation.

### 70.3 Starting progression

- New character is Level 1.
- XP begins at the normal Level-1 starting state.
- Character has no active specialisation.
- Character has no talent allocation.
- Spawn/login does not randomly alter the level.

### 70.4 Starting items/economy

- New character starts with 0 gold.
- Class starter kit is granted exactly once.
- Starter kit is Level-1 Standard-quality-equivalent.
- Repeated character creation cannot generate meaningful sale/material profit.
- Login does not recreate the starter kit.

### 70.5 Abilities

- Ordinary class spell catalogue is not automatically learned at spawn.
- Class trainer can teach appropriate initial Level-1 abilities for 0 gold.
- Later ability/rank acquisition follows Abilities/Talents rules.

### 70.6 Starting location

- Race resolves to a stable starting-location definition.
- Account HomeWorld + racial location resolves the initial character placement.
- Persistence can use the racial start as a final safe recovery fallback.

### 70.7 No tutorial

- Character enters the real world directly.
- There is no mandatory tutorial island/instance/quest/control sequence.
- Contextual tips can appear without locking player control.
- Ignoring/dismissing a tip does not block progression.

### 70.8 Appearance

- Character can select race-valid presentation/appearance options.
- Appearance does not alter stats, hitbox, reach or movement.
- Appearance persists through relog/world transfer/restart.
- Invalid race/appearance combinations are rejected.

### 70.9 Names

- FirstName must be 2–12 characters.
- LastName must be 2–12 characters.
- Server validates allowed characters/normalization.
- Global normalized full-name duplicates are rejected.
- Same first name may exist with different surname.
- Same surname may exist with different first name.
- Case variants cannot bypass uniqueness.

### 70.10 Post-creation services

- Appearance service can change ordinary cosmetics without changing CharacterID.
- Rename preserves CharacterID.
- Race cannot be changed through ordinary appearance service.
- Class cannot be changed through ordinary character service.

### 70.11 Character selection/restoration

- Selection UI supports 12 account slots.
- Active characters show full name, level, race, class, spec where relevant, location and appearance.
- Deleted characters are shown separately for restoration.
- Restoration respects the active slot limit.

### 70.12 Current implementation regressions removed

- NewCharacterData no longer relies on one combined Name.
- Character creation enforces account slot limit.
- Character creation enforces global name uniqueness.
- ServerSpawnManager no longer injects random character levels.
- ServerSpawnManager no longer clears/replaces real persistent state with test data.
- Starting reputation is not appended on every spawn.
- Starter equipment is not repeatedly generated on login.

---

## 71. Design Summary

Ninth Age character creation establishes a persistent identity without forcing a tutorial funnel.

The player chooses:

```text
Race
→ Class
→ Appearance
→ First + Last Name
→ Review
```

Race defines cultural origin, faction/reputation and starting region.

Class defines gameplay identity.

Appearance defines presentation only.

Characters begin at Level 1 with 0 gold, a minimal Level-1 Standard starter kit, no talents and no active specialisation. Class abilities are learned from class trainers, with the initial Level-1 abilities available for free.

All playable races may choose all playable classes by default; restrictions exist only where explicitly authored.

The game starts in the real world. Ninth Age does not use a tutorial island or mandatory handholding sequence. Contextual UI tips explain systems when relevant without directing the player's path.

Names are two-part, globally unique as a normalized pair, and constrained to 2–12 characters for each first and last name.

Race and class are permanent ordinary character identity choices. Cosmetic appearance and names can be changed later without replacing CharacterID.

The intended result is a character-creation process that gives meaningful identity choices, then gets out of the way and places the player directly into the world.
