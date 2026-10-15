# Ninth Age — Phase 1.0 Performance Baseline

**Status:** Provisional evidence handoff; **not accepted as a frozen or production baseline**<br>
**Issues:** [#55 — Capture immutable performance baselines and establish acceptance budgets](https://github.com/FrontierDev/ninthage/issues/55), [#56 — Execute repeated performance baseline measurement campaign](https://github.com/FrontierDev/ninthage/issues/56)<br>
**Campaign:** `campaign-20261015T095750417Z-878db4d7`<br>
**Campaign window:** 2026-10-15 10:02:05Z — 10:25:40Z
**Last updated:** 2026-10-15

> **Qualification:** The campaign completed 25 attempts with clean provenance, correct fixture/lifecycle checks and five repeats per selected job. It does not satisfy the complete-profile, portable-retrieval or standalone-build conditions required to sign off a Stage 1.0 production performance baseline. The first Outdoor graphical repeat produced no GPU or render-thread samples and reported zero draw calls/triangles; those metric-specific limitations remain explicit.

## 1. Executive result

The campaign demonstrated that the current test-session harness can repeatedly execute the selected server and graphical workloads, export traceable evidence, and preserve correctness failures as structured results. It established the following useful observations:

- 25 of 25 attempts completed with `COMPLETE-MEASURED`, `MEASUREMENT_COMPLETE`.
- All 25 summaries reported correctness `pass`, with four assertions checked, zero failed assertions and cleanup complete.
- Both data-only server workloads achieved the requested 100 ECS entities and returned fixture state and actor counts to zero after cleanup.
- The activity workload repeatedly produced the expected activity and spawn/despawn invariants.
- The graphical workloads completed all four route phases and achieved their requested object counts.
- Frame, CPU, GPU, draw-call, triangle, memory and harness metrics were exported with units, sample counts and explicit unavailable reasons. No unavailable metric was replaced with zero.

The campaign did **not** establish production capacity, networked-player capacity, standalone server performance, normal PurrNet streaming performance or final acceptance thresholds.

## 2. Evidence identity and protocol

| Field | Recorded value |
| --- | --- |
| Campaign manifest | `Builds/Benchmarks/Results/Campaigns/campaign-20261015T095750417Z-878db4d7/campaign.json`; SHA-256 is recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Evidence index | `Builds/Benchmarks/Results/Campaigns/campaign-20261015T095750417Z-878db4d7/index.json`; SHA-256 is recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Attempt evidence | local / not externally retrievable; run IDs and `artifacts.json` hashes are recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Branch / commit | `dev` / `665abcc51a6761354f0feb9e350e148dc04ba1a3` |
| Source state | `dirty=false`; source SHA-256 `d1c44164274aa355c8a2a0110af462e3b50942f304bb320e6036e071bf228d6b`; diff SHA-256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Unity | `6000.3.10f1` |
| Operating system | Windows 11 `10.0.26200` |
| Hardware | AMD Ryzen 7 5700X 8-Core Processor, 16 logical CPUs; NVIDIA GeForce RTX 4060 Ti; Direct3D12 |
| Graphics settings | `quality_level=0; vsync=0; target_fps=-1; render_pipeline=PC_RPAsset` |
| Dependency identity | Packages SHA-256 `8d45b883217f48705f8b8c06544fc362f7972b391d302786dbde0fd2d8606a90`; lock SHA-256 `933a4d97a9bd17fa89939b99509bdbca107a4e296e70fb2cf3155aab6b96adf9`; PurrNet SHA-256 `e232f713a923f9b079a1f2de34d7730c926af37cc001467fb2fa2a4b4d574941` |
| Workload timing | 2 s ramp / 2 s warmup / 5 s measurement / 1 s recovery |
| Repetition | Five attempts per selected job; seed `424242` |

The run identity, fixture references, fixture hashes and listed artifact hashes were checked against the local files available during the campaign review. The source branch and full commit match the current clean `dev` checkout. The catalogue explicitly does not make the ignored local results retrievable from GitHub.

## 3. Measured workload coverage

| Job | Workload | Fixture / requested population | Repeats | Result |
| --- | --- | --- | ---: | --- |
| `job-20261015T095751713Z-8f6790a3` | `EcsDataOnlyMinimal` | Outdoor / 100 ECS entities / fixture `92bbacf122d17d4f73377bdfd4bf59e494754d7d0d34aae391f2b2bf08552e73` | 5/5 | 100 achieved; cleanup returned to zero |
| `job-20261015T095751724Z-e3c5a1bb` | `EcsDataOnlyActivity` | Outdoor / 100 ECS entities / fixture `92bbacf122d17d4f73377bdfd4bf59e494754d7d0d34aae391f2b2bf08552e73` | 5/5 | 100 achieved; activity and cleanup invariants passed |
| `job-20261015T095751728Z-b46f05ba` | Graphical outdoor | 33 objects / fixture `6d527e46b4b51622b18a46a5e1224161ba78531f0a661dabdb616813fd22b882` | 5/5 | 33 achieved; route 4/4; 97 visible objects |
| `job-20261015T095751743Z-b0dced97` | Graphical forest | 251 objects / fixture `9ad4e51e30c595f6b6a668ce66c9333be71fc16a184640a5f2b4472c8eac60d7` | 5/5 | 251 achieved; route 4/4; 551 visible objects |
| `job-20261015T095751747Z-c1b51b19` | Graphical settlement | 96 objects / fixture `d381512a0382367f08ab470721ccd04d11ae6d5ecf4e06c036a2ad3ff9035605` | 5/5 | 96 achieved; route 4/4; 286 visible objects |

### 3.1 Server correctness and activity observations

The minimal workload reported 100 ECS entities in every measurement. The activity workload reported the same achieved population and, on every run, the following activity/event values:

| Observation | Value |
| --- | ---: |
| Idle actors | 85 |
| Patrolling actors | 4 |
| Combat actors | 11 |
| Wake-surge actors | 10 |
| Spawn events | 5 |
| Despawn events | 5 |
| Total spawn events | 105 |
| Total despawn events | 5 |
| Wake surge is a combat subset | `true` |

The post-cleanup fixture and actor counts returned to zero for both server workloads. These are workload correctness observations, not claims about authenticated players, networked actors or supported population capacity.

### 3.2 Graphical correctness observations

All graphical attempts achieved their requested object count and completed all four route waypoints. The harness reported 97 visible objects for outdoor, 551 for forest and 286 for settlement. `harness.graphical.route_distance` used metres (`m`) and was measured in all 15 graphical attempts. Its p99 is unavailable because each route metric has only 11 retained samples; this is an explicit sample-size limitation, not a missing or fabricated value.

## 4. Performance observations

The tables below follow the campaign comparator convention: each cell is the median of the corresponding per-run statistic over qualified repeats. `N` is the number of qualified repeats for that metric. These values are observations for this exact clean Editor configuration, not approved budgets.

### 4.1 Server metrics

| Workload | Metric | Unit | N | Median run p50 | Median run p95 | Median run p99 |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Minimal | `server.actor_manager.update_duration` | ms | 5 | 0.030 | 0.044 | 0.057 |
| Minimal | `server.ecs.actor_update_duration` | ms | 5 | 0.026 | 0.037 | 0.049 |
| Minimal | `server.actor.full_entity_scan_count` | count | 5 | 200 | 200 | 200 |
| Minimal | `server.memory.total_allocated_bytes` | bytes | 5 | 1,080,087,387 | 1,081,555,068 | 1,081,683,708 |
| Activity | `server.actor_manager.update_duration` | ms | 5 | 0.030 | 0.039 | 0.048 |
| Activity | `server.ecs.actor_update_duration` | ms | 5 | 0.026 | 0.033 | 0.055 |
| Activity | `server.actor.full_entity_scan_count` | count | 5 | 200 | 200 | 200 |
| Activity | `server.memory.total_allocated_bytes` | bytes | 5 | 1,080,660,812 | 1,082,128,520 | 1,082,259,610 |

All selected server metrics met the 95% retained-sample policy in all five repeats. The median-run values above do not hide rare raw-sample spikes: the maximum observed `server.actor_manager.update_duration` was 3.1536 ms in Minimal and 9.9785 ms in Activity, while the corresponding ECS maxima were 0.5174 ms and 0.4033 ms. These are diagnostic sample maxima, not the p99 references used by the review guardrails.

### 4.2 Graphical frame and resource metrics

| Scenario | Metric | Unit | N | Median run p50 | Median run p95 | Median run p99 | Max observed range |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Outdoor | `client.rendered.frame_time` | ms | 5 | 1.982 | 2.335 | 2.670 | 15.325-20.715 |
| Forest | `client.rendered.frame_time` | ms | 5 | 1.998 | 2.392 | 2.804 | 4.456-21.607 |
| Settlement | `client.rendered.frame_time` | ms | 5 | 2.208 | 2.738 | 3.240 | 5.984-24.072 |

| Scenario | Main-thread p99 | Render-thread p99 | GPU p99 | Client allocated-memory median p50 |
| --- | ---: | ---: | ---: | ---: |
| Outdoor | 1.247 ms | 1.021 ms (`N=4`) | 0.749 ms (`N=4`) | 1,317,416,180 bytes |
| Forest | 1.266 ms | 1.024 ms | 0.756 ms | 1,330,793,461 bytes |
| Settlement | 1.449 ms | 1.119 ms | 0.835 ms | 1,342,087,560 bytes |

Fourteen of the fifteen graphical attempts reported the fixture signature of 26 draw calls and 1,930 triangles. The first Outdoor attempt, `run-20261015T101803597Z-5db2d245`, reported measured `0/0` for those two metrics and no GPU or render-thread samples; its rendered-frame, main-thread and memory metrics remain measured. The nonzero signature describes these exact test fixtures, not a general scene-density budget, and the first-Outdoor anomaly must be resolved before promotion to a hard guardrail. `harness.graphical.route_distance` remains p99-unavailable because each route metric has only 11 retained samples.

## 5. Evidence limitations and excluded claims

The campaign reports explicitly identify the following limitations:

1. The source was clean when the campaign was captured. This improves reproducibility provenance but does not establish a production build.
2. The campaign contains no controlled telemetry-enabled versus telemetry-disabled comparison, so collector overhead cannot be separated from workload cost.
3. The selected jobs cover minimal/activity outdoor server work and outdoor/forest/settlement graphical work. Empty, streaming-route, density-sweep and other catalogue profiles were not present, while `excluded_scenarios` was empty. Full Stage 1.0 matrix coverage is therefore not demonstrated.
4. The runs used Editor Play Mode. They cannot establish standalone headless server capacity, authenticated clients, real PurrNet multi-client scale, ordinary world streaming, or production actor/nameplate/VFX density.
5. No profiler capture is listed in the finalized artifact sets. No profiler-based claim is made here.
6. No measured metric fell below the 95% retained-sample policy. However, the first Outdoor repeat has no GPU/render-thread samples and reports a zero draw/triangle signature, so those GPU/render-thread distributions have `N=4/5` and the draw/triangle signature is not yet a clean five-repeat observation.
7. The portable archive is incomplete for the optional `logs/runner.log` artifacts: the historical manifests declare them present, but the original ignored `.log` files are unavailable in this checkout. Their manifests and recorded hashes remain preserved; no replacement log or historical rewrite is valid.

The warnings in the individual reports are part of the evidence and must remain attached to any comparison. In particular, 100 data-only ECS entities must not be described as 100 concurrent authenticated users or 100 full networked NPCs.

## 6. Baseline disposition and next handoff

This document is a useful provisional evidence handoff to #55. It records reproducible workload outcomes and hardware-specific metric observations without silently promoting unsupported claims. It is **not** a signed Stage 1.0 baseline.

Before promotion, rerun or diagnose the first Outdoor graphical repeat so GPU/render-thread and draw/triangle observations are complete, add the controlled telemetry comparison if the runner can support it, document or execute the missing profiles, export and independently verify the archive, and obtain the user's reproducibility/sign-off. The candidate guardrails and comparison rules are recorded separately in [Phase 1.0 Performance Budgets](Phase-1-Performance-Budgets.md).

## 7. Profile coverage and blockers

| Profile | Relevant mode | Status | Exact current capability/blocker | Runnable alternative | Next issue/stage |
| --- | --- | --- | --- | --- | --- |
| `P1-S0-01` | server + rendered client / networked city | `BLOCKED` | Generated city density, authenticated clients, cross-tile interests and required telemetry are unavailable. | `P1-S0-04` ECS data-only; `P1-S0-06` isolated rendering | #43-#52; later Stage 1.2/1.5 |
| `P1-S0-02` | networked server + clients / PvP | `BLOCKED` | Repeatable factions, legal PvP targets and multi-client correctness fixtures are unavailable. | `P1-S0-04`; `P1-S0-06` | #43-#52; Stage 1.2/1.5 |
| `P1-S0-03` | networked server + clients / boss and AoE | `BLOCKED` | Authored boss, legal AoE fan-out and replayable combat fixtures are unavailable. | `P1-S0-04`; `P1-S0-06` | #45, #51/#52 |
| `P1-S0-04` | Editor server / ECS data-only and activity | `SUPPORTED-MEASURED` | The latest five-run campaign measured the existing minimal/activity snapshots; full PurrNet dormant/wake variants remain blocked. | Minimal and activity variants | Stage 1.4 for true dormant/wake simulation |
| `P1-S0-05` | rendered client + streamed world / traversal | `BLOCKED` | Six-tile route, streaming transitions and origin-boundary correctness are not executed by the current harness. | `P1-S0-06` fixed rendered fixtures | #44/#46/#49/#52; Stages 1.2/1.6 |
| `P1-S0-06` | Editor rendered client / geometry, forest, settlement | `SUPPORTED-MEASURED` | The latest five-run campaign measured the existing isolated routes; the first Outdoor repeat has missing GPU/render-thread samples and a zero draw/triangle signature, and the profile does not qualify network density or production streaming. | Outdoor, Forest, Settlement | Resolve the first-Outdoor metric gap; #44/#49/#52 |
| `P1-S0-07` | server + clients / login and reconnect | `BLOCKED` | Safe disposable accounts, real admission and reconnect correctness are unavailable. | `P1-S0-04`; `P1-S0-06` | #46/#51; Stages 1.7-1.8 |
| `P1-S0-08` | Editor server / authoritative soak | `BLOCKED` | Multi-hour churn, persistence, leak and scheduler-backlog observation are not supported. | Short bounded `P1-S0-04` server measurement only | #47-#51; Stage 1.1 and 1.7-1.8 |

The five jobs in the campaign remain five genuine fixtures, not networked/full-actor, streaming, fixed-tick or standalone-capacity claims.

## 8. Run-level variability and guardrail disposition

The comparator now reports every run ID, metric-level valid N, retained/attempted count, coverage, run-level p50/p95/p99 medians, min/max, p50 IQR and coefficient of variation where at least two qualified run values exist. It also lists excluded run IDs and reasons. It never pools frame samples across attempts. This campaign has no retained-coverage rejection; the first Outdoor run remains preserved with explicit `no_measurement_samples` reasons for GPU/render-thread metrics and its measured `0/0` draw/triangle result.

The 10% server and 20% graphical changes are still review alerts, not approved budgets: the latest campaign has one metric-specific four-of-five gap and a first-Outdoor rendering anomaly, and does not establish a stable production noise estimate. Editor-process memory/frame values and per-fixture draw/triangle signatures remain separate from standalone/product-wide budgets.
