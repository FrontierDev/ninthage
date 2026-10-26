# Stage 1.5 PurrNet capability audit

Status: complete source audit for the locally installed PurrNet package. This document is an implementation input for the Stage 1.5 networking work; it does not change runtime code, tests, package metadata, or vendor files.

## Scope and inspection identity

The audit covers the installed PurrNet APIs needed for visibility, observer lifecycle, state/RPC delivery, serialization, transport observability, and update/bandwidth policy. The inspected vendor root is:

`C:\Users\recip\Unity\NinthAge\Assets\ThirdParty\PurrNet`

The local package metadata is `Assets/ThirdParty/PurrNet/package.json`:

- package name: `dev.purrnet.purrnet`
- package version: `1.19.1`
- changelog top release: `1.19.1`, dated 2026-02-25 in `Assets/ThirdParty/PurrNet/CHANGELOG.md`
- package files included in the audit: 1,196, including Unity metadata files

The package has no nested Git repository, so the package version and the reproducible directory hash below are the available local identity evidence. The hash was calculated with the project-owned algorithm in `Assets/Tests/Performance/Harness/Editor/Test_PerformanceEvidence.cs:101-131`: SHA-256 of sorted project-relative file paths and per-file SHA-256 hashes, with forward-slash paths and lowercase hexadecimal output.

Installed PurrNet directory hash:

`e232f713a923f9b079a1f2de34d7730c926af37cc001467fb2fa2a4b4d574941`

The project is using Unity `6000.3.10f1`, recorded in `ProjectSettings/ProjectVersion.txt`. `Packages/manifest.json` and `Packages/packages-lock.json` do not identify PurrNet as a Unity Package Manager dependency; the authoritative source for this audit is therefore the checked-in local directory above. `ProjectSettings/PurrNetSettings.asset` has code stripping disabled (`stripServerCode: false`).

All vendor paths below are relative to the installed vendor root unless the path begins with `Assets/Runtime` or `Assets/Tests`. Line ranges identify the inspected type/member and are evidence pointers, not generated API documentation.

## Project-owned integration already present

The project already has integration points for the capabilities that are supported by composition:

- `Assets/Runtime/Shared/Networking/ChunkVisibilityRule.cs:7-63` defines `ChunkVisibilityRule : NetworkVisibilityRule`, sets `complexity` to 50, and implements `CanSee(PlayerID, NetworkIdentity)` using actor/chunk context, accepted position snapshots, `NetworkIdentity.IsObserver`, and entry/exit radii. This is the project-owned context/range policy.
- `Assets/Runtime/Shared/Runtime/Actors/Actor.cs:18,38,150-199` uses a PurrNet `SyncVar<ActorPositionSnapshot>` for the server-maintained accepted position. The server setter updates the SyncVar value immediately after validation; the read path exposes the current accepted snapshot to visibility evaluation.
- `Assets/Runtime/Shared/Runtime/Actors/Actor.cs:368-387,453-466` contains representative `ServerRpc`, `ObserversRpc`, and `TargetRpc` call sites.
- `Assets/Runtime/Shared/Runtime/Actors/ActorStatContainer.cs:73-97,172-214` uses the server-side observer-entry callback to construct a `StatSnapshot` and sends it to the entering player with a `TargetRpc`; the same type also has an observer broadcast path.
- `Assets/Runtime/Server/Core/ServerConnectionManager.cs:47-89,112-122` subscribes to the public raw transport events and records `ByteData.length` per received or sent transport payload.
- `Assets/Runtime/Shared/Networking/NetworkingPerformanceMetrics.cs:13-22,60-71,278-287,378-382` explicitly distinguishes payload bytes from unavailable on-wire framing, retransmission, loss, backlog, and latency counters. The existing constants match the installed public API evidence below; no production metric changes are part of this audit.
- `Assets/Runtime/Shared/Networking/PackStatSnapshot.cs:25-104` is a project-owned custom packer. It length-prefixes the snapshot array and writes the project-selected fields (`StatID`, `EffectiveMax`, and `Current`) for each entry. It is not evidence that all `StatSnapshotEntry` fields are currently replicated.

## Visibility and observer lifecycle

### Rule composition and reevaluation

`PurrNet/Runtime/Components/NetworkVisibility/NetworkVisibilityRule.cs:8-20` and `PurrNet/Runtime/Components/NetworkVisibility/INetworkVisibilityRule.cs:5-17` define the public rule contract: a `complexity` value and `CanSee(PlayerID, NetworkIdentity)`. `NetworkVisibilityRuleSet.Setup` in `PurrNet/Runtime/Components/NetworkVisibility/NetworkVisibilityRuleSet.cs:20-35` sorts rules by ascending complexity. Its `CanSee` method at `PurrNet/Runtime/Components/NetworkVisibility/NetworkVisibilityRuleSet.cs:58-70` returns on the first rule that allows visibility and otherwise denies visibility.

`PurrNet/Runtime/CoreModules/HierarchyV2/VisilityV2.cs:23-44` exposes `RefreshVisibilityForGameObject` overloads. `EvaluateAll` at `PurrNet/Runtime/CoreModules/HierarchyV2/VisilityV2.cs:144-167` refreshes roots for every supplied player, and `Evaluate` at `PurrNet/Runtime/CoreModules/HierarchyV2/VisilityV2.cs:172-244` applies parent visibility, whitelist/blacklist state, override/default rules, owner visibility, and the rule-set result. A rule returning false removes the observer; a rule returning true adds it. The source does not expose a project-facing generic “actor moved” callback that automatically reevaluates an arbitrary actor. Reevaluation cadence and the choice of roots therefore remain project-owned integration decisions.

`NetworkIdentity.Rules` at `PurrNet/Runtime/Components/NetworkIdentity/NetworkIdentity.Rules.cs:31-118` marks visibility dirty when whitelist, blacklist, or rule settings change. The project’s spatial/chunk policy remains the source of position/range context; PurrNet supplies the rule evaluation and observer mutation mechanism.

### Observer registry and lifecycle

`PurrNet/Runtime/Components/NetworkIdentity/NetworkIdentity.cs:32-34,355-359` exposes public observer-added/removed events, a public `IReadOnlyList<PlayerID> observers`, and `IsObserver(PlayerID)`. The list’s `Count` is the observer count; there is no separate public count API. The underlying list is maintained by the identity, not by a parallel project registry.

Server-only lifecycle hooks are `OnPreObserverAdded`, `OnObserverAdded`, and `OnObserverRemoved` at `:716-761`. The corresponding trigger methods at `:1426-1553` invoke the protected callbacks and public events. `ActorStatContainer.OnObserverAdded` is a concrete project use of this integration.

The normal observer-add path is staged. `PurrNet/Runtime/CoreModules/HierarchyV2/HierarchyV2.cs:1660-1689` sends a catch-up spawn packet, adds the observer, invokes the pre-add hook, queues the late observer event, and then triggers the spawn event. `SendDelayedObserverEvents` at `:1626-1638` invokes `NetworkIdentity.TriggerOnObserverAdded` during the later network-message phase. Manual additions and removals are exposed at `:1892-1923`.

There is a meaningful bootstrap ordering caveat. `PurrNet/Runtime/CoreModules/RPCs/RPCModule.cs:86-98,116-123,490-526` replays buffered RPCs from the `onSentSpawnPacket` event, before the later observer callback phase. `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:129-135,248-265` separately sends latest state through a `TargetRpc`. The server-side scheduling order is evidenced, but the source does not establish a single cross-channel, end-to-end arrival order for a spawn packet, replayed RPC, SyncVar state, and a later observer RPC. Any gameplay dependency on that order must use an explicit project-owned bootstrap protocol or remain a validation item.

For a single entering observer, the installed public API is sufficient for a project-owned bootstrap: `NetworkBehaviour.OnObserverAdded(PlayerID, bool)` plus a `TargetRpc` targeted to that player. `NetworkIdentity.Broadcasting.GetObservers` and `SendRPCNormal` at `PurrNet/Runtime/Components/NetworkIdentity/NetworkIdentity.Broadcasting.cs:231-261,349-380` show observer selection and observer RPC batching; `TargetRpcAttribute` is defined at `PurrNet/Runtime/CoreModules/RPCs/TargetRpcAttribute.cs:7-20`. The project’s `ActorStatContainer` uses this shape. Reliable delivery still depends on the connection remaining alive, as with any network transport.

## SyncVar semantics and update cadence

`PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:13-32` defines `SyncVar<T>` and its public `sendIntervalInSeconds`. The `value` setter at `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:45-72` compares with `PurrEquality<T>`, immediately updates the local `_value`, and marks the variable dirty when the value changes. This means an interval delays wire publication, not the server’s current value.

The dirty/send state machine at `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:176-235` coalesces repeated assignments into the current value, sends an unreliable update when the interval permits, and sends a reliable follow-up when the dirty period ends. The default constructor value at `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:241-246` is an interval of zero seconds. The cadence is tick-driven; the project’s `Actor.SetAcceptedPositionSnapshotFromServer` writes the current accepted value immediately at `Assets/Runtime/Shared/Runtime/Actors/Actor.cs:150-176`, while `ChunkVisibilityRule` reads the current value at `Assets/Runtime/Shared/Networking/ChunkVisibilityRule.cs:46-49`.

Observer entry state is handled at `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:129-135,248-265`: a new observer receives the current value via a targeted reliable RPC, subject to the owner-authentication exception for the spawning owner. Owner-authenticated variables send owner changes to the server and distribute them to other observers using the owner-excluding methods at `PurrNet/Runtime/Components/NetworkModule/SyncVar.cs:267-313`; server-authenticated variables use the all-observer methods. “Owner included” is therefore a property of the selected SyncVar ownership path, not a single global SyncVar flag.

`PurrNet/Runtime/Components/NetworkBehaviour/NetworkRules.cs:141-145,341-344` exposes `syncedTickUpdateInterval`, and `PurrNet/Runtime/CoreModules/Ticks/TickManager.cs:120-137` uses it for client tick synchronization. This is global tick synchronization, not a per-variable or per-connection replication budget.

## RPC delivery, ordering, and bootstrap behavior

The public RPC attributes define these defaults:

- `ServerRpcAttribute` at `PurrNet/Runtime/CoreModules/RPCs/ServerRpcAttribute.cs:15-27` defaults to `Channel.ReliableOrdered` and requires ownership.
- `ObserversRpcAttribute` at `PurrNet/Runtime/CoreModules/RPCs/ObserversRpcAttribute.cs:7-20` defaults to `ReliableOrdered` and exposes `excludeOwner`, `excludeSender`, `bufferLast`, and `deltaPacked`.
- `TargetRpcAttribute` at `PurrNet/Runtime/CoreModules/RPCs/TargetRpcAttribute.cs:7-20` defaults to `ReliableOrdered` and exposes `bufferLast` and `deltaPacked`.

`PurrNet/Runtime/Transports/Interface/ITransport.cs:110-131` defines channel semantics: `ReliableOrdered` is delivered in order, `ReliableUnordered` is reliable without order, `UnreliableSequenced` is ordered but may be dropped, and `Unreliable` has no receipt or order guarantee. `PurrNet/Runtime/Transports/UDPTransport.cs:360-368` maps the public channels to LiteNetLib channels. The source proves ordering within a selected channel; it does not prove a total ordering between different channels.

`PurrNet/Runtime/Components/NetworkIdentity/NetworkIdentity.Broadcasting.cs:231-261,263-347,349-430` shows recipient selection: observer RPCs target the current observer list with owner/sender exclusions, target RPCs use the signature’s target player list, and both are queued with the signature channel. `PurrNet/Runtime/CoreModules/RPCs/RPCSignature.cs:25-57` records the target and channel choices. A targeted owner or new observer can therefore use the reliable ordered `TargetRpc` path, while cosmetic traffic can explicitly choose an unreliable channel.

`PurrNet/Runtime/CoreModules/RPCs/RPCModule.cs:616-647` implements `bufferLast` as one retained latest packet per RPC ID; `PurrNet/Runtime/CoreModules/RPCs/RPCModule.cs:490-526` replays matching buffered observer/target RPCs to a newly observing player using reliable ordered delivery. This is retained replay, not a generic bounded outgoing queue. Ordinary non-buffered RPCs are not replayed for late observers.

RPCs are batched per player and channel. `PurrNet/Runtime/CoreModules/RPCs/RPCBatch.cs:134-150,180-225,227-266` caches the player/channel MTU, splits a batch when the next content would cross the cached MTU, and queues the content for later send. `ITransport.GetMTU` at `PurrNet/Runtime/Transports/Interface/ITransport.cs:151-173` is the public size boundary; the default is 1,024 bytes for unreliable/unreliable-sequenced/reliable-unordered channels and 8,192 bytes for reliable-ordered. This establishes packet batching/splitting, not a public application payload quota or queue-depth API.

## Serialization and payload accounting

The installed public serializer is bit-oriented. `PurrNet/Runtime/BitPacker/BitPacker.cs:16-45,48-103,121-196` exposes `positionInBits`, `positionInBytes`, `length`, memory/span access, `BitData` conversion, and position control. `:452-520` provides byte read/write operations. These members are sufficient for project-owned encoded-size measurements at the serializer boundary.

Custom packers can be registered through `PurrNet/Runtime/BitPacker/Packer.cs:13-19,21-101`, including typed writers/readers and `Packer.Transform`. `PurrNet/Runtime/BitPacker/DeltaPacker.cs:8-122` and `PurrNet/Runtime/BitPacker/DeltaPacker_T.cs:10-142` provide changed-bit/full-value fallback delta packing and registered delta packers. `PurrNet/Runtime/BitPacker/RegisterNetworkTypeAttribute.cs:7-19` exposes registration controls including `DontPack` and `DontDeltaCompress`. `PurrNet/Runtime/BitPacker/Packers/PackCollections.cs:10-43` registers list, array, and hash-set packers; the collection comparators compare null/length/count and elements (`PurrNet/Runtime/BitPacker/Packers/ArrayComparator.cs:5-28` and `PurrNet/Runtime/BitPacker/Packers/ListComparator.cs:5-29`).

`PurrNet/Runtime/BitPacker/BitPacker.cs:57-87` exposes LZ4-backed `Pickle`/`Unpickle` operations. RPC attributes additionally expose `CompressionLevel` and `deltaPacked`; these are RPC/code-generation options, not a generic replication-priority system. The project’s `PackStatSnapshot` is the authoritative current `StatSnapshot` wire shape, and its field selection should be preserved unless a separate feature explicitly changes that contract.

For payload accounting, `ITransport` exposes `onDataReceived`, `onDataSent`, `connections`, `SendToClient`, and `SendToServer` at `PurrNet/Runtime/Transports/Interface/ITransport.cs:151-200`. `RawNetManager.rawTransport` is public at `PurrNet/Runtime/Managers/RawNetManager.cs:68`, and `NetworkManager.rawTransport` is public at `PurrNet/Runtime/Managers/NetworkManager.cs:191`. The concrete transport raises data events around transport payloads: `PurrNet/Runtime/Transports/PurrTransport.cs:633-705` and `PurrNet/Runtime/Transports/UDPTransport.cs:159-180,360-392`. The project’s `ServerConnectionManager` correctly measures `ByteData.length` at the event boundary.

That boundary is payload, not on-wire bytes. `PurrNet/Runtime/Transports/PurrTransport.cs:656-683` wraps client data with its transport/relay framing but raises `onDataSent` with the original payload data; the public `ITransport` interface exposes no framing/header byte count. The existing `TRANSPORT_ON_WIRE_BYTES_UNAVAILABLE_REASON` is therefore reconciled with the installed source and remains unchanged.

## Transport observability and policy gaps

The public transport contract supplies per-connection payload events and a live read-only connection list, so message counts can be derived by counting event invocations and payload bytes can be derived from `ByteData.length`. It does not expose a public queue depth, send backlog, congestion state, retransmission count, packet-loss statistic, RTT/latency measurement, or generic per-connection send quota. A transport implementation may have internal state, but internal or third-party implementation details are not a project-owned public integration contract.

Likewise, the audit found no generic public replication priority, network LOD, per-connection bandwidth budget, or bounded outgoing-state queue. `NetworkBones._sendRatePerSecond` at `PurrNet/Runtime/Components/NetworkBones/NetworkBones.cs:11-18` is component-specific and cannot be treated as a generic actor replication scheduler. `SyncVar.sendIntervalInSeconds` is a real per-variable cadence control, and `bufferLast` is real latest-retained RPC behavior, but neither supplies a global priority/budget/backpressure system.

## Metric reconciliation

The installed API confirms the project’s current metric boundary:

- `SERVER_TRANSPORT_PAYLOAD_BYTES_SENT` and `SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED` are implementable from `ITransport.onDataSent`/`onDataReceived` and `ByteData.length`; the project already does this.
- `TRANSPORT_ON_WIRE_BYTES` cannot be populated from the public API because framing is not included in the event payload and no public framing counter exists.
- Retransmission count, packet-loss ratio, send backlog, and latency are not public PurrNet `ITransport` counters or measurements. They must remain explicitly unavailable unless an approved external instrumentation boundary is added in project-owned code.
- No unavailable metric is represented as a fabricated zero in the existing definitions.

No production code, tests, package metadata, Unity settings, or third-party files were changed for this audit.

## Normalized capability table

| Capability | Status | Installed evidence and project integration | Boundary / implementation note |
|---|---|---|---|
| Context/range visibility integration | Supported with project-owned integration | `NetworkVisibilityRule.CanSee`; `NetworkVisibilityRuleSet.CanSee`; `VisilityV2.Evaluate`; project `ChunkVisibilityRule.CanSee` | Project owns chunk/context lookup, accepted-position inputs, and reevaluation cadence. |
| Observer enumeration/count | Supported directly | `NetworkIdentity.observers : IReadOnlyList<PlayerID>` and `IsObserver(PlayerID)` | Count is `observers.Count`; no separate count method is needed. |
| Observer-entry callback | Supported with project-owned integration | `NetworkBehaviour.OnObserverAdded`; `NetworkIdentity.TriggerOnObserverAdded`; project `ActorStatContainer.OnObserverAdded` | Server-side lifecycle callback; do not assume it is a client callback. |
| Targeted new-observer bootstrap | Supported with project-owned integration | `OnObserverAdded(PlayerID, bool)` plus `TargetRpc`; project `ActorStatContainer.Client_ReceiveSnapshot` | Explicit project bootstrap is required for ordering-sensitive state; reliable transport does not create cross-channel total ordering. |
| Retained late-observer state | Supported directly | `SyncVar<T>.OnObserverAdded` → `SendLatestState`; RPC `bufferLast` replay in `RPCModule` | SyncVar current state is sent to the entering observer; only `bufferLast` RPCs are retained/replayed. |
| SyncVar cadence control without stale server value | Supported directly | `SyncVar<T>.sendIntervalInSeconds`, immediate `value` assignment, dirty/send state machine; project accepted-position SyncVar | Interval controls publication cadence; server `_value` remains current. |
| Observer RPC reliability/ordering | Supported directly | `ObserversRpcAttribute`; `Channel` semantics; `NetworkIdentity.Broadcasting` observer batching | Reliable ordered is the default; ordering is only established within the selected channel. |
| Owner-targeted RPC reliability/ordering | Supported directly | `TargetRpcAttribute`; `RPCSignature` target selection; target batching | Reliable ordered is the default; target must be selected by the RPC signature and normal observer/connection rules. |
| Optional cosmetic/best-effort delivery | Supported directly | `Channel.Unreliable` and `Channel.UnreliableSequenced`; RPC attributes expose `channel` | No receipt guarantee for unreliable traffic; sequenced traffic may drop older packets. |
| Serializer delta/change support | Supported with project-owned integration | `DeltaPacker<T>`, `Packer<T>`, `deltaPacked`, project `PackStatSnapshot` | Project-owned packers must define the actual field contract; generic collection/delta support does not replace that contract. |
| Per-connection payload byte measurement | Supported with project-owned integration | `ITransport.onDataSent/onDataReceived`, `Connection`, `ByteData.length`; project `ServerConnectionManager` | Measures transport payload bytes at the public event boundary, not framing/on-wire bytes. |
| Per-connection message count | Supported with project-owned integration | Count `onDataSent`/`onDataReceived` invocations keyed by `Connection` | Derived event count; no native cumulative message counter is exposed. |
| Transport backlog | Unsupported by installed public API | `ITransport` exposes send/events/connections but no queue-depth or backlog member | Do not infer backlog from payload events or MTU batching. |
| Latency | Unsupported by installed public API | No public RTT/latency measurement in `ITransport` or the inspected manager/transport contract | External/project-owned probes would be a separate design decision. |
| Loss | Unsupported by installed public API | `Channel` semantics state delivery guarantees but expose no loss counter/statistic | Do not derive loss from unreliable traffic without sequence instrumentation. |
| Retransmission | Unsupported by installed public API | Reliable channel selection is public; retransmission counters are not | Transport internals are not an approved project integration contract. |
| Replication priority | Unsupported by installed public API | No generic priority/network-LOD member in the inspected visibility, RPC, SyncVar, or transport APIs | `complexity` orders visibility rules; it is not replication priority. |
| Bandwidth/send budget | Unsupported by installed public API | MTU-based batching/splitting is public; no per-connection quota or budget API is exposed | `sendIntervalInSeconds` is per-variable cadence, not a bandwidth budget. |
| Bounded/latest-state coalescing | Unsupported by installed public API | `bufferLast` retains one latest RPC per RPC ID; no bounded outgoing queue or generic state coalescer is exposed | The limited `bufferLast` behavior must not be generalized into backpressure or live queue control. |
