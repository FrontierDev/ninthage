# GitHub Issue, Label and Validation Workflow

**Status:** Active project workflow  
**Applies to:** Ninth Age repository issues, milestones, GitHub Project fields, Codex implementation and validation  
**Primary branch:** `dev`

# 1. Purpose

This document defines how implementation work is represented and controlled in GitHub.

The objective is to keep work:

- focused and independently reviewable;
- traceable to the project roadmap and PDDs;
- explicit about the exact code or tests being changed;
- conservative toward the existing working game foundation;
- safe to implement with Codex;
- manually validated before dependent work continues.

The running game is the behavioural baseline. An issue must not use cleanup, restyling or architectural consistency as justification for redesigning behaviour that already works.

# 2. Core Rules

## 2.1 One focused purpose per issue

An issue should solve one coherent problem or perform one coherent mechanical migration.

Do not combine unrelated cleanup, bug fixes or subsystem changes into one issue merely because the same files are nearby.

If implementation discovers unrelated work, report it separately rather than fixing it opportunistically.

## 2.2 Code changes and regression tests are separate issues

An issue must be one of the following:

1. **Codebase issue** — modifies production code, documentation, configuration, workflows or repository structure; or
2. **Regression-test issue** — adds or modifies regression tests and test-only support files.

Do not combine production changes and new regression tests in the same issue.

If a production change requires critical uncovered regression coverage, **first reuse an existing regression-test issue/fixture**. Keep test-only changesets separate from production changes; create a distinct test issue only when the owner approves a genuinely uncovered separate test deliverable. Avoid generating tickets for every change or speculative edge case.

## 2.3 Preserve working behaviour

Unless an issue explicitly identifies defective or temporary prototype behaviour:

- preserve gameplay behaviour;
- preserve networking semantics;
- preserve persistence semantics;
- preserve ECS update behaviour;
- preserve serialization;
- preserve asset references;
- preserve scene/prefab behaviour.

Mechanical cleanup must remain mechanical.

## 2.4 One focused change per issue; commit authority

Each ordinary implementation issue should result in **one focused change set** on `dev`. When a GitHub commit is expressly authorized by the owner, prefer one focused commit per issue and stop for validation afterward. **For an explicitly owner-approved multi-subsystem implementation within one existing issue, use separately reviewable phases/changesets and an owner validation gate after each; do not require one large all-at-once diff or generate one child ticket per changeset.** A reviewable phase is a work unit within the same issue, not a new architecture owner.

**`AGENTS.md` currently prohibits Codex from committing or pushing by default.** Without explicit owner permission to commit, Codex must prepare and validate the focused change without committing/pushing, then report the diff and manual validation required. This guide never overrides `AGENTS.md` or grants commit permission merely by defining a workflow. Do not automatically proceed to the next issue.

## 2.5 Third-party code protection (mandatory for every issue)

**Mandatory third-party boundary:** Third-party code is read-only unless the user explicitly requests a change to the specific third-party code. This includes imported packages, plugins, SDKs, tools, and externally authored source anywhere in the repository (including `Assets/` and `Assets/Editor/`). **Never modify third-party namespaces**, identifiers, files, assembly definitions, metadata, or vendor-owned assets. Do not restyle, rename, relocate, patch, reformat, or regenerate them. Follow `AGENTS.md`; if the task requires a third-party change, stop and request explicit permission. GitHub issues, plans, general refactor requests, and coding standards do not grant that permission.

Every issue must be implemented without changing third-party code or its namespace. Issue scope, labels, and `ai-safe` status never override this rule. Review changed paths and ownership before claiming completion.

## 2.6 Small, atomic implementation scope (mandatory)

A production implementation issue must have **one directly testable behavioural invariant or mechanical change**. A roadmap workstream spanning spell casting, cooldowns, aura management, actor lifecycle and ECS scheduling is a **tracking issue**, not a single Codex implementation assignment.

Default planning target (not a hard line-count quota): **one primary subsystem, one to three production files, and about 150 or fewer net new production lines**. Treat multiple authoritative state owners, new queues/maps, and changes exceeding that target as mandatory scope-review triggers even if the diff is short.

Before starting an oversized issue, **first check for an existing issue that already owns the work** and whether the owner has approved implementing it in multiple internal phases. Do **not** automatically create child tickets or mark the parent unimplementable. When one coherent approved architecture spans several production owners, keep **one existing issue** with ordered internal phases, each naming exact methods, state transitions, prerequisites, non-goals, separately reviewable changesets and owner validation gates. Implement one phase per review cycle, continuing the same issue only after validation. Create a separate issue **only** for a distinct, independently owned deliverable that cannot reasonably stay in the existing issue, and only with explicit owner approval. The default 1–3 files/~150 net new lines is a review guideline **per changeset**, not a hard total-issue limit. Do not meet targets by minifying code or omitting needed lifecycle handling.

## 2.7 Defect evidence and test-first investigation

Classify every review finding before requiring a production revision:

1. **Confirmed defect:** reproducible failure, directly proven incorrect execution path, or conflict with a stated approved contract. Include concrete reproduction or code proof, expected versus actual behaviour, and the narrowest affected method(s). Require a targeted fix.
2. **Potential defect:** plausible but unverified edge case, inferred race, or unclear semantics. **Investigate or add a focused regression test first** (test-only issue); do not demand production changes or add defensive state based on possibility alone.
3. **Future concern:** improvement outside the current acceptance criteria. Record it independently when actionable; otherwise do not create work.

Avoid review ping-pong: a review must distinguish evidence from conjecture, acknowledge when an earlier concern was only hypothetical, and refrain from repeatedly expanding code to cover conceivable failures. A test that exposes real incorrect behaviour justifies a subsequent narrow production fix. A test that confirms existing behaviour does not justify new infrastructure.

The separation of production and regression-test issues in §2.2 still applies: “test first” means a **separate test-only issue/commit**, not mixing tests and runtime edits in one issue.

## 2.8 Mandatory implementation-ready issue specification

Every **Codex-executable implementation issue** must be written as an implementation contract, not as a design prompt or high-level task summary.

**Reference quality bar:** issue **#161** and the revised **#166–#170** demonstrate the expected level of implementation specificity. Future issues do not need to copy their wording, but they must provide the same kind of executable detail for the work they own.

An implementation issue is sufficiently specified only when a developer or fresh Codex session can implement it from **latest `dev` + the cited repository documents + the issue body**, without needing conversation history and without inventing architecture, state ownership, error semantics, ordering, constants or acceptance behaviour.

Before drafting the issue, the author must inspect the relevant current implementation on latest `dev` and the authoritative PDD/plan/style documents. Do not write target methods, signatures or lifecycle claims from memory when they can be read from the repository.

Every Codex-executable issue must contain all of the following where applicable:

1. **Single behavioural invariant**
   - State one directly testable invariant or mechanical change that the issue owns.
   - If the issue has approved internal phases, state the invariant for each phase.
   - Do not substitute a broad aspiration such as “make navigation robust” or “improve ECS safety.”

2. **Observed/current state**
   - Name the exact current behaviour, limitation, throw, call path, data owner or approved missing requirement that motivates the change.
   - When fixing a defect, distinguish confirmed behaviour from conjecture under §2.7.

3. **Exact target locations**
   - Name every project-owned production file expected to change.
   - Name the exact type, method/member and lifecycle/call-site boundary being modified.
   - Identify important **read-only dependencies** that must be reused but not edited.
   - If a new file/type/method is required, state its exact intended path/name and why the existing owner cannot contain the behaviour.

4. **Exact API contract**
   - For every new or materially changed production method/type, provide the expected signature or an equally precise contract.
   - Define parameter meaning, output meaning, ownership, mutability and whether the API creates/mutates state.
   - State exact `true`/`false`/return-value meanings and the exception/failure classes that represent invalid caller state, unavailable runtime state and internal contradiction.
   - Do not leave Codex to invent public/internal API shape when the issue can specify it.

5. **Ordered implementation operations**
   - Describe the required control-flow/state-mutation order step by step.
   - Identify which validations occur **before** mutation, which mutation is authoritative, and what must happen after it.
   - For transactional/reversible work, specify captured state, mutation order, rollback order and hard-failure behaviour.
   - For frame/tick/lifecycle-sensitive work, state the exact callback/tick boundary and ordering relative to existing owners.

6. **Exact state ownership and reuse**
   - Name the existing dictionary/component/manager/service/endpoint/authoritative owner that must be reused.
   - State whether new persistent/runtime state is allowed. If it is, name and justify it.
   - Explicitly prohibit duplicate authority, parallel caches/registries/managers or fallback ownership when they are not required.

7. **Deterministic rules and constants**
   - Specify required formulas, ordering, tie-breaks, limits, budgets, tolerances, enum/state transitions and coordinate/frame rules when behaviour depends on them.
   - Do not say “use a small tolerance,” “pick the best candidate,” “retry a few times,” or similar unless the issue intentionally owns a design decision and is marked `needs-design`.
   - If an existing project setting/helper is authoritative, name it instead of duplicating the value.

8. **Failure and unavailable-state semantics**
   - State what happens for malformed input, temporarily unavailable resources, stale state, confirmed unreachable/invalid state and internal registry contradictions.
   - State which cases return/no-op/wait/retry and which fail fast.
   - Never leave fallback behaviour implicit.

9. **Preserved behaviour and exact non-goals**
   - Name neighbouring methods/systems/contracts that must remain unchanged.
   - Explicitly exclude likely scope-expansion paths such as new managers, new registries, compatibility fallbacks, unrelated cleanup, test edits in a codebase issue, networking/persistence changes, or future roadmap work.

10. **Objective completion proof**
    - Map the implementation contract to static/automated assertions and exact owner-only manual validation.
    - State source searches, compile targets, expected callback/order/state assertions and representative runtime scenarios.
    - Distinguish checks Codex can actually run from Unity/PlayMode/headless checks the owner must perform.
    - Where a failure path is part of the contract, validation must cover the expected settled state, not merely successful compilation.

### Readiness gate

A Codex-executable issue **must not be marked `Ready`** if any material implementation choice is still left to the implementer, including:

- which production owner should hold the behaviour;
- which files/methods are actually affected;
- whether state is canonical or cached/derived;
- mutation/callback/tick order;
- API return/error semantics;
- rollback/failure behaviour;
- coordinate/reference-frame interpretation;
- required constants/formulas/tie-breaks/budgets;
- whether adjacent behaviour should be preserved or redesigned.

If these cannot yet be specified from the current code and approved documents, create or retain an **investigation/design issue** (and apply `needs-design` when appropriate) rather than disguising discovery work as an implementation issue.

“Codex can inspect the code and figure it out” is **not** an acceptable implementation specification for behaviour, ownership or architecture. Codex may discover exact line numbers and incidental local details, but the issue must already define the intended contract.

Reject vague or bundled assignments such as “fix all timing,” “improve ECS safety,” “refactor the combat managers,” “support cross-region movement,” or a checklist of unrelated subsystem migrations. A description can be long and **still be too broad**: precision of each item does not make several different changes one atomic issue.

## 2.9 Changes discovered during implementation and review

If an unforeseen requirement spans another owner/subsystem or needs a new production state store, **stop and reassess scope before coding**. Check the approved PDD and existing issues before deciding if the requirement is a missing internal phase, a genuine separate issue or an unapproved design change. **Do not automatically create issues**; require owner approval for issue creation or genuinely new authoritative state. Do not add dictionaries, caches, mutation queues, managers, compatibility fallbacks or lifecycle layers solely to anticipate theoretical cases.

Reviewers should distinguish:
- **Acceptance blocker:** confirmed defect of the current issue's required invariant.
- **Regression-test candidate:** plausible scenario whose outcome is unknown.
- **Out-of-scope follow-up:** separate behaviour or optimisation.

A broad Phase 1.x tracker may describe the future end-state. **If the owner authorizes staged implementation in that same issue (e.g. #134), its named internal phases replace a child-issue hierarchy** and each phase must fit a focused review and validation pass.


## 2.10 Test value and critical-coverage gate (mandatory)

**Do not add tests for the sake of tests, coverage percentages or an automatic test-per-issue rule.** Before creating a regression-test issue or adding a test to an existing one, establish that **all four** conditions hold:

1. **Critical behaviour:** It protects an important gameplay outcome, server-authority/security boundary, networking or persistence contract, or authoritative timing/lifecycle invariant.
2. **Credible failure:** The test would catch an observed regression, a realistic failure introduced by the affected change, or a particularly sensitive integration boundary. State the concrete failure mode; speculative permutations do not qualify on their own.
3. **Unique coverage:** Existing tests and reliable assertions do not already adequately guard this behaviour. Reuse existing fixtures and coverage instead of cloning a fixed-tick, render-FPS, restart or cast-identity test into every subsystem.
4. **Meaningful assertion:** The test checks an observable authoritative state, event identity/order/count, rejected action or other user-relevant result. Merely invoking a method without exception, testing a trivial getter, or asserting an implementation detail without a gameplay consequence does not qualify.

**Every executable test issue must explicitly specify for each required test:**
- the critical invariant and concrete failure it detects;
- why existing coverage is insufficient;
- the preconditions/fixture, triggering operation and relevant timing or lifecycle boundary;
- the expected **observable** result (including exact order/count/tick when important);
- the intended test-only file/owner, and what validation can actually be executed versus what needs owner/Unity validation.

**Minimise test count and fixture cost.** Prefer a small number of high-value integration/regression tests where a system boundary is the risk. Avoid duplicated assertions, giant end-to-end fixtures, exhaustive frame-rate or numerical edge-case matrices, trivial property tests, testing currently unimplemented mechanics, and synthetic failure scenarios without a credible production path. When foundational clock/event tests already prove generic cadence or lifecycle, gameplay tests should assert only the new gameplay-specific consequence. Add a further case only when it protects a distinct uncovered critical failure.

A tracker may record broader possible coverage, but **candidates are not mandatory tests**. Reassess each child against the four conditions when the corresponding production implementation exists. Remove or downgrade unjustified candidates to optional investigation/manual smoke checks; do not require an entire checklist simply because it was drafted. A discovered defect still requires evidence-first classification under §2.7. Keep test-only work separate from production changes under §2.2.


## 2.11 One consolidated review comment per issue revision (mandatory)

When the owner asks for a GitHub issue comment containing review findings or required fixes, post **exactly one comprehensive top-level comment per reviewed issue revision** (including the initial implementation). Identify the revision by its commit SHA on the intended branch. **Do not split correctness defects, duplicated code, styling issues, documentation corrections or other required changes into separate comments for the same revision.** Review the complete relevant diff, owning methods and previous review findings **before posting**, so the comment is one self-contained corrective task rather than fragmented instructions.

The single comment must be specific enough to implement like a focused GitHub issue, and contain:

1. **Revision and disposition:** issue number, branch, exact reviewed commit SHA/link, acceptance/blocker status, and which previous findings are resolved versus still outstanding.
2. **Evidence and exact locations:** for each finding, project-owned file path, type, method/member, relevant execution or lifecycle boundary (and line/diff reference when useful), observed versus expected behaviour, and evidence class per §2.7 (confirmed defect, unverified concern, or future work).
3. **Required fixes:** precise minimal edits and expected invariants for every confirmed problem, named existing project owners/helpers to reuse, permitted files/methods, and required callback/tick/ownership ordering. Do not use vague instructions such as "refactor this".
4. **Explicit non-fixes / preserved behaviour:** concrete adjacent files, systems, methods, gameplay values, network/DTO/serialization contracts, timing, third-party boundaries, and unrelated optimisations that **must remain unchanged**. Link deferred architectural work, tests and independent issues rather than silently expanding scope.
5. **Acceptance and validation:** objective source/static/compilation assertions, focused tests where justified, exact owner-only Unity/multiplayer checks, what is still unverified, and the conditions for removing blockers or closing the issue.

Combine all relevant items into **one structured comment** with separate numbered corrective sections as needed. The one-comment rule does **not** authorise bundling unrelated implementation scopes or mixing production changes with regression tests. If a concern belongs to another issue, record the dependency/non-fix in the same comment, not a second review comment.

**Follow-up revisions:** after a *new* revision is pushed, issue at most one new consolidated review comment for the new SHA and explicitly state which earlier requirements were met. If an omission is discovered **after posting** for the *same* revision, edit the existing review comment rather than posting another; if editing is unavailable, tell the owner and do not silently create a second top-level review comment. Do not repeatedly request changes already satisfied.

**Preferred comment outline:** Revision/commit and assessment → resolved prior findings → exact locations and required corrections (with evidence) → non-fixes and preservation constraints → static/runtime validation and closure criteria.


# 3. Issue Title Format

Roadmap work uses:

```text
[Phase X.Y] Imperative or descriptive issue title
```

Examples:

```text
[Phase 0.8] Rename shared networking services to endpoints
[Phase 0.19] Migrate CharacterData persisted identifier names compatibly
[Phase 2.4] Implement authoritative NPC death rewards
```

Where:

- `X` is the roadmap phase;
- `Y` is the ordered implementation step within that phase.

The phase-step number is an implementation-order identifier, not the GitHub issue number.

Do not renumber established issues casually. Dependencies and external references may already use the phase-step number.

# 4. Standard Issue Structure

Issues should use the following structure where applicable.

## 4.1 Purpose

State exactly why the issue exists and what it is intended to accomplish.

The purpose should also make behavioural constraints clear where relevant.

## 4.2 Sequence / dependency

State which prior issue or gate must be completed first.

Example:

```markdown
## Sequence / dependency

Requires **Phase 0.18** to be implemented and manually validated first.
```

Parallel work may omit a strict predecessor when it is genuinely independent.

## 4.3 Exact target locations

Name the exact project-owned files, types, methods/members and relevant lifecycle/call boundaries affected **after inspecting latest `dev`**.

For each target, distinguish:

- **Modify:** exact file + type + method/member expected to change;
- **Create:** exact new path/type/method, only when explicitly required and justified;
- **Read only / reuse:** existing owners/helpers/contracts the implementation must consume but must not alter.

Prefer:

```text
Assets/Runtime/Server/Core/ServerAutoAttackManager.cs
- AutoAttackState
- Update()
- TryCommitReadyRegionHop(...)

Read only:
Assets/Runtime/Server/Core/ServerPhysicsManager_NavMesh.cs
- ValidateRegionSeam(...)
```

Avoid vague scope such as:

```text
Clean up the server.
Fix networking.
Update navigation code.
Refactor the UI.
```

“Relevant files” or “where needed” is not enough for a `Ready` implementation issue. If the correct production owner cannot yet be identified, the work is investigation/design, not implementation.

For broad approved mechanical migrations, reference an authoritative file-by-file manifest and explicitly constrain the issue/phase to the entries assigned to that stage.

## 4.4 Exact changes

Describe the implementation as an **ordered executable contract**, not only an outcome.

Where applicable, specify:

- the exact existing block/branch/callback being replaced or extended;
- required new/changed method/type signatures;
- validation order before mutation;
- authoritative state owner and exact fields/containers affected;
- ordered state transitions/callbacks/tick phases;
- exact return values and exception/failure semantics;
- rollback/restoration order for reversible work;
- constants, formulas, budgets, tolerances and deterministic tie-breaks;
- coordinate/reference-frame conversions and the owner that performs them;
- existing helpers/APIs that **must** be reused;
- behaviour that must remain unchanged;
- the exact point at which canonical state is accepted/committed.

Use code snippets or pseudocode when they materially remove ambiguity. Snippets are a contract for behaviour/order, not permission for unrelated restructuring.

Codex must not be expected to invent architecture, failure handling, numerical rules, API shape or ownership from a short issue description. If those are still undecided, the issue is not implementation-ready.

## 4.5 Exact non-goals

Add a non-goals section whenever nearby code creates a meaningful risk of scope expansion.

Typical examples:

- do not redesign the event architecture;
- do not create a new message bus;
- do not change gameplay values;
- do not rename server domain services;
- do not add compatibility fallbacks;
- do not change tests in a codebase issue.

## 4.6 Static / automated validation

Specify what can be objectively checked without subjective gameplay assessment.

Examples:

- repository searches for stale names;
- Unity compilation;
- Client/Server/Shared assembly compilation;
- EditMode tests;
- PlayMode tests;
- persistence round-trip tests;
- no missing serialized references;
- no unexpected runtime-code diff.

## 4.7 Manual validation

Every implementation stage requires user validation before dependent work proceeds.

State the exact gameplay, Editor, asset or repository checks the user must perform.

Manual validation is not replaced by successful compilation or automated tests.

## 4.8 Codex implementation protocol

Every implementation issue should end with the standard Codex protocol defined in section 11.

# 5. Labels

Labels are used for **exceptional or cross-cutting conditions**.

Do not use labels to duplicate normal Project fields such as Phase, Area, Work Type or Priority.

## 5.1 `bug`

Use when existing behaviour is objectively incorrect.

Examples:

- duplicate reputation mutation;
- null dereference on an invalid request;
- an event subscription that cannot be unsubscribed correctly.

Do not use `bug` merely because functionality has not yet been implemented.

## 5.2 `regression`

Use when behaviour that previously worked has broken.

Do **not** use `regression` simply because an issue adds regression tests.

A test-only issue may still carry this label where the repository convention intentionally groups regression-protection work, but the semantic meaning of the label remains “protecting or addressing previously working behaviour”; it must never imply that the test itself is a regression.

## 5.3 `needs-design`

Use when implementation is blocked by a genuine unresolved design decision.

Do not use it when:

- a PDD already defines the behaviour;
- the implementation is merely unfinished;
- Codex needs to inspect the current code to locate a known implementation point.

An issue carrying `needs-design` should not be implemented until the design is resolved or the issue explicitly concerns documenting the unresolved gap.

## 5.4 `needs-validation`

Use when work requires independent/manual validation before it is accepted.

For phased implementation work, keep this label until the required user validation has been completed.

Removing `needs-validation` means the required validation gate has been passed, not merely that Codex reported success.

## 5.5 `performance`

Use when performance, scalability, memory, frame time, server tick cost or similar efficiency concerns are a material part of the issue.

Do not apply it to ordinary optimization opportunities discovered during unrelated work.

## 5.6 `breaking-change`

Use when the issue changes a public or cross-system contract, including:

- type/API naming consumed across assemblies;
- persistence schema;
- protocol or RPC-facing contract;
- serialized type identity;
- compatibility-sensitive repository structure.

A mechanical breaking change can still be behaviour-preserving.

## 5.7 `security`

Use when authority, authentication, ownership, validation, exploit prevention or another security-sensitive boundary is materially involved.

## 5.8 `ai-safe`

`ai-safe` is an implementation-risk classification.

An issue qualifies only when its requested implementation:

- does **not** create a new production source file;
- does **not** create a new production method or type;
- does **not** require Codex to invent a new architecture or behavioural abstraction.

The exception is regression-test work.

For regression tests, new test-only files, test fixtures, test methods/types and test assembly-definition files are permitted under `ai-safe`.

Examples that can be `ai-safe`:

- editing an existing method body;
- removing confirmed dead code;
- renaming references inside existing files without creating a new production path;
- applying a known field/namespace/style manifest inside existing files;
- adding new files exclusively under the test/fixture structure.

Examples that are **not** `ai-safe`:

- creating a new production source file;
- adding a new production method/type;
- renaming or moving a production source file, because this creates a new repository path and may affect Unity `.meta` identity;
- creating a new workflow or documentation file;
- adding new production migration/helper architecture;
- discovery-driven cleanup whose exact resulting changes are not predetermined.

### Scope expansion

If an `ai-safe` issue unexpectedly requires any non-AI-safe operation:

1. stop implementation;
2. report the requirement;
3. remove or reconsider the `ai-safe` classification before proceeding;
4. apply the strict manual-validation requirements in section 9.

Do not silently expand an `ai-safe` issue.

# 6. GitHub Project Fields

Normal work classification belongs in GitHub Project fields, not labels.

## 6.1 Phase

Use the roadmap phase:

```text
P0
P1
P2
...
P10
```

The Phase field should match the issue title and milestone.

## 6.2 Area

Use the primary subsystem affected by the issue.

Standard areas include:

- Architecture
- Networking
- Server
- Client
- Persistence
- World
- Combat
- Character
- NPC/AI
- Items
- UI
- Quests
- Groups
- Economy
- Social
- PvP
- Tools
- Live Ops

Choose the area that owns the work rather than listing every subsystem touched by reference updates.

## 6.3 Work Type

Use the nature of the work:

- Feature
- Refactor
- Bug
- Test
- Tooling
- Documentation
- Content

A correctness fix is normally `Bug`; a behaviour-preserving naming or namespace migration is normally `Refactor`.

## 6.4 Priority

Use:

- Critical
- High
- Normal
- Low

Priority reflects project impact and dependency pressure, not how easy the issue is.

## 6.5 Size

Use:

- XS
- S
- M
- L
- XL

Size estimates implementation/review scope, including migration and validation risk.

A mechanically simple change can still be large if it affects many serialized assets or cross-assembly references.

## 6.6 Status

Use:

- Backlog
- Ready
- In Progress
- Blocked
- Validation
- Done

Recommended transitions:

```text
Backlog
  ↓
Ready
  ↓
In Progress
  ↓
Validation
  ↓
Done
```

Use `Blocked` whenever a dependency, design decision or external requirement prevents progress.

An implementation should enter `Validation` after Codex has completed its focused commit and before the user validation gate is approved.

# 7. Milestones

Milestones represent **roadmap exit gates**, not subsystems or short-lived sprints.

Current roadmap milestone structure:

| Phase | Milestone |
|---|---|
| Phase 0 | Stable Foundation |
| Phase 1 | Durable Character |
| Phase 2 | Playable MMORPG Loop |
| Phase 3 | One Complete Zone |
| Phase 4 | Five-Player Dungeon |
| Phase 5 | Persistent Social Economy |
| Phase 6 | World Alpha |
| Phase 7 | Endgame Alpha |
| Phase 8 | Closed Alpha Ready |
| Phase 9 | Release Candidate |

Issues should normally be assigned to the milestone for their roadmap phase.

Phase 10 is a continuing live-service phase and does not need one permanent “complete” milestone. Use release/version/content milestones for live service, such as `Launch 1.0`, `1.1`, `Season 1`, etc.

# 8. Validation Policy for All Issues

Every phased implementation issue requires user validation before the next dependent issue proceeds.

At minimum:

1. Codex prepares one focused change set **or one focused approved internal phase** (and commits **only when explicitly authorized** under §2.4);
2. automated/static validation passes;
3. the issue/phase is marked for review (`Validation` Project status where appropriate);
4. the user performs the stated manual checks or explicitly records that checks remain pending;
5. `needs-validation` is removed **only when the relevant required owner validation is complete**;
6. the issue enters `Done` only once its whole agreed acceptance gate is met. **If the owner explicitly closes a code-complete issue before live validation (as with #131), keep `needs-validation`, record the exact outstanding runtime gate in the closure, and do not treat GitHub `closed` alone as evidence a downstream prerequisite has passed.**

For a phased **single** issue, keep it open while internal phases progress; record phase completion/review in a short issue checklist or existing issue discussion, not new tickets. Preserve stable issue numbers and avoid Project status churn per internal phase. Do not open child issues as a proxy for checkpoint bookkeeping.

The exact validation burden depends on whether the issue is `ai-safe`.

# 9. Strict Validation for Non-AI-Safe Issues

Every non-`ai-safe` issue requires strict manual inspection.

Before accepting the issue, the user must:

1. inspect **every changed file**;
2. inspect **every diff hunk**;
3. inspect every created, renamed, moved or deleted file;
4. inspect the associated Unity `.meta` change for every Unity asset/file move;
5. inspect every new or renamed production method/type line-by-line;
6. confirm every change is explicitly required by the issue;
7. confirm there is no unrelated cleanup;
8. confirm there are no unexpected generated files;
9. confirm no fallback behaviour or speculative abstraction has been introduced;
10. complete all issue-specific runtime/Editor validation.

Codex must finish a non-AI-safe issue by reporting:

- the complete changed-file list;
- every created file;
- every renamed/moved file;
- every deleted file;
- every created production method/type;
- every renamed production method/type;
- all validation performed.

If an unrequested new production file/method/type appears, the issue must not be accepted without explicit review.

# 10. Unity-Specific Change Rules

## 10.1 File moves and renames

For C# assets renamed or moved inside Unity:

- preserve the existing `.meta` GUID;
- never delete and recreate the script merely to rename it;
- validate prefabs/scenes/assets that reference the script.

## 10.2 Serialized field renames

Every planned rename of a Unity-serialized field must:

1. add `FormerlySerializedAs` with the exact old name;
2. rename the field;
3. update all C# references;
4. update Editor `SerializedProperty` paths/reflection strings;
5. inspect representative existing assets;
6. confirm no inspector values reset.

## 10.3 Persisted data

A persisted-field rename requires an explicit backward-compatible migration and round-trip validation.

Never treat a C# member rename as sufficient for JSON/database compatibility.

## 10.4 Behaviour-preserving refactors

Compilation is necessary but not sufficient.

For networking, serialization, ECS, persistence and Unity asset changes, the issue must state the relevant runtime/manual smoke tests.

# 11. Standard Codex Implementation Protocol

Unless an issue has a stricter protocol, use the following.

```markdown
## Codex implementation protocol

1. Use **GPT-5.6 Luna High** for implementation.
2. Work from the latest `dev` branch only. Do not modify `main`.
3. Before editing, read `AGENTS.md` and the relevant authoritative project documents:
   - `.docs/CSharp-Style-Conventions.md`;
   - `.docs/CSharp-Architecture.md`;
   - `.docs/MMORPG-Master-PDD.md`;
   - `.docs/Plans/Project-Roadmap.md`;
   - relevant PDDs and implementation plans.
4. Re-read every target file from the current branch before editing. Treat the issue's exact targets, signatures, ordered operations, failure semantics, preservation constraints and non-goals as an implementation contract; do not replace them with a different design merely because another implementation seems cleaner.
5. Preserve the running game's existing behaviour unless the issue explicitly identifies defective or temporary behaviour.
6. Keep production-code issues and regression-test issues separate.
7. Keep the change strictly within the issue's exact target scope.
8. Preserve Unity `.meta` GUIDs and all required serialization/persistence compatibility.
9. Do not fix unrelated findings. Report them separately.
10. Do not add speculative abstractions, compatibility fallbacks or new gameplay-design decisions.
11. Run every static, automated and manual-preparation validation listed by the issue.
12. Prepare one focused change set on `dev`; commit only with explicit user authorization consistent with `AGENTS.md`, then stop for user validation.
13. If the issue conflicts with an authoritative design/style/implementation document, stop and report the conflict rather than improvising.
14. Do not modify ANY third-party code or namespaces unless explicitly requested by the user for that third-party code; verify the final diff contains no unauthorized third-party changes. If required, stop and ask.
```

For a regression-test issue, explicitly add:

```text
Do not modify production/runtime code. Only test assemblies, test assets,
fixtures and test-only helpers may change.
```

For a codebase issue, explicitly add:

```text
Do not add, remove or modify regression tests in this issue.
```

For a non-`ai-safe` issue, also include the strict manual-validation section from section 9.

### 11.1 Minimal-diff implementation and validation reporting

Before writing production code, Codex must list the **specific behaviour/invariant it will change**, the existing owner(s) it will reuse, the intended files/methods, and whether a new state container/helper is truly necessary. Do not convert every reviewer hypothesis into a production edit.

At completion, report net new production lines, changed production files, new persistent/runtime state owners, and the actual static/unit/manual checks performed. If the diff substantially exceeds the approved small scope, stop for owner scope review rather than claiming success because it compiles. Avoid repeated corrective revisions without a demonstrated regression or contract violation.


# 12. Issue Readiness Checklist

A Codex-executable implementation issue is `Ready` only when **all applicable items below are explicit in the issue body**:

- purpose and current/observed state are clear;
- one directly testable behavioural invariant is stated;
- roadmap phase and milestone are assigned;
- Area, Work Type, Priority and Size are set;
- dependencies and owner-validation gates are explicit;
- relevant latest-`dev` implementation and authoritative documents were inspected before drafting;
- every expected production file is named;
- every affected type/method/member or call/lifecycle boundary is named;
- read-only dependencies/existing owners to reuse are named;
- every new/changed production API has an exact signature or equivalently precise contract;
- ordered validation/mutation/callback/tick operations are specified;
- authoritative state ownership and any permitted new state are explicit;
- return/no-op/wait/retry/exception semantics are explicit;
- rollback or failure settlement is specified where mutation can partially succeed;
- required constants/formulas/tolerances/budgets/tie-breaks/reference frames are specified or delegated to a named existing authoritative setting/helper;
- preserved behaviour is named;
- exact non-goals prevent likely scope expansion;
- unresolved design is absent, or the issue is instead blocked/`needs-design`;
- static/automated validation maps to the requested changes;
- owner-only manual validation names concrete runtime scenarios and expected state;
- `ai-safe` classification has been considered;
- the Codex protocol is present for AI implementation.

**Readiness rejection rule:** if a fresh implementer would still need to decide *what architecture or behaviour the project wants*, the issue is not `Ready`. “Inspect the code and decide” is acceptable only for incidental implementation detail, not for authority, semantics, ordering, error handling or design.

Tracking, investigation and design issues may intentionally omit executable details, but they must not be handed to Codex as production implementation assignments until converted into the implementation-ready form required by §2.8 and §4.

# 13. Completion Checklist

An issue is `Done` only when:

- the focused change is on `dev` (committed only when permitted by §2.4);
- the change is limited to the issue scope;
- all automated/static validation passes;
- required user manual validation passes;
- strict every-hunk review has passed for non-`ai-safe` work;
- no unresolved regression remains;
- `needs-validation` has been cleared;
- relevant documentation is updated when the issue explicitly owns that documentation;
- dependent work may safely proceed.

## 13.1 Review-evidence completion gate

Do not hold an otherwise compliant implementation open **solely** on speculative edge cases. Those belong in focused regression tests or investigation issues. Confirmed failures of an explicit acceptance criterion remain blockers; unresolved design decisions remain `needs-design`.

Before closing a production issue, ensure its *owned* invariants are proven with the available tests/static evidence and the user's stated manual checks. A wider phase gate or follow-on test-only issue may still be pending without inventing additional runtime requirements for the production issue.


# 14. Example Issue Skeleton

The following is the **minimum shape**, not a licence to use one-line sections. Each section must contain the implementation-level detail required by §2.8 and §4. Reference issues #161 and revised #166–#170 for the expected granularity.

````markdown
# [Phase X.Y] Imperative implementation title

**GitHub Project classification:** Phase ... · Area ... · Work Type ... · Priority ... · Size ... · Status ...
**Milestone:** ...

## Purpose

State the current/observed behaviour and the approved objective.

**Single invariant:** state one directly testable invariant owned by this issue.

## Sequence / dependency

Requires #... implementation and owner validation.
Name the authoritative PDD/plan sections governing the work.

## Exact target locations

### Modify
- `path/to/File.cs`
  - `TypeName.MethodName(...)`
  - exact block/state owner being changed

### Create
- `path/to/NewType.cs` (+ `.meta`) — only if explicitly required and justified

### Read only / reuse
- `path/to/ExistingOwner.cs`
  - `ExistingHelper(...)`

## Exact changes

### 1. Exact API/state change

Provide required signature/member/state shape and ownership.

```csharp
internal bool RequiredMethod(
    RequiredType input,
    out ResultType result)
```

Define exact return/error semantics.

### 2. Ordered implementation operations

1. Validate ...
2. Read authoritative ...
3. Reject/wait/return ...
4. Mutate existing owner ...
5. Verify ...
6. Publish/commit exactly once ...

State callback/tick/lifecycle order and coordinate/reference-frame rules.

### 3. Failure and rollback behaviour

- malformed caller input → ...
- temporarily unavailable runtime state → ...
- stale state → ...
- partial mutation failure → rollback ... then throw ...
- internal contradiction → fail fast ...

### 4. Deterministic rules

Specify formulas, constants, budgets, tolerances, tie-breaks or the exact existing setting/helper that owns them.

### 5. Preserve existing behaviour

Name methods/systems/contracts that must remain unchanged.

## Exact non-goals

- Do not create ...
- Do not redesign ...
- Do not change ...
- Do not modify regression tests in this codebase issue.

## Static / automated validation

- exact compilation target;
- exact repository/source assertions;
- exact expected state/order/count assertions;
- existing focused tests to run, when justified.

## Manual validation

Owner Unity/PlayMode/headless:
1. scenario → exact expected behaviour/state;
2. failure scenario → exact expected settled state;
3. regression scenario → behaviour that must remain unchanged.

## Codex implementation protocol

Use the standard protocol from `.docs/GitHub-Issue-and-Workflow-Guide.md`,
plus issue-specific scope/failure constraints.

## Strict manual validation — non-AI-safe

Include the §9 requirements when the issue is not `ai-safe`.
````

The issue body should be sufficiently specific that another developer or Codex session can implement it without reconstructing the intended design from conversation history.
