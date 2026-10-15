# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261015T210209875Z-aa0ec874`
- Profile: `P1-S0-06`
- World fixture: `Settlement` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Settlement.unity`)
- Workload: `GraphicalClient`
- Seed: `424242`
- Git commit: `d22a939232c842a1415bd627c2a6a0be3663ce95` (dirty: `true`)
- Requested graphical objects: `96`
- Achieved graphical objects: `96`
- Requested graphical actors: `0`
- Achieved graphical actors: `0`
- Route coverage: `4/4` waypoints
- Ramp-completed ECS entities: `96`
- Measurement-start ECS entities: `96`
- Measurement-end ECS entities: `96`
- Peak ECS entities observed: `96`
- Post-cleanup fixture entities: `unverified`
- Post-cleanup total actor entities: `unverified`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261015T210209875Z-aa0ec874`
- Measurement: configured 5 s; actual 5.001 s; 2258 frames / 11 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.757 s; scene restore restored (0.343 s); total attempt wall-clock 27.988 s
- Focus context: known_foreground; transitions 0; focused 2258; unfocused 0; unknown 0; runInBackground True
- Rendering qualification: verified; probe measured_nonzero

## Result

The selected graphical fixture was rendered through the ordinary client path and the deterministic camera route completed.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode rendered-client measurement. It does not qualify authenticated player density, normal PurrNet world streaming, or multi-client network behavior.
