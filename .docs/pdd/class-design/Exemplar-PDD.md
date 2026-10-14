# Ninth Age — Exemplar Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Exemplar class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

An armoured sacred combatant whose conviction is expressed through martial action, protection, sacrifice and judgement. The Exemplar should feel martial first, magical second.

**Design boundary:** Where a Cleric invokes the spiritual directly, an Exemplar attaches conviction to the act of fighting. It should not become a generic spell-heavy Paladin analogue or share the Fighter's entirely mundane martial identity.

## 3. Class mechanic and expected combat loop

**Vows** propose meaningful benefits paired with restrictions; **Consecrated Ground** proposes positional sacred influence. Their dependency is open. The desired loop is choosing a commitment and managing exposure/position, not toggling a free passive stance for an unconditional damage buff.

Candidate moment-to-moment loop:

1. Engage in credible weapon combat using a small set of attacks, one key defensive response and a clear class commitment.
2. Decide whether to establish, maintain or leave a consecrated position when enemy movement changes.
3. React to threat against allies through protection, interception or sacrifice rather than universal healing.
4. Use advanced vow interactions and specialisation tools to express tanking, punishment or sacrifice.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- Weapon-based baseline damage and a controlled area pressure tool.
- Defensive mitigation and a timely ally-protection/interception action.
- An accessible cast interrupt and at least one control-resistance option.
- A baseline offensive and defensive group contribution tied to conviction rather than passive must-have auras.
- Limited self-sustain; healing allies is a deliberate spec/talent direction, not a full-class healer default.
- An introduction to vow choice or consecrated positioning before advanced talent mechanics.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Start with straightforward melee attacks, a usable guard/protection tool and a readable form of conviction. Keep only one vow/ground interaction active in early play.

**Mid levels (from level 10 onward):** At level 10 choose Crusader, Templar or Martyr; develop positional judgement and the relationship between vows and sacred areas. Provide early enough tank control for Crusader when group roles become mandatory.

**Late levels:** Introduce sophisticated vow restrictions, battlefield commitment and group-scale sacrifices; prohibit easy no-cost switching that erases positional or vow trade-offs.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Crusader | Tank | Hold ground, armour and protection | Tank threat, mitigation and taunt where appropriate; exploit sacred positioning without becoming unusable on mobile bosses. |
| Templar | Melee DPS | Aggressive judgement and sacred weapon strikes | Punishment and offensive conviction; expand base melee kit rather than replace it with caster rotation. |
| Martyr | Support / possible healer or off-tank (open) | Sacrifice and taking harm for others | Health/mitigation trade-offs and ally protection; full healer or tank designation must be proven, not assumed. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Exemplar column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | Interrupt casting, Reduce enemy damage |
| **Specialisation/talent** | Stun, Disarm, Knockback / forced movement, Taunt, Reduce physical defence |
| **Undecided** | — |
| **Not proposed** | Root, Slow, Incapacitate / disorient, Fear / forced fleeing, Silence / spell lockout, Increase magical vulnerability, Reduce healing received, Drain / disrupt resources |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Self-healing, Protect / intercept for allies, Offensive group support, Defensive group support, Control resistance / removal |
| **Specialisation/talent** | Direct ally healing, Group healing, Absorption / magical barriers, Cleanse harmful effects, Resurrection, Threat reduction / redirection |
| **Undecided** | — |
| **Not proposed** | Healing over time, Remove enemy buffs, Restore ally resources |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | Area damage, Persistent ground effects |
| **Specialisation/talent** | Execute / finishing attacks, Charge / dash / leap |
| **Undecided** | — |
| **Not proposed** | Periodic damage, Stealth / concealment, Detect / track hidden targets, Traps, Combat companions / summons, Transformation / shapeshifting, Poison-based combat, Bleeding-focused combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | Two-handed melee weapons, Shields |
| **Specialisation/talent** | — |
| **Undecided** | — |
| **Not proposed** | Dual wield, Bows / crossbows, Unarmed combat |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** **Light** is the natural initial magical school associated with sacred action. Physical attacks remain Physical where appropriate; mixed martial/magic effects can be authored. Other schools are not assumed, and school assignment follows mechanics.

**Equipment direction:** Heavy armour, martial weapons and shields are strong candidates. Exact restrictions and whether two-handed or shield-dependent capabilities exist outside one spec remain open.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Exemplar contributes frontline protection and group utility; Crusader must be a viable tank alongside Fighter, Naturalist and Ascetic tanks. Avoid exclusive raid-defining consecration buffs or obligatory sacred auras.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define the binding cost and permitted number of active Vows.
2. Decide whether Consecrated Ground is placed, attached to the Exemplar or both.
3. Specify how moving away from sacred ground affects an encounter.
4. Determine how much magical healing a base Exemplar needs, if any.
5. Resolve Martyr as full healer, support or off-tank and its sacrifice safety limits.
6. Decide what constitutes a reliable Exemplar interrupt versus other frontline control.
7. Determine shield and two-handed availability per spec.
8. Prevent vows from becoming arbitrary flat bonuses with trivial restrictions.

## 11. Criteria before ability-list authoring

- Changing vow or placement creates a genuine choice in an encounter.
- Crusader meets the tank role with mobile-boss counterplay and no mandatory Exemplar-specific content.
- Templar and Martyr reuse shared sacred martial tools while changing their functional use.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Exemplar to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
