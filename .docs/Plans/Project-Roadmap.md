# Ninth Age — Project Roadmap

**Status:** Living implementation roadmap  
**Project:** Ninth Age  
**Branch:** `dev`  
**Scope:** Dependency-ordered delivery plan from the current working prototype through production readiness  
**Last updated:** 2026-10-07

---

## 1. Purpose

This document defines the implementation roadmap for Ninth Age.

It does not replace the MMORPG Master PDD or subsystem PDDs. Product behaviour remains owned by those documents. This roadmap determines **when** approved systems should be implemented, integrated, hardened and expanded.

The project is not a blank slate. The current codebase already includes working foundations for:

- graphical client and headless server execution;
- PurrNet networking;
- shared runtime actors and gameplay definitions;
- character creation and world entry;
- movement and interest management;
- additive world streaming;
- combat, spells, auras, cooldowns and auto-attacks;
- server ECS-backed simulation;
- inventory, equipment, stats and progression;
- NPC spawning and basic behaviour;
- Addressables;
- world-authoring/editor tooling;
- UI systems;
- prototype persistence.

The roadmap therefore prioritises **stabilising and generalising existing systems before adding breadth**.

---

## 2. Roadmap Principles

1. **Working behaviour is preserved unless a deliberate change is required.**
2. **Architecture cleanup precedes large feature growth where inconsistency would otherwise multiply.**
3. **Production-critical foundations are implemented early when postponing them would force broad rewrites later.**
4. **Every major phase ends in a playable or operational milestone.**
5. **Systems are validated in multiplayer, not only in isolated editor tests.**
6. **The headless server remains authoritative for important gameplay state.**
7. **The world-runtime architecture is completed and generalised rather than replaced without a separate architectural review.**
8. **Content pipelines must scale before large quantities of content are authored.**
9. **Performance is tested under representative MMORPG load, not empty-scene conditions.**
10. **A subsystem PDD must be sufficiently resolved before implementation treats its behaviour as permanent.**

---

## 3. Development Model

Repository work is performed on `dev`.

`main` represents the stable integration line and should only receive changes that have completed the relevant validation for their milestone.

Work should be organised into focused implementation issues rather than large mixed-system changes.

Each roadmap phase may contain parallel workstreams, but its exit milestone should only be declared complete when the integrated build satisfies the phase exit criteria.

---

# Phase 0 — Codebase Stabilisation and Consistency

## Goal

Make the existing working codebase easier to extend safely before system count and cross-system dependencies increase substantially.

## Work

### C# architecture and style

- Implement the C# Restyling Plan.
- Complete namespace, filename, field and acronym consistency work.
- Standardise networking terminology:
  - genuine PurrNet RPC surfaces use `*Endpoint`;
  - shared in-process event surfaces use an event-oriented name where appropriate;
  - server cross-layer wiring uses `*Bindings`;
  - long-lived subsystem owners remain `*Manager`;
  - server domain capabilities remain `*Service`.
- Keep the terminology cleanup behaviour-preserving.

### Event and lifecycle hygiene

- Correct obviously ineffective event unsubscription patterns.
- Add missing lifecycle cleanup where subscriptions can outlive their owner.
- Prevent accidental duplicate binding registration.
- Review dead callback fields and remove only those proven unused repository-wide.

### Validation foundation

- Establish automated compilation/tests for Shared, Client, Server and Editor code.
- Run server-specific tests in CI where practical.
- Add focused regression coverage around:
  - login and world entry;
  - actor spawn/despawn;
  - movement;
  - combat;
  - persistence round-trips;
  - world streaming.

### Repository hygiene

- Remove confirmed obsolete code and stale test scaffolding.
- Keep technical architecture documentation aligned with the post-restyle structure.

## Exit milestone — Stable Foundation

A client can connect to the headless server, create/select a character, enter the world, move, interact with the existing combat/runtime systems and disconnect without regressions introduced by the cleanup.

---

# Phase 1 — Production Technical Foundations

## Goal

Replace prototype-only infrastructure that would become expensive to change after more gameplay systems depend on it.

## Work

### Persistence

Implement the Account, Character and Persistence PDD production direction:

- immutable AccountID and CharacterID;
- relational server persistence;
- explicit schema and migrations;
- transactional ownership/progression changes;
- character roster persistence;
- inventory/equipment persistence;
- progression persistence;
- quest/reputation/profession persistence as those systems come online;
- persistent world position;
- disconnect/reconnect continuity;
- deletion/restoration foundations;
- migration away from whole-file JSON persistence.

### Session and authentication architecture

- Enforce one authoritative session per account.
- Implement reconnect/disconnect-grace behaviour.
- Separate account creation policy from authentication where required by the PDD.
- Harden server-side validation of ownership and character selection.

### Release and compatibility identity

Implement the first production versioning baseline from the Live Content and Versioning PDD:

- client build version;
- server build version;
- network protocol version;
- gameplay-content revision;
- Addressables catalog revision;
- database-schema version;
- release manifest;
- compatibility handshake before world entry;
- fail-closed behaviour for incompatible authoritative state.

### Addressables and live-content foundation

- Enable the intended remote catalog/content-update workflow.
- Define remote content ownership and bundle-grouping rules.
- Remove `Resources.Load`/direct-reference authority from content intended for live hotfixing.
- Establish immutable content revision output.
- Validate content update size and dependency behaviour.

### Operational foundations

- Structured server logging.
- Clear environment configuration for development/production.
- Persistent-data backup/restore workflow.
- Server startup validation for required content/schema versions.

## Exit milestone — Durable Character

A character can be created, saved to the production-style persistence layer, enter the world, change meaningful state, disconnect/reconnect and survive a server restart without losing or exploiting state. Incompatible client/content/schema combinations are rejected explicitly.

---

# Phase 2 — Core Playable Vertical Slice

## Goal

Prove the complete fundamental MMORPG gameplay loop in one deliberately small content slice before broad content production.

## Work

### Character

- Finalise the implemented Level 1 character creation path.
- Apply race/class starting state from authoritative definitions.
- Complete core stat/resource calculation.
- Complete level/XP progression required for the slice.
- Complete learned ability/rank and talent foundations required for the slice.
- Ensure all relevant progression persists.

### Movement and traversal

- Complete baseline walking/running/jumping/falling/swimming behaviour required by the Movement PDD.
- Harden client-authoritative/server-validated movement.
- Validate correction behaviour under latency.
- Ensure movement interacts correctly with combat and world streaming.

### Combat

Complete the reusable combat substrate:

- hard targeting;
- range/facing/line-of-sight validation;
- auto-attacks;
- cast/channel execution;
- GCD and cooldowns;
- damage/healing resolution;
- hit/crit/avoidance/mitigation;
- auras and periodic effects;
- interrupts;
- crowd-control representation;
- threat;
- death and resurrection baseline;
- combat log and essential feedback.

### NPCs and AI

- Data-driven NPC definitions.
- Behaviour Definitions.
- perception/aggro;
- threat-driven target selection;
- melee/ranged/caster baseline behaviours;
- navigation and positioning;
- leash/reset/evasion;
- spawn/despawn/respawn;
- loot source integration.

### Items

- inventory;
- equipment;
- item requirements;
- durability;
- authored loot tables;
- basic loot ownership;
- vendors/repair where required for the loop;
- persistence.

### UI

Provide the minimum complete player-facing interface for the slice:

- login/character selection;
- HUD;
- target frame;
- action bars;
- cast bars;
- auras;
- combat text/log;
- inventory;
- equipment;
- character statistics;
- tooltips;
- basic settings.

## Vertical-slice content

Use a deliberately constrained content set:

- one small playable region;
- one settlement or service area;
- several ordinary creature archetypes;
- at least one elite encounter;
- enough authored items to exercise equipment progression;
- enough abilities to exercise the combat architecture;
- a short progression path rather than a full levelling experience.

## Exit milestone — Playable MMORPG Loop

A player can log in, enter a streamed multiplayer world, fight server-authoritative NPCs, gain progression, obtain/equip loot, die/recover, use abilities and retain all meaningful state after relogging.

The vertical slice must be playable as a game rather than as disconnected system demonstrations.

---

# Phase 3 — World Runtime and Content Pipeline

## Goal

Prove that Ninth Age can build and run the large continuous world it is designed around.

## Work

### World runtime

Complete/generalise the World Runtime and Instancing PDD architecture:

- authoritative double-precision world position;
- floating origin;
- origin-cell transitions;
- 256 × 256 world-tile streaming;
- additive environment scenes;
- actor interest independent from environment streaming;
- seamless tile boundaries;
- seamless public interiors;
- private seamless instance support;
- detached-map instance support;
- instance identity and lifecycle;
- group-aware transitions;
- reconnect into the correct runtime context.

### World authoring

Productionise the World Editor and associated tooling:

- terrain generation/import;
- terrain-tile placement;
- mesh-overlay workflow;
- biome/zone authoring;
- environment-object chunk authoring;
- spawn authoring;
- NPC/service placement;
- interior entrances/transitions;
- navigation generation;
- validation of Addressable scene/chunk metadata.

### Presentation

- Day/night cycle.
- Weather foundation.
- lighting/atmosphere compatible with the Graphical Approach PDD;
- terrain and environment LOD;
- vegetation/prop density rules;
- VFX/SFX distance/culling foundations.

### Performance budgets

Define and enforce budgets for:

- loaded world tiles;
- visible actors;
- server actor count;
- AI update cost;
- network bandwidth;
- CPU frame time;
- GPU frame time;
- memory;
- Addressable load/unload churn.

## Exit milestone — One Complete Zone

A representative zone can be traversed continuously, including settlement, wilderness and at least one seamless interior, with stable world streaming, floating origin, actor interest and representative visual density.

---

# Phase 4 — PvE Content Systems

## Goal

Turn the core combat/world technology into a real cooperative PvE game.

## Work

### Quests and narrative

- quest definitions and state;
- objective types;
- acquisition/completion;
- shared/group credit;
- rewards;
- dialogue;
- NPC interaction;
- world-state conditions;
- quest persistence;
- quest log/tracking UI.

### Factions and reputation

- faction definitions;
- hostility/friendliness;
- reputation progression;
- faction rewards/services;
- persistence and UI.

### Groups

- parties;
- invitations;
- leadership;
- party visibility;
- markers;
- ready checks where required;
- shared credit;
- group loot modes;
- group persistence/rejoin behaviour.

### Dungeon runtime

- private five-player instance creation;
- entry through world-connected entrances;
- group routing;
- disconnect/rejoin;
- checkpoint/reset behaviour;
- wipe recovery;
- instance cleanup;
- dungeon lockout/reward foundations.

### Encounter framework

- Encounter Definitions;
- encounter state;
- boss phase/state transitions;
- encounter reset;
- scripted mechanics layered over reusable AI;
- encounter telemetry/logging.

## Exit milestone — Five-Player Dungeon

Five players can form a party in the open world, enter a physically grounded dungeon instance, complete multiple encounters and bosses, receive/distribute loot, leave/rejoin correctly and retain progression.

---

# Phase 5 — Economy, Professions and Social Backbone

## Goal

Add the systems that turn a multiplayer RPG into a persistent social world.

## Work

### Crafting and gathering

Implement the Crafting PDD:

- gathering skills;
- profession slots;
- recipes;
- profession progression;
- exact-recipe mastery;
- craftsmanship tiers;
- cooperative crafting;
- profession UI;
- persistence.

### Economy

Implement the Economy PDD:

- currencies;
- vendors;
- player trade;
- economic sinks;
- bank/storage;
- market/auction system where specified;
- transaction validation;
- economic auditability.

### Communication

- Say/Yell/emotes;
- whispers;
- party/raid/guild chat;
- General/Trade/LFG channels;
- custom channels;
- rich links;
- local history;
- spam/flood controls;
- reporting context;
- blocks.

### Friends and guilds

- account-level friends;
- blocks;
- guild identity/membership;
- ranks/permissions;
- guild chat;
- guild bank/treasury;
- audit log;
- guild hall ownership/rent foundations where required.

## Exit milestone — Persistent Social Economy

Players can gather/craft/trade, communicate, form persistent social relationships and guilds, and participate in an economy whose important transactions are authoritative and persistent.

---

# Phase 6 — Broad World Content

## Goal

Scale from one complete zone and one dungeon into a convincing persistent world without changing the core pipelines.

## Work

### World expansion

- multiple authored zones and biomes;
- settlements;
- transport networks/fast travel as designed;
- caves/interiors;
- exploration rewards;
- rare NPCs;
- ambient populations;
- profession resources;
- zone-specific factions and quests.

### Open-world PvE

- elite areas;
- encounter chains;
- open-world events;
- world bosses;
- contribution/reward rules;
- server-wide or region-wide event state where designed.

### Character-content breadth

- full intended playable class roster foundations;
- specialisations;
- talent trees;
- class trainers;
- ability progression;
- broader itemisation;
- equipment sets;
- gems/enchantments;
- consumables.

### Dungeon breadth

- additional five-player dungeons;
- varied encounter archetypes;
- profession interactions;
- optional bosses/routes;
- normal difficulty content coverage.

## Exit milestone — World Alpha

The game contains enough interconnected zones, class gameplay, quests, items, professions and group content to support sustained multiplayer play without relying on developer-only test content.

---

# Phase 7 — PvP and Endgame Systems

## Goal

Implement the systems that depend on a stable combat, progression, world and social foundation.

## Work

### PvP

- opt-in PvP rules for Normal worlds;
- permanent open-world flag rules for PvP worlds;
- faction and free-for-all relationship rules;
- duels;
- contest-zone objectives;
- honorable-kill validation;
- anti-repeat-kill rules;
- Honor;
- seasonal 14-rank progression;
- PvP-specific balance modifiers where required;
- runtime hooks for future battlegrounds/arenas.

### Maximum-level progression

- level-60 progression end state;
- non-level progression;
- Legacy/account-wide progression where defined;
- endgame faction/economic goals;
- max-level crafting/item progression.

### Raid foundation

- raid groups;
- larger encounter scaling;
- raid instance/runtime rules;
- raid lockouts/rewards;
- representative raid encounter.

## Exit milestone — Endgame Alpha

Maximum-level characters have durable PvE, social/economic and PvP goals, and the runtime has proven it can support content above the five-player dungeon scale.

---

# Phase 8 — Alpha Hardening

## Goal

Turn the feature-complete game architecture into a robust service.

## Work

### Scale and performance

- multi-client load testing;
- actor-density testing;
- AI stress testing;
- network bandwidth profiling;
- interest-management validation;
- persistence concurrency tests;
- world-streaming soak tests;
- memory leak/load-unload testing;
- headless-server profiling.

### Correctness and security

- server-authority audit;
- movement exploit testing;
- combat validation audit;
- inventory/economy duplication testing;
- permission/ownership tests;
- persistence transaction failure tests;
- malformed RPC/request handling;
- rate limiting where required.

### Reliability

- reconnect/recovery;
- server restart recovery;
- database backup/restore;
- migration rehearsal;
- instance cleanup;
- world-state recovery;
- graceful failure when dependencies are unavailable.

### Player experience

- full input rebinding;
- controller architecture;
- UI scaling;
- accessibility options;
- subtitle/caption support;
- graphics settings;
- audio settings;
- onboarding polish;
- error/message consistency.

## Exit milestone — Closed Alpha Ready

A representative production environment can host sustained multiplayer sessions with monitored performance, reliable persistence and no known architecture-level blockers to content completion.

---

# Phase 9 — Beta and Release Readiness

## Goal

Finish content, balance and operations without changing fundamental architecture.

## Work

### Content completion

- launch world/zone coverage;
- launch class/spec/talent coverage;
- launch quests and narrative;
- launch dungeons;
- launch itemisation;
- launch professions/economy;
- launch faction/reputation content;
- launch social systems;
- launch endgame/PvP scope selected for release.

### Balance

- levelling pace;
- class performance;
- dungeon difficulty;
- encounter tuning;
- item progression;
- economy sources/sinks;
- profession value;
- PvP reward/balance curves.

### Release operations

- production Addressables/CDN flow;
- patch/update tests;
- immutable release manifests;
- rollback rehearsal;
- database migration rehearsal;
- maintenance workflow;
- monitoring/alerting;
- crash/error reporting;
- release build automation;
- dedicated-server deployment automation.

### QA

- regression suite;
- multiplayer compatibility testing;
- save migration testing;
- content validation;
- hardware/performance matrix;
- latency/loss testing;
- long-duration soak testing.

## Exit milestone — Release Candidate

No unresolved architectural migration is required for launch. Remaining work is limited to defects, balance, content corrections and release operations.

---

# Phase 10 — Live Service

## Goal

Operate and expand Ninth Age without destabilising the production world.

## Work

- content hotfixes through the approved live-content path;
- new zones;
- new dungeons/raids;
- new world events;
- additional class/spec/talent content where designed;
- new profession/economic content;
- structured PvP expansion;
- seasonal PvP progression;
- guild/social expansion;
- performance/scaling improvements;
- schema/content migrations;
- telemetry-guided balance changes.

Production changes follow the immutable Release State and compatibility rules in the Live Content and Versioning PDD.

---

# 4. Cross-Phase Workstreams

The following continue throughout the roadmap rather than belonging to one phase.

## 4.1 Design

- Resolve open decisions before implementation depends on them.
- Keep subsystem PDDs authoritative.
- Reconcile PDD conflicts explicitly.
- Keep Class Design and content-specific class documents ahead of class implementation.

## 4.2 Testing

- Add regression tests with every corrected defect.
- Prefer system/integration tests for cross-boundary gameplay.
- Test both graphical-client and headless-server execution.

## 4.3 Tooling

Any content type expected in quantity should have authoring and validation tools before mass content production begins.

Priority tooling includes:

- world/zone authoring;
- NPC/behaviour definitions;
- encounters;
- quests/dialogue;
- items/loot;
- abilities/talents;
- vendors;
- crafting/recipes;
- factions/reputation.

## 4.4 Performance

Performance budgets should become stricter as representative content becomes available. Optimisation must target actual gameplay scenes and server workloads.

## 4.5 Documentation

Update:

- C# Architecture when architecture changes;
- PDDs when product behaviour changes;
- this roadmap when dependencies or milestone scope materially change.

---

# 5. Milestone Summary

| Phase | Milestone |
|---|---|
| 0 | Stable Foundation |
| 1 | Durable Character |
| 2 | Playable MMORPG Loop |
| 3 | One Complete Zone |
| 4 | Five-Player Dungeon |
| 5 | Persistent Social Economy |
| 6 | World Alpha |
| 7 | Endgame Alpha |
| 8 | Closed Alpha Ready |
| 9 | Release Candidate |
| 10 | Live Service |

---

# 6. Immediate Priority Order

The next implementation work should proceed in this order:

1. finish the C# restyling/consistency pass;
2. add/fix automated regression coverage around the existing working core;
3. complete the endpoint/events/bindings terminology and lifecycle cleanup;
4. design and implement the relational persistence migration;
5. implement release/version/compatibility identity before content architecture expands;
6. harden the existing world-runtime and movement foundations;
7. build the compact Phase-2 vertical slice using existing combat/NPC/item systems;
8. only then expand into production-scale world/content authoring.

This order intentionally delays broad feature/content expansion until the systems most expensive to replace later are stable.
