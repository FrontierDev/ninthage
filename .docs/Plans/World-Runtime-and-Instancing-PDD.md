# Ninth Age — World Runtime and Instancing Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Persistent worlds, world streaming, spatial partitioning, floating origin, actor visibility, seamless interiors, instanced spaces, map transitions and runtime scene authoring  
**Last updated:** 2026-10-05

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

The exact default visibility distance is not currently implemented and is therefore not numerically specified in this PDD.

The value must be configurable rather than embedded throughout gameplay code.

### 8.3 Precision

Visibility and gameplay range calculations must use double-precision authoritative positions.

Client-local float transforms are not authoritative for these checks.

### 8.4 Visibility hysteresis

The implementation should support a small difference between visibility-entry and visibility-exit thresholds where required to prevent an actor repeatedly appearing and disappearing while moving around a range boundary.

The exact values are implementation tuning.

### 8.5 Dynamic membership

Actor spatial membership must update when an actor moves.

An actor that crosses a spatial boundary must not remain permanently registered against its spawn region.

Visibility indexes and other spatial registries must follow the actor's authoritative current location.

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

### 23.3 Preserve authoritative double precision

Server gameplay systems currently relying on absolute float transforms for spatial decisions must migrate to authoritative double-precision world positions where correctness requires it.

### 23.4 Complete actor spatial membership updates

Moving actors must update their current spatial membership and visibility indexes.

Spatial identity must not remain fixed to the actor's original spawn scene.

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

- exact actor replication/visibility distance;
- visibility hysteresis distance;
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
