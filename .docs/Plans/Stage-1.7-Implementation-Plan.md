# Ninth Age — Stage 1.7 Implementation Plan

**Status:** Approved architecture; implementation issues not yet written  
**Project:** Ninth Age  
**Stage:** 1.7 — Relational persistence, transactional state and timed-state continuity  
**Repositories:** `FrontierDev/ninthage` (`dev`) and `FrontierDev/ninthage-master` (`main`)  
**Last updated:** 2026-10-28

---

## 1. Purpose

Stage 1.7 replaces the Unity World Server's whole-file `player_data.json` character authority with a central relational persistence foundation owned by the Master Server application and backed by PostgreSQL.

The stage does **not** move live gameplay simulation into the Master Server or database.

The core model is:

```text
PostgreSQL
    ↑
Master persistence services
    ↑
World Server
    ↑
live authoritative gameplay
```

While a character is online, its World Server owns the live working state used by gameplay. PostgreSQL stores the latest committed durable state needed for recovery, ownership integrity and future transfer. The Master Server coordinates durable identity, persistence access and which World Server is allowed to write a character.

Stage 1.7 is a foundation stage. It contains **no new regression-test campaign and no performance benchmarking**. Validation is limited to compilation/static checks, database migration rehearsal and narrow owner-operated functional smoke checks. Systematic regression coverage is deferred to Stage 1.10 and integrated stress/performance verification to Stage 1.11.

---

## 2. Authoritative references

### Ninth Age repository

- `.docs/MMORPG-Master-PDD.md`
- `.docs/Plans/Phase-1-Implementation-Plan.md`
- `.docs/pdd/Account-Character-and-Persistence-PDD.md`
- `.docs/pdd/Character-Creation-and-Identity-PDD.md`
- `.docs/pdd/Character-Stats-and-Progression-PDD.md`
- `.docs/pdd/Items-Equipment-and-Loot-PDD.md`
- `.docs/pdd/World-Runtime-and-Instancing-PDD.md`
- `.docs/CSharp-Architecture.md`
- `.docs/CSharp-Style-Conventions.md`
- `.docs/GitHub-Issue-and-Workflow-Guide.md`
- `AGENTS.md`

### Master Server repository

- `.docs/Master-Server-PDD.md`
- `.docs/Implementation-Plan.md`
- `.docs/CSharp-Style-Conventions.md`
- `.docs/GitHub-Issue-and-Workflow-Guide.md`
- `AGENTS.md`

Where product behaviour and this plan conflict, the relevant PDD remains authoritative. This plan resolves the implementation decisions deliberately delegated to Stage 1.7 by those PDDs.

---

## 3. Locked Stage 1.7 architecture

### 3.1 World Server owns live character simulation

After successful world entry, the World Server owns the live character state required by gameplay, including:

- movement and authoritative position;
- combat state;
- current Health and resources;
- runtime cooldown and aura state;
- inventory/equipment representation;
- current progression changes;
- current quest/reputation/weapon-skill state already supported by the runtime;
- the live `PlayerActor` and its ECS/GameObject representation.

Normal gameplay does not round-trip through the Master Server or PostgreSQL on every simulation change.

The World Server remains the only simulation authority for that world.

### 3.2 Master Server owns global durable coordination

The Master Server owns:

- durable `WorldID`, `AccountID` and `CharacterID` identity;
- account home-world relationship;
- durable character roster;
- which registered World Server is permitted to write a character's persistent state;
- the persistence service boundary;
- character creation/deletion/restoration transactions;
- cross-world transfer coordination foundations;
- database schema/migration ownership through `NinthAge.Persistence`.

The Master Server does **not** own movement, combat, NPC/AI, physics, world ticking or actor simulation.

### 3.3 PostgreSQL is durable storage, not a gameplay runtime

PostgreSQL stores committed state that must survive:

- logout;
- World Server crash/restart;
- Master Server restart;
- client disconnect;
- future world transfer;
- machine restart.

It is the durable recovery and ownership ledger. It is not queried every frame and is not used as the live combat/movement state store.

### 3.4 Account state is not migrated wholesale into a World Server

The World Server receives only the account/character information required to run the selected character and validate its current persistence lease.

Global account identity, home world and roster remain Master-owned. A World Server must not become the durable authority for an account simply because one of its characters is online there.

---

## 4. Technology baseline

The Master Server already targets `.NET 10`.

Stage 1.7 uses:

- PostgreSQL **18**, kept on the current supported 18.x minor release;
- Entity Framework Core **10.0.13**;
- `Microsoft.EntityFrameworkCore.Design` **10.0.13** for design-time migrations;
- `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3**;
- the provider's compatible Npgsql dependency.

The database packages live only in `ninthage-master`, primarily `NinthAge.Persistence`. Unity does not reference EF Core or Npgsql.

`NinthAge.Persistence` owns:

- the EF `DbContext`;
- relational entity types;
- per-entity EF configuration;
- migrations;
- repository/service implementations used by Master application services.

The Master application owns API/authentication/composition. `NinthAge.Contracts` owns wire-facing request/response DTOs shared with the World Server.

Production startup must not silently run pending migrations. Schema upgrades are explicit operator/deployment actions. Development may use an explicit migration command/workflow.

---

## 5. World Server ↔ Master Server communication

### 5.1 Transport

Stage 1.7 uses the existing ASP.NET Core service direction:

- HTTP(S);
- JSON request/response payloads;
- shared DTOs in `NinthAge.Contracts`;
- no gRPC/message bus introduced for Stage 1.7.

The local development workflow uses `http://127.0.0.1:5080` by default. The World Server reads the endpoint from `NINTHAGE_MASTER_URL`; Development may fall back to that localhost URL, while non-Development startup requires an explicit HTTPS URL.

The Unity World Server uses one long-lived `System.Net.Http.HttpClient` owned by the Master/persistence client service. It must not create an HttpClient per persistence operation.

### 5.2 World Server authentication

Persistence endpoints never trust network reachability alone.

Each World Server deployment/world has one configured opaque **256-bit random secret**. Requests use:

```text
Authorization: Bearer <world-server-secret>
```

The World Server reads its secret from `NINTHAGE_WORLD_SERVER_SECRET`. The secret is never accepted as a command-line argument.

The Master owns a configuration mapping from the canonical WorldName to the expected secret. Production values come from normal ASP.NET Core secret/environment configuration rather than source-controlled files. The Development world store may persist a generated local-development secret in its existing per-user application-data state and the Development process launcher injects that secret into the child World Server environment.

Rules:

1. the Master authenticates the credential before disclosing whether the requested world/account/character exists;
2. authentication establishes the one WorldName the credential may operate;
3. registration must resolve that authenticated WorldName to the same durable WorldID returned to the server;
4. later heartbeat/persistence requests must use that WorldID and current WorldServerRunID;
5. secret comparison is constant-time and secrets are never logged;
6. credential rotation/hardened secret-management infrastructure may be extended later, but Stage 1.7 does not store World Server secrets in gameplay/client data or PostgreSQL.

The existing Development-only loopback endpoints may remain temporarily for local bootstrap compatibility while the authenticated `/world/v1/*` boundary is introduced. Stage 1.7 persistence operations use the authenticated boundary.

---

### 5.3 Cross-repository contract ownership

The two repositories remain independently buildable. Stage 1.7 does **not** add a git submodule, copied DLL, NuGet/UPM dependency or generated shared package between them.

`ninthage-master/src/NinthAge.Contracts` is the canonical wire-protocol definition. The HTTP API is versioned under:

```text
/world/v1/...
```

The Unity repository defines the minimal matching transport DTOs it consumes under the server-side Master integration namespace, with explicit JSON field names matching the canonical contract. These Unity DTOs are transport models only and must not become a second source of product/business rules.

Contract changes are made Master-contract first, then the paired Unity client issue updates the mirrored transport shape before either side is considered integrated.

The Unity client uses Newtonsoft.Json already available in the project; ASP.NET Core may continue using System.Text.Json. The wire contract therefore uses ordinary JSON primitives/arrays/objects and explicit property names rather than serializer-specific polymorphism.


## 6. Durable WorldID cut-over

Stage 1.7 begins by completing the Unity World Server registration/heartbeat integration already planned as Master F1.3b.

A World Server launched with:

```text
--world <world-name>
```

must:

1. generate one `WorldServerRunID` for the process lifetime;
2. register with the configured local/deployment Master Server;
3. receive the durable Master-owned `WorldID`;
4. retain that `WorldID` for the process lifetime;
5. heartbeat at the Master-provided cadence;
6. unregister on orderly shutdown;
7. fail startup before actor admission if durable world registration cannot be established.

The Stage 1.2 launch-generated GUID is removed only at this integration point. There must never be two competing current `WorldID` authorities in one World Server process.

### 6.1 Durable world record

The initial relational world row contains:

- `WorldID` — UUID primary key, immutable;
- `WorldName` — required, unique human-readable configured name;
- `CreatedAtUtc`;
- `RetiredAtUtc` — nullable; non-null means the durable world is retired from normal admission.

Runtime fields such as `WorldServerRunID`, heartbeat timestamps, PID, online/offline state and lease timing remain in the Master runtime registry, not in the durable world row.

When PostgreSQL persistence is configured, PostgreSQL is the durable world source used to compose the runtime registry. The existing per-user development world JSON remains available only for explicitly database-free local development and is never treated as production durable authority.

---

## 7. Identity model

Use PostgreSQL UUID / C# `Guid` for:

- `WorldID`;
- `AccountID`;
- `CharacterID`;
- `ItemInstanceID`;
- persistence `OperationID`;
- `CharacterPersistenceLeaseID`.

Stable authored content/definition IDs remain canonical strings and are never replaced with Unity object references.

### 7.1 Account identity

The relational `Account` row contains at minimum:

- immutable `AccountID`;
- temporary unique `LegacyUsername` used by the Stage 1.7 authentication bridge;
- `HomeWorldID`;
- `CreatedAtUtc`;
- `Revision`.

Stage 1.7 does **not** define final login credentials, identity-provider integration or account session tokens. Those belong to Stage 1.8.

### 7.2 Character identity

A `Character` row contains at minimum:

- immutable `CharacterID`;
- owning `AccountID`;
- `FirstName` / `LastName` display values;
- normalized first/last values used by the unique constraint;
- race/class/faction definition IDs;
- level and XP;
- active specialisation where currently supported;
- title/identity state currently supported;
- appearance JSONB when appearance data exists;
- `CreatedAtUtc`;
- `DeletedAtUtc` nullable;
- `Revision`.

A soft-deleted character does not consume one of the 12 active character slots. Restoration requires an available active slot. The name remains reserved until permanent deletion after the 30-day recovery window.

---

## 8. Character-name normalization

The Stage 1.7 database constraint uses application-computed normalized fields rather than relying on database collation for game rules.

For each First/Last name:

1. leading/trailing whitespace is invalid rather than meaningful;
2. validate length as **2–12 Unicode text elements**;
3. permit Unicode letters and combining marks needed by those letters;
4. permit only the approved punctuation forms apostrophe (`'` / `’`) and hyphen (`-`);
5. reject digits, control characters, whitespace inside a field, leading/trailing punctuation and repeated punctuation;
6. normalize with Unicode **NFKC / FormKC**;
7. apply invariant uppercase for the stored comparison value;
8. preserve the original valid display capitalization separately.

The unique database constraint is on:

```text
NormalizedFirstName + NormalizedLastName
```

A name collision fails character creation/rename atomically.

---

## 9. Relational schema scope

Stage 1.7 creates schema only for durable state that is currently implemented or explicitly required for the persistence foundation.

### 9.1 Initial tables/entities

The initial relational model contains equivalents of:

- `Worlds`;
- `Accounts`;
- `Characters`;
- `CharacterWorldStates`;
- `CharacterResources`;
- `CharacterCooldowns`;
- `CharacterPersistentAuras`;
- `CharacterAbilities`;
- `CharacterTalents`;
- `CharacterWeaponSkills`;
- `CharacterReputations`;
- `CharacterQuests`;
- `ItemInstances`;
- `CharacterItemLocations`;
- `PersistenceOperationReceipts`;
- `LegacyImportRecords`.

`CharacterItemLocations` is one authoritative item-location relation rather than allowing the same item to exist independently in inventory and equipment tables. It contains the owning `CharacterID`, location kind and slot/index data. The same `ItemInstanceID` cannot occupy two locations simultaneously.

### 9.2 Deliberately deferred schema

Do not create empty/speculative tables merely because future PDDs mention them. Stage 1.7 does not create final schemas for systems that do not yet have an implemented durable domain, including as applicable:

- professions;
- mail;
- market/auction/escrow;
- guild banks;
- final action-bar persistence;
- dungeon/raid lockouts beyond any state already required by current gameplay;
- future account-wide collections/Legacy details not yet implemented.

Those systems add explicit migrations when their approved implementation requires them.

### 9.3 JSONB

JSONB is allowed for naturally variable, non-ownership payloads such as appearance option data or narrowly scoped extensible state.

Core IDs, names, ownership, item location, progression balances and concurrency fields remain relational columns/relations.

---

## 10. Character persistence lease

Stage 1.7 introduces a **character persistence lease** so a stale World Server cannot continue writing a character after ownership has moved.

This is not yet the final Stage 1.8 account gameplay-session system.

A lease is bound to:

- `CharacterPersistenceLeaseID`;
- `AccountID`;
- `CharacterID`;
- `WorldID`;
- `WorldServerRunID`;
- the current registered World Server;
- the character revision returned at acquisition.

Rules:

1. one `CharacterID` has at most one active persistence lease;
2. only the current registered run for the lease's `WorldID` may mutate through it;
3. every character mutation/checkpoint carries the lease ID and current expected revision;
4. stale lease/run/revision requests fail explicitly;
5. releasing/invalidating the lease prevents further writes from that old owner;
6. future Stage 1.8 gameplay sessions bind/extend this mechanism at account level rather than creating a competing ownership system.

Future cross-world transfer may allow direct World A → World B transfer snapshots for efficiency, but Stage 1.7 does not implement that optimisation. The safe baseline is commit to durable state, change ownership through the Master, then load on the destination.

---

## 11. Persistence operation classes

Stage 1.7 deliberately avoids a generic `SaveCharacter(CharacterData)` production API.

### 11.1 Load operations

Load operations return the durable state required to reconstruct the character runtime after the Master has accepted the lease/ownership request.

The World Server converts persistence DTOs into its existing runtime objects and authored definition references.

Missing critical definitions fail world entry rather than silently substituting content.

### 11.2 Checkpoints

Checkpoints cover lower-risk frequently changing state such as:

- world/map/location/facing;
- current Health;
- current resources;
- other approved high-frequency persistent fields.

The World Server keeps the live copy. A checkpoint records the latest recoverable durable copy.

Checkpoint writes are partial/field-scoped and must not overwrite unrelated transactional state with stale snapshots.

### 11.3 Transactional commands

Ownership/progression changes use explicit commands, including as they become wired during Stage 1.7:

- character creation;
- character soft delete/restore;
- item grant/remove/move/equip/unequip;
- inventory/equipment mutations;
- learned ability/rank changes;
- talent changes where a persistence mutation is required;
- weapon-skill/progression updates where persistence is committed immediately;
- significant quest/reputation progression changes where the existing runtime owns them.

Each retryable mutation carries an immutable `OperationID`.

The Master/database stores an operation receipt so the same `OperationID` cannot apply a successful transaction twice. If the first response was lost, retry returns the previously committed result rather than performing the mutation again.

---

## 12. Concurrency and revisions

Mutable account/character aggregates use an application-visible signed 64-bit `Revision` starting at 1.

For character writes:

1. load/lease acquisition returns the current revision;
2. the World Server submits the expected revision;
3. successful mutation increments the character revision;
4. stale revision fails with an explicit concurrency result;
5. the World Server serializes persistent mutations for one `CharacterID` so it does not race its own requests;
6. PostgreSQL constraints and transactions remain the final ownership/integrity guard.

Do not expose PostgreSQL `xmin` as the public concurrency contract.

---

## 13. World Server persistence coordinator

The Unity server gains one server-owned persistence coordination service. Gameplay/ECS code does not call HTTP, EF Core or PostgreSQL directly.

Conceptually:

```text
Gameplay/service mutation
        ↓
WorldPersistenceCoordinator
        ↓
MasterPersistenceClient
        ↓
Master Server HTTP API
```

Responsibilities:

- hold the current durable `WorldID`/`WorldServerRunID` context;
- acquire/release character persistence leases;
- load character state;
- serialize mutations per CharacterID;
- coalesce low-risk checkpoints so one character does not accumulate many stale position/resource saves;
- send retry-safe transactional commands with `OperationID`;
- report failure explicitly to the requesting gameplay/service layer;
- never block the authoritative simulation tick waiting for network/database I/O.

### 13.1 Bounded work

Checkpoint work is bounded naturally to at most one dirty/pending checkpoint per online CharacterID.

Transactional requests use a bounded server-wide queue with initial configuration:

```text
MaxPendingPersistenceTransactions = 4096
```

This is an engineering safety bound, not a performance target.

If that queue is full, a new high-value mutation is rejected **before** it is committed to authoritative gameplay state. The system does not drop an already-applied ownership transaction to make room.

### 13.2 Retry policy

For retry-safe requests:

- immediate first attempt;
- retry after 500 ms;
- retry after 1 s;
- retry after 2 s;
- then fail the current operation visibly.

Checkpoint failure leaves the character dirty; a later checkpoint cycle may retry the newest state. Multiple stale checkpoints are not accumulated.

Transactional retry is safe because the same `OperationID` is reused and the Master deduplicates it.

Loads/lease acquisition use the same bounded transient retry policy; failure means character entry does not proceed.

---

## 14. Failure policy

### 14.1 Master/database unavailable during login or world entry

Fail closed. Do not spawn a character from guessed/default data.

### 14.2 Master/database unavailable during normal gameplay

The already-running character may continue ordinary simulation temporarily.

Low-risk checkpoint state remains dirty for later retry.

Operations that would create or move durable value are unavailable until their transaction can be accepted. Examples include item ownership changes and other high-value persistence mutations.

### 14.3 Ambiguous transactional response

Retry using the same `OperationID`. Never generate a new operation ID merely because the response was lost.

### 14.4 Stale World Server

If `WorldServerRunID`, lease or expected revision is stale, the Master rejects the write. The old server must not overwrite newer durable character state.

### 14.5 Missing durable definitions

Critical missing class/race/identity definitions fail safe. Peripheral obsolete data may only be removed/changed by an explicit migration.

---

## 15. Checkpoint policy

Stage 1.7 uses a default periodic checkpoint interval of:

```text
30 seconds
```

This is a crash-recovery policy, not a benchmark target.

Additionally checkpoint appropriate live state on:

- clean logout;
- safe disconnect despawn;
- world/server transfer preparation;
- relevant instance transition;
- level-up;
- death/resurrection;
- major progression boundaries;
- clean World Server shutdown.

High-value ownership/progression transactions are committed at the operation boundary and do not wait for the 30-second checkpoint.

---

## 16. Persistent location

`CharacterWorldState` stores:

- `CharacterID`;
- `WorldID`;
- canonical `MapID`;
- nullable `InstanceID` (`null` means the public map instance; runtime conversion uses `Guid.Empty` where required by existing spatial APIs);
- double-precision `PositionX`, `PositionY`, `PositionZ`;
- floating-point normalized facing quaternion `FacingX/Y/Z/W`;
- optional authored `SafeReturnLocationID`;
- `SavedAtUtc`.

On load, private-instance association is used only if the current World Server's instance owner confirms that instance is still valid. Otherwise use the PDD recovery order:

1. valid reconnectable instance;
2. saved persistent-world/map position;
3. authored safe checkpoint/return location;
4. regional/settlement fallback;
5. character starting location.

No arbitrary origin fallback is permitted.

---

## 17. Health, resources, cooldowns and persistent auras

### 17.1 Health

Persist exact current Health. Logout/reconnect/restart does not refill it.

On load, clamp only against the newly reconstructed effective maximum when required by legitimate changed persistent inputs.

### 17.2 Resources

Each persistent resource row stores at minimum:

- resource definition ID;
- exact current value;
- last authoritative update UTC.

Resource definitions gain an explicit offline behaviour. Initial supported policies are:

- `Unchanged` — default if no rule has been deliberately authored;
- `Regenerate` — apply the resource's approved normal regeneration over elapsed offline time;
- `Decay` — apply its approved normal decay;
- `Reset` — only for a resource whose normal gameplay rule would reset independently of logout.

No resource receives an offline benefit merely because the player logged out.

### 17.3 Cooldowns

Only cooldowns that must survive actor lifetime are persisted.

Persist:

- ability/definition ID;
- charge state where applicable;
- absolute UTC recharge/expiry timestamp(s).

Offline real time therefore consumes those persisted cooldowns. Runtime load converts the remaining real duration into the server's current simulation-tick deadline representation.

### 17.4 Auras

Aura persistence is explicit authored policy, not inferred from duration.

Initial policies:

- `RuntimeOnly`;
- `PersistAcrossActorLifetime`.

Persistent aura rows store only state required to reproduce the effect, such as definition ID, source identity where meaningful, stacks, absolute UTC expiry and tick timing where required.

World Server load reconstructs a valid runtime aura; it does not restore obsolete/invalid definitions silently.

---

## 18. Temporary Stage 1.7 authentication bridge

Final player authentication and one-account/one-gameplay-session authority remain Stage 1.8.

Stage 1.7 therefore keeps the existing Unity credential check as a temporary compatibility path, but removes its authority over durable character state.

The transition is:

1. current `PlayerAuthService` validates the current username/password through a temporary local credential store;
2. Stage 1.7 separates that credential-only role from `PlayerDatabase`: the compatibility store contains only username/password hash/salt and no character roster/state;
3. after successful local authentication, the World Server resolves that username to the Master-owned `AccountID`;
4. if this is a newly created temporary-auth account, the Master creates the Account row idempotently with `HomeWorldID` equal to the current registered WorldID;
5. the Master supplies the durable roster/character state;
6. `player_data.json` no longer owns or receives roster/character saves after cut-over;
7. the local credential store is explicitly temporary and is removed/replaced by Stage 1.8 authentication.

Password hashes/salts are not returned by the general persistence API and are not copied into character persistence DTOs.

This bridge must not create a second durable AccountID authority. Username is only the temporary lookup key used to reach the Master-owned account row.

---

## 19. Migration from `player_data.json`

Stage 1.7 includes an explicit one-time importer. It is not an implicit startup migration.

### 19.1 Import inputs

The importer receives:

- an explicit source `player_data.json` path;
- an explicit target durable `WorldID` used as imported accounts' `HomeWorldID`;
- an optional explicit legacy-name mapping file for records whose one-field name cannot be safely converted to First/Last name.

### 19.2 Account identity

Legacy accounts do not already have `AccountID`.

For first import, generate a nonempty `Guid` and record the source identity in `LegacyImportRecords`. Re-importing the same source account returns/reuses the already imported AccountID rather than creating another account.

### 19.3 Character identity

If a legacy character GUID parses as a nonempty unique `Guid`, retain it as `CharacterID`.

If it is invalid, empty or collides with another imported/existing CharacterID, the record is rejected unless an explicit migration mapping resolves it. Do not silently replace it with a new identity.

### 19.4 Legacy names

If the existing single `Name` can be unambiguously parsed into exactly two valid Stage 1.7 name fields, import it.

Otherwise an explicit mapping must provide FirstName and LastName. Do not invent a surname automatically.

### 19.5 Idempotency

Compute a SHA-256 fingerprint for the source file and record import metadata. Import is transactional **per account**.

A repeated import of the same source/account cannot duplicate:

- account identity;
- character identity;
- item instances;
- inventory/equipment ownership;
- progression rows.

Invalid/unmigratable records are reported explicitly and do not cause silent partial mutation of that account.

### 19.6 Imported state

Import currently meaningful state including:

- account username mapping;
- characters;
- level/XP/title/race/class/faction;
- known abilities;
- inventory/equipment and valid item IDs;
- weapon skills;
- talents;
- reputations;
- quests;
- currently saved resources.

Data not present in the legacy format receives only approved creation/default semantics; it is not guessed from unrelated runtime state.

---

## 20. Character creation/deletion/restoration

Character creation becomes a Master persistence transaction after the World Server validates authored race/class/appearance choices.

The transaction atomically enforces:

- owning AccountID exists;
- fewer than 12 active characters;
- valid nonempty CharacterID generated server-side;
- normalized full-name uniqueness;
- initial identity/progression state;
- starting inventory/equipment when provided by the approved character-creation flow;
- starting location from HomeWorldID + race starting location;
- starting reputation/other one-time state exactly once.

The World Server must stop appending directly to an in-memory `PlayerAccountData.Characters` list as durable authority.

Soft deletion:

- sets `DeletedAtUtc`;
- hides the character from normal active roster;
- retains CharacterID/data/name reservation;
- immediately frees one active roster slot;
- permits restoration for 30 days only when an active slot is available.

Permanent cleanup after the recovery period is an explicit persistence operation, not an incidental read side effect.

---

## 21. Item persistence and atomic ownership

The durable item model uses:

- immutable `ItemInstanceID`;
- stable authored `ItemDefinitionID`;
- mutable instance state such as stack, durability and modifications;
- exactly one durable owner/location relation where applicable.

A transaction that moves/equips/unequips/transfers an item must update all affected durable ownership/location rows atomically.

The World Server updates its live representation only after the durable transaction succeeds for operations where failure could duplicate/destroy value.

Stage 1.7 does not implement future mail/market/trade gameplay merely to exercise these transactions.

---

## 22. Ordered implementation sequence

### Step 1 — Complete Unity Master registration and durable WorldID integration

Repositories: both.

- implement the Unity registration/heartbeat/unregister client against the existing Master F1.3a contract;
- make `ServerWorldManager` consume the returned durable `WorldID`;
- remove the Stage 1.2 launch-generated GUID at that single boundary;
- fail startup before actor admission if registration fails.

### Step 2 — Add PostgreSQL/EF Core persistence foundation

Repository: `ninthage-master`.

- add approved packages to `NinthAge.Persistence`;
- define `NinthAgeDbContext` and configuration loading;
- add explicit migration tooling/workflow;
- add startup schema-compatibility/readiness state without automatic production migration.

### Step 3 — Persist durable world records

Repository: `ninthage-master`.

- add relational `World` entity/migration;
- compose `WorldRegistry` from the relational durable directory when DB persistence is enabled;
- keep runtime lease state in memory;
- retain development JSON only as explicit database-free local mode.

### Step 4 — Add account/character core schema

Repository: `ninthage-master`.

- Accounts, Characters, naming constraints, HomeWorldID, soft deletion and revision fields;
- exact name normalization service;
- roster/read models.

### Step 5 — Add remaining Stage 1.7 durable state schema

Repository: `ninthage-master`.

- world state;
- resources;
- cooldowns;
- persistent auras;
- abilities/talents/weapon skills/reputation/quests;
- item instances/location;
- operation receipts;
- import ledger.

Do not add deferred future-domain tables.

### Step 6 — Add authenticated persistence service contracts

Repositories: both.

- define canonical `/world/v1/*` HTTP/JSON DTOs in `NinthAge.Contracts`;
- add the minimal mirrored Unity transport DTOs with explicit JSON names rather than a cross-repository binary/package dependency;
- implement Master World Server credential authentication using the configured per-world 256-bit bearer secret;
- use `NINTHAGE_MASTER_URL` and `NINTHAGE_WORLD_SERVER_SECRET` on the Unity World Server;
- validate authentication before entity existence/state checks;
- use deterministic result codes rather than transport exceptions as gameplay semantics.

### Step 7 — Add Master persistence application services

Repository: `ninthage-master`.

- account/character queries;
- persistence lease ownership;
- optimistic revision checks;
- checkpoint updates;
- idempotent transactional commands and operation receipts;
- explicit failure settlement.

Endpoints remain thin; business/transaction rules live in services/persistence layer.

### Step 8 — Add Unity World persistence coordinator/client

Repository: `ninthage`.

- one World Server persistence owner;
- async HTTP client;
- per-character ordering;
- checkpoint coalescing;
- bounded transaction queue/retry;
- no database/API calls from ECS hot loops or ordinary gameplay components.

### Step 9 — Cut character roster/load/creation over to Master persistence

Repositories: both.

- split the current local authentication compatibility path away from `PlayerDatabase` so it stores credentials only;
- resolve durable AccountID from the Master after local credential validation;
- create new temporary-auth accounts idempotently with the current durable WorldID as HomeWorldID;
- load roster/character state from Master;
- persist character creation transactionally;
- stop `PlayerDatabase` from owning durable roster/character state.

### Step 10 — Capture/restore live character state

Repository: `ninthage` plus contracts as needed.

- capture/restore actual authoritative current Health/resources;
- capture location/facing;
- stop spawn-time test resets of inventory/equipment/talents/weapon skills/reputation/quests;
- load persisted values without silently generating replacement progression.

### Step 11 — Persist timed state

Repositories: both.

- resource offline policy data;
- persistent cooldown representation;
- explicit aura persistence policy;
- convert durable UTC expiry state to current runtime tick deadlines on load.

### Step 12 — Wire transactional gameplay mutations

Repositories: both.

- item ownership/location changes and other current high-value mutable domains route through explicit Master transactions;
- live state commits only after transaction acceptance where rollback would otherwise duplicate/destroy value;
- no speculative future gameplay systems.

### Step 13 — Implement legacy JSON importer and cut-over

Repositories: primarily `ninthage-master`; read legacy DTO definitions from `ninthage`.

- explicit importer/manifest;
- stable identity preservation;
- name mapping;
- target HomeWorldID;
- per-account transactions;
- idempotent import ledger;
- detailed failure report.

After successful owner-approved migration/cut-over, `player_data.json` is no longer production character/roster authority.

### Step 14 — Stage close-out

Repositories: both documentation sets.

Confirm through narrow functional smoke validation that:

- Master and World Server establish durable WorldID;
- database schema is current;
- one account resolves to stable AccountID;
- roster loads from PostgreSQL;
- character creation persists atomically;
- entering the world reconstructs persisted state;
- ordinary play remains World Server-local;
- checkpoint updates durable location/Health/resources;
- a high-value supported mutation is committed through the Master transaction path;
- clean logout/shutdown checkpoints current state;
- restart/re-entry restores the committed state;
- legacy JSON is no longer the production character authority.

Record deferred Stage 1.10 regression candidates; do not implement them here.

---

## 23. Manual/static validation only

Stage 1.7 issues may require:

- `dotnet restore` / `dotnet build` for `ninthage-master`;
- Unity compilation/static checks for `ninthage`;
- EF migration generation review;
- migration script generation;
- applying migrations to a disposable local PostgreSQL 18 database;
- manual empty-database bootstrap;
- manual legacy-import rehearsal against disposable data;
- owner-operated login/create/enter/save/restart/re-enter smoke checks.

Do **not** create a new automated regression suite or performance benchmark campaign during Stage 1.7.

---

## 24. Explicit non-goals

Stage 1.7 does not implement:

- final player authentication/identity provider;
- final one-account-one-gameplay-session admission logic (Stage 1.8);
- reconnect grace/session replacement policy (Stage 1.8);
- multiple live World Server machines/world hopping in production (Phase 3);
- direct World Server-to-World Server transfer optimisation;
- cross-world chat/social routing;
- PostgreSQL replication/HA/disaster-recovery topology;
- final backup operations/dashboard controls;
- mail, market, guild bank or other unimplemented durable gameplay systems;
- automated regression-test expansion;
- performance benchmarking, capacity testing or soak testing.

---

## 25. Stage 1.7 completion gate

Stage 1.7 is complete when:

1. the Unity World Server uses Master-issued durable `WorldID`;
2. PostgreSQL is the durable account/character/world state store when persistence is enabled;
3. `NinthAge.Persistence` owns the EF Core/Npgsql schema and explicit migrations;
4. AccountID/CharacterID/world/name/roster constraints are enforced centrally;
5. one active World Server persistence lease is the only accepted writer for a character;
6. ordinary gameplay remains local to the World Server rather than using the Master/database as a hot path;
7. 30-second + boundary checkpoints preserve low-risk live state;
8. high-value current-domain ownership changes use idempotent atomic transactions;
9. Health/resources/location and required persistent timed state restore without logout reset exploits;
10. legacy JSON can be imported explicitly/idempotently and is no longer production character authority after cut-over;
11. current authentication is clearly marked as the temporary Stage 1.7 bridge pending Stage 1.8;
12. no Stage 1.7 implementation requires regression benchmarking/testing work to proceed to Stage 1.8.
