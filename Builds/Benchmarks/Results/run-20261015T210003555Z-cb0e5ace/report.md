# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261015T210003555Z-cb0e5ace`
- Profile: `P1-S0-06`
- World fixture: `Forest` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Forest.unity`)
- Workload: `GraphicalClient`
- Seed: `424242`
- Git commit: `d22a939232c842a1415bd627c2a6a0be3663ce95` (dirty: `true`)
- Requested graphical objects: `251`
- Achieved graphical objects: `251`
- Requested graphical actors: `0`
- Achieved graphical actors: `0`
- Route coverage: `4/4` waypoints
- Ramp-completed ECS entities: `251`
- Measurement-start ECS entities: `251`
- Measurement-end ECS entities: `251`
- Peak ECS entities observed: `251`
- Post-cleanup fixture entities: `unverified`
- Post-cleanup total actor entities: `unverified`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261015T210003555Z-cb0e5ace`
- Measurement: configured 5 s; actual 5.003 s; 2223 frames / 11 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.749 s; scene restore restored (0.34 s); total attempt wall-clock 28.269 s
- Focus context: known_foreground; transitions 0; focused 2223; unfocused 0; unknown 0; runInBackground True
- Rendering qualification: verified; probe measured_nonzero

## Result

The selected graphical fixture was rendered through the ordinary client path and the deterministic camera route completed.

Stop reason: `MEASUREMENT_COMPLETE`

This is an Editor Play Mode rendered-client measurement. It does not qualify authenticated player density, normal PurrNet world streaming, or multi-client network behavior.
