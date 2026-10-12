# Ninth Age — Communication Systems Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Text chat, spatial communication, whispers, party/raid/guild channels, public channels, custom channels, free-text and authored emotes, account-level friends and blocking, social presence, rich chat links, client chat history, spam prevention, reporting and moderation  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended player communication model for **Ninth Age**.

It is authoritative for:

- text-chat channel types;
- proximity communication;
- Say and Yell;
- free-text emotes;
- `/e` and `/me`;
- authored emote commands;
- whispers/replies;
- party and raid communication;
- guild and private-guild communication;
- public world channels;
- custom player channels;
- cross-world communication behaviour;
- cross-faction communication behaviour;
- friend relationships;
- ignore/block behaviour;
- social presence exposed through communication;
- clickable game-entity links;
- chat-history behaviour;
- profanity filtering;
- spam/flood protection;
- reporting and moderation context;
- communication-system authority and persistence.

This document does not define:

- group/raid membership — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- guild membership/ranks/halls — [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md);
- mail item/Cash-on-Delivery economics — [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- chat/combat-log visual styling beyond communication requirements — [UI and UX PDD](UI-and-UX-PDD.md);
- world/instance routing — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- PvP rules — [PvP PDD](PvP-PDD.md);
- accessibility-specific communication alternatives — [Accessibility and Input PDD](Accessibility-and-Input-PDD.md).

Where implementation conflicts with this document, this PDD defines intended product behaviour.

---

## 2. Design Pillars

### 2.1 Physical speech should respect physical space

Communication that represents a character speaking in the world should depend on:

- distance;
- compatible world/map context;
- compatible runtime instance.

Say, Yell and free-text emotes should feel spatial.

### 2.2 Social relationships should survive world boundaries

Persistent social communication should not stop functioning merely because players are on different persistent worlds.

Whispers, guild chat, custom channels and qualifying group channels are not physical speech.

### 2.3 Persistent worlds should retain community identity

Default public community channels are world-specific.

Ninth Age should not collapse every persistent world into one global public chat stream.

### 2.4 Cross-faction social friction should not obstruct cooperation

Cross-faction players can communicate normally.

Faction identity remains meaningful through gameplay, politics, reputation, world access and future PvP rather than an artificial inability to understand another player.

### 2.5 Communication should be controllable

Players need:

- filtering;
- tab/channel control;
- reliable blocking;
- reporting;
- optional profanity filtering;
- spam protection.

### 2.6 Communication should be useful without becoming permanent surveillance

Ordinary player-visible chat history is local and finite.

The game does not expose a permanent server-hosted archive of every ordinary conversation.

Operational/moderation evidence may be retained separately according to service policy.

---

## 3. Communication Categories

Ninth Age communication is divided into four conceptual groups.

### 3.1 Spatial communication

- Say;
- Yell;
- free-text Emote;
- authored local emotes.

### 3.2 Persistent social communication

- Whisper;
- Party;
- Raid;
- Raid Warning;
- Guild;
- Private Guild / Officer;
- Custom channels.

### 3.3 World-community channels

- General;
- Trade;
- Looking for Group;
- future explicitly authored public channels.

### 3.4 Non-chat information

- system messages;
- combat log.

System output and combat logs may share UI infrastructure with chat, but they are not player communication channels.

---

## 4. Say

Say represents ordinary local speech.

Supported commands include:

- `/s`;
- `/say`.

Default radius:

**25 metres**

at authoritative world scale.

The radius should be data/configuration driven, but 25 m is the initial authoritative default.

---

## 5. Say Delivery Rules

A Say message is delivered only to eligible recipients that are:

- in the same compatible persistent-world/map context;
- in the same compatible runtime instance;
- within the authoritative Say radius;
- not prevented by communication/block policy.

Spatial proximity alone does not bypass instance isolation.

Two characters at numerically equivalent coordinates in different private instances cannot hear one another.

---

## 6. Yell

Yell represents loud local speech over a substantially larger area.

Supported commands include:

- `/y`;
- `/yell`.

Default radius:

**100 metres**

at authoritative world scale.

The radius should be data/configuration driven, but 100 m is the initial authoritative default.

---

## 7. Free-Text Emote

Free-text emotes are supported through both:

- **`/e`**
- **`/me`**

These are aliases for the same communication action.

Example:

```text
/me examines the strange carving.
```

may display as:

```text
Ortellus examines the strange carving.
```

The system must not require one of `/e` or `/me` to behave differently.

---

## 8. Free-Text Emote Range

Free-text emotes use the same default spatial range as Say:

**25 metres**

They obey the same:

- world;
- map;
- instance;
- distance;

delivery restrictions as Say.

---

## 9. Free-Text Emote Formatting

The game may prepend or style the acting character's name.

It should not automatically rewrite the player's prose beyond necessary safe text processing.

The system should not:

- automatically add punctuation;
- change sentence tense;
- replace the player's wording with a canned emote.

---

## 10. Authored Emotes

Ninth Age may support authored commands such as:

- `/wave`;
- `/bow`;
- `/laugh`;
- `/sit`;
- other animation-capable emotes.

An authored emote may trigger:

- character animation;
- authored local text;
- both.

Authored emotes are distinct from the unrestricted free-text `/e` / `/me` command.

---

## 11. Whisper

Whispers are direct online communication between players.

Supported commands should include:

- `/w`;
- `/whisper`;
- `/tell`.

Whispers work **cross-world**.

Physical distance does not restrict whispers.

---

## 12. Whisper Recipient Identity

Whisper routing must resolve the target through stable authoritative player/account identity.

The UI may use character names as the normal human-facing lookup because character full names are globally unique.

The server must not trust an arbitrary client-provided display name as proof of identity.

---

## 13. Reply

`/r` replies to the player's most recent valid whisper conversation target.

The UI should also permit:

- clicking a player name;
- context-menu Whisper;
- selecting a recent whisper conversation.

---

## 14. No Offline Whispers

Whispers are **online-only**.

If the target is offline, the sender receives a clear failure message.

Ninth Age does not turn whispers into asynchronous offline messages.

Mail is the appropriate asynchronous communication system.

---

## 15. Party Chat

Party chat is available to current party members.

Supported command:

- `/p` / `/party`.

Party chat is not constrained by physical distance.

The communication transport must support cross-world party chat whenever party membership spans persistent worlds.

This does not independently require the Group PDD to enable cross-world grouping before that feature is otherwise ready.

---

## 16. Raid Chat

Raid chat is available to current raid members.

Supported command:

- `/ra` / `/raid`.

Raid chat is not constrained by physical distance.

It must support cross-world routing whenever the raid itself contains members on different persistent worlds.

---

## 17. Raid Warning

Raid Warning is a higher-priority raid communication channel.

A player may send Raid Warning only when permitted by raid leadership rules, normally:

- raid leader;
- raid assistant.

The Group and Raid Systems PDD owns those roles/permissions.

---

## 18. Guild Chat

Guild chat is available to members of the same guild.

Supported command:

- `/g` / `/guild`.

Guild chat works **cross-world** because the guild itself is a global persistent organisation.

---

## 19. Private Guild / Officer Chat

Guilds have a private permission-controlled guild channel.

Supported command may include:

- `/o`.

The system may label this channel **Officer** in default UI, but access is not tied to a hard-coded rank named "Officer".

Access is granted through guild permissions.

This preserves the Guild PDD's custom rank architecture.

---

## 20. Cross-Faction Communication

Cross-faction players can communicate normally through supported channels.

Faction does not automatically:

- garble text;
- translate text into nonsense;
- prevent whispers;
- prevent party/raid chat;
- prevent guild chat;
- prevent local Say/Yell where the players otherwise qualify.

Future language systems may add flavour/content but must not silently redefine this rule without a PDD update.

---

## 21. Public Channel Scope

Default public community channels are **world-specific**.

A player on one persistent world does not automatically share General/Trade/LFG public chat with every other persistent world.

This preserves world-level community identity.

---

## 22. General Channel

General is scoped to an authored region/zone context within one persistent world.

Its purpose is ordinary local community discussion.

Players may:

- leave;
- mute;
- filter;

the channel.

Exact zone/region boundaries follow world authoring.

---

## 23. Trade Channel

Trade is a world-specific public channel associated with appropriate settlement/market areas.

It should be available when the player is in an authored area where Trade chat is permitted.

Trade should not become a universal game-wide channel that makes physical market districts irrelevant.

---

## 24. Looking for Group Channel

Looking for Group is world-wide within one persistent world.

It is not limited to one zone.

It is intended for manual social group formation and does not replace any future automated group-finding system.

---

## 25. Default Public Channel Restraint

Ninth Age should not create a large list of redundant default channels.

The initial core set is:

- General;
- Trade;
- Looking for Group.

Additional public channels require an explicit purpose.

---

## 26. Custom Channels

Players may create custom text channels.

Custom channels are lightweight coordination spaces, not replacements for guilds.

They may be used for:

- communities;
- events;
- cross-guild coordination;
- roleplay;
- recurring groups;
- other player-organised communication.

---

## 27. Custom Channel Features

A custom channel should support:

- a name;
- membership;
- optional password;
- channel creator/owner;
- moderators;
- mute/remove capability;
- cross-world communication;
- persistent membership for a character across normal relog.

The exact maximum number of joined/created custom channels remains tuning data.

---

## 28. Custom Channel Persistence

Custom-channel state should survive normal server restart and relog where still valid.

The service may expire channels that are:

- empty;
- abandoned;
- inactive for a sufficiently long period;

according to service policy.

Exact inactivity expiry is not locked by this PDD.

---

## 29. Custom Channel Moderation

Channel owners/moderators may manage their own lightweight channel membership.

This does not replace platform/game moderation.

A custom channel owner cannot use channel moderation to bypass:

- account block;
- service moderation;
- global account sanctions.

---

## 30. Channel Commands and UI

Slash commands are supported as an efficient power-user interface.

The same communication actions must also be reachable through normal UI where appropriate.

Players should not need to memorise slash commands to:

- whisper;
- change active channel;
- report;
- block;
- reply.

---

## 31. Chat Window

Chat is a HUD/text communication module.

The UI should support:

- multiple tabs;
- user-created tabs;
- per-tab channel filters;
- selectable active send channel;
- scrollback;
- optional timestamps;
- adjustable opacity;
- adjustable text size within supported readability ranges;
- move/resize behaviour appropriate to a HUD text panel.

Visual styling follows Radiant Slate.

---

## 32. Chat Window and Combat Log

Chat and Combat Log may use the same general text-panel framework.

They remain logically separate systems.

Combat-log filtering must not depend on social channel membership.

Communication filtering must not treat combat events as player chat.

---

## 33. Chat Tab Examples

A player should be able to construct arrangements such as:

```text
General
Guild
Group
Whispers
Combat
```

This is a UI configuration example, not a required fixed tab set.

---

## 34. Channel Colours

Channels should have consistent semantic visual treatment.

Exact colour values belong to UI/UX, but channel identity must remain quickly distinguishable.

Colour alone must not be the only cue; channel labels/prefixes remain available.

---

## 35. Local Chat History

Recent player-visible chat history is retained **locally on the client**.

It may survive:

- UI reload;
- brief disconnect;
- normal relog.

It is finite and clearable.

Exact retention count/time is a client setting/tuning decision.

---

## 36. No Permanent Player-Visible Server Archive

Ninth Age does not provide a permanent player-facing server archive of all ordinary chat.

This is distinct from:

- moderation evidence;
- abuse/security logs;
- audit information required to investigate reports.

Operational retention policy is a service/operations decision.

---

## 37. Rich Game-Entity Links

Chat supports clickable rich links.

Initial supported link types should include:

- item;
- ability/spell;
- quest;
- player/character;
- guild;
- map location / map pin.

---

## 38. Rich Link Behaviour

Hovering/clicking a link should perform an appropriate action such as:

- open tooltip;
- show item details;
- show ability details;
- show quest details;
- open player context;
- open guild information;
- open/map-focus a location.

Exact interaction follows UI/UX and the linked subsystem.

---

## 39. Rich Link Authority

Rich links must transmit validated typed entity references, not arbitrary trusted client markup.

The receiver must not accept forged client-formatted text as authoritative proof that:

- an item exists;
- a player owns an item;
- a quest is valid;
- a location is valid.

Where ownership/state matters, the server validates it.

---

## 40. Account-Level Friendship

Friend relationships are **account-level**.

A player should not need to add every alt on the same account separately.

Friendship is mutual: one account sends a request and the other accepts it.

---

## 41. Friend Presentation

The social UI may present the friend's currently active character.

Useful presence information includes, subject to privacy rules:

- online/offline;
- active character name;
- character level;
- class;
- current persistent world;
- broad location.

Guild membership remains character-specific even though friendship is account-level.

---

## 42. Presence Privacy

Players must have control over sensitive social presence beyond the minimum needed for the system.

At minimum, the final settings system should allow the project to restrict whether friends can see detailed location.

Exact privacy options remain a tuning/UX decision.

Blocking always overrides friend/presence visibility.

---

## 43. Account-Level Block

Block/ignore is **account-level**.

Blocking one player blocks that account's alternate characters as well.

This prevents a blocked user from trivially bypassing the block by logging onto another character.

---

## 44. Block Enforcement

Blocking is server-enforced.

A blocked account cannot directly contact the blocker through ordinary direct social mechanisms.

At minimum, block prevents:

- whispers;
- friend requests;
- direct party invitations;
- direct raid invitations;
- guild invitations;
- custom-channel invitations;
- direct mail from the blocked account;
- equivalent future direct-contact requests.

---

## 45. Public/Shared Chat and Blocking

Messages from blocked accounts should not be delivered to the blocking player in ordinary public/custom channels where technically appropriate.

A block does not rewrite global world state for other players.

System-critical events are not hidden merely because they were caused by a blocked player.

If two blocked accounts later share a party/raid/guild through another route, the block remains effective for direct/player-authored communication unless a specific system requires otherwise.

---

## 46. Block and Existing Relationships

Blocking an existing friend removes or suspends the friend relationship.

Blocking does not automatically:

- kick either player from a guild;
- remove them from a raid/party created by other players;
- destroy shared economic state.

Those systems retain their own authority.

---

## 47. Profanity Filter

Ninth Age provides an **optional client-side profanity filter**.

Default:

**enabled**

The player may disable it.

The filter affects local display rather than rewriting authoritative chat content for every recipient.

---

## 48. Profanity Filter Philosophy

The profanity filter should not normally prevent a message from being sent merely because a substring matches a word list.

This avoids excessive false positives and language-specific failures.

Harassment/abuse is handled through:

- block;
- report;
- moderation.

---

## 49. Server-Side Spam Protection

Communication routing requires server-side flood/spam controls.

Normal players should be able to converse naturally.

The system should use:

- burst allowance;
- rate limiting;
- repeated-identical-message detection;
- escalating restriction for continued flooding;
- stronger detection for whispers sent rapidly to many distinct recipients;
- other abuse signals where needed.

Exact thresholds are tuning/service data.

---

## 50. Spam Failure Behaviour

When a player exceeds communication limits, the system should:

- reject or delay the abusive message as appropriate;
- tell the sender that they are rate-limited;
- avoid duplicating messages;
- avoid disconnecting normal users for a single accidental burst.

Persistent automated abuse may trigger stronger moderation action.

---

## 51. Reporting

Players can report communication directly from:

- a chat message;
- a player context menu.

Reporting should be low-friction.

---

## 52. Report Categories

Initial categories should include:

- harassment;
- hate/abusive language;
- spam;
- advertising;
- inappropriate name;
- cheating/exploitation;
- other.

Categories may evolve operationally.

---

## 53. Report Evidence

A communication report should preserve authoritative context sufficient for moderation.

This may include:

- message ID;
- sender identity;
- reporter identity;
- channel;
- timestamp;
- relevant surrounding messages/context;
- server/world context where relevant.

The reporter should not need to manually reproduce the offending text as the sole evidence.

---

## 54. Moderation Authority

The server/service layer is authoritative for:

- communication sanctions;
- channel moderation;
- spam restrictions;
- block enforcement;
- report evidence association.

Client-side hiding alone is insufficient for direct-contact blocks.

---

## 55. System Messages

System messages are not player chat.

They may communicate:

- group/guild membership changes;
- loot;
- XP;
- reputation;
- quest updates;
- errors;
- server/service notices;
- other game events.

Players may filter them into chat tabs as allowed by UI/UX.

---

## 56. Combat Log

Combat Log is not a communication channel.

Its events come from authoritative gameplay/combat systems.

It may share:

- text rendering;
- scrolling;
- tab shell;
- filtering UI;

with chat without sharing communication semantics.

---

## 57. Cross-World Routing

Cross-world channels include at minimum:

- Whisper;
- Guild;
- Private Guild / Officer;
- Custom channels;
- Party/Raid/Raid Warning whenever those groups span worlds.

These channels must not assume all recipients are connected to one physical game-server process.

---

## 58. World-Specific Routing

World-specific channels include:

- General;
- Trade;
- Looking for Group;
- future authored public community channels unless explicitly declared otherwise.

Spatial channels are narrower still because they require matching spatial/instance context and range.

---

## 59. Server Authority

The server/service layer is authoritative for:

- sender identity;
- recipient identity;
- channel membership;
- channel permission;
- spatial eligibility;
- block relationships;
- custom-channel membership;
- guild/party/raid eligibility;
- rate limiting;
- report evidence;
- rich-link validation where required.

Clients render messages but do not authoritatively decide who should receive them.

---

## 60. Spatial Chat Calculation

Say/Yell/Emote range checks use authoritative world-position data.

They must not rely solely on:

- client transforms;
- rendered distance;
- scene membership.

Delivery conceptually requires:

```text
compatible world/map/instance
AND
authoritative distance <= channel range
AND
recipient communication policy permits delivery
```

---

## 61. Persistence

Persistent communication/social state includes, where applicable:

- account friend relationships;
- account block relationships;
- custom-channel membership;
- custom-channel definitions while valid;
- user chat-tab/filter preferences;
- locally retained chat history.

Guild/party/raid membership is persisted/owned by their respective systems.

Ordinary chat messages are not long-term gameplay state.

---

## 62. Data/Service Identity

Communication records should use stable IDs rather than mutable display strings.

Conceptual identifiers include:

- AccountID;
- CharacterID;
- GuildID;
- GroupID/RaidID;
- ChannelID;
- MessageID.

The exact schema/service architecture belongs to implementation design.

---

## 63. Offline Behaviour

When a player disconnects:

- they cannot receive new whispers as an offline queue;
- persistent guild/custom membership remains;
- local chat history may remain on their client;
- friend presence updates to offline after normal session rules resolve.

Reconnection does not create duplicate friend/channel membership.

---

## 64. Mail Boundary

Mail is asynchronous communication and an economic/item-transfer mechanism owned by the relevant economy/mail design.

Communication rules constrain it where social safety matters:

- account block prevents direct mail from the blocked account;
- mail is not a substitute for online whisper delivery;
- mail economic/item rules remain outside this PDD.

---

## 65. Cross-Faction Boundary

The communication system does not enforce a faction-language barrier.

Future PvP design may define context-specific restrictions only if explicitly added to the owning PDDs.

Open-world hostility alone does not make chat text unintelligible.

---

## 66. Voice Communication

Built-in voice chat is **not part of the initial Ninth Age communication system**.

The text communication architecture must not assume integrated voice exists.

Voice may be reconsidered later through a dedicated design decision.

---

## 67. UI Requirements

The player must be able to:

- identify the current send channel;
- switch channels quickly;
- create/filter tabs;
- distinguish channel source;
- scroll history;
- click player names;
- whisper/reply;
- block;
- report;
- inspect rich links;
- see rate-limit/error feedback.

All presentation follows the UI/UX PDD.

---

## 68. Accessibility Boundary

Communication UI must remain compatible with future accessibility requirements including:

- text scaling;
- contrast;
- keyboard navigation;
- screen-reader/alternative output if later adopted.

The Accessibility/Input PDD owns final requirements.

---

## 69. Open / Deferred Details

The following remain tuning, service-policy or dependent-system decisions:

- exact maximum custom channels per character/account;
- exact custom-channel inactivity expiry;
- exact chat-history retention count/time;
- exact spam-rate thresholds;
- exact moderation retention duration;
- exact profanity word lists/localisation;
- final presence/privacy settings;
- exact public-channel zone boundaries;
- whether additional world-public channels are needed;
- exact channel colours;
- exact chat font-size options;
- whether custom channels support additional role levels beyond owner/moderator/member;
- future built-in voice communication.

These do not change the core model.

---

## 70. Locked Design Decisions

The following decisions are locked by this PDD:

1. Say is proximity-based spatial communication.
2. Say uses a 25 m default radius.
3. Yell is proximity-based spatial communication.
4. Yell uses a 100 m default radius.
5. `/e` and `/me` are aliases for the same free-text emote command.
6. Free-text emote uses the same 25 m default radius as Say.
7. Spatial chat requires compatible world/map/instance context.
8. Spatial chat uses authoritative world distance.
9. Authored emotes may exist separately from free-text emotes.
10. Whispers work cross-world.
11. Whispers are online-only.
12. `/r` replies to the most recent valid whisper conversation.
13. Party and raid communication is not spatially limited.
14. Party/raid communication can route cross-world when group membership spans worlds.
15. Raid Warning is permission-controlled by group leadership rules.
16. Guild chat works cross-world.
17. Private/Officer guild chat is permission-controlled, not tied to a hard-coded rank name.
18. Cross-faction players can communicate normally.
19. There is no default faction-language obfuscation barrier.
20. Default public community channels are world-specific.
21. General is region/zone scoped within a world.
22. Trade is associated with appropriate settlement/market areas.
23. Looking for Group is world-wide within one persistent world.
24. The initial public-channel set should remain deliberately small.
25. Player-created custom channels are supported.
26. Custom channels can work cross-world.
27. Custom channels may be password protected.
28. Custom channels support owner/moderator controls.
29. Chat UI supports multiple/filterable user tabs.
30. Recent player-visible chat history is retained locally on the client.
31. The game does not expose a permanent server-hosted archive of ordinary player chat.
32. Rich links support game entities including items, abilities, quests, players, guilds and map locations.
33. Rich links use validated typed references rather than trusted arbitrary client markup.
34. Friend relationships are account-level and mutual.
35. Guild membership remains character-specific.
36. Blocking is account-level.
37. Blocking is server-enforced.
38. Blocking applies across alternate characters of the blocked account.
39. Blocking prevents direct whispers and ordinary direct social requests.
40. Blocking prevents direct mail from the blocked account.
41. Blocking does not automatically remove members from shared guild/group structures.
42. The profanity filter is client-side, optional and enabled by default.
43. Profanity filtering does not normally prevent message transmission.
44. Server-side spam/flood protection is required.
45. Reports preserve authoritative message/context evidence.
46. System messages are distinct from player chat.
47. Combat Log is distinct from player communication.
48. Cross-world communication must not assume one physical game-server process.
49. Ordinary chat messages are not long-term gameplay persistence.
50. Built-in voice chat is not part of the initial communication system.

---

## 71. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](../MMORPG-Master-PDD.md);
- [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md);
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md);
- future [PvP PDD](PvP-PDD.md);
- future [Accessibility and Input PDD](Accessibility-and-Input-PDD.md).

---

## 72. Validation Criteria

The Communication Systems design is correctly implemented when:

1. `/s` and `/say` send only to valid recipients within 25 m by default.
2. `/y` and `/yell` send only to valid recipients within 100 m by default.
3. Two characters at the same coordinates in different private instances cannot hear spatial chat.
4. `/e` and `/me` produce the same free-text emote behaviour.
5. Free-text emotes use Say range.
6. Authored emotes can trigger local text/animation separately from free-text emote.
7. A player can whisper another online player on another persistent world.
8. A player cannot queue an offline whisper.
9. `/r` replies to the expected recent whisper target.
10. Party/raid members can communicate independent of physical distance.
11. Cross-world group chat works whenever the group itself spans worlds.
12. Guild chat reaches online guild members across worlds.
13. Private guild chat respects guild permission rather than a rank-name check.
14. Cross-faction players can use ordinary supported chat with each other.
15. General does not merge every persistent world into one channel.
16. General respects its region/zone scope.
17. Trade is available only in configured world contexts.
18. LFG spans the current persistent world rather than all worlds.
19. Players can create/join a custom channel.
20. Custom channels can communicate cross-world.
21. Optional custom-channel passwords work.
22. Custom-channel moderation cannot override an account block.
23. Chat tabs can filter channels independently.
24. Recent chat can survive normal relog locally without becoming authoritative server history.
25. Item/ability/quest/player/guild/location links can be clicked/inspected safely.
26. Forged rich markup cannot create an authoritative nonexistent entity.
27. Adding a friend applies at account level.
28. A friend's active character can be presented without creating separate friend records for every alt.
29. Blocking one character blocks direct contact from that account's other characters.
30. A blocked account cannot whisper the blocker.
31. A blocked account cannot send direct social invitations to the blocker.
32. A blocked account cannot send direct mail to the blocker.
33. Public messages from blocked accounts can be suppressed for the blocker without affecting other recipients.
34. The profanity filter can be enabled/disabled locally.
35. Disabling the profanity filter does not alter what other players see.
36. Flood/spam limits are server-enforced.
37. Ordinary rapid conversation does not feel artificially throttled under normal use.
38. A player can report a specific message directly.
39. A report carries authoritative sender/message/context metadata.
40. System messages can be filtered separately from social chat.
41. Combat Log can share UI infrastructure without being treated as a chat channel.
42. Cross-world routing works even when recipients are served by different game-server processes.
43. Communication state survives server restart where it is supposed to persist.
44. No built-in voice system is required to satisfy the initial Communication PDD.

---

## 73. Design Summary

Ninth Age treats communication according to what it represents.

**Speech is spatial. Relationships are persistent. Worlds remain communities.**

Say, Yell and free-text `/e` / `/me` are grounded in authoritative physical proximity and instance context.

Whispers, guild chat, custom channels and qualifying group communication work across persistent worlds because they represent social relationships rather than physical sound.

Default public channels remain world-specific so each persistent world can develop a recognisable community.

Cross-faction players can communicate normally.

Friends and blocks operate at account level so players do not need to manage every alt separately and blocked users cannot evade the block by switching characters.

The system uses local finite chat history, optional client profanity filtering, server-side spam protection, authoritative report evidence and rich game-entity links.

The result should feel like a traditional MMO communication system without preserving unnecessary historical friction.
