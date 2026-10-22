# Ninth Age — Phase 1 Implementation Plan

**Phase:** 1 — Core Runtime and Production Infrastructure  
**Branch:** `dev`  
**Status:** In progress — stages 1.0/1.1 issues closed; Stage 1.2 partially implemented, manual gates and downstream work remain  
**Last updated:** 2026-10-22  
**Parent roadmap:** [Project Roadmap](Project-Roadmap.md)  
**Implementation baseline:** Existing Phase 0 runtime; not a greenfield replacement

---

## 1. Objective

Establish the **performance, simulation, networking, world-runtime, client-resource, persistence, content-release and operational foundations** that every subsequent gameplay phase can safely depend on.

Ninth Age is an MMO built with Unity 6.3, URP Forward+, a headless authoritative server, PurrNet, a hybrid GameObject/Unity Entities runtime, and a very large streamed world. Phase 1 must prepare that existing architecture for **foreseeable high-stress workloads before content and gameplay complexity multiply**.

**Governing principle:** Anticipate the highest-stress plausible scenarios and implement their reusable architectural prerequisites now. Do not defer an identifiable foundational dependency until it becomes a production bottleneck. Equally, do not adopt speculative frameworks or rewrite functional systems without measured or demonstrable architectural justification.

A scalable architectural design, a tested workload, and a guaranteed production capacity are **three different claims**. No numerical server or player capacity is implied by completion of this plan.

## 2. Authority, constraints and scope

Follow these documents; they own product behaviour and must not be silently redefined by this technical plan:

- [MMORPG Master PDD](../MMORPG-Master-PDD.md).
- [C# Architecture](../CSharp-Architecture.md) and [C# Style Conventions](../CSharp-Style-Conventions.md).
- [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md).
- [AI and Encounter Behaviour PDD](../pdd/AI-and-Encounter-Behaviour-PDD.md).
- [Graphical Approach PDD](../pdd/Graphical-Approach-PDD.md).
- [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md).
- [Live Content and Versioning PDD](../pdd/Live-Content-and-Versioning-PDD.md).
- [GitHub Issue and Workflow Guide](../GitHub-Issue-and-Workflow-Guide.md).
- Repository `AGENTS.md`, including **third-party source is read-only** without explicit permission. Do not modify PurrNet, Unity packages, imported vendor assets or vendor assembly definitions. Adapt project-owned integrations.

Preserve these established decisions:

1. **Headless server, PurrNet networking and hybrid ECS/GameObject architecture.** Improve ownership and hot paths rather than defaulting to a full ECS migration or a second networking stack.
2. **Server-authoritative gameplay; client-authoritative movement subject to server validation.** Scheduling and replication must not weaken exploit protection.
3. **Double-precision authoritative world positions**, floating-origin client rendering, distinct origin cells and 256 × 256 m streamed world tiles.
4. **Actor simulation/identity is separate from additive environment scenes.** Retain the `global_actors` separation until an explicitly approved architectural replacement.
5. **Runtime world/map/instance identity is independent of Unity scene identity.** Two private instances may share authored scenes or equivalent coordinates without seeing or affecting each other.
6. **PostgreSQL-oriented relational persistence**, transactional ownership and progression, and persistence-neutral gameplay boundaries.
7. **Addressables content updates and immutable release artifacts** rather than an alternative bespoke asset patcher.

Phase 1 establishes common mechanisms and verifies them under synthetic stress. Full zone production, sophisticated AI content, private-dungeon gameplay, multi-machine world sharding, final art-density tuning, final capacity promises and deployment at commercial scale remain later work.

## 3. Execution rules

1. Execute stages in the order below **unless a stage explicitly identifies independent parallel work**. Each stage ends with concrete artifacts and a validation gate; failing a gate blocks dependent stages.
2. Begin with baseline instrumentation so every architecture change has a before/after comparison. Preserve representative Phase 0 behaviours and regression coverage.
3. Make timing, relevance, actor lifetime and data-ownership contracts explicit before other features adopt them.
4. Refactor real hot paths at the appropriate stage; do not merely add abstract interfaces while leaving the original per-frame scans and allocations active.
5. Prefer direct, understandable, testable implementations. Add pooling, Jobs, Burst, custom buffers or caches only where appropriate to ownership and workload characteristics, and measure their effects.
6. Each implementation issue must identify affected project-owned files and methods, dependencies, non-goals, automated/static validation and manual Unity/multiplayer validation. Follow the repository workflow's separation of implementation and regression-test issues.
7. Preserve Unity client, standalone headless server, editor and multiplayer operation. Record any validation that cannot run in automation; never represent static tests as proof of performance.
8. Configure operational limits and budgets centrally, not as scattered literals. A threshold should be justified by a test profile, hardware specification and expected gameplay behaviour.

### Suggested existing integration points (not an exhaustive change list)

| Responsibility | Existing project-owned entry points |
| --- | --- |
| Server ECS composition / timing | `Assets/Runtime/Server/Core/ServerECSManager.cs`; `Assets/Runtime/Server/Systems/*System.cs` |
| Actor ECS/GameObject bridge, NPC perception | `ServerActorManager.cs` — `Update`, `LateUpdate`, `UpdateNPCAggro`, `SyncPlayerECS`, `ValidatePlayerPositions` |
| Interest and spatial subscriptions | `ServerInterestManager.cs` — `OnInterestRequest`; `Shared/Networking/ChunkSubscriptions.cs`; `ChunkVisibilityRule.cs` |
| Actor state / network-visible representation | `Server/Systems/ActorComponent.cs`; `Shared/Runtime/Actors/Actor.cs`; `NPCActor.cs`; `ServerSpawnManager.cs` |
| Movement | `Shared/Movement/MovementSimulation.cs`; `Shared/Networking/MovementEndpoint.cs`; `ServerPositionManager.cs` |
| Client streaming / floating origin | `ClientWorldManager.cs`; `ClientPositionManager.cs`; `Shared/Utility/WorldPosition.cs` |
| Networking and serialisation | `Shared/Networking/*Endpoint.cs`, `PackWorldPosition.cs`, `PackStatSnapshot.cs`; project-owned PurrNet integration |
| Persistence / account ownership | `Server/Persistence/PlayerDatabase.cs`, `PlayerAccountData.cs`; `Server/Services/PlayerAuthService.cs`; `Shared/Persistence/CharacterData.cs` |
| Test foundations | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode`, `Assets/Tests/Fixtures` |

Names above identify the **current baseline**, not a requirement to preserve every existing method or create an unverified new one.

---

## 4. Chronological implementation sequence

### Stage 1.0 — Design contracts, stress profiles and measurable baseline

**Prerequisite:** Phase 0 has reached its stable-foundation milestone.  
**Unblocks:** Every subsequent stage.

**Implement:**

- Record a concise performance architecture decision document for time/tick ownership, actor/entity/NetworkIdentity ownership, spatial-index ownership, instance identity, replication authority, resource lifetimes and the distinction between streamed environment and simulated actors.
- Enumerate stress profiles representing **dense cities**, **large open-world battles**, **world-boss/AoE combat**, **large mostly-inactive NPC populations**, **fast seamless traversal**, **dense forest/settlement rendering**, **login/reconnect bursts** and **long-running server soak**.
- Specify reproducible fixtures: actor and player-count distributions, abilities, NPC activity mix, map/instance topology, world-object density, movement patterns, packet latency/loss, server/client build configuration and test-hardware class.
- Define provisional benchmark sizes as **engineering probes**, not shipped capacity limits. Include multiple tiers that increase until overload is observed; record measured capacity rather than asserting a target is already supported.
- Add instrumentation for per-system server CPU time (including p50/p95/p99), simulation backlog/missed deadlines, allocated bytes and GC, live/active/dormant actors, spatial candidates/query costs, replication recipients/bytes/message counts, PurrNet transport metrics available through supported APIs, client main/render/GPU frame time, draw/instance counts, memory, Addressables load/unload latency, and streaming spikes.
- Provide headless-server load drivers and reproducible multiplayer/client harnesses; build release-like profiling configurations and a baseline report from the existing implementation.
- Define **initial pass/fail budgets and regression tolerances with rationale** after capturing hardware-specific baselines. Preserve raw results and compare equivalent builds and fixtures.

**Gate:** Every profile can be run or has a documented executable fixture plan; benchmark identity and metrics are versioned; at least the current runnable profiles have recorded baselines. No performance claim without workload and hardware context.

### Stage 1.1 — Authoritative simulation clock and scheduling

**Prerequisite:** 1.0.  
**Unblocks:** ECS integration, activity tiers, timed persistence, combat and AI expansion.

**Implement:**

- Define a single authoritative server simulation clock with explicit tick ID, simulation delta, configured fixed-step frequency, monotonic elapsed-time handling and a documented phase ordering.
- Route existing movement/validation, spawn activity, regeneration, cast completion, cooldowns, auras and generic tickers through coherent scheduling boundaries. Preserve each subsystem's gameplay semantics and test equivalence.
- Provide repeatable scheduling for high-frequency simulation, lower-frequency decisions and event/deadline-driven work. Ensure later AI and game systems register against this common scheduler instead of inventing independent arbitrary-frequency `Update` loops.
- Specify deterministic **within-server ordering** of authoritative operations and safe event delivery, without claiming bitwise cross-platform determinism.
- Define bounded catch-up, overload telemetry and overload policy. Do not silently discard authoritative combat/timer events to preserve frame rate; surface and test sustained overload.
- Distinguish simulation time from durable UTC/wall-clock timestamps needed for logout, offline timers, restarts, cooldowns and auras. Record how time passes while an entity is inactive and during reconnect.
- Apply scheduling to the existing ECS system group and domain callbacks without allowing callbacks to mutate collections that are being iterated unsafely.

**Gate:** Equivalent combat/timed-state outcomes across variable render frame rates; fixed server tick behaviour under controlled load; tick ordering and catch-up tests; no duplicate/missing expiry or regeneration events; visible overload metrics.

### Stage 1.2 — Instance-aware spatial indexing and interest foundations

**Prerequisite:** 1.1 (timing contract) and existing World Runtime PDD.  
**Unblocks:** NPC activity tiers, scalable perception/combat queries and replication.

**Implement:**

- Define an authoritative spatial membership/index contract keyed by compatible **world, map and runtime instance**, with double-precision positions. Keep these separate from client floating-origin cells, environment-streaming tiles and `GameObject.scene`.
- Support insertion, movement, removal, teleports/transfers, despawn, restart/rebuild and instance teardown with no stale memberships.
- Expose allocation-conscious candidate and exact-range query paths for AI perception, nearby players, AoE/combat target lookup, spawning and proximity interactions; support filters and reliable double-precision distance checks.
- Integrate with the current chunk/scene subscription system and PurrNet visibility, retaining configured environment scene interest while adding distance-based actor interest and optional hysteresis.
- Make broad-phase spatial cell dimensions and actor visibility radii configurable independently from the **256 m world tile** and origin-cell grid.
- Prevent cross-instance visibility, targeting, combat, aggro and subscription leakage even when world coordinates overlap. Define ordering/ownership when an actor changes runtime context.
- Replace applicable repeated global scans and unnecessary per-actor physics candidate searches with spatially constrained, player-centric/event-driven evaluation. Keep actual line-of-sight/physics checks where gameplay requires them.

**Gate:** Correctness tests for movement across cell/tile/origin boundaries, range hysteresis, simultaneous instances at identical coordinates and actor cleanup; query cost grows with relevant local candidates rather than total world actor count in representative workloads.

#### Stage 1.2 — Actual implementation status and revised dependency gates (22 October 2026)

**This section supersedes the original chronological issue assumptions above where they conflict; it does not change the Stage 1.2 exit guarantee.** GitHub issue states are implementation records, **not** evidence that all owner-only Unity/headless/multiplayer runtime checks passed.

**Implemented/closed source prerequisites:** #108–#113 established spatial context/config/index/query/spawn membership; #129 approved the selected movement and server-physics design; #130 introduced the origin-qualified client proposal; #131 implemented the accepted absolute-double ECS player position and removed writable `Actor.WorldPos`. #131's PlayMode suite was reported passing by the owner, but real network correction, client rebases, ownership changes and reconnect runtime validation remain outstanding. No later stage should claim that runtime gate has passed merely because #131 is closed. Historic issue descriptions may still describe older source snapshots: read current `dev` first.

**Approved current architecture:** live server `ActorComponent.WorldPositionD` is the only mutable canonical accepted absolute position. Environment tiles are 256 m, client origin cells 100 m, actor spatial-index cells independently configured, and server Physics/NavMesh uses an initial **16 × 16 non-overlapping local slot pool at 512 m pitch**, keyed by `(WorldID, MapID, InstanceID, environment tile)` (#129). No global physics origin rebase or origin-zero fallback. #134 owns slot mapping, scene/collider/NavMesh placement, spawn/frame binding and frame-aware `ActorUpdateSystem.OnUpdate` gravity raycasts. #135 owns absolute gateway routing and agent transitions; #132 owns enabled NavMesh pre-ECS absolute acceptance and ECS-owned post-ECS projection; #136 owns observer replication and NetworkTransform position-sync cutover; #114 alone publishes accepted positions to the index.

**Revised immediate execution/dependency order (reuse existing issues; no new issues):**

1. **#133 — Player origin-rebase regression coverage:** after closed #130/#131; **independent of #132**. Validate the actual proposal→ECS origin-qualified contract, separately track live client/server runtime checks.
2. **#128 — Context-qualified environment scene ownership:** move before #134's scene-slot integration. #128 must prove that the installed project-owned PurrNet/Addressables integration can maintain independent loaded-scene instances and in-flight callbacks for the same authored tile under different logical contexts. **Do not wait for #115 context-transfer gameplay or #116 actor subscription refactor** merely to define server-owned environment load identity. If the PurrNet scene API cannot support this, stop for an explicit design/asset-scoping review before #134's multi-context placement.
3. **#134 — Server physics frames:** **one existing issue, four internal implementation phases, not child issues**: A. context/tile slot registry + verified local↔absolute conversion; B. scene/physics/NavMeshData placement and safe retirement (consumes #128); C. authoritative actor spawn and frame binding; D. ECS-ground raycast conversion (double world origin→slot-local ray, local hit→absolute ECS Y). Expand the total multi-file scope as approved, keep each phase a manageable separately reviewed change set, and obtain the owner's runtime/scene validation before the next phase.
4. **#135 → #132 → #136:** first validate long-distance NavMesh traversal over already verified packed slots; then accept enabled NavMesh pose into the canonical ECS component and project ECS-owned NPCs back to the same local frame; finally deploy one server-authored observer position stream and disable unqualified NetworkTransform position syncing only in the coordinated, functional client/server cutover.
5. **#114 — Accepted-position spatial-index Move:** only after #133 and the #134/#135/#132/#136 implementation and relevant owner runtime gates. Retain same-cell exact-position query truth and publish no rejected position.
6. **#115–#128 remaining consumers:** #115 context transfer, #116 subscription qualification, #117 visibility, #118 observer reevaluation, #119–#124 combat/AI candidate/range consumers, #125 despawn registrations, #126 accepted movement-driven interest, #127 stale async scene cleanup. Sequence according to each actual data owner and validated prerequisites, not solely original issue-number order. #128 is pulled earlier because it is a **prerequisite for** #134's context-qualified scene placement, not a post-#115 feature.

**Issue/workflow policy:** The default ~1–3 files/~150 net lines is **per focused reviewable changeset**, not a hard cap that makes approved cross-owner infrastructure impossible. #134 remains one open issue containing ordered phases with individually recorded acceptance, not a tracker with extra child tickets. Only the owner authorizes creation of new GitHub issues or unapproved new state/asset designs; do not automatically split to inflate issue count. Keep production and high-value regression-test changes separate. Repo `AGENTS.md` prohibits Codex commits/pushes without explicit approval. **Phase 1.2 sign-off remains blocked** until client/server and two simultaneously distant NPC populations, negative coordinates, physics/NavMesh scene lifecycle, gravity raycasts, context isolation and performance-sensitive spatial consumers have owner-validated evidence.

### Stage 1.3 — ECS data ownership, batched hot paths and Jobs/Burst boundary

**Prerequisite:** 1.1–1.2.  
**Unblocks:** Efficient actor population and lifecycle management.

**Implement:**

- Establish a per-field **single source of authority** for position, motion, actor state, casts, auras, cooldowns, threat and visibility. Document precisely when ECS, server domain logic, NavMeshAgent, PurrNet NetworkBehaviour or client presentation is authoritative.
- Review `ServerActorManager` full-entity iterations and the per-actor `GetComponent`/Transform/EntityManager synchronisation boundary. Replace unnecessary scans and lookups with tracked/dirty/eligible sets or batched data transfer where beneficial.
- Separate data-only simulation from operations requiring UnityEngine objects, Physics, NavMesh, network identity or main-thread presentation. Make a coherent main-thread command/event boundary for work produced by jobs.
- Convert suitable numeric, homogeneous, measured hot paths to Unity Jobs/Burst, while retaining managed paths when domain callbacks or PurrNet APIs require them. Confirm safety, scheduling, job completion and disposal.
- Keep actor spawn/despawn and structural ECS changes explicit, testable and safe relative to running jobs and queued events.
- Establish allocation standards for high-frequency code, reuse temporary buffers where justified, and ensure cache/pool cleanup. No blanket ban on managed code or indiscriminate rewrite of all gameplay into Entities.

**Gate:** Actor and timed-state regression tests, stable actor/entity ownership across lifecycle transitions, zero unexpected job-safety failures, and before/after benchmark results for changed hot paths.

### Stage 1.4 — Actor lifecycle, simulation LOD and high-population AI

**Prerequisite:** 1.1–1.3.  
**Unblocks:** NPC content production and dense-world simulation.

**Implement:**

- Define explicit actor lifetime and simulation activity states (for example: **engaged/full**, **nearby/reduced**, **distant/minimal**, **dormant**) with legal transitions, ownership, wake-up triggers and state-retention rules.
- Drive activity from player relevance, encounter participation and gameplay obligations. An unloaded environment scene alone must **not** imply that a persistent actor ceases to exist.
- Move expensive AI perception/decision scheduling to shared relevance-based frequencies and spatial queries; avoid all-NPC, every-frame perception and allocating a collider array per NPC.
- Support event-driven activation from threat, combat, damage, scripted encounters, movement into relevance and instance transitions.
- Preserve authoritative timers, cooldowns, auras, death, respawn, aggression, threat and persistence invariants when sleeping and waking. Require explicit catch-up/fast-forward semantics; never grant exploits by deactivation or disconnect.
- Define when an actor needs a live GameObject, NavMeshAgent or PurrNet identity, and when cheaper authoritative data can suffice. Introduce lightweight representation only where compatible with required game/network semantics.
- Test mass awakening and density transitions to avoid spikes merely moved from steady-state updates into reactivation.

**Gate:** Dormant populations incur demonstrably lower steady-state CPU costs; waking restores correct game state; nearby combat remains responsive; stress tests capture activation spikes and no cross-instance leakage.

### Stage 1.5 — PurrNet replication and bandwidth scalability

**Prerequisite:** 1.2–1.4 and authoritative session identity contract.  
**Unblocks:** Large multiplayer encounters and populated hubs.

**Implement:**

- Audit supported PurrNet visibility, serialization, transport, delivery and update policies **before** introducing custom mechanisms; extend only the project-owned integration.
- Define who receives which actor/state updates based on server-approved instance membership, spatial relevance and gameplay exceptions (e.g. party/group information that has different visibility semantics).
- Define update priority/frequency classes for movement, combat-critical events, actor state, noncritical cosmetic state and distant actors. Set per-connection bandwidth/backlog monitoring and overload behaviour; authoritative actions must not be silently dropped.
- Use change detection/delta-like update strategies or batching **only where compatible with supported PurrNet APIs and correctness**; keep delivery ordering/reliability explicit.
- Bound replication work to relevant audiences, not a global actors × connected players sweep. Audit `PackWorldPosition`, stat snapshots and similar serialization hotspots.
- Validate late joins, transitions, teleport/reconnect, death/despawn, packet loss, latency and rapid appearance/disappearance; maintain server movement validation and combat authority.

**Gate:** Correct in-range/instance visibility and state ordering; recorded bytes/sec, messages/sec, interest-evaluation cost and connection backlog for sparse and concentrated player profiles; no regressions under network impairment.

### Stage 1.6 — Client rendering, world streaming and resource lifetime foundations

**Prerequisite:** 1.0, 1.2–1.3 where shared actor/world contracts are required.  
**May proceed in parallel with:** 1.4–1.5 after contracts are stable.

**Implement:**

- Define client-side lifetime ownership and budgets for streamed world tiles, additive object scenes, terrain LOD representations, instantiated actors, effects, UI/nameplates, pooled assets and remote Addressables handles.
- Instrument and bound scene loading/activation/unloading work and main-thread spikes while moving rapidly across tile boundaries. Enforce balanced asset acquisition/release and teardown across logout, world transfer and reconnection.
- Establish practical pooling policies for repeat-spawn transient VFX/UI/actors when beneficial, with hard cleanup and reset-state requirements; do not pool everything.
- Standardise distance/visibility LOD and culling integration for terrain, meshes, vegetation, animation, shadows and VFX. Define required asset-authoring metadata and validation so future art is economical by construction.
- Benchmark **URP Forward+** with representative forest, settlement and crowded-character fixtures. Evaluate GPU instancing, SRP Batcher, GPU-driven options and impostors/HLOD by compatibility and profiling evidence; do not assume every technique stacks or should be turned on globally.
- Preserve day/night and future weather/lighting hooks without building complete environmental systems in this phase.
- Test seamless additive scene transitions, floating-origin shifts, distant terrain continuity and stable memory over repeated load/unload cycles.

**Gate:** No unbounded Addressables/scene/pooled-object retention; representative density and high-speed traversal metrics documented; predictable culling/LOD contracts for future content; no visible world/actor discontinuity from optimisations.

### Stage 1.7 — Relational persistence, transactional state and timed-state continuity

**Prerequisite:** 1.0; final timed-state integration depends on 1.1 and lifecycle contracts in 1.4.  
**May proceed in parallel with:** 1.2–1.6 for schema design and independent storage work.

**Implement:**

- Follow the Account, Character and Persistence PDD: immutable `AccountID`/`CharacterID`, account-owned home world, durable roster, relational ownership/state entities, database constraints, schema migrations and auditable high-value transactions.
- Replace whole-file `player_data.json` authority with a server-owned SQL persistence boundary. Use the PDD's PostgreSQL / EF Core + Npgsql reference direction, subject to explicit Unity/headless runtime compatibility validation; do not quietly substitute a different provider.
- Keep gameplay and ECS systems independent of database APIs. Define async work, concurrency, ordering and bounded queues so database operations cannot block or unpredictably stall the authoritative simulation tick.
- Implement creation/selection, inventory/equipment, progression, world/map/instance location and relevant timed state; add quest, reputation and profession schema foundations consistent with currently available gameplay state, without implementing Phase 4–5 gameplay systems prematurely.
- Preserve Health/resources, cooldowns, persistent auras, death state, offline progression rules and anti-relog-exploit invariants. Reconstruct derived statistics rather than persisting duplicated computed output.
- Implement explicit JSON prototype-data import/migration with idempotency, diagnostics and rollback/backup rehearsal. Validate concurrent transactions, interrupted saves, restart and round-trip consistency.
- Provide soft delete/restore identity and name-reservation foundations where required by the PDD.

**Gate:** All existing meaningful character state persists through relog/restart; competing or interrupted economic/progression operations cannot duplicate ownership; failed database dependencies fail explicitly without corrupting authoritative state; load bursts have measured tick impact.

### Stage 1.8 — Sessions, authentication and reconnect authority

**Prerequisite:** Persistence identity model 1.7 and actor lifetime contracts 1.4.

**Implement:**

- Distinguish account, gameplay session, selected character, runtime actor and network connection; enforce **one authoritative session per account**.
- Implement authoritative ownership/character-selection validation, login/logout eligibility and disconnect-grace semantics consistent with the persistence PDD.
- Reclaim the correct actor and world/map/instance context on reconnect where permitted; do not respawn a clean character or reset combat-relevant state.
- Account for simultaneous login attempts, transient connection loss, server restart, invalid location recovery and authentication-rate safeguards appropriate to the baseline.
- Test multi-client connection races and mass reconnects under load.

**Gate:** No duplicate authoritative sessions/actors or lost/duplicated items; reconnect and restart preserve relevant state; burst traffic has bounded, measured server and persistence effects.

### Stage 1.9 — Release identity, compatibility and Addressables content delivery

**Prerequisite:** Stable persistence schema and gameplay-definition ownership; client asset lifetime baseline 1.6.

**Implement:**

- Establish explicit client/server build, network protocol, gameplay-content, Addressables catalog and database-schema identities, plus release/build metadata and an immutable release manifest.
- Validate compatibility before world entry; fail closed for incompatible authoritative client/content/schema state with actionable diagnostics.
- Establish stable data-definition IDs and runtime registries for hotfixable definitions; remove `Resources.Load` / direct-reference **authority** only for data designated as remotely hotfixable, preserving local bootstrap and executable-bound content.
- Complete the existing Addressables remote catalog, content-update, local-cache, bundle ownership/dependency and platform-specific client/server content workflows.
- Preserve each release's `addressables_content_state.bin` and immutable artifacts; verify patch download size, cache invalidation, content hash/revision and rollback compatibility.
- Enforce reliable asset/definition references across client/server revisions and prevent accidental cross-release mixing.

**Gate:** Controlled compatible hotfix works without rebuilding the client; incompatible builds/content cannot enter; catalog/bundle updates are reproducible and measured; asset dependency/release artifacts are auditable.

### Stage 1.10 — Runtime operations, failure handling and CI performance gates

**Prerequisite:** 1.0–1.9 interfaces and metrics are established.  
**Some logging/config/CI scaffolding may begin in parallel after 1.0.**

**Implement:**

- Structured headless-server logs with event, tick, world/instance, correlation and error context as appropriate; avoid costly per-entity logs on hot paths.
- Separate development/test/production configuration and sensitive credentials; validate required settings and database/content/schema compatibility at startup.
- Provide operational metrics for tick overload, network pressure, persistence backlogs, active/dormant population, stream churn and memory trends.
- Implement persistent-data backup/restore, migration rehearsal, clean shutdown and failure recovery. Ensure unsupported failures are explicit rather than masked by silent fallbacks.
- Run lightweight deterministic perf/behaviour smoke tests in CI and heavier multiplayer, rendering and soak suites on suitable dedicated runners. Preserve hardware/build metadata; avoid brittle universal thresholds across dissimilar machines.
- Track failures to owner, regression range and source commit. Tie performance gating to repeatable baseline comparisons and accepted thresholds.

**Gate:** Operational failure and overload are observable; backup/restore and migration rehearsal are demonstrated; representative CI/workflow runs produce comparable, inspectable artifacts.

### Stage 1.11 — Integrated stress verification and Phase 1 sign-off

**Prerequisite:** All prior stage gates passed.

Run the Phase 1 benchmark matrix against a release-like headless server and graphical client, exercising **combined** workloads rather than isolated microbenchmarks:

| Profile | What must be stressed together |
| --- | --- |
| Dense settlement | Many nearby players/NPCs, interest changes, network updates, animation/nameplates, interaction and rendering |
| Open-world battle / world boss | Concentrated players, frequent movement and combat, AoE, auras, threat, replication and event ordering |
| Large NPC population | Many dormant entities, moving players, nearby activation and mass wake-up |
| Rapid traversal | Streaming and unloading, actor relevance changes, floating-origin shifts, Addressables memory and activation spikes |
| Dense forest / geometry | Visibility, vegetation/terrain LOD, instancing, lighting/shadows and GPU/frame-time constraints |
| Login / disconnect / reconnect burst | Session exclusivity, persistence concurrency, actor restoration and network queues |
| Extended soak and recovery | Server memory/GC, actor/index/pool lifetime, timed state, database reconnect and process restart |

**Measure:** median/tail server tick cost, deadline overruns/backlog, client CPU/GPU p95/p99, peak and retained memory, GC/allocations, actor states, spatial query work, network bandwidth/backlogs, streaming stalls and persistence latency/correctness.

**Sign-off requirements:**

1. The approved Phase 1 budgets for specified hardware and stress profiles are met **or** every unmet gate has a documented architecture-level blocker and explicit decision; a known foundational blocker cannot be deferred as routine future optimisation.
2. No common gameplay subsystem must perform routine full-world scans or per-NPC per-frame expensive perception when a shared spatial/scheduler mechanism exists.
3. All actor representations, ECS jobs, subscriptions, Addressables handles, pools and DB operations have correct ownership and cleanup.
4. Instance isolation, server authority and double-precision spatial correctness are preserved.
5. The production-style persistence/reconnect milestone and content/schema compatibility checks pass under load.
6. Representative headless and client test results are recorded and comparable; automated regression checks exist for the critical contracts.
7. Later Phase 2–3 gameplay, AI and world-authoring implementations can consume documented shared infrastructure without inventing local schedulers, interest models, pooling schemes or persistence access.

**Exit milestone — Scalable, Durable Runtime Foundation:** A durable character can enter and traverse a seamless multiplayer world, change authoritative state, reconnect and survive restart; compatibility mismatches are rejected; and the hybrid server/client runtime has **demonstrated** its agreed performance and resource-lifetime behaviour on documented high-stress workloads.

---

## 5. Dependency overview and parallel tracks

```text
Phase 0 stable foundation
      |
      v
1.0 Contracts, instrumentation, benchmark baseline
      |
      v
1.1 Authoritative simulation clock and scheduler
      |
      v
1.2 Instance-aware spatial index / interest
      |
      v
1.3 ECS ownership / batched hot paths / Jobs-Burst boundary
      |
      v
1.4 Actor lifecycle + simulation LOD
      |
      v
1.5 Network replication scalability
      |
      +----------------------+
      |                      |
      v                      v
1.6 Client resource      1.7 Persistence layer
    + streaming          (schema work can begin after 1.0;
      |                    timed-state integration after 1.1/1.4)
      |                      |
      |                      v
      |                 1.8 Sessions / reconnect
      |                      |
      +-----------+----------+
                  v
       1.9 Release/version/content delivery
                  |
                  v
       1.10 Operations and automated gates
                  |
                  v
       1.11 Integrated stress verification
```

The diagram illustrates **integration dependencies**, not a demand to leave independent teams idle. Persistence schema work, early operational scaffolding and client benchmark fixture work can begin once 1.0 contracts are established. They cannot be considered integrated until their respective prerequisites pass.

## 6. Hand-off contracts for later phases

- **Phase 2 gameplay and combat:** use the authoritative clock, simulation scheduler, spatial queries, actor ownership and relevance-aware replication; never independently create per-frame global entity scans.
- **Phase 2 NPCs/AI:** implement authored decision logic above the shared activity/AI scheduling model; expensive perception must respect spatial and instance membership.
- **Phase 3 world runtime:** expand tile authoring, world-streaming LOD, instance features and interiors through the established resource and spatial boundaries; do not conflate logical instance with scene.
- **Phase 3 presentation:** author art according to the Phase 1 renderer/LOD/culling/streaming contracts, then tune quality and density through measured budgets.
- **Phases 4–7 gameplay breadth:** integrate new persistent state through transactional server persistence and versioned definitions; use established networking and metrics.
- **Phase 8 hardening:** increase validated density, soak duration, supported capacity and fault tolerance, rather than first introducing scheduling, spatial indexing, activity tiers or observability.

## 7. Issue decomposition and evidence requirements

Create narrowly scoped GitHub issues under Phase 1 stage labels/fields, respecting the repository's issue workflow. Each issue should include:

- Stage and prerequisite issue(s).
- Exact current source references, affected APIs, ownership boundaries and expected changes.
- Explicit non-goals and compatibility constraints.
- Static/unit/regression tests; separated implementation/test issues where the workflow requires.
- Manual headless/client/Editor/multiplayer tests and workload profiles.
- Baseline-versus-result measurements where performance is material.
- Passing acceptance criteria and evidence links.

**Do not close an implementation issue simply because it compiles, produces a microbenchmark improvement or works in a single-player empty scene.** Validate the relevant shared contract and regression scenarios.

The plan is intended to be revised as specific implementation decisions are measured, but changes to its foundational guarantees require a deliberate architectural review rather than silent deferral.
