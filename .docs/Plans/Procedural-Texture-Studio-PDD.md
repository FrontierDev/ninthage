# Procedural Texture Studio — Product Design Document

**Status:** Proposed design specification  
**Project:** Ninth Age (first consumer and visual validation host)  
**Product:** Procedural Texture Studio (standalone desktop application)  
**Target platforms:** Windows, Linux and macOS, subject to validation  
**Primary output:** Engine-independent PBR texture maps, editable recipes and manifests  
**First downstream target:** Unity 6.3 URP, Forward+  
**Specification repository:** `FrontierDev/ninthage`  
**Implementation repository:** Separate desktop application repository (TBD)  
**PDD location:** `.docs/Plans/Procedural-Texture-Studio-PDD.md`  
**Last updated:** 2026-10-12

---

## 1. Purpose and Authority

Procedural Texture Studio is a **parameter-driven, non-node-based standalone desktop application** for producing attractive, reusable, seamless textures and physically based rendering (PBR) material maps. Its generation, preview and export workflows must work without Unity or another game engine.

The objective is **good-looking, production-usable environment materials with a short authoring loop**, not full Adobe Substance feature parity. Ninth Age is the first consumer and artistic validation host, **not a runtime, compile-time, installation or filesystem dependency**. Other engines and renderers must be able to consume the canonical outputs.

This PDD defines intended behaviour and acceptance criteria, not completed work. Detailed implementation issues belong in a separate plan, preferably the new application repository.

### 1.1 Relationship to Ninth Age's design standards

When authoring Ninth Age materials, the Graphical Approach PDD in this repository remains authoritative for visual direction: **grounded mid-poly fantasy, physically believable restraint, recognisable medium-scale forms and controlled normals**. Avoid raw scan aesthetics and uniformly noisy spatial detail. The standalone app is general-purpose; Ninth Age-specific presets must not hard-code the game's requirements into shared generation algorithms.

Ninth Age C# style and assembly conventions apply only to optional integration code residing in the game project. The independent application establishes its own coding conventions.

## 2. Locked Design Decisions

1. The product is a **standalone desktop application**, not a Unity Editor plugin or UPM package.
2. No Unity installation, project or editor session is required for generation, editing, preview, recipe saving or canonical export.
3. Authoring remains **preset- and parameter-driven**; no node editor.
4. Generation is deterministic given recipe, seed, generator version and a supported environment, within documented tolerances.
5. **Seamless 2D tiling** is a foundational requirement.
6. Canonical outputs are **portable PBR maps, a versioned JSON recipe and a machine-readable manifest**.
7. GPU compute is primary. Unsupported hardware/backends cause explicit errors, not silent CPU fallback.
8. The integrated 3D PBR preview shares appearance-generating algorithms with export.
9. Unity 6.3 URP Lit is the **first validation/integration target**, but not a core dependency.
10. Prioritise material quality, controllability, portability and safe regeneration over feature count.

## 3. Goals and Non-Goals

### 3.1 Goals

- Produce usable tiling materials for terrain, cliffs, paths, buildings and environmental props.
- Enable repeatable material generation from an editable saved recipe.
- Provide a fast iteration loop: **choose material → adjust parameters → preview → save**.
- Generate internally consistent PBR outputs from a shared procedural material description.
- Support both stylised restraint and more natural variants through deliberate parameter ranges.
- Export conventionally encoded PBR texture files with documented naming, colour spaces and channels.
- Keep standalone application code, shader resources and dependencies independent from Ninth Age, Unity and other engines.
- Support manifest-owned regeneration without discarding unrelated or externally edited files.

### 3.2 Non-goals for the initial product

- Node-based authoring.
- Prompt-driven/AI texture generation.
- Image-to-PBR reconstruction from photographs.
- Procedural 3D modelling, terrain heightfield generation or biome placement.
- Substance Designer file compatibility or material format conversion.
- Mandatory cloud services, accounts, downloads or third-party generators.
- Game-runtime generation, server execution and mandatory headless operation.
- Automatically generating convincing arbitrary materials from a single generic noise preset.

## 4. Intended Users and Primary Workflow

The user is an environment artist or developer creating and varying PBR materials without launching a game engine or operating a full node-based suite.

### 4.1 Core authoring workflow

1. Launch **Procedural Texture Studio** as a desktop application.
2. Choose a material family and starter preset.
3. Adjust meaningful material parameters.
4. Preview the resulting material on lit 3D objects, including approximate gameplay-distance and grazing-light views.
5. Change the seed to create variations.
6. Choose resolution, tiling scale, map selection, normal orientation and export preset.
7. Save an editable **JSON recipe** in a chosen directory.
8. **Export Material** to generate texture files and a manifest in a chosen folder.
9. Import the resulting maps into Unity, Unreal, Godot or a custom renderer. Optional engine adapters may streamline imports without becoming prerequisites.

### 4.2 Editing existing work

- Reopen and edit a recipe; record generator and schema versions to avoid silent reinterpretation.
- Manifest entries record outputs and content hashes to establish ownership.
- Regeneration may replace only verified owned files; moved, missing or externally changed files require a clear choice.
- The standalone application **never writes Unity .meta files, GUIDs or project settings**. Stable export filenames can help preserve Unity references on reimport, but GUID verification is Unity-side functionality.

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

A versioned, portable **JSON recipe** contains:

- stable recipe identity, display name and family/generator ID;
- schema and generator versions;
- deterministic seed;
- material-specific parameters with safe ranges and units;
- palettes, PBR controls and selected map outputs;
- resolution, normal convention and export preset;
- optional preview settings and output-set identifier.

Factory presets are read-only templates; **Duplicate Preset** creates an independent user recipe. Save recipes in user-selected directories, not as Unity ScriptableObjects under Assets. Define documented schemas, migrations and portable relative paths; never require machine-specific absolute paths.

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
         Export PBR maps + manifest
             + optional adapter
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
- Invalid inputs, unsupported GPU features, shader dispatch and export failures must produce actionable errors, never placeholder textures.
- Different GPU drivers/backends may yield small numerical differences; document tolerances for supported platforms instead of promising universal byte-identical results.

## 7. Texture Outputs and PBR Semantics

| Map | Meaning | Default handling |
|---|---|---|
| **Base Colour / Albedo** | Surface colour without baked lighting | Export sRGB colour map |
| **Normal** | Tangent-space normals from shared relief | Export with selectable OpenGL/DirectX green-channel orientation |
| **Height** | Normalised relative relief | Linear scalar; store strength/range in recipe |
| **Roughness** | Microsurface roughness (0 smooth, 1 rough) | Linear scalar; canonical roughness retained |
| **Metallic** | Physically meaningful metallic fraction | Linear scalar; zero for dielectric materials |
| **Ambient Occlusion** | Local relief occlusion, not directional shadows | Linear scalar; adjustable radius/strength |

Document channel order, colour spaces, UV orientation, normal convention, height range and bit depth explicitly.

### 7.1 Engine-neutral export and adapters

The **default export** consists of standalone image maps and a versioned JSON manifest containing filenames, map roles, dimensions, formats, channel conventions, hashes, generator and version. The canonical map data must be separate from engine-specific channel packing.

An **optional Unity URP preset** may produce packed derivative maps with smoothness = 1 - roughness in the relevant channel. However, **Unity .mat creation, shader keywords, TextureImporter settings and GUID preservation must happen inside Unity** via a documented manual workflow or a small optional importer. The standalone application must not write .mat or .meta assets. No Unity integration can be required for its core functionality.

Other engine or shader export presets can be added independently. Source maps remain available individually for terrain, triplanar and custom rendering.

### 7.2 Resolution and format

Support **512, 1024 and 2048** square tiles initially; **4096** is optional after profiling. Use lossless source images. 8-bit PNG is acceptable for colour and many scalar maps; higher-precision height support requires a tested encoder and an explicit documented contract.

Use predictable suffixes such as _Albedo, _Normal, _Height, _Roughness, _Metallic and _AO plus an export manifest. Optional adapters may emit _URP_Mask or other packed derivatives. **No .mat file is required or emitted by normal standalone export.**

## 8. Desktop User Experience

Use a **single-window desktop application** with material library, grouped parameters, integrated 3D preview and file export regions. Provide artist-facing controls and avoid exposing raw noise coefficients as primary inputs.

### 8.1 Controls

- Meaningful labels, sensible ranges, sliders plus numeric inputs, tooltips and grouped reset.
- Seed change/restoration, unsaved-change indication and undo.
- Only show parameters relevant to the selected material.
- Use system file pickers and platform-appropriate keyboard/display scaling.
- Slider interaction refreshes preview, **never rewrites output files**.

### 8.2 Preview

- Plane, sphere and cube previews; a tiled **3 × 3 plane** reveals seams/repetition.
- Lit and individual map inspection; orbit, zoom and adjustable light direction/intensity.
- Neutral lighting and approximate gameplay-distance mode.
- An **engine-independent physically plausible PBR preview shader** with documented semantics. Differences from Unity URP must be validated, not hidden.
- Preview and export use identical appearance-defining algorithms; lower preview resolution is permitted.
- Debounce changes, discard superseded jobs, show failures and keep the interface responsive.

## 9. Standalone Architecture and Portability

### 9.1 Recommended technology

**Rust + egui + wgpu/WGSL** is the recommended initial desktop stack, subject to a small technical prototype. egui supplies the UI, wgpu provides GPU compute and the 3D preview. This avoids Unity and browser-surface integration dependencies. Saved material formats must remain implementation-agnostic.

Create a **dedicated application repository** with clear boundaries for: recipe/schema/core generators; GPU operators/shaders; 3D preview; canonical export/manifest/optional adapters; UI and asynchronous job orchestration; presets, tests and platform packaging. Generator logic must not depend on the UI or any particular engine. A Unity importer, if desired later, remains a separate thin package.

### 9.2 Dependencies

The application must not reference Unity Editor/runtime APIs, Unity compute shaders, Ninth Age assemblies, Addressables, PurrNet, Unity scenes or project settings. Use supported wgpu backends (typically Direct3D 12, Vulkan and Metal) and validate their required device limits/features. Unsupported configurations fail visibly, never with a silently inferior material.

### 9.3 Distribution

- Ship portable/installable Windows, Linux and macOS releases **after testing each advertised combination** of OS, architecture and GPU backend.
- Unity, an engine SDK and a cloud service are not installation prerequisites.
- Recipes, presets and manifests travel between platforms using portable paths and explicit schema versions.
- Document minimum requirements, supported GPU/OS matrix, installation, updating, migrations, output contracts and removal.
- Unity compatibility is verified through map import into a clean Unity 6.3 URP project, separate from the core application tests.

### 9.4 GPU resource ownership

Release temporary GPU buffers/textures, staging readbacks and preview objects on completion, failure, cancellation or exit. Prefer asynchronous readback where appropriate. Separate CPU-only recipe/manifest tests from GPU device tests.

## 10. Export and File Safety

- Validate destination paths, filenames, permissions and available disk space.
- Produce outputs in temporary locations when practical; only publish a complete, validated set and manifest.
- Never silently overwrite unknown or externally modified files. Use manifest ownership and recorded hashes for safe regeneration.
- Record version, map roles, formats and output ownership in the manifest.
- Never edit an engine project's renderer settings, Unity .meta files, GUIDs, Addressables or other engine-owned configuration.
- Keep names/paths stable for reimport, but treat Unity GUID preservation as Unity importer behaviour, **not** something a desktop exporter can guarantee.
- Report failed generation, encoding and writing accurately.

## 11. Performance and Resource Requirements

Keep the UI responsive on a supported development GPU; do not promise fixed timings before measurement. Profile generation, conversion, GPU readback, encoding, disk I/O and 3D preview separately. Unity import is a downstream process, not part of application generation time.

- Use reduced-resolution interactive previews and full-resolution export.
- Cache stages safely, with correct invalidation.
- Avoid synchronous disk writes/GPU readbacks in interactive slider updates.
- Bound GPU memory at 2048 and optional 4096 resolutions.
- Cancel superseded previews and prevent stale results from replacing current ones.
- Surface hardware/memory errors instead of stalling.

Define acceptable timings and hardware budgets after representative platform benchmarks.

## 12. Validation and Acceptance Criteria

### 12.1 Functional acceptance

- [ ] Application opens, edits, previews, saves and exports with **no Unity installation**.
- [ ] Each of the four initial families has a convincing authored preset.
- [ ] Editable portable recipes restore all parameters and versions across supported operating systems.
- [ ] Canonical PBR maps and complete versioned manifest export correctly.
- [ ] Preview and output share the same look-producing algorithms.
- [ ] Safe regeneration preserves unrelated/external files and identifies conflicts.
- [ ] Unsupported GPUs, invalid inputs and export failures show useful errors.
- [ ] Maps import and render correctly in Unity 6.3 URP, independently of a running studio.
- [ ] Packaged workflows work on each advertised desktop OS.

### 12.2 Visual acceptance

- [ ] Materials read correctly at normal Ninth Age gameplay distance.
- [ ] No visible seams in 3 × 3 repeats of colour, height, normal, roughness or AO.
- [ ] Tile-edge samples and neighbourhood derivatives satisfy numerical tolerances.
- [ ] Colour, height, normal, roughness and AO derive from coherent feature geometry.
- [ ] Materials stay legible under neutral, grazing, day and night lighting.
- [ ] Dielectrics avoid implausible metallic response.
- [ ] No unintended grid artefacts, oversized stamps or conspicuous repetition.
- [ ] Unity URP appearance broadly matches the standalone preview within documented shader/lighting differences.

### 12.3 Automated acceptance

- [ ] JSON recipe/manifest round trips and schema migrations pass tests.
- [ ] Invalid values fail validation.
- [ ] Same inputs give repeatable results within supported GPU/backend tolerances.
- [ ] Map dimensions, colour spaces, normal orientation, channels and scalar values validate.
- [ ] Normal vectors and periodic derivatives remain valid across boundaries.
- [ ] Atomic export, manifest ownership, external modifications and failure cleanup are covered.
- [ ] CPU-only schema/export tests and GPU integration tests are separated.
- [ ] Windows/Linux/macOS builds and tests exist; advertised GPU configurations are hardware-tested.
- [ ] Clean Unity 6.3 URP imports succeed without Ninth Age client/server or player-build dependency.

## 13. Delivery Phases

| Phase | Scope | Exit criterion |
|---|---|---|
| **1. Standalone foundation** | Dedicated repository, Rust/egui app, wgpu setup, JSON recipe schema and tests | Launches without Unity and round-trips a recipe |
| **2. GPU generation foundation** | Periodic operators, shared feature maps, normal/roughness/AO, deterministic seeds | Verified seamless synthetic PBR |
| **3. First materials** | Stone and soil, meaningful controls, factory presets | Visual and tiling checks pass |
| **4. Terrain materials** | Grass and sand, repeat suppression, gameplay-distance review | All four families meet quality bar |
| **5. UX/export polish** | Preview, manifest export, safe regeneration, optional URP packing, profiling | Reliable standalone workflow and Unity map import |
| **6. Portability validation** | Tested builds for Windows/Linux/macOS, documentation, versions and regressions | First self-contained release on all advertised platforms |

Phases are proposals, not claims of work completed. App-specific issues should live in the new application repository.

## 14. Deferred or Open Decisions

1. **Name/repository:** Procedural Texture Studio is a working title; create a dedicated repository.
2. **Implementation stack:** validate Rust/egui/wgpu preview integration and platform support with a prototype.
3. **Support matrix:** establish minimum OS/GPU/backend combinations from actual tests.
4. **Precision:** decide whether 16-bit height and EXR are required for the first release.
5. **4096:** benchmark memory, readback and file encoding.
6. **Unity importer:** optional lightweight integration after validating manual URP imports.
7. **Other engine adapters:** Godot, Unreal and custom packing can follow without changing canonical maps.
8. **Advanced blends/variation sets:** later additions after baseline visual quality succeeds.
9. **CPU generation and CLI:** possible future options, not silent fallback requirements.

## 15. Definition of Done

The product is ready for practical use when a developer can **launch it without Unity**, select one of four materially distinct generators, author convincing seamless PBR maps, inspect them at gameplay distance, save and reopen portable recipes, safely export and regenerate maps with a manifest, and import the result into a clean Unity 6.3 URP project. The canonical maps must also be useful in other engines without requiring Ninth Age or Unity.

**Success is measured by material quality, portability and trustworthy exports — not feature count.**

---

## References

- Ninth Age Graphical Approach PDD (development branch)
- Unity Manual — URP Lit shader and material conventions: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/lit-shader.html
- wgpu: https://wgpu.rs/
- egui: https://github.com/emilk/egui
