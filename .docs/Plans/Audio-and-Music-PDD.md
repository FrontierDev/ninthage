# Ninth Age — Audio and Music Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** World ambience, environmental and combat audio, UI sound, voice presentation, spatialisation, concurrency, acoustics, music structure, zone/dungeon music, day-night/weather response, authoring and runtime constraints  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended audio and music model for **Ninth Age**.

It is authoritative for:

- player-facing audio categories;
- 2D versus spatial audio;
- ambience and environmental emitters;
- footsteps and movement audio;
- combat and ability sound structure;
- audio variation, concurrency, priority and attenuation;
- occlusion and acoustic-space direction;
- UI sound;
- dialogue/voice presentation;
- music behaviour;
- day/night and weather audio;
- procedural-audio authoring boundaries.

Gameplay rules remain owned by their gameplay PDDs. Subtitle/caption accessibility is owned by the [Accessibility and Input PDD](Accessibility-and-Input-PDD.md). UI visual presentation is owned by the [UI and UX PDD](UI-and-UX-PDD.md).

Where implementation conflicts with this document, this PDD defines intended player-facing behaviour.

---

## 2. Design Pillars

### 2.1 Audio serves world presence and gameplay readability

Audio should make places feel physically present while making actions and important events easier to understand.

### 2.2 Large-scale combat must remain readable

Ninth Age is an MMORPG. The audio system must prioritise important information rather than playing every possible sound at equal prominence.

### 2.3 Music is sparse, location-led and composition-led

The music structure should resemble **classic WoW in approach**, not modern retail-style adaptive scoring.

The reference is structural only. Ninth Age must use original compositions and its own musical identity.

### 2.4 Silence and ambience are valuable

The soundtrack should not run continuously. Long periods of world ambience without music are intentional.

---

## 3. Audio Categories

The player-facing mixer must provide at least:

- **Master**
- **Music**
- **Effects**
- **Dialogue / Voice**
- **UI**
- **Ambience**

Internal mixer routing may subdivide Effects into combat, footsteps, world SFX or similar.

Master volume scales the complete mix without destroying the player's category settings.

---

## 4. Runtime Audio Direction

Unity's native audio stack is the initial runtime direction.

The project does not require FMOD, Wwise or another external middleware package.

Audio assets should use the existing Addressables content architecture.

The existing `ClientAudioManager` provides useful foundations for:

- Addressable loading;
- async loading;
- clip caching;
- simple one-shot playback.

It is not the final production audio architecture.

---

## 5. 2D and 3D Audio

Use 2D/non-spatial playback for:

- UI sounds;
- music;
- major system notifications;
- selected local player-feedback layers.

Use 3D spatial playback for world-originating sounds such as:

- footsteps;
- players and creatures;
- weapon swings and impacts;
- spell casts and impacts;
- doors;
- machinery;
- fires;
- waterfalls;
- rivers;
- local NPC chatter.

A player action may combine a world-space sound with a restrained local feedback sound where necessary for readability.

---

## 6. World Ambience

World ambience should be assembled from authored context rather than one permanent loop per zone.

Conceptually:

```text
Region / biome ambience
+ time-of-day context
+ weather context
+ settlement / wilderness context
+ local spatial emitters
```

Ambient sound may use:

- subtle looping beds;
- intermittent one-shots;
- local spatial emitters.

Ambient density should remain restrained.

---

## 7. Day and Night

The day/night cycle must affect environmental sound.

Authored day/night differences may include:

- wildlife;
- insects;
- settlement activity;
- ambient one-shots;
- ambient beds.

Night should not simply be the daytime soundscape at lower volume.

Music may also use different complete-track pools for day/night where specifically authored.

---

## 8. Weather

Weather should have audible presence through appropriate combinations of:

- rain;
- wind;
- storm;
- thunder;
- snow/winter wind;
- other authored weather.

Weather audio should blend with the existing world ambience.

Thunder should behave as world audio rather than a UI notification and may use distance/direction/timing appropriate to the authored effect.

---

## 9. Local Environmental Emitters

World objects may emit spatial ambience or SFX, including:

- waterfalls;
- rivers;
- fountains;
- forges;
- campfires;
- taverns;
- magical rifts;
- machinery;
- crowds;
- livestock.

These sounds should attenuate with distance and cooperate with streamed scene loading.

Loading/unloading world chunks must not produce persistent duplicate loops or obvious avoidable popping.

---

## 10. Interiors and Acoustic Spaces

Moving between exterior and interior spaces should support coherent audio transitions.

Examples include:

- rain becoming muffled indoors;
- tavern ambience replacing street ambience;
- caves and stone halls sounding more enclosed.

The system should support simple authored acoustic/reverb contexts such as:

- open exterior;
- forest;
- cave;
- stone hall;
- cathedral;
- tavern;
- underground chamber.

Transitions should blend.

Physically exhaustive acoustic simulation is not required.

---

## 11. Footsteps

Footsteps are a primary world-presence system.

Selection should consider:

- movement state;
- surface family;
- actor/creature type where relevant.

Initial surface families should include at least:

- dirt;
- grass;
- stone;
- wood;
- metal;
- shallow water;
- snow;
- sand.

Repeated footsteps must support sufficient clip/pitch/volume variation to avoid mechanical repetition.

---

## 12. Repeated-Sound Variation

Frequently repeated sound families should support:

- multiple clips;
- small pitch variation;
- small volume variation;
- weighted/random selection;
- minimum repeat intervals.

This applies particularly to:

- footsteps;
- weapon impacts;
- spell impacts;
- creature vocalisations;
- UI sounds;
- ambient one-shots.

---

## 13. Combat Audio

Combat sound prioritises readability rather than raw density.

A useful importance order is:

1. player-critical warnings;
2. the player's own important action feedback;
3. important target/boss actions;
4. nearby hostile actions;
5. nearby friendly actions;
6. low-priority repeated combat texture.

Exact numerical priorities are implementation tuning.

---

## 14. Ability Audio Events

Abilities should be capable of defining audio for relevant phases such as:

- cast start;
- cast/channel loop;
- cast completion;
- projectile/travel;
- impact;
- aura application;
- aura removal;
- periodic tick.

Not every ability needs every phase.

Abilities may reuse coherent sonic families such as elemental or martial families while iconic abilities may add bespoke sounds.

---

## 15. Weapon Combat

Weapon audio should distinguish meaningful combat results such as:

- swing;
- hit;
- critical impact where useful;
- block;
- parry;
- miss/dodge.

Impact audio may use broad weapon and target-material/armour families without creating a combinatorial asset explosion.

Resolved outcome audio must correspond to authoritative gameplay results.

---

## 16. Concurrency and Priority

Audio definitions must support:

- maximum simultaneous instances;
- priority;
- voice-stealing/replacement rules;
- repeat cooldowns;
- distance-based culling.

When the voice budget is saturated, low-priority repeated sounds should disappear before critical player/encounter feedback.

Large raids, world bosses, busy cities and large PvP fights must not create unlimited audio voices.

---

## 17. Spatial Attenuation and Occlusion

Spatial sound definitions must support authored:

- reference/minimum distance;
- maximum audible distance;
- rolloff behaviour.

The system should support **simple occlusion** for appropriate sounds.

Occlusion may reduce:

- volume;
- high-frequency content.

Occlusion must remain economical enough for MMORPG-scale scenes. Full physical acoustic simulation is not required.

---

## 18. UI Audio

Radiant Slate uses a restrained UI sound language.

Useful families include:

- hover/select;
- confirm;
- cancel/back;
- tab;
- drag/drop;
- error;
- important notification;
- item/equipment action;
- window open/close where useful.

The UI should not produce a sound for every trivial visual state change.

Muting UI sound must not remove necessary visual feedback.

---

## 19. Voice Acting

Ninth Age does **not** require full voice acting for every quest/dialogue line.

Text remains a first-class narrative medium.

Selective voice acting may be used for:

- major narrative scenes;
- important NPC introductions;
- important quest moments;
- bosses;
- combat barks;
- greetings/farewells;
- ambient settlement chatter;
- ceremonies/events.

Meaningful voiced dialogue must support subtitles according to Accessibility/Input.

---

## 20. NPC and Player Vocalisations

NPC ambient chatter should be:

- spatial;
- varied;
- proximity-aware;
- concurrency-limited;
- cooldown-limited.

Player characters may vocalise:

- damage;
- death;
- exertion;
- emotes;
- selected reactions.

Players should not constantly shout every ability.

---

## 21. Music Model

Ninth Age uses **complete authored musical compositions**.

Music is selected primarily from authored location/context such as:

- region;
- zone;
- settlement;
- special interior;
- dungeon;
- special scripted location;
- time-of-day pool where authored.

A context may contain one or several complete tracks.

---

## 22. No Adaptive Music

There is **no general adaptive music system**.

Ordinary music must not dynamically respond to:

- aggro;
- threat;
- Health;
- number of enemies;
- ordinary combat state;
- moment-to-moment encounter intensity.

There is no default exploration-to-tension-to-combat music state machine.

---

## 23. No Retail-Like Runtime Stem Layering

Ordinary soundtrack playback must not dynamically assemble music from:

- combat stems;
- threat stems;
- percussion intensity layers;
- health/intensity layers;
- similar reactive components.

Tracks may be internally orchestrated however the composer wishes, but runtime playback treats normal soundtrack content as complete compositions.

---

## 24. Music Frequency

World music should be intermittent rather than continuous.

The ordinary pattern is:

```text
Eligible location track
→ complete authored composition
→ music stops
→ ambience-only period
→ later eligible track
```

Exact silence/gap durations are content tuning.

The game should deliberately spend meaningful time with no music playing.

---

## 25. Track Selection

Locations with multiple tracks may use simple authored selection such as:

- random;
- weighted random;
- sequence/rotation.

The system should avoid immediate repetition where alternatives exist.

Small streamed world-chunk boundaries must not restart or change music.

---

## 26. Location Transitions

Entering a materially different music context may:

- allow the current track to finish;
- fade it out;
- select a track from the new location;
- remain silent until the next eligible cue.

The context authoring decides the appropriate rule.

Rapid switching at small or ambiguous boundaries should be avoided.

---

## 27. Zone and Settlement Music

Regions/zones should develop recognisable musical identity using recurring:

- instrumentation;
- motifs;
- harmonic language;
- cultural associations.

Settlements may use:

- dedicated complete tracks;
- calmer tracks;
- music related to the broader region.

Major cities may have particularly recognisable themes.

---

## 28. Dungeon Music

Dungeons may define complete tracks for:

- general exploration;
- particular areas/wings;
- major encounters;
- resolution.

Dungeons do not need a music transition for every ordinary trash pull.

Where geographically connected to the world, dungeon music may reuse musical language from the surrounding region.

---

## 29. Boss and Scripted Music

Major bosses and major scripted narrative moments may explicitly trigger a complete authored music track.

This is event-triggered track selection, **not adaptive scoring**.

Boss phase changes do not require stem layering.

If a specific encounter needs a music change, it may use:

- another complete track;
- a short stinger;
- a one-shot transition cue.

---

## 30. PvP Music

Open-world PvP does not automatically replace location music because:

- an enemy player is nearby;
- combat begins;
- the player becomes PvP flagged.

Contest zones may have their own normal location soundtrack.

Future battlegrounds/arenas may use:

- complete intro/background tracks;
- victory/defeat cues;
- match stingers.

They still do not require adaptive stem layering.

---

## 31. Music and World Streaming

Music context exists above individual streamed terrain/content chunks.

Loading/unloading additive scenes must not restart soundtrack playback merely because a chunk boundary was crossed.

Detached-map transitions may use loading music where appropriate.

---

## 32. Procedural Sound Generator

The existing procedural sound-generator plan remains an **editor authoring tool**.

It may generate assets such as:

- UI sounds;
- spell impacts;
- rift effects;
- elemental effects;
- other short SFX.

Generated output becomes an ordinary imported audio asset.

---

## 33. No Runtime Procedural Synthesis Requirement

Ordinary runtime SFX must not depend on regenerating procedural PCM.

Procedural recipes may be retained for:

- reproducibility;
- variants;
- iteration.

They are authoring metadata, not gameplay state.

---

## 34. Data-Driven Audio Definitions

Designers should be able to author sound definitions with properties such as:

- clip/variant set;
- routing category;
- 2D/3D mode;
- volume/pitch variation;
- attenuation;
- priority;
- concurrency;
- repeat cooldown;
- occlusion response;
- caption metadata where applicable.

Gameplay systems should refer to stable audio definitions/events rather than embedding arbitrary clip paths throughout unrelated code.

---

## 35. Network and Authority

The network communicates gameplay semantics/state, not audio streams.

Examples:

- ability result;
- NPC action;
- weather state;
- world event.

The client then plays the appropriate local audio.

Harmless local differences such as cosmetic clip variation or culled low-priority sound do not affect gameplay authority.

---

## 36. Performance and Caching

The production audio system must remain stable during:

- raids;
- world bosses;
- busy settlements;
- large PvP fights.

This requires:

- concurrency limits;
- priority;
- distance culling;
- efficient spatial emitters;
- appropriate cache/streaming behaviour.

Caching must not grow without bound.

Large music assets should use a memory-appropriate loading/streaming strategy.

---

## 37. Accessibility

The Accessibility/Input PDD is authoritative for:

- independent volume categories;
- subtitles;
- closed captions;
- visual equivalents for important sound cues.

Core gameplay must not require a sound to be heard when no usable visual/text alternative exists.

Gameplay-significant non-dialogue sounds that require captions should have authored caption/event metadata rather than inferred runtime text.

---

## 38. Current Implementation Changes Required

The current `ClientAudioManager` must evolve beyond:

- one global non-spatial SFX source;
- a small fixed clip cache;
- simple one-shot playback.

Production audio requires responsibilities for:

- UI/2D playback;
- spatial world playback;
- combat sound;
- ambience;
- music context;
- dialogue/voice;
- mixer routing;
- priority/concurrency;
- Addressable loading/cache.

These responsibilities do not need to exist in one monolithic class.

---

## 39. Open / Deferred Details

The following remain content, balance or technical tuning:

- exact voice/concurrency budgets;
- exact priority values;
- exact attenuation curves;
- exact occlusion implementation;
- exact reverb settings;
- exact music gap durations;
- track catalogues;
- composers/instrumentation;
- exact voice-acting coverage;
- exact footstep variant counts;
- exact music fade durations;
- exact mixer hierarchy beyond required player categories;
- exact cache/memory budgets;
- exact AudioClip streaming/import settings.

These do not change the architecture defined here.

---

## 40. Locked Design Decisions

1. Player-facing audio controls include Master, Music, Effects, Dialogue/Voice, UI and Ambience.
2. Unity native audio is the initial runtime direction.
3. External audio middleware is not required.
4. Runtime audio assets integrate with Addressables.
5. UI/music may use 2D playback; world-originating sound normally uses 3D playback.
6. World ambience is context-layered rather than one permanent loop per zone.
7. Day/night affects environmental sound.
8. Weather has audible world presence.
9. Local environmental emitters are spatial.
10. Footsteps use surface families and repeated-sound variation.
11. Combat audio uses importance/concurrency rather than unlimited equal-priority playback.
12. Audio definitions support attenuation, priority, concurrency and repeat controls.
13. Simple audio occlusion and authored acoustic spaces are supported.
14. UI audio is restrained and never the sole carrier of state.
15. Full voice acting for all dialogue is not required.
16. Selective voice acting is supported.
17. Meaningful voiced dialogue supports subtitles.
18. Music follows a classic-WoW-like structural philosophy while remaining original to Ninth Age.
19. Music is primarily location/context driven.
20. Normal soundtrack content uses complete authored compositions.
21. World music is intermittent and includes substantial ambience-only periods.
22. There is no general adaptive music state machine.
23. Ordinary combat does not automatically trigger combat music.
24. Normal music is not dynamically assembled from retail-like combat/threat/intensity stems.
25. Zones, settlements and dungeons may define pools of complete tracks.
26. Day/night may change eligible complete-track pools.
27. Streamed world-chunk boundaries do not restart music.
28. Major scripted events/bosses may explicitly select complete tracks.
29. Boss phase music changes may use complete tracks or stingers, not required adaptive layering.
30. Dungeons do not require music changes for every trash pull.
31. Open-world PvP does not automatically trigger combat music.
32. Future battleground/arena music uses complete tracks/cues without requiring adaptive stems.
33. The procedural sound generator is an editor authoring tool.
34. Generated procedural SFX become ordinary runtime audio assets.
35. Runtime ordinary SFX do not depend on procedural PCM generation.
36. Gameplay transmits semantic events/state rather than audio streams.
37. Audio must remain performant under MMORPG-scale concurrency.
38. Important gameplay audio must support accessibility alternatives.

---

## 41. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [Accessibility and Input PDD](Accessibility-and-Input-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- [PvP PDD](PvP-PDD.md);
- [Graphical Approach PDD](../Graphical-Approach-PDD.md).

---

## 42. Validation Criteria

The design is correctly implemented when:

- all required volume categories work independently;
- world-originating sounds use sensible spatial playback;
- footsteps vary by major surface family;
- repeated high-frequency sounds do not become obvious identical machine repetition;
- large combat culls redundant low-priority audio before critical feedback;
- occlusion/acoustic contexts can distinguish interiors and exteriors;
- day/night/weather alter the world soundscape;
- UI remains understandable with UI audio muted;
- important voiced dialogue has subtitles;
- normal world music consists of complete authored tracks;
- ordinary combat does not automatically change music;
- no normal soundtrack requires reactive musical stems;
- zones can contain multiple complete tracks and deliberate music-free intervals;
- chunk streaming does not restart soundtrack playback;
- major bosses can explicitly trigger complete tracks;
- PvP combat does not automatically replace location music;
- procedural editor-generated sounds play as ordinary runtime assets;
- audio remains stable in raids, settlements, world bosses and large PvP fights.

---

## 43. Design Summary

Ninth Age uses spatial, varied and priority-aware sound to make its world tangible and its combat readable.

Music deliberately follows a sparse, place-driven model inspired structurally by classic WoW:

- complete compositions;
- recognisable regional identity;
- simple track pools;
- meaningful silence;
- ambience between musical appearances;
- explicit complete tracks for major moments where appropriate.

It explicitly avoids modern retail-style adaptive soundtrack layering.

> **The world should not constantly score the player. Music should have enough space around it to become part of the memory of a place.**
