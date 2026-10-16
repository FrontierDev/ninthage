# Ninth Age — Spell Icon Art Direction PDD

**Status:** Approved visual direction; individual spell icons require art review  
**Project:** Ninth Age  
**Theme:** **Radiant Slate** (not Arcane Slate)  
**Last updated:** 2026-10-15  
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
6. **Controlled palette and light:** each icon should usually use two or three dominant hues/value families with one legible focal highlight. Colours support the spell's fantasy and mechanical meaning; magical-school assignments do not force a colour formula. Background values remain supportive.
7. **Simplified, painterly fantasy rendering:** allow visible hand-painted form, deliberate broad strokes, compact highlights and moderate material cues. Do not substitute splash art, photorealistic scenery, rendered concept art, dense simulation effects or isolated pictograms.
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

## 4. Relationship to Radiant Slate UI

Radiant Slate's layout/panel system is **not** a requirement to draw the whole icon in grey. Rather, painted icon backgrounds should work with neutral lifted slate surfaces and thin slot borders. Keep the artwork self-contained; normal hover, selected, disabled, cooldown and range feedback belong to UI shaders/overlays.

Reference colour tokens from the supplied Radiant Slate guide include `#1E222A` (background), `#2A303A` (panel), `#353E4A` (elevated panel), `#4B5566` (border), `#E6EAF2` (primary text) and `#E0BF72` (gold accent). These govern **UI chrome**. Spell-art palettes can vary much more widely and should not all be violet/blue simply because the interface was once called Arcane Slate.

## 5. Artwork workflow and approval gates

1. **Mechanics first:** confirm what the spell does, its role, conditions, level and school where known. Do not invent a mechanic to justify a visual.
2. **Visual brief before generation:** specify one dominant subject/action, a distinct silhouette, background treatment, light direction, two to three colour families and explicit neighbouring icons it must differ from.
3. **Create one icon at a time or a deliberately comparable test set.** Do not mass-generate a full class sheet until the visual benchmark is approved.
4. **Review the unframed image** at full size, then composite it into a Radiant Slate action-bar slot using the UI-owned frame.
5. **Review at 32, 40, 48 and 64 px** against a representative action bar (including disabled/cooldown overlays). Verify readability in bright and dark gameplay backgrounds.
6. **Compare the class set for silhouette/colour diversity.** Revise or reject any repeated generic motif.
7. **Approve only after small-scale review.** Revision may simplify or redesign the concept; adding more effects is not the default fix.

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
| **Phase Shift** | Caster silhouette partly slipping out of visibility | Charcoal/violet silhouette with missing/displaced edge; unlike Rewind/time spiral |
| **Control Rift: Riftwalk** | Distinct small figure stepping through a tear | Dark indigo spatial cut; recognisable travel, not static Dimensional Rift |
| **Superposition** | Several aligned/offset outlines of **a visible** caster | Low-saturation lavender/slate; not invisible/dissolving Phase Shift |
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
- Atmospheric/detail effects overpower the spell's function.
- An ice **shield** reads as an offensive ice projectile, or an **interrupt** reads as an explosion.
- The artwork is an oversized splash painting or scenic portal rather than compact painted fantasy iconography.
- A beautiful full-size result becomes noise when slotted into the actual Radiant Slate HUD.

The approval artefact for each icon is a side-by-side **real-scale action-bar preview plus full-resolution source**, not just a high-resolution standalone render.
