# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261026T200023348Z-79ad1cf6`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- Ordinary runtime environment: `ordinary_authoritative_server`
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `a66def51f391225923b6a37de02e3b287fb9cde5` (dirty: `false`)
- Requested full networked NPCs: `100`
- NPC activity scenario: `ForcedFull`
- Real players achieved: `1/1`
- Full-networked fixture NPCs achieved: `100/100`
- Ordinary runtime context: `ccca5c88-ca80-4296-b258-02acf966dfc7/main-world/public`
- Ordinary runtime chunks: `0,0`
- Ramp-completed ECS entities: `100`
- Measurement-start ECS entities: `100`
- Measurement-end ECS entities: `100`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `4`
- Server-wide NPC activity telemetry: Full mean=103 (min=103, max=103); Nearby mean=0 (min=0, max=0); Dormant mean=0 (min=0, max=0).
- Server-wide activity counters include ordinary-world NPCs as well as benchmark fixture NPCs.
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T200023348Z-79ad1cf6`
- Measurement: configured 300 s; actual 300.003 s; 56496 frames / 300 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.875 s; scene restore restored (0.376 s); total attempt wall-clock 567.491 s
- Focus context: known_background; transitions 0; focused 0; unfocused 56496; unknown 0; runInBackground True

## Result

The Stage 1.4 full networked NPC activity workload was measured and cleaned up successfully.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
