# Ninth Age — Cleric Class Design PDD

**Status:** Draft / Preliminary Toolkit Design — **not an implementation specification**  
**Project:** Ninth Age  
**Last updated:** 2026-10-14  
**Scope:** Cleric class fantasy, baseline combat loop, provisional capabilities, progression, specialisations and outstanding authoring decisions

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Provisional in this document:** Proposed individual ability functions, resource loops, specialisation roles, school/weapon access, and every `C/T/?/—` allocation below. These are design candidates to validate, **not** automatically granted abilities, final equipment constraints or numerical balance. Do not implement or name individual abilities solely from this draft.

## 2. Identity and differentiation

A direct practitioner of divine, soul and mind magic. A Cleric works on lives and consciousness rather than expressing belief through armed combat. Three distinct spiritual approaches must read as one coherent class without turning Holy, Spirit and Mind into unrelated casters.

**Design boundary:** Unlike the Exemplar, the Cleric solves problems through spellcasting, preservation and spiritual intervention. Unlike an Occultist, power does not primarily come from bargaining with outside patrons; unlike a Sorcerer, power does not push primordial field limits.

## 3. Class mechanic and expected combat loop

No class-wide secondary resource is established. The shared gameplay foundation is selecting when to intervene: protect, restore, disrupt or punish. Explore a linked cast/interaction mechanic only if it offers deliberate decisions shared across Holy, Spirit and Mind; ordinary mana remains a candidate, not an approved final resource model.

Candidate moment-to-moment loop:

1. Maintain an accessible direct offensive spell and a modest means of defending or restoring oneself while solo.
2. Read targets' health, harmful auras and casts, and choose between immediate healing, preventative defence and targeted disruption.
3. Trade cast time, resource expenditure, position and opportunity cost instead of chaining multiple equivalent heals.
4. Use specialisation enhancements to make shared spiritual intervention more efficient, aggressive or controllable without losing baseline spells.

This loop must provide meaningful choices without needing every listed step in every fight. Exact resource timing and costs remain open until individual spells are authored.

## 4. Base-class ability toolkit — mechanical responsibilities

All specialisations should retain access to a coherent class foundation. Candidate class-wide ability responsibilities, **not spell names**, are:

- A reliable direct-damage option for solo levelling; an early basic heal that can later be superseded by a more capable heal.
- A basic preventative defensive effect, useful self-healing and cleansing of selected harmful effects.
- A class-distinct supportive aura that does not become a mandatory permanent raid-stat slot.
- An emergency answer to a threatened ally and a manageable way to respond to disruptive enemies.
- Meaningful spell timing and resource trade-offs, not an obligatory long healing rotation.
- Resurrection and basic battlefield information for a healing/support identity; combat resurrection remains open.

Ability coverage is not the same as an exact spell count. The authoring pass should select the smallest effective number of spells that provides real responses to common combat situations; intentional upgrades/replacements are valid.

## 5. Progression and talent development

**Early levels (pre-specialisation):** Introduce direct damage, basic healing, a single preventative protection or cleanse, and a resource choice. Do not withhold all offensive autonomy until a spec is selected. An intentionally simple early heal may be surpassed by a later one.

**Mid levels (from level 10 onward):** At level 10 select an active spec; later extend shared cures, barriers and intervention into distinct Light/Shadow/Psychic applications. A Holy healer must become group-viable at the levels where groups require a dedicated healer.

**Late levels:** Enhance triage, advanced cleansing, anticipation and group-scale responses; avoid making every Cleric spec a complete healer with equivalent throughput.

At level 10 a player chooses an active specialisation, but talent points may be invested across all three trees. Class-wide abilities are learned at trainers, active-specialisation abilities are granted based on authored eligibility, and individual talents can grant or modify abilities as defined in the Abilities and Talents PDD. All three spec trees stay accessible; no default talent-only active loadout cap is assumed.

## 6. Proposed specialisations

| Specialisation | Candidate role | Theme | Mechanical direction |
|---|---|---|---|
| Holy | Healer/support | Light and divine preservation | Reliable direct/group healing, timely cleansing, protection and intervention; healer role toolkit cannot rely on random utility talent selection. |
| Spirit | Support/healing hybrid (provisional) | Shadow and manipulation of souls | Soul interaction, life-force trades or spiritual assistance; must not simply duplicate Holy healing or Occultist curses. |
| Mind | Offensive caster/control | Psychic and consciousness | Mental pressure, casting interference and focused disruption; retains Cleric intervention identity without duplicating Sorcerer volatility. |

These are roles and design themes, **not** fixed eligibility or complete ability inventories. Any role marked tentative needs a tested tank/healer/damage toolkit before being advertised as group-ready.

## 7. Provisional capability allocation

The following is the Cleric column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here to support ability authoring. **Core** means a candidate base-class capability; **specialisation/talent** means access through at least one possible spec or talent (not all specs); **open** requires a decision; **not proposed** is not a permanent ban.

### Crowd control and debuffs

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | Stun, Slow, Incapacitate / disorient, Fear / forced fleeing, Interrupt casting, Silence / spell lockout, Reduce enemy damage, Increase magical vulnerability, Reduce healing received, Drain / disrupt resources |
| **Undecided** | — |
| **Not proposed** | Root, Disarm, Knockback / forced movement, Taunt, Reduce physical defence |

### Healing, defence and group support

| Placement | Capabilities |
|---|---|
| **Core class** | Self-healing, Direct ally healing, Absorption / magical barriers, Cleanse harmful effects, Defensive group support, Resurrection, Control resistance / removal |
| **Specialisation/talent** | Group healing, Healing over time, Protect / intercept for allies, Remove enemy buffs, Restore ally resources, Offensive group support |
| **Undecided** | — |
| **Not proposed** | Threat reduction / redirection |

### Offensive and tactical mechanics

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | Periodic damage, Area damage, Detect / track hidden targets, Persistent ground effects |
| **Undecided** | Combat companions / summons |
| **Not proposed** | Execute / finishing attacks, Charge / dash / leap, Stealth / concealment, Traps, Transformation / shapeshifting, Poison-based combat, Bleeding-focused combat |

### Equipment and fighting styles

| Placement | Capabilities |
|---|---|
| **Core class** | — |
| **Specialisation/talent** | — |
| **Undecided** | Two-handed melee weapons, Shields |
| **Not proposed** | Dual wield, Bows / crossbows, Unarmed combat |

Shared basics such as direct offense, target selection and enough personal survival to solo level are requirements even when the table does not enumerate them. A spec intended to tank or heal must receive a complete working package for that role; no other class's presence is presumed.

## 8. Magical schools and equipment — assign after mechanics

**Schools:** Holy → **Light**, Spirit → **Shadow**, Mind → **Psychic** are established specialisation associations. This is not a guarantee that each spec is locked to exactly one school on every spell. Light/Shadow/Psychic are spiritual magic; Shadow's social stigma is lore, not a class-alignment check. No generic magical instability penalty.

**Equipment direction:** Light-armour caster direction; spell focus/weapon rules and attribute coefficients remain in the equipment/stat PDDs.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Holy is a primary healer candidate; Spirit's healing/support capacity is still unresolved. Multiple other classes must be viable healers. Cleansing and resurrection may be valuable but ordinary encounter success must not require specifically a Cleric.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Further consideration required

1. Define the shared Cleric mechanic, if any, beyond ordinary mana.
2. Decide how Spirit (Shadow/soul) supports or heals without overlapping Holy.
3. Decide which Mind controls are class-wide, spec-specific or talent-gated.
4. Determine whether Cleric has a reliable baseline cast interrupt or gains only a talent option.
5. Assign the point at which group healing and solo defence should mature.
6. Define limits and recovery rules for resurrection and candidate combat resurrection.
7. Set healing cadence and resource costs without mandatory downranking assumptions.
8. Determine whether soul-themed summons are relevant or should be excluded.

## 11. Criteria before ability-list authoring

- A new Cleric handles ordinary solo encounters using multiple decisions, not only a filler cast.
- A mid-level Holy healer can meet encounter healing requirements without a unique Cleric-only boss dependency.
- Holy, Spirit and Mind share an understandable class toolkit while producing distinct decision patterns.

Also confirm there is no essential role gap at each level band, no requirement that a party invite Cleric to overcome ordinary boss mechanics, no accidental duplication with adjacent classes, and no school/icon/animation decision driving the original mechanics. Once these are decided, create a full named spell/talent roster and resolve exact levels, cooldowns, costs and numerical balance in a subsequent class-specific design revision.
