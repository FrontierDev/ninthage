# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261025T200113527Z-d07c03fc`
- Profile: `P1-S1.3-TIMED-SCALE`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `EcsDataOnlyTimedStateScale`
- Seed: `424242`
- Git commit: `9c5681a671a86555fdd304247b18c83f843044a1` (dirty: `true`)
- Requested ECS entities: `2500`
- Ramp-completed ECS entities: `2500`
- Measurement-start ECS entities: `2500`
- Measurement-end ECS entities: `2500`
- Peak ECS entities observed: `2500`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `0`
- Measured activity snapshot (`measurement_end_pre_cleanup`, before teardown): idle=`2500`, patrolling=`0`, combat=`0`, wake surge=`0`
- Measured activity events: spawn=`0`, despawn=`0`
- Total spawn events including ramp: `2500`
- Wake surge is included in combat and is not added as a disjoint population.
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261025T200113527Z-d07c03fc`
- Measurement: configured 5 s; actual 5.007 s; 1120 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.668 s; scene restore restored (0.599 s); total attempt wall-clock 32.055 s
- Focus context: known_foreground; transitions 0; focused 1120; unfocused 0; unknown 0; runInBackground True

## Result

The requested data-only population was measured and cleaned up successfully.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.
