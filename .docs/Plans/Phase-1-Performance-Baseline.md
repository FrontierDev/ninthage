# Ninth Age — Phase 1.0 Performance Baseline

**Status:** Provisional evidence handoff; **not accepted as a frozen or production baseline**<br>
**Issues:** [#55 — Capture immutable performance baselines and establish acceptance budgets](https://github.com/FrontierDev/ninthage/issues/55), [#56 — Execute repeated performance baseline measurement campaign](https://github.com/FrontierDev/ninthage/issues/56)<br>
**Campaign:** `campaign-20261014T184624379Z-d9cbdff6`<br>
**Campaign window:** 2026-10-14 18:46:52Z — 19:07:31Z
**Last updated:** 2026-10-14

> **Qualification:** The campaign produced useful, internally consistent workload and telemetry evidence. It does not satisfy the complete-profile, adequate-coverage, portable-retrieval or standalone-build conditions required to sign off a Stage 1.0 production performance baseline. Dirty working-tree state is recorded provenance only and is not a qualification failure.

## 1. Executive result

The campaign demonstrated that the current test-session harness can repeatedly execute the selected server and graphical workloads, export traceable evidence, and preserve correctness failures as structured results. It established the following useful observations:

- 25 of 25 attempts completed with `COMPLETE-MEASURED`, `MEASUREMENT_COMPLETE`.
- All 25 summaries reported correctness `pass`, with four assertions checked and zero failed assertions.
- The data-only server workloads achieved the requested 100 ECS entities and returned fixture state to zero after cleanup.
- The activity workload repeatedly produced the expected activity and spawn/despawn invariants.
- The graphical workloads completed all four route phases and achieved their requested object counts.
- Frame, CPU, GPU, draw-call, triangle, memory and harness metrics were exported with units, sample counts and explicit unavailable reasons. No missing metric was replaced with zero.

The campaign did **not** establish production capacity, networked-player capacity, standalone server performance, normal PurrNet streaming performance or final acceptance thresholds.

## 2. Evidence identity and protocol

| Field | Recorded value |
| --- | --- |
| Campaign manifest | `Builds/Benchmarks/Results/Campaigns/campaign-20261014T184624379Z-d9cbdff6/campaign.json`; SHA-256 is recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Evidence index | `Builds/Benchmarks/Results/Campaigns/campaign-20261014T184624379Z-d9cbdff6/index.json`; SHA-256 is recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Attempt evidence | local / not externally retrievable; run IDs and `artifacts.json` hashes are recorded in the [tracked evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Branch / commit | `dev` / `9fa2b9afbd254e622482ac93a1f56de6fe5dffb9` |
| Source state | `dirty=true`; source SHA-256 `a54f0c779ac367d4d7558b8f4b9edf735a8c81659e56ce2195c5cadea55e0a62`; diff SHA-256 `182d6414326e2688e240aef6f4dadd6ac3d9275cd0ff31178c8e7ca1104fc9ad` |
| Unity | `6000.3.10f1` |
| Operating system | Windows 11 `10.0.26200` |
| Hardware | AMD Ryzen 7 5700X 8-Core Processor, 16 logical CPUs; NVIDIA GeForce RTX 4060 Ti; Direct3D12 |
| Graphics settings | `quality_level=0; vsync=0; target_fps=-1; render_pipeline=PC_RPAsset` |
| Dependency identity | Packages SHA-256 `8d45b883217f48705f8b8c06544fc362f7972b391d302786dbde0fd2d8606a90`; lock SHA-256 `933a4d97a9bd17fa89939b99509bdbca107a4e296e70fb2cf3155aab6b96adf9`; PurrNet SHA-256 `e232f713a923f9b079a1f2de34d7730c926af37cc001467fb2fa2a4b4d574941` |
| Workload timing | 2 s ramp / 2 s warmup / 5 s measurement / 1 s recovery |
| Repetition | Five attempts per selected job; seed `424242` |

The run identity, fixture references, fixture hashes and listed artifact hashes were checked against the local files available during the campaign review. The source branch and full commit are recorded; dirty state is informational. The catalogue explicitly does not make the ignored local results retrievable from GitHub.

## 3. Measured workload coverage

| Job | Workload | Fixture / requested population | Repeats | Result |
| --- | --- | --- | ---: | --- |
| `job-20261014T184627390Z-89defca9` | `EcsDataOnlyMinimal` | Outdoor / 100 ECS entities / fixture `36c8cb1e46748569bb2c70d2281aad1c6f19be820d869253f8048dec9a89aee1` | 5/5 | 100 achieved; cleanup returned to zero |
| `job-20261014T184627395Z-d00f5949` | `EcsDataOnlyActivity` | Outdoor / 100 ECS entities / fixture `36c8cb1e46748569bb2c70d2281aad1c6f19be820d869253f8048dec9a89aee1` | 5/5 | 100 achieved; activity and cleanup invariants passed |
| `job-20261014T184627395Z-f733c531` | Graphical outdoor | 33 objects / fixture `423612d210e880863d28aab60e7d23467fe40f218da02bf8a44eb42ee22e4456` | 5/5 | 33 achieved; route 4/4; 97 visible objects |
| `job-20261014T184627399Z-7ad8836a` | Graphical forest | 251 objects / fixture `8032ebdd9ca93fa048a1a85a3b9e829eeb0a60edb18aa0f9927bf476d0771376` | 5/5 | 251 achieved; route 4/4; 551 visible objects |
| `job-20261014T184627404Z-74fc733e` | Graphical settlement | 96 objects / fixture `573fadf18178b416e9f91f4ac432eee186bab308109d225305e054a8d255136b` | 5/5 | 96 achieved; route 4/4; 286 visible objects |

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

The tables below follow the campaign comparator convention: each cell is the median of the corresponding per-run statistic over qualified repeats. `N` is the number of qualified repeats for that metric. These values are observations for this exact dirty Editor configuration, not approved budgets.

### 4.1 Server metrics

| Workload | Metric | Unit | N | Median run p50 | Median run p95 | Median run p99 |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Minimal | `server.actor_manager.update_duration` | ms | 5 | 0.030 | 0.038 | 0.050 |
| Minimal | `server.ecs.actor_update_duration` | ms | 5 | 0.026 | 0.033 | 0.044 |
| Minimal | `server.actor.full_entity_scan_count` | count | 5 | 200 | 200 | 200 |
| Minimal | `server.memory.total_allocated_bytes` | bytes | 5 | 1,844,680,855 | 1,846,015,767 | 1,846,132,887 |
| Activity | `server.actor_manager.update_duration` | ms | 4/5 | 0.031 | 0.038 | 0.048 |
| Activity | `server.ecs.actor_update_duration` | ms | 4/5 | 0.026 | 0.032 | 0.040 |
| Activity | `server.actor.full_entity_scan_count` | count | 4/5 | 200 | 200 | 200 |
| Activity | `server.memory.total_allocated_bytes` | bytes | 4/5 | 1,844,950,508 | 1,846,355,348 | 1,846,498,764 |

The activity repeat `run-20261014T184951561Z-61bba0f5` completed correctly but fell below the measurement-coverage policy for several server metrics. Those observations are unavailable and are excluded from the `N=4/5` distributions; they must not be treated as zeros.

### 4.2 Graphical frame and resource metrics

| Scenario | Metric | Unit | N | Median run p50 | Median run p95 | Median run p99 | Max observed range |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Outdoor | `client.rendered.frame_time` | ms | 5 | 2.050 | 2.490 | 2.937 | 20.681–23.514 |
| Forest | `client.rendered.frame_time` | ms | 4/5 | 2.433 | 3.566 | 4.312 | 8.032–65.632 |
| Settlement | `client.rendered.frame_time` | ms | 4/5 | 2.048 | 2.724 | 3.821 | 21.144–25.093 |

| Scenario | Main-thread p99 | Render-thread p99 | GPU p99 | Client allocated-memory median p50 |
| --- | ---: | ---: | ---: | ---: |
| Outdoor | 1.455 ms | 1.015 ms | 0.774 ms | 1,874,179,797 bytes |
| Forest | 2.181 ms (`N=4`) | 1.790 ms | 1.141 ms | 1,887,811,252 bytes (`N=4`) |
| Settlement | 1.887 ms (`N=4`) | 1.200 ms | 1.121 ms | 1,893,227,185 bytes (`N=4`) |

Draw-call and triangle observations were stable for the qualified samples: 26 draw calls and 1,930 triangles. They describe these exact test fixtures; they are not a general scene-density budget. The forest repeat `run-20261014T185639993Z-84c311b3` and settlement repeat `run-20261014T190708296Z-84263c64` had below-policy coverage for the affected frame/render/memory metrics. The runs remain preserved, but those metric observations are not qualified.

## 5. Evidence limitations and excluded claims

The campaign reports explicitly identify the following limitations:

1. The source was dirty when the campaign was captured. This is retained as informational provenance and does not block qualification by itself.
2. The campaign contains no controlled telemetry-enabled versus telemetry-disabled comparison, so collector overhead cannot be separated from workload cost.
3. The selected jobs cover minimal/activity outdoor server work and outdoor/forest/settlement graphical work. Empty, streaming-route, density-sweep and other catalogue profiles were not present, while `excluded_scenarios` was empty. Full Stage 1.0 matrix coverage is therefore not demonstrated.
4. The runs used Editor Play Mode. They cannot establish standalone headless server capacity, authenticated clients, real PurrNet multi-client scale, ordinary world streaming, or production actor/nameplate/VFX density.
5. No profiler capture is listed in the finalized artifact sets. No profiler-based claim is made here.
6. Three repeats have metric-level coverage rejection. The harness correctly marked affected values unavailable, but the campaign does not provide five qualified observations for every reported metric.

The warnings in the individual reports are part of the evidence and must remain attached to any comparison. In particular, 100 data-only ECS entities must not be described as 100 concurrent authenticated users or 100 full networked NPCs.

## 6. Baseline disposition and next handoff

This document is a useful provisional evidence handoff to #55. It records reproducible workload outcomes and hardware-specific metric observations without silently promoting unsupported claims. It is **not** a signed Stage 1.0 baseline.

Before promotion, rerun the currently supported matrix with the recorded branch/full SHA, five qualified repeats per metric, add the controlled telemetry comparison if the runner can support it, document or execute the missing profiles, export and independently verify the archive, and obtain the user's reproducibility/sign-off. The candidate guardrails and comparison rules are recorded separately in [Phase 1.0 Performance Budgets](Phase-1-Performance-Budgets.md).

## 7. Profile coverage and blockers

| Profile | Relevant mode | Status | Exact current capability/blocker | Runnable alternative | Next issue/stage |
| --- | --- | --- | --- | --- | --- |
| `P1-S0-01` | server + rendered client / networked city | `BLOCKED` | Generated city density, authenticated clients, cross-tile interests and required telemetry are unavailable. | `P1-S0-04` ECS data-only; `P1-S0-06` isolated rendering | #43-#52; later Stage 1.2/1.5 |
| `P1-S0-02` | networked server + clients / PvP | `BLOCKED` | Repeatable factions, legal PvP targets and multi-client correctness fixtures are unavailable. | `P1-S0-04`; `P1-S0-06` | #43-#52; Stage 1.2/1.5 |
| `P1-S0-03` | networked server + clients / boss and AoE | `BLOCKED` | Authored boss, legal AoE fan-out and replayable combat fixtures are unavailable. | `P1-S0-04`; `P1-S0-06` | #45, #51/#52 |
| `P1-S0-04` | Editor server / ECS data-only and activity | `SUPPORTED-MEASURED` | The five-run campaign measured the existing minimal/activity snapshots; full PurrNet dormant/wake variants remain blocked. | Minimal and activity variants | Five qualified repeats per budgeted metric; Stage 1.4 for true dormant/wake |
| `P1-S0-05` | rendered client + streamed world / traversal | `BLOCKED` | Six-tile route, streaming transitions and origin-boundary correctness are not executed by the current harness. | `P1-S0-06` fixed rendered fixtures | #44/#46/#49/#52; Stages 1.2/1.6 |
| `P1-S0-06` | Editor rendered client / geometry, forest, settlement | `SUPPORTED-MEASURED` | The five-run campaign measured the existing isolated routes; it does not qualify network density or production streaming. | Outdoor, Forest, Settlement | Five qualified repeats per budgeted metric; #44/#49/#52 |
| `P1-S0-07` | server + clients / login and reconnect | `BLOCKED` | Safe disposable accounts, real admission and reconnect correctness are unavailable. | `P1-S0-04`; `P1-S0-06` | #46/#51; Stages 1.7-1.8 |
| `P1-S0-08` | Editor server / authoritative soak | `BLOCKED` | Multi-hour churn, persistence, leak and scheduler-backlog observation are not supported. | Short bounded `P1-S0-04` server measurement only | #47-#51; Stage 1.1 and 1.7-1.8 |

The five jobs in the campaign remain five genuine fixtures, not networked/full-actor, streaming, fixed-tick or standalone-capacity claims.

## 8. Run-level variability and guardrail disposition

The comparator now reports every run ID, metric-level valid N, retained/attempted count, coverage, run-level p50/p95/p99 medians, min/max, p50 IQR and coefficient of variation where at least two qualified run values exist. It also lists excluded run IDs and reasons. It never pools frame samples across attempts. The three named coverage-rejected attempts remain preserved and excluded only for the affected metrics.

The 10% server and 20% graphical changes are still review alerts, not approved budgets: the previous campaign has only four qualified observations for affected metrics and does not establish a stable noise estimate. Editor-process memory/frame values and per-fixture draw/triangle signatures remain separate from standalone/product-wide budgets.
