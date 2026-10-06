# Ninth Age — Accessibility and Input Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Input abstraction, remapping, keyboard/mouse and controller support, UI navigation, camera/input options, global UI/text scaling, colour dependence, subtitles/captions, motion/flashing reduction, combat-information alternatives, accessibility settings and settings persistence  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended accessibility and player-input model for **Ninth Age**.

It is authoritative for:

- the reference input scheme;
- input abstraction;
- runtime input remapping;
- binding persistence;
- mouse-button/modifier bindings;
- keyboard-only UI navigation;
- controller support requirements;
- device switching;
- focus navigation;
- camera sensitivity/inversion/recentring options;
- hold/toggle alternatives;
- global UI scaling;
- independent text scaling;
- chat text scaling;
- subtitle/caption requirements;
- colour-dependence rules;
- semantic colour alternatives;
- flashing/motion/VFX reduction;
- floating-combat-text accessibility;
- tooltip readability options;
- accessibility-related audio requirements;
- accessibility/settings persistence.

This document does not define:

- core UI art direction or ordinary component dimensions — [UI and UX PDD](UI-and-UX-PDD.md);
- exact audio mix/content — [Audio and Music PDD](Audio-and-Music-PDD.md);
- combat rules — [Combat System PDD](Combat-System-PDD.md);
- ability action-bar ownership — [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- communication-channel semantics — [Communication Systems PDD](Communication-Systems-PDD.md);
- camera implementation details — client technical architecture;
- platform-certification requirements beyond the product requirements stated here.

Where implementation conflicts with this document, this PDD defines intended player-facing behaviour.

---

## 2. Design Pillars

### 2.1 Accessibility is a system requirement, not a post-processing feature

Accessibility must be supported by:

- input architecture;
- UI layout;
- combat presentation;
- camera behaviour;
- subtitles/captions;
- settings persistence.

It must not depend on retrofitting one monolithic "accessibility mode" after systems are complete.

### 2.2 Input actions, not devices, define gameplay

Gameplay code should consume semantic actions such as:

- Move;
- Look;
- Jump;
- Interact;
- Target;
- AutoAttack;
- ActionBarSlot;
- OpenInventory.

It should not fundamentally depend on a specific physical key, mouse button or controller button.

### 2.3 Important information should have more than one presentation path

Critical gameplay state should not be communicated only through:

- colour;
- sound;
- flashing;
- screen movement.

Where practical, important information should be represented through at least two compatible channels such as:

- icon;
- text;
- shape;
- position;
- animation;
- sound;
- world effect.

### 2.4 Readability takes precedence over authored density

The default Radiant Slate layout remains carefully authored, but players must be able to increase UI/text size without clipping or losing access to controls.

### 2.5 Reducing effects must not remove required information

Settings that reduce:

- flashing;
- VFX;
- camera shake;
- FCT animation;
- full-screen effects

must preserve the underlying gameplay information through a clearer/subtler alternative where necessary.

### 2.6 Settings should be granular

Accessibility should be exposed through independent options.

A single "Accessibility Mode" is not sufficient.

---

## 3. Existing Implementation Baseline

The current repository already contains useful foundations:

- Unity Input System action maps;
- separate `Gameplay`, `UINav` and `Interface` action maps;
- `InputMode.GameplayMovement`;
- `InputMode.UINavigation`;
- `InputMode.TextInput`;
- `InputMode.Disabled`;
- UI navigation actions;
- keyboard/mouse and Gamepad control schemes in the Input Actions asset;
- local settings persistence through `ClientSettingsManager`;
- Cinemachine-based player camera;
- Radiant Slate UI scaling/layout foundations.

These should be completed rather than replaced without reason.

---

## 4. Current Implementation Gaps

The present implementation is not yet fully device-independent.

Examples include:

- `ClientInputController.Update()` directly polling `Mouse.current` for world pointer interaction;
- `CharacterCameraController` directly polling `Mouse.current`;
- `CharacterCameraController` directly polling `Keyboard.current` for WASD movement state;
- camera-character rotation directly derived from mouse delta;
- only the first 12-slot action bar currently routed by `ClientInputController`;
- hard-coded interface actions in code;
- no complete runtime remapping screen;
- no full controller gameplay path despite Gamepad UI-navigation bindings;
- hard-coded camera recenter timing/speed;
- no complete Accessibility settings screen;
- no global UI/text-scale implementation;
- generic PlayerPrefs storage without a strongly defined settings catalogue/schema.

These are implementation gaps relative to this PDD.

---

## 5. Reference Control Scheme

The primary/reference control scheme is:

**Keyboard + Mouse**

This determines:

- initial default bindings;
- baseline UI interaction assumptions;
- default tooltip/input-prompt presentation.

It does not permit gameplay code to hard-code keyboard or mouse devices.

---

## 6. Input-System Abstraction

All player gameplay/interface control must route through semantic input actions.

Direct device polling in gameplay logic should be eliminated wherever the input represents a bindable player action.

Conceptually:

```text
Physical Input
    ↓
Unity Input Action / Binding
    ↓
Semantic Player Action
    ↓
Gameplay / UI Behaviour
```

---

## 7. Direct Device Polling

Direct access such as:

- `Keyboard.current.wKey`;
- `Mouse.current.rightButton`;
- `Mouse.current.delta`;

must not be the authoritative definition of a remappable gameplay action.

Direct device inspection remains acceptable for low-level device detection/diagnostics where it does not bypass the binding system.

---

## 8. Remapping

All ordinary gameplay and interface actions should be remappable.

This includes at minimum:

- movement;
- jump;
- camera/look controls where represented as bindings;
- targeting;
- interact;
- auto-attack;
- action-bar slots;
- interface windows;
- map;
- chat activation;
- other player-triggered gameplay actions.

---

## 9. Multiple Bindings

Each bindable action should support at least:

**two user bindings**

where the input type permits it.

This allows combinations such as:

- keyboard key + mouse button;
- primary key + secondary key;
- controller + keyboard configuration.

---

## 10. Modifier Bindings

Bindings may use modifiers such as:

- Shift;
- Ctrl;
- Alt;

where supported by the input model.

Examples include:

- `Shift+1`;
- `Ctrl+E`;
- `Alt+Mouse4`.

The system must distinguish a modified binding from its unmodified equivalent.

---

## 11. Mouse-Button Binding

Players may bind ordinary bindable gameplay actions to available mouse buttons.

The input system must not assume:

- left/right/middle are the only mouse buttons;
- side buttons cannot be bound.

Where left/right mouse retain world-selection/camera conventions, conflicts must be clearly represented.

---

## 12. Binding Conflicts

The settings UI should detect conflicting bindings.

Conflicts should:

- produce a clear warning;
- identify the other action using that binding;
- allow the player to resolve the conflict.

Duplicate bindings are not universally forbidden.

The player may deliberately keep a duplicate where context/action-map separation makes it useful.

---

## 13. Clearing and Restoring Bindings

Players must be able to:

- clear an individual binding;
- restore an individual action to default;
- restore a category/action map to defaults;
- restore all bindings to defaults.

Resetting bindings must not reset unrelated graphics/audio/UI settings.

---

## 14. Input Persistence

Input bindings persist as client/account-level preferences across characters.

Character-specific action-bar contents remain character-owned gameplay configuration under the Ability/Persistence systems.

The physical key bound to "Action Bar Slot 1" is not character progression.

---

## 15. Input-Mode Separation

The existing conceptual modes are retained:

- Gameplay Movement;
- UI Navigation;
- Text Input;
- Disabled.

Switching modes must prevent unintended actions.

Examples:

- typing into chat should not cast abilities because the player pressed `1`;
- navigating UI should not move the character unless explicitly designed;
- closing text input should return to the appropriate prior gameplay/UI state.

---

## 16. Keyboard-Only UI Navigation

All ordinary game menus must be operable without requiring a mouse.

This includes at minimum:

- Login/character selection;
- Character Creation;
- Settings;
- Inventory;
- Character;
- Talents;
- Quest Log;
- Reputation;
- Guild;
- map;
- dialogue/choices;
- confirmation dialogs;
- ordinary vendors/services.

---

## 17. UI Focus

Keyboard/controller UI focus must always be visually apparent.

Focus presentation follows Radiant Slate and must not rely solely on subtle colour change.

The user must be able to determine:

- which control is focused;
- what will happen on Submit;
- where focus moved after opening/closing a window.

---

## 18. UI Navigation Order

UI focus navigation should follow meaningful visual/logical order.

It must not be dependent on arbitrary GameObject hierarchy when that produces confusing focus movement.

Complex grids such as:

- inventory;
- equipment;
- talents;

should support predictable directional navigation.

---

## 19. Pointer Independence

Critical menu actions must not require precision pointer interaction when the same function can reasonably be exposed through:

- focus navigation;
- context actions;
- keyboard shortcuts.

Drag/drop may remain a primary convenience interaction but should not be the only way to perform critical inventory/equipment actions where an alternative is practical.

---

## 20. Controller Support Direction

The architecture must support complete controller operation.

Controller support should eventually cover:

- movement;
- camera;
- targeting;
- combat actions;
- interaction;
- UI navigation;
- inventory;
- map;
- dialogue;
- action bars.

Keyboard/mouse remains the reference control scheme.

---

## 21. Controller Delivery Scope

A fully polished controller scheme may be staged separately from initial keyboard/mouse implementation.

However:

- input architecture;
- UI focus/navigation;
- action abstractions;
- prompts;

must not be designed in a way that makes complete controller support require replacing core systems.

---

## 22. Controller Action Bars

Traditional MMO action-bar breadth means controller interaction will likely use:

- modifiers;
- action sets;
- radial/context layers;
- other controller-appropriate mapping.

The exact controller action-bar layout is intentionally deferred.

The controller scheme must not reduce the authoritative ability/action-bar system to a small permanent action-RPG loadout.

---

## 23. Device Switching

The client should support changing between available input devices without restarting the game.

Input prompts should update to the most recently active relevant control scheme where practical.

Device switching must not reset:

- bindings;
- camera settings;
- UI configuration.

---

## 24. Controller Dead Zones and Sensitivity

Controller support should expose:

- look sensitivity;
- stick dead-zone settings;
- invert-axis options.

Exact default/dead-zone ranges remain implementation tuning.

---

## 25. Mouse Camera Sensitivity

The camera settings must expose independent:

- horizontal look sensitivity;
- vertical look sensitivity.

Sensitivity must not be tied to display resolution.

---

## 26. Axis Inversion

Players can independently configure:

- Invert Horizontal Look;
- Invert Vertical Look.

These options apply to relevant pointer/stick camera inputs.

---

## 27. Camera Zoom

Camera options must include:

- zoom sensitivity;
- maximum/allowed camera distance within gameplay constraints.

The camera should retain enough authored limits to prevent invalid clipping/exploit views.

Exact min/max distance values remain camera/content tuning.

---

## 28. Camera Recentering

Automatic camera recentering must be configurable.

Required options include:

- Auto Recenter: On/Off;
- Recenter Delay;
- Recenter Speed.

The current hard-coded recenter wait/time must become settings-driven.

---

## 29. Camera Shake

Camera shake intensity is configurable from:

**0%–100%**

0% disables camera shake.

Gameplay information may not depend on camera shake being enabled.

---

## 30. Camera Bob and Sway

If camera bob/sway effects are used, the player must be able to disable them.

The game should not require camera bob to understand movement speed/state.

---

## 31. Motion Blur

Motion blur can be disabled independently.

The setting should be accessible without requiring a special graphics preset.

---

## 32. Other Motion Effects

Any later feature that creates substantial involuntary camera motion should expose reduction/disable controls where practical.

Examples include:

- exaggerated knockback camera movement;
- cinematic sway;
- rapid zoom;
- screen-space warping.

---

## 33. Hold versus Toggle

Where a sustained action reasonably supports both interaction styles, the settings system should provide Hold/Toggle alternatives.

Candidates include:

- autorun-related modes;
- crouch/crawl;
- camera-look modes where practical;
- target/lock modes introduced later.

This is not a requirement to add meaningless toggle modes to every button.

---

## 34. Rapid/Repetitive Input

Core gameplay should avoid requiring unnecessary rapid repeated button pressing as an accessibility gate.

If a future interaction is fundamentally based on rapid/repeated input, it should consider:

- hold alternative;
- reduced repetition;
- other equivalent input method.

Ordinary menu navigation must never require rapid timed input.

---

## 35. Global UI Scale

Global UI scale is configurable from:

**75% to 200%**

Default:

**100%**

The setting is independent from per-HUD-module scales defined by the UI/UX PDD.

---

## 36. Global UI Scale Increments

The normal settings UI should expose global UI scale in:

**5% increments**

from 75% through 200%.

The exact implementation may use a continuous slider that snaps/presents these values.

---

## 37. Global UI Scale Behaviour

Global scale applies coherently to:

- windows;
- controls;
- icons;
- tooltips;
- HUD base presentation;
- ordinary UI text.

It must preserve:

- aspect/proportion;
- shader edge quality;
- screen containment;
- navigability.

The UI should not simply render a low-resolution surface and scale it until blurry.

---

## 38. HUD Module Scale Interaction

Per-module HUD scaling remains:

- 80%;
- 90%;
- 100%;
- 110%;
- 125%;
- 150%.

Global UI scale and module scale are separate.

The implementation must constrain/combine them in a way that prevents essential controls becoming permanently inaccessible off-screen.

---

## 39. Independent Text Scale

The player may increase functional UI text independently from global UI scale.

Text Scale options:

- **100%**
- **125%**
- **150%**
- **175%**
- **200%**

The authored Radiant Slate font sizes remain the 100% baseline.

---

## 40. Text-Scale Layout Requirements

UI layouts must tolerate larger text through appropriate use of:

- flexible heights;
- wrapping;
- scrolling;
- reflow;
- wider tooltip layouts where needed.

Increasing text scale must not:

- clip important labels;
- hide button meaning;
- make settings unusable;
- overlap unrelated data without recovery.

---

## 41. Chat Text Size

Chat has an independent text-size setting.

Reference range at 1920×1080 / 100% UI scale:

**12–28 px**

This does not alter the underlying communication semantics.

---

## 42. Combat Log Text Size

Combat Log should support independent or shared chat-style text scaling.

It must remain readable without requiring Floating Combat Text.

---

## 43. Tooltips

Tooltip readability settings should support:

- tooltip text scaling;
- instant tooltip;
- normal delay;
- extended delay.

Exact delay values are tuning.

---

## 44. Stationary Tooltip Option

The accessibility/settings system should support a mode where tooltips appear in a stable authored screen position rather than following/repositioning around the pointer continuously.

This is particularly useful for users who find moving reading targets difficult.

---

## 45. Subtitles

The game must support subtitles for voiced dialogue/cinematic speech where such voice content exists.

Required options include:

- Subtitles Off/On;
- Subtitle Size;
- Speaker Names Off/On;
- Background Off/On;
- Background Opacity.

---

## 46. Subtitle Size

Subtitle size must support at least:

- 100%;
- 125%;
- 150%;
- 175%;
- 200%.

Subtitle presentation must remain within safe screen bounds.

---

## 47. Subtitle Readability

Subtitles should use:

- high-contrast text;
- configurable backing/background;
- sensible line wrapping;
- speaker identification where enabled.

Ordinary subtitle layout should avoid excessively long full-width lines.

Exact typography follows the Radiant Slate/UI system.

---

## 48. Closed Captions

Closed captions are distinct from dialogue subtitles.

Captions may describe important non-dialogue audio such as:

- warning bell;
- footsteps;
- roar;
- explosion;
- approaching threat;
- mechanical alarm;
- other gameplay-significant sound.

---

## 49. Gameplay Audio Equivalence

If gameplay-significant information is communicated through audio, a visual/textual equivalent must be available where practical.

Examples include:

- boss warnings;
- interruptible casts;
- stealth/detection cues;
- PvP objective changes;
- important environmental hazards.

---

## 50. Colour Must Not Be the Sole Critical Signal

Critical state must not depend on colour alone.

Examples include:

- hostile versus friendly;
- dangerous versus safe ground effects;
- buff versus debuff;
- PvP/FFA state;
- objective ownership;
- valid versus invalid interaction.

Use additional cues such as:

- icon;
- shape;
- pattern;
- border;
- label;
- animation;
- position.

---

## 51. Semantic Colour Overrides

Players should be able to customise key semantic UI colours where colour differentiation matters.

Candidate categories include:

- Friendly;
- Hostile;
- Neutral;
- Party;
- Raid;
- Guild;
- PvP;
- FFA;
- important telegraph categories.

Exact exposed colour set/picker implementation remains UI tuning.

---

## 52. Colour-Vision Presets

Colour-vision presets may be provided for convenience.

They are supplementary.

The primary accessibility strategy is:

- semantic UI differentiation;
- non-colour cues;
- optional semantic-colour overrides.

A crude whole-screen colour filter is not sufficient by itself.

---

## 53. Combat Telegraphs

Important combat telegraphs should use at least two complementary channels where practical.

Examples:

- world visual + sound;
- icon + text;
- ground shape + colour;
- cast bar + audio cue.

Disabling one accessibility-sensitive effect must not make the mechanic unreadable.

---

## 54. Ground Effects

Dangerous/beneficial ground effects should differ through more than red versus green.

Useful differentiation may include:

- edge shape;
- interior pattern;
- iconography;
- animation direction;
- intensity;
- outline style.

---

## 55. Reduce Flashing Effects

The client must provide:

**Reduce Flashing Effects: Off/On**

When enabled, rapid/intense flashes should be reduced or replaced with a less abrupt presentation.

Important gameplay results remain communicated.

---

## 56. Screen Flash

Full-screen damage/status flashes should be reducible or disableable.

If disabled, damage/status must remain visible through:

- unit frames;
- combat text;
- icons;
- other feedback.

---

## 57. VFX Intensity

The client should provide a player-facing VFX intensity/reduction option.

This may reduce:

- excessive particles;
- secondary decorative emissions;
- bloom-like effect intensity;
- nonessential persistent combat clutter.

It must not remove essential telegraph geometry/state.

Exact tiers/slider model remains graphics implementation tuning.

---

## 58. Reduce Screen Effects

A separate option should reduce intrusive screen-space effects such as:

- heavy vignette pulses;
- chromatic-style distortion if introduced;
- strong overlays;
- warping;
- rapid screen tints.

It should preserve readable state indication.

---

## 59. Floating Combat Text Accessibility

Floating Combat Text should support:

- scale;
- category filters;
- display duration;
- movement/animation reduction;
- critical-result emphasis Off/On.

The player may disable FCT entirely.

---

## 60. Combat Information Without FCT

Disabling Floating Combat Text must not remove access to combat information.

Alternative sources include:

- health/resource frames;
- cast bars;
- auras;
- combat log;
- explicit status/error text.

---

## 61. Motion-Reduced FCT

A motion-reduced mode should reduce:

- large vertical travel;
- lateral drift;
- excessive bounce;
- rapid scaling.

The result should remain readable without becoming static clutter.

---

## 62. Combat-State Alternatives

Important combat states should not rely on one sensory channel.

Examples include:

- stun;
- interrupt;
- silence;
- dispellable effect;
- incoming boss cast;
- PvP flag/FFA;
- low Health;
- objective capture.

At least one clear visual representation is required.

---

## 63. Audio Accessibility Boundary

The detailed mixer and sound-content design belong to Audio/Music.

Accessibility requires independent user volume controls for at least:

- Master;
- Music;
- Effects;
- Dialogue/Voice;
- UI;
- Ambience.

This allows players to suppress nonessential audio while retaining useful cues.

---

## 64. Audio Cannot Be the Only Required Cue

No core gameplay mechanic should require the player to hear a sound when no usable visual/text alternative exists.

This applies particularly to:

- combat;
- PvP;
- timed encounters;
- navigation-critical warnings.

---

## 65. Accessibility Settings Location

The game settings UI includes a clearly discoverable **Accessibility** category.

Relevant options may also appear in:

- Controls;
- Interface;
- Camera;
- Audio;
- Graphics;

when that improves discoverability.

They should cross-reference consistently rather than exist only in obscure locations.

---

## 66. No Single Accessibility Mode

The game does not rely on a single binary "Accessibility Mode".

Presets may be offered later for convenience, but every important option remains independently adjustable.

---

## 67. First-Run Accessibility Access

Accessibility and input settings must be reachable before entering normal gameplay.

A player should be able to change:

- UI/text size;
- subtitles;
- camera motion;
- bindings;

without first navigating a combat tutorial or entering the open world.

---

## 68. Settings Persistence Scope

Accessibility and physical input preferences are normally client/account-level.

They persist across characters.

Examples:

- keybindings;
- UI scale;
- text scale;
- subtitles/captions;
- colour settings;
- camera sensitivity;
- shake;
- VFX reduction.

---

## 69. Character-Specific Exceptions

Settings should be character-specific only when the preference is genuinely part of character gameplay configuration.

Examples include:

- action-bar contents;
- potentially character-specific HUD layout if the UI system later intentionally supports that distinction.

Physical accessibility requirements should not need to be reconfigured for every alt.

---

## 70. Settings Storage

The current `ClientSettingsManager` / PlayerPrefs layer is a valid prototype foundation for local settings.

Production settings should use a defined catalogue/schema rather than unrelated string keys invented independently by systems.

The implementation should support:

- defaults;
- versioning/migration;
- reset by category;
- change notifications.

Exact C# representation belongs to implementation architecture.

---

## 71. Settings Failure Behaviour

Invalid/corrupt local settings should fall back to safe defaults without losing unrelated settings where possible.

A bad keybinding setting must not wipe all accessibility preferences.

Reset/recovery should be explicit and granular.

---

## 72. Input Prompt Presentation

Where input prompts are shown, the UI should present the binding actually configured by the player rather than hard-coded text such as "Press E".

Prompts should update after rebinding.

Where multiple control schemes are active, prompts should follow the relevant/recent device.

---

## 73. Action-Bar Keybind Labels

Action-bar slots should display configured bindings using compact readable notation.

They must not assume the default `1–0,-,=` keyboard arrangement after the player remaps controls.

---

## 74. Targeting and Interaction Accessibility

Targeting/interact actions must be expressible through remappable semantic actions.

World interaction must not be accessible only through precision mouse clicking.

The game should support keyboard/controller targeting/interaction pathways appropriate to the combat model.

Exact target-cycle implementation remains combat/input design tuning.

---

## 75. Camera and World Interaction Refactor Requirement

Current direct `Mouse.current` and `Keyboard.current` polling in:

- `ClientInputController`;
- `CharacterCameraController`;

must be reconciled with semantic input actions for bindable behaviour.

This is required for:

- remapping;
- controller support;
- accessibility devices;
- consistent input-mode gating.

---

## 76. Action-Bar Input Refactor Requirement

Current `ClientInputController` binds only Bar 0 slots 1–12.

The input system must expand to support the multi-bar action model defined by UI/Abilities.

Bindings should target semantic action-bar slots/actions rather than one hard-coded first bar.

---

## 77. Gamepad Foundation

The existing Input Actions asset already contains a Gamepad control scheme and UI navigation bindings.

These are a useful foundation, but they do not satisfy complete controller support by themselves.

Gameplay actions, prompts, world interaction and camera behaviour must all become control-scheme independent.

---

## 78. Accessibility and Competitive Integrity

Accessibility settings may alter presentation and control method.

They must not:

- automate gameplay decisions;
- reveal hidden information;
- increase authoritative detection/range;
- bypass targeting/line-of-sight rules;
- create server-side combat advantages beyond equivalent accessible input.

Readable alternative presentation is not considered an unfair advantage.

---

## 79. Multiplayer Determinism

Accessibility/input settings are client presentation/control preferences.

They do not alter server-authoritative:

- movement limits;
- ability timing;
- cooldowns;
- combat range;
- PvP eligibility;
- interaction rules.

A different input method must produce the same authoritative gameplay request semantics.

---

## 80. Open / Deferred Details

The following remain implementation/tuning decisions:

- exact default mouse sensitivities;
- exact controller dead-zone ranges;
- exact controller action-bar layout;
- exact gamepad button defaults;
- exact keyboard default map beyond current baseline;
- exact Hold/Toggle catalogue;
- exact tooltip delay values;
- exact semantic colour override UI;
- exact colour-vision preset palette;
- exact VFX reduction tiers;
- exact caption taxonomy;
- exact subtitle max width/line count;
- whether HUD layout is account-wide or optionally character-specific;
- exact settings-profile/cloud-sync behaviour;
- exact accessibility-controller certification/support.

These do not change the product architecture.

---

## 81. Locked Design Decisions

The following decisions are locked by this PDD:

1. Keyboard + mouse is the reference control scheme.
2. Gameplay/input logic is semantic-action based rather than device-key based.
3. Bindable gameplay actions must route through the input-action system.
4. Ordinary gameplay and interface actions are remappable.
5. Bindable actions support at least two user bindings where input type permits.
6. Modifier-key bindings are supported.
7. Mouse side buttons can be bound.
8. Binding conflicts are detected and explained.
9. Duplicate bindings may be retained where the player deliberately chooses them.
10. Individual/category/all binding reset is supported.
11. Physical input bindings persist across characters.
12. Text-input/UI/gameplay modes must not trigger unrelated actions.
13. Ordinary game menus are keyboard navigable.
14. UI focus must be clearly visible.
15. Focus order should be logically authored.
16. Critical menu interaction should not depend solely on precision pointer drag/drop.
17. The architecture must support complete controller operation.
18. Controller support must not reduce the normal MMO action-bar system to a small permanent loadout.
19. Input devices can be switched without restarting.
20. Controller look/dead-zone/inversion settings are supported when controller gameplay is delivered.
21. Horizontal and vertical camera sensitivities are separate.
22. Horizontal and vertical look inversion are independently configurable.
23. Camera zoom sensitivity is configurable.
24. Automatic camera recentering can be disabled.
25. Camera recenter delay and speed are configurable.
26. Camera shake supports 0–100%, including complete disable.
27. Camera bob/sway can be disabled if present.
28. Motion blur can be disabled independently.
29. Other substantial involuntary camera effects should expose reduction options.
30. Hold/Toggle alternatives are supported where the action reasonably permits both.
31. Core gameplay should avoid unnecessary rapid-input accessibility gates.
32. Global UI scale supports 75–200%.
33. Global UI scale uses 100% default.
34. The normal UI exposes 5% scale increments.
35. Global UI scale is independent from HUD module scale.
36. Functional text can scale independently to 200%.
37. Text-scale options are 100/125/150/175/200%.
38. Chat supports independent text sizing, with a 12–28 px reference range.
39. Tooltip text/delay accessibility options are supported.
40. A stationary-tooltip presentation option is supported.
41. Subtitles can be enabled/disabled.
42. Subtitle scale supports at least 200%.
43. Speaker names can be toggled.
44. Subtitle background and opacity are configurable.
45. Closed captions for important non-dialogue audio are supported.
46. Gameplay-significant audio should have a visual/text alternative where practical.
47. Critical information cannot rely on colour alone.
48. Semantic UI colours can be made customisable where differentiation matters.
49. Colour-vision presets may supplement but not replace non-colour cues.
50. Important combat telegraphs should use more than one information channel where practical.
51. Dangerous/beneficial ground effects must not differ only by colour.
52. Reduce Flashing Effects is supported.
53. Full-screen flashes can be reduced/disabled.
54. VFX intensity/reduction is supported.
55. Intrusive screen-space effects can be reduced.
56. FCT supports scale/filter/duration/motion options.
57. FCT can be disabled without removing authoritative combat information.
58. Critical combat states retain clear visual representation.
59. Independent Master/Music/Effects/Dialogue/UI/Ambience volume controls are required.
60. Core gameplay must not require sound as the only usable information source.
61. Accessibility settings are clearly discoverable.
62. The game does not rely on one binary Accessibility Mode.
63. Accessibility/input settings are reachable before entering ordinary gameplay.
64. Accessibility/input preferences are normally account/client-level.
65. Production settings use a defined schema/catalogue rather than unrelated arbitrary keys.
66. Input prompts reflect the player's actual bindings.
67. Action-bar key labels reflect configured bindings.
68. World targeting/interaction must have semantic remappable input pathways.
69. Current direct device polling for bindable camera/world behaviour must be refactored.
70. Multi-bar action input must replace first-bar-only hard-coding.
71. Accessibility settings cannot alter server-authoritative gameplay rules.
72. Equivalent accessible control methods produce the same authoritative gameplay semantics.

---

## 82. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Communication Systems PDD](Communication-Systems-PDD.md);
- [PvP PDD](PvP-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [Graphical Approach PDD](../Graphical-Approach-PDD.md);
- [Audio and Music PDD](Audio-and-Music-PDD.md).

---

## 83. Validation Criteria

The Accessibility and Input design is correctly implemented when:

1. Default keyboard/mouse gameplay works entirely through semantic player actions.
2. Rebinding Move/Jump/Interact/actions changes behaviour without code changes to gameplay systems.
3. Camera/world interaction no longer depends on hard-coded physical mouse/keyboard controls for bindable actions.
4. A player can bind eligible actions to mouse side buttons.
5. A player can use modifier bindings.
6. A player can maintain two bindings for an eligible action.
7. Binding conflicts are visible before/after applying them.
8. A player can clear/reset bindings without resetting unrelated settings.
9. Entering chat/text input cannot accidentally cast bound abilities.
10. Inventory/Character/Talents/Quest/Reputation/Settings can be navigated by keyboard.
11. Focus is always visually identifiable.
12. Grid-based UI navigation behaves predictably.
13. Complete controller gameplay can be layered on the same action architecture without replacing core systems.
14. Device prompts update to the configured/recent input.
15. Camera horizontal/vertical sensitivity can be changed independently.
16. Look axes can be inverted independently.
17. Camera auto-recentering can be disabled.
18. Recenter timing can be changed.
19. Camera shake can be set to 0%.
20. Motion blur can be disabled.
21. Global UI scale can be set anywhere from 75–200% in the supported steps.
22. 200% UI scale remains usable and does not permanently hide core controls.
23. Text can be set to 200% while preserving functional UI access.
24. Chat can be enlarged independently.
25. Tooltips can use a longer/instant delay configuration.
26. Tooltips can use a stable screen location.
27. Subtitles can be shown with enlarged text and an opaque/semi-opaque backing.
28. Important non-dialogue sounds can be captioned.
29. Hostile/friendly or dangerous/safe critical states remain distinguishable without relying solely on colour.
30. A user can reduce/disable flashing effects.
31. A user can reduce intrusive screen-space effects.
32. VFX reduction preserves essential telegraph information.
33. FCT can be motion-reduced or disabled.
34. Combat remains understandable through frames/cast bars/auras/combat log when FCT is disabled.
35. Audio categories can be mixed independently.
36. Muting one category does not make a core mechanic unreadable when a visual alternative is required.
37. Accessibility settings are available before entering the game world.
38. Settings apply across characters.
39. Action-bar labels display current bindings rather than hard-coded default keys.
40. The first action bar is no longer the only action bar addressable by the input architecture.
41. Accessibility/input preferences do not alter server-authoritative combat/movement/PvP rules.

---

## 84. Design Summary

Ninth Age treats accessibility and input as architectural requirements.

**The player chooses how information is read and how actions are triggered; the server still receives the same authoritative gameplay intent.**

Keyboard and mouse remain the reference control scheme, but gameplay is defined through semantic Input Actions rather than hard-coded physical devices. Controls are remappable, support multiple/modifier bindings, and ordinary UI remains keyboard navigable while the architecture supports complete controller operation.

Radiant Slate remains the authored visual baseline while global UI scaling reaches 200% and text can scale independently. Critical gameplay information cannot depend solely on colour, sound, flashing or camera motion.

Players can reduce camera shake, recentering, motion blur, flashing, VFX clutter, screen effects and FCT movement without losing the information required to play.

Accessibility/input preferences persist across characters and are available before the player enters ordinary gameplay.

The intended result is:

> **Accessibility options should change how the player receives information and supplies input, not what the game rules allow them to do.**
