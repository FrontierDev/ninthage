# NPC Cross-Region Pursuit Investigation

**Status:** Investigation record  
**Date:** 24 October 2026  
**Branch analysed:** `dev`  
**Code baseline analysed:** `79e75888c09741fb8ed5caed5ffd3fe0e15cb6df` (`Issue 181 rev 1`)  
**Stage:** Phase 1, Stage 1.2 — instance-aware spatial indexing, simulation regions and interest foundations

## 1. Purpose

This document records the codebase findings from the investigation into NPC chase failures observed when a player crosses a 256 m chunk/floating-origin boundary.

The investigation specifically examined:

- client floating-origin rebasing;
- authoritative absolute player movement;
- server simulation-region mapping and physical placement;
- the initial 3×3 streamed environment window;
- same-region NPC pursuit;
- cross-region NPC seam discovery and handoff;
- NPC target loss and reacquisition;
- NPC aggro perception;
- server auto-attack distance checks;
- actor visibility/chunk membership;
- existing Stage 1.2 validation coverage.

This is an evidence record, not an implementation plan. Confirmed code behaviour, owner-observed runtime behaviour, ruled-out explanations and remaining unverified behaviour are kept separate.

---

## 2. Owner-observed runtime behaviour

The following observations were reported during live testing after the NPC debug Inspector work:

1. While moving on Z toward the negative chunk boundary:
   - immediately before crossing, the client-local Z value was approximately `-127.9`;
   - immediately after the floating-origin shift, the client-local Z value was approximately `+127.9`.
2. NPCs sometimes stop pursuing directly and move primarily along X after this crossing.
3. If the player crosses back into the NPCs' previous chunk, the NPCs resume moving toward the player.
4. In some runs, NPCs lose their target entirely when the player crosses the boundary.
5. Target loss is not consistently correlated with ordinary NPC-player distance and appears dependent on which chunk boundary is crossed.

These observations motivated the code analysis below. They are not themselves used to infer undocumented runtime state.

---

## 3. Shared 256 m grid and simulation-region partition

### 3.1 Chunk coordinate calculation

`WorldPosition.Chunk` uses the centred 256 m grid:

```csharp
int cx = (int)Math.Floor((x + halfCell) / cs);
int cz = (int)Math.Floor((z + halfCell) / cs);
```

with `ChunkSize = 256`.

Therefore chunk 0 spans approximately:

```text
-128 <= coordinate < +128
```

and crossing `-128` enters chunk `-1`.

### 3.2 Simulation-region mapping

The current configured simulation-region span is 4×4 environment tiles. `SimulationRegionKey.FromEnvironmentTile(...)` uses floor division of environment-tile coordinates by that span.

Along either horizontal axis:

```text
chunks -4 -3 -2 -1  -> simulation region -1
chunks  0  1  2  3  -> simulation region  0
chunks  4  5  6  7  -> simulation region  1
```

This produces an important asymmetry around world chunk 0:

- chunk `0 -> +1` crosses a client origin/environment-tile boundary but remains inside simulation region 0;
- chunk `0 -> -1` crosses the same kind of client origin/environment-tile boundary **and** crosses a server simulation-region boundary.

The same applies independently on X and Z.

### 3.3 The initial 3×3 window spans four simulation regions

The spawn interest set centred on `(0,0)` contains:

```text
(-1, 1)  (0, 1)  (1, 1)
(-1, 0)  (0, 0)  (1, 0)
(-1,-1)  (0,-1)  (1,-1)
```

Under the 4×4 floor-divided simulation-region mapping, these tiles occupy:

```text
region (-1, 0) | region (0, 0)
---------------+--------------
region (-1,-1) | region (0,-1)
```

The 3×3 streaming window does not create these regions; it merely intersects four already-defined coarse simulation-region partitions because chunk `(0,0)` lies at a corner of region `(0,0)`.

The current server simulation-region capacity is 8. The initial 3×3 can therefore activate four of those physical region slots immediately.

---

## 4. Client floating origin is not the cause of absolute-position corruption

The client movement path preserves absolute world position when rebasing.

`ClientPositionManager.SetOriginChunk(...)`:

1. reconstructs the player's absolute `WorldPosition` from the old origin;
2. shifts client presentation/world roots;
3. changes `OriginChunk`;
4. reprojects the preserved absolute position into the new client-local frame.

The movement proposal is then reconstructed from the new origin/local representation.

The server does **not** perform an equivalent client-style floating-origin shift. It consumes the absolute `WorldPosition` and projects that position into the actor's current or destination simulation-region frame.

Therefore:

- the visible `-127.9 -> +127.9` local-coordinate jump is expected client presentation behaviour;
- it does not imply the canonical absolute player position changed discontinuously;
- the coincident behavioural discontinuity on the negative side comes from the simulation-region transition, not from a server floating-origin rebase.

---

## 5. The first failing adjacent tile is already loaded

At player spawn, `ServerSpawnManager` explicitly invokes the interest path for a 3×3 window centred on `(0,0)`.

`ServerInterestManager.GetGrid(...)` returns exactly the nine coordinates from centre ±1 on X/Z.

For each subscribed tile, `ServerWorldManager.EnsureChunkLoaded(...)` loads the scene. A server world scene is only added to `_loadedChunks` after `ServerPhysicsManager.BindEnvironmentScene(...)` completes successfully.

The client world-entry flow also waits for nine streamed scenes before finishing entry.

Therefore the adjacent tiles `(0,-1)`, `(0,+1)`, `(-1,0)` and `(+1,0)` are part of the original spawn window.

### 5.1 Ordinary movement currently does not advance server environment interest

The normal character movement controller submits absolute position proposals. It does not issue `Server_RequestChunkInterest(...)` when the player changes chunk.

The only project-owned runtime paths found for changing server chunk interest are:

- the initial spawn invocation;
- the optional `Test_InterestController`, when its keyboard test path is enabled.

Consequently, under normal movement the original server 3×3 remains unchanged.

This establishes that, for the first `0 -> -1` boundary test:

- the destination tile is not newly loaded at the moment of crossing;
- the previous adjacent tile is not being unloaded because the player moved;
- scene-window movement is not the explanation for the first-boundary chase failure.

Stage 1.2 step 18 — driving environment interest from accepted authoritative movement — remains unfinished, but it is a separate issue from this first-boundary failure.

---

## 6. Same-simulation-region NPC pursuit path

When the NPC and target resolve to the same `SimulationRegionKey`, `ServerActorManager_NPCMovement` performs direct pursuit.

The target position is obtained from canonical accepted server state and projected into the NPC's verified simulation-region frame with `ServerPhysicsManager.ToRegionLocal(...)`.

The agent destination is therefore based on:

```text
canonical target WorldPosition
    -> NPC simulation-region local coordinates
    -> NavMeshAgent.SetDestination
```

It is not based on the client's floating-origin transform.

Normal 256 m tile crossings that remain inside one simulation region therefore do not require a physical-region handoff.

---

## 7. Crossing chunk 0 -> -1 enters the cross-region navigation system

Once the player's accepted position resolves to a different simulation region from the NPC, the movement system no longer uses the direct same-region pursuit path.

`UpdateNPCNavigation()` instead:

1. resolves the NPC region, target region and target canonical `WorldPosition`;
2. chooses an adjacent region that reduces Manhattan distance toward the target region;
3. searches the shared simulation-region boundary for a validated seam;
4. retains temporary hop state;
5. after exhausting the candidate search and choosing the best seam, issues `SetDestination` toward the seam's source-side approach position;
6. at the authoritative pre-ECS movement boundary, attempts the physical handoff through `TryHandoffEnabledNavMeshActor(...)`;
7. warps to the validated destination-side landing point, transitions actor-region membership, and requests a new destination-region path.

This is a completely different navigation path from same-region pursuit.

---

## 8. X-axis movement during a Z-region crossing is consistent with the seam algorithm

For a Z-adjacent simulation-region pair, `GetAdjacentRegionBoundary(...)` correctly sets:

```text
alongX = true
```

because:

- Z is the boundary-normal axis;
- X is the coordinate along the shared boundary.

`ProjectAlongBorder(...)` projects the source-to-target route onto that Z boundary and obtains an X coordinate. Candidate source-approach and destination-landing points then share that along-boundary X value.

The code does not contain an identified X/Z swap in:

- `GetAdjacentRegionBoundary(...)`;
- `ProjectAlongBorder(...)`;
- `CreateSeamPosition(...)`.

Therefore an NPC that has already reached the source side of a Z boundary can legitimately move predominantly along X while heading toward a selected seam approach point.

This explains why the observed sideways movement is characteristic of the cross-region seam path rather than direct pursuit.

---

## 9. Confirmed defect: NPC movement code clears gameplay target on seam-search failure

The current `UpdateNPCNavigation()` implementation explicitly clears `Actor.Target` after an exhausted seam search when no candidate was accepted and the search did not mark any candidate as temporarily unready:

```csharp
if (!state.HopSawUnreadyCandidate)
{
    state.Agent.ResetPath();

    if (state.Actor.Target != null
        && state.Actor.Target.ID == state.HopTargetID)
    {
        state.Actor.SetTargetFromServer(null);
    }

    ClearNPCNavigationHop(ref state);
    StoreNPCNavigationStateIfRegistered(actorID, state);
    continue;
}
```

This is the direct code mechanism that can produce the observed target loss immediately after the player moves into another simulation region.

Target loss in this path:

- does not require the target to exceed NPC aggro range;
- does not require the target to die;
- does not require a chunk scene to unload;
- depends on the cross-region seam search reaching an exhausted/no-valid-seam outcome.

### 9.1 This conflicts with the approved movement contract

Movement and Traversal PDD §20.3.2 states that when no verified crossing/path exists, the movement system reports unreachable navigation to the owning AI behaviour/recovery policy, which may wait, reposition, retarget or disengage/reset.

`ServerActorManager_NPCMovement` is the locomotion/navigation-intent owner. It is not supposed to unilaterally make the gameplay target-selection decision.

The current unconditional `SetTargetFromServer(null)` on this navigation outcome is therefore a confirmed implementation defect relative to the approved PDD.

---

## 10. Confirmed defect: NPC aggro acquisition uses physical-region-local Unity Physics

`ServerActorManager.UpdateNPCAggro(...)` still performs:

```csharp
Vector3 npcPos = state.NPC.transform.position;
var colliders = Physics.OverlapSphere(npcPos, aggroRange);
```

It then selects the nearest player using Unity `Transform.position`.

This is incompatible with cross-simulation-region perception.

### 10.1 Why

Active simulation regions are deliberately placed into physically isolated Unity float-space slots.

`ServerPhysicsManager.GetRegionLocalAnchor(...)` allocates region slots along X with a pitch equal to twice the region width. With the current 4×4 × 256 m region size:

```text
region width = 1024 m
slot anchor pitch = 2048 m
```

Actors in different simulation regions can therefore be physically thousands of Unity metres apart even when their canonical absolute `WorldPosition` values are only metres apart across a logical region boundary.

A single `Physics.OverlapSphere` around the NPC cannot represent logical proximity across those isolated region frames.

### 10.2 Re-aggro behaviour follows directly

After cross-region navigation clears the NPC's target:

- `UpdateNPCAggro()` becomes active again because `NPC.Target == null`;
- a player represented in another physical simulation-region slot is not correctly discoverable by the NPC's local `Physics.OverlapSphere`;
- if the player crosses back into the NPC's region, the player's server GameObject is projected into that same region frame;
- the Physics overlap can then see the player;
- the normal aggro-delay path can reacquire and set the target again.

This directly accounts for the observed “NPC re-aggros when the player steps back into its chunk/side of the boundary” behaviour.

Stage 1.2 step 17 already requires migration of applicable NPC proximity/aggro queries to the context-qualified server spatial index followed by exact double-precision range filtering.

---

## 11. Other confirmed cross-region coordinate misuse

### 11.1 `Actor.DistanceTo(...)`

`Actor.DistanceTo(...)` is based on Unity transform distance.

`ServerAutoAttackManager` uses this method for auto-attack range checks.

Across isolated simulation-region physical frames, this is not a valid canonical gameplay distance.

This does not clear the target and is not the cause of the initial seam-search failure, but it is a confirmed Stage 1.2 cross-region correctness problem.

### 11.2 Raw Transform positions are not globally comparable across simulation regions

Server actor `Transform.position` values are physical-region-local representations.

Two server actors in different simulation regions cannot have their raw transforms subtracted to obtain canonical gameplay distance or direction.

Debugging tools must therefore distinguish:

- canonical absolute `WorldPosition`;
- simulation-region key;
- region-local Unity transform;
- active navigation-hop/seam state.

---

## 12. Seam-search behaviour and limitations

### 12.1 Search is incremental

`UpdateNPCNavigation()` scans four seam candidates per ordinary Unity `Update()`:

```csharp
ScanAdjacentRegionSeams(..., candidateBudget: 4, ...)
```

The search proceeds in deterministic batches.

### 12.2 A valid early candidate is not selected immediately

If a batch finds a valid candidate, it is retained as the current best candidate, but the NPC does not immediately begin moving toward it.

The code waits until the eligible candidate search is exhausted before setting:

- `HopSelected = true`;
- `HopSearchComplete = true`;
- the agent destination to the chosen source-side approach.

Therefore the seam search can introduce a visible delay after the target enters another region.

### 12.3 The scanner covers the whole logical region boundary

The candidate range is based on the full simulation-region boundary.

With a 4×4 region, this boundary is 1024 m long.

The initial 3×3 does not stream all 16 tiles in each intersected region. For example, region `(0,-1)` can be active while only a small subset of its logical 4×4 tile footprint is physically loaded.

The seam scanner therefore examines candidate locations along a logical region edge even where corresponding streamed navigation content does not exist.

---

## 13. Confirmed classification weakness: missing streamed navigation can be treated as unreachable

`TryEvaluateRegionSeamCandidate(...)` first attempts NavMesh sampling of source and destination nominal seam points.

Only **after both samples succeed** does it construct the destination landing tile and determine whether that tile is absent or pending unload.

Therefore, when `NavMesh.SamplePosition(...)` fails because the candidate lies on an unstreamed/unbuilt portion of the destination region edge, the candidate returns as simply invalid before `unavailableCandidate` can be marked.

At the higher level, `HopSawUnreadyCandidate` is used to distinguish temporary physical unavailability from an exhausted unreachable route.

This ordering means unavailable streamed navigation can be misclassified as ordinary seam failure, allowing the target-clearing branch to execute.

This is a concrete logic weakness. Whether this is the exact candidate failure in a specific reproduction still requires direct runtime seam-state evidence.

---

## 14. Existing Stage 1.2 test evidence does not validate cross-region NPC handoff

`Test_SimulationRegionSizingProbe.VerifyNavMeshIsolationAndContinuity(...)` validates that a NavMesh path can cross an **internal 256 m fixture-tile boundary inside one simulation region**.

It does not exercise:

- `ScanAdjacentRegionSeams(...)`;
- `ValidateRegionSeam(...)`;
- `TryHandoffEnabledNavMeshActor(...)`;
- the `0 -> -1` negative simulation-region crossing;
- NPC target retention during failed/pending seam search.

No dedicated current PlayMode test was found for enabled-NavMesh NPC simulation-region handoff.

Therefore the existing successful internal-tile continuity evidence does not establish that the cross-region pursuit/handoff path works.

---

## 15. Separate confirmed visibility defect

Actor broad-phase visibility still relies on spawn-era chunk state.

At spawn:

- `ServerSpawnManager` assigns `Actor.CurrentChunk`;
- the actor `NetworkIdentity` is registered under the spawn scene in `ChunkSubscriptions`.

Normal NPC accepted movement updates `ActorComponent.WorldPositionD` but does not move this chunk/identity registration.

`ChunkVisibilityRule` uses `targetActor.CurrentChunk`.

Therefore a moving NPC remains broad-phase associated with its spawn chunk.

This is a real Stage 1.2 defect, but it does **not** explain the first `0 -> -1` target-loss reproduction because the original server 3×3 remains unchanged during normal movement and still includes chunk 0.

---

## 16. Explanations ruled out for the first 0 -> -1 reproduction

Based on the analysed code, the following are not supported as causes of the first adjacent negative-boundary chase failure:

1. **Client floating-origin corruption of the absolute target position.**  
   The client preserves/reconstructs absolute position across rebase.

2. **A server floating-origin shift.**  
   The server does not perform a client-style FPO rebase.

3. **The player entering a newly loaded adjacent tile.**  
   Tile `(0,-1)` is part of the initial 3×3.

4. **The previous adjacent NPC tile being unloaded because player interest advanced.**  
   Normal character movement currently does not advance server chunk interest at all.

5. **Ordinary same-region NavMesh tile continuity.**  
   The failing negative boundary is not same-region: it is a simulation-region boundary.

6. **NPC aggro range itself clearing an existing target.**  
   `UpdateNPCAggro()` explicitly skips perception while `NPC.Target != null`; target clearing occurs in the cross-region navigation path.

---

## 17. Established causal chain

The current code supports the following concrete sequence for the observed negative-boundary failure:

```text
Player crosses z = -128
        |
        v
Client FPO rebases chunk 0 -> -1 while preserving absolute position
        |
        v
Server accepts continuous absolute player position
        |
        v
Player physical binding changes simulation region (0,0) -> (0,-1)
        |
        v
NPC and target no longer share a simulation region
        |
        v
Direct same-region pursuit stops
        |
        v
Cross-region seam search starts
        |
        +--> valid seam selected -> NPC moves to source-side seam approach,
        |    may move along X on a Z boundary, then attempts handoff
        |
        +--> search exhausts with no accepted seam and no unready flag
                 |
                 v
             NPC path reset
                 |
                 v
             Actor.Target explicitly cleared
                 |
                 v
             NPC aggro perception resumes
                 |
                 v
             Physics.OverlapSphere cannot correctly perceive a player
             represented in a different physical simulation-region slot
                 |
                 v
             player crosses back into NPC's physical region
                 |
                 v
             overlap perception can see player again
                 |
                 v
             normal aggro delay reacquires target
```

This is the current evidence-backed explanation for the reported behaviour.

---

## 18. Architectural conclusion

The observed defect is not fundamentally a generic 256 m chunk-boundary problem.

It is a **cross-simulation-region NPC pursuit/handoff problem** that is encountered unusually early because the current 4×4 simulation-region partition places chunk `(0,0)` at a region corner.

The first negative chunk/FPO transition therefore exercises:

- cross-region route/seam discovery;
- simulation-region physical isolation;
- NPC handoff/recovery behaviour;

while the first positive transition remains a normal same-region tile crossing.

The region-grid alignment itself is an architectural choice with significant debugging and capacity consequences. The confirmed correctness defects are in the current cross-region navigation recovery/target ownership and in remaining transform/Physics-based cross-region gameplay queries.

---

## 19. Relevant code locations

Primary runtime files examined:

- `Assets/Runtime/Client/Core/ClientPositionManager.cs`
- `Assets/Runtime/Client/Core/Controllers/Test_CharacterController.cs`
- `Assets/Runtime/Server/Core/ServerActorManager.cs`
- `Assets/Runtime/Server/Core/ServerActorManager_NPCMovement.cs`
- `Assets/Runtime/Server/Core/ServerActorManager_PlayerMovement.cs`
- `Assets/Runtime/Server/Core/ServerAutoAttackManager.cs`
- `Assets/Runtime/Server/Core/ServerInterestManager.cs`
- `Assets/Runtime/Server/Core/ServerPhysicsManager_Actors.cs`
- `Assets/Runtime/Server/Core/ServerPhysicsManager_EnvironmentScenes.cs`
- `Assets/Runtime/Server/Core/ServerPhysicsManager_NavMesh.cs`
- `Assets/Runtime/Server/Core/ServerPhysicsManager_RegionLifecycle.cs`
- `Assets/Runtime/Server/Core/ServerPhysicsManager_Regions.cs`
- `Assets/Runtime/Server/Core/ServerSpawnManager.cs`
- `Assets/Runtime/Server/Core/ServerWorldManager.cs`
- `Assets/Runtime/Server/Core/ServerWorldManager_EnvironmentScenes.cs`
- `Assets/Runtime/Server/Core/SimulationRegionDescriptor.cs`
- `Assets/Runtime/Server/Core/SimulationRegionKey.cs`
- `Assets/Runtime/Server/Core/SimulationRegionSeam.cs`
- `Assets/Runtime/Shared/Networking/ChunkSubscriptions.cs`
- `Assets/Runtime/Shared/Networking/ChunkVisibilityRule.cs`
- `Assets/Runtime/Shared/Runtime/Actors/Actor.cs`
- `Assets/Runtime/Shared/Utility/WorldPosition.cs`

Relevant design/test references:

- `.docs/Plans/Phase-1-Implementation-Plan.md` — Stage 1.2
- `.docs/pdd/Movement-and-Traversal-PDD.md` — §20.3.1–20.3.2
- `.docs/pdd/World-Runtime-and-Instancing-PDD.md`
- `.docs/pdd/AI-and-Encounter-Behaviour-PDD.md`
- `.docs/Performance-and-Simulation-Architecture.md` - current live profiling ownership; retired harness artifacts are not validation evidence

---

## 20. Baseline caveat

At the time this investigation was recorded, the remote `dev` branch visible through the GitHub repository API pointed to `79e75888` (`Issue 181 rev 1`). Issue #182 was closed but its Inspector files were not present on that remote branch snapshot.

The runtime conclusions above are therefore tied to the stated commit baseline. Any later code changes to simulation-region mapping, NPC movement, seam discovery, interest management, aggro acquisition or distance calculation must be checked against this record rather than assuming these findings remain current.

---

## 21. Resolution — 24 October 2026

The findings in Sections 1–20 remain accurate for the recorded baseline commit `79e75888c09741fb8ed5caed5ffd3fe0e15cb6df`. They document the behaviour of the fixed 4×4 tile-derived simulation-region, isolated physical-slot and calculated seam/handoff architecture that existed at that revision.

After this investigation, the owner superseded that architecture rather than repairing the seam system incrementally. The selected response was architectural simplification: retain the working client floating-origin path and canonical double-precision server position, collapse the current server runtime to one continuous physical/NavMesh space, and defer any future coarse simulation-region/server-worker boundaries until they can be deliberately authored around geography, gameplay and operational requirements.

The cleanup sequence was:

- **#188** — realigned the Stage 1.2 implementation and performance architecture documents with continuous server physical space;
- **#189** — cut the live player/NPC/spawn/world physics paths over to the continuous physical/NavMesh representation;
- **#190** — deleted the fixed-region types, actor/region registries, configuration and temporary compatibility stubs;
- **#191** — retired the executable simulation-region sizing workload while preserving its historical benchmark evidence.

As of source revision `b23d62bb070423cf33ca490d3aa7b89d6b7d8412` (`Issue 191`), the executable sizing probe has also been removed. The earlier 4×4 span and active-region-capacity results remain historical engineering evidence only; they are not current runtime configuration.

This document is therefore intentionally retained as a historical debugging and architecture record. Its cross-region defects should not be read as descriptions of the current continuous-space implementation, but they explain why the superseded architecture was retired.
