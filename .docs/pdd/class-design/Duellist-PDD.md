# Ninth Age — Duellist Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Duellist class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A fast, precise martial opportunist exploiting vulnerability, position, timing, trickery and deception. Fights should reward creating and recognising openings, not merely maintaining damage buffs.

**Design boundary:** Unlike Fighters, Duellists win by exploiting moments instead of controlling broad frontlines. Unlike Rangers, they do not depend on quarry preparation or wilderness mastery.

## 3. Class mechanic and expected combat loop

An **Opportunity** model, perhaps driven by vulnerability, counters and positional advantage, is a candidate class identity. Previous **Energy + Combo Points** are merely a prototype starting point. Keep a single readable early loop and determine if either previous resource survives.

Candidate moment-to-moment loop:

1. Apply a quick attack and manoeuvre into or create a vulnerable position.
2. Read a clear opening caused by target behaviour, successful counter, stealth or disruption.
3. Cash the opening into pressure, finishing damage or control instead of spamming two interchangeable attacks.
4. Use escape, misdirection or threat redirection when cornered; advanced talents vary how openings are generated.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Reliable melee damage and clear single-target burst/finishing opportunities.
- Stealth/concealment in the base class and one mobility action for engaging/disengaging.
- Precision control such as stun, disarm, interrupt, slow or incapacitation, but not every crowd-control type.
- Access to debilitation such as poisons, wounds and healing-reduction where justified by design.
- An evasive or reactive defence rather than strong routine self-healing.
- Threat manipulation or disengagement as a class utility, not a replacement for actual durability.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Give quick strikes, one mobility tool and a simple exploitable opening. Introduce stealth in a controlled, useful context rather than a mandatory pre-combat ritual for every fight.

**Mid levels (from level 10 onward):** Choose the active spec at level 10; introduce counters, specialised vulnerabilities and optional trickery/poison talents. Preserve the base loop under each.

**Late levels:** Deeper counter-strategies and specialised finishers enhance earlier abilities. Obsolete early finisher variants are acceptable if replacement is explicit.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Assassin | Burst melee DPS | Preparation, weakness exploitation and execution | Opens planned damage windows; should not monopolise every stealth tool. |
| Duelist — rename required | Reactive single-target melee | Parry, counter, riposte and pressure | Concept retained provisionally; name conflicts with Duellist class and must be replaced. |
| Third specialisation — TBD | Disruption / utility | Deception, mobility, dirty fighting or thrown techniques | Identity and name open. **Shadowcraft** is reserved for a profession and cannot be used. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Duellist column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Stun, Slow, Incapacitate / disorient, Interrupt casting, Disarm, Reduce physical defence, Reduce healing received |
| **Specialisation/talent** | Silence / spell lockout, Knockback / forced movement, Reduce enemy damage, Drain / disrupt resources |
| **Undecided** | — |
| **Not proposed** | Root, Fear / forced fleeing, Taunt, Increase magical vulnerability |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Threat reduction / redirection |
| **Specialisation/talent** | Remove enemy buffs, Control resistance / removal |
| **Undecided** | — |
| **Not proposed** | Self-healing, Direct ally healing, Group healing, Healing over time, Absorption / magical barriers, Protect / intercept for allies, Cleanse harmful effects, Restore ally resources, Offensive group support, Defensive group support, Resurrection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Periodic damage, Execute / finishing attacks, Charge / dash / leap, Stealth / concealment, Poison-based combat, Bleeding-focused combat |
| **Specialisation/talent** | Area damage, Detect / track hidden targets, Traps |
| **Undecided** | — |
| **Not proposed** | Persistent ground effects, Combat companions / summons, Transformation / shapeshifting |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | Dual wield |
| **Specialisation/talent** | Two-handed melee weapons, Bows / crossbows, Unarmed combat |
| **Undecided** | — |
| **Not proposed** | Shields |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** **Physical** is the default. Poison and bleed are authored mechanics/auras, **not** additional magical schools. Any magical exceptions require explicit fantasy justification.

**Equipment direction:** Dexterity-focused agile melee; dual wield currently a base-class candidate, with thrown weapons/bows or two-hand options potentially spec-dependent. Weapon types are not locked.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

A Duellist contributes precision disruption, stealth/skips, controlled target removal and threat management. Encounter design must supply alternatives when stealth or disarm is helpful, rather than requiring Duellist admission.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Name and define the second and third specialisations without the class/spec naming collision.
2. Choose between Energy/Combo Points, Opportunity, or a hybrid and define payoffs.
3. Determine whether stealth is essential to all specs or a supporting tool.
4. Decide the exact scope of poisons and whether they are shared or specialist.
5. Define baseline durability without homogenising into self-healing.
6. Establish whether reliable crowd control should use cooldowns or earned openings.
7. Determine weapon choices and any ranged identity.
8. Avoid reproducing a conventional MMORPG Rogue wholesale.

## 11. Criteria before ability-list authoring

- Base Duellist makes tactical opportunity decisions without talent investment.
- All three eventual specialisations reuse opportunities but have distinct routes to exploit them.
- Stealth and control confer tactical advantage without locking parties into Duellist-only solutions.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Duellist to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
