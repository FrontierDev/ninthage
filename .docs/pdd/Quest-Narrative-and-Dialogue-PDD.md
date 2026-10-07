# Ninth Age — Quest, Narrative and Dialogue Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Narrative delivery, quests, objectives, quest chains, dialogue, branching consequences, rewards, shared progress, markers, narrative state and persistence  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended quest, narrative and dialogue model for **Ninth Age**.

It is authoritative for:

- quest discovery and acquisition;
- quest states;
- quest objectives;
- quest chains and branching;
- quest requirements;
- quest completion and turn-in;
- repeatable and timed quests;
- quest rewards at the quest-system level;
- group quest progress;
- quest markers and objective-location presentation;
- dialogue structure;
- conditional dialogue;
- dialogue actions;
- narrative state and consequence scopes;
- narrative persistence;
- narrative presentation;
- quest and dialogue authoring requirements.

This document does not define:

- detailed item reward generation or inventory behaviour — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- faction/reputation progression — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- group creation, membership and raid management — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- open-world event behaviour — [Open-World Events PDD](Open-World-Events-PDD.md);
- dungeon structure — [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- combat mechanics — [Combat System PDD](Combat-System-PDD.md);
- detailed account/world persistence implementation — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- final visual styling of quest/dialogue UI — [UI and UX PDD](UI-and-UX-PDD.md).

Where current implementation conflicts with this document, this document is the intended product behaviour.

---

## 2. Design Pillars

### 2.1 Quests belong to the world

Quests should feel attached to characters, places, factions, objects and events rather than existing as an abstract checklist layer.

Players may discover quests through:

- NPCs;
- objects or items;
- locations;
- previous quests;
- professions;
- environmental discovery;
- other authored world-state triggers.

### 2.2 Guidance should inform rather than navigate for the player

Ninth Age should give players enough information to understand an objective without turning questing into GPS navigation.

Objective locations may be shown on the world map, but by default they should be approximate authored points rather than exact live positions, dynamic search overlays or routes.

### 2.3 Dialogue choices should represent choices the character can actually make

If a dialogue choice is unavailable because its conditions fail, it should normally be hidden.

The interface should not routinely show lists of unavailable choices merely to reveal hypothetical alternatives.

### 2.4 Narrative consequences may exist at different scopes

A player choice may affect:

- only that character;
- the persistent world/server the character currently occupies;
- the game globally across all persistent worlds/servers.

These scopes must be explicit.

World-wide and game-wide consequences are powerful tools and should be deliberately authored rather than casually attached to ordinary dialogue.

### 2.5 Group play should share progress, not identity

Players should be able to make quest progress cooperatively.

Dialogue and narrative choices remain individual unless content explicitly produces a shared-world or game-wide consequence after one player's choice.

Party membership must not cause one player to select dialogue options on behalf of another.

### 2.6 Narrative presentation should remain restrained

The primary narrative tools are:

- dialogue windows;
- books, letters, notes and readable items;
- environmental storytelling;
- normal world interaction.

Scripted scenes are supported but should be used sparingly.

The game should avoid routinely taking camera or character control away from the player.

---

## 3. Existing Architectural Foundation

The current codebase already establishes several useful foundations that should be retained and completed.

### 3.1 Quest definitions

The current QuestDefinition supports:

- icon;
- description;
- level;
- recommended player count;
- optional timer;
- quest categorisation;
- gold reward;
- experience reward;
- reputation reward;
- item rewards;
- all-items versus choose-item reward presentation;
- repeatability;
- repeat cooldown;
- polymorphic objectives;
- polymorphic requirements through ConditionDefinition.

This remains the basis of authored quest data.

### 3.2 Quest objectives

The existing QuestObjective hierarchy is polymorphic.

Current concrete objectives include:

- Quest_KillObjective;
- Quest_ItemObjective.

The architecture should be expanded rather than replaced with a single large objective enum.

### 3.3 Quest progress

Current quest progress is stored per character as:

- quest identity;
- objective progress values;
- completion state.

This establishes individual character ownership of quest progress.

The persistence model must be extended for the additional states and history defined by this PDD.

### 3.4 Dialogue definitions

The current dialogue model is a branching node graph.

A dialogue contains nodes, and nodes contain choices that may lead to other nodes.

Choices already support:

- display text;
- icons;
- authored conditions;
- a target node;
- a ShowAlways concept.

The graph-based model is retained.

### 3.5 Current runtime limitations

The current implementation is incomplete and must not be mistaken for the full product design.

Notably:

- quests are currently inserted for testing rather than acquired through a complete offer/accept flow;
- authored quest requirements are not yet the complete runtime availability system;
- kill objectives are the principal objective currently progressed at runtime;
- group credit is not yet implemented as a proper group system;
- completion/turn-in/reward granting is incomplete;
- dialogue conditions are authored but not currently applied by the normal client choice list;
- dialogue choices currently move between nodes but do not provide a general authoritative action system.

These are implementation gaps to complete against this PDD.

---

## 4. Quest State Model

A quest should support the following conceptual states:

1. **Unavailable**
2. **Available**
3. **Active**
4. **Objectives Complete**
5. **Ready for Turn-In**
6. **Completed**
7. **Failed**, where applicable
8. **Repeat Cooldown**, where applicable

Not every quest needs every state.

### 4.1 Unavailable

The quest exists but the character does not currently satisfy its availability requirements or has not discovered its offer.

### 4.2 Available

The character may accept the quest.

### 4.3 Active

The quest has been accepted and its objectives are being progressed.

### 4.4 Objectives Complete

All required objectives have been satisfied.

### 4.5 Ready for Turn-In

The quest is complete enough to be handed in to its configured turn-in source.

For normal quests, **manual turn-in is required**.

### 4.6 Completed

The quest has been turned in successfully and its completion history/rewards have been committed.

### 4.7 Failed

A quest may enter a failed state when explicitly authored failure conditions are met.

### 4.8 Repeat Cooldown

A repeatable quest that has been completed may remain unavailable until its configured reset/cooldown permits reacquisition.

---

## 5. Quest Discovery and Acquisition

Quests may be offered or discovered through:

- NPC dialogue;
- interactable world objects;
- readable or usable items;
- entering/discovering a location;
- completion of another quest;
- profession activity;
- event/encounter outcomes;
- explicit authored world-state conditions.

Quest discovery does not imply automatic acceptance.

### 5.1 Normal acceptance

Quests should normally be accepted deliberately by the player.

Automatic acceptance is supported only where explicitly authored and appropriate to the content.

### 5.2 Hidden/discovered quests

The system must support quests that are not advertised before their discovery trigger is encountered.

Examples include:

- finding a hidden object;
- entering a remote location;
- reading a particular document;
- discovering a dead NPC;
- completing an unusual interaction.

A hidden quest should not reveal itself prematurely through a global marker or quest list.

---

## 6. Quest Requirements

Quest availability and other requirement checks should use the shared polymorphic ConditionDefinition framework.

Requirements may include, as needed:

- character level;
- class;
- race;
- previous quest completion;
- another quest being active or complete;
- faction/reputation;
- profession;
- profession skill;
- possession of an item;
- location;
- narrative state;
- world state;
- game-wide state;
- other future conditions.

Quest requirements must be validated authoritatively by the server.

The implementation should add condition types as needed rather than create parallel quest-specific prerequisite fields for each new requirement.

---

## 7. Quest Categorisation

Quest categorisation is conceptually separate from repeatability.

The current categories Normal, Story, Daily and Crafting are therefore provisional.

In particular:

**Daily is not inherently a quest narrative/category type.**

A normal, story or profession quest may independently be repeatable on a daily or other reset.

Quest categorisation may remain useful for:

- organisation;
- UI filtering;
- authoring;
- content identity.

The final category list may be refined without changing the core quest architecture.

---

## 8. Quest Objectives

Objectives remain polymorphic and extensible.

Initial/general-purpose objective types should support at least:

- **Kill** — defeat one or more specified NPCs/creatures;
- **Acquire / Possess Item** — obtain or hold an authored item/quantity;
- **Interact** — use or interact with an authored object;
- **Speak** — speak with a specified NPC;
- **Reach / Discover Location** — enter or discover an authored location;
- **Complete Encounter** — complete an authored encounter/event;
- **Protect / Escort** — successfully protect or escort an actor/objective;
- **Craft** — produce an authored item or satisfy a crafting result;
- **Gather** — gather an authored resource;
- **Faction / Reputation** — reach an authored relationship requirement;
- **Condition / State** — satisfy another explicitly authored game-state condition.

Bespoke objective types remain possible where genuinely required.

Generic/reusable objective types should be preferred over one-off hard-coded quest logic.

---

## 9. Objective Progress

Quest objective progress belongs to the individual character.

Cooperative actions may grant progress to several characters, but each character retains their own progress state.

Progress should be server-authoritative.

The client may display predicted/pending feedback where appropriate but may not award itself quest progress.

---

## 10. Objective Map Locations

Objectives may author an approximate location hint for the world map.

The default model is deliberately imprecise:

- a fixed authored point may indicate the general location;
- the point need not identify the exact NPC/object;
- the point does not dynamically follow a moving target;
- no default GPS trail is required;
- no default exact search polygon is required;
- objectives may omit a map location entirely.

The intent is to help orient the player while preserving observation, exploration and route-finding.

A quest may provide more precise information through its text where narratively appropriate.

---

## 11. Quest Markers

Quest-related NPCs/objects may display recognisable markers.

Ninth Age should **not** use conventional exclamation-mark/question-mark quest symbols.

The marker language should nevertheless make relevant states identifiable.

At minimum, visual states should exist for:

- **Available Quest**
- **Ready for Turn-In**

A distinct active/relevant-interaction marker may also be used where useful.

### 11.1 Marker visibility

Markers must respect:

- quest requirements;
- hidden/discovered quest rules;
- completion state;
- repeat cooldown;
- narrative state.

A hidden quest must not be exposed by its marker before discovery.

Exact iconography belongs to UI/visual design.

---

## 12. Quest Tracking

The game should support a compact quest tracker.

Quests should be **manually selected for tracking** rather than every accepted quest automatically occupying the player's HUD.

Tracked quests may show:

- quest name;
- objective text;
- numeric progress where applicable.

The tracker should not become a substitute for understanding the world.

Map location hints remain governed by Section 10.

---

## 13. Quest Completion and Turn-In

Normal quest completion requires **manual turn-in**.

Completing all objectives moves the quest into its turn-in-ready state rather than immediately granting final rewards.

Turn-in may occur through an authored source such as:

- NPC dialogue;
- object interaction;
- another deliberate quest-completion interaction.

The server must validate:

- the quest is active;
- its required objectives are complete;
- the turn-in source is valid;
- any required turn-in conditions still hold.

Rewards are granted only after authoritative turn-in succeeds.

---

## 14. Quest Rewards

Quest definitions may provide:

- experience;
- gold/currency;
- faction reputation;
- item rewards;
- future authored reward types.

Item rewards may support:

- granting all authored items;
- allowing the player to choose from authored rewards.

Detailed item creation, inventory overflow and binding behaviour belongs to the Items, Equipment and Loot PDD.

Reward granting is server-authoritative.

---

## 15. Quest Chains

Quest definitions should support explicit relationships between quests without requiring chain logic to be hard-coded into NPC scripts.

A quest may:

- require previous quests;
- unlock one successor;
- unlock multiple successors;
- participate in a branching path;
- exclude another branch.

### 15.1 Branching

Branching should be expressed through:

- quest requirements;
- narrative flags/state;
- dialogue choices;
- authored successor relationships.

A single rigid linear-chain structure must not be required.

### 15.2 Mutually exclusive branches

The system must support choices that make another quest or branch unavailable.

Such consequences must be explicit and persistent at the appropriate scope.

---

## 16. Repeatable Quests

Repeatability is independent from quest category.

A quest may be non-repeatable or repeatable.

Repeatable quests may use:

- daily reset;
- weekly reset;
- monthly reset;
- explicitly authored/custom reset periods.

Cooldown/reset state must be server-authoritative and persistent.

It must not rely on client elapsed time.

A completed repeatable quest remains part of completion history even when it becomes available again.

---

## 17. Timed Quests

Timed quests are supported but should not be the default for ordinary questing.

A timer may begin:

- on acceptance;
- on an authored later trigger.

Timer start semantics must be explicit per quest.

Expiry may cause the quest to fail according to authored rules.

A failed timed quest may be reacquired if its quest definition permits it.

Exact timer presentation belongs to UI design.

---

## 18. Quest Failure

Failure is opt-in content behaviour.

A quest may fail because of:

- timer expiry;
- failure of an escort/protection objective;
- an encounter outcome;
- explicit narrative choice;
- another authored condition.

Failure does not automatically erase already-committed world or narrative consequences.

The quest definition determines whether a failed quest may be retried, restarted or permanently lost.

---

## 19. Abandoning Quests

Normal quests may be abandoned unless explicitly marked non-abandonable.

By default, abandoning a quest:

- removes it from the active quest list;
- clears its active objective progress;
- does not erase committed narrative consequences;
- allows reacquisition if the quest remains eligible.

A quest may explicitly preserve some progress across abandonment where content requires it.

Abandoning a quest must not roll back:

- already changed reputation;
- already committed narrative flags;
- world-state changes;
- game-wide consequences.

---

## 20. Quest History

Characters must maintain quest history independently from the active quest list.

History should be able to represent at least:

- completed quest identities;
- repeatable completion history/reset eligibility;
- significant branch choices where not represented through narrative state.

Quest history is used for:

- requirements;
- dialogue;
- chains;
- achievements/progression where applicable;
- narrative continuity.

Completion history must not disappear merely because a repeatable quest becomes available again.

---

## 21. Active Quest Capacity

There is no arbitrary hard active-quest limit by default.

If a large quest log becomes a usability problem, the preferred solutions are:

- filtering;
- categorisation;
- search;
- tracking controls;

rather than forcing players to abandon active content.

A future explicit gameplay reason for a limit would require a deliberate revision of this PDD.

---

## 22. Group Quest Progress

Quest progress may be earned cooperatively.

Party membership alone is not sufficient to grant all progress automatically.

Credit should require the player to be eligible and meaningfully relevant to the action.

### 22.1 Kill credit

An eligible nearby group member should not need to personally damage every quest target.

If the group legitimately defeats the target and the member is sufficiently relevant to the encounter, that member may receive kill credit.

Exact relevance/range rules belong to implementation tuning.

### 22.2 Encounter, escort and protection credit

Eligible nearby group members may receive shared progress when the group successfully completes the relevant encounter/protection objective.

### 22.3 Interaction credit

Interaction objectives should normally require each character to perform the interaction themselves.

An objective may explicitly opt into group-shared interaction credit where that better fits the content.

### 22.4 Item/gather/craft credit

These objectives normally remain tied to the character actually acquiring, gathering or crafting the required result unless the objective explicitly defines shared progress.

### 22.5 Individual storage

Even where progress is earned cooperatively, each character's quest state is stored independently.

---

## 23. Group Dialogue

Dialogue remains individual.

Group membership does not:

- open one shared dialogue session;
- allow a leader to choose dialogue for others;
- force party members onto the same branch;
- automatically accept or turn in another player's quest.

Players may stand near the same NPC and make different dialogue choices independently.

If a choice changes **World** or **GameWide** narrative state, other players may subsequently observe the consequence because of that state change, not because their dialogue was shared.

---

## 24. Dialogue Model

Dialogue uses the existing branching graph model.

Conceptually:

NPC / interaction → Dialogue Definition → Node → one or more available Choices → next Node and/or Actions.

Dialogue definitions should remain reusable authored data rather than bespoke UI scripts.

---

## 25. Dialogue Nodes

A dialogue node should be able to provide:

- dialogue text;
- optional internal/editor title;
- zero or more choices;
- other presentation metadata where later required.

A node with no choices may represent:

- an ending;
- a simple statement;
- a transition to closing the conversation.

---

## 26. Conditional Dialogue

Dialogue choices may contain conditions.

Conditions should reuse the shared condition system wherever practical.

When a choice fails its conditions:

**the choice is hidden.**

Unavailable choices should not normally remain visible as disabled/grayed-out responses.

This preserves character/world-state reactivity without using the dialogue UI to advertise every hypothetical branch.

### 26.1 Inverted conditions

The current DialogueCondition supports inversion.

That capability should remain available until/unless the shared condition architecture gains a cleaner general composite/NOT mechanism.

The exact wrapper implementation is architectural rather than a product requirement.

---

## 27. Dialogue Actions

Dialogue choices must support polymorphic/data-driven **Dialogue Actions**.

Moving to another dialogue node is not sufficient for consequential narrative interaction.

Initial action capabilities should support:

- offer quest;
- accept quest;
- turn in quest;
- set narrative state/flag;
- clear narrative state/flag;
- give item;
- remove item;
- change reputation;
- open vendor/service interaction;
- trigger encounter/event;
- begin combat;
- grant an explicitly authored reward;
- invoke an explicitly authored world-state change.

Additional action types may be added through the same extensible architecture.

Dialogue actions should not require new NPC-specific scripts for ordinary narrative behaviour.

---

## 28. Dialogue Authority

Pure presentation may be handled locally where appropriate.

Any dialogue choice that changes authoritative game state must be validated and executed by the server.

This includes:

- quest acceptance;
- quest completion;
- rewards;
- inventory changes;
- reputation changes;
- narrative-state changes;
- combat initiation;
- event/encounter triggers;
- world/global consequences.

The client must not be able to create an authoritative outcome by directly invoking a local dialogue action.

---

## 29. Narrative State

The narrative system must support explicit persistent state values/flags.

Narrative state should use stable authored identifiers rather than ad-hoc booleans embedded throughout quest or NPC code.

At minimum, narrative state must support these scopes:

- **Character**
- **World**
- **GameWide**

### 29.1 Character scope

Character state affects one character.

Examples:

- a personal branch choice;
- whether the character knows a secret;
- whether the character betrayed an NPC;
- a personal narrative unlock.

### 29.2 World scope

World state affects the specific persistent world/server.

Examples:

- an NPC or settlement state changed on one world;
- a world boss chain advanced on that world;
- a faction-controlled local outcome changed for that world.

A character moving to another persistent world does not inherently carry that world's shared state with them.

### 29.3 GameWide scope

GameWide state affects the game across all persistent worlds/servers.

Examples may include exceptionally significant narrative outcomes or global progression events.

GameWide state changes must be:

- server-authoritative;
- explicitly authored;
- persistent;
- auditable;
- rare enough that their broad impact remains deliberate.

Ordinary quests should not casually mutate game-wide state.

---

## 30. Branching Consequences

Dialogue and quest decisions may produce consequences at any supported narrative scope.

A branch may change:

- future dialogue;
- quest availability;
- NPC relationship/behaviour;
- faction/reputation;
- encounter availability;
- local world state;
- world-boss/event chains;
- other explicitly authored systems.

Branching does not require every choice to produce permanent mechanical consequences.

The system must nevertheless support meaningful persistent divergence when content calls for it.

---

## 31. Consequence Conflict and Concurrency

World and GameWide narrative changes require authoritative conflict handling.

If multiple players attempt mutually incompatible consequences concurrently:

- the server must resolve which transition is valid;
- state transitions must be atomic at the relevant scope;
- clients must receive the authoritative result.

The product design does not mandate a particular database/locking implementation.

It does require that shared narrative state cannot diverge because different clients independently believed contradictory outcomes succeeded.

---

## 32. Narrative Presentation

The primary narrative presentation methods are:

### 32.1 Dialogue windows

Direct NPC interaction uses the dialogue-window system.

Dialogue should normally remain player-paced.

### 32.2 Books and items

Readable content may be delivered through:

- books;
- letters;
- journals;
- notes;
- inscriptions;
- artefacts;
- other interactable/readable items.

These should use appropriate readable-content panels rather than forcing all written narrative into NPC dialogue.

### 32.3 Environmental storytelling

The world may communicate narrative through:

- object placement;
- ruins;
- corpses;
- environmental damage;
- abandoned spaces;
- faction occupation;
- visual clues;
- other authored world details.

### 32.4 Scripted scenes

Scripted scenes are supported but should be **very limited**.

They should be used when normal interaction cannot communicate the moment effectively.

Routine quest presentation should not repeatedly seize camera control or disable player agency.

---

## 33. Relationship to Open-World Events

A quest may:

- reveal an event;
- point toward an event;
- require participation in an event;
- respond to an event outcome;
- change because of world/event state.

However:

**not every event is a quest, and not every event should become a quest objective.**

The non-intrusive event philosophy defined by the Open-World Events PDD remains authoritative.

Quest markers/trackers must not turn ambient events into automatically advertised public objectives.

---

## 34. Relationship to Dungeons

Every dungeon must have associated quest content as defined by the Dungeon and Group Content PDD.

Dungeon quests may include:

- narrative introduction;
- investigation;
- boss objectives;
- item recovery;
- optional side objectives;
- follow-up consequences.

Dungeon completion and quest completion remain separate concepts.

A group may complete a dungeon without every member having the same quests.

---

## 35. Quest and Dialogue Persistence

Persistent character quest data must be expanded beyond the current prototype where necessary.

The system must persist, as appropriate:

- active quest identities;
- objective progress;
- ready-for-turn-in state;
- completed quest history;
- failed state where relevant;
- repeatable reset/cooldown state;
- character-scoped narrative state.

Shared persistence must also support:

- World narrative state;
- GameWide narrative state.

The exact storage implementation belongs to persistence architecture.

---

## 36. Quest Editor Requirements

The existing dedicated Quest Editor should be extended rather than replaced.

It should support authoring and inspection of:

- identity;
- icon;
- description;
- categorisation;
- level/recommended players;
- requirements;
- objectives;
- approximate objective-map locations;
- quest offer/acquisition information;
- turn-in source;
- rewards;
- repeatability;
- cooldown/reset;
- timer/failure rules;
- predecessor/successor relationships;
- branching/exclusion rules;
- abandon/reacquire behaviour;
- relevant dialogue links.

Objective and requirement types should remain discoverable/extensible rather than requiring manual editor rewrites for every new subtype where practical.

---

## 37. Dialogue Editor Requirements

The existing graph-based Dialogue Editor should be extended rather than replaced.

It should support:

- nodes;
- node text;
- choices;
- node linking;
- choice conditions;
- condition inversion where supported;
- dialogue actions;
- narrative-state actions;
- consequence scope;
- quest offer/accept/turn-in actions;
- validation of broken links and invalid references.

Designers should be able to understand a dialogue's branching structure visually.

---

## 38. Authoring Validation

Authoring tools should detect or report, where practical:

- missing quest definitions;
- missing dialogue-node targets;
- broken dialogue references;
- invalid item/NPC/faction references;
- impossible quest prerequisites;
- invalid self-dependencies;
- circular quest requirements where they make progression impossible;
- missing turn-in sources;
- reward definitions with invalid references;
- missing objective targets;
- shared-state actions missing a valid scope/key;
- authoritative dialogue actions lacking valid server-side targets.

Validation should fail explicitly rather than silently substituting unrelated fallback content.

---

## 39. Server Authority

The server is authoritative for:

- quest availability;
- quest acceptance;
- objective progress;
- completion eligibility;
- turn-in;
- reward granting;
- repeat cooldowns;
- timed quest deadlines;
- completed quest history;
- consequential dialogue actions;
- narrative-state changes;
- shared World/GameWide consequence resolution.

The client is responsible for presentation and input but must not be the authority for persistent narrative outcomes.

---

## 40. Current Implementation Changes Required

Implementation should evolve the existing systems rather than replacing useful foundations.

Required work includes:

- implement authoritative quest offer/accept flow;
- implement explicit quest state beyond the current prototype completion boolean;
- implement objective completion evaluation;
- implement manual turn-in validation;
- implement reward granting;
- implement completion history and repeat cooldown persistence;
- generalise objective progression beyond kill objectives;
- implement proper group progress rules;
- implement quest marker state;
- implement manual quest tracking and approximate map hints;
- evaluate quest ConditionDefinition requirements at runtime;
- evaluate dialogue choice conditions;
- hide failed dialogue choices;
- implement server-authoritative dialogue actions;
- implement narrative state at Character, World and GameWide scope;
- extend persistence accordingly.

The current kill-credit logic based on temporary threat-table participation is not itself a permanent group-credit contract.

---

## 41. Intentionally Deferred Decisions

The following remain unresolved or belong to later dependent design:

- final quest category list;
- exact visual form of quest-state markers;
- exact HUD layout of the quest tracker;
- exact map-marker styling;
- exact objective-location iconography;
- exact group-credit relevance radius;
- exact escort credit rules in unusual edge cases;
- exact default repeat reset times/timezones;
- whether Account-scoped narrative state is later required in addition to Character/World/GameWide;
- exact scripted-scene tooling;
- exact readable-book/item UI styling;
- exact localisation pipeline;
- voice acting policy;
- cinematic/cutscene production policy beyond the restraint defined here.

These do not block the core architecture.

---

## 42. Locked Design Decisions

The following decisions are locked by this PDD:

1. Quest content should be grounded in NPCs, locations, objects, professions and world state.
2. Quests normally require deliberate acceptance rather than automatic acceptance.
3. Hidden/discovered quests are supported.
4. Normal quest completion requires manual turn-in.
5. The quest-state model distinguishes availability, active progress, objectives complete, ready for turn-in and completed state.
6. Failed and repeat-cooldown states are supported where applicable.
7. Quest requirements use the shared polymorphic condition architecture.
8. Quest objectives remain polymorphic/extensible.
9. Kill, item, interaction, dialogue, location, encounter, escort/protection, craft, gather, faction and generic state objectives must be supportable.
10. Quest progress is individually stored per character.
11. Group members can earn shared progress when meaningfully eligible/relevant.
12. Group kill credit does not require every eligible member to damage the target personally.
13. Interaction objectives normally require individual interaction unless explicitly shared.
14. Group dialogue is not supported; dialogue choices remain individual.
15. Quest chains support branching and mutually exclusive paths.
16. Repeatability is independent from quest category.
17. Daily/weekly/monthly/custom reset behaviour is supported.
18. Timed quests are supported but should be used selectively.
19. Normal quests may be abandoned unless explicitly non-abandonable.
20. Abandoning normally clears active objective progress but does not roll back committed narrative consequences.
21. Completed quest history persists independently of the active quest list.
22. There is no arbitrary hard active-quest limit by default.
23. Quest markers must not use conventional exclamation-mark/question-mark symbols.
24. Distinct identifiable markers must support at least available-quest and ready-for-turn-in states.
25. Hidden quests must not be revealed early by markers.
26. Objective locations may be shown as approximate fixed points on the world map.
27. Default objective guidance should not use exact moving-target tracking, GPS routing or mandatory search overlays.
28. A compact quest tracker is supported and quests are manually selected for tracking.
29. Dialogue uses the existing branching graph model.
30. Failed conditional dialogue choices are hidden rather than shown disabled.
31. Dialogue choices support extensible authored actions.
32. Consequential dialogue actions are server-authoritative.
33. Narrative state supports Character, World and GameWide scopes.
34. World-scoped consequences affect the relevant persistent world/server.
35. GameWide consequences affect all persistent worlds/servers.
36. World and GameWide state changes must be explicit, authoritative and persistent.
37. Branching consequences may alter quests, dialogue, NPC/world state, reputation, encounters and other authored systems.
38. Dialogue windows are the primary direct NPC narrative presentation.
39. Books/items and environmental storytelling are first-class narrative delivery methods.
40. Scripted scenes are supported but should be very limited.
41. The existing Quest Editor and graph-based Dialogue Editor should be extended rather than replaced.
42. The existing quest/objective/condition/dialogue data architecture is a foundation to complete, not a requirement to rewrite.
43. Quest and narrative state that affects persistent gameplay must never rely solely on client authority.

---

## 43. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](../MMORPG-Master-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- [Open-World Events PDD](Open-World-Events-PDD.md);
- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Crafting System PDD](Crafting-System-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md).

---

## 44. Validation Criteria

The Quest, Narrative and Dialogue design is correctly implemented when:

1. An NPC/object/location can offer a quest through authored data rather than bespoke quest code.
2. A hidden quest can remain undisclosed until its discovery trigger is satisfied.
3. The server validates quest requirements before acceptance.
4. Accepted quests persist per character.
5. Polymorphic quest objectives can be added without redesigning the base quest definition.
6. Kill objectives can grant progress to eligible nearby group members without every member tagging the target.
7. Interaction objectives can require individual interaction.
8. Quest progress remains individually stored despite shared credit.
9. Completing objectives moves a normal quest into a turn-in-ready state rather than automatically granting final rewards.
10. A quest can only be turned in through a valid authored turn-in interaction.
11. Turn-in rewards are granted authoritatively by the server.
12. Completed non-repeatable quests remain recorded in character history.
13. Repeatable quests can become available again according to persistent server-authoritative reset rules.
14. Timed quests can fail when their authored deadline expires.
15. A normal quest can be abandoned/reacquired without reverting already committed narrative state.
16. Quest chains can branch.
17. A branch can make another branch unavailable.
18. Quest requirements can depend on previous quest/narrative/faction/profession state through reusable conditions.
19. Available and ready-for-turn-in quests can be identified using original non-conventional markers.
20. Hidden quests do not expose those markers prematurely.
21. An objective may show a fixed approximate world-map location.
22. A moving objective is not automatically tracked precisely on the map.
23. Players can choose which quests appear in the HUD tracker.
24. Dialogue choices can branch between authored nodes.
25. Dialogue conditions are actually evaluated at runtime.
26. A dialogue choice whose conditions fail is absent from the available choices.
27. Dialogue actions can accept/turn in quests and alter other authored state without custom NPC scripts.
28. Consequential dialogue actions are validated and executed by the server.
29. Party members may conduct independent dialogue with the same NPC.
30. One player's personal dialogue branch does not force the same choice on their party.
31. Character narrative state can alter future dialogue/quest availability for that character.
32. World narrative state can alter content for players on one persistent world.
33. GameWide narrative state can alter content across all persistent worlds.
34. Conflicting shared-state transitions resolve authoritatively to one valid outcome.
35. Books/items can provide narrative independently of NPC dialogue.
36. Normal quest content does not require scripted cutscenes.
37. Scripted scenes can be authored selectively where genuinely required.
38. Quest and dialogue editors expose the authored systems defined by this PDD.
39. Broken graph links, invalid quest references and impossible authoring errors are surfaced through validation.
40. Existing quest/dialogue assets can migrate to the completed architecture without requiring the useful data-driven foundation to be discarded.
