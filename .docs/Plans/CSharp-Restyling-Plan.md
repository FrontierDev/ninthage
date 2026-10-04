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


### Audit batch 2: files 16–30 of 386

#### `Assets/Editor/Drawers/StatScalingDrawer.cs`
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public class StatScalingDrawer : PropertyDrawer`.
- Line 10: add XML documentation to public API `public override float GetPropertyHeight(SerializedProperty property, GUIContent label)`.
- Line 15: add XML documentation to public API `public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)`.

#### `Assets/Editor/EDEN_ErosionTools/Editor/EDEN_ErosionTools_Editor.cs`
- Change namespace `GapperGames` → `Game.Editor.EDEN_ErosionTools.Editor`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class EDEN_ErosionTools_Editor : Editor`.
- Line 11: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/EDEN_ErosionTools/Scripts/GPU/EDEN_ErosionTools.cs`
- Change namespace `GapperGames` → `Game.Editor.EDEN_ErosionTools.Scripts.GPU`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class EDEN_ErosionTools : MonoBehaviour`.
- Line 13: rename private field `h_Iterations` → `_h_Iterations`; **serialized** — add `[FormerlySerializedAs("h_Iterations")]` before renaming and verify existing assets.
- Line 14: rename private field `h_Resolution` → `_h_Resolution`; **serialized** — add `[FormerlySerializedAs("h_Resolution")]` before renaming and verify existing assets.
- Line 15: rename private field `h_ErosionSteps` → `_h_ErosionSteps`; **serialized** — add `[FormerlySerializedAs("h_ErosionSteps")]` before renaming and verify existing assets.
- Line 18: rename private field `Windiness` → `_Windiness`; **serialized** — add `[FormerlySerializedAs("Windiness")]` before renaming and verify existing assets.
- Line 19: rename private field `WindDirection` → `_WindDirection`; **serialized** — add `[FormerlySerializedAs("WindDirection")]` before renaming and verify existing assets.
- Line 19: use target-typed `new(...)` for member `WindDirection`.
- Line 20: rename private field `w_Resolution` → `_w_Resolution`; **serialized** — add `[FormerlySerializedAs("w_Resolution")]` before renaming and verify existing assets.
- Line 23: rename private field `TerraceSpacing` → `_TerraceSpacing`; **serialized** — add `[FormerlySerializedAs("TerraceSpacing")]` before renaming and verify existing assets.
- Line 24: rename private field `TerraceAngle` → `_TerraceAngle`; **serialized** — add `[FormerlySerializedAs("TerraceAngle")]` before renaming and verify existing assets.
- Line 27: rename private field `SharpenStrength` → `_SharpenStrength`; **serialized** — add `[FormerlySerializedAs("SharpenStrength")]` before renaming and verify existing assets.
- Line 28: rename private field `sharpenIterations` → `_sharpenIterations`; **serialized** — add `[FormerlySerializedAs("sharpenIterations")]` before renaming and verify existing assets.
- Line 29: rename private field `PeakMixStrength` → `_PeakMixStrength`; **serialized** — add `[FormerlySerializedAs("PeakMixStrength")]` before renaming and verify existing assets.
- Line 32: add XML documentation to public API `public int SmoothingWidth = 2;`.
- Line 35: add XML documentation to public API `public ComputeShader HydraulicErosionComputeShader;`.
- Line 36: add XML documentation to public API `public ComputeShader WindErosionComputeShader;`.
- Line 37: add XML documentation to public API `public ComputeShader SmoothSharpenComputeShader;`.
- Line 38: add XML documentation to public API `public ComputeShader TerraceComputeShader;`.
- Line 40: rename private field `terrain` → `_terrain`.
- Line 41: rename private field `heightMap` → `_heightMap`.
- Line 43: rename private field `rt` → `_rt`.
- Line 44: rename private field `heightmapTex` → `_heightmapTex`.
- Line 45: rename private field `rtTex2d` → `_rtTex2d`.
- Line 47: add XML documentation to public API `public void HydraulicErode()`.
- Line 68: add XML documentation to public API `public void WindErode()`.
- Line 84: add XML documentation to public API `public void Terrace()`.
- Line 116: add XML documentation to public API `public void Smooth(int width)`.
- Line 140: add XML documentation to public API `public void Sharpen()`.
- Line 179: use `var` for local `rectReadPicture`; the RHS makes `Rect` explicit.
- Line 202: use `var` for local `tex`; the RHS makes `Texture2D` explicit.

#### `Assets/Editor/EDEN_ErosionTools/Scripts/GPU/EDEN_Hydraulics.cs`
- Change namespace `GapperGames` → `Game.Editor.EDEN_ErosionTools.Scripts.GPU`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public static class EDEN_Hydraulics`.
- Line 22: add XML documentation to public API `public static float[,] Erode(ComputeShader erosion, int numIterations, float[,] heightmap, int heightmapResolution, int erosionResolution)`.
- Line 42: use `var` for local `mapArray`; the RHS makes `List<float>` explicit.
- Line 61: use `var` for local `brushIndexOffsets`; the RHS makes `List<int>` explicit.
- Line 62: use `var` for local `brushWeights`; the RHS makes `List<float>` explicit.
- Line 85: use `var` for local `brushIndexBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 86: use `var` for local `brushWeightBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 102: use `var` for local `randomIndexBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 107: use `var` for local `mapBuffer`; the RHS makes `ComputeBuffer` explicit.

#### `Assets/Editor/EDEN_ErosionTools/Scripts/GPU/EDEN_Wind.cs`
- Change namespace `GapperGames` → `Game.Editor.EDEN_ErosionTools.Scripts.GPU`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public static class EDEN_Wind`.
- Line 14: add XML documentation to public API `public static float[,] Erode(ComputeShader erosion, int numIterations, float[,] heightmap, int heightmapResolution, int erosionResolution, V`.
- Line 34: use `var` for local `mapArray`; the RHS makes `List<float>` explicit.
- Line 53: use `var` for local `brushIndexOffsets`; the RHS makes `List<int>` explicit.
- Line 54: use `var` for local `brushWeights`; the RHS makes `List<float>` explicit.
- Line 77: use `var` for local `brushIndexBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 78: use `var` for local `brushWeightBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 94: use `var` for local `randomIndexBuffer`; the RHS makes `ComputeBuffer` explicit.
- Line 99: use `var` for local `mapBuffer`; the RHS makes `ComputeBuffer` explicit.

#### `Assets/Editor/Inspectors/ActorSpawnPointInspector.cs`
- Line 8: add XML documentation to public API `public class ActorSpawnPointInspector : Editor`.
- Line 10: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Inspectors/ActorSpellcasterInspector.cs`
- Alphabetize `using` directives (first at line 3).
- Line 10: add XML documentation to public API `public class ActorSpellcasterInspector : Editor`.
- Line 12: rename private field `showSpells` → `_showSpells`.
- Line 13: rename private field `scrollPosition` → `_scrollPosition`.
- Line 15: add XML documentation to public API `public override void OnInspectorGUI()`.
- Line 106: add XML documentation to public API `public override bool RequiresConstantRepaint() => Application.isPlaying;`.

#### `Assets/Editor/Inspectors/ActorStatContainerInspector.cs`
- Alphabetize `using` directives (first at line 3).
- Line 9: add XML documentation to public API `public class ActorStatContainerInspector : Editor`.
- Line 11: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Inspectors/AuraBehaviourEditor.cs`
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public class AuraBehaviourEditor : UnityEditor.Editor`.
- Line 10: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Inspectors/PlayerActorInspector.cs`
- Line 8: add XML documentation to public API `public class PlayerActorInspector : Editor`.
- Line 10: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Inspectors/PlayerEquipmentInspector.cs`
- Alphabetize `using` directives (first at line 3).
- Line 10: add XML documentation to public API `public class PlayerEquipmentInspector : Editor`.
- Line 12: rename private field `showInventory` → `_showInventory`.
- Line 13: rename private field `scrollPosition` → `_scrollPosition`.
- Line 15: add XML documentation to public API `public override void OnInspectorGUI()`.
- Line 95: add XML documentation to public API `public override bool RequiresConstantRepaint() => Application.isPlaying;`.

#### `Assets/Editor/Inspectors/PlayerExperienceInspector.cs`
- Line 9: add XML documentation to public API `public class PlayerExperienceInspector : Editor`.
- Line 11: add XML documentation to public API `public override void OnInspectorGUI()`.

#### `Assets/Editor/Inspectors/PlayerInventoryInspector.cs`
- Line 8: add XML documentation to public API `public class PlayerInventoryInspector : Editor`.
- Line 10: rename private field `showInventory` → `_showInventory`.
- Line 11: rename private field `scrollPosition` → `_scrollPosition`.
- Line 13: add XML documentation to public API `public override void OnInspectorGUI()`.
- Line 92: add XML documentation to public API `public override bool RequiresConstantRepaint() => Application.isPlaying;`.

#### `Assets/Editor/Inspectors/PlayerTalentsInspector.cs.cs`
- Rename file `PlayerTalentsInspector.cs.cs` → `PlayerTalentsInspector.cs`; preserve its `.meta` file.
- Line 9: add XML documentation to public API `public class PlayerTalentsInspector : Editor`.
- Line 11: rename private field `showTalents` → `_showTalents`.
- Line 12: rename private field `scrollPosition` → `_scrollPosition`.
- Line 14: add XML documentation to public API `public override void OnInspectorGUI()`.
- Line 78: add XML documentation to public API `public override bool RequiresConstantRepaint() => Application.isPlaying;`.

#### `Assets/Editor/ToDoGenerator.cs`
- Change namespace `(global)` → `Game.Editor`; update all references atomically and protect serialized managed-reference type moves.
- Line 10: add XML documentation to public API `public static class ToDoGenerator`.
- Line 17: use `var` for local `lines`; the RHS makes `List<string>` explicit.

**Batch result:** 15 files scanned; 15 files contain listed convention changes; 100 individual changes listed.


### Audit batch 3: files 31–46 of 386

#### `Assets/Editor/Windows/ActorStatEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 13: rename private field `selectedTab` → `_selectedTab`.
- Line 14: rename private field `tabNames` → `_tabNames`.
- Line 16: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 17: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 24: add XML documentation to public API `public static void OpenWindow()`.

#### `Assets/Editor/Windows/AuraEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class AuraEditorWindow : DataDefinitionEditorWindow<AuraDefinition, AuraDefinitionLibrary>`.
- Line 11: rename private field `selectedTab` → `_selectedTab`.
- Line 12: rename private field `tabNames` → `_tabNames`.
- Line 14: rename private field `componentList` → `_componentList`.
- Line 15: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 16: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 18: rename private field `pendingTooltip` → `_pendingTooltip`.
- Line 19: rename private field `pendingTooltipText` → `_pendingTooltipText`.
- Line 20: rename private field `pendingTooltipAccentColor` → `_pendingTooltipAccentColor`.
- Line 27: add XML documentation to public API `public static void OpenWindow()`.
- Line 162: use `var` for local `accentColor`; the RHS makes `Color` explicit.
- Line 244: use `var` for local `tooltipRect`; the RHS makes `Rect` explicit.
- Line 261: use `var` for local `accentColor`; the RHS makes `Color` explicit.

#### `Assets/Editor/Windows/ClassEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 14: rename private field `selectedTab` → `_selectedTab`.
- Line 15: rename private field `tabNames` → `_tabNames`.
- Line 17: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 18: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 21: rename private field `pendingExplorerOpen` → `_pendingExplorerOpen`.
- Line 28: add XML documentation to public API `public static void OpenWindow()`.

#### `Assets/Editor/Windows/DamageSchoolEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public class DamageSchoolEditorWindow : DataDefinitionEditorWindow<DamageSchoolDefinition, DamageSchoolDefinitionLibrary>`.
- Line 9: rename private field `selectedTab` → `_selectedTab`.
- Line 10: rename private field `tabNames` → `_tabNames`.
- Line 12: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 13: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 20: add XML documentation to public API `public static void OpenWindow()`.

#### `Assets/Editor/Windows/DataDefinitionEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 18: rename private field `leftScrollPosition` → `_leftScrollPosition`.
- Line 20: rename private field `selected` → `_selected`.
- Line 21: rename private field `library` → `_library`.
- Line 22: rename private field `definitions` → `_definitions`.
- Line 23: rename private field `searchFilter` → `_searchFilter`.
- Line 24: rename private field `useInstanceFallback` → `_useInstanceFallback`.
- Line 28: rename private field `sortMode` → `_sortMode`.
- Line 29: rename private field `tagSortOrder` → `_tagSortOrder`.
- Line 30: rename private field `showSortSettings` → `_showSortSettings`.

#### `Assets/Editor/Windows/DefinitionExplorerWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 16: rename private field `library` → `_library`.
- Line 17: rename private field `onSelectionCallback` → `_onSelectionCallback`.
- Line 18: rename private field `searchQuery` → `_searchQuery`.
- Line 19: rename private field `scrollPosition` → `_scrollPosition`.
- Line 20: rename private field `filteredDefinitions` → `_filteredDefinitions`.
- Line 21: rename private field `defType` → `_defType`.
- Line 24: rename private field `searchMode` → `_searchMode`.
- Line 27: rename private field `pendingSelection` → `_pendingSelection`.
- Line 28: rename private field `shouldClose` → `_shouldClose`.

#### `Assets/Editor/Windows/FactionEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 13: rename private field `selectedTab` → `_selectedTab`.
- Line 14: rename private field `tabNames` → `_tabNames`.
- Line 16: rename private field `alliedList` → `_alliedList`.
- Line 17: rename private field `enemyList` → `_enemyList`.
- Line 19: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 20: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 27: add XML documentation to public API `public static void OpenWindow()`.
- Line 311: use `var` for local `segmentColor`; the RHS makes `Color` explicit.
- Line 326: use `var` for local `overflowColor`; the RHS makes `Color` explicit.

#### `Assets/Editor/Windows/ItemEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 17: rename private field `selectedTab` → `_selectedTab`.
- Line 18: rename private field `tabNames` → `_tabNames`.
- Line 19: rename private field `selectedTemplateIndex` → `_selectedTemplateIndex`.
- Line 20: rename private field `previewLevel` → `_previewLevel`.
- Line 22: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 23: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 24: rename private field `cachedGameConfig` → `_cachedGameConfig`.
- Line 25: rename private field `pendingTooltip` → `_pendingTooltip`.
- Line 26: rename private field `pendingTooltipText` → `_pendingTooltipText`.
- Line 27: rename private field `pendingTooltipAccentColor` → `_pendingTooltipAccentColor`.
- Line 63: add XML documentation to public API `public static void OpenWindow()`.
- Line 284: use `var` for local `tooltipRect`; the RHS makes `Rect` explicit.
- Line 534: rename private field `derivedStatsMap` → `_derivedStatsMap`.
- Line 536: rename private field `DerivedStatInfo` → `_DerivedStatInfo`.
- Line 538: add XML documentation to public API `public string label;`.
- Line 539: add XML documentation to public API `public float ratingPerPercent;`.
- Line 540: add XML documentation to public API `public bool isRating;`.
- Line 541: add XML documentation to public API `public float multiplier;`.
- Line 924: use `var` for local `random`; the RHS makes `System.Random` explicit.
- Rename method `GenerateItemId` → `GenerateItemID` for acronym casing.
- Rename method `ConvertToItemId` → `ConvertToItemID` for acronym casing.

#### `Assets/Editor/Windows/NPCEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 18: rename private field `leftScrollPosition` → `_leftScrollPosition`.
- Line 19: rename private field `npcPrefabs` → `_npcPrefabs`.
- Line 20: rename private field `selected` → `_selected`.
- Line 21: rename private field `searchFilter` → `_searchFilter`.
- Line 22: rename private field `scanFolder` → `_scanFolder`.
- Line 25: rename private field `rightScrollPosition` → `_rightScrollPosition`.
- Line 26: rename private field `selectedTab` → `_selectedTab`.
- Line 27: rename private field `tabNames` → `_tabNames`.
- Line 30: rename private field `cachedPrefab` → `_cachedPrefab`.
- Line 31: rename private field `actorSO` → `_actorSO`.
- Line 32: rename private field `behaviorSO` → `_behaviorSO`.
- Line 33: rename private field `statProfileSO` → `_statProfileSO`.
- Line 34: rename private field `spellcasterSO` → `_spellcasterSO`.
- Line 37: rename private field `lootTableList` → `_lootTableList`.
- Line 38: rename private field `baseStatsList` → `_baseStatsList`.
- Line 39: rename private field `knownSpellsList` → `_knownSpellsList`.
- Line 42: rename private field `cachedSpellDefinitions` → `_cachedSpellDefinitions`.
- Line 45: add XML documentation to public API `public static void OpenWindow()`.
- Rename method `AutoSetNpcID` → `AutoSetNPCID` for acronym casing.

#### `Assets/Editor/Windows/QuestEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 17: rename private field `selectedTab` → `_selectedTab`.
- Line 18: rename private field `tabNames` → `_tabNames`.
- Line 20: rename private field `objectiveList` → `_objectiveList`.
- Line 21: rename private field `requirementList` → `_requirementList`.
- Line 22: rename private field `itemRewardList` → `_itemRewardList`.
- Line 24: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 25: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 27: rename private field `pendingTooltip` → `_pendingTooltip`.
- Line 28: rename private field `pendingTooltipText` → `_pendingTooltipText`.
- Line 29: rename private field `pendingTooltipAccentColor` → `_pendingTooltipAccentColor`.
- Line 31: rename private field `descriptionScrollPosition` → `_descriptionScrollPosition`.
- Line 38: add XML documentation to public API `public static void OpenWindow()`.
- Line 355: use `var` for local `accentColor`; the RHS makes `Color` explicit.
- Line 437: use `var` for local `tooltipRect`; the RHS makes `Rect` explicit.
- Line 454: use `var` for local `accentColor`; the RHS makes `Color` explicit.

#### `Assets/Editor/Windows/RaceEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 12: rename private field `selectedTab` → `_selectedTab`.
- Line 13: rename private field `tabNames` → `_tabNames`.
- Line 15: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 16: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 19: rename private field `pendingExplorerOpen` → `_pendingExplorerOpen`.
- Line 26: add XML documentation to public API `public static void OpenWindow()`.

#### `Assets/Editor/Windows/RatingScalingPreviewWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public class RatingScalingPreviewWindow : EditorWindow`.
- Line 8: rename constant `ReferenceLevel` → `REFERENCE_LEVEL`.
- Line 9: rename constant `ScalingExponent` → `SCALING_EXPONENT`.
- Line 11: rename private field `baseRatingPerPercent` → `_baseRatingPerPercent`.
- Line 12: rename private field `scrollPos` → `_scrollPos`.
- Line 13: rename private field `simLevel` → `_simLevel`.
- Line 14: rename private field `simDesiredPercent` → `_simDesiredPercent`.
- Line 16: add XML documentation to public API `public static void Show(float baseRatingPerPercent)`.

#### `Assets/Editor/Windows/SpellEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 14: rename private field `selectedTab` → `_selectedTab`.
- Line 15: rename private field `tabNames` → `_tabNames`.
- Line 17: rename private field `componentList` → `_componentList`.
- Line 18: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 19: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 21: rename private field `pendingTooltip` → `_pendingTooltip`.
- Line 22: rename private field `pendingTooltipText` → `_pendingTooltipText`.
- Line 23: rename private field `pendingTooltipAccentColor` → `_pendingTooltipAccentColor`.
- Line 30: add XML documentation to public API `public static void OpenWindow()`.
- Line 324: use `var` for local `accentColor`; the RHS makes `Color` explicit.
- Line 406: use `var` for local `tooltipRect`; the RHS makes `Rect` explicit.
- Line 423: use `var` for local `accentColor`; the RHS makes `Color` explicit.

#### `Assets/Editor/Windows/TalentEditorWindow.cs`
- Alphabetize `using` directives (first at line 1).
- Line 17: rename private field `selectedTab` → `_selectedTab`.
- Line 18: rename private field `tabNames` → `_tabNames`.
- Line 20: rename private field `componentList` → `_componentList`.
- Line 21: rename private field `cachedSerializedObject` → `_cachedSerializedObject`.
- Line 22: rename private field `cachedDefinition` → `_cachedDefinition`.
- Line 24: rename private field `modifierList` → `_modifierList`.
- Line 25: rename private field `cachedModifierTypes` → `_cachedModifierTypes`.
- Line 26: rename private field `cachedModifierNames` → `_cachedModifierNames`.
- Line 29: rename private field `pendingTooltip` → `_pendingTooltip`.
- Line 30: rename private field `pendingTooltipText` → `_pendingTooltipText`.
- Line 31: rename private field `pendingTooltipAccentColor` → `_pendingTooltipAccentColor`.
- Line 38: add XML documentation to public API `public static void OpenWindow()`.
- Line 206: use `var` for local `accentColor`; the RHS makes `Color` explicit.
- Line 288: use `var` for local `tooltipRect`; the RHS makes `Rect` explicit.
- Line 305: use `var` for local `accentColor`; the RHS makes `Color` explicit.

#### `Assets/Editor/WorldEditor/Erosion/Hydraulics.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public static class Hydraulics`.
- Line 9: add XML documentation to public API `public static float[,] Erode(ComputeShader erosion, int numIterations, float[,] heightmap, int heightmapResolution, int erosionResolution)`.

#### `Assets/Editor/WorldEditor/Erosion/Interfaces/IErosionMode.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Interfaces`; update all references atomically and protect serialized managed-reference type moves.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 172 individual changes listed.


### Audit batch 4: files 47–62 of 386

#### `Assets/Editor/WorldEditor/Erosion/LocalModes/LocalFineHydraulicErosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.LocalModes`; update all references atomically and protect serialized managed-reference type moves.
- Line 23: add XML documentation to public API `public string ModeName => "Fine-Scale Hydraulic";`.
- Line 24: add XML documentation to public API `public string Description => "Creates detailed water channels and gully patterns with aggressive pixel-level erosion.";`.
- Line 26: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 38: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.
- Line 51: use `var` for local `random`; the RHS makes `System.Random` explicit.

#### `Assets/Editor/WorldEditor/Erosion/LocalModes/LocalSedimentTransport.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.LocalModes`; update all references atomically and protect serialized managed-reference type moves.
- Line 23: add XML documentation to public API `public string ModeName => "Sediment Transport";`.
- Line 24: add XML documentation to public API `public string Description => "Aggressive gravity-driven material transport creating terraces, scarps, and visible accumulation.";`.
- Line 26: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 38: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.
- Line 51: use `var` for local `random`; the RHS makes `System.Random` explicit.

#### `Assets/Editor/WorldEditor/Erosion/LocalModes/LocalThermalWeathering.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.LocalModes`; update all references atomically and protect serialized managed-reference type moves.
- Line 23: add XML documentation to public API `public string ModeName => "Thermal Weathering";`.
- Line 24: add XML documentation to public API `public string Description => "Aggressive slope collapse and rockfall creating dramatic scarps and talus deposits.";`.
- Line 26: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 38: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.
- Line 53: use `var` for local `random`; the RHS makes `System.Random` explicit.

#### `Assets/Editor/WorldEditor/Erosion/LocalTileErosion/LocalErosionWindow.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.LocalTileErosion`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public enum ErosionMode`.
- Line 20: add XML documentation to public API `public class ErosionOperation`.
- Line 22: add XML documentation to public API `public ErosionMode mode = ErosionMode.Hydraulic;`.
- Line 25: add XML documentation to public API `public int h_Iterations = 100000;`.
- Line 26: add XML documentation to public API `public int h_Resolution = 256;`.
- Line 27: add XML documentation to public API `public int h_ErosionSteps = 3;`.
- Line 30: add XML documentation to public API `public float w_Windiness = 0.5f;`.
- Line 31: use target-typed `new(...)` for member `w_WindDirection`.
- Line 31: add XML documentation to public API `public Vector2 w_WindDirection = new Vector2(1, 2);`.
- Line 32: add XML documentation to public API `public int w_Resolution = 257;`.
- Line 35: add XML documentation to public API `public float t_TerraceSpacing = 15;`.
- Line 36: add XML documentation to public API `public float t_TerraceAngle = 20;`.
- Line 39: add XML documentation to public API `public int s_SmoothingWidth = 2;`.
- Line 42: add XML documentation to public API `public float sh_SharpenStrength = 5;`.
- Line 43: add XML documentation to public API `public int sh_SharpenIterations = 10;`.
- Line 44: add XML documentation to public API `public float sh_PeakMixStrength = 0.7f;`.
- Line 48: add XML documentation to public API `public class ErosionProfile`.
- Line 50: add XML documentation to public API `public string name = "New Profile";`.
- Line 51: use target-typed `new(...)` for member `operations`.
- Line 51: add XML documentation to public API `public List<ErosionOperation> operations = new List<ErosionOperation>();`.
- Line 55: add XML documentation to public API `public class ErosionProfilesData`.
- Line 57: use target-typed `new(...)` for member `profiles`.
- Line 57: add XML documentation to public API `public List<ErosionProfile> profiles = new List<ErosionProfile>();`.
- Line 80: use target-typed `new(...)` for member `_w_WindDirection`.
- Line 102: use target-typed `new(...)` for member `_profiles`.
- Line 119: add XML documentation to public API `public static void ShowWindow()`.
- Line 506: use `var` for local `terrainLookup`; the RHS makes `Dictionary<string, Terrain>` explicit.
- Line 779: add XML documentation to public API `public static void ErodeLocalGrid(`.
- Line 797: use `var` for local `originalHeightmaps`; the RHS makes `Dictionary<Terrain, float[,]>` explicit.
- Line 997: use `var` for local `nameToTerrain`; the RHS makes `Dictionary<string, Terrain>` explicit.
- Line 1251: use `var` for local `rt`; the RHS makes `RenderTexture` explicit.
- Line 1259: use `var` for local `tex`; the RHS makes `Texture2D` explicit.
- Line 1280: use `var` for local `rectReadPicture`; the RHS makes `Rect` explicit.
- Line 1283: use `var` for local `rtTex2d`; the RHS makes `Texture2D` explicit.

#### `Assets/Editor/WorldEditor/Erosion/Modes/CurvatureSmoothingErosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Modes`; update all references atomically and protect serialized managed-reference type moves.
- Line 22: add XML documentation to public API `public string ModeName => "Curvature Smoothing";`.
- Line 23: add XML documentation to public API `public string Description => "Laplacian smoothing with slope limiting to preserve cliffs and macro features.";`.
- Line 25: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 40: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.

#### `Assets/Editor/WorldEditor/Erosion/Modes/ErosionUtils.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Modes`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Editor/WorldEditor/Erosion/Modes/HydraulicErosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Modes`; update all references atomically and protect serialized managed-reference type moves.
- Line 28: add XML documentation to public API `public string ModeName => "Hydraulic Erosion";`.
- Line 29: add XML documentation to public API `public string Description => "Water flow simulation that creates realistic valleys and channels.";`.
- Line 31: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 43: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.

#### `Assets/Editor/WorldEditor/Erosion/Modes/MicroSmoothingErosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Modes`; update all references atomically and protect serialized managed-reference type moves.
- Line 23: add XML documentation to public API `public string ModeName => "Micro Smoothing";`.
- Line 24: add XML documentation to public API `public string Description => "Ultra-light noise removal with macro form preservation. Apply last for final polish.";`.
- Line 26: add XML documentation to public API `public ErosionSettings GetDefaultSettings()`.
- Line 41: add XML documentation to public API `public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)`.

#### `Assets/Editor/WorldEditor/Erosion/MultiTile/MultiTileErosionBuffer.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.MultiTile`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 13: add XML documentation to public API `public float[] UnifiedHeightmap { get; private set; }`.
- Line 14: add XML documentation to public API `public int UnifiedWidth { get; private set; }`.
- Line 15: add XML documentation to public API `public int UnifiedHeight { get; private set; }`.
- Line 201: add XML documentation to public API `public void SetUnifiedHeightmap(float[] heightmap)`.
- Line 317: add XML documentation to public API `public void Dispose()`.

#### `Assets/Editor/WorldEditor/Erosion/MultiTile/MultiTileErosionManager.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.MultiTile`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).

#### `Assets/Editor/WorldEditor/Erosion/MultiTile/MultiTileHydraulicErosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.MultiTile`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).

#### `Assets/Editor/WorldEditor/Erosion/Processing/ErosionModeRegistry.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Processing`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Editor/WorldEditor/Erosion/Processing/ErosionProcessor.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Processing`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Editor/WorldEditor/Erosion/Processing/ErosionQueue.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Processing`; update all references atomically and protect serialized managed-reference type moves.
- Line 12: use target-typed `new(...)` for member `_workQueue`.
- Line 14: use target-typed `new(...)` for member `_workLock`.
- Line 15: use target-typed `new(...)` for member `_resultLock`.
- Rename parameter `workItemId` → `workItemID`; update named arguments.
- Rename parameter `workItemId` → `workItemID`; update named arguments.

#### `Assets/Editor/WorldEditor/Erosion/Processing/ErosionSettings.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Processing`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Editor/WorldEditor/Erosion/Processing/ErosionWorkItem.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Erosion.Processing`; update all references atomically and protect serialized managed-reference type moves.
- Line 37: rename member `id` → `ID` for acronym casing.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 92 individual changes listed.


### Audit batch 5: files 63–78 of 386

#### `Assets/Editor/WorldEditor/Generators/TerrainChunkGenerator.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Generators`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 201: use `var` for local `terrainData`; the RHS makes `TerrainData` explicit.
- Line 213: use `var` for local `chunkRoot`; the RHS makes `GameObject` explicit.
- Line 288: use `var` for local `terrainData`; the RHS makes `TerrainData` explicit.
- Line 300: use `var` for local `chunkRoot`; the RHS makes `GameObject` explicit.
- Line 455: use `var` for local `waterGO`; the RHS makes `GameObject` explicit.
- Line 463: use `var` for local `waterMesh`; the RHS makes `Mesh` explicit.

#### `Assets/Editor/WorldEditor/Generators/TerrainChunkPlacer.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Generators`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 16: rename static readonly field `ChunkNameRegex` → `CHUNK_NAME_REGEX`.
- Line 16: use target-typed `new(...)` for member `ChunkNameRegex`.

#### `Assets/Editor/WorldEditor/Processors/HeightmapProcessor.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Processors`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Editor/WorldEditor/Streaming/HeightmapArrayPool.cs`
- Line 13: rename static readonly field `_pools` → `POOLS`.
- Line 85: add XML documentation to public API `public float[] Array => _array;`.
- Line 87: add XML documentation to public API `public PooledArray(int size)`.
- Line 92: add XML documentation to public API `public void Dispose()`.

#### `Assets/Editor/WorldEditor/Streaming/TerrainLODGenerator.cs`
- Alphabetize `using` directives (first at line 1).
- Line 14: add XML documentation to public API `public enum LODDetail`.
- Line 21: rename private field `LODLevel` → `_LODLevel`.
- Line 23: add XML documentation to public API `public readonly float screenRelativeTransitionHeight;`.
- Line 24: add XML documentation to public API `public readonly int heightmapResolution;`.
- Line 25: add XML documentation to public API `public readonly float meshSimplificationFactor;`.
- Line 27: add XML documentation to public API `public LODLevel(float transition, int resolution, float meshFactor)`.
- Line 35: rename static readonly field `_lodLevels` → `LOD_LEVELS`.

#### `Assets/Editor/WorldEditor/Streaming/TiledHeightmapSystem.cs`
- Alphabetize `using` directives (first at line 1).
- Line 16: add XML documentation to public API `public const int TILE_SIZE = 256;`.
- Line 31: add XML documentation to public API `public int TotalTilesX => _tilesX;`.
- Line 32: add XML documentation to public API `public int TotalTilesZ => _tilesZ;`.
- Line 33: add XML documentation to public API `public Vector2Int WorldSize => new(_tilesX * TILE_SIZE, _tilesZ * TILE_SIZE);`.
- Line 35: rename private field `CachedTile` → `_CachedTile`.
- Line 37: add XML documentation to public API `public float[] heightdata;`.
- Line 38: add XML documentation to public API `public long lastAccessTime;`.
- Line 39: add XML documentation to public API `public bool isDirty;`.
- Line 42: add XML documentation to public API `public TiledHeightmapSystem(Texture2D sourceHeightmap, int worldWidth, int worldDepth)`.
- Line 103: use `var` for local `readableTexture`; the RHS makes `Texture2D` explicit.
- Line 302: add XML documentation to public API `public void Dispose()`.

#### `Assets/Editor/WorldEditor/Terrain/TerrainAutoPainterHelper.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Terrain`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 24: rename constant `KernelThreadGroupSize` → `KERNEL_THREAD_GROUP_SIZE`.
- Line 25: rename constant `RequiredLayerCount` → `REQUIRED_LAYER_COUNT`.
- Line 28: rename constant `LayerAssetFolder` → `LAYER_ASSET_FOLDER`.
- Line 29: rename constant `DebugOutputFolder` → `DEBUG_OUTPUT_FOLDER`.
- Line 32: add XML documentation to public API `public const float DefaultSlopeThreshold = 15f;`.
- Line 33: add XML documentation to public API `public const float DefaultTalusThreshold = 25f;`.
- Line 34: add XML documentation to public API `public const float DefaultCliffThreshold = 40f;`.
- Line 35: add XML documentation to public API `public const float DefaultBlendWidth = 5f; // half-width of smoothstep band in degrees`.
- Line 36: add XML documentation to public API `public const int DefaultAlphamapResolution = 1024;`.
- Line 37: add XML documentation to public API `public const float DefaultTextureTileSize = 15f; // world metres per texture tile`.
- Line 39: add XML documentation to public API `public const float DefaultSnowElevation = 800f;`.
- Line 40: add XML documentation to public API `public const float DefaultSnowHeightBlend = 30f;`.
- Line 41: add XML documentation to public API `public const float DefaultSnowSlopeLimit = 35f;`.
- Line 42: add XML documentation to public API `public const float DefaultSnowSlopeBlend = 5f;`.
- Line 43: add XML documentation to public API `public const float DefaultIceElevation = 1200f;`.
- Line 44: add XML documentation to public API `public const float DefaultIceHeightBlend = 30f;`.
- Line 45: add XML documentation to public API `public const float DefaultIceSlopeLimit = 20f;`.
- Line 46: add XML documentation to public API `public const float DefaultIceSlopeBlend = 5f;`.
- Line 47: rename constant `SliceCopyShaderPath` → `SLICE_COPY_SHADER_PATH`.
- Line 50: rename static readonly field `HeightmapId` → `HEIGHTMAP_ID`.
- Line 51: rename static readonly field `ControlMap0Id` → `CONTROL_MAP0_ID`.
- Line 52: rename static readonly field `ControlMap1Id` → `CONTROL_MAP1_ID`.
- Line 53: rename static readonly field `CliffThresholdId` → `CLIFF_THRESHOLD_ID`.
- Line 54: rename static readonly field `TalusThresholdId` → `TALUS_THRESHOLD_ID`.
- Line 55: rename static readonly field `SlopeThresholdId` → `SLOPE_THRESHOLD_ID`.
- Line 56: rename static readonly field `BlendWidthId` → `BLEND_WIDTH_ID`.
- Line 57: rename static readonly field `TalusCurvatureBlendId` → `TALUS_CURVATURE_BLEND_ID`.
- Line 58: rename static readonly field `HeightmapSizeId` → `HEIGHTMAP_SIZE_ID`.
- Line 59: rename static readonly field `AlphamapSizeId` → `ALPHAMAP_SIZE_ID`.
- Line 60: rename static readonly field `TerrainBaseYId` → `TERRAIN_BASE_Y_ID`.
- Line 61: rename static readonly field `TerrainHeightRangeId` → `TERRAIN_HEIGHT_RANGE_ID`.
- Line 62: rename static readonly field `SnowElevationId` → `SNOW_ELEVATION_ID`.
- Line 63: rename static readonly field `SnowHeightBlendId` → `SNOW_HEIGHT_BLEND_ID`.
- Line 64: rename static readonly field `SnowSlopeLimitId` → `SNOW_SLOPE_LIMIT_ID`.
- Line 65: rename static readonly field `SnowSlopeBlendId` → `SNOW_SLOPE_BLEND_ID`.
- Line 66: rename static readonly field `IceElevationId` → `ICE_ELEVATION_ID`.
- Line 67: rename static readonly field `IceHeightBlendId` → `ICE_HEIGHT_BLEND_ID`.
- Line 68: rename static readonly field `IceSlopeLimitId` → `ICE_SLOPE_LIMIT_ID`.
- Line 69: rename static readonly field `IceSlopeBlendId` → `ICE_SLOPE_BLEND_ID`.
- Line 143: use `var` for local `heightmapTex`; the RHS makes `Texture2D` explicit.
- Line 161: use `var` for local `rt0`; the RHS makes `RenderTexture` explicit.
- Line 165: use `var` for local `rt1`; the RHS makes `RenderTexture` explicit.
- Line 304: use `var` for local `layer`; the RHS makes `TerrainLayer` explicit.
- Line 402: use `var` for local `mat`; the RHS makes `Material` explicit.
- Line 412: use `var` for local `rt`; the RHS makes `RenderTexture` explicit.
- Line 423: use `var` for local `slice`; the RHS makes `Texture2D` explicit.
- Line 486: use `var` for local `result`; the RHS makes `Texture2D` explicit.

#### `Assets/Editor/WorldEditor/Terrain/Texture2DArrayBuilder.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.Terrain`; update all references atomically and protect serialized managed-reference type moves.
- Line 19: use target-typed `new(...)` for member `_slices`.
- Line 29: add XML documentation to public API `public static void Open() => GetWindow<Texture2DArrayBuilder>("Texture2D Array Builder");`.
- Line 201: use `var` for local `rt`; the RHS makes `RenderTexture` explicit.
- Line 202: use `var` for local `readback`; the RHS makes `Texture2D` explicit.

#### `Assets/Editor/WorldEditor/UI/WorldEditorSettings.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 13: add XML documentation to public API `public int worldWidth = 256;`.
- Line 14: add XML documentation to public API `public bool flipCoastlineMaskY = false;`.
- Line 15: add XML documentation to public API `public int seaLevel = 10;`.
- Line 16: add XML documentation to public API `public float waterPlaneOffset = -2f;`.
- Line 17: add XML documentation to public API `public float mountainLevel = 0.7f;`.
- Line 18: add XML documentation to public API `public float maxHeight = 100f;`.
- Line 21: add XML documentation to public API `public bool showPlateaus = false;`.
- Line 22: add XML documentation to public API `public int plateauCount = 5;`.
- Line 23: add XML documentation to public API `public bool enablePlateauGeneration = false;`.
- Line 24: add XML documentation to public API `public float plateauSharpness = 2.0f;`.
- Line 27: add XML documentation to public API `public bool showPreview = true;`.
- Line 28: add XML documentation to public API `public float zoomLevel = 1f;`.
- Line 29: add XML documentation to public API `public float panOffsetX = 0f;`.
- Line 30: add XML documentation to public API `public float panOffsetY = 0f;`.
- Line 31: add XML documentation to public API `public int selectedSidebarTab = 0;`.
- Line 32: add XML documentation to public API `public int visualizationMode = 0; // VisualizationMode.Heightmap`.
- Line 35: add XML documentation to public API `public bool enableErosion = false;`.
- Line 36: add XML documentation to public API `public bool[] erosionModeEnabled = new bool[0];`.
- Line 39: add XML documentation to public API `public bool visualizeGeneratedChunks = true;`.
- Line 40: add XML documentation to public API `public bool visualizeMarkedChunks = true;`.
- Line 41: add XML documentation to public API `public bool visualizeUnmarkedChunks = true;`.
- Line 42: add XML documentation to public API `public bool selectiveGenerationMode = false;`.
- Line 51: use target-typed `new(...)` for member `settings`.
- Line 51: add XML documentation to public API `public WorldEditorSettings settings = new WorldEditorSettings();`.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Base.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Base.cs` → `WorldEditorWindow_Base.cs`; move its existing `.meta` file.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public enum VisualizationMode`.
- Line 15: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.
- Line 56: use target-typed `new(...)` for member `_sidebarModules`.
- Line 64: use target-typed `new(...)` for member `_erosionModeSettings`.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Erosion.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Erosion.cs` → `WorldEditorWindow_Erosion.cs`; move its existing `.meta` file.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Input.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Input.cs` → `WorldEditorWindow_Input.cs`; move its existing `.meta` file.
- Line 6: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Modules.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Modules.cs` → `WorldEditorWindow_Modules.cs`; move its existing `.meta` file.
- Line 3: add XML documentation to public API `public interface IWorldEditorWindowModule`.
- Line 9: add XML documentation to public API `public partial class WorldEditorWindow`.
- Line 11: rename private field `IWorldEditorWindowModule` → `_IWorldEditorWindowModule`.
- Line 13: add XML documentation to public API `public string TabName => "World";`.
- Line 15: add XML documentation to public API `public void Draw(WorldEditorWindow window)`.
- Line 23: add XML documentation to public API `public string TabName => "Global Erosion";`.
- Line 25: add XML documentation to public API `public void Draw(WorldEditorWindow window)`.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Overlay.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Overlay.cs` → `WorldEditorWindow_Overlay.cs`; move its existing `.meta` file.
- Line 6: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.
- Line 50: use `var` for local `chunkRect`; the RHS makes `Rect` explicit.

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.Terrain.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.Terrain.cs` → `WorldEditorWindow_Terrain.cs`; move its existing `.meta` file.
- Alphabetize `using` directives (first at line 1).
- Line 12: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.
- Line 331: use `var` for local `result`; the RHS makes `Texture2D` explicit.
- Line 578: use `var` for local `maskedHeightmap`; the RHS makes `Texture2D` explicit.
- Line 633: use `var` for local `readableTexture`; the RHS makes `Texture2D` explicit.
- Line 715: use `var` for local `container`; the RHS makes `GameObject` explicit.

**Batch result:** 16 files scanned; 15 files contain listed convention changes; 152 individual changes listed.


### Audit batch 6: files 79–94 of 386

#### `Assets/Editor/WorldEditor/UI/WorldEditorWindow.UI.cs`
- Change namespace `Game.Editor.WorldEditor` → `Game.Editor.WorldEditor.UI`; update all references atomically and protect serialized managed-reference type moves.
- Rename partial file `WorldEditorWindow.UI.cs` → `WorldEditorWindow_UI.cs`; move its existing `.meta` file.
- Line 6: add XML documentation to public API `public partial class WorldEditorWindow : EditorWindow`.
- Line 602: use `var` for local `progressBarRect`; the RHS makes `Rect` explicit.
- Line 604: use `var` for local `fillRect`; the RHS makes `Rect` explicit.
- Line 715: use `var` for local `gridColor`; the RHS makes `Color` explicit.
- Line 864: use `var` for local `groupRect`; the RHS makes `Rect` explicit.

#### `Assets/Runtime/Client/Core/CameraManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class CameraManager : MonoBehaviour`.
- Line 12: add XML documentation to public API `public static CameraManager Instance => _instance;`.

#### `Assets/Runtime/Client/Core/ClientAccountManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public sealed class ClientAccountManager : MonoBehaviour`.
- Line 25: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 35: add XML documentation to public API `public static CharacterData ActiveCharacter => _activeCharacter;`.
- Line 37: add XML documentation to public API `public static Actor PlayerActor => _playerActor;`.
- Line 105: add XML documentation to public API `public static void UpdateCharacterList(List<CharacterData> newCharacterList)`.
- Line 112: add XML documentation to public API `public static bool HasReceivedCharacters() => _received;`.
- Line 114: add XML documentation to public API `public static bool TryGetCharacterList(out List<CharacterData> characterList)`.
- Line 129: add XML documentation to public API `public static CharacterData GetCharacterData()`.

#### `Assets/Runtime/Client/Core/ClientAudioManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class ClientAudioManager : MonoBehaviour`.
- Line 14: add XML documentation to public API `public static ClientAudioManager Instance => _instance;`.
- Line 17: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 19: rename constant `MaxCacheSize` → `MAX_CACHE_SIZE`.
- Line 21: rename private field `sfxSource` → `_sfxSource`; **serialized** — add `[FormerlySerializedAs("sfxSource")]` before renaming and verify existing assets.
- Line 100: add XML documentation to public API `public void PlaySFX(string addressablePath)`.
- Line 134: add XML documentation to public API `public void PreloadSFX(string addressablePath)`.

#### `Assets/Runtime/Client/Core/ClientCombatManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 15: add XML documentation to public API `public sealed class ClientCombatManager : MonoBehaviour`.
- Line 18: add XML documentation to public API `public static ClientCombatManager Instance => _instance;`.
- Line 20: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 23: rename private field `inCombat` → `_inCombat`.
- Line 24: add XML documentation to public API `public bool InCombat => inCombat;`.
- Line 30: rename private field `activeCooldowns` → `_activeCooldowns`.
- Line 31: rename private field `globalCooldownRemaining` → `_globalCooldownRemaining`.
- Line 138: add XML documentation to public API `public void CastSpell(string spellID, int rank = 1)`.
- Line 186: add XML documentation to public API `public void StartAutoAttack()`.
- Line 194: add XML documentation to public API `public void StopAutoAttack()`.
- Line 239: add XML documentation to public API `public bool IsOnGlobalCooldown()`.
- Line 244: add XML documentation to public API `public bool IsSpellOnCooldown(SpellDefinition spell)`.

#### `Assets/Runtime/Client/Core/ClientConnectionManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 14: add XML documentation to public API `public enum ConnectionState`.
- Line 21: add XML documentation to public API `public class ClientConnectionManager : MonoBehaviour`.
- Line 36: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 39: add XML documentation to public API `public static ConnectionState CurrentConnectionState => _connectionState;`.
- Line 88: add XML documentation to public API `public void ConnectToServer(string usernameInput = "Player", string passwordInput = "password")`.

#### `Assets/Runtime/Client/Core/ClientECSManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public static class ClientECSManager`.
- Line 10: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 12: add XML documentation to public API `public static void ClientInitializeECSWorlds()`.

#### `Assets/Runtime/Client/Core/ClientPositionManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 16: add XML documentation to public API `public static ClientPositionManager Instance => _instance;`.
- Line 18: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 23: rename private field `chunkSize` → `_chunkSize`.
- Line 39: use `var` for local `originOffset`; the RHS makes `Vector3` explicit.
- Line 56: use `var` for local `shift`; the RHS makes `Vector3` explicit.

#### `Assets/Runtime/Client/Core/ClientSettingsManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Line 16: rename private field `settings` → `_settings`.
- Line 16: use target-typed `new(...)` for member `settings`.
- Line 19: add XML documentation to public API `public static Action<string, object> onSettingChanged;`.
- Line 20: add XML documentation to public API `public static Action onSettingsLoaded;`.

#### `Assets/Runtime/Client/Core/ClientVFXManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 12: add XML documentation to public API `public sealed class ClientVFXManager : MonoBehaviour`.
- Line 15: add XML documentation to public API `public static ClientVFXManager Instance => _instance;`.
- Line 17: add XML documentation to public API `public static bool Initialized => _initialized;`.

#### `Assets/Runtime/Client/Core/ClientWorldManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public sealed class ClientWorldManager : MonoBehaviour`.
- Line 25: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 27: rename private field `loadedScenes` → `_loadedScenes`.
- Line 27: use target-typed `new(...)` for member `loadedScenes`.
- Line 38: add XML documentation to public API `public void OnSceneLoaded(SceneID sceneId, bool asServer)`.
- Line 80: add XML documentation to public API `public void GetWorldScenes(List<Scene> buffer)`.
- Rename parameter `sceneId` → `sceneID`; update named arguments.
- Rename parameter `sceneId` → `sceneID`; update named arguments.

#### `Assets/Runtime/Client/Core/Collections/LoadingScreenCollection.cs`
- Change namespace `Game.Client.Collections` → `Game.Client.Core.Collections`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 14: rename private field `splashImages` → `_splashImages`; **serialized** — add `[FormerlySerializedAs("splashImages")]` before renaming and verify existing assets.
- Line 14: use target-typed `new(...)` for member `splashImages`.

#### `Assets/Runtime/Client/Core/Controllers/CharacterCameraController.cs`
- Change namespace `Game.Client` → `Game.Client.Core.Controllers`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class CharacterCameraController : MonoBehaviour`.
- Line 12: add XML documentation to public API `public static CharacterCameraController Instance => _instance;`.
- Line 14: rename private field `cameraInputController` → `_cameraInputController`; **serialized** — add `[FormerlySerializedAs("cameraInputController")]` before renaming and verify existing assets.
- Line 15: rename private field `orbitalFollow` → `_orbitalFollow`; **serialized** — add `[FormerlySerializedAs("orbitalFollow")]` before renaming and verify existing assets.
- Line 18: rename private field `characterRotationSpeed` → `_characterRotationSpeed`; **serialized** — add `[FormerlySerializedAs("characterRotationSpeed")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/Core/Controllers/ClientInputController.cs`
- Change namespace `Game.Client` → `Game.Client.Core.Controllers`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 12: add XML documentation to public API `public enum InputMode`.
- Line 20: add XML documentation to public API `public class ClientInputController : MonoBehaviour`.
- Line 23: add XML documentation to public API `public static ClientInputController Instance => _instance;`.
- Line 26: add XML documentation to public API `public InputMode CurrentMode => _currentMode;`.
- Line 28: add XML documentation to public API `public event System.Action<InputMode> onInputModeChanged;`.
- Line 29: add XML documentation to public API `public event System.Action<Vector2> onMovementInput;`.
- Line 30: add XML documentation to public API `public event System.Action onJumpInput;`.
- Line 31: add XML documentation to public API `public event System.Action onInteractInput;`.
- Line 32: add XML documentation to public API `public event System.Action onCancelInput;`.
- Line 33: add XML documentation to public API `public event System.Action<Game.Shared.IInteractable> onHoverInteractable;`.
- Line 34: add XML documentation to public API `public event System.Action<int, int> onActionBarInput;`.
- Line 35: add XML documentation to public API `public event System.Action<string> onInterfaceHotkeyInput;`.
- Line 90: add XML documentation to public API `public void SetInputMode(InputMode mode)`.

#### `Assets/Runtime/Client/Core/Controllers/Test_CharacterController.cs`
- Change namespace `Game.Client` → `Game.Client.Core.Controllers`; update all references atomically and protect serialized managed-reference type moves.
- Line 9: add XML documentation to public API `public class Test_CharacterController : MonoBehaviour`.
- Line 12: add XML documentation to public API `public static Test_CharacterController Instance => _instance;`.
- Line 14: rename private field `moveSpeed` → `_moveSpeed`; **serialized** — add `[FormerlySerializedAs("moveSpeed")]` before renaming and verify existing assets.
- Line 15: rename private field `jumpPower` → `_jumpPower`; **serialized** — add `[FormerlySerializedAs("jumpPower")]` before renaming and verify existing assets.
- Line 17: rename private field `actorMotor` → `_actorMotor`; **serialized** — add `[FormerlySerializedAs("actorMotor")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/Core/Initialisation.cs`
- Change namespace `Game.Client` → `Game.Client.Core`; update all references atomically and protect serialized managed-reference type moves.
- Rename file `Initialisation.cs` → `ClientInitialization.cs`; preserve its `.meta` file.
- Alphabetize `using` directives (first at line 1).
- Line 15: add XML documentation to public API `public static class ClientInitialization`.
- Line 17: add XML documentation to public API `public static void Begin(MonoBehaviour host)`.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 120 individual changes listed.


### Audit batch 7: files 95–110 of 386

#### `Assets/Runtime/Client/Core/Managers/CharacterCreationManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core.Managers`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class CharacterCreationManager : MonoBehaviour`.
- Line 12: add XML documentation to public API `public static CharacterCreationManager Instance => _instance;`.
- Line 15: rename private field `characterCreationWindow` → `_characterCreationWindow`.
- Line 17: rename private field `sceneCamera` → `_sceneCamera`; **serialized** — add `[FormerlySerializedAs("sceneCamera")]` before renaming and verify existing assets.
- Line 20: rename private field `_selectedClassId` → `_selectedClassID`; **serialized** — add `[FormerlySerializedAs("_selectedClassId")]` before renaming and verify existing assets.
- Line 21: rename private field `_selectedRaceId` → `_selectedRaceID`; **serialized** — add `[FormerlySerializedAs("_selectedRaceId")]` before renaming and verify existing assets.
- Line 44: add XML documentation to public API `public void SelectClass(ClassDefinition def)`.
- Line 51: add XML documentation to public API `public string GetCurrentClassId() { return _selectedClassId; }`.
- Line 53: add XML documentation to public API `public void SelectRace(RaceDefinition def)`.
- Line 60: add XML documentation to public API `public string GetCurrentRaceId() { return _selectedRaceId; }`.
- Line 62: add XML documentation to public API `public void CreateCharacter(string characterName)`.
- Rename method `GetCurrentClassId` → `GetCurrentClassID` for acronym casing.
- Rename method `GetCurrentRaceId` → `GetCurrentRaceID` for acronym casing.

#### `Assets/Runtime/Client/Core/Managers/CharacterSelectionManager.cs`
- Change namespace `Game.Client` → `Game.Client.Core.Managers`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public sealed class CharacterSelectionManager : MonoBehaviour`.
- Line 13: add XML documentation to public API `public static CharacterSelectionManager Instance => _instance;`.
- Line 16: rename private field `characterSelectionWindow` → `_characterSelectionWindow`.
- Line 18: rename private field `sceneCamera` → `_sceneCamera`; **serialized** — add `[FormerlySerializedAs("sceneCamera")]` before renaming and verify existing assets.
- Line 47: add XML documentation to public API `public void SelectCharacter(CharacterData character)`.
- Line 58: add XML documentation to public API `public void EnterWorld()`.

#### `Assets/Runtime/Client/Shaders/UIShaderProperties.cs`
- Change namespace `Game.Client.UI` → `Game.Client.Shaders`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public static class UIShaderProperties`.
- Line 12: add XML documentation to public API `public static void SetTexture(Image image, string propertyName, Texture2D texture)`.
- Line 20: add XML documentation to public API `public static void SetFloat(Image image, string propertyName, float value)`.
- Line 28: add XML documentation to public API `public static void SetColor(Image image, string propertyName, Color color)`.
- Line 40: add XML documentation to public API `public static void SetFloat(MeshRenderer renderer, string propertyName, float value)`.
- Line 48: add XML documentation to public API `public static void SetColor(MeshRenderer renderer, string propertyName, Color color)`.
- Line 56: add XML documentation to public API `public static void SetTexture(MeshRenderer renderer, string propertyName, Texture2D texture)`.

#### `Assets/Runtime/Client/Systems/TestClientSystem.cs`
- Change namespace `Game.Client` → `Game.Client.Systems`; update all references atomically and protect serialized managed-reference type moves.
- Rename test type `TestClientSystem` → `Test_ClientSystem`; update references and filename.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public partial struct TestClientSystem : ISystem`.
- Line 9: add XML documentation to public API `public readonly void OnCreate(ref SystemState state)`.
- Line 14: add XML documentation to public API `public readonly void OnUpdate(ref SystemState state)`.
- Line 19: add XML documentation to public API `public readonly void OnDestroy(ref SystemState state)`.

#### `Assets/Runtime/Client/UI/ActionBars/UI_ActionBarButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ActionBars`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 15: add XML documentation to public API `public sealed class UI_ActionBarButton : UI_Button, IPointerClickHandler`.
- Line 18: rename private field `keybindText` → `_keybindText`; **serialized** — add `[FormerlySerializedAs("keybindText")]` before renaming and verify existing assets.
- Line 19: rename private field `spellIconImage` → `_spellIconImage`; **serialized** — add `[FormerlySerializedAs("spellIconImage")]` before renaming and verify existing assets.
- Line 20: rename private field `spellID` → `_spellID`; **serialized** — add `[FormerlySerializedAs("spellID")]` before renaming and verify existing assets.
- Line 21: rename private field `spellRank` → `_spellRank`; **serialized** — add `[FormerlySerializedAs("spellRank")]` before renaming and verify existing assets.
- Line 22: rename private field `contextMenuEntryPrefab` → `_contextMenuEntryPrefab`; **serialized** — add `[FormerlySerializedAs("contextMenuEntryPrefab")]` before renaming and verify existing assets.
- Line 25: rename private field `key` → `_key`; **serialized** — add `[FormerlySerializedAs("key")]` before renaming and verify existing assets.
- Line 38: add XML documentation to public API `public void SetKeybindText(string text)`.
- Line 83: add XML documentation to public API `public override void OnClick(PointerEventData eventData = default)`.
- Line 103: add XML documentation to public API `public void OnPointerClick(PointerEventData eventData)`.
- Line 108: add XML documentation to public API `public void SetSpell(string newSpellID, int newSpellRank, Sprite newSpellIcon)`.

#### `Assets/Runtime/Client/UI/ActionBars/UI_ActionBarPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ActionBars`; update all references atomically and protect serialized managed-reference type moves.
- Line 7: add XML documentation to public API `public sealed class UI_ActionBarPanel : UI_Panel`.
- Line 9: rename private field `actionBarIndex` → `_actionBarIndex`; **serialized** — add `[FormerlySerializedAs("actionBarIndex")]` before renaming and verify existing assets.
- Line 17: add XML documentation to public API `public void Initialize(int index)`.
- Line 37: add XML documentation to public API `public void ActivateSlot(int slotIndex)`.

#### `Assets/Runtime/Client/UI/ActionBars/UI_ActionBarWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ActionBars`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_ActionBarWindow : UI_Window`.
- Line 6: add XML documentation to public API `public static UI_ActionBarWindow Instance => _instance;`.
- Line 8: add XML documentation to public API `public static bool Initialized => _initialized;`.

#### `Assets/Runtime/Client/UI/CastBars/UI_CastBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CastBars`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public sealed class UI_CastBar : UI_ProgressBar`.
- Line 8: rename private field `parentWindowName` → `_parentWindowName`.
- Line 11: rename private field `defaultMaterial` → `_defaultMaterial`; **serialized** — add `[FormerlySerializedAs("defaultMaterial")]` before renaming and verify existing assets.
- Line 21: add XML documentation to public API `public void StartCast(float duration, bool reverse = false)`.
- Line 29: add XML documentation to public API `public void InterruptCast()`.
- Line 36: add XML documentation to public API `public void FinishCast()`.

#### `Assets/Runtime/Client/UI/CastBars/UI_ClientCastBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CastBars`; update all references atomically and protect serialized managed-reference type moves.
- Rename file `UI_ClientCastBar.cs` → `UI_ClientCastBarWindow.cs`; preserve its `.meta` file.
- Line 9: add XML documentation to public API `public sealed class UI_ClientCastBarWindow : UI_Window`.
- Line 12: add XML documentation to public API `public static UI_ClientCastBarWindow Instance => _instance;`.
- Line 14: rename private field `castBar` → `_castBar`; **serialized** — add `[FormerlySerializedAs("castBar")]` before renaming and verify existing assets.
- Line 16: rename private field `castColor` → `_castColor`; **serialized** — add `[FormerlySerializedAs("castColor")]` before renaming and verify existing assets.
- Line 17: rename private field `interruptColor` → `_interruptColor`; **serialized** — add `[FormerlySerializedAs("interruptColor")]` before renaming and verify existing assets.
- Line 18: rename private field `completeColor` → `_completeColor`; **serialized** — add `[FormerlySerializedAs("completeColor")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_ClassInfoPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class UI_CC_ClassInfoPanel : UI_Panel`.
- Line 12: rename private field `classNameText` → `_classNameText`; **serialized** — add `[FormerlySerializedAs("classNameText")]` before renaming and verify existing assets.
- Line 13: rename private field `classDescriptionText` → `_classDescriptionText`; **serialized** — add `[FormerlySerializedAs("classDescriptionText")]` before renaming and verify existing assets.
- Line 14: rename private field `classIconImage` → `_classIconImage`; **serialized** — add `[FormerlySerializedAs("classIconImage")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_ClassListEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class UI_CC_ClassListEntry : UI_ListEntry<ClassDefinition>`.
- Line 13: rename private field `classIconImage` → `_classIconImage`; **serialized** — add `[FormerlySerializedAs("classIconImage")]` before renaming and verify existing assets.
- Line 15: add XML documentation to public API `public ClassDefinition classDefinition;`.
- Line 16: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public override void Initialize(ClassDefinition _classDefinition, int _index)`.
- Line 27: add XML documentation to public API `public void OnClassSelected(ClassDefinition def)`.
- Line 33: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.
- Line 41: add XML documentation to public API `public override void OnPointerEnter(PointerEventData eventData)`.
- Line 46: add XML documentation to public API `public override void OnPointerExit(PointerEventData eventData)`.
- Line 51: add XML documentation to public API `public override void Focus()`.
- Line 57: add XML documentation to public API `public override void Unfocus()`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_ClassListPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class UI_CC_ClassListPanel : UI_Panel, IListPanel`.
- Line 12: rename private field `contentContainer` → `_contentContainer`; **serialized** — add `[FormerlySerializedAs("contentContainer")]` before renaming and verify existing assets.
- Line 13: rename private field `entryPrefab` → `_entryPrefab`; **serialized** — add `[FormerlySerializedAs("entryPrefab")]` before renaming and verify existing assets.
- Line 17: rename private field `classDefinitionLibrary` → `_classDefinitionLibrary`.
- Line 19: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 35: add XML documentation to public API `public override void Refresh()`.
- Line 40: add XML documentation to public API `public override void Show()`.
- Line 55: add XML documentation to public API `public override void Hide()`.
- Line 62: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_CreateButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Line 5: add XML documentation to public API `public sealed class UI_CC_CreateButton : UI_Button`.
- Line 7: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_FinalizePanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public sealed class UI_CC_FinalizePanel : UI_Panel`.
- Line 9: add XML documentation to public API `public static UI_CC_FinalizePanel Instance => _instance;`.
- Line 24: rename private field `createButton` → `_createButton`; **serialized** — add `[FormerlySerializedAs("createButton")]` before renaming and verify existing assets.
- Line 25: rename private field `nameInput` → `_nameInput`; **serialized** — add `[FormerlySerializedAs("nameInput")]` before renaming and verify existing assets.
- Line 27: add XML documentation to public API `public override void Show()`.
- Line 35: add XML documentation to public API `public string GetCharacterName()`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_NextStageButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_CC_NextStageButton : UI_Button`.
- Line 5: add XML documentation to public API `public static UI_CC_NextStageButton _instance;`.
- Line 6: add XML documentation to public API `public static UI_CC_NextStageButton Instance => _instance;`.
- Line 8: rename private field `currentPanel` → `_currentPanel`.
- Line 9: rename private field `nextPanel` → `_nextPanel`.
- Line 23: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 37: add XML documentation to public API `public void SetCurrentPanel(string panelName)`.
- Line 42: add XML documentation to public API `public void SetNextPanel(string panelName)`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_RaceInfoPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class UI_CC_RaceInfoPanel : UI_Panel`.
- Line 12: rename private field `raceNameText` → `_raceNameText`; **serialized** — add `[FormerlySerializedAs("raceNameText")]` before renaming and verify existing assets.
- Line 13: rename private field `raceDescriptionText` → `_raceDescriptionText`; **serialized** — add `[FormerlySerializedAs("raceDescriptionText")]` before renaming and verify existing assets.
- Line 14: rename private field `raceIconImage` → `_raceIconImage`; **serialized** — add `[FormerlySerializedAs("raceIconImage")]` before renaming and verify existing assets.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 132 individual changes listed.


### Audit batch 8: files 111–126 of 386

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_RaceListEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class UI_CC_RaceListEntry : UI_ListEntry<RaceDefinition>`.
- Line 13: rename private field `raceIconImage` → `_raceIconImage`; **serialized** — add `[FormerlySerializedAs("raceIconImage")]` before renaming and verify existing assets.
- Line 15: add XML documentation to public API `public RaceDefinition raceDefinition;`.
- Line 16: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public override void Initialize(RaceDefinition _raceDefinition, int _index)`.
- Line 27: add XML documentation to public API `public void OnRaceSelected(RaceDefinition def)`.
- Line 34: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.
- Line 41: add XML documentation to public API `public override void OnPointerEnter(PointerEventData eventData)`.
- Line 46: add XML documentation to public API `public override void OnPointerExit(PointerEventData eventData)`.
- Line 51: add XML documentation to public API `public override void Focus()`.
- Line 57: add XML documentation to public API `public override void Unfocus()`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_RaceListPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class UI_CC_RaceListPanel : UI_Panel, IListPanel`.
- Line 12: rename private field `contentContainer` → `_contentContainer`; **serialized** — add `[FormerlySerializedAs("contentContainer")]` before renaming and verify existing assets.
- Line 13: rename private field `entryPrefab` → `_entryPrefab`; **serialized** — add `[FormerlySerializedAs("entryPrefab")]` before renaming and verify existing assets.
- Line 17: rename private field `raceDefinitionLibrary` → `_raceDefinitionLibrary`.
- Line 19: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 35: add XML documentation to public API `public override void Show()`.
- Line 50: add XML documentation to public API `public override void Hide()`.
- Line 57: add XML documentation to public API `public override void Refresh()`.
- Line 62: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CC_StageButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Rename file `UI_CC_StageButton.cs` → `StageButton.cs`; preserve its `.meta` file.
- Line 5: add XML documentation to public API `public class StageButton : UI_Button`.
- Line 7: rename private field `openPanels` → `_openPanels`; **serialized** — add `[FormerlySerializedAs("openPanels")]` before renaming and verify existing assets.
- Line 8: rename private field `closePanels` → `_closePanels`; **serialized** — add `[FormerlySerializedAs("closePanels")]` before renaming and verify existing assets.
- Line 10: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterCreation/UI_CharacterCreationWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterCreation`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class UI_CharacterCreationWindow : UI_Window`.
- Line 12: add XML documentation to public API `public static UI_CharacterCreationWindow Instance => _instance;`.
- Line 14: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 17: add XML documentation to public API `public Action<ClassDefinition> onClassSelected;`.
- Line 18: add XML documentation to public API `public Action<RaceDefinition> onRaceSelected;`.
- Line 31: add XML documentation to public API `public override void Show()`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_CharacterListEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 12: add XML documentation to public API `public sealed class UI_CharacterListEntry : UI_ListEntry<CharacterData>`.
- Line 14: rename private field `classIconImage` → `_classIconImage`; **serialized** — add `[FormerlySerializedAs("classIconImage")]` before renaming and verify existing assets.
- Line 15: rename private field `characterNameText` → `_characterNameText`; **serialized** — add `[FormerlySerializedAs("characterNameText")]` before renaming and verify existing assets.
- Line 16: rename private field `characterInfoText` → `_characterInfoText`; **serialized** — add `[FormerlySerializedAs("characterInfoText")]` before renaming and verify existing assets.
- Line 17: rename private field `factionIcon` → `_factionIcon`; **serialized** — add `[FormerlySerializedAs("factionIcon")]` before renaming and verify existing assets.
- Line 19: add XML documentation to public API `public CharacterData characterData;`.
- Line 20: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 22: add XML documentation to public API `public override void Initialize(CharacterData _characterData, int _index)`.
- Line 45: add XML documentation to public API `public void OnCharacterSelected(CharacterData character)`.
- Line 51: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.
- Line 59: add XML documentation to public API `public override void OnPointerEnter(PointerEventData eventData)`.
- Line 64: add XML documentation to public API `public override void OnPointerExit(PointerEventData eventData)`.
- Line 69: add XML documentation to public API `public override void Focus()`.
- Line 75: add XML documentation to public API `public override void Unfocus()`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_CharacterListPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public sealed class UI_CharacterListPanel : UI_Panel, IListPanel`.
- Line 10: add XML documentation to public API `public static UI_CharacterListPanel Instance => _instance;`.
- Line 13: rename private field `contentContainer` → `_contentContainer`; **serialized** — add `[FormerlySerializedAs("contentContainer")]` before renaming and verify existing assets.
- Line 14: rename private field `entryPrefab` → `_entryPrefab`; **serialized** — add `[FormerlySerializedAs("entryPrefab")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 20: add XML documentation to public API `public override void Refresh()`.
- Line 25: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_CharacterSelectWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public sealed class UI_CharacterSelectWindow : UI_Window`.
- Line 10: add XML documentation to public API `public static UI_CharacterSelectWindow Instance => _instance;`.
- Line 12: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 14: add XML documentation to public API `public Action<CharacterData> onCharacterSelected;`.
- Line 27: add XML documentation to public API `public void PopulateCharacterList(System.Collections.Generic.List<CharacterData> characters)`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_CreateCharacterButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public class UI_CreateCharacterButton : UI_Button`.
- Line 8: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_EnterWorldButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Line 5: add XML documentation to public API `public class UI_EnterWorldButton : UI_Button`.
- Line 7: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterSelection/UI_SelectedCharacterPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterSelection`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public sealed class UI_SelectedCharacterPanel : UI_Panel`.
- Line 11: add XML documentation to public API `public static UI_SelectedCharacterPanel Instance => _instance;`.
- Line 14: rename private field `enterWorldButton` → `_enterWorldButton`; **serialized** — add `[FormerlySerializedAs("enterWorldButton")]` before renaming and verify existing assets.
- Line 15: rename private field `characterNameText` → `_characterNameText`; **serialized** — add `[FormerlySerializedAs("characterNameText")]` before renaming and verify existing assets.
- Line 16: rename private field `characterInfoText` → `_characterInfoText`; **serialized** — add `[FormerlySerializedAs("characterInfoText")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_CharacterResourceBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class UI_CharacterResourceBar : UI_ProgressBar`.
- Line 12: rename private field `playerStatContainer` → `_playerStatContainer`; **serialized** — add `[FormerlySerializedAs("playerStatContainer")]` before renaming and verify existing assets.
- Line 13: rename private field `valueText` → `_valueText`; **serialized** — add `[FormerlySerializedAs("valueText")]` before renaming and verify existing assets.
- Line 14: rename private field `percentageText` → `_percentageText`; **serialized** — add `[FormerlySerializedAs("percentageText")]` before renaming and verify existing assets.
- Line 15: rename private field `trackedResources` → `_trackedResources`; **serialized** — add `[FormerlySerializedAs("trackedResources")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_CharacterSpecialBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class UI_CharacterSpecialBar : UI_Panel, IListPanel`.
- Line 14: add XML documentation to public API `public static UI_CharacterSpecialBar Instance => _instance;`.
- Line 16: rename private field `playerStatContainer` → `_playerStatContainer`.
- Line 17: rename private field `trackedStat` → `_trackedStat`.
- Line 19: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 20: rename private field `counterPrefab` → `_counterPrefab`; **serialized** — add `[FormerlySerializedAs("counterPrefab")]` before renaming and verify existing assets.
- Line 21: rename private field `counters` → `_counters`; **serialized** — add `[FormerlySerializedAs("counters")]` before renaming and verify existing assets.
- Line 24: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 26: rename private field `currentMax` → `_currentMax`.
- Line 27: rename private field `currentValue` → `_currentValue`.
- Line 79: add XML documentation to public API `public override void Refresh()`.
- Line 103: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_CharacterUnitWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_CharacterUnitWindow : UI_Window`.
- Line 6: add XML documentation to public API `public static UI_CharacterUnitWindow Instance => _instance;`.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_InstabilityBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 2).
- Line 11: add XML documentation to public API `public class UI_InstabilityBar : UI_ProgressBar, ISpecialResourceBar`.
- Line 13: rename private field `playerStatContainer` → `_playerStatContainer`; **serialized** — add `[FormerlySerializedAs("playerStatContainer")]` before renaming and verify existing assets.
- Line 15: add XML documentation to public API `public void Initialize(Actor playerActor)`.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_SpecialResourceCounter.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public sealed class UI_SpecialResourceCounter : UI_ListEntry<Sprite>`.
- Line 8: rename private field `background` → `_background`; **serialized** — add `[FormerlySerializedAs("background")]` before renaming and verify existing assets.
- Line 9: rename private field `icon` → `_icon`; **serialized** — add `[FormerlySerializedAs("icon")]` before renaming and verify existing assets.
- Line 11: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 13: add XML documentation to public API `public override void Initialize(Sprite sprite, int slotId)`.
- Line 20: add XML documentation to public API `public void SetFilled(bool filled)`.
- Line 26: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 30: add XML documentation to public API `public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)`.
- Rename parameter `slotId` → `slotID`; update named arguments.

#### `Assets/Runtime/Client/UI/CharacterUnitFrame/UI_SpecialResourcePanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterUnitFrame`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public interface ISpecialResourceBar`.
- Line 13: add XML documentation to public API `public sealed class UI_SpecialResourcePanel : UI_Panel`.
- Line 16: add XML documentation to public API `public static UI_SpecialResourcePanel Instance => _instance;`.
- Line 24: add XML documentation to public API `public void OnPlayerActorAssigned(Actor playerActor)`.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 128 individual changes listed.


### Audit batch 9: files 127–142 of 386

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterDataPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public sealed class UI_CharacterDataPanel : UI_Panel`.
- Line 13: add XML documentation to public API `public static UI_CharacterDataPanel Instance => _instance;`.
- Line 15: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 17: rename private field `characterNameText` → `_characterNameText`; **serialized** — add `[FormerlySerializedAs("characterNameText")]` before renaming and verify existing assets.
- Line 18: rename private field `characterTitleText` → `_characterTitleText`; **serialized** — add `[FormerlySerializedAs("characterTitleText")]` before renaming and verify existing assets.
- Line 19: rename private field `characterLevelText` → `_characterLevelText`; **serialized** — add `[FormerlySerializedAs("characterLevelText")]` before renaming and verify existing assets.
- Line 20: rename private field `characterClassText` → `_characterClassText`; **serialized** — add `[FormerlySerializedAs("characterClassText")]` before renaming and verify existing assets.
- Line 21: rename private field `characterRaceText` → `_characterRaceText`; **serialized** — add `[FormerlySerializedAs("characterRaceText")]` before renaming and verify existing assets.
- Line 22: rename private field `characterClassIcon` → `_characterClassIcon`; **serialized** — add `[FormerlySerializedAs("characterClassIcon")]` before renaming and verify existing assets.
- Line 23: rename private field `characterRaceIcon` → `_characterRaceIcon`; **serialized** — add `[FormerlySerializedAs("characterRaceIcon")]` before renaming and verify existing assets.
- Line 24: rename private field `characterClassBackground` → `_characterClassBackground`; **serialized** — add `[FormerlySerializedAs("characterClassBackground")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterEquipmentEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 11: add XML documentation to public API `public sealed class UI_CharacterEquipmentEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler`.
- Line 13: add XML documentation to public API `public ItemInstance item;`.
- Line 14: add XML documentation to public API `public ItemSlot slotId;`.
- Line 14: rename member `slotId` → `slotID` for acronym casing.
- Line 15: rename private field `graphics` → `_graphics`; **serialized** — add `[FormerlySerializedAs("graphics")]` before renaming and verify existing assets.
- Line 26: add XML documentation to public API `public override void Initialize(ItemInstance _item, int _slotId)`.
- Line 32: add XML documentation to public API `public void SetItem(ItemInstance _item)`.
- Line 54: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 68: add XML documentation to public API `public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 86: add XML documentation to public API `public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 93: add XML documentation to public API `public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Rename parameter `_slotId` → `_slotID`; update named arguments.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterEquipmentList.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public sealed class UI_CharacterEquipmentList : UI_Panel, IListPanel`.
- Line 12: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 13: rename private field `slotEntryPrefab` → `_slotEntryPrefab`; **serialized** — add `[FormerlySerializedAs("slotEntryPrefab")]` before renaming and verify existing assets.
- Line 14: rename private field `equipmentSlots` → `_equipmentSlots`; **serialized** — add `[FormerlySerializedAs("equipmentSlots")]` before renaming and verify existing assets.
- Line 17: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 38: add XML documentation to public API `public override void Refresh()`.
- Line 44: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterEquipmentPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public sealed class UI_CharacterEquipmentPanel : UI_Panel`.
- Line 10: add XML documentation to public API `public static UI_CharacterEquipmentPanel Instance => _instance;`.
- Line 13: rename private field `subPanels` → `_subPanels`; **serialized** — add `[FormerlySerializedAs("subPanels")]` before renaming and verify existing assets.
- Line 28: add XML documentation to public API `public override void OnShow()`.
- Line 38: add XML documentation to public API `public override void OnHide()`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterFocusStatsPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class UI_CharacterFocusStatsPanel : UI_Panel, IListPanel`.
- Line 12: add XML documentation to public API `public static UI_CharacterFocusStatsPanel Instance => _instance;`.
- Line 14: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 15: rename private field `statEntryPrefab` → `_statEntryPrefab`; **serialized** — add `[FormerlySerializedAs("statEntryPrefab")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 20: rename private field `focusStatDefinitions` → `_focusStatDefinitions`.
- Line 20: use target-typed `new(...)` for member `focusStatDefinitions`.
- Line 21: rename private field `currentStats` → `_currentStats`.
- Line 63: add XML documentation to public API `public override void Refresh()`.
- Line 87: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterSkillsPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public sealed class UI_CharacterSkillsPanel : UI_Panel`.
- Line 9: add XML documentation to public API `public static UI_CharacterSkillsPanel Instance => _instance;`.
- Line 11: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 13: rename private field `subPanels` → `_subPanels`.
- Line 13: use target-typed `new(...)` for member `subPanels`.
- Line 27: add XML documentation to public API `public override void OnShow()`.
- Line 36: add XML documentation to public API `public override void OnHide()`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterStatEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class UI_CharacterStatEntry : UI_ListEntry<ActorStatDefinition>`.
- Line 13: rename private field `statNameText` → `_statNameText`; **serialized** — add `[FormerlySerializedAs("statNameText")]` before renaming and verify existing assets.
- Line 14: rename private field `currentValueText` → `_currentValueText`; **serialized** — add `[FormerlySerializedAs("currentValueText")]` before renaming and verify existing assets.
- Line 15: rename private field `statIcon` → `_statIcon`; **serialized** — add `[FormerlySerializedAs("statIcon")]` before renaming and verify existing assets.
- Line 17: add XML documentation to public API `public ActorStatDefinition statDefinition;`.
- Line 18: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 20: add XML documentation to public API `public void SetStat(StatSnapshotEntry statEntry)`.
- Line 26: add XML documentation to public API `public void Refresh(ActorStatContainer statContainer)`.
- Line 42: add XML documentation to public API `public override void Initialize(ActorStatDefinition _statDefinition, int _index)`.
- Line 65: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 70: add XML documentation to public API `public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 86: add XML documentation to public API `public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 92: add XML documentation to public API `public void SetStatValue(float current)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterStatList.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public sealed class UI_CharacterStatList : UI_Panel, IListPanel`.
- Line 11: add XML documentation to public API `public static UI_CharacterStatList Instance => _instance;`.
- Line 13: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 14: rename private field `statEntryPrefab` → `_statEntryPrefab`; **serialized** — add `[FormerlySerializedAs("statEntryPrefab")]` before renaming and verify existing assets.
- Line 17: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 19: rename private field `statDefinitions` → `_statDefinitions`; **serialized** — add `[FormerlySerializedAs("statDefinitions")]` before renaming and verify existing assets.
- Line 19: use target-typed `new(...)` for member `statDefinitions`.
- Line 20: rename private field `focusStatDefinitions` → `_focusStatDefinitions`; **serialized** — add `[FormerlySerializedAs("focusStatDefinitions")]` before renaming and verify existing assets.
- Line 20: use target-typed `new(...)` for member `focusStatDefinitions`.
- Line 21: rename private field `relevantStatDefinitions` → `_relevantStatDefinitions`; **serialized** — add `[FormerlySerializedAs("relevantStatDefinitions")]` before renaming and verify existing assets.
- Line 21: use target-typed `new(...)` for member `relevantStatDefinitions`.
- Line 22: rename private field `currentStats` → `_currentStats`; **serialized** — add `[FormerlySerializedAs("currentStats")]` before renaming and verify existing assets.
- Line 57: add XML documentation to public API `public override void Refresh()`.
- Line 90: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterStatTypeButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 9: add XML documentation to public API `public sealed class UI_CharacterStatTypeButton : UI_Button`.
- Line 11: rename private field `statTypeTag` → `_statTypeTag`; **serialized** — add `[FormerlySerializedAs("statTypeTag")]` before renaming and verify existing assets.
- Line 13: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterStatsPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 2).
- Line 10: add XML documentation to public API `public sealed class UI_CharacterStatsPanel : UI_Panel`.
- Line 13: add XML documentation to public API `public static UI_CharacterStatsPanel Instance => _instance;`.
- Line 15: rename private field `subPanels` → `_subPanels`.
- Line 15: use target-typed `new(...)` for member `subPanels`.
- Line 29: add XML documentation to public API `public override void OnShow()`.
- Line 38: add XML documentation to public API `public override void OnHide()`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterTabButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 8: add XML documentation to public API `public sealed class UI_CharacterTabButton : UI_Button`.
- Line 10: rename private field `associatedPanel` → `_associatedPanel`; **serialized** — add `[FormerlySerializedAs("associatedPanel")]` before renaming and verify existing assets.
- Line 11: rename private field `tabText` → `_tabText`; **serialized** — add `[FormerlySerializedAs("tabText")]` before renaming and verify existing assets.
- Line 12: rename private field `tabGraphics` → `_tabGraphics`; **serialized** — add `[FormerlySerializedAs("tabGraphics")]` before renaming and verify existing assets.
- Line 13: add XML documentation to public API `public Image TabGraphics => tabGraphics;`.
- Line 14: add XML documentation to public API `public UI_Panel AssociatedPanel => associatedPanel;`.
- Line 15: add XML documentation to public API `public TMP_Text TabText => tabText;`.
- Line 17: rename private field `characterTabPanel` → `_characterTabPanel`.
- Line 25: add XML documentation to public API `public void Register(UI_CharacterTabPanel panel)`.
- Line 30: add XML documentation to public API `public override void OnClick(PointerEventData eventData = null)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterTabPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public sealed class UI_CharacterTabPanel : UI_Panel`.
- Line 10: add XML documentation to public API `public static UI_CharacterTabPanel Instance => _instance;`.
- Line 12: rename private field `tabButtons` → `_tabButtons`; **serialized** — add `[FormerlySerializedAs("tabButtons")]` before renaming and verify existing assets.
- Line 12: use target-typed `new(...)` for member `tabButtons`.
- Line 13: rename private field `tabButtonList` → `_tabButtonList`; **serialized** — add `[FormerlySerializedAs("tabButtonList")]` before renaming and verify existing assets.
- Line 35: add XML documentation to public API `public void OnTabSelected(UI_CharacterTabButton selectedButton)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterWeaponSkillBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_CharacterWeaponSkillBar : UI_ProgressBar`.
- Line 5: rename private field `weaponSkillEntry` → `_weaponSkillEntry`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterWeaponSkillEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 8: add XML documentation to public API `public sealed class UI_CharacterWeaponSkillEntry : UI_ListEntry<ItemWeaponType>`.
- Line 10: rename private field `weaponTypeText` → `_weaponTypeText`; **serialized** — add `[FormerlySerializedAs("weaponTypeText")]` before renaming and verify existing assets.
- Line 11: rename private field `levelText` → `_levelText`; **serialized** — add `[FormerlySerializedAs("levelText")]` before renaming and verify existing assets.
- Line 12: rename private field `weaponSkillBar` → `_weaponSkillBar`; **serialized** — add `[FormerlySerializedAs("weaponSkillBar")]` before renaming and verify existing assets.
- Line 14: rename private field `weaponType` → `_weaponType`; **serialized** — add `[FormerlySerializedAs("weaponType")]` before renaming and verify existing assets.
- Line 15: add XML documentation to public API `public ItemWeaponType WeaponType => weaponType;`.
- Line 17: rename private field `index` → `_index`.
- Line 19: add XML documentation to public API `public override void Initialize(ItemWeaponType _weaponType, int _index)`.
- Line 27: add XML documentation to public API `public void UpdateWeaponSkill(int level, int experience)`.
- Line 35: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterWeaponSkillsList.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class UI_CharacterWeaponSkillsList : UI_Panel, IListPanel`.
- Line 12: add XML documentation to public API `public static UI_CharacterWeaponSkillsList Instance => _instance;`.
- Line 14: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 15: rename private field `skillEntryPrefab` → `_skillEntryPrefab`; **serialized** — add `[FormerlySerializedAs("skillEntryPrefab")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 20: rename private field `trackedWeaponTypes` → `_trackedWeaponTypes`; **serialized** — add `[FormerlySerializedAs("trackedWeaponTypes")]` before renaming and verify existing assets.
- Line 21: rename private field `playerExperience` → `_playerExperience`; **serialized** — add `[FormerlySerializedAs("playerExperience")]` before renaming and verify existing assets.
- Line 60: add XML documentation to public API `public override void Refresh()`.
- Line 85: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/CharacterWindow/UI_CharacterWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.CharacterWindow`; update all references atomically and protect serialized managed-reference type moves.
- Line 5: add XML documentation to public API `public sealed class UI_CharacterWindow : UI_Window`.
- Line 8: add XML documentation to public API `public static UI_CharacterWindow Instance => _instance;`.
- Line 10: add XML documentation to public API `public static bool IsInitialized => _initialized;`.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 154 individual changes listed.


### Audit batch 10: files 143–158 of 386

#### `Assets/Runtime/Client/UI/Common/IDraggableSlot.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public interface IDraggableSlot`.

#### `Assets/Runtime/Client/UI/Common/IListPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public interface IListPanel`.

#### `Assets/Runtime/Client/UI/Common/UI_AuraBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public abstract class UI_AuraBar : UI_Panel, IListPanel`.
- Line 12: add XML documentation to public API `[SerializeField] public GameObject container;`.
- Line 13: add XML documentation to public API `[SerializeField] public UI_AuraEntry auraEntryPrefab;`.
- Line 16: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 19: add XML documentation to public API `public virtual void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/Common/UI_AuraEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 11: add XML documentation to public API `public sealed class UI_AuraEntry : UI_ListEntry<AuraInstance>`.
- Line 13: rename private field `data` → `_data`; **serialized** — add `[FormerlySerializedAs("data")]` before renaming and verify existing assets.
- Line 14: add XML documentation to public API `public AuraInstance Data => data;`.
- Line 16: rename private field `graphics` → `_graphics`; **serialized** — add `[FormerlySerializedAs("graphics")]` before renaming and verify existing assets.
- Line 17: rename private field `durationText` → `_durationText`; **serialized** — add `[FormerlySerializedAs("durationText")]` before renaming and verify existing assets.
- Line 19: rename private field `index` → `_index`; **serialized** — add `[FormerlySerializedAs("index")]` before renaming and verify existing assets.
- Line 21: add XML documentation to public API `public override void Initialize(AuraInstance data, int index)`.
- Line 46: add XML documentation to public API `public void UpdateDisplay(float currentDuration = -1f)`.
- Line 66: add XML documentation to public API `public override void OnClick(PointerEventData eventData = null)`.

#### `Assets/Runtime/Client/UI/Common/UI_Button.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public abstract class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler`.
- Line 14: add XML documentation to public API `public virtual void Start()`.
- Line 20: add XML documentation to public API `public virtual void OnClick(PointerEventData eventData = default)`.
- Line 29: add XML documentation to public API `public virtual void OnPointerEnter(PointerEventData eventData)`.
- Line 34: add XML documentation to public API `public virtual void OnPointerExit(PointerEventData eventData)`.
- Line 39: add XML documentation to public API `public virtual void EnableButton()`.
- Line 44: add XML documentation to public API `public virtual void DisableButton()`.
- Line 49: add XML documentation to public API `public virtual bool IsVisible()`.
- Line 54: add XML documentation to public API `public virtual void Show()`.
- Line 61: add XML documentation to public API `public virtual void Hide()`.

#### `Assets/Runtime/Client/UI/Common/UI_DragGhost.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public sealed class UI_DragGhost : MonoBehaviour`.
- Line 20: rename private field `iconImage` → `_iconImage`.
- Line 21: rename private field `rectTransform` → `_rectTransform`.
- Line 22: rename private field `rootCanvas` → `_rootCanvas`.
- Line 51: add XML documentation to public API `public void Show(Sprite icon)`.
- Line 58: add XML documentation to public API `public void UpdatePosition(Vector2 screenPosition)`.
- Line 63: add XML documentation to public API `public void Hide()`.

#### `Assets/Runtime/Client/UI/Common/UI_GenericPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.

#### `Assets/Runtime/Client/UI/Common/UI_ListEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public abstract class UI_ListEntry : UI_Button`.
- Line 8: add XML documentation to public API `public IListPanel parentPanel;`.
- Line 9: add XML documentation to public API `public bool isFocused;`.
- Line 11: add XML documentation to public API `public virtual void Register(IListPanel _parentPanel)`.
- Line 17: add XML documentation to public API `public virtual void Destroy()`.
- Line 23: add XML documentation to public API `public virtual void Focus() { }`.
- Line 24: add XML documentation to public API `public virtual void Unfocus() { }`.
- Line 25: add XML documentation to public API `public virtual void Highlight() { }`.
- Line 26: add XML documentation to public API `public virtual void ClearHighlight() { }`.
- Line 29: add XML documentation to public API `public abstract class UI_ListEntry<T> : UI_ListEntry`.
- Line 31: add XML documentation to public API `public abstract void Initialize(T data, int index);`.

#### `Assets/Runtime/Client/UI/Common/UI_Manager.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 10: add XML documentation to public API `public class UI_Manager : MonoBehaviour`.
- Line 12: rename private field `instance` → `_instance`.
- Line 13: add XML documentation to public API `public static UI_Manager Instance => instance;`.
- Line 21: use target-typed `new(...)` for member `openedWindows`.
- Line 48: add XML documentation to public API `public void Initialize()`.
- Line 54: add XML documentation to public API `public bool TryGetWindow(string windowName, out UI_Window window)`.
- Line 60: add XML documentation to public API `public virtual void ShowWindow(string windowName)`.
- Line 66: add XML documentation to public API `public virtual void HideWindow(string windowName)`.
- Line 72: add XML documentation to public API `public virtual void ToggleWindow(string windowName)`.
- Line 78: add XML documentation to public API `public virtual void OnShowWindow(UI_Window window) => openedWindows.Enqueue(window);`.
- Line 79: add XML documentation to public API `public virtual void OnHideWindow(UI_Window window) => openedWindows = new Queue<UI_Window>(openedWindows.Where(w => w != window));`.
- Line 80: add XML documentation to public API `public virtual void CloseLastWindow()`.

#### `Assets/Runtime/Client/UI/Common/UI_Panel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 18: add XML documentation to public API `public string PanelName => panelName; // Public getter for panel name.`.
- Line 27: rename private field `onShowWindow` → `_onShowWindow`; **serialized** — add `[FormerlySerializedAs("onShowWindow")]` before renaming and verify existing assets.
- Line 28: rename private field `onHideWindow` → `_onHideWindow`.
- Line 41: add XML documentation to public API `public virtual void Show()`.
- Line 47: add XML documentation to public API `public virtual void ShowImmediate()`.
- Line 53: add XML documentation to public API `public virtual void Hide()`.
- Line 59: add XML documentation to public API `public virtual void HideImmediate()`.
- Line 65: add XML documentation to public API `public virtual void Toggle()`.
- Line 88: add XML documentation to public API `public virtual void OnShow()`.
- Line 94: add XML documentation to public API `public virtual void OnHide()`.
- Line 100: add XML documentation to public API `public virtual void Refresh()`.

#### `Assets/Runtime/Client/UI/Common/UI_ProgressBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 7: add XML documentation to public API `public abstract class UI_ProgressBar : MonoBehaviour`.
- Line 9: rename private field `canvasGroup` → `_canvasGroup`; **serialized** — add `[FormerlySerializedAs("canvasGroup")]` before renaming and verify existing assets.
- Line 12: add XML documentation to public API `public virtual void SetMaterial(Material material)`.
- Line 18: add XML documentation to public API `public void Show()`.
- Line 23: add XML documentation to public API `public void Hide()`.
- Line 28: add XML documentation to public API `public void SetProgress(float progress)`.
- Line 35: add XML documentation to public API `public void SetColor(Color color)`.

#### `Assets/Runtime/Client/UI/Common/UI_Window.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public abstract class UI_Window : MonoBehaviour`.
- Line 12: add XML documentation to public API `public string WindowName => windowName; // Public getter for window name.`.
- Line 45: add XML documentation to public API `public virtual void Show()`.
- Line 51: add XML documentation to public API `public virtual void ShowImmediate()`.
- Line 57: add XML documentation to public API `public virtual void Hide()`.
- Line 63: add XML documentation to public API `public virtual void HideImmediate()`.
- Line 69: add XML documentation to public API `public virtual void Toggle()`.
- Line 110: add XML documentation to public API `public virtual bool TryGetPanel(string panelName, out UI_Panel panel)`.
- Line 121: add XML documentation to public API `public virtual void ShowPanel(string panelName)`.
- Line 126: add XML documentation to public API `public virtual void HidePanel(string panelName)`.
- Line 131: add XML documentation to public API `public virtual void TogglePanel(string panelName)`.

#### `Assets/Runtime/Client/UI/Common/UI_WorldSpace.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 5: add XML documentation to public API `public class UI_WorldSpace : MonoBehaviour`.
- Line 7: rename private field `instance` → `_instance`.
- Line 8: add XML documentation to public API `public static UI_WorldSpace Instance => instance;`.
- Line 11: add XML documentation to public API `[SerializeField] public Canvas root;`.
- Line 13: add XML documentation to public API `[SerializeField] public Transform nameplateContainer;`.
- Line 14: add XML documentation to public API `[SerializeField] public Transform fctContainer;`.

#### `Assets/Runtime/Client/UI/Common/UI_WorldSpaceUIManager.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Common`; update all references atomically and protect serialized managed-reference type moves.
- Line 5: add XML documentation to public API `public abstract class UI_WorldSpaceUIManager : MonoBehaviour`.
- Line 8: rename private field `cornerBuffer` → `_cornerBuffer`.

#### `Assets/Runtime/Client/UI/ContextMenu/UI_ContextMenuEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ContextMenu`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public sealed class UI_ContextMenuEntry : UI_ListEntry`.
- Line 11: rename private field `iconImage` → `_iconImage`; **serialized** — add `[FormerlySerializedAs("iconImage")]` before renaming and verify existing assets.
- Line 12: rename private field `nameText` → `_nameText`; **serialized** — add `[FormerlySerializedAs("nameText")]` before renaming and verify existing assets.
- Line 14: rename private field `onClick` → `_onClick`; **serialized** — add `[FormerlySerializedAs("onClick")]` before renaming and verify existing assets.
- Line 16: add XML documentation to public API `public void Initialize(string displayName, Sprite icon, Action onClickCallback)`.
- Line 24: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.

#### `Assets/Runtime/Client/UI/ContextMenu/UI_ContextMenuPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ContextMenu`; update all references atomically and protect serialized managed-reference type moves.
- Line 7: add XML documentation to public API `public sealed class UI_ContextMenuPanel : UI_Panel, IListPanel`.
- Line 15: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 16: add XML documentation to public API `public Transform ContentRoot => _contentContainer.transform;`.
- Line 18: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.
- Line 27: add XML documentation to public API `public void Populate(GameObject entryPrefab, Action<UI_ContextMenuPanel> populateAction)`.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 127 individual changes listed.


### Audit batch 11: files 159–174 of 386

#### `Assets/Runtime/Client/UI/ContextMenu/UI_ContextMenuWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ContextMenu`; update all references atomically and protect serialized managed-reference type moves.
- Line 8: add XML documentation to public API `public enum ContextMenuPivot`.
- Line 16: add XML documentation to public API `public sealed class UI_ContextMenuWindow : UI_Window, IPointerClickHandler`.
- Line 19: add XML documentation to public API `public static UI_ContextMenuWindow Instance => _instance;`.
- Line 21: add XML documentation to public API `public static bool Initialized => _initialized;`.
- Line 23: rename private field `contentsPanel` → `_contentsPanel`; **serialized** — add `[FormerlySerializedAs("contentsPanel")]` before renaming and verify existing assets.
- Line 24: rename private field `backdropBlocker` → `_backdropBlocker`; **serialized** — add `[FormerlySerializedAs("backdropBlocker")]` before renaming and verify existing assets.
- Line 26: rename private field `panelRect` → `_panelRect`; **serialized** — add `[FormerlySerializedAs("panelRect")]` before renaming and verify existing assets.
- Line 40: add XML documentation to public API `public void OnPointerClick(PointerEventData eventData)`.
- Line 46: add XML documentation to public API `public void Show(Vector2 position, ContextMenuPivot pivot, GameObject entryPrefab, Action<UI_ContextMenuPanel> populate)`.
- Line 61: add XML documentation to public API `public override void Hide()`.

#### `Assets/Runtime/Client/UI/Dialogue/UI_DialogueBodyPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Dialogue`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public sealed class UI_DialogueBodyPanel : UI_Panel`.
- Line 11: add XML documentation to public API `public static UI_DialogueBodyPanel Instance => _instance;`.
- Line 13: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 15: rename private field `dialogueText` → `_dialogueText`; **serialized** — add `[FormerlySerializedAs("dialogueText")]` before renaming and verify existing assets.
- Line 16: rename private field `optionList` → `_optionList`; **serialized** — add `[FormerlySerializedAs("optionList")]` before renaming and verify existing assets.
- Line 18: add XML documentation to public API `public int CurrentNodeID { get; private set; }`.
- Line 27: add XML documentation to public API `public void ShowDialogue(DialogueDefinition dialogue, int nodeID = 0)`.
- Line 48: add XML documentation to public API `public void GoToNode(int nodeID)`.

#### `Assets/Runtime/Client/UI/Dialogue/UI_DialogueNPCInfoPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Dialogue`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public sealed class UI_DialogueNPCInfoPanel : UI_Panel`.
- Line 11: add XML documentation to public API `public static UI_DialogueNPCInfoPanel Instance => _instance;`.
- Line 13: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 15: rename private field `npcNameText` → `_npcNameText`; **serialized** — add `[FormerlySerializedAs("npcNameText")]` before renaming and verify existing assets.
- Line 16: rename private field `npcSubtext` → `_npcSubtext`; **serialized** — add `[FormerlySerializedAs("npcSubtext")]` before renaming and verify existing assets.
- Line 17: rename private field `npcPortrait` → `_npcPortrait`; **serialized** — add `[FormerlySerializedAs("npcPortrait")]` before renaming and verify existing assets.
- Line 26: add XML documentation to public API `public void UpdateNPC(Actor npc)`.

#### `Assets/Runtime/Client/UI/Dialogue/UI_DialogueOptionEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Dialogue`; update all references atomically and protect serialized managed-reference type moves.
- Line 9: add XML documentation to public API `public sealed class UI_DialogueOptionEntry : UI_ListEntry<DialogueChoice>`.
- Line 11: add XML documentation to public API `public DialogueChoice data;`.
- Line 12: add XML documentation to public API `public int index;`.
- Line 14: rename private field `choiceText` → `_choiceText`; **serialized** — add `[FormerlySerializedAs("choiceText")]` before renaming and verify existing assets.
- Line 15: rename private field `choiceIcon` → `_choiceIcon`; **serialized** — add `[FormerlySerializedAs("choiceIcon")]` before renaming and verify existing assets.
- Line 17: add XML documentation to public API `public override void Initialize(DialogueChoice data, int index)`.
- Line 30: add XML documentation to public API `public override void OnClick(PointerEventData eventData = null)`.

#### `Assets/Runtime/Client/UI/Dialogue/UI_DialogueOptionList.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Dialogue`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 7: add XML documentation to public API `public sealed class UI_DialogueOptionList : UI_Panel, IListPanel`.
- Line 10: add XML documentation to public API `public static UI_DialogueOptionList Instance => _instance;`.
- Line 12: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 14: rename private field `currentNode` → `_currentNode`.
- Line 16: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 17: rename private field `slotEntryPrefab` → `_slotEntryPrefab`; **serialized** — add `[FormerlySerializedAs("slotEntryPrefab")]` before renaming and verify existing assets.
- Line 20: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 29: add XML documentation to public API `public void SetNode(int nodeID)`.
- Line 34: add XML documentation to public API `public override void Refresh()`.
- Line 49: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/Dialogue/UI_DialogueWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Dialogue`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public class UI_DialogueWindow : UI_Window`.
- Line 9: add XML documentation to public API `public static UI_DialogueWindow Instance => _instance;`.
- Line 11: add XML documentation to public API `public static bool IsInitialized => _initialized;`.
- Line 13: add XML documentation to public API `public DialogueDefinition CurrentDialogue { get; private set; }`.
- Line 21: add XML documentation to public API `public void StartDialogue(Actor npc)`.

#### `Assets/Runtime/Client/UI/ExperienceBar/UI_ExperienceBar.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ExperienceBar`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 8: add XML documentation to public API `public sealed class UI_ExperienceBar : UI_ProgressBar`.

#### `Assets/Runtime/Client/UI/ExperienceBar/UI_ExperienceBarWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.ExperienceBar`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_ExperienceBarWindow : UI_Window`.
- Line 6: add XML documentation to public API `public static UI_ExperienceBarWindow Instance => _instance;`.

#### `Assets/Runtime/Client/UI/FloatingCombatText/UI_FCTManager.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.FloatingCombatText`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class UI_FCTManager : MonoBehaviour`.
- Line 11: rename private field `instance` → `_instance`.
- Line 12: add XML documentation to public API `public static UI_FCTManager Instance => instance;`.
- Line 15: rename private field `fctPrefab` → `_fctPrefab`; **serialized** — add `[FormerlySerializedAs("fctPrefab")]` before renaming and verify existing assets.
- Line 18: rename private field `autoHitColor` → `_autoHitColor`; **serialized** — add `[FormerlySerializedAs("autoHitColor")]` before renaming and verify existing assets.
- Line 19: rename private field `abilityHitColor` → `_abilityHitColor`; **serialized** — add `[FormerlySerializedAs("abilityHitColor")]` before renaming and verify existing assets.
- Line 20: rename private field `petHitColor` → `_petHitColor`; **serialized** — add `[FormerlySerializedAs("petHitColor")]` before renaming and verify existing assets.
- Line 21: rename private field `healColor` → `_healColor`; **serialized** — add `[FormerlySerializedAs("healColor")]` before renaming and verify existing assets.
- Line 22: rename private field `buffColor` → `_buffColor`; **serialized** — add `[FormerlySerializedAs("buffColor")]` before renaming and verify existing assets.
- Line 23: rename private field `debuffColor` → `_debuffColor`; **serialized** — add `[FormerlySerializedAs("debuffColor")]` before renaming and verify existing assets.
- Line 24: rename private field `experienceColor` → `_experienceColor`; **serialized** — add `[FormerlySerializedAs("experienceColor")]` before renaming and verify existing assets.
- Line 27: rename private field `heightOffset` → `_heightOffset`; **serialized** — add `[FormerlySerializedAs("heightOffset")]` before renaming and verify existing assets.
- Line 28: rename private field `randomSpreadX` → `_randomSpreadX`; **serialized** — add `[FormerlySerializedAs("randomSpreadX")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/FloatingCombatText/UI_FloatingCombatText.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.FloatingCombatText`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 6: add XML documentation to public API `public sealed class UI_FloatingCombatText : MonoBehaviour`.
- Line 8: rename private field `text` → `_text`; **serialized** — add `[FormerlySerializedAs("text")]` before renaming and verify existing assets.
- Line 9: rename private field `floatSpeed` → `_floatSpeed`; **serialized** — add `[FormerlySerializedAs("floatSpeed")]` before renaming and verify existing assets.
- Line 10: rename private field `fadeDuration` → `_fadeDuration`; **serialized** — add `[FormerlySerializedAs("fadeDuration")]` before renaming and verify existing assets.
- Line 11: rename private field `screenSize` → `_screenSize`; **serialized** — add `[FormerlySerializedAs("screenSize")]` before renaming and verify existing assets.
- Line 13: rename private field `floatDirection` → `_floatDirection`; **serialized** — add `[FormerlySerializedAs("floatDirection")]` before renaming and verify existing assets.
- Line 14: rename private field `elapsedTime` → `_elapsedTime`; **serialized** — add `[FormerlySerializedAs("elapsedTime")]` before renaming and verify existing assets.
- Line 15: rename private field `sizeMultiplier` → `_sizeMultiplier`.
- Line 16: rename private field `durationMultiplier` → `_durationMultiplier`.
- Line 18: add XML documentation to public API `public void Initialize(string content, Color color, Vector3 direction, float sizeMultiplier = 1f, float durationMultiplier = 1f)`.

#### `Assets/Runtime/Client/UI/Inventory/UI_InventoryFooterPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Inventory`; update all references atomically and protect serialized managed-reference type moves.
- Line 6: add XML documentation to public API `public sealed class UI_InventoryFooterPanel : UI_Panel`.
- Line 8: rename private field `capacityText` → `_capacityText`; **serialized** — add `[FormerlySerializedAs("capacityText")]` before renaming and verify existing assets.
- Line 9: rename private field `currencyText` → `_currencyText`; **serialized** — add `[FormerlySerializedAs("currencyText")]` before renaming and verify existing assets.

#### `Assets/Runtime/Client/UI/Inventory/UI_InventoryGridPanel.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Inventory`; update all references atomically and protect serialized managed-reference type moves.
- Line 7: add XML documentation to public API `public sealed class UI_InventoryGridPanel : UI_Panel, IListPanel`.
- Line 10: add XML documentation to public API `public static UI_InventoryGridPanel Instance => _instance;`.
- Line 12: rename private field `container` → `_container`; **serialized** — add `[FormerlySerializedAs("container")]` before renaming and verify existing assets.
- Line 13: rename private field `slotEntryPrefab` → `_slotEntryPrefab`; **serialized** — add `[FormerlySerializedAs("slotEntryPrefab")]` before renaming and verify existing assets.
- Line 16: add XML documentation to public API `public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();`.
- Line 33: add XML documentation to public API `public override void Refresh()`.
- Line 48: add XML documentation to public API `public void PopulateList(bool forceClear = false)`.

#### `Assets/Runtime/Client/UI/Inventory/UI_InventorySlotEntry.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Inventory`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 14: add XML documentation to public API `public class UI_InventorySlotEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler,`.
- Line 17: rename private field `dragSource` → `_dragSource`.
- Line 19: rename private field `index` → `_index`.
- Line 20: rename private field `itemInstance` → `_itemInstance`.
- Line 22: rename private field `graphics` → `_graphics`; **serialized** — add `[FormerlySerializedAs("graphics")]` before renaming and verify existing assets.
- Line 23: rename private field `stackSizeText` → `_stackSizeText`; **serialized** — add `[FormerlySerializedAs("stackSizeText")]` before renaming and verify existing assets.
- Line 25: add XML documentation to public API `public int SlotIndex => index;`.
- Line 26: add XML documentation to public API `public ItemInstance SlotItem => itemInstance;`.
- Line 35: add XML documentation to public API `public override void Initialize(ItemInstance data, int index)`.
- Line 59: add XML documentation to public API `public void UpdateEntry(ItemInstance newData)`.
- Line 85: add XML documentation to public API `public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 109: add XML documentation to public API `public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 128: add XML documentation to public API `public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)`.
- Line 135: add XML documentation to public API `public void OnPointerClick(PointerEventData eventData)`.
- Line 142: add XML documentation to public API `public void OnBeginDrag(PointerEventData eventData)`.
- Line 150: add XML documentation to public API `public void OnDrag(PointerEventData eventData)`.
- Line 157: add XML documentation to public API `public void OnEndDrag(PointerEventData eventData)`.
- Line 165: add XML documentation to public API `public void OnDrop(PointerEventData eventData)`.
- Line 172: add XML documentation to public API `public void OnSlotBeginDrag(Vector2 position)`.
- Line 181: add XML documentation to public API `public void OnSlotDrag(Vector2 position)`.
- Line 186: add XML documentation to public API `public void OnSlotEndDrag()`.
- Line 191: add XML documentation to public API `public void OnSlotDrop(IDraggableSlot source)`.

#### `Assets/Runtime/Client/UI/Inventory/UI_InventoryWindow.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.Inventory`; update all references atomically and protect serialized managed-reference type moves.
- Line 3: add XML documentation to public API `public sealed class UI_InventoryWindow : UI_Window`.
- Line 6: add XML documentation to public API `public static UI_InventoryWindow Instance => _instance;`.
- Line 8: add XML documentation to public API `public static bool IsInitialized => _initialized;`.

#### `Assets/Runtime/Client/UI/LoadingScreen/LoadingScreenController.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.LoadingScreen`; update all references atomically and protect serialized managed-reference type moves.
- Alphabetize `using` directives (first at line 1).
- Line 9: add XML documentation to public API `public class LoadingScreenController : MonoBehaviour`.
- Line 12: add XML documentation to public API `public static LoadingScreenController Instance => _instance;`.
- Line 16: add XML documentation to public API `public LoadingScreenCollection Collection => _collection;`.
- Line 19: rename private field `canvasGroup` → `_canvasGroup`; **serialized** — add `[FormerlySerializedAs("canvasGroup")]` before renaming and verify existing assets.
- Line 20: rename private field `splashImage` → `_splashImage`; **serialized** — add `[FormerlySerializedAs("splashImage")]` before renaming and verify existing assets.
- Line 21: rename private field `loadingIcon` → `_loadingIcon`; **serialized** — add `[FormerlySerializedAs("loadingIcon")]` before renaming and verify existing assets.
- Line 23: rename private field `stage` → `_stage`; **serialized** — add `[FormerlySerializedAs("stage")]` before renaming and verify existing assets.
- Line 24: rename private field `maxStages` → `_maxStages`; **serialized** — add `[FormerlySerializedAs("maxStages")]` before renaming and verify existing assets.
- Line 25: rename private field `targetFillAmount` → `_targetFillAmount`.
- Line 27: rename private field `isShowing` → `_isShowing`.
- Line 64: add XML documentation to public API `public void Show(int stages = 1)`.
- Line 87: add XML documentation to public API `public void Tick()`.
- Line 102: add XML documentation to public API `public void Finish()`.
- Line 109: add XML documentation to public API `public void Cancel()`.

#### `Assets/Runtime/Client/UI/MainMenu/UI_LoginButton.cs`
- Change namespace `Game.Client.UI` → `Game.Client.UI.MainMenu`; update all references atomically and protect serialized managed-reference type moves.
- Line 7: add XML documentation to public API `public sealed class UI_LoginButton : UI_Button`.
- Line 9: add XML documentation to public API `public override void OnClick(PointerEventData eventData)`.
- Line 24: add XML documentation to public API `public override void OnPointerEnter(PointerEventData eventData) => base.OnPointerEnter(eventData);`.
- Line 26: add XML documentation to public API `public override void OnPointerExit(PointerEventData eventData) => base.OnPointerExit(eventData);`.

**Batch result:** 16 files scanned; 16 files contain listed convention changes; 150 individual changes listed.
