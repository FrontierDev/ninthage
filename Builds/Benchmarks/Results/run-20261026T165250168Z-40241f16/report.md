# Performance workload run

- Status: `FAIL-INSTRUMENTATION`
- Run ID: `run-20261026T165250168Z-40241f16`
- Profile: `P1-S1.4-NPC-ACTIVITY`
- World fixture: `Outdoor` (`Assets/Tests/Performance/Fixtures/Worlds/Test_WorldFixture_Outdoor.unity`)
- Workload: `FullNetworkedActors`
- Seed: `424242`
- Git commit: `f6ed231dc06b4987a33ab57cc484234e9099cafa` (dirty: `true`)
- Requested full networked NPCs: `100`
- NPC activity scenario: `ForcedFull`
- Real players achieved: `1/1`
- Full NPCs achieved: `100/100`
- Ramp-completed ECS entities: `0`
- Measurement-start ECS entities: `0`
- Measurement-end ECS entities: `0`
- Peak ECS entities observed: `100`
- Post-cleanup fixture entities: `unverified`
- Post-cleanup total actor entities: `unverified`
- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)
- Report directory: `C:\Users\recip\Unity\NinthAge\Builds\Benchmarks\Results\run-20261026T165250168Z-40241f16`
- Measurement: configured 300 s; actual unknown; 0 frames / 0 samples
- Editor orchestration: Play Mode startup unknown; teardown unknown; scene restore restored (0.313 s); total attempt wall-clock 30.403 s
- Focus context: unknown; transitions 0; focused 0; unfocused 0; unknown 0; runInBackground True

## Result

FAIL-INSTRUMENTATION: required Stage 1.4 telemetry was unavailable or unmeasured: server.actors.full_count, server.actors.nearby_count, server.actors.dormant_count, server.actor.npc_perception.player_center_count, server.actor.npc_perception.unique_cell_count, server.spatial.candidate_count, server.actor.npc_perception.unique_npc_candidate_count, server.actor.npc_perception.local_pair_count, server.actor.npc_perception.decision_count, server.actor_manager.npc_aggro_duration, server.actor_manager.npc_navigation_duration, server.actor_manager.npc_navigation_sync_duration, server.actor.npc_navigation.active_count, server.actor.npc_activity.reevaluation_duration, server.actor.npc_activity.transition_count, server.simulation.tick_duration, server.simulation.tick_backlog, server.simulation.deadline_miss_count, server.simulation.catch_up_cap_hit_count, server.memory.total_allocated_bytes, server.memory.mono_used_bytes, server.memory.mono_heap_bytes, server.memory.gc_gen0_collection_count, server.memory.gc_gen1_collection_count, server.memory.gc_gen2_collection_count

Stop reason: `MISSING_REQUIRED_TELEMETRY`

This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity.
