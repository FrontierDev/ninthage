# Performance workload run

- Status: `FAIL-CORRECTNESS`
- Run ID: `run-20261026T182446099Z-d841f27a`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- Ordinary runtime environment: `ordinary_authoritative_server`
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `eec5aabcdf9358cd44869018827316ee15de2a42` (dirty: `true`)
- Requested full networked NPCs: `100`
- NPC activity scenario: `ForcedFull`
- Real players achieved: `1/1`
- Full NPCs achieved: `100/100`
- Ordinary runtime context: `f9db4f56-8fae-4d70-b53a-ce46df86d27c/main-world/public`
- Ordinary runtime chunks: `0,0`
- Ramp-completed ECS entities: `100`
- Measurement-start ECS entities: `100`
- Measurement-end ECS entities: `100`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `4`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T182446099Z-d841f27a`
- Measurement: configured 300 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.921 s; scene restore restored (0.718 s); total attempt wall-clock 204.584 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

A captured Stage 1.4 PlayerActor was destroyed, became dead, or lost its owner during measurement.

Stop reason: `RUNNER_EXCEPTION`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
