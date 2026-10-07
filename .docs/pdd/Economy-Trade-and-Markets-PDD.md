# Ninth Age — Economy, Trade and Markets Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Gold, economic sources and sinks, NPC vendors, direct player trade, mail, the global player exchange, restricted marketplace channels, work orders, market access, transaction authority, economic telemetry and anti-exploit requirements  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended economy, trade and market model for **Ninth Age**.

It is authoritative for:

- the ordinary game currency;
- economic sources and sinks;
- NPC vendor economic behaviour;
- direct player-to-player trade;
- economic use of mail and Cash on Delivery;
- the player market/exchange;
- global versus local market scope;
- physical marketplace access;
- restricted market channels such as contraband;
- commodity and non-commodity listing behaviour;
- market listing, purchase, cancellation, fees and taxation;
- work-order market behaviour;
- cross-world economic behaviour;
- economic persistence;
- transaction authority and atomicity;
- price-history and economic telemetry requirements.

This document does not define:

- item identity, binding, durability mechanics or item transferability — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- profession progression, recipe mastery or cooperative crafting mechanics — [Crafting System PDD](Crafting-System-PDD.md);
- faction/reputation progression — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- quest reward structure — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- lodestone travel rules beyond its gold cost — [Movement and Traversal PDD](Movement-and-Traversal-PDD.md);
- group loot distribution — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md) and [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- account/character database implementation — [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- detailed chat/mail presentation — [Communication Systems PDD](Communication-Systems-PDD.md);
- final market/vendor UI styling — [UI and UX PDD](UI-and-UX-PDD.md).

Where current implementation conflicts with this document, this document defines intended product behaviour.

---

## 2. Design Pillars

### 2.1 One understandable ordinary economy

The normal economy should use one primary currency:

**Gold**

Ninth Age should not create a large collection of routine currencies for systems that can sensibly use gold, items, reputation or authored access conditions instead.

Special-purpose currencies may exist later where a system genuinely requires them, but they are exceptions rather than the default economic model.

### 2.2 Valuable goods should primarily come from players and content

The economy should connect:

- gathering;
- crafting;
- loot;
- quests;
- vendors;
- services;
- player trade.

NPC vendors provide useful goods and services, but should not routinely replace players as the primary source of high-value crafted or endgame goods.

### 2.3 Global liquidity should not erase the physical world

The ordinary player market is global across persistent worlds.

However, players must physically reach an appropriate marketplace to use it.

This preserves:

- meaningful settlements;
- market districts;
- illicit/hidden marketplaces;
- faction-controlled access;
- world discovery;

without fragmenting ordinary supply and demand into many low-liquidity economies.

### 2.4 Restricted trade should create places to discover, not isolated economies by default

Some items may require access to specific market channels such as contraband or specialist goods.

A player may need to find a hidden or restricted marketplace to access those listings.

Unless explicitly authored otherwise, the listings behind that channel still participate in the same game-wide exchange.

### 2.5 Economic friction should come from understandable costs

The economy may use:

- transaction tax;
- listing deposits;
- repair costs;
- travel costs;
- profession/service fees;
- permits;
- vendor purchases.

It should avoid hidden throttles, arbitrary currency deletion or repetitive caps whose main purpose is simply to slow players down.

### 2.6 Transactions must be trustworthy

Economic operations must be server-authoritative and atomic.

A player should never lose an item or gold because one half of a trade completed while the other half failed.

---

## 3. Existing Architectural Foundation

The current codebase contains useful but incomplete economic foundations.

### 3.1 Item trading fields

ItemDefinition currently contains:

- CanTrade;
- CanSell;
- SellPrice;
- stackability;
- binding state;
- item identity.

These remain useful inputs to economy rules, although binding and market-channel behaviour require stronger runtime enforcement than the current prototype provides.

### 3.2 Item instance identity

ItemInstance already carries a unique GUID and mutable instance state such as:

- durability;
- stack size;
- modifications.

Unique item-instance identity is required for safe trade, mail, market escrow and audit logging.

### 3.3 Quest gold rewards

QuestDefinition already supports authored gold rewards.

The economy should complete the authoritative character-currency flow rather than introduce an unrelated reward currency.

### 3.4 Current persistence gap

CharacterData currently does not persist an ordinary gold balance.

A production economy requires authoritative persisted currency state.

### 3.5 Current transfer-system gap

The current repository does not yet contain production implementations for:

- direct trade;
- mail;
- market listings;
- work-order market;
- vendor purchase/sale flow.

These systems should be implemented from this PDD rather than inferred from placeholder fields.

---

## 4. Primary Currency

The ordinary game currency is:

**Gold**

Gold is used for ordinary monetary transactions such as:

- NPC purchases;
- NPC sales;
- player market purchases;
- player market fees;
- direct player trade;
- Cash on Delivery;
- repair costs;
- lodestone travel;
- profession/service fees;
- permits where authored;
- other normal services.

### 4.1 Currency representation

The authoritative stored currency value must use an integer representation.

A character cannot have a negative gold balance.

If the UI later presents denominations below one displayed gold unit, the persistence representation must still use one exact smallest unit rather than floating-point currency.

### 4.2 Additional currencies

Additional currencies are not prohibited, but require an explicit system/content reason.

They should not be introduced simply because a new raid, faction or activity exists.

Reputation is not currency.

---

## 5. Gold Ownership

Gold belongs to the character's persistent economic state unless a future Account/Persistence PDD explicitly introduces an account-wide wallet.

The economy architecture must not depend on account-wide gold.

Characters on the same account may transfer eligible value using supported systems rather than assuming one shared balance.

---

## 6. Gold Sources

Normal gold sources may include:

- quest rewards;
- looted currency where appropriate;
- selling eligible items to NPC vendors;
- selling items through the player market;
- direct player trade;
- crafting commissions/work orders;
- authored event rewards;
- other explicit content rewards.

The system should be cautious about large amounts of raw gold from endlessly repeatable trivial enemies.

Economic value from repeatable gameplay may instead enter through tradeable materials/items where appropriate.

---

## 7. Gold Sinks

Intended recurring or situational gold sinks include:

- equipment repair;
- NPC goods;
- lodestone travel;
- profession permits;
- crafting/workstation/service fees where authored;
- market listing deposits;
- market transaction tax;
- mail/CoD fees where adopted;
- cosmetic or convenience services;
- other explicit world services.

A sink should provide a legible service or transaction cost.

The economy should not rely on arbitrary currency wipes.

---

## 8. Inflation Philosophy

Inflation should be controlled primarily through:

- balanced raw-gold generation;
- recurring useful services;
- transaction costs;
- repair;
- travel;
- market fees;
- vendor spending;
- profession/activity costs.

The preferred response to inflation is economic balancing based on telemetry rather than:

- deleting player currency;
- imposing arbitrary daily trade caps;
- reducing sale value simply because a player traded frequently;
- hidden diminishing returns.

---

## 9. NPC Vendors

NPC vendors use explicitly authored inventories.

Vendor stock may include:

- basic goods;
- consumables;
- equipment;
- crafting materials;
- recipes;
- bags;
- cosmetics;
- faction goods;
- specialist items;
- other authored content.

Vendors should not use one universal catalogue.

Settlement, culture, faction and NPC identity may all affect available stock.

---

## 10. Vendor Stock Models

A vendor entry may use one of several authored availability models.

### 10.1 Unlimited stock

Appropriate for:

- basic supplies;
- ordinary consumables;
- common service goods.

### 10.2 Restricted stock

A vendor item may require:

- reputation tier;
- faction;
- quest/narrative state;
- profession;
- permit;
- other reusable conditions.

### 10.3 Limited stock

Limited stock is supported because the Items PDD already allows it.

It should be used deliberately.

Where shared global stock would primarily encourage camping or automated purchasing, a personal purchase limit/restock rule is preferred.

Exact restock rules remain content data.

---

## 11. Vendor Purchase Prices

Vendor purchase prices derive from authored base economic values plus explicit modifiers.

Modifiers may include:

- faction/reputation benefits;
- vendor-specific rules;
- explicit content/service modifiers.

Reputation discounts must use authored tier benefits as defined by the Factions and Reputation PDD.

There is no invisible continuous formula in which every reputation point directly changes price.

---

## 12. Vendor Sell Prices

Eligible items may be sold to NPC vendors.

The existing ItemDefinition SellPrice field is a useful base value.

Vendor sale value should represent a substantial loss relative to:

- purchase cost;
- crafting cost;
- likely player-market value.

This prevents routine vendor arbitrage.

Player-market value is not mechanically derived from vendor value.

### 12.1 Non-vendorable items

Items may explicitly have no vendor sale value.

Typical examples include:

- quest items;
- key/narrative objects;
- special non-commercial items.

---

## 13. Vendor Buyback

Vendors support buyback of recently sold items.

Buyback exists to recover accidental sales, not as a storage system.

The exact:

- number of remembered items;
- retention duration;
- price rule;

remain balance/UI decisions.

Buyback state is server-authoritative.

---

## 14. Repairs

Durability and repair behaviour is defined by the Items, Equipment and Loot PDD.

Economically:

- specialist repair NPCs provide full repair services;
- NPC repair costs gold;
- repair cost scales with missing durability, item level, rarity and craftsmanship;
- repair is intended as a recurring gold sink.

Repair cost should matter without making normal dungeon learning/wipes prohibitively punitive.

Exact repair coefficients remain balance data.

---

## 15. Direct Player Trade

Direct trade is a secure two-player transaction.

A direct trade may contain:

- eligible items;
- gold.

Both players must be physically co-located in the same compatible world/instance context and within an appropriate interaction distance.

Direct trade is not a remote transfer mechanism.

### 15.1 Trade confirmation

Each participant must explicitly confirm the final proposed trade.

If either side changes:

- an item;
- item quantity;
- gold amount;

both confirmations reset.

### 15.2 Trade restrictions

The trade service must enforce:

- item binding;
- CanTrade;
- item/category restrictions;
- inventory capacity;
- item ownership;
- unique-item restrictions where relevant;
- available gold.

Bound/non-tradeable items cannot bypass restrictions through trade.

### 15.3 Atomic completion

Final exchange is atomic.

Either:

- all authorised items/gold transfer successfully; or
- nothing transfers.

---

## 16. Cross-World Direct Trade

Cross-world group membership does not create cross-world direct trade.

Players must first become physically co-located in a compatible world/instance before using direct trade.

This prevents the social cross-world group layer from becoming unrestricted remote item transfer.

---

## 17. Mail

Mail supports asynchronous item and currency transfer.

Mail may contain:

- text;
- multiple eligible item attachments;
- gold where appropriate;
- Cash on Delivery.

Binding and item-category restrictions remain authoritative.

Quest items cannot be mailed.

### 17.1 Mail expiry

Mail expires after a fixed configurable duration.

Expired ordinary mail should normally return eligible attachments/gold to the sender where a valid sender exists.

Exact expiry duration remains a balance/communication-system value.

System-generated mail may define its own safe expiry behaviour.

### 17.2 Cash on Delivery

CoD allows a sender to require a specified gold payment before the recipient claims the attached goods.

The transfer must be atomic:

- recipient pays the required gold;
- recipient receives the attached goods;
- sender becomes entitled to the payment.

If the recipient cannot pay, the attachments remain unclaimed.

---

## 18. Player Market Overview

Ninth Age uses a server-authoritative **player exchange market**.

The ordinary exchange is:

- buyout-oriented;
- globally pooled across persistent worlds;
- accessed physically through marketplace locations;
- organised by market channels.

The system is not primarily a traditional timed bidding auction house.

---

## 19. Global Exchange Scope

The default market listing pool is shared across:

**all persistent worlds**

A player listing an ordinary item through a normal marketplace contributes to the same global exchange as a player listing from another persistent world.

A buyer may purchase that listing from any compatible marketplace access point that exposes the relevant market channel.

This creates one game-wide ordinary player economy rather than one isolated economy per persistent world.

---

## 20. Physical Marketplace Access

Global market scope does not mean remote access from anywhere.

To:

- browse listings;
- buy;
- create listings;
- cancel listings;
- manage work orders where relevant;

the player must normally be physically present at an appropriate marketplace or service access point.

Major settlements may provide ordinary exchange access.

Remote settlements may provide:

- limited channel access;
- no market;
- specialist/restricted access.

The world should retain meaningful economic geography.

---

## 21. Market Channels

The global exchange is divided into authored **Market Channels**.

A marketplace exposes one or more channels.

Conceptually:

```text
Global Exchange
├── General
├── Contraband
├── Specialist Arcana
├── Faction Restricted
└── other authored channels
```

The exact channel catalogue is content data.

### 21.1 General channel

Most ordinary tradeable goods use the **General** channel.

Normal major-city markets should expose General unless content intentionally says otherwise.

### 21.2 Restricted channels

A restricted channel may represent:

- contraband;
- black-market goods;
- faction-controlled trade;
- culturally restricted items;
- specialist magical goods;
- profession-specialist goods;
- other authored categories.

A player may need to discover or reach an appropriate marketplace to access such a channel.

---

## 22. Contraband Markets

Contraband is the canonical example of restricted market access.

A contraband item:

- does not appear through ordinary General-only market access;
- may only be listed/bought through a marketplace exposing the relevant Contraband channel;
- may require additional authored access conditions.

A hidden market in one location and another hidden market elsewhere may both expose the same Contraband channel.

Unless the channel is explicitly local/world-scoped, both access points expose the same **global contraband listing pool**.

This lets discovering illicit marketplaces matter without fragmenting liquidity.

---

## 23. Marketplace Access Conditions

Marketplace or channel access may use reusable authored conditions such as:

- reputation;
- faction;
- quest/narrative state;
- profession;
- permit;
- discovery;
- location/context;
- other future ConditionDefinition-compatible requirements.

The market system should not hard-code special logic for every restricted marketplace.

---

## 24. Exceptional Local and World-Scoped Markets

The normal rule is:

**global listing pool + physically restricted access**

However, the system may author exceptional market channels whose listing scope is deliberately limited to:

- one persistent world;
- one region;
- another explicit economic scope.

These exceptions should be uncommon and exist for a clear gameplay/world reason.

A physically regional marketplace does **not** automatically imply a regional listing pool.

Scope must be explicit.

---

## 25. Item Market Eligibility

An item may only be listed if it is eligible for player trade.

Market eligibility must respect at least:

- binding;
- CanTrade;
- item/category restrictions;
- market-channel rules;
- ownership;
- current escrow state;
- other explicit conditions.

Bound items cannot be listed.

Quest items cannot be listed.

Items already committed to:

- another listing;
- mail;
- direct trade;
- work-order escrow;
- another authoritative transaction;

cannot be duplicated into the market.

---

## 26. Item Market Channels

An item definition or related economic metadata should be able to specify its permitted market channels.

Examples:

- ordinary ore → General;
- illicit poison → Contraband;
- specialist magical component → Specialist Arcana;
- faction-controlled permit token → potentially non-tradeable or a restricted channel.

Most items should not require bespoke market logic.

---

## 27. Buyout-Only Ordinary Market

The normal player exchange uses **fixed-price buyout listings**.

There is no general requirement for player bidding/auction countdown gameplay.

The seller chooses:

- eligible item;
- quantity where stackable;
- price.

The buyer accepts the listed price.

This reduces:

- auction sniping;
- artificial waiting;
- commodity friction.

A future specialised true-auction system may be added for exceptional content without changing the ordinary exchange.

---

## 28. Commodity Listings

Fungible stackable goods such as common crafting materials should use commodity-style market behaviour.

Commodity listings expose:

- item identity;
- available quantity;
- unit price.

Buyers may purchase partial quantities.

The system should prioritise the lowest compatible unit-price listings when fulfilling a commodity purchase.

### 28.1 Commodity aggregation

The buyer should not need to browse pages of identical stacks.

The UI may aggregate supply by item/unit price while the server retains the underlying individual seller/listing records.

### 28.2 Partial fills

A commodity listing may be partially filled.

Remaining quantity stays listed until:

- fully purchased;
- cancelled;
- expired.

---

## 29. Non-Commodity Listings

Non-stackable equipment and other instance-specific goods remain individual listings.

The listing must preserve item-instance state such as:

- item identity;
- GUID;
- durability;
- installed modifications/enchantments/gems where applicable;
- binding/eligibility state;
- craftsmanship/provenance where applicable.

The market must not recreate a listed equipment item from its definition and discard instance state.

---

## 30. Market Search and Filters

The market should support practical filtering.

For equipment, useful filters include:

- item type;
- equipment slot;
- required level;
- item level;
- rarity;
- armour/weapon category;
- relevant stat properties;
- crafting/craftsmanship properties where appropriate;
- market channel.

For commodities, the primary buying experience should emphasise:

- item;
- total available quantity;
- unit price.

Exact UI belongs to the UI/UX PDD.

---

## 31. Listing Duration

Listings expire after a configurable duration.

Exact duration may be global or channel-specific.

Expired unsold items return safely to the seller through mail or another guaranteed market-return mechanism.

Listing expiry must never silently destroy a valid item because the player was offline.

---

## 32. Listing Deposit

Creating a listing requires a small **listing deposit** in gold.

The deposit exists primarily to:

- discourage listing spam;
- create modest economic friction.

The normal policy is:

- successful sale → deposit returned;
- natural expiry → deposit may be lost;
- seller cancellation → deposit is lost.

Exact deposit formula remains balance data.

---

## 33. Market Transaction Tax

Successful player-market sales pay a transaction tax/commission.

Initial balancing target:

**approximately 5% of sale value**

This percentage is data-driven and may be adjusted through economic telemetry.

The tax is removed from the economy.

Reputation discounts do **not** automatically reduce global exchange tax.

---

## 34. Listing Cancellation

A seller may cancel an active listing where no conflicting transaction lock exists.

Cancellation:

- stops future purchases;
- returns remaining item/quantity safely;
- does not reverse completed partial sales;
- forfeits the listing deposit.

An item cannot be cancelled out from under an already-committing atomic purchase.

---

## 35. Market Purchase

Market purchase is server-authoritative.

The service must validate:

- listing still exists;
- requested quantity remains available;
- buyer can access the relevant channel;
- buyer has sufficient gold;
- buyer is eligible to receive the item;
- transaction does not violate unique/binding rules.

Completion atomically:

- consumes/updates the listing;
- deducts buyer gold;
- transfers item entitlement;
- records seller proceeds minus tax;
- records the transaction.

Exact item/proceeds delivery presentation may use inventory, market collection or mail according to final UX, but economic ownership must be committed exactly once.

---

## 36. Market Settlement and Offline Sellers

The seller does not need to be online for a listing to sell.

Sale proceeds must remain safely claimable/credited through persistent economic state.

The implementation must not require seller presence on the same world or server process as the buyer.

---

## 37. Market Geography and Persistent Worlds

The ordinary market economy is game-wide.

Persistent-world boundaries do not partition:

- General supply;
- General demand;
- global restricted-channel supply.

This is deliberately different from:

- cross-world direct trade;
- world-local event participation;
- world-local NPC state.

Economic listing scope is an explicit service rule, not a consequence of player location.

---

## 38. Player Market and Reputation

Faction reputation may affect:

- whether a marketplace/vendor can be accessed;
- whether a restricted channel is exposed;
- NPC vendor prices;
- NPC vendor stock;
- permits/services.

Faction reputation does not ordinarily alter:

- another player's listing price;
- global exchange tax;
- commodity matching priority.

If a faction-owned marketplace needs a special authored fee, it should be explicit rather than inferred from raw reputation.

---

## 39. Work Orders

Work orders support commissioned crafting.

They are an economic service distinct from selling a finished item.

The commissioning player specifies:

- desired authored recipe/result;
- required supplied materials;
- offered commission/payment;
- visibility: public or private.

Materials required from the customer are committed up front into authoritative escrow.

---

## 40. Public Work Orders

Public work orders are browsable by eligible crafters through appropriate work-order market access.

The system should filter or clearly distinguish orders that the current character is qualified to complete.

Eligibility may depend on:

- profession;
- profession level;
- known recipe;
- recipe mastery/quality requirements;
- other crafting conditions.

The exact sorting/search UI remains a UX decision.

---

## 41. Private Work Orders

A private work order is addressed to a specific eligible crafter.

It is not visible as an open public commission.

Private orders use the same escrow and authoritative completion rules as public orders.

---

## 42. Work-Order Escrow

Customer-supplied materials are removed/reserved into server-controlled escrow when the order is created.

They are not left in the customer's ordinary inventory while simultaneously promised to the order.

The system must prevent:

- double spending;
- trading escrowed materials;
- market listing escrowed materials;
- consuming escrowed materials in another craft.

If the order expires/cancels without valid completion, refundable materials are returned safely according to authored rules.

---

## 43. Work-Order Completion

An eligible crafter accepts/performs the commissioned craft according to the Crafting PDD.

On successful completion:

- escrowed materials are consumed as required;
- commission/payment is transferred according to the order;
- finished item is delivered to the customer by mail;
- normal binding/craftsmanship rules apply;
- transaction/audit history is recorded.

The crafter does not need to take temporary ownership of customer-bound output if doing so would violate its binding rules.

---

## 44. Work-Order Market Scope

Public work orders may participate in the global economy similarly to ordinary exchange listings unless their content/channel explicitly restricts scope.

Private work orders target one named crafter regardless of ordinary public-listing visibility, subject to reachability/eligibility rules.

Exact marketplace access required to post/accept work orders may vary by profession/content.

---

## 45. Crafted Goods and the Economy

Most ordinary crafted output should be capable of participating in the economy when its binding rules allow it.

Economic participation may include:

- finished Standard goods;
- eligible Superior goods;
- ordinary components;
- consumables;
- bags;
- gems/enchantments;
- services/work orders;
- raw and processed materials.

The economy does not override the Crafting PDD's Masterwork/Legendary binding rules.

---

## 46. Masterwork and Bound Crafting Output

Finished Masterwork equipment remains Bind to Account as defined by the Crafting PDD.

Masterwork components remain Bind on Pickup to their creator.

Therefore they do not become ordinary market commodities.

The market/work-order system must not provide a loophole around those restrictions.

Legendary cooperative contribution uses the Crafting PDD's direct contribution model rather than ordinary market transfer of bound components.

---

## 47. Gathering Materials

Ordinary gathering materials should generally be tradeable unless explicitly restricted.

Communal gathering systems such as quarries may grant personal material rewards while contributing to shared activity progress.

The market therefore provides a major mechanism through which gatherers supply crafters.

Profession levelling must nevertheless not assume a perfectly liquid market at every level.

---

## 48. Material Sinks

Material demand should come from actual production and progression.

Useful sinks include:

- consumables;
- equipment crafting;
- profession progression;
- recipe mastery where later authored;
- high-tier recipes;
- components;
- other crafting activities.

Higher-tier recipes may intentionally continue consuming selected lower-tier materials to maintain relevance.

Stored materials do not decay merely to force economic turnover.

---

## 49. No General Salvage Economy

The Items PDD defines:

- no general salvaging system;
- no disenchant-to-material loop for unwanted equipment.

Vendoring unwanted items remains relevant.

The economy must not assume that old equipment automatically returns materials to the market.

---

## 50. Economic Value of Loot

Dungeon, raid and world-boss loot may enter the economy only where its item binding allows it.

The economy system does not loosen binding rules to increase market supply.

Item rarity/source and binding determine whether high-end rewards become:

- market goods;
- direct character progression;
- account progression.

This preserves the distinction between economic and non-economic rewards.

---

## 51. Market Price Discovery

Player-market prices are set by player listings and purchases.

The system should not impose routine central price controls.

Vendor SellPrice is not a market price floor.

Vendor purchase price is not a market price ceiling.

Price limits may exist only for technical validation or explicit content reasons.

---

## 52. Price History

The server must record market transaction history sufficient for:

- economic telemetry;
- inflation monitoring;
- exploit investigation;
- future price-history UI.

Useful recorded values include:

- item/channel;
- quantity;
- unit/total price;
- timestamp;
- listing/sale scope;
- anonymised/internal seller and buyer references as appropriate for auditing.

Player-facing historical charts are optional initially.

The underlying data should exist from the start.

---

## 53. Economy Telemetry

At minimum, economy monitoring should be able to report:

- total gold created;
- total gold destroyed;
- gold created/destroyed by source/sink;
- gold held by active characters;
- distribution/median balances;
- market transaction volume;
- median/percentile prices for major commodities;
- listing volume;
- expired/cancelled listing rates;
- repair/travel/market-tax sink volume;
- work-order commission volume.

This allows balance changes to target actual economic problems.

---

## 54. Anti-Exploit and Transaction Logging

Economically significant operations should produce server-side audit records.

This includes, as appropriate:

- direct trades;
- mail attachments/gold;
- CoD;
- market listings;
- market purchases;
- cancellations;
- vendor sales/purchases;
- work-order escrow;
- work-order completion;
- unusually large gold transfers.

Logging supports:

- duplicate-item investigation;
- exploit detection;
- rollback/recovery analysis;
- economy balancing.

---

## 55. Atomicity and Escrow

Any transaction that moves value between owners/systems must be atomic.

Examples:

- direct trade;
- CoD claim;
- market purchase;
- market listing escrow;
- work-order creation;
- work-order completion.

The system must never:

1. delete/consume the sender's value;
2. fail before guaranteeing the corresponding receiver/system state;
3. leave the transaction half-complete.

Items committed to escrow are not simultaneously available in ordinary inventory.

---

## 56. Concurrency

Market listings may be targeted by multiple buyers.

The server must ensure that:

- the same unique item cannot sell twice;
- commodity quantity cannot be oversold;
- gold is deducted only for successfully committed quantity;
- a cancellation cannot race a completed sale into item duplication.

Transaction locking/implementation belongs to technical architecture, but these outcomes are product requirements.

---

## 57. Persistence

Economic state requiring persistence includes:

- character gold;
- market listings;
- listing escrow;
- market proceeds/settlement;
- mail and attachments;
- CoD state;
- work orders;
- work-order escrow;
- vendor limited/personal stock state where applicable;
- price-history/audit records as required.

Market state must survive:

- player logout;
- disconnect;
- persistent-world transfer;
- server process restart.

The global market cannot depend on one world server process remaining alive.

---

## 58. Cross-World Economy

The global exchange is explicitly cross-world.

The following are **not** automatically cross-world simply because the exchange is global:

- direct trade;
- physical vendor interaction;
- repair interaction;
- marketplace access;
- quest/event participation.

Players still interact physically with the world while the exchange provides game-wide liquidity.

---

## 59. Market Access and World Design

Marketplace placement should reinforce settlement identity.

Potential market access points include:

- major city exchanges;
- merchant districts;
- faction bazaars;
- hidden smuggler dens;
- specialist arcane markets;
- profession halls;
- temporary/event-specific markets where authored.

A hidden marketplace can matter because it exposes a channel the player cannot otherwise access, even though the listings themselves are global.

---

## 60. User Experience Requirements

The market UI must make clear:

- which marketplace/channel the player is accessing;
- whether listings are global or exceptionally local/world-scoped;
- unit price;
- quantity;
- total purchase cost;
- listing deposit;
- sale tax;
- listing duration;
- item instance details for non-commodities;
- why an item cannot be listed;
- why a channel is unavailable;
- whether a listing/work order is public/private;
- work-order supplied materials;
- work-order commission;
- binding consequence of commissioned output.

Players should not discover transaction fees or binding consequences only after committing valuable goods.

---

## 61. Content Authoring Requirements

Designers need data-driven control over:

- vendor inventories;
- vendor conditions;
- vendor prices/base values;
- limited/personal stock rules;
- item market channels;
- marketplace channel exposure;
- marketplace access conditions;
- market-channel scope;
- exceptional regional/world-local channels;
- work-order access;
- authored fees/services where relevant;
- vendor reputation benefits;
- restricted/contraband item categorisation.

The system should avoid marketplace-specific hard-coded scripts where data can express the rule.

---

## 62. Current Implementation Changes Required

The current economy implementation is incomplete.

Required work includes:

- persisted character gold;
- authoritative gold mutation service;
- vendor buy/sell transactions;
- buyback;
- specialist repair payment flow;
- direct trade;
- mail storage and attachments;
- CoD;
- global market service;
- market-channel definitions;
- marketplace/channel access definitions;
- market listing escrow;
- commodity matching and partial fills;
- individual equipment listings preserving ItemInstance state;
- listing deposits/tax;
- work-order public/private posting;
- work-order escrow;
- transaction/audit history;
- price/economy telemetry.

### 62.1 ItemDefinition integration

Existing:

- CanTrade;
- CanSell;
- SellPrice;

should be retained where useful.

Additional economic metadata should be introduced cleanly rather than overloading unrelated fields.

### 62.2 Binding enforcement

Existing ItemDefinition currently exposes trading fields separately from binding.

Runtime economic services must evaluate the authoritative binding state and cannot assume CanTrade alone makes an item transferable.

---

## 63. Intentionally Deferred Decisions

The following remain balance or dependent-system decisions rather than unresolved architecture:

- exact market listing duration;
- exact listing-deposit formula;
- final market sale-tax percentage around the initial ~5% target;
- exact vendor buy/sell coefficients;
- exact faction vendor-discount values;
- exact repair-cost coefficients;
- exact mail expiry duration;
- whether ordinary mail has a gold postage fee and its value;
- exact buyback history size/duration;
- exact material sinks for individual professions;
- exact public work-order expiration duration;
- exact work-order acceptance/reservation timing;
- exact catalogue of market channels;
- exact contraband items;
- exact locations/conditions of restricted marketplaces;
- which exceptional channels, if any, are truly region/world-scoped;
- exact player-facing price-history UI.

These values can be authored/tuned without changing the architecture defined here.

---

## 64. Locked Design Decisions

The following are locked by this PDD:

1. Gold is the primary ordinary game currency.
2. The game should avoid routine proliferation of special currencies.
3. Reputation is not currency.
4. Authoritative currency uses an integer representation and cannot become negative.
5. Gold is character economic state unless a future persistence design explicitly introduces an account wallet.
6. Quests, vendoring, loot and player economic activity may generate gold.
7. Repairs, travel, vendors, permits/services and market costs provide gold sinks.
8. The economy should not rely on arbitrary currency wipes or generic daily trade caps.
9. Vendor inventories are explicitly authored and may vary by settlement/faction/culture.
10. Vendors may use unlimited, restricted or deliberately limited stock.
11. Reputation vendor discounts are authored tier benefits rather than continuous raw-reputation formulas.
12. Vendor resale represents a substantial value loss and must not enable trivial arbitrage.
13. Vendor buyback is supported.
14. Specialist NPC repair is an intended recurring gold sink.
15. Direct player trade supports eligible items and gold.
16. Direct trade requires physical co-location in a compatible world/instance.
17. Trade changes reset both parties' confirmations.
18. Direct trade completes atomically.
19. Binding and CanTrade restrictions cannot be bypassed by direct trade.
20. Mail supports multiple attachments, gold and Cash on Delivery.
21. Quest items cannot be mailed.
22. Expired mail returns eligible value safely where a sender exists.
23. Ordinary market trading is fixed-price/buyout based rather than a general bidding auction.
24. The default player market listing pool is global across all persistent worlds.
25. Players must normally physically reach an appropriate marketplace to access the exchange.
26. Market access is organised into authored channels.
27. General is the normal channel for ordinary goods.
28. Restricted channels such as Contraband are supported.
29. A physically restricted marketplace may expose a global channel.
30. Contraband access therefore creates meaningful hidden marketplace locations without requiring fragmented liquidity.
31. Marketplace/channel access may use reusable authored conditions.
32. Exceptional region/world-scoped market channels may exist only when explicitly authored.
33. Physical region does not implicitly determine listing scope.
34. Bound/non-tradeable/quest items cannot be listed.
35. Item/category economic metadata determines allowed market channel(s).
36. Stackable fungible goods use commodity-style unit-price listings with partial purchase.
37. Commodity purchases prioritise cheapest compatible unit-price supply.
38. Non-stackable/instance-specific goods remain individual listings preserving ItemInstance state.
39. Listings expire after a configurable duration and unsold goods return safely.
40. Listings require a small gold deposit.
41. Seller cancellation forfeits the deposit.
42. Successful market sales pay a data-driven transaction tax with an initial balancing target around 5%.
43. Reputation does not automatically reduce global exchange tax.
44. Market purchase is server-authoritative and atomic.
45. Sellers may sell while offline.
46. Public and private crafting work orders are supported.
47. Work-order customer materials are supplied up front into authoritative escrow.
48. Public work orders are browsable by eligible crafters.
49. Private work orders target a specified crafter.
50. Successful commissioned output returns to the customer by mail.
51. Work orders do not bypass item binding/craftsmanship rules.
52. Ordinary crafted/gathered goods participate in the economy when their binding permits it.
53. Masterwork components and finished Masterwork equipment remain outside ordinary open-market trade according to the Crafting PDD.
54. Stored materials do not decay merely to create artificial turnover.
55. There is no general salvage/disenchant material economy.
56. Player prices are discovered through listings/purchases rather than centrally imposed market prices.
57. Market transaction history and economic telemetry must be collected.
58. Significant economic transfers should be auditable server-side.
59. Economic transactions and escrow operations must be atomic.
60. The same item/commodity quantity cannot be sold twice under concurrency.
61. Global market state persists independently from any one persistent-world server process.
62. Cross-world exchange does not imply cross-world direct trade or remote physical-service interaction.
63. Economic market access should reinforce world locations while retaining global liquidity.

---

## 65. Dependencies

This PDD depends on or constrains:

- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Crafting System PDD](Crafting-System-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- [Movement and Traversal PDD](Movement-and-Traversal-PDD.md);
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [Communication Systems PDD](Communication-Systems-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md).

---

## 66. Validation Criteria

The Economy, Trade and Markets system satisfies this PDD when:

1. A character has a persisted non-negative authoritative gold balance.
2. Quest gold rewards can increase that balance server-side.
3. NPC purchases deduct gold exactly once.
4. Eligible vendor sales grant gold exactly once.
5. Vendor buyback can restore a recently sold item according to authored rules.
6. Specialist repair can deduct an authoritative repair cost and restore durability.
7. Two co-located players can trade eligible items and gold securely.
8. Modifying either side of a trade clears prior confirmations.
9. A failed trade leaves both sides unchanged.
10. Bound/non-tradeable items cannot be transferred through trade.
11. Mail can persist multiple eligible attachments and gold.
12. CoD cannot release attachments unless payment succeeds atomically.
13. Quest/bound restrictions cannot be bypassed through mail.
14. Ordinary market listings from different persistent worlds appear in the same global General exchange.
15. A player cannot access the market without an appropriate physical marketplace/service.
16. A General-only marketplace does not expose Contraband listings.
17. Two different Contraband marketplaces can expose the same global Contraband listings.
18. A channel can explicitly be authored as region/world-scoped when required.
19. An item's physical listing location does not implicitly change its listing scope.
20. Marketplace access can be gated by reusable conditions such as reputation/quest/profession.
21. Commodity listings can be partially purchased.
22. Commodity purchases consume the cheapest compatible unit-price listings correctly.
23. Non-stackable equipment preserves GUID/durability/modification state through listing and sale.
24. Listing an item removes it from ordinary usable inventory through escrow.
25. Expired/cancelled listings cannot duplicate or destroy the listed item.
26. Listing deposits and sale taxes are deducted/returned according to the authored policy.
27. A market sale can complete while the seller is offline.
28. Two simultaneous buyers cannot both purchase the same unique item.
29. Commodity stock cannot be oversold during concurrent purchases.
30. Public work orders can be discovered by eligible crafters.
31. Private work orders are visible/available only to their intended crafter/context.
32. Work-order supplied materials cannot simultaneously remain spendable in ordinary inventory.
33. Work-order completion consumes escrow and pays/delivers exactly once.
34. Commissioned output returns to the customer by mail.
35. Work orders cannot trade away bound Masterwork components through ordinary inventory transfer.
36. Reputation can unlock vendor/market access without changing another player's listing price.
37. Direct trade remains local even when the two players are cross-world group members.
38. Global market operation continues independently of any one persistent-world process.
39. Market transaction history records enough information for audit and price telemetry.
40. The system can measure gold generation and destruction by source/sink.
41. Economic operations produce no half-completed value transfers after failure.
