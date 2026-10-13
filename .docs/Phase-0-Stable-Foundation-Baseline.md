# Ninth Age — Phase 0 Stable Foundation Baseline

**Status:** Source baseline reconstructed; **Phase 0 validation incomplete / not certified**
**Repository:** `FrontierDev/ninthage`
**Branch:** `dev`
**Phase 0 candidate snapshot:** `f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f` — [Issue 39, 40 commit](https://github.com/FrontierDev/ninthage/commit/f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f), 2026-10-13
**Last examined `dev` head:** `1677098d27967f9ef4a965e1421737fad3bd892d` — later Phase 1 documentation-only commits; not a substitute for the Phase 0 snapshot
**Source review date:** 2026-10-13
**Additional evidence:** User-supplied local Unity Test Runner XML for EditMode (2026-10-13 16:10:45–16:10:46 UTC) and PlayMode (2026-10-13 16:11:46 UTC); their exact Git commit is **not embedded in the reports**.
**Issue:** [#40 — Record the Stable Foundation baseline and close Phase 0](https://github.com/FrontierDev/ninthage/issues/40)

> **Validation qualification:** Issue #40 is closed in GitHub but retains the `needs-validation` label. The accessible GitHub Actions runs have **not executed Unity tests successfully**. Separately, user-supplied local Unity Test Runner XML shows **one failed EditMode and one failed PlayMode test**. Required integrated manual game validation and production pre-restyle persistence migration have not been independently verified. This file records the actual candidate codebase and the outstanding acceptance evidence; it does **not** certify Phase 0.

## 1. Purpose, evidence and scope

This document reconstructs the precise Phase 0 codebase baseline and implementation conventions required by Issue #40, without inferring that source code or a closed issue proves gameplay correctness. It is descriptive and subordinate to the [MMORPG Master PDD](MMORPG-Master-PDD.md), [C# Architecture](CSharp-Architecture.md), [C# Style Conventions](CSharp-Style-Conventions.md), [GitHub Issue and Workflow Guide](GitHub-Issue-and-Workflow-Guide.md), [C# Restyling Plan](Plans/CSharp-Restyling-Plan.md) and [Project Roadmap](Plans/Project-Roadmap.md).

Evidence categories are deliberately distinct:

- **Source-verified:** file, content, symbol, test annotation, configuration or GitHub commit exists at the candidate snapshot.
- **CI-observed:** GitHub Actions run and job outcomes were inspected; a configured test job is not proof that tests ran.
- **Manual verification outstanding:** Unity Editor, client/server runtime, gameplay, asset integrity or legacy data round trip has not been observed in this review.
- **Deferred:** documented prototype limitation or PDD-owned future implementation, not proof the Phase 0 foundation is production-ready.

The candidate commit `f3c16ffc` adds `.github/workflows/unity-validation.yml` and three Unity test `.meta` files. It did **not** create this Phase 0 baseline file, mark the roadmap milestone complete or add a status note to the historical restyling plan. Later `dev` commits `4f39a8f` and `1677098` concern [Phase 1.0 Issue #41](https://github.com/FrontierDev/ninthage/issues/41)'s architecture document, not Phase 0 runtime changes.

## 2. Codebase identity and technical foundation

| Subject | Candidate snapshot evidence |
| --- | --- |
| Git branch / candidate SHA | `dev`, `f3c16ffcb99f8cdc47fbf90babca87b1fd8ec49f` |
| Unity Editor | `6000.3.10f1` (revision `e35f0c77bd8e`), `ProjectSettings/ProjectVersion.txt` |
| Process model | One Unity project with graphical client and headless-server execution paths; `Assets/Runtime/Core/GameBootstrapper.cs` |
| Server/client boundary | `Game.Server`, `Game.Client` and `Game.Shared`, selected through `Game.Core` bootstrap |
| Networking | Project-owned PurrNet integration using `NetworkManager`, `NetworkBehaviour`, RPC endpoints, network identities and scene modules |
| Simulation | Unity Entities server world and system group; GameObject/network actors bridged to ECS state; no Phase 1 fixed authoritative scheduler yet |
| World | Double-precision `WorldPosition` and ECS `double3` foundation, floating-origin client, Addressable additive scene loading and chunk-based interest |
| Rendering | Universal Render Pipeline client with Forward+ architectural intent, subject to runtime/graphics validation |
| Persistence | `PlayerDatabase` versioned JSON account snapshot with a legacy migration code path; **not** production SQL persistence |
| Content | Addressables, scriptable gameplay definitions and preproduction runtime/authoring tools |

These describe code and configuration, **not** results of executable gameplay or performance testing.

## 3. Final Phase 0 assembly layout

Project-authored assembly definitions present at the candidate snapshot:

| Assembly | Assembly definition | Responsibility |
| --- | --- | --- |
| `Game.Runtime` | `Assets/Runtime/Runtime.asmdef` | Parent runtime assembly boundary; nested project assemblies own substantive runtime code |
| `Game.Core` | `Assets/Runtime/Core/Core.asmdef` | Client/server bootstrap and process selection |
| `Game.Shared` | `Assets/Runtime/Shared/Shared.asmdef` | Common data definitions, runtime types, networking contracts, utilities and movement |
| `Game.Client` | `Assets/Runtime/Client/Client.asmdef` | Rendering, input, UI, client controllers, VFX/audio and client scene/origin state |
| `Game.Server` | `Assets/Runtime/Server/Server.asmdef` | Authoritative managers, ECS systems, networking bindings, services and persistence |
| `Game.Editor` | `Assets/Editor/Editor.asmdef` | Project-owned Unity Editor tooling; Editor-only platform constraint |

Project test assemblies are **separate** from these runtime/editor assemblies:

- `Assets/Tests/EditMode/Game.Shared.Tests.asmdef` — `Game.Shared.Tests`, Editor-only.
- `Assets/Tests/PlayMode/Game.Runtime.Tests.asmdef` — `Game.Runtime.Tests`.

The source tree at `f3c16ffc` contains **130 Shared**, **139 Client**, **37 Server**, and **1 Core** C# files beneath `Assets/Runtime/`; these are a repository source inventory, not a compilation result. The larger assembly dependency overview remains in [C# Architecture](CSharp-Architecture.md).

## 4. Established terminology and naming boundaries

The [C# Style Conventions](CSharp-Style-Conventions.md) and Phase 0 restyling work establish the following project-owned terminology:

| Name/category | Meaning and source example |
| --- | --- |
| `*Endpoint` | Shared client/server RPC-facing contracts and related callback surfaces, e.g. `Assets/Runtime/Shared/Networking/AccountEndpoint.cs`, `ActorEndpoint.cs`, `MovementEndpoint.cs`, `StatEndpoint.cs`. RPC methods preserve directional `Server_`, `Client_`, `Observers_` prefixes. |
| `*Bindings` | Server-side registration/wiring between shared callbacks and server handlers, e.g. `Assets/Runtime/Server/Core/ServerBindings.cs`, `Server/Networking/AccountBindings.cs`, `ActorDeathBindings.cs`. `ServerBindings.Initialize` guards repeated registration. |
| `*Service` | Domain action or capability implementing gameplay/account decisions, e.g. `Server/Services/CharacterCreationService.cs`, `EnterWorldService.cs`, `PlayerAuthService.cs`. |
| `*Manager` | Longer-lived runtime owner/coordinator, e.g. `ServerActorManager.cs`, `ServerWorldManager.cs`, `ServerConnectionManager.cs`, `ClientWorldManager.cs`. |
| `*System` | Unity ECS system processing server simulation data, e.g. `Server/Systems/ActorUpdateSystem.cs`, `SpellCastSystem.cs`, `AuraTickSystem.cs`. |
| Events/callbacks | `on...` event members; `On...` handlers; keep callback direction explicit. |
| Namespaces | Project namespace follows folder hierarchy (`Game.Core`, `Game.Shared.*`, `Game.Client.*`, `Game.Server.*`, `Game.Editor`). |
| Source style | Private `_camelCase`, PascalCase types and ordinary methods, uppercase abbreviations such as `ID`/`GUID`/`ECS`, `SCREAMING_SNAKE_CASE` constants/static readonly, retained `UI_` and gameplay type-family prefixes, test/prototype `Test_`. |

**Source-ownership constraint:** These project conventions **do not apply to third-party code**. PurrNet, Unity packages and imported/vendor-owned scripts, assemblies, assets, namespaces and metadata remain read-only without specific user authorization. Do not treat historical restyling manifests as permission to change vendor material.

This section describes the intended terminology and representative current source; it does **not** certify that every C# file or serialized reference passed a full compliance/build/Editor audit.

## 5. Automated regression suite — source inventory versus results

At the candidate snapshot, the following test methods are declared in the repository (counted as literal NUnit `[Test]` method annotations, **not** counted as passed executions):

| Mode | File | Declared tests |
| --- | --- | ---: |
| EditMode | `Assets/Tests/EditMode/Shared/CombatRatingHelperTests.cs` | 4 |
| EditMode | `Assets/Tests/EditMode/Shared/ExperienceCalculatorTests.cs` | 5 |
| EditMode | `Assets/Tests/EditMode/Shared/MovementSimulationTests.cs` | 6 |
| EditMode | `Assets/Tests/EditMode/Shared/StatInstanceTests.cs` | 3 |
| EditMode | `Assets/Tests/EditMode/Shared/WorldPositionTests.cs` | 6 |
| EditMode | `Assets/Tests/EditMode/Smoke/EditModeTestHarnessTests.cs` | 1 |
| **EditMode total** | **Six test files** | **25** |
| PlayMode | `Assets/Tests/PlayMode/Persistence/CharacterPersistenceRoundTripTests.cs` | 1 |
| PlayMode | `Assets/Tests/PlayMode/Server/ServerBindingsTests.cs` | 1 |
| PlayMode | `Assets/Tests/PlayMode/Smoke/PlayModeTestHarnessTests.cs` | 1 |
| **PlayMode total** | **Three test files** | **3** |
| **Total** | **Nine test files** | **28** |

**Execution result (updated):** The local Unity Test Runner **did execute** these suites, but **both suites failed**; see Section 5.2. The source inventory remains 25 project EditMode and 3 project PlayMode test methods. The uploaded XML does not independently establish the exact Git SHA under test or demonstrate a fully passing baseline.

### 5.1 GitHub Actions

The `.github/workflows/unity-validation.yml` workflow was added in `f3c16ffc`, using `game-ci/unity-test-runner@v4` with separate `editmode` and `playmode` jobs, pull-request/push/dispatch triggers, and attempted artifact upload. It checks for `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` before running the Unity tests.

Accessible run outcomes as of the review:

| Run | Source SHA | Observed result |
| --- | --- | --- |
| [#1 — 38421657778](https://github.com/FrontierDev/ninthage/actions/runs/38421657778) | `f3c16ffc` | **Failure** — both jobs failed at `Validate Unity licensing configuration`; Unity test execution **skipped** |
| [#2 — 38426366217](https://github.com/FrontierDev/ninthage/actions/runs/38426366217) | `4f39a8f` | **Failure** — workflow did not establish a successful test result |
| [#3 — 38426544373](https://github.com/FrontierDev/ninthage/actions/runs/38426544373) | `1677098` | **Failure** — both jobs failed at `Validate Unity licensing configuration`; Unity test execution **skipped** |

**Successful workflow run reference required by Issue #40: none verified.** This is a **missing acceptance artifact**, not a passed CI test. The licensing/check configuration must be addressed in GitHub repository Actions settings and/or project-owned workflow configuration, then both modes must run and pass with a durable run URL and test artifacts. Do not embed secrets in this file or claim they have been configured.

### 5.2 Local Unity Test Runner evidence — 13 October 2026

The user provided two original NUnit-style Unity XML reports. These are **local Editor Test Runner results**, not GitHub Actions executions. The recorded times are UTC; the XML identifies the test assemblies, individual outcomes and failures, but does **not** identify the Git commit or confirm that its compiled assemblies exactly match `f3c16ffc`.

| Report (user-provided) | UTC test interval | Results across all discovered assemblies | Ninth Age project assembly only |
| --- | --- | --- | --- |
| `TestResults_20261013_171050(1).xml` — EditMode | 16:10:45–16:10:46 | **26 total, 25 passed, 1 failed, 0 skipped** | `Game.Shared.Tests.dll`: **25 total, 24 passed, 1 failed** |
| `TestResults_20261013_171336.xml` — PlayMode | 16:11:46 | **3 total, 2 passed, 1 failed, 0 skipped** | `Game.Runtime.Tests.dll`: **3 total, 2 passed, 1 failed** |
| **Combined** | — | **29 total, 27 passed, 2 failed** | **28 project tests, 26 passed, 2 failed** |

The additional passing EditMode case comes from the **third-party** `Unity.Addressables.DocExampleCode.Editor.Tests.dll` assembly (`TestStub.RequiredTest`). It does not count toward Ninth Age's 25 project-owned EditMode tests; neither the vendor test nor its code should be modified as part of a project regression correction.

**Failure A — incorrect expected Y at a world-position chunk boundary**

- Test: `Game.Shared.Tests.Shared.WorldPositionTests.ExactChunkBoundariesResolveToExpectedChunks`.
- Source: `Assets/Tests/EditMode/Shared/WorldPositionTests.cs:64`.
- Observed: expected `(0, 0, 0)`, actual `(0, 4, 0)`.
- Current test constructs `WorldPosition(chunkSize, 4d, -chunkSize)` and `WorldPosition(-chunkSize, 4d, -chunkSize)` and asserts the entire `LocalPosition` equals `Vector3.zero`. Current `WorldPosition.LocalPosition` computes local X/Z from the chunk boundary while deliberately retaining `y`. Other tests in the same fixture explicitly verify height preservation.
- **Assessment:** On reviewed source, this is a **test expectation mismatch**: the correct position at the horizontal chunk boundary retains Y = 4. Update only the relevant project-owned test assertion to validate X = 0, Y = 4, Z = 0 (or X/Z individually), unless a conflicting authoritative position design is demonstrated. **Do not change `WorldPosition` semantics to force the test to pass.**
- Actual outcome: **failed**. Correction and a passing rerun are required.

**Failure B — legacy player-data names bypass migration in the round-trip test**

- Test: `Game.Runtime.Tests.Persistence.CharacterPersistenceRoundTripTests.CurrentPlayerDataFixtureRoundTripsWithoutLosingCharacterState`.
- Source: `Assets/Tests/PlayMode/Persistence/CharacterPersistenceRoundTripTests.cs:40`.
- Observed: expected `"character-phase0-baseline-0001"`, actual `null` for `CharacterData.GUID`.
- The tracked JSON fixture uses legacy keys `Guid`, `PvPFactionID`, `ItemGuid`, `ItemId` and `StatId`, whereas the current DTOs contain `GUID`, `PVPFactionID`, `ItemGUID`, `ItemID` and `StatID`. The test directly calls `JsonUtility.FromJson<PlayerDataWrapper>` where `PlayerDataWrapper.Characters` ultimately use **current** `CharacterData`; this does not call `PlayerDatabase.MigrateLegacyData` / `MigrateLegacyCharacter`.
- `PlayerDatabase.LoadFromDisk` has a separate version-0 legacy path, and the fixture has no explicit `DataVersion` field; thus the production migration route must be tested **as a distinct behavior**, not assumed to be disproved or proven by direct deserialization into current DTOs.
- **Assessment:** The current regression test's loading strategy does not faithfully exercise the migration it purports to protect. Correct test coverage should preserve the legacy fixture and check that the **real versioned migration path**, followed by current-version save/load, retains all identity, inventory/equipment, progression and resource fields. If that exposes a genuine data-loss bug, raise a separate targeted persistence-code fix. Do not rename the legacy fixture keys merely to make a current-DTO test pass.
- Actual outcome: **failed**. End-to-end legacy migration and round trip remain **unverified**.

**Runner observation:** Repeated EditMode tests emit `GameConfigurationManager` already-initialized warnings; they do not constitute the reported failing assertion. Address this separately only if reproducible state pollution affects tests or lifecycle behavior.

**Gate consequence:** These XML files materially improve evidence (the Unity suites *did* run locally) but do **not** satisfy Issue #40's full regression gate. Rerun both suites after focused corrections, preserve passing XML evidence and record the exact tested Git SHA/build. The GitHub-hosted workflow still lacks a successful run.

## 6. Pre-restyle persistence compatibility

### Evidence present

- A tracked baseline JSON file exists: [`Assets/Tests/Fixtures/player_data_phase0_baseline.json`](../Assets/Tests/Fixtures/player_data_phase0_baseline.json).
- The fixture contains a single representative account and character with class/race, experience/level, known spells, inventory, equipment, weapon skills, talents, reputations, quests, and resource state. Its naming and member layout include legacy spellings such as `Guid`, `PvPFactionID` and `ItemGuid`.
- [`CharacterPersistenceRoundTripTests.cs`](../Assets/Tests/PlayMode/Persistence/CharacterPersistenceRoundTripTests.cs) declares one PlayMode/NUnit test that reads that fixture, asserts individual character fields, serializes via `JsonUtility.ToJson`, deserializes, and compares the resulting state.
- [`PlayerDatabase.cs`](../Assets/Runtime/Server/Persistence/PlayerDatabase.cs) declares `CURRENT_DATA_VERSION = 1`, a version-0/legacy migration branch, and a current-version JSON load/save path.

### Evidence missing

- A **local execution result exists and explicitly failed**: `CharacterData.GUID` was `null` after direct deserialization of the legacy fixture into the current DTO. The failed test did not invoke the version-0 production migration, so a successful migration is **not established**, nor is a migration defect conclusively proved.
- The round-trip test exercises the fixture through a test wrapper and Unity JSON serialization; it does **not** by itself prove production `PlayerDatabase.LoadFromDisk` migrated actual pre-restyle saved data end-to-end and persisted it correctly afterward.
- No verified manual restore/migration of a real pre-restyle `player_data.json` save was supplied with Issue #40.

**Issue #40 requires an affirmative pre-restyle save load/migration and round-trip validation, not merely presence of a migration method or test.** The requirement therefore remains **unverified**. Any future proof should identify the legacy fixture/save provenance, tested Unity build/SHA, test steps, restored fields, resulting saved file version and outcome, without committing real player data.

## 7. Phase 0 integrated manual validation record

Issue #40 requires the user to run the actual integrated game **before** certifying the Phase 0 Stable Foundation milestone. The source review cannot replace those steps.

| # | Required manual check from Issue #40 | Evidence status |
| ---: | --- | --- |
| 1 | Headless server starts | **Not independently verified** |
| 2 | Client connects and authenticates | **Not independently verified** |
| 3 | Character creation and selection work | **Not independently verified** |
| 4 | Character enters world | **Not independently verified** |
| 5 | Actor spawning and ownership work | **Not independently verified** |
| 6 | Movement, correction and chunk streaming work | **Not independently verified** |
| 7 | Targeting, combat and auto-attack work | **Not independently verified** |
| 8 | Spell start/cast/interrupt/finish work | **Not independently verified** |
| 9 | Damage, healing, aura, cooldown and stat replication work | **Not independently verified** |
| 10 | NPC death, progression, loot and quest effects work as currently designed | **Not independently verified** |
| 11 | Inventory, equipment, talents, reputation and weapon skills persist | **Not independently verified** |
| 12 | Disconnect/reconnect preserves state | **Not independently verified** |
| 13 | Pre-restyle persistence still migrates | **Not independently verified** |
| 14 | No missing scripts or lost serialized values | **Not independently verified** |

**Not independently verified is not synonymous with failed.** These checks have not been demonstrated using accessible evidence in this review. If a real check fails, Issue #40 requires a focused regression issue and revalidation, not a retrospective claim of success.

Certain complete production behaviours (for example, graceful disconnect/reconnect, instance-qualified spatial ownership and SQL-backed persistence) are already documented as **Phase 1 obligations** in their authoritative PDDs. Do not confuse the Phase 0 *existing-game behaviour smoke test* with certification of those future requirements.

## 8. Known deliberately unresolved and deferred work

The comprehensive audit is [Phase 0 — Open Design and Implementation Gaps](Plans/Phase-0-Open-Design-and-Implementation-Gaps.md). That source currently has **status `Needs manual validation`** and enumerates **GAP-01 through GAP-21**. Its classifications (design unresolved, implementation missing, prototype/testing scaffold, deferred productionisation, resolved/stale marker) are preserved and not silently reinterpreted as Phase 0 regression failures.

Relevant named entries include:

- **GAP-02 / GAP-03:** authored player spawn/invalid-location recovery and world selection/bootstrap still use prototype scene/position assumptions. Correct completion belongs to World Runtime and Account/Persistence requirements.
- **GAP-05:** per-frame, per-NPC `Physics.OverlapSphere` aggro candidate queries and allocation-heavy scanning; Phase 1 spatial/AI performance work.
- **GAP-09:** complete actor despawn lifecycle, associated ECS cleanup and reconnect-safe identity ownership remain unresolved.
- **GAP-17:** client audio resource lifetime and streaming/concurrency policies need productionisation.
- **GAP-21:** runtime definition-library `Resources` references need controlled migration for hotfixable gameplay content.
- Further identified gaps address trainer/known-spell seeding, authoritative equipment/talent validation, casts and combat effects, and provisional death-credit/group-loot logic. The owning PDDs, not this baseline, determine desired behaviour.

Do not infer that *all* audit items are Phase 1 blockers: their respective audit entries state classification and phase ownership. The separate [Phase 1 Implementation Plan](Plans/Phase-1-Implementation-Plan.md) schedules scalable simulation, spatial queries, actor activity, replication, persistence, compatibility, client resource lifetime and integrated load testing.

Other production-wide issues intentionally remain outside Phase 0: persistence transactions and one-account-one-session semantics; authoritative reconnect grace and saved location; release manifests, PurrNet/network protocol compatibility, remote Addressables and stable content revisions; full logical world/map/instance isolation; and high-density performance targets. Phase 0 is a stabilisation baseline, not proof of production readiness.

## 9. Phase 0 acceptance status and remaining evidence

| Issue #40 exit condition | Evidence and status |
| --- | --- |
| Exact candidate `dev` commit recorded | **Recorded:** `f3c16ffc` |
| Unity version recorded | **Recorded:** `6000.3.10f1` |
| Shared/Client/Server/Core/Editor assembly layout recorded | **Recorded:** Section 3 |
| Endpoint/Binding/Service/Manager terminology recorded | **Recorded:** Section 4 |
| EditMode/PlayMode test counts and actual outcomes recorded | **Source:** 25 project EditMode / 3 project PlayMode. **Local execution:** 24/25 project EditMode passed, 2/3 project PlayMode passed; both suites failed. Third-party EditMode: 1/1 passed. Exact tested commit unknown. |
| Successful GitHub Actions workflow link | **Missing:** all accessible runs failed before tests |
| Pre-restyle persistence load and round-trip confirmed | **Missing:** test and fixture present, successful execution/migration not demonstrated |
| Open design/prototype gap audit linked | **Recorded:** Section 8; audit's own manual-validation status remains open |
| Integrated server/client/manual checklist | **Pending:** Section 7 |
| Strict review and user approval of baseline diff | **Pending:** required by workflow |
| Roadmap marks Phase 0 as completed | **Not changed by this reconstruction:** must not be declared complete before validation |
| Historical restyling plan notes true completion commit | **Not changed:** completion is unverified; `f3c16ffc` is labelled candidate snapshot rather than accepted final completion |

### To certify Phase 0

1. Correct and rerun the two failed project-owned regression tests described in Section 5.2. Verify Y-coordinate boundary semantics and validate the actual legacy persistence migration rather than bypassing it. Preserve complete successful XML results and the exact tested commit.
2. Resolve or document access to the Unity Actions licensing configuration without exposing secrets; execute both jobs and retain the passing run URL, test counts and output artifacts.
3. Perform Issue #40's 14 integrated manual checks on a build identified by the candidate commit and Unity version; record exact outcomes and any corrective commits.
4. Execute and validate legacy persistence migration and its round trip using preserved pre-restyle data without changing or exposing private player saves.
5. Review the open-gap audit and ensure earlier Phase 0 manual-validation gates are genuinely satisfied, rather than relying solely on issue closure.
6. If validation requires new source changes, identify a new candidate commit and repeat relevant checks. The **accepted baseline SHA must match the tested revision**.
7. After independent user approval, update this document with evidence, add the completion-status note to [C# Restyling Plan](Plans/CSharp-Restyling-Plan.md), mark only Phase 0 completed in the [Project Roadmap](Plans/Project-Roadmap.md), and clear the `needs-validation` label through the repository workflow.

**The newly supplied local Unity tests were performed by the user, not by this documentation update.** No Unity Editor access, build, new test execution, successful CI test, manual gameplay test or persistence migration was performed by the assistant. The reported local tests failed and do not certify Phase 0.
