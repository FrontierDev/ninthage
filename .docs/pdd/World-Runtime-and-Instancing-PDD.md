# Ninth Age — World Runtime and Instancing Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Persistent worlds, world streaming, spatial partitioning, floating origin, actor visibility, seamless interiors, instanced spaces, map transitions and runtime scene authoring  
**Last updated:** 2026-10-22

---

## 1. Purpose and Authority

This document defines the intended world-runtime and instancing model for **Ninth Age**.

It is authoritative for:

- player-selectable persistent worlds;
- the relationship between absolute world coordinates and client-local coordinates;
- open-world spatial streaming;
- world-content and terrain tiles;
- the floating-origin system;
- server-authoritative actor positioning;
- spatial network interest and actor visibility;
- public seamless interior regions;
- private seamless instances;
- detached-map instances;
- instance access scopes;
- runtime instance creation and lifecycle;
- Unity scene and Addressables authoring requirements for world and instance content;
- player transitions between world regions, interiors, instances and maps.

This document does not define:

- world geography, zone identities or biome design — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- dungeon encounter design, boss structure, rewards, lockouts or dungeon pacing — [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- detailed NPC behaviour — [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md) and [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- detailed movement mechanics — [Movement and Traversal PDD](Movement-and-Traversal-PDD.md);
- player housing rules beyond the runtime requirements necessary to support private seamless interiors;
- detailed group-management rules — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md).

The current C# architecture and working world-streaming implementation are the baseline for this design. The existing architecture should be completed and generalised rather than replaced without a deliberate architectural review.

---

## 2. Design Pillars

### 2.1 The world should feel continuous

The open world should feel geographically continuous rather than like a collection of disconnected levels.

Technical streaming boundaries must normally be invisible to the player.

Crossing a terrain tile, world-content boundary or floating-origin boundary must not produce a loading screen.

### 2.2 Streaming systems should be invisible infrastructure

Terrain loading, additive scene loading, actor-interest changes and floating-origin corrections exist to support the world.

Players should not perceive those mechanisms during ordinary traversal.

### 2.3 Double-precision world position is authoritative

An actor's authoritative spatial position is its absolute double-precision world position.

Unity `Transform.position` is a local float-space representation used where required for rendering, physics, navigation and other Unity systems.

Gameplay must not treat a floating-origin-adjusted float transform as the authoritative absolute position.

### 2.4 World streaming and actor visibility are related but separate

Loading nearby terrain or world content does not automatically mean every actor in those scenes should be replicated to the client.

World-content streaming determines which environment content is available.

Actor-interest management determines which networked units a player is permitted to know about.

### 2.5 Interiors should feel physically connected where appropriate

A cave, mine, house, castle interior or dungeon does not inherently require a loading screen.

Where the fiction and physical layout support it, Ninth Age should seamlessly stream between exterior and interior content.

### 2.6 Instancing is a simulation concept, not a scene concept

A Unity scene is an authored content asset.

An instance is a particular runtime simulation context.

Several runtime instances may use the same underlying Unity scene assets while maintaining completely independent actor and gameplay state.

### 2.7 World identity should be visible and meaningful

Ninth Age uses explicit persistent worlds rather than invisible open-world layering.

Players should normally remain on the world they selected until they deliberately leave it.

---

## 3. Runtime Terminology

The following terms must remain distinct.

### 3.1 World Position

The absolute double-precision position of an actor or other spatial entity.

World positions are independent of the client's current floating origin.

### 3.2 Origin Cell

A regular spatial cell used by the floating-origin and position-encoding system.

The current implementation uses:

**100 × 100 world units**

for the horizontal origin-cell grid.

Origin cells are not themselves terrain assets.

Crossing an origin-cell boundary may cause the client's floating origin to move.

### 3.3 World Tile

A streamed unit of open-world environment content.

The existing terrain-authoring pipeline uses:

**256 × 256 world units**

per terrain tile.

World tiles contain or correspond to streamed environment content such as terrain and world objects.

The world-tile system is distinct from the 100 m origin-cell system.

### 3.4 Additive Scene

A Unity `.unity` scene loaded alongside other scenes rather than replacing the currently loaded world.

Open-world content, interiors and instance content may all use Addressable additive scenes.

### 3.5 World

A persistent player-selectable copy of the open world, such as `World 32`.

A world represents a coherent open-world simulation and community context.

### 3.6 Map

A distinct spatial coordinate space.

The primary open world is one map.

A battleground, raid realm, alternate dimension or other detached location may use another map.

### 3.7 Instance

A runtime simulation context that isolates state between groups of players even when the same authored scene assets are reused.

---

## 4. Persistent World Model

Ninth Age uses **explicit player-selectable worlds** rather than invisible dynamic open-world layering.

Players should be able to see which world their friends, party members and other relevant social contacts are currently using.

World switching is an explicit player action during normal operation.

The game must not routinely collapse layers or move players between open-world copies without their control.

A player's character is not intrinsically bound to only one world. Persistent character state may move between worlds according to the game's world-selection rules.

The system should support a future concept of a **home world**, allowing systems such as player housing, persistent community identity or other world-specific ownership to attach significance to a chosen world if later PDDs require it.

A logical world does not need to correspond permanently to one physical machine. The infrastructure may later distribute a world across multiple server processes while preserving the player's perception of one coherent world.

---

## 5. Absolute Position and Floating Origin

### 5.1 Authoritative position

Actors must maintain an authoritative position using double precision.

The current architecture represents this through `WorldPosition` and server-side `double3` position data.

The authoritative position must be used for gameplay calculations that require spatial accuracy, including:

- range checks;
- spell targeting;
- melee distance;
- interaction distance;
- aggro and perception distance;
- area-of-effect membership;
- server movement validation;
- actor spatial indexing;
- determining relevant world-content regions.

Float transforms must not replace this data as the authoritative world position.

### 5.2 Client-local position

Unity rendering and normal GameObject operations continue to use `Vector3` positions.

The client derives these positions relative to its current floating origin.

Conceptually:

```text
absolute WorldPosition
        ↓
subtract client floating origin
        ↓
local Vector3
        ↓
Unity Transform
```

### 5.3 Origin cells

The current implementation uses a 100 m origin-cell size.

Crossing an origin-cell boundary permits the client to move its floating origin so that active gameplay remains close to Unity's coordinate origin.

Changing origin must shift relevant loaded scene roots and actors without changing their absolute double-precision world positions.

A floating-origin shift is not a teleport and must not:

- alter gameplay distance;
- modify absolute actor position;
- change which world a player occupies;
- inherently cause an instance transition;
- inherently require loading different terrain or environment content.

### 5.4 Networking

Movement replication must preserve enough spatial information for the server to reconstruct an actor's absolute double-precision position.

A client-local `Vector3` must never be interpreted by the server as an absolute world position after floating-origin adjustment.

Chunk/cell-relative position encoding may be used for transport, provided the resulting server position is reconstructed into authoritative double precision.

The inspected installed transport does not meet this requirement by itself. `Assets/Prefabs/PlayerPrefab.prefab` uses an unparented root `PurrNet.NetworkTransform` with world-position sync, owner authority and parent sync enabled. The project-owned PurrNet source stores its transform payload as `NetworkTransformData` containing compressed `Vector3` values; the owner-auth server path applies those values directly to the server transform. It has no chunk/context field or origin-qualified position codec, and the prefab has no stable shared parent frame.

The fixed solution is one project-owned PurrNet position RPC/serialization path. After `CharacterController.Move`, the client uses its current `ClientPositionManager.OriginChunk` and `LocalToChunkRelative`, then reuses `PackWorldPosition` to send chunk/local `WorldPosition` plus a monotonically increasing ownership-epoch proposal sequence. The server authenticates the sender and actor binding, resolves `ActorSpatialContext` itself, orders and validates the proposal at the authoritative tick, and commits the resulting absolute double position to the ECS component. A client origin shift reconstructs the same absolute position and is zero displacement. `MovementEndpoint.Server_RequestChunkInterest` remains interest bookkeeping, not movement authority.

The project-owned path replaces only `NetworkTransform` position. `NetworkTransform` remains available for rotation and other suitable properties, and its position sync is set to `No` only in the coordinated #136 cutover after server-authored observer position updates and client-origin projection are functional. No vendor source is modified and no competing gameplay position stream is retained. #130 owns proposal sending/serialization; #131 owns validation and canonical commit; #136 owns observer updates and the cutover.

### 5.5 Server-local physics/NavMesh frames (approved Stage 1.2 contract)

Client floating-origin cells (100 m), environment tiles (256 m), server physics slots and logical `ActorSpatialContext` are four distinct concepts. The server uses one shared Unity Physics/NavMesh world with **16 × 16 non-overlapping physical slots at 512 m pitch** initially, subject to validated authored extents. Each live slot represents precisely one `(WorldID, MapID, InstanceID, environment tile)` key; the authoritative slot owner is `ServerPositionManager` (#134). Slot keys must not be inferred from `CurrentChunk`, Unity scene names, origin cells or PurrNet scene identity. The slot is a temporary physical representation, **never an alternative position or logical-instance authority**.

Slot registration owns a verified association of context/tile, absolute double-precision tile origin, local float slot anchor, assigned geometry/NavMeshData, resident physics actors/agents and lifetime. A tile's slot must be reserved **before** its authored scene colliders, NavMeshData, actors and physics queries become active. All use the same placement and conversion:

```text
physics-local float = slot anchor + (absolute double position - absolute double tile origin)
absolute double position = absolute double tile origin + (physics-local float - slot anchor)
```

The subtraction/inverse reconstruction is performed in double precision before conversion at the slot boundary; never cast large absolute world coordinates directly to Unity floats. A physical-slot change must preserve `ActorComponent.WorldPositionD` and `ActorSpatialContext`; it is not a teleport. Slots are stable while referenced; a slot cannot be reused until scene geometry, colliders, NavMeshData, actor/agent bindings and in-flight physics use are safely retired. Missing, stale, conflicting or exhausted slots fail explicitly rather than aliasing another tile or falling back to global-origin coordinates. Frame lookup for ECS code uses authoritative actor ID/context binding without introducing another mutable absolute position.

**#134 also owns frame-aware ECS ground detection.** `ActorUpdateSystem.OnUpdate` currently casts `ActorComponent.WorldPositionD` directly to a `Vector3` ray origin and writes `Physics.Raycast`'s local `hit.point.y` back to absolute ECS Y. For gravity-owned NPCs, the query origin must be converted into the actor's verified slot; the entire hit point must be reconstructed into absolute doubles through that *same frame* before changing `WorldPositionD.y`. Preserve the existing ground mask, distances, velocity, grounded-state and gravity rules, including the exclusion of players and enabled NavMesh agents. #132 consumes the established frames for enabled NavMesh-agent pre-ECS position acceptance and disabled-agent post-ECS projection; #135 owns route gateways and agent handoff. #114 alone publishes accepted ECS positions to the spatial index.

**Implementation sequencing:** the full frame system crosses manager/scene/ECS owners and is intentionally **not** one three-file/~150-line changeset. #134 is a tracking issue whose ordered, narrowly scoped children establish (1) frame registry and conversion, (2) environment geometry/NavMesh slot placement and retirement, (3) actor spawn/frame association, and (4) ECS ground-raycast integration. Each stage requires source review and owner-only Unity/headless validation before advancing. Changes that require new state owners, vendor edits or asset-layout decisions outside the approved design remain separate approval gates; no speculative workaround or partial origin-zero sign-off is permitted.

---

## 6. Open-World Content Streaming

### 6.1 Existing model

Ninth Age uses **Addressable additive scenes** for streamed world content.

`ServerWorldManager` is responsible for loading and unloading world-content scenes.

PurrNet scene membership is used to deliver relevant scenes to connected players.

The existing implementation loads world scenes privately and adds interested players to them.

This overall architecture is retained.

### 6.2 World tiles

The terrain-authoring system currently produces 256 × 256 m terrain tiles.

The world-runtime architecture must preserve a distinction between these world-content tiles and the 100 m floating-origin cells.

A floating-origin transition may therefore occur without crossing a streamed world-tile boundary, and a world-tile transition may occur independently of an origin correction.

### 6.3 Streamed world content

A world tile may contain or reference:

- terrain;
- terrain collision;
- static environment geometry;
- buildings;
- vegetation;
- local props;
- environmental trigger volumes;
- authored spawn locations;
- other spatially local world content.

Persistent runtime actors do not need to reside physically inside the Unity scene that originally defined their spawn.

### 6.4 Streaming radius

The current runtime uses a **3×3 nearby-scene interest set** around the player's current world region.

This remains the initial implemented behaviour.

The architecture must not permanently hard-code the assumption that all content types require exactly the same 3×3 radius.

Streaming ranges must be capable of becoming configuration-driven as the world and performance requirements mature.

### 6.5 Terrain LOD

Terrain visibility may extend substantially farther than detailed environment-object streaming.

The terrain system should therefore be able to retain distant representations while detailed nearby content is loaded and unloaded independently.

The exact LOD distances belong to implementation/performance tuning and are not fixed by this PDD.

---

## 7. Persistent Actor Scene

The existing `global_actors` architecture is retained.

Networked runtime actors exist in a persistent actor scene rather than being destroyed solely because a streamed environment scene unloads.

This separates:

- **environment-content lifetime**, from
- **actor simulation lifetime**.

An actor may logically belong to a world region or instance without its GameObject belonging to the additive environment scene representing that region.

Actor spatial ownership must therefore be determined from authoritative runtime spatial data, not from `GameObject.scene`.

NPC lifecycle rules beyond this requirement belong to the NPC, AI and persistence systems.

---

## 8. Actor Spatial Membership and Visibility

### 8.1 Broad-phase interest

The existing chunk/scene subscription system remains the broad-phase mechanism for determining which actors could be relevant to a player.

A client should not need to evaluate every actor in the world.

### 8.2 Range-based visibility

Actor replication must additionally support **range-based visibility**.

An actor should only be replicated to a player when:

1. the actor is within a spatial region relevant to that player; and
2. the authoritative double-precision distance between the player and actor is within the applicable actor-visibility range.

The owner-approved initial actor-visibility entry and exit distances are specified in §8.6. They are configurable engineering defaults, not immutable gameplay or capacity limits. The values must not be embedded throughout gameplay code.

### 8.3 Precision

Visibility and gameplay range calculations must use double-precision authoritative positions.

Client-local float transforms are not authoritative for these checks.

### 8.4 Visibility hysteresis

The implementation should support a small difference between visibility-entry and visibility-exit thresholds where required to prevent an actor repeatedly appearing and disappearing while moving around a range boundary.

The initial entry/exit thresholds are recorded in §8.6 and remain subject to later performance and gameplay tuning.

### 8.5 Dynamic membership

Actor spatial membership must update when an actor moves.

An actor that crosses a spatial boundary must not remain permanently registered against its spawn region.

Visibility indexes and other spatial registries must follow the actor's authoritative current location.

### 8.6 Approved Stage 1.2 spatial-index and actor-visibility defaults

The following **owner-approved initial engineering defaults** are the starting configuration for Stage 1.2:

| Configuration setting | Initial value | Meaning |
| --- | --- | --- |
| Spatial-index cell width | **64 m** | Independent horizontal broad-phase spatial-index cell width. |
| Actor visibility entry radius | **80 m** | A previously non-visible actor may enter an observer's replication interest at or within this exact authoritative distance, subject to valid matching world/map/instance context and other interest rules. |
| Actor visibility exit radius | **96 m** | An already-visible actor remains within distance-based interest until its authoritative distance exceeds this radius, subject to continued valid matching context and other interest rules. |
| Visibility hysteresis | **16 m** | The difference between exit and entry radii; helps avoid rapid visibility toggling around the boundary. |

All three primary settings must be **independently configurable** through the existing `GameConfiguration` authority. Require a finite, positive cell width; finite, positive entry and exit radii; and **exit radius >= entry radius** (equal radii may disable hysteresis). Use authoritative double-precision distance calculations, even if the Inspector-authored configuration fields use `float`. Do not infer these values from, or change, the **100 m origin-cell grid**, **256 m terrain tile grid** or current **3×3 environment-scene subscription grid**.

These values affect **actor spatial candidate indexing and network visibility**, not spell range, combat reach, NPC aggro/perception rules, world streaming distance, PurrNet transport settings or world capacity. Relevance and visibility must still respect the complete logical spatial context from §§18.1–18.2. The initial defaults are provisional engineering choices to be revisited using Stage 1.2 representative multiplayer/density measurements; they are **not validated performance guarantees**.

---

## 9. Public Seamless Interiors

An ordinary public cave, mine, building interior or similar space is **not inherently an instance**.

Where everyone entering the location on the same world should encounter the same players, NPCs and state, the interior remains part of the same open-world simulation context.

Example:

```text
World 32 open world
        │
        │ cave entrance
        ▼
public cave interior
        │
same world
same shared simulation
no loading screen
```

The interior may nevertheless use separate Addressable additive scene content.

### 9.1 Streaming transition

As the player approaches the entrance, relevant interior scene content may be preloaded.

After the player crosses sufficiently far into the interior:

- distant outdoor content may be unloaded;
- outdoor content may move to cheaper LOD representations;
- interior content becomes the primary detailed world content;
- actor-interest calculations continue normally within the same world context.

The reverse occurs when leaving.

### 9.2 Transition spaces

Entrances should be authored to provide sufficient physical transition distance where practical.

Examples include:

- cave tunnels;
- corridors;
- staircases;
- gates;
- entrance halls;
- lifts;
- narrow passages.

These spaces allow destination content to stream before it becomes visible.

### 9.3 Failure to complete preload

The normal experience must not expose a loading screen for a seamless public interior.

If required content is not ready before the player reaches the final transition boundary, movement may be temporarily prevented from crossing that boundary rather than allowing the player into unloaded space.

This is a fallback condition, not the intended normal experience.

---

## 10. Instance Model

An instance is a **logical simulation context**.

It must not be defined merely as "a Unity scene".

The same authored scene content may be used simultaneously by several runtime instances.

For example:

```text
Dungeon_Crypt.unity
        │
        ├── Runtime Instance 1042 — Party A
        ├── Runtime Instance 1043 — Party B
        └── Runtime Instance 1044 — Party C
```

Each runtime instance may maintain independent:

- actors;
- encounters;
- doors;
- object state;
- progression state;
- temporary instance state.

The authored Unity scene remains reusable content.

---

## 11. Seamless Private Instances

Some spaces must appear physically connected to the open world while providing isolated simulation state.

Examples include:

- five-player dungeons entered through a physical cave or doorway;
- player-owned housing;
- guild-owned interiors;
- other private or group-scoped spaces.

These are **seamless spatial instances**.

### 11.1 Entry

The player approaches a physical entrance in the open world.

Before the final transition boundary, the required Addressable scene content should begin loading.

When the player crosses the authoritative boundary, the server resolves the destination runtime instance.

The transition should occur without a conventional loading screen.

### 11.2 Party dungeon example

Two unrelated parties may enter the same physical dungeon doorway.

They resolve to different runtime dungeon instances:

```text
World 32
    │
    └── dungeon entrance
          ├── Party A → Dungeon Instance 501
          └── Party B → Dungeon Instance 502
```

The underlying Unity scene assets may be identical.

The players do not see or interact with actors belonging to the other party's instance.

### 11.3 Player housing example

A house exterior may exist visibly in the shared open world.

Crossing its entrance resolves the relevant property instance.

Visitors permitted to enter that property resolve to the same property interior as the owner.

The interior is not required to remain loaded when nobody is present.

Detailed housing ownership and permission rules belong to a future housing design document.

---

## 12. Detached-Map Instances

Some content intentionally occupies a different map or spatial context.

Examples include:

- battlegrounds;
- portal-accessed raid maps;
- arenas;
- other dimensions;
- distant scenario spaces;
- environments for which physical continuity with the open world is unnecessary.

These are **detached-map instances**.

A conventional loading transition is permitted.

Conceptually:

```text
Open World
    │
    │ portal / queue / teleport
    ▼
Loading transition
    ▼
Detached map instance
```

The previous open-world scene set may be unloaded completely.

The new map establishes its own spatial context and relevant additive scene set.

Returning performs the corresponding reverse transition.

---

## 13. Instance Scope

Instance visibility and ownership must be explicitly scoped.

The runtime model must be capable of supporting at least:

### 13.1 Party scope

Players belonging to the same qualifying party resolve to the same runtime instance.

This is the normal model for a conventional five-player dungeon.

### 13.2 Raid scope

A raid group resolves to a shared raid instance where required.

Detailed raid lockout and group rules belong to the relevant group/dungeon PDDs.

### 13.3 Player / property scope

The destination is resolved from a specific player-owned or property-owned space.

This supports player housing and similar systems.

### 13.4 Guild scope

Members or invited visitors may resolve to a persistent guild-owned interior.

### 13.5 Explicit instance scope

Systems must also be capable of directing a player to a specifically identified existing instance where required.

The access-policy system should be extensible rather than implemented as unrelated special cases for every content type.

---

## 14. Runtime Instance Creation

Instances are created from reusable authored content.

A runtime instance must have a unique runtime identity separate from the scene assets from which it was created.

Conceptually, runtime state includes:

```text
Instance ID
Instance/content definition
Map
Scope
Owning party/player/guild where applicable
Active players
Loaded scene set
Runtime actor state
Runtime encounter/object state
Lifecycle state
```

### 14.1 Creation timing

Private instances should normally be created lazily.

Examples:

- a party dungeon may be created when the first valid party member commits to entry;
- a house interior may become active when the owner or permitted visitor enters;
- a guild interior may become active when first required.

The game should not maintain every possible private interior as a permanently active Unity scene.

### 14.2 Reuse

When another eligible player should join an already active instance, the resolver should return that existing runtime instance rather than create another copy.

For example, the second member of a party entering a dungeon must join the party's existing dungeon instance.

---

## 15. Instance Lifecycle

The instance runtime must support at least the following states conceptually:

```text
not created
    ↓
creating/loading
    ↓
active
    ↓
empty/inactive
    ↓
suspended or destroyed
```

The exact gameplay reset rules depend on content type.

### 15.1 Empty instances

An instance does not need to keep all Unity scenes actively loaded merely because persistent state may still exist.

Empty instance content may be unloaded while preserving whatever runtime or persistent state is required by the owning gameplay system.

### 15.2 Re-entry

Content whose rules permit re-entry must be capable of resolving the player back into the appropriate existing instance.

### 15.3 Destruction

An instance may be destroyed when its governing gameplay rules no longer require it.

For dungeons, detailed reset, expiry and lockout behaviour belongs to the Dungeon and Group Content PDD.

For housing, persistent property state must survive unloading and process restarts according to the eventual housing/persistence design.

---

## 16. Unity Authoring Model

The system must provide a practical Unity workflow for designers.

### 16.1 Dungeon and interior scenes

A dungeon or interior can be authored as a normal Unity `.unity` scene.

A small interior may use one scene.

A sufficiently large dungeon or interior may be composed from several scenes loaded additively.

Example:

```text
AncientCrypt_Entrance.unity
AncientCrypt_LowerHalls.unity
AncientCrypt_BossWing.unity
```

### 16.2 Addressables

Runtime world and instance scenes must use the existing Addressables-based loading architecture.

The instance system must not introduce an unrelated content-loading pipeline.

### 16.3 Instance definition

Instanced content requires an authored definition describing the runtime behaviour associated with its scene content.

The definition should provide, directly or through referenced data:

- stable content/template identity;
- associated scene or scene set;
- map/context information;
- transition type;
- access/scope policy;
- entry anchors;
- exit/return anchors;
- preload requirements;
- capacity where relevant;
- lifecycle/reset policy references;
- persistence requirements.

The exact class or ScriptableObject implementation belongs to technical design.

### 16.4 Entrance authoring

A seamless entrance must be authorable directly in the Unity editor.

The authoring workflow must support:

- an entrance or transition volume;
- the destination definition;
- preload trigger volume or distance;
- authoritative transition threshold;
- entry anchor;
- return/exit anchor;
- optional direction/orientation information.

Designers should not need to hard-code instance transitions in gameplay scripts for each dungeon or house.

---

## 17. Relationship to Existing Additive Scene Architecture

The instance system extends the existing world-streaming architecture.

It does not replace:

- `ServerWorldManager`;
- Addressable scene loading;
- additive Unity scenes;
- PurrNet scene subscriptions;
- the persistent actor-scene concept;
- client floating-origin handling.

The runtime architecture must evolve from identifying loaded content only by a global spatial coordinate to being capable of identifying content by both **spatial context** and **runtime instance context** where necessary.

The same Addressable scene asset may therefore be loaded for more than one runtime instance.

Open-world scene loading remains a special case of the same general streamed-scene infrastructure.

---

## 18. Scene and Simulation Identity

Scene identity and simulation identity must remain separate.

The following are different concepts:

```text
Authored scene asset
Runtime loaded scene
Runtime instance
Logical map
Persistent world
```

Code must not assume that two players viewing content originating from the same `.unity` scene are necessarily in the same gameplay instance.

Similarly, two additive scenes may form part of one runtime instance.

### 18.1 Authoritative spatial-identity contract

The authoritative spatial context of an actor or spatial query is the complete `(WorldID, MapID, InstanceID)` value. **This is the approved Stage 1.2 identifier contract**, independent of `WorldPosition` and scene/content identities.

| Identifier | C# representation | Meaning and lifetime |
| --- | --- | --- |
| `WorldID` | `System.Guid` | Identity of a hosted logical world, distinct from its human-readable server launch name, scene ID and account `HomeWorldID`. The long-term persistent-world design requires an identity stable across server restarts; the expressly approved single-world testing exception in §18.3 generates a new GUID on each launch. |
| `MapID` | `string` | Stable, case-sensitive logical map definition/coordinate-space identity. One map can contain many additive scenes and can be used in multiple worlds or runtime instances. It is **not** a chunk name, scene path or PurrNet `SceneID`. |
| `InstanceID` | `System.Guid` | Identity of a particular **private runtime simulation instance**, issued by the server-local trusted instance owner. Nonempty IDs must be unique among live private instances hosted by that server; identity is interpreted in combination with `WorldID` and `MapID`, not as a global cross-server allocation key. |

A **public-world instance** means the common non-private simulation for one specific `WorldID` and `MapID`. It is represented by **`InstanceID == Guid.Empty`**, deliberately reserved for the public context rather than allocated to a private instance. The public context is **not globally shared**: `(World A, Main Map, Guid.Empty)` and `(World B, Main Map, Guid.Empty)` are separate, as are public contexts on two maps of one world. An ordinary shared cave or public interior on the same logical map retains the same public context despite changing environment scenes. A seamless private interior uses a nonempty instance ID even if it shares the exterior's map.

### 18.2 Validity, equality and compatibility

- `WorldID == Guid.Empty` is invalid/unassigned. A `MapID` that is `null`, empty or whitespace-only is invalid/unassigned. A default-initialised spatial-context value is therefore invalid. **`InstanceID == Guid.Empty` by itself is valid** when the other two identifiers are valid, and means *public*.
- Construct valid contexts explicitly through named operations such as `CreatePublic(worldID, mapID)` and `CreateInstanced(worldID, mapID, instanceID)`. Both reject an invalid world or map; the latter also rejects `Guid.Empty` as a private instance ID. Do not silently substitute a prototype world, main map or public instance when supplied identity is missing.
- `ActorSpatialContext` is an immutable C# `readonly struct` implementing `IEquatable<ActorSpatialContext>`, with `IsValid`, value equality and matching hash semantics. Equality compares **all three** fields; map comparison and hashing use `StringComparer.Ordinal`, without culture-sensitive or case-insensitive matching. Map IDs are issued in their canonical authored form rather than guessed or normalised from scene names.
- **Compatibility** for spatial lookup, replication, targeting, interaction, aggro and combat requires **both contexts valid and all three identifiers exactly equal**. Even if two default/invalid structs are equal as values, they are *not compatible for gameplay*. Reject or report attempts to use invalid contexts at server authority boundaries; never let invalid contexts populate a shared spatial bucket.
- Actor location is the combination of this logical context **and** authoritative double-precision world position within its map. Float transforms, origin cells, world tiles, `Actor.CurrentChunk`, `ActorComponent.CurrentScene`, `NetworkIdentity`, `GameObject.scene` and scene asset names cannot supply or replace any member of the identity triple.

The position authority is deliberately separate from this context identity. `Actor.SpatialContext` is the immutable server-assigned `(WorldID, MapID, InstanceID)` value; it is not inferred from a movement proposal, scene, chunk-interest request or floating-origin frame. The accepted position and context are published together at the authoritative tick boundary, and a proposal with missing or mismatched context is rejected rather than repaired from `CurrentChunk` or `SceneID`.

Server physics/NavMesh slot identity adds the active environment tile to this full logical context. A tile slot is not a 100 m origin cell, a spatial-index cell, a Unity 3D physics-scene identity or a replacement for the actor context. The selected bounded pool assigns each active qualified tile a unique stable slot near the Unity origin; the actor's canonical absolute ECS position remains independent of that slot.

Stage 1.2 first introduces this value contract in Shared code. Server-owned lifecycle/spawn integration resolves and assigns the actor's **real authoritative context** before publication. The approved *temporary single-world test-hosting contract* for this integration is defined below. No client-supplied context authority, implicit scene-derived identity or persistent-world allocation service is approved.

### 18.3 Stage 1.2 single-world test-server launch and spawn context

**Approved initial deployment:** one server process hosts **one public world**, initially using the authored main-world logical map. The process is launched with an explicit human-readable world/server name, such as `world101` (for example, a `--world world101` launch option). This **launch name is not the `WorldID`** and must not be parsed as a GUID, derived from a scene, or used as a replacement for the context identifier.

- **`WorldID`:** The server creates a new, nonempty `System.Guid` **once per process launch**, before spawns are accepted. The same GUID is reused for every actor in that hosted world's runtime until shutdown. **For this single-server/single-world testing phase, a restart deliberately creates a different GUID even if the launch name remains `world101`.** This temporary identity is not yet a durable identifier for persistent world-keyed records, server restarts, inter-server handoff or multiplayer world selection. A future persistent-world implementation must resolve stable world GUIDs from authoritative world records instead.
- **`MapID`:** The server obtains the **canonical case-sensitive string identifier of the selected authored logical map**. For the present single-map prototype, the **explicit canonical main-map ID is `main-world`**, owned by the server's map/runtime setup; it must be **intentionally supplied by that server-owned setup**, not inferred from `SceneID`, chunk names, `GameObject.scene`, transforms, or a client interest request. A different logical map, such as an instanced zone, uses its own map ID even when it shares scene assets.
- **`InstanceID`:** `Guid.Empty` denotes the public simulation of this world and logical map. Future private runtime instances receive nonempty GUIDs from their **server-local instance owner**, unique within that server's live instance scope. No cross-server instance-ID coordination is needed under this model; the full identity remains `(WorldID, MapID, InstanceID)`.
- **Ownership and lifetime:** The server startup path validates the explicit launch name, establishes the runtime `WorldID` and initial authored `MapID`, and makes the resulting `ActorSpatialContext.CreatePublic(worldID, mapID)` available through a single server-owned runtime/world authority (reuse `ServerWorldManager` where appropriate). A valid context must exist **before any actor spawn is committed**, and it must be cleared on shutdown. There must be **no newly invented global mutable fallback, duplicate source-of-truth map or per-player context registry** for this prototype.
- **Spawn assignment:** For explicitly supported initial **public main-map** player/NPC spawns, `ServerSpawnManager` consumes the server-owned current public context, then assigns that context and authoritative double-precision position to the actor **before** publishing `ActorEndpoint.onServerActorSpawned`. An absent launch name, uninitialised context, unsupported different-map/private-instance spawn, or any invalid identity **fails explicitly** rather than silently substituting the public/main-map context. Existing prefab, reservation, owner/session and spawn rollback behaviour is preserved.
- **Future scope:** Multiple hosted worlds, persistent GUID resolution, world selection, cross-server movement, detached-map selection, instance creation and instance transfer require distinct later work. When supported, each spawn must carry or resolve its explicitly authorised map/instance context at the source; it must never inherit the main-world context merely because the same server or Unity scene is involved.

This is a deliberate **prototype exception** to the long-term persistent-world lifetime described in §4; it changes neither the `System.Guid` representation nor the immutable, fully qualified identity/equality rules in §§18.1–18.2.

---

## 19. Server Interest and Instance Isolation

A player's network interest must include instance context.

Two actors may occupy equivalent coordinates while belonging to different private instances and must not see or interact with one another.

Actor visibility therefore conceptually requires:

```text
compatible world/map/instance context
            AND
broad spatial relevance
            AND
range-based relevance
```

Private-instance isolation takes precedence over geographic proximity.

A player outside a private dungeon must not receive its internal actors simply because their coordinates are numerically nearby.

---

## 20. Player Transition Rules

### 20.1 Open-world tile transition

Invisible.

No loading screen.

The existing and incoming additive scene sets overlap as necessary.

### 20.2 Floating-origin transition

Invisible.

No loading screen.

No gameplay-state transition occurs.

### 20.3 Public seamless interior transition

Invisible.

Destination interior content is preloaded.

The player remains in the same open-world simulation.

### 20.4 Private seamless-instance transition

Normally invisible.

Content is preloaded before the final threshold.

Crossing the authoritative boundary changes instance context.

### 20.5 Detached-map transition

A loading screen is permitted.

The previous map may be unloaded before or during loading of the destination map.

---

## 21. Failure Behaviour

World and instance transitions must fail explicitly and safely.

A player must never be placed into a destination whose required authoritative content or simulation context failed to initialise.

Examples of acceptable fallback behaviour include:

- preventing entry at the final seamless boundary;
- returning the player to a valid entry position;
- displaying a clear error for a failed detached-map transfer.

The system must not silently place the player into an invalid or partially initialised spatial state.

---

## 22. Current Implementation Baseline

The current repository already establishes the following architecture:

- `WorldPosition` stores double-precision absolute coordinates;
- the current configured positional chunk/cell size is 100;
- `ClientPositionManager` implements client floating-origin shifts;
- loaded world scenes are shifted relative to the current client origin;
- `global_actors` is shifted alongside world scenes;
- `ServerWorldManager` loads Addressable additive world scenes;
- PurrNet private scenes and player scene membership are used for world-content delivery;
- `ServerInterestManager` currently uses a 3×3 interest grid;
- `ChunkVisibilityRule` provides chunk/subscription-based network visibility;
- actors live persistently in `global_actors`;
- server ECS actor data already contains double-precision position state;
- terrain authoring currently generates 256 × 256 m terrain tiles;
- the terrain pipeline already provides LOD-oriented terrain generation.

These systems are to be completed and reconciled rather than replaced.

---

## 23. Required Completion of the Existing Architecture

The following are architectural completion requirements, not a replacement design.

### 23.1 Separate origin cells from world-content tiles

The current prototype uses the same runtime chunk coordinate in several places.

The completed system must distinguish the 100 m positional/floating-origin grid from the 256 m terrain/world-content grid.

### 23.2 Complete movement integration

Normal player movement must automatically update:

- authoritative double-precision position;
- origin-cell membership;
- world-content interest where required;
- actor spatial interest.

The test interest controller must not remain the production mechanism.

The production player path is one project-owned PurrNet position RPC. `CharacterController.Move` is sampled with the client's current floating-origin chunk, encoded through `PackWorldPosition`, and paired with an ownership-epoch proposal sequence. The server resolves context, validates ordering and legality at the authoritative tick, and writes only the accepted value to `ActorComponent.WorldPositionD`. The existing `NetworkTransform` position stream is removed only during #136's coordinated observer cutover; it is not run as a second authoritative position stream.

### 23.3 Preserve authoritative double precision

Server gameplay systems currently relying on absolute float transforms for spatial decisions must migrate to authoritative double-precision world positions where correctness requires it.

### 23.4 Complete actor spatial membership updates

Moving actors must update their current spatial membership and visibility indexes.

Spatial identity must not remain fixed to the actor's original spawn scene.

`ServerSpatialIndex.Move` consumes the changed accepted ECS position exactly once per authoritative commit. Same-cell moves retain broad-phase membership while exact queries and observer updates read the latest ECS position. The index does not own a second precise position store.

#### Server-local navigation slots

Server navigation uses a bounded pool of **16 × 16 unique slots at 512 m pitch**, sized for 256 m tile footprints after explicit extent validation. Each active streamed environment tile, qualified by full world/map/instance context, is assigned one stable slot while active. The slot mapping is owned by `ServerPositionManager` and uses:

```text
local physics float = slot anchor + (absolute double position - absolute tile origin)
absolute position = absolute tile origin + (local physics float - slot anchor)
```

Tile colliders, `NavMeshSurface`/`NavMeshData`, agents and physics participants use the same slot. Two far-away logical tiles therefore remain distinct in the single default physics/NavMesh world without requiring large absolute Unity floats. Exhausted capacity fails explicitly; active slots are never aliased or silently evicted.

#135 maintains absolute tile gateway/adjacency routing. It loads or prepares the next tile and slot at a verified gateway, rebinds the agent and recalculates the next local segment while preserving the continuous absolute ECS route. A slot rebind is not a logical teleport. Cross-slot destinations use canonical absolute target position and gateway routing rather than a remote `Transform.position`. Missing required tile data stops navigation explicitly.

### 23.5 Add range-based actor visibility

The current chunk subscription rule remains useful as a broad phase.

Exact actor replication must additionally respect authoritative range.

### 23.6 Complete scene-lifecycle handling

Asynchronous loads that cease to be required before completion must not leave permanently orphaned loaded scenes.

Scene registration and identity indexes must be cleaned when actors or scenes are removed.

### 23.7 Support instance-qualified streamed scenes

The additive scene system must be capable of loading equivalent authored scene content into separate runtime instance contexts without sharing private gameplay state.

---

## 24. Player Experience Requirements

Players should experience:

- continuous traversal through the open world;
- no loading screens for normal world-tile streaming;
- no visible floating-origin correction;
- no arbitrary disappearance of nearby players caused by hidden open-world layers;
- understandable explicit world selection;
- seamless public caves and interiors where appropriate;
- seamless private dungeon or housing entry where appropriately authored;
- loading screens only where a genuine detached-map transition makes them reasonable;
- reliable grouping into the same private instance;
- consistent return destinations when leaving an instance.

Infrastructure decisions must not become unnecessary player friction.

---

## 25. Dependencies

This PDD depends on or constrains:

- **World and Zone Design PDD** — geography and zone layout must accommodate streamed world tiles and seamless interiors;
- **Movement and Traversal PDD** — movement must cooperate with authoritative world-position and transition boundaries;
- **Dungeon and Group Content PDD** — dungeon gameplay uses the instance architecture defined here;
- **Group and Raid Systems PDD** — party/raid identity is used when resolving group-scoped instances;
- **Account, Character and Persistence PDD** — character location, world selection and persistent instance/property state require persistence;
- **NPC and Creature Design PDD** — NPC lifecycle must cooperate with streamed-content and instance lifetime;
- **AI and Encounter Behaviour PDD** — inactive or unloaded areas must not require inappropriate full-rate AI simulation;
- **Graphical Approach PDD** — terrain and environment LOD must support seamless large-world presentation.

---

## 26. Intentionally Unspecified Values

The following are deliberately not assigned arbitrary values by this PDD because the current implementation does not yet provide an approved value:

- final terrain LOD distances;
- maximum players per persistent world;
- maximum players per particular instance type;
- instance empty-timeout duration;
- dungeon reset and lockout durations;
- exact preload distances for seamless entrances.

These values should be exposed through appropriate configuration/data definitions and chosen through later gameplay or performance testing.

---

## 27. Validation Criteria

The system satisfies this design when all of the following are possible:

1. A player can traverse a large open world while authoritative actor coordinates remain double precision.
2. Client float coordinates remain near the floating origin during long-distance travel.
3. Floating-origin correction produces no visible teleport or gameplay-position change.
4. Open-world environment scenes load and unload additively according to spatial interest.
5. The 100 m origin grid can operate independently of the 256 m world-content/terrain grid.
6. Terrain can remain visible at appropriate LOD beyond the range at which detailed world content is loaded.
7. Actors can cross spatial boundaries while preserving correct authoritative position and network visibility.
8. Actor replication uses double-precision range checks rather than scene membership alone.
9. A public cave can be entered without a loading screen and remains part of the same shared world.
10. A five-player party can enter a physical dungeon entrance without a loading screen and resolve to its own private instance.
11. Another party entering the same doorway can receive an independent instance using the same authored scene assets.
12. A player-owned interior can be loaded only when needed and shared with authorised visitors.
13. A battleground, raid map or alternate dimension can transition through a conventional loading screen into a detached map.
14. Designers can author dungeon/interior content as ordinary Unity scenes and make them available through Addressables.
15. Runtime instance state is distinct from authored scene state.
16. Players on different private instances cannot see or interact with one another merely because they share equivalent coordinates.
17. Normal open-world population management does not silently migrate players between invisible layers.
18. Scene loads and instance transitions cleanly release unused runtime resources.
