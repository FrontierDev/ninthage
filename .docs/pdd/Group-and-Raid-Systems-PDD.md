# Ninth Age — Group and Raid Systems Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Party and raid formation, logical group identity, membership, leadership, roles, permissions, subgroups, markers, group visibility, shared quest credit, group-owned instances, disconnect/rejoin behaviour, cross-world grouping requirements, raid lockouts and world-boss participation eligibility  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended party and raid system for **Ninth Age**.

It is authoritative for:

- party creation and capacity;
- raid creation and authored raid capacity;
- logical group identity;
- invitations and membership;
- leadership and assistants;
- party and raid roles;
- raid subgroups;
- ready checks and role checks;
- target markers and world markers;
- group visibility/state;
- party and raid communication permissions;
- party-to-raid and raid-to-party conversion;
- disconnect, reconnect and leader succession behaviour;
- group-owned instance association;
- joining and replacing players in active instances;
- cross-faction grouping;
- the product requirements for future cross-world grouping;
- group quest-credit context;
- loot-mode authority;
- raid lockout cadence at the group-system level;
- world-boss participation and personal reward eligibility.

This document does not define:

- dungeon structure, encounter count, dungeon pacing or five-player difficulty — [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- item generation or the detailed rules of Need/Greed and other loot modes — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- quest objective semantics — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- runtime scene loading or instance implementation — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- boss mechanics — encounter-specific content and [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- world-boss discovery, availability, spawning or event chains — [Open-World Events PDD](Open-World-Events-PDD.md);
- detailed PvP rules — [PvP PDD](PvP-PDD.md);
- final group/raid frame layout — [UI and UX PDD](UI-and-UX-PDD.md);
- general chat-system implementation — [Communication Systems PDD](Communication-Systems-PDD.md);
- persistent guild organisation — [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md).

Where current implementation conflicts with this document, this document defines intended product behaviour.

---

## 2. Design Pillars

### 2.1 A group is a logical social/gameplay entity, not a world-process object

Party or raid identity must be independent of:

- the current leader;
- a specific persistent world;
- a specific server process;
- a Unity scene;
- party versus raid presentation.

This is required so the system can support cross-world grouping in the long term without replacing the group model.

### 2.2 Grouping coordinates players without erasing individual state

Grouping may coordinate:

- combat;
- instance access;
- quest progress;
- loot;
- communication;
- markers.

It does not merge:

- character quest state;
- faction reputation;
- NPC disposition;
- narrative choices;
- loot eligibility where content uses personal eligibility.

### 2.3 Group organisation should be explicit and predictable

Leadership, assistants, roles, subgroup placement, loot mode and markers should have clear ownership.

The game should not silently reorganise groups or infer irreversible gameplay state from UI convenience.

### 2.4 Groups should survive normal disruption

A temporary disconnect, leader loss, zone change or instance transition should not unnecessarily destroy group membership or group-owned content.

### 2.5 Open-world cooperation should not require formal raid membership

World bosses and similar shared-world encounters remain open participation content.

Groups and raids improve coordination but do not monopolise the encounter or its rewards.

---

## 3. Existing Architectural Foundation

The current repository does not yet contain a production party/raid system.

Relevant existing foundations are:

- the World Runtime PDD already defines party-scoped and raid-scoped runtime-instance resolution;
- the Dungeon PDD defines five-player dungeon groups and group-owned dungeon instances;
- the Quest PDD defines cooperative quest credit while storing progress individually;
- the Items/Loot PDD defines shared group loot modes;
- the Open-World Events PDD defines shared world bosses and world-level boss availability;
- current PartyLootService and ActorDeathHooks behaviour is explicitly provisional test code.

The production party/raid system should therefore be introduced as the authoritative shared group foundation rather than formalising the current loot prototype.

---

## 4. Group Types

Ninth Age supports two primary temporary group types:

- **Party**
- **Raid**

Both use the same logical group identity and membership foundation.

A party may be converted to a raid without creating a new logical group.

A raid may be converted back to a party when its membership fits party capacity.

---

## 5. Logical Group Identity

Every party or raid has a stable **Group ID** for the lifetime of that group.

The Group ID is independent of:

- leader identity;
- group type;
- current persistent world;
- current runtime instance;
- current server process.

The Group ID is used by systems that need to refer to the group, including:

- instance ownership/resolution;
- group membership;
- group communication;
- group UI;
- raid subgroup state;
- loot context where appropriate.

### 5.1 Party-to-raid conversion

Converting a party to a raid preserves the Group ID.

### 5.2 Leader changes

Changing leader preserves the Group ID.

### 5.3 World movement

Moving one or more members between compatible worlds or instances does not create a new group.

---

## 6. Group Creation

A solo player is not represented as a one-person party.

A party is created when:

1. one player sends a valid group invitation;
2. the invited player accepts;
3. the server creates the logical group;
4. the inviter becomes the initial leader;
5. both players become members.

Further members may then be invited subject to capacity and permissions.

---

## 7. Party Capacity

A party supports a maximum of:

**5 players**

This matches the standard five-player dungeon structure defined by the Dungeon and Group Content PDD.

A party cannot exceed five members.

Inviting a sixth player does not silently convert the party into a raid.

The leader must explicitly convert the party to a raid first.

---

## 8. Raid Capacity

Raid size is **authored per raid/content definition**.

There is no single universal player count that every raid must use.

A raid definition may specify, as appropriate:

- minimum supported players;
- maximum supported players;
- intended/nominal player count;
- scaling range where the raid supports player-count scaling.

The underlying group system should support a technical ceiling of at least **40 players** even if initial authored raids use smaller sizes.

The Dungeon PDD rule still applies: raids may scale with player count, but must retain a meaningful authored minimum and must not collapse into small-party content.

---

## 9. Raid Subgroups

Raid members are organised into subgroups of up to:

**5 players**

Subgroups primarily support:

- raid organisation;
- raid-frame layout;
- assignments;
- authored mechanics that intentionally reference a subgroup.

Subgroup membership does not normally restrict:

- healing;
- buffs;
- threat;
- targeting;
- ordinary raid-wide gameplay.

An encounter may explicitly author subgroup-scoped mechanics where useful.

### 9.1 Moving members

The raid leader and assistants may move members between subgroups while the raid is out of combat.

Subgroup reassignment is blocked during active combat unless future encounter-specific rules explicitly require otherwise.

---

## 10. Roles

The group role model is:

- **Tank**
- **Healer**
- **Damage**

Roles are declared organisational intent.

A player's class or specialisation does not hard-lock which role they may select.

A role does not itself grant combat bonuses.

The player chooses their own role.

A leader may request or coordinate a different role, but does not silently overwrite another player's declared role outside a role-check flow.

---

## 11. Leadership

Every party or raid has exactly one **Leader**.

The leader has full temporary-group management authority.

Leadership does not grant combat/statistical power.

Leadership has no special ownership of a dungeon or raid instance.

A group-owned instance belongs to the logical group, not to the leader.

---

## 12. Raid Assistants

A raid may have one or more **Assistants**.

Assistants may:

- invite players;
- remove ordinary members;
- move raid members between subgroups out of combat;
- assign target markers;
- place/clear world markers;
- initiate ready checks;
- initiate role checks;
- send Raid Warning messages.

Assistants may not:

- remove the raid leader;
- change the active loot mode;
- disband the raid;
- transfer leadership unless the leader explicitly allows a supported leadership action.

An assistant does not automatically gain authority over another assistant for removal.

The leader may promote or demote assistants.

---

## 13. Member Invite Permissions

By default:

- party leader may invite;
- raid leader may invite;
- raid assistants may invite.

A party leader may optionally enable **member invites**, allowing ordinary party members to invite additional players while capacity remains.

Raid ordinary-member invites are not enabled by default.

Invite permissions are server-authoritative.

---

## 14. Invitations

A group invitation contains enough information for the recipient to identify:

- inviter;
- group type where already grouped;
- relevant group context.

The recipient may:

- accept;
- decline;
- allow the invitation to expire.

Exact invitation timeout is a UI/configuration detail.

A player already in another group must not be silently moved into the new group.

Any leave-and-join transition requires an explicit player action/confirmation.

---

## 15. Party-to-Raid Conversion

Only the party leader may convert a party to a raid.

Conversion:

- is explicit;
- preserves the Group ID;
- preserves existing members;
- preserves leader;
- preserves current instance association where compatible;
- enables raid subgroups and raid permissions.

Conversion is required before membership can exceed five.

---

## 16. Raid-to-Party Conversion

A raid may be converted back to a party when:

**membership is 5 players or fewer.**

Only the raid leader may perform the conversion.

Conversion preserves:

- Group ID;
- membership;
- leader;
- compatible instance association.

Raid-only assistant/subgroup state is discarded or normalised as appropriate.

---

## 17. Ready Checks

Ready checks are supported.

They may be initiated by:

- leader;
- raid assistants.

Each active member reports one of:

- **Ready**
- **Not Ready**
- **No Response**

The default ready-check response window is approximately:

**30 seconds**

The exact presentation/timer may be configured without changing the core behaviour.

A ready check is informational and does not itself start an encounter.

---

## 18. Role Checks

Role checks are distinct from ready checks.

They may be initiated by:

- leader;
- raid assistants.

Each player confirms one of:

- Tank;
- Healer;
- Damage.

Role checks update/confirm declared organisational roles.

They do not validate that a class or build is mechanically capable of performing the selected role.

---

## 19. Target Markers

The system supports a familiar set of:

**8 target markers**

Target markers may be assigned by:

- leader;
- raid assistants.

Rules:

- one target marker may identify one current target at a time;
- one target may have at most one target marker;
- assigning the same marker to a new target clears it from the previous target;
- markers clear when their target becomes invalid/despawns or they are explicitly cleared.

Exact visual symbols belong to UI/art direction.

---

## 20. World Markers

Raids support group-visible world/ground markers for encounter coordination.

The initial system should support at least:

**4 world markers**, with room to support up to **8** without changing the group model.

They may be placed/cleared by:

- leader;
- raid assistants.

World markers:

- are visible only to the relevant group/raid;
- are server-authoritative;
- exist in a specific world/instance context;
- clear when that context becomes invalid or the raid explicitly clears them.

They are organisational markers, not persistent world objects.

---

## 21. Group UI Data Contract

The group system must expose enough authoritative/member state for the client to present, where relevant:

- character name;
- health;
- primary/relevant resource;
- declared role;
- alive/dead state;
- connected/disconnected/offline state;
- leader state;
- assistant state;
- raid subgroup;
- current zone/map/instance;
- current persistent world where relevant;
- in-range/out-of-range state where meaningful;
- important debuff/dispellable state needed by group combat UI.

Exact frame layout, sorting, colours and presentation belong to the UI and UX PDD.

---

## 22. Group Communication

The group model must support the permissions/context required for:

- **Party Chat**
- **Raid Chat**
- **Raid Warning**

Raid Warning is available to:

- raid leader;
- raid assistants.

Party/Raid communication should continue to function when members are in different zones and, when cross-world grouping is implemented, across persistent worlds.

Detailed communication transport, moderation and chat UI belong to the Communication Systems PDD.

A separate raid-subgroup chat channel is not required initially.

---

## 23. Group Quest Progress

The Quest, Narrative and Dialogue PDD remains authoritative for objective-specific shared-credit behaviour.

The group system must provide the membership and relevance context necessary to implement those rules.

In summary:

- quest progress remains stored individually per character;
- nearby eligible group members may share kill credit without each personally damaging the target;
- encounter/escort/protection objectives may share credit with relevant eligible nearby members;
- interaction objectives normally require personal interaction unless explicitly shared;
- item/gather/craft objectives normally remain personal unless explicitly shared.

Group membership alone never grants quest progress from arbitrary distance or another world.

---

## 24. Group-Owned Dungeon Instances

Five-player dungeon instances belong to the **logical group**.

The instance is not owned by:

- leader;
- first entrant;
- an individual member.

Changing leader, roles or membership does not recreate the instance.

Replacement players who become valid group members may resolve into the group's existing instance subject to encounter-entry rules.

This extends the party-scope runtime model in the World Runtime and Instancing PDD.

---

## 25. Raid Instance Association

Where a raid uses a private runtime instance, the raid's logical Group ID is the group ownership/reference identity.

All eligible raid members resolve to the raid's shared runtime instance.

Changing leader or subgroup arrangement does not change instance ownership.

Raid lockout eligibility may further restrict whether a particular character may enter or receive rewards.

---

## 26. Joining an Active Instance

A valid new/rejoining member may enter an existing group instance while it remains active and their eligibility permits it.

### 26.1 Normal non-boss state

Joining is normally allowed:

- between encounters;
- during traversal;
- during ordinary dungeon activity;
- during normal trash combat where technically safe.

### 26.2 Active boss encounter

A joining player must not enter the active boss encounter space while that boss encounter is in progress.

The player may:

- enter the broader instance where safe;
- wait outside the encounter boundary;
- join normally after the encounter ends or resets.

This prevents late joining from altering an active boss pull.

---

## 27. Replacement Players

A group may replace departed/disconnected players subject to capacity.

### 27.1 Five-player dungeons

Because normal five-player dungeons have no lockout, a replacement may join the group's existing dungeon instance when otherwise eligible.

A replacement does not retroactively receive loot from previously defeated bosses.

### 27.2 Raids

Raid replacements may join the existing raid instance only if compatible with that raid's authored lockout policy.

Joining a progressed raid does not grant retroactive reward eligibility for already defeated bosses.

---

## 28. Disconnect and Reconnect

Temporary disconnect does not immediately remove a player from the group.

The group reserves the member's membership/slot for a default grace period of:

**5 minutes**

This grace period exists for ordinary connection loss and may be tuned globally.

After the grace period, the player may remain represented as offline until removed or until other session rules expire; the slot is not required to be automatically destroyed purely because the timer elapsed.

A reconnecting member should recover the same:

- Group ID membership;
- declared role;
- raid subgroup where still valid;
- assistant status where still valid;
- compatible group-instance access.

---

## 29. Normal Logout

A deliberate completed logout is different from a temporary connection loss.

After normal logout completes, the character leaves an ordinary temporary party/raid rather than creating a long-lived phantom membership.

Persistent social groups such as guilds are separate systems.

---

## 30. Group Persistence and Server Restart

Ordinary parties and raids are **session-oriented**, not permanently stored social organisations.

They do not need to survive a full game/server restart as intact groups.

However:

- instance/raid lockout state that is character/content persistence must survive according to its owning system;
- the group data model must remain independent of one server process to permit future cross-world operation.

---

## 31. Leader Disconnect

A brief leader disconnect does not immediately transfer leadership.

The default leader-disconnect succession grace period is:

**90 seconds**

If the leader reconnects within that time, leadership remains unchanged.

If succession is required after the grace period:

1. the oldest active assistant becomes leader;
2. otherwise the oldest active group member becomes leader.

If the former leader reconnects after succession, they rejoin with their remaining appropriate membership state and do not automatically reclaim leadership.

---

## 32. Leader Leaving

If the leader deliberately leaves:

1. leadership transfers immediately to the oldest active assistant;
2. if there is no active assistant, leadership transfers to the oldest active member.

The logical group and its group-owned instances remain intact.

---

## 33. Leaving a Group

A player may leave a normal party/raid freely unless a specific PvP or encounter system imposes a separate restriction.

Leaving:

- removes membership;
- removes group communication permissions;
- removes group friendliness where applicable;
- removes normal future shared-credit entitlement;
- does not reset the remaining group's instance.

Instance relocation/exit follows Section 35.

---

## 34. Removing Members

The leader may remove group members.

Raid assistants may remove ordinary raid members.

Assistants may not remove:

- leader;
- other assistants.

The server applies membership removal immediately at the logical-group level.

Removal must not be implemented as an unsafe instantaneous teleport while the removed character is actively in combat.

---

## 35. Instance Exit After Removal

If a player is removed from a private group instance:

- group membership ends immediately;
- the player may remain physically present until safe relocation is possible;
- they must not retain ordinary group credit/loot rights for future activity;
- once out of active combat/encounter state, the runtime relocates them to an appropriate authored safe exit/entrance context.

The exact transition implementation belongs to World Runtime.

---

## 36. Vote Kick

Manually formed parties and raids do **not** require a vote-kick system.

Leadership manages membership.

If automated matchmaking is introduced later, matchmade groups may define a separate vote-kick policy without changing the manually formed group model.

---

## 37. Group Disbanding

A group dissolves when:

- the final member leaves; or
- the leader explicitly disbands it.

Disbanding invalidates the temporary logical group.

It does not necessarily synchronously destroy every runtime instance referenced by the group.

Instance cleanup follows the lifecycle rules of World Runtime and the relevant content/lockout system.

---

## 38. Loot Mode Authority

The Items, Equipment and Loot PDD defines the supported shared group loot modes:

- Need / Greed;
- Master / Leader Assignment;
- Round Robin;
- Free-for-All;
- Random Assignment.

Need / Greed remains the default.

The **group leader** controls the active group loot mode.

### 38.1 Loot-mode timing

Loot mode cannot be changed during an active boss encounter.

A loot-mode change applies only to future loot eligibility/context.

It must not retroactively alter:

- an active roll;
- loot already generated;
- participant snapshots already established;
- rewards from an encounter whose loot context has already been fixed.

The Items PDD remains authoritative for the mechanics of each loot mode.

---

## 39. Cross-Faction Grouping

Players from politically opposed factions may voluntarily group together for PvE.

Group membership does not change:

- either character's political faction;
- either character's reputation;
- static faction diplomacy;
- NPC faction membership.

### 39.1 Temporary player friendliness

While validly grouped for normal cooperative PvE, group members are treated as friendly to each other for cooperative player-to-player gameplay.

This temporary group friendliness ends when group membership ends.

### 39.2 NPC disposition remains individual

NPCs continue to evaluate each character separately.

A cross-faction party may therefore contain:

- one player welcomed by a settlement;
- another player treated neutrally;
- another player attacked because of faction/reputation.

Grouping does not bypass:

- reputation requirements;
- permits;
- service restrictions;
- NPC hostility;
- narrative access.

### 39.3 PvP exceptions

PvP-specific contexts may:

- prohibit cross-faction groups;
- suspend temporary group friendliness;
- impose other authored PvP participation rules.

Those exceptions belong to the PvP PDD.

---

## 40. Cross-World Grouping — Long-Term Requirement

Cross-world grouping is an explicit long-term product requirement.

The initial implementation may constrain invitations/interactions to players in the same persistent world if necessary.

However, the architecture must **not** make same-world locality a permanent requirement of group identity.

A future group may contain members in different persistent worlds while preserving:

- Group ID;
- leader/assistant state;
- roles;
- raid subgroup organisation;
- group chat;
- group membership.

### 40.1 World locality and gameplay

Members in different worlds are socially grouped but cannot automatically share physical gameplay.

Cross-world membership does not grant:

- remote quest credit;
- remote loot eligibility;
- remote encounter participation;
- remote NPC interaction.

Gameplay credit still requires the member to be physically/relevantly present in the correct world/instance.

---

## 41. Joining Another Member's World

Cross-world grouping does not automatically transfer players.

The system should eventually support an explicit action equivalent to:

**Join Member's World**

The player chooses to request transfer into a compatible world occupied by a group member.

No automatic rule forces the whole group onto the leader's world.

The leader's world may be presented conveniently in UI, but does not own the group.

### 41.1 Transfer validation

World transfer may fail because:

- destination world is full;
- destination is unavailable;
- character is not eligible;
- content/world state is incompatible;
- the player is in combat;
- the player is in an active encounter;
- another runtime restriction forbids transfer.

Failure must leave group membership intact.

### 41.2 No implied teleportation

Cross-world group membership does not create a lore/gameplay teleport ability.

World transfer remains governed by World Runtime and other movement/travel rules.

---

## 42. Cross-World Instance Formation

In the future, eligible group members from different persistent worlds may converge into the same private dungeon/raid instance.

The private instance is resolved from:

- logical Group ID;
- content eligibility;
- relevant lockout state;

not from whichever persistent world originally hosted the leader.

This allows a group to remain a cross-world social object while entering shared instanced content deliberately.

---

## 43. Raid Lockouts

Normal five-player dungeons have no lockout, as defined by the Dungeon PDD.

Raids use authored lockouts.

### 43.1 Per-raid duration

Each raid definition authors an integer lockout duration:

**X days**

Different raids may therefore use different lockout periods.

There is no requirement for one global weekly raid reset cadence.

### 43.2 Reset basis

A raid lockout follows that raid's authored reset cycle/reset point.

The duration and reset schedule are server-authoritative.

### 43.3 Character eligibility

Raid lockout/reward eligibility is tracked per character.

A group may contain members with different raid eligibility states.

The raid's authored policy determines whether such a character may:

- enter the current progressed instance;
- participate without reward eligibility;
- become bound to the instance/progression;
- receive rewards from remaining encounters.

The exact boss-specific versus instance-progression binding policy may be authored per raid/content policy; the mandatory shared rule is the configurable X-day lockout cadence.

### 43.4 Persistence

Raid lockout state must survive:

- logout;
- disconnect;
- world transfer;
- server restart.

It is not dependent on the temporary raid group surviving.

---

## 44. World Boss Participation

World bosses remain shared-world encounters.

No party or raid owns the boss.

Formal group membership is not required to participate.

Group/raid membership provides coordination but does not itself grant reward eligibility.

There is no first-tag ownership rule for world bosses.

---

## 45. World Boss Reward Eligibility

World bosses use **personal participation eligibility** followed by independent personal reward rolls.

This is intentionally different from normal shared group corpse loot.

### 45.1 Binary eligibility, not competitive ranking

Participation is evaluated as an eligibility threshold:

**Eligible / Not Eligible**

It is not a bronze/silver/gold contribution ranking.

Eligible players do not receive better loot because they produced higher damage/healing numbers than other eligible players.

### 45.2 Meaningful contribution

The server may recognise meaningful contribution through relevant encounter activity such as:

- damage dealt to the boss/encounter;
- effective healing/support of participating players;
- tanking or serving as an active threat target;
- completing authored encounter mechanics/objectives.

The purpose is to reject trivial last-second tagging while recognising legitimate support roles.

Exact thresholds may be tuned per encounter/framework.

### 45.3 Group independence

Eligibility is evaluated per character.

Being in the same party/raid as an eligible character does not automatically make another character eligible.

A raid does not obtain one shared world-boss loot pool.

### 45.4 Dead players

A player who meaningfully participated and then died may remain eligible provided they remain part of the encounter context and have not otherwise invalidated participation.

Death does not erase legitimate contribution.

---

## 46. World Boss Personal Rewards

Each eligible character receives their own independent reward resolution.

The default model is:

- **one personal item roll** against the boss's authored world-boss loot table;
- the item roll may result in no item;
- optional guaranteed baseline rewards such as gold, reputation or another authored reward may also exist.

One player's successful item roll does not reduce another player's chance.

Group/raid membership does not change reward quality.

The Open-World Events PDD remains authoritative that world-boss loot tables should be narrower/more recognisable than comparable dungeon/raid tables and broadly comparable in appropriate item power.

Exact:

- item probabilities;
- item quantities if a boss intentionally differs from the default;
- item levels;
- rarity;
- baseline currencies/reputation;

remain item/content balance.

---

## 47. World Boss Reward Lockouts

A world boss may author a **per-character reward lockout**.

This is separate from:

- boss respawn;
- boss availability cooldown;
- shared-world boss state.

Example:

- boss availability may return after 2 days;
- a character's personal item eligibility may reset after 7 days.

The exact reward-lockout duration is configurable per world boss.

A character who is reward-locked may still participate in the encounter unless other content rules prohibit it.

The server must make reward eligibility understandable to the player before or during participation where practical.

---

## 48. World Boss Reward Authority

The server is authoritative for:

- encounter participation tracking;
- meaningful-contribution evaluation;
- personal reward-lockout state;
- eligible/not-eligible resolution;
- independent personal loot rolls;
- reward granting.

The client must not infer or grant world-boss eligibility locally.

---

## 49. Group Death and Wipes

Death does not dissolve a party/raid.

A wipe does not dissolve a party/raid.

Group membership is independent from encounter state.

Encounter reset, recovery and resurrection rules are owned by the relevant combat/dungeon/raid encounter systems.

---

## 50. Server Authority

The server is authoritative for:

- Group ID;
- membership;
- invitations;
- party/raid type;
- leader;
- assistants;
- roles;
- raid subgroups;
- ready-check state;
- role-check state;
- markers;
- loot mode;
- group-instance association;
- disconnect/reconnect membership state;
- leader succession;
- cross-faction group friendliness;
- world-transfer group eligibility inputs;
- raid lockout state;
- world-boss participation eligibility.

Clients request changes and display authoritative results.

---

## 51. Current Implementation Changes Required

A production implementation must add a real group system.

Required foundations include:

- logical Group ID;
- server-owned party/raid records;
- membership and invitations;
- roles;
- leadership and assistants;
- raid subgroups;
- party/raid conversion;
- disconnect/reconnect handling;
- group UI replication;
- group chat context;
- marker state;
- ready/role checks;
- group-owned instance resolution;
- quest-credit membership/relevance queries;
- loot-mode state;
- cross-faction friendliness rules;
- raid lockout persistence;
- world-boss personal eligibility/reward state.

### 51.1 Current PartyLootService

The current PartyLootService explicitly states that it is a basic placeholder.

Its behaviour must not become the production party model.

Shared loot distribution should instead use the Items/Loot PDD rules and authoritative group membership/loot-mode state.

### 51.2 Current ActorDeathHooks

Current death hooks use threat-table membership as a temporary proxy for:

- XP recipients;
- quest kill credit;
- personal test loot.

This is not the permanent group-credit or world-boss eligibility contract.

Production code must query the appropriate group/quest/world-boss systems.

---

## 52. UX Requirements

Players must be able to understand:

- whether they are in a party or raid;
- group leader;
- raid assistants;
- their role;
- raid subgroup;
- member connectivity;
- member location/world/instance context;
- who is ready/not ready;
- active loot mode;
- target/world markers;
- whether a member is in another world;
- whether a world/instance transfer request failed and why;
- world-boss reward eligibility/lockout where relevant.

The interface must not imply that a cross-world member is physically nearby.

Exact visual design belongs to UI/UX.

---

## 53. Intentionally Deferred Decisions

The following remain content, balance or dependent-system decisions:

- exact authored player count of each raid;
- exact scaling formula for a scaling raid;
- exact raid boss loot quantities;
- exact raid-specific boss/instance binding policy within the X-day lockout framework;
- exact initial raids' X-day lockout values;
- exact ready-check visual presentation;
- exact invitation timeout;
- final target-marker symbols;
- whether the initial world-marker count ships at 4 or a larger value up to 8;
- exact meaningful-contribution thresholds for world bosses;
- exact world-boss item chances;
- exact world-boss reward-lockout periods;
- detailed PvP restrictions on cross-faction grouping;
- initial technical rollout date for cross-world grouping.

These details do not require redesigning the group foundation.

---

## 54. Locked Design Decisions

The following are locked by this PDD:

1. Parties and raids share one logical group foundation.
2. Every group has a stable Group ID independent of leader, world, server process and party/raid type.
3. A party is created when the first invitation is accepted.
4. Solo players are not one-person parties.
5. Party capacity is 5.
6. Raid size is authored per raid.
7. The group system supports a technical raid ceiling of at least 40 players.
8. Raid subgroups contain up to 5 players.
9. Raid subgroups are organisational unless content explicitly authors subgroup mechanics.
10. Raid subgroup movement is restricted to out-of-combat normal operation.
11. Group roles are Tank, Healer and Damage.
12. Players declare their own role; class/spec does not hard-lock the role selector.
13. Every group has one leader.
14. Raids support assistants with delegated organisational permissions.
15. Loot-mode changes are leader-only.
16. Party member invites may be enabled by the party leader; raid ordinary members do not invite by default.
17. Party-to-raid conversion is explicit and preserves Group ID.
18. Raid-to-party conversion is allowed at 5 or fewer members and preserves Group ID.
19. Ready checks and role checks are supported.
20. Ready checks default to approximately 30 seconds.
21. Eight target markers are supported.
22. World markers are supported with an initial capacity of at least four and architecture for up to eight.
23. Group quest progress remains individually stored and follows the Quest PDD's relevance rules.
24. Dungeon/raid instance ownership references the logical group rather than the leader.
25. Late joining is blocked from an active boss encounter space until the encounter ends/resets.
26. Five-player dungeon replacements may join an existing group instance because normal dungeons have no lockout.
27. Temporary disconnect reserves membership for a default 5-minute grace period.
28. Normal completed logout removes the character from the temporary group.
29. Ordinary parties/raids are session-oriented and need not persist intact through a full server restart.
30. Leader disconnect uses a default 90-second succession grace period.
31. Leader succession prefers the oldest active assistant, then the oldest active member.
32. Manually formed groups do not require vote kick.
33. Removing a member ends logical membership immediately but does not forcibly relocate the character during unsafe active combat.
34. Need/Greed remains the default group loot mode as defined by the Items/Loot PDD.
35. Loot mode cannot be changed retroactively for an already-established encounter/loot context.
36. Cross-faction PvE grouping is allowed.
37. Group membership does not change faction, reputation or NPC disposition.
38. Group members are temporarily friendly to one another for normal cooperative gameplay, subject to PvP-specific exceptions.
39. NPCs evaluate each cross-faction group member individually.
40. Cross-world grouping is an explicit long-term requirement.
41. Group identity must not be architecturally tied to one persistent world/server process.
42. Cross-world grouping does not automatically transfer players between worlds.
43. Future world joining is an explicit validated player action.
44. Cross-world membership alone grants no remote quest/loot/encounter credit.
45. Raid lockout duration is authored per raid as X days.
46. Raid lockout state is character-persistent and server-authoritative.
47. World bosses are open participation and are not owned by a party/raid or first tag.
48. World-boss reward eligibility is personal and based on meaningful participation.
49. World-boss participation is binary eligible/not eligible, not contribution-ranked.
50. Damage, effective healing/support, tanking/threat participation and authored mechanics may establish meaningful participation.
51. Dead players may remain eligible after legitimate participation.
52. Group membership alone does not grant world-boss reward eligibility.
53. Each eligible world-boss participant receives an independent personal reward roll.
54. The default world-boss item model is one personal item roll which may produce no item.
55. A world boss may provide optional guaranteed baseline rewards in addition to the personal item roll.
56. World-boss personal reward lockout is configurable per boss and separate from boss respawn/availability.
57. World-boss personal reward state is character-specific and server-authoritative.
58. Death/wipes do not dissolve groups.
59. Group membership and management state are server-authoritative.

---

## 55. Dependencies

This PDD depends on or constrains:

- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Open-World Events PDD](Open-World-Events-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [Communication Systems PDD](Communication-Systems-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- [PvP PDD](PvP-PDD.md).

---

## 56. Validation Criteria

The Group and Raid system satisfies this PDD when:

1. Accepting the first invite creates one logical group with a stable Group ID.
2. The group remains the same logical group after leader changes.
3. Explicit party-to-raid conversion preserves Group ID and members.
4. Explicit raid-to-party conversion works when five or fewer members remain.
5. A party cannot exceed five players.
6. Different raids can author different minimum/maximum/intended player counts.
7. A raid can organise members into five-player subgroups.
8. Leader/assistants can move raid members between subgroups out of combat.
9. Players can declare Tank, Healer or Damage roles without class-hard-locking.
10. Leader and assistant permissions are enforced server-side.
11. Ready checks distinguish Ready, Not Ready and No Response.
12. Role checks collect player-declared roles.
13. Eight target markers can be assigned without conflicting duplicate ownership.
14. Group-visible world markers can be placed and cleared authoritatively.
15. Group UI can distinguish nearby, elsewhere, disconnected and future cross-world members.
16. Eligible nearby party members can receive shared quest kill credit without each tagging the target.
17. Quest progress remains separately stored per character.
18. A five-player dungeon instance remains owned by Group ID after a leader leaves.
19. A valid replacement can join the group's existing dungeon instance.
20. A late joiner cannot enter an active boss encounter space.
21. A disconnected player can reconnect within the grace period without group recreation.
22. Leadership does not immediately transfer on a brief disconnect.
23. Leadership deterministically succeeds after the configured grace period when required.
24. Removing a player does not reset the remaining group's instance.
25. Removed players are safely relocated after combat rather than teleported unsafely mid-fight.
26. Group loot mode is authoritative and changes do not rewrite active/existing loot.
27. Cross-faction players can form a PvE group without changing their faction/reputation.
28. Cross-faction group members are cooperative-friendly while grouped in normal PvE.
29. NPC disposition remains individual for cross-faction group members.
30. The group data model can represent members in different persistent worlds without changing Group ID.
31. Cross-world membership does not grant remote gameplay credit.
32. An explicit future world-join request can fail without dissolving the group.
33. Different raids can configure different X-day lockout durations.
34. Raid lockout state survives logout/server restart independently of the temporary raid group.
35. A world boss can be fought by grouped and ungrouped players simultaneously.
36. No first tag gives exclusive world-boss ownership/rewards.
37. World-boss contribution from damage, support/healing, tanking or mechanics can qualify a player.
38. Eligibility is binary rather than competitively ranked.
39. An eligible dead participant can remain reward eligible.
40. A group member who did not meaningfully participate is not made eligible solely by group membership.
41. Eligible characters receive independent personal world-boss reward rolls.
42. One player's world-boss item reward does not reduce another player's chance.
43. World-boss reward lockout can differ from world-boss respawn/availability.
44. A reward-locked player can still participate unless the boss explicitly prohibits it.
45. Current provisional PartyLootService/ActorDeathHooks logic is no longer the source of group membership, quest-credit or world-boss reward truth.
