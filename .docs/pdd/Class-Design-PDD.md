# Product Design Document: Playable Class Design

**Project:** Ninth Age  
**Document Type:** Product Design Document  
**Status:** Draft / Design Baseline (capability matrix provisional; Sorcerer specialisation roles agreed)  
**Purpose:** Define the intended playable class roster, class identity, cross-class toolkit philosophy, preliminary capability coverage, and remaining design questions.

---

# 1. Purpose

Ninth Age uses a class-based character system inspired by traditional MMORPG roles while avoiding direct reproduction of existing MMORPG class designs.

Each class should have:

- A clearly identifiable fantasy.
- A distinct source or philosophy of power.
- A recognisable combat role.
- A class-level gameplay mechanic that changes how the player approaches combat.
- Multiple specialisations that substantially alter how the class fulfils its fantasy.
- Sufficient mechanical separation from neighbouring classes.
- A clear reason to exist independently within the class roster.

This document represents the current design baseline.

Anything specifically marked **Further Consideration Required** is not considered final and should not be treated as an implementation requirement until resolved.

---

# 2. Current Class Roster

The current intended base classes are:

1. Cleric
2. Exemplar
3. Fighter
4. Duellist
5. Ranger
6. Sorcerer
7. Occultist
8. Naturalist
9. Ascetic

These supersede the earlier base-class naming set:

- Exemplar
- Sorcerer
- Fighter
- Cleric
- Duelist
- Ritualist
- Warden
- Ranger
- Pugilist

The following broad replacements occurred during design development:

- Ritualist → **Occultist**
- Warden → **Naturalist**
- Pugilist → **Ascetic**

Earlier specialisation names and mechanics should therefore not automatically be considered canonical.

---

# 3. Class Design Principles

## 3.1 Power Source

Classes should differ not merely in weapon choice or combat role, but in the fundamental source and philosophy of their abilities.

The current broad divisions are:

| Class | Primary Conceptual Power Source |
|---|---|
| Cleric | Divine and spiritual power |
| Exemplar | Faith expressed through martial action |
| Fighter | Martial skill and battlefield discipline |
| Duellist | Finesse, opportunity and exploitation |
| Ranger | Hunting, preparation and wilderness mastery |
| Sorcerer | Unstable innate/raw magic |
| Occultist | Forbidden external powers and pacts |
| Naturalist | Natural and primal systems |
| Ascetic | Internal discipline and bodily mastery |

This distinction should remain visible in mechanics, animations, equipment, talent design and narrative presentation.

---

# 4. Cleric

## 4.1 Fantasy

The Cleric is a divine and spiritual spellcaster concerned with life, souls, healing, protection, judgement and the mind.

The Cleric should represent direct interaction with spiritual forces rather than martial expressions of faith.

This distinction separates the Cleric from the Exemplar.

## 4.2 Attribute Direction

Likely primary attributes:

- Intelligence
- Willpower

The exact attribute relationships remain subject to the final character-stat system.

## 4.3 Proposed Specialisations

### Holy

Primary themes:

- Healing
- Cleansing
- Protection
- Preservation
- Divine intervention

Expected role:

- Primarily healer/support.

### Spirit

Primary themes:

- Souls
- Spiritual essence
- Life-force manipulation
- Spiritual assistance
- Hybrid support

Expected role:

- Support/healing hybrid.

### Mind

Primary themes:

- Psychic influence
- Judgement
- Mental pressure
- Control
- Offensive spiritual magic

Expected role:

- Offensive caster/control.

## 4.4 Further Consideration Required

The Cleric currently lacks a sufficiently defined **class-wide mechanical system**.

Questions requiring resolution include:

- Does the Cleric use a conventional mana system alone?
- Does it accumulate or consume a secondary spiritual resource?
- Is there a shared mechanic involving Faith, Grace, Spirit, Favour or another concept?
- Should all three specialisations interact with the same class mechanic?
- How strongly should the Mind specialisation overlap with psychic/occult concepts?
- What exactly differentiates Spirit from Holy mechanically rather than thematically?
- Is Spirit primarily a healer, support class, summoner, or resource-manipulation specialisation?

The distinction between **Holy** and **Spirit** requires particular attention before implementation.

---

# 5. Exemplar

## 5.1 Fantasy

The Exemplar is an armoured sacred warrior.

Where the Cleric invokes spiritual power through spellcasting, the Exemplar expresses belief through action, combat, conviction and sacrifice.

The class should feel martial first and magical second.

## 5.2 Attribute Direction

Likely emphasis:

- Strength
- Willpower
- Stamina

## 5.3 Core Mechanical Concepts

Two major concepts have been established:

### Consecrated Ground

The Exemplar establishes or benefits from areas of sacred influence.

Consecrated Ground may affect:

- The Exemplar.
- Allies.
- Enemies.
- Specific abilities.
- Vow behaviour.

### Vows

The Exemplar may adopt commitments that change the way the character fights.

A Vow should ideally create both:

- A benefit.
- A behavioural restriction or expectation.

Vows should therefore feel more meaningful than conventional passive stances.

## 5.4 Proposed Specialisations

### Crusader

Themes:

- Defence
- Protection
- Armour
- Sacred fortification
- Frontline presence

Expected role:

- Tank.

### Templar

Themes:

- Judgement
- Aggressive melee combat
- Sacred weapons
- Punishment
- Offensive conviction

Expected role:

- Melee damage.

### Martyr

Themes:

- Sacrifice
- Taking harm for others
- Converting health or suffering into power
- Group support

Expected role:

- Hybrid support, potentially healer/off-tank.

## 5.5 Further Consideration Required

The relationship between **Vows** and **Consecrated Ground** requires formalisation.

Questions include:

- Are Vows selected outside combat or switched dynamically?
- Can multiple Vows exist simultaneously?
- Are Vows class-wide or specialisation-specific?
- Is Consecrated Ground generated by abilities or permanently centred around the Exemplar?
- Can multiple areas of Consecrated Ground exist?
- Does movement away from Consecrated Ground intentionally create a positional weakness?
- What is the exact combat role of Martyr?
- How much conventional spellcasting should the class possess?

The class should avoid drifting into a conventional Paladin analogue.

---

# 6. Fighter

## 6.1 Fantasy

The Fighter represents complete mastery of mundane warfare.

The class should have no inherent magical requirement.

Its power comes from:

- Weapons.
- Armour.
- Training.
- Tactical awareness.
- Battlefield positioning.
- Exploitation of opportunities.

## 6.2 Attribute Direction

Likely emphasis:

- Strength
- Stamina

Dexterity may remain relevant depending on weapon configuration.

## 6.3 Core Mechanical Concept

### Momentum

Successful martial actions generate or maintain Momentum.

Momentum represents control of the engagement rather than supernatural energy.

### Maneuver Points

Momentum may enable or generate Maneuver Points that are spent on tactical actions.

Potential sources include:

- Successful attacks.
- Parries.
- Blocks.
- Movement.
- Interruptions.
- Exploiting enemy positioning.

## 6.4 Proposed Specialisations

### Guardian

Themes:

- Shields
- Protection
- Interception
- Damage mitigation
- Holding ground

Expected role:

- Tank.

### Battlemaster

Themes:

- Tactical control
- Positioning
- Heavy weapon techniques
- Disruption
- Battlefield manipulation

Expected role:

- Damage/control hybrid.

### Slayer

Themes:

- Sustained aggression
- Dual wielding
- Bleeding
- Escalating pressure
- Relentless melee attacks

Expected role:

- Melee damage.

## 6.5 Further Consideration Required

The relationship between **Momentum** and **Maneuver Points** is not yet sufficiently defined.

The final design must determine whether they are:

- Separate resources.
- A resource and its spendable representation.
- Two layers of the same mechanic.
- Specialisation-dependent systems.

Additional questions:

- Does Momentum decay naturally?
- Does being hit reduce Momentum?
- Is Battlemaster specifically a two-handed weapon specialisation?
- Is Slayer explicitly dual wield?
- How much weapon freedom should each specialisation retain?
- Can Guardian function without a shield?

The Fighter must remain mechanically interesting without introducing pseudo-magical resource behaviour.

---

# 7. Duellist

## 7.1 Fantasy

The Duellist is a finesse-based martial class centred around:

- Timing.
- Positioning.
- Exploiting vulnerabilities.
- Mobility.
- Precision.
- Deception.
- Underhanded techniques.

The class occupies the agile martial space without simply reproducing the traditional MMORPG Rogue.

## 7.2 Attribute Direction

Primary emphasis:

- Dexterity

Secondary attributes remain unresolved.

## 7.3 Previous Mechanical Foundation

The immediately preceding Rogue design used:

- Energy.
- Combo Points.

This provides a functional starting point but should not automatically become the final Duellist system.

## 7.4 Previous Specialisation Concepts

### Assassin

Themes:

- Preparation
- Weakness exploitation
- Burst damage
- Execution

### Duelist

Themes:

- One-on-one combat
- Counters
- Parries
- Reactive attacks
- Pressure

### Third Specialisation — TBD

The third Duellist specialisation remains unnamed.

Themes:

- Deception
- Disruption
- Theft
- Stealth
- Utility

**Shadowcraft is reserved for the profession and must not be used as a Duellist specialisation name.**

## 7.5 Further Consideration Required

This class currently requires substantial consolidation.

Most importantly, having a **Duellist class with a Duelist specialisation is not acceptable as a final naming structure**.

The specialisation set therefore requires reconsideration.

Questions include:

- Should Energy remain the class resource?
- Should Combo Points remain?
- Can the class be mechanically distinguished more strongly from a traditional Rogue?
- Should stealth be class-wide or concentrated into one specialisation?
- How important are poisons?
- Should counter-attacking/parrying become the class's central mechanic?
- What should replace the Duelist specialisation name?
- What should the unnamed third specialisation be called?
- Should the third specialisation emphasise trickery, mobility, thrown weapons, dirty fighting or another mundane concept?

This is one of the classes requiring the most design work before implementation.

---

# 8. Ranger

## 8.1 Fantasy

The Ranger is a martial wilderness specialist.

Its identity includes:

- Ranged weapons.
- Tracking.
- Hunting.
- Preparation.
- Traps.
- Pursuit.
- Knowledge of creatures and terrain.

The Ranger should remain primarily martial rather than becoming a conventional nature caster.

## 8.2 Attribute Direction

Likely emphasis:

- Dexterity
- Willpower

## 8.3 Core Mechanical Concepts

### Focus

A combat resource representing concentration and deliberate attack preparation.

### Quarry

The Ranger designates or identifies prey and receives specialised interactions against it.

Quarry should create a hunting relationship rather than merely applying a generic damage debuff.

## 8.4 Proposed Specialisations

### Marksman

Themes:

- Precision
- Range
- Aimed attacks
- Weak-point exploitation

Expected role:

- Ranged damage.

### Trapper

Themes:

- Preparation
- Zones
- Traps
- Denial
- Battlefield control

Expected role:

- Damage/control.

### Stalker

Themes:

- Pursuit
- Mobility
- Ambush
- Skirmishing
- Close engagement

Expected role:

- Mobile damage.

## 8.5 Further Consideration Required

Questions requiring resolution:

- Does Ranger have an animal-companion mechanic?
- If companions exist, are they class-wide or specialisation-specific?
- Does Stalker use melee weapons, ranged weapons, or both?
- How is Quarry selected?
- Can Quarry change rapidly during combat?
- Does killing Quarry refund or empower something?
- Is Focus generated actively or regenerated automatically?
- How viable is Trapper when fighting highly mobile targets or bosses?
- How are traps handled in networked combat and enemy AI?

The class must also remain distinct from the Naturalist.

---

# 9. Sorcerer

## 9.1 Identity and class mechanic

Sorcerers manipulate raw, volatile magic, putting strain on the world's protective field. Instability is a **Sorcerer-only** risk/reward mechanic, not a universal penalty for elemental spellcasting. Mana and Instability are the intended resources. **Surge no longer exists as a secondary resource**; it is the name of a later Cataclyst talent spell.

The shared toolkit is taught during levels 1–10, introducing ranged magical offence, a weapon-scaled melee spell, a small barrier that reduces Instability, party utility, a standard interrupt and dimensional rift controls. At level 10 the Sorcerer selects an active specialisation and automatically receives its defining first ability. Full specification and unresolved numerical decisions: [Sorcerer Class PDD](class-design/Sorcerer-PDD.md).

## 9.2 Agreed specialisation roles and Instability relationships

| Specialisation | Role | Instability philosophy | Initial level-10 specialisation spell |
|---|---|---|---|
| **Cataclyst** | **Ranged spell DPS** | Favour **high** Instability; exploit and continually generate it, later surviving extreme output. | **Molten Barrage** — channeled Fire damage; requires 40+ Instability, rapidly generates further Instability, no cooldown. |
| **Fluxweaver** | **Shield/indirect-healing healer** | Aim for **moderate** Instability as a reserve; generated Instability heals through marked allies; shields and later Rewind reduce/spend it. | **Locus** — one allied marker, healing proportionally to actual Instability gained. |
| **Voidbrand** | **Melee spell DPS / recurring stealth** | Prefer **low** Instability; melee attacks generate it and excess blocks re-entry into magical invisibility. | **Phase Shift** — magical invisibility sustained outside combat; combat re-entry depends on Instability. |

Voidbrand stealth is not Duellist stealth: Duellist uses stealth mainly as an opener, whereas Voidbrand repeatedly enters invisibility or stealth-qualified states as part of prolonged combat.

## 9.3 Confirmed level 1–10 shared spells

| Level | Spell | Function |
|---|---|---|
| **1** | **Chaos Bolt** | Ranged Chaos damage, generates Instability (Cataclyst-themed). |
| **2** | **Brand** | Weapon-scaled melee Chaos damage, -Attack Power debuff for 6 sec, generates Instability; 6-sec cooldown (Voidbrand-themed). |
| **4** | **Crystallise** | 1-sec cast, 6-sec cooldown; costs Mana, reduces Instability, small shield (Fluxweaver-themed). Fluxweaver talents can accelerate the cast and improve absorption. |
| **6** | **Enlightenment** | Party Intelligence buff (utility). |
| **8** | **Nullify** | Moderate-range interrupt, 15-sec cooldown, Voidbrand-themed. Fifteen seconds is the standard interrupt cooldown. |
| **10** | **Dimensional Rift** | Creates one active rift maximum for that Sorcerer. |
| **10** | **Control Rift: Nova** | Destroys the active rift to deal area damage. |

These spells belong to the **base class** even when associated with a particular tree. Together with the initial level-10 spec ability, the character has seven combat abilities and one utility (excluding passives/ranks) before talent investment.

## 9.4 Advanced talent spells and culmination

| Tree | Talent spells | Progression of the combat loop |
|---|---|---|
| **Cataclyst** | **Pyroclasm** (long-cast heavy damage, high Instability generation); **Surge** (requires 80+ Instability, powerful damage, resets Instability to zero, grants **5 sec Overload**). | Gain damage from high Instability; finally push limits and survive a **forced low-Instability Overload phase**. During Overload take periodic self-damage and **cannot generate Instability**. |
| **Fluxweaver** | **Dissipate** (channel Instability reduction and minor pulses healing allies); **Rewind** (restore damage suffered in previous **4 sec**, by drawing down Instability). | Master Instability-backed shields and group healing; ultimately reverse recent **health damage**, not statuses or full world state. |
| **Voidbrand** | **Control Rift: Riftwalk** (teleport to active rift); **Superposition** (for **5 sec**, count as stealthed for **stealth-only abilities** but remain **visible/targetable**). | Extradimensional repositioning and strikes; culminate in more freely interleaved stealth-state combat. |

The final talent(s) of a tree should **meaningfully alter the rotation**, not simply be a larger spell. Exact talent placement/capstones remain open. Spell ranks can continue through level 60 independent of tree culmination.

## 9.5 Outstanding design and implementation gaps

The existing prototype still has a separate Surge stat and an older **Riftwalker** specialisation entry; neither reflects the agreed target design. The authoritative Sorcerer PDD details the migration work and unresolved numerical values.

Still to define: spell-specific Instability costs/gains, generation at cap, self-damage tuning, Phase Shift threshold, Rewind Instability expenditure and damage-history rules, rift lifetime/Riftwalk consumption, school assignment of other effects, mana costs, cooldowns and talent tier depth. Design high-damage spell scaling against ordinary NPCs so that early long-cast opening nukes do not trivialise encounters.

---

# 10. Occultist

## 10.1 Fantasy

The Occultist accesses powers that originate outside conventional mortal or divine magic.

Themes include:

- Pacts.
- Forbidden knowledge.
- Summoning.
- Binding.
- Otherworldly entities.
- Dangerous bargains.

The Occultist derives strength from relationships with external powers.

## 10.2 Attribute Direction

Likely emphasis:

- Intelligence
- Willpower

## 10.3 Core Mechanical Direction

Pacts are intended to form the central conceptual mechanic.

A pact should ideally involve exchange:

- Power for obligation.
- Power for risk.
- Power for sacrifice.
- Power for restrictions.

## 10.4 Proposed Specialisations

### Pactbound

Themes:

- Stable agreements
- Controlled external power
- Maintaining contracts
- Reliable occult casting

### Oathbreaker

Themes:

- Violating agreements
- Stealing power
- Dangerous short-term advantages
- Consequences for betrayal

### Binder

Themes:

- Summoning
- Controlling entities
- Delegating actions
- Binding hostile powers

## 10.5 Further Consideration Required

The Occultist requires a formal gameplay system.

Questions include:

- What actually constitutes a Pact mechanically?
- Are patrons/entities persistent character choices?
- Can Pacts be changed?
- Does Pactbound select a patron?
- What penalties exist for breaking a Pact?
- Does Oathbreaker literally break active Pacts?
- Can Binder control permanent summons?
- How many entities can be active?
- What class-wide resource does the Occultist use?
- How is Occultist magic visually and mechanically separated from Sorcerer Void magic and Cleric Spirit magic?

The external source of the Occultist's power should be mechanically visible.

---

# 11. Naturalist

## 11.1 Fantasy

The Naturalist channels natural systems rather than merely casting traditional nature spells.

The class encompasses:

- Growth.
- Decay.
- Weather.
- Animals.
- Spirits.
- Transformation.
- Natural cycles.

The Naturalist should represent nature as an interconnected system.

## 11.2 Attribute Direction

The class may require hybrid attribute support.

Exact primary attributes remain undecided.

## 11.3 Core Mechanical Concept

### Attunement

The player moves between different natural states.

A previously discussed model uses:

**Growth ↔ Decay**

with **Storm** acting as an additional triggered or temporary state.

Possible behaviour:

- Growth encourages healing, defence and creation.
- Decay encourages damage, decomposition and consumption.
- Storm creates moments of heightened volatility or offensive power.

## 11.4 Proposed Specialisations

### Warden

Themes:

- Protection
- Stability
- Natural resilience
- Defensive transformation

Expected role:

- Tank/support.

### Totemist

Themes:

- Ritual anchors
- Totems
- Area effects
- Group support

Expected role:

- Support/healer.

### Primalist

Themes:

- Transformation
- Aggression
- Rapid shifting
- Instinct

Expected role:

- Damage.

## 11.5 Further Consideration Required

Attunement requires significant mechanical definition.

Questions include:

- Is Growth ↔ Decay one continuous meter?
- Is neutral Attunement meaningful?
- How is Storm activated?
- Can players deliberately remain within one state?
- Does every specialisation use all Attunements?
- Is transformation class-wide or primarily Primalist?
- Does Warden transform physically?
- Are Totems persistent world objects?
- Can enemies destroy Totems?
- Does Totemist overlap too strongly with traditional Shaman design?
- How does the class interact with animal companions or summons?

The distinction between Naturalist and Ranger must remain strong:

- Ranger uses **knowledge of nature**.
- Naturalist uses **the power of nature**.

---

# 12. Ascetic

## 12.1 Fantasy

The Ascetic uses physical discipline, internal balance and mastery of the body.

Its power is generated internally rather than through:

- Conventional magic.
- External entities.
- Weapons.
- Divine intervention.

The class should support an unarmed or lightly armed martial fantasy.

## 12.2 Attribute Direction

Likely emphasis:

- Dexterity
- Willpower

## 12.3 Core Mechanical Concept

### Flow

Flow represents the rhythm and continuity of combat.

It should reward sequencing and maintaining momentum rather than functioning as conventional mana.

Potential influences include:

- Alternating techniques.
- Successful counters.
- Movement.
- Avoiding repeated abilities.
- Continuous combat engagement.

## 12.4 Proposed Specialisations

### Bulwark

Themes:

- Redirection
- Counters
- Deflection
- Defensive stance
- Using enemy force against them

Expected role:

- Tank.

### Pugilist

Themes:

- Combination attacks
- Speed
- Continuous offence
- Physical pressure

Expected role:

- Melee damage.

### Harmonist

Themes:

- Balance
- Restoration
- Support
- Manipulating Flow between allies

Expected role:

- Support/healer hybrid.

## 12.5 Further Consideration Required

Questions include:

- How exactly is Flow generated and lost?
- Is repeating the same technique discouraged?
- Does Flow have discrete stages or a continuous value?
- How does Bulwark tank without conventional heavy armour?
- Does Harmonist provide literal healing?
- What weapons, if any, can Ascetics use?
- Is unarmed combat mechanically superior or merely stylistic?
- How are martial animations handled across different character skeletons?
- How much supernatural visualisation should accompany Ascetic techniques?

The Ascetic must not simply become a conventional Monk analogue.

---

# 13. Role Coverage

The intended roster broadly supports traditional MMORPG roles while allowing hybrid specialisations.

Tentative coverage:

| Class | Tank | Healing/Support | Melee DPS | Ranged/Caster DPS | Control |
|---|---:|---:|---:|---:|---:|
| Cleric | — | Strong | — | Strong | Moderate |
| Exemplar | Strong | Moderate | Strong | — | Moderate |
| Fighter | Strong | — | Strong | — | Strong |
| Duellist | — | — | Strong | Limited | Strong |
| Ranger | — | Limited | Possible | Strong | Strong |
| Sorcerer | — | — | — | Strong | Strong |
| Occultist | Possible | Possible | — | Strong | Strong |
| Naturalist | Strong | Strong | Strong | Possible | Moderate |
| Ascetic | Strong | Moderate | Strong | — | Moderate |

This table is **not yet an implementation specification**.

---

# 14. Cross-Class Design Questions

Several decisions need to be made at the system level rather than class-by-class.

## 14.1 Resource Philosophy

The project should determine how many distinct resource models are desirable.

Currently proposed mechanics include:

- Momentum
- Maneuver Points
- Energy
- Combo Points
- Focus
- Quarry
- Instability
- Pacts
- Attunement
- Flow

Introducing unique mechanics for every class provides strong identity but also increases:

- UI complexity.
- Tutorial burden.
- Balance complexity.
- Networking state.
- AI complexity.
- Development cost.

Resources should therefore only exist where they materially change gameplay.

---

## 14.2 Shared Base Resources

A decision is still required regarding conventional resources such as:

- Mana.
- Stamina.
- Energy.
- Health-sacrifice mechanics.

Classes may combine conventional resources with their class mechanic.

For example:

**Mana + Instability**

may be more readable than treating Instability itself as the cost of every Sorcerer spell.

---

## 14.3 Specialisation Structure

The current Abilities and Talents PDD establishes **three specialisations and three freely mixable talent trees per class**, with one active specialisation selected from level 10. Active specialisation determines the character's primary role and its exclusive automatic ability package; talent investment may cross any of the class's three trees. The exact ability cadence and possible future roster expansion remain open. Role flexibility outside the active specialisation is class/content design, not an automatic entitlement.

---

## 14.4 Weapon Restrictions

Weapon identity requires a dedicated design pass.

Particular questions exist around:

- Fighter weapon flexibility.
- Duellist weapon requirements.
- Ranger melee capability.
- Exemplar weapon archetypes.
- Ascetic weapons versus unarmed combat.
- Whether caster weapons materially affect spellcasting.

---

## 14.5 Armour

The armour system should reinforce class identities without unnecessarily preventing character customisation.

Likely archetypes include:

- Heavy armour: Fighter, Exemplar.
- Medium/light armour: Ranger, Duellist.
- Light armour: Cleric, Sorcerer, Occultist.
- Special handling: Naturalist, Ascetic.

This remains provisional.

---

# 15. Highest-Priority Outstanding Design Work

Before detailed implementation, the following issues should be resolved approximately in this order:

1. **Duellist identity and specialisation structure**
   - Resolve class/spec naming collision.
   - Decide whether Energy + Combo Points survive.
   - Establish the class's unique mechanic.

2. **Cleric class mechanic**
   - Establish what unifies Holy, Spirit and Mind mechanically.

3. **Occultist Pact system**
   - Define how Pacts work as actual gameplay rather than narrative flavour.

4. **Fighter Momentum/Maneuver relationship**
   - Determine whether these are one or two systems.

5. **Naturalist Attunement**
   - Define Growth, Decay and Storm behaviour.

6. **Sorcerer Instability**
   - Class and spec identities are defined; finish numerical balancing, Fluxweaver healing, Voidbrand stealth rules, Overload and legacy Surge/Riftwalker migration.

7. **Exemplar Vows and Consecration**
   - Establish their interaction and restrictions.

8. **Ascetic Flow**
   - Define sequencing, generation and decay.

9. **Ranger Quarry**
   - Establish how target designation and target switching affect combat.

10. **Cross-class weapon and armour rules**
    - Establish equipment expectations before ability authoring becomes extensive.

---

# 16. Current Design Confidence

For implementation planning purposes:

### Relatively Stable

- Nine-class roster.
- Broad fantasy of each class.
- Fighter identity.
- Ranger identity.
- Exemplar identity.
- Naturalist versus Ranger conceptual distinction.
- Ascetic conceptual identity.
- Sorcerer use of Instability as its central direction.

### Provisional

- Most specialisation names.
- Role distribution.
- Attribute preferences.
- Equipment restrictions.
- Fighter Momentum/Maneuver implementation.
- Ranger Focus/Quarry implementation.
- Exemplar Consecration/Vow implementation.
- Ascetic Flow implementation.

### Requires Significant Further Design

- Duellist specialisation structure and unique mechanic.
- Cleric class-wide mechanic.
- Holy versus Spirit mechanical distinction.
- Occultist Pact mechanics.
- Naturalist Attunement details.
- Final numerical rules and talent-tier implementation for the now-defined Sorcerer specialisation structure.

---

# 17. Implementation Constraint

Class content should **not** be implemented in depth solely from the thematic descriptions in this document.

A class should first receive a dedicated class PDD defining:

- Core gameplay loop.
- Resource mechanics.
- Resource generation and expenditure.
- Ability categories.
- Defensive model.
- Mobility model.
- Crowd-control model.
- Equipment expectations.
- Specialisation mechanics.
- Role expectations.
- Progression.
- Talent structure.
- Multiplayer interactions.
- Required UI.
- Required animation support.
- Required server-authoritative state.

The present document defines the **class roster and design direction**, not complete class gameplay specifications.

---

# 18. Design Goal

At a glance, a player should be able to understand not only **what a class does**, but **why that class approaches combat differently from every other class**.

The target conceptual separation is:

- **Fighter:** mastery of warfare.
- **Duellist:** mastery of opportunity.
- **Ranger:** mastery of the hunt.
- **Ascetic:** mastery of self.
- **Exemplar:** power through conviction.
- **Cleric:** power through the spiritual/divine.
- **Sorcerer:** power through unstable magic.
- **Occultist:** power through forbidden relationships.
- **Naturalist:** power through natural systems.

Future class design should preserve these distinctions.


---

# 19. Ability-Toolkit Design Principles (2026-10-14)

The following principles are established design direction for all nine classes; exact ability lists and numerical balance are not yet approved.

1. **Class first.** Develop a coherent shared class toolkit before defining specialisation-specific abilities. The class itself must feel complete, not like three disconnected mini-classes.
2. **Accessible early combat with meaningful decisions.** The opening levels establish the core class loop, fundamentals of resource use and a few offensive, defensive and situational choices. Simplicity must not mean repeated use of one button; complexity is introduced incrementally.
3. **Specialisation depth later.** Active specialisation is selected at level 10 (per the Abilities and Talents PDD), but major exclusive spells and talent-driven gameplay transformations should generally arrive at mid-to-late levels. Early exceptions must be justified by role viability.
4. **Multiple paths to mechanical depth.** Talents can modify shared abilities, add passive interactions, or grant genuinely new active abilities. The base toolkit stays useful; specialisation makes it behave differently.
5. **Purpose, interaction and progression.** Plan each ability as a role in the toolkit: baseline attack, burst, defence, interruption, control, utility, etc. Avoid duplicate functions without a progression reason. Some early abilities may intentionally be superseded or become redundant (e.g., an early basic heal replaced by a later stronger one). Do not artificially preserve every ability at endgame.
6. **Complete but unequal toolkits.** Every class should have sufficient offense, survivability, and relevant situation/group contribution to play independently. Every class does **not** need every healing, movement, crowd-control or support mechanic. Role-specialised builds need all necessary tools for the role they claim to fulfil, but a healer need not use direct healing: for example, Sorcerer Fluxweaver specialises in shielding and Instability-proportional healing.
7. **Functional redundancy without forced composition.** Encounters may require tanking, healing, interrupts, control or counterplay; multiple class/specialisation combinations must reasonably provide required functions. No unique class mechanic, buff, dispel or summon should create a mandatory class slot in ordinary group content. Overlapping functional coverage need not mean identical ability buttons or effectiveness.
8. **Distinguish access from effectiveness.** Capability access, reliability, conditions, cooldown, range, resource cost and niche value are separate design decisions. Share functions selectively but retain different class solutions and strengths.
9. **Design in order:** (a) shared class + specialisation ability toolkit and interactions, (b) magical school(s) / Physical as appropriate, (c) ability names, icons, animation, visual effects and audio effects. Neither school themes nor presentation should determine the initial mechanical toolkit.
10. **Level-10 playable identity:** use roughly **5–7 learned combat abilities (including learnable combat passives) plus 1–2 learned utilities (including utility passives)** through level 10 as a flexible budget, excluding ranks and inherent class mechanics. Before choosing a specialisation, the base class must be mechanically enjoyable. Selecting an active specialisation at **level 10** grants an immediate identity-defining spell without talent investment. Balance cumulative trainer costs across classes so more passives or skills do not unfairly penalise early gold expenditure.
11. **No early pull-deleting abilities:** lengthy cast / high-burst spells are principally mid-to-late progression tools. Balance against normal NPC health, not solely bosses. Establish meaningful gameplay decisions before introducing overwhelming opening attacks.
12. **Talent mastery without artificial ten-level milestones:** talent trees build from their level-10 introduction toward advanced mastery and a **rotation-transforming culmination**, rather than simply granting a high-number nuke at the end. Fundamental role viability must precede the capstone; spell ranks continue through level 60.

The [Abilities and Talents PDD](Abilities-and-Talents-PDD.md) remains authoritative for how class abilities, automatic active-specialisation abilities and talent-granted abilities are acquired and persisted.

# 20. Magical School Assignment in Class Design

Ninth Age has **eight magical schools**: **Chaos, Earth, Air, Water, Fire, Light, Shadow and Psychic**. Physical damage is separate and is not a ninth magical school. Chaos is primordial magic; Earth, Air, Water and Fire are elemental expressions; Light, Shadow and Psychic are spiritual expressions involving consciousness and soul.

- Magical schools are **not class-exclusive**. A class may access only a subset consistent with its identity (for example, Naturalists need not use Fire).
- A school describes the magic's nature, **not a fixed catalogue of effects**. Periodic damage, slows, casting disruption and other mechanics are not universally locked to one school; class expression governs actual ability design.
- Ordinary magical casting does **not** impose a universal instability/backlash/environmental penalty on gameplay. Deliberate manipulation near the magical field's limits and its gameplay consequences are part of the **Sorcerer** identity (Instability).
- The Cleric specialisation associations are **Holy → Light (divinity), Spirit → Shadow (soul), Mind → Psychic (mind)**. Shadow's negative moral and cultural associations are world lore and do not automatically prescribe a caster's morality or ability mechanics.
- Final school access and any multi-school interactions remain subject to class-specific review; do not infer school-specific spell names, effects or numerical resistances from this table alone.

# 21. Preliminary Cross-Class Capability Matrix (2026-10-14)

**Status:** Provisional coverage proposal, **not** locked ability lists or implementation instructions.

**Legend:** `C` = intended core/base-class access; `T` = candidate through specialisation or talents (not necessarily every spec); `?` = undecided; `—` = not currently proposed (not an immutable prohibition).

**Columns:** Cl = Cleric; Ex = Exemplar; Fi = Fighter; Du = Duellist; Ra = Ranger; So = Sorcerer; Oc = Occultist; Na = Naturalist; As = Ascetic.

Every class is assumed to have baseline direct offense and enough individual defence to function, whether or not it has healing or a particular control primitive. The matrix is a **coverage tool**, not an entitlement checklist.

### Crowd control and debuffs

| Capability | Cl | Ex | Fi | Du | Ra | So | Oc | Na | As |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Stun | T | T | C | C | T | T | — | — | C |
| Root | — | — | — | — | C | T | T | C | — |
| Slow | T | — | T | C | C | C | — | C | T |
| Incapacitate / disorient | T | — | — | C | C | T | T | — | T |
| Fear / forced fleeing | T | — | T | — | — | — | C | — | — |
| Interrupt casting | T | C | C | C | C | C | T | T | C |
| Silence / spell lockout | T | — | — | T | — | C | C | — | — |
| Disarm | — | T | C | C | T | — | — | — | T |
| Knockback / forced movement | — | T | C | T | — | T | T | C | C |
| Taunt | — | T | T | — | — | — | — | T | T |
| Reduce enemy damage | T | C | C | T | — | — | C | T | — |
| Reduce physical defence | — | T | C | C | C | — | — | — | T |
| Increase magical vulnerability | T | — | — | — | — | C | C | T | — |
| Reduce healing received | T | — | — | C | T | — | C | — | — |
| Drain / disrupt resources | T | — | — | T | — | C | C | — | — |

### Healing, defence and group support

| Capability | Cl | Ex | Fi | Du | Ra | So | Oc | Na | As |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Self-healing | C | C | T | — | T | T | T | C | C |
| Direct ally healing | C | T | — | — | — | T | — | C | T |
| Group healing | T | T | — | — | — | T | — | T | T |
| Healing over time | T | — | — | — | — | — | — | C | T |
| Absorption / magical barriers | C | T | T | — | — | C | T | T | T |
| Protect / intercept for allies | T | C | T | — | — | — | T | T | T |
| Cleanse harmful effects | C | T | — | — | T | T | T | C | T |
| Remove enemy buffs | T | — | — | T | — | C | C | T | — |
| Restore ally resources | T | — | — | — | — | T | T | T | — |
| Offensive group support | T | C | C | — | C | T | — | T | — |
| Defensive group support | C | C | T | — | — | T | — | C | T |
| Resurrection | C | T | — | — | — | — | — | T | T |
| Threat reduction / redirection | — | T | T | C | T | — | T | — | — |
| Control resistance / removal | C | C | C | T | T | T | — | T | C |

### Offensive and tactical mechanics

| Capability | Cl | Ex | Fi | Du | Ra | So | Oc | Na | As |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Periodic damage | T | — | T | C | C | C | C | C | — |
| Area damage | T | C | T | T | C | C | T | C | T |
| Execute / finishing attacks | — | T | T | C | C | C | T | — | C |
| Charge / dash / leap | — | T | C | C | C | T | — | T | C |
| Stealth / concealment | — | — | — | C | T | T | — | — | — |
| Detect / track hidden targets | T | — | — | T | C | T | — | T | — |
| Traps | — | — | — | T | C | — | — | — | — |
| Persistent ground effects | T | C | — | — | C | T | T | C | — |
| Combat companions / summons | ? | — | — | — | ? | — | T | T | — |
| Transformation / shapeshifting | — | — | — | — | — | — | — | T | — |
| Poison-based combat | — | — | — | C | T | — | T | T | — |
| Bleeding-focused combat | — | — | T | C | T | — | — | — | T |

### Equipment and fighting styles

| Capability | Cl | Ex | Fi | Du | Ra | So | Oc | Na | As |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Dual wield | — | — | T | C | ? | — | — | — | T |
| Two-handed melee weapons | ? | C | C | T | T | — | — | T | ? |
| Shields | ? | C | C | — | — | — | — | T | — |
| Bows / crossbows | — | — | T | T | C | — | — | — | — |
| Unarmed combat | — | — | T | T | — | — | — | T | C |

# 22. Review and Encounter Coverage Rules

Before locking a class toolkit:

- Check the overall roster for **healthy asymmetry**: some tools should be rare, and specialisations should feel different; avoid distributing every capability to every class.
- Check each intended tank and healer specialisation for **role completeness** at the levels where group encounters expect that role. The Dungeon and Group Content PDD expects flexible low-level parties and, at mid-level and above, a conventional 1 tank / 1 healer / 3 DPS party without prescribing classes.
- Audit every ordinary mandatory encounter function against **multiple feasible group compositions** without a named-class requirement. Some classes may solve the problem differently, including with positioning, damage prevention or alternative counters.
- Avoid unique, irreplaceable permanent group buffs that implicitly require all nine classes in a raid. Class-specific utility may be useful or advantageous without being required to access content or achieve normal success.
- Test in both solo and group scenarios. `C` and `T` do not imply equal output, cooldown, range, or reliability.

# 23. Dedicated Class PDDs

Further class-specific design is maintained in [`class-design/`](class-design/). Each class document records current source-grounded identity and proposed specialisations, a candidate class-first toolkit and its share of the provisional capability matrix, differentiation, progression, and open issues. These drafts do **not** lock the final ability catalogue, role eligibility, equipment rules, magic-school allocation or numerical balance until those questions are explicitly resolved.
