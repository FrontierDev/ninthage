# Ninth Age — Spell Icon Art Direction PDD

**Status:** Approved visual direction; individual spell icons require art review  
**Project:** Ninth Age  
**Theme:** **Radiant Slate** (not Arcane Slate)  
**Last updated:** 2026-10-16  
**Scope:** Art standards, composition, production workflow, and acceptance criteria for class spells, talent-granted abilities and reusable spell/aura imagery

## 1. Purpose and authority

This document defines the **spell-icon artwork** standard. It is subordinate to the [UI and UX PDD](UI-and-UX-PDD.md) for slot dimensions, presentation and UI theme, and complements the [Abilities and Talents PDD](Abilities-and-Talents-PDD.md) and [Class Design PDD](Class-Design-PDD.md) for mechanics-first authoring. The [Graphical Approach PDD](Graphical-Approach-PDD.md) governs the broader world art direction.

**Radiant Slate is the authoritative overall Ninth Age UI theme.** Use the user's approved **Radiant Slate MMO UI concept sheet** (especially its **Icons** and **Action Hotbar** examples) and the [Radiant Slate UI Style Guide](../../Assets/Runtime/Client/Shaders/Radiant%20Slate.html) as primary visual references. The legacy [ArcaneSlate shader instructions](../../Assets/Runtime/Client/Shaders/Arcane_Slate_UI_Shader_Copilot.md) describe shader tooling, **not** a competing art direction. The `ArcaneSlate` shader code namespace can remain for compatibility.

Radiant Slate's *interface chrome* is neutral, dark and deliberately restrained. Individual **painted spell illustrations** can be richer, but their silhouettes, values, accent colours and detail density must fit the small action-bar setting.

## 2. Locked icon art requirements

1. **Complete square painted artwork:** each spell icon is a cohesive, edge-to-edge **opaque** painting. All four corners and the entire background are intentionally authored. Final alpha must be fully opaque; **no transparent background**.
2. **Painted background is part of the art:** use a contextual, relatively dark background integrated with the foreground via lighting, shapes and restrained atmosphere. Avoid an isolated emblem floating on a generic flat fill.
3. **No baked frame, beveled slot border, text, keybind, rank badge or tooltip:** the Radiant Slate UI supplies slot framing and overlays. Do not bake UI decoration into the painting.
4. **One dominant subject or action:** the spell's meaning should be recognisable from its principal shape without inspecting particles or reading a label.
5. **Bold, distinct silhouettes:** neighbouring abilities must differ in *shape and composition*, not just in colour. A projectile, melee impact, protective shield, cancelling glyph, planar tear and area explosion must not share the same swirling-circle motif.
6. **Controlled palette and light:** each icon should usually use two or three dominant hues/value families with **one principal focal highlight**. Colours support the spell's fantasy and mechanical meaning; magical-school assignments do not force a colour formula. Background values remain supportive. **An effect filling most of the canvas with maximum brightness is not acceptable.**
7. **Simplified, painterly fantasy rendering:** allow visible hand-painted form, deliberate broad strokes, compact highlights and moderate material cues. Do not substitute splash art, photorealistic scenery, rendered concept art, dense simulation effects or isolated pictograms. **Match the rendering density, brushwork and contrast of the class's approved visual anchors.**
8. **Avoid interchangeable magical noise:** overused concentric vortices, floating crystals, bright cyan/violet ribbons, excess fragments, excessive bloom and uniformly neon rendering are not an acceptable default visual grammar.
9. **Visual impact belongs at working size:** the icon should still read at **32×32**, the standard **40×40** action-bar slot (100% UI scale), **48×48** and **64×64**. Reviewing only the full-resolution image is insufficient.
10. **No other-purpose content:** do not introduce scenic architecture, unrelated characters, generic portals, random runes or effects merely to make an icon elaborate. Objects must reinforce the intended ability action.

This document describes **spell icons**; item icons, navigation glyphs and talent-passive iconography can adopt tailored rules only where separately authorised.

## 3. Readability and composition at 40 px

- Primary subject should dominate the image and remain identifiable as a distinct value mass after downscaling.
- Use a meaningful foreground/background separation and a recognisable negative-space structure **inside the opaque painting**; negative space is a deliberately dark painted area, **not transparency**.
- Choose a unique *direction* when applicable: lateral sweep, diagonal projectile, upward wind, inward spatial tear, radiating wave, interposed barrier.
- Compose for square cropping from the outset; edges may be painted, but critical parts of the subject should not disappear under thin slot framing or UI overlays.
- Reserve fine particles and micro-runes as supporting detail that can vanish harmlessly at action-bar size.
- Assess each new icon alongside existing class icons. Independent beauty does not compensate for an illegible action bar.

### 3.1 Mandatory visual continuity with approved icons

**New spell icons must pass two independent reviews: (a) readable and mechanically appropriate on their own, and (b) stylistically consistent with their class's already approved icons.** A coherent standalone image can still be rejected for stylistic drift.

- **Reference the actual approved icon images in each new art brief.** General descriptions of "fantasy" or "Radiant Slate" are insufficient substitutes when class references exist. If reference images cannot be supplied or viewed, treat the output as exploratory, **not approved production art**.
- **Preserve the class's shared visual treatment:** hand-painted brushwork, shape simplification, background depth, dominant light placement, contrast, saturation and relative level of detail. Change the focal silhouette and spell action, not the entire rendering style.
- **Compare side by side at real UI size:** at minimum, preview the candidate immediately beside two approved icons from its class at **40×40 px**, as well as at 32, 48 and 64 px. Check the standalone art at full size separately.
- **One dominant action, one dominant light source:** a readable silhouette must survive loss of particles, secondary light streaks, micro-runes and background texture when downscaled. If a detailed humanoid is necessary, simplify it to a recognisable figure/action; the character's costume and anatomy must not become the primary subject.
- **Do not solve readability by adding more glow or effects.** Simplify the image, darken subordinate areas, remove competing centres of interest and increase shape separation.
- **Do not mistake palette similarity for style continuity.** Icons that are both purple can still be from different art styles due to line treatment, lighting, composition, anatomical detail or effects density.
- **Do not mistake deliberate variation for style drift.** Different spells should retain unique silhouettes and meaningful colours; consistency refers to the *rendering language*, not repeating a shape, pose or exact hue.

### 3.2 Sorcerer / Voidbrand continuity rule

The accepted **Chaos Bolt** and **Brand** icons are the **primary painterly style anchors** for further Voidbrand work. Chaos Bolt is a shared Sorcerer spell, but its artwork is part of the family used to judge Voidbrand. The original image files from the approved conversation must be provided as generation references when available; they are **not claimed to be committed assets in this repository**.

- **Core palette:** near-black/plum shadows, deep violet and restrained magenta or crimson-magenta accents. Light lavender/white-violet may define a *small* focal highlight. Keep **bright blue/cyan secondary or absent**, rather than letting it dominate the icon.
- **Painterly treatment:** bold directional painted strokes, dark values around the bright action, organic shaped energy, moderate edge detail, controlled texture. Avoid glossy sci-fi portal rendering, sharp all-over electric neon, polished character splash art or large geometric/rune ornaments.
- **Composition:** a tight, readable physical/magical action that fills the icon. Preserve dark painted negative space and a clear forward movement or interaction; avoid complex full-body action scenes, multiple overlapping subjects, long energy trails and large secondary portals competing with the subject.
- **Light:** no uniform perimeter glow, excessive pale-magenta bloom or multiple independent luminous centres. The main action must remain recognisable when flattened to 40 px.
- **Spell distinction:** **Brand** is a melee impact, **Phase Shift** is genuine invisibility, **Control Rift: Riftwalk** is movement into an *already active* rift, and **Superposition** is a still-visible stealth-qualified state. Do not illustrate all four as the same violet tear/swirl; use different subjects and silhouette dynamics.

**Control Rift: Riftwalk correction:** depict a **single compact, dark moving figure visibly entering a narrow, established dimensional tear**, with the tear serving as the destination rather than dominating the entire image. The gesture, silhouette and violet/magenta brushwork should be legible beside Chaos Bolt and Brand. **Avoid the previous direction of a detailed humanoid action scene with a large blue-lit portal and competing pink energy streams.** That treatment is **not an approved style anchor** and must be revised before asset approval.

This palette guidance is specific to **Voidbrand** and does not require Fluxweaver (ice/protection/healing) or Cataclyst (fire/high-Instability damage) to use violet/magenta palettes.

## 4. Relationship to Radiant Slate UI

Radiant Slate's layout/panel system is **not** a requirement to draw the whole icon in grey. Rather, painted icon backgrounds should work with neutral lifted slate surfaces and thin slot borders. Keep the artwork self-contained; normal hover, selected, disabled, cooldown and range feedback belong to UI shaders/overlays.

Reference colour tokens from the supplied Radiant Slate guide include `#1E222A` (background), `#2A303A` (panel), `#353E4A` (elevated panel), `#4B5566` (border), `#E6EAF2` (primary text) and `#E0BF72` (gold accent). These govern **UI chrome**. Spell-art palettes can vary much more widely and should not all be violet/blue simply because the interface was once called Arcane Slate.

## 5. Artwork workflow and approval gates

1. **Mechanics first:** confirm what the spell does, its role, conditions, level and school where known. Do not invent a mechanic to justify a visual.
2. **Visual brief before generation:** specify one dominant subject/action, a distinct silhouette, background treatment, light direction, two to three colour families and explicit neighbouring icons it must differ from. **Include the approved class icon images as mandatory style anchors** and state what visual qualities must be preserved and what will intentionally differ.
3. **Create one icon at a time or a deliberately comparable test set.** Do not mass-generate a full class sheet until the visual benchmark is approved.
4. **Review the unframed image** at full size, then composite it into a Radiant Slate action-bar slot using the UI-owned frame **beside at least two approved class icons**.
5. **Review at 32, 40, 48 and 64 px** against a representative action bar (including disabled/cooldown overlays). Verify readability in bright and dark gameplay backgrounds.
6. **Compare the class set for silhouette/colour diversity AND painterly continuity.** Separately evaluate composition, brushwork, subject complexity, shadow depth, highlight scale and palette balance against the approved anchors. Reject either repeated generic motifs **or** style drift.
7. **Approve only after small-scale review.** Revision may simplify or redesign the concept; adding more effects is not the default fix.

**Approval is not automatic:** image-generation output is a *candidate*. A renderer can repeatedly produce unwanted motifs even after textual correction; do not accept or use the newest image merely because another prompt was attempted. If a generator repeatedly violates the approved style, revise the art brief or production method instead of continuing the same drifting visual treatment.

**Recommended authored working size:** 512×512 px or larger square master (source resolution is not final presentation size). Export a standard **opaque RGB/RGBA image with fully opaque alpha**; the production asset pipeline can determine final compression and import settings. Do not promise that a 1024 px generated scene is usable merely because it is high-resolution.

### Reference benchmark

Validate four visually contrasting Sorcerer spells first: **Chaos Bolt** (projectile), **Crystallise** (ice-themed protection), **Winds of Wisdom** (party-wide Intelligence/wind) and **Dimensional Rift** (spatial tear). Approval of these is a prerequisite to a bulk Sorcerer icon pass.

## 6. Sorcerer concept vocabulary (provisional per-icon art briefs)

The primary subjects below are icon design directions only, not new gameplay effects or confirmed final colours. Spell mechanics remain defined in the [Sorcerer PDD](class-design/Sorcerer-PDD.md).

| Spell | Primary readable shape / action | Painted backdrop and differentiation |
|---|---|---|
| **Chaos Bolt** | Single sharp diagonal unstable projectile | Deep plum and dark ash; clear forward motion, not a broad splash |
| **Brand** | Melee weapon strike leaving a branded mark | Dark magenta/red-umber close-contact impact; not a second ranged bolt |
| **Crystallise** | **Protective formation of ice** enclosing/interposing before a target | Midnight blue and cold frost shadows; shield silhouette, not a summoned crystal or ice explosion |
| **Winds of Wisdom** | Distinct flowing wind currents interacting with a simple intellect/clarity symbol | Deep teal/blue-grey and pale mist; supportive, calm, not a cyclone/attack |
| **Nullify** | Intact enemy casting glyph being abruptly suppressed/cut off | Deep blue/charcoal with a restrained contrasting cancellation stroke; **not** a shattering explosion |
| **Dimensional Rift** | **Narrow vertical tear in space** with displaced opposing edges | Ink-black/indigo planar void; not a round crystalline wormhole or a vista of distant worlds |
| **Control Rift: Nova** | Collapsing tear releasing an outward shockwave | Dark indigo with one precise bright radial burst; recognisably different from stationary Rift |
| **Molten Barrage** | Multiple directional streaks of molten fire | Ember-dark burnt red/orange, rapid sequence not one giant fireball |
| **Locus** | A single ally-mark / anchored focus of restoration | Turquoise-green/soft gold on deep teal; clear recipient emphasis |
| **Phase Shift** | Caster silhouette partly slipping out of visibility | **Voidbrand palette:** plum-black/violet with controlled magenta edges; simplified partial silhouette, not a blue-glowing portal or elaborate full-body casting scene |
| **Control Rift: Riftwalk** | One compact figure entering an existing narrow rift | **Voidbrand palette:** near-black/plum, deep violet, restrained magenta; tight directional movement, **minimal cyan**; no oversized portal, multi-layered character rendering or competing ribbons |
| **Superposition** | Several aligned/offset outlines of **a visible** caster | **Voidbrand palette:** dark plum/violet with restrained magenta; visible figure/afterimages must not imply true invisibility; render in the same simplified painterly treatment as Brand |
| **Dissipate** | Instability energy released into gentle outward healing pulses | Quiet teal/sea-green movement, controlled and restorative |
| **Rewind** | A single reversing temporal path restoring the subject | Blue-grey with muted gold direction accent; avoid dense decorative clockwork |
| **Pyroclasm** | Heavy descending/compressed mass of destructive flame | Black-red/bright molten orange, distinct from rapid multi-shot Molten Barrage |
| **Surge** | Focused magical discharge immediately exhausting its source | Sharp white-hot central release against hot red/purple-black backdrop; not another rift explosion |

**A palette or silhouette in this table can be revised after side-by-side testing**, but the core requirement—distinctive, readable, coherent painted art—is not provisional.

## 7. Rejection criteria and design validation

Reject a spell icon when any of the following are true:

- The artwork has a transparent or incompletely painted background.
- It includes a baked frame/border, lettering, rank marker or action-bar UI.
- The central subject cannot be understood at 40×40 px without the spell name.
- Multiple icons have the same dominant swirl, shard halo, purple/cyan palette and focal point.
- An icon deviates substantially from approved class anchors in **brushwork, composition complexity, highlight concentration, shadow values or colour balance**, even if its mechanics and broad colours are correct.
- A Voidbrand icon is overwhelmed by electric blue/cyan, high-luminance magenta, detailed humanoid staging, a large sci-fi portal or multiple competing energy streams instead of the restrained Chaos Bolt/Brand style.
- Atmospheric/detail effects overpower the spell's function.
- An ice **shield** reads as an offensive ice projectile, or an **interrupt** reads as an explosion.
- The artwork is an oversized splash painting or scenic portal rather than compact painted fantasy iconography.
- A beautiful full-size result becomes noise when slotted into the actual Radiant Slate HUD.

The approval artefact for each icon is a side-by-side **real-scale action-bar preview (candidate beside at least two approved class icons) plus full-resolution source and documented reference anchors**, not just a high-resolution standalone render. Approval must explicitly pass **both** independent tests: **spell readability** and **class-style continuity**.
