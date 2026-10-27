# Ninth Age — Performance and Simulation Architecture

**Status:** Stages 1.1–1.3 implemented and owner accepted; Stage 1.4 activity-state contract approved on 25 October 2026.
**Scope:** Historical Phase 1.0 baseline, Stage 1.1 as-built simulation/time integration, and forward-looking Phase 1 technical contracts.
**Source baseline:** Original `f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f`; Stage 1.1 reviewed on `dev` through `4db4aed1e18c7d7fbd92421f160d4d982d22f53c` (#106 rev 1).
**Updated:** 2026-10-26
**Unity version:** 6000.3.10f1

## 1. Purpose, authority and interpretation

This document distinguishes historical source observations, **implemented Stage 1.1 authoritative scheduling and UTC boundaries**, and separately the intended requirements for later Phase 1 stages. Retained pre-Stage-1.1 proposals are historical unless explicitly described as implemented. No unsupported game population, headless server capacity or later gameplay feature is implied.

Authority follows [MMORPG Master PDD](MMORPG-Master-PDD.md): the Master PDD defines project-wide principles; subsystem PDDs own player-facing behaviour; this document owns technical integration decisions subordinate to those PDDs; the [Project Roadmap](Plans/Project-Roadmap.md) and [Phase 1 Implementation Plan](Plans/Phase-1-Implementation-Plan.md) own sequence and delivery. Follow [C# Architecture](CSharp-Architecture.md), [C# Style Conventions](CSharp-Style-Conventions.md), [GitHub Issue and Workflow Guide](GitHub-Issue-and-Workflow-Guide.md) and `AGENTS.md` for all implementation work. **Third-party source, namespaces, packages and vendor assets are read-only without specific user authorisation.** Project-owned PurrNet integrations may evolve; PurrNet itself may not be modified under this contract.

**Authoritative product references:**

- [World Runtime and Instancing PDD](pdd/World-Runtime-and-Instancing-PDD.md) — world/map/instance identity, double precision, one shared 256 m origin/environment grid with separate projection/streaming ownership, streamed scenes, actor interest, persistence across scene unloads.
- [Movement and Traversal PDD](pdd/Movement-and-Traversal-PDD.md) — responsive locally controlled movement, server validation and authoritative world position.
- [Account, Character and Persistence PDD](pdd/Account-Character-and-Persistence-PDD.md) — identity/session separation, location recovery, minimum disconnect grace, timed-state continuity and persistence.
- [AI and Encounter Behaviour PDD](pdd/AI-and-Encounter-Behaviour-PDD.md) — server authority, NPC activation and scalable decision frequency.
- [Combat System PDD](pdd/Combat-System-PDD.md) — cast, aura, cooldown, combat and spatial execution semantics.
- [Graphical Approach PDD](pdd/Graphical-Approach-PDD.md) — URP-facing rendering, LOD and art performance requirements.
- [Live Content and Versioning PDD](pdd/Live-Content-and-Versioning-PDD.md) — immutable compatible release/content state and controlled Addressables ownership.

**Approved hosting boundary:** one World Server process on one physical machine simulates one persistent world, including all its maps/instances. Every World Server connects to a separate Master Server for global identity, persistence/session authority, world registry/admission and later cross-world communication. The Master Server does not run Unity/ECS gameplay simulation. Multi-process world sharding and off-process instance simulation are prohibited; any later simulation-region design is strictly in-process.

**Terminology:** **Current** means verified in project-owned source at the reviewed revision, not confirmed by a running Unity build. **Required contract** means the technical invariant later stages must meet. **Deferred** means the named Stage 1 issue owns implementation. **Open** means the relevant PDD or implementation decision does not yet fix a value or method; a benchmark or engineering issue must not silently invent player-facing behaviour.

### Live server profiling ownership

The retired Editor/test performance harness is not an evidence source for current or future Stage 1.5 measurements. Current performance evidence comes from an ordinary dedicated server launched with `--profiling`, real connected clients and ordinary runtime gameplay, and is written as one `performance-report-*.json` beside Unity's active server log.

Ownership is deliberately split: production systems own metric definitions and `TryRecord` producers; `PerformanceTelemetry` owns fixed-capacity bounded aggregation; `ServerPerformanceProfiler` owns the opt-in server session lifecycle and report output; actual gameplay owns the workload. Client profiling, synthetic actors, fixture populations and workload orchestration remain outside this server contract.

Graceful shutdown is owned by `ServerShutdownController` and `ServerInitialization`: the simulation clock stops before the PurrNet network drain, the disconnect wait is bounded, ECS/world teardown completes, and profiling finalises before process exit. `Application.quitting` remains fallback protection only; it is not the primary controlled-shutdown boundary.

### Baseline qualification

The current [C# Architecture](CSharp-Architecture.md) is a descriptive snapshot at source revision `3232b1cfc1a889d1f8be7665714bf145e8329a4b`; this document additionally reviews the later `dev` revision above. The [Phase 0.40 issue](https://github.com/FrontierDev/ninthage/issues/40) is closed, but its prescribed `.docs/Phase-0-Stable-Foundation-Baseline.md` is **not present in the reviewed `dev` tree**. That discrepancy must be reconciled before treating the Phase 0 acceptance artifact as independently verified. It does **not** justify changing Phase 0 records within Issue #41.

## 2. Actual current-runtime boundaries

| Area | Current source and behaviour | Contract gap (not implemented by this document) |
| --- | --- | --- |
| Process selection | `Assets/Runtime/Core/GameBootstrapper.cs::Awake` selects `ServerInitialization.Begin` for batch mode/no graphics/forced mode, otherwise `ClientInitialization.Begin`. | Dedicated benchmark modes and unambiguous test-client selection are Stage 1.0 tooling work. |
| ECS clock | **Stage 1.1:** `ServerSimulationClock` advances the seven-system server ECS group manually with a fixed 0.05 s tick (20 Hz), deferred domain callbacks, four-tick catch-up and observable backlog metrics. | Full bounded PurrNet ingress admission, sustained-overload readiness operations and measured headless/networked capacity remain outside the implemented Stage 1.1 boundary. |
| GameObject actor and ECS state | `Actor` remains a PurrNet `NetworkBehaviour`; Stage 1.1 moves actor/ECS gameplay bridge and player-position validation into authoritative pre-/post-ECS tick boundaries. | Full per-field single-writer ownership, spatial indexing and job-safe bridges remain Stages 1.2–1.3. |
| Movement | Existing PurrNet owner-controlled movement remains responsive; server speed validation and approved movement projection occur at authoritative tick boundaries. | Further absolute-position and spatial-interest integration remain Stages 1.2–1.3. |
| World content | `ServerWorldManager.EnsureChunkLoaded` loads `World_X_Y` via PurrNet Addressable additive scenes; `UnloadChunk` unloads. `ServerInterestManager` uses a 3×3 neighbouring scene set. | Origin-cell, streamed world-tile and instance qualifications are not yet separate in all implementation paths (Stages 1.2 and 1.6). |
| Actor interest | `ChunkSubscriptions` indexes actors/NetworkIdentities by scene-name strings; `ChunkVisibilityRule.CanSee` checks subscribed actors; changed-chunk identity re-evaluation is targeted in `ServerInterestManager.OnInterestRequest`. | Does not implement general instance-aware double-precision distance relevance or spatial queries (Stages 1.2 and 1.5). |
| Scene and actor lifetime | `ServerSpawnManager` instantiates runtime actors into the persistent `global_actors` scene; `Actor.OnDestroy` emits `onServerActorDespawned`; `ServerSpawnManager` observes that event for spawn-point bookkeeping. | Complete actor ECS/index/replication cleanup and autonomous persistence lifetime must be defined/implemented (Stages 1.3–1.4 and 1.8). |
| Reconnect / persistence | `ServerConnectionManager.OnPlayerLeft` currently saves a JSON account snapshot and despawns the network identity, then removes connection/session maps. `PlayerDatabase` persists JSON account data. | This does not meet the Persistence PDD's 30-second minimum unexpected-disconnect actor continuity (Stages 1.7–1.8). |
| Client placement | `ClientPositionManager.SetOriginChunk` shifts loaded scene roots and `global_actors`; `ClientWorldManager` tracks delivered world scenes and uses `global_world`. | Precise separation from world tiles, automatic position-driven updates and measured resource lifetimes remain incomplete (Stages 1.2 and 1.6). |

These observations are **not** an instruction to change the named implementation now. They identify which existing pathways the later architecture must reconcile.

## 3. Authoritative time and simulation scheduling

**Stage 1.1 status: implemented, owner-confirmed validated.** The actual runtime baseline uses 20 Hz (0.05 s ticks), 4 maximum catch-up ticks/driver iteration, a single monotonic clock, manually ordered ECS simulation, deferred domain events and a separate server UTC authority. All critical regressions, including the now-committed #106 PlayMode UTC/cooldown isolation, are owner-reported validated. Earlier 30 Hz and seven-phase proposals below are *historical design targets*, not assertions about the exact implemented values/sequence. The bounded generic network-ingress quota and full operational overload readiness policy remain future work.

### 3.1 Historical pre-Stage-1.1 baseline, superseded

The initial baseline had a Unity player-loop-driven seven-system ECS world, a placeholder `ServerStartSimulation()`, render-frame authoritative time advancement and direct cast/aura/ticker domain callbacks during ECS-buffer traversal. Simulation backlog declarations were not evidence of an active clock. **These observations no longer describe `dev`.**

Stage 1.1 introduces `ServerSimulationClock.ExecuteTick()`, which executes the server's fixed-time `ServerECSManager.UpdateSimulation()` before registered callbacks; ECS commits pending domain mutations, validates/bridges actor movement, runs the ordered seven systems, commits spawn outcomes and dispatches buffered domain events. Existing `ResourceRegenSystem.OnUpdate()` remains intentionally inert. Unity/PurrNet rendering and transport still have their own non-authoritative frame-based work.

### 3.2 Stage 1.0 evidence and limits on choosing values

The Phase 1.0 baseline/budgets use five-second measured windows in Unity Editor Play Mode, principally the data-only 100-entity `P1-S0-04` Minimal/Activity fixtures and graphical `P1-S0-06` fixtures. The provisional Editor-only comparison budget is `server.ecs.actor_update_duration` median-run p99 ≤ 0.250 ms and `server.actor_manager.update_duration` median-run p99 ≤ 0.100 ms **for those fixtures only**. These figures are **per measured subsystem**, not the duration or safe throughput of a complete server simulation tick.

No qualified standalone/headless, networked combat, real-player, backlog-recovery or long-running tick-capacity measurement exists yet; the other Phase 1.0 profiles remain blocked/unmeasured as recorded in `.docs/Plans/Phase-1-Performance-Budgets.md`. Consequently, the following settings are **bounded initial engineering proposals**, not measured production recommendations, pass/fail tick CPU budgets or supported concurrency claims. Stage 1.1 tests must measure their consequences; an owner decision may change them without changing PDD-owned gameplay durations.

### 3.3 One clock owner, identity, lifecycle and configuration — implementation and historical proposal

**As implemented:** `GameConfiguration.ServerSimulationTickDuration = 0.05f` (20 Hz) and `ServerSimulationMaxCatchUpTicks = 4`. `ServerSimulationClock` validates settings, advances one server ECS system group, and offers `RegisterEachTick`, `RegisterLowerRate` and `RegisterDue`. The original 30 Hz starting point in item 2 below was not adopted; the numbered design text remains as historical rationale, not an exact as-built manifest.

1. **Ownership:** a single server-owned `ServerSimulationClock` (implementation location proposed in `Assets/Runtime/Server/Core`) drives both the ECS system group and registered server-domain work. `ServerECSManager` owns world/system composition and must cease automatic Unity player-loop simulation updates before manual scheduler-driven group updates begin; **never run both**. PurrNet remains the transport owner, not the simulation clock.
2. **Proposed initial frequency:** **30 authoritative ticks/second**, with `fixedDeltaSeconds = 1 / 30` (~33.333 ms) and an integer `long` tick ID. The first completed tick is ID 1; ID 0 is the initial state. All gameplay clock reads use the current tick and fixed delta; future deadlines are represented as integer due-tick values. Configure the frequency on the existing server configuration path, not in individual managers.
3. **Proposed configuration bounds:** **10–60 ticks/second**, inclusive, with invalid/missing/out-of-range settings rejected visibly at server initialization (no silent clamping or substitution). Bounds are engineering guardrails subject to benchmarking, **not** proof that all supported settings give equivalent timing quality. Do not change `GlobalCooldownDuration`, authored cast/aura durations, charge rules or movement tolerances to make the tick rate fit.
4. **Elapsed-time source:** use a monotonic process clock (such as `Stopwatch.GetTimestamp` with its frequency), independent of `Time.deltaTime`, frame count, client clocks and system UTC. Accumulate full monotonic elapsed time at each driver iteration; advance the authoritative tick once per consumed fixed interval. Derive tick time from integer tick ID rather than repeatedly adding rounded floating-point deltas. One ECS world-time update and one ordered scheduled-work pass must occur for each authoritative tick.
5. **Conversions:** convert authored seconds to due ticks **once at scheduling time** with documented ceiling-to-next-tick semantics for positive durations; zero-delay actions become eligible in the next valid execution phase of the current tick when registered before that phase, otherwise no earlier than the next tick. Do not shorten an authored delay. Preserve the actual configured tick interval for periodic effects, phase/stack rules and the one-second baseline GCD. Define repeating intervals from an absolute previous due tick (not repeated addition of drifting frame delta); emit each due occurrence once.
6. **Lifecycle:** initialise after configuration/authoritative-world creation and before simulation starts; run only once per live server runtime; stop accepting new tick work during shutdown; drain or cancel outstanding work according to its owner, complete ECS jobs, detach the old ECS player-loop update, unsubscribe manager/domain registrations and dispose/reset runtime scheduling state. New process/runtime instance starts with tick 0. A local simulated tick is **not** a persistent global ID; durable temporal records use UTC as defined in §3.9. A shutdown/reinitialisation must not execute the old scheduler, callbacks or world a second time.
7. **Responsibilities:** project-owned managers must register scheduled operations with this owner rather than create competing `Update` clocks. Client UI/prediction, Unity rendering and PurrNet transport may still update per frame; those frames must not commit authoritative gameplay time. Actors without game-state ownership must not schedule their own authoritative clock.

**Historical 30 Hz rationale (superseded by the 20 Hz configured runtime):** at the prototype stage this gives ~33 ms authoritative cadence for movement/validation while retaining 30 integer steps per existing 1.0 s GCD. It makes less frequent decision schedules possible as integer tick multiples. The five-second Editor-only baseline does **not** prove a 30 Hz headless-server budget. **Implemented choice:** the owner has accepted the current 20 Hz engineering baseline; positive authored durations use non-shortening tick conversion. Any future frequency change requires its own qualification.

### 3.4 Fixed per-tick phase order — implemented boundaries versus historical target

**Implemented order:** pending mutation commit → pre-ECS actor movement validation/bridge → fixed seven-system ECS simulation → post-ECS actor synchronisation → spawn-command commit → buffered/deferred domain callbacks → scheduler callbacks. The table below is the **original conceptual target**, not an exact implementation trace: notably generic bounded PurrNet ingress/admission, its phase-5-before-phase-6 conceptual ordering and a separate phase-7 snapshot publisher are not implemented verbatim. These differences do not invalidate the actual deferred ECS callback safety boundary.

**Historical target phase table:**

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

### 3.5 Unified scheduling and equal-deadline semantics — implemented scheduling core

**Current APIs:** the server clock provides *three* callback registration forms (each tick, lower rate, due tick) and cancellable handles. Urgent gameplay/domain events use existing callbacks and deferred ECS event buffering, not a new fourth generic scheduler API. Clock callbacks have stable registration ordering; buffered ECS events are sorted by tick/phase/system/actor/entity/operation/sequence, rather than only the abstract key below.

The original design distinguishes **four conceptual work classes**, not four independently advancing clocks:

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


**Historical target key:** `(dueTick, phasePriority, actorIdentity, registrationSequence)`; the actual clock uses registered callback order and buffered ECS events use their own explicit comparator. Do not treat this abstract key as the literal comparator of both implementations. Ordering *within* an actor's authored effect chain retains its existing explicit effect order. Global registration sequence is monotonic for the scheduler lifetime, and handles include a generation or otherwise prevent a recycled registration identifier from inheriting stale events. Where an actor has no ID, a stable registered operation identity is required; memory addresses/hash-map enumeration are not tie-breakers.

**Cancellation and reentrancy:** cancellation or actor despawn invalidates its pending registrations before subsequent dispatch. Each callback is marked claimed before invocation, exactly once; a duplicate completion, dispel, expiry or auto-attack command must not retrigger it. A callback that schedules another callback for a phase already being drained (including a zero-delay one) executes **no earlier than the next tick**, avoiding unbounded same-tick recursion. A callback scheduled before its target phase can run in that phase during the current tick where the authored timing permits. Additions/removals during dispatch are staged and committed after traversal. Exceptions are surfaced with tick/actor/registration context and must not silently discard unrelated due events.

**Ownership and cleanup:** actor-specific registrations follow the authoritative actor lifetime, not the Unity scene lifetime. Cancel on actual actor retirement and runtime teardown, **not** simply when an environment tile unloads or a PurrNet connection drops. A still-authoritative disconnected character continues due combat/timed events as required by the Persistence PDD.

### 3.6 Bounded catch-up — implemented debt; later operational overload response

- **Current configured cap:** 4 ticks per Unity driver iteration, at 20 Hz: up to **200 ms of owed simulation time** executed per iteration (not a 200 ms CPU budget). `ServerSimulationClock` retains backlog and reports scheduled/executed counts, per-tick time, overdue callbacks and cap hits; the original 30 Hz/~133 ms illustration is superseded.
- Always add the *full* elapsed monotonic duration to accumulator/debt; do **not** discard accumulated time, change `fixedDeltaSeconds`, compress multiple gameplay ticks into one, or jump tick IDs to make the backlog look healthy. Consume at most the cap per driver iteration and leave the remainder pending. If backlog remains when a frame ends, resume from the oldest unprocessed tick on the next iteration; during spare capacity, drain debt at the permitted cap until caught up.
- Record at least: `currentTickID`, executed ticks/iteration, elapsed tick execution time, pending whole ticks, pending simulation seconds, age of oldest due event, ingress/deferred queue depths, deferred/due/processed/cancelled counts, missed wall-clock deadlines and consecutive overloaded iterations. A deliberately delayed event remains a **late event**, not a completed-on-time or dropped event.
- Classify **normal** (no whole-tick backlog), **catching up** (positive backlog but decreasing), **overloaded** (backlog persists or increases across successive driver iterations), and **sustained overload** (bounded rolling observation confirms continued growth). Classification windows and alert thresholds are operational configuration to be validated with real load tests; do not invent a production acceptance threshold from a five-second Editor run.
- During overload, the server must preserve authoritative simulation/event order and expiry semantics. It may reduce *non-authoritative* work, such as redundant presentation snapshots or optional diagnostics, while maintaining required PurrNet control and correction traffic. It must not skip ticks, teleport timers to current wall time, discard combat/AI obligations, give players an advantageous cooldown, or auto-despawn actors to conceal overload.
- Under sustained overload, mark health/readiness visibly degraded, publish rate-limited diagnostics and require operator/load-test investigation or an explicitly approved capacity/admission policy; do not silently introduce disconnects, gameplay-rate slowing, or automatic shard migration. If incoming commands exceed the bounded admission quota, retain ordered backlog and expose its age/size, with **no silent loss**.
- **Deferred operations:** bounded general PurrNet input-admission quotas, sustained-overload health/readiness alerts and operator thresholds require separately qualified headless/networked tests and later implementation. The Stage 1.1 scheduler exposes debt but does not claim a complete operational response or production player-capacity budget.

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

Same-tick and equal-deadline behaviour is ordered within one server; cross-platform bitwise physics determinism is not promised. Focused Stage 1.1 regression tests (#66, #68, #70, #72, #74, #76, #78) cover fixed ECS time/order, cancellation/reentrancy, cast/cooldown/aura/ticker/delayed actions, movement, spawn and NPC/auto-attack eligibility.

**#106 (rev 1, `4db4aed1`):** `Assets/Tests/PlayMode/Server/ServerTimeSeparationTests.cs` implements `UtcObservationDiscontinuitiesDoNotAdvanceClockOrCooldown()`: a real clock and ECS cooldown do not progress during injected server UTC jumps, but do progress by one real tick. **#107:** `Assets/Tests/EditMode/Server/ServerTimeAuthorityTests.cs` implements `ReconciliationValidatesElapsedTimeAndRejectsInvalidCredit()`: permitted/frozen offline-time rules and invalid future/source/over-credit rejection. The **owner reports all Stage 1.1 validation completed**; this documentation edit does not claim an independently reproduced XML test report or headless benchmark.

### 3.11 Stage 1.1 closure and owner validation — completed

The Issue #64 pre-implementation design approval is superseded by the committed Stage 1.1 implementation and **owner-confirmed** completion:

- [x] Single 20 Hz monotonic server clock; integer tick identity, non-shortening time conversion, controlled startup/shutdown and four-tick catch-up cap.
- [x] Fixed ECS update and safe deferred cross-domain event/structural boundaries; movement validation, NPC bridge, spawn outcomes, casts, cooldowns, auras and tickers use authoritative time. Unimplemented resource regeneration remains inert.
- [x] Shared each-tick, lower-rate and due-tick scheduling; approved 200 ms NPC perception and every-tick auto-attack eligibility.
- [x] Separate server UTC observations and pure caller-rule reconciliation; no UTC-driven live cooldown progression.
- [x] Required critical tests, including #106/#107; owner attests that Unity/runtime/manual validation has been completed; [#78](https://github.com/FrontierDev/ninthage/issues/78) is closed.
- [x] Qualification boundary retained: short Editor-only baselines do not establish headless/networked production capacity.

**Stage 1.1 exit:** owner-confirmed complete. General PurrNet ingress quotas, sustained-overload operational alerts, activity tiers, disconnect grace and actual persistence/reconnect integration remain separate later-stage deliverables.

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

**Position ownership decision for the Stage 1.2 handoff:** Each live actor has exactly one stored, writable authoritative absolute double-precision position. The existing server ECS `ActorComponent.WorldPositionD` is the canonical position store for a live ECS actor. `Actor.WorldPos` is not a second live position authority: its redundant writable storage is removed, and the spawn position is handed directly into ECS actor creation. The source audit found only the current server spawn write and ECS-registration read; `Actor` is Shared code and cannot read `Game.Server`'s ECS component without reversing the assembly boundary. No Shared-side caller currently requires a live `Actor.WorldPos` read; a future proven Shared consumer may receive a read-only server projection, never another setter or mutable mirror. The broader Stage 1.3 decisions for actor-ID-to-Entity ownership, batching, Jobs/Burst boundaries and dormancy remain open.

## 5. Position ownership and synchronisation

### 5.1 Required state ownership matrix

| State | Current writer / reader | Intended writer of authority and synchronisation direction | Lifetime and invariant |
| --- | --- | --- | --- |
| `Actor.WorldPos` (`WorldPosition` doubles) | `ServerSpawnManager.Update` currently initialises it; `ServerActorManager.OnActorSpawned` currently reads it. No other runtime caller currently reads or updates it. | Remove the redundant live storage. Spawn construction hands its already-resolved `WorldPosition` directly to the canonical ECS component. If a future Shared consumer proves necessary, expose only a read-only projection from the server-owned authority. | No independent lifetime or writer. It must never be updated from a client/local-origin transform or maintained as a second mutable copy. |
| `ActorComponent.WorldPositionD` (`double3`) | Spawn currently initialises it from `Actor.WorldPos`; `ServerActorManager.SyncPlayerECS` and `SynchronizeNPCPositionToECS` currently overwrite it from float transforms; `ActorUpdateSystem.OnUpdate` writes ECS-owned NPC motion. | Canonical writable position for every live ECS actor. One authoritative tick commit writes the accepted player or NavMesh result; the ECS simulation writes ECS-owned NPC motion in that same authority. All index queries and projections read this value. | Process-local entity lifetime; removed/recreated with the entity. Not a durable persistence identifier and invariant under client origin shifts. |
| Server `Actor.transform.position` (`Vector3`) | PurrNet owner-authoritative movement updates player transform; enabled NPC `NavMeshAgent` drives its transform; ECS gravity writes other NPC transforms in `ServerActorManager.LateUpdate`. | Presentation/physics/NavMesh or received movement **candidate** in server-local coordinates; server validation/reconstruction precedes committing authoritative world state. After accepted movement, project canonical position back to Unity space where needed. | Exists only while GameObject is instantiated; float transform must not be treated as globally authoritative, particularly across wide-world positions. |
| PurrNet owner movement / project-owned position RPC | The installed owner-auth `NetworkTransform` currently sends world-space compressed `Vector3` data to the server transform. | Replace only the gameplay position stream with one project-owned PurrNet RPC carrying `WorldPosition` and an ownership-epoch proposal sequence. `NetworkTransform` retains rotation/other suitable properties and its position sync is disabled only during the coordinated observer cutover. | One owner epoch and proposal sequence per actor. Server-authored accepted positions are the only observer gameplay position stream; interpolation is presentation-only. |
| Client `Transform.position` / floating origin | `ClientPositionManager.SetOriginChunk`, `ShiftSceneRoots` and `NetworkToLocal` manage shifted local positions. | Client-local projection from absolute authoritative coordinate and that client's origin. Outgoing movement must carry enough origin/cell context for absolute reconstruction; client shifts never modify server absolute position. | Scene/presentation lifetime; a local origin shift is not gameplay motion or a teleport. |

The **authority** in the table is the *server-accepted absolute spatial state*. The current server transform-to-`double3` bridge does not satisfy that contract: it loses origin/cell context after a client rebase and must not be retained as the position handoff. Stage 1.2 consumes the canonical ECS value only after the source-specific commit contract below is implemented.

### 5.2 Direction by actor/path

| Path | Intended information flow | Validation and ownership constraint |
| --- | --- | --- |
| Player input | Client local simulation → owner movement proposal with spatial context → server legal-movement check → committed absolute position → ECS/spatial index/replication projections. | Client authoritative over responsiveness, **not** server gameplay truth; speed, mode, spatial context, status and world/instance changes validated server-side. |
| NavMesh-driven NPC | Server navigation result → authorised absolute-position commit → ECS view, spatial index and replicated transform. | Unity NavMesh/Physics is a motion provider, not a separate gameplay-distance authority. |
| ECS-driven NPC | Scheduled server ECS motion → absolute-position commit → GameObject/replication where materialised. | No second `LateUpdate` writer may overwrite newer authoritative ECS results. |
| Teleport / forced movement / world transfer | Authoritative action validates destination and context → atomic logical-context and position transition → update spatial memberships/visibility and relevant scene/actor representations. | No intermediate externally visible state in an invalid or old instance; PDD owns valid fallback behaviour. |
| Client floating-origin correction | Absolute position + new local origin → derived client scene/actor `Vector3` shifts only. | No effect on authoritative distance, ownership, timers, simulation or membership. |

### 5.3 Stage 1.2 accepted-position and server-local simulation contract

Stage 1.2 uses one mutable stored server-accepted absolute position for each live ECS actor: `ActorComponent.WorldPositionD`. Spatial indexing, replication, physics/NavMesh projection and environment interest consume that state; none owns a second accepted-position store.

#### Player movement transport

Player movement uses one project-owned PurrNet position proposal path rather than treating `NetworkTransform` world-space `Vector3` as global authority:

```text
CharacterController.Move
    -> ClientPositionManager.LocalToChunkRelative(current local position)
    -> PackWorldPosition + ownership-epoch proposal sequence
    -> server authenticates sender/actor binding
    -> server resolves ActorSpatialContext
    -> authoritative tick validates ordered proposal
    -> ActorComponent.WorldPositionD committed once
```

`MovementEndpoint.Server_RequestChunkInterest` remains interest bookkeeping. A stale, reordered, wrong-owner, invalid-context or rejected proposal never changes the accepted ECS position. A client floating-origin shift that represents the same absolute position has zero gameplay displacement.

After the observer path is functional, server-authored accepted `WorldPosition` updates replace `NetworkTransform` position synchronization. Rotation/other suitable transform properties may remain on `NetworkTransform`. Client interpolation is presentation-only and never writes back to server authority.

#### Continuous server physical space, not streamed-tile regions

A 256 m environment tile is an Addressables/content-streaming unit and an aligned client origin/network cell. It is **not** a server physics partition.

For the current implementation phase, one active public map/runtime instance uses **one continuous Unity physics and NavMesh space** across its streamed detailed environment. Loading or unloading a 256 m tile adds or removes environment representation in that same physical space. It must not move existing actors, change their server physics frame, warp a `NavMeshAgent`, clear gameplay targets or trigger a navigation handoff.

The server ownership split is fixed:

- `ServerWorldManager` owns context-qualified environment scene loading/unloading, PurrNet `SceneID` correlation and terminal async scene lifecycle.
- `ServerPositionManager` owns general coordinate/transport conversion independent of physical simulation, including `WorldPosition` ↔ chunk/local network encoding.
- `ServerPhysicsManager` owns reusable physical coordinate checks/conversion, environment collider/NavMesh binding/lifetime and server physics-query execution/conversion.
- `ServerActorManager` and ECS own actor lifecycle and canonical `ActorComponent.WorldPositionD`; physical helpers never become a second accepted-position authority.

`ServerPositionManager` must not own collider/NavMesh lifetime merely because both systems use coordinates. `ServerWorldManager` must not retain physical-placement registries merely because it owns streamed scenes. `ServerPhysicsManager` must not own an actor-region registry or cache a second mutable accepted actor position.

For the current tested world scale, a server Unity transform is a finite-float physical projection of canonical absolute state in this continuous physical space. The existing client floating-origin system remains the precision mechanism for graphical clients; the server does not mirror each client origin change and does not introduce a server floating-origin system in Stage 1.2. This current projection is not a claim that one process can represent an arbitrarily large future world in floats forever.

Environment scenes retain their authored world-space adjacency when streamed. Streamed NavMesh content must form one continuous navigation domain wherever authored terrain is traversable. Whether Unity stores that contribution as one `NavMeshData` or several adjacent data objects is an implementation detail; a streamed-tile boundary must not be visible to gameplay navigation.

A future **simulation region**, if justified by precision or workload evidence, is an **in-process World Server** partition only. It is not derived from floor-dividing 256 m tile coordinates, has no current small fixed capacity and is not part of ordinary traversal. Such regions may organise work within one World Server but can never move world simulation, navigation or authoritative actors to another server process/machine. Ordinary tile crossing remains continuous and never creates an inter-server handoff or a `NavMeshAgent.Warp` seam search.

The fixed 1×1/2×2/4×4/8×8/16×16 region-sizing experiment from Issue #147 is **historical evidence, not an active architecture requirement**. Its NavMesh build-cost, isolation and precision measurements remain useful for future comparison, but the temporary 4×4 span and eight-active-region engineering result must not be treated as current production configuration or capacity.

#### NPC movement and physics-query ownership

- **NavMesh-driven NPC:** Unity navigation provides motion in the continuous server physical/NavMesh space. The pre-ECS bridge converts that result through `ServerPhysicsManager` and commits one absolute double-precision position to `WorldPositionD`.
- **ECS-driven NPC:** ECS updates `WorldPositionD`. Physics queries and materialised GameObject projection route through `ServerPhysicsManager`, which converts between canonical absolute state and the current continuous physical representation.
- **GameObject transforms:** are physical/presentation materialisations. They never become a second gameplay-distance or world-position authority.
- **Physical membership:** there is no per-actor fixed simulation-region membership in the current phase. Logical `ActorSpatialContext` and canonical `WorldPositionD` remain independent of streamed tile boundaries and any future **in-process** simulation-region ownership.

Crossing a streamed 256 m environment/origin boundary changes neither server physics frame nor logical context. It may independently change client floating origin and environment streaming interest, but ordinary NPC pursuit continues through normal NavMesh movement without calculated seam search, Warp or target-state mutation.

#### Authoritative tick ordering

The intended movement/spatial sequence is:

```text
ingress proposal / server movement source
    -> source-specific validation or local-to-absolute reconstruction
    -> ActorComponent.WorldPositionD commit
    -> ECS consumers / physics projection as appropriate
    -> ServerSpatialIndex movement publication
    -> observer relevance and replication
    -> environment interest updates
```

A rejected position is never published to the spatial index or observer path. Same-cell movement may retain index bucket membership while exact-range queries resolve the current canonical ECS position.

#### Ownership boundaries

| Concern | Owner | Must not own |
| --- | --- | --- |
| Logical world/map/instance identity | `ActorSpatialContext` and server runtime/world authority | Scene names, transforms, origin cells or spatial cells |
| Accepted actor position | `ActorComponent.WorldPositionD` | GameObject transform, spatial index or physical helper state |
| Spatial broad phase | `ServerSpatialIndex` | A second precise accepted-position store |
| Environment tile streaming | `ServerWorldManager` / supported PurrNet scene lifecycle | Physical/NavMesh ownership or actor physics state |
| Continuous server physical/NavMesh representation | `ServerPhysicsManager` | Logical context, gameplay teleport policy, accepted actor position or environment scene loading |
| Future in-process simulation-region ownership | Deferred internal World Server design selected from precision/workload evidence | Cross-machine world sharding, fixed tile-floor partitioning, ordinary traversal seams or current gameplay target state |
| Observer position output | Project-owned server replication path | Server gameplay position mutation |
| Position/network conversion | `ServerPositionManager` | Collider, NavMesh or physics-query lifetime |
| Client floating origin | Client position/world presentation | Server absolute position or logical context |

## 6. Spatial coordinate and runtime-context model

**These concepts are not interchangeable:**

| Concept | Owner and meaning | Current value / future requirement |
| --- | --- | --- |
| Absolute world position | Server-authoritative `WorldPosition`/double-precision coordinates within an identified logical map/world context. | Already represented by `WorldPosition` and ECS `double3`; coordinate-use correctness incomplete. |
| **Origin/network cell** | Position encoding and client floating-origin projection. | World Runtime PDD: **256 × 256 m** horizontally; `WorldPosition.Chunk` uses the shared `GameConfigurationManager.Config.ChunkSize` authority. |
| **World tile** | Streamed environment/terrain authored region; separate lifecycle/ownership from origin projection despite sharing its grid. | World Runtime PDD: **256 × 256 m** terrain tiles using the same `GameConfiguration.ChunkSize` grid size. |
| **Spatial-index cell** | Internal broad-phase search bucket for combat, perception and network relevance. | **64 m** Stage 1.2 broad-phase cell width, intentionally independent of the shared 256 m origin/environment grid. |
| Authored scene asset | Reusable `.unity` scene/content from Addressables. | Existing `World_X_Y` and `global_actors` names are asset/runtime conventions, not instance identities. |
| Loaded additive scene / `SceneID` | Specific Unity/PurrNet loaded scene, delivering content and/or holding representations. | Currently used by `ServerWorldManager` and scene subscriptions; may be reused across logical contexts only with correct separate identity. |
| **World** | Explicit persistent player-selectable world (`World 32` in PDD example). | World Runtime PDD defines concept; not equivalent to a process or scene. |
| **Map** | Independent spatial coordinate space within world/runtime context. | Map ID must not be inferred solely from numerical coordinates. |
| **Runtime instance** | Isolated gameplay simulation context sharing authored assets if appropriate. | Instance ID and isolation not completed; not inferred from `SceneID` or scene asset name. |

**Required query/context rule:** Every spatially relevant actor/query/replication operation first checks compatible **world + map + runtime instance** according to the World Runtime PDD, then local spatial candidates, then an exact authoritative double-precision range/line-of-sight check where required. Identical coordinates in two private instances never imply visibility, targeting, aggro or combat interaction. A player may share authored scene assets with players in other contexts without sharing simulation state.

**Spatial indexing authority (Stage 1.2):** A server-owned spatial membership service maintains actor current region and context; publishes insertion, accepted movement, removal, teleport/transfer and context teardown to consumers. AI, combat, interaction and replication must use the same qualified spatial source rather than independent global scans or a spawn-scene string. World/environment tile subscriptions remain a separate delivery mechanism. The existing `ServerInterestManager` changed-chunk targeted reevaluation is a useful retained starting point; do not falsely characterise it as an all-actor scan every interest update.

**Open implementation values:** Environment scene radii and later tuning remain separate from the shared grid. Stage 1.2 already fixes the initial 64 m spatial-index cell and 80/96 m visibility hysteresis defaults; these actor-search/relevance values remain independent of the shared 256 m origin/environment grid.

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
  -> Actor gets initial WorldPos, ID, Owner, CurrentChunk and context
  -> ChunkSubscriptions registers NetworkIdentity / player association
  -> onServerActorSpawned
  -> ServerActorManager.OnActorSpawned creates ECS ActorComponent from Actor.WorldPos + buffers

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
Client CharacterController.Move
  -> one project-owned PurrNet WorldPosition proposal + ownership-epoch sequence
  -> server validates sender, ordering, trusted context and legality
  -> one accepted absolute double-precision ECS position commit
  -> one ServerSpatialIndex.Move publication (membership may remain in the same cell)
  -> one server-authored observer position update through the project-owned observer path
  -> exact queries and observers resolve the same ECS position
  -> world-content interest update (only if tile relevance changed)
  -> actor replication interest update (independent of scene interest)
  -> visible client representations / correction where needed
```

**Invariant:** Local floating-origin changes and unrelated streamed-tile boundaries cannot make an actor move, change logical instance or bypass range validation. A rejected or stale proposal cannot overwrite a later accepted commit.

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
| **1.1** | **Implemented and owner-confirmed validated:** 20 Hz single fixed clock, four-step catch-up/debt metrics, seven-system ECS, buffered domain delivery, authoritative movement/spawn/combat/NPC scheduling and UTC/reconciliation boundary; critical tests through #107. | Generic bounded PurrNet ingress quotas, sustained-overload operational action policy, headless capacity qualification, activity tiers, disconnect grace and durable gameplay-state reconciliation remain later work. |
| **1.2** | World/map/instance-qualified double-precision spatial membership, query and visibility candidate interface; event-driven insert/move/remove/context transfer; independent world-content streaming interest; the accepted-position handoff and continuous server physical-space ownership defined in §5.3. | The fixed-grid region implementation still requires cleanup; range-qualified visibility, observer publication, spatial-consumer migration and accepted-position-driven environment interest remain incomplete. |
| **1.3** | **Implemented and owner accepted:** exact single-writer per-field ownership, actor ID ↔ ECS Entity mapping, batched managed/ECS bridge, measured managed/job boundaries and safe teardown. `ActorComponent.WorldPositionD` remains the accepted live-position owner. Stage 1.3 timed-state scaling did not justify Jobs/Burst conversion. | Dormancy/activity scheduling remains Stage 1.4 work. |
| **1.4** | **Approved contract:** three NPC activity states — Full, Nearby and Dormant. Combat/gameplay obligations and player relevance select activity; dead player actors count for relevance. Dormant NPCs are out of combat, not visible to any player and outside the Nearby band, and suspend ordinary patrol/perception/decision/NavMesh work while retaining authoritative identity/context/position. | Exact Nearby outer radius and reduced decision cadence remain engineering configuration/performance-tuning values for the Stage 1.4 implementation and benchmark. Lightweight actor dematerialisation is not required by the initial implementation. |
| **1.5** | PurrNet-supported observer policy, prioritised update frequency, bandwidth/accounting and correct instance/range observer set. | Existing chunk visibility is not a network budget/priority implementation. |
| **1.6** | Content streaming and renderer LOD/culling contracts, resource/Addressables lifetime budgets, client pooling policies and origin-shift performance. | No guarantees of GPU/object throughput without measurement. |
| **1.7–1.8** | Master Server-owned central account/character persistence and global session/admission authority, transactional actor/character state, timestamps, restoration and reconnect-safe lifetime; initial single-world server registration. | Current JSON and immediate disconnect/despawn do not meet PDD continuity requirements; concurrent multi-world deployment and transfer are Phase 3. |
| **1.9–1.11** | Immutable content/version compatibility, operational metrics/CI and integrated stress sign-off on documented hardware. | No production capacity or performance guarantee is established here. |

Implementations should provide a direct project-owned integration against existing Managers/Endpoints where practical; do not create a generic message bus, unapproved gameplay network transport or any multi-process world-sharding framework. The specific Master Server connectivity/persistence/session boundary is required in Stages 1.7–1.8 and later extended for Phase 3 multi-world hosting, but it must remain separate from world gameplay simulation. If a future measurement demonstrates a different internal owner is necessary, document and validate the change **without altering PDD-owned player behaviour**.

## 11. Measurable invariants and benchmark evidence

Stage 1.0 issue [#42](https://github.com/FrontierDev/ninthage/issues/42) and subsequent benchmark issues must turn these invariants into measurable tests. They are **required outcomes**, not claimed current test results:

| Invariant | Evidence/metric |
| --- | --- |
| Single logical actor and one legal network owner per active character/session | Actor IDs versus CharacterIDs, session/connection counts, duplicate detection during spawn/reconnect. |
| Consistent authoritative absolute position | Error between committed double-position views; accepted/rejected movement events; negative-coordinate and boundary crossings. |
| Floating-origin and world-tile independence | Origin shift count, distinct tile load count, stable absolute position/instance and unchanged combat range during shifts. |
| Instance isolation | Cross-instance candidates, visibility recipients and forbidden target/damage events = **zero** under overlapping coordinates. |
| Bounded relevance queries | Eligible local candidate count, examined candidates and query time versus total server population across density sweeps. |
| Scheduled authoritative work correctness | Stage 1.1 records simulation tick duration, scheduled/executed counts, retained backlog and deadline lateness; critical tests check tick/event identity and lifetime. Qualified headless/networked capacity remains unmeasured. |
| Dormancy without state exploits | Full/nearby/dormant population, transition counts and wake-up cost. Out-of-combat NPC reset clears transient combat state before dormancy; dead players remain relevance sources while their actors remain in context. **Instrumentation remains unavailable until Stage 1.4 implementation.** |
| Actor/scene/handle teardown | Spawn/despawn balance, ECS entity counts, stale memberships, memory retention after unload/churn, Addressables handle release evidence where observable. |
| Legitimate reconnect continuity | Same authoritative actor/character and continuing combat/timers during required grace; current implementation is known to lack the PDD behaviour. |
| Network relevance and limits | Visibility decisions by world/map/instance/range, bytes/messages per relevant observer where supported, explicitly unavailable transport counters otherwise. |
| Streaming/renderer stability | Client p50/p95/p99 frame time, activation/unload stalls, draw/batch/GPU samples on specified hardware, memory/GC trends. |

**Measurement policy:** Every result must identify Git commit, Unity/PurrNet/package versions, build mode, graphics settings, hardware, workload definition/seed, achieved player/actor counts, sample count, test duration and whether each metric is measured, estimated, not applicable or unavailable. Instrumentation must not fabricate zeros or equate architecture-readiness with tested production capacity.

## 12. Open decisions, omissions and validation gate

The following **remain open on purpose**:

- **Stage 1.1 — implemented, owner-confirmed validated:** §3 records 20 Hz fixed ticks, a four-tick catch-up cap, deferred domain delivery, 200 ms NPC perception, every-tick auto-attacks and UTC/reconciliation primitives. Bounded general PurrNet input admission and full sustained-overload operational response are **future** integration tasks; qualified headless capacity remains unmeasured. Real offline resource/aura/cooldown restoration belongs to Stages 1.7–1.8.
- **Stage 1.2:** The logical context, canonical absolute position, project-owned player transport, shared 256 m client-origin/environment grid, spatial index and visibility defaults are established foundations. The current fixed-grid simulation-region implementation is superseded and must be collapsed into the continuous server physical/NavMesh model in §5.3; `ServerPhysicsManager` remains the physical-query/NavMesh-binding owner without per-actor region authority. After that cleanup is owner-validated, remaining work continues with movement publication, observer visibility, spatial-consumer migration, logical-context transfer and accepted-position-driven environment interest. The old region-sizing campaign remains historical evidence rather than a capacity/design requirement.
- **Stage 1.3 — complete:** per-actor ECS mapping, managed/ECS boundaries, deadline-authoritative timed state and the Jobs/Burst decision gate are implemented and owner accepted. The measured 100–5,000 timed-state campaign did not justify Jobs/Burst conversion for the current timed systems.
- **Stage 1.4 — activity contract approved:** NPCs use **Full**, **Nearby** and **Dormant** simulation activity. Combat/gameplay obligations override distance. Visible NPCs are Full; out-of-combat non-visible NPCs may be Nearby or Dormant according to authoritative player relevance. Dead player actors count identically to living player actors for relevance until their actor leaves the logical context/lifetime. Leaving combat performs the normal transient combat reset first, then activity is reevaluated; combat state is not suspended for dormant catch-up. Dormant NPCs perform no ordinary patrol/perception/decision/NavMesh work. The initial implementation retains authoritative actor identity/context/position and does not require GameObject/PurrNet dematerialisation. The exact Nearby radius and reduced Nearby decision cadence are configurable engineering values to be chosen and validated by Stage 1.4 implementation/performance work.
- **Stage 1.5–1.6:** PurrNet-supported replication features, per-connection budgets, asset LOD thresholds, streaming concurrency and pooling strategy.
- **Stages 1.7–1.8:** Database decomposition/checkpoint cadence, session duplicate-login UX, exact cleanup sequencing and valid instance-reentry policy as assigned to the relevant PDDs.
- **Other product decisions:** Combat tuning, instance scope, NPC respawn policy, housing/dungeon UX and invalid-location recovery must follow their owning PDDs. Technical implementation cannot silently decide them.

### Stage 1.1 acceptance and Stage 1.2 handoff

Issue [#64](https://github.com/FrontierDev/ninthage/issues/64) and Stage 1.1 exit tracker [#78](https://github.com/FrontierDev/ninthage/issues/78) are closed; the owner confirms the implementation and validation gates are completed. Begin Stage 1.2 as a separate spatial-index contract. Unimplemented generic ingress quotas, operational sustained-overload interventions and later persistence/reconnect requirements are explicitly **not** claimed as Stage 1.1 functionality. Prior Phase 1.0 Editor-only performance baselines remain qualified to their existing workloads, not generalized server capacity.

### Acceptance for Issue #41

- [ ] Architecture owner reviews and approves the present/proposed boundaries and identity matrix.
- [ ] Cross-check current-source references at the implementation head of `dev` (source may advance).
- [ ] Verify origin/network cells and world tiles use the same 256 m `GameConfiguration.ChunkSize` grid while retaining separate projection/streaming ownership; no scene is used as world/map/instance identity.
- [ ] Verify spawn, movement, scene unload, actor despawn, instance transfer and reconnect diagrams explicitly distinguish current from required behaviour.
- [ ] Resolve/document the missing Phase 0.40 baseline artifact before treating Phase 0 documentary acceptance as verified.
- [ ] Review the changed architecture/PDD documents and ensure **no runtime code, tests, scene/prefab assets, Unity `.meta`, vendor files or configuration changed**.
- [ ] Only after manual approval remove `needs-validation` and permit dependent Stage 1.0 implementation issues.

**No Unity compile, Play Mode, dedicated-server run, automated regression run or performance measurement is asserted by this documentation change.**
