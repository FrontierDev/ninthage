# Performance workload run

- Status: `BLOCKED-PREREQUISITE`
- Run ID: `run-20261026T152054391Z-e91fc9b9`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `5a9bb8cb8d2c392b1ce1a89cd21899716632deb3` (dirty: `true`)
- Requested full networked NPCs: `1000`
- NPC activity scenario: ``
- Real players achieved: `0/0`
- Full NPCs achieved: `0/0`
- Ramp-completed ECS entities: `0`
- Measurement-start ECS entities: `0`
- Measurement-end ECS entities: `0`
- Peak ECS entities observed: `0`
- Post-cleanup fixture entities: `unverified`
- Post-cleanup total actor entities: `unverified`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T152054391Z-e91fc9b9`
- Measurement: configured 300 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown 0.482 s; scene restore restored (0.882 s); total attempt wall-clock 132.564 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

BLOCKED-PREREQUISITE: Stage 1.4 real-player preflight did not complete: Stage 1.4 requires exactly 1 real player connection(s); PurrNet reports 0.

Stop reason: `PREREQUISITE_BLOCKED`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
No workload execution occurred.
