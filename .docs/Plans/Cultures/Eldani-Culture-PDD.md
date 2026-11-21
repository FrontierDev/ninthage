# Ninth Age — Eldani Culture Product Design Document

**Project:** Ninth Age  
**Document Type:** Cultural Sub-PDD  
**Status:** Draft / Design Baseline  
**Scope:** Eldani human culture, Kingdom of Astonia, Astonian Mahlism, the aristocratic houses, regional subcultures, visual identity and cultural asset direction  
**Last updated:** 2026-11-21

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
- Approximately **1,000 years ago**, a godlike figure known as **Mahlfach, the Masked Saint**, saved the proto-Eldani from the **Merrshaulk**, the Serpent-men. **Established historical truth, unknown to Eldani:** Mahlfach was **Osmutuun**, the Malamer God-King. See [World Lore and History](../World-Lore-and-History-PDD.md).
- Eldani society experienced significant **Arhîn cultural influence around 1,000 years ago**, which has been assimilated into its own traditions.
- **Much of the Eldani royal lineage has some degree of Arhîn ancestry.** The specific genealogies, extent of ancestry in individual families and biological consequences are not yet defined.
- The proto-Eldani had practised a **pantheonic religion** in which **Lythaer**, the god of light and renewal, held a central place.
- From that earlier religious tradition, **Orthodox Mahlism first** developed as an interpretation of Mahlfach's example, and **Astonian Mahlism developed later** amid successive crises and a public desire for a god to pray to after the loss of the earlier Erôthian pantheon. Astonian Mahlism became overwhelmingly dominant in Eldani society.

These events provide historical context for the culture; this PDD does not yet specify the chronology or exact causal relationship between elven contact, the deliverance from the Merrshaulk and the rise of Mahlism.

## 2.3 Visual Archetype — Established / Proposed

**Established:** Eldani architecture and culture most closely resemble **late-medieval to Renaissance Europe**. The Eldani build castles and stone towns. Their visual culture retains an elven inheritance from a millennium earlier. The previously established architectural, clothing and material baseline primarily describes the **Astonian Highlands**; it must not be imposed uniformly on every province. The Kingdom of Astonia contains diverse Eldani regional subcultures. Shared racial, historical and institutional foundations remain applicable where established; see section 2.14 for regional constraints.

**Proposed historical-art references:**
- French late-medieval castle and military engineering;
- Central European medieval stone towns and timber-and-plaster upper floors;
- Gothic civic and ecclesiastical architecture;
- selective early-Renaissance symmetry, courtyards and refinement.

The aim is neither a literal reconstruction nor an eclectic collection of recognisable historical buildings. Eldani work must feel part of one coherent, independently developed civilisation with substantial regional variation. The Highland baseline is a reference point, not a universal provincial kit. **Established governing principle: Eldani first, regional adaptations second.** After approximately 1,000 years of cultural development, the regional Eldani peoples share a coherent Eldani identity. Climate, trade, ancestral continuity and external influences modify this identity rather than replace it. Real-world historical references are secondary design tools, not separate in-world civilisations.

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

These descriptions are **Eldani religious beliefs**, not proof that Mahlfach is a deity dispensing spells. **Established hidden truth:** Mahlfach is **Osmutuun**. **Light magic is the positive aspect of soul/spirit magic**; Mahlist conviction in the teachings itself manifests apparent divine power, without requiring Osmutuun to actively grant it. Osmutuun was imprisoned during the Seventh and Eighth Ages and returned to his former place of imprisonment after the Serpent War, withdrawing from the world's events.

### Mainstream Astonian Mahlite religious visual identity — Proposed

This visual direction applies to mainstream Astonian Mahlism, not Orthodox Mahlism. **Established:** mainstream priests wear hoods and gold masks; Orthodox Mahlist priests do not wear masks or use mask imagery, and idolatry is forbidden in Orthodox Mahlism. See the Dalmen Highlands section for the orthodox distinction.

**Primary subject:** the **Masked Saint**. The mask and masked figure are the strongest prospective motifs for:
- statuary, reliefs, murals and reliquaries;
- cathedral and sanctuary portals;
- devotional objects, religious vestments and ritual props;
- commemorative monuments to deliverance from the Merrshaulk.

**Architecture:** monumental stone cathedrals, sanctuaries, monasteries, fortified abbeys and religious civic buildings, with Gothic-derived massing, tall openings, stone vaults and strong verticality. The oldest buildings may preserve earlier geometry or elven-influenced workmanship.

**Inherited motifs:** light, radiating forms, renewal imagery and older solar design may appear in historical layers, reused ornament or pre-Mahlite ruins. Their continued theological status is unresolved.

**Proposed institutional palette:** ivory (**#E5D9BA**), old gold (**#B49659**), deep crimson (**#6F172A**), charcoal (**#424858**) and stone grey (**#A7A39A**). This is an art-direction starting point only.

**Important distinction:** the **mask** is the proposed emblem of mainstream Astonian Mahlism, not Orthodox Mahlism; the **eight-pointed sun** is established royal heraldry; historical **Lythaer** imagery is a separate older visual tradition. Their relationship should not be merged by assumption. Do not substitute the royal sun for a religious emblem in either tradition.

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
- **Mahlite institutions:** preserve distinct religious silhouettes and denomination-specific symbolism. Mainstream Astonian Mahlism uses Masked Saint symbolism; Orthodox Mahlism prohibits masks and mask imagery and forbids idolatry. Inherited non-figurative geometric details may be explored as ornament, but do not equate Arhîn religious iconography with Mahlism.

### Physical appearance and lineage — Further consideration required

Eldani are human. Arhîn ancestry in much of the royal lineage is established, but its prevalence outside royal families and any visible inherited traits remain open. **Established for Aurësén–human or Aurësén–orc descendants:** mixed children are **not immortal** and **do not Wither**; some may show **emotional volatility and higher intelligence**, depending on their proportion of Aurësén ancestry. Specific inheritance thresholds, facial appearance and character-creation implications remain open. Do not automatically assign Arhîn skull shape, pointed ears, muted eye colours or particular magic talents to Eldani characters merely because royal bloodlines contain Arhîn ancestry.

### Eldani naturalists — Established / Proposed

**Established:** Eldani naturalists are visually subtler than Arhîn naturalists. They wear clothing similar to ordinary Eldani civilians, using various wools and plant-based natural adornments rather than the conspicuous animal-part attire associated with Arhîn occultist naturalists.

**Proposed treatment:** familiar fitted tunics, doublets, bodices, skirts and shoulder mantles; layered wool and linen appropriate to local climate; modest dried herb bundles, woven botanical motifs, plant-fibre ties and wooden accessories. Prefer practical herbalist or field-scholar silhouettes over elaborate ritual robes, antler headdresses or conspicuous bone trophies. Regional adaptations follow the same Eldani-first principles as civilian clothing.

Their formal organisation, precise religious affiliations and magical practices remain open, but **regional religious attitudes are established**: Orthodox Mahlists are generally unconcerned by druidism, which is normal among Estyenni; cosmopolitan districts regard it as strange, and mainstream priests toward Eldanon are more hostile. Eldani naturalists do **not** experience the darker ancestral attunement characteristic of Arhîn druids. Do not assign Crown heraldry or an unapproved religious emblem by default.

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
8. Regional Eldani assets must read as **Eldani first, regional adaptations second**, even without heraldry. They must also reflect their specific province and climate: Highland forms are not defaulted indiscriminately to Perramere, whose Germanic-inspired urban forms and savannah adaptation must be distinguishable in silhouette and construction, or to the Ninh Sea coast, whose maritime infrastructure and proposed Mediterranean-derived waterfront forms should establish a separate identity. Northern Eldani assets primarily adapt existing forms to cold conditions, rather than copying Estyenni culture. Dalmen assets communicate proto-Eldani continuity, orthodox Mahlism, poorer material circumstances and a cool, dry highland climate.

---

## 2.14 Regional Subcultures — Established / Proposed

### Governing principle — Eldani first, adaptations second (Established)

All Eldani regional subcultures are **Eldani first**. A millennium of shared cultural development has produced an identifiable civilisation across its provinces, notwithstanding political rebellion, local poverty, differences in religious practice and foreign contact. Regional expressions are adaptations of that civilisation, not independent aesthetic systems based directly on real-world analogues.

**Art-direction hierarchy:** start with the shared Eldani design grammar (human medieval-to-Renaissance construction and tailoring, assimilated Arhîn influence and applicable Mahlite and heraldic traditions); then modify it for local climate, resources, economy, historical continuity and trade. The Astonian Highlands remain the primary reference, but common Eldani forms need not be identical to the royal heartland's most affluent forms. Maintain recognisable relationships in door and window composition, masonry and timber construction, garment cuts, armour articulation and ornament. Provincial identities must remain distinguishable in silhouette and practical adaptations without relying on a separate racial kit or royal heraldry to demonstrate Eldani identity.

Historical references (French-Gothic, Germanic, Mediterranean and Anglo-Saxon) indicate relative tendencies within the Eldani vocabulary, not instructions to reproduce those cultures wholesale. Political rebellion does not remove Eldani cultural identity or require royal livery. The **Estyenni** are a distinct people and are not included in this rule; it applies to northern Eldani communities, not the independent Estyenni culture.

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

The stronger Germanic influence should be legible in gable profiles, façade proportions, framing rhythm and civic silhouettes, as a regional inflection of Eldani architecture rather than either a direct German historical recreation or merely a recoloured Highland kit. Shade and airflow should be functional. Do not default to desert or ancient Greek architecture.

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

The coast should be a distinctly maritime and cosmopolitan Eldani regional tradition, adapting common Eldani architecture to coastal needs with secondary references to late-medieval and Renaissance Mediterranean port forms (particularly Venetian and Genoese construction forms). These references inform regional features, not an independent architectural identity.

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

### Selheim Basin and the northern Eldani — Established

- The **Selheim Basin** lies to the north and is inhabited by the **Estyenni**, a Viking/Scandinavian-inspired people with a strong cultural identity.
- The Selheim Basin was formerly part of the Kingdom of Astonia but has broken away, reflecting that distinct cultural identity.
- The northern climate is considerably colder than the Astonian Highlands.
- Northern Eldani cultural adaptations are primarily **practical responses to the cold**, not a wholesale adoption of Estyenni culture.

The Estyenni are a distinct cultural identity; this Eldani PDD records their relationship to the former kingdom and neighbouring northern Eldani communities, not their complete cultural design. The precise political borders, dates and circumstances of separation, and present relations between Astonia and the Estyenni remain unspecified. Do not assume that all inhabitants of the Selheim Basin are Eldani or that the basin remains an Astonian province.

### Northern Eldani visual direction — Proposed

Retain the underlying Highland-derived Eldani construction grammar, fitted tailoring, heraldic distinctions and selective assimilated Arhîn influence where applicable. Climate adaptation, not borrowed Scandinavian motifs, is the primary differentiator.

- **Architecture:** substantial masonry foundations, insulated timber or masonry walls, smaller recessed openings, shutters, sheltered entrances, steep roof pitches where snowfall and materials warrant them, enclosed circulation and prominent hearths or chimneys.
- **Settlements and defences:** compact protected streets, wind shelter, covered work areas, enclosed guardhouses and robust fortifications adapted to cold conditions.
- **Dress:** thick wool, layered fitted garments, lined and heavy cloaks, gloves, practical insulated boots and weather-protective headwear. Fur and other specific materials depend on availability.
- **Military equipment:** conventional Eldani plate, mail and padded equipment adapted for cold-weather service, with greater emphasis on insulated underlayers and sheltered fittings rather than a new Scandinavian armour language.
- **Material direction:** weathered grey stone, dark timber, subdued wool colours and restrained cold-weather surface effects, following the Graphical Approach PDD and the actual biome.

The amount of snowfall, availability of timber, specific northern settlements and degree of cultural exchange with the Estyenni are unresolved. Develop Estyenni architecture, dress, religion, military and heraldry separately rather than using the northern Eldani variant as a substitute.

### Dalmen Highlands — Established

- The **Dalmen Highlands** lie **west of Eldanon**.
- The inhabitants are generally **poor** and adhere to an **orthodox form of Mahlism**.
- They retain stronger connections to their **proto-Eldani past**, with an **Anglo-Saxon-like** cultural reference.
- The climate is **cool and dry**.

The region is culturally conservative in its ancestral connections and religious identity. Orthodox Mahlism is a form of Mahlism. **Established doctrine: idolatry is not permitted. Orthodox Mahlist priests do not wear masks or use mask imagery.** This distinguishes their practice from mainstream Astonian Mahlism without changing the shared worship of Mahlfach. The remainder of its doctrine, institutions, relationship to mainstream Astonian Mahlism, ritual practices and the precise origin of its orthodoxy have not yet been defined. Stronger proto-Eldani continuity does not imply surviving worship of Lythaer or other pantheonic gods, nor does it establish reduced Arhîn ancestry. Poverty is a regional socioeconomic characteristic, not a claim that every individual is poor or incapable of skilled craftsmanship.

### Dalmen architectural and material identity — Proposed

Use early medieval Anglo-Saxon-derived building forms as a secondary reference for retained proto-Eldani traditions within recognisably contemporary Eldani architecture, rather than depicting a separate early medieval society frozen in the past. Distinguish Dalmen through simpler construction, economical materials, visible maintenance and the deliberate preservation of old forms.

| Asset family | Proposed direction |
|---|---|
| Rural houses and halls | Low timber-framed or stone-based buildings, simple rectangular plans, pronounced roof silhouettes, modest windows and practical hearths |
| Settlement structures | Small clustered farmsteads and villages, livestock enclosures, common halls, practical walls and sheltered storage |
| Religious buildings | Austere stone sanctuaries or small village chapels, compact naves and simple monumental entrances; selectively preserved older Eldani ornament |
| Defence | Economical stone or timber works, earthworks and modest fortified halls where appropriate; no universal grand castle standard |
| Material character | Weathered stone, rough plaster, dark or greyed timber, muted natural fabrics and locally appropriate roofing |

The cool, dry climate suggests sheltered interiors, durable construction and restrained water-management features where needed. Exact vegetation, rainfall, roofing materials and resources remain unestablished. Avoid indiscriminately applying northern snow effects, a hot-desert palette or the elaborate stone and roofline detailing of affluent Astonian Highland cities.

### Dalmen dress, faith and ancestral continuity — Proposed

- Practical wool and linen tunics, simple trousers or hose, pinned cloaks, substantial belts and sturdy boots. Economical construction, repairs and restrained ornament should distinguish everyday attire.
- Older interlace, geometric borders and simple metal clasps may provide a distinctive proto-Eldani-derived decorative vocabulary, pending approval of that ancestral visual language.
- Orthodox Mahlite sacred objects and vestments must respect the established prohibition on idolatry: no worn masks, mask-shaped pendants or brooches, embroidered masks, mask imagery or devotional idols. A proposed priestly outfit uses an uncovered face beneath a simple hood, austere wool or linen vestments, a plain sash or cord and restrained non-figurative woven ornament. Places of worship and ritual objects must likewise respect the prohibition; do not introduce devotional figurative imagery by default. Do not use the Crown's eight-pointed sun as a substitute faith emblem. Specific permitted non-figurative religious symbols, the full scope of permissible representations, other doctrinal differences and formal restrictions on wealth remain unresolved.
- If martial equipment is depicted, use plausible practical Eldani equipment with economical maintenance and older design influences: padded clothing, shields, mail or simple helmets where appropriate. No separate military doctrine, force composition or heraldry is established by this brief.

Dalmen should not be reduced to a generic Viking culture or treated as identical to the Estyenni. Its proposed visual distinctiveness lies in stronger proto-Eldani continuity, orthodox Mahlism, comparatively modest material circumstances and cool-dry highland adaptation. Preserve differences in craftsmanship, age and wealth within the region.

### Regional production rules

Shared Eldani asset families and construction principles are the default foundation. Create regional variants and genuinely necessary climate- or function-specific modules; do not develop unrelated complete race kits from real-world historical references. The acceptance test is whether assets read as Eldani even before regional colours or political emblems are added, while still communicating their local circumstances.

Maintain shared Eldani construction, garment and equipment foundations where appropriate, but develop distinct regional gable, roof, wall, shutter, gallery and courtyard modules. For the Ninh Sea coast, add reusable waterfront, quay, warehouse, sea-wall, harbour tower and shipyard modules where appropriate; preserve shared masonry and other asset families without flattening provincial silhouettes. Northern Eldani variants should reuse Highland construction families with sheltered openings, roofing, insulation and cold-weather adaptations. Dalmen requires a simpler ancestral-influenced set of halls, modest dwellings and chapels; do not substitute Estyenni modules for Dalmen. Preserve the grounded mid-poly and modular-production standards of the Graphical Approach PDD. The proposed palettes do not constitute final shader or environmental specifications.

---

## 2.15 Established Mahlite Theology and Institutions

### The two religious traditions — Established

**Orthodox Mahlism** is the older tradition, founded on the proto-Eldani interpretation of **Mahlfach/Osmutuun's deeds during the Serpent War**, not on explicit doctrine personally taught by Osmutuun. Orthodoxy teaches that individuals should **improve themselves to protect the vulnerable**. It rejects the premise that gods give power freely: divine offers instead demand **servitude**. It therefore rejects **idolatry and mask imagery of Mahlfach**, encouraging believers to **follow his example** rather than venerate his representation.

Orthodox Mahlism **has no concept of a promised afterlife**. Doing good merely to secure a personal reward after death contradicts its ethics: adherents should act **for the benefit of living others**. Devotion is **primarily personal**. Orthodox communities, monasteries and individual teachers operate **without a single central ecclesiastical authority**. Orthodoxy is notably associated with the **Dalmen Highlands**. Orthodox Mahlists **generally do not concern themselves with druidic worship**.

**Astonian Mahlism** grew later from Orthodox Mahlism amid successive political and societal crises that increased demand for a deity to pray to in the absence of the **old Erôthian pantheon**. Its theology holds the **opposite emphasis**: believers seek **divine aid and protection from Mahlfach**. **Regular attendance at temples is expected.** Mainstream clergy wear hoods and **gold masks**; their organisation and imagery must not be imposed on Orthodox communities.

In Astonian belief, the **faithful's souls go to Mahlfach after death** and are **reborn when he summons them to confront the world's evils again**. After that final campaign, they anticipate a **paradisal afterlife** as their reward. This is **religious belief**, not verified actual post-mortem cosmology.

**Both branches' Light magic** arises from conviction: it is the positive aspect of **soul/spirit magic**. The magical result does **not require Osmutuun to intervene personally**, although Astonian and Orthodox adherents interpret it differently.

### Church governance — Established

The **King of Astonia is head of the Astonian Church**. A **High Priest directly beneath the king** administers its day-to-day affairs. Exact titles beneath this office, ranks, revenues, control of temples and regional offices remain open.

**Orthodox Mahlism** instead has no unified central head and is taught through **local communities, individual teachers and monasteries**. Its rejection of masks and idolatry is established.

**Exemplars:** some serve the **Crown** directly; others belong to **militant Mahlist orders**. Some orders operate **independently of both Crown and central Church**, especially in distant territories. Their precise orders, command, rulebooks and iconography remain unspecified.

### Eldani naturalists and regional attitudes — Established

Eldani naturalists wear **ordinary Eldani-style garments**, with **various wools and understated plant-based natural adornments**, not the conspicuous animal-hide, bone and antler dress of Arhîn occultist naturalists. **Only Arhîn** become attuned to their sinister ancestry through druidic practice; this **does not happen to Eldani**.

**Regional attitudes vary:** in **Estyenni** communities druidism is ordinary daily practice; in cosmopolitan areas it is regarded as unusual; closer to **Eldanon**, mainstream Mahlist priests are increasingly hostile; **Orthodox Mahlists generally do not care**. No single universal legal ban on druidism has been established. The precise roles, theology, community standing and class affiliation of Eldani naturalists remain open.

## 2.16 The Eldanon Academy and Magical Regulation — Established

Town communities often possess **magic schools affiliated with the Eldanon Academy of the Arcane Arts**, headed by **Arch-Lector Roderich Squall**. Admissions depend on **aptitude, merit and ability to pay tuition**. Poor children may be unable to attend, although **limited scholarships are available**. The Academy's precise governing charter, curriculum, campus map and credential system remain open.

**Independent magical practice is permitted**, but magical laws are **strictly enforced** throughout **Astonia and its rebellious states**.

- **Necromancy is outright prohibited**.
- **Transmutation is forbidden in regulated uses** due to its **economic effects** (the exact scope needs definition).
- **Illusion is forbidden in various applications** (scope unspecified).
- **Teleportation is heavily regulated** rather than universally banned.

Do not generalise the bans beyond the established categories and uses or invent enforcement agencies, penalties or exemptions.

## 2.17 Military Institutions and the Secessions — Established

**Astonia maintains a substantial standing royal army** and **draws upon, incorporates and integrates household forces**. Its main organised magical military wing is the **Astonian Royal Arcanists**, recruited **directly from the Eldanon Academy of the Arcane Arts**, with **additional direct-entry routes**.

**The breakaway states maintain volunteer armies** and **do not possess a centralised military sorcery wing** comparable to the Royal Arcanists. This does **not** mean no individual rebel soldiers can use magic.

The secession of the **Ninh Sea maritime states** caused Astonia to lose **about 90% of its naval forces**. Some **loyal ships and crews escaped**, retaining a much-reduced royal naval capability. The loss must not automatically be attributed to a naval battle or the destruction of ships: the mechanism includes political loss of territory, fleets and personnel.

The **Estyenni in the north** had **already seceded during the reign of King Achov III's father**, before Achov's rule began. They are an independent people, not merely colder-climate Eldani.

## 2.18 The Astonian Political Crisis — Established Facts and Open Motives

### Confirmed facts

- **King Achov III** rules Astonia, with **Lord Talen Perydain** as his **High Chancellor**.
- **Perydain was responsible for assassinating Achov's father**, intending to **destabilise the monarchy** and **blame the assassination on the centaurs**.
- Two of Achov's principal advisers were working, respectively, for the interests of the **Arhîn** and **Urngor (mountain dwarves)**. Their identities, exact demands and coordination are **not specified**.
- The **Arhîn adviser** was working in the interests of a people who particularly **wanted Achov removed**, but the Arhîn **were unaware of Perydain's assassination plot and his attempt to blame the centaurs**.
- The centaurs were **already inside Astonian territory** when Achov prohibited their **migration/passage**. They travelled from the **western steppes** along a **sacred route** towards the **Moonlight Well**, for the generational **Ertniiagra** sacrifice of chosen **western and eastern centaurs**.
- The Arhîn sought the removal of Achov **to restore the sacred centaur migration corridor**, not because they had jointly orchestrated the assassination.
- The **southern Perramere provinces**, including **Kimius and Vonich**, are rebellious; the **Ninh Sea maritime states seceded**; the **Estyenni departure predates Achov's reign**.
- The distinct Urngor adviser's strategic objectives and whether any of the conspirators knowingly collaborated remain undefined.

### Possible immediate military catalyst — Proposed, not yet established

With centaurs already in Astonian territory, **Achov could have deployed troops north to evict them**. This may have further alienated maritime soldiers asked to serve far from their homes and simultaneously reduced available loyal troops in the south, allowing already-organised rebels to seize ports, arsenals or government buildings. **The deployment, defections and revolt sequence are proposals, not approved historical fact.**

The previously discussed **royal centralisation, provincial rights, merchant wealth, fiscal grievance, ecclesiastical interference and military patronage** are **candidate underlying tensions**, not yet canon. Their relative importance, protagonists, constitutional mechanisms and the exact casus belli of the Ninh Sea secession remain to be established.

### Cross-cultural stakes

The centaurs perform the **Ertniiagra** at the Moonlight Well. Publicly, the sacrifices are thought to preserve the imprisonment of the Night Serpent. The hidden truth is that they **supply sylvan blood**, which **Kabithh uses to suppress the Night Serpent inside its own mind**; the Tinithas misrepresented the original bargain. The Arhîn interest in preserving the migration route is established, but that **does not imply the Arhîn adviser or Achov knew Kabithh's hidden purpose**. See [World Lore and History](../World-Lore-and-History-PDD.md).

---

# 3. Further Consideration Required

## 3.1 Eldani History and Geography
- Exact name and layout of Astonia's principal Black Peaks foothill city.
- Timeline and circumstances of Arhîn–proto-Eldani contact; Mahlfach's hidden identity as Osmutuun and the later theological divergence of Orthodox and Astonian Mahlism are established.
- Precise relationship among the Merrshaulk conflict, Mahlfach's intervention and the emergence of the kingdom.
- Any additional Eldani provincial subcultures and their relationship to the Astonian Highlands, Rift of Perramere, Ninh Sea coast, northern Eldani communities and Dalmen Highlands.
- The precise geography of the Rift, its settlements, resources and trade routes; individual identities of Kimius and Vonich, the nature of their rebellion, and extent of Miut and Adritan influence.
- Names and individual identities of the rebellious Ninh Sea coastal states; their relationship to Eldanon, the Crown and each other; ports, coastal geography, trade routes, commodities, foreign influence, shipbuilding, fleets and territorial boundaries.
- Present political relations, northern Eldani settlement locations and the Estyenni territorial boundary; the **Estyenni secession under Achov III's father** is established.
- Dalmen settlement patterns, resources, origins and persistence of poverty, regional political institutions and extent of proto-Eldani cultural continuity.

## 3.2 Religion
- Clerical offices and rites below the established **king-as-head** and subordinate daily **High Priest**, and Orthodox community/monastery arrangements.
- Further Orthodox liturgy, non-figurative decoration and visual identity beyond the established absence of idolatry and masks, personal devotion, decentralised organisation and ethics of self-improvement for others.
- Final iconographic design of Mahlfach's mask in mainstream Astonian Mahlism and any additional restrictions on depictions of the Saint; Orthodox Mahlism already forbids masks and mask imagery and prohibits idolatry.
- What historical Lythaer imagery survives and how Mahlism interprets it.
- Whether the eight-point royal sun has any formal theological meaning.
- Whether there are minority, surviving or syncretic religious practices among the Eldani.

## 3.3 Houses and Crown
- The number, names, territories, lineages and political privileges of the houses, including the distribution and history of Arhîn ancestry in royal and other genealogies.
- Design of individual coats of arms and any restrictions on heraldic inheritance.
- Detail of royal army integration with household forces, the Royal Arcanists and the volunteer armies of the breakaway states; their broad arrangements are established.
- Exact colours, geometry and field rules of the Astonian sun banner.

## 3.4 Asset Direction
- Approved final material swatches and calibrated in-engine appearances.
- Degree of Renaissance versus late-medieval expression in different building ages.
- Formal distinction of everyday, merchant, sacred, aristocratic and royal clothing; exact Arhîn-derived motifs, tailoring details and military armour variants.
- The exact prevalence and appearance of Arhîn ancestry in Eldani individuals; mixed descendants' **lack of immortality and Withering**, and their **possible volatile emotion and increased intelligence**, are already established.
- Extent of fantasy elements in elite architecture and ceremonial equipment.
- Final library of architectural motifs, sacred symbols and modular asset components.

None of these questions should be silently settled by a single concept image or prototype.

---

# 4. Dependencies and Document Boundaries

- [MMORPG Master PDD](../MMORPG-Master-PDD.md): product design authority, cross-document governance.
- [World Lore and History](../World-Lore-and-History-PDD.md): Osmutuun/Mahlfach, the Grey Ones, Ertniiagra, Night Serpent, Silver Key and shared chronology.
- [Graphical Approach PDD](../../Graphical-Approach-PDD.md): mandatory global visual constraints and asset-production workflow.
- [World and Zone Design PDD](../World-and-Zone-Design-PDD.md): placement of settlements, city routes, terrain integration and world geography.
- [Character Creation and Identity PDD](../Character-Creation-and-Identity-PDD.md): player-facing race and appearance systems.
- [Factions and Reputation PDD](../Factions-and-Reputation-PDD.md): gameplay relationships, faction standings and access rules.
- [Class Design PDD](../Class-Design-PDD.md): mechanical class identities; no class restriction or combat rule is implied by cultural religion.
- [Quest, Narrative and Dialogue PDD](../Quest-Narrative-and-Dialogue-PDD.md): narrative presentation of historical events, religious traditions and house politics.
- [Items, Equipment and Loot PDD](../Items-Equipment-and-Loot-PDD.md): equipment and item rules; this PDD supplies only cultural appearance guidance.

---
