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


### Audit batch 1: files 1–15 of 386

#### `Assets/Editor/Components/TerrainTileComponentEditor.cs`
- Alphabetize `using` directives (first at line 1).
- Line 65: add XML documentation to public API `public class TerrainTileComponentEditor : UnityEditor.Editor`.
- Line 69: rename constant `PrefKey` → `PREF_KEY`.
- Line 70: rename constant `PrefKeyCliffThresh` → `PREF_KEY_CLIFF_THRESH`.
- Line 71: rename constant `PrefKeySlopeThresh` → `PREF_KEY_SLOPE_THRESH`.
- Line 72: rename constant `PrefKeyTalusThresh` → `PREF_KEY_TALUS_THRESH`.
- Line 73: rename constant `PrefKeyBlendWidth` → `PREF_KEY_BLEND_WIDTH`.
- Line 74: rename constant `PrefKeyAlphamapRes` → `PREF_KEY_ALPHAMAP_RES`.
- Line 75: rename constant `PrefKeySnowElevation` → `PREF_KEY_SNOW_ELEVATION`.
- Line 76: rename constant `PrefKeySnowHBlend` → `PREF_KEY_SNOW_H_BLEND`.
- Line 77: rename constant `PrefKeySnowSlopeLimit` → `PREF_KEY_SNOW_SLOPE_LIMIT`.
- Line 78: rename constant `PrefKeySnowSlopeBlend` → `PREF_KEY_SNOW_SLOPE_BLEND`.
- Line 79: rename constant `PrefKeyIceElevation` → `PREF_KEY_ICE_ELEVATION`.
- Line 80: rename constant `PrefKeyIceHBlend` → `PREF_KEY_ICE_H_BLEND`.
- Line 81: rename constant `PrefKeyIceSlopeLimit` → `PREF_KEY_ICE_SLOPE_LIMIT`.
- Line 82: rename constant `PrefKeyIceSlopeBlend` → `PREF_KEY_ICE_SLOPE_BLEND`.
- Line 83: rename constant `PrefKeyTileSize` → `PREF_KEY_TILE_SIZE`.
- Line 85: rename static readonly field `AlphamapResOptions` → `ALPHAMAP_RES_OPTIONS`.
- Line 86: rename static readonly field `AlphamapResLabels` → `ALPHAMAP_RES_LABELS`.
- Line 137: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Data/ItemStatTemplate.cs`
- Alphabetize `using` directives (first at line 1).
- Line 14: rename private field `templateName` → `_templateName`; **serialized** — add `[FormerlySerializedAs("templateName")]` before renaming and verify existing assets.
- Line 15: rename private field `stats` → `_stats`; **serialized** — add `[FormerlySerializedAs("stats")]` before renaming and verify existing assets.
- Line 15: use target-typed `new(...)` for member `stats`.
- Line 17: add XML documentation to public API `public string TemplateName => templateName;`.
- Line 18: add XML documentation to public API `public IReadOnlyList<StatEntry> Stats => stats;`.
- Line 21: add XML documentation to public API `public class StatEntry`.
- Line 23: add XML documentation to public API `public ActorStatDefinition stat;`.
- Line 24: add XML documentation to public API `public float value;`.
- Line 34: rename private field `templates` → `_templates`; **serialized** — add `[FormerlySerializedAs("templates")]` before renaming and verify existing assets.
- Line 34: use target-typed `new(...)` for member `templates`.
- Line 36: add XML documentation to public API `public IReadOnlyList<ItemStatTemplate> Templates => templates;`.
- Line 38: rename private field `instance` → `_instance`.
- Line 67: add XML documentation to public API `public static void ResetInstance()`.

#### `Assets/Editor/DialogueEditor/DialogueDefinitionEditor.cs`
- Change namespace `(global)` → `Game.Editor.DialogueEditor`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public class DialogueDefinitionEditor : Editor`.
- Line 8: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/DialogueEditor/DialogueEditorUtils.cs`
- Change namespace `(global)` → `Game.Editor.DialogueEditor`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public static class DialogueEditorUtils`.
- Line 8: add XML documentation to public API `public static string GenerateId()`.
- Line 13: add XML documentation to public API `public static void MarkDirty(DialogueDefinition def)`.
- Rename method `GenerateId` → `GenerateID` for acronym casing.

#### `Assets/Editor/DialogueEditor/DialogueEditorWindow.cs`
- Change namespace `Game.Editor.Dialogue` → `Game.Editor.DialogueEditor`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class DialogueEditorWindow : EditorWindow`.
- Line 12: rename private field `dialogue` → `_dialogue`.
- Line 13: rename private field `serializedDialogue` → `_serializedDialogue`.
- Line 15: rename private field `nodePositions` → `_nodePositions`.
- Line 15: use target-typed `new(...)` for member `nodePositions`.
- Line 17: rename private field `draggingNodeId` → `_draggingNodeID`.
- Line 18: rename private field `dragOffset` → `_dragOffset`.
- Line 19: rename private field `selectedNodeId` → `_selectedNodeID`.
- Line 20: rename private field `isDraggingConnection` → `_isDraggingConnection`.
- Line 21: rename private field `dragFromNodeId` → `_dragFromNodeID`.
- Line 22: rename private field `dragFromChoiceIndex` → `_dragFromChoiceIndex`.
- Line 23: rename private field `dragPosition` → `_dragPosition`.
- Line 25: rename private field `hoveredLinkSourceId` → `_hoveredLinkSourceID`.
- Line 26: rename private field `hoveredLinkChoiceIndex` → `_hoveredLinkChoiceIndex`.
- Line 27: rename private field `hoveredLinkTargetId` → `_hoveredLinkTargetID`.
- Line 28: rename private field `nodeTextStyle` → `_nodeTextStyle`.
- Line 31: add XML documentation to public API `public static void ShowWindow()`.
- Line 37: add XML documentation to public API `public static void OpenWith(DialogueDefinition def)`.
- Line 353: use `var` for local `linkColor`; the RHS makes `Color` explicit.
- Line 354: use `var` for local `dragColor`; the RHS makes `Color` explicit.
- Line 355: use `var` for local `highlightColor`; the RHS makes `Color` explicit.
- Line 372: use `var` for local `start`; the RHS makes `Vector3` explicit.
- Line 382: use `var` for local `endPos`; the RHS makes `Vector3` explicit.
- Line 421: use `var` for local `start`; the RHS makes `Vector3` explicit.
- Line 425: use `var` for local `end`; the RHS makes `Vector3` explicit.
- Line 436: use `var` for local `endPos`; the RHS makes `Vector3` explicit.

#### `Assets/Editor/Drawers/ActorStatBaseEntryDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class ActorStatBaseEntryDrawer : PropertyDrawer`.
- Line 11: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 16: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/AuraBehaviourDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class AuraBehaviourDrawer : PropertyDrawer`.
- Line 11: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 28: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/AuraComponentDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class AuraComponentDrawer : PropertyDrawer`.
- Line 12: rename private field `cachedEffectTypes` → `_cachedEffectTypes`.
- Line 13: rename private field `cachedEffectNames` → `_cachedEffectNames`.
- Line 15: rename private field `cachedTargetTypes` → `_cachedTargetTypes`.
- Line 16: rename private field `cachedTargetNames` → `_cachedTargetNames`.
- Line 18: rename constant `SectionSeparatorHeight` → `SECTION_SEPARATOR_HEIGHT`.
- Line 19: rename constant `SectionBottomPadding` → `SECTION_BOTTOM_PADDING`.
- Line 20: rename constant `ArrayElementSeparatorHeight` → `ARRAY_ELEMENT_SEPARATOR_HEIGHT`.
- Line 23: rename static readonly field `SectionSeparatorColor` → `SECTION_SEPARATOR_COLOR`.
- Line 24: rename static readonly field `SectionHeaderBgColor` → `SECTION_HEADER_BG_COLOR`.
- Line 25: rename static readonly field `ArrayElementSeparatorColor` → `ARRAY_ELEMENT_SEPARATOR_COLOR`.
- Line 49: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 86: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/ClassSpellDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public class ClassSpellDrawer : PropertyDrawer`.
- Line 10: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 15: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/ClassStatGrowthDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public class ClassStatGrowthDrawer : PropertyDrawer`.
- Line 10: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 15: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/ConditionDefinitionDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class ConditionDefinitionDrawer : PropertyDrawer`.
- Line 12: rename private field `cachedConditionTypes` → `_cachedConditionTypes`.
- Line 13: rename private field `cachedConditionNames` → `_cachedConditionNames`.
- Line 32: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 53: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/ModifierEntryDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class ModifierEntryDrawer : PropertyDrawer`.
- Line 12: rename private field `cachedModifierTypes` → `_cachedModifierTypes`.
- Line 13: rename private field `cachedModifierNames` → `_cachedModifierNames`.
- Line 15: rename constant `SectionBottomPadding` → `SECTION_BOTTOM_PADDING`.
- Line 17: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 37: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/QuestObjectiveDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class QuestObjectiveDrawer : PropertyDrawer`.
- Line 12: rename private field `cachedTypes` → `_cachedTypes`.
- Line 13: rename private field `cachedNames` → `_cachedNames`.
- Line 31: rename static readonly field `SeparatorColor` → `SEPARATOR_COLOR`.
- Line 32: rename static readonly field `HeaderBgColor` → `HEADER_BG_COLOR`.
- Line 33: rename constant `HeaderHeight` → `HEADER_HEIGHT`.
- Line 34: rename constant `Padding` → `PADDING`.
- Line 36: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 61: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.
- Rename method `DrawNpcIDField` → `DrawNPCIDField` for acronym casing.
- Rename method `FindNpcPrefabById` → `FindNPCPrefabByID` for acronym casing.
- Rename parameter `npcId` → `npcID`; update named arguments.

#### `Assets/Editor/Drawers/SpellComponentDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 12: add XML documentation to public API `public class SpellComponentDrawer : PropertyDrawer`.
- Line 14: rename private field `cachedEffectTypes` → `_cachedEffectTypes`.
- Line 15: rename private field `cachedEffectNames` → `_cachedEffectNames`.
- Line 17: rename private field `cachedTargetTypes` → `_cachedTargetTypes`.
- Line 18: rename private field `cachedTargetNames` → `_cachedTargetNames`.
- Line 20: rename constant `SectionSeparatorHeight` → `SECTION_SEPARATOR_HEIGHT`.
- Line 21: rename constant `SectionBottomPadding` → `SECTION_BOTTOM_PADDING`.
- Line 22: rename constant `ArrayElementSeparatorHeight` → `ARRAY_ELEMENT_SEPARATOR_HEIGHT`.
- Line 25: rename static readonly field `SectionSeparatorColor` → `SECTION_SEPARATOR_COLOR`.
- Line 26: rename static readonly field `SectionHeaderBgColor` → `SECTION_HEADER_BG_COLOR`.
- Line 27: rename static readonly field `ArrayElementSeparatorColor` → `ARRAY_ELEMENT_SEPARATOR_COLOR`.
- Line 51: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 88: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/Drawers/SpellEffectDefinitionDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class SpellEffectDefinitionDrawer : PropertyDrawer`.
- Line 12: rename private field `cachedTypes` → `_cachedTypes`.
- Line 13: rename private field `cachedNames` → `_cachedNames`.
- Line 31: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 48: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

**Batch result:** 15 files scanned; 15 files contain listed convention changes; 148 individual changes listed.
