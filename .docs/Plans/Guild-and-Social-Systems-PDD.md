# Ninth Age — Guild and Social Systems Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Persistent guild identity, membership, ranks, permissions, guild storage, guild halls and tenancy, hall funding/tariffs, guild trophies, friends, ignore/block relationships, social persistence and cross-world social behaviour  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended **Guild and Social Systems** model for Ninth Age.

It is authoritative for:

- persistent guild identity;
- cross-world guild scope;
- guild membership;
- guild leadership;
- guild ranks and permissions;
- guild invitations and applications;
- guild roster state;
- guild storage and treasury concepts;
- guild auditability;
- guild halls;
- world-specific guild-hall tenancy;
- guild-hall rent and eviction;
- recoverable hall investment;
- guild hall funding;
- optional guild tariffs;
- guild-hall access;
- trophies and guild identity displays;
- guild social progression principles;
- friend relationships;
- ignore/block relationships;
- social persistence.

This document does not define:

- party/raid structure — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- chat channels, whispers, moderation or spam prevention — [Communication Systems PDD](Communication-Systems-PDD.md);
- general economy/market rules — [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- item binding/trading rules — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- world runtime implementation — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- settlement geography and building placement — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- detailed UI styling — [UI and UX PDD](UI-and-UX-PDD.md);
- account/session implementation — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- PvP guild relationships, wars or territorial ownership — future [PvP PDD](PvP-PDD.md).

Where implementation conflicts with this document, this PDD defines intended product behaviour.

---

## 2. Design Pillars

### 2.1 Guilds are social organisations, not stat progression systems

A guild exists to create:

- persistent community;
- shared identity;
- organisation;
- cooperation;
- social history;
- physical presence in the world.

Guild membership must not become a mandatory source of character combat power.

### 2.2 Guilds are global, halls are local

A guild exists across persistent worlds.

A guild hall is tied to one authored physical property on one persistent world.

This allows the social organisation to remain stable across worlds while giving physical guild identity real geographic meaning.

### 2.3 Guild halls should reinforce the world

A guild hall is a place in the world, not a menu lobby detached from it.

Settlements should contain plausible buildings or compounds that can serve as guild halls.

### 2.4 Guild halls should not replace settlements

Guild halls are primarily:

- gathering places;
- meeting places;
- identity spaces;
- trophy/display spaces.

They should not become private substitutes for the public services that make towns and cities socially active.

### 2.5 Guild halls should be meaningful commitments

A hall should be difficult enough to obtain and maintain that it has prestige and economic weight.

It should not be a trivial entitlement granted automatically to every new guild.

### 2.6 Economic obligations should be transparent

Hall rent, arrears, tariff income and recoverable investment must be understandable and auditable.

Members should be able to see when guild leadership is imposing economic costs.

### 2.7 Administration should be flexible but safe

Guilds need custom rank structures and granular permissions.

Dangerous actions require stronger permission boundaries and audit history.

### 2.8 Social systems should preserve player agency

Friends, guilds and social visibility should make cooperation easier without forcing players into unwanted communication or exposure.

Ignore/block systems must be reliable.

---

## 3. Guild Scope

A guild is a **global persistent organisation**.

Guild identity is not owned by one world-server process.

Members may belong to the same guild while their characters are currently on different persistent worlds.

The guild must therefore be addressable through a stable immutable **GuildID**.

---

## 4. Guild Identity

A guild requires at minimum:

- GuildID;
- guild name;
- creation timestamp;
- current leader;
- member roster;
- rank definitions;
- permissions;
- guild description;
- message of the day;
- guild treasury/storage state;
- guild hall tenancy state where applicable;
- guild trophy/unlock state;
- audit history for sensitive actions.

GuildID must remain stable if the guild later changes its visible name.

---

## 5. Guild Name

Guild names must be globally unique after the project's normal text normalization rules.

The exact:

- minimum length;
- maximum length;
- permitted punctuation;
- rename cost/cooldown;

remain tuning/policy decisions.

A guild name must not be scoped only to one persistent world.

---

## 6. Guild Membership Ownership

Guild membership is attached to a **character**.

Different characters on the same account may therefore:

- belong to different guilds;
- belong to the same guild;
- have no guild membership.

A character may belong to only one ordinary guild at a time unless a future system explicitly introduces a different organisation type.

---

## 7. Cross-World Membership

Cross-world membership is supported.

A guild roster may include members whose characters:

- have different account HomeWorldIDs;
- are currently playing on different persistent worlds;
- are offline.

World location must not fragment the guild into separate guild records.

---

## 8. Guild Creation

Guild creation requires an explicit creation process rather than being automatic.

The final creation request must be server-authoritative and atomic.

Creation must validate:

- founder eligibility;
- proposed name;
- name uniqueness;
- required founding support if configured;
- creation cost if configured;
- membership restrictions;
- any other explicit prerequisites.

The exact founding numbers/cost are not locked by this PDD.

---

## 9. Invitations

Guild members with permission may invite eligible characters.

Invitations should:

- identify the guild;
- identify the inviting member;
- expire after a configurable period;
- be accepted/rejected explicitly;
- become invalid if the recipient becomes ineligible.

Guild membership does not begin until the server accepts the join operation.

---

## 10. Applications

Guilds may optionally allow applications.

A guild may configure recruitment state such as:

- closed;
- invitation only;
- applications open.

Applications should provide only the player information intentionally exposed by the social system.

Detailed recruitment UI belongs to UI/UX.

---

## 11. Leaving and Removal

A normal member may leave a guild voluntarily.

A permitted guild administrator may remove a lower-authority member.

Leaving/removal must not:

- delete personal items;
- delete personal progression;
- reset unrelated character state.

Guild-owned state remains owned by the guild.

---

## 12. Leadership

Every guild has one authoritative guild leader.

Leadership transfer must be an explicit server-authoritative operation.

A guild may not enter a state with:

- zero leaders;
- multiple authoritative leaders;

except transiently inside one atomic persistence transaction.

Inactive-leadership succession policy remains open for later tuning.

---

## 13. Ranks

Guilds use **customisable ranks**.

A guild can:

- create rank definitions;
- rename rank definitions;
- order them by authority;
- assign permissions to them;
- assign members to ranks.

The system must not rely exclusively on hard-coded titles such as Officer or Veteran.

Default ranks may be created for convenience.

---

## 14. Permission Model

Permissions are assigned to ranks.

Required permission categories include, where applicable:

- invite member;
- approve application;
- remove member;
- promote member;
- demote member;
- edit lower ranks;
- edit public member note;
- edit private/officer note;
- edit guild description;
- edit message of the day;
- issue guild announcements;
- manage guild events/calendar;
- view sensitive audit information;
- deposit guild-bank items;
- withdraw guild-bank items;
- move guild-bank items;
- manage guild-bank permissions;
- deposit/allocate guild currency;
- withdraw ordinary guild currency;
- manage guild tariff;
- manage guild hall tenancy;
- manage hall access;
- place/remove guild decorations;
- place/remove guild trophies;
- rename guild where permitted;
- transfer leadership;
- disband guild.

Exact permission names may differ in implementation.

---

## 15. Rank Authority

A member should not use administrative permissions against members/ranks above their own authority.

For example, a permitted officer should not be able to:

- remove the guild leader;
- promote themselves above the maximum authority they are allowed to assign;
- alter a higher rank;
- demote a higher-rank member.

Leadership transfer and guild disbanding should remain especially restricted.

---

## 16. Guild Progression

Ninth Age does **not** use an ordinary guild-level treadmill.

There should not be a system where guilds repeatedly gain Guild XP to unlock permanent passive combat/stat bonuses.

Guild prestige/progression instead emerges through:

- age/history;
- membership;
- accomplishments;
- trophies;
- hall tenancy;
- wealth;
- event/raid achievements;
- crafting/social accomplishments;
- other visible history.

---

## 17. Guild Achievements and Trophies

Guild-wide accomplishments may be recorded.

These should primarily reward:

- recognition;
- trophies;
- cosmetic identity;
- historical records;
- display objects.

Examples may include:

- significant raid kills;
- world-boss achievements;
- major guild events;
- unusual exploration accomplishments;
- high-level crafting accomplishments.

They should not become a mandatory passive-stat progression system.

---

## 18. Guild Bank and Storage

Guilds require substantial shared storage.

Guild storage is distinct from:

- a character's inventory;
- a character's globally accessible city-bank storage;
- local world stashes.

The exact number of tabs, slots and purchase costs remains provisional.

---

## 19. Guild Bank Permissions

Guild-bank access must support per-rank permissions.

At minimum, the system should be capable of controlling:

- view;
- deposit;
- withdraw;
- move/reorganise;
- withdrawal limits;
- gold withdrawal.

Different tabs may use different access rules.

---

## 20. Guild Treasury

Guilds may hold ordinary guild-owned gold.

Guild gold is not owned by an individual member.

Guild-gold operations must be server-authoritative and transactional.

Sensitive withdrawals and administrative transfers must be audited.

---

## 21. Guild Hall Concept

A **guild hall** is a rented authored property associated with:

- a specific property;
- a specific settlement/location;
- a specific persistent world;
- an occupying guild.

Guild halls are not generic automatically generated rooms detached from geography.

---

## 22. Guild Hall World Scope

Guild-hall tenancy is **world-specific**.

A global guild may therefore have members from many worlds while its hall exists physically on one specific persistent world.

The hall's WorldID/property identity must be durable guild state.

---

## 23. Guild Hall Property Types

Guild halls may take many physical forms.

Examples include:

- large command tent;
- fortified camp structure;
- single large house;
- guild lodge;
- townhouse;
- manor house;
- estate;
- small fort;
- keep-like compound.

These examples are not strict progression tiers.

Individual properties may have distinct:

- size;
- location;
- prestige;
- layout;
- display capacity;
- rent.

---

## 24. Settlement Property Requirement

World design must intentionally include suitable guild-rentable properties.

Cities, towns and some villages should be large enough to contain believable vacant or rentable buildings/plots where appropriate.

A settlement should not need to expose every building as rentable.

Guild-property supply is authored.

Major settlements may contain many options.

Small settlements may contain:

- few;
- one;
- no

guild properties.

---

## 25. Vacant Property Presentation

A vacant guild property should still make sense as part of the settlement.

It should not look like a missing/incomplete building waiting for a player organisation to exist.

Where possible, vacant properties may have neutral/default environmental dressing.

---

## 26. Hall Acquisition

Guild halls should be **difficult to obtain** relative to ordinary guild creation.

Acquisition may require combinations of:

- guild age;
- guild membership;
- upfront gold;
- deposit/investment;
- settlement/faction reputation;
- property availability;
- other authored eligibility.

Exact requirements remain economy/content balancing.

---

## 27. Scarcity

Guild properties are scarce physical social assets.

A property cannot simultaneously be occupied by multiple unrelated guilds on the same persistent world unless a future specific property type is intentionally authored that way.

The normal rule is one tenancy per physical guild property.

---

## 28. Hall Instance Behaviour

The existing World Runtime and Instancing PDD already supports guild-scoped seamless interiors.

Where a property has an interior requiring private guild simulation:

- the exterior belongs to the shared world;
- entry resolves the guild/property instance;
- permitted guild members/visitors resolve to the same interior;
- the interior may unload when empty.

A hall does not need a conventional loading screen merely because it is guild-scoped.

---

## 29. Hall Purpose

Guild halls are primarily for:

- social gathering;
- meetings;
- roleplay;
- ceremonies;
- recruitment/social events;
- guild identity;
- trophy display;
- decoration;
- guild organisation.

Their value should come from social meaning and place, not from concentrating every useful service behind a private door.

---

## 30. Services Prohibited by Design

Guild halls should **not** provide private replacements for core public settlement services.

By default, a guild hall must not provide:

- general crafting stations;
- global character-bank access;
- global player-market/exchange access;
- broad profession trainers;
- ordinary general-purpose service hubs;
- private lodestone/fast-travel replacement.

This protects activity in:

- cities;
- towns;
- villages;
- public crafting areas;
- public market districts.

---

## 31. Guild-Specific Storage at Halls

This PDD does not prohibit access to **guild-owned storage** at a guild hall.

Guild storage is conceptually different from a character's global city-bank access.

The exact set of locations from which the guild bank may be accessed remains a UX/economy implementation decision.

The central locked rule is that a guild hall must not replace the normal global-access personal bank/service ecosystem.

---

## 32. Hall Decoration

Guild halls should support authored decoration/display points.

Decoration should preserve:

- collision safety;
- entrance usability;
- server-authoritative ownership;
- deterministic placement;
- sensible limits.

The system does not require unrestricted freeform physics placement.

---

## 33. Guild Trophies

Trophies belong to the guild rather than permanently to one rented property.

A hall provides places to display them.

If the guild:

- moves;
- relinquishes;
- loses;

its hall, trophy ownership persists.

The new property may expose different display capacity/layout.

---

## 34. Weekly Rent

Guild halls charge **weekly rent**.

Rent is a recurring guild obligation.

The exact weekly value is authored per property.

Higher-value properties may cost substantially more because of:

- size;
- prestige;
- location;
- facilities that are permitted;
- defensive/architectural character.

---

## 35. Rent Composition

Weekly rent is conceptually divided into:

1. **upkeep/services/tax** — nonrecoverable expenditure;
2. **investment** — recoverable guild value associated with the tenancy.

This split must be visible in the hall/economy data model.

Exact percentages are not locked.

---

## 36. Recoverable Investment

The investment portion of hall payments accumulates as a recoverable tenancy value.

It exists so that long-term hall occupation is not entirely destroyed by later eviction or relocation.

The guild does not receive ordinary unrestricted ownership of the building merely because investment accumulates.

---

## 37. Voluntary Relinquishment

A guild may intentionally relinquish its hall through an appropriately permissioned action.

Some or all accumulated investment should be returned to the guild according to the authored tenancy rules.

Exact refund percentage remains provisional.

---

## 38. Arrears

If a guild cannot pay its weekly rent:

- the hall enters arrears;
- the guild does not immediately lose the property after the first missed payment.

The guild must be clearly informed of:

- missed payment;
- amount owed;
- next payment deadline;
- eviction risk.

---

## 39. Two-Missed-Payment Eviction Rule

If a guild fails to pay the rent **two weeks in a row**, it is evicted.

A guild may not remain indefinitely one payment behind.

To return to good standing before eviction, the outstanding obligation must be resolved according to the authoritative tenancy balance.

---

## 40. Eviction

Eviction:

- ends the tenancy;
- frees the property for future use;
- removes guild occupancy rights;
- does not delete the guild;
- does not delete guild trophies;
- does not delete guild history;
- does not delete ordinary guild storage.

A portion of accumulated investment is returned to the guild.

The exact eviction-refund percentage is tuning data.

---

## 41. Hall Rent Fund

The guild should have a dedicated **Hall Rent Fund** when it maintains a hall.

This balance is separate from ordinary withdrawable guild treasury gold.

Its purpose is to receive hall-specific funding and pay:

- rent;
- other explicitly authored hall-property charges if later supported.

Tariff proceeds go directly into this fund.

---

## 42. Hall Rent Fund Restrictions

Hall Rent Fund money should not be freely withdrawable as ordinary guild gold.

This ensures that a tariff presented as funding the guild hall actually funds the hall.

Guild leadership may be allowed to transfer ordinary guild treasury gold **into** the Hall Rent Fund.

The reverse transfer is not the normal operation.

---

## 43. Guild Tariff

A guild may optionally impose a **guild tariff** on its members.

The tariff is an additional guild-specific economic charge associated with ordinary eligible service/sale activity.

Tariff revenue goes directly to the Hall Rent Fund.

The exact tariff range and increments are provisional.

---

## 44. Tariff on Paid Services

For an eligible paid service, the tariff is an additional charge paid by the member.

Conceptually:

```text
normal service cost
+ guild tariff
= member total cost

normal service cost → service/economy sink
guild tariff        → Hall Rent Fund
```

The tariff must not reduce what the normal service is supposed to receive.

---

## 45. Tariff on NPC Sales

For an eligible sale to an NPC vendor, the tariff is deducted from the member's proceeds.

Conceptually:

```text
vendor purchase value
- guild tariff
= member proceeds

guild tariff → Hall Rent Fund
```

The NPC vendor does not pay above its authored purchase value merely because the seller belongs to a guild.

---

## 46. Tariff-Eligible Activity

The tariff system should be able to cover ordinary economic interactions such as:

- eligible NPC vendor sales;
- NPC repair payments;
- paid transport/services;
- lodestone fees;
- trainer/service fees;
- other explicitly tariff-eligible NPC/service transactions.

Eligibility should be explicit.

---

## 47. Tariff Exclusions by Default

The tariff should **not** automatically apply to:

- direct player-to-player trade;
- player mail transfers;
- ordinary market-sale proceeds;
- crafting work-order payments;
- arbitrary looted gold;
- quest rewards.

Such taxation would create broader economic side-effects and requires explicit future approval if desired.

---

## 48. Tariff Transparency

Members must be able to see:

- whether a tariff exists;
- current tariff rate;
- that proceeds fund the hall;
- current hall rent;
- next rent deadline;
- whether the hall is in arrears;
- Hall Rent Fund balance where policy permits.

A tariff must not be a hidden deduction.

---

## 49. Tariff Administration

Changing the tariff requires an explicit guild permission.

Changes must be audited.

Members should receive clear notification of tariff changes.

The exact delay/cooldown before a tariff change takes effect remains open.

---

## 50. Hall Access

Hall entry policy is guild-controlled subject to property/world rules.

The access model must be able to support:

- guild members;
- explicitly invited guests;
- public/open access where permitted.

Exact UI modes are implementation details.

---

## 51. Hall Access Is Separate from Hall Administration

Permission to enter a hall does not imply permission to:

- change access policy;
- move decorations;
- remove trophies;
- alter tenancy;
- spend guild money.

These are separate guild permissions.

---

## 52. Hall Occupancy

The system should not impose a small arbitrary player cap merely because a hall is considered a lower-tier property.

Practical occupancy is constrained by:

- physical space;
- networking;
- server/runtime capacity;
- authored property design.

A small house may simply feel crowded when many members attend.

---

## 53. Guild Hall and Settlement Interaction

Guild halls should increase activity in settlements rather than empty them.

A guild gathering in a rented manor should still need public areas for:

- crafting;
- markets;
- global personal banking;
- trainers;
- normal vendors/services;
- public transport.

This is a core reason for restricting private hall services.

---

## 54. Hall Relocation

A guild may later move from one property to another if eligible.

Moving must be an explicit transaction that safely resolves:

- old tenancy;
- investment return;
- new upfront cost;
- hall state;
- trophies/decorations;
- access.

The exact relocation convenience rules remain tuning data.

---

## 55. Guild Hall Persistence

Persistent hall state includes, where applicable:

- GuildID;
- WorldID;
- PropertyID;
- tenancy status;
- next rent deadline;
- arrears state;
- current rent obligation;
- investment balance;
- Hall Rent Fund;
- access policy;
- decoration/trophy placement;
- other durable property state.

It must survive:

- logout;
- disconnect;
- server restart;
- physical-server/process migration.

---

## 56. Guild Persistence

Guild state must be SQL-backed persistent server data.

Important persistent entities include conceptually:

- Guild;
- GuildMember;
- GuildRank;
- GuildPermissionSet;
- GuildInvitation/Application;
- GuildBank/Storage;
- GuildTreasury;
- GuildHallTenancy;
- GuildHallFund;
- GuildTrophy/Unlock;
- GuildAuditEntry;
- social relationship records where applicable.

Exact physical schema belongs to implementation architecture.

---

## 57. Transaction Authority

The server is authoritative for:

- guild creation;
- join/leave;
- invitations;
- applications;
- promotions/demotions;
- permission changes;
- treasury changes;
- guild-bank transfers;
- hall acquisition;
- rent payment;
- tariff collection;
- investment accounting;
- eviction;
- trophy ownership;
- hall access state.

High-value mutations must be transactional.

---

## 58. Guild Audit Log

Sensitive guild actions must be auditable.

Audit categories should include at least:

- membership changes;
- promotions/demotions;
- rank/permission changes;
- ordinary guild-gold withdrawals;
- guild-bank withdrawals;
- tariff changes;
- hall acquisition;
- hall relinquishment;
- rent payment/failure;
- eviction;
- investment refund;
- trophy removal;
- leadership transfer;
- guild disbanding.

Audit history should identify:

- actor;
- action;
- affected target/value;
- timestamp.

---

## 59. Friends

The social system should support persistent friend relationships.

Friend state should be tied to stable account/character identities rather than transient network connections.

The exact model — account-level friend, character-level friend, or both — remains open and should be resolved with Communication/Privacy design before implementation is final.

---

## 60. Ignore / Block

Players require reliable ignore/block controls.

At minimum, the system must support preventing unwanted ordinary communication from blocked identities.

The exact scope across:

- whispers;
- chat;
- invitations;
- friend requests;
- guild applications;
- matchmaking/group requests;

belongs jointly to this PDD and Communication Systems.

Block semantics must be enforceable server-side.

---

## 61. Social Presence

Guild/friend UI should be able to expose permitted presence information such as:

- online/offline;
- character name;
- level;
- class;
- current world;
- broad location where privacy rules allow;
- guild rank.

Exact privacy controls remain open for Communication/Accessibility/Privacy design.

---

## 62. Guild UI Requirements

The Guild UI should make at least the following understandable:

- guild identity;
- guild description;
- message of the day;
- roster;
- rank;
- permissions relevant to the current user;
- online/world state;
- guild bank/treasury where permitted;
- hall status;
- hall world/property;
- rent;
- next payment;
- arrears;
- tariff;
- Hall Rent Fund;
- trophies/achievements;
- audit log where permitted.

Visual presentation follows Radiant Slate.

---

## 63. Guild Hall UI Requirements

Hall management should clearly separate:

- property identity;
- weekly rent;
- upkeep portion;
- investment portion;
- accumulated recoverable investment;
- Hall Rent Fund;
- payment status;
- arrears warning;
- eviction deadline;
- access policy.

Economic obligations must not be hidden behind decorative housing UI.

---

## 64. Content Authoring Requirements

A guild-rentable property requires authored data such as:

- stable PropertyID;
- WorldID;
- settlement/region association;
- display name;
- property category/type;
- physical entrance;
- interior/runtime content reference where needed;
- weekly rent;
- rent split;
- acquisition requirements;
- access constraints;
- trophy/display points;
- decoration points/limits;
- default vacant-state dressing.

---

## 65. World Authoring Requirements

Settlement design must consider guild properties as part of population/architecture planning.

Designers should intentionally answer:

- how many guild properties exist;
- where they are;
- what social/economic tier they imply;
- how they fit the settlement;
- how public paths/services remain accessible;
- how vacant properties look;
- whether property entrances are seamless.

Guild-hall design is therefore a dependency of World and Zone authoring.

---

## 66. Economy Integration

Guild halls create intended gold sinks through:

- nonrecoverable weekly upkeep;
- possible property-related fees;
- possible acquisition costs.

The investment component is not a sink while recoverable.

Tariffs redirect member value into hall maintenance; they do not create gold.

All calculations must use the authoritative integer currency model from the Economy PDD.

---

## 67. No Guild Combat Bonuses

Guild ownership, guild hall prestige, tariff payments or guild achievements must not automatically grant ordinary character combat-stat bonuses.

A guild must not be mandatory for optimal combat statistics.

Future guild content may offer activities/rewards subject to their owning PDDs, but the guild system itself is not a passive power multiplier.

---

## 68. Provisional Numerical Recommendations

The following are **recommendations only**.

They are included to give future implementation/balancing a starting point and must not be treated as locked product rules.

| Area | Provisional target |
|---|---|
| Maximum membership | 250 characters |
| Founding group | founder + 4 co-signers from distinct accounts |
| Creation fee | 500 gold |
| Custom rank count | up to 10 |
| Initial bank tabs | 6 |
| Slots per bank tab | 98 |
| Maximum bank tabs | 10 |
| Hall upfront payment | 4× weekly rent |
| Ongoing rent split | 70% upkeep / 30% investment |
| Voluntary investment refund | 100% |
| Eviction investment refund | 75% |
| Guild tariff range | 0–10% in 1% increments |
| Active hall count | one hall per guild |

These values should be revisited during economy/content testing.

---

## 69. Provisional Hall Categories

The following categories are organisational/balancing suggestions only:

| Category | Example |
|---|---|
| Camp | command tent / fortified camp |
| Lodge | large house / guild lodge |
| Hall | townhouse / dedicated guild hall |
| Manor | manor / estate |
| Fortified | small fort / keep compound |

They are not item-rarity-style tiers and do not require a linear upgrade path.

---

## 70. Open / Deferred Decisions

The following remain open:

- exact guild membership cap;
- exact founding-signature requirement;
- exact guild creation fee;
- exact name limits;
- exact maximum custom-rank count;
- exact guild-bank tab/slot counts;
- guild-bank tab purchase costs;
- daily withdrawal-limit options;
- inactive-leader succession rules;
- exact friend relationship scope (account/character/both);
- detailed privacy/presence controls;
- exact hall acquisition requirements;
- whether a guild may ever hold more than one hall;
- exact upfront hall cost;
- exact rent split;
- exact refund percentages;
- exact tariff range/increments;
- whether tariff changes have a delay/cooldown;
- exact guild-bank access locations;
- exact decoration system;
- exact guild-achievement catalogue;
- whether any future guild calendar is implemented here or in Communication Systems.

These open values do not undermine the core architecture/design defined above.

---

## 71. Locked Design Decisions

The following are locked by this PDD:

1. Guilds are persistent server-owned organisations with stable GuildIDs.
2. Guilds are global across persistent worlds.
3. Guild membership is character-owned.
4. A character belongs to at most one ordinary guild at a time.
5. Guilds use customisable ranks.
6. Ranks use granular permissions.
7. Higher-authority members cannot be administratively controlled by lower-authority members through ordinary rank permissions.
8. Guilds do not use a mandatory guild-level XP treadmill.
9. Guilds do not grant ordinary passive combat-stat bonuses merely for membership/progression.
10. Guilds support substantial shared storage.
11. Guild storage/treasury operations are server-authoritative.
12. Sensitive guild actions are audited.
13. Guild halls are rented authored physical properties.
14. Guild-hall tenancy is tied to a specific persistent world.
15. Guild halls should be difficult/meaningful to obtain.
16. Settlements should intentionally include suitable rentable guild properties where appropriate.
17. Guild properties may range from camps/houses to manors/fortified compounds.
18. Weekly hall rent is required.
19. Weekly rent is conceptually split into nonrecoverable upkeep and recoverable investment.
20. A portion of accumulated investment is returned when tenancy ends according to the tenancy rules.
21. One missed rent payment creates arrears rather than immediate eviction.
22. Two consecutive missed weekly rent payments cause eviction.
23. Eviction does not delete the guild, guild trophies or guild history.
24. Hall-rent funding is separated from ordinary withdrawable guild money.
25. A guild may optionally impose a tariff on eligible member economic activity.
26. Tariff proceeds go directly to hall rent funding.
27. Tariffs must be transparent to members.
28. Tariff changes require permission and audit.
29. Tariffs do not automatically apply to direct player trade, mail, market sales, work-order payments, quest rewards or arbitrary looted gold.
30. Guild halls are primarily gathering, identity, meeting and trophy spaces.
31. Guild halls do not provide ordinary crafting stations by default.
32. Guild halls do not provide global character-bank access.
33. Guild halls do not provide private global-market access.
34. Guild halls do not replace normal public trainer/service hubs.
35. Guild halls should reinforce settlement activity rather than remove it.
36. Guild trophies persist independently of the currently rented property.
37. Hall access and hall-administration permissions are separate.
38. Guild-hall interiors may use guild-scoped seamless runtime instances.
39. Guild and hall persistent state must survive logout, restart and server-process changes.
40. Ignore/block semantics must be enforceable server-side.

---

## 72. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- [Communication Systems PDD](Communication-Systems-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- future PvP design.

---

## 73. Validation Criteria

The Guild and Social Systems design is correctly implemented when:

1. Guilds have stable server-owned GuildIDs.
2. One guild can contain characters associated with different persistent worlds.
3. Guild state is not tied to one server process.
4. Membership persists per character.
5. A character cannot belong to two ordinary guilds simultaneously.
6. Guilds can create and rename custom ranks.
7. Permissions can be assigned per rank.
8. Permission hierarchy prevents ordinary lower-rank control of higher-rank members.
9. Guild joining/leaving/removal is authoritative and persistent.
10. Guilds have no mandatory guild-XP level treadmill.
11. Guild membership does not grant passive combat-stat bonuses.
12. Guild storage supports granular access permissions.
13. Guild treasury/storage mutations are transactional.
14. Sensitive guild actions produce audit entries.
15. A guild can rent an authored property on a specific world.
16. The property is physically associated with a settlement/location.
17. The same physical property cannot normally have two unrelated simultaneous tenants on the same world.
18. Hall tenancy persists across server restart.
19. Hall interiors can resolve guild members/guests into the same guild-scoped interior.
20. Weekly rent is charged.
21. Rent data distinguishes upkeep from investment.
22. A failed first payment causes arrears.
23. A second consecutive weekly failure causes eviction.
24. Eviction frees the property.
25. Eviction preserves guild trophies/history/storage.
26. The defined investment portion can be refunded according to tenancy outcome.
27. Hall Rent Fund is separated from normal guild treasury.
28. Tariff proceeds cannot be silently redirected as ordinary withdrawable gold.
29. Eligible service transactions can apply a guild tariff.
30. NPC sale tariffs reduce seller proceeds rather than increasing NPC payout.
31. Members can see the current tariff.
32. Tariff changes are permissioned and audited.
33. Guild halls do not expose general crafting stations.
34. Guild halls do not expose global personal-bank access.
35. Guild halls do not expose private global market access.
36. Members still need public settlements for core crafting/trading/service activity.
37. Trophies belong to the guild and can survive hall relocation/loss.
38. Hall access can be separated from decoration/administration rights.
39. World designers can author property identity, rent, entrance, display points and vacant state.
40. Ignore/block state persists and is enforced by server-side social/communication systems.

---

## 74. Design Summary

Ninth Age guilds are **global social organisations with local physical presence**.

A guild can span persistent worlds, but its hall is a specific place in a specific world.

Guild halls are scarce, prestigious rented properties embedded in real settlements: anything from a command tent or large house to a manor or small fort. They exist primarily to create gathering places, identity and social history.

They deliberately do **not** collapse public settlement life by duplicating crafting stations, global personal banks, markets and ordinary service hubs.

Hall tenancy creates a transparent recurring economic commitment through weekly rent, a recoverable investment component, arrears and two-missed-payment eviction. Optional member tariffs can fund that commitment directly without becoming hidden guild taxation.

The intended result is:

> **Guilds should feel socially persistent across the game, while guild halls make individual worlds and settlements more socially distinctive.**
