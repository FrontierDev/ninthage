# Procedural Texture Studio — Product Design Document

**Status:** Proposed design specification  
**Project:** Ninth Age (initial development and validation host)  
**Product:** Procedural Texture Studio (reusable Unity Editor package)  
**Target engine:** Unity 6.3 (`6000.3`); other Unity versions are not presumed supported until tested  
**Primary render pipeline:** Universal Render Pipeline (URP), Forward+  
**Repository:** `FrontierDev/ninthage`  
**Proposed repository location:** `.docs/Plans/Procedural-Texture-Studio-PDD.md`  
**Last updated:** 2026-10-12

---

## 1. Purpose and Authority

Procedural Texture Studio is a **parameter-driven, non-node-based Unity Editor tool** for producing attractive, reusable, seamless surface textures and physically based rendering (PBR) material assets without leaving Unity.

The primary objective is **good-looking, production-usable environment materials with a short authoring loop**. Reproducing the full feature set of Adobe Substance products is explicitly not the objective.

The plugin will be developed and used inside the existing Ninth Age Unity project, but its **code, package assets, settings, generators, and tests must remain transferable to unrelated Unity projects**. Ninth Age is the development and artistic validation environment, not a runtime or compile-time dependency.

This PDD defines the desired product and acceptance criteria. It is not a claim that any of the functionality is already implemented. Detailed implementation tasks should be placed in separate plans/issues.

### 1.1 Relationship to Ninth Age's design standards

When materials are intended for Ninth Age, their output must comply with:

- `.docs/Graphical-Approach-PDD.md` — authoritative visual direction;
- `.docs/CSharp-Style-Conventions.md` — normative C# conventions for work in the Ninth Age repository;
- `.docs/CSharp-Architecture.md` — descriptive baseline for the existing project, not a source of dependencies for this package.

Ninth Age's visual target is **grounded, mid-poly fantasy with physically believable but restrained materials**. Materials should have recognisable forms, broad colour/roughness structure, readable medium-scale detail and controlled normal detail. They should not resemble unprocessed photo-scans or equally noisy detail at every spatial frequency.

The plugin itself is a general-purpose authoring tool; its presets can be tuned to Ninth Age's art direction without hard-coding the game's artistic decisions into core generation algorithms.

## 2. Locked Design Decisions

1. The product is a **Unity Editor plugin**, not a separate Windows application.
2. Development takes place **inside Ninth Age**, as an **embedded Unity Package Manager (UPM) package** under `Packages/`.
3. The package must also be installable in another Unity project without copying Ninth Age assets or assemblies.
4. The authoring interface is **preset- and parameter-driven**. No node graph, visual shader graph, or general-purpose node editor is required.
5. Generation is **procedural and deterministic**. The same recipe, seed, generator version, and supported execution environment must produce equivalent maps within documented numerical tolerances.
6. **Seamless 2D tiling is a core guarantee**, not an optional post-processing trick.
7. The authoring tool executes in the Editor; generated textures and standard Unity materials must be usable in client builds **without any plugin runtime component**.
8. GPU compute shaders are the primary generation backend, subject to supported-device checks and explicit error reporting. There is no requirement for a silently degraded CPU implementation.
9. **URP Lit** is the primary material export and preview target. The generated source maps should remain useful for custom shaders and future pipeline adapters.
10. Visual quality, controllability, stable exports and a simple workflow take precedence over a large preset catalogue or exotic effects.

## 3. Goals and Non-Goals

### 3.1 Goals

- Produce usable tiling materials for terrain, cliffs, paths, buildings and environmental props.
- Enable repeatable material generation from an editable saved recipe.
- Provide a fast iteration loop: **choose material → adjust parameters → preview → save**.
- Generate internally consistent PBR outputs from a shared procedural material description.
- Support both stylised restraint and more natural variants through deliberate parameter ranges.
- Export conventional Unity assets with correct import settings and predictable naming.
- Keep package code, shader resources and dependencies completely independent from Ninth Age.
- Support safe regeneration without discarding unrelated or hand-edited project assets.

### 3.2 Non-goals for the initial product

- Node-based authoring.
- Prompt-driven/AI texture generation.
- Image-to-PBR reconstruction from photographs.
- Procedural 3D modelling, terrain heightfield generation or biome placement.
- Substance Designer file compatibility or material format conversion.
- Mandatory cloud services, accounts, downloads or third-party generators.
- Runtime generation and server/headless build functionality.
- Automatically generating convincing arbitrary materials from a single generic noise preset.

## 4. Intended Users and Primary Workflow

The primary user is a Unity environment artist or developer who wants to produce material variations quickly without an external texturing application.

### 4.1 Core authoring workflow

1. Open **Tools → Procedural Texture Studio** in the Unity Editor.
2. Choose a material family and a named starter preset.
3. Adjust a short set of meaningful parameters (for example, stone fracture density or soil pebble coverage).
4. Inspect a live preview under neutral, grazing, and representative game lighting, including at normal gameplay distance.
5. Optionally change the seed to explore a different arrangement without changing the material character.
6. Choose output resolution, world-scale tiling guidance, and output maps.
7. Save the editable recipe to a chosen project folder.
8. **Generate Material** to create the texture assets and a URP Lit material in the selected output folder.
9. Apply the material to terrain or meshes using ordinary Unity material workflows. No connection to the plugin is required for rendering.

### 4.2 Editing existing work

- Reopen a recipe, adjust parameters and regenerate its outputs.
- The recipe records generator identity and version so later changes do not silently reinterpret old results.
- Regeneration updates previously generated, recipe-owned assets in place where safe, preserving Unity asset GUIDs and references.
- If files have been removed, moved, edited outside the tool or otherwise no longer match recorded ownership, the tool must report the conflict and request an explicit choice; it must not silently replace arbitrary assets.

## 5. Material Families and Visual Requirements

The software must use **material-specific feature models** rather than applying the same fractal noise with a different colour palette. Noise is a supporting technique, not a sufficient material generator.

### 5.1 Initial release families

| Family | Required defining features | Typical controllable parameters |
|---|---|---|
| **Stone / Rock** | Broken planes, directional fissures, weathering and mineral colour variation | Fracture density, scale, erosion, strata, roughness, mineral tones |
| **Soil / Dirt** | Granular aggregates, compacted areas, fines, occasional embedded stones | Grain size, compaction, pebble coverage, moisture, colour variation |
| **Grass / Ground Cover** | Recognisable fine vegetation clusters with soil visible between them; not green noise | Coverage, clump size, blade/patch direction, dry growth, soil exposure |
| **Sand** | Fine grain variation and coherent ripples/dunes at appropriate scales | Ripple wavelength, orientation, intensity, grain coarseness, dryness |

These four families constitute the initial quality bar; a convincing small catalogue is preferable to a broad set of poor presets.

### 5.2 Subsequent families

Gravel/scree, snow/ice, mud, cobblestone/paving, weathered timber and other environment materials may be introduced once the initial pipeline and quality tests have proven reliable.

**Material family** describes the generator; **preset** describes an authored configuration. For example, the Stone generator can provide presets such as *Weathered Granite*, *Layered Sandstone* and *Dark Basalt*. New presets should not require cloned generator logic.

### 5.3 Quality principles

- **Primary read first:** each material must be identifiable at typical gameplay distance before fine detail is considered.
- **Multiple deliberate scales:** broad value/roughness variation; recognisable medium-sized features; subtle fine grain.
- **Height-anchored geometry:** cracks, aggregates, ridges and depressions should produce coherent effects across height, normal, AO, colour and roughness.
- **Controlled repetition:** periodic boundaries are mandatory, while interior repetition should be minimised using non-obvious feature arrangements and varied scale/orientation.
- **Art-direction controls:** colour palettes, feature strength and detail frequency must be editable independently where meaningful.
- **PBR credibility:** ordinary soil, grass, sand, wood and non-metallic rock should not receive arbitrary metallic values or mirror-like specular response.
- **Visual stability:** preview materials should hold up under different light directions and the game's day/night lighting; heavy painted-in directional shadows are undesirable.

## 6. Generation Model

### 6.1 Recipe and parameters

Each editable recipe is a versioned Unity `ScriptableObject` containing:

- stable recipe identity and display name;
- material family / generator identifier;
- generator version and recipe schema version;
- deterministic integer seed;
- generator-specific parameters with safe ranges and meaningful units;
- chosen palettes and physically meaningful PBR control values;
- preview configuration (non-authoritative for exported textures);
- desired map selections and resolution;
- references/identifiers for the generated output set where relevant.

Factory presets bundled with the package are treated as read-only examples/templates. User-authored recipes and output assets are stored under `Assets/`, not edited directly in package content. **Duplicate Preset** creates an independent recipe. Recipes remain editable and are the authoritative source for regeneration.

### 6.2 Shared material description

A generator must first construct a consistent set of intermediate signals such as feature masks, elevations, surface classifications and material-property fields. The output maps derive from these shared signals rather than being fabricated independently.

Conceptual flow:

```text
Material family + preset + parameters + seed
                    ↓
       Tileable procedural features
                    ↓
       Height / masks / properties
                    ↓
    Albedo | Normal | Roughness | AO
         | Height | Metallic (where valid)
                    ↓
          Export + URP material
```

The flow is a **code-level processing pipeline**, not a user-visible node graph.

### 6.3 Seamless tiling

Every exported map must tile seamlessly on both axes. All feature generators and derivative computations must respect periodic boundaries:

- use periodic/tileable noise, wrapped feature placement, periodic domains or mathematically equivalent methods;
- wrap neighbourhood samples when computing normals, curvature, erosion-like filters and AO;
- avoid edge discontinuities introduced by later mask operations or transforms;
- validate the complete **output**, not just the base noise field.

Seam blending may be a specialised helper but must not serve as the default substitute for correct periodic generation.

### 6.4 Determinism and compatibility

- A seed changes spatial arrangement without unpredictably changing unrelated material parameters.
- UI preview and final export share the same generation logic. Reduced preview resolution is allowed but must not use a different look-producing algorithm.
- Algorithm changes that alter the appearance of an existing recipe require explicit generator-version handling or migration.
- Invalid parameter ranges, unsupported GPU features, failed shader dispatches and export failures must surface actionable errors. No silently incorrect or placeholder texture exports.

## 7. Texture Outputs and PBR Semantics

| Map | Meaning | Default handling |
|---|---|---|
| **Base Colour / Albedo** | Surface colour, without baked directional lighting | Export colour image; import with sRGB sampling enabled |
| **Normal** | Tangent-space surface normal derived from the same relief geometry | Unity-compatible tangent-space normal; import as Normal Map |
| **Height** | Normalised relative relief for the procedural tile | Linear scalar map; height range/strength stored in recipe |
| **Roughness** | Microsurface roughness (0 = smooth, 1 = rough) | Linear scalar map; retain canonical roughness in recipe/output |
| **Metallic** | Physically meaningful metallic fraction | Linear scalar map; default 0 for dielectric materials |
| **Ambient Occlusion** | Local relief-based occlusion, not a baked world-light shadow | Linear scalar map; adjustable local radius/strength |

### 7.1 URP Lit export adapter

The generator's canonical map set must be kept separate from Unity-specific channel packing. The URP export adapter creates/configures the material using the selected maps and the shader's actual supported inputs.

- The standard URP Lit metallic workflow uses metallic information in its metallic texture and **smoothness** in the configured alpha source; where appropriate generate `smoothness = 1 - roughness`, packed into metallic alpha.
- Configure the material's keywords, workflow mode, smoothness source, strength values and texture references correctly; assigning files alone is insufficient.
- Treat colour textures as sRGB and scalar/packed property maps as linear data; configure normal-map imports explicitly.
- Do not assume every surface shader consumes standalone height or AO maps in the same way. Unsupported inputs remain available as exported assets rather than being silently miswired.
- Document the specific URP Lit channel contract used by the package and test it with the supported Unity/URP version.
- Source maps should also be exportable individually for custom terrain shaders, triplanar shaders and other uses.

### 7.2 Resolution and format

Support **512, 1024 and 2048** square tile resolutions initially. **4096** is an optional quality/export tier once memory use and readback behaviour have been measured.

Use lossless file formats for first-generation source outputs, with 8-bit PNG acceptable for ordinary colour and many scalar maps. Higher precision height export is an explicit later capability unless a supported 16-bit encoding path is actually implemented and tested. Never claim a file contains 16-bit height data when an 8-bit conversion occurred.

Outputs require consistent filename suffixes such as `_Albedo`, `_Normal`, `_Height`, `_Roughness`, `_Metallic`, `_AO` and `_URP_Mask`, plus a `.mat` file. Individual maps can be disabled only when their absence does not invalidate the chosen export.

## 8. Editor User Experience

The interface must be a **single dockable UI Toolkit `EditorWindow`** with clear functional regions rather than multiple mandatory windows:

1. **Material library:** family and starter-preset selection, search/filter when needed.
2. **Parameters:** grouped controls with labels, sensible defaults, tooltips and Reset Group / Reset Preset actions.
3. **Preview:** mesh and texture preview, pan/orbit/zoom and different display modes.
4. **Output:** resolution, map selection, file destination, naming, asset generation/regeneration.

### 8.1 Controls

- Use meaningful artist-facing names such as *Crack Width*, *Grass Coverage* and *Pebble Density*; do not expose low-level noise coefficients as the main UX.
- Common controls include seed, colour palette, surface variation and overall relief strength; only show controls that make sense for the selected material.
- Allow numeric entry as well as slider control and show units/valid ranges when relevant.
- Show modified/unsaved state and provide undo support for recipe editing.
- Distinguish **preview refresh** from **export**. Editing a slider must never rewrite project assets automatically.
- Expose a **New Seed** action and a way to restore the previous seed.

### 8.2 Preview

- Provide sphere, plane and cube previews; the plane is essential for tiled surfaces.
- Allow a tiled plane view (for example, 3 × 3 repeats) to reveal seams and repetitive patterns.
- Provide material-lit and individual-map inspection modes.
- Allow orbit, zoom and adjustable light direction/intensity; a neutral lighting preset must remain available.
- Include an approximate gameplay-distance view; a strong close-up must not mask poor medium-distance readability.
- Apply preview quality scaling and debounce/throttle repeated edits to prevent UI stalls. A pending preview request supersedes obsolete pending requests.
- Show generation status and clear failure messages; do not block UI indefinitely.

## 9. Package Architecture and Portability

### 9.1 Embedded development package

Develop directly in the Ninth Age repository at:

```text
Packages/
└── com.frontierdev.procedural-texture-studio/
    ├── package.json
    ├── README.md
    ├── CHANGELOG.md
    ├── LICENSE.md
    ├── Editor/
    │   ├── FrontierDev.ProceduralTextureStudio.Editor.asmdef
    │   ├── UI/
    │   ├── Recipes/
    │   ├── Generators/
    │   ├── Processing/
    │   ├── Preview/
    │   ├── Export/
    │   └── Resources/
    │       └── ComputeShaders/
    ├── Tests/
    │   └── Editor/
    ├── Documentation~/
    └── Samples~/
```

The folders describe responsibility boundaries, not a mandatory class-by-class implementation plan. Unity `.meta` files and asset GUIDs must be included for tracked package assets. An Editor-only assembly is sufficient for the initial release. If another assembly becomes necessary later, it must be justified by an actual dependency or runtime-use case.

### 9.2 Dependency isolation

The package must **not** reference:

- `Game.Client`, `Game.Server`, `Game.Shared`, `Game.Core` or `Game.Editor`;
- Ninth Age-specific definitions, terrain controllers, streaming or Addressables code;
- PurrNet, Entities or game networking services;
- any scene, object or asset path outside its own package except user-selected input/output assets.

Allowed direct dependencies are Unity Editor/core APIs and the explicitly declared Unity package dependencies needed for the preview/export targets (primarily URP). Do not create hidden transitive dependencies on Ninth Age's project settings, layers, tags, input actions or scenes.

Shader resources and factory presets must resolve from the installed package, not by assuming the package lives under `Assets/Editor`. Save user assets only to locations the user explicitly chooses under `Assets/`.

### 9.3 Transferability

- The embedded directory must be valid as a standalone UPM package with a unique reverse-domain package identifier and semantic version.
- Users must be able to copy the directory to another project's `Packages/` folder or install it from a compatible local/Git package source.
- The package should compile and operate in a clean Unity 6.3 URP test project.
- Generated `.mat` and texture assets must remain valid if the plugin is subsequently removed (assuming the destination project retains the required URP shader or provides a compatible material conversion).
- The package documentation must include installation, Unity/URP version support, minimum graphics requirements, output conventions and removal/migration instructions.

### 9.4 Resource ownership

Generation uses GPU resources with explicit, exception-safe lifecycle management. Temporary `RenderTexture`s, buffers, textures, preview objects and readback resources must be cleaned up on success, cancellation, failure, assembly reload and window closure.

Use asynchronous GPU readback where supported and beneficial. Fail clearly if generation cannot run on the current environment; do not assume that a headless server or Unity batch process has a usable compute GPU. Tests that do not require hardware should be separated from GPU integration tests.

## 10. Asset Generation and Safety

- Export must validate output paths and asset names before writing.
- A failed generation or import must not leave a partially successful material masquerading as valid.
- A new export never silently overwrites an existing unrelated asset.
- Regeneration targets only explicitly associated outputs and should preserve their GUIDs to avoid breaking references.
- A manifest/recipe record must identify which assets belong to a generation set and their generator version.
- The tool must not modify an existing project's renderer assets, terrain settings, Quality settings, Graphics settings, Addressables configuration or other global options.
- Export/import settings must be explicit, repeatable and checked after import; Unity defaults are not a substitute.
- When Unity import or GPU export fails, report the failure and prevent the operation from being presented as complete.

## 11. Performance and Resource Requirements

The user experience should be responsive on a supported development GPU, but fixed generation-time promises must not be made before benchmarking. Profiling should distinguish feature generation, map conversion, GPU readback, encoding and AssetDatabase import.

Requirements:

- Preview at a lower resolution while actively editing, with full-resolution generation on demand.
- Avoid recomputing unchanged stages where safe; invalidation must not produce stale or inconsistent maps.
- Avoid per-slider-change synchronous file writes or `AssetDatabase.Refresh` calls.
- Control temporary GPU memory allocation, especially at 2048 and optional 4096 resolutions.
- Cancel/ignore obsolete preview jobs and prevent stale readbacks from replacing the latest preview.
- Surface memory or hardware-limit failures meaningfully.

The acceptance benchmark machine, maximum time and GPU memory budget should be set after an initial implementation is profiled in Ninth Age and in a clean test project.

## 12. Validation and Acceptance Criteria

### 12.1 Functional acceptance

- [ ] A user can open the tool in the Unity 6.3 Editor without entering Play mode.
- [ ] The four initial material families each have at least one deliberately authored, convincing factory preset.
- [ ] The user can modify meaningful parameters, change seed, preview and save a reusable recipe.
- [ ] Loading a saved recipe restores its authored settings.
- [ ] The tool exports a complete, internally consistent map set and correctly configured URP Lit material.
- [ ] A generated material renders correctly on a Unity mesh independently of the plugin.
- [ ] Preview and export use the same appearance-defining algorithms.
- [ ] The user can regenerate a recipe-owned output set without breaking existing Unity references.
- [ ] External/unowned assets are not silently overwritten.
- [ ] Unsupported GPU or failed export paths report actionable errors.

### 12.2 Visual acceptance

- [ ] Each material reads as its stated category at a normal Ninth Age gameplay camera distance.
- [ ] No obvious seam appears across a 3 × 3 repeat in base colour, height, normal, roughness or AO.
- [ ] Periodic boundary tests sample opposite edges and neighbourhood derivatives within an agreed numerical tolerance.
- [ ] Normal detail and roughness reflect the same relief features rather than contradictory independent noise.
- [ ] Materials remain visually legible under neutral, grazing, daytime and nighttime lighting.
- [ ] Dielectric materials do not display physically implausible metallic response.
- [ ] There are no dominant grid artefacts, oversized feature stamps or conspicuous repeated motifs except where intentionally authored.

### 12.3 Automated acceptance

- [ ] Recipe serialization and deserialization round-trip correctly.
- [ ] Parameter validation rejects invalid values with useful diagnostics.
- [ ] Identical recipe/seed/version inputs reproduce stable results within the supported tolerance.
- [ ] All outputs have expected dimensions, channel use and import settings.
- [ ] Normal vectors are valid and appropriately normalized, including across tile boundaries.
- [ ] Scalar property maps have finite, in-range values and defined colour-space handling.
- [ ] Export naming, conflict detection and GUID-preserving regeneration are covered by EditMode tests.
- [ ] GPU tests and non-GPU tests are separated so batch/headless limitations are explicit.
- [ ] The package compiles and generates assets when copied into a new clean Unity 6.3 URP project.
- [ ] Ninth Age client/server assemblies and player builds introduce no dependency on the authoring package.

## 13. Delivery Phases

| Phase | Scope | Exit criterion |
|---|---|---|
| **1. Package foundation** | Embedded UPM structure, Editor-only assembly, UI window, recipe schema, test harness, preview/export skeleton | Package loads independently and can round-trip a trivial recipe |
| **2. Generation foundation** | Periodic procedural operators, masks/height pipeline, normal/roughness/AO derivation, deterministic seeds | Verified seamless, consistent synthetic PBR maps |
| **3. First production materials** | Stone and soil generators, meaningful controls, authored factory presets | Materials pass visual and 3 × 3 tiling review |
| **4. Terrain-oriented materials** | Grass and sand generators, refinements to repeat suppression, gameplay-distance review | Four required families meet the initial quality bar |
| **5. Authoring/export polish** | Reliable URP Lit export, stable regeneration, saved recipes, undo/error handling, profiling | Production workflow passes functional and safety acceptance |
| **6. Portability validation** | Clean project installation, package documentation, versioning, regression tests | Portable first release can be transferred without Ninth Age |

The phases are a recommended implementation sequence, not evidence of completed work. Detailed GitHub issues should be scoped from them after design approval.

## 14. Deferred or Open Decisions

These decisions should be made through deliberate design/profiling rather than silently fixed by the first implementation:

1. **Package display name / final identifier:** `Procedural Texture Studio` and `com.frontierdev.procedural-texture-studio` are proposed names.
2. **Exact output format options:** whether lossless high-bit-depth scalar/height exports are needed in the first release.
3. **4096 support:** enable only after GPU memory use and import cost are evaluated.
4. **Alternative render pipelines:** HDRP/Built-in adapters are not first-release targets.
5. **Advanced blending:** two-material blends and layered moss, mud or wetness are useful, but must not compromise the simplicity and visual quality of individual generators.
6. **Variation sets:** one-click material families with several coordinated variants, anti-repetition masks or macro-tint textures are possible later enhancements.
7. **Custom shader interoperability:** explicit terrain shader adapters can be introduced later, but a generic, documented map export exists from the start.
8. **Non-GPU execution:** a CPU reference backend may be valuable for unit tests or portability but is not a hidden fallback requirement.

## 15. Definition of Done

The initial product is ready for practical use when a developer can install the package into Ninth Age, select one of four materially distinct generators, create a convincing seamless texture, adjust the parameters, inspect it at gameplay distance, export a correctly wired URP Lit material, save/regenerate the recipe without losing references, and then transfer the package to a clean Unity 6.3 URP project where the same workflow succeeds.

**The success criterion is the quality and reusability of the generated materials—not feature count.**

---

## References

- Ninth Age: `.docs/Graphical-Approach-PDD.md`
- Ninth Age: `.docs/CSharp-Style-Conventions.md`
- Ninth Age: `.docs/CSharp-Architecture.md`
- Unity manual — Custom packages: https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/cus-pkg-development/custom-packages
- Unity manual — URP Lit shader material: https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader