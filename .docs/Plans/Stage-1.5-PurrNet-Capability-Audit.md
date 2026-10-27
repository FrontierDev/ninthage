# Stage 1.5 PurrNet capability audit

Status: active Step 1C dependency-audit refresh for the locally installed PurrNet package. This document is an implementation input for the remaining Stage 1.5 networking work. It records source evidence only; it does not authorize runtime, prefab, package, test, or vendor changes.

## Scope and inspection identity

The inspected vendor root is:

`C:\Users\recip\Unity\NinthAge\Assets\ThirdParty\PurrNet`

The active package identity is:

- package name: `dev.purrnet.purrnet`;
- installed package version: `1.25.0` from `Assets/ThirdParty/PurrNet/package.json`;
- changelog identity: `1.25.0`, dated 2026-10-26, at `Assets/ThirdParty/PurrNet/CHANGELOG.md:1`;
- package files included in the directory hash: 1,488, including Unity metadata files;
- Unity editor: `6000.3.10f1` from `ProjectSettings/ProjectVersion.txt`;
- nested Git repository: none;
- reproducible local directory SHA-256:

`8d3afad9a51ff6f298f0caf6aa69acda7b72e2608e665ed3a88ab988582a8fa7`

The directory hash uses the established project convention in `Assets/Tests/Performance/Harness/Editor/Test_PerformanceEvidence.cs:111-131`: sort all files by full path using ordinal comparison; emit each project-relative path with forward slashes, a colon, the lowercase per-file SHA-256, and a newline; SHA-256 the UTF-8 concatenation. This local directory hash is the authoritative audit identity.

The package metadata and changelog identify the 1.25.0 release line and link upstream tags/commit hashes, including the stacked-identity fix at changelog line 31. They do not prove that this checkout is an exact upstream tag or commit: there is no nested Git metadata or independently verified ref. No upstream tag/commit is therefore trusted as the audit identity.

The previous 1.19.1 audit remains historical context only: package version `1.19.1`, 1,196 files, directory hash `e232f713a923f9b079a1f2de34d7730c926af37cc001467fb2fa2a4b4d574941`. It is superseded by this 1.25.0 audit and must not be used as the active Stage 1.5 capability contract. #275 and #276 are historical baselines for the refresh.

All vendor paths below are relative to the installed PurrNet root unless a path begins with `Assets/Runtime` or `Assets/Tests`. Line ranges are source-evidence pointers.

## Project-owned integration rechecked

The current project integration was re-read against this package:

- `Assets/Runtime/Shared/Networking/ChunkVisibilityRule.cs:10-68` owns context, chunk, accepted-position, and 80/96 m range policy through `NetworkVisibilityRule.CanSee` and `NetworkIdentity.IsObserver`.
- `Assets/Runtime/Server/Core/ServerInterestManager.cs:151-408,997-1037` derives candidate visibility work from accepted movement and handles teardown/transfer reevaluation; it does not maintain a second PurrNet observer registry.
- `Assets/Runtime/Shared/Runtime/Actors/Actor.cs:38,318-429,502-641` owns the accepted-position SyncVar, target bootstrap, target/combat/aura RPCs, and project publication metrics.
- `Assets/Runtime/Shared/Runtime/Actors/ActorStatContainer.cs:81-126,161-240` owns observer-entry and owner stat bootstraps, snapshot publication, and the current five-field snapshot model.
- `Assets/Runtime/Shared/Runtime/Actors/ActorVFXController.cs:33-107` and `ActorSFXController.cs:26-44` expose project-owned server publication boundaries for transient observer effects.
- `Assets/Runtime/Shared/Networking/MovementEndpoint.cs:28-66` contains movement proposal ingress and targeted interest/correction callbacks.
- `Assets/Runtime/Server/Core/ServerConnectionManager.cs:61-193` consumes public transport events, records payload bytes/message counts, and keeps live connection state from `ITransport.connections`.
- `Assets/Runtime/Shared/Networking/NetworkingPerformanceMetrics.cs:73-132,365-378,477-570` records project publication counts, intended recipient fan-out, handler invocations, and transport payload metrics. Handler counts remain distinct from emission and delivery.

## Visibility, reevaluation, and observer lifecycle

### Rule composition and reevaluation

`NetworkVisibilityRule` and `INetworkVisibilityRule` define `complexity` and `CanSee(PlayerID, NetworkIdentity)` at `Runtime/Components/NetworkVisibility/NetworkVisibilityRule.cs:5-20` and `INetworkVisibilityRule.cs:5-17`. `NetworkVisibilityRuleSet.Setup` sorts rules by ascending complexity and `CanSee` returns on the first rule that allows visibility at `NetworkVisibilityRuleSet.cs:20-72`.

The installed visibility engine is `Runtime/CoreModules/HierarchyV2/VisilityV2.cs`. `RefreshVisibilityForGameObject` traverses a root hierarchy at `:43-90`; `EvaluateAll` gathers root identities and refreshes each root for each supplied player at `:265-296`; `Evaluate` at `:298-407` applies parent visibility, whitelist/blacklist, overrides/default rules, owner visibility, and the rule result. A false result removes an observer; a true result adds one. PurrNet does not provide a generic actor-moved callback, so the project still owns candidate selection and reevaluation cadence.

`NetworkIdentity.EvaluateVisibility()` and `EvaluateVisibility(PlayerID)` are server-side entry points at `Runtime/Components/NetworkIdentity/NetworkIdentity.cs:1104-1131`. The project calls those through `ServerInterestManager` after accepted-position and spatial/chunk updates. Visibility remains separate from Full/Nearby/Dormant simulation activity.

### Stacked identities, root resolution, and ownership

PurrNet 1.25.0 makes `NetworkTransform` a `NetworkIdentity`: `Runtime/Components/NetworkBehaviour/NetworkTransform.cs:16-19` declares `NetworkTransform : NetworkIdentity`.

On one GameObject, `NetworkIdentity.siblingIdentities` is an internal cached array populated by `GetComponents<NetworkIdentity>()` in component order at `Runtime/Components/NetworkIdentity/NetworkIdentity.cs:39-63`. `GetRootIdentity()` starts with `gameObject.GetComponent<NetworkIdentity>()`, then follows nearest network parents at `:485-499`; other root/evaluation paths also call `GetComponent<NetworkIdentity>()` (`HierarchyV2.cs:2298-2307`). The first `NetworkIdentity` component on the GameObject is therefore the root identity used by these paths. `HierarchyV2.GetComponentsInChildren` collects all identities on that first identity's hierarchy, including siblings, at `Runtime/CoreModules/HierarchyV2/HierarchyV2.cs:3254-3279`.

`VisilityV2.EvaluateNode` evaluates all sibling identities together and uses the first sibling's direct-child tree at `:104-124`. `Evaluate` computes `isAnyVisible` across siblings. When one sibling gains or loses an observer while another sibling keeps the GameObject visible, it uses `SetMutedObserver` and raises `observerChanged` rather than treating the whole GameObject as fully hidden at `:374-406`. `HierarchyV2.OnObserverChanged` invokes the per-identity pre-add/late-add or removal lifecycle at `:2757-2770`; a full root visibility loss removes muted observers and sends one hierarchy despawn at `:2784-2805`. This is the 1.25.0 stacked/sibling behavior; it is not the old duplicate-spawn failure model.

The normal visibility-add path sends the spawn packet, calls pre-observer callbacks, and queues late observer callbacks in `HierarchyV2.OnVisibilityChanged:2712-2743`. `SendDelayedObserverEvents` invokes `TriggerOnObserversAdded`/`TriggerOnObserverAdded` after the spawn phase at `:3727-3781`. Manual targeted add/remove paths are public at `:4499-4588`. `NetworkIdentity` exposes `observers`, `IsObserver`, observer events, and protected observer hooks at `NetworkIdentity.cs:65-67,436-452,884-929,1721-1840`.

Ownership is per identity. `NetworkIdentity.owner` resolves the closest network parent owner at `NetworkIdentity.cs:394-400`. `GiveOwnership`/`RemoveOwnership` accept an explicit `propagateToChildren` option at `:1133-1144,1248-1285`; `GlobalOwnershipModule.GetAllChildrenOrSelf` expands the selected identity to all identities in its hierarchy, including same-GameObject siblings, only when propagation is enabled and authorized at `Runtime/CoreModules/Ownership/GlobalOwnershipModule.cs:538-664,771-835,1055-1079`. Ownership callbacks are triggered for each affected identity. Sibling visibility does not itself grant or remove ownership.

### Owner-confirmed Ninth Age runtime evidence

The project owner validated the following prefab ordering result after the 1.25.0 upgrade:

```text
PlayerPrefab:
NetworkTransform before dedicated NetworkIdentity
    -> remote player remained instantiated/frozen outside relevance

dedicated NetworkIdentity before NetworkTransform
    -> player visibility/despawn behaved correctly
```

This is project evidence of the first-identity/root-order dependency described above. It does not authorize or require any vendor modification.

## Observer bootstrap and current-state semantics

`NetworkIdentity` exposes `OnObserverAdded(PlayerID, bool isSpawner)` and `OnObserverRemoved(PlayerID)` at `NetworkIdentity.cs:903-929`; `TriggerOnObserverAdded` invokes both overloads for a late observer at `:1775-1840`. PurrNet therefore supports a project-owned targeted bootstrap on observer entry. It does not make an ordinary non-buffered observer RPC a current-state channel.

The current project has completed targeted bootstrap for the affected Stage 1.5 surfaces:

- `Actor.OnObserverAdded` sends `Client_BootstrapTarget` to the entering observer (`Actor.cs:318-329,421-429`).
- `ServerAuraManager.OnActorObserverAdded` derives active aura state from authoritative ECS/manager state and calls `Actor.PublishAuraBootstrapToObserver` (`ServerAuraManager.cs:140-177`; `Actor.cs:546-573`).
- `ServerSpellcastManager.OnActorObserverAdded` derives the active cast and remaining time and calls `ActorSpellcaster.PublishActiveCastBootstrap` (`ServerSpellcastManager.cs:171-201`; `ActorSpellcaster.cs:389-427`).
- `ActorStatContainer.OnObserverAdded` sends observer-mode state and, for the owner, owner-mode state through targeted RPCs (`ActorStatContainer.cs:81-126`).

These bootstraps target the entering `PlayerID`; they do not force a referenced actor or target identity visible. Ordinary aura/cast/target event RPCs remain event traffic, while the bootstrap is the current-state path.

## SyncVar semantics and cadence

`SyncVar<T>` is defined at `Runtime/Components/NetworkModule/SyncVar.cs:13-44`. Its setter compares the new value, updates the local `_value` immediately, triggers local change events, and marks the variable dirty at `:50-85`. `sendIntervalInSeconds` delays wire publication, not the controlling server's current value (`:27-37,218-225,253-313`). The serialized default interval is zero unless the project sets another value.

The current tick state machine sends a reliable value on the first dirty send, sends subsequent dirty values unreliably when the interval permits, and sends a reliable follow-up when the dirty period ends (`:276-313`). A newly added observer receives the current value through `OnObserverAdded` and `SendLatestState` at `:178-184`. Owner-authenticated variables use a separate owner path, including owner-change/reconnect behavior at `:102-129`; owner inclusion is not a global SyncVar guarantee.

`NetworkRules.syncedTickUpdateInterval` remains global tick synchronization, not a per-variable or per-connection replication budget (`Runtime/Components/NetworkBehaviour/NetworkRules.cs:141-145,341-344`).

## RPC delivery, retention, MTU, and compression

The installed defaults are:

- `ServerRpcAttribute`: `Channel.ReliableOrdered`, `requireOwnership = true`, `deltaPacked = false`, `mtuExceeded = NetworkManager`, `immediate = false` (`Runtime/CoreModules/RPCs/ServerRpcAttribute.cs:16-30`);
- `ObserversRpcAttribute`: `ReliableOrdered`, `bufferLast = false`, `excludeOwner = false`, `excludeSender = false`, `deltaPacked = false`, `mtuExceeded = NetworkManager`, `immediate = false` (`ObserversRpcAttribute.cs:8-23`);
- `TargetRpcAttribute`: `ReliableOrdered`, `bufferLast = false`, `deltaPacked = false`, `mtuExceeded = NetworkManager`, `immediate = false` (`TargetRpcAttribute.cs:8-22`).

`ITransport.Channel` semantics are explicit at `Runtime/Transports/Interface/ITransport.cs:176-197`: reliable ordered is delivered in order; reliable unordered is reliable without ordering; unreliable sequenced is ordered but may drop; unreliable has neither guarantee. The source proves ordering within a chosen channel, not a total order between channels.

`bufferLast` retains the latest packet per RPC identity/target and replaces the prior retained payload at `RPCModule.cs:738-875`. A new observer receives matching retained observer/target RPCs through `SendAnyInstanceRPCs`/`SendAnyChildRPCs` at `:601-677`, using reliable ordered delivery. Non-buffered ordinary event RPCs are not replayed by this mechanism.

`deltaPacked` is an RPC code-generation/signature option. The installed delta facilities provide changed-bit/full-value fallback and registered delta writers/readers at `Runtime/BitPacker/DeltaPacker.cs:35-131` and `DeltaPacker_T.cs:10-144`. The 1.25.0 changelog records baseline-resync and missing-baseline hardening; this does not create a generic priority or queue policy.

Compression maps `CompressionLevel.Fast`, `Balanced`, and `Best` to LZ4 levels in `RPCModule.PreProcessRpc` at `RPCModule.cs:1169-1198`. `BitPacker.Pickle`/`Unpickle`, bit/byte positions, spans, and logical length are public at `Runtime/BitPacker/BitPacker.cs:30-101,133-180`.

Per-RPC `MTUBehaviour` is public at `ITransport.cs:110-173`: follow the manager, upgrade unreliable to reliable, drop, or fragment. `RPCBatch` caches `ITransport.GetMTU` per player/channel, splits batches, and applies the selected over-MTU behavior at `Runtime/CoreModules/RPCs/RPCBatch.cs:303-374,383-416,510-583`. The default `ITransport.GetMTU` is 1,024 bytes for unreliable, unreliable-sequenced, and reliable-unordered channels, and 8,192 bytes for reliable-ordered at `ITransport.cs:232-240`; concrete transports may return different values. MTU is a packet-size boundary, not a bandwidth budget.

## Serialization and transport observability

Project-owned custom packers can register through `Packer<T>`/`Packer` and use `Packer.Transform` (`Runtime/BitPacker/Packer.cs:21-101,191-228`). The project uses `PackActorPositionSnapshot`, `PackWorldPosition`, `PackCombatLogEntry`, and `PackStatSnapshot`; `PackStatSnapshot` now serializes `StatID`, `EffectiveMax`, `FlatModifier`, `PercentModifier`, and `Current` (`Assets/Runtime/Shared/Networking/PackStatSnapshot.cs:10-107`). Serializer-boundary byte counts are not on-wire framing counts.

The public generic transport boundary includes:

- `onConnected`, `onDisconnected`, `onDataReceived`, `onDataSent`, `connections`, `GetMTU`, `SendToClient`, and `SendToServer` at `Runtime/Transports/Interface/ITransport.cs:8-16,217-291`;
- `ByteData.length`, offset, and payload span at `ITransport.cs:27-58`;
- optional RTT support: `GetRoundTripTime(Connection, bool)` and `measuresRoundTripTime`, with a default `-1`/`false`, at `ITransport.cs:242-256`. `UDPTransport`, `PurrTransport`, `CompositeTransport`, and the Steam add-on implement this boundary (`UDPTransport.cs:142-155`; `PurrTransport.cs:345-356`; `CompositeTransport.cs:290-305`; `Addons/Steam/Runtime/SteamTransport.cs:91-105`).

The optional Steam-specific public boundary additionally exposes `SteamTransport.GetSendStatistics(Connection, bool)` and `SteamSendStatistics.queuedReliableMessages`, `queuedReliableBytes`, `acceptedMessages`, `rejectedSendAttempts`, `droppedUnreliableMessages`, and `deferredReliableMessages` (`Addons/Steam/Runtime/SteamTransport.cs:100-105`; `SteamSendStatistics.cs:3-14`). The source explicitly limits queued bytes to the project-owned Steam reliable retry queue and excludes Steam's native send queue. This is not a generic `ITransport` backlog contract.

Network impairment simulation is also a supported public boundary: PurrNet 1.25.0 exposes serializable `NetworkSimulation` (`Runtime/Transports/NetworkSimulation.cs:7-48`), and `UDPTransport.networkSimulation` provides its public get/set integration (`Runtime/Transports/UDPTransport.cs:36,62-70`). `NetworkSimulation.includeInBuild` controls whether the configured simulation applies outside the Editor. Ninth Age's `LatencySimulator` uses this boundary and no longer requires private PurrNet or LiteNetLib transport reflection.

No generic public PurrNet counter was found for queue depth/backlog, congestion, packet loss, or retransmission. UDP/LiteNetLib statistics are disabled in the installed `UDPTransport` setup (`UDPTransport.cs:175-190`), and internal queues in optional transports are not a generic project integration boundary. The public RTT boundary is transport-dependent and currently uninstrumented by Ninth Age.

## Metric reconciliation and active contract

The installed 1.25.0 source supports the following project decisions:

- project transport callbacks measure payload bytes and callback message counts per connection; `ServerConnectionManager` records them at `ServerConnectionManager.cs:176-193`;
- project publication and intended-recipient metrics are now present for combat-critical, movement-presentation, owner-private, and cosmetic classes at `NetworkingPerformanceMetrics.cs:73-132,477-551`; observer recipient counts read `NetworkIdentity.observers.Count`, while targeted publication counts record one intended target;
- RPC handler metrics remain local handler execution counts and are not wire delivery counts (`NetworkingPerformanceMetrics.cs:553-570`);
- on-wire/framed bytes remain unavailable because `onDataSent`/`onDataReceived` expose `ByteData` payloads and no framing/header counter;
- generic backlog, loss, and retransmission remain unavailable;
- RTT is publicly queryable where the selected transport supports it, but Ninth Age's existing latency metric remains explicitly unavailable because no project instrumentation was added in this audit;
- Steam reliable retry-queue statistics are publicly queryable only through the optional Steam-specific API and are not part of the current generic project metric contract.

Unavailable metrics are not represented as fabricated zeroes. No production code, tests, package metadata, Unity settings, prefab, or third-party file was changed for this refresh.

### Normalized capability table

| Stage 1.5 capability | Installed 1.25.0 source evidence | Result for later steps |
|---|---|---|
| Visibility rule composition | `NetworkVisibilityRule.cs:5-20`; `NetworkVisibilityRuleSet.cs:20-72` | Supported with project-owned rule ordering and reevaluation integration. |
| Visibility reevaluation and observer mutation | `VisilityV2.cs:43-90,265-407`; `NetworkIdentity.cs:1104-1131` | Supported; cadence/candidate selection remains project-owned. |
| Stacked/sibling identity visibility | `NetworkIdentity.cs:39-63,180-190,485-499`; `VisilityV2.cs:104-136,374-407`; `HierarchyV2.cs:2712-2805` | Supported with sibling-aware partial visibility; root order is material. |
| Observer registry and lifecycle | `NetworkIdentity.cs:65-67,436-452,884-929,1721-1840`; `HierarchyV2.cs:3727-3781` | Supported directly; do not add a parallel registry. |
| Targeted observer bootstrap | `NetworkIdentity` observer hooks plus project `ActorStatContainer`, `Actor`, `ServerAuraManager`, and `ServerSpellcastManager` paths above | Supported with explicit project-owned TargetRpc bootstraps. |
| SyncVar current value, observer entry, cadence | `SyncVar.cs:27-37,50-85,178-184,218-313` | Supported; interval affects publication, not server-local current state. |
| RPC default/explicit channels | RPC attributes above; `ITransport.cs:176-197` | Supported; channel ordering is per channel only. |
| Retained RPC state | `RPCModule.cs:601-677,738-875` | `bufferLast` supported as one latest retained packet per RPC/target; ordinary events are not replayed. |
| Delta packing | `DeltaPacker.cs:35-131`; `DeltaPacker_T.cs:10-144` | Supported for explicit delta-packed contracts; baseline behavior must be validated per use. |
| Compression | `RPCModule.cs:1169-1219`; `BitPacker.cs:68-101` | Supported through explicit RPC compression levels/LZ4. |
| MTU and over-MTU behavior | `ITransport.cs:110-173,232-240`; `RPCBatch.cs:303-374,510-583` | Supported packet boundary; not a bandwidth budget. |
| Custom bit serialization | `BitPacker.cs:30-58,133-180`; `Packer.cs:21-101,191-228` | Supported at the serializer boundary. |
| Per-connection payload callbacks | `ITransport.cs:8-16,217-291` | Supported and integrated by `ServerConnectionManager`. |
| RTT | `ITransport.cs:242-256`; concrete implementations cited above | Public transport-dependent boundary; not instrumented by Ninth Age. |
| Generic queue/backlog | `ITransport` has no generic queue-depth/backlog counter; Steam-only queue API is cited above | Generic capability unavailable; Steam statistics are transport-specific and not implemented here. |
| Loss/retransmission counters | No public generic counter in the installed source | Unavailable; do not infer from reliable delivery or handler counts. |

The remaining Stage 1.5 implementation decisions must cite this 1.25.0 evidence. The 1.19.1 audit is not an active prerequisite.

## Exact non-goals

- No runtime C# changes, prefab changes, package installation/update, or PurrNet/vendor edits.
- No new RPCs, queues, registries, replication manager, stat payload reduction, position-rate choice, RPC channel choice, bandwidth threshold, or transport instrumentation.
- No regression-test changes; Unity Test Runner, Play Mode, and owner runtime validation remain manual follow-up.

## Static/documentation validation

This refresh is complete when:

1. both Stage 1.5 documents identify installed PurrNet 1.25.0 as active;
2. both documents record the local directory hash and Unity editor version;
3. every Step 5-8 capability claim points to current 1.25.0 source evidence;
4. stacked identity, root resolution, sibling visibility, ownership propagation, and owner-confirmed root-order evidence are explicit;
5. #279-#290 completed project behavior is reflected in the matrix;
6. RTT and Steam queue boundaries are documented without fabricating generic metrics;
7. the diff contains only these two documentation files and zero third-party changes;
8. `git diff --check` passes.

Owner review remains required for the package identity, root-order runtime finding, completed project behavior, and the absence of any 1.19.1-only active prerequisite.
