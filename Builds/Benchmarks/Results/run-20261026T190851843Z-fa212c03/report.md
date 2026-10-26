# Performance workload run

- Status: `FAIL-INSTRUMENTATION`
- Run ID: `run-20261026T190851843Z-fa212c03`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- Ordinary runtime environment: `ordinary_authoritative_server`
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `8170235f87beae53dca98dfca9ce0e21a1d9c61c` (dirty: `true`)
- Requested full networked NPCs: `100`
- NPC activity scenario: `ForcedFull`
- Real players achieved: `1/1`
- Full NPCs achieved: `100/100`
- Ordinary runtime context: `acda00f3-5630-4431-aaa2-53e0debdf3ba/main-world/public`
- Ordinary runtime chunks: `0,0`
- Ramp-completed ECS entities: `100`
- Measurement-start ECS entities: `100`
- Measurement-end ECS entities: `100`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `4`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T190851843Z-fa212c03`
- Measurement: configured 300 s; actual 300.006 s; 61964 frames / 300 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.891 s; scene restore restored (0.643 s); total attempt wall-clock 565.041 s
- Focus context: known_background; transitions 0; focused 0; unfocused 61964; unknown 0; runInBackground True

## Result

Metric 'server.simulation.tick_backlog' retained only 15.93 % of its measurement samples; the configured minimum coverage is 95 %.

Stop reason: `MEASUREMENT_SAMPLE_COVERAGE`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
