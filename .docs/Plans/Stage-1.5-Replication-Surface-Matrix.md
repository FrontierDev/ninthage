# Stage 1.5 replication-surface matrix

Status: Step 1C refresh of the Step 1B project-owned replication inventory against the installed PurrNet 1.25.0 package.

The active capability contract is [Stage-1.5-PurrNet-Capability-Audit.md](Stage-1.5-PurrNet-Capability-Audit.md): package `dev.purrnet.purrnet` 1.25.0, Unity `6000.3.10f1`, local PurrNet directory SHA-256 `8d3afad9a51ff6f298f0caf6aa69acda7b72e2608e665ed3a88ab988582a8fa7`. The 1.19.1 audit from #275 and the original matrix from #276 are historical baselines only.

**Single invariant:** every later Stage 1.5 decision about delivery, visibility, ownership, serialization, cadence, transport semantics, or overload policy must cite the active 1.25.0 audit evidence below and preserve the listed project authority and recipient rule unless that later issue explicitly changes the contract.

## Authoritative recipient classes

### Embodied observer state

Normal actor/world state uses the actor's PurrNet observer set. Eligibility remains:

```text
valid compatible WorldID / MapID / InstanceID
    AND
chunk/scene broad-phase relevance
    AND
authoritative 80 m entry / 96 m exit range policy
```

`ChunkVisibilityRule` and `ServerInterestManager` own this project policy. Visibility remains separate from Full/Nearby/Dormant simulation activity. PurrNet 1.25.0 may evaluate sibling identities independently while keeping the GameObject visible through another sibling.

### Owner-private state

Inventory, equipment, talents, cooldowns, corrections, and owner-replicated stat definitions remain targeted to the controlling player only. They are not actor-observer state.

### Explicit targeted state

An RPC to one specifically authorised `PlayerID` is recorded separately when the target is not simply the actor observer set. Target selection does not make another actor visible.

### Logical non-spatial state

Group and Raid state is a future logical recipient class. No group visibility, group registry, or remote-identity exception is implemented here.

## Preservation and evidence rules

- `ActorComponent.WorldPositionD` remains the sole mutable accepted-position authority. The accepted-position SyncVar is a presentation snapshot whose wire cadence may change only after preserving its immediate server-local current value.
- The 64 m spatial-index cell size, 80/96 m actor visibility hysteresis, context compatibility, and Full/Nearby/Dormant activity model remain unchanged.
- PurrNet 1.25.0 is sibling-aware. `NetworkTransform` is itself a `NetworkIdentity`; component order determines the first identity/root used by hierarchy paths. The owner-confirmed PlayerPrefab ordering result is recorded in the audit and is not a vendor change.
- `NetworkIdentity.observers.Count` and `IsObserver` are the PurrNet observer boundary. No parallel observer registry is introduced.
- A missing current-state path is a potential gap only where the current project source lacks a bootstrap. The target, active-aura, active-cast, and stat observer-entry bootstraps listed below are completed project behavior.
- Project publication/recipient metrics, transport callback metrics, and client handler metrics are distinct. Handler counts are not wire delivery counts; intended observer fan-out is not proof of transport receipt.
- `Stage 1.5 class` uses: **A - Authority/control**, **B - Combat-critical state/events**, **C - Movement presentation**, **D - Rare actor state**, **E - Owner-private state**, **F - Cosmetic transient**, **G - Logical group/social**. Ingress/control rows that are not server-to-client replication are labelled `Ingress/control`.

## Replication-surface matrix

The final column cites current 1.25.0 evidence in the active audit; it replaces the former #275 capability references.

| ID | Project source and authoritative owner | Direction / PurrNet mechanism | Recipient and current-state rule | Payload / current telemetry | Class / current status / next action | 1.25.0 evidence |
|---|---|---|---|---|---|---|
| VIS-01 | `ChunkVisibilityRule.cs:10-68`; `ChunkSubscriptions`; rule owns context/chunk/range decision | Server -> observer eligibility; `NetworkVisibilityRule.CanSee` | Compatible context and chunk plus entry/exit radius. No state bootstrap in the rule. | Boolean decision; visibility evaluation/accepted/rejected/duration metrics | A; confirmed current policy; retain | Audit: visibility composition and reevaluation |
| VIS-02 | `ServerInterestManager.cs:151-408`; `ServerActorManager` accepted-position publication | Server accepted movement -> selected visibility evaluation; hierarchy `EvaluateVisibility` | Candidate `(observer, subject)` keys are derived from previous/current accepted positions; no global actors x players scan. | Candidate, interest request, changed-subscription, visibility metrics | A; confirmed ordering; retain | Audit: project integration and reevaluation |
| VIS-03 | `ChunkSubscriptions.cs:49-429`; project chunk/identity registry | Server internal interest state | Context-qualified actor/identity membership feeds the rule; it is not PurrNet observer authority. | No network payload; chunk subscription metric | A; confirmed project state; retain | Audit: project integration and observer boundary |
| VIS-04 | `ServerInterestManager.cs:997-1037`; `ServerActorManager.cs:2430-2755` | Teardown/transfer -> reevaluation | Removes/rolls back project subscriptions, then reevaluates affected identities. | No network payload; interest/visibility metrics | A; confirmed rollback behavior; retain | Audit: visibility mutation/lifecycle |
| POS-01 | `Actor.cs:37-41,150-199`; `ServerActorManager.cs:1963-2053`; ECS `ActorComponent.WorldPositionD` owns accepted state | Server -> embodied observers; `SyncVar<ActorPositionSnapshot>` with `sendIntervalInSeconds = 0.1 s` | Observer set only. The server-local SyncVar value remains current on every accepted commit; new observers receive the current value. | Authoritative accepted-position state remains 20 Hz server simulation; observer wire class is 10 Hz / 0.1 s. `PackActorPositionSnapshot`/`PackWorldPosition` and serializer boundary metrics remain unchanged. | C; accepted ECS -> spatial/chunk -> snapshot ordering is fixed; observer publication capped at 10 Hz via the existing SyncVar | Audit: SyncVar current value, observer entry, cadence |
| POS-02 | `MovementEndpoint.cs:28-46`; server movement validation owns commit | Client -> server; `ServerRpc` | Owner/player is request source; validation and accepted commit remain server-side. | `WorldPosition`, epoch, sequence; no dedicated proposal metric | Ingress/control; receipt is not commit; retain | Audit: RPC defaults and project integration |
| POS-03 | `MovementEndpoint.cs:48-55`; server interest publication | Server -> owner; `TargetRpc` | Affected player only; no actor-observer fan-out. | `Vector2Int[]`; client handler metric | C; retain for Step 6 | Audit: targeted delivery |
| POS-04 | `MovementEndpoint.cs:57-66`; server validation owns correction | Server -> owner; `TargetRpc` | Affected player only; correction is not observer state. | Chunk/local position; client handler metric | C; retain for Step 6 | Audit: targeted delivery |
| RARE-01..05 | `PlayerActor.cs:22-43`; server `SetCharacterData` owns persisted identity data | Server -> actor observers/owner; `SyncVar<int/string>` | Actor observer set; owner inclusion follows the selected SyncVar ownership path. Current SyncVar state bootstraps on entry. | Primitive int/string; generic handler aggregates only | D; confirmed retained metadata; retain | Audit: SyncVar owner/entry semantics |
| RARE-06 | `NPCActor.cs:23,36-50`; server initialization | Server -> NPC observers; `SyncVar<int>` | NPC observer set; current value bootstraps on entry. | Primitive int; generic handler aggregates only | D; confirmed retained state; retain | Audit: SyncVar observer entry |
| TGT-01 | `Actor.cs:318-429`; `Actor._target` and server assignment own target state | Owner -> server `ServerRpc`; server -> observers `ObserversRpc`; observer entry `TargetRpc` bootstrap | Updates go to current actor observers. `OnObserverAdded` sends `Client_BootstrapTarget` to the entering player without forcing the referenced target visible. | `Actor` reference; combat-critical publication/recipient metrics plus client handler metric | B; completed current-state bootstrap; Step 7 delivery review remains | Audit: targeted bootstrap and RPC retention |
| AURA-01 | `ServerAuraManager.cs:288-334`; `Actor.cs:539-544`; server aura/ECS state owns apply | Server -> observers; `ObserversRpc`; entry `TargetRpc` bootstrap | Current target actor observers. Active aura bootstrap is derived from authoritative ECS/manager state. | aura ID, instance, duration, stacks; combat-critical publication/recipient and handler metrics | B; bootstrap completed; Step 7 semantics review | Audit: targeted bootstrap |
| AURA-02 | `ServerAuraManager.cs:536-571`; `Actor.cs:620-629` | Server -> observers; `ObserversRpc` tick | Current actor observers; a late observer receives current active state through AURA-01 bootstrap, not a replayed tick. | instance, stacks, duration; combat-critical publication/recipient and handler metrics | B; event path remains non-retained; retain bootstrap contract | Audit: observer lifecycle and event retention |
| AURA-03 | `ServerAuraManager.cs:573-601`; `Actor.cs:609-618` | Server -> observers; `ObserversRpc` expiry | Current actor observers; removal is a transition and does not rebuild state by itself. | instance; combat-critical publication/recipient and handler metrics | B; completed active-state path; retain | Audit: observer lifecycle and project authority |
| AURA-04 | `ServerAuraManager.cs:602-625`; `Actor.cs:632-641` | Server -> observers; `ObserversRpc` dispel | Current actor observers; active-state bootstrap remains authoritative. | instance; combat-critical publication/recipient and handler metrics | B; completed active-state path; retain | Audit: observer lifecycle and project authority |
| CAST-01 | `ServerSpellcastManager.cs:171-201,647`; `ActorSpellcaster.cs:389-427` | Server -> observers `ObserversRpc`; entry `TargetRpc` bootstrap | Current caster observers. Active cast bootstrap sends spell ID/remaining time to the entering player. | spell ID/duration; combat-critical publication/recipient and handler metrics | B; completed current-state bootstrap; Step 7 review | Audit: targeted bootstrap |
| CAST-02 | `ServerSpellcastManager.cs:789`; `ActorSpellcaster.cs:429-434` | Server -> observers; `ObserversRpc` tick | Late observers use CAST-01 bootstrap; tick is not a current-state source. | spell ID; combat-critical publication/recipient and handler metrics | B; event path retained only for current observers; retain | Audit: RPC retention |
| CAST-03 | `ServerSpellcastManager.cs:922`; `ActorSpellcaster.cs:436-441` | Server -> observers; `ObserversRpc` interrupt | Current caster observers; interrupt is a transition. | spell ID; combat-critical publication/recipient and handler metrics | B; active cast state is bootstrapped; retain | Audit: RPC retention |
| CAST-04 | `ServerSpellcastManager.cs:1040`; `ActorSpellcaster.cs:444-449` | Server -> observers; `ObserversRpc` finish | Current caster observers; finish is a transition. | spell ID; combat-critical publication/recipient and handler metrics | B; active cast state is bootstrapped; retain | Audit: RPC retention |
| CD-01..02 | `ActorSpellcaster.cs:461-470`; `ServerSpellcastManager.cs:666-684,992-997` | Server -> owner; `TargetRpc` | Controlling owner only; no observer bootstrap. | duration, spell ID; owner-private publication/recipient and handler metrics | E; confirmed private state; retain | Audit: targeted delivery |
| STAT-01 | `ActorStatContainer.cs:81-90,100-126`; `ServerStatManager.cs:467-488` | Server -> embodied observers; observer-entry `TargetRpc` plus observer `ObserversRpc` | Current actor observers receive observer-mode snapshot; entering observer receives explicit current snapshot. | `StatSnapshot`; packer serializes all five fields; serializer/handler metrics | B; bootstrap completed; Step 5 may measure reduction | Audit: project bootstrap and serialization |
| STAT-02 | `ActorStatContainer.cs:92-97,111-126`; `ServerStatManager.cs:479-487` | Server -> owner; targeted snapshot | Controlling owner receives owner-mode snapshot on entry. | `StatSnapshot`; all five fields; owner-private metrics | E; private bootstrap completed; Step 5 may measure reduction | Audit: targeted delivery |
| STAT-03 | `ActorStatContainer.cs:161-177,208-223` | Server -> embodied observers; `ObserversRpc` | Current observer set; current state also comes from STAT-01. | `StatSnapshot`; all five fields; combat-critical publication/recipient and handler metrics | B; full snapshot event path; Step 5 reduction | Audit: serializer and observer RPC |
| STAT-04 | `ActorStatContainer.cs:161-177,201-206` | Server -> owner; `TargetRpc` | Controlling owner only; owner state remains private. | `StatSnapshot`; all five fields; owner-private publication/recipient and handler metrics | E; full snapshot event path; Step 5 reduction | Audit: serializer and targeted RPC |
| STAT-05 | `ServerStatManager.cs:361-369,392-430,467-488`; `ActorStatContainer` | Server -> embodied observers; observer snapshot publication | `SendSnapshot` rebuilds the observer-mode snapshot. | `StatSnapshot` now serializes `StatID`, `EffectiveMax`, `FlatModifier`, `PercentModifier`, `Current`; publication/recipient, serializer, handler metrics | B; #279-#290 completed source/recipient instrumentation; Step 5 reduction | Audit: metric and packer reconciliation |
| STAT-06 | `ServerStatManager.cs:467-488` | Server -> owner; targeted snapshot publication | Explicit owner receives observer/owner snapshots as selected by the server stat path. | Same five-field `StatSnapshot`; owner-private publication/recipient metrics | E; owner path remains separate; Step 5 reduction | Audit: owner path and serialization |
| STAT-07 | `ServerSpellcastManager.cs:645-647,787-789,920-922,1038-1040` | Server -> embodied observers; direct `Observers_ReceiveSnapshot` | Current actor observers; entry state comes from STAT-01. | Five-field `StatSnapshot`; combat-critical publication/recipient, serializer, handler metrics | B; completed publication boundary; Step 5 reduction | Audit: project authority and packer |
| STAT-08 | `ServerSpellcastManager.cs:666-684,991-997` | Server -> owner; direct `Client_ReceiveSnapshot` | Controlling owner only; entry state comes from STAT-02. | Five-field `StatSnapshot`; owner-private publication/recipient, serializer, handler metrics | E; completed owner publication boundary; Step 5 reduction | Audit: project authority and packer |
| COMBAT-01 | `Actor.cs:502-530` | Server -> embodied observers; `ObserversRpc` | Current caster/actor observers; transient log is not retained current state. | `CombatLogEntry`; combat-critical publication/recipient and handler metrics | B; delivery policy review in Step 7 | Audit: RPC channels/retention |
| COMBAT-02 | `Actor.cs:513-537` | Server -> explicit target; `TargetRpc` | Explicit `PlayerID`; not actor observer fan-out. | `CombatLogEntry`; combat-critical targeted publication/recipient and handler metrics | B; explicit target semantics preserved; Step 7 review | Audit: targeted delivery |
| COMBAT-03 | `ActorSpellcaster.cs:452-459` | Server -> embodied observers; `ObserversRpc` | Current caster observers; combat outcome is separate from authoritative stat state. | Actor, damage, damage schools; combat-critical publication/recipient and handler metrics | B; Step 7 reliability/cosmetic review | Audit: RPC channels/retention |
| VFX-01 | `ActorVFXController.cs:33-84`; spell-effect call sites | Server -> embodied observers; `ObserversRpc` | Current actor observers; transient effect is not retained. | effect ID/path/world position/lifetime; custom world-position serializer and cosmetic publication/recipient/handler metrics | F; confirmed presentation-only path; Step 7 review | Audit: serializer, RPC, metrics |
| VFX-02 | `ActorVFXController.cs:44-97`; projectile call sites | Server -> embodied observers; `ObserversRpc` | Current actor observers; transient projectile is not retained. | target/path/speed; cosmetic publication/recipient/handler metrics | F; confirmed presentation-only path; Step 7 review | Audit: RPC, metrics |
| VFX-03 | `ActorVFXController.cs:55-107`; trigger call sites | Server -> embodied observers; `ObserversRpc` | Current actor observers; one-shot trigger is not retained. | effect ID; cosmetic publication/recipient/handler metrics | F; confirmed presentation-only path; Step 7 review | Audit: RPC, metrics |
| SFX-01 | `ActorSFXController.cs:26-44` | Server -> embodied observers; `ObserversRpc` | Current actor observers; one-shot sound is not retained. | addressable path; cosmetic publication/recipient/handler metrics | F; confirmed presentation-only path; Step 7 review | Audit: RPC, metrics |
| PRIV-01 | `PlayerInventory.cs:349-379` | Owner -> server `ServerRpc`; server -> owner `TargetRpc` | Owning player only; persistence/load owns initial state. | Inventory DTO/delta; owner-private path, no observer broadening | E; retain | Audit: targeted delivery |
| PRIV-02 | `PlayerEquipment.cs:137-208` | Owner -> server; server -> owner | Owning player only; equipment feeds stat authority, not observer state. | Equipment DTO/delta; owner-private path | E; retain | Audit: targeted delivery |
| PRIV-03 | `PlayerTalents.cs:124-151` | Owner -> server; server -> owner | Intended owner-only; current request validation TODO remains a project risk outside this audit. | Talent list/entry; owner-private path | E; retain/out of Stage 1.5 | Audit: targeted delivery |
| CTRL-01 | `MovementEndpoint.cs:28-46` | Client -> server; `ServerRpc` | Sending owner/player is request source; server validates before commit. | Position/epoch/sequence; no dedicated proposal metric | Ingress/control; retain | Audit: RPC defaults |
| CTRL-02 | `Actor.cs:394-419` | Owner -> server; `ServerRpc(requireOwnership: true)` | Owner authorization is distinct from target validity; server assignment remains authoritative. | Actor reference; server/handler metrics | Ingress/control; Step 4 consumes TGT-01 | Audit: ownership and targeted state |
| CTRL-03 | `Actor.cs:439-447`; `ActorSpellcaster.cs:259-312` | Owner -> server; `ServerRpc(requireOwnership: true)` | Owner request only; server validates spell/action and owns cast. | Spell ID/optional world position; server handler metric | Ingress/control; retain | Audit: RPC defaults |
| CTRL-04 | `Actor.cs:449-460` | Owner -> server; `ServerRpc(requireOwnership: true)` | Owner request only; server action systems own result. | Call plus `RPCInfo`; generic server handler metric | Ingress/control; retain | Audit: RPC defaults |
| CTRL-05 | inventory/equipment/talents request methods above | Owner -> server; `ServerRpc` | Owner-only workflows; not observer replication. | Project DTOs/primitives; generic server handler metrics | Ingress/control; out of Stage 1.5 | Audit: targeted delivery |
| CTRL-06 | `AccountEndpoint.cs:44-89`; `CharacterEndpoint.cs:44-49` | Client -> session/server; `ServerRpc` | Sending player/session; not actor observer state. | Character/ID/spell DTOs; account handler metrics | Ingress/control; out of Stage 1.5 | Audit: RPC defaults |
| CTRL-07 | `ActorEndpoint.cs:163-168` | Client -> server control; `ServerRpc` | Sending player; validation/debug control only. | RPC info; no dedicated metric | Ingress/control; out of Stage 1.5 | Audit: RPC defaults |
| CTRL-08 | `AccountEndpoint.cs:34-89`; `CharacterEndpoint.cs:51-83` | Server -> explicit owner/target; `TargetRpc` | Requesting/target player; session/progression state, not world observer state. | Character/progression DTOs; account handler metrics | E; out of Stage 1.5 | Audit: targeted delivery |
| CTRL-09 | `ActorEndpoint.cs:170-183` | Server -> explicit requester; `TargetRpc` | Requesting player only. | Validation result/message; no dedicated metric | E; out of Stage 1.5 | Audit: targeted delivery |
| TEL-01 | `ServerConnectionManager.cs:61-193`; metrics definitions `:73-84,365-370` | Transport -> server telemetry; `ITransport` callbacks | Per transport connection for callback windows; live list remains `ITransport.connections`. | `ByteData.length`, callback count, connection lifecycle; aggregate and connection-interval payload/message metrics | A; completed #279-#283 boundary; Step 2 may extend project-owned attribution | Audit: transport callbacks |
| TEL-02 | `ChunkVisibilityRule.cs:12-68`; `ServerInterestManager.cs:151-408` | Visibility evaluation -> server telemetry | Counts candidate/evaluation/accept/reject work, not network recipients. | Project visibility/interest metrics | A; completed project workset telemetry; retain | Audit: visibility integration |
| TEL-03 | `PackWorldPosition.cs:18-70`; `PackStatSnapshot.cs:10-107`; metrics serializer definitions | Serializer -> shared telemetry | Encoded serializer operations independent of recipient count. | World position and five-field stat snapshot serializer metrics; application vs encoded bytes remain distinct | A; stat pack omission is no longer current; Step 5 may measure reduction | Audit: serialization |
| TEL-04 | project publication call sites in `Actor`, `ActorSpellcaster`, `ServerAuraManager`, `ServerSpellcastManager`, `ActorStatContainer`, VFX/SFX; metrics `:477-570` | Server publication/recipient accounting and client/server handler accounting | Observer metrics use current `NetworkIdentity.observers.Count`; targeted metrics record one intended target; handler metrics are local execution only. | Publication, intended recipient, handler metrics by class | A; completed #279-#290 instrumentation; Step 2 may add attribution only at proven boundaries | Audit: metric reconciliation |
| TEL-05 | `ServerConnectionManager.cs:176-193`; `NetworkingPerformanceMetrics.cs:12-23,73-132,365-378`; 1.25.0 `ITransport.cs:217-291` | Transport/project capability gap | Project now measures aggregate/per-connection payload and callback counts plus class publication/fan-out; PurrNet generic backlog/loss/retransmission remains unavailable. RTT is a public transport-dependent API but not instrumented; Steam queue stats are optional and transport-specific. | Payload callbacks; no on-wire framing; no generic backlog, loss, retransmission, latency metric in current project | A; preserve unavailable semantics; Step 8 waits for supported project-owned boundary | Audit: transport observability and metrics |

## Completed work reflected by #279-#290

| Completed issue range | Current project behavior represented in this matrix |
|---|---|
| #279-#283 | Added project-owned transport payload/message telemetry, publication/recipient class metrics, and handler metrics. TEL-01/TEL-04 distinguish emission, intended fan-out, transport callback counts, and local handler execution. |
| #284-#285 | Added explicit stat observer/owner publication boundaries and transient VFX/SFX publication boundaries. STAT-01/02/05-08 and VFX/SFX rows now reflect those call sites. |
| #286 | Routed combat/effect producer paths through explicit project-owned publication methods so server emission metrics are recorded at the source boundary. COMBAT, AURA, CAST, VFX, SFX, and STAT rows use those paths. |
| #287-#290 | Added observer-entry target, active-aura, and active-cast bootstraps; completed the observer-added integration event; and updated `PackStatSnapshot` to serialize all five current snapshot fields. TGT, AURA, CAST, and STAT rows no longer describe those completed paths as missing. |

These completed changes do not change recipient classes or authoritative owners. Event RPCs remain event RPCs; explicit observer-entry bootstraps are the current-state paths.

## 1.25.0 transport capability status

| Capability | 1.25.0 result | Matrix use |
|---|---|---|
| Connection-specific send/receive callbacks | Supported by `ITransport.onDataSent`/`onDataReceived(Connection, ByteData, bool)` | TEL-01; project measures payload bytes and callback message counts. |
| Disconnect state/reason | Supported by `onConnected`, `onDisconnected`, and `DisconnectReason` | TEL-01. |
| Payload byte measurement | Supported at `ByteData.length` | TEL-01; payload only, not on-wire framing. |
| Generic RTT | Public optional `ITransport.GetRoundTripTime(Connection, bool)` and `measuresRoundTripTime`; transport-dependent | Documented capability; no project instrumentation added. |
| Generic queue depth/send backlog | Not exposed by `ITransport` | TEL-05 remains unavailable. Steam-only reliable retry queue statistics are not generic. |
| On-wire/framed bytes | Not exposed by public callbacks | TEL-05 remains unavailable. |
| Packet loss | No public generic counter | Remains unavailable. |
| Retransmission | No public generic counter | Remains unavailable. |
| Bandwidth/send budget | Not exposed; MTU is only a packet-size boundary | Step 8 remains blocked on project-owned measurement, not on a fabricated budget. |

## Step 1C implementation gate

| Later step | Rows consumed | Verified 1.25.0 dependency | Current project owner(s) | Allowed later change | Explicit prohibition in this refresh |
|---|---|---|---|---|---|
| Step 2 - per-connection/network-source telemetry | TEL-01..05 and recipient-bearing rows | Payload callbacks, observer list, targeted RPCs, serializer boundary; generic backlog/loss/retransmission unavailable; RTT optional but uninstrumented | `ServerConnectionManager`, `NetworkingPerformanceMetrics`, existing call sites | Add project-owned counters only at proven publication/recipient/transport boundaries | No vendor instrumentation, fabricated zeros, or handler-count substitution |
| Step 4 - late-observer bootstrap | TGT-01, AURA-01..04, CAST-01..04, STAT-01/02 | Observer hooks, targeted RPCs, retained RPC distinction, sibling visibility | Actor, aura/spellcast managers, ActorStatContainer | Extend bootstraps only from existing authority and target the entering player | No second authority, forced target visibility, or assumption that ordinary events replay |
| Step 5 - stat snapshot reduction | STAT-01..08, TEL-03 | Five-field current packer, SyncVar/TargetRpc semantics, serializer/delta facilities | ActorStatContainer, ServerStatManager, ServerSpellcastManager, PackStatSnapshot | Measure/reduce payload while preserving owner/observer modes and bootstrap | No unmeasured contract removal or authority broadening |
| Step 6 - position cadence | POS-01..04, VIS-01/02/04 | Immediate server-local SyncVar value, visibility reevaluation, serializer boundary | ServerActorManager, ServerInterestManager, Actor, visibility rule | Choose wire cadence only after preserving accepted-position ordering | No second position authority, range/cell change, or global scan |
| Step 7 - delivery semantics | COMBAT-01..03, VFX-01..03, SFX-01, TGT/AURA/CAST | Per-channel ordering, explicit RPC defaults, bufferLast, targeted bootstrap, MTU behavior | Existing actor/effect/spellcast owners | Select delivery per surface after evidence; retain critical reliability until reviewed | No blanket unreliable conversion or transient-to-state conversion |
| Step 8 - measured budgets/overload | TEL-01/TEL-05 and all server->client A-F rows | Payload/recipient measurements; no generic backlog/budget; optional RTT not instrumented | Future project-owned scheduler/telemetry | Define thresholds only after Step 2 establishes a supported boundary | No threshold, generic replication manager, or queue in Step 1C |

## Exact non-goals

- No production C# changes, tests, performance-harness changes, asset/package changes, prefab changes, or third-party changes.
- No new RPCs, callbacks, queues, registries, caches, observer manager, stat delta implementation, position-rate choice, reliability choice, bandwidth threshold, or transport instrumentation.
- No change to recipient classes, authoritative owners, 64 m cells, 80/96 m range, or Full/Nearby/Dormant activity.

## Static/documentation validation

1. Both Stage 1.5 documents identify installed PurrNet 1.25.0 and the same local hash.
2. Every Step 5-8 capability claim cites current 1.25.0 audit evidence rather than the historical 1.19.1 contract.
3. The stacked-identity/root-order behavior and owner-confirmed runtime result are explicit.
4. #279-#290 completed behavior is represented accurately, including target/aura/cast/stat bootstraps and publication metrics.
5. Unsupported generic on-wire, backlog, loss, and retransmission counters are not fabricated; RTT and Steam-specific queue boundaries are qualified accurately.
6. The working-tree diff contains only these two documentation files and zero `Assets/ThirdParty/PurrNet` changes.
7. `git diff --check` passes.
8. Unity compilation and Unity Test Runner are not required for this documentation-only change; owner runtime review remains manual.
