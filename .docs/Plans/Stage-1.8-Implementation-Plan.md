# Ninth Age — Stage 1.8 Implementation Plan

**Status:** Ready for implementation planning  
**Project:** Ninth Age  
**Stage:** 1.8 — Sessions, authentication and reconnect authority  
**Repositories:** `FrontierDev/ninthage` (`dev`) and `FrontierDev/ninthage-master` (`main`)  
**Implementation baseline:** Stage 1.7 is complete except for its separately deferred validation work  
**Parent plan:** [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md)  
**Product authority:** [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md)

---

## 1. Objective

Complete the player login/session lifecycle by making the Master Server the **sole global gameplay-session and admission authority**, while the World Server remains the authority for the admitted character's live actor and gameplay state.

Stage 1.8 must guarantee that:

- one AccountID can have at most one active gameplay session globally;
- authentication produces or reclaims a Master-owned gameplay session, not merely a locally validated username;
- entering the world is a Master-authorised admission operation binding AccountID, GameplaySessionID, CharacterID, WorldID and WorldServerRunID;
- a network disconnect does not immediately destroy the gameplay session or actor;
- the disconnected actor remains authoritative for at least 30 seconds and longer while authoritative combat/damage restrictions remain unresolved;
- reconnect during that grace period reattaches the new network connection to the **existing runtime actor** rather than spawning another one;
- normal logout obeys authored safe-place, countdown, combat and hostile-damage rules;
- planned World Server shutdown/restart and unexpected connection loss never create simultaneous actors or a state-reset exploit;
- session/admission/reconnect metrics exist for Stage 1.10–1.11 verification.

This plan contains only work that is still required after Stage 1.7. It does not re-plan persistence, world registration, heartbeat, character checkpointing, state restoration or safe-location recovery that already exist.

---

## 2. Existing foundations that are not Stage 1.8 work

Do **not** recreate or replace these completed foundations unless a Stage 1.8 change explicitly needs to consume them:

- Master-owned durable AccountID, CharacterID, HomeWorldID and WorldID;
- PostgreSQL/EF Core account/character/item persistence;
- Master World Server registration, heartbeat, lease expiry and authenticated `/world/v1/*` transport;
- existing username/password credential verification and account enumeration protection;
- account identity/roster lookup and Master-owned character creation/deletion/restoration;
- character persistence lease acquisition/release and revision ownership;
- 30-second and boundary checkpoints, including inventory/equipment;
- Health/resource/offline-state restoration, cooldown restoration and persistent-aura restoration;
- durable world/map/position/facing state;
- authored safe-return and invalid-saved-location recovery;
- final checkpoint settlement before character persistence-lease release;
- one-world/one-process topology.

The existing Stage 1.7 `AccountAuthenticate` call is a **temporary authentication bridge**. Stage 1.8 replaces that bridge with gameplay-session acquisition, while reusing the existing credential verifier as the login factor. This plan does not invent player self-registration, OAuth, email identity, MFA or another external identity provider because no approved product contract currently defines one.

---

## 3. Stage 1.8 implementation decisions

The following decisions close implementation gaps that the PDD intentionally leaves open.

### 3.1 Duplicate-login policy

Use **reject-active / reclaim-disconnected** semantics:

- if an account already has a connected authoritative gameplay session, another fresh successful credential attempt is rejected with `SessionAlreadyActive`;
- if the existing session has been marked disconnected by its owning live World Server, successful authentication from that same current `WorldID + WorldServerRunID` reclaims the **same GameplaySessionID**;
- a reconnect never creates a second gameplay session or second character lease;
- a session belonging to a stale/offline/retired World Server run cannot be reclaimed. It is invalidated through the World runtime lifecycle and a subsequent login may create a new session from durable state.

Do not implement "kick the old login and replace it" in Stage 1.8.

### 3.2 Gameplay sessions are runtime control-plane state

Gameplay-session state is Master-owned **runtime state**, not a new durable PostgreSQL aggregate.

A Master restart invalidates gameplay sessions just as it invalidates runtime character persistence leases. Durable character state remains in PostgreSQL and is reacquired normally after the World Server re-establishes its runtime authority.

Do not add a GameplaySessions table or migration in this stage.

### 3.3 Session identity never comes from the client

The Master generates a non-empty `Guid GameplaySessionID`.

The ordinary game client never supplies, stores or signs this ID as an authority token. It is carried only between the authenticated World Server and Master and in World Server runtime state associated with the exact authenticated network connection.

### 3.4 Logout timings remain centrally authored tuning

The PDD fixes a **minimum 30-second disconnect grace**, but does not fix the ordinary logout countdown or hostile-damage/combat lock durations.

Add these durations to the existing central game configuration and validate them there. The implementation plan does not prescribe balance values except that disconnect grace must never be below 30 seconds.

No scattered literals are permitted.

---

## 4. Implementation sequence

Stage 1.8 is implemented as **four coherent blocks**, not one GitHub issue per numbered requirement.

The detailed requirements below are acceptance criteria inside each block. They should be split into separate issues only when one of these conditions applies:

- a cross-repository contract must land before its consumer;
- a real authority/dependency boundary requires independent review;
- one changeset would otherwise become too broad to review safely;
- implementation uncovers a genuinely separate bug or design decision.

Do **not** create a new issue merely because a requirement has its own bullet or numbered operation. The implementation model should minimise repeated repository/context reconstruction while preserving focused diffs and explicit validation.

### Current progress mapping

At the time this plan was compacted:

- Block 1 is functionally implemented: Master gameplay-session authority, authentication, throttling, replay-safe settlement and exact World connection handoff are present.
- Block 2 character admission is implemented; session-bound roster/create/delete/restore and the remaining leave/close/World-lifecycle work are the next Master tasks.
- Blocks 3 and 4 remain the main World/runtime work.

### Step 1 — Establish gameplay-session authentication authority end-to-end

This block owns the complete login/session identity boundary across Master and World. It combines the former Master contract/registry/authentication/rate-limit work with the World connection-exact authentication handoff.

#### Master contracts and registry

Under `src/NinthAge.Contracts/Sessions/`, define the gameplay-session contract family, including:

- `GameplaySessionResult`;
- `GameplaySessionDisposition` with at least `Created` and `Reclaimed`;
- authentication/disconnect/session-settlement contracts;
- the later character-admission/leave/close contracts as required by subsequent blocks;
- `GameplaySessionRoutes`.

The Master generates a non-empty `GameplaySessionID`; the ordinary game client never supplies or stores it as an authority token.

`GameplaySessionRegistry` is the **single global owner** of active gameplay-session runtime state. Store one session object per AccountID and index the same object by GameplaySessionID. Session runtime state contains only:

- GameplaySessionID;
- AccountID;
- WorldID;
- WorldServerRunID;
- nullable selected CharacterID;
- connected/disconnected state;
- Master-owned created/last-state-change UTC timestamps;
- generation/version state needed to guard asynchronous mutation;
- authentication-attempt replay metadata needed to settle uncertain transport outcomes.

Do not store passwords, credential hashes, Unity PlayerID/Actor references, character gameplay state, inventory or another persistence cache.

Registry mutation is lock-protected; no registry lock spans database/network awaits. Return detached immutable snapshots rather than mutable entries.

Required registry capabilities include:

1. create one session when the account has none;
2. inspect by AccountID or exact GameplaySessionID + World/run;
3. reclaim the same session only when disconnected and still owned by the exact current World runtime;
4. mark the exact session disconnected idempotently;
5. set/clear selected CharacterID using the observed generation;
6. close an exact session only when no selected character remains;
7. invalidate every session owned by an expired/unregistered/retired WorldServerRunID.

#### Authentication and duplicate-login policy

Reuse the existing credential verifier and account projection. Do not create another password store.

Authentication executes in this order:

1. validate request shape;
2. authenticate the World bearer and current WorldID/WorldServerRunID through the existing authorizer;
3. apply authentication throttling before repeated expensive password verification;
4. verify credentials with the existing constant-work/dummy-hash behavior for unknown/deleted/incorrect accounts;
5. require `HomeWorldID == request.WorldID` for the current one-world baseline;
6. revalidate World runtime after the async credential/database boundary;
7. atomically create or reclaim the allowed gameplay session;
8. return the deterministic result.

Duplicate policy remains **reject-active / reclaim-disconnected**:

- connected current session + fresh valid login -> `SessionAlreadyActive`;
- disconnected session from the same current WorldID + WorldServerRunID -> reclaim the same GameplaySessionID;
- stale/offline/retired run -> cannot reclaim;
- do not implement kick-and-replace.

Authentication attempts must be replay-safe across uncertain World→Master transport outcomes:

- one logical World authentication attempt gets one nonempty World-generated attempt ID;
- retries of that same logical attempt reuse it and return the same accepted gameplay session without a second mutation;
- a distinct attempt against a connected session still gets `SessionAlreadyActive`;
- an old attempt cannot revive a session after a later state transition;
- disconnect settlement is idempotently replayable across transient transport loss.

#### Authentication-rate safeguards

One Master-owned limiter under the Sessions/Auth boundary must:

- key failed work by at least WorldServerRunID + submitted login identity;
- read limits/windows from central validated configuration;
- check before PBKDF2/database credential work;
- treat unknown and real usernames with wrong passwords identically;
- return a deterministic throttled result;
- expire entries so the dictionary remains bounded;
- never log passwords or bearer secrets.

The limiter is process-local runtime state, not repository/EF state.

#### World exact-connection handoff

The World authentication path must bind an accepted Master authentication result to the exact PurrNet `Connection`.

Replace any global validated-username queue with an immutable pending authenticated context keyed by `Connection`. The context contains the accepted Master identity:

- GameplaySessionID;
- AccountID;
- HomeWorldID;
- account Revision;
- Created/Reclaimed disposition;
- nullable SelectedCharacterID.

The flow is:

1. send username/password through the typed Master session client/coordinator;
2. accept PurrNet authentication only when Master accepts;
3. store the accepted context against that exact Connection;
4. consume it once when `OnPlayerJoined(PlayerID)` resolves PlayerID back to that live Connection;
5. compensate Master if an accepted-but-unpublished context is abandoned;
6. never retain the password after the Master response.

`ConnectedAccountContext` carries GameplaySessionID and the accepted account/session identity locally, but remains a connection-scoped binding rather than global authority.

Once cut over, remove the retired temporary `ACCOUNT_AUTHENTICATE` / `ACCOUNT_RESOLVE` production bridge. Keep the underlying account/credential persistence used by the new session service.

---

### Step 2 — Bind all account/character control-plane operations to the gameplay session

This block makes GameplaySessionID, not a caller-supplied AccountID, the authority for player-facing roster, character lifecycle and character admission operations. It also completes Master session lifecycle integration.

#### Character admission

Add one Master `AdmitCharacter` application operation. The request contains only:

- WorldID;
- WorldServerRunID;
- GameplaySessionID;
- CharacterID.

It does **not** contain AccountID.

Admission must:

1. resolve the exact GameplaySessionID;
2. require the session is connected and owned by the requesting current WorldID + WorldServerRunID;
3. require no CharacterID is already selected;
4. capture the session AccountID and generation;
5. call the existing `CharacterLeasePersistenceService.AcquireAsync` using that resolved AccountID;
6. after the database/lease await, revalidate the exact session, generation and current World runtime;
7. only then bind SelectedCharacterID through `GameplaySessionRegistry.SetSelectedCharacter`;
8. if revalidation/binding fails after lease acquisition, release that acquired lease before returning/throwing.

Do not duplicate character ownership/deletion/lease/projection logic in the session registry or admission service.

An accepted admission response reuses the existing acquired character persistence state, lease ID and revision. Do not introduce another character-state DTO.

#### Roster and character create/delete/restore

The World Server must no longer be able to present an arbitrary AccountID as sufficient player authority.

For roster and existing create/delete/restore operations:

- the World-facing request carries GameplaySessionID and World/run identity;
- AccountID is resolved from the current connected Master session;
- session ownership/current World runtime is validated before durable account/character work;
- after any asynchronous durable work where session replacement/disconnect could matter, revalidate the session generation/current World runtime before treating the result as current;
- pass the resolved AccountID into the existing Stage 1.7 roster/create/delete/restore services unchanged;
- preserve existing OperationID/revision/idempotency rules for character mutations;
- do not add GameplaySessionID to durable repository entities or persistence receipts.

The existing Stage 1.7 persistence services remain the owners of:

- account/character existence;
- ownership;
- character limits;
- name uniqueness;
- soft-delete/recovery rules;
- durable transactions and OperationID replay.

Session application code composes authority; it does not copy those rules.

Once all production World callers have cut over, raw World-facing roster/create/delete/restore endpoints that trust caller-supplied AccountID must stop being public player authority. Keep the underlying services/contracts internally where still needed.

#### Disconnect, leave and close

Expose authenticated World-only operations with exact session identity.

**DisconnectSession**
- mark the exact connected session disconnected;
- preserve SelectedCharacterID;
- do not release the character persistence lease;
- do not alter durable character state;
- exact already-disconnected settlement is idempotent.

**LeaveCharacter**
- require exact GameplaySessionID, selected CharacterID and current lease/revision;
- invoke existing lease release only after the World has confirmed its final checkpoint;
- clear SelectedCharacterID only after accepted/stale-safe lease settlement according to the approved leave contract;
- keep the gameplay session connected so the player may return to character selection.

**CloseSession**
- close only a session with no selected character;
- remove AccountID and GameplaySessionID indexes atomically;
- use for a connected client leaving character-selection entirely, or for a disconnected session after its admitted character has been finalized/left;
- never use close as a shortcut around final checkpoint + lease release.

#### World-runtime invalidation

Integrate gameplay-session invalidation with existing WorldRegistry lifecycle ownership.

When an exact WorldServerRunID is retired by:

- heartbeat expiry;
- graceful unregister;
- run replacement/retirement;

invalidate all gameplay sessions owned by that exact run. A stale run cannot reclaim sessions by replaying old IDs.

Do not add cross-world transfer or destination reservation in Stage 1.8.

#### Persistence authority after admission

Do **not** add GameplaySessionID to character checkpoints or ordinary durable writes. Once a character is admitted, the existing `CharacterPersistenceLeaseID + CharacterID + revision + World/run` remains the durable write authority.

---

### Step 3 — Cut the World Server fully over to session-authorised account and character operations

This block removes the remaining World-side dependence on raw AccountID-based player authority and makes local runtime session storage independent of a single network connection.

#### Session-authorised roster and character mutations

World roster/create/delete/restore calls must send the current local GameplaySessionID, never AccountID as the authority credential.

Local code may still carry AccountID as accepted identity/data, but Master resolves and authorizes the operation from GameplaySessionID.

Preserve the existing client-facing character-selection/creation behavior and existing authored-definition validation. Only the authority transport changes.

After successful character creation/delete/restore, roster refresh uses the same session-authorised roster operation.

#### Cut world entry over to Master admission

Modify `EnterWorldService` so ordinary entry calls:

`AdmitCharacterAsync(GameplaySessionID, CharacterID)`

instead of raw `AcquireCharacterAsync(AccountID, CharacterID)`.

Preserve all existing Stage 1.7 post-admission behavior:

- critical definition validation;
- persisted resource validation;
- five-tier location recovery;
- durable recovery checkpoint when relocation is required;
- `CharacterPersistenceProjection`;
- `PlayerSessionData` construction;
- spawn;
- failure cleanup.

Remove redundant static ownership checks already guaranteed by accepted Master admission. Retain dynamic checks that can change across async/coroutine boundaries:

- connection still live where required;
- same local GameplaySessionID;
- same World runtime;
- current admission/lease ownership/generation.

If spawn fails after Master admission, unwind through the session-aware leave/finalization path rather than a raw player-authority acquire/release shortcut.

After the cut-over proves no ordinary player caller remains, stop exposing the raw character-acquire World route. The lease service remains an internal Master dependency.

#### Separate gameplay-session lifetime from PlayerID/connection lifetime

A live admitted character must not be stored solely under PlayerID.

`PlayerSessionData` must include:

- GameplaySessionID;
- current attached PlayerID/connection state;
- disconnect state/deadline;
- existing AccountID;
- CharacterID;
- persistence state;
- entry location;
- live safe-return state;
- PlayerActor.

`ServerConnectionManager` maintains:

- one admitted-session mapping by GameplaySessionID;
- one current PlayerID -> GameplaySessionID attachment mapping.

Do not add a second AccountID uniqueness registry; Master owns global uniqueness.

Existing callers that start with PlayerID resolve through the attachment mapping. Disconnecting the network connection must not by itself destroy the admitted gameplay-session/actor state.

---

### Step 4 — Implement authoritative disconnect, reconnect, logout, finalization and shutdown

This block owns the runtime lifecycle after a character is admitted. It should be implemented as one coherent lifecycle system, not as separate one-off timers/coroutines for each case.

#### Disconnect grace

On unexpected disconnect of an admitted character:

1. remove only the live PlayerID/connection attachment;
2. tell Master to mark GameplaySessionID disconnected;
3. keep PlayerSessionData, PlayerActor, ECS/gameplay registration, spatial context, auras, cooldowns, resources, death state, combat/target state and persistence lease alive;
4. set a server-simulation deadline at least 30 seconds in the future;
5. do **not** final-checkpoint, unregister, retire, despawn or release the character lease at disconnect time.

Do not call `Actor.SetOwner(null)` merely to model connection loss if that path changes player/non-player gameplay semantics.

Use the authoritative simulation clock/deadline owner; no per-player `Update` loop or wall-clock timer.

At/after the grace deadline:
- finalize only if authoritative logout eligibility is satisfied;
- otherwise keep the actor authoritative and reschedule/check through the same central owner.

#### Reconnect and actor reclaim

When Master authentication returns `Reclaimed`:

1. require the returned GameplaySessionID exists in the World disconnected-session map;
2. require AccountID and SelectedCharacterID exactly match;
3. require the same PlayerActor still exists and remains registered for that CharacterID/current context;
4. cancel the disconnect-finalization deadline;
5. transfer PurrNet ownership to the new PlayerID through the project-owned `NetworkIdentity.GiveOwnership` integration and call `Actor.SetOwner(newPlayerID)`;
6. update PlayerID -> GameplaySessionID attachment;
7. let normal ownership callbacks rebuild validation/index state;
8. republish owner-private state required by the reconnecting client from **live runtime state**.

Do not rerun spawn initialization, offline restoration, item loading, aura OnApply effects or cooldown restoration.

If Master says `Reclaimed` but the local disconnected actor/session is gone, fail closed and settle/close the stale Master session. Do not spawn a second actor under that reclaimed session.

#### Authoritative logout/combat eligibility

Client presentation combat state is not authority.

Add one small server-owned eligibility state keyed by ActorID containing only:

- combat-lock deadline;
- hostile-damage-lock deadline.

Update those deadlines from authoritative combat/damage execution boundaries, including at minimum:

- auto attacks;
- direct spell damage;
- periodic aura damage;
- any other existing authoritative hostile-damage path.

Central configuration owns:

- minimum disconnect grace, validated >= 30 seconds;
- ordinary logout countdown;
- combat-lock duration;
- hostile-damage-lock duration.

Use simulation ticks/deadlines and the existing non-shortening conversion contract. No scattered literals.

Expose one authoritative query such as `CanCompleteLogout(Guid actorID)` used by both normal logout and disconnected finalization.

#### Normal logout and safe locations

Add the shared account/session request and server binding for "leave world/logout character".

Normal logout behavior:

- explicit authored safe logout location + no combat/damage lock -> immediate completion;
- otherwise use one configured server-owned countdown;
- combat or hostile damage blocks start/completion and cancels an active countdown;
- completion revalidates session/actor identity and eligibility;
- disconnect during countdown becomes the disconnect-grace path.

Never trust a client-provided safe/out-of-combat flag.

Extend `WorldLocationDefinition` with an explicit `SafeLogout` purpose and dedicated positive safe-logout radius. The server safe-place query uses the accepted ActorSpatialContext and double-precision WorldPosition, matching authored map/public context. Do not infer safety from scene names or reuse an unrelated radius field.

After normal logout, keep the gameplay session connected and return to character selection by completing session-aware `LeaveCharacter`.

#### One finalization path

Route both eligible disconnect-grace expiry and completed normal logout through one finalization owner.

Order:

1. synchronously capture current authoritative Stage 1.7 checkpoint while the actor still exists;
2. queue and flush it;
3. require accepted settlement of the newest/current generation;
4. call Master session-aware `LeaveCharacter` so the persistence lease is released and SelectedCharacterID is cleared;
5. unregister the actor from server gameplay/stat owners;
6. `RetireFromServer`;
7. despawn its NetworkIdentity;
8. clear admitted-session character state;
9. if no network connection exists, call Master `CloseSession`; if the connection is still live after normal logout, retain the account gameplay session and refresh/publish the session-authorised roster.

If required persistence/leave settlement fails, keep the actor/session in an explicit unsettled state. Never report logout as durably complete while final checkpoint/lease transition is unresolved.

Reuse existing Stage 1.7 checkpoint/generation/lease owners. Do not add a session-specific persistence queue.

#### World shutdown/restart

During graceful shutdown:

- stop accepting new authentication/admission first;
- retain the existing final online-character checkpoint sequence;
- settle admitted characters and leases through the same finalization rules;
- unregister the World Server;
- let Master invalidate any remaining runtime gameplay sessions for that WorldServerRunID.

Unexpected World loss is handled by heartbeat expiry invalidating that run's gameplay sessions. A later World run authenticates into new sessions and recovers from durable Stage 1.7 state.

Do not preserve GameplaySessionID across World or Master process restart.

## 5. Telemetry additions

Add Stage 1.8 correctness telemetry without introducing performance thresholds.

### Master

Record counters/gauges for:

- authentication attempts by result;
- authentication throttling;
- active gameplay-session count;
- new session creation;
- duplicate connected-session rejection;
- disconnected-session reclaim;
- session invalidation due to World run expiry/unregister;
- character admission by result;
- character leave/session close by result.

Do not emit username/password/bearer/session-secret material into metrics labels.

### World Server

Extend existing PerformanceTelemetry/NetworkingPerformanceMetrics ownership with:

- authenticated connected gameplay sessions;
- admitted connected characters;
- disconnected-grace actors;
- reconnect attempts/success/failure;
- disconnect finalization after grace;
- admission rejection;
- normal logout started/cancelled/completed.

Do not add Stage 1.8 numerical pass/fail capacity targets.

---

## 6. C# hygiene and architecture requirements

Every Stage 1.8 implementation issue must preserve these rules:

1. **Master is the sole global session owner.** Do not enforce uniqueness through only `ServerConnectionManager._connectedAccounts` or another World-local account dictionary.
2. **GameplaySessionID, CharacterPersistenceLeaseID, CharacterID, AccountID, WorldID, WorldServerRunID, PlayerID and ActorID remain distinct concepts.**
3. No PostgreSQL GameplaySession table in Stage 1.8.
4. Do not add another World registry, character lease manager, HTTP transport, checkpoint queue, actor registry or item/session persistence cache.
5. Reuse `MasterWorldClient` authenticated transport and the existing World→Master coordination boundary for typed session calls; do not call HTTP directly from gameplay RPC handlers.
6. Never hold a lock across an awaited DB/network call. Snapshot/guard mutable session state, await, then revalidate its generation and World runtime.
7. Validate untrusted input at the owning boundary once. Do not duplicate the same DTO validator in contracts, service and repository.
8. Session registry does not perform durable account/character queries itself; application services coordinate it with existing repositories/lease services.
9. Unity main-thread actor/NetworkIdentity mutation remains on the Unity main thread. No `Task.Run`, `.Wait()`, blocking `.Result` or `async void`.
10. Reconnect does not replay spawn-time gameplay effects.
11. No per-player `Update` loop or independent wall-clock timer for grace/logout. Use the authoritative server simulation clock/scheduler.
12. No client-reported combat/safe-place state is authoritative.
13. Public Master contracts/methods receive useful XML documentation; using directives remain alphabetized; existing acronym casing is preserved.
14. Third-party/PurrNet source remains untouched.
15. Keep changesets focused by invariant and review the changed-file list before commit.

---

## 7. Explicit non-goals

Stage 1.8 does **not** implement:

- multiple concurrently selectable World Servers for players;
- cross-world character transfer or handoff;
- World Server-to-World Server RPCs;
- world queues or population balancing;
- cross-world chat/social routing;
- player account registration, email verification, OAuth, MFA or password-recovery product flows not yet specified by PDD;
- durable gameplay-session rows;
- trade/auction/escrow special-case transactions;
- new combat gameplay mechanics beyond the minimum authoritative logout/damage eligibility state;
- a new death/corpse system;
- broad regression suites, mass-login/reconnect load tests or new performance thresholds.

Those remain in their approved later phases/stages.

---

## 8. Validation gate

Stage 1.8 uses compilation/static checks plus narrow owner-operated smoke validation. Stage 1.10 owns systematic race/regression coverage and Stage 1.11 owns representative load/stress execution.

Required narrow validation:

1. Valid credentials create one Master GameplaySessionID and one World connected-account binding.
2. A second simultaneous valid login to the same AccountID is rejected and does not affect the first actor/session.
3. Character entry succeeds only through session-authorised admission; wrong-account/deleted/invalid CharacterID never acquires/spawns.
4. Disconnect while in world leaves the same actor alive and simulated for at least 30 seconds.
5. Reconnect during grace reclaims the same GameplaySessionID and same PlayerActor/CharacterID, with live Health/resources/items/cooldowns/auras preserved.
6. No second actor is spawned during reconnect.
7. Grace expiry while logout-eligible commits the final checkpoint, releases the lease, despawns once and closes the disconnected session.
8. Grace expiry while combat/damage locked does **not** despawn the actor; cleanup occurs only after the authoritative lock resolves.
9. Safe-location normal logout completes immediately when otherwise eligible.
10. Non-safe normal logout uses the configured countdown; hostile combat/damage cancels or blocks it.
11. Normal logout returns a connected gameplay session to character selection without creating a new account session.
12. Master/DB unavailable during session authentication or admission fails closed.
13. Master unavailable during finalization never produces false durable success or duplicate admission.
14. World Server heartbeat expiry/unregister invalidates its Master gameplay sessions.
15. Clean World Server restart reconstructs the character from the latest acknowledged Stage 1.7 state under a new runtime session.
16. Existing Stage 1.7 invalid-location recovery continues to run before fresh actor spawn when required.
17. Authentication throttling activates under a narrow manual burst without exposing account existence.
18. Session/admission/reconnect telemetry increments on the expected paths.

**Stage 1.8 is complete when these lifecycle invariants are implemented and statically accepted.** Any broad multi-client race campaign, mass reconnect burst, soak validation or capacity claim remains deferred to Stages 1.10–1.11.
