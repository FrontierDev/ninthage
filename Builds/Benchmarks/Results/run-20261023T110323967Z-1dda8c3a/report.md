# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261023T110323967Z-1dda8c3a`
- Profile: `P1-SIM-REGION-SIZING`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `SimulationRegionSizing`
- Seed: `424242`
- Git commit: `69fa96060077a3cef5faa1566d661e5266a9e204` (dirty: `true`)
- Simulation-region sizing evidence: `simulation-region-sizing.json`
- Production region capacity and defaults: `not selected by this test-only probe`
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261023T110323967Z-1dda8c3a`
- Measurement: configured 5 s; actual 5.001 s; 3467 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.544 s; scene restore restored (0.52 s); total attempt wall-clock 19.619 s
- Focus context: known_background; transitions 0; focused 0; unfocused 3467; unknown 0; runInBackground True

## Result

The requested simulation-region sizing probe completed with qualified evidence and cleanup.

Stop reason: `SIMULATION_REGION_SIZING_COMPLETE`

This is an Editor Play Mode test-only simulation-region sizing probe. It records candidate geometry, physics and NavMesh evidence but does not select production region capacity or defaults.
