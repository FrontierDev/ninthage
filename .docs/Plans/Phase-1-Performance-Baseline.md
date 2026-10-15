# Ninth Age — Phase 1.0 Performance Baseline

**Status:** PROVISIONAL EDITOR-ONLY REFERENCE — captured and documented; not a standalone or production-capacity baseline; manual approval pending  
**Campaign:** `campaign-20261015T205113129Z-d1d3d540`  
**Benchmark source:** `dev @ d22a939232c842a1415bd627c2a6a0be3663ce95` (clean)  
**Immutable evidence reference:** `c2840577b7d098e7c9118d7f56161721038e7663` (the commit containing this campaign's archived files; distinct from benchmark source)  
**Campaign window:** 2026-10-15 20:51:56Z — 21:04:46Z (UTC)  
**Related:** [#55](https://github.com/FrontierDev/ninthage/issues/55), [#56](https://github.com/FrontierDev/ninthage/issues/56), [#63](https://github.com/FrontierDev/ninthage/issues/63), [performance budgets](Phase-1-Performance-Budgets.md), [evidence catalogue](Phase-1-Performance-Evidence-Catalogue.md)  
**Updated:** 2026-10-15

> This document **supersedes** the earlier `campaign-20261015T095750417Z-878db4d7` campaign as the reference for these five Editor fixtures. The earlier campaign may be removed by the owner; its findings are not mixed into the new five-run cohorts. The budgets remain *provisional Editor-only review thresholds*, not evidence of a production MMORPG's playable capacity.

## 1. Executive outcome

- **25/25 scheduled attempts** in five five-repeat jobs finished `COMPLETE-MEASURED`, with stop reason `MEASUREMENT_COMPLETE`. The persisted campaign lists no failed, skipped, interrupted, retried or additional attempts. This statement applies to the *archived campaign inventory*, not to earlier development/test executions.
- **25/25** reported correctness `pass` (four checks, zero failures), all required phases complete and cleanup successful.
- **10/10 server runs** achieved 100 data-only ECS entities and returned fixture/actor counts to zero after cleanup. Activity fixtures repeated the intended idle/patrol/combat/wake/spawn/despawn counts.
- **15/15 graphical runs** achieved the requested scene object counts, completed the four-waypoint camera route, reported nonzero draw/triangle statistics, and were marked `rendering.qualification_status = verified`. Unlike the superseded campaign, no graphical run has an absent GPU or render-thread measurement.
- All 25 measurement phases took approximately **5.001–5.004 s** against the configured 5 s, with no detected pause gap over one second. Focus was `known_foreground` in all runs, with zero reported transitions. No measured metric fell below 95% retained/attempted coverage.
- The five selected workloads satisfy the **provisional** thresholds in the [budget policy](Phase-1-Performance-Budgets.md), evaluated against the median of qualified *run-level* statistics, not pooled samples.

**Boundary:** These are Unity **Editor Play Mode** measurements for isolated test-owned workloads. No authenticated player throughput, standalone headless capacity, normal PurrNet streamed-world traversal, PvP/boss scaling, production NPC density, login/reconnect or soak capability was measured.

## 2. Evidence identity and execution protocol

| Field | Evidence |
| --- | --- |
| Campaign and index | [committed campaign manifest](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/campaign.json); [committed campaign index](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/index.json) |
| Attempt files | 25 tracked `run-*/` directories with `fixture.json`, `run.json`, `summary.json`, `report.md`, `artifacts.json` and `samples.ndjson`; see [catalogue](Phase-1-Performance-Evidence-Catalogue.md) |
| Benchmark commit / branch | `d22a939232c842a1415bd627c2a6a0be3663ce95` / `dev` |
| Source state | `dirty=false`; source SHA-256 `5ecd7ffe55cd3296642fa8d7d260466411aab8d2884d3f8296b054e25b8e8310`; diff SHA-256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| Package identity | manifest SHA-256 `8d45b883217f48705f8b8c06544fc362f7972b391d302786dbde0fd2d8606a90`; lock SHA-256 `933a4d97a9bd17fa89939b99509bdbca107a4e296e70fb2cf3155aab6b96adf9` |
| PurrNet identity | SHA-256 `e232f713a923f9b079a1f2de34d7730c926af37cc001467fb2fa2a4b4d574941` |
| Unity / OS | `6000.3.10f1` / Windows 11 `10.0.26200` |
| CPU / GPU | AMD Ryzen 7 5700X (16 logical CPUs); NVIDIA GeForce RTX 4060 Ti; Direct3D12 |
| Graphics settings | `quality_level=0;vsync=0;target_fps=-1;render_pipeline=PC_RPAsset` |
| Fixture seed | `424242`, recorded per job and in fixture artifacts |
| Timing | 2 s ramp / 2 s warmup / **5 s measured** / 1 s recovery (graphical warmup/measurement configured at 2/5 s) |
| Repetitions | Five independent sequential executions per selected fixture; exact run IDs linked from catalogue |
| Metric retention | All selected budget metrics have five qualified per-run p99 values; route-only interval metrics have ~11 samples/run and no p99, by policy |
| Archive integrity | **INCOMPLETE on GitHub**: all 25 optional `logs/runner.log` entries are declared present but untracked (`*.log` ignored). Required tracked artifacts are present with indexed byte counts matching repository objects; cryptographic digest re-verification is not claimed. See catalogue. |

The campaign's original `index.json` reports `artifact_index_status=verified` based on evidence available when the index was produced. That is not a substitute for validating a *fresh clone*, which currently lacks declared-present optional logs. Missing optional logs are **not** converted into missing measurements or a correctness failure.

## 3. Workload and correctness coverage

| Workload | Job ID | Requested population | Enabled renderers reported | Execution |
| --- | --- | --- | ---: | --- |
| ECS Minimal (`P1-S0-04`) | `job-20261015T205114351Z-68cde3a6` | 100 data-only ECS entities | — | 5/5 complete |
| ECS Activity (`P1-S0-04`) | `job-20261015T205114351Z-dd7d0a2f` | 100 data-only ECS entities | — | 5/5 complete |
| Graphical Outdoor (`P1-S0-06`) | `job-20261015T205114353Z-112ad139` | 33 fixture objects | 97 | 5/5 complete |
| Graphical Forest (`P1-S0-06`) | `job-20261015T205114357Z-013e671e` | 251 fixture objects | 551 | 5/5 complete |
| Graphical Settlement (`P1-S0-06`) | `job-20261015T205114360Z-d346599b` | 96 fixture objects | 286 | 5/5 complete |

All server runs recorded 100 ECS entities at the end of measurement and zero remaining fixture/actor entities after cleanup. **ECS Activity** recorded, in each of five repeats: 85 idle; 4 patrolling; 11 combat; 10 wake-surge (included in combat); 5 measurement spawn events; 5 despawn events; 105 total spawns including ramp; 5 total despawns.

Each graphical run completed route 4/4. The graphical summary describes 33 Outdoor, 251 Forest and 96 Settlement scene objects, with 97/551/286 enabled renderers counted in their loaded scenes. **Enabled renderer counts are not equivalent to confirmed draw contribution by each object.** All 15 graphical traces reported `26` draw calls, `26` batches and `1,930` triangles (p50/p95/p99 and sampled minimum/maximum); these are recorded **test-fixture signatures**, not general graphical-density or scene-visibility budgets. The qualification test confirms nonzero rendering statistics, not independent attribution of every fixture renderer.

## 4. Measured reference values

Aggregation rule: **take the median of the five individual run-level p50, p95 and p99 values**, independently for each metric. Do not pool all frames into one percentile. All budget rows below have qualified **N = 5**; the units are those emitted by the summary metrics.

### 4.1 Server — `P1-S0-04`, 100 data-only ECS entities

| Workload / metric | N | Median run p50 | Median run p95 | Median run p99 | Largest individual sample |
| --- | ---: | ---: | ---: | ---: | ---: |
| Minimal / `server.actor_manager.update_duration` (ms) | 5 | 0.0306 | 0.0422 | **0.0518** | 0.2173 |
| Minimal / `server.ecs.actor_update_duration` (ms) | 5 | 0.0264 | 0.0351 | **0.0716** | 0.8601 |
| Minimal / `server.actor.full_entity_scan_count` (count) | 5 | 200 | 200 | **200** | 200 |
| Minimal / `server.memory.total_allocated_bytes` (bytes) | 5 | 1,085,722,738 | 1,087,127,218 | **1,087,251,058** | 1,094,024,536 |
| Activity / `server.actor_manager.update_duration` (ms) | 5 | 0.0307 | 0.0384 | **0.0498** | 3.1462 |
| Activity / `server.ecs.actor_update_duration` (ms) | 5 | 0.0260 | 0.0329 | **0.0455** | 0.2240 |
| Activity / `server.actor.full_entity_scan_count` (count) | 5 | 200 | 200 | **200** | 200 |
| Activity / `server.memory.total_allocated_bytes` (bytes) | 5 | 1,086,829,335 | 1,088,313,533 | **1,088,483,029** | 1,088,929,311 |

Frame/operation percentiles describe work **inside** the measurement window; a large individual raw-sample maximum is retained for diagnosis rather than substituted for a run p99. The maximum Actor Manager Activity sample was **3.1462 ms**, while that workload's median-run p99 was **0.0498 ms**.

### 4.2 Graphical client — `P1-S0-06`

| Scenario / metric | N | Median run p50 | Median run p95 | Median run p99 | Largest individual sample |
| --- | ---: | ---: | ---: | ---: | ---: |
| Outdoor / `client.rendered.frame_time` (ms) | 5 | 2.1576 | 2.6506 | **3.0143** | 24.2804 |
| Forest / `client.rendered.frame_time` (ms) | 5 | 2.1740 | 2.6823 | **3.4821** | 21.2869 |
| Settlement / `client.rendered.frame_time` (ms) | 5 | 2.1419 | 2.5791 | **3.1237** | 24.2834 |

| Scenario | Main-thread p99 (ms) | Render-thread p99 (ms) | GPU p99 (ms) | Client allocated memory, median run p50 (bytes) |
| --- | ---: | ---: | ---: | ---: |
| Outdoor | 1.6050 | 1.0442 | 0.822272 | 1,351,172,901 |
| Forest | 1.8642 | 1.0833 | 0.824320 | 1,363,893,340 |
| Settlement | 1.6136 | 1.0326 | 0.815104 | 1,375,133,452 |

All 15 graphical attempts supplied >2,100 measured frame samples/run and valid GPU/render-thread measurements; no graphical metric above uses a missing-sample substitute. `harness.graphical.route_distance` is measured with approximately 11 retained samples/run, so its **p99 is legitimately unavailable** (<100 samples) and is not used as a budget percentile.

## 5. Variability, time domains, and interpretation

- **Forest frame-time tail:** five individual run p99 values **3.0427, 3.2658, 3.4821, 4.5349, 7.0678 ms**, versus median frame-time p50 values near 2.15–2.24 ms. Forest main-thread p99 similarly reaches 4.3839 ms in repeat 5. Corresponding GPU p99 values remain around 0.82–0.88 ms. The cause is **not established**; Editor/system contention and scheduling are possible but unproven. Keep the outlier visible, not silently excluded.
- **ECS Minimal actor-update tail:** individual p99 values **0.0803, 0.0426, 0.0716, 0.0425, 0.0843 ms**. This is a reproducibility/noise observation, not proof of a regression against the obsolete pre-#63 protocol.
- **Measurement vs Editor overhead:** the measurement windows are ~5 s. Individual queue/orchestration durations were roughly **20–29 s**, including Play Mode startup, domain reload and scene restoration, and **are not game-workload CPU/GPU time**. Some startup duration fields are `null` because a monotonic clock domain cannot safely be carried through Editor reload; their UTC timestamps remain available. Do not treat these nulls as zero or as evidence that Editor startup took no time.
- **Focus:** `known_foreground` for all 25, zero recorded transitions, `run_in_background=true`. This gives comparable recorded conditions **within this campaign**, but does not measure or rule out competing CPU/GPU work elsewhere on the PC.

## 6. Coverage gaps and excluded capacity claims

The source campaign records all eight Phase 1 profile statuses; only `P1-S0-04` and `P1-S0-06` have supported measurements:

| Profile | Status | Capability or blocker | Runnable alternative | Next step |
| --- | --- | --- | --- | --- |
| `P1-S0-01` | `BLOCKED` | generated city density, real authenticated clients, cross-tile interests and required telemetry are not available in the current runner. | P1-S0-04 ECS data-only or P1-S0-06 isolated rendered geometry. | #43-#52; later Stage 1.2/1.5 client and world-interest work. |
| `P1-S0-02` | `BLOCKED` | repeatable faction, legal PvP target and multi-client correctness fixtures are not available. | P1-S0-04 server data-only or P1-S0-06 rendered geometry. | #43-#52 and later Stage 1.2/1.5 combat and network qualification. |
| `P1-S0-03` | `BLOCKED` | authored boss encounter, legal AoE target fan-out and replayable combat fixtures are not available. | P1-S0-04 server data-only or P1-S0-06 rendered geometry. | #45 and #51/#52 content and correctness verification. |
| `P1-S0-04` | `SUPPORTED-MEASURED` | The existing runner supports ECS data-only minimal/activity snapshots; full PurrNet dormant and wake variants remain blocked. | EcsDataOnlyMinimal and EcsDataOnlyActivity are the current runnable variants. | Run five qualified repeats for each required metric; Stage 1.4 for true dormant/wake simulation. |
| `P1-S0-05` | `BLOCKED` | the current harness does not execute the required six-tile route, streaming transitions and origin-boundary correctness contract. | P1-S0-06 fixed Outdoor/Forest/Settlement rendered fixtures without a streaming claim. | #44/#46/#49/#52 and Stages 1.2/1.6 streaming corrections. |
| `P1-S0-06` | `SUPPORTED-MEASURED` | The existing graphical fixture runner supports isolated rendered Outdoor, Forest and Settlement routes; it does not qualify network density or production streaming. | Outdoor, Forest and Settlement are the current runnable variants. | Run five qualified repeats for each required metric; #44/#49/#52 for production-density and client telemetry expansion. |
| `P1-S0-07` | `BLOCKED` | safe disposable account generation, real client admission and reconnect correctness are not available in the current runner. | P1-S0-04 server data-only or P1-S0-06 rendered geometry. | #46/#51 and Stages 1.7-1.8 reconnect and durable-session work. |
| `P1-S0-08` | `BLOCKED` | the current harness has no compliant multi-hour churn, persistence, leak and scheduler-backlog observation contract. | P1-S0-04 bounded short server measurement only; it is not a soak or production-capacity claim. | #47-#51; Stage 1.1 scheduler telemetry and Stages 1.7-1.8 durability. |

There is **no** measured budget yet for network visibility/serialization throughput, login or reconnect rate, real-client combat density, standalone headless server tick/backlog, streaming scene transfer, persistence, soak/leaks or telemetry-off overhead. No measured observations from other workloads are substituted for these missing profiles.

## 7. Baseline disposition and next actions

**Established for provisional Editor-only comparison:** the immutable evidence identity, five-run references, qualification policy and thresholds in the linked [budget document](Phase-1-Performance-Budgets.md). Later Editor campaigns can compare compatible fixtures/hardware/protocols against this documented reference, keeping changes in source revision explicit. These are **not approved production limits**.

**Still required for a portable complete evidence bundle:** add the original optional runner logs if available (do not fabricate them) or keep the archive explicitly `INCOMPLETE`. See catalogue for the offline hash procedure. This limitation does not invalidate the existing required summary files.

**Still required for broader sign-off:** user review of this documented provisional policy, controlled collector-overhead comparison when implemented, the blocked profile execution plans and future ordinary client/headless/networked benchmarks. No further 25-repeat Editor campaign is necessary solely to document the existing results.
