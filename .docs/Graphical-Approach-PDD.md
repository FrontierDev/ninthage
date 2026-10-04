# Ninth Age — Graphical Approach Product Design Document

**Status:** Authoritative visual-design ground truth  
**Project:** Ninth Age  
**Engine:** Unity 6.3  
**Rendering:** Universal Render Pipeline (URP), Forward+  
**Scope:** World visuals, environment assets, terrain integration, architecture, foliage, actors, props, VFX, lighting, atmosphere, post-processing and UI cohesion  
**Last updated:** 2026-10-04

---

## 1. Purpose and Authority

This document defines the graphical approach for **Ninth Age** and is the ground truth for future visual, asset-production and rendering work.

The game must not drift toward either photorealistic/high-poly asset production or a deliberately low-poly/cartoon presentation. The target is a **grounded mid-poly fantasy world presented with modern lighting, atmosphere and post-processing**.

Where a future asset, shader, scene, tool or rendering feature conflicts with this document, this document takes precedence until it is intentionally revised.

The central rule is:

> **The authored world is mid-poly. The presentation of that world is high-fidelity.**

The game should look richer than the underlying asset complexity would suggest. Perceived fidelity should come primarily from composition, lighting, fog, weather, VFX, material response and atmospheric depth rather than from excessive geometric or texture detail.

Gameplay, readability and performance take priority over graphical fidelity. A visual feature that makes the game substantially harder to run, build, traverse or maintain must justify that cost through meaningful improvement to the player experience.

---

## 2. Visual Identity

### 2.1 Target Style

Ninth Age uses **grounded mid-poly fantasy visuals**.

This means:

- realistic or near-realistic proportions;
- mature, gritty fantasy presentation;
- simplified geometry with broad, readable forms;
- strong silhouettes;
- restrained high-frequency surface detail;
- physically believable material behaviour;
- deliberate use of colour rather than constant saturation;
- atmospheric depth through fog, haze, smoke, weather and lighting;
- modern realtime presentation applied to economical assets.

The world must not look overtly low-poly. Geometry may be economical, but players should primarily perceive **clear shapes and cohesive art direction**, not triangle reduction.

The world must also not pursue photorealism. Photogrammetry-style rocks, scan-density cliffs, strand-heavy foliage, cinematic character meshes and pervasive 4K–8K unique textures are contrary to the target unless a narrowly justified hero asset requires them.

### 2.2 Fidelity Position

The intended visual position is between strongly stylised low-poly fantasy and high-detail realistic fantasy, but closer to grounded realism in proportion, material behaviour and atmosphere.

Useful reference boundaries are:

- **RuneScape: Dragonwilds** — too overtly low-poly/stylised to be the Ninth Age target;
- **AION and modern high-detail fantasy MMOs/RPGs** — higher raw asset fidelity than Ninth Age should target;
- **WoW: Forever** — useful as a reference for how relatively economical world geometry can be elevated by lighting, atmosphere, colour and composition, but not a direct art-style target.

Ninth Age should borrow the **allocation of fidelity**, not the cartoon proportions or visual language of World of Warcraft.

### 2.3 Grit and Maturity

The setting is gritty. Assets should support age, wear, weather, conflict and environmental history.

Appropriate visual language includes:

- worn timber;
- damaged masonry;
- faded cloth;
- oxidised and scratched metal;
- damp stone;
- mud;
- soot;
- moss;
- ash;
- weathering;
- patched or repaired construction;
- restrained grime accumulation.

This wear should generally be delivered through materials, decals, vertex colour, masks and selected geometry rather than indiscriminate mesh density.

---

## 3. Fidelity Model

Ninth Age separates **content fidelity** from **presentation fidelity**.

### 3.1 Content Fidelity

The authored content layer is intentionally economical:

- generated terrain provides the large-scale landform;
- terrain-kit meshes add cliffs, shelves, caves, banks and other non-heightfield forms;
- foliage uses a limited, reusable vocabulary;
- architecture is primarily kitbashed from modular components;
- characters and actors use mid-poly models with strong silhouettes;
- props use geometry where it materially affects silhouette or interaction;
- textures and normal maps support forms rather than attempt to replace art direction with noise.

### 3.2 Presentation Fidelity

The presentation layer makes the world feel richer:

- realtime directional lighting;
- the required day–night cycle;
- dynamic local lights;
- shadows;
- atmospheric fog and distance haze;
- weather;
- smoke, mist, ash, dust and other environmental VFX;
- water response and reflections where appropriate;
- restrained bloom;
- colour grading;
- ambient and contact shading where justified;
- biome-specific atmosphere;
- strong foreground/midground/background separation.

Quality scaling should preferentially reduce presentation cost before compromising core gameplay silhouettes or making assets structurally inconsistent.

---

## 4. Rendering and World Constraints

The client uses **Unity 6.3 with URP Forward+**. The graphical approach must be designed around a very large seamless MMORPG world rather than around small, cinematic scenes.

The project also requires:

- a day–night cycle;
- chunk-based world streaming;
- terrain loaded according to visibility/LOD requirements;
- additive loading of world-object content;
- a headless server with no graphics dependency.

Visual systems must therefore be client-side, scalable and compatible with large view distances, repeated assets, streamed world content and potentially large numbers of actors.

A graphical feature is not successful merely because it looks good in a static test scene. It must remain viable when used across a large open world.

---

## 5. Terrain: Existing Foundation

### 5.1 Terrain Generation Is Not the Art-Style Problem

Ninth Age already has terrain generation. Future visual work must build on that system rather than replacing it merely to create a mid-poly appearance.

The current world tooling includes:

- heightmap-driven terrain generation;
- 256 m terrain chunks;
- Unity Terrain for the highest-detail terrain representation;
- mesh-based lower terrain LODs;
- erosion tooling;
- terrain auto-painting;
- zone-based texture arrays;
- streamed terrain/world chunks.

The terrain heightfield is responsible for **macro landform**:

- valleys;
- ridges;
- plateaus;
- hills;
- basins;
- large slopes;
- river courses;
- broad mountain shapes.

It does not need to become visibly faceted or low-resolution in order to support the visual style.

### 5.2 Terrain Detail

Terrain detail must be restrained.

Fine procedural height noise may be used to prevent sterile surfaces, but the terrain should not become a field of constant small bumps. The preferred visual hierarchy is:

1. large landform;
2. readable local terrain shape;
3. authored terrain-kit geometry;
4. material detail;
5. sparse small ground detail.

The player should read a hillside as a hillside before reading individual stones or noise features.

---

## 6. Terrain Materials and Zone Texturing

The existing terrain system already provides a useful semantic foundation for the art direction.

Terrain auto-painting currently classifies terrain into slope/height-driven material bands including:

- base terrain;
- mid-slope transition;
- talus/scree;
- cliff/exposed rock;
- snow;
- ice.

Zone definitions provide texture arrays used by the terrain material system. Future terrain-adjacent shaders should be designed to cooperate with this system rather than inventing unrelated material logic for each prop type.

### 6.1 Material Goals

Terrain materials must:

- use broad, readable colour and roughness variation;
- avoid excessive high-frequency normal detail;
- preserve world-scale consistency;
- remain coherent across terrain chunks;
- support biome identity;
- remain visually stable under different times of day and weather;
- avoid an obvious scanned/photogrammetric appearance.

### 6.2 World-Space Consistency

Terrain and terrain-kit meshes should share a consistent sense of texture scale.

Where practical, reusable environmental materials should use world-space or triplanar techniques so that cliff faces, rocks and terrain transitions do not reveal arbitrary UV scale differences.

---

## 7. Terrain Kit

The terrain kit is a primary source of the game's mid-poly visual language.

The generated heightfield creates the world surface. Authored terrain-kit meshes provide shapes that heightfields cannot represent well or that benefit from stronger art direction.

Typical terrain-kit assets include:

- cliff faces;
- cliff corners and caps;
- escarpments;
- rock shelves;
- large boulders;
- medium boulders;
- talus/scree forms;
- overhangs;
- cave entrances;
- tunnel entrances;
- river banks;
- shore edges;
- ravine walls;
- volcanic formations;
- ruined terrain-integrated masonry where appropriate.

### 7.1 Terrain-Kit Geometry Style

Terrain-kit geometry must use:

- large, deliberate planes;
- strong silhouette changes;
- broad forms;
- limited subdivision where the silhouette does not require it;
- restrained geometric surface noise;
- smooth or controlled normals where needed to prevent crude faceting.

Do not model every crack, chip, pebble or fracture.

A useful test is:

> If geometry can be removed without materially changing the object at normal gameplay distance, that geometry probably should not exist.

This does not mean every rock must look faceted. It means geometry should be spent on meaningful shape.

### 7.2 Cliff and Terrain-Overlay Shaders

Terrain-kit meshes must not appear glued onto the terrain.

Specialised shaders for cliff and terrain-overlay assets must be developed around seamless contact with the generated terrain.

Functional requirements include:

- terrain-compatible world-space texture scale;
- triplanar or equivalent projection on steep surfaces where useful;
- blending of the overlay mesh into the terrain near intersections;
- the ability to sample or reproduce the relevant terrain material near the contact region;
- smooth colour, normal and roughness transition at the seam;
- support for authored masks or vertex colours where automatic blending needs art direction;
- compatibility with future wetness, snow, ash or other environmental overlays;
- no obvious hard seam at ordinary gameplay camera distances.

The exact technical implementation may evolve. The visual requirement does not: **terrain-kit geometry must look embedded in the terrain, not placed on top of it.**

Where practical, the cliff base should transition from rock into the terrain material below it, while cliff tops and shelves should be capable of inheriting the surrounding ground treatment.

---

## 8. Foliage and Ground Detail

### 8.1 Density Philosophy

“Dense forest” does not mean maximum object count.

Forest density should be communicated through:

- foreground trunks;
- overlapping mid-distance trees;
- canopy framing;
- controlled undergrowth;
- terrain occlusion;
- haze and fog;
- distant tree LODs or impostors;
- composition that limits long sightlines where appropriate.

The game must preserve traversal, combat readability and performance.

### 8.2 Trees

Trees should be mid-poly and silhouette-led.

Priorities are:

1. recognisable species silhouette;
2. convincing trunk and major branch structure;
3. canopy mass;
4. acceptable close-range material response;
5. efficient distant representation.

Avoid building photorealistic tree assets whose complexity is largely invisible during normal play.

Foliage cards, leaf clusters and branch cards are acceptable and expected where they provide the intended look efficiently. Alpha overdraw must be controlled.

### 8.3 Undergrowth

Ground detail should be **clustered**, not uniformly carpeted.

Use grasses, ferns, reeds, flowers, fungi, fallen branches and shrubs to reinforce local ecology and composition. Concentrate detail around:

- water;
- tree bases;
- rocks;
- paths;
- ruins;
- biome transitions;
- sheltered terrain;
- points of interest.

Large areas of continuous dense grass are not the default.

### 8.4 Dead and Diseased Vegetation

Corrupted, undead, volcanic and otherwise hostile zones should often derive identity from altered vegetation silhouettes and distribution rather than simply recolouring healthy assets.

Dead trunks, sparse canopies, fungal forms, ash-covered vegetation and twisted silhouettes are preferable to excessive geometric decoration.

---

## 9. Architecture and Settlements

Architecture should be built primarily from **modular kits**.

A settlement should not require every building to be a unique bespoke mesh.

Typical kit components include:

- foundations;
- wall sections;
- corners;
- beams;
- pillars;
- roof sections;
- roof caps;
- chimneys;
- windows;
- doors;
- stairs;
- balconies;
- arches;
- fences;
- gates;
- bridge sections;
- decorative trims;
- faction-specific attachments.

### 9.1 Architectural Style

Architecture must use grounded proportions with selective exaggeration for readability.

Examples:

- structural beams may be slightly thicker than real-world equivalents;
- windows and doors may be slightly larger for visual clarity;
- roof shapes should be readable at distance;
- towers should have distinctive silhouettes;
- faction or cultural motifs should use large readable forms before tiny ornament.

This is not permission for cartoon architecture. Exaggeration exists to preserve readability in an MMO camera, not to create caricature.

### 9.2 Reuse and Variation

Variation should come from recombining a controlled kit through:

- different footprints;
- roof combinations;
- material variants;
- damage states;
- prop dressing;
- banners and faction elements;
- vegetation;
- terrain relationship;
- lighting;
- decals.

Unique buildings are appropriate for landmarks, major civic structures, dungeons and other important locations.

---

## 10. Characters and Actors

Characters are a higher-priority fidelity class than most environment assets, but they remain part of the same visual language.

### 10.1 Character Geometry

Characters should use:

- grounded anatomy;
- realistic or near-realistic proportions;
- clean body and equipment silhouettes;
- broad clothing folds;
- readable armour segmentation;
- restrained micro-geometry;
- simplified but expressive faces;
- efficient hair solutions such as sculpted clumps/cards rather than strand-heavy systems by default.

A character should look intentionally modelled, not decimated.

### 10.2 Equipment

Weapons and armour should prioritise silhouette and recognisability.

Small engravings, stitching and surface decoration should usually be baked or textured. Geometry should be reserved for details that materially affect silhouette, animation, attachment or close-range readability.

Slight exaggeration of weapon thickness, guards, armour plates or iconic shapes is acceptable where it materially improves readability while preserving the grounded tone.

### 10.3 Rigging

Production characters should use a controlled Ninth Age rigging standard rather than relying blindly on third-party automatic rigs.

The final production topology must be established before final rigging and skin-weight work.

High-value humanoids should share a standard or compatible humanoid skeleton wherever practical to maximise animation reuse and retargeting quality.

Joint placement and deformation quality matter more than visually tidy bone placement. Shoulders, elbows, wrists, hips, knees, ankles, hands and the face require particular care.

### 10.4 AI-Generated Models

AI-generated models may be used as **source assets or design accelerators**, but they are not automatically production-ready.

A typical AI-assisted character or prop path is:

```text
concept / prompt / reference
        ↓
AI-generated source mesh
        ↓
mesh cleanup
        ↓
retopology into Ninth Age geometry standards
        ↓
UV / material preparation / baking
        ↓
final mesh validation
        ↓
rig and skin weights if required
        ↓
LOD and collision authoring
        ↓
Unity import
```

AI tools must not define the final game's topology density or rig structure merely because that is what they generate by default.

For AI-generated characters, automatic rigging may be useful for previewing motion, but important player characters and reusable actors should be properly rigged to the project standard.

---

## 11. Props, Weapons and Interactables

Props follow the same silhouette-first rule.

Important questions are:

- Can the player identify the object quickly?
- Does the silhouette survive normal gameplay distance?
- Does the object communicate interaction or function?
- Is its detail level appropriate to its importance?

Small incidental props should be extremely economical. Hero props may spend more geometry and texture budget where the camera and gameplay justify it.

Repeated props should favour shared materials, trim sheets, atlases or other efficient reuse strategies rather than unique high-resolution material sets for every object.

---

## 12. Materials and Texturing

### 12.1 PBR, but Restrained

Ninth Age retains physically believable material behaviour.

Metal should read as metal. Cloth should read as cloth. Wet stone should respond differently from dry stone. Wood should not look like plastic.

The style is achieved by simplifying **material complexity**, not by abandoning PBR.

### 12.2 Surface Frequency

Avoid covering every material with multiple layers of equally strong fine detail.

The preferred hierarchy is:

1. base colour and material identity;
2. broad roughness/value variation;
3. medium-scale wear or pattern;
4. restrained normal detail;
5. selective local accents.

The material should still read correctly when seen from a normal gameplay camera.

### 12.3 Texture Resolution

Texture resolution must be justified by screen-space usage and reuse.

General principles:

- use shared tiling materials wherever practical;
- use trim sheets and atlases for architecture and repeated props;
- use unique texture sets when the asset genuinely requires them;
- do not use 4K or 8K textures merely because they are available;
- prefer consistent texel density over arbitrary maximum resolution;
- texture streaming and memory cost are part of asset quality.

Exact per-category texel-density targets may be defined later in a dedicated asset-budget document without changing the art direction in this PDD.

### 12.4 Decals and Local Variation

Use decals, masks and local overlays for:

- dirt;
- leaks;
- soot;
- blood;
- moss;
- damage;
- road wear;
- faction markings;
- environmental storytelling.

Do not bake all local variation into unique geometry or unique base textures when a reusable overlay solves the problem.

---

## 13. VFX

VFX are a major part of the final fidelity layer.

### 13.1 Environmental VFX

Environmental VFX include:

- mist;
- fog banks;
- smoke;
- embers;
- drifting ash;
- dust;
- snow;
- rain;
- water spray;
- insects;
- pollen;
- magical contamination;
- heat distortion where appropriate.

These effects should reinforce biome identity and atmosphere without making the world visually unreadable.

### 13.2 Combat and Magic

Combat VFX must be readable before they are spectacular.

Spell effects should communicate:

- source;
- target or area;
- damage/healing type where relevant;
- timing;
- threat or danger;
- persistence.

Avoid excessive particle density or bloom that obscures actors and ground telegraphs in group combat.

### 13.3 VFX Style

VFX may be more stylised and saturated than ordinary world materials because they need to communicate gameplay, but they must remain compatible with the gritty setting and Radiant Slate UI.

---

## 14. Lighting

Lighting is one of the principal ways Ninth Age achieves high perceived fidelity.

### 14.1 Day–Night Cycle

The required day–night cycle must produce meaningful visual differences between:

- dawn;
- morning;
- midday;
- afternoon;
- sunset;
- dusk;
- night.

The world should not simply become darker at night. Direction, colour temperature, sky contribution, fog, local lights and contrast should all change coherently.

### 14.2 Directional Light

The sun/moon directional-light system should provide the dominant large-scale scene shape.

Low-angle light should be used to create depth and readable terrain where appropriate. Midday scenes may be flatter, but must still preserve material and form readability.

### 14.3 Local Lights

Lanterns, windows, torches, lava, magical sources and other local lights should be used strategically to establish landmarks, navigation cues and atmosphere.

The Forward+ path permits richer local-light use than a minimal forward renderer, but light count is still a performance resource.

### 14.4 Night Readability

Night must remain playable.

Dark environments may be grim and high-contrast, but navigation surfaces, actors and important gameplay information must remain legible without flattening the atmosphere.

---

## 15. Fog, Atmosphere and Weather

Atmosphere is a primary fidelity multiplier.

### 15.1 Atmospheric Perspective

Scenes should deliberately separate:

- foreground;
- midground;
- background.

Distance haze, fog and colour shift should prevent large landscapes from looking like collections of equally sharp objects.

### 15.2 Fog

Fog should be biome- and weather-aware.

Examples include:

- low morning valley fog;
- marsh mist;
- sea haze;
- volcanic smoke and ash;
- cold mountain haze;
- undead taint or magical fog;
- rain haze.

Fog must not be used to conceal poor scene composition. It should enhance good composition.

### 15.3 Weather

Weather should materially affect scene presentation where practical:

- rain changes atmosphere and surface response;
- snow reduces saturation and changes accumulation;
- storms alter light and cloud character;
- volcanic zones may carry ash and ember fall;
- undead zones may use abnormal fog colour and particulate behaviour.

---

## 16. Post-Processing

Post-processing should raise presentation quality without becoming the art style by itself.

Appropriate uses include:

- colour grading;
- tonemapping;
- controlled bloom;
- vignette only where justified;
- exposure management;
- ambient/contact shading where appropriate;
- subtle sharpening or anti-aliasing support where required.

Avoid:

- excessive bloom;
- crushed blacks that damage gameplay readability;
- extreme saturation;
- heavy film effects that fight the UI;
- permanent cinematic grading that makes every biome look identical.

The game should still look coherent with post-processing reduced. Post-processing enhances the authored scene; it does not rescue a scene with poor shapes or materials.

---

## 17. Water

Water should follow the same philosophy as the rest of the project: relatively economical geometry with strong presentation.

Priorities are:

- readable surface motion;
- coherent reflection/specular response;
- colour appropriate to the biome;
- fog/depth treatment;
- shoreline integration;
- waterfalls and spray where appropriate;
- efficient long-distance rendering.

Rivers and marshes should derive much of their quality from reflections, lighting, mist, bank treatment and surrounding foliage rather than geometric complexity in the water mesh.

---

## 18. UI Cohesion — Radiant Slate

The **Radiant Slate** design sheet is the grounding point for the interface.

The UI visual language is:

- dark slate and charcoal foundations;
- restrained metallic/gold accents;
- high readability;
- clean typography;
- controlled use of blue, green, orange and other semantic colours;
- fine ornament rather than excessive decoration;
- mature fantasy rather than cartoon framing.

The world does not need to reuse the literal UI palette everywhere, but it should feel compatible with it.

A useful relationship is:

- world: restrained natural palette and gritty material variation;
- UI: dark neutral foundation with clear semantic accents;
- magic/interactables: allowed to use stronger accent colour where gameplay requires it.

The interface must remain legible over bright snow, dark marshes, volcanic scenes, forests and settlements without requiring each world biome to be artificially darkened around the HUD.

---

## 19. Asset Production Standards

### 19.1 General Asset Workflow

A standard non-character asset should normally pass through:

```text
brief / concept
    ↓
blockout and scale validation
    ↓
high/source model if needed
    ↓
production mesh / retopology
    ↓
UV and material assignment
    ↓
bake where required
    ↓
texture/material authoring
    ↓
LOD creation
    ↓
collision
    ↓
Unity import and validation
    ↓
placement test in representative lighting
```

No asset is complete merely because it looks correct in a modelling package.

### 19.2 Gameplay-Distance Test

Every asset must be judged at the camera distance at which players normally see it.

Close-up beauty shots are secondary.

Questions to ask:

- Does the silhouette read?
- Is the geometry visibly more complex than necessary?
- Is the material too noisy?
- Does it survive different lighting?
- Does it still look good beside other Ninth Age assets?
- Does the LOD transition preserve identity?

### 19.3 Scale

Assets must use consistent real-world scale in Unity.

Artistic exaggeration may alter proportions for readability, but inconsistent physical scale between kits is not acceptable.

### 19.4 Pivot and Modularity

Modular assets should use predictable pivots and dimensions so that kitbashing does not require arbitrary transforms or correction objects.

Architecture pieces should snap cleanly according to the relevant kit standard.

### 19.5 Collision

Rendering geometry and collision geometry should be treated separately where appropriate.

Do not use unnecessarily detailed visual meshes as colliders when a simpler collider communicates the same gameplay surface.

---

## 20. LOD and Distance Strategy

LOD is part of asset authoring, not a late optimisation pass.

Significant world assets should be designed as a distance family:

```text
LOD0 — close gameplay view; full intended silhouette and required local detail
LOD1 — preserves silhouette and major material identity
LOD2 — preserves category and large shape
LOD3 / impostor — distant representation where appropriate
```

The exact number of LODs depends on the asset class.

Priority order when reducing detail:

1. remove invisible/internal geometry;
2. remove small non-silhouette geometry;
3. simplify local shape while preserving silhouette;
4. consolidate materials where possible;
5. reduce small texture/material features;
6. use cards/impostors for distant vegetation or other suitable assets.

LOD changes should not obviously alter the character of an object.

---

## 21. Performance as an Art Requirement

Performance is not solely an engineering problem. Asset design must participate.

Artists and asset-generation tools must consider:

- triangle count;
- draw calls;
- material count;
- texture memory;
- transparency/overdraw;
- shadow-casting cost;
- light count;
- VFX particle count;
- LOD behaviour;
- streaming footprint;
- repeated asset reuse;
- actor count in MMO scenes.

The project should prefer many well-composed economical assets over a small number of extremely expensive assets that constrain world density or player count.

A scene that only performs well because almost nothing else is present is not a valid performance test for an MMORPG.

---

## 22. Biome Construction

A biome is not defined solely by terrain texture colour.

Each biome should be built from a coordinated visual kit containing some combination of:

- terrain material set;
- cliff/rock set;
- tree set;
- undergrowth set;
- environmental props;
- water treatment;
- fog/atmosphere profile;
- weather behaviour;
- characteristic VFX;
- architectural language where inhabited;
- colour-grade guidance.

The same mid-poly principles apply to every biome, but the kit composition changes.

Examples:

### Temperate Forest

- spaced conifers/deciduous trees;
- clustered ferns and shrubs;
- simple rock shelves;
- streams and bridges;
- sun shafts/haze where appropriate;
- muted green/brown materials.

### Undead Marsh

- fewer healthy trees;
- dead trunks and corrupted silhouettes;
- reflective shallow water;
- reeds in clusters;
- thick low fog;
- cool ambient palette;
- selective sickly or magical accents;
- distant warm lights for navigation contrast.

### Volcanic Region

- broad dark terrain and basalt forms;
- relatively simple rock geometry;
- emissive lava as a major light source;
- smoke, ash and ember VFX;
- dark clouds illuminated from below;
- strong orange/black value contrast;
- sparse vegetation and props.

The volcanic environment must not compensate for lack of art direction by using extremely dense rock meshes. Its richness should come from silhouette, lava, smoke, cloud illumination and atmosphere.

---

## 23. Dungeon and Interior Relationship to the World

Dungeons should feel physically and visually connected to the world rather than like unrelated standalone levels.

Where possible:

- exterior geology should continue into cave/dungeon entrances;
- architectural kits should share cultural materials and motifs with nearby settlements;
- lighting transition should be gradual and believable;
- fog and atmosphere may change but should not abruptly abandon biome identity;
- cave and tunnel geometry should use the same terrain-kit principles as exterior overhangs and cliffs.

Interiors may spend more local detail because view distance is lower, but they remain subject to the same material and silhouette discipline.

---

## 24. Explicit Non-Goals

The following are not the Ninth Age visual target:

- photorealism;
- scan/photogrammetry-driven environment art as the default;
- visibly faceted low-poly art;
- cartoon proportions as the general world style;
- dense microgeometry across terrain and rocks;
- unique high-resolution materials for every prop;
- vegetation carpets that harm traversal or performance;
- ultra-dense forests solely to make a scene look expensive;
- architecture built entirely as bespoke one-off meshes;
- AI-generated topology accepted without production cleanup;
- automatic rigs accepted without deformation validation;
- post-processing used to hide weak assets;
- cinematic lighting that makes gameplay unreadable;
- excessive particles or bloom in group combat;
- fidelity decisions that make the seamless MMO world impractical to render.

---

## 25. Scene Acceptance Criteria

A representative Ninth Age scene is visually on-target when all of the following are true:

- the scene reads as grounded fantasy rather than cartoon low-poly;
- the underlying assets are visibly economical when inspected, but do not look crude during play;
- large terrain forms remain readable;
- cliffs and other terrain-kit meshes do not look pasted onto the heightfield;
- foliage density communicates the biome without becoming visual clutter;
- architecture reads clearly at gameplay distance;
- actor silhouettes remain readable against the environment;
- materials remain believable but restrained;
- fog and atmosphere establish depth;
- lighting materially improves the scene without being required to hide asset problems;
- the scene works under multiple times of day;
- VFX reinforce the environment without dominating it;
- the Radiant Slate UI remains legible over the scene;
- LOD transitions do not create distracting shape changes;
- performance remains appropriate for a large-world MMORPG context.

---

## 26. Asset Acceptance Checklist

An individual asset is not ready for production unless it satisfies the applicable points below:

- scale is correct;
- pivot is appropriate;
- silhouette is readable;
- geometry is no denser than its role requires;
- no unnecessary internal or hidden geometry remains;
- material count is justified;
- surface detail is not excessively noisy;
- normals/tangents produce the intended shading;
- UVs or world-space mapping are appropriate;
- terrain-contact blending is supported where required;
- LODs exist where required;
- collision is appropriate and economical;
- transparency/overdraw is controlled;