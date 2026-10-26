# Performance workload run

- Status: `FAIL-INSTRUMENTATION`
- Run ID: `run-20261026T180338070Z-72a50f05`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- Ordinary runtime environment: `ordinary_authoritative_server`
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `87edbc182da80ab683c68b4c86d2ac754483d659` (dirty: `true`)
- Requested full networked NPCs: `100`
- NPC activity scenario: `ForcedFull`
- Real players achieved: `1/1`
- Full NPCs achieved: `100/100`
- Ordinary runtime context: `cbd55248-65b6-44ae-afbf-fc9873931cf7/main-world/public`
- Ordinary runtime chunks: `0,0`
- Ramp-completed ECS entities: `0`
- Measurement-start ECS entities: `0`
- Measurement-end ECS entities: `0`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `unverified`
- Post-cleanup total actor entities: `unverified`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T180338070Z-72a50f05`
- Measurement: configured 300 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown unknown; scene restore restored (0.593 s); total attempt wall-clock 98.88 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

The performance run and its teardown both failed. (The performance harness runner was destroyed while Play Mode was still active. Active scene: 'Assets/Tests/Performance/Harness/Editor/PerformanceHarnessServer.unity', selected fixture: '<unavailable>', fixture unload attempted: False, cleanup completed: False.) (Object reference not set to an instance of an object)

Stop reason: `CLEANUP_FAILED`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
