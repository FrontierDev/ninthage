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

### 2.4 Ordinary perception cadence

Ordinary proximity aggro uses one shared authoritative perception cadence:

| Activity | Ordinary aggro/perception participation |
| --- | --- |
| Full | eligible for the shared existing `NPCAggroDecisionIntervalSeconds` cadence = **0.2 s** |
| Nearby | **none** |
| Dormant | **none** |

Nearby is a reduced activity/navigation state, not a reduced-rate aggro polling state.

The existing 0.2 s authored setting is converted once to an integer simulation-tick stride with `ServerSimulationClock.GetNonShorteningTickCount(...)`. Do not introduce an independent Nearby perception timer, frame timer or accumulated float countdown.

### 2.5 Spatially batched aggro broad phase

The 0.2 s perception pass must not execute one spatial query per Full NPC and must not execute the same NPC decision branch once per player.

The required broad-phase shape is:

```text
collect live player actors
    ↓
group players by compatible ActorSpatialContext
    ↓
for each context:
    derive the union of spatial cells covered by the players' Full/acquisition envelope
    ↓
    traverse each unique covered cell once
    ↓
    collect unique Full, alive, out-of-combat, aggro-capable NPC IDs
    ↓
    for each unique NPC once:
        evaluate spatially local, living player candidates
        apply cheap validity/hostility/range filters
        apply stealth/detection and other NPC-specific rules
        perform expensive LoS only after cheap filters
        choose/acquire at most one target
```

`ServerSpatialIndex` remains the sole spatial membership owner. Extend it with a narrow batch/cell-union query rather than creating a parallel player-cell or NPC-cell registry.

The batch query must:

- accept multiple authoritative player centres belonging to one compatible `ActorSpatialContext`;
- use the existing spatial cell grid;
- build a reusable set of unique intersecting cells for the supplied centres and broad-phase radius;
- visit each unique cell at most once for that call;
- append unique candidate actor IDs into caller-owned reusable output storage;
- report examined unique-cell count and examined actor-candidate count for telemetry;
- preserve the existing no-membership-mutation-during-query invariant;
- perform no hostility, stealth, faction, aggro or target-selection logic itself.

The broad-phase radius is the existing Full relevance envelope (`ActorVisibilityExitRadius`). Ordinary effective detection ranges must remain within that envelope. This keeps all normally detectable NPCs Full before acquisition can occur and avoids a second maximum-detection configuration value.

After the batch query, `ServerActorManager` resolves only unique eligible NPCs and performs each NPC's acquisition decision once. It must restrict player comparisons to genuinely local players in the same compatible context; do not fall back to scanning every connected player for every candidate NPC.

The exact player↔NPC pair checks that remain after spatial pruning are legitimate work. The optimisation target is redundant bucket traversal and repeated NPC decision entry, not eliminating necessary local relationship checks.

Dead player actors:
- **do** participate in activity relevance;
- **do not** participate as ordinary hostile acquisition targets.

NPC-specific effective detection range may incorporate base aggro range, NPC role/situational modifiers such as guards, and player stealth/detection effects. Apply cheap range/disposition/validity checks before expensive line-of-sight work.

### 2.6 Dormant behaviour

A Dormant NPC:

- performs no ordinary perception/aggro decision pass;
- performs no ordinary patrol/pursuit destination work;
- receives no per-frame project-owned NPC navigation update;
- retains authoritative ActorID, ECS entity, `ActorSpatialContext`, canonical world position, GameObject and PurrNet identity in the initial implementation;
- remains in `ServerSpatialIndex` so player movement can wake it;
- may later drive a low-cost dormant/idle animation state, but animation work is not part of Stage 1.4.

### 2.7 Out-of-combat reset

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

### 2.8 Representation

Initial Stage 1.4:

| Representation/work | Full | Nearby | Dormant |
| --- | --- | --- | --- |
| ECS actor/entity | retained | retained | retained |
| Spatial-index membership | retained | retained | retained |
| Actor GameObject | retained | retained | retained |
| PurrNet NetworkIdentity | retained | retained | retained |
| ordinary aggro/perception | shared 0.2 s pass | none | none |
| ordinary NPC navigation manager work | yes | yes | no |
| combat/pursuit | yes | no unless promoted Full | no |
| environment-scene dependency | none | none | none |

A dormant actor is not despawned and is not re-created on wake.

## 3. Ownership

Canonical NPC activity is stored on the server ECS entity in an `NPCActivityComponent`.

`ServerActorManager` owns:

- activity transitions;
- Full NPC perception eligibility;
- the shared ordinary perception/acquisition pass;
- NPC-specific effective detection and target-acquisition decisions;
- player-relevance reevaluation;
- dormant navigation suspension/resume;
- activity/perception telemetry.

`ServerSpatialIndex` remains the sole spatial membership and broad-phase candidate owner. Its Stage 1.4 extension batches multiple player centres by unioning covered cells; it does not own hostility, stealth, detection or target choice.

`ServerInterestManager`/PurrNet remain network-visibility projection owners and do not own activity.

No mutable activity field is added to `Actor` or `NPCBehavior`.

## 4. Ordered implementation

### Step 1 — Design contract

Complete. Issue #258 recorded and owner approved the Full/Nearby/Dormant contract.

### Step 2 — Canonical activity state and configuration

Add the ECS activity component, Nearby entry/exit configuration and validation, registration/ownership lifecycle, and transition helper. Preserve current behaviour by registering existing NPCs as Full until relevance driving is implemented. Do **not** add a Nearby perception cadence setting.

### Step 3 — Actor-level combat-reset primitives

Add explicit actor-level reset operations to the existing threat, cooldown, ticker and stat owners. These APIs must respect the current ECS mutation boundary and must not create another combat-state owner.

### Step 4 — Commit NPC disengagement reset

Make `ServerActorManager.DropNPCTarget(...)` the current ordinary-NPC disengagement/reset boundary. Use the Step-3 reset APIs plus the existing spell/aura owners, then reevaluate activity. Do not reset merely because combat ended if the NPC remains Full/Nearby by player relevance.

### Step 5 — Player-relevance activity transitions

Drive activity from authoritative spawn/movement/despawn/context-transfer changes using `ServerSpatialIndex` and the exact thresholds in §2.3. Do not scan all NPCs each simulation tick.

### Step 6 — Spatially batched Full-NPC perception

Replace the current all-NPC `UpdateNPCAggro(...)` pattern with one shared 0.2 s perception pass driven by player-occupied space.

Implementation responsibilities:

1. add the narrow multi-centre/cell-union broad-phase operation to `ServerSpatialIndex`;
2. collect current player actors by compatible `ActorSpatialContext` without excluding dead players from **activity** relevance;
3. for ordinary aggro acquisition, use only living valid player actors as target centres/candidates;
4. batch the player centres for each context through the spatial-index cell union using `ActorVisibilityExitRadius` as the broad-phase envelope;
5. collect/deduplicate Full, alive, out-of-combat, aggro-capable NPC candidates;
6. evaluate each candidate NPC at most once in the perception pass;
7. restrict that NPC's target checks to spatially local living players, not all connected players;
8. retain the existing NPC-owned aggro delay/deadline semantics;
9. apply cheap context/validity/hostility/effective-range checks before any expensive line-of-sight checks;
10. reuse owned scratch sets/lists; do not allocate a new NPC-key snapshot per pass.

Nearby and Dormant NPCs receive no ordinary aggro/perception polling.

Add telemetry for:
- player centres included;
- unique spatial cells traversed;
- spatial actor candidates examined;
- unique NPC candidates produced;
- local player↔NPC candidate pairs examined;
- NPC acquisition decisions executed;
- perception-pass duration.

### Step 7 — Dormant navigation and event-driven activation

Remove Dormant NPCs from project-owned per-frame navigation work, stop/reset their active path once on dormancy, and restore navigation eligibility on wake without recreating the actor. Add immediate damage-driven promotion to Full through the existing `ActorEndpoint` boundary. Ordinary aggro acquisition in Step 6 promotes the acquiring NPC to Full before target/combat commitment where it is not already Full; player movement/spawn/transfer activity promotion is owned by Step 5.

### Step 8 — Focused regression tests

Add only critical integration coverage for:

- exact Full/Nearby/Dormant distance+hysteresis transitions;
- dead players retaining relevance;
- combat/damage forcing Full;
- disengagement reset clearing transient combat state before dormancy;
- cross-instance players never waking an NPC;
- Nearby and Dormant NPCs performing no ordinary aggro polling;
- clustered players causing each covered spatial cell and each candidate NPC to be processed once per perception pass;
- dead players retaining activity relevance while remaining invalid ordinary aggro targets;
- dormant NPCs performing no ordinary navigation work and waking correctly.

No production changes in the test issue.

### Step 9 — Stage 1.4 close-out and performance hand-off

Stage 1.4 closes on actor-lifecycle/simulation-activity correctness, ownership and focused regression coverage.

Do **not** run a dedicated Stage 1.4 population/perception performance campaign as a completion gate. Preserve the telemetry added by this stage so the later integrated Phase 1 performance programme can evaluate:

- Full/Nearby/Dormant counts;
- player perception centres;
- unique spatial cells traversed;
- spatial actor candidates examined;
- unique NPC candidates;
- local player↔NPC candidate pairs examined;
- NPC acquisition decisions executed;
- actor-manager/aggro/navigation cost;
- whole simulation tick timing;
- allocation/GC;
- mass wake transition cost.

The intended later comparisons remain:

1. same large NPC population forced Full;
2. same population distributed mostly Dormant;
3. mass transition Dormant → Nearby/Full;
4. dense-player clustering to verify overlapping player envelopes do not multiply work incorrectly;
5. density/context transition with zero cross-instance wake.

Those comparisons are executed in Stages 1.10–1.11 after the server/client foundations are assembled. Stage 1.4 smoke validation may confirm that counters/timers change on representative transitions, but it must not expand into a density/capacity/soak benchmark campaign.

## 5. Non-goals

Stage 1.4 does not implement:

- PurrNet bandwidth/update-priority policy (Stage 1.5);
- actor GameObject/PurrNet dematerialisation;
- client dormant animation;
- new encounter gameplay;
- persistent NPC database state;
- reconnect continuity;
- new respawn/corpse timers;
- new threat formulas or target-selection design beyond preserving the existing authoritative NPC choice while changing its broad-phase candidate discovery;
- a parallel player/NPC spatial registry;
- custom Jobs/Burst work without new measurements;
- world-streaming redesign.

## 6. Validation order

Each production step requires owner validation before its dependent step proceeds.

Production-code issues and regression-test issues remain separate.

After the production sequence and focused regression coverage are accepted, Stage 1.4 hands its instrumentation to Stages 1.10–1.11. No Stage 1.4 performance campaign is required before dependent foundation work proceeds.
