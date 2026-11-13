# Ninth Age — Arhîn Culture Product Design Document

**Project:** Ninth Age  
**Document Type:** Cultural Sub-PDD  
**Status:** Draft / Design Baseline  
**Scope:** Arhîn (Dawn Children), Aurësén inheritance, the Withering, Culosíma and the Great Forests, caste society, Queen Ilya, Tiandre and the Suntower; cultural visual identity and asset direction  
**Last updated:** 2026-11-13

---

# 1. Purpose and Authority

This is the dedicated **Arhîn** cultural sub-PDD, under the [Culture Design PDD](../Culture-Design-PDD.md). It is the canonical place for Arhîn-specific history, society, religion, geography as cultural context, art direction and questions awaiting decisions.

Its purpose is to define an identity that can inform environment art, architecture, props, clothing, armour, characters and visual storytelling without either repeating the Eldani style or drifting into generic elven fantasy.

Decision labels follow the parent:
- **Established:** explicitly supplied lore or directly supported by existing game data. Where they differ, both are recorded and the discrepancy flagged.
- **Proposed:** interpretative visual direction suitable for concepting, **not** new canon.
- **Further Consideration Required:** unresolved; do not silently invent implementation or lore decisions.

The [Graphical Approach PDD](../../Graphical-Approach-PDD.md) controls the project's grounded mid-poly visual fidelity, modularity, materials, concept presentation, shaders and asset construction. Cultural description does not create character gameplay, Withering mechanics, faction rules or final maps.

---

# 2. Arhîn (Dawn Children)

## 2.1 Core Identity — Established

The **Arhîn**, the **Dawn Children**, are descendants of the **Aurësén**, the **Daylight People**.

The Aurësén lost the **Gift of Light** at the end of the **Serpent War**, approximately **1,000 years ago**. Their descendants continue an ancient and exceptionally magical civilisation, but now depend on regular contact with **sunlight or magical sources of power** to avoid the Withering.

The Arhîn are **functionally immortal** but **do not naturally possess godlike powers**. Longevity is not equivalent to divinity, invulnerability or exemption from the Withering.

Their society revolves around a three-caste hierarchy, material dependence on protectors, concentrated magical authority and the management of access to light.

**Defining visual premise:** luminous, elevated privilege above an ancient forest world, with progressively darker and more dependent forms of habitation at lower levels. The civilisation is both magnificent and vulnerable. **Its present splendour is the diminished inheritance of a more accomplished Aurësén civilisation, not a new golden age.**

## 2.2 Historical Foundation — Established

- The Aurësén are the Arhîn's ancestors.
- The Serpent War ended approximately 1,000 years ago and culminated in the **loss of the Gift of Light** and the destruction of **Yer-Aranie**, a great Aurësén city.
- Following this loss, the Arhîn became susceptible to **the Withering**.
- Over the subsequent millennium, Arhîn architectural expertise and the ability to construct their grandest buildings have **degraded**: master designers and craftsmen died in the Serpent War or were later lost to the Withering.
- Despite retaining a highly sophisticated civilisation and splendid buildings, **contemporary Arhîn architecture is generally less accomplished than architecture from the Aurësén peak**. Only the highest rulers retain truly opulent abodes.
- The present Arhîn primarily occupy the **three Great Forests** around the **Culosíma**.
- Existing race data identifies the ancestral realm as the **Aurësén Diarchy**, with Arhîn lands continuing part of its former western territory.
- Existing race data uses the older terms **Terhat-Ilu** for the cataclysmic war and **Annokal** for the Gift of Light. These terms appear in existing game definitions and may be retained as in-world terms, but their preferred forms and spelling should be reviewed before further use.

**Important:** the history does not establish the full politics of the Aurësén Diarchy, whether Yer-Aranie was its capital, how the Gift was originally obtained or what happened to every Aurësén elsewhere. Do not infer those details.

## 2.3 Nostalgia and Public Memory-Viewing Scriptures — Established / Proposed

### Cultural nostalgia — Established

The Arhîn are **inherently nostalgic** for their ancestral civilisation and the times of the **Aurësén**. Reflecting on ancient times is a **common pastime**, not merely an occasional scholarly or religious activity.

Public spaces contain **magical runic scriptures** whose patterns induce **trances and memory viewing** in Arhîn who contemplate them. To an outsider, the practice can seem strange: the Arhîn appear simply to **stand or sit staring at the runic patterns**, while the experience of remembering unfolds inwardly.

The contrast between their outward stillness and inward engagement with the lost past should be central to their everyday cultural portrayal. Nostalgia must not be treated as a symptom of the Withering: it is an established characteristic of Arhîn culture in its own right.

This information does **not** establish whether the memories are first-hand recollections, preserved Aurësén records, inherited/collective memories or magically constructed representations. The precise mechanism, historical accuracy, and whether outsiders can experience the trance remain open. The practice does not, by itself, establish worship of the runes or any protective effect against the Withering.

### Public-space and visual direction — Proposed

- Incorporate runic scriptures into **ordinary shared public spaces**—for example, along squares, galleries, passages or sheltered gathering areas—rather than reserving them exclusively for temples or elite libraries.
- Present the scriptures as carefully authored **legible patterned surfaces**: inset stone or metal panels, engraved walls or freestanding tablets are possible forms, not yet fixed designs.
- Arrange some public spaces to allow people to pause and contemplate the patterns without obstructing circulation. These places may form quiet counterpoints to Arhîn towers, markets and workshops.
- Use restrained ambient effects around the scriptures; the primary observable behaviour should remain an Arhîn character **quietly gazing at the runes**. A large holographic projection visible to every bystander would undermine the established outsider's perspective.
- Depict the experience through still poses, subtle shifts in posture and sustained attention rather than treating every trance as a dramatic magical ceremony.
- If memory-viewing scenes are ever visualised to the player, their form and narrative reliability require a separate approved presentation design.

**Design distinction:** these runic scriptures are a **widespread cultural medium for memory viewing**. Their relationship to temples, Tiandre, the Grey Ones, magic-sourced protection and the three-caste hierarchy is not yet established.

## 2.4 The Withering — Established

The Withering is a consequence of the loss of the Gift of Light. **Prolonged withdrawal from sunlight or magical sources of power** causes:
1. increasingly deep negative and violent emotions;
2. worsening emotional instability and, eventually, physical degradation;
3. skin becoming progressively **transparent**;
4. the body disappearing altogether, leaving a **violent, hysterical shadow**.

The Withering is not ordinary ageing. The Arhîn's functional immortality does not protect them from this fate.

No exposure duration, severity thresholds, curability, reversal rules, rate of progression or gameplay debuffs are established here. Ordinary visible light is **not** automatically equivalent to a magical source of power; the distinction must be respected until clarified.

### Withering character and VFX direction — Proposed

Use several visually understandable **conceptual states**, not a fixed gameplay staging system:

- **Unaffected:** physically present, recognisable Arhîn body and normal material response.
- **Emotionally affected:** movement, pose, expression and behaviour become increasingly troubled or threatening; avoid implying a compulsory visible skin change at the first sign of distress.
- **Physically degrading:** skin opacity progressively lessens; transparent skin and layered internal silhouettes should communicate material disappearance, not merely desaturation or a generic undead appearance.
- **Fully Withered:** a barely corporeal, violent shadow. Readable gesture and silhouette matter more than particle density or graphic distortion.

VFX should feel uncanny and tragic rather than casually demonic. Whether a Withered shadow retains its identity, can recover or can be destroyed remains unresolved.

## 2.5 The Three-Caste Society — Established

Arhîn society is divided into **exactly three castes** as currently defined:

| Caste | Established social description | Spatial and art implication |
|---|---|---|
| **I — Intelligentsia** | Highest of the three castes | Power, education and privileged access to sunlight; monumental elevated spaces are particularly associated with the wealthy and powerful |
| **II — Craftsmen, traders and tutors** | Intermediate caste | Professional workshops, teaching and commercial functions; dependants of the caste above |
| **III — All others** | Lowest caste | Domestic, service and other unclassified work; dependants of the intermediate caste |

The **lower castes are invariably indentured servants of those in the caste above them**, living **under their protection** and **on the ground floors of their masters' abodes**.

The system is not simply a distinction of wealth: control of shelter and access to protection is woven into social dependence. Its architecture should depict the imbalance without portraying indenture as an uncomplicated luxury.

**Not established:** how individuals enter or leave a caste, the legal terms of indenture, inheritance rules, whether all intelligentsia are nobles or spellcasters, which offices the castes may hold or how duties work between families.

### Vertical social organisation — Proposed spatial interpretation

- At the top: large luminous rooms, open views, glass walls, domes and private terraces where privileged inhabitants enjoy direct solar exposure.
- In the middle: workspaces, teaching halls, commercial galleries, stairs and controlled passageways.
- At the base: sheltered communal and service spaces, retained within the master's structure and receiving indirect light or supervised magical protection when necessary.

The ground floor is specifically established as the place where lower-caste dependants dwell; the above hierarchy describes an **art direction** for how such a building could communicate that fact, not a mandatory floor-by-floor legal plan.

**Critical tension:** protection may be genuine and necessary, but is also a form of power exercised over dependants. Residential design must convey both.

## 2.6 Government — Established

The Arhîn are ruled by **Queen Ilya**, who is also **High Priestess of Tiandre**.

Queen Ilya rules from the mystical **Suntower**, a structure so intensely bright that it **cannot be looked upon without protection**.

The game faction definition identifies the Arhîn dominion as **Culosimlond**, on the shores of the **Culosíma**, and describes the queen as reclusive. It also describes high nobles obeying her commands. The name **Culosimlond** is therefore an existing in-game faction name, not a newly devised term.

The precise relationship between royal rule, the caste system, high nobles, temples and scholarly institutions remains unestablished. Do not replace the three castes with a hereditary noble-house structure modelled on the Eldani.

## 2.7 Religion — Tiandre and the Grey Ones — Established

**Tiandre** is the **Queen of the Summer Court** and **one of the Grey Ones**.

Queen Ilya is her High Priestess. **Other Grey Ones are also revered in various capacities**.

The presence of several revered beings should be reflected in religious diversity within Arhîn visual culture. It does **not** yet establish a formal pantheon hierarchy, unified priesthood, precise doctrine, or claims about which Grey Ones are deities.

### Proposed sacred visual language

- Tiandre-associated spaces: warm radiance, open-to-sky sanctuaries, carefully directed sunbeams, elevated ritual spaces and solar-responsive architecture.
- Court symbolism: elegant repetitive radial forms, seasonal motifs and formal processional layouts; their final designs are undefined.
- Other Grey Ones: distinct symbolic treatments can exist within the same cultural design language, but **their identities, names, domains, symbols and relative importance are not established**.
- Sacred spaces should feel intensely magical without resorting to unbounded levitation, excessive glowing lines or a single generic golden symbol on every building.

**Do not assume** that Tiandre directly grants immunity to the Withering, that the Suntower is powered by Tiandre, or that devotion is required to control magic. Those relationships are open.

## 2.8 The Suntower — Established / Proposed

**Established:** The Suntower is Queen Ilya's seat and is **blindingly bright**, such that it cannot be safely looked upon without protection.

**Proposed landmark design:**
- a very tall, unmistakable vertical silhouette rising above the surrounding canopy and ordinary Arhîn structures;
- a monumental domed or faceted summit, supported by a physically convincing structural framework;
- luminous glass and reflective or refractive architectural elements, with a strong single light source or concentrated glow rather than arbitrary emission across every surface;
- elevated terraces and long vertical supports that convey royal distance from the world below;
- surrounding architecture whose sightlines and shading acknowledge the tower's overwhelming brilliance.

The Suntower's supernatural visual effect should be presented with a scalable combination of material emission, local lighting, atmospheric scattering and restrained camera effects. The camera must remain playable; the fiction of impossible direct sight should not automatically require permanently obscuring the game view.

**Unresolved:** exact shape, source of radiance, whether it is the tallest building, material composition, the nature of its protective viewing devices, and the relation between its brightness and the Withering.

## 2.9 Geography — The Three Great Forests — Established

The Arhîn primarily inhabit three Great Forests around the **Culosíma**:

| Forest | Established meaning |
|---|---|
| **Arataure** | **Day Forest** |
| **Mortaure** | **Dusk Forest** |
| **Histaure** | **Dawn Forest** |

These are names and cultural regions; they do **not** necessarily imply permanent day, dusk or dawn lighting cycles.

### Regional visual differentiation — Proposed

**Arataure — Day Forest:** favour open, high canopy spaces, bright sun shafts, pale masonry, clear glass and warm golden-metal detailing. Towers can appear confidently sun-facing.

**Mortaure — Dusk Forest:** favour denser shade, copper or amber glazing, contrasting pockets of magic-sourced light and more visible tension between shelter and exposure.

**Histaure — Dawn Forest:** favour mist-filtered light, cool pale stone, opalescent glass and delicate luminous accents amid dense greenery.

These differences are provisional environmental motifs, not claims about biome, chronology, magical mechanics or terrain maps. All three should retain the same Arhîn construction vocabulary.

### Forest integration — Proposed

Tower crowns and domes should emerge **above or through the canopy**, where exposure is better. Stable masonry or other engineered foundations, supported terraces, narrow vertical building profiles and elevated connections make the architecture plausible in a forest environment.

Do not turn Arhîn settlements into a uniform network of treehouses. Large trunks, roots and living vegetation may shape streets and sightlines, but the architectural identity is **light-seeking towers**, not trees as buildings.

Controlled canopy gaps, courtyards and shafts of light can tell a social and ecological story without implying that every tree is magically altered.

## 2.10 Yer-Aranie — Established / Proposed

**Established:** **Yer-Aranie** was a great Aurësén city, inhabited before its destruction in the catastrophic conclusion of the Serpent War.

**Established comparative constraint:** Aurësén civilisation reached an architectural peak that present Arhîn builders generally cannot match; the loss of many original master craftsmen and designers, both in the Serpent War and subsequently to the Withering, must remain apparent in how the ages are compared.

**Proposed art direction:** its ruins should feel **older and architecturally distinct** from contemporary Arhîn tower settlements. They may contain monumental public plazas, broken light-gathering structures, ruined transparent enclosures and remnants of an unusually confident civilisation from before the Gift was lost.

The contrast should be archaeological rather than a simple colour swap. The Aurësén were the ancestors of the Arhîn, but their built world need not have been organised around the same desperate need for sunlight.

**Unresolved:** the city's original plan, geography, size, surviving materials, precise destruction mechanism and whether its ruins remain accessible. No assertion of floating buildings, technological superiority or a specific magical cause of destruction is implied.

## 2.11 Architectural Design Language — Proposed

**Central architectural principle:** **light is both a survival resource and a visible instrument of social hierarchy**.

The wealthiest and most powerful Arhîn **live in high towers with glass walls and domed roofs** to maximise exposure to sunlight. This is established and must anchor the entire asset language.

Suggested supporting design grammar:

| Asset family | Visual direction |
|---|---|
| Privileged residences | Slender, tall towers with glass-wall panels, domed roofs and light-facing chambers; **genuinely opulent architectural treatment is restricted to the highest rulers**, not every wealthy inhabitant |
| Structural frames | Elegant but functional stone, metal or composite ribs and mullions; legible beams, joints and supports |
| Shared residential levels | Low, robust foundations; service halls, sheltered galleries and domestic rooms at ground level |
| Work and teaching spaces | Compact workshops, arcaded galleries, educational halls and trade rooms attached to larger residences |
| Civic and sacred buildings | Taller ceremonial halls, luminous domes, processional stairs, vaults and colonnades |
| Forest infrastructure | Raised connections, bridge sections, retaining structures, clearings, stair towers and sheltered ground paths |
| Defensive elements | Defensive outer shells, overlapping sloped bastions, strongly controlled gates and protected open-to-sun inner spaces, with glazing concentrated away from directly vulnerable outer faces |
| Ancient Aurësén ruins | Monumental remains with related but earlier shape language, distinct from survival-driven modern towers |

### A millennium of diminished craftsmanship — Established and proposed visual translation

**Established:** Arhîn society is living through a long architectural decline. Important Aurësén designers and craftsmen were killed during the Serpent War or subsequently lost to the Withering. Therefore even splendid contemporary construction should be recognisably **less ambitious and less accomplished than the greatest Aurësén work**. Only the highest rulers still possess truly opulent residences.

**Approved cultural art-direction implication:** do not make all Arhîn buildings pristine, extravagantly glazed palaces. A typical tower may remain elegant and expensive by Eldani standards, but its construction should show simplified profiles, repeated stock components, imperfect replacements, restrained ornament and older exceptional elements selectively preserved within later work. A very few royal or supreme-ruler precincts may retain outstanding luxury.

**Proposed visual methods:** a broken rhythm of replaced dome panes; a finely carved ancient colonnade connected to a comparatively plain later gallery; patched metal mullions; different masonry epochs; abandoned ornamental recesses now filled with practical stone; smaller and more repetitive modern arches set beside far finer surviving Aurësén details. Such contrasts should emerge from coherent construction history, not random damage decals.

Decline is **not** general collapse, technological primitiveness or universal poverty. The Arhîn are still sophisticated, intensely magical and capable of maintaining formidable defences and their defining glazed towers.

### Geometry

Preferred silhouettes: elongated vertical stems, restrained tapering, large domes or lantern-like crowns, gently segmented curved elements and recognisable translucent wall areas. Broad flat glass skyscraper panels and science-fiction curtain-wall grids are not the intended character.

The structure beneath glass must have an intelligible load path. Larger glass fields should have appropriately sized frames and repeatable panel dimensions. Magical reinforcement may be depicted only as a proposal until its existence and form are established.

### Contrast with Eldani architecture

The Eldani emphasise masonry mass, defensible enclosures, steep rooflines, heraldic fortifications and historical accretion. The Arhîn should emphasise slender **verticality, glazing, domes, filtered light and dependence on controlled access**. Their cities must be recognisable by silhouette even when stripped of banners or surface ornament.

### Light and caste

The brightest and most private spaces belong to the most privileged. The deeper living levels should reveal the need for magical support where direct sun is scarce. This contrast is crucial; it should not be flattened by making every interior equally luminous.

Magical illumination can be visualised through controlled light wells, enclosed luminaires, protected chambers or conduits, **but the devices and their energy systems remain proposals**. A normal lantern must not silently become an approved replacement for sunlight or magical sources of power.

### Fortress archetype: the Sunward Redoubt — Approved concept direction

**Status:** approved **cultural fortress concept**, not a claim that every Arhîn fortress has this exact plan, name, origin, size or magical mechanism.

The fortress has a deliberately defensive silhouette unlike an Eldani castle or an uninterrupted Aurësén pleasure-palace: **a heavy protective outer shell wrapped around a delicate, light-filled architectural heart**.

- **Petal-shaped defensive shell:** massive, sloping, overlapping stone bastions arranged radially around the stronghold. Interlocking faces and flanking approaches make the perimeter read as a functional defensive work rather than an ornamental wall. Number of bastions and exact military engineering remain open.
- **Defended open-air solar court:** a substantial sun-exposed inner courtyard protected by surrounding walls and gateways. Maintaining access to daylight is a guiding architectural objective even in wartime.
- **One exceptional ancient core:** a distinguished **glazed, domed central tower**, visually representing surviving greater Aurësén workmanship. This is a *concept choice for this example fortress*, not a universal rule that all contemporary fortresses contain an Aurësén tower.
- **Sun-catching reflective arrays:** large, visibly angled bronze or other reflective panels suggested for redirecting sunlight into the fortress's sheltered spaces; they must have readable supports and adjustable or plausible fixed orientations. Their actual biological efficacy, whether magical assistance is needed and the underlying technology are **not established**.
- **Diminished contemporary outer construction:** later bastions, galleries and service ranges use larger, plainer pieces, reused elements, replaced glazing and conspicuous but purposeful repairs. The outer fortress should communicate a civilisation maintaining inherited magnificence rather than duplicating its ancestral peak.
- **Vertical caste contrast:** prestigious sunlit rooms and outlooks occupy the upper central precinct, while practical garrison, workshops and dependent residences occupy less luxurious lower levels. Ground-floor habitation by indentured dependants follows the established caste structure.
- **Site relationship:** an elevated forest-edge or rocky approach is a plausible setting for this fortress **concept**, but cliffs, waterfalls and other incidental features in exploratory illustrations are not required cultural canon.

**Explicit exclusions:** a sprawling open palace complex; a uniform skyline of equally sumptuous glass towers; unprotected perimeter walls built primarily of fragile curtain glazing; arbitrary floating architecture; default sunburst flags, purple livery or religious emblems copied from exploratory imagery without separate heraldic approval.

The image-concept composition and any presentation-board framing are **not** approved world assets. Actual game-asset references must follow the [Graphical Approach PDD](../../Graphical-Approach-PDD.md): one isolated complete three-quarter-view modelling reference against a plain neutral background unless another format is expressly requested.

## 2.12 Materials and Colour — Proposed

Suggested material palette for concepting; **not approved material/shader values**:

| Use | Indicative colour / response |
|---|---|
| Primary pale structural stone | Warm ivory-grey **#D5D0BE** |
| Secondary stone or aged ceramic | Cool pale grey **#A9B3B0** |
| Framing metal | Soft aged gold / brass **#BCA16A** |
| Ordinary glazing | Desaturated clear-green / pale aqua **#BBD7CF** |
| Magical luminous accent | Warm amber **#F1CB72** |
| Deep lower-level material | Slate-teal **#465F61** |
| Dusk/regional accent | Muted smoky plum **#675C77** |

Avoid making every surface gold, white and glowing. The strongest contrast should be between physically convincing daylight on upper levels and relatively deep, materially grounded shade in the lower parts of settlements.

Glass should appear as **glass** rather than unlit aquamarine stone or holographic energy barriers. Specular response, frame silhouettes, occasional transmission and reflected environment matter more than extreme transparency.

The [Graphical Approach PDD](../../Graphical-Approach-PDD.md) remains authoritative for physical material scale, normal-detail frequency, shared wear and final in-engine appearance.

## 2.13 Heraldry and Ornament — Established / Proposed

**Established:** No specific Arhîn royal banner, caste device, state insignia, symbol of Tiandre or icon for any Grey One is defined by the present brief.

**Proposed motif families:**
- repeated curved dome ribs, solar arcs and refracted light patterns;
- restrained concentric and radial geometry;
- architectural tracery echoing framed glass panels;
- formal representations of seasonal light and courts.

Symbols of Tiandre, Queen Ilya, Culosimlond and individual caste institutions should be treated as **distinct, unresolved** identities. Their art must not invent a single mandatory sun emblem and assign it to all of them.

The name **Grey Ones** does not itself imply that their symbols, bodies or architecture are grey.

## 2.14 Clothing and Physical Presentation — Proposed

The Arhîn need a recognisable humanoid visual design, but neither facial anatomy nor canonical clothing has been specified. Do not silently settle ear shape, eye colour, skin tone, body proportions or how functional immortality affects appearance.

Potential costume language:
- **Intelligentsia / powerful:** long, carefully cut layered garments, light-catching surfaces, translucent over-layers, refined metal ornaments and formal silhouettes that prioritise upper-body visibility in lit spaces.
- **Craftspeople, traders and tutors:** precise, practical tailoring; tool and trade identifiers; hard-wearing fabrics suitable for workshops, commerce and instruction.
- **All others:** durable, repaired and less ornamented workwear appropriate to ground-level domestic and service life.

Caste distinction should be readable through silhouette, quality, cut, accessories and environmental context, not merely three fixed colours. Indenture is a social structure, not a reason to make every lower-caste character visually identical.

Proposed military equipment may use slender but plausible protection, curved or finely proportioned weapon forms, glass-derived motifs in ornament and compact symbolic devices. Avoid compulsory glowing crystals, nonfunctional oversized armour or claims that every Arhîn is a spellcaster.

## 2.15 Magic and Everyday Life — Established / Proposed

**Established:** Arhîn society is intensely magical; magical sources of power can protect against withdrawal that would otherwise cause the Withering. The Arhîn do not naturally possess godlike powers.

**Proposed environmental storytelling:**
- public runic scriptures and apparently motionless Arhîn viewers, providing recognisable daily-life scenes of memory contemplation;
- supervised rooms, wells or fixtures that visibly provide magical refuge where sunlight cannot reach;
- upper-caste-controlled access to particular bright or magically protected areas;
- workshops and scholarly halls with precise, purposeful magical equipment;
- sheltered movement routes that make long residence in deep forest environments socially and logistically plausible.

Do not assume constant free energy, universal magical illumination, flying towers, unlimited teleportation, immortality via resurrection or any defined combat ability. These belong to other design systems and may require later lore decisions.

## 2.16 Historical Layering — Established history / proposed visual translation

Representative sites may distinguish:
1. **Aurësén inheritance:** the surviving works and remnants of an older architectural peak, potentially exhibiting craftsmanship no longer reproducible by most current builders;
2. **The Serpent War's aftermath:** destruction, depopulation and loss of master artisans and designers, with further loss across centuries to the Withering;
3. **The current Arhîn adaptation:** architecture reorganised to favour sunlight and protected magical access, with reduced technical ambition and greater reliance on simpler, reusable building parts;
4. **Present-day selective splendour and maintenance:** repaired glazing, replaced ribs, mismatched masonry, adapted older structures, dependent ground-floor residences, and exceptional opulence retained only by the highest rulers.

These are visual strata, not a formally approved chronology, and not all settlements need contain every layer.

## 2.17 Production and Reuse Constraints

Arhîn components must follow the [Graphical Approach PDD](../../Graphical-Approach-PDD.md): material approval first, **atomic modular architectural parts**, meaningful medium-scale geometry, reusable textures and masks, controlled runtime weathering, scalable LODs and gameplay-distance validation.

The glass-heavy identity introduces additional constraints:
- Prefer an intelligible set of reusable glass-panel dimensions, mullions, ribs, dome segments, cornices and supporting walls.
- Large glass walls and dome shapes must read in silhouette without requiring every panel to be fully transparent.
- URP transparency, double-sided surfaces, sorting, overdraw and reflections must be budgeted; use opaque or selectively translucent treatments when the appearance allows.
- Material emission should not substitute for actual playable illumination or guarantee high exposure for lower-caste residents.
- Weathering, grime, moss and ageing must be controlled through reusable materials and shader profiles, not unique geometry per building.
- Distinct regional and caste variations should primarily reuse approved construction kits and change material treatment, detailing and arrangement.

### Suggested atomic asset families

| Family | Reusable atomic components | Potential variations |
|---|---|---|
| Tower structure | Base walls, columns, core shaft segments, braces, galleries, stairs | Caste, wealth, region, size |
| Glazing | Rectangular/curved panels, framed windows, mullions, arched inserts | Clear, tinted, worn, damaged |
| Domes | Radial ribs, roof panels, apex caps, lanterns, ring supports | Scale, material, translucent fraction |
| Ground service level | Entrances, workshops, rooms, corridors, supports, partitions | Residential, teaching, trade, service |
| Bridges and forest adaptation | Supports, railings, walking surfaces, attachment sockets | Canopy crossing, ground crossing, sacred |
| Light and ritual props | Brackets, sockets, housings, ceremonial furniture | Religious, civic, residential |
| Public memory scriptures | Engraved rune-pattern panels, wall insets, freestanding supports and gathering-space assemblies | Location, scale, age, approved rune families |
| Ruins | Broken ribs, displaced panels, fractured foundations, collapsed dome pieces | Ancient Aurësén, later Arhîn |
| Fortress outer shell | Sloping petal-bastion segments, flanking walls, gate throat modules, coping and repair blocks | Radial layout, attack-facing elements, simplified later construction |
| Solar courtyard and reflectors | Ground paving, perimeter galleries, reflector panels, mount frames, support brackets | Court dimensions, reflection direction, material age |

These are potential component families, **not** an instruction to create them before art approval.

## 2.18 Arhîn Art Acceptance Criteria

An Arhîn concept, building or scene should:
1. Read as Arhîn from its **tall glazed, domed construction and light-seeking silhouette**, not simply from leaf motifs or glowing ornaments.
2. Express the relationship between **upper-level privilege** and **ground-floor dependence**.
3. Convey a magical but physically grounded civilisation that can plausibly exist within great forests.
4. Preserve the distinction between sunlight, magical power and decorative lights.
5. Show the Withering as progressively corporeal disappearance, not default dark-elf or undead makeup.
6. Keep Queen Ilya, Tiandre, the Grey Ones and the Suntower visually meaningful without inventing undocumented religious symbols.
7. Remain distinct from Eldani masonry-and-heraldry architecture.
8. Meet grounded mid-poly, modularity, material and performance requirements at MMO camera distances.
9. Allow Arataure, Mortaure and Histaure variants to appear related without relying on arbitrary permanent times of day.
10. Represent public runic scripture viewing as a commonplace, inward-looking practice: Arhîn viewers may seem simply to stare at patterns to outsiders.
11. Contemporary grandeur should read as **splendour in decline** relative to Aurësén excellence, with true opulence restricted to the highest rulers.
12. A Sunward Redoubt-style fortress should read as a **defensible shell around a protected sunlit court**, retaining one exceptional ancient dome while showing simpler repaired later construction.

---

# 3. Further Consideration Required

## 3.1 Terminology and Legacy Game Data

**Documented discrepancy:** The current game race definition glosses **Arhîn** as **"high child[ren]"**, whereas the present cultural brief specifies **"Dawn Children"**. The brief's wording is recorded here as the current intended meaning; the asset's description must be reconciled separately if that meaning is confirmed.

The existing race definition uses **Annokal** and **Terhat-Ilu**. The existing faction definition spells its display name **Culosimlond**, but uses **Culosmlond** in one description sentence. Both require terminology review before mass use.

The race/faction assets currently omit accents in display names (**Arhin**, **Auresen**, **Culosima**) that appear in the cultural text (**Arhîn**, **Aurësén**, **Culosíma**). Choose consistent display and localisation conventions rather than changing IDs or serialized assets as an incidental art task.

## 3.2 History, Geography and Aurësén

- Origin and function of the Gift of Light.
- Exact timeline, consequences and participants of the Serpent War.
- Relationship of the historical Aurësén Diarchy to today's Arhîn dominion.
- Geography, construction, destruction and remaining ruins of Yer-Aranie.
- Exact arrangement and political differences of the three Great Forests.
- Nature and size of Culosíma and of Culosimlond.

## 3.3 The Withering and Light

- Minimum exposure frequency and precise environmental requirements.
- Whether magical light, magical energy, direct sun and reflected sunlight have different effects.
- Whether the process can be arrested or reversed, including its final shadow state.
- What a fully Withered shadow remembers, whether it can communicate and how it behaves.
- Visual differences between natural transparency and magical concealment.
- Relationship of physical immortality to injury, sickness and resurrection.
- Whether and how gameplay ever simulates these conditions.

## 3.4 Caste, Protection and Authority

- How caste membership, indenture and inheritance function.
- Whether and how dependants obtain protected light or magic at ground level.
- Laws, sanctions, mobility, employment and protection obligations.
- Relationship of intellectual, religious, noble and royal authority.
- Titles, institutions and identities other than Queen Ilya.

## 3.5 Religion and Heraldry

- Identities, aspects, symbols and ritual significance of the Grey Ones.
- Iconography and theology of Tiandre's Summer Court.
- Whether Tiandre's power is related to the Suntower or Withering.
- Personal, royal, factional and caste emblems.
- Culosimlond's formal heraldry and possible regional symbols.

## 3.6 Architecture and Character Art

- Final tower profiles and glass-to-masonry ratio.
- How widespread the Sunward Redoubt fortress pattern is, whether this is an actual named site or a concept archetype, and its military capabilities.
- Exact geometry and support design of overlapping bastions, solar court and reflective arrays; their physical/magical effectiveness.
- Which surviving Aurësén structures are original and which contemporary rulers can still maintain or commission exceptional opulence.
- Final dome geometry, structural materials and colour palette.
- Whether canopy-clearance and magical support influence settlement planning.
- Historical shape language of pre-War Aurësén versus contemporary Arhîn.
- Physiognomy and skin/eye/hair variations.
- Caste clothing, equipment and armour styles.
- Performance-approved glass and Withering shader approaches.

## 3.7 Nostalgia and Runic Memory Viewing

- Are the viewed memories personal, inherited, archived, collectively shared or reconstructed?
- What determines a scripture's subject, authenticity and association with specific Aurësén periods or individuals?
- Can non-Arhîn perceive the same memories, or do they see only the external trance?
- Are scriptures publicly accessible to all three castes, and are any viewed memories restricted by status or institution?
- What visual grammar, material, placement and maintenance rules define the runic patterns?
- How long do trances last, can they be interrupted, and are there any consequences beyond remembrance?
- Do the scriptures have any separate religious, political or protective-magic function, or are they solely for contemplation?
- How should any player-facing memory experience be conveyed without implying that bystanders see the memories?

No unresolved detail should be inferred as canon solely from a visual prototype.

---

# 4. Dependencies and Document Boundaries

- [Culture Design PDD](../Culture-Design-PDD.md): parent cultural index and cross-cultural constraints.
- [MMORPG Master PDD](../MMORPG-Master-PDD.md): design hierarchy and product boundaries.
- [Graphical Approach PDD](../../Graphical-Approach-PDD.md): mandatory art style, rendering, shaders, modular assets and production workflow.
- [World and Zone Design PDD](../World-and-Zone-Design-PDD.md): placement and terrain integration of the Great Forests, Culosíma, settlements and ruins.
- [Character Creation and Identity PDD](../Character-Creation-and-Identity-PDD.md): visual character creation and potential player-facing race rules.
- [Factions and Reputation PDD](../Factions-and-Reputation-PDD.md): Culosimlond faction gameplay and relationships.
- [Class Design PDD](../Class-Design-PDD.md): class abilities; intense cultural magic does not imply that every Arhîn has a specific class or divinity.
- [Quest, Narrative and Dialogue PDD](../Quest-Narrative-and-Dialogue-PDD.md): presentation of the Serpent War, Withering, castes and religious stories.
- [NPC and Creature Design PDD](../NPC-and-Creature-Design-PDD.md): future creature/NPC requirements of Withered shadows, if adopted.
- [Items, Equipment and Loot PDD](../Items-Equipment-and-Loot-PDD.md): equipment mechanics; this document specifies only cultural appearance.

---
