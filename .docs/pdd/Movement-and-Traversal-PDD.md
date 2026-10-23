# Ninth Age — Movement and Traversal Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Player movement, jumping, crawling, falling, swimming, movement impairment, mounts, physical transport, fast travel and traversal-related progression  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended movement and traversal model for **Ninth Age**.

It is authoritative for:

- baseline player movement;
- walking and running;
- jumping;
- crawling;
- falling and fall damage;
- swimming;
- movement during combat;
- movement-impairing effects;
- knockbacks and other forced movement;
- mounts;
- boats, lifts and similar physical transport;
- fast travel;
- lodestones;
- the boundary between baseline traversal and profession-specific traversal mechanics;
- movement-related multiplayer and world-runtime requirements.

This document does not define:

- the technical world-streaming architecture — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- exact combat ability behaviour — [Combat System PDD](Combat-System-PDD.md);
- NPC perception and aggro implementation — [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- geographical placement of roads, lodestones, mountains, waterways or transport networks — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- individual mount items, acquisition sources or economy — the appropriate Items, Quest and progression PDDs;
- profession-gated traversal content such as Shadowcraft agility shortcuts beyond establishing its ownership boundary here.

Where existing implementation conflicts with this document, this document defines the intended player-facing behaviour.

---

## 2. Design Pillars

### 2.1 Movement should be conventional, responsive and readable

Ninth Age does not require an elaborate action-movement system.

Core movement should feel immediately familiar to MMORPG players:

- walk;
- run;
- jump;
- crawl;
- fall;
- swim.

The world and gameplay systems should create traversal depth without requiring excessive movement mechanics.

### 2.2 Combat does not create a separate movement mode

Entering combat must not intrinsically slow the player or replace normal movement behaviour.

A player who can run at a particular speed outside combat can normally run at the same speed during combat.

Combat abilities and status effects may deliberately modify movement, but combat state itself does not.

### 2.3 The world should remain physically meaningful

Ninth Age should favour travelling through the world rather than routinely bypassing it.

This supports:

- geographic identity;
- roads and routes;
- settlements;
- wilderness;
- boats and other transport;
- exploration;
- discovery;
- world scale.

Fast travel exists, but is constrained and integrated into the setting.

### 2.4 Traversal progression should open opportunities rather than invalidate geography

Mounts, lodestones and profession-specific shortcuts make travel more capable as the character progresses.

They should not make the physical world irrelevant.

There are no flying mounts.

### 2.5 Movement must remain compatible with the large seamless world

Movement operates across the streaming, floating-origin and double-precision positioning systems defined by the World Runtime and Instancing PDD.

Technical world boundaries must not become movement boundaries.

---

## 3. Baseline Ground Movement

### 3.1 Walking

Walking is the slower standard movement mode.

It is always available unless another gameplay effect explicitly prevents movement.

Walking is selected through a player keybind rather than through a stamina or sprint system.

### 3.2 Running

Running is the normal default travel speed on foot.

Running:

- does not consume stamina;
- does not have a duration limit;
- remains available in combat;
- is not treated as sprinting.

Ninth Age has **no baseline sprint mechanic**.

### 3.3 Walk/run selection

Players can toggle or otherwise select walking and running through a configurable keybind.

The exact default key binding belongs to input configuration rather than this PDD.

Changing between walking and running should not require opening an interface or activating an ability.

---

## 4. Movement Direction and Control

Player ground movement is directly controlled by player input.

Movement direction and character facing must remain sufficiently independent to support the combat model defined by the Combat System PDD.

Movement should remain responsive on the local client while being subject to authoritative server validation.

Ordinary traversal must not feel delayed by waiting for a server round trip before movement is displayed.

---

## 5. Jumping

Players can jump during normal ground movement.

Jumping is a general movement capability rather than a class or profession feature.

Jumping may be used for:

- ordinary world navigation;
- crossing small obstacles;
- navigating uneven terrain;
- encounter mechanics where appropriate.

Jumping is not intended to replace authored traversal paths or allow players to bypass arbitrary world geometry.

The existing movement prototype already supports grounded jumping and gravity. Exact jump velocity and height remain tuning values rather than permanent design values in this PDD.

---

## 6. Crawling

Crawling is a dedicated slow movement state.

It has two primary purposes.

### 6.1 Tight-space traversal

Crawling permits navigation through specifically authored spaces that cannot be traversed while standing normally.

Examples may include:

- gaps beneath structures;
- low tunnels;
- collapsed passages;
- narrow cave routes;
- concealed entrances;
- environmental shortcuts.

World geometry intended for crawling must be deliberately authored for the mechanic.

Crawling must not depend on accidental collider behaviour.

### 6.2 Reduced NPC detection

Crawling reduces the effective detection radius of NPCs attempting to detect the crawling player.

The reduction applies to NPC perception rather than making the player generically invisible.

The exact detection-radius modifier is not yet fixed and should be configurable.

NPCs may still detect a crawling player where other detection rules permit it.

The detailed perception calculation belongs to the AI and Encounter Behaviour PDD.

### 6.3 Crawl speed

Crawling is intentionally much slower than walking or running.

The exact movement-speed multiplier remains a tuning value.

---

## 7. Falling

Players are affected by gravity and can fall naturally from world geometry.

The current implementation uses normal downward gravity and maintains vertical velocity independently of horizontal movement.

### 7.1 Airborne movement

Jumping or falling should preserve sensible horizontal momentum.

Airborne control may exist sufficiently to keep traversal playable, but should not create highly artificial mid-air manoeuvrability.

Exact air-control behaviour remains an implementation-tuning concern.

### 7.2 Maximum fall velocity

The current game configuration already supports a maximum falling speed.

This concept is retained.

The exact cap may be tuned as movement and fall damage are developed.

---

## 8. Fall Damage

Falling from sufficient height causes damage.

The fall-damage model should be **fairly strict**: significant drops should become dangerous relatively quickly.

However, the system must remain playable and should not punish ordinary navigation errors excessively.

The intended progression is approximately:

```text
small drop
    ↓
no damage

moderate drop
    ↓
minor to meaningful damage

large drop
    ↓
heavy damage

extreme drop
    ↓
potentially fatal
```

The fall-damage curve should have a clear safe threshold followed by increasingly severe consequences.

Fall damage should be based on authoritative movement information, such as fall distance and/or impact velocity, rather than purely client-reported damage.

Exact thresholds and damage formulas are deliberately left for tuning.

---

## 9. Swimming

### 9.1 Initial implementation

Initial swimming support is **surface-only**.

Players entering sufficiently deep water can swim horizontally across the water surface.

The initial system does not require free underwater movement.

### 9.2 Future full 3D swimming

The movement architecture must not prevent later support for full three-dimensional swimming.

A later implementation may add:

- diving;
- ascending;
- descending;
- underwater exploration;
- underwater combat where appropriate.

This is a future extension and is not required for the initial movement system.

### 9.3 Water transitions

Entering and leaving water should occur naturally through world geometry.

The movement controller must correctly transition between:

```text
grounded movement
    ↕
surface swimming
```

without requiring a loading or interaction screen.

---

## 10. Movement During Combat

Combat does not inherently change baseline movement.

A player may:

- walk;
- run;
- jump;
- move normally;

while in combat unless prevented by a specific gameplay effect.

There is no generic combat movement-speed penalty.

There is no separate combat sprint or combat locomotion system.

Individual abilities may impose their own movement restrictions according to the Combat System PDD.

---

## 11. Movement-Impairing Effects

Movement impairment is a normal part of combat.

The combat and ability systems may support effects such as:

- movement-speed reductions;
- roots;
- immobilisation;
- other explicitly authored restrictions on movement.

These effects modify the player's normal traversal capabilities.

They must be represented as explicit gameplay states rather than being inferred simply because the player is in combat.

Detailed crowd-control taxonomy and duration rules belong to the Combat System PDD.

---

## 12. Forced Movement

Forced movement exists but should be used sparingly.

Examples include:

- knockbacks;
- pulls;
- pushes;
- encounter-driven displacement.

These mechanics are expected primarily in deliberately authored encounters, especially raid encounters.

Forced movement should not become a routine property of ordinary attacks.

This restriction exists to preserve:

- positional readability;
- player control;
- predictable movement;
- network robustness;
- encounter clarity.

The system must nevertheless support authoritative forced movement when encounter design requires it.

---

## 13. Mounts

Mounts are a progression feature rather than an immediately available baseline ability.

### 13.1 Acquisition timing

Mount access should begin at approximately the **mid-to-high-level stage** of character progression.

The exact level or progression requirement belongs to the Character Stats and Progression and/or relevant content PDDs.

### 13.2 Ground mounts

Ninth Age supports ground mounts.

Mounted travel increases practical world-travel speed while retaining the geography and route structure of the world.

### 13.3 No flying mounts

Ninth Age does **not** support flying mounts.

This is a deliberate world-design constraint.

The world should continue to make use of:

- roads;
- mountain passes;
- bridges;
- tunnels;
- gates;
- boats;
- lifts;
- dangerous territory;
- geographical barriers;
- traversal shortcuts.

Mount progression must not allow players to ignore this structure by flying over it.

### 13.4 Outdoor use

Mounts may be used **everywhere outdoors**.

There is no ordinary zone-by-zone outdoor mount restriction.

Specific temporary gameplay circumstances may prevent mounting where explicitly required, but arbitrary outdoor no-mount regions should not be the baseline.

### 13.5 Indoor use

The general right to use mounts outdoors does not imply normal mount use inside buildings, caves, dungeons or other interiors.

Indoor mount rules may be defined according to content requirements.

---

## 14. Physical Transport

Ninth Age supports physical world transport.

This includes at least:

- boats;
- lifts.

Other forms may later use the same conceptual transport model.

### 14.1 Passenger movement

Physical transport differs from ordinary player-controlled movement.

The transport object moves through the world and the player travels with it as a passenger.

The player should remain spatially coherent with the moving transport while:

- standing on it;
- walking on it where allowed;
- entering or leaving it.

### 14.2 Boats

Boats may connect geographically separated locations and reinforce the physical scale of the world.

Where practical, boat travel should occur through the actual world rather than merely disguising a teleport.

Exact routes, schedules and whether particular journeys remain continuously simulated belong to later world design and implementation work.

### 14.3 Lifts

Lifts provide vertical or local transport through the physical environment.

Examples may include:

- settlement lifts;
- mine lifts;
- fortress lifts;
- large mechanical platforms.

Lifts must carry players reliably without requiring the player position to be manually updated as a sequence of teleports.

---

## 15. Fast Travel Philosophy

General-purpose free teleportation is not the normal travel model for Ninth Age.

This is supported both by world-design goals and by the setting.

In the game's lore, teleportation is heavily prohibited because of the disastrous magical consequences associated with it.

Fast travel therefore exists as a controlled exception rather than as unrestricted teleportation.

---

## 16. Lodestones

Lodestones are the primary standard fast-travel system.

### 16.1 Progression unlock

Lodestone travel is initially unavailable.

At approximately the **early-to-mid-level boundary**, the player undertakes a quest that unlocks the ability to use the lodestone network.

The exact quest, level and narrative circumstances belong to the Quest, Narrative and Dialogue PDD.

The unlock should feel like a meaningful progression milestone.

### 16.2 Physical access

A player must be physically present at a lodestone in order to initiate normal lodestone travel.

Lodestones are therefore a network of travel nodes rather than a menu that can be opened from anywhere in the world.

Conceptually:

```text
travel through world
        ↓
reach lodestone
        ↓
select known destination
        ↓
pay travel cost
        ↓
travel to destination lodestone
```

### 16.3 Destination discovery

A lodestone destination must have been discovered before the player can travel to it.

The normal expectation is that the player first reaches a location through ordinary exploration or transport.

Discovering its lodestone then adds that destination to the player's available network.

This preserves the importance of initial exploration.

### 16.4 Cost

Lodestone travel costs the game's base currency: **gold**.

There is no separate lodestone-specific currency.

The exact cost formula is not fixed by this PDD.

It may account for factors such as destination or travel distance if later economic design requires it.

### 16.5 Persistence

Discovered lodestones and the player's lodestone-system unlock are persistent character progression.

Logging out, changing worlds or restarting the client must not erase them.

### 16.6 World relationship

Lodestones transport the character between locations within the appropriate game-world context.

Normal lodestone travel is not intended as a mechanism for silently changing the player's selected world.

---

## 17. Exceptional Teleportation and Class Abilities

The setting's restrictions on teleportation do not require teleportation to be mechanically impossible in every circumstance.

Rare, explicitly justified abilities may create exceptions.

A possible example is a **Sorcerer ability capable of opening a portal to a lodestone**.

If implemented, such an ability should interact with the established lodestone network rather than provide unrestricted arbitrary-coordinate teleportation.

Potential rules such as:

- whether the destination must already be discovered;
- whether party members can use the portal;
- resource or cooldown cost;
- casting restrictions;
- lore consequences;

belong to the Class Design and Abilities and Talents PDDs.

This PDD only establishes that the movement architecture must be capable of supporting such deliberately authored exceptions.

---

## 18. Shadowcraft Traversal

Climbing, agility routes and similar traversal shortcuts are **not part of the universal baseline movement kit**.

They belong to the **Shadowcraft profession**.

Examples may include:

- climbing specific authored surfaces or routes;
- crossing profession-gated agility obstacles;
- entering difficult-to-access passages;
- using specialised traversal shortcuts.

This distinction allows the world to contain alternate routes and profession-based exploration opportunities without requiring every character to have a universal climbing system.

Baseline movement must therefore not assume arbitrary climbability of world geometry.

Shadowcraft traversal should operate through deliberately authored traversal opportunities rather than attempting to make every wall or cliff universally climbable.

Detailed Shadowcraft progression, requirements and rewards belong to the appropriate profession design documentation.

---

## 19. Movement and World Streaming

Movement must integrate transparently with the systems defined by the World Runtime and Instancing PDD.

Ordinary movement may cause:

- floating-origin changes;
- open-world tile streaming;
- actor-interest changes;
- public interior streaming;
- seamless instance transitions.

These systems must not require the player to stop moving under normal conditions.

### 19.1 Floating-origin transitions

Crossing a 256 m origin/network cell boundary may trigger a client floating-origin adjustment. This boundary is intentionally aligned with the 256 m environment-tile grid through the shared `GameConfiguration.ChunkSize` value. An aligned boundary may also change environment streaming interest, but rebase and streaming work remain separate operations and may be staggered/deferred.

This must not:

- interrupt movement;
- change apparent velocity;
- affect jump/fall state;
- cause a loading screen;
- change absolute authoritative position.

### 19.2 World-tile transitions

Crossing between streamed open-world content tiles must be seamless.

Movement should continue while nearby Addressable scenes are loaded and no-longer-needed scenes are unloaded.

### 19.3 Seamless interiors

Walking, running, crawling, jumping or swimming through an entrance into a public seamless interior should continue naturally.

A seamless private-instance entrance should likewise preserve the player's sense of continuous movement where the instance design calls for it.

---

## 20. Movement Authority and Networking

Ninth Age uses responsive client-controlled movement with server validation.

The player should receive immediate local response to movement input.

The server remains responsible for determining whether the resulting movement is legal.

### 20.1 Client responsibilities

The client may perform immediate local movement simulation for:

- walking;
- running;
- jumping;
- crawling;
- falling;
- swimming;
- mounted travel where appropriate.

### 20.2 Server responsibilities

The server must validate movement against relevant constraints, including where appropriate:

- maximum permitted speed;
- impossible displacement;
- movement mode;
- movement-impairing effects;
- forced movement;
- valid spatial context;
- authoritative world position.

The server may correct illegal client positions.

### 20.3 Absolute position

Movement must update the authoritative double-precision world position defined by the World Runtime and Instancing PDD.

The production movement system must not treat client-local floating-point transforms as globally authoritative coordinates.

For each live actor, the server stores one accepted absolute double-precision position. The approved existing backing store is `ActorComponent.WorldPositionD` for the live ECS actor. `Actor.WorldPos` is not an independent movement store: its redundant writable storage is removed by the movement integration, while spawn construction hands its resolved position directly to ECS. A future Shared-side read, if proven necessary, is read-only and derived from the server authority.

The source-specific handoff is fixed as follows:

- Player movement uses one project-owned PurrNet position RPC, replacing only the gameplay position part of `NetworkTransform`. After `CharacterController.Move`, the client uses its current `ClientPositionManager.OriginChunk` and `LocalToChunkRelative`, reuses `PackWorldPosition`, and sends a monotonically increasing sequence scoped to the ownership epoch. The server authenticates the owner binding and resolves `ActorSpatialContext`; client context is never authoritative. `MovementEndpoint.Server_RequestChunkInterest` remains interest bookkeeping. A rejected, stale, reordered or old-epoch proposal does not modify accepted state.
- `NetworkTransform` position sync is disabled only after the server-authored observer path is ready. Rotation and other suitable properties may remain on it. Observers receive one server-authored accepted `WorldPosition` update with tick/proposal sequence, and clients project it through their own floating origin. Smoothing is presentation-only and never writes back to ECS.
- NavMesh movement uses the actor's verified **simulation-region frame**. Many adjacent 256 m environment tiles share that one contiguous local frame, so ordinary tile crossings remain normal NavMesh movement and never require a physics-frame rebind or agent warp. Region-local positions are reconstructed to absolute double precision before authoritative ECS commit.
- ECS-driven NPC motion writes the canonical ECS component during the authoritative ECS step. Physics queries and GameObject projection convert through the actor's simulation-region frame; a GameObject transform never becomes a new source of truth.

All three sources converge at the fixed-tick boundary:

```text
source proposal/result and provenance
    -> source validation/reconstruction
    -> accepted WorldPosition -> ActorComponent.WorldPositionD
    -> ServerSpatialIndex.Move once for the committed tick
    -> ECS, GameObject, replication and interest projections
```

The spatial-index publication occurs only after the accepted commit. It may retain same-cell membership, but exact-range queries and observer updates must resolve the latest precise position from the canonical ECS component. A rejection must not publish or later overwrite a newer accepted commit. A player origin shift reconstructing the same absolute position is accepted at most once with zero displacement; it does not increment speed violations or change spatial state.

Conceptually:

```text
player input
    ↓
local movement simulation
    ↓
client local-space position
    ↓
absolute double-precision world position
    ↓
server validation
    ↓
authoritative actor spatial state
```

### 20.3.1 Simulation-region frame and NPC movement ownership

A server simulation region is keyed by full `ActorSpatialContext` plus a coarse region identity and contains **multiple adjacent 256 m environment tiles** in one contiguous local Unity coordinate frame. Environment tile boundaries are streaming/content boundaries only; crossing one must not change the actor's physics frame, warp a `NavMeshAgent`, or create a navigation gateway.

`ServerPhysicsManager` is the sole owner of active simulation-region physical state. It owns region creation/retirement, region-local physical coordinate conversion, physical isolation/placement, collider/NavMesh binding and physics-query execution. `ServerWorldManager` only owns environment scene streaming/identity, and `ServerPositionManager` only owns general position/network encoding conversions. Neither manager may retain a second region registry or physical-frame lifetime.

The sole mutable accepted absolute actor position remains `ActorComponent.WorldPositionD`. Spawn registration seeds that canonical value from an already resolved absolute position. `Actor.WorldPos` must not be restored as a second mutable position source, and `ServerPhysicsManager` must not cache another authoritative absolute actor position.

The existing tick ownership boundary is retained:

- **Enabled NavMesh agent:** the agent moves continuously inside the current simulation region. The pre-ECS NPC bridge obtains the verified region from `ServerPhysicsManager`, converts the region-local pose to absolute double precision and commits that value to `WorldPositionD`. ECS gravity does not advance this NPC.
- **Disabled or absent NavMesh agent:** ECS owns vertical movement. Ground/physics queries are executed through `ServerPhysicsManager`, which converts `WorldPositionD` into the verified region-local frame before querying Unity physics and returns/reconstructs results in absolute double precision before authoritative ECS state changes. The post-ECS projection uses the same verified region frame.
- **All NPCs:** actor identity and server-owned logical context resolve the current simulation region through the physical-simulation owner. The region association is representation state, not another accepted-position store. Client origin shifts and streamed tile load/unload do not alter it.

A **simulation-region boundary** is the only physical-frame transition. Such transitions are intentionally much coarser than 256 m tiles. `ServerPhysicsManager` owns physical detach/rebind, while movement/navigation logic preserves canonical absolute position, velocity/navigation intent and logical context unless gameplay explicitly performs a world/map/instance transition. The concrete handoff may remain in-process or move to another server worker; either way, it is not an ordinary environment-streaming operation.

Region dimensions must be configurable whole multiples of the shared 256 m origin/environment grid. Initial region spans and concurrent-region capacity are selected through representative server measurements before region integration is considered complete; the span is not inferred from the 64 m spatial-index cells, scene names or the fact that one origin cell and one environment tile share a coordinate.

NavMesh physical data belongs to `ServerPhysicsManager` because it shares the region-local physical frame. Do not create a separate navigation manager in Stage 1.2 unless later route-planning responsibilities independently justify one.

The previous per-tile packed-slot/gateway design is superseded and must not be used as the basis for new movement code.

### 20.3.2 Approved Stage 1.2 enabled-NavMesh region-boundary handoff

**Decision (Issue #165, 23 October 2026):** Enabled server `NavMeshAgent` actors cross only at a **calculated, validated seam between adjacent simulation regions**. The seam connects two independently built and physically separated region NavMeshes; it is not a Unity NavMesh link joining the datasets, an authored portal, an overlapping NavMesh band, or a gateway between 256 m environment tiles. A crossing is an authoritative movement step followed by a local physical-frame rebind, **not** a world/map/instance transfer. One region continues to own at most one registered NavMesh dataset for the configured agent type.

#### Seam discovery and reachability

- The candidate destination must be one of the four cardinally adjacent `SimulationRegionKey` values in the same valid `ActorSpatialContext`. Diagonal or nonadjacent physical handoffs are invalid. Use the configured whole-tile region dimensions, not scene/chunk names or local float positions.
- For the shared absolute X or Z boundary, derive paired **source approach** and **destination landing** positions in absolute double precision. Both use the same position along the boundary; each lies strictly inside its own region's half-open footprint. Start candidate search near the projection of the intended route toward the boundary, then examine alternatives outward in deterministic order. A search that exhausts the eligible boundary without a valid pair reports no seam. Budget expensive sampling/path queries across updates; an incomplete search is pending, not proof that the seam is blocked.
- Sample each candidate against the region's registered NavMesh using the correct agent type and allowed areas; reconstruct the sampled Unity-local point through that specific region descriptor and reject a point in any other region. Sampling radius, along-seam spacing, approach/landing offsets and arrival tolerance must be bounded from the agent dimensions and NavMesh build resolution, with any independently tunable limits centrally configured. Do not silently snap a candidate a large distance to a convenient NavMesh.
- Accept a candidate only with a complete NavMesh path from the source agent to the approach point and a complete path from the landing point to a usable onward destination within the receiving region (the target when it is there, otherwise the next reachable region-local route waypoint). A successful path-query boolean by itself is insufficient: partial and invalid paths fail. Require navigable clearance and walkable geometric continuity across the small seam gap, including collision obstruction, walkable slope and vertical difference within the configured agent climb/step capability. Nearest-point proximity alone cannot authorise passage through a wall, cliff or missing terrain.
- Choose a valid pair deterministically by the combined reachable travel cost, with a stable along-boundary coordinate tie-break. Do not retain old-frame `NavMeshPath.corners` as cross-region route data. Region-owned NavMesh rebuild/unload invalidates any pending seam that uses its changed content; it must be revalidated before admission.

These calculated **region** seams do not alter the continuous NavMesh generated from tiles inside either region. No per-tile gateway, persistent duplicate edge NavMesh, manually authored border transition or speculative navigation manager is approved.

#### Gameplay route intent and incremental multi-region progress

`ServerAutoAttackManager` remains the owner of an auto-attack chase and `Actor.Target` remains its target identity. Its existing per-attacker state may hold only a temporary current-hop plan: target ID, source/destination region keys, absolute approach/landing points and the effective absolute onward destination. This is navigation intent, **not** a second authoritative actor-position store or a general region-route registry. Other future NPC movement behaviours own their own intents and may reuse the physical seam-query/handoff operations.

- When attacker and target occupy the same region, preserve the existing direct local `SetDestination` behaviour and gameplay attack stopping distance.
- Otherwise, consult the target's latest canonical ECS absolute position and context. Choose one adjacent region that reduces Manhattan distance in region-grid coordinates to the target, first along the axis with the greater remaining displacement, then the other reducing axis if needed (stable axis tie-break). Advance **one adjacent region per successful handoff** and recalculate from the new region; a moving target may invalidate or change the next hop.
- There is no immediate teleport to a remote target and no precomputed multi-region corner list. Stage 1.2 deliberately does not support detours that first increase region-grid distance; if every distance-reducing adjacent seam is unusable, report unreachable navigation to the NPC's behaviour/recovery policy. A future independently justified coarse path-planning layer may extend this without changing the physical handoff contract.
- While a hop is pending, direct target coordinates from another physical region must never be passed to the current agent's `SetDestination`. Use its **source-region-local approach point** with a seam-appropriate near-zero stopping distance. Recompute/clear this state on target change, chase stop, actor despawn/death, or source/target region change. Changing the target's absolute position inside the same region updates the onward intent without treating the old position as immutable.

#### Crossing admission and authoritative tick

The chase owner may prepare a hop during its normal navigation update, but **only the established pre-ECS NPC movement boundary** may perform and accept the physical crossing. The crossing is eligible only when all of the following remain true:

1. the actor is alive, has the expected target and same logical context, and retains its verified source-region physical binding;
2. the enabled agent matches the registered source NavMesh type and remains attached, is not stopped, has no pending path, has a complete source approach path, and its remaining path distance is within the bounded seam arrival tolerance;
3. a currently valid source/landing pair passes the latest NavMesh and traversability checks, and the exact landing environment tile and destination region NavMesh are admitted as specified in the World Runtime PDD;
4. the candidate destination key reconstructed from the **absolute landing position and authoritative context** equals the planned immediately adjacent key.

For a positive-X boundary at `B`, the source endpoint has `x < B` and the destination endpoint has `x >= B`; the corresponding strict/inclusive inequalities reverse for an opposite-direction crossing. Apply the same rule to Z and negative world coordinates. No one absolute position belongs to both region footprints. The source agent must stop at a valid in-footprint approach point rather than drifting beyond its region and forcing the pre-ECS `ToWorld` bridge to interpret an out-of-region float.

The short approach-to-landing displacement is a specifically bounded, server-authorised seam traversal that has passed terrain, collision and movement-capability validation. It is **not** a jump to the target region's centre or a reinterpretation of the same Unity `Vector3` in a different anchor frame. The previous `ActorComponent.WorldPositionD` stays accepted until the landing absolute position is committed; the warp projects that exact selected landing position into the destination-local frame.

#### Agent snapshot, reattachment and commit

Capture before mutation: enabled state, `isStopped`, pre-handoff `velocity` as motion direction/speed intent, the gameplay `stoppingDistance`, effective target identity/absolute destination, and the currently effective route hop. The same `NavMeshAgent` retains its agent type, area mask, speed, acceleration, angular speed, avoidance and other locomotion configuration. The transfer is only available to an **enabled** agent; it must not silently switch to ECS-driven movement.

At one authoritative pre-ECS boundary:

1. Verify the latest canonical source position/actor context and the verified source-region pose; recheck target, both region NavMeshes, exact destination tile and the candidate seam. Temporarily retain both active physical regions for the in-flight operation so rollback cannot lose its source frame.
2. Quiesce the agent's current path as needed, then call `NavMeshAgent.Warp` using `ServerPhysicsManager.ToRegionLocal(destinationDescriptor, acceptedLandingAbsolutePosition)`. Require `Warp` success, `isOnNavMesh`, correct registered agent type and a resulting pose verified against the intended destination region. Do not assume a successful Warp just because the GameObject Transform moved.
3. Use `ServerPhysicsManager.TransitionActorRegion` for the existing same-context, no-region-creation physical membership change. Request a **new** destination-region path toward the next local waypoint or current target, restoring the appropriate stopping distance and stopped/moving state. Do not copy source `NavMeshPath`, path corners, `destination`, `nextPosition` or region-local vectors into the destination frame.
4. Only after successful physical reattachment and binding may the pre-ECS movement bridge accept the landing absolute position into `ActorComponent.WorldPositionD`. Later Stage 1.2 movement publication uses that sole canonical value to update `ServerSpatialIndex` once at the agreed tick boundary. A failed attempt must not publish a destination position or second movement stream. Release the temporary region holds after commit/rollback.

Because physical region frames differ by translation, the captured velocity vector's world direction does not require rotation. It represents motion **intent** for route continuation, not a promise of numerically identical Unity crowd-simulation velocity on the first frame after Warp. Do not assign it indiscriminately to `NavMeshAgent.velocity`: Unity treats that setter as manual velocity control that overrides path following, avoidance and acceleration. Ordinary agent simulation resumes on the newly constructed path. `SetDestination` success is only acceptance of a path request, not proof that its later asynchronous result stays reachable; normal navigation recovery applies if the resulting path becomes invalid.

#### Failure, rollback and AI recovery

- **No verified crossing/path:** the chase owner reports an unreachable route, allowing the AI behaviour to reposition, wait briefly, retarget or disengage/reset under the AI and Encounter Behaviour PDD. Do not repeatedly force `SetDestination` into an invalid frame or attack through geometry.
- **Region or exact destination tile unavailable, pending unload, or capacity exhausted:** do not create/borrow a frame or mutate accepted ECS/spatial state. Hold at the valid source-side waypoint or invoke bounded AI recovery. Environment loading remains the world/interest owner's responsibility.
- **NavMesh rebuild or seam loss:** invalidate the pending hop and recalculate. A route prepared against an earlier representation cannot authorise the handoff.
- **`Warp`, attachment, route request or membership failure:** restore the source physical binding and original verified source pose/navigation state before retrying; the old canonical position remains authoritative. Retain the source region through rollback. If Unity cannot restore a valid source agent, report a hard invalid-physical-state failure and stop its movement; never mark the transfer successful or conceal that recovery failed.
- **Target identity/context changed during preparation or transfer:** cancel stale intent and, if uncommitted, restore the source. A new target is resolved from current authoritative state after the old attempt terminates.

The operation must be same-process, non-interleaved and reversible until authoritative commit. It must never expose a partially rebound agent or dual physical membership as accepted gameplay state. Logical map/private-instance transfers remain a distinct later contract.

#### Future worker compatibility

Keep the logical handoff description portable: actor ID, full `ActorSpatialContext`, coarse integer region coordinates, canonical absolute position, bounded crossing/landing position, movement-intent vector, target ID/absolute route destination and an admission/commit identity. A future destination worker would confirm admission before source authority is relinquished. `LocalAnchor`, physical slot index, local Unity `Vector3`, `NavMeshDataInstance`, Unity scene identity and GameObject references are **never** fields of that portable contract. Stage 1.2 implements only the in-process physical handoff, not networked worker transfer.

---

### 20.4 Movement-driven spatial updates

Ordinary movement must automatically drive any required updates to:

- origin-cell membership;
- world-content streaming interest;
- actor network interest;
- current spatial/instance context.

Development-only manual interest controls must not be required in production.

The update is emitted only after the accepted-position commit, never from a render update, client origin shift, `CurrentChunk`, scene identity or an unvalidated transform. The committed position and immutable `ActorSpatialContext` are passed together to the spatial index. World-content interest and actor visibility remain separate consumers: crossing the aligned 256 m origin/environment boundary does not change logical context, and a rejected movement candidate changes neither membership nor exact-range truth. Floating-origin projection and content-streaming work may execute at different times even though the boundary coordinate is shared.

---

## 21. Movement State

The runtime movement model must be able to distinguish at least:

- walking;
- running;
- crawling;
- jumping;
- falling;
- surface swimming;
- mounted;
- passenger/on transport;
- movement impaired;
- immobilised;
- forced movement.

These states need not all be implemented through one enum or class.

The technical architecture may represent them differently as long as gameplay behaviour remains coherent and mutually incompatible states are validated correctly.

---

## 22. Death and Movement

Detailed death, corpse, graveyard and resurrection travel rules are outside this PDD.

The movement system must nevertheless be capable of entering whatever movement restrictions or alternate movement state the eventual death system requires.

No specific ghost-running or corpse-run mechanic is approved by this document.

---

## 23. Input and UX Requirements

Movement actions must support configurable input bindings.

At minimum the player must have clear controls for:

- directional movement;
- jump;
- walk/run selection;
- crawling;
- relevant swimming controls when swimming is available;
- mounting/dismounting once mounts are unlocked.

The game should communicate movement-state changes clearly through animation and character presentation rather than depending on intrusive UI.

When a player attempts an unavailable movement action, feedback should distinguish between cases such as:

- movement prevented by an effect;
- cannot mount here;
- destination lodestone undiscovered;
- insufficient gold;
- traversal route requires Shadowcraft.

---

## 24. Content-Authoring Requirements

World designers must be able to author traversal intentionally.

Required content-authoring concepts include:

- normal walkable ground;
- jumpable obstacles;
- crawl-only passages;
- swimmable water volumes;
- mount-compatible outdoor spaces;
- boat routes and boarding areas;
- moving lifts/platforms;
- lodestone locations;
- discovery triggers;
- seamless transition spaces;
- Shadowcraft traversal shortcuts.

Traversal requirements must be explicit enough that designers do not need to depend on accidental collider gaps or undefined controller behaviour.

---

## 25. Current Implementation Baseline

The existing repository already establishes part of this architecture.

The current client movement prototype includes:

- Unity `CharacterController` movement;
- direct movement input;
- grounded jumping;
- gravity;
- maximum fall velocity;
- preservation of horizontal velocity while airborne;
- local client movement simulation;
- PurrNet owner-authoritative movement replication;
- server position validation and correction.

The current prototype has an implementation movement speed of **5 units per second** and jump-power value of **4**, while global configuration currently uses:

- gravity: **-9.81**;
- maximum fall speed: **50**.

These are current implementation values, not locked balance values established by this PDD.

The existing implementation does not yet provide the complete intended systems for:

- walk/run selection;
- crawling;
- crawl-based NPC detection modification;
- fall damage;
- swimming;
- mounts;
- boats;
- lifts;
- lodestones;
- production movement-driven world-streaming updates;
- authoritative absolute movement reconstruction throughout the full floating-origin pipeline.

---

## 26. Required Completion of the Existing Movement Architecture

The existing movement system should be extended rather than replaced unnecessarily.

Required work includes:

### 26.1 Authoritative world-position integration

Client movement must produce sufficient information for the server to maintain the correct double-precision absolute world position.

Server movement validation must not assume that client-local float coordinates are global coordinates.

### 26.2 Automatic spatial-interest updates

Movement across relevant spatial boundaries must automatically update origin and streaming/interest state.

### 26.3 Movement modes

The controller must support the designed walk, run, crawl, surface-swim and mounted states.

### 26.4 Server validation

The server must validate permitted speed and movement capabilities according to the actor's current state rather than relying on one universal maximum speed.

### 26.5 Fall state

The movement system must retain sufficient authoritative information to resolve fall damage reliably.

### 26.6 Moving transport

Player positioning must support boats, lifts and other moving platforms without unstable network or physics behaviour.

---

## 27. Intentionally Unspecified Values

The following values are deliberately not fixed by this PDD:

- walking speed;
- final running speed;
- jump height or jump velocity;
- crawl speed multiplier;
- crawl detection-radius modifier;
- safe fall distance;
- fall-damage curve;
- final maximum fall velocity;
- swimming speed;
- mount speed;
- exact mount-unlock level;
- exact lodestone-unlock level;
- lodestone travel cost formula;
- boat speeds and schedules;
- lift speeds;
- exact movement-impairment values;
- exact server movement-validation tolerances.

These should be data/configuration driven where appropriate and tuned through gameplay testing.

---

## 28. Locked Design Decisions

The following decisions are currently locked by this PDD:

1. Ninth Age has walking and running, but no sprinting system.
2. Walk/run selection is controlled through a keybind.
3. Players can jump.
4. Players can crawl.
5. Crawling is slow, reduces NPC detection radius and enables authored tight-space traversal.
6. Players can fall and take fall damage.
7. Fall damage should become dangerous relatively quickly while remaining playable.
8. Initial swimming is surface-only.
9. Full 3D swimming is a planned later extension.
10. Combat does not intrinsically alter movement.
11. Movement-impairing effects are normal combat mechanics.
12. Knockbacks and other forced movement are comparatively rare and primarily intended for authored encounter mechanics.
13. Ground mounts unlock during mid-to-high-level progression.
14. Mounts may be used throughout outdoor areas.
15. Flying mounts are not supported.
16. Boats and lifts are part of the physical transport system.
17. Free general-purpose teleportation is not a standard player traversal system.
18. Teleportation is heavily restricted in the setting because of its disastrous magical consequences.
19. Lodestones are the normal fast-travel network.
20. Lodestones are unlocked by a quest around the early-to-mid-level progression boundary.
21. Normal lodestone travel must begin at a lodestone.
22. Destination lodestones must first be discovered.
23. Lodestone travel costs gold.
24. Rare class-specific exceptions, such as a Sorcerer lodestone portal, may be designed separately.
25. Climbing and agility-style traversal shortcuts are not universal movement abilities and belong to the Shadowcraft profession.
26. Player movement remains compatible with the double-precision, floating-origin and additive world-streaming architecture.

---

## 29. Dependencies

This PDD depends on or constrains:

- **World Runtime and Instancing PDD** — authoritative coordinates, floating origin, streaming and seamless instance boundaries;
- **World and Zone Design PDD** — roads, terrain, transport routes, water, lodestones and traversal geography;
- **Combat System PDD** — movement restrictions, roots, slows and forced movement during combat;
- **AI and Encounter Behaviour PDD** — crawl-based perception modification and NPC detection;
- **Character Stats and Progression PDD** — mount and lodestone progression timing;
- **Quest, Narrative and Dialogue PDD** — lodestone unlock quest and teleportation lore;
- **Class Design PDD / Abilities and Talents PDD** — exceptional class movement and teleportation abilities;
- **profession design documentation** — Shadowcraft climbing and agility shortcuts;
- **Items, Equipment and Loot PDD** — mount items or ownership where applicable;
- **Dungeon and Group Content PDD** — encounter-specific forced movement and dungeon traversal requirements.

---

## 30. Validation Criteria

The movement and traversal system satisfies this design when:

1. Players can switch between walking and running with a keybind.
2. Running does not use a sprint/stamina system.
3. Players can jump and fall naturally through the world.
4. Ordinary combat does not automatically slow player movement.
5. Movement-impairing combat effects can deliberately modify otherwise normal movement.
6. Players can enter a crawl state and move substantially more slowly.
7. Authored crawl spaces can be traversed while standing characters cannot pass through them.
8. Crawling reduces NPC detection according to authoritative perception rules.
9. Falling from small heights is safe while progressively larger falls become threatening and eventually potentially fatal.
10. Players can enter water and perform surface swimming.
11. The movement architecture can later be extended to full 3D swimming without replacement.
12. Players can acquire and use ground mounts during later progression.
13. Mounts function across outdoor world regions without arbitrary zone restrictions.
14. Players cannot use flying mounts.
15. Players can travel correctly on moving boats.
16. Players can travel correctly on moving lifts/platforms.
17. Forced movement can be authored for selected encounter mechanics without becoming ubiquitous.
18. Lodestone travel remains unavailable until its progression quest has been completed.
19. A player must reach a lodestone before initiating standard lodestone travel.
20. Undiscovered lodestones cannot be selected as destinations.
21. Lodestone travel charges the appropriate gold cost.
22. Lodestone discovery and unlock state persist correctly.
23. Profession-gated Shadowcraft traversal routes can exist without providing universal climbing to every character.
24. Player movement remains responsive under ordinary network latency.
25. The server can reject or correct illegal movement.
26. Authoritative actor movement is represented in double-precision world coordinates.
27. Crossing floating-origin cells does not visibly interrupt movement.
28. Crossing streamed world tiles does not produce a loading screen.
29. Movement naturally carries players through seamless public interiors and properly authored seamless instance entrances.
