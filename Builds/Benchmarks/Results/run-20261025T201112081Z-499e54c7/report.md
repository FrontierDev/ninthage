# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261025T201112081Z-499e54c7`
- Profile: `P1-S1.3-TIMED-SCALE`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `EcsDataOnlyTimedStateScale`
- Seed: `424242`
- Git commit: `411a476ab7aeabe68fcbc4526fa51a1e653dfcc5` (dirty: `true`)
- Requested ECS entities: `5000`
- Ramp-completed ECS entities: `5000`
- Measurement-start ECS entities: `5000`
- Measurement-end ECS entities: `5000`
- Peak ECS entities observed: `5000`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `0`
- Measured activity snapshot (`measurement_end_pre_cleanup`, before teardown): idle=`5000`, patrolling=`0`, combat=`0`, wake surge=`0`
- Measured activity events: spawn=`0`, despawn=`0`
- Total spawn events including ramp: `5000`
- Wake surge is included in combat and is not added as a disjoint population.
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261025T201112081Z-499e54c7`
- Measurement: configured 5 s; actual 5.004 s; 686 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.604 s; scene restore restored (0.557 s); total attempt wall-clock 24.44 s
- Focus context: known_background; transitions 3; focused 594; unfocused 92; unknown 0; runInBackground True

## Result

The requested data-only population was measured and cleaned up successfully.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.
