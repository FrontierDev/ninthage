# Product Design Document: Playable Class Design

**Project:** Ninth Age  
**Document Type:** Product Design Document  
**Status:** Draft / Design Baseline  
**Purpose:** Define the current intended playable class roster, establish each class's core identity and gameplay space, and explicitly identify areas requiring further design work.

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

### Shadowcraft

Themes:

- Deception
- Disruption
- Theft
- Stealth
- Utility

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
- Is Shadowcraft too explicitly magical for the intended class identity?
- Should the third specialisation instead emphasise trickery, mobility, thrown weapons, dirty fighting or another mundane concept?

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

## 9.1 Fantasy

The Sorcerer manipulates raw and unstable magical power.

Unlike a scholarly wizard archetype, the Sorcerer should feel instinctive, dangerous and volatile.

## 9.2 Attribute Direction

Primary emphasis:

- Intelligence

## 9.3 Core Mechanical Concept

### Instability

Casting magic causes Instability to accumulate or fluctuate.

Increasing Instability should create opportunities for greater power while also introducing consequences.

Potential consequences may include:

- Spell alteration.
- Backlash.
- Resource volatility.
- Increased output.
- Unpredictable side-effects.
- Access to stronger abilities.

Instability should create meaningful decisions rather than simply being another resource bar.

## 9.4 Current Proposed Specialisations

### Cataclyst

Themes:

- Explosive magic
- Destruction
- Volatility
- Burst

### Fluxweaver

Themes:

- Manipulation
- Delaying effects
- Redistributing magic
- Altering magical states

### Voidbrand

Themes:

- Marks
- Magical brands
- Detonations
- Accumulated effects

## 9.5 Previous Specialisation Concepts

Earlier versions used:

- Temporal
- Elemental
- Chaos

These should not currently be treated as canonical.

## 9.6 Further Consideration Required

The specialisation structure requires validation.

Questions include:

- Is Cataclyst broad enough to replace an elemental specialisation?
- Does Voidbrand overlap too much with Occultist themes?
- What exactly does Fluxweaver manipulate?
- Is temporal magic still part of the Sorcerer's identity?
- Is elemental magic class-wide?
- How deterministic should Instability be?
- Can players intentionally maintain high Instability?
- What happens at maximum Instability?
- Should Instability ever produce random negative effects?

Excessive randomness should be avoided if it removes player agency.

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

Three specialisations per class is currently the working structure.

This has not yet been formally established as an immutable system requirement.

Consideration should be given to whether:

- Every class requires exactly three.
- Additional specialisations may be added later.
- Specialisations determine role.
- A class can perform secondary roles outside its specialisation.

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
   - Define the player-controlled risk/reward loop and final specialisation identities.

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
- Final Sorcerer specialisation structure.

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
