# Performance workload run

- Status: `FAIL-CORRECTNESS`
- Run ID: `run-20261023T105155892Z-37bdd6f5`
- Profile: `P1-SIM-REGION-SIZING`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `SimulationRegionSizing`
- Seed: `424242`
- Git commit: `c5154ea9873bc15e6c99fded9066a910f4af1624` (dirty: `true`)
- Simulation-region sizing evidence: `simulation-region-sizing.json`
- Production region capacity and defaults: `not selected by this test-only probe`
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261023T105155892Z-37bdd6f5`
- Measurement: configured 5 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 1.229 s; scene restore restored (0.793 s); total attempt wall-clock 10.473 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

Physics isolation probe did not observe the known region geometry.

Stop reason: `RUNNER_EXCEPTION`

This is an Editor Play Mode test-only simulation-region sizing probe. It records candidate geometry, physics and NavMesh evidence but does not select production region capacity or defaults.
