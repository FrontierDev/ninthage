# Ninth Age — Cleric Class Design PDD

**Status:** Active class design baseline — named level 1–10 spells and specialisation milestones agreed; mechanics and values not yet fully specified or implemented  
**Project:** Ninth Age  
**Last updated:** 2026-10-17  
**Scope:** Cleric class fantasy, Interventions, Sermons, agreed level 1–10 spell roster, specialisation healing identities and milestone talents

## 1. Authority and design status

This document is subordinate to the [Class Design PDD](../Class-Design-PDD.md) and must be read alongside the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established project direction:** Nine-class roster, class-first ability design, shared early baseline, progressively deeper gameplay, level-10 active specialisation with all three talent trees open, mid-to-late emphasis for major specialisation spells, ability replacement when useful, and avoiding class-mandatory group composition.

**Established in this revision:** The named spell list in Section 10, the Interventions reaction-window concept, the travelling-scholar Sermon teaching model, and the Holy/Spirit/Mind gameplay distinctions. These are approved *design directions*, not evidence of implementation. **Still provisional:** Exact resource costs, cooldowns, ranges, cast/channel times beyond those specified, scaling, stacking, ability targeting limits, talent unlock levels and the `C/T/?/—` capability allocation below. Do not interpret the provisional capability matrix as an approved spell inventory.

## 2. Identity and differentiation

A direct practitioner of divine, soul and mind magic. A Cleric works on lives and consciousness rather than expressing belief through armed combat. Three distinct spiritual approaches must read as one coherent class without turning Holy, Spirit and Mind into unrelated casters.

**Design boundary:** Unlike the Exemplar, the Cleric solves problems through spellcasting, preservation and spiritual intervention. Unlike an Occultist, power does not primarily come from bargaining with outside patrons; unlike a Sorcerer, power does not push primordial field limits.

## 3. Class mechanic and expected combat loop

**Interventions** are the Cleric's signature, class-wide reactive spell mechanic. Certain significant combat events create a **5-second opportunity** to use an eligible Intervention. The player chooses whether and how to respond before the window expires. Interventions should remain useful against ordinary enemies, bosses and players, and avoid creating constant alerts from trivial combat events. Their timing and validation must be server-authoritative with appropriate latency handling.

**Terminology:** Use *Interventions* for this mechanic and its spells; do not introduce a separate category named *Intercessions*. This is a reactive layer alongside ordinary casting, not a dedicated secondary resource or an obligation to wait for triggers.

At level 10, all Clerics learn **Divine Intervention** (Holy category): when an ally falls below 50% health, a 5-second opportunity opens to **manually cast an instant heal** on that ally. No healing is performed automatically. Healing amount, cost, cooldown and precise threshold-crossing/retrigger behaviour remain unspecified.

No Cleric-specific secondary resource has been agreed. Ordinary mana is a working candidate, not an approved final cost model.

Expected combat loop:

1. Use reliable ordinary spells for damage, healing and utility between Intervention opportunities.
2. Notice a relevant ally or enemy event and assess whether spending an Intervention now is worthwhile.
3. Holy prioritises direct and multi-target restoration; Spirit maintains individual targets through healing over time and mitigation; Mind pressures and disrupts enemy actions.
4. Accept trade-offs in casting time, cooldowns, cost and target selection rather than making Interventions universally optimal responses.

**Sermon**, learned at level 6, is a second class-wide distinctive feature. Clerics are travelling religious scholars who learn different Temples' teachings, not devotees required to swear allegiance. Visiting a Temple unlocks its Sermon teaching permanently; the single Sermon spell takes on the effect of the currently selected learned teaching. The Sermon is the Cleric's group-buff ability rather than a separate fixed class group buff. Examples: the **Temple of the Masked Saint** in **Eldanon**, and the **Temple of Tiandre** in the **Suntower**. The exact buffs and restrictions on changing a selected teaching remain open.

## 4. Base-class ability toolkit — mechanical responsibilities

The named **level 1–10 shared spell roster** is defined in Section 10. Its Holy, Spirit or Mind associations describe the spell's category/theme and do **not** restrict those shared spells to the active specialisation. Only spells expressly listed as **specialisation-exclusive** are limited to the corresponding specialisation.

All Clerics obtain basic direct Light damage (Wrath), a single-target HoT (Spirit Mend), direct healing (Healing Light), Psychic periodic damage (Dissonance), a Temple-derived group buff (Sermon), break-on-damage incapacitation (Turmoil), and the introductory reactive heal (Divine Intervention).

Future baseline responsibilities that remain candidates rather than authored spells include selected cleansing, resurrection, self-defence, an answer to hostile spellcasting, and sufficient survival tools for solo levelling. A candidate is not permission to add a permanent passive aura or a second fixed group buff alongside Sermon.

Ability coverage is not the same as an exact spell count. Select the smallest effective number of spells meeting solo/group gameplay needs; intentional ability upgrades or replacements are valid.

## 5. Progression and talent development

**Levels 1–9:** Every Cleric has access to the agreed early baseline spells regardless of later specialisation. **Sermon** unlocks at level 6, not level 10. It is learned and equipped as one spell whose group-buff effect reflects a selected previously learned Temple teaching.

**Level 10:** The Cleric chooses an active specialisation. All Clerics also learn **Divine Intervention** (a Holy-category spell with class-wide availability). Separately, each active specialisation receives **one exclusive level-10 spell**: Holy receives Ray of Hope, Spirit receives Bolster Spirit, and Mind receives Psychic Backlash.

**Milestone talents:** Each specialisation has a first active milestone spell and a capstone milestone spell, as listed in Section 10. Their exact unlock levels and intervening talents have not yet been decided.

The level-10 active-specialisation choice does not close the other two talent trees: talent points may be invested across all three. Exclusive specialisation spell access and talent-granted abilities follow the [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md). Exact cooldowns, costs and rank rules are separate balance/implementation decisions.

## 6. Specialisations — established gameplay identities

| Specialisation | Role | Gameplay identity |
|---|---|---|
| **Holy** | Direct/group healer | Strong direct single-target heals, long-cast powerful heals, AoE/party healing, and later channelled healing with cleansing. Holy is the Cleric's strongest group-healing specialisation. |
| **Spirit** | Single-target sustain/mitigation healer | Applies and maintains Spirit Mend on chosen targets individually (one cast = one HoT application). Improves, accelerates and eventually consumes healing-over-time instances for mitigation or shields. Unlike a Naturalist, Spirit does **not** rapidly spread HoTs throughout a group. |
| **Mind** | Offensive caster/control | Psychic damage over time, mental disruption, reactive punishment for hostile actions and resource pressure. It is not merely a Shadow-damage variant of a Sorcerer. |

**Boundary with other healers:** Fluxweaver specialises in shielding, damage reversal and rewinds; Naturalist in rapid group HoT spreading; Exemplar/Martyr in close-range shared-burden healing and supporting a small number of allies (especially tanks). Spirit Font does grant an absorption shield, but **by consuming prepared Spirit Mend instances**, rather than serving as a baseline Fluxweaver-type shield.

A healer's viability cannot depend exclusively on rare Intervention triggers or on another specific class's presence. Further role-completeness and balance testing remains necessary.

## 7. Provisional capability allocation

The following is the Cleric column of the cross-class [capability matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14), transposed here for historical/provisional capability planning. **It predates the agreed named spell list in Section 10 and must not override it** (for example, Spirit Font is a Spirit capstone shield). **Core** means a candidate base-class capability; **specialisation/talent** means potential access through at least one spec/talent; **open** requires a decision; **not proposed** is not a permanent ban.

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

**Schools:** Holy → **Light**, Spirit → **Shadow**, Mind → **Psychic** are established specialisation associations, not exclusive school restrictions: **Amnesia is specifically a Mind spell that deals Shadow damage**. Other spell schools must follow their individually agreed effects. Light/Shadow/Psychic are spiritual magic; Shadow's social stigma is lore, not a class-alignment check. No generic magical instability penalty.

**Equipment direction:** Light-armour caster direction; spell focus/weapon rules and attribute coefficients remain in the equipment/stat PDDs.

School selection follows the full mechanical toolkit, then names, icons, animations, VFX and audio are authored **last**. Do not manufacture a Fire spell to fill a Fire slot, or assign a status effect merely from a damage school. Physical damage is not among the eight magic schools.

## 9. Group play, solo play and capability overlap

Holy and Spirit are both intended to be viable healers, with substantially different healing patterns: Holy emphasises direct party healing and cleansing, while Spirit emphasises single-target HoTs and mitigation. Multiple other classes must also be viable healers. Cleansing and resurrection may be valuable but ordinary encounter success must not require specifically a Cleric.

Design tests must confirm the class can survive and complete reasonable solo content, and that group encounters can be addressed by **multiple distinct class compositions**. Its unique strengths should be attractive, not required. Equivalent capabilities may differ in cooldown, range, reliability and cost.

## 10. Agreed named spell roster and specialisation milestones

The tables below record the named spells and effects agreed during Cleric design. **An association with Holy, Spirit or Mind does not, by itself, impose specialisation-only access.** Levels refer to character level. Numerical balance not explicitly specified here is intentionally left unresolved.

### 10.1 Shared spells (levels 1–10)

| Level | Spell | Category | Agreed effect |
|---|---|---|---|
| **1** | **Wrath** | Holy | Direct spell dealing **Light damage**. |
| **2** | **Spirit Mend** | Spirit | Applies a **single-target healing-over-time effect**; one button press applies one HoT. |
| **4** | **Healing Light** | Holy | Conventional **direct healing** spell. |
| **4** | **Dissonance** | Mind | **Psychic damage over time**. |
| **6** | **Sermon** | Class-wide | A **group buff** determined by the selected, previously learned **Temple teaching**. No fixed separate Cleric group-buff spell. |
| **8** | **Turmoil** | Mind | **Incapacitates** the enemy; the incapacitation **breaks when the target takes damage**. |
| **10** | **Divine Intervention** | Holy (**class-wide availability**) | When an ally **falls below 50% health**, opens a **5-second Intervention window** allowing the Cleric to **manually cast an instant heal** on that ally. It does not automatically heal and does not rewind damage. |

All seven shared abilities are available regardless of the Cleric's active specialisation.

### 10.2 Level 10 — exclusive specialisation spells

These three spells are **in addition to** Divine Intervention. The Cleric learns the spell for their selected active specialisation, not all three exclusive spells.

| Specialisation | Spell | Agreed effect |
|---|---|---|
| **Holy** | **Ray of Hope** | Powerful **single-target direct heal** with a **long cast time** (Greater-Heal-style role). |
| **Spirit** | **Bolster Spirit** | Temporarily **mitigates incoming damage** to one ally and **increases the effectiveness of Spirit Mend** on that ally. |
| **Mind** | **Psychic Backlash** | Applies an offensive mental effect that **punishes the enemy for performing offensive actions**, inflicting **Psychic damage**. |

### 10.3 Specialisation milestone talent spells

Milestone positions are relative (**first milestone** and **capstone**) rather than final level assignments.

| Specialisation | First milestone spell | Capstone milestone spell |
|---|---|---|
| **Holy** | **Radiant Light** — direct **AoE/group heal**, establishing Holy's strength in healing a party. | **Purification** — **channelled healing** which also **cleanses harmful effects**. |
| **Spirit** | **Weave Soul** — **channelled** spell causing the target's **Spirit Mend to tick faster**. | **Spirit Font** — **consumes up to three instances of Spirit Mend** to provide an **absorption shield** to its target. |
| **Mind** | **Shatter** — **stuns** the target; the Cleric's only planned **stun**. | **Amnesia** — **channelled Shadow damage** which makes the affected enemy's **spell costs significantly higher** while channelled. |

**Control distinction:** Turmoil is a break-on-damage incapacitate, while Shatter is a stun. Shatter is thus the only planned Cleric *stun*, not the only spell with any crowd-control effect. The icon discussion also referred to Shatter as "Mind Shatter"; the milestone name remains **Shatter** unless explicitly revised.

**Spirit Mend / Spirit Font interaction:** The design specifies that Spirit Font consumes up to three *instances* of Spirit Mend, but has not yet decided whether they are multiple stacks on one ally or otherwise how instances are acquired and selected. Do not invent the stacking rule.

### 10.4 Sermon teaching model

A Cleric learns a Temple's **Sermon teaching by visiting and studying at the Temple**. This is spiritual scholarship, **not religious allegiance or membership**. Learned Sermons are retained, and the single Sermon ability's group buff changes according to the chosen teaching.

| Learning location | Sermon variant |
|---|---|
| **Temple of the Masked Saint**, **Eldanon** | **Sermon of the Masked Saint** |
| **Temple of Tiandre**, **Suntower** | **Sermon of Tiandre** |

The mechanical buff for each teaching and any restrictions on changing the chosen teaching are not yet defined.

### 10.5 Unspecified spell parameters

Unless fixed above, spell cast/channel durations, HoT tick frequency, damage/healing coefficients, target counts and areas, cooldowns, costs, ranges, mitigation percentages, shield conversion/scaling, cleanse cadence, Intervention trigger edge cases and exact talent milestone levels remain **to be decided**. The spell roster does not establish these as implementation-ready values.

## 11. Further consideration required

1. Confirm Divine Intervention's heal amount, cost/cooldown, and threshold-crossing/retrigger rules; formalise which events create future 5-second Intervention opportunities.
2. Define Sermon effects for individual Temple teachings and where/how an already learned Sermon may be selected; do not require religious allegiance.
3. Decide Spirit Mend stacking and how Spirit Font identifies/consumes up to three instances; tune the shield against Fluxweaver overlap.
4. Define the duration and numerical values for Bolster Spirit, Weave Soul, Psychic Backlash, Turmoil and Amnesia.
5. Establish Radiant Light's group-target count/area, and Purification's healing/cleansing interval, eligibility and channel interruption behaviour.
6. Set the exact first-milestone and capstone talent levels; determine remaining abilities/talents needed for each spec's solo, dungeon and late-game viability.
7. Confirm baseline interrupts, advanced cleansing, resurrection/combat resurrection, defence, resource costs and equipment constraints without adding unapproved named spells.
8. Validate that reactive Mind effects retain useful outcomes against targets immune to hard crowd control, including bosses.

## 12. Criteria for further ability authoring

- A new Cleric can complete ordinary solo encounters using the shared spells without depending on rare Intervention triggers.
- Holy is a functional **direct and group healer**; Spirit is a functional **single-target HoT and mitigation healer**, neither simply reproducing Fluxweaver, Naturalist or Martyr.
- Mind has a viable offensive loop involving Psychic pressure, disruption and meaningful responses against CC-immune enemies.
- **Divine Intervention** offers its manual 5-second emergency reaction, and **Sermon** supports world exploration without religious allegiance requirements.
- Level-10 class-wide access, level-10 exclusive spell allocation and milestone talent spells match Section 10.

Confirm there is no essential group-role gap at each level band, no mandatory Cleric-only boss dependency, and no school/icon/animation decision driving the spell mechanics. Numerical balance and implementation acceptance criteria require a subsequent design pass.
