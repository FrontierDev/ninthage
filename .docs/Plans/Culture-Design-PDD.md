# Product Design Document: Racial and Cultural Design

**Project:** Ninth Age  
**Document Type:** Parent Product Design Document  
**Status:** Draft / Design Baseline  
**Purpose:** Maintain the cultural design roster, cross-cultural principles and index of race-specific sub-PDDs.  
**Last updated:** 2026-11-13

---

# 1. Purpose and Authority

This document is the cultural counterpart to the [Class Design PDD](Class-Design-PDD.md), organised as a **parent PDD with one dedicated sub-PDD per culture**.

The parent owns:
- the cultural design principles shared by all Ninth Age peoples;
- the roster of cultures, links to their authoritative sub-PDDs and their design status;
- boundaries among race, faith, kingdom, region and political institutions;
- uniform expectations for art-direction specificity, production constraints and unresolved decisions.

**Individual cultural sub-PDDs** own the corresponding people's historical identity, religious traditions, government and houses, architecture, dress, heraldry, materials, asset families and explicit open design questions. Do not duplicate culture-specific canon in this index.

This document does **not** replace:
- the [Graphical Approach PDD](../Graphical-Approach-PDD.md), which controls fidelity, materials, modularity, shaders, concept-art presentation and asset production;
- the [World and Zone Design PDD](World-and-Zone-Design-PDD.md), which controls geography and settlement structure;
- the [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md), which owns character creation and appearance;
- the [Factions and Reputation PDD](Factions-and-Reputation-PDD.md), which owns faction gameplay;
- the [Class Design PDD](Class-Design-PDD.md), which controls class mechanics and specialisations.

**Decision labels used in every culture sub-PDD:**
- **Established:** explicitly approved cultural lore or information already present in game definitions.
- **Proposed:** derived or suggested art direction, not independently approved as immutable lore.
- **Further Consideration Required:** unresolved details that must not silently become canon.

---

# 2. Cultural Design Principles

Every racial culture must have a visual identity distinguishable from the others through **massing, silhouettes, materials, construction logic, dress, ornament and iconography**. A race must not be reduced to a single colour palette or a generic fantasy architectural kit.

Cultures should appear historically developed. Religious change, inherited artistic influence, political subdivision, repairs and material availability should visibly affect their settlements and objects.

Art should be believable before it is fantastical. Architecture should demonstrate construction logic; armour and weapons should read as usable equipment. Magical or inherited non-human influences should add character without erasing that foundation.

There must be a clear distinction between **race**, **kingdom or faction**, **faith** and **aristocratic house**. Their emblems, clothing and building elements may coexist, but they are not interchangeable.

All cultures use the global **grounded, mature mid-poly fantasy** target. Assets should communicate form through readable geometry at meaningful scales, with restrained surface complexity and environmental character delivered largely by materials, shader systems and controlled dressing.

---

---

# 3. Cultural Roster and Sub-PDD Index

| Culture | Sub-PDD | Status | Short identity |
|---|---|---|---|
| **Eldani (Human)** | [Eldani Culture PDD](Cultures/Eldani-Culture-PDD.md) | Established lore / provisional visual baseline | Astonian stone towns and castles, late-medieval/Renaissance design, inherited elven influence, Mahlism and hereditary noble houses |
| **Arhîn (Dawn Children)** | [Arhîn Culture PDD](Cultures/Arhin-Culture-PDD.md) | Established lore / provisional visual baseline | Ancient Aurësén descendants dependent on light; three-caste society, glazed domed towers, magical forests, Tiandre and the Withering |

This is an **index of cultures documented so far**, not an assertion that Eldani are the only playable race. Additional sub-PDDs will be created as cultural briefs are established.

---

# 4. Culture Sub-PDD Conventions

Every culture must be stored separately in:

```text
.docs/Plans/Cultures/<Culture-Name>-Culture-PDD.md
```

The parent PDD remains a short, stable index and cross-cultural policy document. Detailed information must reside in the relevant cultural sub-PDD, not as one expanding chapter in this index.

Each sub-PDD should cover, where relevant:

1. Purpose, authority, scope and design-decision status.
2. Core cultural and racial identity.
3. Historical foundation and external influences.
4. Geography, principal polities and settlements.
5. Religion, beliefs, religious architecture and symbols.
6. Government, aristocracy, institutions and social organisation.
7. Architecture, construction logic, silhouettes and settlement character.
8. Materials, colours, heraldry and ornamental motifs.
9. Clothing, armour, weaponry, objects and props.
10. Historical layering, regional variation and lived-in conditions.
11. Reusable modular asset families and graphical-production requirements.
12. Art acceptance criteria and explicit further-consideration questions.
13. Dependencies on other PDDs.

These are a consistent authoring vocabulary, not a mandate to invent unapproved lore or to create empty detailed prescriptions for an unexplored culture. Avoid imposing the Eldani's specific religious, noble or architectural institutions on other peoples.

---

# 5. Ownership and Dependencies

- The **Culture Design PDD** owns cross-cultural rules and the canonical cultural document index.
- Each **culture sub-PDD** owns its own established lore and culture-specific art direction.
- The [Graphical Approach PDD](../Graphical-Approach-PDD.md) owns game-wide presentation, fidelity, shaders and asset workflow.
- The [World and Zone Design PDD](World-and-Zone-Design-PDD.md) owns geographical and settlement placement rules.
- The [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md) owns player-facing race selection, character appearances and customisation.
- The [Factions and Reputation PDD](Factions-and-Reputation-PDD.md) owns faction mechanics, not cultural iconography.
- The [Class Design PDD](Class-Design-PDD.md) owns class mechanics, not cultural religious affiliations.
- The [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md) owns narrative delivery, using cultural history and institutions defined in the corresponding cultural documents.

Changes to a culture's symbols, history or visual language should be recorded in **that culture's sub-PDD**. Update this parent only when its index or shared principles change.
