# Ninth Age — Live Content and Versioning Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Production release identity, client/server compatibility, network protocol versioning, gameplay-content revisioning, Addressables/CDN content delivery, asset-bundle architecture, hotfixes, persistent-data migrations, live-content activation, maintenance, rollback and release validation  
**Last updated:** 2026-10-31

---

## 1. Purpose and Authority

This document defines the production versioning and live-content model for **Ninth Age**.

It is authoritative for:

- how a production release is identified;
- client/server compatibility;
- network-protocol compatibility;
- gameplay-content revisions;
- Addressables catalog revisions;
- database-schema compatibility;
- hotfix eligibility;
- Addressables local versus remote ownership;
- AssetBundle grouping and mutability rules;
- content-update builds;
- CDN publication/activation;
- cache behaviour;
- stable content identity;
- live-event scheduling;
- database migration expectations;
- deployment/maintenance behaviour;
- release rollback;
- failure behaviour when compatibility cannot be proven.

This PDD does not prescribe:

- the CDN/vendor;
- the CI/CD provider;
- the launcher/storefront/distribution platform;
- exact cloud infrastructure;
- exact database migration framework implementation;
- exact bundle-size thresholds;
- exact maintenance duration;
- low-level deployment scripts.

Those are implementation/operations decisions so long as they preserve the rules in this document.

---

## 2. Central Principle

A production world runs against one explicitly identified, internally compatible release state.

> **Client binaries, server binaries, network protocol, gameplay content, Addressables catalogs and persistent database schema may have separate version identifiers, but they must be validated as one compatible release before a player enters the world.**

The production system must never infer compatibility from "probably close enough" versions.

---

## 3. Design Pillars

### 3.1 Compatibility is explicit

A client must know whether it is compatible with the world it is attempting to enter.

A server must know which content and database state it is running.

### 3.2 Content hotfixing is a first-class architecture

Balance data and presentation assets should be patchable without rebuilding executable code when the existing runtime already understands their schema.

### 3.3 Production artifacts are immutable

A published production revision is not edited in place.

A new change creates a new revision/artifact and the active release is advanced to it.

### 3.4 Small changes should produce small patches

Asset-bundle layout must be designed around update frequency, dependency structure and patch cost.

A one-line class balance change must not routinely invalidate hundreds of megabytes of unrelated assets.

### 3.5 Persistent state is never casually downgraded

Database/content rollback must respect state that may already have been written by a newer release.

### 3.6 Fail closed on incompatible authoritative state

If compatibility cannot be proven, the player does not enter the world and the server does not guess.

---

## 4. Existing Codebase Foundations

The current repository already provides foundations for this design:

- Unity Addressables is used by both client and server startup;
- Addressables groups exist for World Chunks, Creatures, UI, VFX and SFX;
- those groups have Content Update schemas;
- mutable groups generally use `m_StaticContent: 0`;
- `Assets/AddressableAssetsData/Windows/addressables_content_state.bin` exists;
- Development, Production and Local Addressables profiles exist;
- profile variables exist for `Remote.BuildPath` and `Remote.LoadPath`;
- `PlayerSettings.bundleVersion` currently provides the client build version and is currently `0.1.0`;
- gameplay definitions are predominantly ScriptableObject-based `DataDefinition` assets with stable `DefinitionId` values.

These are foundations to complete rather than replace with an unrelated patching system.

---

## 5. Existing Implementation Gaps

The production live-content path is not yet complete.

Current gaps include:

- remote catalog building is disabled;
- Unity CCD integration is disabled;
- current Addressable groups still resolve through local build/load paths;
- no production CDN/load URL is configured;
- there is no runtime catalog-update/download flow;
- there is no release manifest;
- there is no explicit network protocol version;
- authentication currently carries credentials only;
- there is no client/server compatibility handshake;
- several gameplay definition libraries are loaded through `Resources.Load`;
- those library assets hold direct serialized references to authoritative definitions;
- no production content revision is pinned by the server;
- no release pipeline validates expected patch/download size.

These are implementation gaps relative to this PDD.

---

# 6. Production Release Identity

## 6.1 Release State

Every production deployment has one immutable **Release State**.

A Release State identifies the exact combination of executable, content and schema versions expected to operate together.

---

## 6.2 Required Version Axes

At minimum, Ninth Age tracks separately:

| Identifier | Purpose |
|---|---|
| **Client Build Version** | Human-facing executable release version |
| **Server Build ID** | Exact deployed server build/revision |
| **Network Protocol Version** | Compatibility version for RPC/network serialization |
| **Gameplay Content Revision** | Monotonic revision of authoritative gameplay definitions |
| **Addressables Catalog Revision(s)** | Exact catalog/hash per relevant platform/build target |
| **Database Schema Version** | Applied relational migration state |
| **Release ID** | Immutable identifier for the complete production release state |
| **Build/Commit ID** | Diagnostic source revision |

One number must not be overloaded to represent all of these concepts.

---

## 7. Client Build Version

The client uses a human-readable version in the form:

```text
MAJOR.MINOR.PATCH
```

Examples:

```text
0.6.0
0.6.1
1.0.0
```

The existing Unity `PlayerSettings.bundleVersion` is an appropriate source for this value.

The client build version is not, by itself, the network compatibility algorithm.

---

## 8. Build / Commit ID

Production client/server builds should record the exact source/build identity, normally including the Git commit SHA or equivalent immutable build identifier.

This is diagnostic metadata used for:

- bug reproduction;
- support;
- release auditing;
- deployment verification.

It does not replace the explicit protocol/content/schema versions.

---

## 9. Network Protocol Version

The network layer has an explicit integer **Protocol Version**.

The protocol version changes when compatibility may be broken by changes such as:

- RPC signatures;
- RPC identifiers;
- serialization layout;
- network DTO fields/semantics;
- connection/authentication protocol;
- networked prefab/component expectations;
- other PurrNet-facing compatibility changes.

---

## 10. Protocol Compatibility Rule

The default production rule is:

> **Client and server Protocol Version must match exactly.**

A compatibility range/allowlist may be introduced only when that mixed-version behaviour is explicitly implemented and tested.

Semantic client version similarity does not imply protocol compatibility.

---

## 11. Gameplay Content Revision

Authoritative gameplay data has a monotonically increasing **Gameplay Content Revision**.

Examples of authoritative gameplay content include:

- classes;
- class base stats/growth;
- spells/abilities;
- talents;
- actor stats;
- items;
- crafting recipes;
- loot definitions;
- NPC/creature definitions;
- quests where data affects authoritative state;
- faction/reputation definitions;
- combat/balance data;
- other server-read gameplay definitions.

Once a revision has been published to Production, that revision number is never reused for different content.

---

## 12. Content Manifest Hash

The authoritative gameplay-content set should also have a generated manifest/hash.

The revision provides an operationally readable identity.

The hash verifies that a given revision actually contains the expected definitions.

Conceptually:

```text
GameplayContentRevision: 185
GameplayContentManifestHash: <hash>
```

---

## 13. Addressables Catalog Revision

Addressables catalogs have explicit immutable release identity.

The Release State records the exact catalog revision/hash expected for each relevant build target/platform.

Catalog revision is distinct from Gameplay Content Revision because a presentation-only asset update may change:

- textures;
- VFX;
- SFX;
- music;
- UI art;

without changing authoritative gameplay data.

---

## 14. Database Schema Version

The persistence database has an explicit ordered schema/migration state.

The server build declares which schema state it expects.

The server must not operate against an incompatible database schema.

---

# 15. Release Manifest

## 15.1 Exact manifest contract

Every candidate or active Release State is represented by one immutable manifest with schema version **1**.

The authored C# contract uses the project's normal uppercase-ID convention:

```text
ManifestVersion
ReleaseID
Environment
ClientBuildVersion
ClientBuildID
ServerBuildID
ProtocolVersion
GameplayContentRevision
GameplayContentManifestHash
AddressablesCatalogs[]
DatabaseSchemaVersion
CreatedAtUTC
```

Each `AddressablesCatalogs` entry contains:

```text
BuildTarget
CatalogRevision
CatalogHash
RemoteLoadPath
ContentStateSHA256
```

The initial supported build target is `StandaloneWindows64`.

Field rules are exact:

- `ManifestVersion` is the integer `1`.
- `ReleaseID` is a nonempty GUID generated once for that manifest. Its serialized string form is canonical lowercase GUID `D` format. A published `ReleaseID` is never reused.
- `Environment` is exactly one of `Development`, `Staging` or `Production`.
- `ClientBuildVersion` is the Unity `PlayerSettings.bundleVersion` value and must be `MAJOR.MINOR.PATCH`.
- `ClientBuildID` and `ServerBuildID` are immutable source/build identities. Repository-produced promoted builds use the exact Git commit SHA; client and World Server remain separate fields even when they were built from the same commit.
- `ProtocolVersion` is a positive 32-bit integer. Initial production-compatible protocol identity starts at `1`; breaking protocol changes increment it and exact equality remains the default compatibility rule.
- `GameplayContentRevision` is a positive signed 64-bit integer. Published Production revisions increase monotonically and are never reused for different authoritative gameplay content.
- `GameplayContentManifestHash` is the lowercase 64-character SHA-256 hex digest of the canonical authoritative gameplay-definition manifest for that revision.
- `AddressablesCatalogs` contains exactly one entry per supported client build target and no duplicate `BuildTarget`.
- `CatalogRevision` is a positive signed 64-bit integer, monotonically advanced for a changed published catalog of that build target and never reused for different Production catalog contents.
- `CatalogHash` is the exact Addressables-generated catalog hash token associated with the catalog artifact; it is compared as an opaque ordinal string rather than reinterpreted as a project-defined hash algorithm.
- `RemoteLoadPath` is the exact remote Addressables root for the immutable content release. For Unity CCD release manifests it uses the release-specific `/releases/{releaseID}/entry_by_path/content/?path=` form, not a movable `release_by_badge/latest` path.
- `ContentStateSHA256` is the lowercase 64-character SHA-256 digest of the exact `addressables_content_state.bin` retained for that build target/release baseline.
- `DatabaseSchemaVersion` is the exact latest required EF Core migration ID for the Master persistence schema. It is not an invented parallel integer.
- `CreatedAtUTC` is a UTC `DateTimeOffset` identifying manifest creation time and is immutable.

The manifest contains no password, bearer secret, player/account identity, mutable health state, current World Server run identity or mutable activation timestamp.

## 15.2 Master-owned active Release State

The immutable manifest and the **active-release pointer** are separate concepts.

The Master Server owns the active Release State used for compatibility decisions. For Stage 1.9 the active manifest is selected through explicit deployment/startup configuration and loaded once when the Master starts. Changing the active release therefore requires a controlled Master restart/deployment; runtime online manifest switching is not introduced in Phase 1.

Conceptually:

```text
Configured active manifest
→ Master validates it once at startup
→ Master owns active ReleaseID for that process lifetime
→ World Server registration must match that active release
→ accepted World Server run is pinned to that ReleaseID
→ client compatibility is validated against the World Server's pinned release
```

The Master must never discover an active release by asking CCD for `latest`.

The client and World Server must never independently choose whichever catalog/content revision is newest.

## 15.3 Environment and promotion rule

A Release Manifest identifies one exact environment-specific release state because the immutable CCD release URL is environment/bucket specific.

Promotion therefore produces a new immutable manifest/ReleaseID for the target environment while preserving any unchanged executable, protocol, gameplay-content and schema identities. The target manifest records the target environment's immutable CCD release URL.

Development may use CCD's movable `latest` badge for rapid manual iteration outside the formal pinned-release path. A manifest used to validate Stage 1.9 release compatibility must use the immutable release-specific URL.

## 15.4 Manifest validation and failure semantics

A manifest is invalid and the Master must fail startup before serving normal World/player traffic when:

- any required field is missing or malformed;
- `ReleaseID` is empty;
- the environment does not match the Master's configured hosting environment;
- build/version identifiers are blank;
- protocol/content/catalog revisions are nonpositive;
- gameplay/content-state SHA-256 strings are not lowercase 64-character hexadecimal;
- build-target entries are duplicated;
- a required build target is absent;
- the Addressables remote path is not an absolute URI;
- Staging/Production remote content is not HTTPS;
- a Unity CCD manifest uses a movable badge path instead of a release-specific path;
- `DatabaseSchemaVersion` is blank;
- the actual Master database schema does not equal the manifest's required schema version.

No fallback manifest, previous release, `latest` lookup or inferred compatibility is permitted.

## 15.5 Release Manifest Authority

The Release Manifest defines the compatible executable/content/schema combination. The active-release selection defines which immutable manifest is authoritative for the running environment.

A newer CCD release or manifest may already exist but remains inactive until deployment configuration selects it and the Master starts successfully against it.

# 16. Compatibility Handshake

## 16.1 Compatibility Before World Entry

Compatibility validation must occur before a player enters gameplay.

The preferred architecture performs compatibility negotiation before normal authentication/world entry.

The current credential-only authentication payload must therefore be extended or preceded by a distinct compatibility handshake.

---

## 16.2 Client Compatibility Data

The client must be able to present at least:

- Client Build Version;
- Protocol Version;
- Gameplay Content Revision;
- expected/loaded Addressables Catalog Revision;
- Build ID where useful diagnostically.

---

## 16.3 Server Validation

The server validates the client against the active Release State.

Incompatibility must produce a specific reason rather than a generic authentication failure.

Examples include:

- mandatory client update required;
- protocol mismatch;
- content update required;
- content verification failure;
- server maintenance;
- server version unavailable.

---

## 17. Required Client Build

Production initially uses one required client build per active Release State unless an explicit compatibility allowlist has been defined.

A content-only hotfix may keep the same client build while advancing content/catalog revisions.

---

## 18. Required Gameplay Content

A player may not enter an authoritative production world using a stale Gameplay Content Revision.

This prevents situations where:

- server Fireball damage is one value;
- client tooltip/data is another;
- server class growth differs from the client representation;
- item/quest definitions disagree.

---

# 19. Addressables as the Live-Content System

Unity Addressables Content Update is the canonical client-content hotfix mechanism.

Ninth Age should complete the existing Addressables architecture rather than introduce a second independent patching system.

The system must support:

- baseline player builds;
- preserved production content-state files;
- content-update builds;
- remote catalogs;
- remote bundles;
- local bundle caching;
- catalog revisioning;
- changed-bundle download only.

---

## 20. Content Update Granularity

Addressables provides **bundle-level** update granularity.

It must not be treated as binary-delta patching inside an AssetBundle.

If one asset changes in a mutable bundle, that bundle may need to be rebuilt/downloaded in full.

Therefore:

> **Bundle architecture directly controls patch size.**

---

# 21. Local Versus Remote Content

## 21.1 Local Bootstrap Content

The installed client must contain enough local content to:

- start the executable;
- show mandatory update/maintenance/error UI;
- discover/fetch the active Release Manifest;
- initialise the content system;
- perform login/update flows;
- recover from missing/corrupt remote content.

Local bootstrap content should be deliberately small and stable.

---

## 21.2 Remote Content

Content that benefits from independent patching should normally be remote Addressable content.

This includes, where compatible with runtime schema:

- gameplay definitions;
- world chunks;
- creature content;
- UI assets;
- VFX;
- SFX;
- music;
- models/textures/materials;
- localisation;
- other content assets.

---

## 21.3 Executable-Bound Content

Content required to construct the executable/bootstrap itself remains local.

Code cannot be hotfixed through ordinary AssetBundles.

New C# types/behaviours require a new executable release.

---

# 22. Gameplay Definitions as Hotfixable Content

## 22.1 Balance Data

ScriptableObject gameplay definitions are intended to be hotfixable where the existing executable already understands their serialized schema.

Examples include:

- class base-stat changes;
- class stat-growth changes;
- spell damage/value changes;
- cooldown changes;
- resource-cost changes;
- range/cast-time changes;
- talent values;
- item stats;
- NPC stats;
- recipe/loot values.

---

## 22.2 Existing Runtime Types Only

A content hotfix may only use runtime types already present in the executable.

For example, a `SpellDefinition` may change the values of existing effect components.

Adding a new `SerializeReference` effect/condition/target type requires executable code containing that type and therefore a full client/server release.

---

## 22.3 New Definitions

New content definitions may be introduced by content update when they use existing supported schemas/behaviours and pass all reference/persistence validation.

A new definition that depends on new code is not a content-only hotfix.

---

# 23. Definition Registry Architecture

## 23.1 Stable Definition IDs

All persistent/gameplay definitions use stable `DefinitionId` identity.

Display names and filenames are not persistent identity.

A production definition ID must not be casually reused for a different concept.

---

## 23.2 Resources-Based Libraries Must Not Own Hotfixable Authority

The current pattern:

```text
Resources.Load<ClassDefinitionLibrary>()
→ serialized direct references to every class
```

and equivalent libraries are not suitable as the final authority for remotely hotfixable gameplay definitions.

A `Resources` library can cause authoritative definition inventory/dependencies to be embedded in the player build and defeat the remote-update model.

---

## 23.3 Runtime Registries

Production gameplay-definition registries should be populated from the active Addressables/Release content set.

The runtime API may remain conceptually similar:

```text
ClassDefinitionLibrary.GetDefinition("ranger")
SpellDefinitionLibrary.GetDefinition("fireball")
```

but the authoritative definitions behind that lookup must come from the active content revision.

---

## 23.4 Bootstrap Registry Knowledge

Local bootstrap data may know:

- stable labels;
- registry keys;
- category identifiers;

but should not require a permanent direct object-reference inventory of all hotfixable authoritative definitions.

---

# 24. AssetBundle Architecture

## 24.1 Primary Rule

Assets should be bundled according to:

- update frequency;
- download cost;
- runtime locality;
- dependency graph;
- asset size;
- mutability.

Bundle grouping must not be based solely on "these are all the same asset type."

---

## 24.2 Avoid Monolithic Gameplay Bundles

Do not place all gameplay definitions into one giant mutable bundle.

A class-balance patch should not require downloading every:

- item;
- quest;
- recipe;
- NPC;
- faction;
- spell;

unless those assets genuinely share the same update/dependency unit.

---

## 24.3 Avoid One Bundle Per Tiny Asset

The opposite extreme is also prohibited as the default.

One AssetBundle per small ScriptableObject would create excessive:

- bundle count;
- catalog size;
- request overhead;
- loading overhead;
- dependency complexity.

The intended unit is a **coherent hotfix domain**.

---

## 24.4 Recommended Gameplay Bundle Domains

Initial remote gameplay domains should be separated along lines such as:

- Classes;
- Spells/Abilities;
- Talents;
- Actor/Combat Stats;
- Items;
- Crafting/Recipes;
- Loot;
- NPC/Creature gameplay definitions;
- Quests;
- Factions/Reputation;
- other coherent gameplay domains.

Exact final groups may be adjusted after dependency/patch-size profiling.

---

## 24.5 Presentation Bundle Domains

Presentation content should likewise use coherent groupings such as:

- UI family;
- VFX family;
- SFX family;
- music;
- creature presentation;
- shared environment assets.

Large media should not be forced into the same frequently-changing bundle as tiny gameplay data.

---

# 25. World Chunk Bundling

World chunk scenes must remain independently patchable at useful geographic granularity.

A change to one village/chunk should not normally invalidate an entire continent bundle.

Chunk scenes should therefore be packed:

- per chunk/scene; or
- in deliberately small coherent region groups

where dependency analysis supports it.

Exact chunk bundle granularity is performance/patch-size tuning.

---

## 26. Shared Dependencies

Frequently reused dependencies must be identified and separated where appropriate.

Examples include:

- shared materials;
- shader assets;
- common textures;
- common models;
- common audio;
- shared UI atlases.

The build must avoid duplicating a large shared dependency into many independent bundles.

---

## 27. Dependency Ownership

Every frequently shared asset should have intentional bundle ownership.

Content authors must not create accidental dependency graphs that cause:

- duplication;
- unexpected bundle invalidation;
- huge patch deltas;
- cyclic/opaque bundle relationships.

Dependency analysis is part of release validation.

---

## 28. Local-to-Remote Reference Safety

Local/bootstrap assets must not accidentally embed remote mutable assets through hard references.

A direct reference is acceptable only where build analysis confirms that it does not create:

- duplicate local copies;
- unintended bundle ownership;
- loss of hotfixability.

Hotfixable definitions should generally be resolved through Addressable references/keys/registries rather than being pulled into local Resources/player data.

---

# 29. Static Versus Mutable Addressables

## 29.1 Frequently Mutable Groups

Frequently updated content should use mutable content-update groups designed for small coherent updates.

Typical examples:

- gameplay balance definitions;
- UI content;
- selected VFX/SFX;
- active world content.

---

## 29.2 Rarely Changing Large Content

Large content that rarely changes may use Addressables content-update restrictions/strategies appropriate to static content.

A rare changed asset may be moved into a new update bundle rather than requiring routine replacement of a very large baseline bundle.

Exact Unity group settings belong to implementation, but the product objective is minimum safe patch size.

---

# 30. Platform / Build-Target Separation

AssetBundles/catalogs are build-target-specific artifacts.

The release system must not assume that one platform's bundles/catalog/content-state file can be reused blindly for another target.

The Release Manifest records the appropriate catalog identity per platform/build target.

---

# 31. Client and Server Content

The headless server and graphical client may require different physical Addressable bundles.

For example:

- server requires authoritative gameplay definitions;
- client additionally requires meshes, textures, VFX, UI and audio.

They nevertheless share the same logical **Gameplay Content Revision** for authoritative definitions.

---

# 32. Content-State Artifact

The Addressables `addressables_content_state.bin` associated with a production player build is a release artifact.

It must be:

- retained;
- associated with its exact player/build target;
- archived by the release pipeline;
- used as the baseline for compatible content-update builds.

It must not be treated as disposable local build output.

---

# 33. Content Update Build Workflow

A content hotfix is built against the exact previous production Addressables content-state artifact.

Conceptually:

```text
Production baseline
→ preserved content-state file
→ edit eligible content
→ build Addressables Content Update
→ inspect changed bundles/dependencies
→ publish immutable bundles
→ publish catalog
→ activate new Release State
```

---

# 34. Immutable Bundle Publication

Production AssetBundles are immutable.

If a bundle changes, publish a new content-addressed/versioned bundle.

Do not overwrite the bytes behind an already-published immutable bundle identity.

This enables:

- safe caching;
- reproducibility;
- rollback;
- post-release debugging.

---

# 35. Catalog Publication Order

Publishing must be atomic from the client's perspective.

Required order:

1. build and validate;
2. upload all new/changed bundles;
3. verify uploaded bundle availability/integrity;
4. upload/publish the new catalog;
5. publish its hash/revision;
6. activate/publish the new Release Manifest/current-release pointer last.

The active catalog must never reference bundles that have not yet reached the content origin/CDN.

---

# 36. CDN Provider

The initial Stage 1.9 content host is **Unity Cloud Content Delivery (CCD)**.

Ninth Age uses:

- separate CCD `Development`, `Staging` and `Production` environments;
- matching platform buckets, initially `windows`;
- `StandaloneWindows64` as the initial client build target;
- Development uploads/releases first, with later promotion to Staging and Production;
- Addressables remote load paths supplied by CCD.

The architecture remains provider-portable: game/runtime code consumes an absolute remote load path from Release State rather than depending on CCD SDK types for content reads.

CCD badges are operational pointers, not release identity. `latest` may be used for rapid Development iteration, but a pinned Release Manifest uses the immutable CCD release URL.

## 37. CDN Cache Behaviour

Immutable hashed/versioned bundles may use long-lived CDN/browser/cache lifetimes.

Small mutable pointers such as:

- active release manifest;
- catalog hash/current pointer;

must use cache behaviour that permits timely activation.

Exact HTTP headers are an operations decision.

---

# 38. Client Cache Behaviour

Addressables/Unity bundle cache is the intended local cache for remote bundles.

The client should:

- reuse unchanged cached bundles;
- download only required bundles missing for the active catalog;
- not redownload unchanged bundles after every catalog update.

---

## 39. Bundle-Level Delta Behaviour

A content update does not imply binary patching inside a changed AssetBundle.

Conceptually:

```text
old Classes bundle → no longer referenced
new Classes bundle → downloaded whole
unchanged World bundle → remains cached/reused
```

Bundle layout must therefore keep likely hotfixes appropriately small.

---

## 40. Cache Cleanup

Catalog updates may leave old unreferenced bundles in local cache.

The client must support safe cleanup of unreferenced bundles.

Cleanup should avoid forcing unnecessary redownload of bundles still referenced by the active catalog.

Exact cleanup timing is implementation tuning.

---

# 41. Mandatory Versus Lazy Downloads

The client does not need to download the entire remote game catalogue before login.

The active catalog must be known/validated first.

Content may then be divided into:

- mandatory pre-world dependencies;
- content loaded/downloaded lazily when required.

A player may not enter a context whose mandatory assets are unavailable.

---

## 42. Startup / Update Flow

The production client startup flow should conceptually be:

```text
Start local bootstrap
→ obtain active Release Manifest
→ compare client/protocol/content/catalog state
→ require executable update if necessary
→ initialise/update remote Addressables catalog
→ verify active required content revision
→ download required bootstrap/world-entry dependencies
→ authenticate
→ character selection
→ enter world
```

Exact ordering of authentication versus content download may be optimised, but compatibility must be proven before world entry.

---

## 43. Runtime Catalog Updates

The current client only calls `Addressables.InitializeAsync()`.

Production must add the equivalent runtime flow for:

- checking the active remote catalog;
- updating to the Release State's catalog;
- downloading required dependencies;
- reporting progress/errors.

Exact Unity API placement belongs to implementation.

---

# 44. Gameplay Hotfix Classification

## 44.1 Content-Only Hotfix

A content-only hotfix may change content already understood by the deployed executable.

Examples:

- Ranger stat growth 1.2 → 1.1;
- Fireball base value change;
- cooldown/resource-cost change;
- item stat adjustment;
- NPC stat adjustment;
- loot probability;
- quest text/objective data using existing objective types;
- asset/model/VFX/SFX replacements.

---

## 44.2 Full Executable Release

A full client/server release is required for changes including:

- new C# behaviour/type;
- new `SerializeReference` type;
- changed network protocol;
- changed RPC schema;
- changed serialization contract;
- incompatible ScriptableObject schema;
- new runtime system;
- executable/bootstrap dependency changes.

---

## 45. Hotfix Safety Rule

The release pipeline must classify whether a change is content-only.

If compatibility cannot be demonstrated, it must be treated as a full release.

The pipeline must not force an incompatible change through the content-update path merely because Unity can build an AssetBundle containing it.

---

# 46. Gameplay Content Activation

Authoritative gameplay definitions should not silently change beneath already-running encounters.

The default production rule is:

> **A server process is pinned to one Gameplay Content Revision for its lifetime.**

Gameplay-content hotfixes become active through a controlled server deployment/restart or another explicitly implemented safe activation boundary.

Presentation-only content can use less restrictive activation where safe.

---

## 47. Server Revision Pinning

The Master Server is the active Release State authority.

A World Server registration presents the immutable server-side compatibility identity required by the active manifest. The Master rejects registration unless the World Server matches the active:

- Release ID;
- Server Build ID;
- Protocol Version;
- Gameplay Content Revision;
- Gameplay Content Manifest Hash.

The Master separately verifies its own actual database schema against the active manifest before normal service is opened.

An accepted World Server run records the accepted Release ID in its registration snapshot for that run lifetime. That World Server does not query CCD or independently adopt `latest`.

---

## 48. Client Revision Pinning

The accepted World Server run exposes the client-facing compatibility identity from its pinned Release Manifest.

Before authentication/world entry, the client must prove or update to the pinned:

- Client Build Version / Client Build ID;
- Protocol Version;
- Gameplay Content Revision / manifest hash;
- build-target-specific Addressables Catalog Revision / hash.

The client may have newer inactive content cached, but it must use the catalog/content revision selected by the pinned active Release State.

# 49. Production Environments

At minimum, release workflow distinguishes:

- **Development**
- **Staging**
- **Production**

Development may use local/rapid content iteration.

Staging should reproduce the production content-delivery and compatibility model closely enough to validate releases.

Production artifacts are immutable.

---

# 50. Staging Validation

Before Production promotion, Staging should validate:

- client/server handshake;
- protocol compatibility;
- gameplay content revision;
- remote catalog loading;
- bundle downloads;
- database migration;
- world entry;
- representative world-chunk streaming;
- rollback/recovery path where practical.

---

# 51. Definition Lifecycle

## 51.1 Stable IDs After Publication

A published persistent definition's ID is effectively immutable.

Changing an ID is a migration, not a rename.

---

## 51.2 Removing Definitions

A definition referenced by persistent player/world state must not simply disappear.

Removal requires an explicit policy such as:

- migrate to replacement;
- preserve deprecated compatibility data;
- remove/reset affected persistent state intentionally.

Unknown/missing persistent definitions must not silently map to an arbitrary fallback.

---

# 52. Persistent Database Migrations

Production database changes use explicit ordered migrations as required by the Account/Character/Persistence PDD.

A migration must be associated with the release that requires it.

---

## 53. Server Schema Preflight

Before world simulation/login opens, the server must verify database schema compatibility.

If the required schema is not present and migration has not successfully completed, the server must not enter normal service.

---

## 54. Migration Backups

Potentially destructive/risky production migrations require an appropriate pre-deployment database backup/snapshot.

The exact database backup mechanism is operational.

---

# 55. Rollback Philosophy

Rollback is a compatibility operation, not simply a Git operation.

Reverting code does not guarantee that:

- database state;
- persistent records;
- content definitions;
- catalogs;

are compatible with the older release.

---

## 56. Application / Content Rollback

The release system should retain the previous known-good:

- client build;
- server build;
- Release Manifest;
- gameplay content set;
- Addressables catalogs;
- bundles;
- database migration context.

An older release may be reactivated only if its database/persistent-state compatibility remains valid.

---

## 57. Database Rollback

Production recovery should prefer:

1. roll forward with a corrective migration where practical;
2. roll application/content back only where schema remains compatible;
3. for catastrophic incompatible migration failure, restore an appropriate pre-deployment database snapshot.

Routine production recovery must not assume that executing migration `Down()` after players generated newer state is safe.

---

# 58. Artifact Retention

Old immutable catalogs/bundles must remain available for at least the project's operational rollback/support window.

The active catalog must never reference content that has already been removed from the CDN.

Exact retention duration is operations policy.

---

# 59. Controlled Maintenance

Ninth Age does not initially require zero-downtime mixed-version deployment.

Controlled maintenance is the default for releases requiring:

- server executable changes;
- protocol changes;
- gameplay-content activation;
- database migrations;
- other incompatible state transitions.

---

## 60. Maintenance Sequence

A normal incompatible deployment should conceptually follow:

```text
announce maintenance
→ prevent new world entries/logins at cutoff
→ persist active character/world state
→ stop affected server processes
→ take required database backup
→ apply migrations
→ deploy server/content
→ validate Release State
→ open service
```

Exact infrastructure sequencing may differ while preserving these guarantees.

---

## 61. Maintenance UX

Players should receive clear states for:

- scheduled maintenance;
- maintenance countdown where already online;
- server unavailable for maintenance;
- mandatory client update;
- mandatory content download;
- content verification/download failure.

The game should not reduce all of these conditions to "connection failed."

---

# 62. Scheduled Live Content

Time-based live content should be data-driven where practical.

Examples:

- seasonal events;
- PvP seasons;
- holiday content;
- temporary world events;
- scheduled unlocks.

Definitions should use authoritative server time.

---

## 63. Server Time

Scheduled activation uses server/UTC time, not the client's local clock.

Clients may display localised times, but eligibility/state is authoritative server state.

---

## 64. Predeployment

Scheduled content may be uploaded/downloaded before it becomes active.

Availability and activation are separate concepts.

This allows content to be present on the CDN before:

- a season starts;
- an event begins;
- a scheduled unlock occurs.

---

# 65. Release Validation

A Production release must be gated by automated validation appropriate to its change type.

At minimum, validate:

- build success;
- required tests/static checks;
- unique stable definition IDs;
- required references resolve;
- no prohibited missing definitions;
- Addressable keys/labels resolve;
- catalogs build;
- required bundles exist;
- content manifest/hash is generated;
- expected protocol version is declared;
- database migration target is valid;
- client/server/release manifest versions agree.

---

## 66. Bundle Dependency Validation

The release pipeline must inspect AssetBundle dependencies.

It should detect:

- accidental duplicated dependencies;
- unexpected local embedding of remote assets;
- overly broad shared dependencies;
- bundle dependency cycles/problems where detectable;
- content that moved into an inappropriate bundle.

---

## 67. Patch-Size Validation

Every content-update build should produce a patch/delta report showing at least:

- bundles added;
- bundles changed;
- estimated client download size;
- major dependency changes.

If a small expected balance patch unexpectedly creates a very large download, the release should fail or require explicit review.

Exact size thresholds are implementation/tuning.

---

## 68. Example Balance Hotfix

A normal balance hotfix should be capable of behaving conceptually like:

```text
Class_ranger.asset
Dexterity growth: 1.20 → 1.10

Spell_fireball.asset
Base effect: 120 → 115

→ Gameplay Content Revision 185
→ only relevant gameplay bundle(s) rebuilt
→ new bundles uploaded
→ catalog revision advanced
→ server deployment pinned to revision 185
→ clients update catalog and download changed bundles
→ unchanged world/UI/audio bundles stay cached
```

A tiny data change should not require a new executable unless the runtime schema/behaviour changed.

---

# 69. Content Download UX

When a mandatory content download is required, the client should show:

- that an update is required;
- total/remaining size where available;
- download progress;
- retry/failure state.

The client should not appear frozen while downloading required bundles.

---

## 70. Content Verification Failure

If required cached/remote content fails integrity/loading:

- retry/redownload the affected content where appropriate;
- show a specific error if recovery fails;
- do not enter gameplay with an unknown/stale fallback.

A corrupt bundle does not justify substituting unrelated content.

---

## 71. CDN Outage Behaviour

If the exact active Release State and all mandatory required bundles are already cached, the client may use that valid cache.

If required content is missing and cannot be obtained, world entry fails clearly.

A CDN outage must not cause silent fallback to an older Gameplay Content Revision.

---

# 72. No Silent Fallbacks

Examples of required fail-closed behaviour:

- protocol mismatch → reject world connection;
- stale authoritative content → require update;
- missing mandatory content → block relevant world entry;
- failed database migration → server does not open;
- unknown persistent definition → explicit migration/recovery path;
- catalog references unavailable bundle → content update fails;
- release manifest mismatch → do not guess.

---

# 73. Open / Deferred Details

The following remain implementation/operations choices:

- launcher/storefront;
- exact CI/CD provider;
- exact bundle-size thresholds;
- exact Addressables group names/labels;
- exact cache-cleanup timing;
- exact CCD retention period;
- exact maintenance duration;
- exact database backup technology;
- exact deployment orchestration;
- exact release-manifest generation/packaging command;
- exact administrator UX for later release activation after Phase 1;
- whether explicitly tested mixed-build compatibility is ever introduced;
- whether gameplay-content activation can eventually occur without server restart;
- whether rolling/zero-downtime deployment is ever justified later.

These do not change the product architecture.

# 74. Locked Design Decisions

1. Every production world runs one explicitly identified Release State.
2. Client build, World Server build, protocol, gameplay content, Addressables catalog and database schema versions are distinct.
3. Client Build Version uses a human-readable MAJOR.MINOR.PATCH form.
4. Repository-produced promoted client/World builds record the immutable Git commit SHA as their build identity.
5. Networking has an explicit positive integer Protocol Version, starting at 1.
6. Protocol versions match exactly by default.
7. Gameplay content has a positive monotonically increasing signed 64-bit revision.
8. Published Production gameplay revision numbers are never reused for different content.
9. Gameplay content also has a canonical SHA-256 manifest hash for verification.
10. Addressables catalog identity is tracked separately per build target.
11. Database schema identity is the exact required EF Core migration ID.
12. Release Manifest schema version 1 uses the exact fields defined in §15.1.
13. Release ID is a nonempty GUID generated once per immutable environment-specific manifest and never reused.
14. Immutable Release Manifest data and mutable/operational activation selection are separate concepts.
15. The Master Server owns the active Release State for its process lifetime.
16. Phase 1 selects the active manifest explicitly at Master startup; changing it requires controlled Master restart/deployment.
17. Master, World Server and client never infer release authority from CCD `latest`.
18. World Server registration must match the Master's active release and accepted World runs are pinned to the accepted Release ID.
19. Compatibility is validated before authentication/world entry.
20. Version incompatibility is reported distinctly from credential failure.
21. Stale authoritative gameplay content cannot enter the production world.
22. Unity Addressables Content Update is the canonical content-hotfix system.
23. Unity Cloud Content Delivery is the initial remote content host.
24. Development, Staging and Production use separate CCD environments with matching platform buckets.
25. Formal pinned Release Manifests use immutable CCD release URLs; movable badges are Development iteration conveniences only.
26. Addressables updates operate at bundle/catalog granularity, not binary delta within bundles.
27. Bundle layout is a product-critical patch-size concern.
28. Local bootstrap content is deliberately small/stable.
29. Hotfixable content is remote Addressable content where appropriate.
30. C# executable code is not hotfixed through ordinary AssetBundles.
31. Existing-schema ScriptableObject gameplay values are hotfixable.
32. New runtime/SerializeReference types require an executable release.
33. Persistent gameplay definitions use stable Definition IDs.
34. Resources-loaded direct-reference libraries must not remain the final authority for remotely hotfixable definitions.
35. Runtime definition registries load the active content revision.
36. Large monolithic gameplay bundles are prohibited as the default.
37. One-bundle-per-tiny-definition is also prohibited as the default.
38. Gameplay definitions are grouped into coherent hotfix domains.
39. Large presentation/media content is not forced into tiny frequently-changing gameplay bundles.
40. World chunks remain independently patchable at useful geographic granularity.
41. Shared dependencies receive intentional ownership to prevent duplication.
42. Local assets must not accidentally embed remote mutable content.
43. Frequently mutable and rarely-changing large content may use different Addressables update strategies.
44. Catalog/bundle artifacts are build-target specific.
45. Client/server may have different physical bundles while sharing one logical Gameplay Content Revision.
46. Production Addressables content-state files are retained release artifacts and are SHA-256 identified in the manifest.
47. Content updates are built against the exact compatible production content-state baseline.
48. Production bundles are immutable.
49. Bundles are uploaded/verified before the new catalog/release is activated.
50. Unchanged cached bundles are reused.
51. Changed bundles are downloaded whole; unchanged bundles are not.
52. Unreferenced cached bundles can be cleaned safely.
53. The full remote catalogue does not need to be downloaded eagerly; mandatory context dependencies do.
54. Production adds runtime catalog update/download handling beyond `Addressables.InitializeAsync()`.
55. Content-only versus executable-release eligibility is explicit.
56. Incompatible content changes fail classification rather than being forced through hotfixing.
57. A server process is pinned to one Gameplay Content Revision for its lifetime by default.
58. Gameplay balance does not silently change mid-encounter.
59. Development, Staging and Production are distinct release environments.
60. Staging validates the production compatibility/content-delivery model.
61. Published persistent definition IDs are not casually renamed/reused.
62. Persistent definition removal requires explicit migration/deprecation handling.
63. Database changes use ordered explicit migrations.
64. Master startup validates actual database schema against the active Release Manifest before opening normal service.
65. Risky production migrations require appropriate backup/snapshot.
66. Rollback validates persistent-data/schema compatibility.
67. Database rollback does not assume migration `Down()` is safe after newer writes.
68. Previous known-good release artifacts are retained for rollback.
69. Controlled maintenance is the default for incompatible releases.
70. Players receive explicit maintenance/update/content-error states.
71. Scheduled live content uses authoritative server/UTC time.
72. Live content may be predeployed before activation.
73. Production releases validate definitions, references, catalogs, bundles, protocol and migration state.
74. Bundle dependency and update-size reports are release gates.
75. Unexpectedly huge patches from small changes require failure/review.
76. Required download progress/failure is visible to the player.
77. Cached exact active content may be reused during CCD outage.
78. Missing required content never silently falls back to an older authoritative revision.
79. Compatibility/migration/content failures fail closed rather than guessing.

# 75. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](../MMORPG-Master-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md);
- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- [Crafting System PDD](Crafting-System-PDD.md);
- [Open-World Events PDD](Open-World-Events-PDD.md);
- [PvP PDD](PvP-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md);
- [Audio and Music PDD](Audio-and-Music-PDD.md).

---

# 76. Validation Criteria

The Live Content and Versioning design is correctly implemented when:

1. a production server can report its Release ID, protocol, gameplay content revision and schema expectation;
2. a production client can report its build, protocol, gameplay content and catalog identity;
3. an incompatible protocol is rejected before world entry;
4. a stale gameplay-content client cannot enter the world;
5. compatibility failure produces a specific player-facing reason;
6. production Release Manifests reproduce the exact deployed state;
7. a class stat-growth value can be changed through an eligible content-only hotfix;
8. a spell balance value can be hotfixed without rebuilding unrelated world/media content;
9. adding a new runtime effect type is correctly classified as requiring an executable release;
10. authoritative gameplay definitions are loaded from the active content revision rather than being permanently embedded through Resources registries;
11. changing one gameplay domain does not routinely invalidate all gameplay bundles;
12. world-chunk changes do not routinely invalidate an entire continent;
13. shared dependencies are not unnecessarily duplicated across many bundles;
14. a baseline production content-state artifact is preserved;
15. content-update builds use the correct baseline;
16. new bundles are available before an activated catalog references them;
17. production bundles are not overwritten in place;
18. unchanged bundles remain usable from local cache;
19. changed bundles are downloaded without redownloading unrelated unchanged bundles;
20. runtime catalog updates are checked/applied intentionally;
21. mandatory update downloads expose progress/failure;
22. server processes remain pinned to one authoritative gameplay revision during their lifetime;
23. Staging can reproduce the production remote-content update flow;
24. a missing persistent definition cannot silently become another definition;
25. database schema incompatibility prevents server service from opening;
26. release rollback checks schema/persistent-state compatibility;
27. previous release artifacts remain available for recovery;
28. scheduled live content uses authoritative server time;
29. CI/release validation detects duplicate IDs/missing references;
30. release validation reports changed bundles and estimated patch size;
31. an unexpectedly large patch from a tiny balance change is blocked/reviewed;
32. CDN failure never causes silent gameplay-content downgrade;
33. the complete design can hotfix class/spell/item/NPC balance data while preserving deterministic server/client compatibility.

---

# 77. Design Summary

Ninth Age treats live content as a controlled versioned production system, not as arbitrary files being replaced underneath running servers.

Executable version, network protocol, authoritative gameplay content, Addressables catalogs and database schema are tracked independently and tied together by one immutable Production Release State.

Addressables Content Update is the canonical hotfix path. The existing project foundations — content-update schemas, profiles and content-state data — are completed with remote catalogs, CDN delivery, runtime catalog checks and explicit release activation.

Asset bundling is deliberately designed around patch size. Frequently changed gameplay definitions are placed in coherent small hotfix domains; world chunks remain independently patchable; large shared dependencies receive intentional ownership; local Resources/direct references must not defeat remote mutability.

The client caches immutable bundles and downloads only changed/missing bundles referenced by the active catalog. The server remains pinned to the Release State's gameplay revision rather than adopting arbitrary "latest" CDN content.

Persistent database changes use explicit migrations and rollback respects the fact that newer releases may already have written newer state.

The result is:

> **A balance hotfix should be able to change a class or spell with a small controlled content download, while every client and server can still prove exactly which authoritative game they are running.**
