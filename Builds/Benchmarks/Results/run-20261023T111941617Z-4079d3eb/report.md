# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261023T111941617Z-4079d3eb`
- Profile: `P1-SIM-REGION-SIZING`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `SimulationRegionSizing`
- Seed: `424242`
- Git commit: `8eb460394b94ee8098e11518e36867f6416f6d47` (dirty: `true`)
- Simulation-region sizing evidence: `simulation-region-sizing.json`
- Production region capacity and defaults: `not selected by this test-only probe`
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261023T111941617Z-4079d3eb`
- Measurement: configured 5 s; actual 5.001 s; 3320 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.544 s; scene restore restored (0.485 s); total attempt wall-clock 31.081 s
- Focus context: known_foreground; transitions 0; focused 3320; unfocused 0; unknown 0; runInBackground True

## Result

The requested simulation-region sizing probe completed with qualified evidence and cleanup.

Stop reason: `SIMULATION_REGION_SIZING_COMPLETE`

This is an Editor Play Mode test-only simulation-region sizing probe. It records candidate geometry, physics and NavMesh evidence but does not select production region capacity or defaults.
