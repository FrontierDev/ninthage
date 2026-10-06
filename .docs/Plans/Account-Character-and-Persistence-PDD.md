# Ninth Age — Account, Character and Persistence Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Account ownership, character records, character-slot limits, home-world ownership, naming identity, sessions, login/logout, disconnect recovery, world transfer, persisted gameplay state, transactional persistence, deletion/recovery, persistence-neutral gameplay boundaries, relational database direction and migration from the current JSON persistence prototype  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the product-level persistence contract for **Ninth Age**.

It is authoritative for:

- what belongs to an account versus an individual character;
- character roster limits;
- account home-world ownership;
- character identity and persistent IDs;
- first-name / last-name identity;
- session exclusivity;
- login and logout behaviour;
- disconnect recovery;
- persistence across world/server transfer;
- persistence across server restart;
- persistent versus derived gameplay state;
- resource, Health, cooldown and aura persistence expectations;
- character deletion and restoration;
- persistence transaction requirements;
- migration away from whole-file JSON persistence;
- high-level relational database requirements;
- schema migration and auditability.

This document does not own:

- the visual character-creation flow — [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md);
- combat resolution itself — [Combat System PDD](Combat-System-PDD.md);
- character stat formulas — [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- talent and ability rules — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- item rules — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- market/work-order economic rules — [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- world-instance creation and routing — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- live content compatibility and deployment policy — [Live Content and Versioning PDD](Live-Content-and-Versioning-PDD.md).

Where the current implementation conflicts with this PDD, this document defines intended behaviour.

---

## 2. Core Persistence Principle

Persistence boundaries must not create exploitable gameplay resets.

The governing rule is:

> **Logging out, disconnecting, reconnecting, restarting the server or transferring between world-server contexts must not grant a gameplay benefit that the character would not otherwise have received.**

These operations are state-continuity boundaries, not character reset boundaries.

A player must not be able to gain an advantage by deliberately:

- relogging;
- force-closing the client;
- disconnecting;
- transferring worlds;
- waiting for a server restart;
- causing a runtime actor to despawn.

This principle governs Health, resources, cooldowns, auras, death state, inventory, durability, quests, progression and all other persistent or time-dependent character state.

---

## 3. Design Pillars

### 3.1 Accounts own account-level identity

The account owns:

- account identity;
- home persistent world;
- character roster;
- Legacy progression;
- explicitly account-wide unlocks;
- account-level settings/preferences;
- future explicitly account-wide collections.

Ordinary gameplay progression remains character-owned unless another PDD explicitly says otherwise.

### 3.2 Characters own ordinary gameplay progression

Individual characters own their own:

- class and race;
- level and XP;
- active specialisation;
- talents;
- learned class abilities/ranks;
- weapon skills;
- inventory and equipment;
- durability and item-instance state;
- currency where the Economy PDD defines it as character-owned;
- quests;
- reputation;
- professions;
- action bars;
- world position;
- character-specific unlocks;
- combat-relevant persistent state.

### 3.3 Persistent inputs, derived outputs

Persist authoritative inputs.

Recalculate derived state wherever practical.

Examples of state that should normally be reconstructed rather than independently persisted include:

- effective derived statistics;
- equipment-derived bonuses;
- effective known-ability set where it can be rebuilt from persistent sources;
- talent modifiers;
- specialisation-derived abilities;
- Legacy-derived bonuses.

### 3.4 High-value changes are transactional

Ownership/progression operations must not rely on eventual whole-character snapshots where partial failure could duplicate or destroy value.

Economically or progression-significant operations require atomic persistence semantics.

### 3.5 SQL-backed server persistence

The current whole-file JSON persistence is a prototype and is not the target production architecture.

Ninth Age requires a proper server-owned relational persistence layer with explicit entities, constraints, indexes, migrations and transactions.

---

## 4. Account Identity

Every account requires an immutable stable **AccountID**.

Authentication names, usernames, email addresses or future login-provider identifiers are not substitutes for AccountID.

Account relationships should reference AccountID.

AccountID must not be reused.

---

## 5. Home World Ownership

**HomeWorldID is account-owned.**

Each account has one home persistent world at a time.

Characters on that account inherit the account's home-world relationship rather than storing independent permanent home-world selections.

Changing the account's home world is therefore an account-level transfer operation unless a future explicit system introduces exceptions.

The account's home world must not be confused with:

- the character's current map;
- dungeon instance;
- temporary server process;
- detached content map;
- runtime instance ID.

Those are runtime/current-location concepts.

---

## 6. Character Slots

Each account has:

**12 character slots.**

The slot limit applies to the account roster.

This provides space for the current class roster plus alternative characters without requiring routine deletion.

The implementation must enforce the limit server-side.

Whether soft-deleted characters consume an active slot during their recovery window remains a small UX/policy decision; restoration must never allow more than the authoritative slot limit of active characters.

---

## 7. Character Identity

Every character has an immutable globally unique **CharacterID**.

CharacterID must:

- be generated server-side;
- remain stable for the life of the character;
- survive rename;
- survive world transfer;
- survive soft deletion/restoration;
- never be reused after permanent deletion.

Persistent relationships must use CharacterID rather than:

- character name;
- roster slot;
- world process;
- connection/player ID;
- current runtime actor.

---

## 8. Character Names

A character name consists of:

- **First Name**
- **Last Name**

The complete character name is globally unique.

Individual first names and surnames may be reused provided the full normalized first-name + last-name combination is unique.

For example:

```text
Aren Vale
Aren Thorne
Mira Vale
```

may all coexist, while a second `Aren Vale` may not.

The server must enforce uniqueness through the persistence layer, not only through client-side validation.

Name normalization rules, allowed characters, length limits and rename services belong primarily to the Character Creation and Identity PDD.

---

## 9. Name Uniqueness and Soft Deletion

A soft-deleted character retains its name reservation during the 30-day recovery window.

This ensures restoration returns the same character identity without naming conflict.

The name may be released only after permanent deletion, subject to any future rename/retention policy.

---

## 10. One Account, One Session

An account may have only **one active gameplay session** at a time.

The system must never allow two simultaneous authoritative gameplay sessions for one AccountID.

A reconnect may resume/reclaim the existing session/runtime actor where appropriate.

The exact handling of a second fresh login attempt while an active session exists may be reject-or-replace policy, but simultaneous authoritative sessions are forbidden.

---

## 11. Session Identity

The architecture must distinguish:

```text
Account
Gameplay Session
Selected Character
Runtime Actor
Network Connection
```

These have different lifetimes.

A network connection disappearing does not immediately imply that:

- the gameplay session is over;
- the character is safely logged out;
- the runtime actor should despawn;
- combat state should disappear.

This distinction is required for safe reconnect behaviour.

---

## 12. Character Selection and Entry to World

Entering the world must server-validate:

- AccountID owns CharacterID;
- character is not permanently deleted;
- character is otherwise eligible to enter;
- the account does not already have another incompatible active session;
- saved state can be resolved/migrated;
- persistent definitions required by critical character identity still exist.

The client must not be able to submit arbitrary character data as authoritative state.

---

## 13. Login State Restoration

On successful character login, the server reconstructs the runtime character from authoritative persistence.

This includes, as applicable:

- identity;
- class/race;
- level/XP;
- active specialisation;
- talents;
- learned abilities/ranks;
- weapon skills;
- reputation;
- quests;
- professions;
- inventory;
- equipment;
- item-instance state;
- Health;
- resource state;
- relevant cooldown/timer state;
- relevant persistent aura state;
- death state;
- location;
- action bars;
- lockouts;
- other persistent character/world state.

Derived values are recalculated after authoritative inputs are loaded.

---

## 14. Logout Rules

Logout behaviour depends on player state/location.

### 14.1 Safe-place logout

Logout is immediate in an explicitly authored safe place.

Examples may include appropriate inns, sanctuaries, housing or other content-defined safe locations.

"Safe place" must be an explicit gameplay/content property rather than inferred from arbitrary scene names.

### 14.2 Ordinary logout

Outside a safe place, logout requires a short delay/countdown.

The exact duration remains tuning data.

### 14.3 Combat blocks logout

A character may not complete a normal logout while in combat.

### 14.4 Taking damage blocks logout

A character may not log out while taking hostile damage / while the authoritative damage lock is active.

Incoming hostile damage during a logout countdown cancels or blocks completion.

The exact recent-damage timing window may be tuned, but the player must not be able to log out between hostile damage events to evade consequences.

---

## 15. World Transfer Rules

World/server transfer is not a reset.

A transfer preserves the character's gameplay state.

A player may not transfer worlds while:

- in combat;
- taking hostile damage / under the authoritative damage lock;
- in another explicitly incompatible state defined by the destination/runtime system.

Transfer should be treated as migration of an existing character state, not spawning a fresh character.

---

## 16. Transfer State Continuity

A world/server transfer must preserve, as applicable:

- exact current Health;
- resource state;
- cooldown state;
- persistent/timed auras;
- death/alive state;
- inventory;
- equipment;
- durability;
- item-instance state;
- currency;
- quests;
- reputation;
- talents;
- active specialisation;
- learned abilities/ranks;
- weapon skills;
- professions;
- lockouts;
- all other character progression.

Only the intended world/map/instance/location context changes.

---

## 17. Disconnect Grace Period

An unexpected disconnect does not immediately log the character out.

The disconnected character remains represented by the authoritative server actor for a **minimum of 30 seconds**.

During this period:

- NPCs may continue attacking;
- players may continue interacting where rules permit;
- damage continues;
- auras continue;
- cooldowns continue;
- death may occur;
- combat state remains authoritative.

Reconnect during this grace period should restore control of the existing actor where possible rather than spawning a duplicate actor.

---

## 18. Disconnect While Combat Is Unresolved

The 30-second grace period is a minimum, not an exploit deadline.

After 30 seconds:

- if the character is safely out of combat, it may be persisted and despawned;
- if the character is still in an unresolved combat/damage state, it must not simply disappear to protect the player.

The character remains authoritative until the server reaches a valid persistence/despawn condition.

This prevents force-disconnect from functioning as a combat escape mechanic.

---

## 19. Reconnect Behaviour

When a disconnected session reconnects and the prior authoritative actor still exists:

- reconnect to the same account session;
- reassociate the same CharacterID;
- restore network ownership/control of the existing actor;
- synchronize current authoritative state;
- do not instantiate a second character actor.

If the actor has already been safely persisted/despawned, load the persisted state normally.

---

## 20. Server Restart

A server restart must not grant state-reset benefits.

Before a planned clean shutdown, persistence should checkpoint authoritative state.

After restart, characters reconstruct from persisted state and elapsed-time rules.

Unexpected process failure requires transactional persistence/checkpointing sufficient to limit rollback and avoid ownership duplication.

Exact recovery-point objectives belong to implementation/operations design, but the product requirement is that server restart is not a deliberate gameplay reset mechanism.

---

## 21. Persistent Location

Character location persistence must identify enough context to restore the character correctly.

For the seamless world this includes authoritative world-space position compatible with the World Runtime PDD's double-precision coordinate model.

Persistent location may require:

- map/world context;
- authoritative position;
- facing;
- current runtime-instance relationship where reconnectable;
- safe fallback location metadata.

Do not rely solely on a local Unity float Transform as the durable world position.

---

## 22. Invalid Saved Location Recovery

Saved locations may become invalid after content changes or instance destruction.

Login must resolve through a deliberate recovery policy rather than silently spawning at an arbitrary origin.

Conceptual recovery order:

```text
1. Reconnectable saved runtime instance, if still valid
2. Saved persistent-world/map position, if valid
3. Authored safe checkpoint / return location
4. Appropriate regional/settlement fallback
5. Character starting location as final recovery
```

Critical recovery failures must be logged visibly.

---

## 23. Instance Reconnect

If a player disconnects from an instance and that instance remains valid:

- preserve the association needed to reconnect to the same group-owned/runtime instance;
- do not create a duplicate replacement instance.

If the instance has legitimately ceased to exist, return the character through an authored instance fallback such as:

- entrance;
- checkpoint;
- appropriate world return location.

Instance ownership rules remain defined by the World Runtime and Group PDDs.

---

## 24. Health Persistence

**Current Health is persistent character state.**

Logout, reconnect, transfer and server restart must not refill Health.

A character who leaves a valid persistence boundary at a given current Health returns with that Health unless another explicit gameplay system legitimately changes it.

There is no implicit offline healing merely because the player disconnected or logged out.

Current Health must be bounded against the reconstructed effective maximum Health on load.

If maximum Health changed while offline because persistent inputs changed through another legitimate system, the resolution must be deterministic and must not create an exploit.

---

## 25. Death Persistence

Alive/dead state persists.

A dead player must not become alive by:

- logging out;
- reconnecting;
- transferring worlds;
- restarting the client;
- waiting for a server restart.

The eventual death/corpse/graveyard system defines the exact additional state required, but persistence must retain enough information to resume that system correctly.

---

## 26. Resource Persistence

Resources must be persisted according to resource-specific gameplay rules.

The system must not use one blanket "resources are transient" rule for:

- Mana;
- Energy;
- Rage;
- Focus;
- combo-point-like resources;
- class-specific resources.

For each resource, the persistence system must know enough to restore or advance it without logout exploitation.

---

## 27. Offline Resource Resolution

A resource may require:

- exact current value;
- last authoritative update timestamp;
- normal regeneration rule;
- normal decay rule;
- reset rule, where that reset would also occur without logout;
- other resource-specific persistent metadata.

Conceptually:

```text
resolved current value =
    saved current value
  + legitimate elapsed-time regeneration
  - legitimate elapsed-time decay
  + explicit resource-system adjustments
```

bounded by normal limits.

A resource may remain exactly unchanged offline if that best matches its gameplay rules.

No resource may gain a special benefit solely because the client disconnected.

---

## 28. Cooldown Persistence

Relogging must not reset cooldowns.

Cooldown state that could be exploited through logout must survive persistence boundaries or be represented through an authoritative expiry/recharge model.

For real-time cooldowns, absolute server timestamps are preferred conceptually:

```text
AbilityID
Charge state
Recharge/expiry timestamp(s)
```

Elapsed offline time may consume a cooldown only where the same real-world time progression is intended by the ability/system.

Cooldown persistence includes, where relevant:

- ability cooldowns;
- charge recharge;
- consumable cooldowns;
- profession cooldowns;
- long-duration class-mechanic timers;
- other gameplay restrictions.

---

## 29. Aura Persistence

Aura handling follows the same anti-exploit principle.

A player must not be able to:

- remove a harmful effect by logging out;
- preserve a beneficial timed effect indefinitely by logging out;
- reset stacks or timing advantageously through transfer.

Short combat auras will usually resolve while the disconnected actor remains present.

Longer-lived auras that can outlive runtime actor despawn must persist enough state to continue correctly.

---

## 30. Persistent Aura State

Where required, persistent aura state may include:

- aura definition ID;
- caster/source identity where gameplay-relevant;
- target CharacterID;
- stacks;
- remaining duration or absolute expiry;
- tick state/timestamp;
- other explicit runtime data required to reproduce the aura correctly.

Only auras that need to survive the persistence boundary should be serialized.

Ordinary runtime combat aura state should not be stored redundantly once it can no longer affect the character.

---

## 31. Timed State in General

Any timer or expiry system must explicitly define offline behaviour.

Examples include:

- raid/dungeon lockouts;
- profession cooldowns;
- mail expiry;
- market listing expiry;
- work orders;
- repeatable quest cadence;
- temporary access states;
- long-duration buffs/debuffs;
- world-event eligibility;
- other time-based restrictions.

Use authoritative server time.

A client clock must never determine persistent expiry.

---

## 32. Account-Owned State

At minimum the account owns:

- AccountID;
- authentication/account record;
- HomeWorldID;
- character roster;
- Legacy state;
- explicitly account-wide unlocks/settings.

Account-owned data must not be duplicated independently into every character as separate truth.

---

## 33. Legacy Persistence

Legacy is account-owned.

Persistent account structure should conceptually separate it:

```text
Account
├── HomeWorldID
├── LegacyState
└── Characters[]
```

Characters consume Legacy benefits while resolving gameplay state.

A character does not own a private copy of account Legacy progression.

---

## 34. Character-Owned State

Character persistence includes, as applicable:

- CharacterID;
- FirstName;
- LastName;
- title/identity fields;
- race;
- class;
- faction;
- level;
- XP;
- active specialisation;
- trainer-learned abilities/ranks;
- talent allocation;
- weapon skills;
- quests;
- reputation;
- professions;
- inventory/item instances;
- equipment;
- currency;
- Health;
- resource state;
- relevant timers/cooldowns;
- relevant auras;
- death state;
- world position/context;
- action bars;
- lockouts;
- character-specific progression/unlocks.

Other subsystem PDDs remain authoritative for the exact domain-specific fields.

---

## 35. Derived State

The database should not become a dump of every runtime-calculated value.

Prefer reconstructing:

- effective stats;
- rating percentages;
- equipment stat contributions;
- effective maximum resources;
- specialisation-granted abilities;
- talent-granted abilities;
- talent spell modifiers;
- set bonuses;
- Legacy-derived bonuses;
- other deterministic effective state.

Persist the authoritative inputs that generate them.

---

## 36. Item Persistence

Item instances require stable server-owned identity.

Persistent item state should use stable item-instance IDs and authored item-definition IDs.

Mutable item-instance data belongs to the instance record, including as relevant:

- stack count;
- durability;
- binding/ownership;
- modifications;
- gems;
- enchantment;
- cosmetic/transmog state;
- storage/equipment location.

Do not duplicate full authored ScriptableObject item definitions into persistent database rows.

---

## 37. Transactional Ownership Changes

The following kinds of operations require atomic persistence semantics:

- item transfer;
- currency transfer;
- direct trade;
- market purchase/sale;
- mail attachment transfer;
- work-order escrow/output;
- inventory/equipment mutation where ownership would otherwise duplicate;
- respec fee plus talent reset;
- trainer fee plus learned ability/rank;
- important reward claims;
- similar high-value state mutation.

A successful transaction must commit all required ownership changes together.

Failure must leave the prior valid state intact.

---

## 38. Snapshot / Checkpoint State

Not every frequently changing value requires a fully normalized write on every frame.

Lower-risk continuous state may be checkpointed appropriately, such as:

- position;
- facing;
- current Health/resources;
- action-bar/UI-owned persistent configuration;
- other high-frequency state.

Checkpoint frequency is an implementation/operations decision, but must be sufficient to make crash rollback acceptable and must not replace transactions for high-value ownership changes.

---

## 39. Important Save Boundaries

In addition to transactional writes and periodic checkpoints, the server should checkpoint appropriate state on boundaries such as:

- clean logout;
- safe disconnect despawn;
- world/server transfer;
- entering/leaving relevant instances;
- major progression changes;
- level-up;
- death/resurrection;
- major quest completion;
- clean server shutdown.

These are safety points, not the only persistence mechanism.

---

## 40. Character Deletion

Character deletion uses **soft deletion for 30 days**.

During the recovery window:

- the character retains CharacterID;
- the character data remains recoverable;
- the global full name remains reserved;
- the character is hidden from ordinary active play/selection.

After the recovery period, the character is eligible for permanent deletion according to retention/cleanup policy.

CharacterID is never reused.

---

## 41. Character Restoration

Restoration restores the original character.

It does not clone the character or generate a new CharacterID.

Relationships that reference CharacterID therefore remain stable.

Restoration must respect the 12-active-character slot limit.

---

## 42. Character Creation Constraints

The persistence layer must enforce:

- account active-character slot limit;
- global full-name uniqueness;
- valid AccountID ownership;
- immutable CharacterID generation;
- required race/class IDs;
- required persistent identity state.

Client-side checks are convenience only.

The Character Creation and Identity PDD owns appearance and detailed naming/content rules.

---

## 43. Global Name Constraint

Global first+last uniqueness should be enforced by a database-level unique constraint/index on normalized name identity, not by an in-memory scan alone.

This must remain safe under concurrent character creation requests.

---

## 44. Account Session Constraint

One-account-one-session must be enforced by server session authority.

The persistence/session design must prevent:

- two processes independently accepting the same account as actively playable;
- two characters from the same account being active simultaneously;
- reconnect spawning duplicate actors.

If the architecture becomes multi-process/multi-node, session ownership must be coordinated outside a process-local dictionary.

---

## 45. Persistence Technology Direction

The production persistence implementation must move away from the current `player_data.json` whole-file store.

The target is a **relational SQL-backed server persistence layer**.

The reference implementation pattern is the project's sibling repository:

`FrontierDev/webrpgapp`

which uses:

- Entity Framework Core;
- PostgreSQL through Npgsql;
- a dedicated persistence project/layer;
- a central DbContext;
- per-entity configuration classes;
- proper foreign keys/indexes;
- explicit schema migrations;
- selective PostgreSQL JSONB for structures that genuinely benefit from document-shaped storage.

Ninth Age should follow this architectural separation and relational approach.

Exact package/runtime versions must remain compatible with the chosen Unity/headless-server environment.

---

## 46. Preferred Database Architecture

Conceptually:

```text
Authoritative Game Server
        ↓
Persistence / Repository / Service Layer
        ↓
ORM / SQL access layer
        ↓
Relational SQL Database
```

Gameplay components should not scatter direct database queries throughout:

- PlayerActor;
- inventory;
- talents;
- quests;
- combat managers;
- UI/network components.

Persistence access should be mediated through explicit server-side services/interfaces.

---

## 47. PostgreSQL Direction

PostgreSQL is the preferred/reference production database based on the proven `webrpgapp` pattern.

The persistence design should assume support for:

- UUID primary keys;
- transactions;
- foreign keys;
- unique constraints;
- indexes;
- timestamps with time zone;
- relational joins;
- selective JSONB where appropriate;
- migrations.

A different SQL provider should only replace it through an explicit technical architecture decision that preserves these product requirements.

---

## 48. Relational Entity Direction

The exact schema belongs to implementation design, but the database should model important ownership/state as real entities rather than one serialized CharacterData blob.

Expected domains include structures equivalent to:

```text
Accounts
AccountLegacy
Characters
CharacterTalents
CharacterAbilities
CharacterWeaponSkills
CharacterReputations
CharacterQuests
CharacterProfessions
CharacterResources
CharacterCooldowns
CharacterPersistentAuras
ItemInstances
CharacterInventory
CharacterEquipment
CharacterActionBars
CharacterWorldState
CharacterLockouts
Mail
Market / Escrow state
Audit / Transaction records
```

Not every domain requires exactly one table and table names are not product authority.

---

## 49. JSON / JSONB Use

JSON is not forbidden.

It is inappropriate as the sole monolithic persistence model.

Selective JSONB/document-shaped fields are acceptable where:

- the payload is naturally variable;
- relational querying is not important;
- atomic ownership constraints do not depend on individual embedded fields;
- schema migration remains manageable.

Core identities, ownership, balances and relational constraints should remain relational.

---

## 50. Stable Definition IDs

Persistent gameplay rows should reference stable authored definition IDs for content such as:

- race;
- class;
- specialisation;
- talent;
- ability;
- item definition;
- quest;
- faction;
- profession;
- aura;
- other authored definitions.

Do not persist Unity object references as the durable database contract.

Runtime definitions resolve from stable IDs.

---

## 51. Missing Definitions

If saved data references an authored definition that no longer exists, the server must not silently substitute another definition.

Critical identity failures such as missing:

- class;
- race;
- active specialisation where required

must fail safe and require migration/recovery.

Peripheral obsolete content may be explicitly migrated/removed by a versioned migration.

Silent fallback is not acceptable.

---

## 52. Schema Versioning and Migrations

Persistent data requires explicit schema evolution.

Database changes must use versioned migrations.

Migration behaviour must be:

- deliberate;
- testable;
- forward-moving;
- auditable;
- safe against partial deployment.

The existing `webrpgapp` EF migration model is the reference approach.

Content/definition migration and client/server compatibility remain shared concerns with the Live Content and Versioning PDD.

---

## 53. Migration from Current JSON Persistence

The current `player_data.json` data must not simply be discarded when the SQL persistence implementation replaces it.

An implementation migration plan must:

1. read the existing PlayerAccountData / CharacterData JSON format;
2. validate source records;
3. generate/retain stable account and character identities;
4. split account/character collections into relational entities;
5. preserve inventory/item identities where valid;
6. preserve progression state;
7. preserve current persisted resources;
8. report invalid/unmigratable records explicitly;
9. verify the resulting SQL state;
10. avoid re-importing the same source data multiple times.

The exact one-time migration tooling belongs to implementation planning.

---

## 54. Auditability

Current snapshots answer:

> What does the character/account own now?

Audit records answer:

> How did that state change?

Significant economic or administrative operations require auditable records where practical, including:

- large currency transfer;
- trade;
- market transaction;
- mail transfer;
- work order;
- high-value item grant/removal;
- administrative correction;
- character/account transfer;
- other abuse-sensitive ownership changes.

Audit history should not depend solely on the mutable current character snapshot.

---

## 55. Server Authority

The server is authoritative for all gameplay-relevant persistent state.

The client may cache/display character data but cannot authoritatively mutate:

- identity;
- progression;
- items;
- currency;
- talents;
- known abilities;
- reputation;
- quests;
- professions;
- Health/resources;
- world position at persistence boundaries;
- deletion/restoration;
- Legacy.

All persistent mutations require server validation.

---

## 56. Current Implementation — Foundations to Retain

The current implementation contains useful concepts:

### 56.1 PlayerAccountData

There is already a server-owned account object containing authentication fields and characters.

The concept remains useful, but it must become relational/account-ID-driven rather than a whole-file serialized object.

### 56.2 CharacterData

CharacterData already captures several persistent domains:

- identity;
- level/XP;
- class/race/faction;
- known spells;
- inventory;
- equipment;
- weapon skills;
- talents;
- reputation;
- quests;
- resource values.

This is a useful DTO/prototype inventory of required data, not the target database shape.

### 56.3 Stable item GUID concepts

Inventory/equipment persistence already carries item GUID fields.

The stable item-instance identity concept should remain.

### 56.4 Server connection/session separation

ServerConnectionManager already distinguishes connected account data and PlayerSessionData.

This is a foundation for a stronger account/session/runtime-actor lifecycle.

---

## 57. Current Implementation — Required Changes

### 57.1 Replace PlayerDatabase whole-file JSON

Current PlayerDatabase:

- holds all accounts in a process-local dictionary;
- reads one JSON file at startup;
- rewrites the whole file on save;
- keys accounts by username.

This is not suitable as production MMORPG persistence.

Replace it with relational server persistence.

### 57.2 Introduce AccountID

PlayerAccountData currently lacks an immutable AccountID.

Add stable account identity and use it for relationships.

### 57.3 Add account HomeWorldID

HomeWorldID must be persistent account state.

Characters inherit this account-level home-world identity.

### 57.4 Enforce 12-character limit

Current CharacterCreationService appends characters without enforcing the final 12-slot limit.

Enforce it transactionally/server-side.

### 57.5 Replace single Name with FirstName / LastName

Current CharacterData and NewCharacterData use one `Name` field.

Migrate to separate first/last fields with normalized global-combination uniqueness.

### 57.6 Database-enforce global name uniqueness

Current creation validation only checks empty/length rules.

Global full-name uniqueness must be concurrency-safe at the database constraint level.

### 57.7 Soft deletion

Current character model has no 30-day soft-deletion lifecycle.

Add deletion timestamps/state and restoration flow.

### 57.8 One-account-one-session

Current session ownership is process-local and keyed by network PlayerID.

Enforce one authoritative account session and prevent duplicate active sessions.

### 57.9 Disconnect grace handling

Current `ServerConnectionManager.OnPlayerLeft` immediately saves, unregisters and despawns the actor.

Replace this with the 30-second minimum disconnected-actor grace period and unresolved-combat rule.

### 57.10 Resource saving is incomplete

CharacterData contains `SavedResources` and ServerStatManager can restore them, but the current repository does not contain a corresponding authoritative path that writes live resource values into `SavedResources` before `SavePlayerAccount(account)`.

Implement explicit live-state capture before persistence and remove reliance on stale DTO contents.

### 57.11 Persist current Health exactly

Health must be captured from the authoritative runtime actor and restored without logout refill.

### 57.12 Resource-specific persistence

Do not treat all `SavedResources` identically if their offline behaviour differs.

Persist the metadata required by resource semantics.

### 57.13 Cooldown persistence

Current character persistence has no authoritative cooldown persistence contract.

Add state/expiry persistence where relog reset would be exploitable.

### 57.14 Persistent aura support

Add persistence for aura instances that can legitimately outlive actor despawn.

Do not persist every ordinary transient combat aura unnecessarily.

### 57.15 Persistent world/location state

Current CharacterData does not define the complete authoritative seamless-world position/instance recovery model required here.

Add durable world/map/position/facing/reconnect metadata.

### 57.16 Stop mutating giant account snapshots

Subsystems should persist their authoritative mutations through persistence services/transactions rather than modifying a shared in-memory account blob and rewriting all accounts.

### 57.17 Relationalize item ownership

Inventory/equipment item state must become relational item-instance ownership/location state suitable for atomic trade/mail/market/work-order operations.

### 57.18 Add migrations

Replace ad hoc JSON field compatibility as the main production strategy with database migrations plus explicit content-data migrations.

---

## 58. Data Integrity Requirements

The persistence layer must enforce integrity through database/application constraints.

Examples:

- unique AccountID;
- unique CharacterID;
- unique normalized full character name;
- valid account → character ownership;
- at most 12 active characters per account at the service/transaction level;
- one authoritative active account session;
- valid item ownership/location;
- no duplicate item-instance ownership;
- nonnegative currency;
- valid relational foreign keys;
- atomic escrow/transfer semantics.

---

## 59. Concurrency

Persistence operations must assume multiple server requests/processes may eventually act concurrently.

Correctness must not depend on:

- "only one Unity process currently writes this file";
- process-local dictionaries;
- client ordering;
- unchecked read-modify-write snapshots.

Use database transactions, constraints and appropriate concurrency controls.

---

## 60. Failure Behaviour

Persistence failures must fail visibly.

Do not silently:

- drop character progress;
- replace missing critical definitions;
- duplicate items;
- zero currency;
- refill Health;
- clear cooldowns;
- clear harmful persistent auras;
- spawn at arbitrary origin;
- create a duplicate runtime actor.

Where safe recovery is possible, use an explicit recovery policy.

Otherwise reject the operation and log actionable diagnostics.

---

## 61. Security Boundary

Persistence/database credentials and direct database connectivity belong only to trusted server infrastructure.

Clients must never receive direct database credentials or issue SQL/ORM operations.

Authentication secrets must be stored using appropriate server-side password/authentication practices.

Exact identity-provider/authentication implementation remains technical architecture.

---

## 62. Locked Design Decisions

The following are locked:

1. Accounts have immutable AccountIDs.
2. Characters have immutable globally unique CharacterIDs.
3. CharacterIDs are never reused.
4. Accounts have 12 character slots.
5. HomeWorldID is account-owned.
6. Characters inherit the account home world.
7. Character names consist of First Name + Last Name.
8. The normalized full first+last combination is globally unique.
9. Soft-deleted characters retain their name reservation during recovery.
10. One account may have only one active gameplay session.
11. Disconnect uses a minimum 30-second authoritative actor grace period.
12. A disconnected actor does not despawn merely because 30 seconds elapsed if combat remains unresolved.
13. Immediate logout is allowed in explicitly authored safe places.
14. Ordinary logout outside safe places uses a short delay.
15. Combat blocks logout.
16. Taking hostile damage / authoritative damage lock blocks logout.
17. Combat and hostile damage block world transfer.
18. World/server transfer preserves character gameplay state rather than resetting it.
19. Current Health persists exactly across logout/reconnect/transfer/restart.
20. Logging out does not provide offline Health refill.
21. Death state persists.
22. Resources use resource-specific persistence/offline rules.
23. Cooldowns cannot be reset through relogging.
24. Auras cannot be beneficially cleared/frozen through relogging.
25. Timed state uses authoritative server time.
26. Ordinary character progression is character-owned.
27. Legacy is account-owned.
28. Derived effective state should normally be reconstructed from persistent inputs.
29. High-value ownership/progression operations require atomic persistence semantics.
30. Character deletion is soft for 30 days.
31. Restoration preserves the original CharacterID.
32. Persistent gameplay state is server-authoritative.
33. Production persistence moves away from whole-file JSON.
34. Production persistence uses a relational SQL-backed architecture.
35. PostgreSQL + EF-Core-style relational persistence from `FrontierDev/webrpgapp` is the implementation reference direction.
36. Persistent schema evolution uses explicit migrations.
37. Stable authored definition IDs, not Unity object references, form the durable content contract.
38. Missing critical definitions fail safe rather than silently substituting content.
39. Significant economic/admin ownership changes should be auditable.
40. Client disconnect, login, restart and transfer are not gameplay reset mechanisms.

---

## 63. Open / Deferred Details

The architecture is ready for implementation planning.

The remaining details are not blockers to this PDD.

### 63.1 Ordinary logout delay

Exact countdown duration.

### 63.2 Damage-lock timing

Exact recent-hostile-damage duration used for logout/transfer blocking.

### 63.3 Duplicate-login UX

Whether a fresh second login:

- rejects; or
- explicitly replaces the old session.

It must never allow both simultaneously.

### 63.4 Soft-deleted slot accounting

Whether a soft-deleted character continues consuming a visible slot, or restoration requires a free active slot.

The 12-active-character maximum remains authoritative.

### 63.5 Individual resource offline formulas

Defined by resource/class design.

### 63.6 Checkpoint cadence

Exact persistence interval / crash recovery objective.

### 63.7 Exact SQL schema decomposition

The entity domains are defined, but table boundaries/names may change during architecture work.

### 63.8 Database hosting/topology

Single-node versus replicated/managed PostgreSQL and operational backup strategy belong to infrastructure design.

---

## 64. Validation Criteria

The Account, Character and Persistence system satisfies this PDD when all of the following are true.

### 64.1 Account and roster

- Every account has immutable AccountID.
- Every account has HomeWorldID.
- An account cannot have more than 12 active characters.
- Legacy is account-owned, not duplicated as character truth.

### 64.2 Character identity

- Every character has immutable CharacterID.
- CharacterID survives rename, transfer and restoration.
- CharacterID is never reused.
- FirstName and LastName are stored separately.
- Duplicate normalized full names are rejected globally, including concurrent creation attempts.

### 64.3 Sessions

- One account cannot have two simultaneous authoritative gameplay sessions.
- Reconnect does not spawn a duplicate character actor.
- Disconnect does not immediately despawn the character.

### 64.4 Disconnect

- Disconnected actor remains authoritative for at least 30 seconds.
- Combat/damage continues during disconnect.
- Death can occur while disconnected.
- Actor remains if combat is unresolved after the minimum grace period.
- Safe resolved actors eventually persist and despawn.

### 64.5 Logout and transfer

- Safe-place logout can complete immediately.
- Ordinary logout uses the configured delay.
- Combat blocks logout.
- Hostile damage blocks/cancels logout.
- Combat/hostile damage blocks world transfer.
- Transfer preserves Health/resources/cooldowns/persistent auras/progression.

### 64.6 Health and resources

- Current Health is captured from live authoritative state.
- Relog does not refill Health.
- Server transfer does not refill Health.
- Server restart does not refill Health.
- Dead characters do not resurrect through relog.
- Each resource restores/advances according to its explicit persistence policy.
- Resource state cannot be beneficially reset by relog.

### 64.7 Cooldowns and auras

- Relogging cannot clear meaningful cooldowns.
- Offline elapsed time is applied only according to intended timer semantics.
- Harmful persistent effects cannot be removed by relog.
- Beneficial timed effects cannot be frozen indefinitely by relog.
- Short combat auras continue while disconnected actor remains present.

### 64.8 Ownership and transactions

- Trades cannot duplicate items/currency if a write fails midway.
- Market/mail/work-order transfers are atomic.
- Trainer purchase and learned rank commit together.
- Respec charge and respec state commit together.
- Item instance has one authoritative owner/location at a time.

### 64.9 Database

- Production server no longer relies on rewriting `player_data.json` as the authoritative store.
- Core persistence is relational SQL-backed.
- UUID/ID relationships and database constraints are used.
- Schema migrations are versioned/tested.
- Important indexes/unique constraints exist.
- Selective JSONB does not replace critical relational ownership rules.
- Database credentials remain server-only.

### 64.10 Migration

- Existing valid JSON accounts/characters can be imported once into the SQL schema.
- Import preserves character identity/progression/item state where valid.
- Invalid records are reported explicitly.
- Re-running migration cannot silently duplicate accounts/characters/items.

### 64.11 Recovery

- Invalid saved location uses an explicit safe recovery path.
- Missing critical content definitions fail visibly.
- Persistent state can be reconstructed without trusting client copies.
- Server restart does not act as a deliberate gameplay reset.

---

## 65. Design Summary

Ninth Age treats persistence as continuous MMORPG state, not as a save-game snapshot convenience.

The central rule is that leaving and re-entering a session must not create gameplay advantages.

Accounts own:

- home world;
- roster;
- Legacy;
- explicit account-wide state.

Characters own ordinary progression and gameplay state.

Characters have globally unique two-part names, immutable IDs and 12 account-wide slots. One account may operate only one gameplay session at a time. Disconnect keeps the authoritative actor active for at least 30 seconds and longer when combat remains unresolved.

Health, resources, cooldowns, auras, death and timed restrictions are handled according to their actual gameplay semantics rather than blindly reset at login.

Production persistence moves from the current monolithic JSON prototype to a relational SQL-backed server architecture, using the EF Core/PostgreSQL structure in `FrontierDev/webrpgapp` as the implementation reference: explicit entities, relationships, constraints, transactions, migrations and selective JSONB only where appropriate.

The resulting persistence boundary should be mechanically boring to the player:

> **Logging out changes whether the client is connected. It does not reset the character.**
