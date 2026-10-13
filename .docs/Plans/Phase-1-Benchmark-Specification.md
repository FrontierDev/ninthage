# Ninth Age — Phase 1.0 Benchmark Specification

**Status:** Proposed data/evidence contract; requires user validation of [Issue #43](https://github.com/FrontierDev/ninthage/issues/43).
**Contract revision:** `benchmark-fixture/v1`, `benchmark-run/v1`, `benchmark-summary/v1`, `benchmark-sample/v1`, `benchmark-comparison/v1`.
**Reviewed source:** `dev` at `54505978d613833e0a0083fc68f60c138dcf786d` (13 October 2026).
**Change scope:** Documentation only; zero benchmark implementations or benchmark result claims.

## 1. Authority and purpose

This is the canonical machine-readable contract **for future Phase 1.0 benchmark tooling**, not an implemented parser, generator, profiler, transport integration, test suite or capacity claim. The scenario workloads and engineering-only tiers come from [Phase 1 Performance Stress Profiles](Phase-1-Performance-Stress-Profiles.md); subsystem ownership and semantics from [Performance and Simulation Architecture](../Performance-and-Simulation-Architecture.md). Execution order is defined in the [Phase 1 Implementation Plan](Phase-1-Implementation-Plan.md) and [Project Roadmap](Project-Roadmap.md). Follow [MMORPG Master PDD](../MMORPG-Master-PDD.md), [C# Architecture](../CSharp-Architecture.md), [C# Style Conventions](../CSharp-Style-Conventions.md), [GitHub Issue and Workflow Guide](../GitHub-Issue-and-Workflow-Guide.md) and repository `AGENTS.md`. Third-party source/assets and metadata remain read-only.

[#44](https://github.com/FrontierDev/ninthage/issues/44) builds tracked world fixtures, [#45](https://github.com/FrontierDev/ninthage/issues/45) seeded actors/actions, [#46](https://github.com/FrontierDev/ninthage/issues/46) build/startup, [#47–#50](https://github.com/FrontierDev/ninthage/issues/47) telemetry, [#51](https://github.com/FrontierDev/ninthage/issues/51) load runner, [#52](https://github.com/FrontierDev/ninthage/issues/52) graphical harness and [#55](https://github.com/FrontierDev/ninthage/issues/55) measured baselines/budgets.

**Normative interpretation:** "MUST" is required for a conforming implementation. "MAY" is optional and cannot substitute for a required evidence item. A syntactically valid manifest is **not** runnable unless every referenced asset, definition, feature and resource passes preflight; do not replace absent input with an empty scene, artificial actor or fabricated telemetry.

### 1.1 Source facts constraining this contract

| Reviewed source | Verified constraint / consequence |
| --- | --- |
| `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.10f1` (`e35f0c77bd8e`). Store exact editor revision in the run; do not assume every run uses it. |
| `Packages/manifest.json` | Includes Addressables `2.9.1`, Entities `1.4.5`, URP `17.3.0`; copy actual full dependency/lock hashes per run. |
| `Packages/packages-lock.json` | Tracked package-resolution lock exists; preserve digest, do not use manifest alone to infer resolved dependency graph. |
| `Assets/Settings/Build Profiles/Windows Debug.asset` | `DEBUG` scripting define is present, but `m_Development`, `m_ConnectProfiler`, `m_BuildWithDeepProfilingSupport`, `m_AllowDebugging` are `0` in this profile. **Record actual build flags**, not an assumed "debug profiling" environment. |
| `Assets/Runtime/Core/GameBootstrapper.cs` | `Application.isBatchMode` or Null graphics selects server mode; #46 needs an explicit supported test-client startup mode. A batchmode process must not be counted as a real PurrNet client unless it actually runs the client path. |
| `Assets/Runtime/Shared/Networking/LatencySimulator.cs` | Existing latency simulator reflects into PurrNet private `UDPTransport` fields. New harness MUST use supported public controls or external shaping; no reliance on private PurrNet internals or modifications to vendor code. |
| `.gitignore` | `/Assets/Scenes/`, `/Assets/Prefabs/`, `/Assets/Art/`, `/Assets/Data/Terrain/`, `/Builds/`, `/ProfilerCaptures/`, `/Logs/` are excluded. **Tracked test-only** generated scenes/assets must live under a non-ignored project-owned test path, while generated run output must live under existing ignored output paths. |

The PDDs require **100 m origin cells**, **256 m world tiles**, independent spatial index cells, logical world/map/instance identity separate from Unity scenes, authoritative double-precision game positions and server legality validation. PDD-required graceful disconnect continuity and dormant AI are **not equivalent to existing prototype code**; require feature preflight and actual correctness or mark blocked.

## 2. File vocabulary, normative types and versions

The fixture is hardware-independent workload input. The run record captures *where/how* it is attempted. The summary and samples describe observed results, not intended work. Use **UTF-8 JSON**, no comments, finite JSON numbers, decimal "." and UTF-8 relative paths with forward slashes. Dates are UTC ISO-8601 with `Z`; file names use safe ASCII. Null means **not observed/unknown** only where the schema permits; null NEVER means zero.

| File | Schema version | Ownership and immutability |
| --- | --- | --- |
| `fixture.json` | `benchmark-fixture/v1` | Frozen resolved profile/variant/seed/hashes; byte-identical across same-hardware repeats. |
| `run.json` | `benchmark-run/v1` | Frozen source/build/hardware/process clocks; finalised before loading actors, except a clearly separate actual-observation note. |
| `summary.json` | `benchmark-summary/v1` | Frozen achieved counts, stop/status, correctness and metrics after attempt (including blocked preflight); no replacement with later edits. |
| `samples/*.ndjson` | `benchmark-sample/v1` per line | Raw time-series with a stable metric key, monotonic timestamp, phase, process identity and unit. |
| `report.md` | Human readable, linked to same run | Narrative assessment with missing/blocked metrics and hardware; never independent contradictory counts. |
| `comparison.json` | `benchmark-comparison/v1` | Derived immutable pair/set comparison, never overwrites underlying runs or baseline. |

Canonical JSON data types: `string` is nonempty unless otherwise stated; `uint` is an integral JSON number ≥0; `positive-int` is ≥1; `ratio` is a finite number in [0,1]; `real` is finite JSON number; `sha256` is exactly 64 lowercase hexadecimal characters; `path` is repository-relative for authored assets and run-folder-relative for artefact pointers. Absolute paths, `..` traversal and machine-specific usernames/passwords are prohibited in exported fixtures.

**Versioning:** `v1` is the schema major; additive optional fields need a documented minor revision, never silently change units or semantics. Breaking changes create `v2`. `profile.revision` changes whenever geometry, populations, ability rules, role ratios, topology or phase definitions change; `spec_revision` points to the authoritative profile catalogue revision. Changing a seed creates a new *replicate*, not a new profile revision.

**Validation stages:** (1) schema (types/units/enums/required fields, unique IDs and sums), (2) immutable content hashes/source and scene/definition availability, (3) required feature and runner/hardware readiness, (4) actual workload provisioning and correctness, (5) observation completeness. Failing any step returns a structured failure; never claim success based only on valid JSON.

## 3. Canonical `fixture.json` schema

### 3.1 Required fields, types and invariants

All top-level objects below are **required**. Paths are JSON dotted paths. Object/array fields have the nested required members listed; unknown keys are rejected by v1 writers unless explicitly versioned. Strings and IDs are case-sensitive. Null is allowed **only where stated**.

| Field path | Type | Requirement |
| --- | --- | --- |
| `schema_version` | literal string | Exactly `benchmark-fixture/v1`. |
| `profile.id` | enum string | One of `P1-S0-01` … `P1-S0-08`. |
| `profile.revision`, `profile.spec_revision` | string | Nonempty semantic revision and `P1-STRESS-1.0.0`-style specification ID; must refer to a documented version. |
| `profile.variant` | string | One of that profile's stable isolated/combined variant IDs as defined in stress profiles. |
| `profile.tier` | string | `T0`–`T4`, `T5+` or profile-specific named density/arrival sweep; actual populations govern, not nominal tier labels. |
| `random.master_seed`, `random.replicate_seed`, `random.replicate_index` | uint | Master default `424242`; replicate seeds `424242`/`424243`/`424244` for indices 0/1/2 unless a manifest explicitly revisions the policy. |
| `random.derivation`, `random.streams` | enum; nonempty unique string[] | `sha256-u32le-v1` and stable subsystem stream names (normally `world`, `actors`, `movement`, `abilities`, `network`). |
| `provenance.fixture_generator`, `provenance.generator_revision` | string | Test-only generator identity and version/Git SHA; missing executable generator is preflight blocker. |
| `provenance.input_path`, `provenance.asset_manifest_path` | path | Source-controlled test-only paths, not ignored production `Assets/Scenes`/`Assets/Prefabs`. |
| `provenance.source_commit` | SHA or explicit unresolved marker during authoring | Must resolve to exact commit SHA before **execution**; unresolved blocks preflight. |
| `provenance.source_sha256`, `provenance.asset_manifest_sha256` | sha256 | SHA-256 of exact bytes of input and fully enumerated asset manifest; must match before execution. |
| `required_features` | unique string[] | Required runtime/fixture capabilities checked before any spawn. No silent removal when feature unavailable. |
| `population.players.requested`, `population.npcs.requested` | uint | Exact desired **real** player clients / NPC actors in full-networked mode. Not the count of ECS-only entities. |
| `population.players.connection_kind` | enum | `real_purrnet` or `none_data_only`; real connected clients MUST authenticate and enter the world. |
| `population.npcs.representation` | enum | `networked_gameobject_ecs` or `ecs_data_only`; do not combine these accounting categories. |
| `population.players.groups[]` | array of group objects | Required `class_id`, `role`, `count`, `cluster_id`, `activity` with `idle`, `travelling`, `combat` ratios summing to 1. Group counts sum to requested P. |
| `population.npcs.groups[]` | array of group objects | Required `definition_id`, `role`, `count`, `cluster_id`, `activity` with `idle`, `patrolling`, `engaged` ratios summing to 1; group counts sum to N. Idle is **not** dormant. |
| `space.world_id`, `space.map_id`, `space.instance_id` | string, string, string/null | Explicit logical identity. `null` only for public/no-instancing mode; private-instance modes require real runtime instance IDs, not Unity `SceneID`. |
| `space.origin_cell_size_m`, `space.world_tile_size_m` | real | Current PDD values exactly 100 and 256 respectively; do not conflate with spatial-index cell size. |
| `space.generated_grid` | object | `columns`/`rows` positive ints and `anchor_tile` exactly two signed ints; grid is authored topology, not forced scene residency. |
| `space.clusters[]` | unique ID objects | `id`, `center_world_m` triple of finite real doubles, `radius_m` ≥0, `players`/`npcs` uint. Cluster IDs referenced by all groups; sums equal P/N. |
| `environment` | object | `tile_archetype`, `terrain_per_tile`, `buildings_per_tile`, `props_per_tile`, `trees_per_tile`, `undergrowth_per_tile`, `material_slots_per_tile`, `dynamic_lights_per_tile`, `lod_policy_id`, `collision_enabled`; nonnegative integer counts, boolean collision. Actual resident/visible objects reported separately. |
| `schedules.movement[]` | movement objects[] | `actor_group`, `route_id`, `loop` bool, `start_phase`; route must resolve to authored navigable legal path and time. |
| `schedules.abilities[]` | ability objects[] | `actor_group`, `ability_definition_id`, `target_group`, `attempts_per_actor_per_s` nonnegative real, `start_phase`. Definition/targets must actually exist and be legal. |
| `schedules.events[]` | event objects[] | `at_phase`, `offset_s` nonnegative real, `kind` from versioned generator event vocabulary; event-specific fields MUST be defined by #45 and validated as game-legal. |
| `network.preset`, `network.transport`, `network.shaper` | strings | Preset one of `NET-LOCAL`, `NET-TYPICAL`, `NET-ADVERSE`; transport `PurrNet`; shaping `none` or versioned public/external method. |
| `network.target_rtt_ms`, `network.target_jitter_ms`, `network.target_loss_ratio` | real/null | Requested network impairment (ms, ms, [0,1]). Null = no requested controlled impairment, NOT measured zero. |
| `network.connection_attempts_per_s` | nonnegative real | Scheduled admission attempts, not achieved connected clients/s. |
| `timing.preflight_timeout_s`, `timing.ramp_s`, `timing.warmup_s`, `timing.measurement_s`, `timing.recovery_s` | nonnegative real | Phase durations measured in monotonic wall seconds; measured phase must be >0. No implicit tick count. |
| `timing.sample_interval_ms` | positive int | Target telemetry interval; real collection cadence may differ and MUST be measured. |
| `stops.total_timeout_s` | positive real | Must cover preflight+ramp+warmup+measurement+recovery or explicitly declare narrower intentional abort test. |
| `stops.memory_ceiling_bytes` | positive integer/null | Limit chosen for the hardware **before run**; null = resource ceiling unspecified, not infinite memory. |
| `stops.terminate_on_correctness_failure`, `stops.terminate_on_missing_fixture`, `stops.terminate_on_missing_required_telemetry` | bool | True for canonical performance acceptance. Optional exploratory disabled checks MUST change manifest/revision and cannot count as qualified passing runs. |

The schema supports future variation by **revising the whole fixture contract**, not burying alternate settings in unspecified fields. For profiles where both NPC representation modes are compared, use separate fixtures with distinct `profile.variant`. All test actors, definitions and route references must be validated against tracked generator inputs.

### 3.2 Seed derivation — deterministic proposal for #44/#45

To remove ambiguity from the common seed policy in #42, define `sha256-u32le-v1`:

1. For a subsystem stream, form the UTF-8 byte string `ninthage-bench-seed-v1\n<profile_id>\n<profile_revision>\n<replicate_seed>\n<stream_name>` with ASCII LF (`\n`), no final newline, and decimal seed with no leading zeroes.
2. Compute SHA-256 of those exact bytes; read digest bytes 0–3 as an **unsigned little-endian 32-bit integer**.
3. Use that unsigned value to initialise a **documented versioned PRNG** (chosen and recorded by #45), independent of process-randomised C# `GetHashCode`, host architecture, .NET runtime hash seed and execution order.
4. For actor-specific streams, derive `<stream_name>:<stable_actor_fixture_id>`, **not** the order in which a parallel Job/spawn happens to run. Record generator revision so algorithm changes cannot silently alter fixtures.

This guarantees reproducibility of *input fixtures/action plans*, **not** bit-identical Unity physics, wall timings or packet order across hardware. JSON floating-point canonicalization, stable actor ordering and PRNG family must be pinned by #43/#45 in executable code; absent that implementation blocks the corresponding executable fixture.

### 3.3 Fully specified illustrative fixture

The following is **syntactically valid v1 JSON and has internally consistent population/cluster/timing counts**. It is **NOT EXECUTABLE**: placeholder definition IDs, generator revision, asset manifest and zero-filled hashes must be replaced with actual tracked fixture artefacts by #44/#45, otherwise preflight is **BLOCKED-PREREQUISITE**. The sample's 5×5 grid is an authored topology; **no 25 resident scene guarantee** is claimed.

```json
{
  "schema_version": "benchmark-fixture/v1",
  "profile": {
    "id": "P1-S0-04",
    "revision": "1.0.0",
    "spec_revision": "P1-STRESS-1.0.0",
    "variant": "S04-I-FULL",
    "tier": "T1"
  },
  "random": {
    "master_seed": 424242,
    "replicate_index": 0,
    "replicate_seed": 424242,
    "derivation": "sha256-u32le-v1",
    "streams": [
      "world",
      "actors",
      "movement",
      "abilities",
      "network"
    ]
  },
  "provenance": {
    "fixture_generator": "NinthAgeBenchmarkFixtureGenerator",
    "generator_revision": "NOT_IMPLEMENTED",
    "input_path": "Assets/Tests/Benchmarks/Definitions/npc-population.json",
    "source_commit": "NOT_RESOLVED",
    "source_sha256": "0000000000000000000000000000000000000000000000000000000000000000",
    "asset_manifest_path": "Assets/Tests/Benchmarks/Generated/fixture-assets.json",
    "asset_manifest_sha256": "0000000000000000000000000000000000000000000000000000000000000000"
  },
  "required_features": [
    "full_networked_npc_spawn",
    "seeded_actor_placement",
    "scripted_legal_npc_activity"
  ],
  "population": {
    "players": {
      "requested": 1,
      "connection_kind": "real_purrnet",
      "groups": [
        {
          "class_id": "FIXTURE_PLAYER_CLASS_ID",
          "role": "traveller",
          "count": 1,
          "activity": {
            "idle": 0.5,
            "travelling": 0.5,
            "combat": 0
          },
          "cluster_id": "client-start"
        }
      ]
    },
    "npcs": {
      "requested": 100,
      "representation": "networked_gameobject_ecs",
      "groups": [
        {
          "definition_id": "FIXTURE_PASSIVE_NPC_ID",
          "role": "idle",
          "count": 95,
          "activity": {
            "idle": 1,
            "patrolling": 0,
            "engaged": 0
          },
          "cluster_id": "distant"
        },
        {
          "definition_id": "FIXTURE_PATROL_NPC_ID",
          "role": "patrolling",
          "count": 4,
          "activity": {
            "idle": 0,
            "patrolling": 1,
            "engaged": 0
          },
          "cluster_id": "outer"
        },
        {
          "definition_id": "FIXTURE_HOSTILE_NPC_ID",
          "role": "engaged",
          "count": 1,
          "activity": {
            "idle": 0,
            "patrolling": 0,
            "engaged": 1
          },
          "cluster_id": "near"
        }
      ]
    }
  },
  "space": {
    "world_id": "FIXTURE_WORLD_ID",
    "map_id": "FIXTURE_MAP_ID",
    "instance_id": null,
    "origin_cell_size_m": 100,
    "world_tile_size_m": 256,
    "generated_grid": {
      "columns": 5,
      "rows": 5,
      "anchor_tile": [
        -2,
        -2
      ]
    },
    "clusters": [
      {
        "id": "client-start",
        "center_world_m": [
          0,
          5,
          0
        ],
        "radius_m": 10,
        "players": 1,
        "npcs": 0
      },
      {
        "id": "near",
        "center_world_m": [
          20,
          5,
          20
        ],
        "radius_m": 20,
        "players": 0,
        "npcs": 1
      },
      {
        "id": "outer",
        "center_world_m": [
          180,
          5,
          40
        ],
        "radius_m": 40,
        "players": 0,
        "npcs": 4
      },
      {
        "id": "distant",
        "center_world_m": [
          620,
          5,
          620
        ],
        "radius_m": 380,
        "players": 0,
        "npcs": 95
      }
    ]
  },
  "environment": {
    "tile_archetype": "FIXTURE_OUTDOOR_TILE_ARCHETYPE",
    "terrain_per_tile": 1,
    "buildings_per_tile": 0,
    "props_per_tile": 10,
    "trees_per_tile": 20,
    "undergrowth_per_tile": 0,
    "material_slots_per_tile": 3,
    "dynamic_lights_per_tile": 0,
    "lod_policy_id": "FIXTURE_LOD_POLICY",
    "collision_enabled": true
  },
  "schedules": {
    "movement": [
      {
        "actor_group": "patrolling",
        "route_id": "FIXTURE_NAVMESH_ROUTE",
        "loop": true,
        "start_phase": "warmup"
      }
    ],
    "abilities": [
      {
        "actor_group": "engaged",
        "ability_definition_id": "FIXTURE_LEGAL_ABILITY_ID",
        "target_group": "traveller",
        "attempts_per_actor_per_s": 0.1,
        "start_phase": "measurement"
      }
    ],
    "events": [
      {
        "at_phase": "measurement",
        "offset_s": 150,
        "kind": "legal_aggro_activation",
        "source_cluster": "distant",
        "requested_fraction": 0.1
      }
    ]
  },
  "network": {
    "preset": "NET-LOCAL",
    "transport": "PurrNet",
    "shaper": "none",
    "target_rtt_ms": null,
    "target_jitter_ms": null,
    "target_loss_ratio": null,
    "connection_attempts_per_s": 0
  },
  "timing": {
    "preflight_timeout_s": 120,
    "ramp_s": 120,
    "warmup_s": 60,
    "measurement_s": 300,
    "recovery_s": 60,
    "sample_interval_ms": 1000
  },
  "stops": {
    "total_timeout_s": 750,
    "memory_ceiling_bytes": null,
    "terminate_on_correctness_failure": true,
    "terminate_on_missing_fixture": true,
    "terminate_on_missing_required_telemetry": true
  }
}
```

## 4. Canonical `run.json` identity and environment schema

`run.json` MUST be unique per attempted run and MUST never overwrite a previous run. Use UTC start intent plus profile/tier/seed/replicate and at least 8 random hex digits or a UUID, e.g. `20261013T180000Z_P1-S0-04_T1_s424242_r0_example0001`. This ID is a **sample identifier**, not evidence of an actual run.

| Required field/object | Meaning and allowed missing values |
| --- | --- |
| `schema_version`, `run_id`, `created_utc` | Version literal `benchmark-run/v1`, collision-free slug and ISO UTC timestamp. |
| `fixture_ref.path`, `fixture_ref.sha256` | Relative `fixture.json` and SHA-256 of **canonical final fixture bytes**. Must resolve and match; the example's zero hash intentionally does not. |
| `git.repository`, `git.commit_sha`, `git.dirty`, `git.dirty_diff_sha256` | Repo, 40-hex Git SHA, bool and sha256/null. Qualified comparable baselines require `dirty=false`; if dirty, capture exact diff hash, refuse baseline promotion. |
| `toolchain.unity`, `toolchain.unity_revision` | Actual Unity Editor/player build identity. |
| `toolchain.packages_manifest_sha256`, `toolchain.packages_lock_sha256` | Complete file byte digests. Also archive full package/version inventory including dependencies; package-lock is authoritative for resolved packages. |
| `toolchain.purrnet_source_id`, `toolchain.purrnet_sha256` | Exact installed PurrNet provenance (package/source revision or immutable archive hash), not assumed because PurrNet isn't a manifest key. Must resolve for qualified full-network comparisons. |
| `toolchain.fixture_generator_version`, `toolchain.runner_version`, `toolchain.telemetry_version` | Exact test-only tools/commit IDs; missing tool blocks attempted run. |
| `build.kind`, `build.target`, `build.subtarget` | `headless_server`, `rendered_client` or labelled diagnostic tool; platform target and accurate build subtarget. Separate server/client build records if both used. |
| `build.development`, `build.autoconnect_profiler`, `build.deep_profiling`, `build.script_debugging` | Actual booleans from built player, never inferred from profile name or `DEBUG` alone. |
| `build.defines`, `build.scripting_backend`, `build.build_sha256` | Full define list, IL2CPP/Mono identity and executable binary or manifest digest. For mixed client/server builds, each executable MUST have its own entry/hash in a v1 minor-compatible extension before qualified network benchmarks. |
| `render.mode`, `render.graphics_api`, `render.urprenderer`, `render.quality_level`, `render.resolution_px`, `render.vsync`, `render.target_fps` | Mode `headless` or `rendered`; rendering fields null when headless; when rendered, required actual resolution [width,height], graphics API, URP renderer settings, quality and frame caps. |
| `hardware[]` | Host ID, role, OS/version, CPU model, physical/logical cores, RAM bytes, GPU/VRAM/driver when relevant, storage class; null values are **disqualifying for hardware-comparable baselines** where that field affects a measured metric. |
| `processes` | Actual server process count, headless network-client process count, rendered clients, actor-only test processes and client-host allocation. Never count actors/ECS entities as connected network clients. |
| `clocks.utc_clock_source`, `clocks.monotonic_clock_source`, `clocks.simulation_clock_source`, `clocks.utc_clock_sync` | Clock identity and synchronisation; document local monotonic timing, UTC event wall clock and future Stage 1.1 authoritative tick separately. |
| `execution.command_template`, `execution.environment_id`, `execution.test_data_scope`, `execution.profiling_mode`, `execution.network_shaper_version` | Reproducible invocation with *redacted* secret args, isolated test data and profiling/network tool versions. |
| `runtime_observed` | Observed build/content digest and measured RTT/loss, nullable only **before preflight/measurement**; observed values after execution go to summary/samples, not by overwriting a frozen run file. |

**Full-stack process rule:** A one-field `build` is permitted only in a single-build or *blocked illustrative* record. A successful mixed server/real-client run MUST capture **all** builds in a revisioned `builds[]` extension with role/host/executable hashes and flags, plus hashes for all emitted content revisions; this requirement must not be waived in #46's eventual implementation.

**Package/profiler caveat:** The current `Windows Debug.asset` is **not** proof that development profiling is on, and no supported public PurrNet packet-byte counter is assumed. Instrumentation MUST mark counters unavailable until supported APIs or project-owned network instrumentation actually supply them.

### 4.1 Illustrative run identity record

This sample identifies how attempted runs would be recorded, **not an executed build**. `NOT_RESOLVED`, `NOT_IMPLEMENTED`, null hardware fields and zero hashes are deliberate **preflight blockers**.

```json
{
  "schema_version": "benchmark-run/v1",
  "run_id": "20261013T180000Z_P1-S0-04_T1_s424242_r0_example0001",
  "fixture_ref": {
    "path": "fixture.json",
    "sha256": "0000000000000000000000000000000000000000000000000000000000000000"
  },
  "created_utc": "2026-10-13T18:00:00Z",
  "git": {
    "repository": "FrontierDev/ninthage",
    "commit_sha": "54505978d613833e0a0083fc68f60c138dcf786d",
    "dirty": false,
    "dirty_diff_sha256": null
  },
  "toolchain": {
    "unity": "6000.3.10f1",
    "unity_revision": "e35f0c77bd8e",
    "packages_manifest_sha256": "0000000000000000000000000000000000000000000000000000000000000000",
    "packages_lock_sha256": "0000000000000000000000000000000000000000000000000000000000000000",
    "purrnet_source_id": "NOT_RESOLVED",
    "purrnet_sha256": null,
    "fixture_generator_version": "NOT_IMPLEMENTED",
    "runner_version": "NOT_IMPLEMENTED",
    "telemetry_version": "NOT_IMPLEMENTED"
  },
  "build": {
    "kind": "headless_server",
    "target": "StandaloneWindows64",
    "subtarget": "dedicated_server",
    "development": false,
    "autoconnect_profiler": false,
    "deep_profiling": false,
    "script_debugging": false,
    "defines": [
      "DEBUG"
    ],
    "scripting_backend": "NOT_RESOLVED",
    "build_sha256": "0000000000000000000000000000000000000000000000000000000000000000"
  },
  "render": {
    "mode": "headless",
    "graphics_api": null,
    "urprenderer": null,
    "quality_level": null,
    "resolution_px": null,
    "vsync": null,
    "target_fps": null
  },
  "hardware": [
    {
      "host_id": "bench-host-a",
      "role": "server",
      "os": "Windows",
      "os_version": "NOT_RESOLVED",
      "cpu_model": "NOT_RESOLVED",
      "physical_cores": null,
      "logical_cores": null,
      "ram_bytes": null,
      "gpu_model": null,
      "vram_bytes": null,
      "gpu_driver": null,
      "storage_class": "NOT_RESOLVED"
    }
  ],
  "processes": {
    "server": 1,
    "rendered_clients": 0,
    "headless_network_clients": 1,
    "actor_test_processes": 0,
    "client_host_allocation": [
      {
        "host_id": "bench-host-a",
        "client_processes": 1
      }
    ]
  },
  "clocks": {
    "utc_clock_source": "host_system_clock",
    "monotonic_clock_source": "NOT_RESOLVED",
    "simulation_clock_source": "unity_player_loop",
    "utc_clock_sync": "not_verified"
  },
  "execution": {
    "command_template": "NOT_IMPLEMENTED",
    "environment_id": "local-development",
    "test_data_scope": "isolated_disposable",
    "profiling_mode": "not_enabled",
    "network_shaper_version": null
  },
  "runtime_observed": {
    "actual_build_sha256": null,
    "actual_content_sha256": null,
    "actual_transport_rtt_ms": null,
    "actual_transport_loss_ratio": null
  }
}
```

## 5. Observations, metrics, summaries and failures

### 5.1 Canonical measurement record

Each metric summary entry in `summary.json.metrics[]` MUST have the following required fields, even when its value is unobservable:

| Field | Type | Invariant |
| --- | --- | --- |
| `metric_id`, `source`, `scope`, `phase`, `unit` | string | Stable namespaced metric and source process, clear per-tick/per-frame/per-interval/per-actor scope, run phase, canonical unit. |
| `observation` | enum | Exactly `measured`, `estimated`, `unsupported`, `unavailable`, `not_applicable`. |
| `sample_count` | uint | Observed raw samples, **not** attempted ticks. Zero for all unmeasured statuses. |
| `min`, `max`, `mean`, `p50`, `p95`, `p99` | number/null | For unmeasured statuses **all null**; for measured use finite numbers and consistent units. If fewer than 100 independent comparable samples, `p99` MUST be null with reason `insufficient_samples` even if other estimates are present. |
| `raw_trace` | relative path/null | Required non-null for measured/estimated metrics, pointing to samples/profiler bytes under same run. |
| `reason` | string/null | Required for all non-measured observations, missing metrics, estimations and insufficient samples; null allowed only for complete measured metrics. |

**Units:** Use `ms` for elapsed CPU/render/GPU or scene-load duration, `bytes` for allocation/memory/network payload, `count` for objects/events/recipients, `ticks` for actual authoritative tick/deadline backlog **only after Stage 1.1**, `events/s` for rates, `ratio` for fractions/loss, and `m` for absolute spatial distance. Do not mix milliseconds, microseconds, bits/s, bytes/s, draw calls and instances under one ID/unit. A metric changes ID or schema version if its unit/scope/aggregation changes.

**Metric scope:** Examples are `simulation_tick`, `server_update`, `rendered_frame`, `client_process`, `server_process`, `scene_operation`, `connection_attempt`, `per_observer`, `interval_1s` and `lifetime`. For each statistic record whether samples are per-host, per-process, per-player, per-actor or per-system; merging these populations requires a **new aggregate metric ID** with documented denominator. Metrics for CPU and bandwidth must preserve their original capture boundaries, not turn one high-frequency raw sample into thousands of correlated observations.

**Percentiles:** Sort finite independent samples ascending by value; nearest-rank percentile `pN = sorted[ceil(N/100 * sample_count) - 1]` using 1-based sample rank. Mean is arithmetic mean, min/max actual extrema, sample_count actual retained observations. Record sample interval and phase; never combine warmup/ramp with measured steady-state unless metric ID explicitly says so. Reject NaN/Inf. One sample cannot substantiate p99 despite being mathematically rankable; require ≥100 measured samples for reportable p99, or null with insufficiency reason. If a metric is estimated, document estimation method and do not compare to direct measurements as equivalent.

**Raw trace line** (`benchmark-sample/v1`): JSON object with required `schema_version`, `run_id`, `metric_id`, `source`, `host_id`, `process_id` (test-generated stable pseudonym), `phase`, `monotonic_elapsed_ns` (nonnegative integer), `utc` (UTC ISO8601 or null when not synchronised), `scope`, `unit`, `value` (finite number), `tags` (string-to-string stable dimensions). Each line MUST have the same metric definition/scope/unit as its summary. Sample files are append-only through the run and immutable after finalisation. Metric names and tag vocabularies are versioned by #47.

### 5.2 Observation meanings and correctness

| Observation | Meaning | Numeric summary |
| --- | --- | --- |
| `measured` | Direct instrumented samples exist on that build/process with known scope and unit. | Actual aggregates; trace required. |
| `estimated` | Inferential/proxy value from documented method, denominator and uncertainty. | Aggregates only with explicit method and raw source; **never silently compared to direct values**. |
| `unsupported` | A required framework/tool facility is not available via supported APIs or feature not implemented (e.g. authoritative tick backlog pre-1.1). | All numeric fields null, count 0; explain missing implementation. |
| `unavailable` | Metric could apply but capture/instrumentation failed, was disabled or hardware data absent. | All numeric fields null, count 0; invalidates benchmark when mandatory. |
| `not_applicable` | Metric logically does not apply (GPU on headless server, private-instance check when run has no logical private instance). | All numeric fields null, count 0; not scored as a passed invariant. |

**Correctness oracle:** `summary.correctness` MUST explicitly contain `status` (`pass` / `fail` / `unverified`), checked assertion count, failed count, and an array of unverified invariant IDs. Future checking implementations must include full logs of actor identity, scene load/unload, cast/tick, reconnect and spatial isolation events. An unverified PDD invariant is NEVER a pass. For a full-contract scenario, required unimplemented invariants force `BLOCKED-PREREQUISITE`, not `COMPLETE-MEASURED`. Never hide actor disappearance behind a reduced achieved population.

### 5.3 Canonical summary / stop record

Required top-level `summary.json` fields:

- `schema_version` = `benchmark-summary/v1`; `run_id` exact run identity; `fixture_sha256` exact canonical final fixture digest.
- `status` one of `COMPLETE-MEASURED`, `BLOCKED-PREREQUISITE`, `FAIL-CORRECTNESS`, `FAIL-PROVISION`, `FAIL-RESOURCE`, `FAIL-TIMEOUT`, `FAIL-INSTRUMENTATION`. Only `COMPLETE-MEASURED` is eligible for controlled baseline comparison, **not automatically passing a capacity target**.
- `started_utc`/`finished_utc` ISO UTC string or null for preflight refusal; `phase_completion` booleans for `preflight`, `ramp`, `warmup`, `measurement`, `recovery`.
- `achieved` actual counts: `connected_players`, `authenticated_players`, `in_world_players`, `npc_actors`, `ecs_entities`, `network_identities`, `gameobjects`, `scene_tiles_loaded`, `renders_visible` (uint/null for headless), `accepted_abilities`. These are observations, never copied from requested values. Later versions may add actors-by-activity and spatial/instance counts but cannot omit these.
- `stop` = `phase`, `reason_code`, `message`; `correctness` = `status`, `checked`, `failed`, `unverified`; `metrics[]` as §5.1; `raw_files[]` relative paths with size/digest metadata when available; `warnings[]` string.

Examples of `stop.reason_code` are `MISSING_ASSET`, `MISSING_GENERATOR`, `MISSING_ABILITY`, `UNSUPPORTED_FEATURE`, `UNSUPPORTED_NETWORK_IMPAIRMENT`, `MISSING_TELEMETRY`, `INVALID_BUILD`, `WORKLOAD_NOT_ACHIEVED`, `DUPLICATE_ACTOR`, `DROPPED_TIMED_EVENT`, `RESOURCE_CEILING`, `TIMEOUT`, `PROCESS_CRASH`. Do **not** interpret the stop reason as free-form success criteria.

### 5.4 Valid blocked-preflight result example

The following example is a **hypothetical structurally valid** summary for the illustrative fixture above. Zero achieved actors here represent **no provisioning happened** (preflight blocked), **not a measured no-load benchmark**. Null timing and metric values are intentional.

```json
{
  "schema_version": "benchmark-summary/v1",
  "run_id": "20261013T180000Z_P1-S0-04_T1_s424242_r0_example0001",
  "fixture_sha256": "0000000000000000000000000000000000000000000000000000000000000000",
  "status": "BLOCKED-PREREQUISITE",
  "started_utc": null,
  "finished_utc": null,
  "phase_completion": {
    "preflight": false,
    "ramp": false,
    "warmup": false,
    "measurement": false,
    "recovery": false
  },
  "achieved": {
    "connected_players": 0,
    "authenticated_players": 0,
    "in_world_players": 0,
    "npc_actors": 0,
    "ecs_entities": 0,
    "network_identities": 0,
    "gameobjects": 0,
    "scene_tiles_loaded": 0,
    "renders_visible": null,
    "accepted_abilities": 0
  },
  "stop": {
    "phase": "preflight",
    "reason_code": "MISSING_GENERATOR",
    "message": "Illustrative fixture generator and authored test definitions do not exist."
  },
  "correctness": {
    "status": "unverified",
    "checked": 0,
    "failed": 0,
    "unverified": [
      "actor_lifetime",
      "timed_events",
      "movement_authority",
      "instance_isolation",
      "session_reconnect"
    ]
  },
  "metrics": [
    {
      "metric_id": "server.simulation.tick_backlog",
      "source": "server",
      "scope": "simulation_tick",
      "phase": "measurement",
      "unit": "ticks",
      "observation": "unsupported",
      "sample_count": 0,
      "min": null,
      "max": null,
      "mean": null,
      "p50": null,
      "p95": null,
      "p99": null,
      "raw_trace": null,
      "reason": "Stage 1.1 authoritative scheduler not implemented."
    },
    {
      "metric_id": "client.gpu.frame_time",
      "source": "rendered_client",
      "scope": "rendered_frame",
      "phase": "measurement",
      "unit": "ms",
      "observation": "not_applicable",
      "sample_count": 0,
      "min": null,
      "max": null,
      "mean": null,
      "p50": null,
      "p95": null,
      "p99": null,
      "raw_trace": null,
      "reason": "Headless benchmark mode has no rendered GPU client."
    }
  ],
  "raw_files": [],
  "warnings": [
    "SYNTHETIC EXAMPLE ONLY: not an executed benchmark."
  ]
}
```

## 6. Output layout, integrity and retention

### 6.1 Ignored runtime path (no `.gitignore` modifications)

Use `Builds/Benchmarks/Results/<run_id>/` under the existing Git-ignored `/Builds/` tree, **not** `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/ThirdParty/`, `Assets/Data/Terrain/` or any production content directory. `Builds/` is already ignored; `ProfilerCaptures/` is also ignored and may hold a shared capture cache, but a run MUST contain immutable linked copies or exact SHA-256 pointers to the capture artefacts. Avoid generating content in tracked `Assets/` just to run a benchmark.

```text
Builds/Benchmarks/Results/<run_id>/
  fixture.json                    # frozen resolved fixture
  run.json                        # frozen run identity and inputs
  summary.json                    # final observed outcome, even for preflight failure
  report.md                       # human-readable, from same summary
  samples/
    server.ndjson                 # JSON per monotonic sample
    client-0001.ndjson            # one file per real process when applicable
  profiler/
    server.raw                    # actual Unity profiler data if captured
    client-0001.raw               # rendered client capture if available
  logs/
    runner.log                    # bounded redacted test orchestration log
    server.log                    # redacted server log
    client-0001.log               # redacted client log where relevant
  artifacts.json                 # paths, byte sizes and SHA-256 of all archived files
  comparisons/
    <comparison_id>/
      comparison.json
      comparison.md
```

**No synthetic empty logs:** An absent client/capture file should be omitted and marked `unavailable`/`not_applicable` in summary, not replaced by a placeholder implying capture success. In `artifacts.json`, record every included file's relative path, byte size, SHA-256, media type and collector identity. Because `artifacts.json` describes itself only indirectly, omit its own hash entry; compute a **separate external digest** of final `artifacts.json` for content-addressed archive publication.

**Lifecycle:** Allocate unique run directory atomically → write intended fixture/run files read-only → preflight all required resources → append temporary traces/profiler/logs with bounded memory/disk policy → finalise summary/report/artifact inventory using atomic rename → seal entire run (no mutable cache data) → copy to immutable external storage or preserve content-addressed versioned archive. Subsequent derived comparisons are **new** artefacts with parent-run digests; never mutate sealed run directories.

**Baseline archive:** Accepted baselines are immutable selected `run_id` + manifest digest + hardware/fixture key + archived checksum. They MAY be indexed in project-owned documentation, but cannot be regenerated in place or overwritten. Record which store (local encrypted archive or approved externally backed immutable version) holds the bytes and the integrity digest. `git clean` and Unity imports must not silently erase the only copy of an accepted result. Never publish private test-user identifiers, credentials, IPs or persistent player data.

### 6.2 Reproducible tracked content requirements

The repo **currently ignores** `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/Art/` and `Assets/Data/Terrain/`. Therefore a local Editor's city/forest/spawn scenes and prefab references **are not adequate reproducible evidence**. #44 and #45 MUST place generator inputs and **generated test-only scenes, prefabs, terrain/test assets, their stable Unity `.meta` GUIDs and dependencies** beneath an explicitly tracked path such as:

```text
Assets/Tests/Benchmarks/
  Definitions/                   # scenario, procedural source settings, legal IDs
  Generated/                     # versioned benchmark-only Unity scenes/assets/meta
  Manifests/                     # source/content SHA-256 inventories
```

The path is a future target, **not created by Issue #43**. Track all project-owned generated dependencies or identify immutable externally retrievable, checksum-verified binary inputs before execution. Never vendor-copy or modify third-party assets/packages to make benchmarks stand alone. A locally referenced file that is untracked, ignored, missing on clean checkout or has altered GUID/dependency hash is `BLOCKED-PREREQUISITE`, not a "close enough" fixture. Record Addressables group/key, source commit, asset GUID, content dependency hash and generator/Unity importer version. Hash asset source bytes **and** compiled Addressables/catalog/bundles when actually used. Remote bundle inputs must be immutable and content-addressed; a mutable URL alone is insufficient.

The logical world/map/instance definition is separate from the Unity scene asset/scene instance/PurrNet `SceneID`, and a generated tile grid is not necessarily simultaneously loaded. Records MUST distinguish authored tiles, loaded environment scenes, resident/visible actors and real client connections.

## 7. Repeat policy, controlled comparison and promotion

**Clean start:** Start every replicate with a fresh server and client process set, isolated disposable datastore/accounts, cleared authoritative actor/ECS/network registry state, known streaming cache policy (cold or warmed and recorded), same fixture hash and clock configuration. Do not carry actors/quests/assets/connection state from another profile. Refuse to run if the required asset/network/definition checks fail; do not silently move to an easier fixture or skip missing avatars.

**Repeats:** Run canonical seeds `424242`, `424243`, `424244` at each candidate baseline tier unless provisioning is blocked. Record all trials including failures and outliers rather than keeping only the fastest. For short smoke-only checks, one replicate is permitted but cannot be promoted as a stable baseline. For material p99/performance regressions, require sufficient measured samples per metric and at least three comparable replicates before making a claim. Re-run if significant environment drift/thermal throttling, profiler overhead, noisy co-resident processes or incomplete traces invalidate comparison; preserve originals.

**Comparability key:** Profile ID/revision + variant + tier + fixture SHA/seed + built content manifest + full source SHA + hardware class and stable actual machine allocation + Unity/PurrNet/package/renderer versions + build development/profiler flags + quality/resolution/fps/vsync + network emulator preset/observed RTT/loss + trial timing + collector/aggregation method. To compare *code revisions*, allow Git SHA to differ but **hold all other relevant dimensions constant**, document intentional differences and compare the **same replicate seeds**. A change to scenario, assets, hardware, profiling mode or missing metrics is a different experiment, not a direct speedup/regression.

**Comparison format:** A `benchmark-comparison/v1` record MUST state `comparison_id`, immutable `baseline_run_ids[]` with digests, `candidate_run_ids[]` with digests, comparability key plus documented differences, metric IDs/units/scopes, repeat counts, per-run observations, delta absolute/percent and confidence/variance context. An unsupported/estimated/non-applicable metric cannot generate an authoritative performance delta; label it incomparable. Compute percent change only when baseline reference is nonzero; otherwise null with reason. Human `comparison.md` must repeat blockers and relevant CI/local test provenance.

**Acceptance budget:** Neither the tier table nor a sample JSON sets an acceptable frame-time, player count or server CPU budget. [#55](https://github.com/FrontierDev/ninthage/issues/55) derives hardware-specific acceptance/reliability bands only **after** measured baselines, independent correctness checks and comparison-equivalent replicates. `COMPLETE-MEASURED` means valid measurement, not a passed target or a production-scale guarantee.

## 8. Implementation boundaries and validation gate

**Issue #43 owns only this documentation.** No production classes/methods/types, Unity scenes/prefabs/meta, test code, Addressables, build profiles, `.gitignore`, CI workflows or third-party files are modified or authorised by this issue. The schema tables, JSON examples and directory layout are contracts for subsequent separately reviewed implementation issues, not assertions that a working harness exists.

**Static acceptance checklist**

- [ ] Fixture schema fully specifies profile/seed/roles/counts/activity/position/topology, legal abilities, scene/content provenance/digests, network impairments, phases and stop/preflight features.
- [ ] Run schema captures UTC, exact Git+package/tool+PurrNet/build identity, graphics quality/resolution, hardware/clients/clock origin and dirty-build qualification.
- [ ] Summary and sample schema distinguish requested vs achieved, sample count, scope/units, p50/p95/p99, min/max, captured raw trace, `unsupported` vs `unavailable` vs `not_applicable`, correctness and setup/resource failures.
- [ ] JSON examples parse and meet the stated structural/value constraints; examples clearly fail **referential preflight** until the future fixture generator exists.
- [ ] All linked files exist; Markdown and JSON fences balanced; changed-file scope contains only this document, zero vendor/production/test/Unity `.meta` changes, no extraneous generated artefacts.
- [ ] User reviews every diff hunk, fixture asset strategy, missing-metric semantics and immutable result/retention policy before approving Issue #43.

**Validation limitations:** No Unity Editor, runtime benchmark, EditMode/PlayMode execution, hardware profiling, PurrNet metrics or GitHub Actions job is run to author this specification. GitHub Actions usage is unavailable until 20 November 2026; relevant future code changes must use user-executed local tests before commit and honestly preserve their results. Do not claim local or CI tests passed for this documentation change.

