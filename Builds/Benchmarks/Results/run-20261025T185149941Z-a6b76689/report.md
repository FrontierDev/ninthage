# Performance workload run

- Status: `FAIL-RESOURCE`
- Run ID: `run-20261025T185149941Z-a6b76689`
- Profile: `P1-S0-04`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `EcsDataOnlyTimedStateStress`
- Seed: `424242`
- Git commit: `0e11441904365e404df8dc83bfb92632ac7c0ac6` (dirty: `false`)
- Requested ECS entities: `100`
- Ramp-completed ECS entities: `0`
- Measurement-start ECS entities: `0`
- Measurement-end ECS entities: `0`
- Peak ECS entities observed: `1`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `0`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261025T185149941Z-a6b76689`
- Measurement: configured 5 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.481 s; scene restore restored (0.974 s); total attempt wall-clock 12.177 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

Unity total allocated memory 5006111381 exceeded the runner ceiling 4294967296 bytes.

Stop reason: `RESOURCE_CEILING`

This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.
