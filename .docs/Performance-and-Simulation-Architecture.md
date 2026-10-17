# Ninth Age — Performance and Simulation Architecture

**Status:** Proposed architecture contract — requires manual validation of [Issue #41](https://github.com/FrontierDev/ninthage/issues/41)
**Scope:** Phase 1.0 runtime ownership and integration boundaries; **documentation only**
**Code baseline reviewed:** `dev` at `f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f`
**Review date:** 2026-10-13
**Unity version:** 6000.3.10f1

## 1. Purpose, authority and interpretation

This document records **what the existing runtime does** and separately defines **the intended performance-oriented ownership contracts** for Phase 1. It does not create classes, methods, gameplay rules, supported population limits, implementation permission or a claim that proposed systems already exist.

Authority follows [MMORPG Master PDD](MMORPG-Master-PDD.md): the Master PDD defines project-wide principles; subsystem PDDs own player-facing behaviour; this document owns technical integration decisions subordinate to those PDDs; the [Project Roadmap](Plans/Project-Roadmap.md) and [Phase 1 Implementation Plan](Plans/Phase-1-Implementation-Plan.md) own sequence and delivery. Follow [C# Architecture](CSharp-Architecture.md), [C# Style Conventions](CSharp-Style-Conventions.md), [GitHub Issue and Workflow Guide](GitHub-Issue-and-Workflow-Guide.md) and `AGENTS.md` for all implementation work. **Third-party source, namespaces, packages and vendor assets are read-only without specific user authorisation.** Project-owned PurrNet integrations may evolve; PurrNet itself may not be modified under this contract.

**Authoritative product references:**

- [World Runtime and Instancing PDD](pdd/World-Runtime-and-Instancing-PDD.md) — world/map/instance identity, double precision, 100 m origin cells, 256 m world tiles, streamed scenes, actor interest, persistence across scene unloads.
- [Movement and Traversal PDD](pdd/Movement-and-Traversal-PDD.md) — responsive locally controlled movement, server validation and authoritative world position.
- [Account, Character and Persistence PDD](pdd/Account-Character-and-Persistence-PDD.md) — identity/session separation, location recovery, minimum disconnect grace, timed-state continuity and persistence.
- [AI and Encounter Behaviour PDD](pdd/AI-and-Encounter-Behaviour-PDD.md) — server authority, NPC activation and scalable decision frequency.
- [Combat System PDD](pdd/Combat-System-PDD.md) — cast, aura, cooldown, combat and spatial execution semantics.
- [Graphical Approach PDD](pdd/Graphical-Approach-PDD.md) — URP-facing rendering, LOD and art performance requirements.
- [Live Content and Versioning PDD](pdd/Live-Content-and-Versioning-PDD.md) — immutable compatible release/content state and controlled Addressables ownership.

**Terminology:** **Current** means verified in project-owned source at the reviewed revision, not confirmed by a running Unity build. **Required contract** means the technical invariant later stages must meet. **Deferred** means the named Stage 1 issue owns implementation. **Open** means the relevant PDD or implementation decision does not yet fix a value or method; a benchmark or engineering issue must not silently invent player-facing behaviour.

### Baseline qualification

The current [C# Architecture](CSharp-Architecture.md) is a descriptive snapshot at source revision `3232b1cfc1a889d1f8be7665714bf145e8329a4b`; this document additionally reviews the later `dev` revision above. The [Phase 0.40 issue](https://github.com/FrontierDev/ninthage/issues/40) is closed, but its prescribed `.docs/Phase-0-Stable-Foundation-Baseline.md` is **not present in the reviewed `dev` tree**. That discrepancy must be reconciled before treating the Phase 0 acceptance artifact as independently verified. It does **not** justify changing Phase 0 records within Issue #41.

## 2. Actual current-runtime boundaries

| Area | Current source and behaviour | Contract gap (not implemented by this document) |
| --- | --- | --- |
| Process selection | `Assets/Runtime/Core/GameBootstrapper.cs::Awake` selects `ServerInitialization.Begin` for batch mode/no graphics/forced mode, otherwise `ClientInitialization.Begin`. | Dedicated benchmark modes and unambiguous test-client selection are Stage 1.0 tooling work. |
| ECS clock | `ServerECSManager.ServerInitializeECSWorlds` creates a Unity Entities `World`, registers seven systems in `SimulationSystemGroup` and appends it to the player loop. Spell, aura, cooldown and ticker systems use `UnityEngine.Time.deltaTime`; `ServerInitialization.ServerStartSimulation` is a placeholder. | No single configured authoritative simulation tick/order/catch-up contract (Stage 1.1). |
| GameObject actor and ECS state | `Actor` is a PurrNet `NetworkBehaviour`; `ServerActorManager.OnActorSpawned` creates its `ActorComponent` and buffers. `ServerActorManager.Update` synchronises client-owner transforms into ECS and `LateUpdate` synchronises NPC positions conditionally. | No demonstrated single writer for double-precision spatial state, stable actor–Entity reverse mapping/lifecycle, or job-safe bridge (Stage 1.3). |
| Movement | Owner-controlled PurrNet transform updates feed server transform; `ValidatePlayerPositions` samples float positions and may send correction; `SyncPlayerECS` converts server float transform to `double3`. | The PDD-required absolute-position reconstruction/validation and automatic spatial-interest update are incomplete (Stages 1.1–1.3). |
| World content | `ServerWorldManager.EnsureChunkLoaded` loads `World_X_Y` via PurrNet Addressable additive scenes; `UnloadChunk` unloads. `ServerInterestManager` uses a 3×3 neighbouring scene set. | Origin-cell, streamed world-tile and instance qualifications are not yet separate in all implementation paths (Stages 1.2 and 1.6). |
| Actor interest | `ChunkSubscriptions` indexes actors/NetworkIdentities by scene-name strings; `ChunkVisibilityRule.CanSee` checks subscribed actors; changed-chunk identity re-evaluation is targeted in `ServerInterestManager.OnInterestRequest`. | Does not implement general instance-aware double-precision distance relevance or spatial queries (Stages 1.2 and 1.5). |
| Scene and actor lifetime | `ServerSpawnManager` instantiates runtime actors into the persistent `global_actors` scene; `Actor.OnDestroy` emits `onServerActorDespawned`; `ServerSpawnManager` observes that event for spawn-point bookkeeping. | Complete actor ECS/index/replication cleanup and autonomous persistence lifetime must be defined/implemented (Stages 1.3–1.4 and 1.8). |
| Reconnect / persistence | `ServerConnectionManager.OnPlayerLeft` currently saves a JSON account snapshot and despawns the network identity, then removes connection/session maps. `PlayerDatabase` persists JSON account data. | This does not meet the Persistence PDD's 30-second minimum unexpected-disconnect actor continuity (Stages 1.7–1.8). |
| Client placement | `ClientPositionManager.SetOriginChunk` shifts loaded scene roots and `global_actors`; `ClientWorldManager` tracks delivered world scenes and uses `global_world`. | Precise separation from world tiles, automatic position-driven updates and measured resource lifetimes remain incomplete (Stages 1.2 and 1.6). |

These observations are **not** an instruction to change the named implementation now. They identify which existing pathways the later architecture must reconcile.

## 3. Authoritative time and simulation scheduling

**Status — Issue #64:** **PROPOSED ENGINEERING CONTRACT; NOT IMPLEMENTED OR OWNER-APPROVED.** This section is the Phase 1.1 design candidate for review. All proposed numerical parameters and ordering choices below require explicit owner approval before #65–#78 proceed. Existing code is described separately; this documentation does not change Unity, PurrNet, combat, movement, AI or persistence behaviour.

### 3.1 Current implementation (verified source, not proposed behaviour)

- `Assets/Runtime/Server/Core/ServerECSManager.cs::ServerInitializeECSWorlds()` creates the server Entities world and `SimulationSystemGroup`, registers seven systems, then calls `ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(DefaultWorld)`. Unity's player loop, **not** a server-authoritative fixed-step driver, currently executes those systems.
- `Assets/Runtime/Server/Core/ServerInitialization.cs::ServerStartSimulation()` is a placeholder. The server startup pipeline does not yet start a coherent simulation tick scheduler.
- `ActorUpdateSystem`, `SpellCastSystem`, `CooldownTickSystem`, `AuraTickSystem` and `ActorTickerSystem` use `UnityEngine.Time.deltaTime` for authoritative state. `ResourceRegenSystem.OnUpdate()` currently has **no regeneration implementation**; migrating its scheduling must not invent one.
- `ServerActorManager.Update()/LateUpdate()` perform NPC aggro, player-position validation and actor/ECS synchronisation; NPC aggro and movement-validation accumulation use frame `Time.deltaTime`. `ServerSpawnManager.Update()` drains ECS spawn commands. `ServerSpellcastManager.Update()` counts down `_pendingActions` with frame delta. `ServerAutoAttackManager.Update()` runs active attacks in the ordinary frame loop.
- ECS cast/aura/ticker systems presently invoke `ActorEndpoint.onServer...` callbacks during system-buffer traversal, allowing downstream managers to modify gameplay state and potentially the collections under iteration. These are exactly the cross-domain boundaries #67–#72 must reconcile.
- `ServerECSManager` declares simulation-tick duration/backlog metric definitions, but their presence is **not evidence** that authoritative fixed ticks or accurate backlog accounting already run. The existing `GlobalCooldownDuration = 1.0f` is a gameplay setting, not a server tick setting. Client `Actor.Update`-driven presentation cannot be a gameplay clock.

### 3.2 Stage 1.0 evidence and limits on choosing values

The Phase 1.0 baseline/budgets use five-second measured windows in Unity Editor Play Mode, principally the data-only 100-entity `P1-S0-04` Minimal/Activity fixtures and graphical `P1-S0-06` fixtures. The provisional Editor-only comparison budget is `server.ecs.actor_update_duration` median-run p99 ≤ 0.250 ms and `server.actor_manager.update_duration` median-run p99 ≤ 0.100 ms **for those fixtures only**. These figures are **per measured subsystem**, not the duration or safe throughput of a complete server simulation tick.

No qualified standalone/headless, networked combat, real-player, backlog-recovery or long-running tick-capacity measurement exists yet; the other Phase 1.0 profiles remain blocked/unmeasured as recorded in `.docs/Plans/Phase-1-Performance-Budgets.md`. Consequently, the following settings are **bounded initial engineering proposals**, not measured production recommendations, pass/fail tick CPU budgets or supported concurrency claims. Stage 1.1 tests must measure their consequences; an owner decision may change them without changing PDD-owned gameplay durations.

### 3.3 One clock owner, identity, lifecycle and configuration — proposed

1. **Ownership:** a single server-owned `ServerSimulationClock` (implementation location proposed in `Assets/Runtime/Server/Core`) drives both the ECS system group and registered server-domain work. `ServerECSManager` owns world/system composition and must cease automatic Unity player-loop simulation updates before manual scheduler-driven group updates begin; **never run both**. PurrNet remains the transport owner, not the simulation clock.
2. **Proposed initial frequency:** **30 authoritative ticks/second**, with `fixedDeltaSeconds = 1 / 30` (~33.333 ms) and an integer `long` tick ID. The first completed tick is ID 1; ID 0 is the initial state. All gameplay clock reads use the current tick and fixed delta; future deadlines are represented as integer due-tick values. Configure the frequency on the existing server configuration path, not in individual managers.
3. **Proposed configuration bounds:** **10–60 ticks/second**, inclusive, with invalid/missing/out-of-range settings rejected visibly at server initialization (no silent clamping or substitution). Bounds are engineering guardrails subject to benchmarking, **not** proof that all supported settings give equivalent timing quality. Do not change `GlobalCooldownDuration`, authored cast/aura durations, charge rules or movement tolerances to make the tick rate fit.
4. **Elapsed-time source:** use a monotonic process clock (such as `Stopwatch.GetTimestamp` with its frequency), independent of `Time.deltaTime`, frame count, client clocks and system UTC. Accumulate full monotonic elapsed time at each driver iteration; advance the authoritative tick once per consumed fixed interval. Derive tick time from integer tick ID rather than repeatedly adding rounded floating-point deltas. One ECS world-time update and one ordered scheduled-work pass must occur for each authoritative tick.
5. **Conversions:** convert authored seconds to due ticks **once at scheduling time** with documented ceiling-to-next-tick semantics for positive durations; zero-delay actions become eligible in the next valid execution phase of the current tick when registered before that phase, otherwise no earlier than the next tick. Do not shorten an authored delay. Preserve the actual configured tick interval for periodic effects, phase/stack rules and the one-second baseline GCD. Define repeating intervals from an absolute previous due tick (not repeated addition of drifting frame delta); emit each due occurrence once.
6. **Lifecycle:** initialise after configuration/authoritative-world creation and before simulation starts; run only once per live server runtime; stop accepting new tick work during shutdown; drain or cancel outstanding work according to its owner, complete ECS jobs, detach the old ECS player-loop update, unsubscribe manager/domain registrations and dispose/reset runtime scheduling state. New process/runtime instance starts with tick 0. A local simulated tick is **not** a persistent global ID; durable temporal records use UTC as defined in §3.9. A shutdown/reinitialisation must not execute the old scheduler, callbacks or world a second time.
7. **Responsibilities:** project-owned managers must register scheduled operations with this owner rather than create competing `Update` clocks. Client UI/prediction, Unity rendering and PurrNet transport may still update per frame; those frames must not commit authoritative gameplay time. Actors without game-state ownership must not schedule their own authoritative clock.

**Why 30 Hz is proposed:** at the prototype stage this gives ~33 ms authoritative cadence for movement/validation while retaining 30 integer steps per existing 1.0 s GCD. It makes less frequent decision schedules possible as integer tick multiples. The five-second Editor-only baseline does **not** prove a 30 Hz headless-server budget. **Owner decision required:** accept/replace 30 Hz, the 10–60 Hz validation band, and exact duration-rounding semantics.

### 3.4 Fixed per-tick phase order and legal writes — proposed

Each completed authoritative tick executes the **following ordered stages exactly once**, after consuming the corresponding monotonic accumulator interval. All stages operate on the same tick ID:

| Phase | Name / operation | Permitted authoritative writes and barrier |
| --- | --- | --- |
| **0** | **External ingress staging (outside active tick)** — PurrNet/endpoint callbacks capture validated transport sender, ingress sequence and payload into a bounded staging queue; server-side commands also register here. | May append to ingress staging; **may not** directly mutate an ECS buffer or an actor collection currently being iterated. Source authentication/authority validation remains mandatory. |
| **1** | **Input admission and ordering** — atomically take the staged queue snapshot eligible at tick start; admit no more than the configured per-tick bounded quota; validate actor/session and sequence commands. | Can update an owned command-intent buffer. Rejected commands produce explicit diagnostics; over-quota commands remain queued in order rather than silently disappearing. Inputs arriving after the snapshot begins are eligible **no earlier than the following tick**. |
| **2** | **Movement and actor bridge** — validate eligible owner-controlled movement and apply allowed actor/transform → ECS synchronisation; place corrections and spatial-state commits in owned buffers. | Only the designated movement/actor authority commits validated positions; ECS data writes are finished before ECS iteration. Preserve client-responsive movement plus server validation and project-required double-precision absolute-position direction; defer the broader ownership/index refactor to Stages 1.2–1.3. |
| **3** | **High-frequency ECS simulation** — advance the existing actor simulation group with the fixed world time (including movement/gravity as currently implemented). | ECS systems update their own ECS state in defined system order. Job completion/dependency barriers precede main-thread interpretation; no synchronous manager callback during buffer iteration. |
| **4** | **Timed ECS state and due events** — process registered cast/channel, cooldown, aura, ticker and already-implemented regeneration work at the same tick; collect due events. | Timed ECS systems update timing fields and append due-event records, not immediate domain writes. Preserve authored cast completion, periodic effect, expiry, stacking, GCD, cooldown/charge and auto-attack rules. Empty regeneration remains empty until independently designed. |
| **5** | **Deferred domain callbacks and effects** — drain due callbacks/queued gameplay events in stable order; validate their actor/registration lifetime and apply authorised effects. | Domain managers may mutate their **owned** ordinary state if no collection is being traversed; structural ECS changes and any changes unsafe during current iteration become commands for phase 6 (or a future tick). Event handlers must not recursively enter this same drain. |
| **6** | **Structural/lifecycle commit** — play back ECB commands and settle spawn/despawn, registration/removal and actor/GameObject bridge lifecycle (including the current `WorldSpawnSystem` → `ServerSpawnManager` handoff). | Only this safe barrier performs structural entity creation/removal, PurrNet actor materialisation and removal. Events generated during playback are staged for the **next** eligible tick; no mid-iteration invalidation or duplicate actor registration. |
| **7** | **State publication** — capture post-commit authoritative state for network snapshots, corrections and per-tick telemetry; pass it to the supported PurrNet transport/presentation pathways. | Read the completed tick snapshot; network transport/presentation timing may differ, but must never publish partially committed authoritative gameplay state. |

**Ingress order:** assign a process-local monotonic sequence **at receipt**. Phase 1 processes by received sequence, with stable actor ID then command registration ID as a tie-break for simultaneous/enqueued domain-origin entries. No unordered dictionary traversal may define observable order. A command received while tick N executes is admitted at N+1 or later (depending on backlog), **never retroactively in N**.

**Cross-domain writes:** input cannot write ECS while ECS jobs iterate; ECS cannot invoke managers while buffers iterate; deferred manager callbacks cannot structurally change ECS until phase 6; phase-6 lifecycle callbacks cannot restart phases 1–5. Callbacks/replies that must be synchronous for a PurrNet/framework requirement remain the framework's responsibility, but their **gameplay mutations** must be admitted through the server's ordered boundary. Do not patch vendor code.

**System ordering:** project-owned registration must explicitly order `WorldSpawnSystem`, `ActorUpdateSystem`, `ResourceRegenSystem`, `SpellCastSystem`, `CooldownTickSystem`, `AuraTickSystem` and `ActorTickerSystem` within their assigned stages. The phase-4 relative order of cast/cooldown/aura/ticker work is as listed; if tests show an existing gameplay-relevant different ordering, stop for owner adjudication instead of silently changing it. Structural ECB playback remains at phase 6; the ECS group must not be auto-updated a second time by Unity.

### 3.5 Unified scheduling and equal-deadline semantics — proposed

The scheduler supports **four registration classes**, not four independently advancing clocks:

1. **Every-tick simulation:** deterministic high-frequency ECS/domain work on every authoritative tick, including authoritative movement-related state.
2. **Lower-frequency actor/NPC decisions:** integer tick strides relative to the common clock (for example, `decisionStrideTicks`), configured centrally or by later AI/activity scheduling. This is an engineering execution cadence, **not** a new NPC threat, target, aggro, pathfinding or combat rule; the AI PDD leaves its numeric frequency to the approved engineering policy below. A due lower-rate decision must not run twice after a stall.
3. **Due-at-tick/deadline work:** one-shot or repeating cast impacts, completion, GCD/cooldown, aura/ticker and other server timers. Represent each deadline with a stable due-tick ID and registration handle; a due event is executed **once**, and repeaters advance their next due tick without drifting from the original cadence. Preserve effect-specific rules about last tick versus expiry; do not universally force an additional final periodic tick.
4. **Urgent event-driven work:** interrupts, deaths, dispels, changed target/aggro state, new spawns and other legitimate event triggers can request the earliest *eligible phase* without polling. An urgent request does not pre-empt a currently iterated ECS buffer or bypass input/authority checks.


#### Issue #99 — Approved initial NPC/auto-attack scheduling policy (17 October 2026)

**Decision status: OWNER-APPROVED for #100 and #101.** This is the initial Stage 1.1 *engineering cadence*, not a new NPC behaviour rule, performance claim, or approval of later relevance/AI tiers. The AI and Encounter Behaviour PDD §44 intentionally leaves the general AI update frequency open; the concrete initial implementation policy below is approved here. The authoritative simulation currently uses `ServerSimulationTickDuration = 0.05f` (20 Hz); do not confuse this with §3.3's historical 30 Hz proposal or change the simulation tick duration as part of #99.

| Operation | Approved cadence | Implementation owner |
| --- | --- | --- |
| NPC proximity search, pending-target acquisition and aggro-delay decisions | **200 ms nominal (5 Hz)**, one centrally configured lower-rate schedule | `ServerActorManager.UpdateNPCAggro` → `ServerSimulationClock.RegisterLowerRate` (#100) |
| Auto-attack eligibility and ordinary spell request | **Every authoritative simulation tick** (20 Hz with the present 50 ms tick) | `ServerAutoAttackManager` → `ServerSimulationClock.RegisterEachTick` (#101) |
| NPC chase destination/stopping distance | **Every Unity render frame**, as existing NavMesh engine integration; no authoritative swing decision in this frame path | `ServerAutoAttackManager` / existing `NavMeshAgent` (#101) |
| Spell casts, cooldown advancement, damage and other timed combat work | **Unchanged authoritative timing and existing combat owners** | Existing combat systems; not part of #99–#101 |

**Configuration and startup validation.** #100 owns adding the single global `GameConfiguration.NPCAggroDecisionIntervalSeconds` value, with initial default **`0.2f`**; there is **no per-NPC cadence override** at this stage. Validate the value at server startup: finite, strictly positive, and convertible to an `int`-representable positive tick stride at the configured fixed delta. Convert *once* using the existing non-shortening `ServerSimulationClock.GetNonShorteningTickCount(seconds, fixedDeltaTime)` rule and reject invalid/unrepresentable configuration explicitly; never silently clamp or fall back. For a 0.05 s server tick, 0.2 s is **four ticks**. At another allowed simulation rate, the effective interval is the **smallest whole number of ticks not shorter than 200 ms**; do not hardcode four ticks. No separate auto-attack rate setting is needed because every-tick evaluation is approved.

**Aggro-delay and candidate timing.** An NPC's authored `NPCBehavior.AggroDelay` remains a *duration*, not a count of perception updates. Its pending acquisition begins at the **first scheduled decision tick that actually detects the candidate**; no time before that observation is credited. Record the start tick and allow acquisition **no earlier than** the first legal decision at/after `startTick + nonShorteningTicks(AggroDelay)`, subject to the same candidate being eligible at that decision. A zero-duration delay permits acquisition at the detection decision; positive delays are never shortened. If the candidate disappears from the existing physics query or changes, clear the pending candidate/deadline; reacquiring starts a fresh authored delay. Maintain the current closest-live-player selection, target-lock semantics, `Physics.OverlapSphere` behaviour and aggro/start-autoattack handoff—do **not** add new faction, LOS, threat or stealth logic in this scheduling migration. Discrete decisions can detect entry/exit up to one perception interval later than their actual physical occurrence.

**Auto-attack and event eligibility.** On every completed authoritative ECS tick, evaluate each currently active attacker's **existing** non-null actor/target, casting, range and cooldown conditions and request the existing `ActorSpellcaster.ServerOnly_RequestSpellCast` at most once for an eligible attacker at that tick; keep the ordinary spell/cooldown owner authoritative. Do not create a second melee timer, guarantee an extra swing, or change cooldown/GCD/range/casting policies. Existing `ActorEndpoint.onServerStartAutoAttack` and `onServerStopAutoAttack` register/unregister active attackers; `onServerActorSpawned`, `onServerActorDespawned`, and `onServerActorOwnerChanged` provide actor-lifetime signals already present in the project. Manager teardown cancels its single clock registration and clears its own per-actor pending state; actor death, retirement or lost target prevents stale acquisition/swing. A restarted attack must not accumulate duplicate registrations.

**Urgent work and ordering.** Use an existing relevant callback to update eligibility/cancel stale work at the **next legal authoritative boundary**, without executing combat or physics perception inside an active ECS iteration. Regular 200 ms perception is the baseline when no supported urgent signal exists; **do not invent** a general target-change/hostile-action event, a second event bus or per-NPC callback infrastructure merely to accelerate it. `ServerSimulationClock` already executes registered callbacks **after** `ServerECSManager.UpdateSimulation()`; retain that safe boundary and the clock's cancellation/reentrancy rules. Continuous `NavMeshAgent.SetDestination`/stopping-distance updates may remain in `Update()` solely for Unity navigation intent, while **all authoritative spell-request eligibility moves off render `Update()`**. The NavMesh agent remains the movement owner when enabled.

**Validation gate.** #100 and #101 must retain owner PlayMode/headless checks for first detection and authorable aggro delay, candidate departure/reacquisition, attack readiness while casting/out of range/on cooldown, moving-target chase, target death/despawn, stop/restart, and multiple catch-up ticks within one render frame. Verify no early aggro or duplicate swing; inspect actual elapsed tick IDs, stable actor cancellation and existing metric scope. These are source/design acceptance checks, **not** claims of completed runtime testing. Any #76 regression-test child must separately satisfy the workflow's four-part critical-coverage gate. Do not add Stage 1.2 spatial indexing, Stage 1.4 simulation LOD/dormancy, new AI rules, gameplay/RPC/protocol changes or third-party modifications.


**Ordering key:** `(dueTick, phasePriority, actorIdentity, registrationSequence)` for independent due callbacks, all compared stably. Ordering *within* an actor's authored effect chain retains its existing explicit effect order. Global registration sequence is monotonic for the scheduler lifetime, and handles include a generation or otherwise prevent a recycled registration identifier from inheriting stale events. Where an actor has no ID, a stable registered operation identity is required; memory addresses/hash-map enumeration are not tie-breakers.

**Cancellation and reentrancy:** cancellation or actor despawn invalidates its pending registrations before subsequent dispatch. Each callback is marked claimed before invocation, exactly once; a duplicate completion, dispel, expiry or auto-attack command must not retrigger it. A callback that schedules another callback for a phase already being drained (including a zero-delay one) executes **no earlier than the next tick**, avoiding unbounded same-tick recursion. A callback scheduled before its target phase can run in that phase during the current tick where the authored timing permits. Additions/removals during dispatch are staged and committed after traversal. Exceptions are surfaced with tick/actor/registration context and must not silently discard unrelated due events.

**Ownership and cleanup:** actor-specific registrations follow the authoritative actor lifetime, not the Unity scene lifetime. Cancel on actual actor retirement and runtime teardown, **not** simply when an environment tile unloads or a PurrNet connection drops. A still-authoritative disconnected character continues due combat/timed events as required by the Persistence PDD.

### 3.6 Bounded catch-up and overload — proposed

- **Proposed default:** maximum **4 authoritative simulation ticks per Unity driver iteration/frame**, configurable in the server runtime configuration with proposed valid bound **1–8**. This caps the *number* of catch-up steps, **not** the CPU duration spent in any single step. The initial 4-step ceiling corresponds to up to ~133 ms of simulation time at 30 Hz, not a 133 ms CPU budget.
- Always add the *full* elapsed monotonic duration to accumulator/debt; do **not** discard accumulated time, change `fixedDeltaSeconds`, compress multiple gameplay ticks into one, or jump tick IDs to make the backlog look healthy. Consume at most the cap per driver iteration and leave the remainder pending. If backlog remains when a frame ends, resume from the oldest unprocessed tick on the next iteration; during spare capacity, drain debt at the permitted cap until caught up.
- Record at least: `currentTickID`, executed ticks/iteration, elapsed tick execution time, pending whole ticks, pending simulation seconds, age of oldest due event, ingress/deferred queue depths, deferred/due/processed/cancelled counts, missed wall-clock deadlines and consecutive overloaded iterations. A deliberately delayed event remains a **late event**, not a completed-on-time or dropped event.
- Classify **normal** (no whole-tick backlog), **catching up** (positive backlog but decreasing), **overloaded** (backlog persists or increases across successive driver iterations), and **sustained overload** (bounded rolling observation confirms continued growth). Classification windows and alert thresholds are operational configuration to be validated with real load tests; do not invent a production acceptance threshold from a five-second Editor run.
- During overload, the server must preserve authoritative simulation/event order and expiry semantics. It may reduce *non-authoritative* work, such as redundant presentation snapshots or optional diagnostics, while maintaining required PurrNet control and correction traffic. It must not skip ticks, teleport timers to current wall time, discard combat/AI obligations, give players an advantageous cooldown, or auto-despawn actors to conceal overload.
- Under sustained overload, mark health/readiness visibly degraded, publish rate-limited diagnostics and require operator/load-test investigation or an explicitly approved capacity/admission policy; do not silently introduce disconnects, gameplay-rate slowing, or automatic shard migration. If incoming commands exceed the bounded admission quota, retain ordered backlog and expose its age/size, with **no silent loss**.
- **Remaining open numerical choice:** the safe per-tick input-admission quota and sustained-overload threshold need qualified headless/multiplayer measurements. The proposed 30 Hz / 4 catch-up values are subject to owner review and benchmarking. If the server cannot keep up, the documented outcome is growing debt and a visible failure of the performance gate, not a false claim that timers ran on time.

### 3.7 Mapping existing owners to stages — implementation hand-off

| Current owner/path | Intended stage | Stage 1.1 issue |
| --- | --- | --- |
| `ServerInitialization.ServerStartSimulation()`, `ServerECSManager.ServerInitializeECSWorlds()` and seven-system group | Clock lifecycle, phases 3–4, teardown and telemetry | #65–#68 |
| `ServerActorManager.Update/LateUpdate`, `ValidatePlayerPositions`, `SyncPlayerECS` | Phases 1–3 for admitted movement, validation and ECS bridge; no separate authoritative `Time.deltaTime` loop | #73–#74 |
| `ServerSpawnManager.Update` / `WorldSpawnSystem.OnUpdate` | Phase 6 structural/spawn commit; new spawn notifications become next-tick eligible | #67, #73–#74 |
| `SpellCastSystem`, `CooldownTickSystem`, `ServerSpellcastManager._pendingActions`, `ServerCooldownManager` | Phases 4–5 fixed-tick cast/channel, delayed impacts and cooldown/charge timing | #69–#70 |
| `AuraTickSystem`, `ActorTickerSystem`, `ServerAuraManager`, `ServerTickerManager` | Phases 4–5 expiry, periodic effect and cancellation; no ECS-buffer-mutation callback | #67, #71–#72 |
| `ResourceRegenSystem.OnUpdate` (currently empty) | Phase 4 registration only; no invented regeneration gameplay | #73–#74 |
| `ServerActorManager.UpdateNPCAggro`, `ServerAutoAttackManager.Update` | Shared lower-rate/deadline/urgent scheduling using phases 4–5 | #75–#76 |
| Server sessions/persistence, `ServerConnectionManager.OnPlayerLeft` | Time-source boundary and inactivity policy, not reconnect implementation | #77–#78 |

Do not conflate tick frequency with how frequently PurrNet transports packets or Unity renders. The later Stages 1.2–1.5 own spatial indexing, single-writer actor mapping, activity tiers and network prioritisation.

### 3.8 Monotonic simulation time versus process uptime

- **Simulation time:** integer tick count and fixed delta while the active server simulation is running. This is the only source of active combat/cast/aura/deadline progression. Advancing process uptime does not itself execute a gameplay tick.
- **Process uptime:** monotonic elapsed process time including startup, shutdown and driver stalls; useful to compute the accumulator and diagnostics. It is not a persisted gameplay deadline and resets on process restart.
- **No client authority:** client-side clocks, local pause, rendered frame cadence and client-supplied timestamps never authorise cooldown expiry, damage, regeneration or persistence eligibility.

### 3.9 Authoritative UTC, inactive actors and restart reconciliation

- **Implemented durable time owner:** `ServerInitialization.TimeAuthority` owns one `ServerTimeAuthority` for each live server lifetime. Its `ReadObservation()` pairs a canonical `DateTimeOffset` UTC instant with a `Stopwatch` timestamp, frequency and in-process source identity. Runtime construction uses `DateTimeOffset.UtcNow` and `Stopwatch.GetTimestamp`; deterministic delegates are supplied only through construction for later tests. Monotonic readings, frequency and source identity are not persisted or compared across restarts.
- **Implemented observation validation:** `ServerTimeAuthority.ValidateObservationPair` requires two observations from the same live source and an explicit non-negative caller-approved tolerance. It reports reversed monotonic order, backwards UTC, checked duration overflow and forward/backward out-of-tolerance clock discontinuity as distinct errors; it never rewrites UTC or advances simulation time.
- **Implemented opt-in reconciliation contract:** `ServerTimedStateReconciliation.EvaluateEligibleElapsed` accepts only a server-persistence UTC origin, validates it against a current #104 observation, and returns both validated real elapsed time and domain-authorised eligible time. It is pure; a domain rule may explicitly exclude offline time, but this helper never advances ticks or applies state.
- **Boundary:** active registered actors/casts/auras/cooldowns progress via authoritative ticks. At a legitimate persistence boundary, only fields whose **owning gameplay/Persistence PDD** requires durable advancement are translated into validated server-UTC expiry/recharge records and saved with enough identity/source metadata; on restore, compare against current server UTC and reconstruct the appropriate remaining active tick duration. Require a known conversion epoch/time-source pair; never persist raw tick ID as an absolute deadline.
- **Disconnect remains a future contract:** losing a PurrNet connection is **not** automatic inactivity. The unimplemented Stage 1.7–1.8 policy still requires a minimum **30-second disconnect grace** and combat continuation while unresolved combat requires it; this helper does not change `ServerConnectionManager.OnPlayerLeft()` or claim reconnect support.
- **Future activity handoff (Stage 1.4 contract only):** a domain must record one authoritative active→inactive or inactive→active handoff, then let exactly one owner process the interval according to that domain's rule. Activity tiers and sleeping actors are not implemented here.
- **Future persistence/restart reconciliation (Stages 1.7–1.8 contract only):** persistence and reconstruction of cooldowns, auras, resources, Health and death state remain domain-owned. This helper adds no records, fields, writes or generic offline formula; invalid saved UTC data must fail visibly when a later owner opts in.
- **No generic offline formula:** no blanket offline Health refill, Energy/Mana tick, Rage reset, aura strip, cooldown wipe or progress catch-up is approved here. The time authority supplies conversion and reconciliation primitives; the owning gameplay domain specifies whether and how an offline period counts.

### 3.10 Reproducibility, instrumentation and required tests

Within one server with the same admitted input/event sequence, all gameplay-affecting phases and same-deadline callbacks have a stable order. This is **not** a promise of bitwise physics determinism or identical simulation across CPU architectures/Unity builds. Never sort by an accidental ECS chunk order or unordered map traversal.

Issues #66, #68, #70, #72, #74, #76 and #78 own **separate test-only** regressions: 30/60/144 render-FPS parity; clock start/stop/reinitialisation; integer due-tick conversion; zero/one/long deadline, periodic remainder and terminal expiry; same-deadline order; cancellation; callback reentrancy; one-time spawn/despawn; NPC/auto-attack timing; controlled frame stalls and bounded catch-up without dropped combat events; visible telemetry debt; UTC wall-clock changes that do not advance active simulation; disconnected active-actor continuity and invalid UTC restoration. Compare against Phase 1.0 evidence only with matching fixtures, hardware and qualification. No tests or runtime changes are made by Issue #64.

### 3.11 Issue #64 owner approval checklist — **PENDING**

- [ ] **Frequency:** explicitly accept/replace proposed **30 Hz**, allowed **10–60 Hz**, integer tick ID and positive-delay rounding policy. Benchmark any changed choice.
- [ ] **Catch-up:** accept/replace **4 ticks/driver iteration**, permitted **1–8**, debt conservation, slow-recovery and persistent-overload reporting; confirm separately measured admission/alert parameters remain gating items.
- [ ] **Phase order:** approve phases 0–7, the seven named ECS systems' relative order, first-next-tick incoming input eligibility and cross-domain write barriers.
- [ ] **Same-deadline/callback:** approve stable (due tick, phase, actor identity, registration sequence) ordering, exact-once/cancellation, earliest eligible phase and next-tick reentrant rule; require observed behaviour equivalence.
- [ ] **Lifecycle:** approve a single `ServerSimulationClock` owner, elimination of automatic ECS player-loop double updates, ECS/job/world teardown and registration disposal.
- [ ] **Time split/inactivity:** approve monotonic active simulation versus process uptime versus server UTC, disconnected active-actor continuity, and per-domain-only offline reconciliation.
- [ ] **Evidence:** acknowledge that existing five-second Editor-only baselines do **not** justify a standalone production tick/overload budget; keep unmeasured headless/networked profiles and later-gameplay decisions open.
- [ ] **Scope review:** inspect the documentation-only diff; confirm **zero** changes to `Assets/`, tests, configuration, vendor code, metadata or gameplay rules.

**Gate:** #65 and all dependent Stage 1.1 runtime changes remain **blocked pending explicit owner approval of this contract**. If any checklist item is rejected or unsettled, update the proposal and retain `needs-design` / `needs-validation`; do not report #64 complete solely because this document was committed. No Unity compile, Play Mode, server load test or performance claim is implied by this documentation update.


## 4. Actor, gameplay and network identity

An actor's identities have distinct owners and lifetimes:

| Identity | Authority and lifetime contract | Current evidence / limitation |
| --- | --- | --- |
| AccountID / CharacterID | Persistent account/character identity; database is the eventual durable authority. Account, session and selected character must remain distinct. | Persistence PDD requires these; current `PlayerDatabase` and `PlayerAccountData` do not yet constitute the final SQL identity model. |
| Gameplay session | Server-owned login/character-control continuity, potentially surviving transport disconnection. | `ServerConnectionManager` currently indexes player sessions by PurrNet `PlayerID` and removes them on leave. |
| Network connection / `PlayerID` | PurrNet connection identity; **not** a durable character key and never the sole key for continued combat state. | Used in connection and actor-owner maps today. |
| Runtime actor ID | Per-spawn/session game-world actor identity; server owns uniqueness and actor lifecycle. | `Actor.ID` is `System.Guid`, assigned/initialised in `Actor.Initialize`; it is not automatically a persistent `CharacterID`. |
| ECS `Entity` | Process-local handle for server simulation representation; never persisted as a character identity or sent as a durable ID. | Created in `ServerActorManager.OnActorSpawned`; no complete reverse lifecycle/reconciliation path is established. |
| PurrNet `NetworkIdentity` | Transport-visible replicated representation, with PurrNet spawn/despawn/ownership rules. Its presence does not define whether gameplay state exists. | Instantiated in `ServerSpawnManager.Update` and located in `global_actors`. |
| Spawn point `GUID` | Authoring/runtime respawn association, not actor lifetime or character identity. | `ServerSpawnManager` maintains a spawn-point/actor map. |
| PurrNet `SceneID` / Unity `Scene` | Loaded content/transport scene instance identity, not logical world, map or gameplay instance identity. | `ActorComponent.CurrentScene` currently stores a `SceneID`. |

**Required contract:** One server actor-lifecycle authority reconciles its gameplay identity, ECS representation and PurrNet identity through spawn, ownership transfer, visibility change, dormancy, reconnect, despawn and shutdown. Exactly one authoritative runtime actor may represent a selected character in one gameplay session. Representation destruction must not implicitly erase persistent or still-live authoritative state.

**Stage 1.3 implementation decision left explicit:** The exact physical owner of the actor-ID-to-Entity mapping and the authoritative data store for per-actor position (ECS component versus server-owned spatial/domain state) is to be resolved by Stage 1.3. Whichever is chosen must be a **single writable authority**, not parallel independent stores. Defining that ownership now does not require introducing a new class or demanding a full ECS rewrite.

## 5. Position ownership and synchronisation

### 5.1 Required state ownership matrix

| State | Current writer / reader | Intended writer of authority and synchronisation direction | Lifetime and invariant |
| --- | --- | --- | --- |
| `Actor.WorldPos` (`WorldPosition` doubles) | `ServerSpawnManager.Update` initialises it; `ServerActorManager.OnActorSpawned` reads it. Other runtime code may also consume it; no continuous one-writer rule is established. | Server spatial-authority path owns the **absolute** value; where `Actor.WorldPos` remains as an exposed view, it is updated **from the accepted authoritative position**, never from an unrelated client/local-origin transform. | Survives origin/scene changes; where required, saved with logical world/map/instance context. Must match authoritative ECS view after the agreed commit boundary. |
| `ActorComponent.WorldPositionD` (`double3`) | Initially written from `Actor.WorldPos`; `ServerActorManager.SyncPlayerECS` copies player `Transform` coordinates; `LateUpdate` copies NavMeshAgent NPC transforms; `ActorUpdateSystem.OnUpdate` writes ECS motion state. | Exactly one server-approved motion/position update commits the double-precision authoritative value for each actor. ECS is either that canonical storage or a synchronised projection, chosen explicitly in Stage 1.3; **never both independent authorities**. | Process-local entity lifecycle; removed/recreated with entity. Not a durable persistence identifier; invariant under client origin shift. |
| Server `Actor.transform.position` (`Vector3`) | PurrNet owner-authoritative movement updates player transform; enabled NPC `NavMeshAgent` drives its transform; ECS gravity writes other NPC transforms in `ServerActorManager.LateUpdate`. | Presentation/physics/NavMesh or received movement **candidate** in server-local coordinates; server validation/reconstruction precedes committing authoritative world state. After accepted movement, project canonical position back to Unity space where needed. | Exists only while GameObject is instantiated; float transform must not be treated as globally authoritative, particularly across wide-world positions. |
| PurrNet owner movement / `NetworkTransform` state | Client sends movement according to owner authority; server reads resulting transform and may issue `MovementEndpoint.Client_ServerCorrection`. | Client remains responsive and can propose/predict motion; **server validation owns legality**. Accepted movement updates authoritative spatial state, then relevant network state/interest; corrections originate on server. Use supported PurrNet facilities, not a second competing transport. | Connection/visibility lifetime. A rejected candidate does not modify committed authoritative position, gameplay range or cross-instance membership. |
| Client `Transform.position` / floating origin | `ClientPositionManager.SetOriginChunk`, `ShiftSceneRoots` and `NetworkToLocal` manage shifted local positions. | Client-local projection from absolute authoritative coordinate and that client's origin. Outgoing movement must carry enough origin/cell context for absolute reconstruction; client shifts never modify server absolute position. | Scene/presentation lifetime; a local origin shift is not gameplay motion or a teleport. |

The **authority** in the table is the *server-accepted absolute spatial state*, not an assertion that the present `WorldPositionD` writes all satisfy it. The current server transform-to-`double3` bridge can lose intended absolute-coordinate semantics after origin/cell changes; Stage 1.3 must reconcile storage ownership, and Stage 1.2 must consume that authoritative output.

### 5.2 Direction by actor/path

| Path | Intended information flow | Validation and ownership constraint |
| --- | --- | --- |
| Player input | Client local simulation → owner movement proposal with spatial context → server legal-movement check → committed absolute position → ECS/spatial index/replication projections. | Client authoritative over responsiveness, **not** server gameplay truth; speed, mode, spatial context, status and world/instance changes validated server-side. |
| NavMesh-driven NPC | Server navigation result → authorised absolute-position commit → ECS view, spatial index and replicated transform. | Unity NavMesh/Physics is a motion provider, not a separate gameplay-distance authority. |
| ECS-driven NPC | Scheduled server ECS motion → absolute-position commit → GameObject/replication where materialised. | No second `LateUpdate` writer may overwrite newer authoritative ECS results. |
| Teleport / forced movement / world transfer | Authoritative action validates destination and context → atomic logical-context and position transition → update spatial memberships/visibility and relevant scene/actor representations. | No intermediate externally visible state in an invalid or old instance; PDD owns valid fallback behaviour. |
| Client floating-origin correction | Absolute position + new local origin → derived client scene/actor `Vector3` shifts only. | No effect on authoritative distance, ownership, timers, simulation or membership. |

## 6. Spatial coordinate and runtime-context model

**These concepts are not interchangeable:**

| Concept | Owner and meaning | Current value / future requirement |
| --- | --- | --- |
| Absolute world position | Server-authoritative `WorldPosition`/double-precision coordinates within an identified logical map/world context. | Already represented by `WorldPosition` and ECS `double3`; coordinate-use correctness incomplete. |
| **Origin cell** | Position encoding and client floating-origin projection. | World Runtime PDD: **100 × 100 m** horizontally; `WorldPosition.Chunk` currently uses `GameConfigurationManager.Config.ChunkSize`. |
| **World tile** | Streamed environment/terrain authored region, independent of origin cell. | World Runtime PDD: **256 × 256 m** terrain tiles; some current runtime `World_X_Y` chunk handling still conflates concepts. |
| **Spatial-index cell** | Internal broad-phase search bucket for combat, perception and network relevance. | **Not implemented as the shared authoritative index.** Cell size/configuration Stage 1.2, independent of both 100 m/256 m grids. |
| Authored scene asset | Reusable `.unity` scene/content from Addressables. | Existing `World_X_Y` and `global_actors` names are asset/runtime conventions, not instance identities. |
| Loaded additive scene / `SceneID` | Specific Unity/PurrNet loaded scene, delivering content and/or holding representations. | Currently used by `ServerWorldManager` and scene subscriptions; may be reused across logical contexts only with correct separate identity. |
| **World** | Explicit persistent player-selectable world (`World 32` in PDD example). | World Runtime PDD defines concept; not equivalent to a process or scene. |
| **Map** | Independent spatial coordinate space within world/runtime context. | Map ID must not be inferred solely from numerical coordinates. |
| **Runtime instance** | Isolated gameplay simulation context sharing authored assets if appropriate. | Instance ID and isolation not completed; not inferred from `SceneID` or scene asset name. |

**Required query/context rule:** Every spatially relevant actor/query/replication operation first checks compatible **world + map + runtime instance** according to the World Runtime PDD, then local spatial candidates, then an exact authoritative double-precision range/line-of-sight check where required. Identical coordinates in two private instances never imply visibility, targeting, aggro or combat interaction. A player may share authored scene assets with players in other contexts without sharing simulation state.

**Spatial indexing authority (Stage 1.2):** A server-owned spatial membership service maintains actor current region and context; publishes insertion, accepted movement, removal, teleport/transfer and context teardown to consumers. AI, combat, interaction and replication must use the same qualified spatial source rather than independent global scans or a spawn-scene string. World/environment tile subscriptions remain a separate delivery mechanism. The existing `ServerInterestManager` changed-chunk targeted reevaluation is a useful retained starting point; do not falsely characterise it as an all-actor scan every interest update.

**Open implementation values:** Spatial index structure/cell size, actor visibility range, enter/exit hysteresis values and independent environment scene radii. The PDD fixes neither actor range nor index dimensions; Stage 1.2 must select/tune implementations without treating 100 m or 256 m as mandatory index-cell dimensions.

## 7. Replication and client/server authority

- **Server/gameplay** owns whether an actor or action exists, which simulation instance it belongs to, what effects resolve, and which client is eligible to receive its authoritative state.
- **PurrNet** owns transport connection and network-identity lifecycle; the project-owned integration determines relevance within PurrNet's supported visibility/replication APIs. Do not implement a parallel networking stack.
- **Client** owns presentation, rendering, UI, local prediction and movement proposal, never authoritative ability targets, aura/timer expiry, persistent state or visibility permissions.
- **Interest** uses context compatibility → regional candidate filtering → precise range → gameplay exceptions defined by their owning PDDs. Environment scene membership alone is insufficient for final actor visibility.
- **Relevance changes** must update subscriptions/identity visibility without stale recipients or cross-instance leakage. Entry/exit hysteresis may be used when tuned (World Runtime PDD).
- **Phase 1.5** specifies update-priority/frequency, available PurrNet bandwidth counters and replication-budget behaviour. This document establishes ownership **only**; it does not claim prioritised replication or network LOD currently exists.

Existing `ChunkVisibilityRule.CanSee` currently tests `CurrentChunk` scene-subscription membership, and `ServerInterestManager.OnInterestRequest` processes client-provided centre-chunk requests. Neither is by itself a guarantee of authoritative absolute position or logical-instance isolation. Normal movement must eventually drive server-approved world-tile and actor-interest updates (Movement PDD).

## 8. Lifecycle contracts and sequence diagrams

The following are **logical responsibility diagrams**, not existing call-graph or new API declarations. Each is labelled with the observed current path and the missing stage contract.

### 8.1 Spawn and first representation

```text
[CURRENT, observed]
PurrNet additive scene becomes loaded
  -> ServerSpawnManager.OnPostSceneLoaded finds authored ActorSpawnPoint
  -> WorldSpawnSystem emits SpawnCommand
  -> ServerSpawnManager.Update instantiates via PurrNet in global_actors
  -> Actor gets initial WorldPos, ID, Owner, CurrentChunk
  -> ChunkSubscriptions registers NetworkIdentity / player association
  -> onServerActorSpawned
  -> ServerActorManager.OnActorSpawned creates ECS ActorComponent + buffers

[REQUIRED, later stages 1.2-1.4]
Authoritative spawn request + resolved world/map/instance/context
  -> validate definition, spawn ownership, location and instance
  -> create ONE actor identity + authoritative state
  -> insert spatial membership and ECS representation as required
  -> materialise GameObject/NetworkIdentity when relevant
  -> expose to authorised observers only after state is complete
```

**Invariant:** Spawning the same actor through multiple paths cannot create duplicate authoritative state. A spawn point is not actor identity. Authored environment scenes may define spawn locations but need not own active actor lifetime.

### 8.2 Normal player movement and region change

```text
[CURRENT, observed]
Client movement -> owner-authoritative PurrNet NetworkTransform
  -> server Actor.transform.position
  -> ServerActorManager.ValidatePlayerPositions (periodic float speed check)
  -> possible Client_ServerCorrection RPC
  -> ServerActorManager.SyncPlayerECS (float Transform -> double3)

Client-side chunk interest request
  -> MovementEndpoint.Server_RequestChunkInterest
  -> ServerInterestManager.OnInterestRequest
  -> PurrNet scene subscriptions / chunk visibility re-evaluation

[REQUIRED, stages 1.1-1.3 / 1.5]
Client movement + absolute position context
  -> server validates and accepts/rejects movement
  -> one absolute double-precision position commit
  -> ECS representation + spatial membership update
  -> world-content interest update (only if tile relevance changed)
  -> actor replication interest update (independent of scene interest)
  -> visible client representations / correction where needed
```

**Invariant:** Local floating-origin changes and unrelated streamed-tile boundaries cannot make an actor move, change logical instance or bypass range validation.

### 8.3 Environment scene unload with continuing actors

```text
[CURRENT, observed]
Player interest region changes or player unsubscribes
  -> ServerInterestManager updates subscriptions
  -> ServerWorldManager.UnloadChunk -> PurrNet scene unload
  -> actors instantiated in global_actors do not reside in that tile scene

[REQUIRED, stages 1.2 / 1.4 / 1.6]
Last content subscriber removed
  -> settle/abort pending tile scene load where possible
  -> unload environment scene and release its resource handles
  -> preserve authoritative actors independent of scene residency
  -> independently evaluate actor activity and replication relevance
  -> clean content scene/identity registries without orphan references
```

**Invariant:** Unloading an environment tile cannot alone delete an active persistent actor or its durable combat/session obligations. PDD leaves NPC despawn/respawn policy to the relevant NPC/AI system.

### 8.4 Actor despawn versus session termination

```text
[CURRENT, observed]
ServerConnectionManager.OnPlayerLeft
  -> save account JSON snapshot
  -> NetworkIdentity.Despawn
  -> remove session/connection maps
Actor.OnDestroy (server)
  -> ActorEndpoint.onServerActorDespawned
  -> ServerSpawnManager clears matching spawn-point association

[REQUIRED, stages 1.3 / 1.4 / 1.7 / 1.8]
Authoritative lifecycle decision (not simply loss of network connection)
  -> resolve combat/timers/persistence ownership
  -> remove actor from observer interest and spatial index
  -> remove/retire ECS state and stale actor-ID mappings
  -> despawn PurrNet identity / release presentation object if present
  -> checkpoint durable state at defined safe boundary
  -> release subscriptions, event handlers, handles and caches
```

**Current gap:** `ServerActorManager` subscribes to spawn/owner/interest events, not `onServerActorDespawned`; no complete ECS/actor-map cleanup is shown by that manager. This is a lifecycle correctness gap for the later stages, **not** a change authorised by Issue #41. Identity destruction cannot be used to infer that all ECS/interest state was safely removed.

### 8.5 Enter or leave a private runtime instance

```text
[REQUIRED ONLY — logical instance transitions are not complete today]
Authoritative transition request or world boundary reached
  -> validate destination world/map/instance and entry eligibility
  -> ensure required simulation context and streamed destination ready
  -> commit logical context + double position together
  -> leave old spatial/replication audiences
  -> enter destination spatial/replication audiences
  -> deliver content/actor state through PurrNet scene/identity handling
  -> unload old content only when no longer required
Failure before commit -> remain in valid previous context / PDD fallback
```

**Invariant:** Equivalent coordinates or identical authored Unity scenes must not mix two private instance populations. Destination admission must not partially complete when content or simulation initialisation fails. Instance reuse/return and transition-specific player-facing policy stay with the World Runtime and Persistence PDDs.

### 8.6 Unexpected disconnect and reconnect

```text
[CURRENT, observed]
PurrNet OnPlayerLeft
  -> account saved
  -> player NetworkIdentity despawned
  -> player session removed
Reconnect creates a subsequent login/world-entry path

[REQUIRED BY PERSISTENCE PDD — stages 1.7 / 1.8]
Network connection lost
  -> gameplay session + authoritative actor retained (>= 30 seconds)
  -> combat, damage, auras, cooldowns and death continue as required
  -> reconnecting account proves session/CharacterID ownership
  -> if actor still exists: rebind connection/NetworkIdentity authority
     to SAME authoritative actor, send current state, no duplicate
  -> if actor legitimately saved/despawned: reconstruct saved state
  -> unresolved combat cannot be escaped by waiting out grace
  -> server restart uses durable checkpoint and elapsed-time policy
```

**Invariant:** PurrNet `PlayerID` is not the persistent actor identity. The PDD explicitly requires a **minimum 30-second** unexpected-disconnect grace period and continuing combat beyond that minimum where necessary; no implementation value should weaken it.

## 9. Runtime resource ownership and cleanup

| Resource | Ownership contract | Acquisition / release triggers | Current baseline or deferred work |
| --- | --- | --- | --- |
| Server ECS `World` and systems | Server runtime composition owns creation, update attachment, orderly disposal and job completion. | Process/world initialise → shutdown/reinitialise; no duplicate registration or leaked player-loop attachment. | `ServerECSManager` creates/attaches world; complete shutdown and scheduler lifecycle Stage 1.1/1.3. |
| ECS actor components/buffers | Server actor-lifecycle authority owns entity creation/retirement in step with actor identity. | Spawn, materialisation, activity transition, teardown. | `ServerActorManager.OnActorSpawned` creates; corresponding complete despawn path not established. |
| Server actor state, session and persistent character | Authoritative game session/actor and persistence boundaries; network disconnection alone cannot erase state. | Login/spawn → safe logout/despawn/restart; reconnect may retain same actor. | `ServerConnectionManager` immediate player cleanup and `PlayerDatabase` JSON snapshot are provisional. |
| PurrNet `NetworkIdentity` and owner | Supported PurrNet lifecycle; project-owned integration tracks logical actor and audience. | Actor becomes network-visible → despawn/no longer represented; reclaim for reconnect where allowed. | Created by `ServerSpawnManager`; Stage 1.5/1.8 relevance and reconnection. |
| Spatial subscriptions/index membership | Server spatial authority and project PurrNet visibility adapter; no stale membership after movement/despawn/context transfer. | Spawn, accepted move, teleports, instance transition, actor retirement. | `ChunkSubscriptions` and `ServerInterestManager` track scene interest; shared index Stage 1.2. |
| Environment additive scenes | `ServerWorldManager` / PurrNet scene module own load/reference/scene membership; client `ClientWorldManager` tracks delivered scenes. | Subscriber/streaming relevance enters/leaves; async cancellation and teardown accounted for. | `EnsureChunkLoaded` / `UnloadChunk` exist; robust in-flight cleanup Stage 1.6. |
| Client floating-origin scene roots | Client position/world managers own local transform shifts, not absolute simulation state. | Origin change, newly loaded scene, scene release. | `ClientPositionManager` and `ClientWorldManager` implemented in prototype form. |
| Addressables loads, catalogs and asset handles | The initiating project-owned resource consumer owns each handle or a documented shared-ownership count; PurrNet-owned scene handles obey supported PurrNet APIs. | Acquire/load → final consumer gone/unload; no leak or premature release. | Actual handle lifecycle must be audited and instrumented in Stages 1.0 and 1.6, not assumed correct now. |
| Effects, UI, audio, pooled representations | Client presentation owner manages allocations, subscriptions, reuse/reset and destruction. | Visible/relevant use → completion/teardown/logout. | Resource budgets and pooling policy Stage 1.6. |
| Persistent timestamps and data writes | Authoritative server clock and persistence component; never client clock. | State mutation/checkpoint/logout/offline progression and release transitions. | Database and timed-state migration Stage 1.7–1.8. |

**Disposal rule:** Each lifetime must have one explicit owner and a verifiable release path; **GC or scene destruction are not substitutes for releasing persistent subscriptions, entity data or Addressables handles**. Shared resources require explicit ownership accounting. Errors fail observably rather than silently placing actor or scene state into an invalid partial lifecycle.

## 10. Stage hand-offs and expected integration surfaces

This table is a **responsibility specification**, not a new named C# API declaration.

| Stage | Intended integration responsibilities | Not presently claimed |
| --- | --- | --- |
| **1.0** | Define ownership/context contracts, stress profiles, measurable invariants, baseline instrumentation/evidence. | No gameplay optimisation or benchmark result is implied by this document. |
| **1.1** | **Issue #64 proposed, awaiting owner approval:** 30 Hz/10–60 range, maximum 4 catch-up ticks/iteration (1–8), phases 0–7, stable deadline/callback order, cancellation/reentrancy and monotonic versus UTC/inactive boundaries (§3). Implementation/tests remain #65–#78. | **Documentation only:** existing ECS still auto-updates via Unity player loop, frame deltas/managers remain; no fixed-clock driver, validated overload policy or approved headless budget yet. |
| **1.2** | World/map/instance-qualified double-precision spatial membership, query and visibility candidate interface; event-driven insert/move/remove/context transfer; independent world-content streaming interest. | `ChunkSubscriptions` is not a fully general spatial index; no range-qualified instance isolation yet. |
| **1.3** | Exact single-writer per-field ownership, actor ID ↔ ECS Entity mapping, batched managed/ECS bridge, job/main-thread boundaries, safe teardown. | GameObject/ECS mirror is not yet governed by this contract. |
| **1.4** | Relevance/activity states, scheduling of NPC perception and decisions, sleep/wake continuity, representation demand and activation spikes. | Dormant/near/engaged simulation tiers not currently established. |
| **1.5** | PurrNet-supported observer policy, prioritised update frequency, bandwidth/accounting and correct instance/range observer set. | Existing chunk visibility is not a network budget/priority implementation. |
| **1.6** | Content streaming and renderer LOD/culling contracts, resource/Addressables lifetime budgets, client pooling policies and origin-shift performance. | No guarantees of GPU/object throughput without measurement. |
| **1.7–1.8** | Transactional persistent actor/character identity, timestamps, session restoration and reconnect-safe actor lifetime. | Current JSON and immediate disconnect/despawn do not meet PDD continuity requirements. |
| **1.9–1.11** | Immutable content/version compatibility, operational metrics/CI and integrated stress sign-off on documented hardware. | No production capacity or performance guarantee is established here. |

Implementations should provide a direct project-owned integration against existing Managers/Endpoints where practical; do not create a generic message bus, new network transport or speculative multi-process sharding framework. If a future measurement demonstrates a different internal owner is necessary, document and validate the change **without altering PDD-owned player behaviour**.

## 11. Measurable invariants and benchmark evidence

Stage 1.0 issue [#42](https://github.com/FrontierDev/ninthage/issues/42) and subsequent benchmark issues must turn these invariants into measurable tests. They are **required outcomes**, not claimed current test results:

| Invariant | Evidence/metric |
| --- | --- |
| Single logical actor and one legal network owner per active character/session | Actor IDs versus CharacterIDs, session/connection counts, duplicate detection during spawn/reconnect. |
| Consistent authoritative absolute position | Error between committed double-position views; accepted/rejected movement events; negative-coordinate and boundary crossings. |
| Floating-origin and world-tile independence | Origin shift count, distinct tile load count, stable absolute position/instance and unchanged combat range during shifts. |
| Instance isolation | Cross-instance candidates, visibility recipients and forbidden target/damage events = **zero** under overlapping coordinates. |
| Bounded relevance queries | Eligible local candidate count, examined candidates and query time versus total server population across density sweeps. |
| Scheduled authoritative work correctness | Expiry/tick/command counts, update order and missed/deferred event counts, with backlog explicitly **unavailable until Stage 1.1**. |
| Dormancy without state exploits | Active/near/dormant population and wake-up state/timing; **unavailable until Stage 1.4**. |
| Actor/scene/handle teardown | Spawn/despawn balance, ECS entity counts, stale memberships, memory retention after unload/churn, Addressables handle release evidence where observable. |
| Legitimate reconnect continuity | Same authoritative actor/character and continuing combat/timers during required grace; current implementation is known to lack the PDD behaviour. |
| Network relevance and limits | Visibility decisions by world/map/instance/range, bytes/messages per relevant observer where supported, explicitly unavailable transport counters otherwise. |
| Streaming/renderer stability | Client p50/p95/p99 frame time, activation/unload stalls, draw/batch/GPU samples on specified hardware, memory/GC trends. |

**Measurement policy:** Every result must identify Git commit, Unity/PurrNet/package versions, build mode, graphics settings, hardware, workload definition/seed, achieved player/actor counts, sample count, test duration and whether each metric is measured, estimated, not applicable or unavailable. Instrumentation must not fabricate zeros or equate architecture-readiness with tested production capacity.

## 12. Open decisions, omissions and validation gate

The following **remain open on purpose**:

- **Stage 1.1 — proposed, not approved (#64):** §3 specifies candidate 30 Hz (valid 10–60), at most 4 catch-up ticks/iteration (valid 1–8), phases 0–7, queue/deadline ordering and the simulation/UTC/inactivity boundary. **Owner must approve** each choice before #65. No measured standalone server tick CPU budget, bounded ingress quota, sustained-overload observation/alert thresholds or numeric AI-decision interval is established by the five-second Editor baseline; these remain explicitly gated by further engineering evidence. Offline-resource/aura/cooldown policy remains with the appropriate PDD.
- **Stage 1.2:** Spatial-index strategy/cell size, range/hysteresis thresholds, concrete world/map/instance identifiers, membership event API, scene interest radius. These are not fixed by the World Runtime PDD.
- **Stage 1.3:** Canonical physical storage for accepted absolute position, per-actor ECS mapping implementation, managed/Jobs/Burst boundaries, entity representation under dormancy.
- **Stage 1.4:** AI decision frequencies and simplified simulation detail; AI PDD explicitly leaves numeric update frequencies unspecified.
- **Stage 1.5–1.6:** PurrNet-supported replication features, per-connection budgets, asset LOD thresholds, streaming concurrency and pooling strategy.
- **Stages 1.7–1.8:** Database decomposition/checkpoint cadence, session duplicate-login UX, exact cleanup sequencing and valid instance-reentry policy as assigned to the relevant PDDs.
- **Other product decisions:** Combat tuning, instance scope, NPC respawn policy, housing/dungeon UX and invalid-location recovery must follow their owning PDDs. Technical implementation cannot silently decide them.

### Approval gate for Issue #64

Review §3.11 and record the owner's explicit approvals/rejections in Issue [#64](https://github.com/FrontierDev/ninthage/issues/64). This documentation proposal alone does **not** satisfy the approval gate; #65–#78 remain blocked until the accepted parameters and timing policy are recorded. The Stage 1.0 Editor-only benchmarks remain provisional, and no simulation code or test change has been made under #64.

### Acceptance for Issue #41

- [ ] Architecture owner reviews and approves the present/proposed boundaries and identity matrix.
- [ ] Cross-check current-source references at the implementation head of `dev` (source may advance).
- [ ] Verify 100 m origin cells and 256 m world tiles remain separate; no scene is used as world/map/instance identity.
- [ ] Verify spawn, movement, scene unload, actor despawn, instance transfer and reconnect diagrams explicitly distinguish current from required behaviour.
- [ ] Resolve/document the missing Phase 0.40 baseline artifact before treating Phase 0 documentary acceptance as verified.
- [ ] Review this sole Markdown addition and ensure **no runtime code, tests, scene/prefab assets, Unity `.meta`, vendor files, configuration or PDDs changed**.
- [ ] Only after manual approval remove `needs-validation` and permit dependent Stage 1.0 implementation issues.

**No Unity compile, Play Mode, dedicated-server run, automated regression run or performance measurement is asserted by this documentation change.**
