# Ninth Age — Crafting System Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Gathering, professions, recipe progression, cooperative crafting, item-quality progression, binding rules, social gathering activities, crafting economy and related UX  
**Last updated:** 2026-10-04

---

## 1. Purpose and Authority

This document defines the intended crafting system for **Ninth Age** and is the ground truth for future design and implementation work in this area.

The crafting system is intended to support long-term character and account progression without making basic profession levelling dependent on other players. Cooperation is deliberately concentrated at higher levels of recipe mastery and prestige crafting, where it can be socially meaningful rather than merely inconvenient.

Where future implementation work conflicts with this document, this document takes precedence until the design is intentionally revised.

This document distinguishes between:

- **Locked design decisions** — decisions already made and which implementation should treat as requirements.
- **Open design decisions** — areas where the system direction is known but exact rules, numbers or UX have not yet been decided.

No implementation should silently invent an answer for an open design decision. Such decisions should be resolved explicitly and this document updated before they become permanent behaviour.

---

## 2. Design Pillars

The crafting system is governed by the following principles.

### 2.1 Gathering should not consume specialist-profession slots

All characters can gather. Players must not be forced to sacrifice a major crafting profession merely to obtain raw materials.

### 2.2 Ordinary profession progression should be self-sufficient

A Blacksmith should not be unable to progress Blacksmithing because no Alchemist, Enchanter or other specialist happens to be available.

Standard levelling recipes should normally be craftable using materials that can be gathered universally, purchased through ordinary trade, or otherwise obtained without mandatory dependence on another specialist profession.

### 2.3 Professions are independently trainable, but not independently perfectible

Cross-profession dependencies and cooperative crafting should become increasingly important at higher quality and prestige levels.

The system should therefore allow players to level a profession alone while still making endgame and mastery-level crafting meaningfully social.

### 2.4 Mastery belongs to exact recipes

General profession level determines what a character is capable of learning and what material tiers they can work with.

It does **not** automatically make every recipe of that profession equally masterful.

Mastery progression is tracked for the exact recipe being crafted.

### 2.5 Low-level mastery must remain worthwhile

Players should have reasons to master recipes before level cap.

A Masterwork item created while levelling should retain account-level value even after the original character outgrows it.

### 2.6 Cooperation must be active rather than ceremonial

Cooperative crafting should not reduce to multiple players standing near a station while one player presses a button.

Where cooperation is required or advantageous, multiple participants should meaningfully contribute to the crafting process.

### 2.7 Gathering should not fail

A valid gathering action against a node the player is qualified to harvest should succeed.

Progression improves access, speed and common-material yield rather than introducing failure rolls or material-purity tiers.

### 2.8 Social gathering should not be competitive

Large communal gathering opportunities should reward participation without creating first-hit, tag-stealing or node-denial behaviour.

---

## 3. Profession Structure

### 3.1 Universal gathering skills

Every character has access to the following gathering skills:

- **Mining**
- **Skinning**
- **Herbalism**
- **Fishing**
- **Farming**

These do not consume specialist crafting-profession slots.

Each gathering skill has its own progression and access requirements.

### 3.2 Universal crafting profession

**Cooking** is available to every character.

Cooking is not treated as a gathering skill. It is a full crafting profession and follows the normal recipe and progression rules, but it does not consume one of the character's specialist crafting-profession slots.

### 3.3 Specialist crafting professions

Each character may choose **two** specialist crafting professions from:

- **Blacksmithing**
- **Engineering**
- **Tailoring**
- **Leatherworking**
- **Enchanting**
- **Jewelcrafting**
- **Alchemy**
- **Inscription**
- **Woodworking**

Inscription includes **runescribing** and covers magical writing and marking such as runes, scrolls, glyphs and sigils.

Woodworking covers wooden weapons and components, including examples such as bows, crossbows, staves, shafts, poles, frames and related wooden weapon/tool components.

### 3.4 Profession progression philosophy

Profession progression is intended to be long-form and substantial rather than something that is trivially completed shortly after acquisition.

Profession level and recipe mastery serve different purposes:

- **Profession level** gates recipes, material tiers, techniques and general competence.
- **Recipe mastery** determines the character's progression toward higher-quality versions of a specific recipe.

A high-level Blacksmith who has never practised a particular sword recipe is not automatically a master of that sword.

---

## 4. Recipe Quality and Mastery Model

The canonical crafting-quality progression is:

> **Standard → Superior → Masterwork → Legendary**

These terms are fixed unless this document is explicitly revised.

### 4.1 Standard

**Standard** is the baseline form of a recipe.

Standard crafting is the normal solo crafting experience and is the foundation of profession progression.

A Standard recipe should generally be self-contained enough that a player can level their profession without routine mandatory dependence on another specialist profession.

Standard crafting may still use tradeable materials obtained from other players, but another specialist crafter should not ordinarily be a hard requirement for basic profession advancement.

### 4.2 Superior

**Superior** is the first quality tier where cooperation becomes deliberately important.

Superior crafting is:

- possible alone;
- more efficient when performed cooperatively;
- one of the primary ways in which the character develops mastery of that exact recipe.

A player choosing to craft Superior items alone should face a meaningful penalty in **time, resource consumption, or both**.

The exact penalty has not yet been decided.

Participation in a cooperative Superior craft should contribute toward exact-recipe mastery for the participants who meaningfully perform relevant crafting work.

Superior crafting is therefore both a production tier and part of the mastery-learning process.

### 4.3 Masterwork

**Masterwork** represents personal mastery of the exact recipe.

Once the character has satisfied the recipe's mastery requirements, the Masterwork version can again be crafted solo.

This return to solo crafting is intentional. The player has already demonstrated expertise through progression and cooperative practice.

Masterwork therefore represents:

- individual mastery;
- reliable personal production;
- a meaningful reward for investing in a specific recipe rather than only raising the profession broadly.

#### 4.3.1 Finished Masterwork items

Finished Masterwork equipment and other appropriate finished Masterwork items are **Bind to Account**.

This is an explicit design requirement.

A Masterwork sword made for a level-20 character should remain useful to the player's account after the original character outgrows it. It can later be passed to an alternate character on the same account.

The purpose is to:

- reward recipe mastery at lower profession and character levels;
- prevent low-level mastery from becoming disposable progression;
- give the account a persistent inheritance of personally crafted equipment;
- keep Masterwork items out of the open player economy;
- make crafting history materially useful to future characters.

A finished Masterwork item must not become freely tradeable simply because it is no longer useful to its original character.

#### 4.3.2 Masterwork components

Masterwork crafting components are **Bind on Pickup** to the character who creates them.

They represent the specific crafter's personal mastery and are not ordinary tradeable commodities.

Masterwork components cannot normally be handed to another character or contributed to another player's ordinary craft.

The Legendary system is the explicit exception described below.

### 4.4 Legendary

**Legendary** is the highest current crafting tier and returns the system to cooperation.

Legendary crafting may use Masterwork components supplied by multiple master crafters, provided each contributing player personally crafted the Masterwork component they contribute.

The contribution mechanism must preserve the Bind-on-Pickup nature of those components.

This means:

- a player does not trade a Masterwork component to another player's inventory;
- the component does not become a normal market commodity;
- the system must record or validate that the contributor is the owner and original crafter where required;
- the component is committed directly to the cooperative Legendary craft.

The intended progression rhythm is therefore:

> **Learn personally → practise together → demonstrate personal mastery → combine masters' work together.**

---

## 5. Exact-Recipe Mastery

Recipe mastery is tracked per exact recipe.

Examples:

- mastery of one longsword recipe does not automatically grant mastery of another longsword recipe;
- mastery of an iron sword does not imply mastery of a later steel sword;
- high Blacksmithing level alone does not grant Masterwork production for all Blacksmithing recipes.

The system must distinguish between:

1. whether the profession level allows the recipe to be learned or used;
2. whether the recipe is known;
3. the character's mastery progress for that exact recipe;
4. the highest quality tier the character is currently permitted to produce for that recipe.

### 5.1 Mastery progression

Superior crafting participation is intended to be a major source of exact-recipe mastery progression.

The system should favour actual crafting and participation over passive unlocks.

### 5.2 Open mastery decisions

The following are **not yet decided**:

- the numerical mastery scale;
- the number of Standard or Superior crafts required to progress;
- whether Standard crafting contributes to mastery and, if so, by how much;
- whether solo Superior and cooperative Superior grant different mastery progress;
- whether mastery progress can decay;
- whether mastery progress is continuous or milestone-based;
- whether each quality tier requires a separate mastery threshold;
- whether failed or interrupted cooperative stages contribute partial mastery;
- whether recipe mastery has any profession-level prerequisites beyond the recipe's own unlock requirement;
- whether discovery, quests, trainers or special achievements can grant mastery progress.

Implementation must not lock in these values without an explicit design decision.

---

## 6. Cooperative Crafting

Cooperative crafting is a first-class system rather than an ordinary crafting action with additional players attached.

### 6.1 Purpose

Cooperative crafting exists to:

- make higher-quality production social;
- create reasons for crafters to work together;
- accelerate or improve Superior crafting;
- provide a route toward exact-recipe mastery;
- enable multi-master Legendary crafts.

### 6.2 Workstation-stage model

The intended interaction is that a craft may contain multiple production stages performed at appropriate workstations or workstation functions.

Examples discussed include:

- firing;
- hammering;
- quenching;
- grinding.

These are examples of the intended interaction model, **not a locked universal four-stage recipe structure**.

Different professions should use stages appropriate to their own fantasy and materials.

A cooperative Blacksmithing workflow should not simply be copied mechanically onto Alchemy, Tailoring or Inscription.

### 6.3 Participation

Multiple participants may perform different stages of the same craft.

The system should distinguish meaningful participation from merely being grouped or standing nearby.

Only meaningful participation should count toward recipe mastery or other cooperative benefits.

### 6.4 Solo Superior crafting

Superior crafting remains possible without other players.

Solo Superior crafting must be deliberately less efficient than cooperative Superior crafting through:

- additional time;
- additional resources;
- or a combination of both.

### 6.5 Open cooperative-crafting decisions

The following remain **explicitly undecided**:

- the exact number of participants supported by a cooperative craft;
- whether one participant is formally the lead crafter;
- how the craft is initiated and how players join it;
- exact workstation-stage counts;
- stage durations;
- whether stages can occur concurrently;
- how stage skill checks, if any, work;
- whether participants must know the recipe;
- whether participants must meet a minimum profession level;
- how mastery credit is divided;
- whether different roles/stages grant different mastery credit;
- whether a craft can continue if a participant disconnects or leaves;
- ownership rules for non-Legendary cooperative output;
- how materials are reserved, committed, refunded or lost when a cooperative craft is cancelled;
- exact solo-Superior time and resource penalties;
- whether cooperative Superior crafting improves output beyond avoiding the solo penalty;
- how cooperative crafting is represented in the world and UI.

These decisions require separate design work before implementation.

---

## 7. Binding and Ownership Rules

The current canonical ownership model is:

| Item/category | Binding rule |
|---|---|
| Ordinary gathered materials | Tradeable unless another system explicitly says otherwise |
| Standard crafted output | **TBD by item/economy design** |
| Superior crafted output | **TBD by item/economy design** |
| Masterwork component | **Bind on Pickup** |
| Finished Masterwork equipment/item | **Bind to Account** |
| Legendary components/output | **TBD**, except for the special Masterwork-component contribution rule |

### 7.1 Binding principle

Mastery-gated materials must not become ordinary commodities merely because cooperative crafting requires more than one crafter.

The special Legendary contribution system exists specifically to allow cooperation without removing the personal nature of Masterwork components.

### 7.2 Open ownership decisions

Still unresolved:

- whether all finished Masterwork outputs are Bind to Account or only equipment and selected durable items;
- binding behaviour of Superior items;
- binding behaviour of Legendary outputs;
- whether Legendary output binds to the initiating crafter, recipient, account, party, guild or another ownership target;
- whether a Legendary craft can nominate a recipient before crafting begins;
- whether Bind-to-Account Masterwork items can be stored in account-wide storage, mailed between characters, or transferred through a dedicated system;
- whether Masterwork items can be salvaged, disenchanted or otherwise converted into tradeable value.

Until resolved, implementations should preserve the locked distinction that **Masterwork components are BoP** and **finished Masterwork equipment is Bind to Account**.

---

## 8. Gathering System

### 8.1 No gathering failure

Gathering never fails when:

- the node is valid;
- the character meets the required gathering-skill threshold;
- the node is available to that player.

There is no "failed to mine", "failed to skin" or equivalent random failure outcome.

### 8.2 No material purity tiers

Gathered materials do not use purity/quality variants such as:

- Poor Iron;
- Fine Iron;
- Perfect Iron.

The material itself is the material.

Craft quality comes from crafting progression and recipe mastery rather than random raw-material quality.

### 8.3 Effects of gathering skill

Increasing a gathering skill provides:

- access to more advanced resources and nodes;
- faster gathering;
- a greater chance for an ordinary gather to produce **two common units instead of one**.

The +1 common-material yield mechanic applies to ordinary common yield.

### 8.4 Rare materials

Rare-material drop rates do **not** increase with gathering skill.

Rare-material availability is determined by the source/node/activity rather than by high-level gatherers receiving increasingly inflated rare-material probabilities.

Gathering progression therefore makes the character more capable and efficient without allowing skill level to trivialise the rarity of scarce resources.

### 8.5 Open gathering decisions

The following remain undecided:

- gathering skill caps;
- progression curves;
- exact node-skill requirements;
- exact gathering-speed scaling;
- the formula for the +1 common-yield chance;
- whether the +1 yield chance has a hard cap;
- how Farming progression differs from ordinary harvest-node progression;
- exact Fishing interaction and progression rules;
- respawn behaviour for normal gathering nodes;
- whether nodes are individual, shared, phased or mixed depending on activity;
- how gathering behaves while grouped;
- whether ordinary gathering nodes can be exhausted locally;
- profession-specific tool requirements.

---

## 9. Quarries and Communal Mining

Quarries are a higher-throughput, social Mining activity.

They are not a separate profession.

### 9.1 Core quarry rules

Quarries are intended to be:

- communal;
- noncompetitive;
- timed activities;
- permit- and/or faction-gated where appropriate;
- rewarding each participant personally rather than splitting a fixed resource pool.

They are closer in spirit to a shared depletion/progression event than to a conventional first-come mining node.

A player joining a quarry should not reduce another player's entitlement to rewards.

### 9.2 Quarry progression

Mining activity contributes toward shared quarry progression.

The current direction is for quarries to have multiple depth phases, with deeper phases providing modest improvements to rewards.

More active miners should accelerate progression through the quarry, but the acceleration must be capped so that large groups do not instantly complete the activity.

### 9.3 Quarry rewards

Rewards are personal.

The quarry should therefore encourage players to gather together rather than compete over tags, spawn points or final hits.

### 9.4 Open quarry decisions

The following are not final:

- exact number of depth phases;
- exact duration of a quarry event;
- exact contribution required to advance phases;
- acceleration formula and participant cap;
- reward tables by phase;
- exact permit system;
- exact faction requirements;
- whether quarry access uses consumable permits, permanent unlocks, reputation thresholds or another system;
- quarry respawn/cooldown schedule;
- contribution thresholds required to qualify for rewards;
- whether late joiners can receive all phase rewards;
- anti-AFK rules;
- whether other professions receive analogous communal gathering activities.

---

## 10. Hidden Dungeon Harvests

Higher-level dungeons may contain hidden profession-gated harvesting opportunities.

The established direction is:

- access is gated by the relevant profession/gathering skill;
- these are higher-tier opportunities rather than ordinary levelling nodes;
- harvest opportunities use a **three-day cadence**;
- a character can harvest the relevant opportunity **once per reset**;
- rewards may include special high-tier materials or catalysts.

These nodes should reward bringing developed gathering/profession characters into dungeon content without making them mandatory for routine profession levelling.

### 10.1 Open dungeon-harvest decisions

Still to decide:

- whether the three-day reset is character-based, account-based or global;
- whether dungeon harvests are personal or shared;
- whether every eligible group member can harvest independently;
- exact skill requirements;
- whether hidden nodes require discovery mechanics;
- exact material tables;
- whether the node is tied to the dungeon's normal lockout;
- whether an instance can contain multiple profession-specific hidden harvests;
- whether access requires a corresponding specialist craft, gathering skill, or either depending on the node.

---

## 11. Cross-Profession Dependencies

### 11.1 Standard progression

Routine Standard profession levelling should avoid hard dependencies on other specialist professions.

External components may exist, but they should not routinely block progression.

### 11.2 Higher-quality crafting

Cross-profession dependencies become more appropriate as quality increases.

Superior, Masterwork and especially Legendary recipes may require:

- specialist components;
- catalysts;
- profession-specific subassemblies;
- cooperative stage participation;
- personally crafted Masterwork components.

The intent is to increase social and economic interdependence at the point where the output justifies it.

### 11.3 Legendary cooperation

Legendary crafting is the strongest expression of this principle.

A Legendary recipe may require several master crafters to contribute personally made Masterwork components.

This must be done without turning those BoP components into freely tradeable goods.

### 11.4 Open dependency decisions

Still undecided:

- how many professions a single Legendary recipe may depend on;
- whether every specialist profession must have a route into Legendary crafting;
- whether cross-profession components are consumed directly or transformed into intermediate assemblies;
- whether contributors receive rewards, mastery progress, credit or recognition beyond ordinary payment/player agreement;
- whether guild systems participate in large cooperative crafting.

---

## 12. Economy Principles

The crafting economy must preserve reasons to:

- gather;
- trade;
- level professions;
- specialise;
- practise exact recipes;
- cooperate;
- master lower-level recipes;
- pursue prestige materials.

At the same time, it must avoid making normal profession progression dependent on a healthy market at every level.

### 12.1 Masterwork and the economy

Finished Masterwork equipment being Bind to Account deliberately removes it from ordinary resale.

This is not a defect.

Masterwork is intended to represent personal/account progression rather than the highest-volume commercial tier.

Players may still participate economically through:

- raw materials;
- Standard output;
- Superior output where allowed;
- ordinary components;
- services;
- Legendary cooperation;
- other tradeable outputs defined later.

### 12.2 Economy integration and remaining decisions

The [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md) now defines the shared economic framework:

- Gold is the ordinary currency.
- Eligible crafted goods may participate in the global exchange.
- Market access is channel-based, including restricted channels where authored.
- Public and private crafting work orders are supported.
- Work-order customer materials use authoritative escrow.
- Market listings use deposits and transaction taxation.
- NPC repair is a recurring gold sink under the Items PDD.
- There is no general salvage/disenchant material economy.
- Recrafting/rerolling of acquired base items is not part of the Items PDD.

The following crafting-specific details remain unresolved:

- binding rules for Standard and Superior output;
- exact crafting fees and workstation costs;
- exact vendor values for crafted outputs;
- material/component sink quantities by recipe/profession;
- whether unwanted BoA Masterwork gear has any account-safe recycling path that does not create an open trade loophole;
- exact service-fee behaviour for cooperative crafting outside work orders;
- whether recipe mastery itself consumes special materials or gold.

---

## 13. Recipe Data Requirements

The eventual recipe definition must be capable of representing, at minimum:

- profession;
- required profession level;
- input materials;
- output;
- workstation requirements;
- quality-tier availability;
- exact-recipe mastery state;
- Superior solo/cooperative behaviour;
- cooperative stages;
- stage eligibility;
- cross-profession requirements;
- Masterwork component requirements;
- Legendary contribution requirements;
- binding rules;
- special-source materials;
- output quantity;
- craft time.

This is a **product requirement**, not a prescribed code schema.

Implementation may represent these concepts differently provided the behaviour remains equivalent.

### 13.1 Quality-specific recipe data

A recipe should not require four unrelated duplicated definitions merely because Standard, Superior, Masterwork and Legendary variants exist.

The implementation should preserve the conceptual identity of one recipe with multiple quality/mastery states unless there is a strong technical or design reason to separate them.

The exact data model remains to be designed.

---

## 14. User Experience Requirements

### 14.1 Crafting interface

The player must be able to understand:

- current profession level;
- known recipes;
- recipe profession-level requirement;
- current mastery progress for a recipe;
- highest unlocked quality;
- materials required;
- which materials are missing;
- whether a Superior craft is being performed solo or cooperatively;
- the consequence of choosing solo Superior production;
- any cooperative participants and their roles/stages;
- binding status of the output before crafting begins.

### 14.2 Binding clarity

The interface must explicitly display:

- **Bind on Pickup** for Masterwork components;
- **Bind to Account** for finished Masterwork equipment/items where applicable;
- Legendary contribution behaviour before a contributor commits a component.

Players must never discover binding behaviour only after consuming expensive materials.

### 14.3 Cooperative-crafting interface

The eventual cooperative UI must make clear:

- who owns/initiated the craft;
- what stage is active;
- which participants can perform it;
- which materials each participant is contributing;
- whether those materials are committed yet;
- what happens if someone leaves;
- who receives the finished output.

The exact interface has not yet been designed.

---

## 15. Progression and Account Value

Crafting should create progression at three different scales.

### Character-scale progression

- gathering skill;
- profession level;
- learned recipes;
- exact-recipe mastery.

### Account-scale progression

- finished Bind-to-Account Masterwork equipment;
- the practical inheritance of prior characters' crafting achievements.

### Social progression

- relationships with other specialist crafters;
- access to cooperative Superior production;
- multi-master Legendary projects;
- communal gathering activities such as quarries.

These three forms of progression should reinforce one another without collapsing into a single account-wide profession system.

Recipe mastery itself remains character-specific unless this document is later revised.

---

## 16. Anti-Goals

The system should explicitly avoid the following:

- requiring gathering professions to consume specialist crafting slots;
- random gathering failure;
- raw-material purity tiers;
- profession level automatically granting mastery of every recipe;
- mandatory cross-profession dependencies for routine levelling;
- making cooperative crafting a passive proximity check;
- making communal gathering competitive;
- allowing high gathering skill to inflate rare-material drop rates;
- turning Masterwork components into ordinary tradeable commodities;
- making low-level Masterwork gear disposable after the original character outgrows it;
- allowing implementation convenience to silently decide unresolved product rules.

---

## 17. Design Decision Register

### 17.1 Locked decisions

The following are currently locked:

1. Universal gathering skills are Mining, Skinning, Herbalism, Fishing and Farming.
2. Cooking is universal and is a full crafting profession.
3. Characters choose two specialist crafting professions.
4. Specialist crafts are Blacksmithing, Engineering, Tailoring, Leatherworking, Enchanting, Jewelcrafting, Alchemy, Inscription and Woodworking.
5. Inscription includes runescribing.
6. Recipe quality progression is Standard → Superior → Masterwork → Legendary.
7. Standard is the normal solo baseline.
8. Superior favours cooperation but remains possible solo at a time/resource disadvantage.
9. Exact-recipe mastery is distinct from general profession level.
10. Superior participation is part of exact-recipe mastery progression.
11. Masterwork represents personal mastery and can be crafted solo once mastery requirements are satisfied.
12. Masterwork components are Bind on Pickup.
13. Finished Masterwork equipment/items are Bind to Account.
14. Legendary crafting can accept contributors' personally crafted BoP Masterwork components through a dedicated contribution mechanism.
15. Normal profession levelling should not routinely require another specialist profession.
16. Gathering does not fail.
17. Gathered materials do not have purity/quality tiers.
18. Gathering skill increases access, gathering speed and the chance of receiving one additional common unit.
19. Gathering skill does not improve rare-material drop rates.
20. Quarries are communal, noncompetitive Mining activities with personal rewards.
21. Quarry activity progresses through shared depth/progression states and more miners accelerate progress subject to a cap.
22. Quarries are permit/faction-gated where designed as such.
23. Hidden high-tier dungeon harvest opportunities are profession-level-gated and operate on a three-day cadence, once per reset.

### 17.2 Open decisions requiring future design

The following must be explicitly resolved before full implementation:

1. Exact profession and gathering skill caps.
2. Profession XP/skill progression curves.
3. Exact recipe-mastery progression formula and thresholds.
4. Whether Standard crafts grant mastery progress.
5. Exact solo-Superior time/resource penalties.
6. Cooperative-crafting participant limits.
7. Cooperative workstation-stage rules and timings.
8. Cooperative disconnect/cancellation behaviour.
9. Material reservation/refund rules.
10. Ownership and binding of Standard output.
11. Ownership and binding of Superior output.
12. Binding and ownership of Legendary output.
13. Whether every finished Masterwork item category is BoA or only selected finished items/equipment.
14. Exact account-transfer mechanism for BoA Masterwork items.
15. Whether Masterwork items can be salvaged/disenchanted/recycled.
16. Quarry depth-phase count.
17. Quarry timing, contribution, acceleration and reward formulas.
18. Exact quarry permit/faction rules.
19. Dungeon-harvest reset scope and node-sharing behaviour.
20. Complete recipe catalogue.
21. Complete material catalogue.
22. Cross-profession dependency matrix.
23. Crafting fees, workstation costs and economy sinks.
24. Recrafting, modification and salvage systems.
25. Cooperative crafting UI.
26. Crafting workstation placement and world-distribution rules.
27. Whether recipe mastery can be influenced by quests, discoveries, trainers or achievements.
28. Whether recipe mastery has any account-level visibility or achievement layer despite remaining character-specific.
29. Rules for contributors' rewards and credit in Legendary crafts.
30. Whether professions other than Mining receive quarry-like social gathering/crafting activities.

---

## 18. Required Future Design Work

Before implementation of the full system, separate design work should resolve at least four areas.

### 18.1 Progression model

Define:

- skill caps;
- profession-level curve;
- exact-recipe mastery curve;
- quality unlock thresholds;
- solo/cooperative Superior progression rates.

### 18.2 Cooperative crafting interaction

Define:

- session creation;
- participant roles;
- workstation/stage interactions;
- failure/interruption rules;
- material commitment;
- output ownership;
- UI flow.

### 18.3 Crafting economy

Model:

- material supply;
- Standard/Superior trade;
- Masterwork account binding;
- Legendary demand;
- sinks;
- vendor values;
- service economy;
- cross-profession dependencies.

### 18.4 Content framework

Define:

- profession-specific recipe families;
- workstation types;
- material tiers;
- rare and prestige sources;
- quarry content;
- hidden dungeon harvests;
- Legendary recipe structures.

None of these areas should be finalised piecemeal in code before the relevant product decisions are made.

---

## 19. Canonical Summary

The Ninth Age crafting system is built around universal gathering, limited specialist crafting and exact-recipe mastery.

Every character can gather and cook. Each character selects two specialist crafts. General profession level provides broad progression and access, while exact-recipe mastery determines the ability to create higher-quality versions of specific recipes.

Standard crafting is the normal solo baseline. Superior crafting encourages active cooperation while remaining possible alone at increased cost or time. Superior participation develops exact-recipe mastery. Masterwork represents individual mastery and returns the recipe to reliable solo production. Masterwork components are character-bound, while finished Masterwork equipment is account-bound so that lower-level mastery retains value for future alternate characters. Legendary crafting returns to cooperation by allowing multiple masters to contribute personally crafted BoP Masterwork components without making those components tradeable.

Gathering never fails and does not use material-purity tiers. Skill improves access, speed and common yield, but not rare-material probability. Quarries provide noncompetitive communal Mining, while hidden dungeon harvests provide time-gated high-tier gathering opportunities.

The system's central economic rule is:

> **Professions should be independently trainable, but not independently perfectible.**

The exact numbers, thresholds, cooperative interaction rules, binding rules outside the locked Masterwork decisions, and detailed economy remain intentionally unresolved and must be decided explicitly before implementation.
