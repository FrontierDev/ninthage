# Ninth Age — Stage 1.4 Implementation Plan

**Stage:** Phase 1, Stage 1.4 — Actor lifecycle, simulation LOD and high-population AI  
**Status:** Approved activity contract; implementation pending  
**Branch:** `dev`  
**Updated:** 2026-10-25

## 1. Purpose

Stage 1.4 makes large NPC populations economical without changing authoritative gameplay semantics.

The stage introduces three NPC simulation activity states:

- **Full**
- **Nearby**
- **Dormant**

Activity is a server simulation/performance state. It is not life/death, persistence, environment streaming or network visibility.

Stage 1.4 initially retains the existing Actor GameObject and PurrNet NetworkIdentity for dormant NPCs. Dematerialising dormant actors into a lighter representation is not required by this stage.

## 2. Locked behavioural contract

### 2.1 Activity priority

For an NPC, activity is selected in this order:

```text
mandatory combat/gameplay obligation
    ↓
Full player relevance
    ↓
Nearby player relevance
    ↓
Dormant
```

A current combat target, threat/combat obligation, damage activation or explicit encounter obligation forces **Full** regardless of distance.

Leaving combat does not itself cause dormancy. The normal out-of-combat reset commits first; activity is then reevaluated from current player relevance.

### 2.2 Player relevance

All live registered `PlayerActor` instances in the same compatible `ActorSpatialContext` count for relevance whether the player is alive or dead.

Player death therefore does not remove relevance. A player stops contributing only when the authoritative player actor leaves the context/lifetime through respawn elsewhere, transfer, logout or despawn.

Relevance uses:

- canonical `ActorComponent.WorldPositionD`;
- `ServerSpatialIndex`;
- compatible logical spatial context.

It does not use `GameObject.scene`, environment-scene loaded state, client-local transforms or client requests.

### 2.3 Initial engineering distances

The initial values are configurable engineering defaults:

| Boundary | Initial value | Rule |
| --- | ---: | --- |
| Full relevance | existing `ActorVisibilityExitRadius` | **96 m** on current configuration; using the exit radius guarantees any actor that may still be visible under visibility hysteresis remains Full. |
| Nearby wake/entry radius | `NPCNearbyEntryRadius` | **128 m** |
| Nearby sleep/exit radius | `NPCNearbyExitRadius` | **160 m** |

Rules:

- combat/gameplay obligation → Full at any distance;
- non-combat NPC with any relevant player at or within the Full relevance radius → Full;
- Dormant NPC with no Full obligation promotes to Nearby when any relevant player is at or within 128 m;
- Nearby NPC remains Nearby until every relevant player is beyond 160 m;
- Full NPC with no Full obligation demotes to Nearby once no relevant player is within the Full relevance radius, provided at least one relevant player remains within 160 m;
- an NPC with no Full obligation and no relevant player inside the applicable Nearby hysteresis boundary is Dormant;
- a teleport/spawn/transfer that places a player directly inside the Full radius may promote Dormant → Full in one transition.

The 128/160 m band is deliberately outside the current 80/96 m actor-visibility band and provides a wake buffer before an approaching player can see the NPC.

### 2.4 Initial decision cadence

The initial configurable decision frequencies are:

| Activity | Ordinary NPC perception/aggro cadence |
| --- | ---: |
| Full | existing `NPCAggroDecisionIntervalSeconds` = **0.2 s** |
| Nearby | `NPCNearbyDecisionIntervalSeconds` = **1.0 s** |
| Dormant | **none** |

All second-based settings are converted once to integer simulation-tick strides with `ServerSimulationClock.GetNonShorteningTickCount(...)`. Do not use independent frame timers or accumulated float countdowns.

### 2.5 Dormant behaviour

A Dormant NPC:

- performs no ordinary perception/aggro decision pass;
- performs no ordinary patrol/pursuit destination work;
- receives no per-frame project-owned NPC navigation update;
- retains authoritative ActorID, ECS entity, `ActorSpatialContext`, canonical world position, GameObject and PurrNet identity in the initial implementation;
- remains in `ServerSpatialIndex` so player movement can wake it;
- may later drive a low-cost dormant/idle animation state, but animation work is not part of Stage 1.4.

### 2.6 Out-of-combat reset

When the current implementation deliberately disengages an NPC, the reset boundary clears transient combat state before activity is reevaluated.

For the initial ordinary-NPC implementation, reset means:

- clear current target;
- stop auto-attack;
- interrupt an active spell cast;
- clear threat;
- remove active auras;
- remove active generic tickers;
- clear active cooldowns;
- restore current Resource-category stats, including Health, to their normal reset values:
  - `StartsAtZero == true` → 0;
  - otherwise → current `EffectiveMaximum`;
- clear pending aggro acquisition;
- clear active pursuit path.

The reset does not alter ActorID, spawn-point identity, spatial context, canonical position, definition/preset identity or death state.

Future explicitly persistent/scripted encounter state requires its own opt-out/retention contract when such state is implemented. Stage 1.4 does not invent persistence metadata now.

### 2.7 Representation

Initial Stage 1.4:

| Representation/work | Full | Nearby | Dormant |
| --- | --- | --- | --- |
| ECS actor/entity | retained | retained | retained |
| Spatial-index membership | retained | retained | retained |
| Actor GameObject | retained | retained | retained |
| PurrNet NetworkIdentity | retained | retained | retained |
| ordinary perception decisions | 0.2 s | 1.0 s | none |
| ordinary NPC navigation manager work | yes | yes | no |
| combat/pursuit | yes | no unless promoted Full | no |
| environment-scene dependency | none | none | none |

A dormant actor is not despawned and is not re-created on wake.

## 3. Ownership

Canonical NPC activity is stored on the server ECS entity in an `NPCActivityComponent`.

`ServerActorManager` owns:

- activity transitions;
- derived Full/Nearby scheduling eligibility;
- player-relevance reevaluation;
- dormant navigation suspension/resume;
- activity telemetry.

`ServerSpatialIndex` remains the spatial candidate owner.

`ServerInterestManager`/PurrNet remain network-visibility projection owners and do not own activity.

No mutable activity field is added to `Actor` or `NPCBehavior`.

## 4. Ordered implementation

### Step 1 — Design contract

Complete. Issue #258 recorded and owner approved the Full/Nearby/Dormant contract.

### Step 2 — Canonical activity state and configuration

Add the ECS activity component, configuration values/validation, registration/ownership lifecycle, and transition helper. Preserve current behaviour by registering existing NPCs as Full until relevance driving is implemented.

### Step 3 — Actor-level combat-reset primitives

Add explicit actor-level reset operations to the existing threat, cooldown, ticker and stat owners. These APIs must respect the current ECS mutation boundary and must not create another combat-state owner.

### Step 4 — Commit NPC disengagement reset

Make `ServerActorManager.DropNPCTarget(...)` the current ordinary-NPC disengagement/reset boundary. Use the Step-3 reset APIs plus the existing spell/aura owners, then reevaluate activity. Do not reset merely because combat ended if the NPC remains Full/Nearby by player relevance.

### Step 5 — Player-relevance activity transitions

Drive activity from authoritative spawn/movement/despawn/context-transfer changes using `ServerSpatialIndex` and the exact thresholds in §2.3. Do not scan all NPCs each simulation tick.

### Step 6 — Activity-aware NPC perception scheduling

Replace the single all-NPC 0.2 s pass with separate derived Full and Nearby eligibility sets and shared 0.2 s / 1.0 s schedules. Dormant NPCs receive no ordinary perception pass. Remove the current per-pass allocation of a new NPC-key list by reusing owned scratch storage.

### Step 7 — Dormant navigation and event-driven activation

Remove Dormant NPCs from project-owned per-frame navigation work, stop/reset their active path once on dormancy, and restore navigation eligibility on wake without recreating the actor. Add immediate damage-driven promotion to Full through the existing `ActorEndpoint` boundary. Existing aggro acquisition already promotes within `ServerActorManager`; player movement/spawn/transfer promotion is owned by Step 5.

### Step 8 — Focused regression tests

Add only critical integration coverage for:

- exact Full/Nearby/Dormant distance+hysteresis transitions;
- dead players retaining relevance;
- combat/damage forcing Full;
- disengagement reset clearing transient combat state before dormancy;
- cross-instance players never waking an NPC;
- dormant NPCs performing no ordinary perception/navigation work and waking correctly.

No production changes in the test issue.

### Step 9 — Stage 1.4 performance campaign and close-out

Extend the existing performance harness for a full server-NPC activity workload using the existing real authenticated player requirement; do not synthesize network clients.

Measure:

- Full/Nearby/Dormant counts;
- NPC decisions executed;
- spatial candidates examined;
- actor-manager/aggro/navigation cost;
- whole simulation tick p50/p95/p99;
- allocation/GC;
- mass wake transition cost.

Required comparisons:

1. same large NPC population forced Full;
2. same population distributed mostly Dormant;
3. mass transition Dormant → Nearby/Full;
4. density/context transition with zero cross-instance wake.

The Stage 1.4 gate passes only when Dormant steady-state CPU is demonstrably lower, wake spikes remain bounded, nearby combat remains responsive, and no cross-instance leakage occurs.

## 5. Non-goals

Stage 1.4 does not implement:

- PurrNet bandwidth/update-priority policy (Stage 1.5);
- actor GameObject/PurrNet dematerialisation;
- client dormant animation;
- new encounter gameplay;
- persistent NPC database state;
- reconnect continuity;
- new respawn/corpse timers;
- new threat formulas or target-selection design;
- custom Jobs/Burst work without new measurements;
- world-streaming redesign.

## 6. Validation order

Each production step requires owner validation before its dependent step proceeds.

Production-code issues and regression-test issues remain separate.

The final performance campaign is run only after the production sequence and focused regression coverage are accepted.
