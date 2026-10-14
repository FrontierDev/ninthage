# Ninth Age — Phase 1.0 Performance Budgets

**Status:** Provisional review guardrails; **not approved acceptance budgets**<br>
**Evidence source:** [Phase 1.0 Performance Baseline](Phase-1-Performance-Baseline.md)<br>
**Issues:** [#55](https://github.com/FrontierDev/ninthage/issues/55), [#56](https://github.com/FrontierDev/ninthage/issues/56)<br>
**Last updated:** 2026-10-14

> The campaign was internally consistent but was captured in Editor Play Mode, omitted part of the runnable profile matrix, lacked a telemetry overhead comparison and had three coverage-rejected repeats. Dirty state is recorded provenance only; it is not a qualification veto. The thresholds below are therefore review guardrails for the exact harness fixtures, not production capacity promises and not a basis for closing Stage 1.0.

## 1. Scope and qualification rules

These guardrails apply only when all of the following match the evidence set:

- same workload, fixture hash, seed, Unity version, package/PurrNet identity, hardware and graphics settings;
- recorded branch and full immutable HEAD commit; `git.dirty` and diff identity are informational and must remain visible;
- at least five independent qualified repeats for every metric being evaluated;
- complete ramp, warmup, measurement and recovery phases;
- correctness passes, exact population/route invariants hold, and teardown returns to the captured baseline;
- measurement coverage meets policy and no unavailable metric is converted to zero;
- comparison uses the comparator's median-of-run p50/p95/p99 distributions, not a single best run.

A metric with fewer than five qualified repeats remains provisional. A p99 with fewer than 100 retained samples is unavailable by design and cannot be substituted or inferred.

## 2. Hard correctness gates

These are vetoes, not performance thresholds:

| Gate | Required observation |
| --- | --- |
| Attempt status | `COMPLETE-MEASURED` and `MEASUREMENT_COMPLETE` |
| Correctness | `pass`; four checks; zero failed checks |
| Minimal/activity population | 100 requested and 100 achieved; post-cleanup fixture and actor counts return to zero |
| Activity invariants | idle 85, patrolling 4, combat 11, wake surge 10; five spawn and five despawn events; wake surge is a combat subset |
| Graphical route | Four of four waypoints complete; requested object count achieved |
| Evidence | Required JSON, report and artifact manifest present and hash-consistent; bounded raw trace and logs are recorded when emitted |

Failure of a hard gate makes the run ineligible for comparison, even if percentile values exist.

## 3. Candidate same-fixture review guardrails

The current campaign reference is the median of qualified run-level statistics. The proposed limits are rounded review thresholds with modest headroom over the observed reference; they are not calibrated production budgets.

### 3.1 Server

| Metric | Current campaign reference | Candidate review guardrail | Scope and rationale |
| --- | ---: | ---: | --- |
| `server.actor_manager.update_duration` p99 | 0.050 ms minimal; 0.048 ms activity | ≤ 0.100 ms | Exact P1-S0-04 Editor workload; approximately 2× the observed median-run p99. |
| `server.ecs.actor_update_duration` p99 | 0.044 ms minimal; 0.040 ms activity | ≤ 0.250 ms | Keeps the isolated minimal-run p99 outlier visible while allowing review headroom; not a tick budget. |
| `server.memory.total_allocated_bytes` p99 | 1,846,132,887 bytes minimal; 1,846,498,764 bytes activity | ≤ 2,000,000,000 bytes | Editor-process snapshot guardrail only. Confirm metric semantics and repeat in a new recorded campaign before treating it as a memory budget. |
| `server.actor.full_entity_scan_count` p50/p95/p99 | 200 / 200 / 200 | 200 for this fixture | Fixture-specific invariant, not a general actor-count scaling limit. |

### 3.2 Graphical client

| Metric | Highest qualified campaign reference | Candidate review guardrail | Scope and rationale |
| --- | ---: | ---: | --- |
| `client.rendered.frame_time` p95 | 3.566 ms (forest) | ≤ 6.000 ms | Exact Editor route fixtures; allows review headroom without claiming a display-FPS target. |
| `client.rendered.frame_time` p99 | 4.312 ms median run; 7.627 ms highest individual qualified p99 | ≤ 10.000 ms | Tail guardrail for the measured route; max-frame spikes remain diagnostic and are not hidden. |
| `client.main_thread.frame_time` p99 | 2.181 ms (forest) | ≤ 5.000 ms | Same-hardware Editor review guardrail. |
| `client.render_thread.frame_time` p99 | 1.790 ms (forest) | ≤ 4.000 ms | Same-hardware Editor review guardrail. |
| `client.gpu.frame_time` p99 | 1.141 ms (forest) | ≤ 2.500 ms | Same-hardware Editor review guardrail. |
| `client.memory.total_allocated_bytes` median p50 | 1,893,227,185 bytes (settlement) | ≤ 2,000,000,000 bytes | Editor-process snapshot guardrail; not a standalone-client memory ceiling. |
| Draw calls / triangles | 26 / 1,930 for qualified samples | Preserve the fixture signature | A changed value indicates fixture or rendering-content drift; it is not a universal scene budget. |

These thresholds must be reviewed after a new qualifying campaign. The graphical values are especially sensitive to Editor state and fixture coverage; the forest and settlement frame-time/memory rows currently have only four qualified repeats.

## 4. Comparison and noise policy

1. Compare only equivalent metric identity, unit, source, scope, retention policy, fixture hash, hardware and source/dependency identity. Different branches/commits are allowed comparison cohorts when explicitly reviewed; they are not automatically defective.
2. Use five independent repeats as the minimum. Report qualified count, median run p50/p95/p99, p50 min/max and p50 IQR. Preserve every failed, interrupted and outlier run.
3. Treat a candidate increase above 10% in server duration metrics or above 20% in graphical frame/resource metrics as a review alert, not an automatic failure. The wider graphical alert reflects the observed run-to-run spread and must be tightened only after more qualified repeats establish lower noise.
4. A hard correctness failure, coverage rejection, missing required artifact, fixture mismatch or unsupported execution mode overrides any apparently acceptable percentile value. Dirty/diff differences alone do not.
5. A p99 that is unavailable because the sample count is below policy remains unavailable. Do not compare it as zero, copy it from another run or replace it with p95.
6. A new approved baseline requires a new immutable campaign identity and a documented reason: source change, fixture revision, dependency/hardware change or an explicitly approved measurement-protocol change. Existing artifacts are never overwritten.

The current campaign cannot produce a before/after regression result because it contains only one candidate set and no approved baseline. The guardrails above are intended to make the next equivalent campaign reviewable, not to retroactively certify this one.

## 5. Missing budgets and required follow-up

The following budgets remain intentionally unapproved until the required evidence exists:

- authenticated client/network traffic, PurrNet visibility and serialization;
- standalone headless server tick time and backlog under real clients;
- streaming scene load/unload latency and route-transition spikes;
- GC allocation rate and long-running memory growth;
- persistence, login/reconnect and multi-machine capacity;
- progressive population tiers and concentrated-versus-distributed actor comparisons;
- telemetry-enabled versus telemetry-disabled overhead.

The next campaign should retain the current evidence, execute the missing currently runnable repeats through the make-up action, resolve or explicitly document the coverage shortfalls, perform the offline archive/hash verification procedure in the evidence catalogue, and obtain manual reproducibility approval before these guardrails are promoted to acceptance budgets. Telemetry-off overhead remains unavailable until a safe same-workload off-channel is implemented and measured; no fabricated off values are permitted.
