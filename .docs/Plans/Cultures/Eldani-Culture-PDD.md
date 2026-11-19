# Ninth Age — Eldani Culture Product Design Document

**Project:** Ninth Age  
**Document Type:** Cultural Sub-PDD  
**Status:** Draft / Design Baseline  
**Scope:** Eldani human culture, Kingdom of Astonia, Astonian Mahlism, the aristocratic houses, regional subcultures, visual identity and cultural asset direction  
**Last updated:** 2026-11-20

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
- Eldani society experienced significant **Arhîn cultural influence around 1,000 years ago**, which has been assimilated into its own traditions.
- **Much of the Eldani royal lineage has some degree of Arhîn ancestry.** The specific genealogies, extent of ancestry in individual families and biological consequences are not yet defined.
- The proto-Eldani had practised a **pantheonic religion** in which **Lythaer**, the god of light and renewal, held a central place.
- From that earlier religious tradition, **Astonian Mahlism** developed and became overwhelmingly dominant in Eldani society.

These events provide historical context for the culture; this PDD does not yet specify the chronology or exact causal relationship between elven contact, the deliverance from the Merrshaulk and the rise of Mahlism.

## 2.3 Visual Archetype — Established / Proposed

**Established:** Eldani architecture and culture most closely resemble **late-medieval to Renaissance Europe**. The Eldani build castles and stone towns. Their visual culture retains an elven inheritance from a millennium earlier. The previously established architectural, clothing and material baseline primarily describes the **Astonian Highlands**; it must not be imposed uniformly on every province. The Kingdom of Astonia contains diverse Eldani regional subcultures. Shared racial, historical and institutional foundations remain applicable where established; see section 2.14 for regional constraints.

**Proposed historical-art references:**
- French late-medieval castle and military engineering;
- Central European medieval stone towns and timber-and-plaster upper floors;
- Gothic civic and ecclesiastical architecture;
- selective early-Renaissance symmetry, courtyards and refinement.

The aim is neither a literal reconstruction nor an eclectic collection of recognisable historical buildings. Eldani work must feel part of one coherent, independently developed civilisation with substantial regional variation. The Highland baseline is a reference point, not a universal provincial kit.

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

The Eldani should not look like Arhîn who happen to use stone. Their Arhîn influence is historical, adapted and assimilated into a distinctly human design language. Its expression is especially important in the aristocracy, merchant classes and military, as well as prestigious architecture.

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

This structure permits local identities while maintaining a recognisable Astonian national identity. Much of the Eldani royal lineage has some Arhîn ancestry; this connection should inform aristocratic visual culture without implying that every noble house or wealthy family has Arhîn ancestry, or that ancestry determines social status.

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

## 2.10 Civilian Dress and Equipment — Established cultural constraints / proposed visual design

**Clothing:** layered tunics, fitted doublets, linen, wool, cloaks, belts, gowns and period-inspired hats. Tailoring, fabric quality, embroidery and jewellery communicate rank and profession.

**Arms and armour:** plausible late-medieval steel plate, mail, brigandine, gambesons, sallets and kettle helmets; swords, halberds, crossbows and other practical period-derived weapons. Elite and ceremonial pieces may integrate limited heraldic or elven-derived ornament.

**Visual rule:** start from functional equipment and introduce fantasy variation selectively through ornament, symbolism, materials, magical effects or silhouette. Avoid excessive sculptural clutter on common equipment.

**Identity layers:** clothing and armour should be able to express royal service, household allegiance, religious office or civilian profession without requiring entirely independent equipment systems for each.

### Arhîn inheritance in Eldani dress and martial equipment

**Established design constraint:** Arhîn cultural influence and the Arhîn ancestry of much of the Eldani royalty should be reflected particularly in aristocratic, merchant and military visual culture. Eldani designs must nevertheless be clearly distinct from Arhîn designs. Cultural borrowing is not evidence of biological ancestry in every wearer.

**Proposed clothing grammar:** retain substantial late-medieval/early-Renaissance tailoring, fitted torsos, structured shoulders, emphatic waists, layered fabrics and recognisably human garment proportions. Adapt Arhîn-derived collar curves, shoulder-yoke construction, geometric seam lines, embroidery and clasp forms into these human garments. Do not copy long cascading panel coats, pervasive narrow bronze framing or the full ceremonial vertical silhouette of Arhîn dress.

- **Aristocracy:** sculpted collars, broad mantles, fitted doublets and structured gowns, velvet, brocade and fine wool, restrained goldwork and inherited geometric ornament integrated with house heraldry. The Crown's red-and-blue quartering and eight-point sun remain distinct from Arhîn symbols. The finest garments may retain more elaborate forms of inherited ornament without becoming Arhîn court dress.
- **Merchant classes:** shorter tailored coats and fitted doublets, shaped cuffs, selective geometric woven or embroidered bands, refined clasps and high-quality dyed cloth. Adapt prestigious fashions into practical garments for commerce, travel and urban life; expensive fabrics and craftsmanship rather than aristocratic heraldry alone express wealth.
- **Commoners:** functional linen and wool shirts, tunics, hose, aprons and cloaks; occasional simplified inherited collar or trim motifs, without universal elaborate ornament.
- **Military:** retain believable human steel plate, mail, brigandine and padded garments. Arhîn inheritance may appear in shaped gorgets, gently curved shoulder plates, shallow architectural fluting or incised geometric borders. Use robust silhouettes, recognisable human armour articulation, heraldic surcoats, shields and livery. Elite and ceremonial equipment may express inherited refinement more strongly than common issue; do not default to Arhîn battlemage attire, slender bronze-framed plate or magical accessories.
- **Mahlite institutions:** preserve a distinct religious silhouette and Masked Saint symbolism. Inherited geometric details may be adopted as ornament, but do not equate Arhîn religious iconography with Mahlism.

### Physical appearance and lineage — Further consideration required

Eldani are human. Arhîn ancestry in much of the royal lineage is established, but its prevalence outside royal families and any visible inherited traits are not. Possible subtle differences in facial refinement or ear shape among particular individuals may be explored as proposals; do not assign Arhîn skull geometry, pointed ears, eye colours, functional immortality, magical aptitude or Withering susceptibility to Eldani by default. The appearance of specific descendants, inheritance rules and character-creation implications require separate approval.

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
3. Arhîn inheritance is selective and assimilated, especially evident in aristocratic, merchant and military design; human tailoring, construction, heraldry and silhouettes remain dominant. An Eldani asset must still read as Eldani without a faction emblem.
4. Crown, religious and house symbols are used in the correct contexts and remain visually distinct.
5. Meaningful geometric detail survives gameplay-distance tests; micro-wear is not needlessly sculpted.
6. Buildings assembled from the kit can support varying ages, regions, wealth and institutional ownership.
7. An Astonian urban scene communicates historical depth and a plausible relationship to its landscape.
8. Regional Eldani assets must reflect their specific province and climate: Highland forms are not defaulted indiscriminately to Perramere, whose Germanic-inspired urban forms and savannah adaptation must be distinguishable in silhouette and construction, or to the Ninh Sea coast, whose maritime infrastructure and proposed Mediterranean-derived waterfront forms should establish a separate identity.

---

## 2.14 Regional Subcultures — Established / Proposed

### Astonian Highlands — primary cultural reference

**Established:** the previously developed Eldani architectural and aesthetic baseline applies primarily to the **Astonian Highlands**, rather than to every province of the Kingdom of Astonia. The kingdom's provincial subcultures differ according to geography and local history.

The Highland baseline includes substantial stone castles, fortified stone towns, late-medieval and early Renaissance forms, assimilated Arhîn influence and visible Crown, House and Faith identities. Existing architectural, clothing, armour and palette proposals in this PDD are Highland-first defaults unless expressly identified as wider cultural rules.

### The Rift of Perramere — Established

- The **Rift of Perramere** lies south of the Astonian Highlands.
- Its climate is **hotter and drier** than the Highlands, with **savannah-like conditions**.
- It lies at a crossroads between the **Miut** (tabaxi) to the west, the **Astonian Highlands** to the north, and the **Adritan**, ancient-Greek-like city-states, to the south.
- Its two main provinces are the rebellious states of **Kimius** and **Vonich**, within the Kingdom of Astonia.
- Their architecture has a **stronger German-like character**, adapted to the region's savannah-like conditions.

The precise political status, nature and chronology of their rebellion, capitals, borders, heraldry and individual differences are not yet defined. Cultural contact does not establish that Miut or Adritan motifs are widespread, that inhabitants adhere to foreign religions, or that the provinces have become independent.

### Perramere architecture — Proposed

Use late-medieval and early-Renaissance Germanic urban forms as the main reference, adapting them to climate rather than importing a cold-climate town unchanged.

| Asset family | Proposed direction |
|---|---|
| Urban houses | Masonry lower storeys, plastered timber frames where materials permit, projecting upper storeys, shaded street fronts and shutters |
| Roofs | Recognisably gabled Germanic silhouettes and stepped or articulated gable ends; moderately reduced pitches where rainfall and materials allow; weathered clay tile |
| Markets | Shaded galleries, covered street-level arcades, courtyards, loading yards and road-facing shopfronts |
| Fortifications | Compact stone or brick works, strong gatehouses and practical towers; avoid uniformly replicating Highland castles |
| Civic buildings | Strong gabled façades, square or polygonal towers, substantial masonry, selective assimilated Arhîn geometric detail |
| Climate adaptation | Recessed windows, shutters, projecting eaves, shaded courtyards, ventilation and water storage; subject to local climate and material availability |

Proposed materials: warm sand-coloured masonry, ochre and off-white plaster, dark weathered timber used selectively, dusty terracotta, iron and muted earth pigments. Timber framing depends on availability; do not assume open savannah supplies abundant construction wood.

Germanic identity should be legible in gable profiles, façade proportions, framing rhythm and civic silhouettes, not merely a recoloured Highland kit. Shade and airflow should be functional. Do not default to desert or ancient Greek architecture.

### Clothing, military and cultural exchange — Proposed

Lighter natural textiles, reduced heavy layering, shorter jackets and adjustable outer garments retain Eldani fitted tailoring while adapting to heat. Merchants may adopt foreign textiles, practical travel goods and selective Miut or Adritan decorative features through contact, subject to separate designs for those cultures. Military equipment may retain practical Eldani plate, mail and padded clothing with adaptations for movement and heat. Provincial military insignia, recruitment and organisation are unresolved.

Kimius and Vonich share a regional basis but should eventually be distinguished through separately approved political, material and artistic features. Do not invent opposing heraldry, political systems, ethnic ancestry or military allegiances to distinguish them prematurely.

### The Ninh Sea coast — Established

- The **Ninh Sea** lies east of the Rift of Perramere and south of **Eldanon**.
- The rebellious Eldani states along the coast have established **maritime traditions**.
- They trade with the **Arhîn**, the **Adritan humans** (ancient-Greek-like city-states), and more distant civilisations including the **Moveli** (Roman-inspired) and **Trebari** (African-inspired).
- Their climate is comparable to the hotter, drier Rift of Perramere, but **coastal**.

The names, number, borders, political arrangements and precise status of these coastal states are not yet established. Trading relationships are established; the specific commodities, routes, institutions and extent of cultural borrowing are not. Do not equate contact with shared religion, ethnicity, government or wholesale adoption of foreign architecture.

### Ninh Sea architecture and settlement identity — Proposed

The coast should be a distinctly maritime and cosmopolitan Eldani regional tradition, drawing primarily on late-medieval and Renaissance Mediterranean port architecture (particularly Venetian and Genoese construction forms), rather than on Highland castles or Perramere's Germanic gables.

| Asset family | Proposed direction |
|---|---|
| Urban housing | Pale stone or plastered masonry, relatively tall narrow street façades, shutters, balconies, recessed windows, shaded ground floors |
| Merchant residences | Arcaded courtyards, arched openings, decorated waterfront façades, prestigious upper reception rooms; assimilated Arhîn detailing where justified |
| Harbours and trade | Stone quays, warehouses, covered markets, loading platforms, lifting equipment and customs facilities |
| Coastal fortification | Sea walls, defensive harbour towers, gates and compact sea-facing fortresses, preserving clear construction and protection logic |
| Civic and sacred architecture | Municipal and commercial halls, galleries, shaded courtyards and locally adapted Mahlite buildings where religious affiliation is established; retain appropriate Eldani institutional imagery |
| Shipbuilding | Timber workshops, slipways, sheltered construction areas, sailmaking and rigging facilities; exact vessel types remain open |
| Climate adaptation | Ventilation, recessed openings, shutters, shade, sheltered courtyards, water storage and coastal weathering; local conditions and building materials must govern the details |

Proposed materials: warm off-white lime plaster, pale limestone or sandstone, warm terracotta tile, salt-weathered timber, oxidised copper or bronze where appropriate and muted maritime blues and greens used selectively. Coastal exposure should be conveyed through shared material and shader treatments, not excessive geometry or uniformly ruined structures.

Architecture should read as Eldani through its late-medieval/early-Renaissance masonry, practical urban construction and locally adapted Arhîn inheritance. The Mediterranean reference is an art-direction proposal, not a claim that the coastal states reproduce Venetian or Genoese institutions. Repeated arches, galleries and waterfront massing should distinguish the region from Perramere even without different colours.

### Maritime trade, clothing and military identity — Proposed

Merchants, shipowners, craftspeople and seafarers may provide a particularly visible expression of the region's cosmopolitan character. Retain Eldani fitted garment construction while using lighter linen and wool where appropriate, shorter coats, practical belts, durable deck footwear and layered garments suited to wind and heat. Wealthy merchant garments may incorporate imported textiles, jewellery and selective ornament; specific foreign influences must follow the relevant culture's eventual PDD.

- Arhîn trade offers a possible continuing source of refined geometric ornament, fine textiles and specialist goods, rather than solely historical influence; specific imports are not established.
- Adritan and Moveli trade may inform selected decorative and commercial forms, but port architecture must not default to ancient Greek or Roman temples.
- Trebari trade creates opportunities for imported textiles, objects and artistic exchange; do not assign particular African regional motifs or technologies before the Trebari cultural design is established.

Proposed naval and military expression: practical maritime equipment, coastal defence, shipboard personnel and locally adapted armour with restrained metal protection where appropriate. Formal naval organisation, vessels, fleets, recruitment and insignia remain unresolved. No common coastal heraldry, independent republics, pirates or merchant oligarchies are established by the rebellious political status.

### Regional production rules

Maintain shared Eldani construction, garment and equipment foundations where appropriate, but develop distinct regional gable, roof, wall, shutter, gallery and courtyard modules. For the Ninh Sea coast, add reusable waterfront, quay, warehouse, sea-wall, harbour tower and shipyard modules where appropriate; preserve shared masonry and other asset families without flattening provincial silhouettes. Preserve the grounded mid-poly and modular-production standards of the Graphical Approach PDD. The proposed palette does not constitute final shader or environmental specifications.

---

# 3. Further Consideration Required

## 3.1 Eldani History and Geography
- Exact name and layout of Astonia's principal Black Peaks foothill city.
- Timeline and nature of elven–proto-Eldani contact.
- Precise relationship among the Merrshaulk conflict, Mahlfach's intervention and the emergence of the kingdom.
- Additional Eldani provincial subcultures outside the Astonian Highlands, Rift of Perramere and Ninh Sea coast.
- The precise geography of the Rift, its settlements, resources and trade routes; individual identities of Kimius and Vonich, the nature of their rebellion, and extent of Miut and Adritan influence.
- Names and individual identities of the rebellious Ninh Sea coastal states; their relationship to Eldanon, the Crown and each other; ports, coastal geography, trade routes, commodities, foreign influence, shipbuilding, fleets and territorial boundaries.

## 3.2 Religion
- Formal Mahlite institution names, ranks, vestments, rituals and sacred spaces.
- Iconographic design of Mahlfach's mask and whether any depiction of the Saint is restricted.
- What historical Lythaer imagery survives and how Mahlism interprets it.
- Whether the eight-point royal sun has any formal theological meaning.
- Whether there are minority, surviving or syncretic religious practices among the Eldani.

## 3.3 Houses and Crown
- The number, names, territories, lineages and political privileges of the houses, including the distribution and history of Arhîn ancestry in royal and other genealogies.
- Design of individual coats of arms and any restrictions on heraldic inheritance.
- Royal versus house military organisation and its effect on visual identity.
- Exact colours, geometry and field rules of the Astonian sun banner.

## 3.4 Asset Direction
- Approved final material swatches and calibrated in-engine appearances.
- Degree of Renaissance versus late-medieval expression in different building ages.
- Formal distinction of everyday, merchant, sacred, aristocratic and royal clothing; exact Arhîn-derived motifs, tailoring details and military armour variants.
- Whether and how Arhîn ancestry manifests in the physical appearance or other inherited characteristics of particular Eldani individuals.
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
