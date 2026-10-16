# Ninth Age — Sorcerer Class Design PDD

**Status:** Active design baseline — agreed level 1–10 kit and specialisation identity; values not yet fully balanced or implemented  
**Project:** Ninth Age  
**Last updated:** 2026-10-15  
**Scope:** Class-first Sorcerer toolkit, Instability, level-10 playable slice, roles, specialisation talent milestones and unresolved balance

## 1. Authority and implementation status

Read with [Class Design PDD](../Class-Design-PDD.md), [Abilities and Talents PDD](../Abilities-and-Talents-PDD.md), [Combat System PDD](../Combat-System-PDD.md) and [Dungeon and Group Content PDD](../Dungeon-and-Group-Content-PDD.md).

**Established design decisions:** the Sorcerer uses **Mana + Instability** (not a separate Surge resource); all Sorcerers learn the common base-class spells up to level 10, then select one of Cataclyst, Fluxweaver or Voidbrand at level 10 and receive its initial specialisation ability automatically. Each tree may then grant additional talent abilities. Instability is the signature Sorcerer risk/reward mechanic, not a general penalty to other elemental spellcasters.

**Implementation is not equivalent to approval:** `SorcererClassBehaviour` currently has fixed +10 Instability for `instability`-tagged spells, 40%/80% self-damage thresholds, delayed natural decay and an additional `surge` stat. Those are **existing prototype behaviours**, not a prescription to keep the Surge resource. The existing `Class_sorcerer.asset` still names the third specialisation **Riftwalker**, and some existing spells are named differently or have old parameters. Neither code nor assets are modified by this PDD change.

## 2. Class fantasy and combat model

The Sorcerer directly manipulates primordial/elemental magical forces, deliberately challenging the world's natural protective magical field. Ordinary spellcasters do not generally inflict this class-specific instability upon themselves.

**One class-wide resource, three relationships:**

| Specialisation | Combat role | Preferred Instability | Specialisation pressure |
|---|---|---|---|
| **Cataclyst** | Ranged spell DPS | **High** | Benefits from high Instability, and continues to generate more; learns how to survive and exploit its extremes. |
| **Fluxweaver** | Shield/indirect-healing healer | **Moderate** | Generates Instability for healing, spends/reduces it for shielding and eventually reactive health restoration. |
| **Voidbrand** | Melee spell DPS with repeated stealth-state play | **Low** | Melee magic generates Instability, but excessive Instability prevents a return to invisibility. |

These are deliberately distinct roles, but all three retain the base Sorcerer offensive/defensive toolkit. **Fluxweaver is a full healer identity without requiring a conventional reliable direct-heal filler spell.** It must have enough shielding, indirect healing and later recovery tools to meet group-healer responsibilities.

Instability generation, natural decay, current value and self-damage should be predictable, observable and server-authoritative. Numerical caps, safe thresholds, natural decay, mana interplay and backlash tuning are still subject to balancing, except for explicitly agreed spell requirements below.

**Surge is not a second resource.** The earlier resource/stat of that name is deprecated in the intended design; **Surge** is now a Cataclyst talent spell.

## 3. Level 1–10 shared class toolkit

All seven of these are **class-wide spells**, available to Cataclyst, Fluxweaver and Voidbrand. The specialisation association describes the theme, **not** a spell-use restriction. Level-10 spells are learned as shared class abilities; specialisation-only spells are listed separately below. Exact trainer costs, ranks, unconfirmed cooldowns/ranges and effect coefficients are not yet fixed.

| Level | Spell | Theme | Mechanical design / known parameters |
|---|---|---|---|
| **1** | **Chaos Bolt** | Cataclyst | Baseline ranged **Chaos** damage; generates Instability. First resource-building offensive spell. |
| **2** | **Brand** | Voidbrand | **Melee-range Chaos** damage based on weapon damage; reduces target **Attack Power for 6 sec**, generates Instability; **6-sec cooldown**. |
| **4** | **Crystallise** | Fluxweaver | **1-sec cast**, **6-sec cooldown**; costs Mana, reduces Instability and grants a **small absorption shield**. |
| **6** | **Winds of Wisdom** | General class utility | **Party-wide Intelligence buff**; exact magnitude, duration and cost undecided. |
| **8** | **Nullify** | Voidbrand | **Interrupt**, **moderate range**, **15-sec cooldown** (the common interrupt cadence). Precise range and any lockout duration undecided. |
| **10** | **Dimensional Rift** | Cataclyst | Places a dimensional rift; **at most one active rift per Sorcerer**. Existing rift implementation is a starting point; exact placement, duration, replacement and cooldown are subject to review. |
| **10** | **Control Rift: Nova** | Cataclyst | **Destroys the active rift**, dealing **area-of-effect damage** at the rift. Further Control Rift abilities are expected later. |

Level-10 shared kit: **six active combat spells and one party utility**. After the first specialisation-specific spell is granted (below), a level-10 Sorcerer has **seven combat spells and one utility** before spending a talent point. The general level-10 budget is **5–7 combat abilities plus 1–2 utilities**, with *learnable combat/utility passives also consuming the budget*; a class mechanic inherently provided at character creation does not count as a purchased passive. Additional ranks are not distinct abilities.

### 3.1 Placeholder spell descriptions

- **Chaos Bolt:** "Hurl unstable Chaos energy at an enemy, dealing damage and generating Instability."
- **Brand:** "Strike an enemy in melee range with chaotic magic, dealing weapon-scaled Chaos damage and reducing its Attack Power for 6 seconds. Generates Instability."
- **Crystallise:** "Crystallise unstable magic into a protective barrier, reducing your Instability and absorbing a small amount of damage."
- **Winds of Wisdom:** "Call forth a gentle current of magical wind that sharpens the minds of your party, increasing their Intelligence."
- **Nullify:** "Disrupt a target's spellcasting, interrupting its current cast."
- **Dimensional Rift:** "Open a dimensional rift at the chosen location. Only one rift may remain active at a time."
- **Control Rift: Nova:** "Collapse your active dimensional rift, dealing area damage around its location."

Names and thematic schools agreed above are intentionally retained even though mechanical design normally precedes naming/presentation. **Brand deals Chaos damage**, not Water damage. Brand's 6-sec cooldown, Crystallise's 1-sec cast / 6-sec cooldown and Nullify's 15-sec cooldown are explicit requirements.

Crystallise **talents** may reduce its cast time and increase shield absorption; the baseline class spell is not already instant. The numbers for its Instability reduction, absorption and Mana cost remain open.

### 3.2 Initial shared combat loop

Start with Chaos Bolt for ranged pressure and Instability generation; use Brand when close-quarters action and Attack Power suppression are advantageous. Choose when to cast Crystallise to reduce Instability and absorb damage. Use Nullify for enemy casts. At level 10, create a rift and decide when to sacrifice it using Control Rift: Nova.

No level-1–10 ability should be tuned as an opening nuke that removes most of a normal enemy's health; high-powered burst/very long casts belong later in progression. A meaningful early toolkit is preferable to an overloaded one.

## 4. Level 10 — initial specialisation abilities

At **level 10**, a player selects one active specialisation, automatically receiving its initial specialisation spell **before any talent-point investment**. Its gameplay loop should be visible immediately; talent points deepen or transform that loop later.

| Specialisation | Initial spell | Agreed function |
|---|---|---|
| **Cataclyst** | **Molten Barrage** | **Channeled Fire damage**, requires **at least 40 Instability**, generates additional Instability rapidly during the channel, **no cooldown**. A rapid Instability accelerator, not an early giant nuke. |
| **Fluxweaver** | **Locus** | Places a **single-target marker on an ally**; as the Sorcerer actually gains Instability, Locus heals the marked ally **proportionally to Instability generated**. |
| **Voidbrand** | **Phase Shift** | Grants **magical invisibility**, indefinitely sustainable out of combat like stealth; repeated combat use is central to Voidbrand but **requires adequately low Instability** to re-enter invisibility. |

### 4.1 Locus proportional-healing contract

Locus healing is based on the **actual positive change in the caster's Instability**, not the nominal amount assigned to a spell. For example, a spell that would grant 10 Instability while only 3 can fit before the cap generates healing corresponding to 3, not 10. At the Instability cap, it generates no Locus healing until the caster reduces Instability.

Locus is initially **one marked ally**, potentially including the caster; exact target rules, healing coefficient, duration, mana cost, cooldown and handling of multiple concurrent marks remain balance/authoring decisions. Multiple targets may become possible through later Fluxweaver talents, rather than being granted automatically at level 10.

### 4.2 Phase Shift versus Duellist stealth

**Duellist:** stealth establishes an opening advantage; its ordinary rotation continues largely **out of stealth**.

**Voidbrand:** invisibility or stealth-qualifying states recur **throughout combat**. High Instability interferes with re-entering actual invisibility; the re-entry threshold and other stealth-break conditions are not yet numerically set. Out-of-combat duration is **unlimited**, not a timed buff.

## 5. Specialisation milestone philosophy

The level-10 spells are **introductions**, not talent-tree culminations. Do **not** mandate new conceptual milestones every 10 character levels: each tree advances from **introduction → advanced mastery → rotation-transforming culmination**, while ability **ranks** continue to be learned through level 60.

**Cataclyst:** (1) learn to generate Instability quickly; (2) gain power from high Instability; (3) push magical limits and survive, with a fundamentally altered high-risk rotation.

**Fluxweaver:** (1) use generated Instability to heal through Locus; (2) gain sophisticated control over spending/reducing Instability for shielding and ally protection; (3) manipulate time to reverse damage and change the reactive healing cycle.

**Voidbrand:** (1) learn actual magical invisibility and melee Chaos attacks; (2) use extradimensional space for movement and attacks; (3) shift between dimensional/stealth states with far greater flexibility, while keeping Instability relevant.

A **talent-tree capstone must change the combat rotation** rather than merely offer a larger spell number, long cooldown or simple throughput bonus. The base role must be functional before the capstone (particularly Fluxweaver healing). Exact talent tier/depth and which of the abilities below serve as capstones have not yet been formally assigned.

## 6. Proposed talent-granted active spells

These are **talent abilities**, not ordinary class-trainer spells and not automatically awarded at level 10. They may be obtained through investment in their associated tree subject to the Abilities and Talents PDD's rules; an off-spec Sorcerer can potentially invest in other trees.

| Tree | Talent spell | Agreed mechanics | Unresolved details |
|---|---|---|---|
| **Cataclyst** | **Pyroclasm** | High-damage, **long-cast** spell; generates a **large amount of Instability**. | Damage/cast time, cost, precise generation and level/tier. Must not trivialise normal enemy pulls. |
| **Cataclyst** | **Surge** | High damage; requires **at least 80 Instability**, then **sets Instability to 0** and applies **Overload for 5 seconds**. | Damage, cooldown, targeting, talent depth. |
| **Fluxweaver** | **Dissipate** | **Channeled** reduction of Instability with **minor pulse-healing to allies**. | Per-tick drain/healing, radius/target rules, costs and cooldown. |
| **Fluxweaver** | **Rewind** | Restores health corresponding to **damage taken over the preceding 4 seconds**. This is damage restoration, **not full rollback** of statuses, positioning or resources. Uses Instability reduction/consumption as part of the intended healer resource cycle. | Instability cost/consumption formula, target rules, cap and cooldown. |
| **Voidbrand** | **Control Rift: Riftwalk** | **Teleports the Sorcerer to their active Dimensional Rift**; requires an existing rift. | Range, line-of-sight restrictions, cooldown, cost and failure rules. |
| **Voidbrand** | **Superposition** | For **5 seconds**, the Sorcerer **counts as stealthed for stealth-only ability requirements while remaining visible** to enemies and other players. | Cooldown, cost, whether normal stealth-break rules apply to this state. |

### 6.1 Surge / Overload contract

Surge is a **spell**, not a generated, stored or decaying secondary stat. On successful cast at 80+ Instability, it deals substantial damage, **resets Instability to 0** and applies **Overload for 5 seconds**. During Overload:

- The Sorcerer **takes damage over time** from the Overload effect.
- The Sorcerer **cannot generate Instability**; normally generating spells can still be used but generate **zero** Instability during the effect.
- Reducing Instability is **not automatically disabled**.
- Instability-based effects such as Locus must respect **actual Instability gained**; Overload therefore does not generate Locus healing merely because a generating spell was cast.

The intended rhythm is **build high Instability → exploit it → Surge → endure a forced low-Instability Overload phase → rebuild**. This is a rotation change, not simply a finishing attack.

### 6.2 Rewind / damage-history contract

Rewind reverses **damage**, not time as a general state rollback: restore health equal to qualifying damage taken within the prior **4 seconds**, without reversing debuffs, deaths, positioning, casts, threat, consumables or mana usage. Healing is subject to the target's normal maximum health. Rules for lethal damage, overhealing, multiple Rewinds and snapshot/history retention must be specified before implementation.

### 6.3 Superposition / stealth-rule contract

Superposition is **visible quasi-stealth**. During its 5 seconds it meets **stealth-only cast requirements**, but it **does not grant actual invisibility** or automatically prevent targeting. This lets Voidbrand use stealth-dependent attacks as part of prolonged combat without relying only on Phase Shift. Instability still matters: Superposition does not inherently lower Instability or guarantee full invisibility.

### 6.4 Rift controls and setting constraint

Dimensional Rift is a **single shared class anchor** (one active at a time). Control Rift: Nova consumes/destroys it for area damage; Control Rift: Riftwalk instead consumes its position for a Voidbrand teleport (**whether the rift survives Riftwalk is undecided**). Further Control Rift commands may become available later. Riftwalk is **anchored to a previously created rift**, not unrestricted general-purpose teleportation; the world's prohibition on dangerous unrestricted portal/teleportation magic remains part of the setting.

## 7. Role coverage and capability matrix adjustments

Sorcerer class-wide access includes a ranged damaging spell, melee weapon-scaling magical attack, a defensive absorption shield, an interrupt, party Intelligence utility, Instability management and the Rift/Nova pair. Other controls, dispels, and AoE options remain possible later but are **not automatically part of the level-10 kit** merely because the provisional cross-class matrix lists their type.

**Role responsibilities:**

- **Cataclyst:** ranged magical damage with high-Instability windows and the choice to survive dangerous peaks.
- **Fluxweaver:** reliable shielding and proportional Locus healing, later supplemented by Dissipate and Rewind. It is intentionally **not modelled on a standard direct-heal spell rotation**.
- **Voidbrand:** melee spell damage using weapon scaling, with invisibility/quasi-stealth and dimensional movement as recurring rotation components.

**Cross-class balance:** Cleric primarily uses direct healing, Exemplar is a candidate sacrificial healer, Naturalist emphasises healing over time, and Fluxweaver uses shielding/Instability conversion. None should become mandatory for an ordinary group encounter because of unique non-substitutable utility.

**Capability-matrix changes relative to the earlier Sorcerer proposal:** self-healing (through Fluxweaver if self-targeting is permitted), direct ally healing and group healing become **specialisation/talent possibilities**; stealth/invisibility becomes a **Voidbrand specialisation mechanic**. This does not give direct-heal filler spells to the entire Sorcerer class. The central [matrix](../Class-Design-PDD.md#21-preliminary-cross-class-capability-matrix-2026-10-14) remains provisional and must be revised if later spell authoring changes access.

## 8. Magical schools and equipment

**Confirmed:** Chaos Bolt and Brand deal **Chaos** damage. Molten Barrage deals **Fire** damage. The precise magical school/mitigation handling of Crystallise, Winds of Wisdom, Nullify, Dimensional Rift, Control Rift effects, Locus and advanced talent spells needs a separate mechanics-first review.

**Equipment direction:** a Sorcerer may be an ordinary light-armour spellcaster, but **Voidbrand must be able to use a melee weapon** for Brand and future melee spells. Exact weapon choices, class equipment rules and damage coefficients are open.

Magic school assignment follows ability mechanics; specialisation theme is **not** a one-school lock. The names already agreed in discussion are retained; icon/animation/VFX/SFX decisions remain a later presentation stage.

### 8.1 Sorcerer icon art direction — Radiant Slate

**Radiant Slate**, not Arcane Slate, is the project's approved overall interface theme. All Sorcerer ability icons must follow the [Spell Icon Art Direction PDD](../Spell-Icon-Art-Direction-PDD.md) and be reviewed within the actual 40×40 action-bar slot from the [UI and UX PDD](../UI-and-UX-PDD.md).

**Mandatory:** complete **opaque, edge-to-edge painted backgrounds**, no built-in borders/frames, one clear focal subject, strong silhouettes, restrained detail and genuinely varied spell-specific palettes. No transparent cut-outs or default purple/cyan shard vortices. These are compact game icons, **not cinematic illustrations**.

The benchmark consists of **Chaos Bolt** (diagonal projectile), **Crystallise** (ice-themed defensive barrier), **Winds of Wisdom** (calm air/intellect party buff) and **Dimensional Rift** (narrow vertical tear through space). Each must be recognisable independently at 32, 40, 48 and 64 pixels without its label. **Crystallise must read as protection rather than crystal summoning; Nullify as spell interruption rather than a destructive explosion; Dimensional Rift as a planar tear rather than a scenic portal.**

**Voidbrand-specific continuity:** use the accepted **Chaos Bolt** and **Brand** icon images as mandatory painterly anchors. Phase Shift, Control Rift: Riftwalk and Superposition should share their restrained near-black/plum, violet and magenta brushwork and contrast, with bright blue/cyan subordinate. Riftwalk must portray a **compact figure entering an existing narrow rift**, not a detailed hero scene, giant portal or multiple competing neon trails. Each Voidbrand icon must be judged beside both reference icons at 40×40. The previously generated, more elaborate Riftwalk direction is **not approved as a style reference**; see [Spell Icon Art Direction PDD](../Spell-Icon-Art-Direction-PDD.md#32-sorcerer--voidbrand-continuity-rule).

Art briefs and differentiation guidance for all sixteen agreed Sorcerer spells are maintained centrally in the Spell Icon Art Direction PDD. Those art briefs do not fix unapproved school, mana, cooldown or damage values.

## 9. Phase 2 vertical-slice goals and verification

Phase 2's **Core Playable Vertical Slice** should demonstrate the **Sorcerer levels 1–10** and its three distinct level-10 specialisation identities, with no talents allocated:

1. Train/acquire class abilities at the intended levels and verify they persist.
2. Experience meaningful early choices between Instability generation, close-range Brand, shielding, Nullify and Rift/Nova timing.
3. Choose Cataclyst, Fluxweaver or Voidbrand at level 10 and verify **Molten Barrage**, **Locus** or **Phase Shift** is granted as appropriate and replaced on spec change, without incorrectly modifying shared class abilities.
4. Demonstrate Cataclyst's ability to accelerate Instability, Fluxweaver's Locus-based healing and Voidbrand's out-of-combat sustained invisibility and Instability-governed in-combat re-entry.
5. Verify shared rifts enforce **one active at a time**, and Nova requires and destroys the active rift.
6. Verify the authoritative server owns Instability changes, health/shield events, time-window damage history (when Rewind is implemented), cast interrupt validation and visibility/stealth eligibility.
7. Verify level-10 play is engaging **before** advanced talents, and no class-specific group utility becomes mandatory.

Later talent-tree spells are *design targets*, **not Phase 2 prerequisites**. Character spell ranks continue through level 60, independently of when talent mastery culminates.

## 10. Open design decisions

1. Specify exact Instability cap, natural decay, tier thresholds/backlash scaling and spell-specific generation/reduction, plus whether the current prototype's 40/80 self-damage thresholds remain.
2. Specify baseline Mana costs, training costs, spell ranks, range/cooldown where not stated, weapon coefficients, shield amounts and AP-debuff magnitude.
3. Define Fluxweaver Locus timing, proportional healing coefficient, valid targets and multi-target extension, avoiding healing credited for attempted generation while capped or Overloaded.
4. Establish Phase Shift's Instability re-entry threshold and interaction with taking damage, attacks, casting and normal combat stealth rules.
5. Resolve Dimensional Rift replacement rules and whether Riftwalk preserves the anchor; ensure rifts are networked and limited to one per caster.
6. Determine which candidate talent spells are true capstones, their depth requirements and rotation effects without making basic healing or melee DPS incomplete until the capstone.
7. Specify Rewind's damage history, dead-target rules and health-only rollback semantics; do not infer rewind of non-health game state.
8. Design Overload self-damage calculation, type/school, interactions with cooldowns, death and Locus, and no-generation enforcement across all Instability sources.
9. Resolve Class asset discrepancies (**Riftwalker → Voidbrand**, deprecated `surge` stat / old rift ability names) when implementation work is authorised, not in this documentation change.

## 11. Design acceptance criteria

- At levels 1–9 the Sorcerer is already an identifiable, enjoyable class without selecting a specialisation.
- At level 10, each active specialisation has an observable unique gameplay loop **before talent spending**.
- Later talent spells **transform** existing loops: Cataclyst cycle around a forced Overload phase, Fluxweaver reserve/release and damage Rewind, Voidbrand repeated combat stealth-state access and rift-based movement.
- Every required ability is clearly owned by **class trainer**, **automatically granted active specialisation** or **talent node**, and early learnable passives consume the same budget as active spells.
- No permanent Surge resource remains in intended gameplay; no ordinary NPC pull is trivialised by an early long-cast high-damage opening spell.
- All numerical details not explicitly specified remain open rather than being silently invented.
