# Ninth Age — Ascetic Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Ascetic class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A combatant of disciplined body, breath, rhythm and self-mastery. Strength comes from internal practice and sequencing rather than heavy weapons, external contracts or divine intervention.

**Design boundary:** Unlike the Fighter's outward tactical control or Duellist's opportunism, the Ascetic's decisions concern internal Flow, repetition, rhythm and redirection. Avoid a conventional Monk clone.

## 3. Class mechanic and expected combat loop

**Flow** is the primary proposal. Sequencing and counters should build or sustain a rhythm that rewards deliberate technique choices, not impose an onerous button pattern. Exact stages/caps/decay are open.

Candidate moment-to-moment loop:

1. Alternate or respond with simple close-range techniques and learn how Flow reacts.
2. Read incoming attacks and choose an appropriate defensive redirection or counter.
3. Convert a maintained sequence into pressure, a short control window or self-protection.
4. As talents mature, extend Flow into Bulwark mitigation, Pugilist strings or Harmonist restoration.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Reliable melee and unarmed combat with at least two deliberately different early techniques.
- An accessible interrupt and short control tools such as stun or forced movement.
- A latency-tolerant reactive defence/counter rather than frame-perfect dodging.
- A charge/dash/reposition action compatible with fast close combat.
- Self-sustain or breathing/recovery with an opportunity cost, not blanket healer equivalence.
- Basic control resistance and Flow maintenance without repetitive A-B-C compulsory rotations.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Teach unarmed basics, a simple Flow response and one defence/counter; unlock technique synergies sequentially so a novice is not punished for missing a complex cadence.

**Mid levels (from level 10 onward):** At level 10 choose Bulwark, Pugilist or Harmonist. Establish defensive role competence for Bulwark; if Harmonist is designated healer, its direct healing tools must arrive by level-appropriate group content.

**Late levels:** Layer conditional Flow conversions, advanced counters and ally-directed rhythm effects. Obsolete basic strikes may be intentionally replaced if interactions stay comprehensible.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Bulwark | Tank | Deflection, redirection and reactive defence | Survives and holds threat without relying on heavy armour or twitch-perfect parries. |
| Pugilist | Melee DPS | Combination pressure and speed | Uses Flow for offensive sequencing and finishing techniques. |
| Harmonist | Support / possible healer hybrid | Balance and restorative Flow | Share rhythm and healing/mitigation; whether it can serve as main healer remains open. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Ascetic column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Stun, Interrupt casting, Knockback / forced movement |
| **Specialisation/talent** | Slow, Incapacitate / disorient, Disarm, Taunt, Reduce physical defence |
| **Undecided** | — |
| **Not proposed** | Root, Fear / forced fleeing, Silence / spell lockout, Reduce enemy damage, Increase magical vulnerability, Reduce healing received, Drain / disrupt resources |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Self-healing, Control resistance / removal |
| **Specialisation/talent** | Direct ally healing, Group healing, Healing over time, Absorption / magical barriers, Protect / intercept for allies, Cleanse harmful effects, Defensive group support, Resurrection |
| **Undecided** | — |
| **Not proposed** | Remove enemy buffs, Restore ally resources, Offensive group support, Threat reduction / redirection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Execute / finishing attacks, Charge / dash / leap |
| **Specialisation/talent** | Area damage, Bleeding-focused combat |
| **Undecided** | — |
| **Not proposed** | Periodic damage, Stealth / concealment, Detect / track hidden targets, Traps, Persistent ground effects, Combat companions / summons, Transformation / shapeshifting, Poison-based combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | Unarmed combat |
| **Specialisation/talent** | Dual wield |
| **Undecided** | Two-handed melee weapons |
| **Not proposed** | Shields, Bows / crossbows |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** Primarily **Physical** body techniques. Psychic or other magic is **not automatically implied** by internal discipline. Any supernatural effects need explicit design and school choice after their mechanics.

**Equipment direction:** Unarmed/lightly armed baseline. Dual wield and two-handed options remain tentative; ensure all race/skeleton animation combinations support required techniques.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Bulwark is a tank candidate with different survival tactics than Fighter/Exemplar/Naturalist. Harmonist may add healing or support but cannot be assumed as an encounter's only acceptable group solution.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define what generates, spends, increases, lowers or interrupts Flow.
2. Decide if alternating techniques is encouraged rather than compulsory.
3. Ensure reactive defence is playable with normal MMORPG latency.
4. Give Bulwark a credible threat and mitigation model without heavy armour.
5. Decide if Harmonist is a real healer or support hybrid.
6. Determine unarmed damage scaling and relationship to weapons.
7. Resolve martial animation compatibility across playable body types.
8. Distinguish Pugilist combo design from Duellist finisher mechanics.

## 11. Criteria before ability-list authoring

- An early Ascetic gets rewarded for a deliberate sequence without severe execution burden.
- Bulwark's protection and threat work in ordinary five-player encounters.
- Harmonist and Pugilist feel different without abandoning the class's Flow language.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Ascetic to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
