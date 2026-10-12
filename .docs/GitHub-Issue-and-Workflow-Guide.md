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

If a production change requires regression coverage, create a separate regression-test issue before or after it as appropriate to the dependency order.

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

## 2.4 One focused commit per issue

Each issue should result in one focused commit on `dev`.

Codex must stop after that commit and report the validation required. It must not automatically proceed into the next issue.

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

Name the exact files, types and methods affected.

Prefer:

```text
Assets/Runtime/Server/Services/EnterWorldService.cs
- EnterWorldService(...)
- ValidateCharacterGUID()
```

Avoid vague scope such as:

```text
Clean up the server.
Fix networking.
Refactor the UI.
```

For broad mechanical migrations, reference an authoritative file-by-file manifest and explicitly constrain the issue to the entries assigned to that stage.

## 4.4 Exact changes

Describe the required changes concretely.

Where practical, list:

- old symbol or behaviour;
- required new symbol or behaviour;
- references that must be updated;
- data/serialization migration requirements;
- behaviour that must remain unchanged.

Codex should not be expected to invent the desired architecture from a short issue description.

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

1. Codex completes one focused commit;
2. automated/static validation passes;
3. the issue enters Project status `Validation`;
4. the user performs the stated manual checks;
5. `needs-validation` is removed only after approval;
6. the issue can then move to `Done`.

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
3. Before editing, read the relevant authoritative project documents:
   - `.docs/CSharp-Style-Conventions.md`;
   - `.docs/CSharp-Architecture.md`;
   - `.docs/MMORPG-Master-PDD.md`;
   - `.docs/Plans/Project-Roadmap.md`;
   - relevant PDDs and implementation plans.
4. Re-read every target file from the current branch before editing.
5. Preserve the running game's existing behaviour unless the issue explicitly identifies defective or temporary behaviour.
6. Keep production-code issues and regression-test issues separate.
7. Keep the change strictly within the issue's exact target scope.
8. Preserve Unity `.meta` GUIDs and all required serialization/persistence compatibility.
9. Do not fix unrelated findings. Report them separately.
10. Do not add speculative abstractions, compatibility fallbacks or new gameplay-design decisions.
11. Run every static, automated and manual-preparation validation listed by the issue.
12. Produce one focused commit on `dev`, then stop for user validation.
13. If the issue conflicts with an authoritative design/style/implementation document, stop and report the conflict rather than improvising.
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

# 12. Issue Readiness Checklist

An issue is `Ready` only when:

- its purpose is clear;
- its roadmap phase is known;
- its milestone is assigned;
- its Area, Work Type, Priority and Size are set;
- exact targets are identified where practical;
- the required change is concrete;
- non-goals are defined when scope creep is plausible;
- unresolved design is either absent or marked `needs-design`;
- validation is specified;
- `ai-safe` classification has been considered;
- dependencies are explicit;
- the Codex protocol is present for AI implementation.

# 13. Completion Checklist

An issue is `Done` only when:

- implementation is committed to `dev`;
- the change is limited to the issue scope;
- all automated/static validation passes;
- required user manual validation passes;
- strict every-hunk review has passed for non-`ai-safe` work;
- no unresolved regression remains;
- `needs-validation` has been cleared;
- relevant documentation is updated when the issue explicitly owns that documentation;
- dependent work may safely proceed.

# 14. Example Issue Skeleton

```markdown
# [Phase X.Y] Issue title

## Purpose

State the exact objective and behavioural constraint.

## Sequence / dependency

Requires **Phase X.Z** to be implemented and manually validated first.

## Exact target locations

- `path/to/File.cs`
  - `MethodName()`

## Exact changes

- Required change 1.
- Required change 2.
- Preserve existing behaviour X.

## Exact non-goals

- Do not redesign Y.
- Do not modify regression tests.

## Static / automated validation

- Compilation/search/test requirement.

## Manual validation

- User check 1.
- User check 2.

## Strict manual validation — non-AI-safe

Include this section only when the issue is not `ai-safe`.

## Codex implementation protocol

Use the standard protocol from `.docs/GitHub-Issue-and-Workflow-Guide.md`,
plus any issue-specific constraints.
```

The issue body should be sufficiently specific that another developer or Codex session can implement it without reconstructing the intended design from conversation history.
