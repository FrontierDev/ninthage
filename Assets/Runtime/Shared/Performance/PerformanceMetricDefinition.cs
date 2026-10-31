using System;

namespace Game.Shared.Performance
{
    /// <summary>Describes the stable identity and dimensions of a telemetry metric.</summary>
    public sealed class PerformanceMetricDefinition
    {
        public PerformanceMetricDefinition(
            string metricId,
            PerformanceMetricUnit unit,
            PerformanceMetricSide side,
            string subsystem,
            string scope)
        {
            if (string.IsNullOrWhiteSpace(metricId))
                throw new ArgumentException("A metric ID is required.", nameof(metricId));
            if (string.IsNullOrWhiteSpace(subsystem))
                throw new ArgumentException("A metric subsystem is required.", nameof(subsystem));
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("A metric scope is required.", nameof(scope));
            if (metricId.Length > 128)
                throw new ArgumentException("A metric ID exceeds its bounded length.", nameof(metricId));
            if (subsystem.Length > 128)
                throw new ArgumentException("A metric subsystem exceeds its bounded length.", nameof(subsystem));
            if (scope.Length > 128)
                throw new ArgumentException("A metric scope exceeds its bounded length.", nameof(scope));

            MetricId = metricId;
            Unit = unit;
            Side = side;
            Subsystem = subsystem;
            Scope = scope;
        }

        public string MetricId { get; }

        public PerformanceMetricUnit Unit { get; }

        public PerformanceMetricSide Side { get; }

        public string Subsystem { get; }

        public string Scope { get; }
    }

    /// <summary>Canonical units used by live performance reports.</summary>
    public enum PerformanceMetricUnit
    {
        Milliseconds,
        Bytes,
        Count,
        Ticks,
        EventsPerSecond,
        BytesPerSecond,
        PacketsPerSecond,
        Ratio,
        Metres
    }

    /// <summary>Identifies the process or runtime side that produced a metric.</summary>
    public enum PerformanceMetricSide
    {
        Shared,
        Server,
        Client
    }

    /// <summary>Describes whether a metric has a direct observation for this run.</summary>
    public enum PerformanceMetricObservation
    {
        Measured,
        Estimated,
        Unsupported,
        Unavailable,
        NotApplicable
    }

    /// <summary>Stable IDs for metrics that later instrumentation may produce.</summary>
    public static class PerformanceMetricIds
    {
        public const string SERVER_ECS_INITIALIZATION_DURATION = "server.ecs.initialization_duration";
        public const string SERVER_ECS_WORLD_SPAWN_DURATION = "server.ecs.world_spawn_duration";
        public const string SERVER_ECS_ACTOR_UPDATE_DURATION = "server.ecs.actor_update_duration";
        public const string SERVER_ECS_RESOURCE_REGEN_DURATION = "server.ecs.resource_regen_duration";
        public const string SERVER_ECS_SPELL_CAST_DURATION = "server.ecs.spell_cast_duration";
        public const string SERVER_ECS_COOLDOWN_TICK_DURATION = "server.ecs.cooldown_tick_duration";
        public const string SERVER_ECS_AURA_TICK_DURATION = "server.ecs.aura_tick_duration";
        public const string SERVER_ECS_ACTOR_TICKER_DURATION = "server.ecs.actor_ticker_duration";
        public const string SERVER_ECS_SPAWN_COMMAND_COUNT = "server.ecs.spawn_command_count";
        public const string SERVER_ECS_ACTIVE_CAST_COUNT = "server.ecs.active_cast_count";
        public const string SERVER_ECS_COOLDOWN_ENTRY_COUNT = "server.ecs.cooldown_entry_count";
        public const string SERVER_ECS_AURA_ENTRY_COUNT = "server.ecs.aura_entry_count";
        public const string SERVER_ECS_TICKER_ENTRY_COUNT = "server.ecs.ticker_entry_count";
        public const string SERVER_SIMULATION_TICK_DURATION = "server.simulation.tick_duration";
        public const string SERVER_SIMULATION_TICK_BACKLOG = "server.simulation.tick_backlog";
        public const string SERVER_SIMULATION_TICKS_SCHEDULED = "server.simulation.ticks_scheduled";
        public const string SERVER_SIMULATION_TICKS_EXECUTED = "server.simulation.ticks_executed";
        public const string SERVER_SIMULATION_DEADLINE_MISS_COUNT = "server.simulation.deadline_miss_count";
        public const string SERVER_SIMULATION_CATCH_UP_CAP_HIT_COUNT = "server.simulation.catch_up_cap_hit_count";
        public const string SERVER_ACTORS_DORMANT_COUNT = "server.actors.dormant_count";
        public const string SERVER_ACTORS_FULL_COUNT =
            "server.actors.full_count";
        public const string SERVER_ACTORS_NEARBY_COUNT =
            "server.actors.nearby_count";
        public const string SERVER_ACTOR_NPC_ACTIVITY_REEVALUATION_DURATION =
            "server.actor.npc_activity.reevaluation_duration";
        public const string SERVER_ACTOR_NPC_ACTIVITY_TRANSITION_COUNT =
            "server.actor.npc_activity.transition_count";
        public const string SERVER_ACTOR_NPC_NAVIGATION_ACTIVE_COUNT =
            "server.actor.npc_navigation.active_count";
        public const string SERVER_ACTOR_MANAGER_NPC_NAVIGATION_DURATION =
            "server.actor_manager.npc_navigation_duration";
        public const string SERVER_ACTOR_MANAGER_NPC_NAVIGATION_SYNC_DURATION =
            "server.actor_manager.npc_navigation_sync_duration";
        public const string SERVER_SPATIAL_CANDIDATE_COUNT = "server.spatial.candidate_count";
        public const string SERVER_NETWORK_PAYLOAD_BYTES = "server.network.payload_bytes";
        public const string SERVER_MEMORY_TOTAL_ALLOCATED_BYTES = "server.memory.total_allocated_bytes";
        public const string SERVER_MEMORY_MONO_USED_BYTES = "server.memory.mono_used_bytes";
        public const string SERVER_MEMORY_MONO_HEAP_BYTES = "server.memory.mono_heap_bytes";
        public const string SERVER_MEMORY_GC_GEN0_COLLECTION_COUNT = "server.memory.gc_gen0_collection_count";
        public const string SERVER_MEMORY_GC_GEN1_COLLECTION_COUNT = "server.memory.gc_gen1_collection_count";
        public const string SERVER_MEMORY_GC_GEN2_COLLECTION_COUNT = "server.memory.gc_gen2_collection_count";
        public const string SERVER_ACTOR_MANAGER_UPDATE_DURATION = "server.actor_manager.update_duration";
        public const string SERVER_ACTOR_MANAGER_NPC_AGGRO_DURATION = "server.actor_manager.npc_aggro_duration";
        public const string SERVER_ACTOR_NPC_PERCEPTION_PLAYER_CENTER_COUNT =
            "server.actor.npc_perception.player_center_count";
        public const string SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_CELL_COUNT =
            "server.actor.npc_perception.unique_cell_count";
        public const string SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT =
            "server.actor.npc_perception.unique_npc_candidate_count";
        public const string SERVER_ACTOR_NPC_PERCEPTION_LOCAL_PAIR_COUNT =
            "server.actor.npc_perception.local_pair_count";
        public const string SERVER_ACTOR_NPC_PERCEPTION_DECISION_COUNT =
            "server.actor.npc_perception.decision_count";
        public const string SERVER_ACTOR_MANAGER_POSITION_VALIDATION_DURATION = "server.actor_manager.position_validation_duration";
        public const string SERVER_ACTOR_MANAGER_PLAYER_ECS_SYNC_DURATION = "server.actor_manager.player_ecs_sync_duration";
        public const string SERVER_ACTOR_PLAYER_MOVEMENT_WORK_COUNT = "server.actor.player_movement_work_count";
        public const string SERVER_ACTOR_PENDING_POSITION_PROPOSAL_COUNT = "server.actor.pending_position_proposal_count";
        public const string SERVER_ACTOR_DUE_POSITION_VALIDATION_COUNT = "server.actor.due_position_validation_count";
        public const string SERVER_ACTOR_ECS_COUNT = "server.actor.ecs_count";
        public const string SERVER_ACTOR_NETWORK_GAMEOBJECT_COUNT = "server.actor.network_gameobject_count";
        public const string SERVER_ACTOR_NPC_OVERLAP_SEARCH_COUNT = "server.actor.npc_overlap_search_count";
        public const string SERVER_ACTOR_COLLIDER_CANDIDATE_COUNT = "server.actor.collider_candidate_count";
        public const string SERVER_ACTOR_FULL_ENTITY_SCAN_COUNT = "server.actor.full_entity_scan_count";
        public const string SERVER_ACTOR_ECS_GAMEOBJECT_SYNC_COUNT = "server.actor.ecs_gameobject_sync_count";
        public const string SERVER_ACTOR_OWNER_CHANGE_ENTITY_SCAN_COUNT = "server.actor.owner_change_entity_scan_count";
        public const string SERVER_ACTOR_INTEREST_CHANGE_ENTITY_SCAN_COUNT = "server.actor.interest_change_entity_scan_count";
        public const string SERVER_ACTOR_ALLOC_HEAVY_OPERATION_COUNT = "server.actor.alloc_heavy_operation_count";
        public const string SERVER_ACTOR_SPAWN_EVENT_COUNT = "server.actor.spawn_event_count";
        public const string SERVER_ACTOR_DESPAWN_EVENT_COUNT = "server.actor.despawn_event_count";
        public const string SERVER_AUTO_ATTACK_UPDATE_DURATION = "server.auto_attack.update_duration";
        public const string SERVER_AUTO_ATTACKER_COUNT = "server.auto_attack.attacker_count";
        public const string SERVER_SPAWN_UPDATE_DURATION = "server.spawn.update_duration";
        public const string SERVER_SPAWN_COMMAND_COUNT = "server.spawn.command_count";
        public const string SERVER_SPAWN_SUCCESS_COUNT = "server.spawn.success_count";
        public const string SERVER_SPAWN_FAILURE_COUNT = "server.spawn.failure_count";
        public const string SERVER_SPAWN_SCENE_LOAD_DURATION = "server.spawn.scene_load_duration";
        public const string SERVER_WORLD_SCENE_LOAD_DURATION = "server.world.scene_load_duration";
        public const string SERVER_WORLD_SCENE_UNLOAD_DURATION = "server.world.scene_unload_duration";
        public const string SERVER_WORLD_PENDING_SCENE_OPERATION_COUNT = "server.world.pending_scene_operation_count";
        public const string SERVER_INTEREST_REQUEST_DURATION = "server.interest.request_duration";
        public const string SERVER_INTEREST_VISIBILITY_EVALUATION_COUNT = "server.interest.visibility_evaluation_count";
        public const string SERVER_INTEREST_CHANGED_SUBSCRIPTION_COUNT = "server.interest.changed_subscription_count";
        public const string SERVER_CONNECTION_ATTEMPT_COUNT = "server.connection.attempt_count";
        public const string SERVER_TRANSPORT_CONNECTED_COUNT = "server.connection.transport_connected_count";
        public const string SERVER_AUTHENTICATED_CLIENT_COUNT = "server.connection.authenticated_client_count";
        public const string SERVER_IN_WORLD_CLIENT_COUNT = "server.connection.in_world_client_count";
        /// <summary>Counts admitted actors currently detached within disconnect grace.</summary>
        public const string SERVER_DISCONNECTED_GRACE_ACTOR_COUNT =
            "server.session.disconnected_grace_actor_count";
        /// <summary>Counts World actor reclaim attempts for reclaimed gameplay sessions.</summary>
        public const string SERVER_RECONNECT_ATTEMPT_COUNT =
            "server.session.reconnect_attempt_count";
        /// <summary>Counts successful World actor reclaim operations.</summary>
        public const string SERVER_RECONNECT_SUCCESS_COUNT =
            "server.session.reconnect_success_count";
        /// <summary>Counts failed World actor reclaim operations.</summary>
        public const string SERVER_RECONNECT_FAILURE_COUNT =
            "server.session.reconnect_failure_count";
        /// <summary>Counts disconnect-grace finalizations that actually begin.</summary>
        public const string SERVER_DISCONNECT_FINALIZATION_AFTER_GRACE_COUNT =
            "server.session.disconnect_finalization_after_grace_count";
        /// <summary>Counts typed nonaccepted Master character-admission responses.</summary>
        public const string SERVER_ADMISSION_REJECTION_COUNT =
            "server.session.admission_rejection_count";
        /// <summary>Counts normal logout operations after their authoritative start boundary.</summary>
        public const string SERVER_NORMAL_LOGOUT_STARTED_COUNT =
            "server.session.normal_logout_started_count";
        /// <summary>Counts started normal logouts abandoned before finalization.</summary>
        public const string SERVER_NORMAL_LOGOUT_CANCELLED_COUNT =
            "server.session.normal_logout_cancelled_count";
        /// <summary>Counts normal logouts after durable character leave and actor retirement.</summary>
        public const string SERVER_NORMAL_LOGOUT_COMPLETED_COUNT =
            "server.session.normal_logout_completed_count";
        public const string SERVER_DISCONNECTED_COUNT = "server.connection.disconnected_count";
        public const string SERVER_LIVE_CONNECTION_COUNT = "server.connection.live_count";
        public const string CLIENT_CONNECTED_COUNT = "client.connection.connected_count";
        public const string CLIENT_AUTHENTICATED_COUNT = "client.connection.authenticated_count";
        public const string CLIENT_IN_WORLD_COUNT = "client.connection.in_world_count";
        public const string CLIENT_DISCONNECTED_COUNT = "client.connection.disconnected_count";
        public const string SERVER_TRANSPORT_PAYLOAD_BYTES_SENT = "server.transport.payload_bytes_sent";
        public const string SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED = "server.transport.payload_bytes_received";
        public const string SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT =
            "server.transport.connection.payload_bytes_sent";
        public const string SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT =
            "server.transport.connection.message_count_sent";
        public const string SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_PER_SECOND =
            "server.transport.connection.payload_bytes_per_second";
        public const string SERVER_TRANSPORT_CONNECTION_MESSAGES_PER_SECOND =
            "server.transport.connection.messages_per_second";
        public const string SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME =
            "server.transport.connection.round_trip_time";
        public const string SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT =
            "server.transport.udp.native_bytes_sent";
        public const string SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT =
            "server.transport.udp.native_packets_sent";
        public const string SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT =
            "server.transport.udp.packet_loss_count";
        public const string SERVER_TRANSPORT_UDP_NATIVE_BYTES_PER_SECOND =
            "server.transport.udp.native_bytes_per_second";
        public const string SERVER_TRANSPORT_UDP_NATIVE_PACKETS_PER_SECOND =
            "server.transport.udp.native_packets_per_second";
        public const string SERVER_TRANSPORT_UDP_PACKET_LOSS_EVENTS_PER_SECOND =
            "server.transport.udp.packet_loss_events_per_second";
        public const string SERVER_TRANSPORT_AGGREGATE_PAYLOAD_BYTES_PER_SECOND =
            "server.transport.aggregate.payload_bytes_per_second";
        public const string SERVER_TRANSPORT_AGGREGATE_MESSAGES_PER_SECOND =
            "server.transport.aggregate.messages_per_second";
        public const string SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT =
            "server.replication.combat_critical.publication_count";
        public const string SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT =
            "server.replication.combat_critical.recipient_count";
        public const string SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT =
            "server.replication.movement_presentation.publication_count";
        public const string SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT =
            "server.replication.movement_presentation.recipient_count";
        public const string SERVER_REPLICATION_OWNER_PRIVATE_PUBLICATION_COUNT =
            "server.replication.owner_private.publication_count";
        public const string SERVER_REPLICATION_OWNER_PRIVATE_RECIPIENT_COUNT =
            "server.replication.owner_private.recipient_count";
        public const string SERVER_REPLICATION_COSMETIC_TRANSIENT_PUBLICATION_COUNT =
            "server.replication.cosmetic_transient.publication_count";
        public const string SERVER_REPLICATION_COSMETIC_TRANSIENT_RECIPIENT_COUNT =
            "server.replication.cosmetic_transient.recipient_count";
        public const string CLIENT_CONNECTION_ATTEMPT_COUNT = "client.connection.attempt_count";
        public const string CLIENT_LIVE_CONNECTION_COUNT = "client.connection.live_count";
        public const string CLIENT_TRANSPORT_PAYLOAD_BYTES_SENT = "client.transport.payload_bytes_sent";
        public const string CLIENT_TRANSPORT_PAYLOAD_BYTES_RECEIVED = "client.transport.payload_bytes_received";
        public const string NETWORK_VISIBILITY_EVALUATION_COUNT = "network.visibility.evaluation_count";
        public const string NETWORK_VISIBILITY_ACCEPTED_COUNT = "network.visibility.accepted_count";
        public const string NETWORK_VISIBILITY_REJECTED_COUNT = "network.visibility.rejected_count";
        public const string NETWORK_CHUNK_SUBSCRIPTION_CHANGE_COUNT = "network.chunk.subscription_change_count";
        public const string NETWORK_INTEREST_VISIBILITY_CANDIDATE_COUNT = "network.interest.visibility_candidate_count";
        public const string NETWORK_VISIBILITY_EVALUATION_DURATION = "network.visibility.evaluation_duration";
        public const string NETWORK_RPC_SERVER_HANDLER_INVOCATION_COUNT = "network.rpc.server.handler_invocation_count";
        public const string NETWORK_RPC_CLIENT_HANDLER_INVOCATION_COUNT = "network.rpc.client.handler_invocation_count";
        public const string NETWORK_RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_HANDLER_INVOCATION_COUNT = "network.rpc.account.client_update_character_list.handler_invocation_count";
        public const string NETWORK_RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_HANDLER_INVOCATION_COUNT = "network.rpc.account.server_try_create_character.handler_invocation_count";
        public const string NETWORK_RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_HANDLER_INVOCATION_COUNT = "network.rpc.account.client_notify_character_creation_result.handler_invocation_count";
        public const string NETWORK_RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_HANDLER_INVOCATION_COUNT = "network.rpc.account.server_request_enter_world.handler_invocation_count";
        public const string NETWORK_RPC_ACCOUNT_CLIENT_ENTER_WORLD_HANDLER_INVOCATION_COUNT = "network.rpc.account.client_enter_world.handler_invocation_count";
        public const string NETWORK_RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_HANDLER_INVOCATION_COUNT = "network.rpc.movement.server_request_chunk_interest.handler_invocation_count";
        public const string NETWORK_RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_HANDLER_INVOCATION_COUNT = "network.rpc.movement.client_update_interest.handler_invocation_count";
        public const string NETWORK_RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_HANDLER_INVOCATION_COUNT = "network.rpc.movement.client_server_correction.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_TARGET_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_target.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_combat_log.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_aura.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_SERVER_SPELL_CAST_HANDLER_INVOCATION_COUNT = "network.rpc.actor.server_spell_cast.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_SPELL_CAST_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_spell_cast.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_COOLDOWN_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_cooldown.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_vfx.handler_invocation_count";
        public const string NETWORK_RPC_ACTOR_CLIENT_SFX_HANDLER_INVOCATION_COUNT = "network.rpc.actor.client_sfx.handler_invocation_count";
        public const string NETWORK_RPC_STAT_CLIENT_SNAPSHOT_HANDLER_INVOCATION_COUNT = "network.rpc.stat.client_snapshot.handler_invocation_count";
        public const string NETWORK_SERIALIZER_OPERATION_COUNT = "network.serializer.operation_count";
        public const string NETWORK_SERIALIZER_APPLICATION_PAYLOAD_ESTIMATED_BYTES = "network.serializer.application_payload_estimated_bytes";
        public const string NETWORK_SERIALIZER_WORLD_POSITION_WRITE_DURATION = "network.serializer.world_position.write_duration";
        public const string NETWORK_SERIALIZER_WORLD_POSITION_READ_DURATION = "network.serializer.world_position.read_duration";
        public const string NETWORK_SERIALIZER_STAT_ENTRY_WRITE_DURATION = "network.serializer.stat_entry.write_duration";
        public const string NETWORK_SERIALIZER_STAT_ENTRY_READ_DURATION = "network.serializer.stat_entry.read_duration";
        public const string NETWORK_SERIALIZER_STAT_SNAPSHOT_WRITE_DURATION = "network.serializer.stat_snapshot.write_duration";
        public const string NETWORK_SERIALIZER_STAT_SNAPSHOT_READ_DURATION = "network.serializer.stat_snapshot.read_duration";
        public const string NETWORK_SERIALIZER_WORLD_POSITION_WRITE_BYTES = "network.serializer.world_position.write_bytes";
        public const string NETWORK_SERIALIZER_WORLD_POSITION_READ_BYTES = "network.serializer.world_position.read_bytes";
        public const string NETWORK_SERIALIZER_STAT_ENTRY_WRITE_BYTES = "network.serializer.stat_entry.write_bytes";
        public const string NETWORK_SERIALIZER_STAT_ENTRY_READ_BYTES = "network.serializer.stat_entry.read_bytes";
        public const string NETWORK_SERIALIZER_STAT_SNAPSHOT_WRITE_BYTES = "network.serializer.stat_snapshot.write_bytes";
        public const string NETWORK_SERIALIZER_STAT_SNAPSHOT_READ_BYTES = "network.serializer.stat_snapshot.read_bytes";
        public const string NETWORK_TRANSPORT_ON_WIRE_BYTES = "network.transport.on_wire_bytes";
        public const string NETWORK_TRANSPORT_RETRANSMISSION_COUNT = "network.transport.retransmission_count";
        public const string NETWORK_TRANSPORT_PACKET_LOSS_RATIO = "network.transport.packet_loss_ratio";
        public const string NETWORK_TRANSPORT_BACKLOG_COUNT = "network.transport.backlog_count";
        public const string NETWORK_TRANSPORT_LATENCY = "network.transport.latency";
        public const string CLIENT_RENDERED_FRAME_TIME = "client.rendered.frame_time";
        public const string CLIENT_FRAME_INTERVAL = "client.rendered.frame_interval";
        public const string CLIENT_MAIN_THREAD_FRAME_TIME = "client.main_thread.frame_time";
        public const string CLIENT_RENDER_THREAD_FRAME_TIME = "client.render_thread.frame_time";
        public const string CLIENT_GPU_FRAME_TIME = "client.gpu.frame_time";
        public const string CLIENT_SCENE_LOAD_DURATION = "client.scene.load_duration";
        public const string CLIENT_SCENE_ACTIVATION_DURATION = "client.scene.activation_duration";
        public const string CLIENT_STREAMED_SCENE_DELIVERY_CALLBACK_DURATION =
            "client.scene.streamed_delivery_callback_duration";
        public const string CLIENT_SCENE_UNLOAD_DURATION = "client.scene.unload_duration";
        public const string CLIENT_SCENE_UNLOAD_CALLBACK_DURATION = "client.scene.unload_callback_duration";
        public const string CLIENT_SCENE_LOAD_COUNT = "client.scene.load_count";
        public const string CLIENT_SCENE_ACTIVATION_COUNT = "client.scene.activation_count";
        public const string CLIENT_SCENE_UNLOAD_COUNT = "client.scene.unload_count";
        public const string CLIENT_TRACKED_SCENE_COUNT = "client.scene.tracked_count";
        public const string CLIENT_STREAMED_SCENE_LOAD_COUNT = "client.scene.streamed_load_count";
        public const string CLIENT_STREAMED_SCENE_UNLOAD_COUNT = "client.scene.streamed_unload_count";
        public const string CLIENT_PENDING_SCENE_OPERATION_COUNT = "client.scene.pending_operation_count";
        public const string CLIENT_ORIGIN_SHIFT_DURATION = "client.origin.shift_duration";
        public const string CLIENT_ORIGIN_SHIFT_COUNT = "client.origin.shift_count";
        public const string CLIENT_ORIGIN_SHIFT_ROOT_COUNT = "client.origin.shift_root_count";
        public const string CLIENT_FRAME_SPIKE_COUNT = "client.frame.spike_count";
        public const string CLIENT_VISIBLE_ACTOR_COUNT = "client.actor.visible_count";
        public const string CLIENT_NAMEPLATE_COUNT = "client.nameplate.count";
        public const string CLIENT_NAMEPLATE_SPAWN_COUNT = "client.nameplate.spawn_count";
        public const string CLIENT_NAMEPLATE_RELEASE_COUNT = "client.nameplate.release_count";
        public const string CLIENT_NAMEPLATE_UPDATE_DURATION = "client.nameplate.update_duration";
        public const string CLIENT_VFX_ACTIVE_COUNT = "client.vfx.active_count";
        public const string CLIENT_VFX_SPAWN_COUNT = "client.vfx.spawn_count";
        public const string CLIENT_VFX_RELEASE_COUNT = "client.vfx.release_count";
        public const string CLIENT_UI_TRANSIENT_COUNT = "client.ui.transient_count";
        public const string CLIENT_UI_TRANSIENT_SPAWN_COUNT = "client.ui.transient_spawn_count";
        public const string CLIENT_UI_TRANSIENT_RELEASE_COUNT = "client.ui.transient_release_count";
        public const string CLIENT_FCT_POOL_RETAINED_COUNT = "client.ui.fct.pool_retained_count";
        public const string CLIENT_FCT_POOL_CREATE_COUNT = "client.ui.fct.pool_create_count";
        public const string CLIENT_FCT_POOL_REUSE_COUNT = "client.ui.fct.pool_reuse_count";
        public const string CLIENT_FCT_POOL_DESTROY_COUNT = "client.ui.fct.pool_destroy_count";
        public const string CLIENT_DRAW_CALL_COUNT = "client.graphics.draw_call_count";
        public const string CLIENT_BATCH_COUNT = "client.graphics.batch_count";
        public const string CLIENT_TRIANGLE_COUNT = "client.graphics.triangle_count";
        public const string CLIENT_MEMORY_TOTAL_ALLOCATED_BYTES = "client.memory.total_allocated_bytes";
        public const string CLIENT_MEMORY_MONO_USED_BYTES = "client.memory.mono_used_bytes";
        public const string CLIENT_MEMORY_MONO_HEAP_BYTES = "client.memory.mono_heap_bytes";
        public const string CLIENT_MEMORY_GC_ALLOCATED_BYTES = "client.memory.gc_allocated_bytes";
        public const string CLIENT_ADDRESSABLE_LOAD_DURATION = "client.addressables.load_duration";
        public const string CLIENT_ADDRESSABLE_RELEASE_COUNT = "client.addressables.release_count";
        public const string CLIENT_ADDRESSABLE_PENDING_COUNT = "client.addressables.pending_count";
        public const string CLIENT_AUDIO_LIVE_ADDRESSABLE_HANDLE_COUNT = "client.audio.live_addressable_handle_count";
        public const string CLIENT_CAMERA_LIVE_ADDRESSABLE_HANDLE_COUNT = "client.camera.live_addressable_handle_count";
        public const string CLIENT_VFX_LIVE_ADDRESSABLE_HANDLE_COUNT = "client.vfx.live_addressable_handle_count";
        public const string CLIENT_WORLD_PRESENTATION_CLEANUP_DURATION = "client.world.presentation_cleanup_duration";
        public const string CLIENT_AUDIO_CACHE_COUNT = "client.audio.cache_count";
        public const string SHARED_SCENE_OPERATION_DURATION = "shared.scene.operation_duration";
    }

    internal static class PerformanceMetricText
    {
        public static string ToText(PerformanceMetricUnit unit)
        {
            switch (unit)
            {
                case PerformanceMetricUnit.Milliseconds:
                    return "ms";
                case PerformanceMetricUnit.Bytes:
                    return "bytes";
                case PerformanceMetricUnit.Count:
                    return "count";
                case PerformanceMetricUnit.Ticks:
                    return "ticks";
                case PerformanceMetricUnit.EventsPerSecond:
                    return "events_per_second";
                case PerformanceMetricUnit.BytesPerSecond:
                    return "bytes_per_second";
                case PerformanceMetricUnit.PacketsPerSecond:
                    return "packets_per_second";
                case PerformanceMetricUnit.Ratio:
                    return "ratio";
                case PerformanceMetricUnit.Metres:
                    return "m";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown metric unit.");
            }
        }

        public static string ToText(PerformanceMetricSide side)
        {
            switch (side)
            {
                case PerformanceMetricSide.Shared:
                    return "shared";
                case PerformanceMetricSide.Server:
                    return "server";
                case PerformanceMetricSide.Client:
                    return "client";
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown metric side.");
            }
        }

        public static string ToText(PerformanceMetricObservation observation)
        {
            switch (observation)
            {
                case PerformanceMetricObservation.Measured:
                    return "measured";
                case PerformanceMetricObservation.Estimated:
                    return "estimated";
                case PerformanceMetricObservation.Unsupported:
                    return "unsupported";
                case PerformanceMetricObservation.Unavailable:
                    return "unavailable";
                case PerformanceMetricObservation.NotApplicable:
                    return "not_applicable";
                default:
                    throw new ArgumentOutOfRangeException(nameof(observation), observation, "Unknown metric observation.");
            }
        }

    }
}
