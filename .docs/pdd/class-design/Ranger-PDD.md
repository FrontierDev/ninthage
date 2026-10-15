# Ninth Age — Ranger Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Ranger class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A martial hunter relying on preparation, tracking, range management, terrain and prey knowledge rather than becoming a nature spellcaster.

**Design boundary:** Ranger uses **knowledge of nature**; Naturalist channels **nature's power**. The Ranger's traps, quarry analysis and pursuit should not feel like a renamed magical totem kit.

## 3. Class mechanic and expected combat loop

**Focus** represents attention/preparation. **Quarry** denotes a real hunting relationship with a target. Quarry cannot remain merely a generic bonus damage debuff; switching target, losing quarry and preparation should affect decisions in an understandable way.

Candidate moment-to-moment loop:

1. Observe or mark prey and position for a favourable attack, including ranges and deadzones where authored.
2. Build or preserve Focus through deliberate shots, movement or choices.
3. Spend Focus on precise attacks, control, protection of escape lanes or finishing prey.
4. Reposition and change quarry when circumstances shift, accepting deliberate costs rather than constant free retargeting.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Reliable ranged weapon basic attack and single-target offence.
- Slows, roots and interruption through martial targeting or tools, without requiring spellcasting.
- Tracking, detection and deliberate target selection as identity-defining utility.
- Traps/area denial or placed devices, designed with boss movement in mind.
- Skirmish movement, escape options and burst windows that reward good positioning.
- Useful group offence through marking/practical coordination, not a mandatory group damage aura.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Start with ranged basics, movement/deadzone awareness and a clear Focus/Quarry introduction. Give a basic slow or tracking decision before complex trap networks.

**Mid levels (from level 10 onward):** Choose Marksman, Trapper or Stalker at level 10; introduce advanced shots, moving-target interaction and trap composition without turning Focus into several disconnected mini-resources.

**Late levels:** Exploit encounter geometry and prey windows. Trapper must be effective in boss encounters, not only against static trash.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Marksman | Ranged DPS | Precision and deliberate aimed shots | Builds decisive quarry exploitation with positioning and range. |
| Trapper | Ranged damage/control | Preparation, zones and traps | Uses deployed objects and planned control; mobility of bosses must not disable most of the spec. |
| Stalker | Mobile damage | Pursuit, ambush and skirmishing | Can explore melee or hybrid weapon options; decide whether it uses stealth or simply concealment. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Ranger column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Root, Slow, Incapacitate / disorient, Interrupt casting, Reduce physical defence |
| **Specialisation/talent** | Stun, Disarm, Reduce healing received |
| **Undecided** | — |
| **Not proposed** | Fear / forced fleeing, Silence / spell lockout, Knockback / forced movement, Taunt, Reduce enemy damage, Increase magical vulnerability, Drain / disrupt resources |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Offensive group support |
| **Specialisation/talent** | Self-healing, Cleanse harmful effects, Threat reduction / redirection, Control resistance / removal |
| **Undecided** | — |
| **Not proposed** | Direct ally healing, Group healing, Healing over time, Absorption / magical barriers, Protect / intercept for allies, Remove enemy buffs, Restore ally resources, Defensive group support, Resurrection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Periodic damage, Area damage, Execute / finishing attacks, Charge / dash / leap, Detect / track hidden targets, Traps, Persistent ground effects |
| **Specialisation/talent** | Stealth / concealment, Poison-based combat, Bleeding-focused combat |
| **Undecided** | Combat companions / summons |
| **Not proposed** | Transformation / shapeshifting |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | Bows / crossbows |
| **Specialisation/talent** | Two-handed melee weapons |
| **Undecided** | Dual wield |
| **Not proposed** | Shields, Unarmed combat |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** Primarily **Physical** martial effects. Nature-related terrain utility should not automatically imply Earth/Air/Water spellcasting; school access remains to be justified, not inherited from Naturalist.

**Equipment direction:** Bows/crossbows foundational. Melee/dual wield alternatives and precise minimum-range rules are open, particularly for Stalker.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Brings scouting, ranged pressure, target control and tactical positioning. Tracking, special traps or any animal companion must be optional advantages, not hard dungeon keys.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define Focus generation, recovery and spending.
2. Define Quarry selection, switching, death triggers and visibility.
3. Decide whether companions exist and which spec owns them.
4. Decide if Stalker uses bows, melee, or hybrid equipment.
5. Give Trapper boss-appropriate tools against mobile or trap-immune enemies.
6. Set minimum range/deadzone exceptions by ability, not blanket bow rules.
7. Distinguish tracking information from combat-relevant mark effects.
8. Determine magical school access, if any, without nature-caster drift.

## 11. Criteria before ability-list authoring

- The Ranger has meaningful ranged preparation and quarry interactions before spec identity is mature.
- Trapper has useful encounter contribution without requiring traps to land on every boss.
- Any Ranger-specific travel/scouting advantage remains nonmandatory for groups.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Ranger to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
