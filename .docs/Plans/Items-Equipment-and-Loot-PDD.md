# Ninth Age — Items, Equipment and Loot Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Item identity, item power, equipment, weapons, armour, item requirements, special effects, sets, sockets, gems, enchanting, consumables, durability, loot generation and distribution, binding, storage, transfer, vendors and item presentation  
**Last updated:** 2026-10-04

---

## 1. Purpose and Authority

This document defines the intended item, equipment and loot systems for **Ninth Age** and is the ground truth for future design and implementation work in this area.

It owns the player-facing rules for:

- item identity and authored properties;
- equipment slots, weapons and armour;
- item level, rarity and craftsmanship interaction;
- equipment statistics and special effects;
- item requirements and activation state;
- equipment sets;
- sockets, gems and enchanting;
- consumable item structure where it materially interacts with the item system;
- durability and repairs;
- loot-table resolution and shared-loot distribution;
- binding and transfer restrictions;
- inventory expansion, banks, world stashes and item transfer;
- vendor behaviour where it directly concerns items;
- transmog and equipment-attached cosmetic options;
- item tooltip content and comparison behaviour.

The [Crafting System PDD](Crafting-System-PDD.md) remains authoritative for profession progression, exact-recipe mastery, cooperative crafting and the Standard → Superior → Masterwork → Legendary craftsmanship progression.

The [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md) will eventually own the detailed numerical character-stat and scaling curves. This document defines which statistics are intended to participate in equipment itemisation, but deliberately leaves unresolved numerical curves to that system where appropriate.

Where future implementation work conflicts with this document, this document takes precedence until the design is intentionally revised.

This document distinguishes between:

- **Locked design decisions** — requirements implementation should follow.
- **Open design decisions** — numerical or detailed behaviour that remains intentionally unresolved.

Existing prototype code is not authoritative merely because it already exists.

---

## 2. Design Pillars

### 2.1 Items are authored, not procedurally assembled

Equipment must have deliberate identity.

Dropped equipment does not roll random attributes, affixes, suffixes, sockets, qualities or generated property packages. Two instances of the same authored equipment definition are mechanically identical except for mutable instance state such as durability, binding, ownership, installed gems, enchantments and cosmetic configuration.

Randomness may determine **which authored item drops**. It must not determine what that item becomes after it drops.

### 2.2 Rarity, craftsmanship and item level are separate concepts

Rarity describes the class and provenance of the item.

Craftsmanship describes the quality of a crafted item or crafted enhancement.

Item level is the numerical power reference used for equipment budgeting.

These concepts may influence one another, but they must not be collapsed into one field.

### 2.3 Equipment should communicate intended use without unnecessary hard restrictions

Statistics, armour weight, weapon type and special effects should usually make an item's intended users apparent.

Normal equipment should generally rely on proficiency requirements rather than arbitrary class restrictions. Class requirements remain available for items that are genuinely class-specific.

### 2.4 The player should understand why an item is better or worse

Tooltips and comparison UI must expose meaningful differences rather than forcing players to manually subtract values.

Statistics should control their own tooltip presentation metadata so new stats can be added without special-case formatting code.

### 2.5 Loot generation and loot ownership are separate systems

A loot table answers:

> **What dropped?**

The active group loot mode answers:

> **Who receives it?**

Loot generation must not silently embed group-distribution policy.

### 2.6 Equipment degradation is a sink, not equipment destruction

Durability provides an ongoing economic cost and encourages repair interaction.

Broken equipment remains usable at sharply reduced effectiveness rather than being deleted or forcibly unequipped.

### 2.7 External enhancement should not become endless item upgrading

Gems, enchantments and temporary treatments may modify an item externally.

The base item itself does not undergo reforging, rerolling, recrafting, item-level upgrading or post-acquisition stat redistribution.

---

## 3. Item Identity and Authored State

### 3.1 Fully authored equipment

Every equipment definition explicitly authors the properties that define it.

There is no random generation of:

- stats;
- affixes or suffixes;
- sockets;
- socket polarity;
- item rarity;
- craftsmanship;
- requirements;
- set membership;
- special effects;
- weapon properties;
- armour properties;
- intrinsic defensive properties.

Mutable item-instance state may include:

- durability;
- binding state;
- ownership;
- installed gems;
- applied enchantment;
- cosmetic configuration;
- other explicitly designed instance state.

### 3.2 No post-acquisition base-item upgrading

Once an item instance is acquired, its authored base identity remains fixed.

There is no general system for:

- increasing its base item level;
- rerolling stats;
- reforging;
- replacing authored stats;
- recrafting the same acquired item into a higher tier;
- redistributing its item budget;
- procedurally adding sockets.

Gems, enchantments and temporary equipment treatments do **not** change the base item's item level.

### 3.3 Item categories

The item system must be capable of representing at least:

- equippable weapons;
- equippable armour;
- jewellery;
- trinkets;
- consumables;
- crafting materials and components;
- gems;
- enchantment items;
- bags;
- cosmetic accessories;
- key items;
- quest items;
- currency/reward references where loot systems require them.

Detailed profession ownership of crafted categories remains defined by the Crafting System PDD.

---

## 4. Item Level and Power Budget

### 4.1 Explicit item level

Every equippable item has an explicit item level.

The canonical relationship is:

> **Item Level = Required Level + Rarity Bonus + Craftsmanship Bonus + Raid Tier Bonus**

Where:

- **Required Level** provides the baseline character-level power reference.
- **Rarity Bonus** represents power associated with the item's rarity/source class.
- **Craftsmanship Bonus** represents additional power associated with crafted quality.
- **Raid Tier Bonus** permits successive endgame raid tiers at the same character-level requirement.

Ordinary non-crafted drops have no craftsmanship contribution.

### 4.2 Item budget

The working budget model is:

> **Item power budget derives primarily from Item Level × Slot Weight.**

The exact function and slot coefficients are intentionally unresolved until the character-stat progression curves are defined.

The budget may be consumed by:

- primary attributes;
- secondary attributes;
- armour;
- weapon damage;
- shield properties;
- passive effects;
- procs;
- active effects.

Special effects consume item budget. They are not free additions on top of an otherwise fully budgeted item.

### 4.3 Intrinsic properties

Some properties are intrinsic to an equipment category rather than ordinary generic secondary stats.

Examples include:

- weapon damage on weapons;
- armour on armour;
- Deflection on shields.

A two-handed weapon should broadly carry the expected combined hand-slot power rather than behaving like a one-handed item that merely blocks the off hand.

The single Trinket slot is deliberately more effect-oriented than an ordinary generic stat-stick slot.

### 4.4 Open numerical decisions

The following remain unresolved:

- exact rarity bonuses to item level;
- exact craftsmanship bonuses to item level;
- exact raid-tier bonuses;
- stat-budget curve by item level;
- slot weights;
- armour scaling;
- weapon damage/DPS/attack-speed budget relationships;
- special-effect budget conversion;
- shield Deflection scaling.

These are balance decisions, not blockers to the item-system architecture.

---

## 5. Rarity and Craftsmanship

### 5.1 Rarity

The canonical item rarity categories are:

- **Common** — ordinary crafting, vendors and ordinary loot.
- **Uncommon** — less common or more specialised crafting, vendor and loot items.
- **Rare** — rare crafting recipes and dungeon loot.
- **Epic** — raid loot.
- **Artifact** — legendary-tier historically unique named items.
- **Legendary** — legendary-tier cooperative crafted items.

Artifact and Legendary are distinct prestige categories.

**Artifact** denotes historically unique named equipment and provenance.

**Legendary** denotes the highest cooperative crafted provenance.

Any existing implementation rarity not listed here is provisional and is not made canonical by existing code.

### 5.2 Craftsmanship

Craftsmanship follows the Crafting System PDD:

> **Standard → Superior → Masterwork → Legendary**

Craftsmanship and rarity are separate.

A crafted item's craftsmanship tier does not replace its rarity, and an enchantment or gem's craftsmanship does not overwrite the craftsmanship or rarity of the equipment receiving it.

### 5.3 Tooltip craftsmanship label

For equipment tooltips:

- **Standard** craftsmanship is not displayed as a subtitle.
- **Superior** craftsmanship is shown immediately below the item name.
- **Masterwork** craftsmanship is shown immediately below the item name.

The exact treatment of a Legendary craftsmanship subtitle remains open because Legendary also exists as a rarity/provenance concept.

---

## 6. Equipment Slots

The canonical combat equipment slots are:

- Head
- Shoulders
- Chest
- Back
- Hands
- Waist
- Legs
- Feet
- Neck
- Ring ×2
- Trinket ×1
- Main Hand
- Off Hand

There is **no wrist slot**.

There is **one Trinket slot**.

Two-handed weapons occupy both hand slots.

Cosmetic-only items and accessory attachment points may exist separately from combat equipment slots and do not consume combat item budget.

---

## 7. Armour

### 7.1 Armour weight classes

Armour uses three equipment weight classes:

- **Light Armour**
- **Medium Armour**
- **Heavy Armour**

The system does not use Cloth / Leather / Mail / Plate as its canonical mechanical categories.

Armour weight describes the protective construction and overall defensive class of the item rather than declaring a single material.

Examples:

- Medium Armour may use substantial chainmail.
- Heavy Armour may combine chainmail with greater plate coverage.

This allows visual and crafting-material flexibility without multiplying mechanical armour categories.

### 7.2 Restrictions

Normal armour should generally be restricted by armour proficiency rather than class.

Class restrictions remain available for genuinely class-specific equipment.

---

## 8. Weapons and Off-Hand Equipment

### 8.1 One-handed weapons

- Sword
- Axe
- Mace
- Dagger
- Rapier
- Fist Weapon

### 8.2 Two-handed weapons

- Greatsword
- Greataxe
- Maul
- Polearm
- Staff

### 8.3 Ranged weapons

- Bow
- Crossbow
- Wand
- Spellbook

The exact combat behaviour of Wand and Spellbook belongs to the Combat and Abilities design work rather than this PDD.

### 8.4 Off-hand equipment

- Shield
- Focus

### 8.5 Parry and weapon stats

Weapons may carry Parry Rating.

The item system should avoid unnecessary stat/slot prohibitions unless a restriction exists for a clear mechanical reason.

---

## 9. Shields, Block and Deflection

Shields have a deliberately specialised tanking budget.

A shield may allocate appropriate combinations of:

- Stamina;
- Armour;
- Block Rating;
- Parry Rating;
- Dodge Rating;
- Critical Resistance;
- elemental/magical resistances;
- appropriate offensive or primary stats.

### 9.1 Block Rating

**Block Rating** controls the chance that an incoming eligible attack is blocked.

### 9.2 Deflection

**Deflection** controls how much a successful block reduces the attack.

Deflection is an intrinsic shield property, not a generic secondary stat intended for allocation across unrelated equipment.

Shields may be authored to trade Block Rating against Deflection.

The exact mathematical representation of Deflection, including whether its final reduction is flat, percentage-based or hybrid, remains open.

---

## 10. Itemisable Character Statistics

### 10.1 Primary attributes

Equipment uses explicit primary attributes:

- Strength
- Dexterity
- Intelligence
- Willpower
- Stamina

There is no generic adaptive "main stat" that changes according to the equipping character.

### 10.2 Offensive stat families

The three-way offensive split is retained.

**Melee**

- Melee Attack Power
- Melee Hit Rating
- Melee Critical Rating
- derived Melee Critical Chance

**Ranged**

- Ranged Attack Power
- Ranged Hit Rating
- Ranged Critical Rating
- derived Ranged Critical Chance

**Spell**

- Spell Power
- Spell Hit Rating
- Spell Critical Rating
- derived Spell Critical Chance

Spell Power applies to damaging spell effects rather than healing.

The separate Melee, Ranged and Spell Hit/Critical families are intentional. They help equipment communicate intended use and preserve meaningful hit requirements for different attack modes.

### 10.3 Healing statistics

The intended healing-specific statistics are:

- **Healing Power**
- **Healing Critical Rating**
- derived **Healing Critical Chance**

These may require additions to the current actor-stat implementation.

### 10.4 Defensive statistics

Normal defensive itemisation may use:

- Armour
- Dodge Rating
- Parry Rating
- Block Rating
- Critical Resistance
- Air Resistance
- Earth Resistance
- Fire Resistance
- Water Resistance
- Light Resistance
- Shadow Resistance

There is no generic Defence or Resilience catch-all stat.

### 10.5 Resource and utility statistics

Normal equipment may use appropriate resource-oriented statistics such as:

- Haste Rating;
- HP Regeneration;
- Mana Regeneration;
- Resource Regeneration where a class/resource supports it;
- Resource Generation where a class/resource supports it.

The following can exist as gameplay statistics or authored special effects but should **not** normally consume routine equipment stat budget:

- Cooldown Recovery;
- Movement Speed;
- Threat Generation;
- Threat Reduction;
- Life Steal / Leech;
- Healing Received;
- Control Resistance;
- Resource Cost Reduction.

These are reserved for deliberate special items/effects rather than ordinary stat allocation.

### 10.6 Few hard stat/slot restrictions

The item system should not maintain an unnecessarily large matrix of allowed stat/slot combinations.

Examples explicitly permitted include:

- Parry Rating on weapons;
- Armour on rings.

Authored identity and budget should do most of the work.

---

## 11. Item Requirements, Proficiencies and Activation

### 11.1 Extensible condition model

Item requirements are authored through the existing polymorphic **ConditionDefinition** framework rather than through separate hard-coded requirement fields.

The requirement system must be readily extensible.

Condition types may include:

- character level;
- class;
- race;
- weapon proficiency;
- armour proficiency;
- location;
- future reputation, quest-state, faction, world-state or other authored conditions.

All conditions attached to an item must evaluate successfully for the item to be active.

### 11.2 Initial equip

An item cannot initially be equipped if its current equip requirements are not satisfied.

### 11.3 Dynamic deactivation

Once equipped, requirements continue to matter.

If an equipped item's requirements later become false, the item:

- **remains equipped**;
- is **not automatically returned to inventory**;
- becomes **inactive**.

An inactive item contributes no combat power while its requirements remain unmet.

This includes, as applicable:

- base item stats;
- intrinsic combat properties;
- passive effects;
- procs;
- active-use equipment effects;
- set membership;
- installed gem effects;
- enchantment effects.

When the requirements become valid again, the item reactivates automatically.

This enables authored contextual equipment such as items that function only in a particular location or state.

### 11.4 Runtime evaluation

Requirement re-evaluation should occur when relevant state changes rather than through wasteful unconditional per-frame polling.

The server remains authoritative for whether an equipped item is active.

### 11.5 Existing codebase direction

The existing Ninth Age code already provides:

- abstract ConditionDefinition evaluation;
- tooltip lines from conditions;
- polymorphic SerializeReference condition lists on ItemDefinition;
- editor discovery of ConditionDefinition subclasses.

Future item implementation should extend this architecture rather than replace it with a parallel item-only requirement system.

---

## 12. Special Equipment Effects

### 12.1 Slot restrictions

The following special-effect identities are locked:

- **Direct ability modification** → Trinket only.
- **On-hit effects** → Weapons only.
- **On-cast effects** → Rings only.
- **Active-use effects** → Hands, Rings and Trinkets.

Trinkets are therefore the only normal equipment slot that can directly modify an ability.

### 12.2 Rarity expectations

- **Common:** no special effects of these categories.
- **Uncommon:** no special effects of these categories.
- **Rare:** only a small number of higher-level Rare items may contain them.
- **Epic:** special effects become substantially more common.
- **Artifact:** special effects are expected where appropriate to identity.
- **Legendary:** special effects are expected where appropriate to crafted prestige.

A special effect still consumes item budget.

Ordinary passive statistics or unusual authored stat combinations are not automatically treated as special procs.

---

## 13. Equipment Sets

Item sets exist.

### 13.1 Membership

Set membership is explicitly authored.

An item can belong to **only one equipment set**.

Bonuses activate according to the **number of equipped active set members**, not exact named-piece combinations beyond membership in the set.

Inactive equipment does not contribute to set thresholds.

### 13.2 Pre-raid sets

Pre-raid set bonuses should remain comparatively generic and limited.

Appropriate examples include:

- primary attributes;
- secondary stats;
- resistances;
- regeneration;
- other broad bonuses.

They should not materially rewrite class rotations or core class mechanics.

### 13.3 Raid-tier sets

Raid-tier set bonuses may be stronger and more specialised.

They may meaningfully influence builds or rotations through broad combat behaviour, resource interactions, conditional bonuses, procs and statistics.

The restriction that direct equipment-based ability modification belongs to Trinkets still applies. A set bonus should not merely bypass the Trinket rule by directly rewriting specific abilities.

### 13.4 Masterwork set

Eligible Masterwork crafted equipment belongs to a shared generic **Masterwork set**.

Its purpose is to reward continued creation and use of Masterwork equipment without making Masterwork permanently mandatory over later prestige equipment.

Percentage-based bonuses are preferred where they scale only the equipped Masterwork pieces themselves.

A strong design direction is to increase the secondary-stat value contributed by equipped Masterwork items rather than grant a permanent global raw damage multiplier.

Exact set thresholds and bonuses remain open.

---

## 14. Binding

The item system supports exactly five binding states:

- **Bind on Equip**
- **Bind to Account on Equip**
- **Bind on Pickup**
- **Bind to Account on Pickup**
- **Does Not Bind**

Binding has two dimensions:

1. **When** the restriction occurs — pickup or equip.
2. **Scope** of the restriction — character or account.

Items using Does Not Bind remain transferable subject to their other item-category restrictions.

The Crafting System PDD requires finished Masterwork equipment to be account-bound and Masterwork components to remain personally bound. The exact pickup/equip event for a particular finished Masterwork item may be authored using the account-binding states, but it must not become freely tradeable on the open market.

Binding defaults for every possible acquisition source are not globally hard-coded by this PDD; individual content may author the appropriate binding state unless another authoritative PDD specifies one.

---

## 15. Unique-Equipped Items

Items may be flagged **Unique-Equipped**.

A character cannot equip more than one instance of the same Unique-Equipped item simultaneously.

Unique-Equipped does **not** prevent:

- owning additional copies;
- storing additional copies;
- receiving an additional copy where other rules permit it.

A more general shared Unique-Equipped Group mechanism may be added if future content requires mutually exclusive different items, but exact-item Unique-Equipped is the currently locked requirement.

---

## 16. Sockets and Gems

### 16.1 Authored sockets

Sockets are explicitly authored on items.

Sockets are never randomly added to dropped equipment.

Every socket has one of six polarities corresponding exactly to the six magic schools:

- Fire
- Water
- Earth
- Air
- Light
- Shadow

### 16.2 Gem insertion

Gems also have a polarity.

A gem may be inserted into **any** socket. A polarity mismatch does not prohibit insertion.

Polarity affects magnitude rather than eligibility.

### 16.3 Per-item polarity state

Gem magnitude is determined from the polarity match state of the **whole item**, not independently per socket.

For an item with installed gems:

- **None** — no installed gems match their corresponding socket polarity.
- **Partial** — at least one, but not all, installed gems match.
- **Full** — every installed gem matches.

The resulting None / Partial / Full state applies a discrete magnitude multiplier to **all gems installed on that item**.

For a three-socket item:

- 0/3 matches = None
- 1/3 matches = Partial
- 2/3 matches = Partial
- 3/3 matches = Full

Exact magnitude multipliers remain a balance decision.

### 16.4 Gem craftsmanship

Gems use the crafting progression:

> **Standard → Superior → Masterwork → Legendary**

Their behaviour by craftsmanship is:

- **Standard gems:** fixed authored polarity.
- **Superior gems:** polarity chosen when crafted.
- **Masterwork gems:** polarity chosen when crafted.
- **Legendary gems:** provide the global polarity override defined below.

### 16.5 Legendary gem global polarity

A character may have **only one Legendary gem equipped across the entire character**.

When a Legendary gem is equipped:

- its polarity overrides the socket polarity of **every socket on every equipped item**;
- all installed gems on equipped items are evaluated against those overridden polarities;
- each equipped item's None / Partial / Full state is still calculated independently;
- unequipping the Legendary gem restores every item's authored socket polarities.

This makes the Legendary gem a character-level build decision rather than merely a stronger gem in one socket.

### 16.6 Item level

Installed gems do not alter the base item's item level.

### 16.7 Open gem decisions

Still unresolved:

- exact None / Partial / Full multipliers;
- socket-count expectations by slot/item level;
- whether removing or replacing a gem destroys the previous gem;
- exact UI presentation of the global Legendary polarity override.

---

## 17. Enchanting

Enchantments are **items**, not abstract upgrade records.

### 17.1 Eligible equipment

All equippable slots may be enchanted **except**:

- Trinket;
- Waist.

### 17.2 Authored effects

Enchantments are fully authored and may provide:

- direct statistics;
- explicit special effects.

Enchantments do not roll random properties.

Applying a new enchantment replaces the existing enchantment on that item.

### 17.3 Craftsmanship

Enchantments participate independently in:

> **Standard → Superior → Masterwork → Legendary**

Enchant craftsmanship may affect enchantment strength or effectiveness.

The equipment item's own rarity and craftsmanship remain unchanged.

### 17.4 Item level

Enchantments do not change the base item's item level.

There is no general rule preventing a high-craftsmanship enchantment from being applied to a lower-rarity or lower-craftsmanship item unless a future explicit requirement is authored.

---

## 18. Consumables

Consumable craftsmanship depends on expected lifetime.

### 18.1 Short-lifetime consumables

Short-lived consumables exist at **Standard craftsmanship only**.

Examples include:

- potions;
- bombs and grenades;
- emergency remedies;
- deployables and gadgets;
- other brief authored effects;
- scrolls.

### 18.2 Long-duration consumables

Long-duration buff consumables may progress through:

> **Standard → Superior → Masterwork**

They do not use Legendary craftsmanship by default.

Long-duration consumable categories may include:

- **Food** — sustained physical/stat-oriented buffs.
- **Drinks** — sustained resource or regeneration effects.
- **Tonics** — alchemical long-duration physiological/combat effects.
- **Incenses** — mystical, resistance or group-oriented effects.
- **Oils / Coatings** — long-duration equipment or weapon treatments.
- **Charms / Wards** — temporary magical protection or utility.

Exact coexistence and stacking rules between these categories remain open.

### 18.3 Scrolls

Scrolls are Standard-quality short-lived consumables produced through Inscription.

They do **not** teach permanent abilities and are not generic substitute class abilities.

Scrolls instead **complement class abilities and reward party composition**.

A scroll may reference one or more explicitly authored class abilities and establish a temporary external effect that reacts when those abilities are used.

Example:

> A Scroll of Blizzard may grant an authored benefit to the caster or party when Blizzard is cast.

Depending on the scroll, the triggered effect may benefit:

- the caster;
- other party members;
- enemies;
- another explicitly authored target set.

This does not violate the Trinket-only rule for equipment ability modification because the scroll is a temporary consumable effect, not an equipped ability modifier.

The design principle is:

> **Scrolls amplify party composition.**

---

## 19. Durability and Repair

### 19.1 Durability loss

Equipment durability may be lost through:

- player death;
- taking damage;
- weapon use.

Exact durability-loss rates remain tuning parameters.

### 19.2 Maximum durability

Maximum durability is derived from:

- equipment slot;
- rarity;
- craftsmanship.

There is no universal maximum durability shared by all items.

### 19.3 Zero durability

At **0 durability**, the item remains equipped and usable but becomes **80% less effective**.

It therefore retains **20% effectiveness** rather than becoming completely inactive.

Weapons at zero durability can still attack.

The item must not be automatically unequipped at zero durability.

Exactly how binary or non-numeric effects degrade at zero durability remains an implementation/balance detail to define; the core requirement is that the item remains usable while its effective combat contribution is heavily reduced.

### 19.4 Artifact exception

**Artifact items do not have durability.**

They do not degrade and do not require repair.

### 19.5 Repair NPCs

Full repair services are provided by **specialist repair NPCs**, not every ordinary merchant.

A specialist repair NPC may also have an authored vendor inventory.

### 19.6 Player repairs

Players with appropriate crafting professions may repair **their own equipment** in the slots/material categories appropriate to those professions.

The exact profession-to-slot mapping remains to be authored.

This does not create a general player repair-service trade system by default.

### 19.7 Repair cost

NPC repair cost scales with:

- durability missing;
- item level;
- rarity;
- craftsmanship.

Exact coefficients remain a balance decision.

Durability repair is intended to be a meaningful recurring coin sink.

---

## 20. Loot Sources and Loot Tables

### 20.1 Explicitly authored source identity

Every NPC or boss references explicitly authored loot content.

Multiple NPCs may share the same authored loot table.

This is sufficient to represent archetypal loot without introducing automatic archetype generation.

Examples:

- multiple caster enemies may reference one caster-oriented authored table;
- multiple melee enemies may reference one melee-oriented authored table;
- bosses may reference bespoke authored boss tables.

The system must not automatically infer or filter loot based on an NPC archetype when the content designer can simply assign the intended authored table.

### 20.2 Loot resolution model

The loot-table model follows the proven RPE2 structure.

A loot table contains:

- a **draw count**;
- an ordered collection of explicitly authored entries.

Each entry contains:

- a unique entry identifier;
- a reward type;
- a positive relative weight;
- a minimum quantity;
- a maximum quantity;
- an item/currency reference where applicable.

Supported reward outcomes are:

- **Item**
- **Currency**
- **Nothing**

Weights are relative and do not need to total 100.

### 20.3 Draws

Each table draw independently selects one weighted entry.

A table with multiple draws may select the same entry more than once.

Repeated identical stackable/reward outcomes may be merged into a resulting quantity where appropriate.

The **Nothing** outcome provides a straightforward way to represent a draw that yields no reward without adding a second probability system.

### 20.4 Direct rewards

The system also supports **direct rewards** that specify a concrete authored item/currency and quantity without a weighted table roll.

This is appropriate for guaranteed rewards.

A loot source may therefore combine deterministic rewards and one or more loot-table resolutions where content requires it.

### 20.5 Validation

Loot authoring must fail validation rather than silently normalise malformed probability semantics.

Validation must reject, as applicable:

- non-positive or non-finite weights;
- invalid quantity ranges;
- missing/invalid referenced items or currencies;
- duplicate entry identifiers;
- invalid draw counts;
- empty loot tables;
- unsupported reward types.

### 20.6 Nested tables

Nested loot tables are not required for the initial system. The authored Direct-or-Table model should remain simple unless future content demonstrates a genuine need for table nesting.

### 20.7 Item identity after roll

Loot randomness chooses an authored reward and quantity only.

It never rolls:

- the item's stats;
- rarity;
- craftsmanship;
- sockets;
- requirements;
- effects;
- affixes;
- any other base item property.

---

## 21. Shared Loot Distribution

Ninth Age does **not** use personal loot as its core loot model.

Shared loot is resolved first, then distributed according to the active group loot mode.

### 21.1 Supported group loot modes

- **Need / Greed**
- **Master / Leader Assignment**
- **Round Robin**
- **Free-for-All**
- **Random Assignment**

**Need / Greed is the default.**

The party or raid leader may change the active loot mode.

### 21.2 Need / Greed

For each shared item requiring a roll, players in the current loot group may choose:

- Need
- Greed
- Pass

Rules:

- Need takes priority over Greed.
- Need and Greed each roll 1–100.
- Highest roll in the highest-priority non-empty pool wins.
- Exact ties automatically reroll between tied players only.
- If nobody chooses Need, Greed rolls resolve.
- If all players pass, the item remains on the corpse.
- The default response timer is **30 seconds**.
- No response at timeout counts as **Pass**.
- There is no class, role, proficiency, level or "can use" validation on the Need choice.
- The participant list is snapshotted when the roll begins so later group changes do not rewrite an active roll.
- Each distinct non-stackable item is resolved independently.
- A stack of identical stackable items is rolled as one stack.
- Binding follows the item's normal binding rule; the loot mode does not add a special binding rule.

If all players pass, the unclaimed item follows the normal corpse-despawn rule.

### 21.3 Master / Leader Assignment

The designated loot controller directly assigns each shared item to a group member.

### 21.4 Round Robin

Shared items rotate through group members in order.

### 21.5 Free-for-All

Any member of the relevant loot group may take available shared loot.

### 21.6 Random Assignment

Each shared item is assigned uniformly at random to one member of the loot group.

### 21.7 No contribution eligibility system

The loot system does not require elaborate damage, healing, proximity or participation scoring to determine who is allowed to receive shared loot.

Distribution operates over the relevant current loot/group context.

---

## 22. Corpse Loot and World Item Handling

### 22.1 No free-dropping items

Players cannot freely drop persistent item objects into the world.

Item transfer occurs through designed systems such as:

- direct trade;
- mail;
- work orders;
- loot interactions.

There is no general persistent ground-item system for discarded equipment or materials.

### 22.2 Corpse loot

Loot not yet claimed remains associated with its corpse/source.

When the corpse despawns, any unclaimed loot on it is **destroyed**.

This includes items for which all players passed in Need / Greed and which were not subsequently claimed before despawn.

---

## 23. Inventory, Bags, Banks and Stashes

### 23.1 Existing inventory foundation

Ninth Age already has an inventory and stacking foundation.

This PDD does not require replacing that foundation simply to redesign inventory from scratch.

### 23.2 Bags

Craftable bags expand player inventory capacity.

Bag properties may author:

- additional capacity;
- item-category restrictions where desired.

Exact bag sizes and restriction catalogue remain content/balance decisions.

### 23.3 Banks

Banks exist in **major cities**.

Bank storage is globally shared between bank locations:

- an item deposited at one bank may be withdrawn at another bank;
- banks are conventional long-term city storage rather than geographically local containers.

Exact bank capacity and expansion rules remain open.

### 23.4 World stashes

**Stashes** are fixed, specific locations in the world.

Each stash stores exactly **5 stacks of items** for the player.

Stash contents are local to that exact stash:

- they are not shared with other stashes;
- they are not shared with banks;
- the player must physically return to that stash to access its contents.

Stashes provide geographically meaningful preparation/storage in remote areas.

The intended distinction is:

- **Inventory** = carried capacity.
- **Bank** = globally accessible city storage.
- **Stash** = small persistent local storage.

---

## 24. Item Transfer

### 24.1 Direct trade

Non-bound items may be transferred through direct player-to-player trade subject to item-category restrictions.

Binding rules cannot be bypassed through trade.

### 24.2 Mail

Any **non-bound item** may be mailed unless its category explicitly forbids mailing.

Quest items cannot be mailed.

Mail supports:

- multiple item attachments;
- sending coin where appropriate;
- **Cash on Delivery (CoD)**.

Binding restrictions cannot be bypassed through mail.

Mail expiry/return timing remains an Economy/Communication-system detail to resolve.

### 24.3 Work orders

Work orders support commissioned crafting.

Rules:

- the commissioning player supplies required crafting materials **up front**;
- supplied materials are reserved/consumed by the work order rather than relying on the crafter's ordinary inventory at completion time;
- the crafter performs the specified recipe/order;
- the resulting commissioned item is returned to the customer **by mail**;
- payment/commission may be part of the work order;
- normal binding and craftsmanship rules still apply.

Public and private work orders are both supported under the Economy, Trade and Markets PDD. Public orders are browsable by eligible crafters; private orders target a specified crafter. Customer-supplied materials remain server-controlled in escrow while the order is active.

---

## 25. Key Items and Quest Items

### 25.1 Key items

Key items are stored in a dedicated **keyring**.

They do not consume normal inventory capacity.

Detailed transfer/destruction rules for every key type remain open unless authored elsewhere.

### 25.2 Quest items

Quest items occupy normal inventory space.

Quest items:

- can be destroyed;
- cannot be mailed.

Quest-item reacquisition behaviour after destruction belongs to quest design and remains to be defined.

---

## 26. Vendors and Item Economy Behaviour

### 26.1 Authored stock

Vendors have explicitly authored inventories.

There is no universal global vendor catalogue.

Vendor inventories may contain appropriate combinations of:

- ordinary goods;
- equipment;
- consumables;
- crafting materials;
- recipes;
- bags;
- cosmetics;
- other authored items.

### 26.2 Regional availability

Vendor inventory and relevant recipe/material availability may vary by:

- settlement;
- region;
- faction;
- culture;
- other authored world context.

This supports regional economic identity without procedural item generation.

### 26.3 Limited stock

Vendors may have limited-stock items.

Limited-stock entries may replenish over time or according to other explicitly authored restock rules.

### 26.4 Buyback

Vendors support buyback of recently sold items.

Exact buyback history size and expiry rules remain open.

### 26.5 Vendoring unwanted equipment

Vendoring items is an intended source of coin.

Vendor sale value should represent a substantial value loss relative to acquisition/crafting cost.

The system should not make buying/crafting and reselling economically neutral.

Exact sell-value formulas remain open.

### 26.6 Repair vendors

Specialist repair NPCs may also sell explicitly authored goods.

Repair availability does not imply that every general vendor can repair equipment.

### 26.7 No salvage or disenchant material loop

There is **no general salvaging system**.

There is **no disenchanting system** that destroys unwanted equipment into Enchanting materials.

The player may simply sell unwanted equipment.

This avoids adding another broad crafting-material source and keeps vendoring relevant.

Enchanting materials must come from other explicitly designed sources.

---

## 27. Transmog and Cosmetic Accessories

### 27.1 Transmog

Transmog exists.

An appearance override is cosmetic only and does not alter:

- stats;
- item level;
- rarity;
- craftsmanship;
- sockets;
- enchantment;
- set membership;
- requirements;
- combat behaviour.

### 27.2 Item cosmetic options

Some equipment may expose additional togglable cosmetic elements.

Examples include Waist-item options such as:

- warhorns;
- pouches.

Not every item needs attached cosmetic options.

### 27.3 Independent cosmetic accessories

Cosmetic accessories may also exist as independent collectible items/unlocks.

They may be:

- crafted;
- found;
- purchased from vendors;
- awarded through quests, achievements, reputation, bosses, events or other authored sources.

Compatibility is explicitly authored.

Examples:

- a warhorn may attach to a compatible Waist appearance;
- a weapon tassel may require a compatible weapon category.

Cosmetic accessories carry no combat power.

### 27.4 Unlock scope

Cosmetic unlocks may be either:

- character-specific;
- account-wide.

The scope is authored for the unlock rather than globally forced into one model.

### 27.5 Persistence and tooltip behaviour

Saved appearance/transmog configuration must persist.

Availability of cosmetic options/accessories does **not** need to be displayed in the normal combat item tooltip.

---

## 28. Item Tooltips

The RPE2 item-tooltip hierarchy is the presentation baseline, extended for Ninth Age-specific systems.

### 28.1 Core ordering

An item tooltip should present, as applicable:

1. rarity-coloured item name;
2. Superior/Masterwork craftsmanship subtitle where applicable;
3. item level;
4. binding state;
5. quest/unique-equipment state where applicable;
6. equipment slot and type;
7. intrinsic weapon/armour/shield properties;
8. base statistics;
9. requirements and proficiency conditions;
10. passive/proc/active effects;
11. sockets and installed gems;
12. enchantment;
13. set membership and set bonuses;
14. durability;
15. flavour/description text;
16. sell value.

Not every item uses every line.

### 28.2 Requirements

Requirements come from the authored ConditionDefinition objects and should provide their own human-readable tooltip lines.

Unmet requirements must be clearly distinguishable.

An already-equipped but inactive item must clearly communicate why it is inactive.

### 28.3 Stat-driven tooltip formatting

Item stat formatting must be driven partly by **ActorStatDefinition** metadata rather than hard-coded inside the item-tooltip implementation.

Following the RPE2 model, stat definitions should be able to control at least:

- item-tooltip display mode;
- tooltip ordering priority;
- tooltip colour.

Required display modes should cover the equivalent of:

- signed value;
- plain value;
- signed percentage;
- Equip-style value;
- Equip-style percentage.

Examples:

- +20 Strength
- 125 Armour
- +4% appropriate percentage stat
- Equip: ... where the stat/effect definition requires that presentation

New stats should not require item-tooltip special-case code solely to format their values correctly.

### 28.4 Set presentation

Set membership and active/inactive threshold bonuses should be shown in the item tooltip.

Exact typography/colour treatment remains a UI detail.

### 28.5 Polarity presentation

The tooltip must expose enough information for the player to understand:

- authored socket polarities;
- installed gem polarities;
- the item's current None / Partial / Full match state;
- the resulting gem magnitude effect.

Exact line formatting and the presentation of a global Legendary gem override remain open.

### 28.6 Durability

Durability is shown on items that use durability.

Artifact items omit durability because they do not possess it.

Exact placement/colour changes for low or zero durability remain a UI detail.

### 28.7 Cosmetics

Normal combat tooltips do **not** list available cosmetic accessory/options merely because the item supports them.

---

## 29. Item Comparison

Hovering an equippable item while an item is equipped in the relevant slot should show the **delta**.

The comparison UI should present net changes rather than simply duplicating the full equipped-item tooltip.

### 29.1 Numeric deltas

Where meaningful, show net differences for values such as:

- primary attributes;
- secondary attributes;
- Armour;
- Deflection;
- resistances;
- weapon damage;
- other numeric intrinsic properties.

Positive and negative changes should be visually distinguishable.

The same stat-definition formatter used by normal item tooltips should also govern comparison formatting.

### 29.2 Non-numeric changes

Effects that cannot be represented as a useful scalar delta should be called out separately.

This includes changes such as:

- gained/lost procs;
- active effects;
- set-threshold changes;
- gem-state changes;
- enchantment changes.

### 29.3 Multi-slot comparison

For Rings, compare against the ring slot that would actually be replaced.

For a two-handed weapon, the comparison must account for losing both currently equipped hand items where applicable.

The comparison must reflect the resulting equipment state, not merely compare one definition against one arbitrary slot.

---

## 30. Persistence

The following item state must persist as appropriate across logout, disconnect and server restart:

- item identity;
- quantity/stack state;
- inventory slot/storage location;
- equipment slot;
- binding/ownership state;
- durability;
- installed gems;
- applied enchantment;
- cosmetic/transmog configuration;
- bank contents;
- stash contents by stash identity;
- relevant mail/work-order item state.

Base authored item definitions should be referenced rather than duplicated into persistent character state wherever practical.

Persistent state must not rely on client authority for ownership or combat-relevant item properties.

---

## 31. Multiplayer and Authority

The server is authoritative for:

- item ownership;
- inventory/equipment mutation;
- whether an item may initially be equipped;
- whether equipped item conditions are satisfied;
- whether an equipped item is active;
- stat/effect contribution from equipment;
- durability changes;
- repairs;
- loot-table resolution;
- loot-distribution outcomes;
- binding transitions;
- trades;
- mail attachment transfer;
- bank/stash mutations;
- work-order material reservation and output delivery.

The client may predict/present interactions where appropriate, but it must not be able to create, strengthen, transfer or reactivate an item without server validation.

---

## 32. Content Authoring Requirements

Designers need authoring tools for the following.

### 32.1 Item definitions

An equippable item should be able to author:

- display identity and icon;
- rarity;
- craftsmanship where applicable;
- required level through conditions;
- item level;
- valid slot(s);
- weapon/armour/off-hand category;
- intrinsic weapon/armour/shield properties;
- stats;
- special effects;
- requirements/conditions;
- set membership;
- socket count and polarities;
- binding state;
- Unique-Equipped;
- durability parameters where applicable;
- sell value;
- cosmetic appearance/options where relevant.

### 32.2 Conditions

Condition authoring should remain polymorphic and editor-discovered.

Adding a new ConditionDefinition subtype should not require redesigning ItemDefinition.

### 32.3 Stat presentation metadata

ActorStatDefinition authoring should expose item-tooltip presentation fields for:

- display mode;
- priority;
- colour.

### 32.4 Loot tables

Loot-table tooling should expose:

- table identity;
- draw count;
- entry list;
- entry identifier;
- reward type;
- referenced item/currency;
- weight;
- minimum quantity;
- maximum quantity;
- validation feedback.

Probability semantics should be inspectable and deterministic from the authored weights.

### 32.5 Vendors

Vendor content authoring should support:

- inventory entries;
- prices;
- finite/unlimited stock;
- restock rules;
- regional/faction placement;
- repair-service capability where applicable.

---

## 33. Technical Constraints that Materially Affect Product Design

### 33.1 Reuse the existing condition architecture

The current code already models ItemDefinition conditions as polymorphic ConditionDefinition entries and already has Condition_ActorLevel and Condition_PlayerClass.

Implementation should extend this model with race, proficiency, location and future conditions rather than add parallel requirement fields.

The current equipment path does not yet enforce these conditions and the current stat manager applies equipped item stats without checking item activation. Those are implementation gaps relative to this PDD.

### 33.2 Data-driven stat presentation

The current Ninth Age ItemTooltipFactory formats item stats generically, while RPE2 demonstrates the intended data-driven pattern using stat-owned display mode, priority and colour.

Ninth Age should move toward the stat-owned presentation model rather than grow item-tooltip conditionals for each stat type.

### 33.3 Existing inventory foundation

The current inventory/stacking implementation is a foundation to extend, not a requirement to rewrite.

### 33.4 Current enums are provisional where they conflict with this PDD

Existing item-slot, rarity, weapon-type, binding or other enums may not yet contain the full canonical design in this document.

Implementation must migrate them toward the PDD rather than treating the current enum shape as a product constraint.

---

## 34. Dependencies

This system directly depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md)
- [Crafting System PDD](Crafting-System-PDD.md)
- [Class Design PDD](Class-Design-PDD.md)
- [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md)
- [Combat System PDD](Combat-System-PDD.md)
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md)
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md)
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md)
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md)
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md)
- [UI and UX PDD](UI-and-UX-PDD.md)
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md)

Where those documents are still placeholders, this PDD's locked item-specific rules remain authoritative within its scope.

---

## 35. Open Design Decisions

The major structure of the item system is considered designed.

The remaining open work is primarily numerical balance or detailed UX/implementation behaviour.

### 35.1 Item power

- rarity item-level bonuses;
- craftsmanship item-level bonuses;
- raid-tier bonuses;
- stat-budget curve;
- slot weights;
- armour scaling;
- weapon damage/DPS/speed relationships;
- effect-budget costing.

### 35.2 Shields

- exact Deflection units and formula;
- exact relationship between Deflection, Block Rating and item level.

### 35.3 Sets

- exact Masterwork set thresholds and bonuses;
- exact pre-raid and raid set thresholds.

### 35.4 Binding/content defaults

- source-by-source binding defaults where content design wants conventions rather than explicit per-item authoring.

### 35.5 Durability

- durability-loss rates for death, damage and weapon use;
- maximum-durability coefficients by slot/rarity/craftsmanship;
- repair-cost coefficients;
- exact handling of non-numeric effects at zero durability;
- exact profession-to-slot player-repair mapping.

### 35.6 Gems

- None / Partial / Full multipliers;
- socket-count expectations;
- removal/replacement fate of old gems;
- Legendary override presentation.

### 35.7 Consumables

- long-duration consumable stacking/coexistence rules;
- exact duration conventions.

### 35.8 Storage and transfer

- bag capacities;
- bank capacity/expansion;
- mail expiry/return;
- work-order visibility and acceptance flow;
- behaviour when automatic loot delivery finds no inventory space.

### 35.9 Vendors

- vendor value formula;
- limited-stock restock schedules;
- buyback history/expiry.

### 35.10 Tooltips

- exact set-bonus typography;
- polarity line formatting;
- low/broken durability colour treatment;
- Legendary craftsmanship subtitle treatment;
- crafted-by/provenance line, if adopted.

### 35.11 Quest/key items

- key-item transfer/destruction edge cases;
- quest-item reacquisition after destruction.

These decisions should be resolved when their dependent systems or balance curves are designed. They are not reasons to redesign the item architecture defined above.

---

## 36. Validation Criteria

The Items, Equipment and Loot system can be considered correctly implemented when all of the following are true.

### 36.1 Authored item identity

- Two copies of the same base equipment definition have identical authored combat properties before mutable enhancements/state.
- Dropping an item never rolls random stats, rarity, sockets, affixes or other base properties.
- Item definitions can represent every canonical slot, weapon category and armour weight in this PDD.

### 36.2 Item level and budget

- Every equippable item exposes item level.
- The item-level model can represent required-level, rarity, craftsmanship and raid-tier contributions.
- Two-handed equipment can be budgeted against both hand slots.
- Special effects consume item budget.

### 36.3 Requirements

- Items can author multiple polymorphic conditions.
- Level, class, race, proficiency and location requirements can be represented.
- An item that fails its requirements cannot initially be equipped.
- An equipped item whose dynamic requirement becomes false stays equipped but ceases contributing combat power.
- Restoring the condition automatically reactivates the item.
- Requirement text is visible in tooltips.

### 36.4 Statistics and effects

- Primary attributes are explicit rather than adaptive.
- Melee, Ranged and Spell offensive stat families remain distinct.
- Healing Power and Healing Critical Rating/Chance can be represented.
- Deflection is represented as an intrinsic shield property.
- Slot-specific special-effect restrictions are enforced.
- Item stat tooltip formatting is driven by stat-definition metadata.

### 36.5 Sets

- An item belongs to at most one set.
- Set thresholds count active equipped members.
- Inactive items do not satisfy set thresholds.
- Masterwork crafted equipment can participate in the shared Masterwork set.

### 36.6 Gems and enchants

- Socket polarities use exactly Fire, Water, Earth, Air, Light and Shadow.
- Any gem can enter any socket.
- None / Partial / Full matching is calculated per item.
- One Legendary gem at most may be equipped across the character.
- The Legendary gem overrides all equipped socket polarities while equipped.
- Enchants can be applied to every combat slot except Trinket and Waist.
- Re-enchanting replaces the prior enchant.
- Gems and enchants do not alter base item level.

### 36.7 Durability

- Death, taking damage and weapon use can cause durability loss.
- Maximum durability can vary by slot, rarity and craftsmanship.
- At zero durability, non-Artifact equipment remains equipped and usable at 20% effectiveness.
- Weapons still attack at zero durability.
- Artifact equipment never has durability.
- Specialist NPC repair and appropriate self-repair through professions are supported.
- Repair cost can scale with missing durability, item level, rarity and craftsmanship.

### 36.8 Loot generation

- NPCs/bosses can reference explicit loot tables.
- Multiple NPCs can share a table.
- Bosses can use bespoke tables.
- Tables support draw count, weighted Item/Currency/Nothing entries and quantity ranges.
- Invalid tables fail validation.
- Loot generation never modifies the authored properties of an item.

### 36.9 Loot distribution

- Shared loot supports Need / Greed, Master / Leader Assignment, Round Robin, Free-for-All and Random Assignment.
- Need / Greed is the default.
- Need / Greed follows Need > Greed > Pass with 1–100 rolls and tied rerolls.
- The default roll timeout is 30 seconds and timeout means Pass.
- Need does not perform class/role/proficiency eligibility filtering.
- Unclaimed corpse loot is destroyed when the corpse despawns.

### 36.10 Transfer and storage

- Items cannot be freely dropped as persistent world objects.
- Non-bound items can be traded and mailed subject to category restrictions.
- Quest items cannot be mailed.
- Mail supports multiple attachments and CoD.
- Work orders reserve customer materials and return the finished item by mail.
- Craftable bags expand inventory.
- City banks share one bank state.
- Each world stash stores five local stacks and is accessible only at that stash.
- Key items use a keyring and quest items use ordinary inventory.

### 36.11 Vendors and sinks

- Vendors use authored inventories.
- Limited and regional stock are supported.
- Buyback is supported.
- Specialist repair NPCs are supported.
- Vendoring provides coin at a meaningful value penalty.
- There is no general salvage system.
- There is no disenchant-equipment-to-materials system.

### 36.12 Presentation

- Tooltips follow the item hierarchy defined in this PDD.
- Superior/Masterwork craftsmanship is displayed below the name.
- Cosmetic option availability is omitted from the normal combat tooltip.
- Hover comparison shows net deltas.
- Two-handed comparisons account for both hands.
- Non-numeric effect changes are surfaced separately from numeric deltas.

---

## 37. Design Summary

The Ninth Age item system is built around authored identity rather than procedural loot generation.

Equipment power is structured by explicit item level, slot budget, rarity and — for crafted items — craftsmanship. Stats and restrictions communicate intended users without requiring unnecessary class locking. Item requirements are extensible conditions that can dynamically deactivate equipment without forcibly unequipping it.

Enhancement is external and legible: authored polarity sockets, gems, enchantments and temporary treatments modify equipment without creating an endless base-item upgrade system.

Loot tables determine what drops; group loot modes determine ownership. Need / Greed is the default shared-loot mode, with alternative group distributions available.

Durability provides a recurring economic sink without destroying equipment. Vendors, storage, trade, mail and work orders provide controlled item circulation, while binding and craftsmanship preserve long-term value and provenance.

The remaining work is primarily balancing coefficients and dependent-system detail rather than foundational item-system design.
