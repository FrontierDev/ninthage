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

## 4. Master Server implementation sequence

### Step 1 — Add gameplay-session contracts

Create a dedicated contracts area under:

`src/NinthAge.Contracts/Sessions/`

Add:

- `GameplaySessionResult`;
- `GameplaySessionDisposition` with at least `Created` and `Reclaimed`;
- `GameplaySessionAuthenticateRequest`;
- `GameplaySessionAuthenticateResponse`;
- `GameplaySessionDisconnectRequest`;
- `GameplaySessionCloseRequest`;
- `CharacterAdmissionRequest`;
- `CharacterAdmissionResponse`;
- `CharacterLeaveRequest`;
- `GameplaySessionResponse` for result-only settlements where appropriate;
- `GameplaySessionRoutes`.

The session-authentication request contains:

- `WorldID`;
- `WorldServerRunID`;
- current login username;
- submitted password.

An accepted authentication response contains:

- `GameplaySessionID`;
- `AccountID`;
- `HomeWorldID`;
- account `Revision`;
- `Disposition`;
- nullable `SelectedCharacterID` when reclaiming a disconnected admitted session.

A rejected response contains none of those success fields.

The character-admission request contains:

- `WorldID`;
- `WorldServerRunID`;
- `GameplaySessionID`;
- `CharacterID`.

The accepted admission response carries the existing acquired character persistence state, character persistence lease ID and revision rather than introducing a second character-state DTO.

Character leave identifies the exact gameplay session and the existing character persistence lease/revision needed to release the admitted character safely.

All public contract types and members require useful XML documentation. Use existing ID casing and existing stable result conventions.

### Step 2 — Add the Master gameplay-session registry

Create:

`src/NinthAge.Master/Sessions/GameplaySessionRegistry.cs`

This is the **single global owner** of active gameplay-session runtime state.

Store one session object per AccountID and index the same object by GameplaySessionID. Each session contains only runtime control-plane identity/state:

- GameplaySessionID;
- AccountID;
- WorldID;
- WorldServerRunID;
- nullable selected CharacterID;
- connected/disconnected state;
- Master-owned created/last-state-change UTC timestamps;
- an internal generation/version used to detect replacement or mutation across asynchronous boundaries.

Do not store passwords, credential hashes, character gameplay state, PlayerID, Unity actor references, inventory state or another copy of character persistence state.

Registry operations must be lock-protected, but no lock may be held across database or network awaits.

Required operations:

1. create a session for an account that currently has none;
2. inspect the existing session for an AccountID;
3. reclaim the same session only when it is disconnected and still belongs to the exact current World runtime;
4. mark a connected session disconnected only for its exact World/run/session identity;
5. mark/clear its selected CharacterID around admission/leave;
6. close the exact session;
7. invalidate all sessions owned by an expired/unregistered/replaced WorldServerRunID.

Return detached immutable snapshots/guards rather than mutable registry objects.

### Step 3 — Replace the temporary authentication bridge with session acquisition

Modify the existing Master authentication path rather than adding a second password store.

Relevant existing owners include:

- `AccountAuthenticationService`;
- `AccountCharacterRepository`;
- the existing account authentication projection;
- `TemporaryAccountPasswordHasher` / credential records;
- `WorldPersistenceRequestAuthorizer`.

Refactor the internal credential verification result so a successful verification supplies the existing durable:

- AccountID;
- HomeWorldID;
- account Revision;

without exposing password salt/hash outside the authentication layer.

Then create:

`src/NinthAge.Master/Sessions/GameplaySessionService.cs`

Its authenticate operation must execute in this order:

1. validate request shape;
2. authenticate the World Server bearer and current WorldID/WorldServerRunID through the existing World authorizer;
3. apply authentication-attempt throttling before repeated expensive password verification;
4. verify credentials using the existing constant-work/dummy-hash behaviour for unknown, deleted or incorrect accounts;
5. require `HomeWorldID == request.WorldID` for the current Stage 1.8 one-world admission baseline;
6. revalidate the World runtime after the asynchronous database/credential boundary;
7. atomically create a new gameplay session or reclaim the one disconnected session allowed by §3.1;
8. return the deterministic session result.

A connected existing session returns `SessionAlreadyActive` **after** credentials have been successfully verified. Wrong credentials continue to return the same generic invalid-credentials outcome regardless of whether an account/session exists.

Once the Unity World Server has cut over to this route, remove the player login flow's dependency on the old separate `ACCOUNT_AUTHENTICATE` + `ACCOUNT_RESOLVE` sequence. The underlying account/credential persistence stays; the temporary bridge endpoint may be removed from the World-facing endpoint catalogue when no production caller remains.

### Step 4 — Add authentication-rate safeguards

Create one Master-owned authentication limiter under the Sessions/Auth application boundary; do not place rate state in repositories.

It must:

- bound repeated failed authentication work per World Server run and login identity;
- have centrally configured limits/windows rather than method literals;
- avoid disclosing whether a username exists;
- return a deterministic throttled result;
- expire limiter entries so the dictionary cannot grow without bound;
- never log submitted passwords or bearer credentials.

This is correctness/resource protection only. Stage 1.8 does not establish production capacity thresholds or a load-test campaign.

### Step 5 — Make character admission session-authorised

Add a Master session admission operation that becomes the only player world-entry path.

For `AdmitCharacter`:

1. resolve the exact GameplaySessionID;
2. require it is connected and owned by the requesting `WorldID + WorldServerRunID`;
3. require its AccountID matches the durable account that owns CharacterID;
4. require no CharacterID is already selected on the session;
5. call the existing `CharacterLeasePersistenceService.AcquireAsync` / character persistence lease owner rather than recreating lease logic;
6. after the database/lease await, revalidate the session generation and current World runtime;
7. only then set the session's selected CharacterID and return the acquired lease/state;
8. if session/world revalidation fails after the lease was acquired, release that lease before returning failure.

Do not copy character eligibility or persistence projection logic into the registry.

The existing raw character-acquire persistence endpoint must no longer be used by ordinary player admission. If no non-session caller remains after cut-over, stop exposing it as a public World route and keep the lease service as an internal application dependency.

### Step 6 — Make roster and character mutations session-bound

The World Server must no longer be able to present an arbitrary AccountID as sufficient player authority for account-facing operations.

For player roster and existing create/delete/restore operations:

- add GameplaySessionID to the World-facing request or wrap the existing persistence service behind a session-authorised application operation;
- resolve AccountID from the Master session rather than trusting an AccountID supplied by the World as player identity;
- require the session is connected and belongs to the requesting World runtime;
- pass the resolved AccountID into the existing Stage 1.7 repository/service unchanged.

Do not add session IDs to character checkpoints. Once admitted, the existing character persistence lease remains the write authority for checkpoint persistence.

### Step 7 — Add disconnect, leave and close session operations

Expose authenticated World-only routes for:

**DisconnectSession**
- marks the exact connected gameplay session disconnected;
- preserves selected CharacterID;
- does not release its character persistence lease;
- does not alter durable character state;
- is idempotent for an already-disconnected matching session.

**LeaveCharacter**
- requires the exact session, selected CharacterID and current character persistence lease;
- invokes the existing lease release only after the World has already confirmed its final checkpoint;
- clears SelectedCharacterID only when lease release is accepted;
- leaves the gameplay session connected so the player may return to character selection.

**CloseSession**
- only closes a session with no selected character;
- removes both AccountID and GameplaySessionID indexes atomically;
- is used for a connected client that disconnects before entering the world and after a disconnected admitted character has been safely finalized/left.

A session with a selected character cannot be closed as a shortcut around final checkpoint/lease release.

### Step 8 — Invalidate sessions with World runtime ownership

Integrate `GameplaySessionRegistry` with the existing `WorldRegistry` lifecycle.

When a WorldServerRunID becomes stale because of:

- heartbeat expiry;
- graceful unregister;
- run replacement/retirement;

invalidate every gameplay session owned by that exact run.

Do not let a stale run reclaim sessions by presenting old IDs.

Do not add cross-world transfer or destination reservation here; that remains Phase 3.

---

## 5. Unity World Server implementation sequence

### Step 9 — Make authentication handoff connection-exact

The current `PlayerAuthService` queue of validated usernames is not a sufficient session identity boundary.

Modify:

- `Assets/Runtime/Shared/Networking/IPlayerAuthService.cs`;
- `Assets/Runtime/Shared/Networking/LoginAuthenticator.cs`;
- `Assets/Runtime/Server/Services/PlayerAuthService.cs`;
- `Assets/Runtime/Server/Core/ServerConnectionManager.cs`;
- Master session protocol/client/coordinator files.

Replace:

- `GetNextValidatedUsername()`;
- the global validated-username queue;

with an exact pending authentication result keyed by the PurrNet `Connection`.

The server authentication operation must:

1. send username/password to the new Master session-authentication route;
2. receive GameplaySessionID, AccountID, HomeWorldID, Revision and disposition;
3. store an immutable pending authenticated-connection context against that exact `Connection`;
4. return PurrNet authentication success only for an accepted Master session result;
5. consume that exact context when `OnPlayerJoined(PlayerID)` resolves the PlayerID back to its live connection;
6. remove unconsumed pending context on unauthentication/disconnection.

Do not retain the password after the Master response.

### Step 10 — Bind local account context to GameplaySessionID

Extend `ConnectedAccountContext` with:

- `Guid GameplaySessionID`;
- the session disposition/reconnect metadata needed during initial connection handling.

It remains a **connection-scoped local binding**, not the global session authority.

`InitializeAuthenticatedPlayer` must no longer call `ResolveAccountAsync(username)` merely to rediscover the identity already returned by Master session authentication.

For a newly created gameplay session:
- obtain the roster through the session-authorised Master route;
- publish the local ConnectedAccountContext;
- send the roster.

For a reclaimed disconnected session:
- route to the reconnect flow in Step 13 rather than publishing a second independent account/session object and waiting for a fresh EnterWorld request.

### Step 11 — Cut world entry over to Master session admission

Modify `EnterWorldService` so ordinary entry calls:

`AdmitCharacterAsync(GameplaySessionID, CharacterID)`

instead of raw `AcquireCharacterAsync(AccountID, CharacterID)`.

Preserve the existing Stage 1.7 logic after acceptance:

- critical definition validation;
- persisted resource validation;
- five-tier world-entry location recovery;
- confirmed recovery checkpoint when relocation is required;
- CharacterPersistenceProjection;
- PlayerSessionData construction;
- SpawnPlayerCoroutine;
- failure cleanup.

Remove redundant local character-ownership checks that are now guaranteed by accepted Master admission, but retain dynamic checks that can change during asynchronous work: connection still live, same local GameplaySessionID, same World runtime and same admission/lease generation.

If spawning fails after Master admission, release the character through the new session-aware leave operation, not the raw lease route.

### Step 12 — Separate gameplay session lifetime from network connection lifetime

Refactor local session storage so a live admitted character is not keyed solely by PlayerID.

`PlayerSessionData` gains:

- `GameplaySessionID`;
- current attached PlayerID/connection state;
- disconnect state and disconnect deadline;
- the existing AccountID, CharacterID, persistence state, entry location, live safe-return state and PlayerActor.

Inside `ServerConnectionManager`, maintain:

- one local admitted-session mapping by GameplaySessionID;
- one current PlayerID → GameplaySessionID attachment mapping.

Do not maintain a second AccountID authority map for uniqueness; Master owns global exclusivity.

Existing callers that need session data from PlayerID should resolve through the attachment mapping. Existing character-session data must remain alive after a transport disconnect.

### Step 13 — Implement disconnect grace without despawning the actor

Replace the current `OnPlayerLeft` actor teardown for admitted characters.

On unexpected disconnect:

1. remove only the live PlayerID/connection attachment;
2. tell Master to mark the GameplaySessionID disconnected;
3. keep `PlayerSessionData`, PlayerActor, ECS registration, spatial context, auras, cooldowns, resources, death state, target/combat state and character persistence lease alive;
4. set a server-simulation deadline at least 30 seconds in the future;
5. **do not** call final checkpoint, `UnregisterActor`, `RetireFromServer`, `NetworkIdentity.Despawn` or character lease release at disconnect time.

Do not call `Actor.SetOwner(null)` merely to model a lost connection: the existing owner-change path treats an unowned actor as non-player movement state. The dead network connection itself prevents further client RPC authority. The old PlayerID is only a stale attachment value until a reconnect replaces it.

Use the authoritative simulation clock/deadline system; do not add one coroutine or `Update` loop per disconnected player.

At/after the grace deadline:

- if authoritative logout eligibility says combat/damage state is resolved, run the finalization sequence in Step 16;
- otherwise keep the actor authoritative and check again through the same central deadline/scheduler owner.

### Step 14 — Reclaim the existing actor on reconnect

When Master authentication returns `Disposition = Reclaimed`:

1. require the returned GameplaySessionID exists in the World Server's disconnected-session map;
2. require AccountID and returned SelectedCharacterID exactly match that local session;
3. require the same PlayerActor still exists and remains registered under its CharacterID/current spatial context;
4. cancel the disconnect-finalization deadline;
5. transfer PurrNet ownership to the new PlayerID using the supported project-owned `NetworkIdentity.GiveOwnership` integration and call `Actor.SetOwner(newPlayerID)`;
6. update local PlayerID → session attachment;
7. let existing server ownership callbacks rebuild player validation/ownership indexes rather than directly mutating their internal dictionaries;
8. republish the current authoritative owner-private state required by the reconnected client.

Reconnect state publication must come from **live runtime state**, not the Stage 1.7 acquired snapshot. Do not rerun spawn initialization, offline-resource resolution, item loading, aura OnApply effects or cooldown restoration.

Where existing PurrNet observer bootstrap already republishes a domain, reuse it. Add explicit owner bootstrap only for state not automatically re-sent on ownership change, including as applicable current stats/resources, inventory/equipment, current cooldown UI and other owner-private state.

If Master says `Reclaimed` but the local disconnected actor/session no longer exists, fail closed and settle/close the stale session with Master. Do not spawn a second actor under that reclaimed session. A later fresh login may create a new session and load the last durable state only after the stale session is closed.

### Step 15 — Add authoritative logout/combat eligibility

The current client `ClientCombatManager.inCombat` is presentation state and must not control logout or disconnect cleanup.

Add one small server-owned eligibility state keyed by ActorID that tracks only the state required by the persistence PDD:

- combat-lock deadline;
- hostile-damage-lock deadline.

Update those deadlines from authoritative server combat/damage execution boundaries. At minimum cover:

- auto attacks;
- direct spell damage;
- periodic aura damage;
- other existing authoritative hostile-damage paths.

Do not infer combat from client callbacks.

Add central configuration for:

- minimum disconnect grace (validated >= 30 seconds);
- ordinary logout countdown;
- combat-lock duration;
- hostile-damage-lock duration.

Every hostile action/damage event that should keep the actor in combat extends the relevant deadline using the existing non-shortening simulation-time conversion. Expired deadlines are not retained as separate historical state.

Expose one server query such as:

`CanCompleteLogout(Guid actorID)`

used by both normal logout completion and disconnected-session finalization.

### Step 16 — Add explicit normal logout

Add a shared Session/Account endpoint request and server binding for "leave world/logout character".

Normal logout is World Server-authoritative:

- if the actor is in an explicit safe logout place and no combat/damage lock applies, complete immediately;
- otherwise start one server-owned logout deadline using the centrally configured duration;
- combat or hostile damage blocks starting completion and cancels an active countdown;
- completion revalidates eligibility and session/actor identity before finalizing;
- client disconnect during a logout countdown stops being a normal logout and enters the minimum disconnect-grace path instead.

No client-provided "safe" or "out of combat" flag is trusted.

After successful normal logout, keep the gameplay session connected and return it to the character-selection state by clearing the selected character through Master `LeaveCharacter`.

### Step 17 — Author safe logout locations explicitly

Safe logout must be content-authored, never inferred from scene names.

Extend the existing `WorldLocationDefinition` model with a `SafeLogout` purpose and a dedicated positive safe-logout radius when that purpose is set.

The server safe-place query uses:

- current accepted ActorSpatialContext;
- current accepted double-precision WorldPosition;
- matching authored MapID/public context;
- horizontal distance to eligible SafeLogout location anchors.

Do not reuse `RegionalFallbackRadius` for a different meaning and do not store safe-logout state on the character.

This is a minimal extension of the existing authored-location system; it does not add inns/housing gameplay.

### Step 18 — Finalize a disconnected or logged-out admitted character through existing persistence

Create one finalization path in `ServerConnectionManager`/session lifecycle ownership and route both:

- eligible disconnect-grace expiry;
- completed normal logout;

through it.

The order is:

1. synchronously capture the current authoritative Stage 1.7 checkpoint while the actor still exists;
2. queue and flush it;
3. require accepted settlement of the current/newest generation;
4. call Master session-aware `LeaveCharacter` so the character persistence lease is released and SelectedCharacterID is cleared;
5. unregister the actor from server stat/gameplay owners;
6. `RetireFromServer`;
7. despawn its NetworkIdentity;
8. clear the World Server's admitted-session character state;
9. if the network connection no longer exists, call Master `CloseSession`; if the connection is still live after normal logout, retain the account gameplay session and refresh/publish the roster.

If any persistence/leave settlement required before actor destruction is unavailable or rejected, keep the actor/session in the unsettled server state and report the failure. Never report logout as durably complete while the final checkpoint/lease transition is unresolved.

Reuse the Stage 1.7 checkpoint queue, generation and lease-revision owners. Do not add a session-specific persistence queue.

### Step 19 — Integrate gameplay sessions with World Server shutdown/restart

During graceful World Server shutdown:

- stop accepting new player session authentication/admission before final capture;
- retain the existing final online-character checkpoint sequence;
- settle admitted characters and leases;
- unregister the World Server;
- let Master invalidate any remaining runtime gameplay sessions bound to that WorldServerRunID.

Unexpected World Server loss is handled by Master heartbeat expiry invalidating that run's gameplay sessions. On a later World Server run, clients authenticate into new sessions and recover from the latest durable Stage 1.7 state.

Do not try to preserve GameplaySessionID across a World Server or Master process restart.

---

## 6. Telemetry additions

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

## 7. C# hygiene and architecture requirements

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

## 8. Explicit non-goals

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

## 9. Validation gate

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
