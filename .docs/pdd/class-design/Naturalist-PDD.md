# Ninth Age — Naturalist Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Naturalist class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A channel for growth, decay, weather and natural balance. Naturalists engage with nature as interconnected systems rather than simply casting spells that happen to be green or blue.

**Design boundary:** Ranger uses knowledge, skill and traps; Naturalist works with primal energies, attunements, restorative cycles and transformations. The Naturalist should not be a conventional Shaman or Druid copy.

## 3. Class mechanic and expected combat loop

**Attunement** is proposed as Growth ↔ Decay, with Storm a possible separate transient state. Switching, maintaining or exploiting an attunement should change available tactical options rather than only apply an unconditional bonus.

Candidate moment-to-moment loop:

1. Begin with a direct natural attack and simple renewal/protection tool.
2. Shift or respond to Growth/Decay through using restorative, defensive or destructive abilities.
3. Use position/terrain and persistent natural effects to shape an engagement.
4. Let the active spec specialise in stability, anchored group support or aggressive form shifting.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- A viable offensive action and some periodic natural pressure.
- Baseline self-healing and restorative/cleanse capacity rather than identical Cleric healing.
- Roots/slows and natural control that are tactically useful against normal encounters.
- A defensive or protective utility for oneself and allies.
- A basic area/persistent natural effect that reinforces the environment.
- A readable attunement shift with a real opportunity cost; multiple transformation mechanics not required early.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Teach Growth vs Decay using one action of each tendency, plus an elementary root/defence. Do not introduce all states, persistent totems and transformation simultaneously.

**Mid levels (from level 10 onward):** Select Warden, Totemist or Primalist at 10; Warden must develop reliable tanking as groups require it, Totemist can develop a full healer/support identity, Primalist deepens aggressive attunement.

**Late levels:** Storm interactions, durable deployed anchors and transformations may provide advanced depth only after baseline attunement remains readable.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Warden | Tank/support | Natural resilience and possible defensive transformation | Sustain, threat and mitigation while adapting to encounter movement; transformation remains an open detail. |
| Totemist | Healer/support | Placed ritual anchors, restorative zones | Potential group healing and sustainable encounter utility; totems should not be mandatory dungeon keys. |
| Primalist | Melee or hybrid damage (provisional) | Transformation, instinct and shifting attacks | Can use natural cycles offensively, likely making more frequent attunement changes. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Naturalist column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Root, Slow, Knockback / forced movement |
| **Specialisation/talent** | Interrupt casting, Taunt, Reduce enemy damage, Increase magical vulnerability |
| **Undecided** | — |
| **Not proposed** | Stun, Incapacitate / disorient, Fear / forced fleeing, Silence / spell lockout, Disarm, Reduce physical defence, Reduce healing received, Drain / disrupt resources |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Self-healing, Direct ally healing, Healing over time, Cleanse harmful effects, Defensive group support |
| **Specialisation/talent** | Group healing, Absorption / magical barriers, Protect / intercept for allies, Remove enemy buffs, Restore ally resources, Offensive group support, Resurrection, Control resistance / removal |
| **Undecided** | — |
| **Not proposed** | Threat reduction / redirection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Periodic damage, Area damage, Persistent ground effects |
| **Specialisation/talent** | Charge / dash / leap, Detect / track hidden targets, Combat companions / summons, Transformation / shapeshifting, Poison-based combat |
| **Undecided** | — |
| **Not proposed** | Execute / finishing attacks, Stealth / concealment, Traps, Bleeding-focused combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | Two-handed melee weapons, Shields, Unarmed combat |
| **Undecided** | — |
| **Not proposed** | Dual wield, Bows / crossbows |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** Likely **Earth, Air and Water**. **Fire is not required or presumed**. Other school access is not automatic; the natural cycle and mechanics determine final schools. Physical transformation attacks remain Physical when appropriate.

**Equipment direction:** Hybrid equipment support is open. Warden shield/armour, Primalist two-handed weapons and actual forms must be decided rather than inferred.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Warden is a tank candidate; Totemist a healer candidate; both should be competitive alongside other role providers. Environmental advantages and totems must be useful but not irreplaceable.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define Attunement scale, direction, persistence, transitions and neutral state.
2. Resolve Storm as separate transient state, climax or specialist-only mechanic.
3. Decide whether healing and damage both shift the same shared meter.
4. Determine placement, ownership, vulnerability and lifetime of totems.
5. Define Warden tank mechanics and whether transformation is required.
6. Determine Totemist group healing package and early role viability.
7. Decide companion/pet access versus dedicated summoner identity.
8. Avoid giving Naturalist all elemental schools by thematic default.

## 11. Criteria before ability-list authoring

- Early Naturalist demonstrates a comprehensible two-state natural loop.
- Totemist and Warden meet their designated group roles with mobile encounters.
- Nature's power feels different from Ranger preparation even when both slow or control.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Naturalist to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
