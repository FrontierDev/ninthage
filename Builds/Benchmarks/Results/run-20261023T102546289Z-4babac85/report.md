# Performance workload run

- Status: `FAIL-TIMEOUT`
- Run ID: `run-20261023T102546289Z-4babac85`
- Profile: `P1-SIM-REGION-SIZING`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `SimulationRegionSizing`
- Seed: `424242`
- Git commit: `48a4b11077b638ddfdadf98716c674d125476981` (dirty: `false`)
- Simulation-region sizing evidence: `simulation-region-sizing.json`
- Production region capacity and defaults: `not selected by this test-only probe`
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261023T102546289Z-4babac85`
- Measurement: configured 5 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.468 s; scene restore restored (0.502 s); total attempt wall-clock 36.495 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

Timed out waiting for ordinary server initialization (configuration, ECS, world manager and network manager).

Stop reason: `TIMEOUT`

This is an Editor Play Mode test-only simulation-region sizing probe. It records candidate geometry, physics and NavMesh evidence but does not select production region capacity or defaults.
