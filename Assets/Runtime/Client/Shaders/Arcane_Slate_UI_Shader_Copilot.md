# Arcane Slate UI Shader Copilot Instructions

## Goal
Create a reusable Unity UI shader stack for the **Arcane Slate** interface style used in this MMORPG project.

The visual target is:
- dark slate materials
- restrained magical accents
- thin engineered borders
- soft gradients
- subtle inner highlights
- controlled hover and selected states
- high readability
- minimal ornament

This is **not** a Warcraft-style carved bronze UI. Avoid heavy bevels, ornate corners, parchment treatment, thick metallic frames, or noisy fantasy textures.

---

## Style Summary

Arcane Slate is a **modern fantasy UI** built around cool, neutral panel surfaces with selective accent colour.

### Visual principles
- Base surfaces should feel calm, premium, and engineered.
- Most UI should remain neutral and dark.
- Fantasy identity should come from accent colour, iconography, and subtle magical light, not decorative frame art.
- Borders should be thin and crisp.
- Gradients should be soft and low contrast.
- Hover, pressed, and selected states should be readable but restrained.
- UI elements should feel slightly recessed or layered, never chunky or overly embossed.

### Primary palette
- Background: `#0F141A`
- Panel: `#1C232C`
- Panel Alt: `#242C38`
- Border: `#3A4656`
- Border Soft: `#4A5A70`
- Text Primary: `#D6DEE8`
- Text Secondary: `#A3ADB8`
- Accent Violet: `#6E7BD9`
- Accent Teal: `#4FA3A1`
- Accent Gold: `#B59A6A`
- Danger: `#BA5D5B`
- Success: `#5D9A78`

### Material feeling
Think:
- matte slate
- cool steel trim
- soft magical edge light
- low-noise premium game UI

Do **not** think:
- carved wood
- ornate gold trim
- old parchment
- thick bevel stacks
- exaggerated glow

---

## Technical Goal
Build a shader system for Unity UI that does most of the material work for:
- windows and subpanels
- inventory slots
- buttons
- tabs and accent lines
- optional input fields and tooltips

The shader stack should reduce reliance on many unique baked textures.

Use shaders for:
- body gradients
- border tint
- inner shadow
- inner top highlight
- subtle recessed depth
- hover state lift
- selected accent line or tint
- pressed state inset feel
- optional rarity or state strip

Do **not** use shaders to replace:
- icon illustration
- unique frame silhouettes
- bespoke ornamental art

---

## Deliverables
Create the following reusable shader families.

### 1. PanelSurface shader
Use for:
- inventory windows
- character windows
- spellbook
- quest log
- settings panels
- tooltips
- subpanels

Must support:
- top-to-bottom body gradient
- thin border
- border tint override
- subtle inner top highlight
- subtle inner shadow
- optional accent tint
- optional corner darkening or vignette
- rounded-rect mask support if needed

### 2. SlotSurface shader
Use for:
- inventory slots
- equipment slots
- hotbar cells
- crafting slots
- bank slots

Must support:
- recessed fill
- thin border
- slightly darker lower half
- faint inner highlight
- hover state
- selected state
- pressed state
- optional rarity edge tint
- disabled/desaturated state

### 3. ButtonSurface shader
Use for:
- menu buttons
- action buttons
- modal buttons
- filter buttons
- tab-like controls

Must support:
- clean panel fill
- thin border
- hover lift
- pressed inset
- optional primary/secondary/utility accent tint
- selected underline or active edge

### 4. AccentLine shader
Use for:
- selected tab underlines
- active separators
- magical highlight strips
- subtle focus accents

Must support:
- solid or gradient line
- soft edge fade
- colour override
- optional very restrained pulse

---

## Unity Context
Target Unity UI rendering for regular game UI.
Assume UGUI unless there is a clear reason to do otherwise.

### Important constraints
- Keep batching considerations in mind.
- Avoid creating a unique material instance for every element unless necessary.
- Prefer shared materials with exposed properties and predictable variants.
- Keep shader feature count reasonable.
- Prioritize maintainability over shader cleverness.

### Preferred implementation approach
- Use simple sprite geometry or a white rounded-rect base.
- Let the shader provide most of the material response.
- Keep masks and outlines crisp.
- Make sure the shader works well with standard Unity `Image` and UI masking workflows.

If using URP-specific UI features, keep the implementation practical and compatible with standard UI rendering.

---

## Shader Behaviour Specification

## PanelSurface shader specification

### Visual behaviour
- Main panel body should use a subtle vertical gradient.
- Top of panel slightly brighter than bottom.
- Border should be thin and cool.
- Very small inner top highlight to imply a polished surface.
- Inner shadow should add depth without looking embossed.
- Selected or special panels may take a very light violet or teal tint.

### Default values
- Fill Top: `#202833`
- Fill Bottom: `#171E27`
- Border Color: `#3A4656`
- Border Width: visually around 1 px
- Top Highlight: very low strength
- Inner Shadow: low strength
- Accent Amount: 0 by default

### Exposed properties
- `_FillTop`
- `_FillBottom`
- `_BorderColor`
- `_BorderWidth`
- `_CornerRadius`
- `_TopHighlightColor`
- `_TopHighlightStrength`
- `_InnerShadowColor`
- `_InnerShadowStrength`
- `_AccentColor`
- `_AccentAmount`
- `_VignetteStrength`
- `_Opacity`

### Notes
- The panel should never look glossy.
- The panel should never look carved or metallic.
- Highlights must be restrained.

---

## SlotSurface shader specification

### Visual behaviour
- Slot should feel like a recessed engineered cell.
- Border should be present but not bright.
- Interior should be slightly layered, not flat black.
- Lower half can be a little darker to suggest depth.
- Hover should brighten fill slightly and increase border clarity.
- Selected should use a controlled violet border or accent line.
- Pressed should darken and flatten slightly.

### Default values
- Fill Top: `#212A35`
- Fill Bottom: `#18202A`
- Border Color: `#445064`
- Hover Border: slightly brighter steel-blue
- Selected Accent: `#6E7BD9`

### Exposed properties
- `_FillTop`
- `_FillBottom`
- `_BorderColor`
- `_BorderWidth`
- `_CornerRadius`
- `_InnerShadowStrength`
- `_TopHighlightStrength`
- `_HoverAmount`
- `_PressedAmount`
- `_SelectedAmount`
- `_SelectedColor`
- `_RarityColor`
- `_RarityAmount`
- `_DisabledAmount`

### State rules
- Default: neutral slate
- Hover: +small value lift, slightly brighter border
- Pressed: darker fill, reduced highlight
- Selected: thin violet border or side/accent strip
- Disabled: reduced saturation and contrast

### Notes
- Do not add large bloom or glows.
- Do not use thick outlines.
- Do not use noisy texture overlays.

---

## ButtonSurface shader specification

### Visual behaviour
- Buttons should feel integrated with the panel system.
- They should not look like separate fantasy props.
- Primary buttons may carry a light violet tint.
- Secondary buttons may carry a teal tint.
- Utility buttons may carry a muted gold tint.

### Exposed properties
- `_FillTop`
- `_FillBottom`
- `_BorderColor`
- `_BorderWidth`
- `_HoverAmount`
- `_PressedAmount`
- `_AccentColor`
- `_AccentAmount`
- `_SelectedAmount`
- `_CornerRadius`

### Notes
- Button states should be readable but subtle.
- Avoid over-glowing active states.
- Pressed should feel mechanically inset, not animated magic.

---

## AccentLine shader specification

### Visual behaviour
- Used for selected tabs, key separators, active edges, or small magical emphasis.
- Should be thin and sharp.
- Can fade softly outward.
- Optional pulse must be extremely restrained.

### Exposed properties
- `_MainColor`
- `_SecondaryColor`
- `_Width`
- `_Softness`
- `_PulseAmount`
- `_PulseSpeed`
- `_Opacity`

### Notes
- This shader is for accents only.
- It must never overpower the interface.

---

## Interaction Design Rules
Apply these visual rules consistently across all shader families.

### Default
- Neutral slate values
- Thin border
- Low contrast highlight

### Hover
- Slight brightness lift
- Slight border clarity increase
- No big glow

### Pressed
- Darker fill
- Reduced top highlight
- Slightly stronger inner shadow

### Selected
- Violet accent preferred for focus and selected states
- Use border, underline, or narrow side strip
- Do not flood the whole control with accent colour

### Disabled
- Lower contrast
- Reduced saturation
- Keep readable shape language

---

## What the shaders should avoid
Do not produce visuals that feel like:
- World of Warcraft bronze panels
- ornate carved fantasy trim
- embossed medieval metalwork
- sci-fi holographic UI
- neon cyber glow
- noisy grunge overlays
- thick high-contrast bevels
- over-textured surfaces

This project wants **minimal arcane**, not baroque fantasy and not sci-fi futurism.

---

## Suggested Architecture
If practical, separate shared logic and style logic cleanly.

### Recommended structure
- one base shader or shared include for rounded-rect, border, and gradient logic
- separate materials or variants for panel, slot, button, and accent line
- avoid duplicated code where possible

### Good implementation ideas
- SDF or analytic rounded-rect rendering for crisp borders
- border and fill derived procedurally from UVs
- optional mask support for standard Unity UI workflows
- material properties tuned from inspector

### Important
Keep the implementation understandable.
Do not create an overly abstract shader architecture that is hard to debug.

---

## Example Usage Targets
The final system should be suitable for:
- Inventory window
- Character panel
- Equipment slots
- Action bar cells
- Tooltip panels
- Search fields
- Tabs
- Buttons
- Progress bars using compatible fill logic

Inventory slots in particular should feel like:
- dark slate recesses
- thin cool borders
- very slight polish
- high icon contrast
- tidy and engineered

---

## Quality Bar
The final result should look:
- premium
- calm
- crisp
- modern fantasy
- highly readable
- restrained

It should not look:
- noisy
- flashy
- chunky
- old-fashioned
- derivative of Warcraft UI

---

## Request to Copilot
When generating code:
- prefer full working shader code, not pseudocode
- include comments only where useful
- keep naming clean and consistent
- expose only meaningful properties
- avoid unused feature clutter
- ensure the shader is practical for production iteration

When generating setup instructions:
- explain which Unity components to pair with each shader
- explain how to assign materials in UGUI
- explain sensible default values
- explain how hover/selected states should be driven from C# if needed

When generating C# support code:
- provide full explicit code
- do not omit sections with placeholders
- include exact property names and material updates

---

## First Tasks
Generate in this order:

1. `ArcaneSlatePanelSurface.shader`
2. `ArcaneSlateSlotSurface.shader`
3. `ArcaneSlateButtonSurface.shader`
4. `ArcaneSlateAccentLine.shader`
5. A small Unity C# helper for driving hover, pressed, and selected states on UI Images using these materials

After that, generate a sample Unity setup for:
- Inventory window panel
- Inventory slot prefab
- Search field styling
- Basic hover and selected behaviour

