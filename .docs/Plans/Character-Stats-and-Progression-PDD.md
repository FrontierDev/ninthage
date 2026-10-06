# Ninth Age — Character Stats and Progression Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Character levels, experience, stat progression, primary and derived statistics, resources, ratings, weapon skills, talent-point cadence, maximum-level progression, Legacy progression and long-term character power structure  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended character-stat and progression model for **Ninth Age**.

It is authoritative for:

- character level structure;
- the permanent character level cap;
- experience and level-up progression;
- the canonical level-scaling formula;
- race and class contribution to character statistics;
- primary-stat identities;
- derived-stat architecture;
- Health, Mana and other resource progression;
- intrinsic versus effective resource maxima;
- combat-rating level scaling;
- weapon-skill progression;
- talent-point acquisition cadence;
- progression after level 60;
- the high-level account-wide Legacy progression contract;
- the relationship between intrinsic character power and equipment power.

This document does not define:

- complete class kits or class fantasies — [Class Design PDD](Class-Design-PDD.md);
- detailed combat resolution — [Combat System PDD](Combat-System-PDD.md);
- talent-tree structure, ability acquisition beyond level/talent-point inputs, or respec rules — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- item stat budgets or item-level formulas — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- NPC challenge classifications — [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- profession progression — [Crafting System PDD](Crafting-System-PDD.md);
- reputation progression — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- account persistence implementation — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- exact UI presentation — [UI and UX PDD](UI-and-UX-PDD.md).

Where current implementation conflicts with this document, this document defines intended product behaviour.

---

## 2. Reference Architecture: RPEngine2

The stat and resource progression architecture used by **RPEngine2** is the primary reference model for Ninth Age.

RPE2 has already been exercised across:

- level-60 character progression;
- multiple classes;
- race and class stat contributions;
- Health and Mana progression;
- equipment;
- traits and auras;
- derived statistics;
- NPC scaling;
- WoW-like endgame stat magnitudes.

Ninth Age should therefore not redesign this architecture without a concrete requirement.

The design rule is:

> **Use the RPE2 progression architecture; use Ninth Age progression targets and combat balance.**

Ninth Age does **not** inherit RPE2's exact numerical coefficients simply because the architecture is shared.

RPE2 was deliberately balanced around a WoW-like stat scale. Ninth Age may choose different:

- level-1 values;
- level-60 values;
- Health/Mana coefficients;
- class growth rates;
- rating requirements;
- item stat budgets.

---

## 3. Design Pillars

### 3.1 Permanent level cap

Level 60 is a permanent foundational cap.

Future content does not require raising the level cap.

### 3.2 Character identity comes from authored progression

Race and class provide deliberate stat profiles.

Characters do not converge on one generic level-scaled stat template.

### 3.3 Progression must remain legible

A player should be able to understand where a stat comes from:

- base definition;
- race;
- class;
- level;
- equipment;
- talent/trait;
- temporary effect.

### 3.4 Level progression and endgame progression are different systems

Levels establish the character.

Endgame progression develops the established character through:

- equipment;
- content;
- professions;
- reputation;
- access;
- convenience;
- mastery;
- Legacy.

Level 60 should not become the start of an infinite replacement level system.

### 3.5 Account progression must not invalidate fresh characters

Legacy may reward long-term account breadth and encourage alts.

It must not make account age a mandatory combat-power gate.

---

## 4. Progression Layers

Ninth Age has three distinct long-term progression layers.

### 4.1 Character progression

Character-specific progression includes:

- level 1–60;
- experience;
- race/class stat growth;
- weapon skills;
- talent-point acquisition;
- abilities unlocked through dependent systems.

### 4.2 Maximum-level character progression

At level 60, progression continues through systems such as:

- equipment;
- raid and dungeon progression;
- professions;
- recipe mastery;
- faction reputation;
- world access;
- transport and service unlocks;
- convenience;
- collections;
- character mastery.

### 4.3 Legacy progression

Legacy is account-wide progression intended primarily to:

- reward breadth of play;
- encourage alternate characters;
- recognise long-term account accomplishments;
- provide small bonuses, convenience and catch-up benefits.

Legacy is not a replacement level system.

---

## 5. Character Levels

Characters begin at:

**Level 1**

The maximum character level is:

**Level 60**

A character's level is persistent and server-authoritative.

Levels are integer values.

A character cannot:

- fall below level 1;
- exceed level 60 through ordinary progression.

---

## 6. Permanent Level-60 Cap

Level 60 is not merely the launch cap.

It is the intended permanent character-level ceiling for Ninth Age.

Future expansions/content updates should use other progression systems rather than raising the character level.

There is no planned:

- level 70 expansion;
- level 80 expansion;
- prestige level reset;
- infinite paragon level;
- post-cap stat level.

A future design change to the level cap would therefore be a major product revision, not routine content scaling.

---

## 7. Experience State

Before level 60, a character stores:

- current level;
- experience earned within that level.

Experience is not required to be stored as one lifetime cumulative total.

When the threshold for the current level is reached:

1. the required amount is consumed;
2. level increases;
3. remaining excess experience carries forward;
4. the process repeats if enough experience exists for another level.

This allows one reward to cause multiple level-ups where legitimate.

---

## 8. Experience Curve

The XP requirement increases with level.

The current ExperienceCalculator already provides an accelerating 1–60 curve and is a valid implementation foundation.

The exact per-level XP table remains balance data.

The design requirements are:

- early levels progress relatively quickly;
- requirements rise smoothly;
- later levels require materially more play;
- there should be no unexplained discontinuities;
- level 60 requires no further ordinary character XP.

The XP curve should be validated against real authored content rather than treated as correct solely because the mathematical curve looks plausible.

---

## 9. Levelling Pace

Ninth Age is intended to have a substantial levelling journey rather than a short tutorial before endgame.

The exact target hours for a normal first character remain a tuning decision.

The earlier working direction of a long-form, Classic-style journey remains useful for testing, but the PDD does not lock an exact hour count before enough world/quest content exists to measure it.

Levelling pace must be measured using:

- normal questing;
- open-world combat;
- dungeon participation;
- travel time;
- exploration;
- realistic player downtime.

It must not be tuned solely against theoretical uninterrupted XP/hour.

---

## 10. Experience Sources

Experience may be awarded by authored systems including:

- quests;
- combat;
- dungeons/encounters;
- exploration/discovery;
- events;
- other explicit progression content.

The exact relative weighting is content balance.

XP rewards must be server-authoritative.

The game should avoid making one endlessly repeatable trivial activity overwhelmingly superior to engaging with the wider world.

---

## 11. Rested Experience

Ninth Age may support rested experience as a catch-up/convenience mechanic.

The intended role is:

- reduce the disadvantage of intermittent play;
- make returning to an alternate character feel productive;
- complement Legacy without replacing it.

Exact:

- accumulation rate;
- maximum rested pool;
- XP sources affected;
- multiplier;

remain balance decisions.

Rested XP must not create permanent combat power.

---

## 12. Level-Up Effects

A level-up may cause:

- race/class stat progression to resolve at the new level;
- resource maxima to update;
- weapon-skill cap to increase;
- talent-point entitlement to increase from level 10 onward;
- ability/content requirements to become satisfied;
- other authored level requirements to become available.

The level-up operation is server-authoritative.

A level-up should not grant arbitrary unallocated primary-stat points unless a future explicit system intentionally adds such a mechanic.

---

## 13. Canonical Progression Formula

All ordinary linear race/class/resource progression uses the canonical RPE2 formula:

```text
resolvedValue =
    initialValue
  + ((level - 1) × perLevelValue)
```

Therefore:

```text
Level 1 = initialValue
Level 2 = initialValue + perLevelValue
...
Level 60 = initialValue + (59 × perLevelValue)
```

This semantic applies wherever a progression entry is authored as:

- initial value;
- per-level value.

Fractional per-level values are supported.

---

## 14. Level-One Semantics

An authored `initialValue` is the value contributed at **level 1**.

Per-level progression begins when moving beyond level 1.

Therefore, implementations must not calculate:

```text
initial + level × perLevel
```

when `initial` is the level-1 value.

This is an important correction to the current Ninth Age class-growth implementation.

---

## 15. Race Progression

Race may contribute to:

- primary stats;
- other explicitly authored stats;
- resources where genuinely required.

Race progression uses the same:

```text
initial + (level - 1) × perLevel
```

model.

The normal design direction is that race strongly informs the starting profile while class increasingly defines the character's level progression.

Race does not need substantial per-level growth for every stat.

A race may have:

```text
perLevelValue = 0
```

where its contribution is intended to be primarily a level-1 baseline.

---

## 16. Class Progression

Class definitions author progression independently from race.

Class progression may contain:

- primary-stat progressions;
- direct resource progressions;
- other explicit class-level progression values where justified.

Classes should have recognisably different progression shapes.

Examples of intended identity include:

- Fighter: stronger martial and durability trajectory;
- Duelist/Ranger: stronger Dexterity trajectory;
- Sorcerer: stronger Intelligence trajectory;
- Cleric: stronger Willpower/support trajectory;
- hybrid classes: deliberate mixed growth.

Exact class numbers remain balance data.

---

## 17. Progression Authoring by Endpoints

Balance should be reviewed using meaningful level endpoints.

For any linear progression:

```text
perLevelValue =
    (targetAt60 - initialValue) / 59
```

Designers may author the per-level value directly, but tools should make it easy to inspect at least:

- level 1;
- representative mid-level values;
- level 60.

This prevents apparently small per-level differences from producing unintended endgame totals.

---

## 18. Primary Statistics

The core primary statistics are:

- Strength;
- Dexterity;
- Stamina;
- Intelligence;
- Willpower.

These are character-facing foundational stats.

They are not themselves required to be percentage values.

They may feed multiple derived statistics.

---

## 19. Strength

Strength represents physical force and heavy martial capability.

Its primary mechanical role is to contribute to:

- melee offensive power;
- selected heavy martial/defensive derived stats where appropriate.

Potential derived relationships may include:

- Melee Attack Power;
- Block-related power/rating;
- Parry-related power/rating.

Exact coefficients belong to stat balance.

Strength should not directly grant fixed percentage outcomes if the corresponding mechanic is intended to use the combat-rating system.

---

## 20. Dexterity

Dexterity represents finesse, precision and agile martial capability.

Its primary mechanical role is to contribute to:

- ranged offensive power;
- finesse melee power;
- selected avoidance/critical ratings where appropriate.

Potential derived relationships may include:

- Ranged Attack Power;
- finesse Melee Attack Power;
- Dodge Rating;
- melee/ranged critical rating.

Dexterity must not become universally dominant simply by contributing full-strength values to every offensive and defensive martial stat.

Coefficients must preserve class/stat trade-offs.

---

## 21. Stamina

Stamina represents physical endurance.

Its universal core role is:

**contribution to maximum Health**

Stamina may contribute to other explicitly designed durability statistics, but Health is its primary guaranteed identity.

The exact Health-per-Stamina coefficient remains balance data.

---

## 22. Intelligence

Intelligence represents magical knowledge, control and offensive/technical magical capability.

Its core roles include:

- contribution to maximum Mana;
- contribution to offensive spell-related power where appropriate.

Potential derived relationships include:

- Spell Power;
- maximum Mana.

The exact coefficients remain balance data.

---

## 23. Willpower

Willpower represents spiritual strength, magical endurance, healing aptitude and sustained casting capability.

It is not merely a renamed copy of Intelligence.

Its intended identity is primarily:

- healing/support power;
- resource regeneration/sustain;
- selected magical defensive or support-derived statistics where appropriate.

Potential relationships include:

- Healing Power;
- Mana regeneration;
- selected magical defensive ratings.

The exact derived-stat mapping remains subject to class/combat balance, but Willpower must retain a distinct mechanical purpose from Intelligence.

---

## 24. Data-Driven Derived Statistics

Derived statistics should be defined through data rather than hard-coded assumptions scattered throughout combat code.

A derived stat may depend on one or more source stats.

Conceptually:

```text
DerivedStat =
    Base
  + Σ(SourceStat × Coefficient)
```

This allows designs such as:

```text
Melee Attack Power =
    Strength contribution
  + Dexterity contribution
```

without requiring every derived stat to have exactly one source.

---

## 25. Multiple Derived Sources

Ninth Age should support:

```text
DerivedSources[]
    sourceStat
    coefficient
```

rather than permanently limiting ActorStatDefinition to one:

```text
SourceStat
Multiplier
```

The stat-resolution system must:

- detect dependency cycles;
- resolve dependencies deterministically;
- fail visibly on invalid cycles rather than silently producing misleading values.

---

## 26. Stat Resolution Model

The conceptual stat-resolution order follows the proven RPE2 model.

For an ordinary stat:

```text
Definition base
+ Race progression
+ Class progression
+ Explicit permanent character bonuses
+ Derived-stat contributions
+ Equipment flat bonuses
+ Temporary flat bonuses
= pre-percent value

then apply authored percentage modifiers
= effective value
```

Systems may expose the individual components for UI/debugging.

---

## 27. Percentage Modifier Ordering

Flat contributions resolve before normal percentage modifiers unless an effect explicitly defines a different operation.

Conceptually:

```text
effective =
    (base + progression + derived + equipment + flat modifiers)
    × percentage modifiers
```

Multiple percentage modifiers require one defined stacking convention in the stat engine.

The final exact additive/multiplicative grouping for different modifier families may be extended later, but it must be deterministic and documented.

---

## 28. Resources

Resources are distinct from ordinary attributes even when they are derived from attributes.

Resource definitions may be:

- manual/fixed;
- derived;
- progression-based;
- combinations of intrinsic progression and derived contribution.

The RPE2 distinction between stat progression and resource progression should be preserved conceptually.

---

## 29. Hybrid Resource Progression

Health and Mana use a hybrid model rather than being determined exclusively by one primary stat.

Conceptually:

```text
Intrinsic Health =
    Health definition base
  + race Health progression
  + class Health progression
  + Stamina-derived contribution
```

and:

```text
Intrinsic Mana =
    Mana definition base
  + race Mana progression
  + class Mana progression
  + Intelligence-derived contribution
```

This allows two classes with similar Stamina or Intelligence to retain different natural resource curves.

---

## 30. Health

Health is the universal life resource.

Its intrinsic maximum is built from:

- authored resource baseline;
- race resource progression where used;
- class resource progression;
- Stamina-derived contribution.

The exact:

- direct Health progression;
- Health-per-Stamina coefficient;
- class level-60 Health targets;

remain balance values.

Current Ninth Age's `5 × Stamina` relationship is not automatically authoritative simply because it exists in data.

---

## 31. Mana

Mana is a common magical resource where used by the class.

Its intrinsic maximum is built from:

- authored resource baseline;
- race resource progression where used;
- class resource progression;
- Intelligence-derived contribution.

The exact:

- direct Mana progression;
- Mana-per-Intelligence coefficient;
- class level-60 Mana targets;

remain balance values.

---

## 32. Intrinsic/Base Resource Maximum

The game must distinguish a character's **intrinsic/base maximum resource** from the final effective maximum.

Intrinsic/base resource maximum represents the character before ordinary equipment and temporary effects.

This is required for mechanics such as:

- “10% of base Mana”;
- “restore 20% of base Mana”;
- percentage-of-base resource costs;
- effects that must not become larger simply because temporary +Mana/+Health was applied.

---

## 33. Effective Resource Maximum

The effective maximum resource is the actual current maximum after applicable modifiers.

Conceptually:

```text
Effective Maximum =
    Intrinsic/Base Maximum
  + equipment contributions
  + temporary flat modifiers
  + applicable percentage modifiers
```

The system should expose both intrinsic/base and effective maximum values where gameplay calculations need them.

---

## 34. Special Class Resources

Not every resource should scale like Health or Mana.

Resources such as class-specific builders/spenders may use:

- fixed caps;
- fixed regeneration;
- event-based generation;
- zero-start behaviour;
- other class-specific rules.

Their progression belongs primarily to the class/ability systems.

A resource should not gain per-level scaling merely because Health and Mana do.

---

## 35. Resource Regeneration

Regeneration may be:

- fixed;
- derived from one or more stats;
- modified by equipment/talents/effects.

Willpower is expected to be an important source for magical/resource sustain where appropriate.

Exact regeneration formulas remain balance data.

Resource regeneration must distinguish between:

- intrinsic/base regeneration;
- temporary modifiers;

where an ability needs that distinction.

---

## 36. Equipment Contributions

Equipment adds to the character's authored intrinsic progression rather than replacing it.

A naked level-60 character should be materially stronger than a low-level character because level/class/race progression is real.

At the same time, equipment should be a substantial source of practical level-60 combat power.

The exact percentage split between:

- intrinsic character power;
- equipment power;

is an item/combat balance decision.

The Items PDD remains authoritative for item-level and stat-budget rules.

---

## 37. Combat Ratings

Raw combat ratings and final percentage outcomes are distinct concepts.

Examples may include:

- Hit Rating;
- Critical Rating;
- Dodge Rating;
- Parry Rating;
- Block Rating;
- Haste Rating;
- Critical Resistance;
- school-specific resistance ratings where the combat system uses them.

The same raw rating should not necessarily produce the same percentage at every character level.

---

## 38. Level-Relative Rating Conversion

Ninth Age uses a level-relative rating conversion model.

The permanent reference level is:

**60**

This is especially suitable because the character level cap is not intended to increase.

Conceptually:

```text
percentage =
    rating / ratingRequiredPerPercent(level)
```

The exact rating curve remains data/balance.

---

## 39. Current Combat-Rating Formula

The current implementation uses:

```text
ratingPerPercent =
    baseRatingPerPercent
  × (level / 60)^1.5
```

with a minimum rating requirement.

This is a useful prototype and preserves the correct design principle.

The exponent `1.5` is **not locked by this PDD**.

The final curve should be calibrated against:

- level-appropriate item budgets;
- level-60 gear progression;
- desired hit/crit/avoidance values;
- low-level usability.

---

## 40. Percentage Stats

Not every percentage must come from a rating.

An authored talent/aura may explicitly grant:

- +5%;
- -10%;
- another fixed percentage;

where the design intends a true percentage effect.

The distinction is:

- **rating-derived percentages** scale according to level;
- **explicit percentage modifiers** mean exactly the authored percentage unless their system says otherwise.

---

## 41. Weapon Skills

Weapon skills are character-specific progression tracks.

Each supported weapon family may have:

- weapon-skill level;
- weapon-skill experience.

Weapon-skill level cannot exceed the character's current level.

A newly learned/untrained weapon skill may begin at level 1 unless the class/race/system grants a higher starting proficiency.

---

## 42. Weapon-Skill Progression Pace

Weapon skills should increase materially faster than full character levelling.

Their purpose is to make proficiency and weapon familiarity matter, not to require replaying the entire 1–60 levelling journey for each weapon.

The implementation should support catch-up behaviour when:

```text
weapon skill << character level
```

Exact:

- skill XP requirements;
- gain frequency;
- catch-up multiplier;
- time-to-cap target;

remain tuning decisions.

Weapon-skill gain must come from legitimate use and should not be optimised around meaningless exploit loops.

---

## 43. Weapon-Skill Cap Increase

When the character gains a level:

- the maximum attainable weapon-skill level increases to the new character level;
- existing weapon skills do not automatically jump to the new cap unless another rule explicitly grants that progression.

This preserves the distinction between character level and actual weapon proficiency.

---

## 44. Talent-Point Acquisition

Talent points begin at:

**Level 10**

The character gains:

**1 talent point per character level from level 10 through level 60**

Therefore:

```text
Maximum Talent Points =
    max(level - 9, 0)
```

At level 60:

**51 total talent points**

This PDD owns the entitlement cadence.

The Abilities and Talents PDD owns:

- talent trees;
- prerequisites;
- rank costs;
- respec;
- specialisation rules;
- how the 51 points may be allocated.

---

## 45. NPC Level Progression

Where an NPC or reusable unit definition uses level-scaled stats/resources, the same canonical progression semantic applies:

```text
resolved =
    initialValue
  + ((level - 1) × perLevelValue)
```

Challenge-level and encounter scaling apply after or alongside this authored baseline according to the owning NPC/encounter systems.

The stat system must not use one meaning for `perLevel` on players and another on NPCs.

---

## 46. Class Balance Targets

Class progression should be balanced using explicit level checkpoints rather than judging only per-level coefficients.

At minimum, balance review should inspect:

- level 1;
- early-game checkpoint;
- mid-game checkpoint;
- late-game checkpoint;
- level 60.

For each class, inspect at least:

- primary stats;
- intrinsic Health;
- intrinsic Mana/primary resource where applicable;
- offensive derived stats;
- relevant defensive/sustain stats.

Exact class endpoint tables remain a balance artefact and are not locked in this PDD.

---

## 47. No Post-60 Character Levels

At level 60:

- normal level XP progression stops;
- XP does not create level 61;
- XP does not automatically convert into permanent combat stats;
- XP does not automatically create paragon/prestige levels.

The level bar may cease to be a primary progression surface at cap.

The game should not create a hidden level system that functionally defeats the permanent level-60 decision.

---

## 48. Maximum-Level Progression Philosophy

A veteran level-60 character should feel more established than a fresh level-60 character without requiring endless character levels.

Progression may come from:

- equipment;
- raid/dungeon progression;
- profession progress;
- reputation;
- recipe mastery;
- transport/access unlocks;
- market/service access;
- world shortcuts;
- storage/organisation conveniences;
- collection/cosmetic accomplishments;
- other character mastery.

This progression may include vertical equipment power, but non-equipment progression should favour access, mastery and convenience over endless raw stat inflation.

---

## 49. Convenience and World-Mastery Unlocks

Maximum-level content may reward RuneScape-like long-term convenience.

Examples of appropriate reward categories include:

- additional transport routes;
- improved lodestone-related convenience;
- access to gates, tunnels or shortcuts;
- specialist bank/storage access;
- profession infrastructure;
- improved work-order/service access;
- faction facilities;
- specialist markets;
- dungeon/raid staging convenience;
- additional safe travel/service locations;
- saved configuration/loadout conveniences;
- cosmetic/prestige rewards.

These are examples, not a locked universal checklist.

The key rule is:

> **Long-term mastery should make an established character easier and richer to operate in the world without becoming an uncapped combat-stat treadmill.**

---

## 50. Legacy Progression

Ninth Age supports a separate account-wide **Legacy** progression concept.

Legacy exists primarily to:

- encourage alternate characters;
- reward breadth of play across the account;
- recognise milestones already achieved elsewhere on the account;
- provide small convenience/catch-up benefits.

Potential Legacy milestones may include:

- reaching character-level milestones with different classes;
- reaching level 60;
- profession accomplishments;
- exploration;
- dungeon/raid achievements;
- faction/reputation accomplishments;
- other broad account achievements.

Exact earning rules remain for later detailed design.

---

## 51. Legacy Reward Constraints

Legacy rewards should primarily be:

- convenience;
- catch-up;
- modest efficiency;
- cosmetic/account recognition;
- alternate-character support.

Examples may include:

- improved rested-XP handling;
- faster weapon-skill catch-up;
- modest profession catch-up;
- reduced selected service/travel costs;
- improved access to already-established account conveniences;
- cosmetic rewards.

Legacy should **not** normally provide large permanent combat advantages such as:

- large universal damage multipliers;
- large universal Health multipliers;
- extra talent points beyond the level entitlement;
- uncapped primary-stat increases.

The account's age should not become a prerequisite for competitive combat power.

---

## 52. Legacy Allocation Model

The exact Legacy progression structure is intentionally not locked yet.

Possible later designs include:

- globally active account perks;
- account-wide earned points with per-character allocation;
- unlockable Legacy trees;
- milestone-driven permanent conveniences.

Whichever model is chosen must preserve the constraints in this PDD.

---

## 53. Character Versus Account Ownership

The following are character-owned unless another PDD explicitly changes them:

- level;
- current XP;
- class/race stat progression;
- weapon skills;
- talent allocation;
- equipment;
- character-specific world/faction progress.

Legacy progress is account-owned.

A Legacy benefit may influence a character, but its unlock state belongs to the account.

---

## 54. Server Authority

The server is authoritative for:

- character level;
- XP;
- level-up eligibility;
- race/class progression inputs;
- resolved combat stats used for gameplay;
- resource maxima;
- current resources;
- rating conversions used for outcomes;
- weapon-skill level/XP;
- talent-point entitlement;
- Legacy unlock state where gameplay relevant.

The client may display/predict values but cannot authoritatively award progression.

---

## 55. Persistence

Persistent character progression includes at least:

- level;
- XP;
- weapon-skill levels;
- weapon-skill XP;
- talent allocation;
- persistent character-owned progression inputs;
- current resources where the persistence system intentionally saves them.

Persistent account progression includes:

- Legacy unlock/progress state.

Derived effective stats should normally be recalculated from authoritative definitions and persistent inputs rather than stored as independent truth.

---

## 56. Stat Recalculation

Stats must be recalculated when an input changes, including as appropriate:

- level;
- race/class context;
- equipment;
- talent/trait state;
- aura/effect;
- persistent stat bonus;
- relevant rule/content version.

Recalculation must produce the same result regardless of which valid event triggered it.

The system should not accumulate duplicate modifiers because a recalculation happened multiple times.

---

## 57. Current Ninth Age Foundations to Retain

The current repository already contains useful foundations.

### 57.1 ExperienceCalculator

Current behaviour already supports:

- level 1 minimum;
- level 60 maximum;
- per-level XP thresholds;
- carry-over XP;
- no further XP requirement at cap.

The exact curve remains tuneable.

### 57.2 ClassDefinition

Current ClassDefinition already contains:

- base stats;
- per-level stat growth;
- class spell level requirements;
- class metadata.

This should evolve toward the canonical progression schema rather than be discarded.

### 57.3 RaceDefinition

Current RaceDefinition already contains race base stats.

This should evolve to support explicit race progression entries where required.

### 57.4 ActorStatDefinition

Current ActorStatDefinition supports:

- fixed stats;
- derived stats;
- ratings;
- regeneration;
- replication mode.

These are useful foundations.

### 57.5 CombatRatingHelper

Current rating conversion already uses:

- actor level;
- permanent level-60 reference;
- data-provided rating-per-percent input.

The exact curve remains balanceable.

### 57.6 Weapon skills

Current PlayerExperience/CharacterWeaponSkillService already store:

- weapon-skill level;
- weapon-skill XP;
- character-level cap.

The gain curve requires later tuning.

### 57.7 Talent-point entitlement

Current PlayerTalents already uses:

```text
max(level - 9, 0)
```

which matches the intended 1-point-per-level cadence from 10–60.

---

## 58. Required Implementation Changes

### 58.1 Fix level-growth semantics

Current ServerStatManager applies:

```text
growth × level
```

for class progression.

It must use the canonical level-1 semantic:

```text
initial + ((level - 1) × perLevel)
```

or an equivalent decomposition that produces exactly the same result.

### 58.2 Explicit progression entries

Race/class progression should use an explicit model equivalent to RPE2:

```text
StatProgression:
    stat
    initialValue
    perLevelValue

ResourceProgression:
    resource
    initialValue
    perLevelValue
```

The exact C# type names may differ.

### 58.3 Separate stat/resource progression

Do not rely on treating every resource exactly like an ordinary primary/secondary stat.

Health/Mana progression needs explicit intrinsic resource semantics.

### 58.4 Multiple derived sources

ActorStatDefinition should be upgraded from one `SourceStat + Multiplier` to a list of source/coefficient relationships or an equivalent general representation.

### 58.5 Intrinsic resource maximum

The runtime stat/resource representation must expose enough state to calculate:

- intrinsic/base maximum;
- effective maximum;
- current value.

### 58.6 Rating curve configuration

The level-relative rating curve should be configurable/testable rather than permanently hidden behind fixed constants.

### 58.7 Weapon-skill curve separation

Weapon-skill progression should not be forced to reuse the full character XP curve if tuning shows that it produces excessive retraining time.

### 58.8 Legacy persistence foundation

The Account/Persistence system will need an account-owned Legacy state separate from character progression state.

---

## 59. Content Authoring Requirements

Designers need data-driven control over:

- stat definitions;
- derived-source coefficients;
- resource definitions;
- resource derived-source coefficients;
- race stat progression;
- race resource progression;
- class stat progression;
- class resource progression;
- XP thresholds/curve;
- rating conversion inputs;
- level requirements;
- weapon-skill progression tuning;
- Legacy milestones/perks once designed.

Authoring tools should display resolved checkpoint values, especially level 1 and level 60.

---

## 60. UX Requirements

The player-facing character/progression UI should clearly communicate:

- character level;
- XP progress before level 60;
- level cap at 60;
- primary stats;
- important derived stats;
- effective resource maxima;
- weapon-skill progress where relevant;
- available talent points;
- major sources of stat changes where practical.

At level 60, the UI should not imply that level 61 is pending.

Legacy should be presented as account-wide progression, not ordinary character XP.

---

## 61. Intentionally Deferred Balance Decisions

The following remain deliberately tuneable:

- exact XP required per level;
- target first-character 1–60 playtime;
- exact rested-XP rules;
- exact race starting values;
- exact class level-1 values;
- exact class level-60 targets;
- exact per-level class coefficients;
- exact Health-per-Stamina coefficient;
- exact Mana-per-Intelligence coefficient;
- exact Strength/Dexterity offensive coefficients;
- exact Willpower-derived formulas;
- exact regeneration formulas;
- exact rating conversion curve/exponent;
- exact level-60 rating budgets;
- exact weapon-skill XP curve and catch-up rate;
- exact intrinsic-vs-equipment power split;
- exact Legacy milestone catalogue;
- exact Legacy perk catalogue;
- exact Legacy point/allocation structure;
- exact maximum-level convenience unlock catalogue.

These values do not block the architecture defined by this document.

---

## 62. Locked Design Decisions

The following are locked by this PDD:

1. Characters begin at level 1.
2. Level 60 is the permanent character-level cap.
3. Future content is not expected to raise the level cap.
4. Normal XP progression ends at level 60.
5. Post-60 XP does not automatically create permanent combat-stat levels.
6. The RPE2 stat/resource progression architecture is the default reference for Ninth Age.
7. Ninth Age uses its own balance targets rather than copying RPE2's WoW-like coefficients.
8. Ordinary linear progression uses `initialValue + ((level - 1) × perLevelValue)`.
9. `initialValue` is the level-1 contribution.
10. Per-level values may be fractional.
11. Race and class progression are separate.
12. Stat and resource progression are conceptually separate.
13. Race primarily establishes starting identity; class is the main long-term authored progression source.
14. Primary stats are Strength, Dexterity, Stamina, Intelligence and Willpower.
15. Stamina's core identity includes maximum Health.
16. Intelligence's core identity includes maximum Mana and offensive/technical magic.
17. Willpower has a distinct support/sustain/healing identity rather than duplicating Intelligence.
18. Derived stats are data-driven.
19. Derived stats must support multiple source stats.
20. Flat contributions normally resolve before percentage modifiers.
21. Health and Mana use hybrid direct-resource + primary-stat-derived progression.
22. Intrinsic/base resource maximum is distinct from effective maximum.
23. Special class resources do not automatically scale like Health/Mana.
24. Equipment contributes substantially to endgame power but does not replace intrinsic level/class progression.
25. Combat ratings are distinct from final percentage outcomes.
26. Rating conversion is level-relative.
27. Level 60 is the permanent rating reference level.
28. The current `1.5` rating exponent is provisional rather than locked.
29. Weapon skills are character-specific progression tracks.
30. Weapon-skill level cannot exceed character level.
31. Weapon skills should progress faster than full character levelling.
32. Weapon-skill catch-up must be supportable.
33. Talent points begin at level 10.
34. Characters gain one talent point per level from 10 through 60.
35. A level-60 character therefore has 51 talent points before any future explicit system changes.
36. Level-scaled NPC baselines use the same level-1/per-level semantic.
37. Level 60 progression continues through equipment, content, professions, reputation, access, mastery and convenience rather than new character levels.
38. Maximum-level convenience/world-mastery unlocks are an intended progression direction.
39. Legacy is a separate account-wide progression concept.
40. Legacy exists primarily to reward account breadth and encourage alternate characters.
41. Legacy should focus on small bonuses, convenience, catch-up and account recognition.
42. Legacy should not become an uncapped mandatory combat-power system.
43. Level, XP, weapon skill and talent allocation are character-owned.
44. Legacy progress is account-owned.
45. Gameplay-relevant progression is server-authoritative.
46. Derived effective stats should normally be recalculated rather than persisted as independent truth.

---

## 63. Dependencies

This PDD directly depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [Class Design PDD](Class-Design-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- [Crafting System PDD](Crafting-System-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Movement and Traversal PDD](Movement-and-Traversal-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md).

---

## 64. Validation Criteria

The Character Stats and Progression system satisfies this PDD when:

1. New characters begin at level 1.
2. No ordinary progression can raise a character above level 60.
3. A level-60 character no longer accumulates ordinary character levels.
4. Experience carries correctly across one or multiple level-ups.
5. Level 60 cannot roll into level 61.
6. A progression entry with initial 100/per-level 10 resolves to 100 at level 1.
7. The same entry resolves to 110 at level 2.
8. The same entry resolves to 690 at level 60.
9. Fractional per-level progression resolves without premature integer truncation.
10. Race and class contributions can be inspected separately.
11. A race may provide a level-1-only contribution using per-level 0.
12. Class progression can define distinct level-60 stat profiles.
13. Player progression and level-scaled NPC progression use the same level-1/per-level semantic.
14. Derived stats can depend on more than one source stat.
15. Derived-stat cycles are rejected or surfaced rather than silently producing invalid values.
16. Equipment flat stats participate in deterministic stat recalculation.
17. Temporary flat and percentage modifiers resolve in the documented order.
18. Health can include both direct class/race resource progression and Stamina contribution.
19. Mana can include both direct class/race resource progression and Intelligence contribution.
20. The system can expose intrinsic/base maximum Health/Mana separately from effective maximum.
21. An effect based on base Mana does not accidentally scale from temporary +Mana where it should not.
22. Special fixed-cap resources can remain independent of primary-stat level scaling.
23. Rating conversion changes with character level according to the configured curve.
24. Level 60 uses the permanent rating reference.
25. The rating curve can be tuned without rewriting combat-resolution code.
26. Weapon-skill level cannot exceed character level.
27. A newly increased character level raises the weapon-skill cap without automatically granting the skill level.
28. Weapon-skill progression can use a dedicated/catch-up tuning path rather than being inseparably tied to character XP.
29. Level 9 grants no talent points.
30. Level 10 grants one total talent point.
31. Level 60 grants 51 total talent points.
32. Server state, not client requests, determines awarded XP and level changes.
33. Recalculating the same unchanged stat state multiple times produces the same result.
34. Recalculation does not duplicate equipment/effect modifiers.
35. A level-60 character can continue progressing through non-level systems.
36. Legacy progress persists at account scope separately from character level/XP.
37. Legacy benefits cannot raise the character above level 60.
38. The system can support Legacy conveniences without requiring permanent universal combat-stat inflation.
39. Authoring tools can display level-1 and level-60 outcomes for progression entries.
40. Current class progression no longer applies one extra per-level increment at level 1.
