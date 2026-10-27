# Ninth Age — Phase 1.0 Performance Stress Profiles

**Status:** Proposed engineering workload specification; requires manual acceptance of [Issue #42](https://github.com/FrontierDev/ninthage/issues/42).
**Specification revision:** `P1-STRESS-1.0.0`
**Source reviewed:** `dev` at `3e5465d372fa843e83d13cb1e00da0c95836221d`, 13 October 2026.
**Scope:** Scenario definitions, reproduction inputs and provisional engineering probes **only**; no gameplay implementation, live population cap, measured result or performance budget.

## 1. Authority and what this document does not prove

This catalogue implements the profile definition requirements of [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md), subordinate to [Performance and Simulation Architecture](../Performance-and-Simulation-Architecture.md) and [Project Roadmap](Project-Roadmap.md). Design authority remains with the [World Runtime and Instancing PDD](../pdd/World-Runtime-and-Instancing-PDD.md), [AI and Encounter Behaviour PDD](../pdd/AI-and-Encounter-Behaviour-PDD.md), [Graphical Approach PDD](../pdd/Graphical-Approach-PDD.md), [Movement and Traversal PDD](../pdd/Movement-and-Traversal-PDD.md), [Combat System PDD](../pdd/Combat-System-PDD.md), and [Account, Character and Persistence PDD](../pdd/Account-Character-and-Persistence-PDD.md). Follow [GitHub Issue and Workflow Guide](../GitHub-Issue-and-Workflow-Guide.md).

Each of the eight profile IDs below is an **independent executable-scenario contract**: the eventual harness must be able to select, provision and run it from a clean state with no previous profile. The contract is **not the executable harness**. All profiles are **DEFINED / NOT EXECUTED** as of this review. A benchmark result requires actual matching source/build, assets, clients, telemetry, hardware and raw results.

Explicitly distinguish:
- **Source capability:** a prototype manager/ECS/PurrNet path exists; this does not imply it can run the target workload.
- **ECS data-only:** no full NPC GameObject, PurrNet identity, actor lifecycle, real connected player, physics/navmesh or renderer unless demonstrated; never label synthetic entities as full NPCs/players.
- **Real networked:** authenticated PurrNet client connections, true actor identities and received game state; never spoof a client by directly calling server methods.
- **Real graphical:** URP Forward+ visible client on real GPU, with world content and measured frame times. Headless GPU data is `not applicable`.
- **Future contract:** fixed authoritative tick, instance-aware spatial query, dormant NPC simulation, priority replication, full reconnect continuity or streamed terrain separation not yet implemented. A blocked future-contract variant is not a pass.

No scenario may change PDD-owned world, combat, AI, session or movement rules merely to reach a requested load. World positions are server-accepted **double precision**. **100 m origin cells**, **256 m authored world-content tiles**, spatial-index buckets, additive Unity scenes and logical world/map/instance identity are distinct concepts; index-cell and visibility range values are intentionally not set by this document. Actors may survive environment tile unload regardless of which Unity scene originally spawned them.

Dependencies for later execution: [#43 manifests/evidence](https://github.com/FrontierDev/ninthage/issues/43), [#44 world fixtures](https://github.com/FrontierDev/ninthage/issues/44), [#45 seeded actor/action fixtures](https://github.com/FrontierDev/ninthage/issues/45), [#46 benchmark build/startup](https://github.com/FrontierDev/ninthage/issues/46), [#47–#50 telemetry](https://github.com/FrontierDev/ninthage/issues/47), [#51 network/headless runner](https://github.com/FrontierDev/ninthage/issues/51), [#52 rendered client harness](https://github.com/FrontierDev/ninthage/issues/52) and [#55 measured baselines](https://github.com/FrontierDev/ninthage/issues/55).

## 2. Shared reproducible fixture contract

### 2.1 Independent manifest requirements

[#43](https://github.com/FrontierDev/ninthage/issues/43) will specify the machine-readable manifest/schema; the inputs required by **every** profile are:

| Category | Minimum reproducible input |
| --- | --- |
| Run identity | `profile_id`, `profile_revision`, this specification revision, trial number, canonical seed, derived subsystem seeds, exact Git SHA, UTC start/end and recorded test environment. |
| Revisions | Unity, URP/Entities/Addressables/PurrNet versions, build target, source/content/scene/fixture hashes; actual content must match stated hashes. |
| Actor counts | Requested **and achieved** connected/authenticated/in-world real players, NPCs, ECS entities, PurrNet identities, GameObjects, roles, location clusters, actually active, idle and truly dormant (only after Stage 1.4). |
| Topology | Absolute coordinate system; logical world/map/instance when available; authored, loaded and visible 256 m tiles; 100 m origin crossings; spawn-point and route seeds. Never infer an instance ID from `World_X_Y` scene name. |
| Gameplay | Chosen *existing* definition IDs for autoattack/spells/auras/abilities and legal targeting, scripted movement routes, cast attempts/interrupts/cooldown results, AI activity and threat/targeting workload. Unsupported required definitions => preflight failure. |
| Scene/render | Objects per tile, building/tree/vegetation/prop counts, material slots, triangle/LOD/occlusion/overdraw/shadow/light/VFX settings, scene residency and actual visible counts. |
| Network | Actual client process count/host placement, transport, application traffic, measured RTT/jitter/loss and configured network impairment, bandwidth limit if applied, connection/reconnect time series. |
| Timing | Seeded action schedule, ramp/warmup/measurement/recovery, preflight timeout, sampling intervals, simulation versus wall/UTC clocks and workload achieved per phase. |
| Build/hardware | Headless or graphical, renderer, quality/resolution, scripting/backend/profiler flags, CPU/physical cores, RAM, GPU/VRAM/driver (when rendered), storage/OS, shared versus dedicated client/server machines, profiler overhead. |
| Evidence | Per-system CPU, GC/memory, counts, spatial/replication and PurrNet-supported counters, client GPU when appropriate, streaming/Addressables, correctness oracle, failures and raw artefact paths. |

**Canonical seed:** `424242` per profile revision; independent replicate seeds `424243` and `424244`. Derive actor/world/action/network streams deterministically from `(profile_id, revision, seed, subsystem)` with a stable versioned hash to be specified by #43; **not** process-dependent `string.GetHashCode()`. The random *plan* is reproducible, but physics/packet scheduling and cross-machine runtime results are **not claimed bitwise deterministic**. Preserve observed achieved event rates and any aborted actions.

**No unsafe defaults:** A missing scene/prefab, unsupported ability, absent supported PurrNet metric, absent network shaper or unavailable GPU must produce an explicit blocked/inapplicable result—not an empty scene, synthetic successful client or zero counter. Use test-only accounts/characters in isolated persistence data; do not publish credentials, IP addresses or player data.

### 2.2 Synthetic activity terminology

These are **benchmark inputs, not gameplay configuration/default balance**. `idle` means present and currently non-engaged, which may still invoke per-frame scans. `patrol` means authored legal navigation/AI activity; `engaged` means server-owned threat/combat. `dormant` is reserved for actual future Stage 1.4 simulation LOD and must **not** be inferred from idle. Requested `casts/s` are **attempts per eligible active actor**, not forcibly successful casts; record accepted/rejected/finished/interrupt, cooldown, resource and periodic effects. `AoE fan-out` is actual legal target count, not injected fake hit events. `Objects` means instantiated, and visible/culled/LOD/rendered objects must be counted separately.

### 2.3 Network impairment presets

| ID | Requested impairment (test parameter, not product tuning) | Validity |
| --- | --- | --- |
| `NET-LOCAL` | Controlled LAN/loopback without injected delay/loss | Still measure achieved RTT/loss, do not assume zero. |
| `NET-TYPICAL` | Nominal 80 ms RTT, ±20 ms jitter, 0.5% loss | Only valid with supported public transport control or external emulator and observed actual conditions. |
| `NET-ADVERSE` | Nominal 160 ms RTT, ±40 ms jitter, 2% loss | Record timeouts/retries, never claim configured impairment was achieved without measurement. |

The project [LatencySimulator](../../Assets/Runtime/Shared/Networking/LatencySimulator.cs) configures PurrNet 1.25.0 through the public `UDPTransport.networkSimulation` boundary. Editor and Development-build impairment may use this supported boundary, but measured RTT/loss must still be recorded rather than inferred from configured values. External shaping remains valid where a transport or platform requires it. Production/non-development builds do not enable project-configured impairment through this component. If traffic shaping is unavailable, impairment variants stay `BLOCKED-PREREQUISITE`.

### 2.4 Provisional timing presets

These are **harness/orchestration defaults**, not approved game tick, cooldown, AI update frequency or measured budgets. Each phase's raw data is separately retained: do not conceal startup/streaming spikes by excluding ramp from the steady-state distribution.

| Run preset | Preflight startup limit | Ramp | Warmup | Measured steady state / challenge | Recovery |
| --- | --- | --- | --- | --- | --- |
| `RUN-STANDARD` | 120 s | 120 s | 60 s | 300 s | 60 s |
| `RUN-TRAVERSAL` | 120 s | 120 s | 60 s | 600 s repeated full routes | 90 s |
| `RUN-BURST` | 120 s | 120 s scheduled admissions | 60 s | 300 s including reconnect churn | 120 s |
| `RUN-SOAK-SMOKE` | 120 s | 120 s | 60 s | 900 s | 120 s |
| `RUN-SOAK` | 120 s | 300 s | 900 s | 7,200 s (2 hours) | 600 s |

Preflight limit is an adjustable declared fixture parameter: slower hardware must record the chosen value rather than silently extend it. A 6–24 h soak is an optional separately revisioned extension, not evidence that a shorter run passed long-term durability.

## 3. Progressive engineering probes — NOT server population caps

Requested totals **P** (real connected in full mode) and **N** (full NPC actors in full mode), with separate accounting for data-only entities. The numeric values are illustrative engineering inputs. They are **not** player-capacity promises, required supported counts or performance pass/fail thresholds.

| Tier | Requested P | Requested N | What the probe explores |
| --- | ---: | ---: | --- |
| `T0` | 1 | 10 | Minimal whole-system smoke. |
| `T1` | 10 | 100 | Small local network/population test. |
| `T2` | 50 | 500 | Concentration and AI scheduling probe. |
| `T3` | 100 | 1,000 | Visibility/packet fan-out probe. |
| `T4` | 250 | 2,500 | Heavy load, only after completed lower tiers. |
| `T5+` | 500+ | 5,000+ | Configurable exploration until overload; **not** a target capacity. |

Each profile overrides P/N composition where the workload requires it (boss has 1 boss, city is dense, NPC test is NPC-dominated, rendering is density-dominated). Measure counts actually spawned/connected, not requested counts. Run lower tiers first and stop after first failed/provision-blocked tier, optionally bisect between last correct and first incorrect probe. Profile IDs, revision and seeds remain stable; population and rate changes are separate fixture parameters, always archived.

**Single-axis diagnostic sweeps:** before combined tests, hold scene density, actor count, action rate, client count or active fraction fixed while changing only the named bottleneck axis. Combined sweeps may scale several dimensions together but cannot by themselves identify a causal hotspot. Retain raw population achieved and hardware per tier.

### 3.1 Global stop conditions

A run must stop, preserve partial raw evidence and classify itself explicitly if:

1. **Correctness veto:** duplicated actor/character, cross-world/map/instance visibility/damage, incorrect authority/range, silent missing cast/aura/cooldown/periodic work, unload data loss or reconnect-as-combat-escape. An unimplemented correctness oracle is `UNVERIFIED`, never a pass.
2. **Simulation overload:** player-loop server cannot make forward progress, processes become nonresponsive, or future Stage 1.1 authoritative tick/deadline misses configured server budget. **Current scheduler backlog is unavailable, not zero.**
3. **Resource ceiling:** prior-to-run declared machine/container/process/GPU-memory/runtime/transport safety ceiling reached; no invented global RAM/fps threshold in this document.
4. **Workload not achieved:** missing prefabs/spells/scene, actual actors/clients, expected rendered objects or generated content, legal AoE target count, network impairment, correct test accounts. Do not silently downgrade to easier scenario.
5. **Uncontrolled failure:** crash, hang, timeout, out-of-memory, scene/persistence failure or uncontrolled connection loss.
6. **Invalid instrumentation:** critical counter/export/profiler unavailable, wrong SHA/content/hardware, incorrect sample count or incomplete run trace.

Proposed outcome vocabulary for #43: `COMPLETE-MEASURED`, `BLOCKED-PREREQUISITE`, `FAIL-CORRECTNESS`, `FAIL-PROVISION`, `FAIL-RESOURCE`, `FAIL-TIMEOUT`, `FAIL-INSTRUMENTATION`. These are descriptive labels pending canonical schema. An exit code of 0 alone does not make a valid benchmark.

## 4. Eight independent stress profiles

### P1-S0-01 — Dense persistent city

**Objective:** Local actor density, nearby observers, idle NPC overhead and heavily repeated client nameplates/draw calls. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Use T0–T5+ baseline P/N. Players: 60% in one plaza (75 m radius), 30% on streets (75–250 m), 10% across adjacent tiles. NPCs: 50% stationary civilians, 35% idle guards/vendor-shaped actors, 10% patrol, 5% combat-capable where legal. Vendor behaviour is not assumed implemented. |
| World/map/scene topology | Single public world/map; plaza and streets cross at least one 100 m origin-cell boundary and one independent 256 m tile boundary; 3×3 nearby tile interests and a stable actor scene. Per populated tile: provisional 30–80 building shells, 150–400 props, 10–50 trees, 5–25 locally visible lights; record measured resident/visible density. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | Players 45% idle/turn/UI, 45% legal walking loops (10% of walkers cross tile edges), 10% existing self/ally utility casters requesting 0.05–0.15 casts per caster/s. Idle NPCs must still count toward current scanning cost; patrollers follow legal paths, combat NPCs use configured hostile targets only. |
| Real connections, packet loss and traffic | Full mode must connect real P accounts; nominal steady churn 0.02 arrivals/s per 10 clients if provisioning permits. NET-LOCAL then NET-TYPICAL; count identities delivered to each player and connection attempts actually admitted. |
| Warmup / ramp / measurement / recovery | RUN-STANDARD: 120 s ramp, 60 s warmup, 300 s measurement, 60 s recovery after preflight. |
| Required measurements | Server manager/ECS CPU p50/p95/p99, NPC perception query candidates and allocations, visibility evaluations, recipients/bytes, actor counts, GC, scene residency; graphical client CPU/render/GPU, draw/batches, nameplates and streaming spikes. |
| Authoritative correctness, failure and stop rules | No duplicate actors or lost identity on tile unload; valid in-range observers receive correct actors, no invalid cross-context recipient; all requested actors, geometry and network clients actually materialised. Stop on §3 common veto or missing render density. |

**Isolated vs combined variants:** S01-I-ACTOR: minimum graphics, unchanged network actor distribution; S01-I-RENDER: constant actors, adjustable render density; S01-C-MIXED: both.

**Current-source availability / executable fixture plan:** Prototype PurrNet actor spawn, movement and 3×3 scene interests exist; generated city, legal scripts, full clients and telemetry require #43–#52. Not currently an automated runnable benchmark.

### P1-S0-02 — Concentrated open-world PvP battle

**Objective:** High-frequency legal player-on-player combat, rapid movement, observer churn, replicated effects and client VFX. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Override N: P is tier player count, split 50/50 between two opposing test factions only if supported by actual gameplay definitions; ambient NPC count = min(ceil(0.1P),100), or zero in isolated combat. Players 70% contesting a 120 m area, 20% in two approach lanes, 10% at the visibility perimeter. T0=one-player movement/cast smoke, NOT actual PvP. |
| World/map/scene topology | One open-world map, at least two neighbouring 256 m tiles and independent origin-cell edges. Per active tile: 10–30 ruins/cover pieces, 100–300 props, 20–80 vegetation pieces; sight lines/collisions are fixture controlled. No assumed instanced battleground support. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | Players 70% moving/combat, 20% ranged/support, 10% retreat/re-entry. Request 0.2–0.5 supported casts/eligible player/s, with 30% cluster/AoE where authored, 25% maintaining an aura where authored and remainder single-target; supported autoattacks. NPC guards passive unless legal combat definition permits. Record actual cast accepts and target fan-out. |
| Real connections, packet loss and traffic | Stable real P connections; optional labelled 1% measurement-phase disconnect challenge. NET-LOCAL, NET-TYPICAL, NET-ADVERSE (if emulator supported); record actual RTT/loss, recipients per effect and bytes delivered. |
| Warmup / ramp / measurement / recovery | RUN-STANDARD (120/60/300/60 s) plus separately recorded interruption/correction intervals. |
| Required measurements | Server combat and movement CPU, action/target/interrupt/tick counts, combat authority errors, observer churn, messages/bytes/latency, client VFX and CPU/render/GPU frame spikes, GC. |
| Authoritative correctness, failure and stop rules | No unauthorised damage, duplicate effects, missing cooldown/periodic ticks, impossible accepted movement, out-of-range or cross-instance targeting. Fail preflight when factions or required ability definitions are absent—never manufacture friendly-fire rules. |

**Isolated vs combined variants:** S02-I-COMBAT: fixed controlled targets, minimal rendering; S02-I-NET: movement/observer churn with casts disabled; S02-C-BATTLE: both.

**Current-source availability / executable fixture plan:** Casting/autoattack code paths exist, but full PvP rules/faction correctness and repeatable networked battles are not verified. #43–#52 and later 1.2/1.5 for full range/instance qualification.

### P1-S0-03 — World boss and high-fan-out AoE

**Objective:** One dense hot encounter; combat/aura ticks, legal high-target-count AoE, threat and replicated damage/effects. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | P chosen tier. Replace default NPC count with 1 boss plus max(2,ceil(P/5)) active adds; separate background variant adds 10P *passive* NPCs. Player rings: 60% within 40 m, 30% at 40–80 m, 10% mobile. T0 is cast-targeting smoke, not a raid claim. |
| World/map/scene topology | Single public world/map, one arena tile plus neighbouring unloaded/loadable tiles and one 100 m origin boundary. Private encounter instance is blocked until Stage 1.2. Fixed arena: 10–40 obstacles, 30–80 props, 0–30 plants and documented lights/shadows. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | Roles 50% damage, 25% support/healing, 25% moving/melee where actual definitions permit. Request 0.2–0.4 legal casts/player/s and 0.1–0.3 NPC casts/s, 1–3 live auras/eligible active actor, intentional cast interrupts. Sweep AoE *requested* legal target fan-out 1/5/20/50; report actual hit/affected targets, never multiply effects outside the game engine. |
| Real connections, packet loss and traffic | P real clients observing shared boss, NET-LOCAL and NET-TYPICAL; optional separate 1% reconnect slice. Record distribution of recipients and on-wire bytes only if exposed by supported transport telemetry. |
| Warmup / ramp / measurement / recovery | RUN-STANDARD; log measured phases and controlled encounter resets only through legal state construction (no unaccounted boss deaths). |
| Required measurements | Per-system cast/aura/cooldown/threat timings, requested/completed casts, target count, tick/interrupt events, GC, recipients/bytes, VFX instances and rendered-client CPU/GPU p95/p99. |
| Authoritative correctness, failure and stop rules | No missing or duplicated damage/heals/auras, invalid target, improper resource/cooldown consumption, cross-instance AoE or shrinking live population without lifecycle accounting. Stop if required spell target shape unsupported. |

**Isolated vs combined variants:** S03-I-TARGET: combat and target-fan-out without heavy render; S03-I-VFX: scripted supported VFX at measured rate, fixed combat; S03-C-RAID: both.

**Current-source availability / executable fixture plan:** Prototype cast/autoattack/aura paths exist; legal AoE spell definitions/boss encounter behaviour and replayable scripts require #45 content verification and #51/#52. No unimplemented boss phase system implied.

### P1-S0-04 — Mostly-inactive NPC population and wake-up storm

**Objective:** Distinguish total actor cost from active NPC perception, GameObject/ECS synchronisation and later dormancy efficiency. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Fix real clients P=1 or 10; increase N through tiers T0–T5+. Start 95% idle/passive, 4% legal patrol, 1% engaged. NPC placement 80% across distant tiles, 15% outer near-view ring, 5% beside players. Separate sweep 0/10/50% engaged at constant total N. |
| World/map/scene topology | At least 5×5 authored world-tile topology (NOT 25 simultaneously resident scenes), with separate origin cells. Per authored tile 5–20 fixed props, 10–30 trees. Actor simulation state must not be equated to unloaded content scenes or automatically disappearing GameObjects. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | Patrolling NPCs move on seeded legal navmesh loops; engaged NPCs request 0.1–0.25 authored casts/s; idle NPCs do not intentionally cast. Mid-measurement provoke a legal alert/engagement of 10% of initially idle NPCs in a specified region, then track legal reset. True dormant/sleep/wake simulation LOD remains a *blocked Stage 1.4 variant*. |
| Real connections, packet loss and traffic | P authenticated clients stable, NET-LOCAL baseline then NET-TYPICAL; remote NPCs should not automatically be counted visible due to existence. |
| Warmup / ramp / measurement / recovery | RUN-STANDARD; wake event at +150 s of the 300 s measured phase. |
| Required measurements | Physics.OverlapSphere per-NPC rate and candidates, NavMesh and full ECS/GO queries, per-system CPU p50/p95/p99, live versus active actor count, memory/GC, origin/scene residency, observer recipients and wake-up latency spike. |
| Authoritative correctness, failure and stop rules | No lost NPC identity/state on scene unload, ghost aggro, uncontrolled respawn cycle, undocumented decrease in total N, or declaration that idle means dormant. Fail if required NPC prefabs/navmesh/state transitions unavailable. |

**Isolated vs combined variants:** S04-I-DATA: ECS data-only, explicitly no PurrNet/GO load; S04-I-FULL: real GO+PurrNet actors; S04-C-WAKE: all plus wake event.

**Current-source availability / executable fixture plan:** Existing ServerActorManager uses per-frame Physics.OverlapSphere; the required dormant tier, lower AI rate and persistent far-away simulation await Stages 1.2–1.4. Generator/harness #43–#51.

### P1-S0-05 — Rapid seamless traversal and boundary churn

**Objective:** Independent floating-origin transitions, streamed world tile activation/unload, actor interest changes and graphics spikes under continuous legal movement. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Default T0–T3 P tiers first, N=max(10,2P), 85% stationary, 10% patrolling and 5% optionally hostile with valid data. Divide players into four route-start cohorts to avoid artificially simultaneous loading unless running separate stress variant. |
| World/map/scene topology | A connected legal route across at least SIX adjacent 256 m tiles and TWELVE independent 100 m origin transitions, including negative positions. Mix open terrain 20–100 props/tile, forest 100–500 vegetation/tile, settlement 50–200 props/tile, navmesh/collision. Public seamless interior path optional when actually implemented. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | 70% running, 20% walking/reversing, 10% faster/alternate travel only if supported; otherwise mark 10% mode blocked, do not speed-hack. Ten percent reverse near interest edges, legal supported casts ≤0.05/s, repeatedly traverse entire course and distinguish origin change from true movement. |
| Real connections, packet loss and traffic | P real clients staggered during ramp, NET-LOCAL, NET-TYPICAL then adverse if available; capture network scene/identity subscription changes and achieved speeds. |
| Warmup / ramp / measurement / recovery | RUN-TRAVERSAL: 120 s ramp, 60 s warmup, 600 s measured, 90 s recovery. |
| Required measurements | Absolute-position versus client origin invariance, movement corrections, per-scene request/activation/unload and in-flight count, root shift cost, streamed tile count, observers/traffic, server CPU, GC, client rendered frame/GPU hitches. |
| Authoritative correctness, failure and stop rules | No floating-origin displacement/teleport, missed loaded terrain, spurious actor disappearance, duplicate scene roots, stale scene subscriptions or incorrect movement authority. Stop if path blocked, tile not loaded in time or required scene assets missing. |

**Isolated vs combined variants:** S05-I-SCENE: 1 player, streamed content; S05-I-ORIGIN: loaded scenes held constant while local origin shifts; S05-C-FLOW: simultaneous players and scene churn.

**Current-source availability / executable fixture plan:** ClientWorldManager waits for nine world scenes and ClientPositionManager shifts roots. Full independent 100 m versus 256 m streaming behaviour and route automation are not yet established; requires #44/#46/#49/#52 and Stages 1.2/1.6.

### P1-S0-06 — Dense forest and settlement rendering

**Objective:** GPU/CPU renderer, LOD, foliage alpha overdraw, shadow/light count, asset residency and streaming; isolated from network actor scaling. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Primary axis is physical render-instance density with P=1 genuine rendered client and N=10 visible GO actors; optional P=10/N=100 for nameplates and effects. Do NOT substitute decorative meshes for connected PurrNet players. |
| World/map/scene topology | Two independently seeded 256 m families: forest and settlement, each central tile plus neighbours, camera route covering near view, occluded aisle, canopy horizon, settlement street, 100 m origin crossing and 256 m streaming boundary. Collision and LOD state recorded. |
| Terrain, collision and asset density | Per tile D0 forest=50 trees+200 undergrowth clusters, settlement=15 buildings+80 props. D1=×2, D2=×4, D3=×8 exploratory asset-instance counts. Independently fix/record triangle totals, material slots, 1/4/16 dynamic lights *if supported*, shadow casters, LOD/impostor ranges, terrain resolution, transparency and GPU memory; these are fixture knobs, not final aesthetic budgets. |
| NPC activity, player movement and gameplay rates | Actor mix 50% idle/turn, 40% patrol, 10% supported casts; static-camera and legal moving-camera samples. Optional VFX sweeps 1/10/50 *simultaneously live, genuine game* effects if existing definitions permit. |
| Real connections, packet loss and traffic | One actual local client/server under NET-LOCAL (optional ten-client slice). Never use Null graphics device as a GPU test. Headless simulation cost is a separate result. |
| Warmup / ramp / measurement / recovery | RUN-STANDARD for repeated static camera views; RUN-TRAVERSAL for camera routes and streaming. |
| Required measurements | Client main/render/GPU p50/p95/p99, draw calls/batches vs actual rendered instances, triangles, shadow/light/VFX/LOD and nameplate counts, texture/system memory, loading spikes, Addressables operations, GC; record hardware/resolution/URP quality. |
| Authoritative correctness, failure and stop rules | Scene and density tier actually loaded/visible; no missing object/prefab references, LOD popping/unloaded geometry at boundaries, asset leaks or fabricated GPU counters. Stop when real graphics or tracked fixture unavailable. |

**Isolated vs combined variants:** S06-I-GEOMETRY: VFX low/fixed actors; S06-I-VFX: same geometry plus effect sweep; S06-C-FOREST and S06-C-SETTLEMENT: density plus representative actors/effects.

**Current-source availability / executable fixture plan:** Rendering PDD requires efficient LOD0–LOD3/impostors; actual dense benchmark scenes and art are Git-ignored, so tracked deterministic generated fixtures #44 and rendered harness #52 are prerequisites. Full client telemetry #49.

### P1-S0-07 — Connection, login and reconnect bursts

**Objective:** Authentication and server admission rate, character entry, initial PurrNet scene delivery and reconnect actor/session correctness. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Use T0–T5+ P as *real authenticated client count*, fixed N=10 idle NPCs. Seed disposable distinct test accounts and characters. Once spawned, 80% enter one plaza and 20% neighbouring legal locations ONLY if authored spawn points exist; never claim non-existent remote spawns. |
| World/map/scene topology | Single public world/map; initial entry through 3×3 nearby world scenes and global actor scene. Fixed 10–30 static props per nearby tile. Instance isolation admission subvariant blocked until logical instance support. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | After admission 70% idle, 20% short legal movement loop, 10% supported utility spell at 0.05/s. During measurement disconnect/reconnect 25% in seeded cohorts; separate adversarial disconnect-during-combat using legal threat/damage state. |
| Real connections, packet loss and traffic | Four independent requested admission sweeps 1/5/10/25 attempts per second, bounded by P and server provisioning. NET-LOCAL/TYPICAL/ADVERSE where shaping supported. Measure attempts vs accepted, failed credential attempts separately, transport handshake, auth, character select, scene readiness, actual in-world actor. |
| Warmup / ramp / measurement / recovery | RUN-BURST: 120 s scheduled arrival ramp, 60 s warmup, 300 s admission/reconnect observation, 120 s recovery. |
| Required measurements | Time to transport/auth/character entry/in-world p50/p95/p99, server CPU, memory, GC, persistence write/load, asset/scene loading, connected/authenticated/in-world counts, duplicates, replication bytes/recipients, reconnect state equality. |
| Authoritative correctness, failure and stop rules | One active authoritative actor per character/session, no credentials in telemetry, no duplicate spawn/identity, no stale subscriptions, no combat-escape via disconnect. PDD requires at least 30 s actor survival and ongoing combat until safe resolution; **current implementation is not assumed compliant**. |

**Isolated vs combined variants:** S07-I-AUTH: login/session only (not full world-entry); S07-I-SPAWN: already-authenticated scene/actor entry; S07-C-LOGIN: entire chain/reconnect.

**Current-source availability / executable fixture plan:** Prototype connection/spawn/JSON persistence exist; safe account generator, real client harness #46/#51 and future Stages 1.7–1.8 are necessary for full compliant reconnect results.

### P1-S0-08 — Long-duration authoritative server soak

**Objective:** Detect unbounded allocations, stale actor/ECS/PurrNet references, event drift, scene/handle leaks, persistence faults and performance degradation over time. **Revision:** `1.0.0`. **Primary seed:** `424242`; replicate seeds in §2.1. **Global stops:** §3.1.

| Dimension | Required workload specification |
| --- | --- |
| Player/NPC population and clustering | Start at last safely completed tier T1 (then T2/T3 only if proven viable), P and N overridden by measured candidate population. 70% NPC idle, 20% patrol, 10% engaged; players 40% idle/turn, 35% travel, 25% supported combat. Distribute 50% shared hotspot, 35% adjacent tiles, 15% outer ring. |
| World/map/scene topology | At least three neighbouring 256 m test tiles; each player travel loop crosses an independent origin-cell boundary. Medium content: 30–100 props and 20–80 trees/tile. Server-only soak declares GPU metrics NOT APPLICABLE, not zero. |
| Terrain, collision and asset density | Terrain and objects as specified in topology; fixture hashes, object counts, loaded/visible counts and LOD/material variation recorded. |
| NPC activity, player movement and gameplay rates | Combatants request 0.05–0.15 supported casts/s, legal autoattacks, 1–2 live auras/eligible actor when definitions exist. Every five minutes perform seeded movement/streaming churn; optional distinct variant disconnect/reconnect 5% of clients every ten minutes. Record actor count and timer/expiry results after every churn. |
| Real connections, packet loss and traffic | Stable actual P clients under NET-LOCAL, NET-TYPICAL in separate comparable runs; isolate test persistence, bounded logs/artifacts and disconnect policy; never keep stale accounts or network actors between replicates. |
| Warmup / ramp / measurement / recovery | RUN-SOAK-SMOKE: 120 s ramp, 60 s warmup, 900 s measurement, 120 s recovery. RUN-SOAK: 300 s ramp, 900 s warmup, 7,200 s (2 h) measurement, 600 s recovery. Optional six-to-24-hour extension separately versioned. |
| Required measurements | All server CPU time series and p50/p95/p99, current ECS/update workload, GC/heap/resident memory trends, entity/identity/handle/scene/connection balance, action/expiry counts, persistence operations, observer/transport counters, exception chronology and future authoritative deadline/backlog when implemented. |
| Authoritative correctness, failure and stop rules | No continually increasing orphan identities/handles after balanced churn, no skipped authoritative events, corrupt checkpoint or vanishing actors, unbounded memory, persistent transport state or failed cleanup. Unsupported timed-backlog counter remains UNAVAILABLE before Stage 1.1. |

**Isolated vs combined variants:** S08-I-IDLE: no churn/casts, resource baseline; S08-I-COMBAT: timers and fixed active encounters; S08-C-SOAK: movement/combat/streaming and optional labelled reconnect churn.

**Current-source availability / executable fixture plan:** Current ECS player-loop and actor managers can supply initial behaviour only. Bounded telemetry and harness #47–#51 required; true scheduler backlog Stage 1.1, compliant durable session state Stages 1.7–1.8.

## 5. Execution/readiness matrix

Each profile is specified independently and gets its own clean fixture and run identifier. The table deliberately contains **no fabricated execution or capacity result**.

| Profile | Bottleneck axis | Standalone isolated mode | Combined mode | Current status / prerequisite |
| --- | --- | --- | --- | --- |
| `P1-S0-01` | Observers and dense city actor/render counts | `S01-I-ACTOR`, `S01-I-RENDER` | `S01-C-MIXED` | DEFINED / NOT EXECUTED; #43–#52. |
| `P1-S0-02` | Player ability rate and network fan-out | `S02-I-COMBAT`, `S02-I-NET` | `S02-C-BATTLE` | DEFINED / NOT EXECUTED; supported PvP definition check, #43–#52, later 1.2/1.5. |
| `P1-S0-03` | AoE legal targets, auras, encounter replication | `S03-I-TARGET`, `S03-I-VFX` | `S03-C-RAID` | DEFINED / NOT EXECUTED; authored AoE/boss support and #43–#52. |
| `P1-S0-04` | Total NPC count vs active count | `S04-I-DATA`, `S04-I-FULL` | `S04-C-WAKE` | DEFINED / NOT EXECUTED; #43–#51; true dormant future Stage 1.4. |
| `P1-S0-05` | Scene/origin transitions and visual hitch | `S05-I-SCENE`, `S05-I-ORIGIN` | `S05-C-FLOW` | DEFINED / NOT EXECUTED; #44/#46/#49/#52; 1.2/1.6 streaming corrections. |
| `P1-S0-06` | GPU/LOD/foliage/settlement and VFX | `S06-I-GEOMETRY`, `S06-I-VFX` | `S06-C-FOREST`, `S06-C-SETTLEMENT` | DEFINED / NOT EXECUTED; tracked #44 fixture, #49/#52 graphics-enabled client. |
| `P1-S0-07` | Connections/s and authenticated actor identity | `S07-I-AUTH`, `S07-I-SPAWN` | `S07-C-LOGIN` | DEFINED / NOT EXECUTED; #46/#51; reconnect contracts Stage 1.7–1.8. |
| `P1-S0-08` | Resource/identity/timing drift | `S08-I-IDLE`, `S08-I-COMBAT` | `S08-C-SOAK` | DEFINED / NOT EXECUTED; #47–#51, Stage 1.1 tick and 1.7–1.8 durability. |

### 5.1 Existing integration surfaces verified at reviewed source

- [ServerECSManager](../../Assets/Runtime/Server/Core/ServerECSManager.cs) appends Unity Entities systems to the player loop; no PDD-complete fixed authoritative tick exists.
- [ServerActorManager](../../Assets/Runtime/Server/Core/ServerActorManager.cs) includes NPC aggro `Physics.OverlapSphere`, movement validation and GameObject/ECS synchronisation. Idle NPCs are **not** demonstrably dormant.
- [ServerAutoAttackManager](../../Assets/Runtime/Server/Core/ServerAutoAttackManager.cs) executes supported autoattack/chase loops; combat workload requires real spell definitions and cannot be fabricated.
- [ServerInterestManager](../../Assets/Runtime/Server/Core/ServerInterestManager.cs) uses the current 3×3 nearby scene subscriptions, not complete logical-instance range indexing.
- [GameBootstrapper](../../Assets/Runtime/Core/GameBootstrapper.cs) selects server under batch mode; a benchmark client needs explicit #46 configuration rather than silently launching in server mode.
- [ClientWorldManager](../../Assets/Runtime/Client/Core/ClientWorldManager.cs) awaits world scenes and [ClientPositionManager](../../Assets/Runtime/Client/Core/ClientPositionManager.cs) shifts local roots; seamless six-tile routes remain unverified.
- World scenes, prefabs, art and terrain data are excluded in [`.gitignore`](../../.gitignore); benchmark #44 must generate and track deterministic test assets rather than rely on arbitrary local editor assets.

## 6. Telemetry and invariants per measured run

Capture p50/p95/p99/max, sample count, collection unit/phase, error distribution and hardware alongside actual achieved workload. Explicitly separate measured, estimated, unavailable and not-applicable values:

| Category | Evidence needed; relevant profiles |
| --- | --- |
| Server time | ECS system and manager CPU per frame/update (all); **authoritative tick backlog/missed deadlines only after Stage 1.1**, not synthetic zeros (02/03/04/08). |
| Actor population | Requested/achieved P/N, PlayerIDs, ECS entities, GameObjects/NetworkIdentities, current idle/patrol/engaged states, spawn/despawn accounting; dormant only Stage 1.4 (all). |
| AI/spatial | Perception/physics queries, candidate counts, NavMesh and later indexed broad-phase query durations; wake-up spikes (01/03/04/08). |
| Combat | Actual cast requested/accepted/rejected/finished, interrupt, cooldown, aura/DoT/HoT tick, target fan-out, threat, damage and healing authority oracle (02/03/08). |
| Replication/PurrNet | Observer transitions, actual recipients, app versus transport message counts, bytes/retries/RTT when public APIs permit; no invented on-wire bytes (01/02/03/05/07/08). |
| Rendering | Client CPU/render/GPU frame, memory, visible vs authored actors, draw calls/batches versus instances, triangles/materials/lights/VFX/LOD (01/02/03/05/06). |
| Streaming | Addressables load/release latency when observable, loaded/unloaded scenes, in-flight operations, origin shifts, resident memory and frame spikes (01/05/06/07/08). |
| Auth/persistence | Real session/account and actor mapping, login-to-world time, saves/checkpoints and reconnect/force-disconnect state continuity (07/08). |
| Run validity | Git SHA, fixture/content hashes, build flags, recorded CPU/GPU/OS, requested vs achieved rates, phase duration, raw trace pointers and explicit stop reason (all). |

**Required invariants:** no silent combat-state loss or dropped timer event; no unauthorised actor/range/instance visibility; no disappeared persistent actor solely from environment unload; no illegitimate movement; no duplicate authoritative actor on reconnect; no forced-disconnect combat escape; no apparent success from accidentally unloaded actors or unmeasured GPU/transport load.

## 7. Handoff to fixture design and acceptance gate

1. #43 turns the common inputs/outputs, seeds, sample units, missing metric semantics and evidence retention into a versioned schema; this document does **not** create a competing JSON contract.
2. #44 generates source-controlled reproducible terrain/city/forest/settlement tiles and content hashes without modifying ignored production scenes or third-party assets.
3. #45 defines seeded game-legal actor/action/test behaviours and distinguishes data-only from full PurrNet/GO modes.
4. #46–#52 supply exact build profiles, bounded telemetry, headless/real-client and real graphical runners. No unsupported private-PurrNet reflection and no invented client graphics numbers.
5. At each tier run isolated variants first; record missing preconditions, then combined variants and controlled network presets. Stop at first failed/blocked tier, preserve raw evidence, optionally bisect measured load.
6. #55 defines **hardware-specific** baselines and provisional budgets **after** real measurements, with explicit noisy-run repeat policy. This specification makes no pass/fail performance guarantees.

**Issue #42 static acceptance:**

- [ ] Exactly eight stable profile IDs, each with revision/seed, player/NPC distributions, role/activity mix, map and scene context, object density, movement and supported action attempt rates, network/connection conditions, explicit timing, required telemetry, correctness/stop criteria, isolated/combined variants and fixture prerequisites.
- [ ] Provisional tiers clearly engineering probes, not product capacity; no success claimed without measured evidence.
- [ ] PDD semantics retained (100 m origin cells, 256 m streamed tiles, logical instance isolation, server movement authority, minimum 30 s reconnect grace); unimplemented features labelled unavailable/blocked.
- [ ] Links resolve, Markdown whitespace/structure valid, **only this one project-owned Markdown document** changed and zero third-party/test/runtime/Unity asset changes.
- [ ] User reviews every document change, workload realism, asset counts, action rates and scenario gates before removing `needs-validation`.

**Validation limitation:** Documentation creation does not run Unity Editor, PlayMode/EditMode, server load, graphical profiling or PurrNet traffic. GitHub Actions usage is unavailable until **20 November 2026**; local Unity testing is required by the agreed temporary workflow for relevant code changes, with results supplied by the user. Do not mark CI or hardware baselines as passed because this document exists.
