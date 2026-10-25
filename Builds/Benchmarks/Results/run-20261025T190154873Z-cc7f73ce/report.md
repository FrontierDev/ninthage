# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261025T190154873Z-cc7f73ce`
- Profile: `P1-S0-04`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `EcsDataOnlyTimedStateStress`
- Seed: `424242`
- Git commit: `ecf1b500c4e163eda8fee97a43eefe66c86222c7` (dirty: `true`)
- Requested ECS entities: `100`
- Ramp-completed ECS entities: `100`
- Measurement-start ECS entities: `100`
- Measurement-end ECS entities: `100`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `0`
- Post-cleanup total actor entities: `0`
- Measured activity snapshot (`measurement_end_pre_cleanup`, before teardown): idle=`100`, patrolling=`0`, combat=`0`, wake surge=`0`
- Measured activity events: spawn=`0`, despawn=`0`
- Total spawn events including ramp: `100`
- Wake surge is included in combat and is not added as a disjoint population.
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261025T190154873Z-cc7f73ce`
- Measurement: configured 5 s; actual 5.024 s; 3134 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.605 s; scene restore restored (0.673 s); total attempt wall-clock 21.138 s
- Focus context: known_foreground; transitions 0; focused 3134; unfocused 0; unknown 0; runInBackground True

## Result

The requested data-only population was measured and cleaned up successfully.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.
