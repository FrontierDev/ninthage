# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261015T205158215Z-6ec0a0a1`
- Profile: `P1-S0-04`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `EcsDataOnlyMinimal`
- Seed: `424242`
- Git commit: `d22a939232c842a1415bd627c2a6a0be3663ce95` (dirty: `true`)
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
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261015T205158215Z-6ec0a0a1`
- Measurement: configured 5 s; actual 5.004 s; 3035 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.685 s; scene restore restored (0.312 s); total attempt wall-clock 20.12 s
- Focus context: known_foreground; transitions 0; focused 3035; unfocused 0; unknown 0; runInBackground True

## Result

The requested data-only population was measured and cleaned up successfully.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.
