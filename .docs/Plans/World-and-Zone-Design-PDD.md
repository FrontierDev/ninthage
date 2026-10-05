# Ninth Age — World and Zone Design Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Player-facing world structure, zones, settlements, routes, wilderness, exploration, barriers, points of interest and the relationship between geography and traversal  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the player-facing structural principles of the world of **Ninth Age**.

It is intentionally high-level.

The world is already substantially determined conceptually, and this PDD should not duplicate lore documents or prematurely fix detailed geography that has not yet been formalised.

This document is authoritative for:

- the relationship between the open world and zones;
- zone-boundary philosophy;
- settlement and wilderness structure;
- roads, paths and route hierarchy;
- points of interest;
- exploration philosophy;
- geographic barriers;
- terrain compartmentalisation;
- verticality;
- waterways;
- world transport integration;
- lodestone placement principles;
- mount-aware world design;
- public interior integration;
- dungeon entrances;
- profession-specific traversal opportunities;
- world-density principles;
- provision of NPC-free locations for player roleplaying.

This document does not currently define:

- the exact continent list;
- the exact zone list;
- zone level bands;
- zone dimensions;
- settlement counts;
- faction borders;
- biome lists;
- resource distribution;
- quest density;
- NPC populations;
- exact transport routes;
- exact lodestone locations;
- detailed world lore.

Those remain deferred until later world-design work.

The runtime implementation of the world is defined separately by the [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md).

Player traversal mechanics are defined by the [Movement and Traversal PDD](Movement-and-Traversal-PDD.md).

---

## 2. Design Pillars

### 2.1 The world should feel geographically coherent

The primary game world should feel like a real contiguous place rather than a collection of disconnected levels.

Regions should have meaningful spatial relationships.

Players should be able to understand that one settlement lies beyond a particular forest, mountain pass, river crossing or coastline rather than thinking primarily in terms of separate game maps.

### 2.2 Geography should influence gameplay

Terrain should matter.

Mountains, waterways, roads, cliffs, valleys, settlements, fortifications and wilderness should affect how players move through and understand the world.

World geography must not exist merely as visual background.

### 2.3 Zones are design regions, not technical loading units

Zones describe meaningful regions of the world.

They may be defined by:

- geography;
- political control;
- culture;
- ecology;
- narrative identity;
- danger;
- progression;
- history.

A zone boundary does not inherently correspond to:

- a Unity scene;
- a terrain tile;
- an origin cell;
- a loading screen;
- a server process boundary.

The technical partitioning of the world is governed by the World Runtime and Instancing PDD.

### 2.4 The world should contain both density and space

Not every part of the world should be packed with objectives.

Cities, settlements, ruins and quest locations may be dense.

Wilderness should be allowed to feel like wilderness.

Some locations should intentionally contain little or no NPC activity at all.

Travel between meaningful locations is itself part of experiencing the world.

### 2.5 Progression should increase mobility without invalidating geography

Later access to:

- ground mounts;
- lodestones;
- transport networks;
- profession shortcuts;

should make travel easier without making roads, passes, rivers and settlements irrelevant.

Ninth Age does not use flying mounts.

---

## 3. World Structure

The primary open world should be experienced as a large continuous geographic space.

Regions and zones exist within that world as player-facing organisational concepts.

Players should normally move between neighbouring zones through the physical environment.

Examples include:

- following a road;
- crossing a bridge;
- travelling through a mountain pass;
- entering a forest;
- crossing a political border;
- following a river valley;
- taking a boat;
- passing through a gate;
- entering a tunnel.

Crossing a normal zone boundary should not inherently require a loading screen.

---

## 4. Zone Boundaries

Zone boundaries may be either **soft** or **hard**.

### 4.1 Soft boundaries

Most neighbouring regions should transition naturally.

A soft boundary may be communicated through changes in:

- terrain;
- vegetation;
- weather;
- architecture;
- NPC population;
- music;
- lighting;
- road design;
- faction presence;
- environmental storytelling.

A player may move directly across such a boundary without being explicitly stopped.

### 4.2 Hard boundaries

Hard barriers between zones are permitted where they make sense within the world.

Examples may include:

- mountain ranges;
- seas;
- major rivers;
- fortified borders;
- city walls and controlled gates;
- collapsed passes;
- destroyed bridges;
- hazardous magical regions;
- hostile military territory;
- sealed tunnels;
- other lore-driven obstacles.

A hard boundary may require the player to:

- use a specific route;
- complete a quest;
- gain access permission;
- use transport;
- approach from another direction;
- unlock an appropriate progression mechanic.

Hard barriers must exist because the geography, politics, narrative or setting supports them.

They should not be arbitrary invisible barriers created solely to stop an under-levelled player.

---

## 5. Terrain Compartmentalisation

Terrain should be used intelligently to organise the world into understandable spaces.

Useful features include:

- ridgelines;
- hills;
- valleys;
- forests;
- cliffs;
- rivers;
- ravines;
- coastlines;
- rock formations;
- settlement walls;
- changes in elevation.

These features may:

- frame a sub-region;
- control sightlines;
- guide players toward routes;
- separate encounter areas;
- create hidden spaces;
- make adjacent areas feel distinct;
- reduce the amount of the world visible at once;
- assist world streaming and content presentation without exposing technical boundaries.

This must not result in every zone feeling like a sequence of enclosed corridors.

Broad open spaces are equally valid where appropriate.

Examples may include:

- plains;
- steppes;
- deserts;
- open valleys;
- large agricultural landscapes;
- tundra;
- broad wetlands.

Terrain compartmentalisation should be used where it improves the region rather than as a universal requirement.

---

## 6. Progression and Zone Access

Zones may eventually have intended progression ranges.

Those ranges are **not defined by this PDD**.

The general principle is that character progression should guide players through the world without requiring every zone transition to be enforced by a hard numerical level gate.

Players may be able to enter dangerous areas before they are ready.

The consequences should normally arise from the actual world:

- stronger enemies;
- dangerous terrain;
- hostile factions;
- difficult travel;
- restricted access;
- lack of nearby services.

Where progression-gated access exists, it should have an in-world justification.

---

## 7. Settlement Structure

The world should contain settlements of different scales.

A useful conceptual hierarchy is:

```text
major city
    ↓
town
    ↓
village / regional settlement
    ↓
outpost / camp / isolated site
```

This hierarchy is descriptive rather than a required fixed taxonomy.

Settlement scale should affect the kinds of services, population and activity that make sense there.

Large settlements may function as:

- regional hubs;
- transport hubs;
- commercial centres;
- faction centres;
- social locations.

Smaller settlements may provide:

- limited services;
- local quests;
- rest points;
- regional identity;
- access to nearby wilderness.

Exact settlement counts and locations are deferred.

---

## 8. Wilderness

Wilderness is an important part of the world rather than unused space between content hubs.

Wilderness may provide:

- exploration;
- gathering;
- hostile creatures;
- rare encounters;
- profession opportunities;
- hidden locations;
- alternate routes;
- environmental storytelling;
- natural landmarks;
- dangerous shortcuts.

Different regions may have substantially different densities.

A settled agricultural region may feel comparatively populated.

A mountain wilderness, remote forest or later-game wild zone may have long stretches with relatively little constructed content.

The design should avoid requiring an objective, NPC or interactable every few metres.

### 8.1 Wild zones

Some later zones may deliberately be much less developed.

A wild zone may have:

- very few maintained roads;
- few settlements;
- long distances between safe areas;
- greater reliance on landmarks and terrain for navigation;
- informal trails rather than constructed roads;
- larger uninterrupted wilderness areas.

Later progression, including access to mounts, allows these regions to be physically larger and less road-dependent without making travel unreasonable.

Sparse infrastructure should reinforce the identity of the zone rather than simply making navigation inconvenient.

---

## 9. Roleplaying Spaces

Every zone should contain some locations intentionally suitable for player roleplaying.

These locations should be **deliberately devoid of routine NPC populations**.

The purpose is to provide players with spaces where they can:

- gather privately or semi-privately;
- stage roleplaying scenes;
- hold meetings;
- use interesting world locations without constant NPC interruption;
- create their own social activity within the world.

Suitable examples may include:

- unused rooms;
- remote camps;
- quiet clearings;
- abandoned structures;
- secluded beaches;
- overlooks;
- isolated ruins;
- empty houses or halls where appropriate.

These locations need not be formally labelled as roleplaying areas.

They should simply exist naturally within each zone.

NPC-free does not necessarily mean mechanically safe in every circumstance, but routine ambient NPC spawning should not continuously occupy these locations.

World design should therefore avoid populating every visually interesting space merely because empty space appears available.

---

## 10. Route Hierarchy

Travel routes should have a recognisable hierarchy where developed infrastructure exists.

Conceptually:

```text
major road
    ↓
regional road
    ↓
minor path
    ↓
informal / wilderness route
    ↓
profession-specific shortcut
```

This hierarchy does not require every zone to contain every route type.

Wild regions may have little or no formal road network.

### 10.1 Major roads

Major roads connect significant settlements and important regional destinations.

Where present, they should generally be:

- easy to follow;
- comparatively safe;
- suitable for mounted travel;
- visually legible.

### 10.2 Regional and minor routes

Smaller roads and paths may connect:

- villages;
- ruins;
- resource areas;
- caves;
- local landmarks;
- secondary settlements;
- quest areas.

These routes may be less safe and less obvious than primary roads.

### 10.3 Wilderness routes

Players should be able to leave established roads.

Cross-country travel may offer:

- shorter routes;
- exploration opportunities;
- profession resources;
- hidden content;
- increased danger;
- difficult terrain.

In some wild zones, wilderness travel may be the normal form of travel rather than an alternative to roads.

### 10.4 Profession shortcuts

Some routes may be inaccessible through ordinary baseline traversal.

The Shadowcraft profession may provide access to:

- climbing routes;
- agility shortcuts;
- concealed passages;
- other deliberately authored traversal opportunities.

These should provide meaningful alternate routes without establishing universal free climbing.

---

## 11. Roads and Navigation

Roads should serve as real navigation tools where roads make sense.

A player should often be able to infer that following a major road will eventually lead to a settlement or regional destination.

Road layout should help communicate:

- regional importance;
- settlement hierarchy;
- trade routes;
- political boundaries;
- safe travel corridors.

However, roads must not become a universal requirement for navigation.

Later or deliberately wild zones may contain few constructed roads.

In those regions, navigation should increasingly rely on:

- landmarks;
- terrain shape;
- waterways;
- trails;
- natural passes;
- player knowledge.

Navigation should not depend entirely on map markers.

The environment itself should help players understand where they are going.

---

## 12. Verticality

The world may make substantial use of vertical terrain.

Examples include:

- cliffs;
- mountains;
- valleys;
- ravines;
- towers;
- terraces;
- caves;
- underground routes;
- elevated settlements.

Verticality should create:

- landmarks;
- route choices;
- defensive geography;
- exploration opportunities;
- visual scale;
- natural compartmentalisation.

Baseline navigation must remain compatible with the movement system.

Because universal climbing is not part of baseline movement, important routes must not assume arbitrary climbing of world geometry.

Special climbing or agility routes may instead be deliberately authored for Shadowcraft.

---

## 13. Water and Coastlines

Water should influence world geography and travel.

Relevant geographic features may include:

- rivers;
- lakes;
- coastlines;
- islands;
- estuaries;
- canals.

Surface swimming allows players to cross smaller bodies of water where reasonable.

Larger waterways may create meaningful travel barriers.

Boats may provide transport across or along waterways where appropriate.

The existence of surface swimming should not require every body of water to be trivially crossable.

---

## 14. Physical Transport

Boats, lifts and similar systems should be integrated into the world as actual infrastructure.

Transport locations should make geographic sense.

Examples include:

- docks connecting coastal settlements;
- ferries crossing major rivers;
- lifts serving mines or elevated settlements;
- mechanical platforms within fortifications;
- other setting-appropriate transit systems.

Transport should reinforce world scale rather than function purely as a disguised menu.

Detailed movement behaviour is governed by the Movement and Traversal PDD.

---

## 15. Lodestones

Lodestones form the standard fast-travel network defined by the Movement and Traversal PDD.

Their placement should be selective.

They should normally be associated with locations significant enough to justify becoming part of the fast-travel network.

Potential locations include:

- major settlements;
- important regional hubs;
- significant crossroads;
- selected strategic sites.

Lodestones should not be placed so densely that ordinary travel becomes irrelevant.

Because destinations must be discovered before use, initial movement through a region remains meaningful.

Exact lodestone locations and spacing are deferred.

---

## 16. Mount-Aware World Scale

The world should support two broad phases of movement progression:

```text
early progression
    ↓
primarily on foot

later progression
    ↓
ground mounts available
```

Early-game travel distances must remain reasonable without a mount.

Later regions may make greater use of:

- longer travel distances;
- wider areas;
- sparse roads;
- extended wilderness;
- greater distances between settlements.

Because there are no flying mounts, terrain barriers remain permanently relevant.

World design may therefore rely on:

- passes;
- bridges;
- gates;
- roads;
- tunnels;
- boats;
- lifts;
- controlled crossings;

without expecting later progression to make them obsolete.

---

## 17. Points of Interest

The world should contain a broad range of recognisable points of interest.

Examples may include:

- ruins;
- caves;
- mines;
- forts;
- towers;
- shrines;
- camps;
- graveyards;
- farms;
- unusual natural formations;
- abandoned settlements;
- bridges;
- watch posts;
- hidden structures;
- profession-specific locations.

Points of interest should support one or more purposes such as:

- navigation;
- exploration;
- narrative;
- combat;
- quests;
- resources;
- profession content;
- secrets;
- rare encounters;
- player roleplaying.

Not every point of interest requires a formal quest.

Not every point of interest requires NPCs.

---

## 18. Exploration

Exploration should be rewarded.

Potential rewards may include:

- discovering a lodestone;
- uncovering a new location;
- finding a shortcut;
- accessing profession content;
- discovering resources;
- encountering rare NPCs or creatures;
- finding hidden quests;
- uncovering lore;
- finding unusual items or treasure.

The precise exploration-reward system is not defined here.

The important principle is that moving away from the most obvious route should sometimes be worthwhile.

Exploration value does not always need to be mechanical. Discovering a visually distinctive, quiet or socially useful location can itself add value to the world.

---

## 19. Public Interiors

Not every cave, mine, building or underground space is a dungeon or instance.

Public interiors may form seamless extensions of the open world.

Players entering the same public cave on the same world should remain within the same shared simulation unless the content is explicitly designed as private.

Public interiors may be:

- small;
- large;
- multi-level;
- underground;
- integrated into settlements;
- connected to wider cave or tunnel networks.

Their technical scene boundaries are defined by the World Runtime and Instancing PDD.

---

## 20. Dungeon Entrances

Where appropriate, dungeons should feel physically connected to the world.

A dungeon may have a real geographic entrance such as:

- a cave;
- fortress gate;
- mine entrance;
- crypt;
- temple;
- ruined structure;
- tunnel.

Crossing that entrance may lead into a private group instance while preserving the physical relationship between the dungeon and the surrounding region.

Not all instanced content must use this model.

Detached maps remain valid where appropriate.

Detailed dungeon structure belongs to the Dungeon and Group Content PDD.

---

## 21. Dangerous and Safe Areas

The world should contain meaningful variation in danger.

Relative safety may be communicated through:

- settlement presence;
- guards;
- roads;
- patrols;
- creature population;
- terrain;
- faction control.

Dangerous routes may provide:

- shorter travel;
- valuable resources;
- exploration rewards;
- access to difficult content;
- high-risk alternatives to safer roads.

Danger should arise from actual world conditions rather than arbitrary movement penalties.

NPC-free roleplaying spaces should not automatically be treated as enemy spawn areas merely because they are otherwise empty.

---

## 22. Faction and Political Geography

Political borders and faction control may influence geography.

Examples include:

- guarded gates;
- contested regions;
- fortified crossings;
- hostile settlements;
- military roads;
- checkpoints.

The detailed faction map is deferred.

Faction behaviour and reputation rules belong to the Factions and Reputation PDD.

This document only establishes that political geography may legitimately create both soft and hard world boundaries.

---

## 23. World Density

World density should vary intentionally.

A useful spectrum is:

```text
major settlement
high density

settled region
moderate density

frontier / wilderness
lower density

remote / wild region
very low density
```

Low-density space is not inherently wasted space.

It can contribute to:

- scale;
- atmosphere;
- anticipation;
- navigation;
- danger;
- environmental identity;
- roleplaying;
- visual relief.

Content density should suit the region rather than follow one universal target.

Every zone must preserve some spaces without routine NPC population specifically so players have natural locations for roleplaying and social use.

Exact density metrics are deferred.

---

## 24. World Authoring Principles

World content should be authored so that geography communicates function.

Where practical:

- important routes should be readable from terrain and landmarks;
- settlements should connect sensibly to roads and transport;
- defensive structures should respond to geography;
- waterways should influence settlement and route placement;
- dungeon entrances should have a believable physical context;
- hard barriers should be visually and narratively understandable;
- terrain should compartmentalise areas where useful;
- large open spaces should remain possible where appropriate;
- traversal shortcuts should be intentionally authored;
- distant landmarks should help orientation;
- wild zones should not be forced to contain artificial road networks;
- some attractive or useful locations should intentionally remain free from routine NPC occupation.

The world should not rely exclusively on UI markers to explain its structure.

---

## 25. Relationship to Runtime Streaming

World-design boundaries must remain separate from technical streaming boundaries.

A player may cross:

- terrain tiles;
- additive scene boundaries;
- floating-origin cells;
- network-interest boundaries;

without crossing a zone boundary.

Likewise, one zone may contain many streamed runtime regions.

Terrain compartmentalisation may help technical presentation by limiting long sightlines in suitable areas, but world geography must not be distorted solely to conceal streaming systems.

Broad open landscapes must remain possible where required by the zone design.

World designers should think in terms of geography and player experience rather than Unity scene size.

The World Runtime and Instancing PDD remains authoritative for implementation-level partitioning.

---

## 26. Locked Design Decisions

The following decisions are locked by this PDD:

1. The primary world should feel large, continuous and geographically coherent.
2. Zones are player-facing design regions rather than technical streaming units.
3. Normal zone transitions should usually occur seamlessly.
4. Both soft and hard zone boundaries are permitted.
5. Hard barriers must have geographic, political, narrative or setting justification.
6. Hard barriers must not exist solely as arbitrary level gates.
7. Terrain should be used intelligently to compartmentalise areas where appropriate.
8. Terrain compartmentalisation must not prevent broad open spaces where those suit the region.
9. Wilderness is a meaningful part of the world.
10. Content density may vary substantially between regions.
11. Every zone must contain some locations intentionally free from routine NPC populations for player roleplaying.
12. Roads and paths should form a recognisable travel hierarchy where developed infrastructure exists.
13. Later or deliberately wild zones may have substantially fewer roads.
14. Some wild zones may rely primarily on terrain, landmarks and informal routes for navigation.
15. Players may travel away from established roads.
16. Waterways should meaningfully affect geography and travel.
17. Boats and lifts should be integrated into the physical world.
18. Lodestones should be selectively placed rather than ubiquitous.
19. Initial exploration of regions remains important before fast travel becomes available.
20. Later access to ground mounts may influence world scale and permit larger, sparser regions.
21. Flying mounts are not supported.
22. Geography such as mountains, bridges, passes and rivers should therefore remain permanently meaningful.
23. Public caves and interiors may remain part of the shared open world.
24. Dungeons should use believable physical entrances where appropriate.
25. Climbing and agility-style shortcuts may be authored for the Shadowcraft profession rather than universal traversal.
26. Exploration should provide meaningful reasons to leave the obvious route.
27. Not every point of interest requires combat, quests or NPC occupation.
28. Zone lists, level bands and detailed geography remain deferred.

---

## 27. Deferred Design Decisions

The following are intentionally unresolved:

- exact continent list;
- exact zone list;
- exact regional hierarchy;
- zone names;
- zone dimensions;
- level ranges;
- settlement counts;
- settlement locations;
- biome roster;
- faction borders;
- faction control;
- world-resource distribution;
- exact point-of-interest density;
- quest density;
- NPC and creature density;
- road network layout;
- transport routes;
- lodestone locations;
- lodestone spacing;
- dungeon locations;
- exact exploration rewards;
- exact safe/dangerous-area distribution.

These should be resolved through later world and content design rather than invented by implementation.

---

## 28. Dependencies

This PDD depends on or constrains:

- **World Runtime and Instancing PDD** — technical continuity, scene streaming, public interiors and private instances;
- **Movement and Traversal PDD** — walking, running, crawling, swimming, mounts, boats, lifts, lodestones and Shadowcraft traversal;
- **Graphical Approach PDD** — terrain, atmosphere, environment presentation and visual identity;
- **NPC and Creature Design PDD** — world populations and preservation of designated NPC-free spaces;
- **AI and Encounter Behaviour PDD** — danger and creature behaviour;
- **Quest, Narrative and Dialogue PDD** — narrative geography and progression through regions;
- **Dungeon and Group Content PDD** — dungeon placement and open-world relationship;
- **Open-World Events PDD** — public content placement;
- **Factions and Reputation PDD** — political geography and hostile/friendly regions;
- **Crafting and profession design** — resource distribution and profession-specific world opportunities;
- **Character Stats and Progression PDD** — eventual level progression through the world.

---

## 29. Validation Criteria

The world and zone design satisfies this PDD when:

1. Players can travel through neighbouring regions without ordinary zone loading screens.
2. Zone boundaries are understandable as world regions rather than technical partitions.
3. Soft boundaries can be communicated naturally through geography and presentation.
4. Hard barriers exist only where they make sense within the setting.
5. Hard barriers are understandable from the world rather than arbitrary invisible walls.
6. Terrain can compartmentalise regions and sub-regions without every area becoming corridor-like.
7. Broad open spaces can exist where appropriate.
8. Major settlements are connected by understandable routes where infrastructure warrants them.
9. Roads and paths have a meaningful hierarchy in developed regions.
10. Wild regions can function correctly with sparse or absent formal road networks.
11. Players can navigate sparse wild regions using terrain and landmarks.
12. Players can choose to leave normal routes and travel through wilderness.
13. Wilderness regions can exist without being filled continuously with objectives.
14. Every zone includes some intentionally NPC-free locations suitable for player roleplaying.
15. Visually interesting empty spaces are not automatically populated simply to increase content density.
16. Terrain and landmarks help players navigate.
17. Rivers, lakes, coasts and mountains meaningfully affect travel routes.
18. Ground mounts improve travel without invalidating world geography.
19. Later regions can become larger and sparser once mounted travel is available.
20. The absence of flying mounts preserves long-term relevance of routes and barriers.
21. Boats and lifts can be positioned as believable physical infrastructure.
22. Lodestones provide fast travel without replacing initial exploration.
23. Public caves and interiors can exist seamlessly within the shared world.
24. Instanced dungeons can retain believable physical entrances.
25. Shadowcraft shortcuts can provide alternate traversal without introducing universal climbing.
26. World content density can vary according to regional identity.
27. The world can be authored without requiring zone design to mirror terrain-tile or scene boundaries.
28. Detailed zone lists, level bands and geography can be added later without contradicting this document.
