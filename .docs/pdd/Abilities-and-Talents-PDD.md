# Ninth Age — Abilities and Talents Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Ability acquisition, spell ranks, active specialisation, specialisation ability ownership, talent trees, talent ranks, talent allocation, respecialisation, known-ability state and action-bar expectations  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended ability-acquisition, specialisation and talent-progression model for **Ninth Age**.

It is authoritative for:

- class-wide ability acquisition;
- active specialisation selection;
- the relationship between active specialisation and the three class talent trees;
- specialisation-specific known abilities;
- talent-point spending rules;
- talent-tree depth requirements;
- talent ranks;
- active abilities granted by talents;
- spell/ability ranks;
- known-ability ownership;
- respecialisation availability;
- action-bar binding expectations;
- server validation of talent and specialisation state;
- persistence requirements for abilities and talents.

This document does not define:

- playable class fantasies or the final class/specialisation roster — [Class Design PDD](Class-Design-PDD.md);
- character level and talent-point entitlement — [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- cast resolution, GCDs, cooldowns, resources, targeting, auras, hit resolution or combat effects — [Combat System PDD](Combat-System-PDD.md);
- item-granted powers and itemisation rules — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- exact trainer locations, world placement or faction access — relevant world/faction PDDs;
- detailed UI presentation — [UI and UX PDD](UI-and-UX-PDD.md).

Where the current implementation conflicts with this document, this document defines intended product behaviour.

---

## 2. Design Pillars

### 2.1 Class identity with build freedom

Each class has three specialisation talent trees.

The player chooses one **active specialisation**, but talent points may be invested freely across all three trees.

The active specialisation determines the character's primary role and exclusive specialisation ability package.

Talent investment determines the build.

### 2.2 Specialisation is not a hard talent lock

Choosing a specialisation does not lock the player out of neighbouring trees.

A character may deliberately combine talents from all three trees.

This is intended to allow:

- hybridisation;
- utility picks;
- secondary-role tools;
- build experimentation;
- meaningful trade-offs.

### 2.3 Trainers remain part of class progression

Class-wide abilities are learned from class trainers rather than appearing automatically solely because the player gained a level.

This keeps class trainers relevant throughout character progression.

### 2.4 Talents should change play, not only add percentages

Talents may:

- modify existing abilities;
- grant passive effects;
- alter resources or mechanics;
- grant new active abilities.

The talent system is not limited to passive stat bonuses.

### 2.5 Spell ranks are real progression

Abilities may have multiple learned ranks.

Ranks improve the authored base effect before ordinary stat scaling.

Spell ranks are not merely cosmetic labels for hidden automatic level scaling.

### 2.6 Action bars are unrestricted

Ninth Age uses a traditional MMORPG binding model.

The player may bind any currently known usable ability to available action-bar slots.

There is no small active-skill loadout cap.

---

## 3. Terminology

For this document:

### 3.1 Class ability

An ability available to the class independent of active specialisation, normally learned from a class trainer.

### 3.2 Specialisation

One of the class's three role/fantasy branches.

A character has one **active specialisation** at a time from level 10 onward.

### 3.3 Specialisation talent tree

The talent tree associated with one specialisation.

All three trees remain available for investment regardless of which specialisation is active.

### 3.4 Specialisation ability

An ability whose availability depends on the currently active specialisation.

### 3.5 Talent

A point-purchased node in one of the class's three talent trees.

A talent may be passive or active.

### 3.6 Spell rank / ability rank

A learned rank of an ability that changes one or more authored base values before normal stat scaling.

The term "spell" in current code is implementation terminology and includes martial/non-magical abilities where applicable.

---

## 4. Character-Level Dependency

The Character Stats and Progression PDD defines:

- talent points begin at level 10;
- one talent point is gained per level from 10 through 60;
- a level-60 character has 51 total talent points.

This PDD consumes that entitlement.

It does not independently create additional ordinary talent points.

---

## 5. Specialisation Availability

Each class has three specialisations.

The three specialisations and their talent trees are class content.

The final identity, role and naming of each specialisation belong to the Class Design PDD and subordinate class-design documents.

A character does not need to purchase access to the three talent trees from a trainer.

The talent trees are part of the class.

---

## 6. Active Specialisation Selection

At **level 10**, the character chooses an active specialisation.

The active specialisation is persistent character state.

A character may have exactly one active specialisation at a time.

Before level 10, the character has no normal active specialisation unless a future explicit class design requires an exception.

---

## 7. Active Specialisation Purpose

The active specialisation defines the character's primary combat role and specialisation identity.

Examples may include:

- tank;
- healer;
- melee damage;
- ranged damage;
- support;
- control-oriented hybrid.

Role ownership remains class-specific.

Choosing an active specialisation should have direct mechanical meaning beyond changing a label or talent-tree highlight.

---

## 8. Talent Trees Remain Fully Accessible

Choosing an active specialisation does **not** lock talent spending to that tree.

A character may spend talent points in:

- the active specialisation tree;
- either neighbouring tree;
- all three trees.

There is no mandatory minimum percentage of points that must be spent in the active tree.

---

## 9. Example Mixed Build

A level-60 character with 51 points may legally use a distribution such as:

```text
Active Specialisation: Guardian

Guardian:     31
Battlemaster: 15
Slayer:        5
Total:        51
```

The exact names above are examples only where the Class Design PDD has not locked them.

The important rule is that active specialisation and talent-point distribution are separate concepts.

---

## 10. Specialisation Defines Role

The active specialisation determines the character's intended primary role.

Talent investment in neighbouring trees may provide:

- utility;
- survivability;
- damage;
- control;
- resource support;
- situational tools.

Neighbouring-tree investment should not normally erase the active specialisation's core role identity.

For example, taking useful damage talents from another tree should not automatically transform a tank specialisation into a full damage specialisation.

---

## 11. Specialisation Ability Ownership

Changing active specialisation changes the character's known specialisation-specific ability set.

Therefore the known-ability set is conceptually composed from sources such as:

```text
Known Abilities =
    learned class abilities
  + active-specialisation abilities
  + talent-granted abilities
  + other explicitly granted persistent abilities
```

Inactive-specialisation exclusive abilities are not treated as currently known/usable solely because the class has access to that specialisation's talent tree.

---

## 12. Specialisation Abilities Are Automatically Granted

Specialisation-specific abilities are automatically acquired from the active specialisation according to their authored eligibility requirements.

They are not ordinary class-trainer purchases.

A specialisation ability may have an authored character-level requirement.

Changing active specialisation recalculates which specialisation abilities should be present in the known-ability set.

The exact number and level schedule of automatically granted specialisation abilities are class-content decisions.

---

## 13. Changing Active Specialisation

The player may change active specialisation outside combat.

Changing active specialisation must:

1. validate the requested specialisation belongs to the character's class;
2. update persistent active-specialisation state;
3. remove abilities granted exclusively by the previous active specialisation where no other source still grants them;
4. grant eligible abilities from the new active specialisation;
5. preserve class-wide learned abilities;
6. preserve valid talent allocations across all three trees;
7. preserve talent-granted abilities where the corresponding talent remains learned;
8. update the client and action-bar usability state.

Whether active-specialisation switching itself is free or invokes the respecialisation fee remains an open economic detail.

---

## 14. No Specialisation Switching During Combat

Active specialisation cannot be changed during combat.

It also cannot be changed during an active encounter state where doing so would allow role/ability-package swapping to bypass encounter design.

The exact dungeon/raid encounter-state integration belongs to the group/encounter systems.

---

## 15. Class-Wide Ability Acquisition

Class-wide abilities are learned from **class trainers**.

A class ability may define:

- class;
- minimum level;
- spell/ability rank;
- training cost;
- prerequisite previous rank where appropriate.

Reaching the required level makes the ability eligible to learn.

It does not automatically teach the ability.

---

## 16. Class Trainers

A class trainer must be able to offer the character abilities/ranks that are:

- appropriate to the character's class;
- appropriate to the character's level;
- not already learned at the offered rank;
- otherwise eligible under any explicit authored requirement.

Trainer placement and access belong to world/content design.

Training transactions are server-authoritative.

---

## 17. Class Ability Persistence

Once a class-wide ability or rank is learned, it remains part of the character's persistent class knowledge unless a future explicit mechanic says otherwise.

Changing active specialisation does not remove learned class-wide abilities.

---

## 18. Spell / Ability Ranks

Abilities may have multiple ranks.

Rank progression improves authored base values before ordinary character-stat scaling.

Conceptually:

```text
Final Effect =
    Rank Base Effect
  + ordinary stat scaling
  + applicable modifiers
```

For multiplicative or otherwise non-additive effects, the same principle applies: rank changes the authored base parameters, then normal stat/talent/equipment modification is resolved.

---

## 19. Rankable Values

A new rank may change appropriate authored values such as:

- base damage;
- base healing;
- resource cost;
- absorption amount;
- threat;
- duration;
- resource generation;
- another explicit base parameter.

A rank does not need to change every property.

---

## 20. Rank and Stat Scaling Separation

Spell rank and character-stat scaling are separate concepts.

For example, a damaging ability may conceptually resolve from:

```text
rank base damage
+ attack/spell-power coefficient contribution
+ talent/item modifiers
```

Increasing the spell rank should not silently change the stat coefficient unless that coefficient is explicitly authored to change.

---

## 21. Rank Identity

The intended data model should treat ranks as ranks of one logical ability rather than unrelated abilities that happen to share a name.

A stable logical ability identity must therefore survive across rank changes.

The exact implementation may use:

- one definition with rank data;
- a root ability definition with rank records;
- another equivalent stable representation.

It should not require the rest of gameplay to pretend that Rank 1 and Rank 2 are completely unrelated abilities.

---

## 22. Rank Acquisition

Ordinary class-ability ranks are intended to remain part of the class-trainer progression loop.

The exact trainer rank schedule and costs remain balance/content data.

Talent-granted abilities are not expected to require a separate class-trainer purchase merely to activate the talent's granted ability.

---

## 23. Lower-Rank Casting

Whether a player may deliberately cast a previously learned lower rank remains **open**.

The system architecture should not make downranking impossible before that product decision is closed.

If downranking is supported, action bars should be capable of binding either:

- a specific learned rank;
- the highest learned rank.

---

## 24. Talent-Point Spending

Talent points may be spent freely across all three talent trees.

The server must validate that:

- the character owns enough unspent points;
- the target talent belongs to one of the character's class trees;
- the requested rank is valid;
- tree-depth requirements are met;
- explicit prerequisites are met where authored;
- the operation does not exceed the talent's maximum rank.

Client UI state is not authority.

---

## 25. Talent Ranks

Talents may have:

- 1 rank;
- 2 ranks;
- 3 ranks;
- 5 ranks.

These are the standard supported maximum-rank counts.

A talent's rank count is authored per talent.

A tree is not required to contain all four types.

---

## 26. Talent Rank Cost

Each ordinary talent rank costs:

**1 talent point**

Therefore a 5-rank talent may consume up to 5 points.

A future exceptional talent should not silently use a different per-rank cost without this PDD being revisited or the exception being made explicit.

---

## 27. Talent Depth Gating

Higher talent rows/tiers are gated primarily by **points spent in that specific tree**.

They are not primarily gated by character level once the talent system is available at level 10.

Conceptually:

```text
Talent eligibility =
    enough total character talent points
  + enough points already spent in this tree
  + prerequisite talent(s), if explicitly authored
```

Exact points-required-per-row values remain a talent-tree layout decision.

---

## 28. Character-Level Talent Requirements

The current implementation contains per-talent `MinLevel`.

The final tree structure should not rely on hard-coded five-level row spacing as its main depth system.

A minimum character level may still be supported for exceptional talents where deliberately authored, but ordinary progression should use tree-point depth.

---

## 29. Talent Prerequisites

Individual talent prerequisites are supported.

The recommended design direction is to use them **sparingly**.

Most tree structure should come from points-spent requirements.

Explicit prerequisite links are most appropriate when:

- one talent directly upgrades another;
- one mechanic logically requires another mechanic;
- a capstone genuinely depends on a prior defining talent.

Whether prerequisites become more common in individual class trees remains class-content design.

---

## 30. Passive Talents

Passive talents may:

- modify character stats;
- modify resource behaviour;
- modify one or more abilities;
- react to combat events;
- grant passive mechanics;
- alter class mechanics;
- apply persistent passive effects.

They should use shared data-driven stat/spell/combat primitives where possible rather than bespoke one-off runtime code for every talent.

---

## 31. Active Talents

Talents may grant new active abilities.

An active talent becomes usable when the character has the required talent rank.

The granted ability participates in the same combat framework as other abilities:

- targeting;
- resources;
- cooldown;
- GCD;
- cast time;
- range;
- effects;
- server validation.

Talent-granted active abilities may themselves have talent ranks where the talent definition intentionally scales them.

---

## 32. Talent Modification of Existing Abilities

Talents may modify existing abilities.

The current spell-modifier/override architecture is an appropriate conceptual foundation.

A modifier may affect authored properties such as:

- range;
- cooldown;
- cast time;
- resource cost;
- components/effects;
- targeting;
- proc behaviour;
- coefficients;
- other explicitly supported spell properties.

Modifiers should be reapplied deterministically from the base ability/rank rather than permanently mutating shared definitions.

---

## 33. Talent Effects and Ranks

Where a talent has multiple ranks, its effect may scale by rank.

The implementation must not assume every modifier can be safely multiplied numerically by rank.

Some multi-rank talents may require authored per-rank values or discrete behaviour changes.

Therefore the talent system should support both:

- scalable numeric modifiers;
- explicit per-rank data/behaviour where necessary.

---

## 34. Talent Allocation Persistence

Talent allocation is persistent character state.

For each learned talent, persistence must retain at least:

- stable talent ID;
- current rank.

The server must rebuild:

- total spent points;
- points spent per tree;
- granted abilities;
- passive effects;
- spell modifiers;

from persistent talent state when the character loads.

These derived runtime values should not be trusted as independent saved truth.

---

## 35. Talent Respecialisation

Talent respecialisation/refunding may be performed anywhere, subject to combat/encounter restrictions.

Through level 20, respecialisation is free.

After level 20, respecialisation costs a fee.

The exact fee formula/value remains an Economy balance decision.

The fee must be validated and charged server-side as one atomic operation with the respec.

---

## 36. Respecialisation Scope

The implementation must support resetting talent investment.

The exact player-facing scope remains partially open:

- full reset of all three trees;
- reset of one tree;
- individual-node refunds;
- combinations of these.

The initial implementation may support full respec only if the UX is clear.

Whatever model is used, it must not allow illegal dependent talents to remain allocated.

---

## 37. Respecialisation Fee Behaviour

The following are locked:

- no fee through level 20;
- a fee exists after level 20;
- the operation may be initiated anywhere outside prohibited combat/encounter states.

The following remain open:

- exact amount;
- level scaling;
- whether repeated respecs escalate;
- whether active-specialisation switching independently incurs a fee.

The recommended direction is a level-scaled, non-escalating gold cost rather than punitive repeated-use escalation.

---

## 38. Saved Builds

Multiple saved talent/specialisation builds are **not yet a locked launch requirement**.

The base system must function correctly with one active build.

Saved builds may later become:

- a maximum-level convenience unlock;
- a Legacy/convenience feature;
- a service unlock.

If implemented, a saved build must not bypass respec costs/restrictions unless that convenience explicitly grants such behaviour.

---

## 39. Known Ability Model

The character needs one authoritative effective known-ability set.

Abilities may be present because of different sources.

A robust conceptual model is source-aware:

```text
Ability Knowledge Source
- Trainer/Class
- Active Specialisation
- Talent
- Item
- Quest/World Unlock
- Other explicit system
```

Removing one source must not remove an ability still granted by another valid source.

---

## 40. Stable Ability Ownership

The known-ability system must not rely solely on appending/removing string IDs without knowing **why** an ability is known.

This is particularly important because:

- active specialisation changes;
- talent respecs;
- items may grant abilities;
- future world/quest systems may grant abilities.

The runtime should be able to recalculate effective knowledge from authoritative persistent inputs.

---

## 41. Action-Bar Model

Action bars use an unrestricted traditional MMORPG model.

Any currently known usable ability may be bound to any compatible action-bar slot.

There is no design rule such as:

- choose only 8 active skills;
- equip only 10 known abilities;
- one ability per category.

The practical limit is the player's available bars/keybindings and UI configuration.

---

## 42. Action-Bar Persistence

Action-bar assignments should persist per character.

A binding should refer to stable ability identity and, where relevant, rank selection.

If the bound ability becomes temporarily unknown because of an active-specialisation change:

- the slot should not silently bind an unrelated ability;
- the binding may remain visibly unavailable so that returning to the prior specialisation can restore usability.

Exact UX presentation belongs to UI/UX.

---

## 43. Action Bars and Spell Ranks

The current prototype stores a spell rank on the action-bar button but does not use it authoritatively.

The final model must make rank binding explicit.

Depending on the final downranking decision, bindings may need to represent:

- highest learned rank;
- a specific rank.

The server must validate the actual cast rank.

---

## 44. Server Authority

The server is authoritative for:

- learned class abilities;
- learned ability ranks;
- active specialisation;
- specialisation eligibility;
- effective known-ability set;
- talent-point entitlement;
- talent allocation;
- per-tree points spent;
- talent prerequisites;
- talent rank caps;
- respec legality;
- respec cost;
- talent-granted abilities/effects;
- spell rank used by a cast.

The client may predict/display but cannot award itself an ability, talent or rank.

---

## 45. Persistence

Persistent character state must include at least:

- active specialisation from level 10 onward;
- trainer-learned class ability/rank state;
- talent IDs and ranks;
- action-bar assignments;
- any other permanent ability-unlock source that cannot be reconstructed from another persistent system.

Specialisation-granted abilities should normally be reconstructed from:

- active specialisation;
- character level;
- class data;

rather than redundantly saved as irreversible learned class abilities.

Talent-granted abilities should normally be reconstructed from talent allocation.

---

## 46. Class/Specialisation Data Model

The current embedded `ClassSpecialisation` struct is not an adequate long-term identity model.

Specialisations require stable identity.

The data architecture should support:

- stable specialisation ID;
- display name;
- description;
- icon;
- colour/presentation metadata;
- role identity;
- specialisation abilities;
- talent-tree definition/reference.

Talent definitions should reference specialisations/trees by stable identity rather than serializing complete copied specialisation structs.

---

## 47. Current Implementation — Foundations to Retain

### 47.1 SpellDefinition

The existing SpellDefinition provides a strong base for active abilities:

- cast time;
- cooldown;
- charges;
- range;
- channels;
- movement while casting;
- resource costs;
- components;
- tags;
- associated classes.

Combat semantics remain owned by the Combat PDD.

### 47.2 Class spell list

ClassDefinition already has:

```text
ClassSpell
- SpellDefinition
- levelRequirement
```

This is a useful foundation for trainer eligibility.

### 47.3 TalentDefinition

TalentDefinition already supports:

- maximum rank;
- minimum level;
- prerequisite talent IDs;
- spell modifiers;
- tags;
- behaviour;
- specialisation association.

The concept should be retained while fixing identity/validation issues.

### 47.4 Talent persistence

CharacterData already stores:

```text
TalentID
Rank
```

which matches the required persistent talent shape.

### 47.5 Spell overrides/modifiers

ActorSpellcaster already rebuilds per-character spell overrides from base definitions plus modifiers.

This is compatible with talents modifying abilities.

### 47.6 Action bars

The action-bar system already exposes known abilities rather than enforcing a small loadout cap.

This matches the intended binding model.

---

## 48. Current Implementation — Required Changes

### 48.1 Class spell level requirements are currently ignored

ServerSpawnManager currently adds every class spell in `ClassSpellList` to the character if missing, regardless of `levelRequirement`.

This must be replaced by the trainer-learning model.

Class spell entries should define trainer eligibility, not unconditional spawn-time grants.

### 48.2 Learn-spell request lacks a complete authoritative handler

CharacterService currently exposes `Server_RequestLearnSpell` through an event, but the current repository does not contain a complete authoritative trainer purchase flow.

Learning must validate:

- trainer interaction/access;
- class;
- level;
- rank prerequisites;
- cost;
- already-known rank;
- any additional authored requirement.

### 48.3 Active specialisation does not exist as persistent state

The current persistence model has no stable active-specialisation field.

It must be added.

### 48.4 Specialisation spell lists are not currently enforced

The current `ClassSpecialisation.spells` content is not used as an authoritative active-specialisation ability source.

This must be implemented as a deterministic known-ability contribution.

### 48.5 Embedded specialisation value copies must be removed

TalentDefinition currently serializes `List<ClassSpecialisation>`.

Because ClassSpecialisation is an embedded value type, talent assets contain copied specialisation data.

Replace this with stable specialisation/tree references or IDs.

### 48.6 Talent allocation RPCs need authoritative validation

Current `Server_RequestAddTalent` accepts requested rank and immediately applies it.

The server must validate:

- character class;
- tree membership;
- talent rank cap;
- current rank;
- available points;
- points-spent gate;
- prerequisites;
- any minimum-level exception;
- request ownership.

### 48.7 Talent removal needs correctness fixes

Current `RemoveTalent` removes the dictionary entry before attempting to read the removed rank.

The talent removal/refund path must use the pre-removal rank and rebuild dependent state safely.

### 48.8 Spent-per-tree state must be reconstructed

Current `LoadTalents` loads talents but does not rebuild `spentTalentPointsBySpecialisation`.

Per-tree spending must be derived from persistent allocation on load and after respec.

### 48.9 Client talent update must not destroy unrelated allocations

Current `Client_UpdateTalents` clears the entire client talent dictionary before applying the supplied update list.

Talent synchronization must represent either:

- a full authoritative snapshot; or
- a true delta;

and handle it consistently.

### 48.10 Persistent talent mutation must be server-owned

Runtime talent allocation/respec must update authoritative CharacterData persistence.

The runtime component cannot be the only copy of the player's choices.

### 48.11 Talent tree layout must stop assuming five-level rows

Current UI derives row position from `MinLevel` using a hard-coded level-10/5-level interval.

Tree layout/progression must instead represent explicit tree tiers/positions and points-spent requirements.

### 48.12 Spell ranks must become functional

The action bar currently stores `spellRank`, but ClientCombatManager ignores the rank when casting.

Rank must be represented in:

- learned ability state;
- action-bar binding where required;
- cast request;
- server validation;
- effect resolution.

### 48.13 Known abilities need source-aware recalculation

The current flat `KnownSpellIDs` list is insufficient by itself for active-specialisation and talent removal.

The server must distinguish persistent class knowledge from effective currently granted ability sources.

---

## 49. Talent Tree Authoring Requirements

Each talent tree should support:

- stable tree/specialisation ID;
- nodes;
- explicit node position/tier;
- maximum rank: 1, 2, 3 or 5;
- points-spent-in-tree requirement;
- optional prerequisite talent IDs;
- passive modifiers/effects;
- optional granted active ability;
- icon;
- name;
- generated/authored description;
- tags;
- validation metadata.

Tree depth must be inspectable in editor tooling.

---

## 50. Ability Rank Authoring Requirements

A rankable ability should support:

- stable logical ability ID;
- rank number;
- level requirement;
- trainer cost where trainer-learned;
- rank-specific base values;
- ability properties that remain shared across ranks;
- explicit per-rank exceptions where needed.

Editor tooling should make it difficult to create:

- missing rank sequences;
- duplicate ranks;
- a higher rank requiring a lower character level;
- inconsistent logical identity.

---

## 51. Trainer Authoring Requirements

Trainer content must be able to query the authoritative class ability/rank catalogue and show:

- currently learnable abilities;
- unavailable future abilities;
- already learned abilities/ranks;
- price;
- level requirement;
- prerequisite rank where applicable.

The PDD does not require every trainer to teach every possible class ability; regional/content restrictions may be authored later if desired.

---

## 52. UX Requirements

The talent UI must communicate:

- active specialisation;
- all three talent trees;
- total available points;
- points spent in each tree;
- each talent's current/max rank;
- points required to reach deeper rows;
- prerequisites;
- unavailable reason;
- active talent abilities;
- respec availability/cost.

The player should be able to understand that:

> choosing an active specialisation does not prevent investment in other trees.

The ability/trainer UI must communicate:

- learned rank;
- next available rank;
- level requirement;
- cost;
- whether an ability is class-wide or specialisation-derived where relevant.

---

## 53. Respecialisation UX

A respec initiated outside combat should show the fee before confirmation where a fee applies.

The operation should be atomic:

- validate;
- charge;
- reset/reallocate state as appropriate;
- rebuild effects/abilities;
- persist;
- notify client.

Failure must not consume gold while leaving the old build unchanged or partially reset.

---

## 54. Interaction with Equipment

Equipment may modify abilities through the item system.

Equipment must not normally:

- permanently teach a trainer ability merely because it was equipped;
- permanently grant a talent rank;
- alter the persistent active specialisation.

If an item grants an ability while equipped, that ability should be source-owned by the item and disappear when the source no longer applies.

---

## 55. Interaction with Character Progression

Character level controls:

- when the specialisation system begins;
- total talent-point entitlement;
- trainer eligibility;
- specialisation-ability level requirements;
- ability-rank eligibility.

Level does not determine how the player's talent points are distributed among the three trees.

---

## 56. Interaction with Combat

Abilities granted by any source ultimately resolve through the shared Combat system.

This PDD does not create separate execution rules for:

- trainer abilities;
- spec abilities;
- talent abilities;
- item abilities.

Once an ability is valid and known, the Combat PDD owns execution.

---

## 57. Locked Design Decisions

The following are locked by this PDD:

1. Each class has three specialisation talent trees.
2. A character chooses one active specialisation at level 10.
3. The active specialisation is persistent character state.
4. The active specialisation defines the character's primary role and specialisation ability package.
5. Talent points may be spent freely across all three trees.
6. Choosing one active specialisation does not lock the other two talent trees.
7. Useful talents may deliberately exist in neighbouring trees.
8. Higher talent depth is gated primarily by points spent in that specific tree.
9. Standard talent maximum ranks are 1, 2, 3 or 5.
10. Each ordinary talent rank costs one talent point.
11. Talents may be passive.
12. Talents may grant active abilities.
13. Talents may modify existing abilities.
14. Class-wide abilities are learned from class trainers.
15. Reaching a level requirement does not automatically teach an ordinary class-trainer ability.
16. Specialisation-specific abilities are automatically granted from the active specialisation according to authored eligibility.
17. Changing active specialisation changes the effective known specialisation ability set.
18. Trainer-learned class abilities remain learned across specialisation changes.
19. Talent-granted abilities depend on the corresponding talent allocation.
20. Abilities may have multiple ranks.
21. Spell/ability ranks improve authored base effects before ordinary stat scaling.
22. Ranks belong to one logical ability identity rather than being treated conceptually as unrelated abilities.
23. Action bars use an unrestricted traditional MMORPG binding model.
24. There is no small active-ability loadout cap.
25. Talent respec may be initiated anywhere outside prohibited combat/encounter states.
26. Talent respec is free through level 20.
27. Talent respec costs a fee after level 20.
28. Active specialisation cannot be changed during combat.
29. Talent allocation and ability acquisition are server-authoritative.
30. The server validates point budget, tree depth, prerequisites and rank caps.
31. Specialisations require stable IDs/references rather than copied embedded structs.
32. Talent state persists by stable talent ID and rank.
33. Active specialisation persists by stable specialisation ID.
34. Specialisation/talent-granted effective abilities should be reconstructable from authoritative state.
35. Action-bar assignments should persist per character.

---

## 58. Provisional / Open Design Decisions

The core architecture is designed.

The following remain intentionally open or provisional.

### 58.1 Downranking

Whether learned lower ranks remain deliberately castable.

Recommended direction:

- allow downranking;
- support binding a specific rank or highest learned rank.

### 58.2 Prerequisite density

Prerequisites are supported.

Recommended direction:

- use them sparingly;
- rely mainly on points-spent tree gates.

### 58.3 Exact talent tier thresholds

The number of points required to reach each deeper row/tier is not yet locked.

### 58.4 Respec fee formula

Locked: free through level 20, fee after.

Open:

- exact gold value;
- level scaling;
- repeated-use escalation.

Recommended direction:

- level-scaled;
- non-escalating.

### 58.5 Active-specialisation switching fee

Whether simply changing active specialisation incurs a fee is not yet locked.

### 58.6 Saved builds

Multiple saved builds are not yet a launch requirement.

Recommended direction:

- one base active build initially;
- saved builds later as convenience progression.

### 58.7 Specialisation ability cadence

Automatically granted specialisation abilities are locked.

The exact number and levels at which they are granted remain class-content decisions.

### 58.8 Exact trainer-rank cadence and prices

These remain balance/content decisions.

---

## 59. Dependencies

This PDD directly depends on or constrains:

- [MMORPG Master PDD](../MMORPG-Master-PDD.md);
- [Class Design PDD](Class-Design-PDD.md);
- [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Movement and Traversal PDD](Movement-and-Traversal-PDD.md);
- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md).

---

## 60. Validation Criteria

The Abilities and Talents system satisfies this PDD when:

1. A level-9 character has no ordinary active specialisation.
2. A level-10 character can choose one active specialisation.
3. Exactly one active specialisation is persisted at a time.
4. All three class talent trees remain visible/available after choosing a specialisation.
5. A character can legally invest talent points in a non-active tree.
6. A character can legally invest across all three trees if point requirements allow.
7. The server rejects spending more talent points than the character owns.
8. The server rejects a rank above the talent's maximum.
9. The server rejects spending into a talent whose tree-depth requirement is not met.
10. The server validates explicit prerequisites where present.
11. A 1-rank talent cannot receive a second rank.
12. A 2-rank talent stops at rank 2.
13. A 3-rank talent stops at rank 3.
14. A 5-rank talent stops at rank 5.
15. Points spent per tree are correctly reconstructed after login.
16. Talent-granted passive effects are reconstructed after login.
17. Talent-granted active abilities are reconstructed after login.
18. Removing/refunding a talent removes only effects/abilities that no remaining source grants.
19. Changing active specialisation removes old exclusive spec abilities.
20. Changing active specialisation grants the new eligible spec abilities.
21. Changing active specialisation does not remove learned class-trainer abilities.
22. Changing active specialisation does not erase the talent allocation.
23. Active specialisation cannot change during combat.
24. A class trainer cannot teach an ability to the wrong class.
25. A class trainer cannot teach a rank before its level requirement.
26. A class trainer cannot teach the same rank repeatedly.
27. Trainer cost is charged atomically with successful learning.
28. Class spells are not automatically granted at spawn merely because they exist in ClassSpellList.
29. Ability rank affects base ability values before stat scaling.
30. The server validates the rank used for a cast.
31. A character cannot cast an ability they do not effectively know.
32. A character can bind any known ability to an ordinary action-bar slot.
33. There is no artificial active-skill-count limit.
34. Action-bar bindings survive relog.
35. A binding to a temporarily unavailable specialisation ability cannot cast it.
36. Returning to a specialisation can restore usability of an existing binding without corrupting the slot.
37. Respec is free through level 20.
38. Respec after level 20 validates and charges the configured fee.
39. Respec cannot occur during prohibited combat/encounter state.
40. Failed respec validation does not partially remove talents or consume the fee.
41. Talent assets reference stable specialisation/tree identity rather than serialized copies of whole specialisations.
42. Talent synchronization does not clear unrelated talents when applying a single delta.
43. Persistent CharacterData reflects successful authoritative talent changes.
44. Effective known abilities can be rebuilt deterministically from persistent trainer knowledge, active specialisation and talent allocation.

---

## 61. Design Summary

Ninth Age uses a **three-tree, freely mixed talent model** with one active specialisation.

The active specialisation supplies role identity and exclusive abilities.

The talent build remains flexible:

```text
one active specialisation
+ freely mixed investment across three trees
+ 51 total points at level 60
```

Class abilities remain part of the world through class trainers.

Talents may meaningfully alter gameplay and may grant active abilities.

Abilities retain meaningful pre-stat-scaling ranks.

The action-bar model remains unrestricted and traditional rather than becoming a limited active-skill loadout.

The result should preserve the flexibility associated with Classic-style talent trees while giving active specialisation a clear mechanical role and allowing Ninth Age's class identities to remain distinct.
