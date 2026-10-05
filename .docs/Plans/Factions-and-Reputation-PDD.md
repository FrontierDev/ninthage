# Ninth Age — Factions and Reputation Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** World factions, political relationships, character reputation, faction disposition, reputation alliances, rewards, penalties, services and faction-dependent access  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended faction and reputation model for **Ninth Age**.

It is authoritative for:

- faction definitions and categories;
- static faction-to-faction diplomacy;
- player political/combat faction identity;
- character-specific reputation;
- reputation ranges and progression;
- reputation tiers;
- reputation-driven NPC disposition;
- reputation alliances;
- reputation rewards and penalties;
- reputation-dependent dialogue, quests, vendors, prices, permits and services;
- reputation persistence and authority;
- faction and reputation authoring requirements.

This document does not define:

- detailed PvP rules — future PvP PDD;
- quest structure and dialogue branching — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- NPC behaviour beyond faction/disposition inputs — [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md) and [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- profession permit mechanics beyond reputation requirements — [Crafting System PDD](Crafting-System-PDD.md);
- detailed vendor economy — [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- item reward behaviour — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- detailed persistence implementation — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- final Reputation-window styling — [UI and UX PDD](UI-and-UX-PDD.md).

Where current implementation conflicts with this document, this document defines the intended product behaviour.

---

## 2. Design Pillars

### 2.1 Reputation represents a character's relationship with a faction

Reputation is personal to the character.

It represents how a faction regards that specific character because of:

- the character's origin;
- quests completed;
- actions taken;
- members of the faction killed;
- significant narrative choices;
- other explicitly authored faction interactions.

Reputation is not an account-wide progression track.

### 2.2 Political diplomacy and personal reputation are separate systems

Faction-to-faction diplomacy describes the political relationship between organisations.

Personal reputation describes the relationship between one character and one faction.

A character may therefore have an unusual personal relationship with a faction whose normal political relationship would suggest something different.

Where the faction permits it, personal reputation may override the baseline combat disposition produced by political diplomacy.

### 2.3 Reputation progression should come from content, not artificial throttling

Reputation is earned primarily through:

- quests;
- repeatable quests.

Other explicit authored sources may award or remove reputation where appropriate.

There is:

- no reputation decay;
- no diminishing-return system;
- no generic reputation timegating merely to slow farming.

If a reputation source needs farming protection, that protection should come from how the activity is designed and made available.

### 2.4 Factions should have their own identity

Ninth Age does not use one universal set of reputation rank names.

Each reputation-bearing faction authors its own:

- tier names;
- thresholds;
- icons;
- benefits;
- penalties.

This allows faction progression to reflect the culture and story of that faction.

### 2.5 Reputation should change the world the character can access

Reputation may affect:

- dialogue;
- quest availability;
- vendor access;
- vendor prices;
- profession permit access;
- services;
- NPC disposition.

Reputation should therefore be a meaningful world relationship rather than only a progress bar.

---

## 3. Existing Architectural Foundation

The current codebase already contains a useful faction and reputation foundation.

### 3.1 FactionDefinition

The existing FactionDefinition supports:

- stable definition identity;
- display name;
- description;
- icon;
- colour;
- category;
- whether the faction supports player reputation;
- CanWar;
- maximum points;
- allied faction IDs;
- enemy faction IDs;
- authored reputation-tier descriptions.

This data-driven foundation should be retained and extended where required.

### 3.2 Faction categories

The current categories are:

- Primary;
- Regional;
- Hidden.

These categories are retained and formally defined by this PDD.

### 3.3 Character reputation

PlayerReputation currently stores a dictionary of faction IDs to integer reputation values.

CharacterData persists saved faction reputation independently for each character.

This character-specific model is retained.

### 3.4 Race-based starting reputation

RaceDefinition currently supports:

- a default PvP/combat faction;
- authored starting reputation entries.

This is retained.

Different races may begin with different political histories and starting relationships rather than all characters starting identically.

### 3.5 Existing reputation UI

The current Reputation window already supports:

- faction category organisation;
- faction icon and description;
- numeric progress;
- named reputation tiers;
- tier threshold markers;
- authored benefits and penalties.

This general presentation model should be retained.

### 3.6 Existing alliance definitions

The current data already uses Hidden faction definitions for political groupings such as the Treaty of Eldanon and Old Dominion.

This is a useful foundation for reputation alliances, although reputation-alliance membership must remain an explicit authored concept rather than relying on accidental inference.

---

## 4. Faction Categories

### 4.1 Primary

Primary factions are major political powers or other major organisations with broad world significance.

They may:

- define a race's default political/combat faction;
- participate in major diplomatic relationships;
- support full player reputation progression.

### 4.2 Regional

Regional factions are more localised organisations, settlements, institutions or groups.

They may have reputation progression and all normal reputation-dependent rewards or penalties.

Regional does not mean mechanically less important; it describes scope.

### 4.3 Hidden

Hidden factions are systemic or non-player-facing faction definitions.

They may be used for:

- political blocs;
- reputation alliances;
- combat grouping;
- other authored relationship structures.

Hidden factions do **not** appear as normal entries in the Reputation UI.

The term Hidden does not necessarily mean the organisation is narratively secret.

---

## 5. Static Faction Diplomacy

Faction-to-faction diplomacy is static authored world data tied to the story of Ninth Age.

A faction may define other factions as:

- allied;
- enemy;
- neither, producing a neutral baseline relationship.

Diplomatic relationships do not dynamically change because of routine player actions or reputation progression.

If the story of the game later changes world diplomacy, that should be introduced through deliberate authored content/data changes rather than an emergent reputation calculation.

### 5.1 Reciprocal relationships

Alliance and enemy relationships should remain reciprocal.

If Faction A is allied with Faction B, Faction B is also allied with Faction A.

If Faction A is an enemy of Faction B, Faction B is also an enemy of Faction A.

The existing editor behaviour that maintains reciprocal relationships is consistent with this design.

---

## 6. Player Political / Combat Faction

A player character has a political/combat faction identity used to determine baseline faction relationships.

A race may define the character's starting/default political faction.

This is distinct from personal reputation.

A player cannot freely declare war on, or manually toggle hostility toward, PvE factions.

Any change to the character's political alignment must come from explicitly authored game content rather than a general-purpose Declare War control.

Whether permanent political-faction switching is used by future story content remains content-dependent, but free switching is not part of the baseline system.

---

## 7. Baseline NPC Disposition

The baseline relationship between a player and an NPC is derived from:

- the NPC's combat faction;
- the player's political/combat faction;
- authored allied/enemy relationships.

The baseline relationship states remain:

- Hostile;
- Neutral;
- Allied.

Neutral is **not** inherently hostile.

Neutral NPCs must not automatically be treated as attack-on-interact targets merely because they are not Allied.

The current client behaviour that treats both Neutral and Hostile as equivalent interaction states is prototype behaviour and is not authoritative design.

---

## 8. Personal Reputation

A character may hold reputation with reputation-bearing factions.

Reputation is:

- character-specific;
- persistent;
- numeric;
- server-authoritative;
- clamped to the faction's allowed standard range.

Reputation does not decay over time.

There is no account-wide reputation inheritance.

---

## 9. Standard Reputation Scale

Ninth Age uses a standard numeric reputation scale across factions.

### 9.1 CanWar factions

For factions with CanWar enabled:

**-1000 to +1000**

These factions permit negative reputation.

Negative standing may cause increasingly severe penalties, including hostile NPC disposition.

### 9.2 Non-CanWar factions

For factions with CanWar disabled:

**0 to +1000**

These factions do not permit negative reputation.

Reputation losses clamp at zero.

### 9.3 Standard scale rationale

Individual factions should not use different numeric maxima/minima merely to make progression faster or slower.

Progression pacing should instead be controlled through:

- reputation awarded by content;
- reputation removed by actions;
- availability of repeatable quests;
- placement of authored tier thresholds.

This keeps reputation values comparable across factions while preserving faction-specific progression.

### 9.4 Existing MaxPoints field

The existing MaxPoints field currently supports per-faction limits.

Under this PDD, the canonical maximum is 1000.

Implementation may retain the field temporarily for migration, but differing faction maxima are not part of the intended design unless this PDD is later revised.

---

## 10. Meaning of CanWar

CanWar means:

- the faction's reputation may go below zero;
- the faction may have negative reputation tiers;
- sufficiently negative reputation may cause members of the faction to become hostile.

CanWar does **not** mean:

- the player may manually declare war;
- faction diplomacy dynamically changes;
- PvP kills affect reputation;
- every negative value must immediately produce identical hostility.

The exact consequences of negative standing remain tier-driven and faction-authored.

The implementation name CanWar may be retained for compatibility, but its product meaning is the negative-reputation capability defined here.

---

## 11. Reputation Tiers

Each faction authors its own reputation tiers.

A tier may define:

- threshold;
- title;
- icon;
- whether it represents positive or negative standing;
- player-facing benefits;
- player-facing penalties;
- disposition consequences;
- content/service unlocks.

The existing bespoke-tier model is retained.

Ninth Age does **not** replace this system with a universal Renown-level ladder.

### 11.1 Tier names

Tier names are faction-specific.

For example, the existing Kingdom of Astonia data uses names such as:

- Wanted;
- Hated;
- Unfriendly;
- Friendly;
- Trusted;
- Respected.

Those exact names and thresholds are content data, not universal reputation ranks.

### 11.2 Threshold design

Tier thresholds are authored per faction within the standard numeric scale.

This allows two factions to use the same -1000 to +1000 range while placing meaningful rewards and penalties at different values.

---

## 12. Reputation and Combat Disposition

Personal reputation **can override baseline combat disposition**, depending on the faction.

This override must be explicitly enabled/authored for the relevant faction.

### 12.1 Resolution model

NPC disposition should conceptually resolve in this order:

1. determine the baseline disposition from political/combat faction diplomacy;
2. determine whether the NPC's faction permits personal reputation to affect disposition;
3. determine the character's current reputation tier with that faction;
4. apply any authored disposition override associated with that tier;
5. produce the effective Hostile, Neutral or Allied relationship.

### 12.2 Faction-specific control

Not every faction must permit reputation to override political diplomacy.

Examples of valid authored behaviour include:

- a normally allied faction becoming hostile to a character who has severely damaged their reputation;
- a normally neutral faction becoming allied/friendly to a highly trusted character;
- a faction whose political hostility cannot be overcome through ordinary personal reputation.

The allowed outcomes are content-authored rather than globally assumed.

### 12.3 Negative reputation

For CanWar factions, negative tiers may escalate from social penalties to active hostility.

The exact reputation threshold at which NPCs attack on sight is faction-specific.

---

## 13. Starting Reputation

Playable races may author starting reputation with individual factions.

Starting reputation may be:

- positive;
- neutral;
- negative;
- strongly negative where the setting requires it.

Starting values are part of worldbuilding and should reflect pre-existing political/cultural relationships.

Starting reputation is still character reputation and is stored on the created character.

---

## 14. Reputation Gain

Reputation is gained primarily through:

- quests;
- repeatable quests.

Additional explicit authored sources may award reputation when appropriate to the content, such as a significant narrative or faction activity.

There is no generic expectation that every kill, event or activity awards reputation.

### 14.1 Repeatable quests

Repeatable quests are the principal repeatable method for deliberately progressing reputation where a faction is intended to be farmable.

Their cadence and availability belong to the Quest PDD.

### 14.2 No diminishing returns

Repeated valid reputation rewards are not reduced merely because the player has earned substantial reputation recently.

There is no general diminishing-return curve.

### 14.3 No generic timegating

The reputation system itself does not impose arbitrary daily caps, weekly caps or similar progression throttles.

If a repeatable source has a reset or availability rule, that is a property of the authored content, not a hidden reputation throttle.

---

## 15. Reputation Loss

Reputation may be lost through:

- quest choices;
- narrative consequences;
- killing members of the faction;
- other explicit authored actions.

### 15.1 Killing faction members

Killing an NPC belonging to a reputation faction causes a **small reputation loss** with that faction.

Ordinary kills should impose a relatively small loss.

Important, named or narratively significant NPCs may author larger losses where appropriate.

Exact values are balance/content data.

### 15.2 No PvP reputation loss

Killing enemy-faction player characters does **not** alter PvE faction reputation.

PvP allegiance and PvE personal reputation must not be conflated through player kills.

---

## 16. Recovery from Negative Reputation

Negative reputation does not recover automatically.

There is no passive reputation decay toward zero.

Where designers intend hostile standing to be recoverable, recovery must come through authored content such as:

- quests;
- repeatable quests;
- intermediaries;
- reparative actions;
- other explicit reputation rewards.

Story content may intentionally make some consequences difficult or impossible to reverse.

The reputation system does not guarantee that every negative narrative outcome has an automatic recovery path.

---

## 17. Reputation Alliances

The system supports **reputation alliances**: authored groupings of factions for content that intentionally rewards all members of an alliance together.

The existing Treaty of Eldanon-style hidden grouping is the model for this concept.

### 17.1 Alliance membership

Reputation-alliance membership is static authored data tied to the world's political structure.

It does not change because a player's personal reputation changes.

### 17.2 Alliance reward distribution

When content explicitly awards reputation to a reputation alliance, the signed reputation amount is distributed **1:1 to each member faction**.

Example:

- reward target: Treaty of Eldanon;
- reward: +100 reputation;
- member factions: Astonia, Urngor and Kiram;
- result: +100 reputation to Astonia, +100 to Urngor and +100 to Kiram.

Negative alliance rewards distribute in the same manner where member factions permit negative reputation.

Each member's final value is clamped to its allowed reputation range.

### 17.3 No automatic member spillover

Reputation earned directly with one member faction does **not** automatically change reputation with the other alliance members.

For example:

+100 Astonia reputation does not automatically grant reputation with Urngor or Kiram.

This prevents ordinary faction progression from producing unintended political spillover.

### 17.4 No enemy spillover

Gaining reputation with a faction or reputation alliance does not automatically reduce reputation with its political enemies.

If a quest should improve one faction while damaging another, the quest must explicitly author both reputation changes.

### 17.5 Alliance UI

A reputation alliance represented by a Hidden faction does not appear as a normal player reputation track.

It is primarily a grouping/reward target.

The player sees the resulting changes on the visible member factions.

---

## 18. Reputation Rewards and Penalties

Reputation tiers may affect access and behaviour across other systems.

Supported consequences include:

- dialogue availability;
- quest availability;
- vendor access;
- vendor prices;
- profession permit access;
- services;
- NPC disposition.

Additional authored consequences may be introduced through reusable conditions/actions where appropriate.

### 18.1 Dialogue and quests

Dialogue choices and quest requirements may test faction reputation or reputation tier.

This should use the shared condition architecture defined by the Quest PDD.

### 18.2 Vendors and prices

A reputation tier may:

- unlock a vendor;
- unlock additional vendor inventory;
- apply an authored purchase-price benefit.

Price benefits should be explicit tier benefits rather than an invisible continuous formula based on every reputation point.

Exact discount percentages belong to content/economy balancing.

### 18.3 Permits

Reputation may unlock profession permit access.

The existing Astonia data already demonstrates the intended pattern of progressively higher permit-zone access at higher positive tiers.

The Crafting PDD owns the activities available inside those permit zones.

### 18.4 Services

A faction may restrict or unlock services based on reputation.

Examples may include transport, repairs, training or other faction-controlled services where appropriate.

The exact services remain owned by their relevant systems.

---

## 19. Faction Conditions

Other systems should be able to test faction/reputation state through reusable conditions.

Required condition concepts include:

- reputation at least a value;
- reputation at most a value;
- current reputation tier;
- political/combat faction identity;
- effective disposition where appropriate.

Quest and dialogue systems should not hard-code faction IDs into bespoke logic when a reusable condition can represent the requirement.

---

## 20. Reputation Reward Actions

Authoring systems should support explicit reputation mutations as reusable actions/rewards.

A reputation change must identify:

- target faction or reputation alliance;
- signed amount.

The server resolves:

- alliance distribution;
- clamping;
- tier transitions;
- resulting disposition changes;
- persistence;
- client notification.

---

## 21. Faction Visibility in the Reputation UI

Primary and Regional factions may appear in the Reputation UI when the character has a relevant reputation entry or when design intentionally exposes them.

Hidden factions do not appear in the normal Reputation UI.

Hidden reputation alliances therefore do not create redundant progress bars alongside their member factions.

---

## 22. Reputation UI

The existing Reputation-window concept is retained.

For a visible faction, the UI should be able to communicate:

- faction name;
- icon;
- description;
- current numeric reputation;
- progress through the standard reputation range;
- current named tier;
- authored tier thresholds;
- benefits;
- penalties.

The UI should make negative and positive standing legible.

Exact colours, animation and typography belong to the UI/UX PDD.

### 22.1 Tier-change feedback

Crossing a reputation tier should produce clear but non-intrusive feedback.

The exact notification presentation is a UI decision.

---

## 23. Reputation Persistence

Reputation is persisted per character.

Persistence must include at least:

- faction identity;
- numeric reputation value.

The system may derive the current tier from authored faction data rather than redundantly storing the tier name.

Changes must persist across:

- logout;
- disconnect;
- server restart;
- normal world transfer.

There is no account-wide reputation progression.

---

## 24. Multiplayer Behaviour

Reputation is personal even when content is completed in a group.

If a quest rewards reputation to multiple eligible party members:

- each character receives the reward independently;
- each character's current standing is updated independently;
- clamping and tier transitions are evaluated per character.

One character's reputation does not automatically change another character's reputation.

World-wide narrative consequences may affect faction-related content through the Quest/Narrative system, but that is distinct from copying personal reputation between players.

---

## 25. Server Authority

The server is authoritative for:

- starting reputation assignment;
- reputation gains;
- reputation losses;
- alliance reward distribution;
- clamping;
- current reputation values;
- tier resolution for gameplay;
- reputation-driven disposition;
- vendor/service eligibility;
- quest/dialogue reputation requirements;
- persistence.

Clients may display reputation state but must not award, remove or set authoritative reputation locally.

---

## 26. Faction Editor Requirements

The existing Faction Editor should be extended rather than replaced.

It should support authoring and validation of:

- identity;
- category;
- icon;
- colour;
- reputation visibility/progression capability;
- CanWar;
- allied factions;
- enemy factions;
- reputation-alliance membership or member list where applicable;
- whether personal reputation can override disposition;
- reputation tiers;
- tier thresholds;
- tier title/icon;
- tier benefits;
- tier penalties;
- tier disposition override where applicable.

The editor should continue maintaining reciprocal static diplomacy.

---

## 27. Reputation Tier Authoring

Tier authoring must allow designers to inspect the complete progression across the standard numeric range.

Validation should ensure:

- thresholds are ordered;
- thresholds remain within the valid normalized/numeric range;
- a faction does not contain contradictory tier definitions;
- disposition overrides are valid states;
- non-CanWar factions do not author inaccessible negative tiers;
- Hidden alliance definitions do not accidentally appear as normal player reputation tracks.

Tier benefits and penalties shown in the UI should correspond to actual gameplay conditions/actions rather than being unsupported descriptive text wherever practical.

---

## 28. Current Implementation Changes Required

The existing implementation is a useful foundation but requires changes to conform to this PDD.

Required work includes:

- standardise reputation ranges at -1000 to +1000 for CanWar factions and 0 to +1000 otherwise;
- clamp all reputation mutations server-side;
- implement faction-controlled personal-reputation disposition overrides;
- stop treating Neutral disposition as equivalent to Hostile in interaction logic;
- implement small reputation losses for killing faction NPCs;
- implement explicit reputation-alliance reward distribution;
- ensure direct member reputation does not automatically spill over;
- ensure enemy factions do not receive automatic inverse spillover;
- filter Hidden factions from the normal Reputation UI;
- implement reusable reputation conditions for dialogue, quests, vendors, permits and services;
- make tier consequences drive actual gameplay eligibility/behaviour;
- ensure reputation updates persist correctly.

### 28.1 CharacterReputationService correctness

The current CharacterReputationService constructor performs an AddReputation call and then sets reputation again using the incoming delta value.

That behaviour can overwrite accumulated reputation and duplicate update notifications.

Implementation must use one authoritative mutation path that:

1. reads the existing value;
2. applies the delta once;
3. distributes alliance rewards if required;
4. clamps the result;
5. persists the result;
6. resolves tier/disposition transitions;
7. sends one coherent client update per affected visible faction.

---

## 29. Relationship to Quest and Narrative

Quests are the primary authored source of reputation progression.

Quest/dialogue content may:

- award reputation;
- remove reputation;
- award reputation to an alliance;
- require reputation values or tiers;
- branch based on reputation;
- alter access to future faction content.

Players cannot manually declare faction hostility outside authored content.

Narrative choices may therefore create negative faction relationships through explicit reputation changes.

Faction-to-faction diplomacy itself remains static authored world data.

---

## 30. Relationship to NPC and AI Systems

The NPC system owns an actor's combat faction.

The faction/reputation system supplies the effective player-to-NPC disposition.

AI may use that effective disposition when deciding:

- whether the NPC is hostile;
- whether guards attack;
- whether detection behaviour changes;
- whether normal interaction is permitted.

Detailed perception, aggro and combat behaviour remains owned by the AI PDD.

---

## 31. Relationship to Crafting

Reputation may control access to profession permit zones and related faction-controlled gathering opportunities.

The current Astonia progression direction is preserved:

- lower positive standing may unlock low-tier permit access;
- higher standing may unlock progressively higher-tier permit access.

Exact tier names, thresholds and permit content remain faction/profession authored data.

---

## 32. Intentionally Deferred Decisions

The following remain content, balance or dependent-system decisions rather than unresolved architecture:

- exact reputation gains for individual quests;
- exact reputation loss per ordinary faction-member kill;
- larger penalties for named/important NPCs;
- exact faction tier thresholds;
- exact tier names for each faction;
- exact vendor discount percentages;
- exact recovery quests for hostile factions;
- which factions permit positive reputation to overcome baseline political hostility;
- which factions permit negative reputation to override baseline political alliance;
- exact UI notification treatment for reputation/tier changes;
- whether future story content ever changes a character's political/combat faction.

These details do not require redesigning the system defined here.

---

## 33. Locked Design Decisions

The following decisions are locked by this PDD:

1. Factions remain data-driven definitions.
2. Faction categories remain Primary, Regional and Hidden.
3. Hidden factions are not shown as normal entries in the Reputation UI.
4. Faction-to-faction diplomacy is static authored story/world data.
5. Allied/enemy faction relationships are reciprocal.
6. Player political/combat faction identity is separate from personal faction reputation.
7. Players cannot freely declare war on PvE factions.
8. Personal reputation is character-specific only.
9. Reputation does not decay.
10. Reputation has no generic diminishing returns.
11. The reputation system does not impose generic progression timegates.
12. Reputation is gained primarily through quests and repeatable quests.
13. Killing faction NPCs causes a small reputation loss with that faction.
14. Killing enemy-faction player characters does not alter PvE faction reputation.
15. CanWar means a faction supports negative reputation.
16. CanWar factions use the standard -1000 to +1000 range.
17. Non-CanWar factions use the standard 0 to +1000 range.
18. Differing faction progression speeds are controlled through gains/losses and tier placement, not different numeric maxima.
19. Reputation values are clamped to their valid range.
20. Each faction authors its own named reputation tiers, thresholds, icons, benefits and penalties.
21. Ninth Age retains the bespoke reputation-tier system rather than adopting a universal Renown-level system.
22. Personal reputation may override baseline combat disposition where the faction explicitly permits it.
23. The exact disposition override is tier/faction-authored.
24. Neutral disposition is not inherently Hostile.
25. Reputation can affect dialogue, quests, vendors, prices, permits and services.
26. Reputation-alliance membership is static authored data.
27. Reputation explicitly awarded to an alliance distributes 1:1 to all member factions.
28. Direct reputation with one alliance member does not automatically grant reputation to the other members.
29. Positive reputation does not automatically cause negative reputation with political enemies.
30. Hidden reputation alliances do not maintain a separate visible player reputation track.
31. Starting reputation may vary by race.
32. Reputation progression and mutation are server-authoritative.
33. Reputation persists per character.
34. Reputation tier state should be derived from the character's numeric reputation and authored faction thresholds.
35. Existing faction/reputation authoring and UI foundations should be extended rather than discarded.

---

## 34. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [Crafting System PDD](Crafting-System-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- future PvP design.

---

## 35. Validation Criteria

The Factions and Reputation system is correctly implemented when:

1. Designers can create Primary, Regional and Hidden faction definitions.
2. Hidden factions do not appear in the normal Reputation UI.
3. Allied/enemy relationships remain reciprocal.
4. Static political diplomacy can resolve an NPC's baseline relationship to the player's political faction.
5. Neutral NPCs are not automatically treated as hostile.
6. A race can define a starting political/combat faction.
7. A race can define different starting reputation values with several factions.
8. Reputation persists independently for each character.
9. Two characters on the same account may have different reputation with the same faction.
10. CanWar factions clamp at -1000 and +1000.
11. Non-CanWar factions clamp at 0 and +1000.
12. Factions can author different tier names and thresholds while sharing the standard numeric scale.
13. A faction can author positive and negative tier consequences.
14. A faction can choose whether personal reputation may override baseline diplomatic disposition.
15. A negative reputation tier can make otherwise non-hostile faction NPCs hostile where authored.
16. A positive reputation tier can alter interaction/disposition where explicitly authored.
17. Quests can award or remove faction reputation.
18. Repeatable quests can provide repeatable reputation progression without a separate diminishing-return system.
19. Killing an ordinary faction NPC can apply a small reputation loss.
20. Killing a player character does not change PvE faction reputation.
21. Reputation does not drift or decay while the character is inactive.
22. Dialogue choices can be gated by reputation through reusable conditions.
23. Quest availability can be gated by reputation through reusable conditions.
24. Vendor availability can be gated by reputation.
25. Vendor price benefits can be attached to authored reputation tiers.
26. Permit access can be gated by reputation.
27. Services can be gated by reputation.
28. Content can target a reputation alliance rather than listing every member manually.
29. A +100 alliance reputation reward gives +100 to each member faction before clamping.
30. A direct +100 reward to one member faction does not alter the other alliance members.
31. Reputation gains do not automatically reduce enemy-faction reputation.
32. Alliance reward distribution is server-authoritative.
33. Reputation mutation occurs exactly once per awarded delta.
34. Reputation updates correctly accumulate rather than being overwritten by the latest delta.
35. Tier transitions update gameplay eligibility/disposition without requiring relogging.
36. The Reputation UI displays the character's current value, tier, thresholds, benefits and penalties for visible factions.
37. Authoring validation catches invalid ranges, unordered thresholds and contradictory relationships.
