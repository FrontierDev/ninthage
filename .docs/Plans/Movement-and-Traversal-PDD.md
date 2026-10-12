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

Crossing a 100 m origin-cell boundary may trigger a client floating-origin adjustment.

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

### 20.4 Movement-driven spatial updates

Ordinary movement must automatically drive any required updates to:

- origin-cell membership;
- world-content streaming interest;
- actor network interest;
- current spatial/instance context.

Development-only manual interest controls must not be required in production.

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
