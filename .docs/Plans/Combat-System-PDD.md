# Ninth Age — Combat System Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Core real-time combat, targeting, spatial execution, casting, auto-attacks, hit/avoidance/mitigation, healing, auras, buffs/debuffs, crowd control, threat, death and combat feedback  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended core combat model for **Ninth Age**.

It is the authoritative product-design reference for:

- the baseline real-time MMORPG combat model;
- hard targeting and spatial ability execution;
- movement, facing, range and line-of-sight rules during combat;
- casts, channels, interrupts, cooldowns and the global cooldown;
- auto-attacks;
- hit, critical, avoidance, mitigation, blocking and absorption;
- healing resolution where it interacts with combat;
- ongoing auras, buffs, debuffs and periodic effects;
- crowd-control representation;
- threat and taunting;
- basic death and resurrection behaviour;
- combat feedback and information requirements;
- combat-relevant server authority and latency-tolerance requirements.

The following are owned primarily by other documents:

- class fantasies and class-wide mechanics — [Class Design PDD](Class-Design-PDD.md);
- detailed ability/talent acquisition and loadouts — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- exact stat curves and progression — [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- equipment power and itemised combat stats — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- NPC aggro/perception and encounter scripting beyond the threat baseline — [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- detailed player death recovery, graveyards, corpse handling and persistent respawn rules — the appropriate World/Persistence PDDs;
- PvP-specific crowd-control, diminishing-return or balance rules — [PvP PDD](PvP-PDD.md).

Where existing implementation conflicts with this document, this document takes precedence until deliberately revised.

---

## 2. Design Pillars

### 2.1 Hard-target MMORPG combat is the foundation

Ninth Age is not an action-combat MMO.

A selected friendly or hostile unit remains the normal reference for combat. Players should be able to read crowded party and raid encounters, identify targets, understand cast states and execute rotations without manually aiming every attack.

Spatial mechanics are layered **on top of** hard targeting rather than replacing it.

### 2.2 Spatial execution matters selectively

Abilities may use:

- hard single targets;
- hard-target-centred area effects;
- ground targeting;
- self-centred area effects;
- directional cones;
- lines and cleaves;
- charges and leaps;
- placed objects;
- traps, wards, totems and similar world-space effects;
- selected physically simulated projectiles or skillshot-like mechanics.

Free aiming should be a deliberate property of particular abilities, not the default combat model.

### 2.3 Combat must tolerate ordinary MMORPG latency

Ninth Age should not rely on frame-perfect defensive timing.

Facing, range, positioning and anticipation may matter, but ordinary combat should remain reliable under realistic network latency.

Examples of appropriate active defence include:

> Block the next eligible attack received within the next 4 seconds.

rather than requiring a player to press Block within a tiny impact window.

### 2.4 Class identity must remain visible in combat state

Ongoing effects should retain the identity of the class and ability that created them.

The combat system must not reduce distinctive class mechanics to a small shared vocabulary such as generic Burning, Poisoned or Chilled conditions.

A Sorcerer effect and an Occultist effect may both deal periodic Fire damage while remaining visibly and mechanically separate authored auras.

### 2.5 Group composition must matter beyond raw damage

Classes must provide meaningful group value beyond personal damage output.

Party buffs, defensive utility, resource support, control, encounter utility and other class-specific contribution are legitimate and important parts of group composition.

The answer should not be to give every class an interchangeable generic percentage-damage aura.

### 2.6 Content should be authored; mechanics should be reusable

A named ability or aura should usually be an authored data object.

Reusable hard-coded effect handlers should implement mechanics such as:

- damage;
- healing;
- stat modification;
- resource modification;
- absorption;
- control;
- aura application/removal;
- threat manipulation.

Designers should compose abilities and auras from these primitives rather than require one bespoke runtime class per spell or aura.

---

## 3. Core Combat Model

Ninth Age uses **real-time, server-authoritative, hard-target MMORPG combat**.

The player may maintain a selected target while moving and looking independently of that target.

Target selection does not force:

- camera facing;
- movement direction;
- character facing;
- automatic character rotation except where a particular ability deliberately does so.

An ability determines whether it needs:

- a selected unit target;
- a world position;
- a direction;
- no external target;
- a placed world object;
- another explicitly authored targeting form.

The combat engine validates the authored targeting requirements when the action executes.

---

## 4. Targeting Forms

The ability system must support at least the following combat targeting shapes.

### 4.1 Single target

A conventional hard-targeted friendly or hostile unit.

### 4.2 Target-centred area

A hard target is selected and becomes the centre/anchor of an area effect.

### 4.3 Ground target

The player selects an eligible world position.

The server validates:

- maximum range;
- valid terrain/world position;
- line of sight where required;
- any authored placement restrictions.

### 4.4 Self-centred area

The effect originates from the caster.

### 4.5 Directional cone

The effect is projected from the caster according to character facing.

### 4.6 Line / cleave

The effect occupies an authored line, strip or forward cleave region.

### 4.7 Charge / leap

The action combines targeting with authored movement.

The destination may be:

- a target;
- a point;
- an offset relative to a target;
- another explicitly authored destination.

Movement and collision validation remain server-authoritative.

### 4.8 Placed objects

Abilities may create world-space combat objects such as:

- traps;
- wards;
- totems;
- rifts;
- consecrated areas;
- deployables;
- other authored anchors.

The object's ownership, lifetime, area and interaction rules belong to the ability definition/system using it.

### 4.9 Physical projectiles

Some ranged effects may use actual travelling projectiles where spatial interaction is part of the mechanic.

A physical projectile may be authored to:

- collide with terrain;
- collide with valid targets;
- pierce targets;
- home;
- be intercepted;
- trigger at a destination;
- produce a trail or area effect.

This is distinct from a conventional hard-target ranged spell whose result is logically resolved and merely represented visually by a projectile.

Physical projectile simulation should be used selectively.

---

## 5. Movement During Combat

Movement is generally permitted during combat.

Whether movement is permitted **during a cast or channel** is authored per ability.

### 5.1 Mobile abilities

Instant abilities normally allow ordinary movement.

Some cast-time/channelled abilities may explicitly allow movement while casting.

This should be part of class and ability identity rather than a universal caster privilege.

### 5.2 Stationary abilities

Some abilities require the caster to remain stationary.

Starting meaningful movement during such a cast interrupts it.

The implementation may use small movement tolerances to avoid cancelling casts because of network/physics jitter.

### 5.3 No damage pushback

Taking ordinary damage does **not** increase cast time or create classic damage-based spell pushback/concentration loss.

If an attack is intended to disrupt casting, it should use an explicit interrupt, control, displacement or other authored mechanic.

---

## 6. Facing

Facing is a real combat rule.

It should matter where it adds tactical meaning, without requiring precision more appropriate to an action game.

### 6.1 Offensive facing

Abilities may require the target or execution area to be within an authored frontal arc.

Typical direct melee attacks should require broadly correct facing rather than exact aim.

Directional cones, cleaves and similar abilities naturally use facing as part of their geometry.

### 6.2 Defensive facing

Normal **Parry** and **Block** opportunities require the defender to be facing the attacker within an appropriate defensive arc.

**Dodge** does not require facing by default.

Individual abilities may override these rules.

### 6.3 Exact arcs

Exact frontal/block/parry arc widths are balance parameters.

They should be generous enough to tolerate latency and ordinary character-control imprecision.

---

## 7. Range, Deadzones and Line of Sight

### 7.1 Authored range

Every ranged ability may define its own maximum range.

Melee abilities use the game's standard melee interaction range unless explicitly exceptional.

### 7.2 No weapon-specific normal melee reach

Normal dagger, sword, mace, axe, polearm and other melee weapon categories do **not** receive different baseline melee ranges.

The potential flavour benefit is not worth the latency/frustration cost in an MMORPG.

Specific abilities may still author unusual geometry or increased reach where that is the actual mechanic.

### 7.3 Minimum range / deadzone

Abilities may define a **minimum range**.

This is particularly appropriate for selected Bow/Crossbow abilities.

The deadzone is an ability rule, not a universal property of every ranged action.

### 7.4 Close-range bonuses

Abilities may also reward intentionally collapsing distance.

Examples include:

- guaranteed critical hit when cast within an authored close-range threshold;
- increased damage at close range;
- altered secondary effects.

These are authored ability mechanics.

### 7.5 Line of sight

Abilities that require line of sight must be blocked by deliberate combat/world geometry.

Decorative micro-geometry should not unpredictably invalidate combat casts.

The server validates line of sight at the relevant execution point.

### 7.6 Range validation timing

Range may be validated:

- when an action begins;
- during a channel;
- when an action completes;
- when a physical projectile impacts;
- at more than one of these stages where required.

The exact stages are properties of the ability/targeting model.

Latency buffers may be applied to server range validation, but must not permit obviously invalid casts.

---

## 8. Casting, Channels and Action Execution

Abilities may be:

- instant;
- cast-time;
- channelled.

### 8.1 Cast lifecycle

A cast-time action has, conceptually:

1. activation/validation;
2. cast start;
3. optional ongoing validation;
4. cast completion;
5. effect resolution.

An interrupted cast does not reach normal completion.

### 8.2 Channelling

A channel may execute one or more effects at authored intervals/ticks.

Channel effects may require the target to remain valid throughout the channel.

### 8.3 Resource costs

Resource costs may occur at authored phases such as:

- cast start;
- cast completion;
- channel tick.

Interrupted-cast refund behaviour may be authored.

### 8.4 Interrupts

Hard interrupts explicitly stop an interruptible active cast/channel.

Abilities may be authored as:

- interruptible;
- uninterruptible;
- interruptible only under particular conditions;
- vulnerable to displacement/control rather than a normal interrupt.

Interrupt design should produce encounter mechanics rather than every cast being solved identically.

### 8.5 No generic concentration mechanic

There is no universal concentration score or damage-pushback system.

Any mechanic that causes a cast to fail must be explicitly authored.

---

## 9. Global Cooldown and Individual Cooldowns

Ninth Age uses a conventional global cooldown.

The current baseline configuration is:

> **Global Cooldown: 1.0 second**

The exact number remains tuneable, but the single-GCD model is the design baseline.

### 9.1 GCD participation

Each ability explicitly authors whether it triggers the global cooldown.

Abilities that do not trigger the GCD must be intentionally designed as such.

### 9.2 Individual cooldowns

Abilities may have individual cooldowns.

Cooldowns may optionally scale with Haste where authored.

### 9.3 Charges

Abilities may use cooldown charges where authored.

### 9.4 Cooldown interaction

The global cooldown and individual cooldowns are separate.

An ability normally needs both its own cooldown/charges and any applicable GCD to permit activation.

---

## 10. Auto-Attacks

Auto-attacks are server-controlled recurring weapon attacks against the actor's selected combat target.

The current implementation provides the correct baseline direction:

- auto-attacks do not trigger the global cooldown;
- they execute through the ordinary spell/effect infrastructure;
- they wait when the target is out of range rather than cancelling the auto-attack state;
- their swing/cooldown period may scale with Haste;
- they stop when there is no valid target or the actor can no longer attack;
- NPCs may chase toward auto-attack range through their AI/movement system.

Normal melee auto-attacks use the standard melee range rather than weapon-category-specific reach.

The current implementation pauses auto-attacks while the actor is performing a non-instant cast/channel. This remains the baseline unless the Abilities/Class design later establishes a clear reason for a specific exception.

The basic attack system should be extensible enough to support ranged weapon basic attacks where required by the final class/weapon design.

---

## 11. Attack Types

Combat effects distinguish at least:

- **Melee**
- **Ranged**
- **Spell**

This classification controls which offensive hit/critical statistics and defensive reactions normally apply.

It is separate from the effect's **damage school**.

For example, an ability may be:

- a Melee attack that deals Physical damage;
- a Ranged attack that deals Fire damage;
- a Spell attack that deals Physical damage;
- another explicitly authored combination.

---

## 12. Hit Resolution

The offensive hit-stat families defined by itemisation remain distinct.

### Melee

- Melee Hit Rating → Melee Hit Chance

### Ranged

- Ranged Hit Rating → Ranged Hit Chance

### Spell

- Spell Hit Rating → Spell Hit Chance

### 12.1 Base miss

The existing implementation currently uses:

- 5% base miss;
- +1 percentage point miss per target level above the attacker;
- a configurable maximum miss chance.

These are acceptable current balance defaults but remain tuning parameters rather than immutable design constants.

### 12.2 Always-hit effects

An effect may explicitly bypass the normal hit roll.

This must be authored and should not be inferred merely because an effect is beneficial or special.

### 12.3 Successful-hit sequence

A normal damaging attack should conceptually resolve in stages:

1. validate target, range, facing and line of sight as required;
2. resolve miss/hit;
3. resolve applicable avoidance;
4. resolve critical outcome;
5. calculate damage;
6. apply Armour/resistance mitigation;
7. apply Block/Deflection where applicable;
8. consume absorption/shields;
9. apply the remaining health damage;
10. generate combat events and threat from the resolved outcome.

The implementation may optimise this sequence internally, but player-visible results must remain equivalent.

---

## 13. Avoidance and Defensive Resolution

### 13.1 Melee

Ordinary melee attacks may be:

- Dodged;
- Parried;
- Blocked.

Parry and Block require appropriate facing by default.

### 13.2 Ranged

Ordinary ranged attacks may be:

- Dodged;
- Blocked where the defender has an appropriate shield/block capability.

They are not normally Parried.

### 13.3 Spell

Ordinary spell attacks are not normally Dodged, Parried or Blocked.

They use Spell Hit for accuracy and damage-school mitigation for damage reduction.

Specific abilities may explicitly opt into other behaviour.

### 13.4 Dodge

A successful Dodge avoids the attack.

### 13.5 Parry

A successful Parry avoids the eligible attack.

Parry is directional by default.

### 13.6 Block and Deflection

A successful Block does **not** inherently nullify the attack.

**Block Rating** determines the chance to block.

**Deflection** determines how much damage a successful block removes.

Deflection is primarily a shield intrinsic property, as defined by the Items PDD.

Block is directional by default.

### 13.7 Current implementation conflict

The current SpellEffect_Damage implementation treats Block as a complete avoidance result and uses an avoidance calculation that does not represent the intended final combat model.

That implementation is provisional and must be replaced by resolution consistent with this PDD.

---

## 14. Critical Hits

Melee, Ranged and Spell critical chance remain separate offensive families.

The target's **Critical Resistance** reduces incoming critical chance.

The current configuration uses:

- 5% base critical chance;
- -1 percentage point critical chance per target level above the attacker;
- 1.5× critical damage.

These are current balance defaults and may be tuned in the Stats/Progression design.

Critical outcome must remain available to combat events so talents, auras and other authored mechanics can react to it.

Healing uses its separate Healing Critical Rating / Healing Critical Chance model rather than Spell Critical Chance.

---

## 15. Damage Schools and Mitigation

Physical and magical damage-school identity is authored per damage effect.

The established magical resistance families are:

- Air
- Earth
- Fire
- Water
- Light
- Shadow

Physical damage is mitigated through Armour and other applicable defensive effects.

### 15.1 Armour

Armour is the normal baseline mitigation for Physical damage.

The exact level-scaling curve belongs to Character Stats and Progression.

### 15.2 Magical resistances

Each canonical magical resistance mitigates its corresponding school.

There is no required generic catch-all Spell Resistance stat in the final itemisation model.

The existing prototype spell_resistance / spell_expertise full-resist path is therefore provisional.

### 15.3 Multi-school effects

Effects may carry more than one damage school.

This is required for authored effects such as a hypothetical Fire/Shadow Occultist aura.

The exact rule used to combine multiple applicable resistance values remains an open numerical combat rule and must be resolved consistently before final damage balancing.

The current prototype behaviour of simply using the highest mitigation among listed schools is not made authoritative by this PDD.

---

## 16. Absorption

Absorption is a consumable defensive pool that removes damage before it reaches Health.

Absorption occurs after normal damage mitigation unless a specific effect explicitly defines another order.

The combat result should retain:

- pre-absorb damage;
- absorbed amount;
- final health damage.

This is important for:

- combat logging;
- threat;
- talents;
- damage meters;
- encounter logic.

Absorbed damage does not count as health damage dealt.

---

## 17. Healing

Healing is resolved separately from damaging Spell Power.

The item/stat system defines:

- Healing Power;
- Healing Critical Rating;
- Healing Critical Chance;
- Healing Received where used by authored effects.

### 17.1 Effective healing

The combat engine must distinguish:

- raw healing;
- effective healing actually restored;
- overhealing.

Threat and mechanics that explicitly reference healing done should use whichever of these the mechanic specifies.

Default healing threat uses **effective healing**, not overhealing.

### 17.2 Healing criticals

Healing effects may critically heal where authored/eligible.

The exact healing-critical multiplier is a balance value.

---

## 18. Active Defence

Ninth Age does not use universal twitch defensive controls such as mandatory dodge-rolls, perfect-parry buttons or frame-tight active blocks.

Class abilities may provide active defensive mechanics, but they should use latency-tolerant windows and authored rules.

Examples include:

- block the next eligible attack within 4 seconds;
- parry the next melee attack within 3 seconds;
- absorb the next X damage for 6 seconds;
- gain increased Block Rating for 5 seconds;
- become immune to one authored control category for a duration.

These effects should normally be represented through the aura/effect/event system rather than special timing code.

Mobility, defensive reactions and counters should remain unevenly distributed across classes to preserve class identity.

---

## 19. Aura System — Core Principle

The combat system does **not** use a generic condition layer for ongoing combat states.

There is no requirement for an ability to reduce itself to a shared player-facing state such as:

> Deals Fire damage and applies Burning.

Instead:

> **Every meaningful ongoing player-facing combat effect is an individually authored AuraDefinition.**

The aura retains:

- its own name;
- icon;
- duration;
- tick rate;
- stacking behaviour;
- caster/source;
- target;
- effect values;
- triggered behaviours;
- tags;
- tooltip identity.

The architecture follows the successful RPE2 model, adapted from turn-based timing to real-time timing.

This does **not** remove unrelated predicate/requirement frameworks such as ConditionDefinition where they are used for item equip requirements or other non-aura validation. It removes the concept of a generic **combat-status condition layer** standing in for authored auras.

---

## 20. Aura Data Model

An authored aura must contain, conceptually:

### Identity

- definition ID;
- display name;
- icon;
- description/generated tooltip data;
- beneficial/debuff presentation state where required;
- optional metadata tags.

### Lifetime

- duration;
- tick interval where periodic;
- maximum stacks;
- stacking behaviour.

### Components

One or more reusable effects associated with an execution phase.

Supported phases include at least:

- On Apply;
- On Tick;
- On Expire;
- On Dispel.

### Triggered events

An aura may also listen to combat events and execute reusable effects in response.

This is separate from its ordinary periodic/persistent components.

---

## 21. Reusable Aura Effects

Aura behaviour should be assembled from reusable effect implementations.

The system should support effect types such as:

- Damage
- Heal
- Absorb
- Stat
- Resource
- Control
- Apply Aura
- Remove Aura
- Dispel / Remove by tag
- Threat modification/manipulation where required
- additional reusable primitives introduced by genuine content needs.

An aura-specific runtime class should **not** be created merely because an aura has a particular name.

For example, there should be no need for:

- LivingFlameBehaviour.cs;
- AshenCorruptionBehaviour.cs;
- MoltenArmourBehaviour.cs.

The named aura supplies data to reusable effect/event handlers.

If a genuinely new mechanic cannot be expressed by existing primitives, the preferred solution is to add a new **reusable** effect/event primitive rather than hard-code one named aura.

---

## 22. Periodic Auras

Each aura may define its own:

- total duration;
- periodic tick interval;
- periodic effect magnitude;
- scaling;
- damage school(s);
- stack behaviour.

This allows two superficially similar DoTs to remain mechanically distinct.

Illustrative example:

> **Living Flame**  
> 12-second duration, 2-second tick interval, Fire periodic damage.

Illustrative example:

> **Ashen Corruption**  
> 18-second duration, 3-second tick interval, Fire/Shadow periodic damage.

Both use the same reusable periodic Damage effect implementation.

They remain different aura definitions with independent identity and balance.

One aura-wide tick interval is the baseline. Per-component periodic intervals should only be added if future content demonstrates a genuine need.

---

## 23. Aura Runtime Identity and Ownership

Runtime aura identity must incorporate:

> **Aura Definition + Caster + Target**

This follows the RPE2 principle.

Two Sorcerers applying the same aura to the same target therefore create separate owned contributions unless that aura's explicitly authored stacking rules say otherwise.

This is required for:

- personal duration tracking;
- damage attribution;
- healing attribution;
- threat attribution;
- refreshing the correct player's aura;
- talent triggers;
- dispels;
- combat logging;
- multiple players of the same class.

The runtime must always be able to identify the original aura caster/source.

---

## 24. Aura Stacking

The current Ninth Age Condition_Stack and Condition_Extend concepts are removed from the canonical combat design.

Stacking behaviour should describe the aura itself, not a supposed condition category.

The baseline stacking modes should remain simple and content-driven.

At minimum, the system must support:

- **Refresh Duration** — reapplication refreshes the owned aura's duration, adding stacks where the aura supports stacks.
- **Independent Duration** — stacks/applications that require independent expiry can track their own remaining duration.

Additional stacking modes should only be introduced when a real authored mechanic requires semantics that cannot be expressed cleanly with these modes.

Maximum stack count is authored by the aura.

---

## 25. Aura Tags

Aura tags are optional metadata.

They have **no automatic combat meaning** merely because a tag exists.

Examples might include:

- sorcerer;
- occultist;
- curse;
- fire_dot;
- bleed;
- poison;
- class_buff.

A talent or ability may explicitly query a tag.

Examples:

> Deal additional damage for each of your active auras tagged curse.

> Remove one aura tagged poison.

> Increase damage dealt by your auras tagged fire_dot.

Tags therefore provide reusable categorisation without replacing named aura identity.

There is no invisible generic Burning/Poisoned/Chilled state inferred unless an explicitly authored mechanic deliberately asks for such a tag/category.

---

## 26. Aura Combat Events

Auras may react to combat events.

This is a core part of the reusable architecture and should follow the RPE2 model.

An authored aura event should be able to specify:

- triggering combat event;
- optional chance;
- optional event filters;
- trigger target;
- one or more reusable effects.

Examples:

> When the aura target is hit by a melee attack, deal Fire damage to the attacker.

> When the aura caster deals Shadow damage, restore a resource.

> When the aura target blocks, apply another aura.

> When this aura's periodic damage critically hits, trigger an effect.

### 26.1 Event provenance

Combat events produced by aura effects must carry enough provenance for talents and other mechanics to identify the source.

At minimum, an aura-produced combat event should make available:

- AuraDefinition;
- runtime aura instance ID/key;
- aura caster;
- aura target;
- source spell where applicable;
- source aura component/effect;
- event type;
- resolved amount;
- damage/healing school/type where relevant;
- critical/avoidance/block result where relevant.

This enables mechanics such as:

> When **your Living Flame** deals damage, restore Health equal to 10% of the damage dealt.

without bespoke code for Living Flame.

---

## 27. Buffs and Debuffs

Buffs and debuffs are **auras**.

A beneficial or harmful effect keeps its own authored identity rather than becoming a generic combat condition.

Examples include:

- class buffs;
- personal defensive buffs;
- damage-over-time effects;
- healing-over-time effects;
- curses;
- poisons;
- bleeds;
- stat reductions;
- crowd-control effects.

The aura's presentation metadata determines whether it is displayed as beneficial/harmful where appropriate.

---

## 28. Dispel and Aura Removal

Aura removal must be able to target:

- an exact aura;
- one or more auras matching an authored tag;
- a limited number of matching auras where required;
- all matching auras where required.

Dispel/removal mechanics operate on the named aura instances.

The system should not require a parallel generic-condition object to make cleansing work.

On-dispel aura components/events may execute when the aura is removed specifically by a dispel mechanic.

Natural expiration and dispel are distinct removal causes.

---

## 29. Bespoke AuraBehaviour Classes

The current AuraBehaviour ScriptableObject layer is not the desired canonical architecture for normal aura authoring.

Existing bespoke behaviours such as:

- Heal Attacker On Hit;
- Damage Host On Hit;

should be replaced by data-driven aura events plus reusable effects.

AuraBehaviour should be phased out rather than becoming a place where one-off spell logic accumulates.

A genuinely novel reusable mechanic may justify a new reusable effect/event handler, but not a named one-off behaviour class for ordinary content.

---

## 30. Crowd Control

Crowd control is represented through individually authored auras containing reusable **Control** effects.

There is no generic condition object called Stunned, Rooted, etc. that replaces the authored aura.

For example:

> **Hammer of Judgment**  
> Aura containing a Control effect that prevents actions/movement as authored.

The engine understands the reusable control primitive; the player sees the actual aura that caused it.

Reusable control capabilities may include:

- stun;
- root;
- silence;
- fear/forced movement where supported;
- incapacitation;
- movement-range restriction;
- forced-hit vulnerability;
- break on damage;
- other explicit control flags/mechanics.

Boss/elite immunities and encounter-specific control rules are authored through NPC/encounter systems.

PvP diminishing returns, if adopted, belong to the PvP PDD rather than being assumed globally.

---

## 31. Class and Party Buffs

Party buffs are an important part of group composition.

The design goal is:

> **Every class should provide meaningful group value that is not reducible to its personal damage output.**

This value may come from:

- offensive buffs;
- defensive buffs;
- healing support;
- resource support;
- resistances;
- mobility;
- control;
- protection;
- encounter utility;
- class-specific mechanics;
- other authored contribution.

These should normally be **named class auras**.

They should not be flattened into generic shared states merely to simplify implementation.

The combat system must permit multiple classes to provide distinct, useful party contributions.

---

## 32. Threat — Core Model

Threat follows the successful RPE2 model, adapted to real-time combat.

Every hostile NPC maintains its own threat table.

Each threat entry associates an eligible hostile actor with accumulated threat against that NPC.

### 32.1 Default target selection

In ordinary threat-driven AI, the NPC chooses the valid hostile actor with the highest threat.

When threat is tied, deterministic tie-breaking should use:

1. distance;
2. stable actor/network identity.

Encounter AI may explicitly override ordinary threat targeting for particular mechanics.

### 32.2 Damage threat

Damage threat is based on **actual Health damage applied**, after mitigation and absorption.

Default formula:

> **Damage Threat = Effective Health Damage × Effect Threat Coefficient × Threat Generated modifier**

Overkill beyond actual removable Health does not create additional applied-damage threat.

### 32.3 Threat coefficient

Damage effects may author a threat coefficient.

Typical intent:

- ordinary damage ≈ 1.0 baseline;
- lower-threat abilities < 1.0;
- high-threat/tank attacks > 1.0.

Exact coefficients belong to ability balance.

### 32.4 Threat Generated

Actors may have a **Threat Generated** modifier/stat.

This allows:

- tank stances;
- temporary buffs/debuffs;
- specialised abilities;
- encounter mechanics

to alter all appropriate threat generated by that actor.

Threat Generated is not a normal routine item-budget stat, consistent with the Items PDD.

### 32.5 Healing threat

Effective healing generates threat.

Default formula:

> **Healing Threat = Effective Healing × Healing Threat Coefficient × Threat Generated modifier**

The default healing-threat coefficient is **0.5** unless an effect specifies otherwise.

Overhealing generates no normal healing threat.

Healing threat is applied to hostile NPCs for which the healing action is relevant to the active engagement, rather than magically aggroing unrelated creatures across the world.

### 32.6 Periodic effects

Periodic damage/healing uses the same threat rules per resolved tick.

### 32.7 Flat and special threat

Abilities may explicitly add, reduce, transfer or otherwise manipulate threat where a class or encounter mechanic requires it.

These are authored mechanics rather than hidden exceptions.

---

## 33. Taunt

Taunt is a **temporary hard target override**.

While a valid taunt is active:

- the affected NPC prioritises the taunting actor regardless of normal threat ranking;
- ordinary threat continues to exist underneath the taunt.

When the taunt expires:

- the NPC returns to normal target selection from its threat table.

Taunt does not need to permanently rewrite the target's threat to the top value merely to create the forced-target behaviour.

An encounter may make a creature immune to taunt or otherwise alter taunt behaviour.

---

## 34. Threat Lifetime

Threat does not normally decay during an active engagement.

Threat is cleared when the relevant NPC:

- dies;
- fully resets/leaves combat;
- is otherwise explicitly reset by encounter logic.

Entering combat/aggro establishes whatever initial threat entry is required for the NPC to engage its initial target.

Exact perception and aggro-acquisition rules belong to the AI and Encounter Behaviour PDD.

---

## 35. Combat State

Actors may enter an **in-combat** state when participating in an active hostile engagement.

Combat state can be used by:

- abilities;
- regeneration rules;
- interaction restrictions;
- UI;
- encounter logic.

The exact grace period for leaving combat is a tuning/AI rule.

Combat state must not be determined solely by whether the player currently has a selected hostile target.

---

## 36. Death

An actor reaches the dead state when Health reaches zero unless an explicit mechanic prevents that death.

On death:

- active casting stops;
- auto-attacking stops;
- ordinary combat actions become unavailable;
- the actor is no longer a normal valid living target;
- threat-driven enemies no longer select the dead actor as an ordinary combat target;
- abilities explicitly allowed to target dead actors may still do so.

World-level corpse, graveyard and respawn behaviour is owned outside this PDD.

### 36.1 Resurrection

Resurrection is an authored ability/effect that targets a dead actor.

The final rules for:

- in-combat resurrection limits;
- restored Health/resources;
- resurrection sickness/penalties;
- encounter restrictions

remain ability/content balance decisions.

### 36.2 Auras through death

Whether a particular long-duration aura persists through death should be explicitly authored or governed by a later global convention.

The aura architecture must be capable of distinguishing expiration/removal due to death from ordinary natural expiration where gameplay requires it.

---

## 37. Telegraphs and Readability

Combat should communicate dangerous actions clearly without requiring every mechanic to paint an exact geometric solution on the ground.

Telegraph channels may include:

- animation;
- cast bars;
- character facing;
- VFX buildup;
- ground indicators;
- audio;
- environment changes.

Precise ground telegraphs are appropriate where exact area boundaries are important.

Other attacks may rely on readable animation/facing/cast information.

The objective is for creatures to appear to perform attacks, not merely to draw coloured polygons that solve the mechanic for the player.

---

## 38. Collision and Combat Geometry

Normal allied-player hard collision should not be required for combat.

Ordinary combat should not depend on players physically body-blocking one another.

World geometry, encounter objects and deliberately authored large-creature collision remain meaningful.

Temporary ability-created denial/collision objects may exist where specifically designed.

Front-line gameplay should primarily arise from:

- threat;
- positioning;
- facing;
- area denial;
- slows/control;
- interception/protection abilities;
- encounter geometry;

rather than universal hard player collision.

---

## 39. Combat UX Requirements

The combat UI must expose enough information for players to understand the state that affects their decisions.

At minimum this includes:

- selected target;
- target Health/resources as appropriate;
- cast/channel progress;
- important target auras;
- important self auras;
- remaining aura durations;
- aura stack counts;
- cooldown/GCD state;
- resource state;
- combat outcomes;
- interruptible cast information where appropriate.

### 39.1 Aura ownership

The player must be able to identify and track **their own authored aura contribution** on a target.

This is particularly important for:

- DoTs;
- HoTs;
- curses;
- class marks;
- debuffs that must be refreshed;
- talents that interact with the player's own aura.

The UI does not need to pretend another player's copy is the same runtime instance.

### 39.2 Combat log

Combat logging should retain:

- source;
- target;
- ability/aura source;
- amount;
- damage/healing type;
- miss/dodge/parry/block/resist outcomes;
- critical outcome;
- absorption;
- threat where surfaced by meters/debugging.

### 39.3 Threat UI

The UI may surface:

- current target threat;
- relative threat position;
- target-of-target;
- warnings when close to taking aggro.

Exact presentation belongs to UI/UX.

---

## 40. Class-System Interaction

This PDD provides the combat substrate for the class mechanics already identified elsewhere.

Examples include:

- Fighter defensive and threat tools;
- Ranger deadzone/close-range/spatial interactions;
- Exemplar Consecrated Ground;
- Sorcerer Instability and authored magical auras;
- Occultist pacts and authored curses/corruptions;
- Naturalist area effects/totems;
- Ascetic reactive/flow mechanics.

The Combat system should provide reusable primitives rather than hard-code these class identities into the core engine.

---

## 41. Multiplayer and Server Authority

The server is authoritative for combat outcomes.

This includes:

- cast legality;
- cooldown/GCD legality;
- resource costs;
- target validity;
- range;
- facing where required;
- line of sight;
- projectile impacts where gameplay-relevant;
- hit/miss;
- Dodge/Parry/Block;
- critical results;
- mitigation;
- absorption;
- Health/resource changes;
- aura application/ticking/removal;
- crowd control;
- threat;
- taunt;
- death;
- resurrection.

The client may provide immediate presentation/prediction but cannot authoritatively decide a combat result.

### 41.1 Latency tolerance

Server validation should include deliberate tolerances where required for:

- cast-end range;
- channel range;
- facing boundaries;
- movement cancellation;
- target position.

These tolerances must reduce false rejection without allowing obviously impossible attacks.

No core combat mechanic should require consistently sub-frame network reaction from the player.

---

## 42. Persistence

Normal active combat state is transient.

The following are **not** intended to persist through a server restart as an in-progress combat encounter by default:

- threat tables;
- active casts;
- auto-attack state;
- short combat auras;
- NPC combat target;
- temporary encounter mechanics.

Long-duration world buffs or special persistent aura-like effects may opt into persistence if another system explicitly requires it.

Player Health/resource persistence belongs to the character/persistence design.

---

## 43. Content Authoring Requirements — Abilities

Ability authoring must support, as applicable:

### Identity

- name;
- icon;
- description/tooltip;
- tags.

### Timing

- instant/cast/channelled;
- cast time;
- channel tick timing;
- movement allowed while casting;
- cooldown;
- Haste scaling where appropriate;
- charges;
- GCD participation.

### Targeting and spatial rules

- target form;
- maximum range;
- optional minimum range/deadzone;
- target disposition;
- facing requirement;
- line-of-sight requirement;
- ground/directional geometry;
- movement/charge destination where applicable.

### Costs

- resource;
- amount;
- payment phase;
- interrupt refund behaviour.

### Effects

- reusable effect definitions;
- damage/healing scaling;
- attack type;
- damage school(s);
- weapon-damage contribution;
- threat coefficient;
- projectile behaviour;
- aura application;
- combat events.

---

## 44. Content Authoring Requirements — Auras

Aura authoring must support:

### Identity

- name;
- icon;
- description/generated tooltip;
- buff/debuff presentation;
- tags.

### Timing

- duration;
- tick interval;
- maximum stacks;
- stacking behaviour.

### Components

For each component:

- execution phase;
- target;
- reusable effect type;
- effect parameters.

### Triggered events

For each event:

- combat-event type;
- optional filters;
- chance where appropriate;
- target selection;
- one or more reusable effects.

### Removal

Where relevant:

- dispellability/removal category through explicit mechanics/tags;
- On Dispel behaviour;
- On Expire behaviour;
- persistence through death where explicitly required.

---

## 45. Technical Direction — Aura Architecture

The target architecture is conceptually:

    AuraDefinition
    ├─ Identity
    │  ├─ Name
    │  ├─ Icon
    │  ├─ Description
    │  └─ Tags
    ├─ Lifetime
    │  ├─ Duration
    │  ├─ Tick Interval
    │  ├─ Stack Behaviour
    │  └─ Max Stacks
    ├─ Components[]
    │  ├─ Phase
    │  ├─ Target
    │  └─ Reusable Effect
    └─ Events[]
       ├─ Combat Event
       ├─ Filters / Chance
       ├─ Target
       └─ Reusable Effects[]

Runtime instance state includes:

    AuraDefinition
    Caster
    Target
    Runtime Instance Identity
    Remaining Duration
    Stacks
    Per-stack timing where required
    Runtime effect state where required

The server owns this runtime state.

---

## 46. Existing Codebase — Foundations to Retain

The current implementation already provides several appropriate foundations.

### 46.1 SpellDefinition

Existing fields already support:

- cast time;
- cooldown;
- charges;
- Haste-scaled cooldown;
- GCD participation;
- range;
- channelling;
- movement while casting;
- resource costs;
- reusable components;
- tags.

These should be extended rather than replaced.

### 46.2 Spell components

The existing:

- SpellCastPhase;
- SpellEffectDefinition;
- SpellTargetDefinition;

composition model is compatible with this PDD.

### 46.3 Real-time aura timing

Current AuraDefinition already has:

- BaseDuration;
- BaseTickInterval;
- components;
- tags.

This is appropriate for the real-time RPE2-style aura architecture.

### 46.4 Server-authoritative managers

Existing server managers for:

- spellcasts;
- cooldowns;
- auto-attacks;
- auras;

provide a useful separation of responsibilities.

---

## 47. Existing Codebase — Required Changes

The following current implementation details conflict with or fall short of this PDD.

### 47.1 Aura condition modes

Remove the combat-aura concepts:

- Condition_Stack;
- Condition_Extend;
- ConditionStack.

Replace them with ordinary aura stacking semantics.

### 47.2 Incomplete aura stack implementation

Current ServerAuraManager.TryResolveStacking() only implements the condition-oriented enum branches while other declared modes fall through.

The aura manager must implement the final authored stacking model consistently.

### 47.3 Aura runtime ownership

The current aura implementation primarily finds existing auras by definition on the target.

It must instead preserve **definition + caster + target** runtime identity so multiple players can own independent copies correctly.

### 47.4 AuraBehaviour

Phase out the current bespoke AuraBehaviour pathway.

Equivalent behaviours should be represented through aura events and reusable effect handlers.

### 47.5 Aura event architecture

Add RPE2-style data-driven aura event handlers so an aura can react to combat events without a bespoke C# behaviour.

### 47.6 Aura event provenance

Periodic and triggered aura effects must emit combat events that carry their exact source aura/instance/caster.

This is required for future talent design.

### 47.7 Block

The current damage implementation treats Block as complete avoidance.

Implement Block + Deflection as defined by this PDD and the Items PDD.

### 47.8 Generic spell resistance prototype

The existing spell_resistance / spell_expertise full-resist logic is inconsistent with the intended per-school resistance model and should not be treated as final.

### 47.9 Avoidance calculation

The current prototype selects an avoidance type and then performs a second roll against that selected chance.

Combat avoidance must be rewritten to produce mathematically intentional Dodge/Parry/Block probabilities.

### 47.10 Threat

Current NPC threat simply adds raw incoming damage from player attackers.

It must be expanded to the authored coefficient/modifier/effective-damage model in this document, including:

- threat coefficient;
- Threat Generated modifier;
- healing threat;
- periodic threat;
- taunt override;
- deterministic targeting.

### 47.11 Minimum range and facing

Ability authoring/runtime needs explicit support for:

- minimum range/deadzone;
- facing constraints;
- corresponding server validation.

### 47.12 Line of sight

Line-of-sight requirements need an explicit ability/targeting contract and server validation path.

### 47.13 Projectile distinction

The current projectile implementation primarily delays a resolved hit to match projectile travel time.

The engine must distinguish that presentation model from genuinely physical/projectile-collision gameplay where an ability requests it.

---

## 48. Dependencies

This system directly depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md)
- [Class Design PDD](Class-Design-PDD.md)
- [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md)
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md)
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md)
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md)
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md)
- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md)
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md)
- [PvP PDD](PvP-PDD.md)
- [UI and UX PDD](UI-and-UX-PDD.md)
- [Movement and Traversal PDD](Movement-and-Traversal-PDD.md)
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md)

Where dependent documents remain placeholders, the combat rules explicitly locked here remain authoritative within this scope.

---

## 49. Open Design Decisions

The major combat architecture is considered designed.

The remaining open work is primarily tuning or dependent-system detail.

### 49.1 Numerical combat tuning

- exact miss/hit curves;
- exact critical curves;
- exact critical-damage/healing multipliers;
- level-difference penalties;
- Haste relationships;
- exact GCD tuning if changed from the current 1.0-second baseline.

### 49.2 Facing

- exact frontal attack arc;
- exact Parry arc;
- exact Block arc.

### 49.3 Range

- standard melee interaction distance;
- default Bow/Crossbow deadzones where individual abilities use them;
- exact latency buffers.

### 49.4 Damage mitigation

- Armour curve;
- resistance curve;
- multi-school mitigation rule;
- exact Block/Deflection formula.

### 49.5 Crowd control

- standard duration conventions;
- boss/elite immunity conventions;
- PvP diminishing returns if PvP adopts them.

### 49.6 Auras

- final complete set of stacking modes beyond Refresh Duration / Independent Duration if genuinely required;
- whether any components ultimately require independent tick intervals;
- global conventions for aura persistence through death;
- exact dispel taxonomies if content requires broader categories than tags.

### 49.7 Threat

- exact default threat coefficients beyond the 1.0 damage / 0.5 healing baseline;
- threat UI thresholds;
- whether any encounters/classes require a target-switch threshold rather than immediate highest-threat selection.

### 49.8 Death/resurrection

- combat-resurrection limits;
- resurrection resource state;
- world respawn/corpse flow.

These are not blockers to the combat architecture defined here.

---

## 50. Validation Criteria

The Combat system can be considered correctly implemented when the following are true.

### 50.1 Core targeting

- Hard-target combat functions as the default.
- Ground, directional, area, charge/leap and placed-object abilities can be represented.
- Limited physical projectile/skillshot mechanics can coexist with hard-target abilities.
- Camera, movement direction and target selection are not unnecessarily locked together.

### 50.2 Movement and casting

- Each cast/channel can author whether movement is allowed.
- Moving interrupts only abilities that require stationary casting.
- Ordinary incoming damage does not cause cast pushback.
- Explicit interrupts stop interruptible casts.

### 50.3 Facing/range

- Facing can be validated for attacks and defensive reactions.
- Parry and Block are directional by default.
- Dodge is not facing-dependent by default.
- Melee weapon categories do not receive different baseline reach.
- Abilities can author minimum range/deadzones.
- Line-of-sight validation is available.

### 50.4 Cooldowns and auto-attacks

- GCD and individual cooldowns are separate.
- Abilities can opt out of triggering the GCD.
- Charges work where authored.
- Auto-attacks do not trigger the GCD.
- Auto-attacks wait rather than cancel when temporarily out of range.
- Auto-attacks resolve through the ordinary combat-effect pipeline.

### 50.5 Hit and damage

- Melee/Ranged/Spell use their correct hit and critical stat families.
- Melee supports Dodge/Parry/Block.
- Ranged supports Dodge/Block.
- Spell attacks use Spell Hit and school mitigation rather than generic melee avoidance.
- Block reduces damage through Deflection instead of acting as automatic full avoidance.
- Armour and per-school resistances are applied consistently.
- Absorption reports pre-absorb, absorbed and final Health damage.

### 50.6 Healing

- Healing uses Healing Power rather than damaging Spell Power by default.
- Healing criticals use Healing Critical Chance.
- Effective healing and overhealing are distinguishable.

### 50.7 Auras

- Named authored auras are the sole normal representation of ongoing combat effects.
- No generic combat-condition layer is required.
- Each aura owns its own duration, tick interval, effects, stacks, tags and presentation.
- Aura runtime identity includes definition, caster and target.
- Multiple players can independently maintain the same aura definition on one target.
- Periodic aura effects are executed through reusable effect handlers.
- Aura events can execute reusable effects from combat events.
- Aura-produced combat events retain exact aura/source provenance.
- Condition_Stack, Condition_Extend and ConditionStack are absent from the final combat-aura architecture.
- Ordinary content does not require bespoke AuraBehaviour classes.

### 50.8 Group buffs and crowd control

- Party buffs retain named class identity.
- Crowd control is authored as named auras using reusable Control effects.
- Cleanse/dispel mechanics can remove exact auras or explicitly tagged groups.
- Group value can exist independently of personal DPS.

### 50.9 Threat

- Each hostile NPC maintains an independent threat table.
- Effective applied damage generates threat using an effect coefficient.
- Effective healing generates appropriate engagement threat.
- Threat Generated modifiers work.
- Taunt temporarily overrides threat targeting without destroying the underlying threat table.
- Normal threat does not decay during an active engagement.
- Reset/death clears appropriate NPC threat state.

### 50.10 Authority and feedback

- Server validation controls all combat outcomes.
- Latency buffers exist where necessary without making invalid attacks legal.
- Players can see their own aura durations/contributions.
- Combat logs preserve source, target, aura/ability provenance and meaningful resolution results.

---

## 51. Design Summary

Ninth Age uses conventional, readable hard-target MMORPG combat as its foundation while allowing selected spatial mechanics to create richer positioning and encounter design.

The combat model deliberately avoids action-MMO assumptions:

- no universal dodge roll;
- no frame-perfect block/parry requirement;
- no damage-based cast pushback;
- no weapon-category melee-reach micro-management.

Instead, tactical depth comes from:

- facing;
- positioning;
- authored range/deadzones;
- ground/directional abilities;
- class-specific active defence;
- interrupts;
- named auras;
- class buffs;
- threat;
- reusable combat interactions.

The aura system follows the RPE2 philosophy:

> **individual authored aura identity, reusable hard-coded effect/event mechanics.**

This preserves class identity and enables precise talent interactions while preventing a proliferation of bespoke runtime code.

The remaining combat work is predominantly numerical balance, class-specific content and encounter-specific rules rather than unresolved core architecture.
