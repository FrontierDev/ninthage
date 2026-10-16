# Ninth Age — Phase 1.0 Performance Budgets

**Status:** FORMALISED **PROVISIONAL EDITOR-ONLY** review thresholds; documented but **not signed off by the project owner** and **not production acceptance budgets**  
**Reference:** [Phase 1.0 Performance Baseline](Phase-1-Performance-Baseline.md) — `campaign-20261015T205113129Z-d1d3d540`  
**Source:** `dev @ d22a939232c842a1415bd627c2a6a0be3663ce95`; Unity `6000.3.10f1`; Ryzen 7 5700X + RTX 4060 Ti; Direct3D12 / URP Forward+; clean source  
**Evidence:** [catalogue](Phase-1-Performance-Evidence-Catalogue.md); [#55](https://github.com/FrontierDev/ninthage/issues/55), [#63](https://github.com/FrontierDev/ninthage/issues/63)  
**Updated:** 2026-10-15

> This document fixes *numeric thresholds and evaluation rules* for the **five currently measured Editor fixtures**. The thresholds are provisional engineering review gates only. They do not certify standalone builds, production concurrency/FPS, network throughput or an approved Stage 1 completion. Owner sign-off remains separate from recording this policy.

## 1. Applicability and qualification

The thresholds apply to **equivalent** `P1-S0-04` ECS Minimal/Activity (100 data-only entities) and `P1-S0-06` graphical Outdoor/Forest/Settlement (33/251/96 fixture objects; four-waypoint camera route), all with seed `424242`, 2 s ramp + 2 s warmup + 5 s measurement + 1 s recovery in Unity Editor Play Mode.

The comparator must establish compatible fixture hashes, workload settings, metric IDs/units/scopes/retention policies, Unity and dependency identities (including PurrNet), CPU/GPU/OS/graphics settings, and recorded source identities. Different commits can be compared **only when the comparison explicitly exposes their source/protocol differences**; changed fixtures or measurement protocols require an explicitly approved new reference, not silent pooling. `git.dirty` is provenance/informational, not an automatic rejection.

Each scenario and required metric must have **at least five independent, metric-qualified runs**:
- final `COMPLETE-MEASURED` and `MEASUREMENT_COMPLETE`; ramp, warmup, measurement and recovery complete;
- four correctness checks pass, zero failures and cleanup complete;
- fixed server population/activity or graphical object/route invariants satisfied;
- graphical render-dependent metrics require recorded `rendering.qualification_status=verified`, genuine nonzero draw/triangle statistics and applicable frame/GPU/render-thread probe evidence;
- valid metric observation, scope/unit/identity, **at least 95%** retained/attempted samples and **at least 100 retained samples** for any p99; unavailable metrics stay unavailable;
- mandatory manifest, fixture, run, summary and report evidence available, with required hashes/byte counts valid. Missing a **declared-present optional log** makes archive integrity `INCOMPLETE`, not an otherwise valid metric an invented failure. Present-but-corrupt files retain integrity errors.

An incomplete or unqualified five-run metric does **not** pass or fail a numeric guardrail: it is **PROVISIONAL/UNQUALIFIED**. Do not turn missing observations into zero. Preserve all attempts and error diagnostics.

## 2. Formalised provisional thresholds — Editor server (`P1-S0-04`)

Evaluate on the **median of the five qualified individual run p99 values** for each workload (except the scan invariant). Bytes refer to the **Unity Editor process**, not a standalone server memory allowance.

| Metric and statistic | Minimal reference | Activity reference | Provisional Editor-only review budget |
| --- | ---: | ---: | ---: |
| `server.actor_manager.update_duration`, median-run p99 (ms) | 0.0518 | 0.0498 | **≤ 0.100 ms** |
| `server.ecs.actor_update_duration`, median-run p99 (ms) | 0.0716 | 0.0455 | **≤ 0.250 ms** |
| `server.memory.total_allocated_bytes`, median-run p99 (bytes) | 1,087,251,058 | 1,088,483,029 | **≤ 2,000,000,000 bytes** |
| `server.actor.full_entity_scan_count`, each run p50/p95/p99 (count) | 200/200/200 | 200/200/200 | **Exactly 200/200/200** for this fixture |

The scan value is a **fixture-specific invariant**, not a recommended query frequency or a scalable actor-count target. The memory limit is a deliberately conservative process-level review ceiling; five-second observations are insufficient to make a leak/soak assertion.

## 3. Formalised provisional thresholds — Editor graphical client (`P1-S0-06`)

For duration and GPU metrics, calculate the median of the **five qualified per-run p95 or p99 statistics**, independently for each scenario. Memory is the median of per-run **p50**; do not confuse p50 memory with a peak/GC budget.

| Metric and statistic | Outdoor reference | Forest reference | Settlement reference | Provisional Editor-only review budget |
| --- | ---: | ---: | ---: | ---: |
| `client.rendered.frame_time`, median-run p95 (ms) | 2.6506 | 2.6823 | 2.5791 | **≤ 6.000 ms** |
| `client.rendered.frame_time`, median-run p99 (ms) | 3.0143 | 3.4821 | 3.1237 | **≤ 10.000 ms** |
| `client.main_thread.frame_time`, median-run p99 (ms) | 1.6050 | 1.8642 | 1.6136 | **≤ 5.000 ms** |
| `client.render_thread.frame_time`, median-run p99 (ms) | 1.0442 | 1.0833 | 1.0326 | **≤ 4.000 ms** |
| `client.gpu.frame_time`, median-run p99 (ms) | 0.822272 | 0.824320 | 0.815104 | **≤ 2.500 ms** |
| `client.memory.total_allocated_bytes`, median-run p50 (bytes) | 1,351,172,901 | 1,363,893,340 | 1,375,133,452 | **≤ 2,000,000,000 bytes** |

**Graphical workload signature (diagnostic, not a general cost ceiling):** `26` draw calls / `26` batches / `1,930` triangles in all 15 new runs. Do **not** impose these counts as game-wide limits or claim that all 33/251/96 objects were independently proven rendered. Nonzero stats and full camera routes satisfy the *current* test-owned qualification contract; attribution of draw calls to each fixture object remains unmeasured.

**Tail observation:** Forest repeat p99 rose from `3.0427` to `7.0678 ms`, with main-thread p99 up to `4.3839 ms`; no corresponding similarly sized GPU p99 rise was recorded. These individual run tails remain attached to the baseline rather than discarded. A single per-run outlier does not automatically fail a **median-of-run** guardrail, but material spread triggers manual review.

## 4. Correctness gates, decisions and alert policy

| Gate | Action |
| --- | --- |
| Missing/invalid required evidence, incorrect fixture, lifecycle/correctness failure, incomplete cleanup, invalid population/activity/route | **Exclude affected run** and report explicit reason. Never mark its numeric measurements qualified. |
| Rendered fixture expects visible geometry but 0 draw calls/triangles or probes unavailable | **Render-workload unverified**; exclude render-dependent metric cohort; retain recorded numbers and available non-render evidence. |
| Metric with <95% retention, <100 samples for p99, or <5 qualified repeats | **Unqualified/provisional** for that metric. No substitute or invented p99. |
| Valid comparable five-run metric **above** its numeric guardrail | **Budget review failure for that metric**; report magnitude and per-run distribution; this is not a claim of production failure. |
| Valid comparable five-run metric **at or below** guardrail | **Within provisional Editor budget** for that metric/fixture only. This does not by itself approve Phase 1 as a whole. |
| Metric increase versus a compatible approved baseline >**10% server duration** or >**20% graphical frame/resource** | **Regression review alert**, regardless of whether absolute guardrail is met; investigate noise before assigning cause. |
| Different/unknown graphical focus contexts or recording conditions | Document context. Consistently backgrounded rendering with verified metrics may still be comparable; focus alone is **never** an execution hard-fail. |
| Editor startup, scene restore, queue-settle wall-clock time | **Diagnostic-only**. Exclude from CPU/GPU/frame percentile budgets and do not fail a run based solely on slow Editor transitions. |

**Comparison aggregation:** Use median of qualified per-run p50/p95/p99; report per-run p99, p50 min/max, p50 IQR and coefficient of variation where available. Preserve raw maxima, sample counts and every failed/aborted/outlier attempt. Do not pool frame samples across attempts or silently join runs from incompatible revisions. Comparison against the **superseded** `campaign-20261015T095750417Z-878db4d7` is descriptive only because the instrumentation/measurement protocol changed.

**Context and noise:** Repeatability is assessed from the independent five-run distribution; there is no invented confidence interval. The new campaign's Forest tail variability and Minimal ECS update variance justify retaining the existing 10%/20% review-alert policy. PC workload cannot be controlled or inferred retrospectively from focus alone. The reference campaign recorded `known_foreground` with no focus transitions; subsequent conditions must be compared transparently.

## 5. Scope not yet budgeted

**No numeric Phase 1 budget is established** by this campaign for:

- standalone headless **server frame/tick**, backlog, job scheduling and true full/networked-actor capacity;
- authenticated PurrNet player load, bandwidth, serialization, interest queries or server/client visibility;
- ordinary streamed world/Addressables scene activation/unload, route transitions, origin shifting and hitch budgets;
- real player combat, PvP, AoE, boss scaling and ability simulation under networked load;
- GC allocation **rate**, collection pauses, long-run memory slope, persistence and soak/leak behaviour;
- account admission, login/reconnect, storage write/read latency or multi-machine scaling;
- telemetry enabled versus telemetry disabled overhead; the current 25 runs measured only telemetry-enabled operation.

Do not assign zero budgets or extrapolate synthetic ECS entities to player concurrency. Blocked profiles `P1-S0-01/02/03/05/07/08` remain documented in the [baseline](Phase-1-Performance-Baseline.md). New source/fixture/quality settings require new compatible measurement evidence before broadening these budgets.

## 6. Approval and revision procedure

1. **Provisional policy established in documentation:** the thresholds above are the active **Editor-only engineering review references** for the five recorded fixtures; they are not production sign-off or performance certification.
2. **Project-owner validation/sign-off: pending.** A reviewer must check the exact commit/hardware, five qualifying run files per scenario, run-level tails and current artifact-integrity caveats. This documentation update does not assert that manual review or a telemetry-off comparison was performed.
3. **Future candidate comparison:** use independent runs with unchanged fixture/harness protocol, capture a new immutable campaign ID, report all runs (including failures) and apply the qualification and numeric rules above. Change a reference baseline only with documented approval and source/protocol rationale; never overwrite historical evidence.
4. **Broader Stage 1 approval:** requires supported evidence/credible execution plans for unmeasured profiles and future standalone/networked tests. A pass against these Editor guardrails alone cannot close that gate.

Reference campaign: [committed campaign manifest](https://github.com/FrontierDev/ninthage/blob/c2840577b7d098e7c9118d7f56161721038e7663/Builds/Benchmarks/Results/Campaigns/campaign-20261015T205113129Z-d1d3d540/campaign.json). The committed GitHub archive is **incomplete solely for the declared-present optional runner logs**; this is tracked in the evidence catalogue and does not change the observed numeric percentiles.
