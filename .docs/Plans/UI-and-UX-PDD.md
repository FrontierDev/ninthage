# Ninth Age — UI and UX Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Radiant Slate visual system, UI design tokens, shader-driven components, HUD, action bars, unit frames, nameplates, inventory, character sheet, talents, quest/reputation presentation, maps, tooltips, combat feedback, contextual tips, window behaviour, interaction conventions, scaling and UI-state persistence  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the player-facing UI/UX system for **Ninth Age**.

It is authoritative for:

- the **Radiant Slate** UI design language;
- typography;
- text sizes;
- colour tokens;
- icon sizes;
- control sizes;
- spacing;
- panel/window surface treatment;
- standard interaction states;
- shader-driven UI presentation;
- HUD module behaviour;
- HUD edit/scaling behaviour;
- action-bar presentation;
- player/target/group frame presentation;
- nameplates;
- aura presentation;
- cast bars;
- floating combat text;
- inventory and equipment interaction;
- tooltips and item comparison presentation;
- character/talent/quest/reputation UI composition;
- minimap/world-map presentation;
- contextual non-blocking tips;
- window stack and Escape behaviour;
- drag/drop conventions;
- error/validation feedback;
- persistence of local UI preferences.

This document does not own:

- gameplay rules shown by the UI;
- combat resolution — [Combat System PDD](Combat-System-PDD.md);
- item mechanics — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- talent/ability mechanics — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- group/raid mechanics — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- quest/dialogue mechanics — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- reputation mechanics — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- character-creation rules — [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md);
- accessibility-specific requirements that require their own dedicated options — [Accessibility and Input PDD](Accessibility-and-Input-PDD.md);
- chat/channel/social semantics — [Communication Systems PDD](Communication-Systems-PDD.md);
- world visual direction — [Graphical Approach PDD](../Graphical-Approach-PDD.md).

Where current UI implementation conflicts with this PDD, this document defines intended behaviour.

---

## 2. Central Design Principle

The UI must be:

> **consistent, high-fidelity, readable and recognisably Radiant Slate across every game system.**

High fidelity does not mean excessive decoration.

It means:

- coherent material response;
- precise spacing;
- consistent typography;
- consistent iconography;
- strong hierarchy;
- predictable interaction;
- restrained animation;
- polished state transitions;
- reliable information density.

No subsystem should create its own unrelated visual language.

---

## 3. Radiant Slate

**Radiant Slate** is the authoritative Ninth Age UI design system.

Its character is:

- dark slate and charcoal surfaces;
- restrained aged-metal structure;
- narrow, precise borders;
- subtle gradients and inner shadows;
- limited vignette;
- pale high-contrast text;
- restrained radiant-gold selection/focus treatment;
- semantic colour only where colour communicates meaning;
- high-fidelity shader treatment rather than large amounts of baked decorative frame art.

Radiant Slate should feel mature and materially grounded rather than:

- cartoon;
- neon;
- sci-fi;
- heavily ornamental;
- skeuomorphic parchment;
- generic flat mobile UI.

---

## 4. Relationship to the Graphical Direction

The Graphical Approach PDD requires a grounded, mature fantasy world with high-fidelity presentation applied to economical authored content.

Radiant Slate follows the same philosophy.

The UI's authored structure should remain economical:

- simple geometric panels;
- reusable icons;
- reusable typography;
- standard component layouts.

Perceived quality comes from:

- shader-driven surface response;
- typography;
- hierarchy;
- interaction states;
- animation;
- spacing;
- carefully constrained colour.

The interface must remain readable against changing:

- daylight;
- darkness;
- interiors;
- weather;
- spell effects;
- combat VFX.

---

## 5. Existing MMO Reference Principles

Ninth Age should learn from established MMO interfaces without copying their visual identity.

Useful reference principles include:

### 5.1 World of Warcraft

Useful lessons:

- central combat information must remain immediately readable;
- action bars can remain traditional and information-dense without becoming visually dominant;
- HUD modules benefit from player-positioning/edit support;
- modernisation should reduce unnecessary frame clutter rather than remove important combat information.

### 5.2 Final Fantasy XIV

Useful lessons:

- HUD elements can be moved and discretely scaled while retaining authored internal proportions;
- individual status/HUD modules can expose visibility/layout options;
- configurability does not require every RPG window to become arbitrarily resizable.

### 5.3 Guild Wars 2

Useful lessons:

- a combat HUD benefits from strong authored visual grouping;
- related combat information should read as one coherent composition.

### 5.4 New World

Useful lessons:

- restraint and screen-space discipline are valuable;
- persistent chrome should not exist merely because space is available.

Ninth Age requires more persistent combat information than New World and should not adopt an excessively sparse action-RPG HUD.

These games are reference points only.

Radiant Slate remains the project's own design language.

---

## 6. Reference Resolution and Coordinate System

Radiant Slate token values are authored at:

**1920 × 1080, UI scale 100%.**

This is the design reference, not a fixed runtime resolution.

UI layout must scale coherently to other supported resolutions.

Standard spacing and component sizes are logical UI pixels at the reference scale.

---

## 7. Base Spacing Grid

Radiant Slate uses a:

**4 px base spacing unit.**

Standard spacing tokens are:

| Token | Size |
|---|---:|
| Space-1 | 4 px |
| Space-2 | 8 px |
| Space-3 | 12 px |
| Space-4 | 16 px |
| Space-5 | 20 px |
| Space-6 | 24 px |
| Space-8 | 32 px |

Ordinary UI layout should use these tokens.

Avoid arbitrary values such as:

- 7 px;
- 13 px;
- 19 px;

unless a geometric effect genuinely requires them.

---

## 8. Structural Dimensions

At 1920×1080 / 100%:

| Element | Baseline |
|---|---:|
| Outer border | 2 px |
| Fine inner border | 1 px |
| Divider | 1 px |
| Standard corner radius | 4 px |
| Standard panel padding | 16 px |
| Compact panel padding | 12 px |
| Standard gutter | 8 px |
| Section gutter | 12 px |
| Major section gutter | 16 px |

The 2 px outer / 1 px inner structure is the normal Radiant Slate hierarchy.

Special HUD components may use authored deviations where defined by a standard preset.

---

## 9. Control Dimensions

At the reference scale:

| Control | Baseline |
|---|---:|
| Compact button | 28 px high |
| Standard button | 36 px high |
| Large/primary button | 40 px high |
| Window title/header | 40 px high |
| Compact list row | 28 px high |
| Standard list row | 32 px high |
| Large list row | 40 px high |
| Checkbox/toggle interaction target | minimum 24 × 24 px |
| Standard input field | 36 px high |

Buttons may be wider according to label/content.

Text should not be vertically squeezed to fit a control below its token height.

---

## 10. Icon Dimensions

At the reference scale:

| Use | Size |
|---|---:|
| Small auxiliary glyph | 16 × 16 px |
| Small gameplay/stat glyph | 20 × 20 px |
| Standard UI icon | 24 × 24 px |
| Standard aura icon | 28 × 28 px |
| Important aura/status icon | 32 × 32 px |
| Action-bar ability slot | 40 × 40 px |
| Inventory item slot | 44 × 44 px |
| Equipment slot | 48 × 48 px |

Icons should be authored to read at their normal presentation size.

Do not compensate for unreadable icons by making every control larger.

---

## 11. Typography

The core functional UI font family is:

**Manrope.**

Manrope is used for:

- buttons;
- labels;
- windows;
- tooltips;
- combat information;
- inventory;
- character sheet;
- talents;
- quest/reputation UI;
- system messages;
- ordinary map labels where appropriate.

Do not mix decorative fantasy faces into routine interactive UI.

A future display typeface may be used sparingly for:

- major location introductions;
- loading presentation;
- exceptional narrative headings.

It must not replace Manrope for functional information.

---

## 12. Typography Scale

At 1920×1080 / 100%:

| Role | Size | Weight |
|---|---:|---|
| Tiny/keybind auxiliary | 11 px | Medium/Semibold |
| Secondary/helper text | 12 px | Medium |
| Standard body/UI | 14 px | Regular/Medium |
| Important value | 14 px | Semibold |
| Section heading | 16 px | Semibold |
| Window heading | 18 px | Semibold |
| Major screen heading | 24 px | Semibold |
| Normal floating combat text | 18 px | Semibold |
| Critical floating combat text | 24 px | Bold |

These sizes should be implemented through shared TextMesh Pro styles/presets.

Do not independently type arbitrary font sizes onto every prefab.

---

## 13. Text Colour Tokens

Radiant Slate text tokens are:

| Token | Colour |
|---|---|
| Text.Primary | `#E6EAF2` |
| Text.Secondary | `#AAB2C5` |
| Text.Muted | `#7C8599` |
| Text.Disabled | derived muted/low-opacity treatment |

Primary text is used for:

- names;
- important labels;
- body content requiring strong readability.

Secondary text is used for:

- descriptors;
- supporting information;
- inactive tabs.

Muted text is used for:

- tertiary metadata;
- unobtrusive helper information.

Do not use pure white for all text.

---

## 14. Core Slate Palette

The Radiant Slate structural palette is:

| Token | Colour |
|---|---|
| Slate.Deep | `#1E2430` |
| Slate.Panel | `#2A3140` |
| Slate.Elevated | `#353D4F` |
| Slate.Border | `#4A5568` |

The overwhelming majority of normal UI chrome should remain within this family.

---

## 15. Core Accent Palette

Semantic accents are:

| Token | Colour |
|---|---|
| Accent.Primary / Radiant Gold | `#E0BF72` |
| Accent.Positive | `#38B986` |
| Accent.Warning | `#E88A52` |
| Accent.Information | `#56A8EF` |
| Accent.Error | `#D85C5C` |

Gold is the ordinary:

- selected;
- active;
- focused;
- important structural accent.

It is not a universal fill colour.

---

## 16. Elemental / Damage Semantic Colours

Where elemental/damage-school colour is meaningful, the established palette is:

| Type | Colour |
|---|---|
| Fire | `#F08A4B` |
| Water | `#4CA7D9` |
| Earth | `#7AA05A` |
| Air | `#9FDFF2` |
| Shadow | `#7A5AC8` |
| Light | `#F0D27A` |

These colours communicate gameplay information.

They must not be used merely to decorate unrelated windows.

---

## 17. Colour Semantics

Colour should have a reason.

Examples:

- gold = selection/focus/high-value structure;
- red = damage/error/hostility where appropriate;
- green = positive/healing/success where appropriate;
- orange = warning;
- blue = information/mana or another explicitly authored resource;
- rarity colour = item rarity only;
- faction relation colour = faction disposition;
- elemental colour = elemental semantic.

Do not allow every subsystem to invent a different colour meaning.

---

## 18. Shader-First Presentation

Radiant Slate should be implemented primarily through shared UI shaders and materials.

The interface should not rely on large quantities of baked nine-sliced decorative frame textures when shader geometry can provide:

- rounded/inset edges;
- borders;
- gradients;
- inner shadows;
- vignettes;
- highlights;
- selection;
- disabled state;
- hover/press response;
- worn/aged structural treatment.

Baked assets remain appropriate for:

- icons;
- illustrations;
- logos;
- maps;
- portraits where used;
- authored decorative motifs that cannot reasonably be generated.

---

## 19. Existing ArcaneSlate Shader Foundation

The current shader family is retained as the implementation foundation.

Current shaders include:

- `ArcaneSlatePanelSurface`;
- `ArcaneSlateButtonSurface`;
- `ArcaneSlateSlotSurface`;
- `ArcaneSlateTabSurface`;
- `ArcaneSlateCheckboxDiamond`;
- line divider shaders;
- generic progress bar;
- cast progress bar;
- resource progress bar;
- horseshoe progress bar;
- target panel.

**Radiant Slate is the design-system name.**

The existing `ArcaneSlate` shader namespace does not need to be renamed solely for terminology consistency.

---

## 20. Shader Parameter Governance

Shader flexibility is not permission for arbitrary prefab styling.

Ordinary UI authors should select standard Radiant Slate component variants rather than independently tuning:

- border thickness;
- corner radius;
- vignette;
- shadow;
- hover amount;
- focus glow;
- gradient colours;
- selection colour.

The implementation should provide centrally controlled presets/material variants.

---

## 21. Standard Surface Treatment

The default Radiant Slate panel uses approximately:

```text
2 px structural outer border
1 px dark inner separation
subtle vertical slate gradient
subtle inner shadow
restrained top-edge highlight
restrained vignette
optional gold selected/focused treatment
```

Normal panels should not:

- emit strong glow;
- use animated borders;
- use bright saturated fills;
- carry noisy texture for its own sake.

---

## 22. Standard Shader Behaviour

Where applicable, standard values should align with the current successful ArcaneSlate shader defaults:

- border: **2 px**;
- radius: **4 px**;
- top highlight strength: approximately **0.06**;
- restrained inner shadow;
- restrained vignette;
- hover brightness increase: approximately **8%**;
- pressed darkening: approximately **15%**.

Exact low-level shader math may evolve while preserving the visual result.

---

## 23. Interaction-State Language

Interactive controls use one consistent state system.

### 23.1 Normal

Neutral slate surface.

### 23.2 Hover

Slight brightness/edge response.

Hover must communicate interactivity without becoming a glow effect.

### 23.3 Pressed

Surface visibly darkens/depresses.

### 23.4 Focused / Selected

Radiant-gold structural highlight.

### 23.5 Disabled

Reduced saturation/luminosity/contrast while remaining readable.

### 23.6 Error / Invalid

Semantic error-red treatment plus text/reason where required.

Do not rely solely on colour for critical validation messaging.

---

## 24. Glow

Glow is deliberately restrained.

Strong glow is appropriate for:

- exceptional magical presentation;
- major attention state;
- selected/focused elements where a subtle rim is insufficient;
- specific authored special states.

Normal clickable buttons should not glow continuously.

---

## 25. Surface Presets

The implementation should expose standard design-system variants equivalent to:

```text
RadiantSlate.Panel.Window
RadiantSlate.Panel.Inset
RadiantSlate.Panel.HUD

RadiantSlate.Button.Standard
RadiantSlate.Button.Compact
RadiantSlate.Button.Primary
RadiantSlate.Button.Danger

RadiantSlate.Slot.Ability
RadiantSlate.Slot.Item
RadiantSlate.Slot.Equipment

RadiantSlate.Bar.Health
RadiantSlate.Bar.Resource
RadiantSlate.Bar.Cast
RadiantSlate.Bar.Progress

RadiantSlate.Tab.Standard
RadiantSlate.Divider
RadiantSlate.Toggle
```

The exact code/material naming may differ.

The concept is authoritative.

---

## 26. Slot Shader Requirements

The current `ArcaneSlateSlotSurface` remains a foundation but must be standardised with the rest of the shader family.

Required direction:

- border/radius configuration should use pixel-space semantics like the other standard shaders;
- ordinary selection should use Radiant Gold rather than the older blue-purple default;
- rarity remains a separate semantic layer;
- rarity pulse should not be mandatory for every rarity-coloured item;
- hover/pressed/disabled behaviour should match the global interaction language;
- action/item/equipment variants should use the standard slot dimensions.

---

## 27. Target/HUD Surface Variant

The existing `ArcaneSlateTargetPanel` worn/noisy edge treatment is suitable as a **special HUD surface variant**.

It should not become the universal treatment for all windows.

Target/player frames may use:

- slightly more pronounced aged-metal structure;
- subtle asymmetry/inset corners;
- restrained Radiant Gold structural accents.

Inventory/settings/quest windows should remain calmer.

---

## 28. UI Token Source

The current `UIConstants` class containing only PrimaryText and SecondaryText is insufficient as the long-term design system.

Implementation requires a central source for:

- colour tokens;
- spacing tokens;
- font styles;
- icon sizes;
- standard dimensions;
- shader/material presets;
- animation timings where appropriate.

This may be implemented through:

- ScriptableObject theme data;
- static token definitions;
- material preset assets;
- TMP style assets;
- a combination.

Product authority is the token system, not a specific C# representation.

---

## 29. HUD Philosophy

The default HUD should be information-rich enough for MMO combat while remaining visually disciplined.

Persistent HUD information should be limited to information that commonly affects immediate decisions.

The screen should not be permanently surrounded by large decorative frames.

Core default combat HUD areas include:

- player frame;
- target frame;
- action bars;
- cast bar;
- auras/statuses;
- experience/progression bar where relevant;
- minimap;
- contextual group/raid frames when grouped;
- contextual notifications.

---

## 30. HUD Edit Mode

Ninth Age should provide a dedicated **HUD Edit Mode**.

HUD modules may be:

- moved;
- shown/hidden where appropriate;
- scaled using approved discrete scales;
- aligned/oriented where the module supports it.

Edit mode should show:

- module bounds;
- anchors/alignment aids;
- module name;
- current scale;
- reset-to-default action.

---

## 31. HUD Scale Steps

Standard HUD module scale choices are:

- **80%**
- **90%**
- **100%**
- **110%**
- **125%**
- **150%**

Internal proportions remain fixed.

A player scales a coherent module rather than individually stretching its child controls.

---

## 32. Ordinary Window Sizing

RPG windows such as:

- Inventory;
- Character;
- Talents;
- Reputation;
- Quest Log;
- vendors;
- crafting;
- market;

use **authored dimensions and layouts**.

They are not arbitrarily resizable by default.

Where useful, these windows may be movable without changing their internal dimensions.

This preserves high-fidelity authored composition.

---

## 33. UI Scale Versus HUD Module Scale

Global UI scale and individual HUD-module scale are separate concepts.

Global UI scale changes the overall interface size for readability/display density.

HUD module scale allows the user to tune a combat module relative to the rest of the UI.

The implementation must avoid compounding scale in a way that produces unusable extremes.

---

## 34. Resolution Behaviour

The UI must support common:

- 16:9;
- 16:10;
- ultrawide;

desktop layouts without stretching shader surfaces or text.

Anchoring should preserve:

- edge offsets;
- central combat composition;
- window visibility;
- minimap location;
- tooltip containment.

The UI must not assume Screen.width/height always corresponds to 1920×1080 design proportions.

---

## 35. Window System

The existing:

`UI_Manager → UI_Window → UI_Panel`

structure is a valid foundation.

The UI PDD requires windows to share:

- the same surface language;
- the same title hierarchy;
- the same close behaviour;
- the same tab behaviour;
- the same spacing/tokens;
- the same interaction conventions.

---

## 36. Window Stack and Escape Behaviour

Open closable windows behave as a **last-opened, first-closed** stack.

Pressing Escape should:

1. close the topmost closable overlay/context menu;
2. otherwise close the most recently opened ordinary window;
3. otherwise perform the gameplay-level cancel behaviour.

The current `UI_Manager.openedWindows` queue behaviour is incorrect for this purpose and must be changed to stack semantics.

---

## 37. Window Opening

Opening a window should:

- bring it to the appropriate foreground layer;
- preserve other compatible windows unless their interaction is explicitly exclusive;
- not unnecessarily block gameplay input;
- avoid full-screen modal darkening unless confirmation genuinely requires it.

MMORPG inventory/character/quest windows are generally non-modal.

---

## 38. Modal Interaction

Use modal confirmation only when the player is about to perform something:

- destructive;
- expensive;
- irreversible;
- identity-changing;
- otherwise difficult to recover.

Examples:

- deleting a character;
- destroying a valuable item;
- confirming a high-cost service where required.

Do not require confirmation dialogs for ordinary reversible actions.

---

## 39. Drag and Drop

The existing drag-ghost approach is retained.

Drag/drop interaction should provide:

- clear source state;
- clear drag ghost;
- clear valid destination highlight;
- invalid destination feedback;
- authoritative result from the server for gameplay mutations.

Inventory, equipment and action bars should use the same underlying drag language.

---

## 40. Context Menus

The existing context-menu system is retained as the standard right-click/secondary-action presentation.

Context menus should:

- open adjacent to the relevant element;
- remain on screen;
- close on outside click/Escape;
- use standard compact rows;
- use icons only where they improve recognition;
- avoid becoming nested menu trees unless necessary.

---

## 41. Action Bars

Ninth Age uses traditional unrestricted MMORPG action bars.

The system must support:

- learned abilities;
- specific learned ability rank where applicable;
- usable item actions where appropriate;
- other explicitly bindable actions.

The player is not restricted to a small action-RPG active-skill loadout.

---

## 42. Action-Bar Structure

A standard action bar contains:

**12 slots.**

The UI should support at least:

**6 action bars**,

for at least 72 ordinary bindable slots.

Only the primary bar needs to be visible by default.

Additional bars can be enabled/repositioned through HUD configuration.

The architecture may support more without changing this PDD.

---

## 43. Action-Bar Slot Presentation

Default action-bar slot size:

**40 × 40 px**

with:

**4 px gap**

at 100% HUD scale.

A slot displays, where relevant:

- ability/item icon;
- keybind;
- cooldown overlay;
- charge count;
- rank where presentation requires it;
- unavailable/disabled state;
- resource/range invalid state.

The icon remains the dominant visual element.

---

## 44. Action-Bar Assignment

Abilities/items should be assignable through consistent drag/drop interaction.

The current right-click context-menu assignment may remain as an auxiliary method.

The UI must not silently force Rank 1 when a different learned rank is selected.

Action-bar bindings persist per character as defined by the Abilities/Persistence PDDs.

---

## 45. Action-Bar Cooldown Feedback

Cooldown presentation should provide:

- radial/clock-style overlay or equivalent readable fill;
- remaining time when useful;
- charge recharge state;
- global/channel cooldown feedback where applicable;
- disabled/unusable treatment distinct from cooldown treatment.

A GCD should not make a spell look permanently disabled.

---

## 46. Player and Target Frames

Player and target frames should be low-profile, high-information Radiant Slate HUD modules.

The default does not require a large portrait.

Identity should come primarily from:

- name;
- level/context;
- health/resource;
- class/faction/target semantics where relevant;
- aura/cast relationships.

Portraits remain appropriate in:

- dialogue;
- character selection;
- character sheet;
- other identity-focused contexts.

---

## 47. Major Unit-Frame Dimensions

At 100% scale, player/target frames should target approximately:

- **400–440 px width**;
- **64–76 px height**;

before externally associated aura/cast extensions.

The exact prefab size may vary within this range to fit the final composition.

The goal is a strong horizontal information strip rather than an oversized panel.

---

## 48. Health and Resource Bars

Standard bar heights:

- ordinary resource/cast bar: **20–24 px**;
- major unit-frame health bar: **24–28 px**.

Bars should use shader-driven:

- fill;
- edge flash where appropriate;
- border;
- subtle gradient;
- inner shadow.

Health/resource text should remain legible without overwhelming the fill.

---

## 49. Resource Colour

Resources use authored semantic colour.

Examples may include:

- Health: positive/health green family;
- Mana: information blue family;
- class resources: authored class/resource colour.

Do not hard-code every resource to the current gold progress-bar default.

The resource definition or presentation mapping should determine its colour.

---

## 50. Cast Bars

Cast bars exist for:

- player;
- current target where visible;
- nameplates where configured.

A cast bar should communicate:

- spell/action identity;
- remaining/progress time;
- channel direction where applicable;
- interrupted/completed state;
- interruptibility if that information is available to the player.

Cast presentation should be consistent across these surfaces.

---

## 51. Auras

Buff/debuff presentation uses:

- icon;
- stacks;
- duration where meaningful;
- semantic buff/debuff treatment;
- tooltip on hover.

Standard target/player aura icon:

**28 × 28 px**

Important/priority effects may use:

**32 × 32 px**

where explicitly configured.

---

## 52. Aura Filtering

Aura UI must not be hard-coded around obsolete implementation categories such as only:

- `Condition_Stack`;
- `Condition_Extend`.

Presentation should consume the authored/runtime aura model.

The UI may filter/sort by:

- beneficial/harmful;
- player-cast;
- dispellable;
- priority;
- boss/important;
- permanent/long duration;
- other explicit presentation metadata.

---

## 53. Aura Ordering

Default ordering should prioritise gameplay relevance over arbitrary insertion order.

A reasonable hierarchy is:

1. important/boss mechanic;
2. harmful effects;
3. player-relevant timed effects;
4. ordinary beneficial effects;
5. long/permanent/passive effects.

Exact tie-break order can remain implementation data.

---

## 54. Nameplates

The existing nameplate system is retained and extended.

It already supports useful concepts:

- name visibility;
- health-bar visibility;
- cast-bar visibility;
- own-nameplate option;
- overlap/stack mode;
- faction colouring;
- target selection highlight.

These become formal supported settings.

---

## 55. Nameplate Default Behaviour

Nameplates should prioritise:

- target;
- hostile/attackable actors;
- combat-relevant actors;
- hovered/interacted actors.

The user may configure supported display modes.

The current `InCombat` health-bar setting must be implemented rather than remaining an empty mode.

---

## 56. Nameplate Stacking

The current overlap/stack option is retained.

Targeted nameplate receives visual priority.

Stacking must not move labels so far from actors that ownership becomes ambiguous.

---

## 57. Floating Combat Text

Floating combat text remains supported.

It should communicate important immediate results such as:

- damage;
- healing;
- critical results;
- miss;
- dodge;
- parry;
- block;
- resist;
- absorb;
- reflect;
- XP/progression gain where desired.

Normal FCT baseline:

**18 px Semibold**

Critical FCT baseline:

**24 px Bold**

before distance/world-space scaling.

---

## 58. Floating Combat Text Configuration

Players should be able to reduce FCT clutter.

At minimum, settings should allow categories such as:

- own outgoing damage;
- incoming damage;
- healing;
- avoidance results;
- XP/progression text.

Exact settings grouping belongs to client implementation.

Critical gameplay information must remain available elsewhere when FCT is disabled.

---

## 59. Combat Log UI

CombatLogEntry already exists as a gameplay/event foundation.

A readable combat-log interface should exist.

It may share a tabbed text-panel shell with future chat/communication UI, but:

- combat log content is not the same as social chat;
- filtering must be independent;
- Communication Systems owns chat/channel semantics.

Combat log filters should support major categories such as:

- damage;
- healing;
- buffs/debuffs;
- avoidance/results;
- system/combat events.

---

## 60. Tooltips

Tooltips are a core information surface.

The existing tooltip window/panel structure is retained.

Tooltips should:

- open adjacent to the hovered/focused element;
- remain within screen bounds;
- not unnecessarily follow the cursor;
- close reliably on pointer exit/context change;
- use standard Radiant Slate surface and typography;
- preserve strong hierarchy.

---

## 61. Item Tooltips

The Items PDD owns item-tooltip content/order.

This PDD owns visual treatment.

Item tooltips should use:

- rarity-coloured item name where defined;
- clear section spacing;
- Primary/Secondary/Muted text roles;
- data-driven stat formatting;
- condition-state emphasis;
- comparison deltas;
- constrained width;
- no unnecessary decorative clutter.

The existing ItemTooltipFactory is not feature-complete relative to the Items PDD.

---

## 62. Item Comparison

Hovering an equippable item when a replacement item is equipped must expose the delta required by the Items PDD.

Comparison should be visually associated with the hovered item tooltip.

Numeric changes use consistent positive/negative semantics.

Non-numeric gained/lost effects should be listed separately.

Two-handed comparisons must account for the resulting equipment state.

---

## 63. Ability/Talent Tooltips

Ability/talent tooltips should use generated/data-driven descriptions wherever the gameplay system supports them.

They should communicate relevant:

- rank;
- cost;
- cast time;
- cooldown;
- range;
- requirements;
- effects;
- talent rank state.

Do not hand-write UI-only rules that can drift from authoritative gameplay data.

---

## 64. Inventory

The existing grid inventory model is retained.

Inventory slots use:

**44 × 44 px**

at 100% scale.

Interaction includes:

- hover tooltip;
- drag/drop movement;
- right-click/context action;
- clear stack count;
- rarity treatment;
- invalid-action feedback.

---

## 65. Equipment

Equipment slots use:

**48 × 48 px**

at 100% scale.

The Character window should clearly distinguish:

- equipped item;
- inactive item due to failed condition;
- empty slot;
- broken/low durability state where applicable.

Equipment comparison follows the Items PDD.

---

## 66. Character Window

The existing Character window/tab concept is retained.

It may include authored tabs/panels for:

- character identity/data;
- equipment;
- stats;
- focus/derived stats;
- skills;
- weapon skills.

Information hierarchy must follow the Character Stats and Items PDDs.

Avoid showing every internal stat merely because the runtime tracks it.

---

## 67. Talent Window

The existing three-tree Talent window concept is retained.

The current implementation that derives talent rows from `MinLevel` every five levels is not authoritative.

Talent presentation must use the Abilities/Talents PDD's explicit:

- tree;
- position;
- tier/depth;
- prerequisite;
- rank;
- spent-points-in-tree gating.

Each tree remains visually identifiable while sharing the Radiant Slate component system.

---

## 68. Quest UI

Quest UI should support the Quest PDD without becoming a constant intrusive objective overlay.

The Quest Log should clearly communicate:

- quest name;
- narrative/description;
- objectives;
- progress;
- rewards;
- completion state;
- turn-in state.

A lightweight tracked-objective HUD may exist.

The player controls which quests are tracked where practical.

---

## 69. Dialogue UI

Dialogue remains an identity/narrative surface rather than a generic system popup.

It may use:

- NPC name;
- portrait/model imagery where available;
- dialogue text;
- response choices;
- condition/availability treatment.

Dialogue should use Radiant Slate typography/surface treatment while allowing more identity presentation than ordinary utility windows.

---

## 70. Reputation UI

The existing Reputation-window concept is retained.

For a visible faction, the UI should communicate:

- name/icon;
- description;
- numeric reputation;
- current tier;
- progression through the relevant range;
- thresholds;
- benefits;
- penalties.

Negative and positive standing must be clearly distinguishable.

Hidden factions remain excluded as required by the Factions PDD.

---

## 71. Group and Raid Frames

Group/raid UI must support the Group PDD's:

- party size 5;
- raid subgroups of up to 5;
- Tank/Healer/Damage role identity;
- target markers;
- larger raid structures.

Party frames should prioritise:

- health;
- important resource/status where useful;
- role;
- dead/offline state;
- important dispellable/encounter auras.

Raid frames should become denser rather than simply scaling party frames up.

---

## 72. Target/World Markers

The UI must present:

- target markers;
- world markers;

with icons that remain legible at MMO camera distances.

Marker graphics should remain visually distinct from ordinary aura/ability icons.

---

## 73. Minimap

Ninth Age should have a minimap.

Default placement:

**top-right HUD region**

unless the player moves it in HUD Edit Mode.

Initial reference size:

**200 × 200 px**

at 100% scale.

The minimap is a navigation aid, not an automatic replacement for observing the world.

---

## 74. Minimap Information

The minimap may show appropriate authored/contextual information such as:

- player position/facing;
- nearby mapped roads/terrain representation;
- discovered services/locations;
- group members;
- relevant tracked objectives where allowed by quest design;
- lodestones;
- explicit world markers.

It should not automatically reveal undiscovered world information.

Exact exploration/reveal rules belong to world/quest design.

---

## 75. World Map

A larger world map should exist as an authored RPG window/screen.

It should support:

- zoom/pan;
- region/zone context;
- player position;
- discovered locations;
- lodestones;
- markers;
- tracked objectives where appropriate.

The map visual language may include map-specific artwork but uses Radiant Slate for:

- chrome;
- controls;
- labels;
- tabs;
- tooltips.

---

## 76. Notifications and Toasts

Small non-blocking notifications may communicate:

- item received;
- reputation change;
- tier change;
- quest update;
- lodestone discovery;
- level/weapon-skill progression;
- system status.

Notifications should:

- queue cleanly;
- avoid covering central combat information;
- disappear automatically where appropriate;
- remain reviewable through their owning system when important.

---

## 77. Contextual Tips

Character Creation and Identity requires onboarding through contextual tips rather than a tutorial.

The UI tip system must be:

- non-blocking;
- concise;
- contextually triggered;
- dismissible;
- non-modal;
- suppressible after acknowledgement where appropriate.

Only one ordinary instructional tip should compete for attention at a time.

---

## 78. Contextual Tip Visual Treatment

Tips should use a compact Radiant Slate callout.

They may anchor visually to the relevant:

- UI element;
- interaction region;
- system notification area.

They must not:

- dim/lock the whole screen;
- disable unrelated controls;
- force a click sequence;
- require tutorial completion.

---

## 79. Tip Persistence

Tip acknowledgement/suppression should persist according to the Character Creation PDD.

A tip may be:

- account-wide;
- character-specific;

depending on what it teaches.

The UI should not repeatedly teach an experienced account how to open inventory on every new character unless intentionally configured.

---

## 80. Input Assumptions

The primary interaction model is keyboard + mouse.

The current Unity Input System foundation is retained.

UI actions should support remapping through the broader input/settings system.

Controller-specific product requirements remain owned by Accessibility/Input.

---

## 81. Keyboard Interaction

Common interface windows should have bindable hotkeys.

Escape follows the window-stack/cancel rule.

Enter/confirm/selection semantics should be predictable where keyboard navigation exists.

Do not require mouse-only interaction for critical menu operations where keyboard navigation is reasonably expected.

---

## 82. Error and Validation Feedback

Errors should explain what failed.

Examples:

- "Name is already in use";
- "Requires 20 gold";
- "Cannot use while in combat";
- "Inventory is full";
- "Requires 15 points in Guardian";
- "Item cannot be equipped";

Avoid silent failure.

Avoid presenting normal validation failures as severe modal errors.

---

## 83. Error Presentation Layers

Use the least intrusive layer that still communicates the issue.

Examples:

- inline field validation for forms;
- transient combat/system message for immediate gameplay invalidation;
- tooltip/disabled reason for unavailable actions;
- modal confirmation only where the player must decide before a destructive action.

---

## 84. Loading/Reconnect UI

Loading and reconnect presentation should distinguish:

- connecting;
- authenticating;
- loading character;
- reconnecting to an existing actor/session;
- transferring world/server context;
- failure.

Do not imply that reconnect means a fresh character spawn if the Persistence PDD is reclaiming an existing runtime actor.

---

## 85. Local UI Settings

Client-side UI preferences may be stored locally.

Examples:

- nameplate display modes;
- HUD layout;
- HUD module scales;
- global UI scale;
- FCT visibility;
- map/minimap preferences;
- window positions where allowed.

Gameplay-authoritative state must not live only in these settings.

---

## 86. Character-Specific UI State

Character-owned UI state includes gameplay-relevant/persistent presentation choices where another PDD requires it, particularly:

- action-bar assignments.

Other local layout preferences may remain account/client-wide.

The persistence scope should be explicit rather than accidental.

---

## 87. Settings Architecture

The existing `ClientSettingsManager` / PlayerPrefs foundation may remain for local preferences, but the user-facing settings system must expose coherent categories and defaults.

Do not create unrelated one-off settings storage inside individual UI components.

---

## 88. Animation

UI animation should be short and functional.

Appropriate uses:

- fade/appear;
- hover transition;
- press response;
- cooldown sweep;
- selection movement;
- progress interpolation;
- notification entry/exit.

Avoid slow ornamental motion that delays interaction.

Normal interactive controls should feel immediate.

---

## 89. Audio Relationship

UI sound should reinforce:

- hover/select;
- confirm;
- cancel;
- error;
- inventory/equipment action;
- major notifications.

Exact sound language belongs to Audio and Music PDD.

Visual state must remain sufficient without relying on sound alone.

---

## 90. Accessibility Boundary

This PDD establishes readable sizing, hierarchy and semantic consistency.

The Accessibility and Input PDD owns deeper requirements such as:

- colour-vision alternatives;
- subtitle controls;
- input accessibility;
- motion reduction;
- additional scaling/readability options;
- controller support requirements.

Radiant Slate implementation must not make such future support unnecessarily difficult.

---

## 91. Current Implementation — Foundations to Retain

The current client contains substantial UI foundations worth retaining.

### 91.1 UI architecture

- `UI_Manager`;
- `UI_Window`;
- `UI_Panel`.

### 91.2 Shader family

The ArcaneSlate shaders already provide:

- SDF/pixel borders;
- corner radii/insets;
- gradients;
- inner shadows;
- top highlights;
- vignettes;
- aged/noisy edge treatment;
- hover;
- pressed;
- focused;
- disabled;
- selection;
- fill/progress;
- icon/rarity presentation.

### 91.3 HUD foundations

Existing:

- player unit frame;
- target frame;
- resource bars;
- special-resource panels;
- player cast bar;
- target cast bar;
- nameplate cast bars;
- auras;
- floating combat text;
- action bars;
- experience bar.

### 91.4 RPG windows

Existing:

- Inventory;
- Character;
- Talents;
- Quest Log;
- Reputation;
- Dialogue;
- Character Creation;
- Character Selection;
- Context Menu;
- Tooltip.

### 91.5 Nameplate settings

Current nameplate visibility/stacking settings are useful foundations.

### 91.6 Drag/drop

Inventory's `IDraggableSlot` + drag ghost provides a useful common interaction base.

---

## 92. Current Implementation — Required Design-System Changes

### 92.1 Expand UIConstants/theme ownership

Replace the current two-colour constant scope with a full Radiant Slate token/theme system.

### 92.2 Standardise shader units

`ArcaneSlateSlotSurface` still exposes normalized border/radius values unlike the newer pixel-based shaders.

Move standard component geometry to pixel-space semantics.

### 92.3 Standardise accents

Current shader defaults mix:

- blue-purple focus/selection;
- gold;
- bright yellow.

Normal selection/focus should use Radiant Gold.

Semantic colours remain separate.

### 92.4 Remove arbitrary hard-coded colours

Replace UI uses of raw values such as:

- `Color.red`;
- `Color.green`;
- `Color.yellow`;

with shared semantic tokens where those colours represent UI meaning.

### 92.5 Shared typography

Create shared TMP styles for the typography scale.

### 92.6 Shared component materials/presets

Prefab authors should consume approved variants rather than cloning/tuning arbitrary material values.

---

## 93. Current Implementation — Required UX Changes

### 93.1 Window close ordering

Replace the current FIFO queue close behaviour with LIFO window-stack behaviour.

### 93.2 HUD edit/layout persistence

Add HUD positioning/scaling persistence.

### 93.3 Action bar input

Current input binding only maps the first 12-slot bar.

Support additional configured action bars/keybindings.

### 93.4 Action-bar assignment

Add drag/drop assignment and preserve chosen rank.

Do not hard-code context-menu assignment to Rank 1.

### 93.5 Cooldown presentation

Extend action-bar presentation beyond the current whole-icon disabled treatment to proper cooldown/charge/GCD states.

### 93.6 Nameplate InCombat mode

Implement the existing `InCombat` display option.

### 93.7 Aura filtering

Remove outdated target-aura filtering tied only to `Condition_Stack` / `Condition_Extend`.

### 93.8 Tooltip completeness

Expand ItemTooltipFactory to satisfy the Items PDD and shared tooltip rules.

### 93.9 Talent layout

Replace MinLevel-derived every-five-level rows with explicit authored tree layout/depth.

### 93.10 Map/minimap

Implement map/minimap UI; none currently exists in the client UI code.

### 93.11 Combat log window

Expose the existing combat-log event stream through a filterable readable UI.

### 93.12 Contextual tips

Implement the non-blocking contextual tip system required by Character Creation.

---

## 94. Design Review Requirement

New major UI components should be reviewed against Radiant Slate before implementation is considered complete.

Review should verify:

- token use;
- typography;
- spacing;
- icon sizing;
- surface preset;
- interaction states;
- shader consistency;
- information hierarchy;
- scaling;
- screen-bound behaviour;
- consistency with adjacent systems.

A technically functional window with arbitrary styling is not UI-complete.

---

## 95. Locked Design Decisions

The following decisions are locked:

1. Radiant Slate is the authoritative Ninth Age UI design system.
2. UI presentation must be consistent across all gameplay systems.
3. Radiant Slate uses dark slate/aged-metal surfaces with restrained Radiant Gold accents.
4. The UI should be shader-driven wherever practical.
5. Existing ArcaneSlate shaders are retained as the implementation foundation.
6. The ArcaneSlate shader namespace does not need renaming merely because the design language is called Radiant Slate.
7. UI component styling is governed by shared tokens/presets rather than arbitrary per-prefab tuning.
8. The reference design resolution is 1920×1080 at 100% UI scale.
9. The spacing grid is 4 px.
10. Normal outer border is 2 px.
11. Normal inner/divider border is 1 px.
12. Standard corner radius is 4 px.
13. Standard panel padding is 16 px.
14. Manrope is the functional UI typeface.
15. Standard UI text is 14 px at the reference scale.
16. Window headings are 18 px.
17. Major screen headings are 24 px.
18. Radiant Slate text colours use the defined Primary/Secondary/Muted tokens.
19. Radiant Gold `#E0BF72` is the ordinary selection/focus accent.
20. Strong glow is exceptional, not normal-button presentation.
21. Standard controls use the defined compact/standard/large dimensions.
22. Action-bar slots are 40×40 px at 100%.
23. Inventory slots are 44×44 px.
24. Equipment slots are 48×48 px.
25. Standard aura icons are 28×28 px; important auras may use 32×32 px.
26. The default action-bar structure uses 12 slots per bar.
27. The system supports at least 6 ordinary action bars.
28. Action-bar binding remains traditional/unrestricted rather than a small active-skill loadout.
29. HUD modules support Edit Mode.
30. HUD scales are 80/90/100/110/125/150%.
31. HUD scaling preserves internal proportions.
32. Ordinary RPG windows use authored layouts and are not arbitrarily resizable by default.
33. Window Escape behaviour is LIFO.
34. Context menus close on outside click/Escape.
35. Drag/drop uses a common drag-ghost/destination-feedback language.
36. Player/target frames are low-profile horizontal HUD modules.
37. Player/target frames do not require large portraits by default.
38. Target/player frame target size is approximately 400–440 px × 64–76 px at 100%.
39. Major Health bars target 24–28 px height.
40. Standard resource/cast bars target 20–24 px height.
41. Aura presentation uses the authored aura model, not obsolete stack-behaviour categories.
42. Existing configurable nameplate display/stacking concepts are retained.
43. Floating combat text remains supported and configurable.
44. A filterable combat-log UI must exist.
45. Tooltips use the shared Radiant Slate visual system and remain within screen bounds.
46. Item tooltip content/order follows the Items PDD.
47. Talent layout must use authored tree/depth/position rather than inferred MinLevel rows.
48. Ninth Age has a minimap.
49. Default minimap reference size is 200×200 px at 100%.
50. Ninth Age has a larger world map.
51. Contextual tips are non-blocking and do not form a tutorial sequence.
52. The primary interaction model is keyboard + mouse.
53. Common interface hotkeys should be remappable.
54. Validation errors should explain the reason rather than fail silently.
55. Local UI preferences may persist client-side.
56. Gameplay-authoritative state remains server-owned.
57. A major UI is not considered complete merely because it is functional; it must conform to Radiant Slate.

---

## 96. Open / Deferred Details

The UI system is design-ready.

The following remain dependent-system or tuning details.

### 96.1 Global UI-scale range — Resolved

The Accessibility and Input PDD defines global UI scale as:

- **75%–200%**
- **100% default**
- **5% increments**

This remains separate from the HUD module scales defined by this PDD.

### 96.2 Controller navigation

Owned by Accessibility/Input.

### 96.3 Chat UI details

The text-panel shell can be designed here, but:

- channels;
- whispers;
- moderation;
- communication behaviour;

belong to Communication Systems.

### 96.4 Exact map reveal/marker rules

Owned by World/Quest design.

### 96.5 Exact group/raid frame layouts

The information hierarchy is defined, while exact 10/20/40-player compositions can be tuned during implementation.

### 96.6 Exact notification timing

Queueing/placement is required; exact seconds and animation curves are tuning.

### 96.7 Exact window dimensions

Each authored RPG window may define its dimensions using this token system.

### 96.8 Decorative display typeface

No additional display font is required for core UI.

A future display face may be selected for limited non-functional presentation.

---

## 97. Validation Criteria

The UI/UX system satisfies this PDD when all of the following are true.

### 97.1 Design system

- All major UI uses Radiant Slate.
- Shared palette tokens are used.
- Shared typography styles are used.
- Standard spacing follows the 4 px grid.
- Standard component dimensions are used.
- Major panels no longer invent unrelated border/radius styles.

### 97.2 Shader implementation

- ArcaneSlate remains the shared shader foundation.
- Slot shader geometry uses consistent pixel-space semantics.
- Standard focus/selection uses Radiant Gold.
- Shader/material presets exist for normal component classes.
- Ordinary prefab authors do not need to hand-tune the full shader parameter set.

### 97.3 Typography

- Manrope is used across functional UI.
- Standard body UI is 14 px at reference scale.
- Window titles use the standard heading token.
- Text colour follows Primary/Secondary/Muted semantics.
- Text remains readable at supported resolutions/scales.

### 97.4 HUD

- Core combat HUD uses coherent player/target/action/cast/aura modules.
- HUD modules can be repositioned.
- HUD modules support locked scale steps.
- Default HUD remains usable without editing.
- HUD configuration persists.

### 97.5 Windows

- Windows use common title/surface/spacing conventions.
- Escape closes the latest relevant overlay/window first.
- Ordinary windows are not arbitrarily resized.
- Context menus close correctly.
- Tooltips stay on-screen.

### 97.6 Action bars

- Each bar has 12 slots.
- At least six bars are supported.
- Additional bars can be enabled/configured.
- Abilities can be drag-bound.
- Selected spell rank is preserved.
- Cooldown/GCD/charges are visually distinct.
- Keybind text remains legible without obscuring the icon.

### 97.7 Unit frames/nameplates

- Player/target frames follow the low-profile Radiant Slate layout.
- Health/resource/cast information is readable.
- Nameplate visibility settings work.
- InCombat mode works.
- Nameplate stack/overlap modes work.
- Target selection remains clear.

### 97.8 Auras

- Aura UI is not restricted to old Condition stack behaviours.
- Stacks/durations display consistently.
- Buff/debuff/priority semantics can be represented.
- Important auras can receive priority sizing/presentation.

### 97.9 Combat feedback

- FCT supports major combat-result categories.
- FCT categories can be reduced/disabled.
- Critical text is visually distinguished.
- Combat log is accessible and filterable.
- Disabling FCT does not remove access to authoritative combat information.

### 97.10 RPG windows

- Inventory uses 44 px slots.
- Equipment uses 48 px slots.
- Character, Talent, Quest and Reputation windows use shared design tokens.
- Talent trees use explicit authored layout.
- Item tooltips meet the Items PDD.
- Item comparison communicates numeric and non-numeric differences.

### 97.11 Navigation

- Minimap exists at the standard reference size.
- World map exists.
- Map chrome uses Radiant Slate.
- Undiscovered information is not automatically exposed unless world/quest rules permit it.

### 97.12 Tips and validation

- Contextual tips are non-blocking.
- Tips do not gate progression.
- Tip acknowledgement can persist.
- Common invalid actions explain why they failed.
- Destructive confirmations are modal only where justified.

### 97.13 Consistency

A reviewer can move between:

- inventory;
- character;
- talents;
- reputation;
- quests;
- combat HUD;
- character creation;
- maps;

without encountering different typography, arbitrary spacing, unrelated border styles or contradictory interaction states.

---

## 98. Design Summary

Ninth Age UI is not a collection of independently styled game windows.

It is one coherent **Radiant Slate** interface system.

Radiant Slate uses a disciplined 4 px layout grid, Manrope typography, dark slate structural surfaces, restrained aged-metal shader treatment, pale text and Radiant Gold selection/focus accents.

The existing ArcaneSlate shaders already provide the correct technical foundation and should be standardised rather than replaced.

Major visual values — fonts, colours, borders, icon sizes, slot sizes, control heights and interaction states — are shared design tokens.

The combat HUD remains information-rich enough for traditional MMORPG play while avoiding excessive permanent chrome. HUD modules can be moved and discretely scaled without destroying their internal proportions. Ordinary RPG windows remain carefully authored rather than arbitrarily resizable.

The result should feel deliberate at every scale:

> **The player should never be able to tell which UI subsystem was implemented first, last or by a different developer. It should all look and behave like one interface.**
