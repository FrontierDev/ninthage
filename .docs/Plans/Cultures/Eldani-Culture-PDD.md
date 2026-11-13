# Ninth Age — Eldani Culture Product Design Document

**Project:** Ninth Age  
**Document Type:** Cultural Sub-PDD  
**Status:** Draft / Design Baseline  
**Scope:** Eldani human culture, Kingdom of Astonia, Astonian Mahlism, the aristocratic houses, visual identity and cultural asset direction  
**Last updated:** 2026-11-13

---

# 1. Purpose and Authority

This is the **Eldani** cultural sub-PDD, governed by the [Culture Design PDD](../Culture-Design-PDD.md), which defines the shared standards and roster for all Ninth Age cultures. This document is the canonical location for Eldani-specific lore, aesthetic decisions, provisional concept direction and outstanding questions.

Decision labels follow the parent document:
- **Established:** user-approved lore or information in existing game definitions.
- **Proposed:** suggested artistic treatment, not fixed canon.
- **Further Consideration Required:** unresolved design points.

The [Graphical Approach PDD](../../Graphical-Approach-PDD.md) continues to control common fidelity, asset authoring, materials, shaders and production workflow. This document does not define character-creation rules, faction gameplay, new mechanics or final geographical layouts.

---

# 2. Eldani (Human)

## 2.1 Core Identity — Established

The Eldani are human. Their principal kingdom is **Astonia**, a historically powerful kingdom whose present strength is challenged. Existing game data describes Astonia as once the most influential kingdom in Ninth Age and identifies its ruler as the young **King Achov III**.

Their appearance and society have been shaped by contact with other cultures. The current race definition specifically describes the Eldani as having an affinity for magic and as culturally influenced by other peoples.

**Visual premise:** a sophisticated, old human civilisation that expresses political power through castles, fortified stone towns, heraldic households and monumental religious construction. Its buildings should suggest lasting institutions, changing fashions and accumulated history rather than a newly founded fantasy city.

## 2.2 Historical Foundation — Established

- The Eldani descend from **proto-Eldani** peoples.
- Approximately **1,000 years ago**, a godlike figure known as **Mahlfach, the Masked Saint**, saved the proto-Eldani from the **Merrshaulk**, the Serpent-men.
- Eldani society also experienced significant **elven cultural influence around 1,000 years ago**.
- The proto-Eldani had practised a **pantheonic religion** in which **Lythaer**, the god of light and renewal, held a central place.
- From that earlier religious tradition, **Astonian Mahlism** developed and became overwhelmingly dominant in Eldani society.

These events provide historical context for the culture; this PDD does not yet specify the chronology or exact causal relationship between elven contact, the deliverance from the Merrshaulk and the rise of Mahlism.

## 2.3 Visual Archetype — Established / Proposed

**Established:** Eldani architecture and culture most closely resemble **late-medieval to Renaissance Europe**. The Eldani build castles and stone towns. Their visual culture retains an elven inheritance from a millennium earlier.

**Proposed historical-art references:**
- French late-medieval castle and military engineering;
- Central European medieval stone towns and timber-and-plaster upper floors;
- Gothic civic and ecclesiastical architecture;
- selective early-Renaissance symmetry, courtyards and refinement.

The aim is neither a literal reconstruction nor an eclectic collection of recognisable historical buildings. Eldani work must feel part of one coherent, independently developed civilisation.

## 2.4 Architecture and Settlement Language — Proposed

| Asset family | Direction |
|---|---|
| Fortifications | Heavy curtain walls, substantial towers, fortified keeps, gatehouses, practical parapets, machicolations and defensible entrances |
| Urban streets | Dense stone streets, cobbled routes, irregular frontages, arcades, stairways, markets and varied roof heights |
| Common houses | Substantial stone ground floors, timber or plaster upper construction where appropriate, steep roofs in slate or clay tile |
| Civic buildings | Large stone halls, vaulted interiors, tall windows, articulated portals and strong civic silhouettes |
| Aristocratic residences | Fortified ancestral keeps and manor houses; later palatial wings, enclosed courtyards and refined gardens |
| Bridges and infrastructure | Heavy masonry arches, fortified crossings, retaining walls, gates and buildings adapted to slopes |
| Military works | Structural masonry, wooden rampart walkways, iron-bound doors, projecting supports and legible defensive construction |

**Massing:** weight and permanence at ground level; vertical, articulated elevations; layered rooflines and towers. The settlement silhouette should read clearly at MMO camera distance.

Avoid uniformly pristine masonry. Building age may be conveyed through changes in stone source, rebuilt parapets, extended wings, different construction periods and materially plausible wear.

### Elven inheritance — Proposed visual expression

The Eldani should not look like elves who happen to use stone. Elven influence is historical, adapted and partly assimilated.

Possible surviving elements include:
- more graceful window proportions and tall, narrow openings;
- restrained pointed arches and refined ribbed vaults;
- carefully proportioned stone galleries and ornamental tracery;
- geometric courtyards, gardens and fountains;
- eightfold or octagonal ornamental compositions where appropriate.

**Constraint:** the building's main construction and silhouette remain recognisably human and practical. Elven influence is more evident in select older, scholarly, religious or prestigious structures than in every common dwelling.

## 2.5 Materials and Everyday Palette — Proposed

| Material / use | Suggested colour / finish |
|---|---|
| Weathered structural stone | Muted grey-beige; reference **#99938A** |
| Lighter limestone / civic trim | Warm chalky beige; reference **#B8AA91** |
| Slate roofs | Desaturated blue-charcoal; reference **#454B56** |
| Dark timber and oak | Brown-grey; reference **#665044** |
| Clay and ceramic roof tile | Muted terracotta; reference **#A16B50** |

These are **starting visual swatches**, not hardcoded shader values or final material approvals. Actual colour and roughness must be approved in the material-library workflow defined by the Graphical Approach PDD.

Use obvious stone and timber construction; preserve controlled texture frequency. Dirt, dampness, soot, moss, corrosion and surface ageing should usually be handled by shared environmental material and shader systems, not unique complex geometry for every wall.

## 2.6 Kingdom of Astonia and the Black Peaks — Established / Proposed

**Established:**
- Astonia is the principal Eldani kingdom.
- Its principal city lies in the **foothills of the Black Peaks**.
- The kingdom is ruled by **King Achov III**.
- It was once exceptionally influential and now faces threats on multiple sides.

**Proposed city composition:** a substantial walled stone city ascending from lower districts toward a dominant, well-defended royal citadel. Terrain terraces, monumental stairways, retaining walls, fortified mountain roads and uneven historical districts would connect the city's identity to its foothill geography.

The royal precinct and prestigious public buildings may preserve more conspicuous elven-derived ornament. Common districts should feel denser and more pragmatic. Older prestige buildings, reinforced defences, and repairs can communicate the contrast between the kingdom's former grandeur and current pressure.

**Not yet determined:** city name, exact location or plan, size, number of walls, precise road network, relationships of major institutions and architectural chronology. Do not treat the proposed citadel layout as approved geography.

## 2.7 Royal Heraldry — Established / Proposed

**Established royal banner:** Astonian heraldry is **quartered red and blue**, with a central **eight-pointed sun**.

The royal mark is distinct from the symbols of Mahlism and of hereditary houses.

**Proposed production direction:**
- deep crimson and muted royal blue rather than high-saturation primary colours;
- a simple eight-point sun silhouette that remains legible at small sizes;
- antique gold as an initial rendering option for the sun, **not yet a canon colour**;
- consistent reproduction across flags, royal shields, seals, civic insignia, military surcoats and selected architecture.

The exact geometry, orientation, gold/metal treatment and permitted variants of the royal standard remain to be approved. The sun's relationship to the older cult of Lythaer is not established; do not assume that the royal emblem is a direct religious survival.

## 2.8 Religion — Astonian Mahlism — Established

**Astonian Mahlism** is the nearly universal Eldani religion and is **monotheistic**.

Its worship centres on **Mahlfach, the Masked Saint**. Within the religion, Mahlfach is understood as:
- the bringer of divine power;
- guardian of the Eldani people;
- the miraculous saviour of the proto-Eldani from the Merrshaulk approximately 1,000 years ago.

Mahlism developed out of the earlier proto-Eldani pantheonic tradition, notably the worship of **Lythaer**, god of light and renewal. Lythaer belongs to that earlier pantheon; he should **not** be depicted as a second active deity of Mahlism without a separately approved theological decision.

These are statements about **Eldani historical tradition and belief**, not final metaphysical rules for how divinity functions in the setting.

### Religious visual identity — Proposed

**Primary subject:** the **Masked Saint**. The mask and masked figure are the strongest prospective motifs for:
- statuary, reliefs, murals and reliquaries;
- cathedral and sanctuary portals;
- devotional objects, religious vestments and ritual props;
- commemorative monuments to deliverance from the Merrshaulk.

**Architecture:** monumental stone cathedrals, sanctuaries, monasteries, fortified abbeys and religious civic buildings, with Gothic-derived massing, tall openings, stone vaults and strong verticality. The oldest buildings may preserve earlier geometry or elven-influenced workmanship.

**Inherited motifs:** light, radiating forms, renewal imagery and older solar design may appear in historical layers, reused ornament or pre-Mahlite ruins. Their continued theological status is unresolved.

**Proposed institutional palette:** ivory (**#E5D9BA**), old gold (**#B49659**), deep crimson (**#6F172A**), charcoal (**#424858**) and stone grey (**#A7A39A**). This is an art-direction starting point only.

**Important distinction:** the **mask** is the proposed emblem of the Mahlite faith; the **eight-pointed sun** is established royal heraldry; historical **Lythaer** imagery is a separate older visual tradition. Their relationship should not be merged by assumption.

## 2.9 Aristocracy and Political Structure — Established

Astonian aristocracy is **devolved into hereditary houses**. These houses **inherit land across the kingdom** and are loyal to **King Achov III**.

This structure permits local identities while maintaining a recognisable Astonian national identity.

**Established hierarchy of identity:**
- **The Crown** represents the kingdom and the royal standard.
- **The Houses** are hereditary landholding aristocratic powers beneath the king.
- **Mahlite institutions** belong to the dominant faith and have their own religious identity.

The precise constitutional powers of the houses, their titles, number, names, rivalries and internal ranking are **not yet established**.

### Aristocratic art direction — Proposed

- Regional noble seats: castles, fortified manor houses, ancestral keeps, estates and house chapels.
- Urban houses: town residences with courtyard spaces, more expensive stonework, later Renaissance-inspired additions and ornamental interiors.
- Family identity: distinct coats of arms, colours, liveries, shields, seals and military standards.
- Military expression: household troops may use house-specific colours and insignia while still appearing part of the wider Eldani equipment family.

**Constraint:** noble houses vary through heraldry, dress, building detail and resources, not wholly disconnected racial architectural styles. House heraldry must not be confused with the red/blue quartering of the Crown.

## 2.10 Civilian Dress and Equipment — Proposed

**Clothing:** layered tunics, fitted doublets, linen, wool, cloaks, belts, gowns and period-inspired hats. Tailoring, fabric quality, embroidery and jewellery communicate rank and profession.

**Arms and armour:** plausible late-medieval steel plate, mail, brigandine, gambesons, sallets and kettle helmets; swords, halberds, crossbows and other practical period-derived weapons. Elite and ceremonial pieces may integrate limited heraldic or elven-derived ornament.

**Visual rule:** start from functional equipment and introduce fantasy variation selectively through ornament, symbolism, materials, magical effects or silhouette. Avoid excessive sculptural clutter on common equipment.

**Identity layers:** clothing and armour should be able to express royal service, household allegiance, religious office or civilian profession without requiring entirely independent equipment systems for each.

## 2.11 Historical Layering — Proposed

Built environments may contain several visibly distinguishable layers:

1. **Proto-Eldani traces:** old foundations, sacred sites and pre-Mahlite motifs.
2. **Deliverance and early Mahlism:** sanctuaries and monuments connected to the survival of the proto-Eldani.
3. **Mature Astonian kingdom:** large walled settlements, cathedrals, noble castles and major civic works.
4. **Later and contemporary additions:** refined Renaissance-era extensions, rebuilt defences, repairs and adaptive reuse.

These are **suggested architectural strata**, not a canon division of historical eras or a final date sequence.

## 2.12 Production and Reuse Constraints

The [Graphical Approach PDD](../../Graphical-Approach-PDD.md) remains authoritative for implementation of all Eldani assets.

In particular:
- Use reusable **material libraries and atomic modular construction components**, rather than monolithic building meshes.
- Establish and approve basic materials first; model medium-scale structure second; generalise and automate only after component designs are accepted.
- Reserve authored geometry for silhouettes, structurally meaningful relief, real damage and collision-relevant features.
- Use shared material, mask, decal and shader workflows for surface age, grime, moss, corrosion and wetness.
- Evaluate architectural motifs at gameplay viewing distances, not only in close-up concept art.
- Permit reusable trims, attachments, symbols, roof variants and dressing sets to express Crown, House and Faith identities without multiplying whole building kits.
- Follow the standard isolated, three-quarter-view asset-concept presentation convention unless a different reference format is explicitly requested.

### Suggested reusable asset families

| Family | Principal shared components | Identity variants |
|---|---|---|
| Fortification | Wall body, parapet, coping, tower modules, gate modules, rampart walkway, supports | Royal, noble house, regional, age/damage |
| Urban building | Foundations, wall/upper-floor sets, roof kits, windows, portals, timber, chimneys | Common, commercial, civic, noble |
| Sacred architecture | Arches, portals, ribs, columns, tracery, vault elements | Mahlite decoration, older reused ornament |
| Heraldic props | Shields, signs, cloth supports, seals, hanging hardware | Crown, house, military, civic |
| Dress and equipment | Shared garments, armour silhouettes, weapon patterns | Profession, class, rank, faith, house |

This is a content-planning vocabulary, not an instruction to create every asset immediately.

## 2.13 Eldani Art Acceptance Criteria

A representative Eldani asset or scene should satisfy these tests:

1. Its construction, scale and silhouette are credible within a grounded late-medieval/early-Renaissance human culture.
2. It remains recognisable as Eldani before heraldry, small carvings or surface textures are added.
3. Elven inheritance is subtle and selective, not the dominant structural language.
4. Crown, religious and house symbols are used in the correct contexts and remain visually distinct.
5. Meaningful geometric detail survives gameplay-distance tests; micro-wear is not needlessly sculpted.
6. Buildings assembled from the kit can support varying ages, regions, wealth and institutional ownership.
7. An Astonian urban scene communicates stone construction, historical depth and a plausible relationship to its landscape.

---

# 3. Further Consideration Required

## 3.1 Eldani History and Geography
- Exact name and layout of Astonia's principal Black Peaks foothill city.
- Timeline and nature of elven–proto-Eldani contact.
- Precise relationship among the Merrshaulk conflict, Mahlfach's intervention and the emergence of the kingdom.
- Distinct regional Eldani cultures outside the royal centre.

## 3.2 Religion
- Formal Mahlite institution names, ranks, vestments, rituals and sacred spaces.
- Iconographic design of Mahlfach's mask and whether any depiction of the Saint is restricted.
- What historical Lythaer imagery survives and how Mahlism interprets it.
- Whether the eight-point royal sun has any formal theological meaning.
- Whether there are minority, surviving or syncretic religious practices among the Eldani.

## 3.3 Houses and Crown
- The number, names, territories, lineages and political privileges of the houses.
- Design of individual coats of arms and any restrictions on heraldic inheritance.
- Royal versus house military organisation and its effect on visual identity.
- Exact colours, geometry and field rules of the Astonian sun banner.

## 3.4 Asset Direction
- Approved final material swatches and calibrated in-engine appearances.
- Degree of Renaissance versus late-medieval expression in different building ages.
- Formal distinction of everyday, sacred, aristocratic and royal clothing.
- Extent of fantasy elements in elite architecture and ceremonial equipment.
- Final library of architectural motifs, sacred symbols and modular asset components.

None of these questions should be silently settled by a single concept image or prototype.

---

# 4. Dependencies and Document Boundaries

- [MMORPG Master PDD](../MMORPG-Master-PDD.md): product design authority, cross-document governance.
- [Graphical Approach PDD](../../Graphical-Approach-PDD.md): mandatory global visual constraints and asset-production workflow.
- [World and Zone Design PDD](../World-and-Zone-Design-PDD.md): placement of settlements, city routes, terrain integration and world geography.
- [Character Creation and Identity PDD](../Character-Creation-and-Identity-PDD.md): player-facing race and appearance systems.
- [Factions and Reputation PDD](../Factions-and-Reputation-PDD.md): gameplay relationships, faction standings and access rules.
- [Class Design PDD](../Class-Design-PDD.md): mechanical class identities; no class restriction or combat rule is implied by cultural religion.
- [Quest, Narrative and Dialogue PDD](../Quest-Narrative-and-Dialogue-PDD.md): narrative presentation of historical events, religious traditions and house politics.
- [Items, Equipment and Loot PDD](../Items-Equipment-and-Loot-PDD.md): equipment and item rules; this PDD supplies only cultural appearance guidance.

---
