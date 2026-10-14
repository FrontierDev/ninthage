# Ninth Age — Fighter Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Fighter class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

An expert of mundane warfare: weapons, armour, discipline, positioning and awareness. No innate magic is required to justify any Fighter ability.

**Design boundary:** Distinct from Exemplar conviction, Duellist opportunism and Ascetic internal discipline. Fighters control the fight through learned martial proficiency and tactical opportunities.

## 3. Class mechanic and expected combat loop

**Momentum** is the promising central notion: successive competent martial choices put the Fighter in control of an engagement. **Maneuver Points** may be a way of spending Momentum or a second layer; their relationship is unresolved. It must not feel like supernatural mana rebranded.

Candidate moment-to-moment loop:

1. Use basic attacks, guards and positioning to generate or retain Momentum.
2. Spend or exploit a tactical opening on a stronger strike, control, protection or interruption.
3. React to an enemy's movement, attack angle or cast with a practical maneuver.
4. Rebuild control after disruption; adapt to defensive Guardian or offensive Battlemaster/Slayer priorities.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Reliable weapon damage and area pressure through martial techniques.
- Accessible cast interrupt, stun/disarm/forced movement and a method of reducing an enemy's damage.
- A defensive action that uses block/parry/timing windows rather than frame-perfect reactions.
- A charge or controlled engagement maneuver, with positioning relevant to execution.
- An offensive group command or tactical buff that is not a mandatory raid stat.
- Base threat tools appropriate to a martial frontline; dedicated taunt and full tank package can be spec-dependent.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Introduce attacks, a reactive guard, an interrupt and one Momentum response. Do not build a complete combo-currency system before the player learns one meaningful trade-off.

**Mid levels (from level 10 onward):** Choose Guardian, Battlemaster or Slayer at level 10. Expand base maneuvers and weapon interactions; Guardian must establish a group tank kit by expected group levels.

**Late levels:** Complexity emerges through counterplay chains, threat control or repeated aggression. Some early training strikes can be replaced by mature variants.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Guardian | Tank | Shield, interception, threat and hold position | Turns Momentum into protection and control; shield dependence requires a conscious equipment rule. |
| Battlemaster | Melee damage/control hybrid | Disarm, disruption and battlefield movement | Turns openings into tactical displacement and high-value attacks; no assumed two-handed lock yet. |
| Slayer | Melee damage | Escalating aggression, bleed and dual-wield candidate | Turns pressure into offensive chaining; dual wield remains a candidate, not obligatory baseline. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Fighter column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Stun, Interrupt casting, Disarm, Knockback / forced movement, Reduce enemy damage, Reduce physical defence |
| **Specialisation/talent** | Slow, Fear / forced fleeing, Taunt |
| **Undecided** | — |
| **Not proposed** | Root, Incapacitate / disorient, Silence / spell lockout, Increase magical vulnerability, Reduce healing received, Drain / disrupt resources |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Offensive group support, Control resistance / removal |
| **Specialisation/talent** | Self-healing, Absorption / magical barriers, Protect / intercept for allies, Defensive group support, Threat reduction / redirection |
| **Undecided** | — |
| **Not proposed** | Direct ally healing, Group healing, Healing over time, Cleanse harmful effects, Remove enemy buffs, Restore ally resources, Resurrection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Charge / dash / leap |
| **Specialisation/talent** | Periodic damage, Area damage, Execute / finishing attacks, Bleeding-focused combat |
| **Undecided** | — |
| **Not proposed** | Stealth / concealment, Detect / track hidden targets, Traps, Persistent ground effects, Combat companions / summons, Transformation / shapeshifting, Poison-based combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | Two-handed melee weapons, Shields |
| **Specialisation/talent** | Dual wield, Bows / crossbows, Unarmed combat |
| **Undecided** | — |
| **Not proposed** | — |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** **Physical** by default. Magical schools must not be silently assigned to ordinary maneuvers to make the class more interesting.

**Equipment direction:** Broad martial weapon proficiency; two-handed and shield fighting are clear directions. Dual wielding is a candidate spec/talent distinction. Decide whether Guardian strictly requires a shield.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Guardian should reliably tank without being the only tank; Fighter offers interrupts, control and tactical support. Encounters cannot presume a Fighter-only disarm, rally or threat reset.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Are Momentum and Maneuver Points one resource or distinct systems?
2. Define generation, decay, cap and recovery after defensive failure.
3. Establish whether strong control is core or exclusive to Battlemaster.
4. Determine if Slayer is dual-wield-only or more flexible.
5. Determine exact shield and weapon restrictions for Guardian.
6. Balance class mobility against Duellist and Ascetic.
7. Design tactical support without creating mandatory group attack bonuses.
8. Specify which early attacks should be replaced versus ranked.

## 11. Criteria before ability-list authoring

- At low levels the Fighter already makes tactical choices around attacks and responses.
- Guardian can hold threat and survive with reasonable encounter positioning.
- Battlemaster, Guardian and Slayer share understandable Momentum but produce different tactical rhythms.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Fighter to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
