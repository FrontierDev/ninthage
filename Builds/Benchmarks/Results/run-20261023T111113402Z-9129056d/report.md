# Performance workload run

- Status: `COMPLETE-MEASURED`
- Run ID: `run-20261023T111113402Z-9129056d`
- Profile: `P1-SIM-REGION-SIZING`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `SimulationRegionSizing`
- Seed: `424242`
- Git commit: `f554c3d7ea221c3602e3d5f63e8cda9c41265132` (dirty: `true`)
- Simulation-region sizing evidence: `simulation-region-sizing.json`
- Production region capacity and defaults: `not selected by this test-only probe`
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261023T111113402Z-9129056d`
- Measurement: configured 5 s; actual 5.001 s; 3347 frames / 10 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.57 s; scene restore restored (0.511 s); total attempt wall-clock 19.063 s
- Focus context: known_foreground; transitions 0; focused 3347; unfocused 0; unknown 0; runInBackground True

## Result

The requested simulation-region sizing probe completed with qualified evidence and cleanup.

Stop reason: `SIMULATION_REGION_SIZING_COMPLETE`

This is an Editor Play Mode test-only simulation-region sizing probe. It records candidate geometry, physics and NavMesh evidence but does not select production region capacity or defaults.
