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
