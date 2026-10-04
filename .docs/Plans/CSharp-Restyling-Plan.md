# Ninth Age — C# Restyling Plan

**Status:** Implementation plan  
**Repository:** `FrontierDev/ninthage`  
**Target branch:** `main`  
**Audit baseline:** `478488ac9f96f01d4d4284d5e70b2c130906a6c4`  
**Style authority:** `.docs/CSharp-Style-Conventions.md`  
**Audit scope:** All C# source files under `Assets/`

## 1. Purpose

This plan brings the existing C# codebase into compliance with the project's locked C# style and naming conventions without intentionally changing runtime behaviour, serialized game data, network behaviour, Unity asset references, or public data contracts.

The audit is exhaustive at the source-file level. Each identified convention breach is listed in the audit appendix with the exact change required.

## 2. Non-functional-change requirement

Restyling must be performed as a behaviour-preserving refactor.

The following safeguards are mandatory:

1. **Do not combine restyling with gameplay or architectural changes.**
2. **Rename symbols atomically.** Every declaration and every code reference must be updated in the same change.
3. **Preserve Unity `.meta` files when renaming C# files.** File renames must be performed as moves so the existing Unity asset GUID is retained.
4. **Protect serialized private-field renames.** When a Unity-serialized field is renamed to the required `_camelCase` or acronym form, add `[FormerlySerializedAs("oldName")]` and retain it until all relevant assets have been resaved and migration has been verified.
5. **Do not rely on `FormerlySerializedAs` for runtime persistence compatibility.** Persisted `JsonUtility` DTO field-name changes must have an explicit backward-compatible load/migration path before the old field name is removed.
6. **Protect serialized managed-reference type moves.** Namespace/type renames affecting `[SerializeReference]` data must be migrated explicitly and validated against existing assets. Use Unity type-move metadata where applicable and verify that no managed references become missing.
7. **Preserve network contracts.** Renaming C# symbols must not change serialized packet layout, RPC direction, RPC attributes, or protocol semantics. Any network payload member whose name is externally persisted or reflected must retain compatibility.
8. **Namespace changes are compile-wide refactors.** Update all `using` directives, fully-qualified references, editor references, generic constraints, attributes, and type references in one coherent change.
9. **Run validation after each phase.** At minimum: Unity script compilation, Editor compilation, client compilation, dedicated-server compilation, automated tests, and a serialization/persistence smoke test.
10. **No asset reserialization should be accepted blindly.** Review scene, prefab, ScriptableObject, and settings diffs caused by symbol/namespace renames.

## 3. Implementation order

### Phase 1 — Pure formatting and syntax normalization

Apply changes that do not rename symbols or types:

- alphabetize `using` directives;
- use `var` where the type is mechanically obvious and readability is not reduced;
- use target-typed `new()` for member construction where the declared type already provides the type;
- add missing XML documentation to public APIs.

Compile after this phase.

### Phase 2 — File-name-only normalization

Perform file moves while preserving each file's existing `.meta` file:

- correct filename/type mismatches;
- convert partial-class filenames from `Type.Section.cs` to `Type_Section.cs`.

Do not rename the contained type unless separately required by the style guide.

Compile and reopen Unity after this phase to confirm all MonoScript references remain intact.

### Phase 3 — Private fields, constants, and static readonly members

Rename:

- private instance/static fields to `_camelCase`;
- constants to `SCREAMING_SNAKE_CASE`;
- static readonly fields to `SCREAMING_SNAKE_CASE`;
- acronym-bearing field names to the required uppercase acronym form.

For every serialized field rename, add the required compatibility metadata before changing the identifier.

Compile, open representative scenes/prefabs/ScriptableObjects, and verify serialized values.

### Phase 4 — General symbol and test-type naming

Apply coordinated codebase-wide renames for:

- acronym forms such as `Id` → `ID`, `Guid` → `GUID`, and `PvP`/`Pvp` → `PVP` in project-defined identifiers;
- test/prototype types to the `Test_` prefix;
- non-RPC methods that do not use PascalCase;
- callback/event-like members that do not use the `on...` convention;
- RPC methods that do not use the required `Server_`, `Client_`, or `Observers_` prefix.

Rename matching files where the primary type name changes.

Compile client and server after each coherent rename set rather than batching unrelated symbol families together.

### Phase 5 — Namespace mirroring

Move each source file's namespace to the namespace implied by its folder path.

This is the highest-risk style-only phase because namespace changes affect cross-file references and can affect Unity serialized managed-reference type identities.

Process one architectural subtree at a time:

1. `Game.Shared`;
2. `Game.Server`;
3. `Game.Client`;
4. `Game.Editor`;
5. `Game.Core` if any subfolder namespace change is required.

For each subtree:

- change namespace declarations;
- update imports and fully-qualified references;
- update editor drawers/inspectors and generic type references;
- migrate serialized managed-reference type names where required;
- compile immediately;
- load affected data assets and confirm no missing managed-reference types.

### Phase 6 — Final verification

After all audit items are complete:

- run Unity compilation with no C# errors;
- run the Editor test suite;
- run client and headless-server builds;
- load existing persisted player data;
- load representative ScriptableObjects containing `SerializeReference` data;
- load representative scenes and prefabs;
- exercise login, character selection/creation, world entry, chunk streaming, combat, spellcasting, inventory, quests, and UI;
- confirm no missing scripts, missing managed references, or reset serialized fields;
- rerun the convention audit and require zero hard convention breaches.

## 4. Audit interpretation

Each file entry below lists only changes required by the locked style guide. A listed rename is a **symbol refactor**, not a textual search-and-replace: all references must be resolved by symbol identity or verified through compilation.

Items labelled **serialized** require the serialization safeguards above.

The `var` and target-typed construction rules are preference rules rather than absolute syntax bans. They are listed only where the existing expression makes the intended type mechanically obvious.

---

## 5. Exhaustive file-by-file audit
