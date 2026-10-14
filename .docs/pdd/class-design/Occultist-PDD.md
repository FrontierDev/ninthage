# Ninth Age — Occultist Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Occultist class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A practitioner of outside and forbidden powers whose strength flows through pacts, obligations, binding and unstable relationships with external entities.

**Design boundary:** Sorcerers manipulate primordial magic directly; Occultists borrow or steal power under conditions. Cleric Spirit acts through the soul, not through occult contractual entities.

## 3. Class mechanic and expected combat loop

**Pacts** are the intended central mechanic. A pact should exchange power for obligation, sacrifice, restriction or a consequential trade-off. Patron persistence, changing pacts, debt state and interaction with summoned entities have not been established.

Candidate moment-to-moment loop:

1. Select or maintain a bargain that grants an advantage with a cost the player can understand.
2. Apply focused debilitating or periodic effects and respond to enemy casting/resource use.
3. Pay, defer or violate a limited obligation in combat to access different power windows.
4. Specialise into reliable contracted casting, dangerous breach/exploitation or entity control, without needing all three playstyles at low levels.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Dependable ranged magical offence and some debilitation or periodic pressure.
- A class-specific spell disruption or buff removal tool with meaningful restrictions.
- Resource interference or conditional enemy weakening, unlike the Sorcerer's instability control.
- A survival mechanism sourced from the bargain—mitigation, sacrifice or a conditional self-recovery—not a generic full healing kit.
- An introductory pact trade-off early enough to establish identity.
- Summons are a specialisation candidate, not an automatic pet for every Occultist.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Give ordinary spell offence, a minor bargain with a readable trade-off and one survival option; avoid multiple patron/debt/servant systems before basic combat is learned.

**Mid levels (from level 10 onward):** Choose Pactbound, Oathbreaker or Binder at level 10; only then unfold deeper contractual obligations and specialised casting or entity interactions.

**Late levels:** Mature pacts impose real combat choice and possibly persistent relationship decisions; Oathbreaker should not exploit free consequence-free violations.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Pactbound | Caster DPS/support | Stable, honoured agreements | Turns contract management into dependable, planned spell advantages. |
| Oathbreaker | Risk-heavy caster/debuffer | Breaking contracts, stolen strength and consequences | Trades stable benefits for deliberate temporary gains and penalties. |
| Binder | Summoner/control | Binding, delegation and entities | Controls or summons beings, with practical server/network and encounter limits. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Occultist column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Fear / forced fleeing, Silence / spell lockout, Reduce enemy damage, Increase magical vulnerability, Reduce healing received, Drain / disrupt resources |
| **Specialisation/talent** | Root, Incapacitate / disorient, Interrupt casting, Knockback / forced movement |
| **Undecided** | — |
| **Not proposed** | Stun, Slow, Disarm, Taunt, Reduce physical defence |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Remove enemy buffs |
| **Specialisation/talent** | Self-healing, Absorption / magical barriers, Protect / intercept for allies, Cleanse harmful effects, Restore ally resources, Threat reduction / redirection |
| **Undecided** | — |
| **Not proposed** | Direct ally healing, Group healing, Healing over time, Offensive group support, Defensive group support, Resurrection, Control resistance / removal |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Periodic damage |
| **Specialisation/talent** | Area damage, Execute / finishing attacks, Persistent ground effects, Combat companions / summons, Poison-based combat |
| **Undecided** | — |
| **Not proposed** | Charge / dash / leap, Stealth / concealment, Detect / track hidden targets, Traps, Transformation / shapeshifting, Bleeding-focused combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | — |
| **Undecided** | — |
| **Not proposed** | Dual wield, Two-handed melee weapons, Shields, Bows / crossbows, Unarmed combat |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** **Shadow** and **Chaos** are plausible schools; other schools may be available through a pact but are not implied by the existence of outside patrons. Choose school only after deciding the individual spell function. Shadow magic's social reception belongs to lore, not automatic alignment.

**Equipment direction:** Likely light armour and magical implements; no ordinary martial weapon identity has been established.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Provides debilitation, spell interference and conditional utility; summons may support encounters but must not create encounter mechanics only an Occultist pet can solve.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define pact object: temporary combat vow, persistent patron choice or both.
2. Define obligations and costs without punitive hidden randomness.
3. Decide whether Oathbreaker can literally cancel or violate current pacts.
4. Define sources, rules and purposes for healing through sacrifice, if present.
5. Specify Binder summon count, persistence, limits and death/respawn handling.
6. Ensure boss and dungeon pathing support legitimate summoned creatures.
7. Resolve visual/mechanical overlap with Sorcerer Voidbrand and Cleric Spirit.
8. Define which magic schools a pact can unlock, and whether it alters ability school.

## 11. Criteria before ability-list authoring

- Low-level Occultist sees a consequential exchange rather than a passive damage bonus.
- Pactbound, Oathbreaker and Binder have different answers to the same pact obligation.
- A group gains alternatives through pacts without being forced to include an Occultist.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Occultist to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
