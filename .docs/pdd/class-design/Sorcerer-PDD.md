# Ninth Age — Sorcerer Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Sorcerer class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

An instinctive wielder of raw, unstable magic. The Sorcerer gains power by intentionally manipulating magic near the world's field limits, not by memorising safer tricks or borrowing bargains from patrons.

**Design boundary:** Sorcerer Instability is the **class-specific gameplay consequence** of pushing magical field limits. Ordinary spellcasters do not share a general environmental backlash meter. Unlike Occultists, Sorcerers themselves drive volatility.

## 3. Class mechanic and expected combat loop

**Instability** is central and distinct from conventional mana. The player should decide when to accumulate, maintain, discharge or exploit instability for stronger results. The risk has to be legible and largely controllable; randomness must not dominate player agency.

Candidate moment-to-moment loop:

1. Cast approachable direct spells while watching an increasingly relevant Instability state.
2. Choose safer, lower-output magic or a stronger spell that deliberately pushes Instability.
3. Use a relief, conversion, protection or controlled release tool to manage high-Instability windows.
4. Exploit specialist abilities that alter how instability builds, converts, spreads or marks targets.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Reliable direct magic damage, periodic damage and an accessible area option.
- A cast interrupt/spell lockout, magical vulnerability manipulation or enemy buff removal.
- A defensive magical barrier to mitigate punishment from deliberately risky casting.
- At least one practical slow or control option without owning all CC types.
- A basic feedback/action to make Instability management a decision, not a passive bar.
- Group utility that is not an irreplaceable raid magic amplification buff.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Introduce a stable cast and a risky/high-output alternative with clear consequences, plus one risk-management response. The player must understand Instability before receiving complex explosions or delayed effects.

**Mid levels (from level 10 onward):** Choose Cataclyst, Fluxweaver or Voidbrand at level 10; increasingly differentiate burst, spell-state manipulation and mark/cascade interaction.

**Late levels:** Advanced spells may push limits further, with powerful but manageable volatility. Keep the known prohibition on world-scale High Magic as lore; do not turn regular gameplay into frequent catastrophic map damage.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Cataclyst | Burst caster DPS | Volatile destruction and explosive discharge | Exploits deliberate high-Instability windows; should retain counterplay and control. |
| Fluxweaver | Caster/control hybrid | Alter, delay and redistribute magical states | Changes timing, spell outcomes or buffs/debuffs; define exact manipulated objects. |
| Voidbrand | Caster DPS/debuffs | Marks, brands and detonations | Manages accumulation/detonation without becoming an Occultist pact or creature-summoning analogue. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Sorcerer column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Slow, Interrupt casting, Silence / spell lockout, Increase magical vulnerability, Drain / disrupt resources |
| **Specialisation/talent** | Stun, Root, Incapacitate / disorient, Knockback / forced movement |
| **Undecided** | — |
| **Not proposed** | Fear / forced fleeing, Disarm, Taunt, Reduce enemy damage, Reduce physical defence, Reduce healing received |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Absorption / magical barriers, Remove enemy buffs |
| **Specialisation/talent** | Cleanse harmful effects, Restore ally resources, Offensive group support, Defensive group support, Control resistance / removal |
| **Undecided** | — |
| **Not proposed** | Self-healing, Direct ally healing, Group healing, Healing over time, Protect / intercept for allies, Resurrection, Threat reduction / redirection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Periodic damage, Area damage, Execute / finishing attacks |
| **Specialisation/talent** | Charge / dash / leap, Detect / track hidden targets, Persistent ground effects |
| **Undecided** | — |
| **Not proposed** | Stealth / concealment, Traps, Combat companions / summons, Transformation / shapeshifting, Poison-based combat, Bleeding-focused combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | — |
| **Undecided** | — |
| **Not proposed** | Dual wield, Two-handed melee weapons, Shields, Bows / crossbows, Unarmed combat |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** Core direction: **Chaos**, with **Earth, Air, Water and Fire** as plausible transmuted forms; exact access belongs after spell functions are designed. Light/Shadow/Psychic are not assumed. Magical school never uniquely determines a status effect.

**Equipment direction:** Light-armour caster/focus direction; weapon/spell power rules are provisional.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Sorcerer supplies magical offence, spell disruption, buff removal and selective control. A dungeon must not require a Sorcerer to counter a single magical attack type; other interruptions or counterplay should work.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define Instability numerical scale, growth, consequences and relief.
2. Decide whether high Instability is a stance, surge, or unstable threshold.
3. Set reproducible, fair backlash rules and avoid uncontrollable randomness.
4. Determine exact domain of Fluxweaver's magical manipulation.
5. Resolve Voidbrand overlap with Occultist Shadow/Chaos aesthetics and mechanics.
6. Set baseline mana/resource interaction with Instability.
7. Decide which transmuted elemental schools are class-wide.
8. Ensure High Magic restriction does not become a global environmental penalty for every caster.

## 11. Criteria before ability-list authoring

- Low-level Sorcerer chooses between meaningful safe and risky casts.
- Instability affects ability decisions rather than only visual feedback.
- All specs transform the same risk/reward core without erasing the world's ordinary-magic rules.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Sorcerer to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
