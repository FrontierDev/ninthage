# Performance workload run

- Status: `FAIL-PROVISION`
- Run ID: `run-20261026T175727436Z-98546353`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- Ordinary runtime environment: `ordinary_authoritative_server`
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `d6eb9c31428fb6e64da7205e384525bdf244cb75` (dirty: `true`)
- Requested full networked NPCs: `100`
- NPC activity scenario: ``
- Real players achieved: `0/0`
- Full NPCs achieved: `0/0`
- Ordinary runtime context: `148c89dc-4f11-400e-ab77-f7c44c419e6c/main-world/public`
- Ordinary runtime chunks: `0,0`
- Ramp-completed ECS entities: `0`
- Measurement-start ECS entities: `0`
- Measurement-end ECS entities: `0`
- Peak ECS entities observed: `0`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `4`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T175727436Z-98546353`
- Measurement: configured 300 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.741 s; scene restore restored (0.727 s); total attempt wall-clock 23.461 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

Spawned Stage 1.4 NPC 'aae58398-6860-492a-a047-a324e56d7f1c' does not have the ordinary enabled NavMeshAgent movement component.

Stop reason: `FIXTURE_PRECONDITION_FAILED`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
