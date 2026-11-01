# Stage 1.9 — Local Addressables Distribution Decision

**Status:** Proposed execution decision pending owner validation
**Scope:** Phase 1, Stage 1.9
**Authority:** Master-owned Release State and the existing client/World compatibility contracts
**Related plan:** [Phase 1 Implementation Plan — Stage 1.9](Phase-1-Implementation-Plan.md)

## Purpose and invariant

Stage 1.9 uses the approved locally installed baseline Addressables policy. CCD is not a Stage 1.9 dependency.

**Invariant:** a Player and World Server start from the complete locally installed launch baseline, while the Master remains the sole owner of immutable process-lifetime Release State and the existing eight-field client compatibility comparison remains before credential authentication.

This document records one execution decision for the Stage 1.9 plan. It is not a second runtime Release State authority, manifest selector, cache owner or compatibility registry.

## Distribution decision

The complete launch baseline is part of the locally installed Player and World Server build artifacts. It includes, at minimum, World chunks, character models and common presentation content. Addressables continues to load and unload scenes and assets on demand from disk; including the baseline in the installed artifact does not require loading all content into memory at startup.

Initial client and World startup therefore:

1. starts the local bootstrap and bootstrap UI;
2. initializes the installed Addressables catalog from disk;
3. loads the installed configuration and continues through the existing client or World initialization owner;
4. loads World scenes through the existing Addressables/PurrNet scene owner when required.

This startup performs no CCD or other HTTP catalog lookup, remote catalog update, remote bundle acquisition, `preworld` download or `latest` fallback. A missing or failed local catalog, required asset or World scene is an explicit startup failure. The bootstrap remains local so the failure can be reported without depending on Addressables content.

Installed content is a build/package and deployment concern. It is not a network download step in this stage.

## Release State and compatibility ownership

The Master continues to:

- select and validate one active Release State at startup;
- own its immutable `ReleaseID` and all required release identities for the Master process lifetime;
- validate the server-build, protocol, gameplay-content, catalog and database-schema identities;
- reject a World registration that does not match the active Release State;
- provide the accepted World with the client-facing catalog declaration; and
- require the World to pin the accepted release for its process lifetime.

The existing pre-authentication client comparison remains unchanged in ownership and ordering. It compares these eight fields before credential authentication:

1. `ClientBuildVersion`;
2. `ClientBuildID`;
3. `ProtocolVersion`;
4. `GameplayContentRevision`;
5. `GameplayContentManifestHash`;
6. `BuildTarget`;
7. `CatalogRevision`;
8. `CatalogHash`.

A mismatch fails closed with the existing actionable compatibility result. It does not start credential authentication, download content or retry client login.

## Proposed Release Manifest schema v2

The Master contract issue owns the actual schema and projection change. Stage 1.9 proposes schema v2 as follows:

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
ContentStateSHA256
```

`ManifestVersion` is the integer `2`. The v1 rules for `ReleaseID`, environment, build/version identities, positive revisions, catalog-target uniqueness, `DatabaseSchemaVersion` and `CreatedAtUTC` remain in force unless this decision explicitly changes the catalog path field. `CatalogHash` remains the opaque Addressables catalog token for the local catalog artifact; it is not replaced with a project-defined bundle digest.

All v1 identities and required schema/provenance fields are retained except catalog `RemoteLoadPath`, which is removed in v2. `ContentStateSHA256` remains the lowercase digest of the exact retained `addressables_content_state.bin` used for build provenance. It is not an active downloading requirement and is not a claim that startup has cryptographically verified every installed bundle.

Schema v1 is not silently reinterpreted as schema v2. A v1 manifest containing `RemoteLoadPath` is not a valid v2 manifest and must be rejected by the Master contract owner before normal service. A different v2 manifest/content release requires a new `ReleaseID`, correct client/server build IDs and the catalog revision/hash of the local catalog actually installed in the target artifact. An old remote release must not be reused as if it were the unchanged local release.

The catalog declaration remains an identity used for compatibility. It does not add a second local release selector and does not change the Master-owned process-lifetime pin.

## Compatibility evidence and integrity limits

Stage 1.9 uses three distinct statements:

1. Matching catalog declarations prove that the declared build-target catalog identity is compatible.
2. Successful `Addressables.InitializeAsync()` proves that the installed catalog loaded successfully.
3. Neither statement proves file-level integrity of all installed bundles.

The plan must not claim automatic full-content cryptographic verification. Any later file-integrity or distribution verification contract requires an explicit owner and transport decision.

## Ordered dependencies

The approved execution order is:

1. Master schema/projection: define and validate Release Manifest v2 and its client-facing projection.
2. Unity removal of remote metadata: remove the remote-path dependency from project-owned integration/configuration while preserving third-party Addressables APIs.
3. Local Addressables group/package build: package the complete baseline and the actual per-target local catalog into Player/World Server build artifacts.
4. Explicit startup failure: fail client or World startup when the installed catalog, required local content or representative World scene cannot load.
5. Cross-repository owner validation: validate the Master contract, Unity package/catalog identity, World pinning and pre-authentication comparison together.

The superseded CCD/remote Block 4 dependencies—remote catalog update, `preworld` download, remote cache reuse, content-only hotfix acceptance and one login retry—are not Stage 1.9 prerequisites or acceptance gates. This documentation decision does not change their GitHub issue state; dependent issue owners must reconcile those later implementation decisions before execution.

## Deferred decisions and non-goals

The following are explicitly deferred and must not be implied by this decision:

- hotfix downloads and remote catalog/bundle transport;
- distribution provider or hosting service;
- storage preflight and file-level integrity verification;
- patcher or game installer;
- cross-environment deployment and promotion workflow;
- remote hotfix activation, live rollback and bundle-cache policy.

This issue changes documentation only. It does not modify production code, Addressables settings, manifests, assets, tests or GitHub issue state.

## Acceptance and owner validation

Stage 1.9 is ready for dependent implementation only when the owner approves this exact policy and the corresponding plan section. The owner validation must confirm that:

- the launch baseline is locally installed for Player and World Server;
- startup performs no CCD/HTTP lookup, remote catalog update, remote bundle acquisition, `preworld` download or client login retry;
- Master Release State ownership, World process-lifetime pinning and the eight-field pre-authentication comparison are unchanged;
- schema v1 is rejected rather than reinterpreted, and v2 catalog identity describes the actual local catalog;
- Addressables declaration matching, catalog initialization and bundle file integrity are not conflated; and
- the acceptance gate is local package startup, compatibility failure and representative World scene load, with deferred transport decisions recorded above.

