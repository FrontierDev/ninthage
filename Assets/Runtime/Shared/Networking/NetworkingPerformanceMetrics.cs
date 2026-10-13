using System.Text;

using Game.Shared.Performance;

namespace Game.Shared.Networking
{
    /// <summary>Project-owned networking metrics recorded at shared networking boundaries.</summary>
    public static class NetworkingPerformanceMetrics
    {
        public const string SERVER_CONNECTION_ATTEMPT_UNAVAILABLE_REASON =
            "The server observes accepted transport connections, not failed client admission attempts; count client attempts at ConnectToServer.";
        public const string TRANSPORT_ON_WIRE_BYTES_UNAVAILABLE_REASON =
            "PurrNet's public ITransport API exposes payload ByteData only; capture framing, headers and transport overhead externally.";
        public const string TRANSPORT_RETRANSMISSION_UNAVAILABLE_REASON =
            "PurrNet's public ITransport API does not expose retransmission counters; use the transport's supported profiler or external capture.";
        public const string TRANSPORT_PACKET_LOSS_UNAVAILABLE_REASON =
            "PurrNet's public ITransport API does not expose packet-loss counters; use the transport's supported profiler or external capture.";
        public const string TRANSPORT_BACKLOG_UNAVAILABLE_REASON =
            "PurrNet's public ITransport API does not expose send backlog counters; use the transport's supported profiler or external capture.";
        public const string TRANSPORT_LATENCY_UNAVAILABLE_REASON =
            "PurrNet's public ITransport API does not expose latency samples; use the transport's supported profiler or external capture.";

        public static readonly PerformanceMetricDefinition SERVER_CONNECTION_ATTEMPT_COUNT = Metric(
            PerformanceMetricIds.SERVER_CONNECTION_ATTEMPT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "connection_attempt");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_CONNECTED_COUNT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_CONNECTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition SERVER_AUTHENTICATED_CLIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_AUTHENTICATED_CLIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "server_process");
        public static readonly PerformanceMetricDefinition SERVER_IN_WORLD_CLIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_IN_WORLD_CLIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "server_process");
        public static readonly PerformanceMetricDefinition SERVER_DISCONNECTED_COUNT = Metric(
            PerformanceMetricIds.SERVER_DISCONNECTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition SERVER_LIVE_CONNECTION_COUNT = Metric(
            PerformanceMetricIds.SERVER_LIVE_CONNECTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "connection",
            "server_process");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_PAYLOAD_BYTES_SENT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_PAYLOAD_BYTES_SENT,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "purrnet_payload",
            "transport_message");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "purrnet_payload",
            "transport_message");
        public static readonly PerformanceMetricDefinition CLIENT_CONNECTION_ATTEMPT_COUNT = Metric(
            PerformanceMetricIds.CLIENT_CONNECTION_ATTEMPT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "connection_attempt");
        public static readonly PerformanceMetricDefinition CLIENT_CONNECTED_COUNT = Metric(
            PerformanceMetricIds.CLIENT_CONNECTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition CLIENT_AUTHENTICATED_COUNT = Metric(
            PerformanceMetricIds.CLIENT_AUTHENTICATED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition CLIENT_IN_WORLD_COUNT = Metric(
            PerformanceMetricIds.CLIENT_IN_WORLD_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition CLIENT_DISCONNECTED_COUNT = Metric(
            PerformanceMetricIds.CLIENT_DISCONNECTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "connection_event");
        public static readonly PerformanceMetricDefinition CLIENT_LIVE_CONNECTION_COUNT = Metric(
            PerformanceMetricIds.CLIENT_LIVE_CONNECTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "connection",
            "client_process");
        public static readonly PerformanceMetricDefinition CLIENT_TRANSPORT_PAYLOAD_BYTES_SENT = Metric(
            PerformanceMetricIds.CLIENT_TRANSPORT_PAYLOAD_BYTES_SENT,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Client,
            "purrnet_payload",
            "transport_message");
        public static readonly PerformanceMetricDefinition CLIENT_TRANSPORT_PAYLOAD_BYTES_RECEIVED = Metric(
            PerformanceMetricIds.CLIENT_TRANSPORT_PAYLOAD_BYTES_RECEIVED,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Client,
            "purrnet_payload",
            "transport_message");

        public static readonly PerformanceMetricDefinition VISIBILITY_EVALUATION_COUNT = Metric(
            PerformanceMetricIds.NETWORK_VISIBILITY_EVALUATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "visibility",
            "server_update");
        public static readonly PerformanceMetricDefinition VISIBILITY_ACCEPTED_COUNT = Metric(
            PerformanceMetricIds.NETWORK_VISIBILITY_ACCEPTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "visibility",
            "server_update");
        public static readonly PerformanceMetricDefinition VISIBILITY_REJECTED_COUNT = Metric(
            PerformanceMetricIds.NETWORK_VISIBILITY_REJECTED_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "visibility",
            "server_update");
        public static readonly PerformanceMetricDefinition CHUNK_SUBSCRIPTION_CHANGE_COUNT = Metric(
            PerformanceMetricIds.NETWORK_CHUNK_SUBSCRIPTION_CHANGE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "interest",
            "server_update");
        public static readonly PerformanceMetricDefinition RELEVANT_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.NETWORK_RELEVANT_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "interest",
            "server_update");
        public static readonly PerformanceMetricDefinition VISIBILITY_EVALUATION_DURATION = Metric(
            PerformanceMetricIds.NETWORK_VISIBILITY_EVALUATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "visibility",
            "server_update");
        public static readonly PerformanceMetricDefinition INTEREST_REQUEST_DURATION = Metric(
            PerformanceMetricIds.SERVER_INTEREST_REQUEST_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "interest",
            "server_update");
        public static readonly PerformanceMetricDefinition INTEREST_VISIBILITY_EVALUATION_COUNT = Metric(
            PerformanceMetricIds.SERVER_INTEREST_VISIBILITY_EVALUATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "interest",
            "server_update");
        public static readonly PerformanceMetricDefinition INTEREST_CHANGED_SUBSCRIPTION_COUNT = Metric(
            PerformanceMetricIds.SERVER_INTEREST_CHANGED_SUBSCRIPTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "interest",
            "server_update");

        public static readonly PerformanceMetricDefinition RPC_EMISSION_COUNT = Metric(
            PerformanceMetricIds.NETWORK_RPC_EMISSION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Shared,
            "rpc",
            "network_message");
        public static readonly PerformanceMetricDefinition RPC_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.NETWORK_RPC_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Shared,
            "rpc",
            "network_message");
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_ENTER_WORLD_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_ENTER_WORLD_COUNT);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_COUNT);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_COUNT);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_TARGET_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_TARGET_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_COMBAT_LOG_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_COMBAT_LOG_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_AURA_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_AURA_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_SPELL_CAST_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_SPELL_CAST_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_COOLDOWN_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_COOLDOWN_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_VFX_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_VFX_COUNT);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_SFX_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_ACTOR_SFX_COUNT);
        public static readonly PerformanceMetricDefinition RPC_STAT_SNAPSHOT_COUNT = RpcCategory(PerformanceMetricIds.NETWORK_RPC_STAT_SNAPSHOT_COUNT);

        public static readonly PerformanceMetricDefinition SERIALIZER_OPERATION_COUNT = Metric(
            PerformanceMetricIds.NETWORK_SERIALIZER_OPERATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Shared,
            "serializer",
            "network_message");
        public static readonly PerformanceMetricDefinition SERIALIZER_APPLICATION_PAYLOAD_ESTIMATED_BYTES = Metric(
            PerformanceMetricIds.NETWORK_SERIALIZER_APPLICATION_PAYLOAD_ESTIMATED_BYTES,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Shared,
            "serializer",
            "network_message");
        public static readonly PerformanceMetricDefinition SERIALIZER_WORLD_POSITION_WRITE_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_WORLD_POSITION_WRITE_DURATION);
        public static readonly PerformanceMetricDefinition SERIALIZER_WORLD_POSITION_READ_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_WORLD_POSITION_READ_DURATION);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_ENTRY_WRITE_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_STAT_ENTRY_WRITE_DURATION);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_ENTRY_READ_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_STAT_ENTRY_READ_DURATION);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_SNAPSHOT_WRITE_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_STAT_SNAPSHOT_WRITE_DURATION);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_SNAPSHOT_READ_DURATION = SerializerDuration(
            PerformanceMetricIds.NETWORK_SERIALIZER_STAT_SNAPSHOT_READ_DURATION);

        private static PerformanceMetricDefinition SerializerDuration(string metricId)
        {
            return Metric(metricId, PerformanceMetricUnit.Milliseconds, PerformanceMetricSide.Shared, "purrnet_serializer", "network_message");
        }

        public static readonly PerformanceMetricDefinition SERIALIZER_WORLD_POSITION_WRITE_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_WORLD_POSITION_WRITE_BYTES);
        public static readonly PerformanceMetricDefinition SERIALIZER_WORLD_POSITION_READ_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_WORLD_POSITION_READ_BYTES);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_ENTRY_WRITE_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_STAT_ENTRY_WRITE_BYTES);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_ENTRY_READ_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_STAT_ENTRY_READ_BYTES);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_SNAPSHOT_WRITE_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_STAT_SNAPSHOT_WRITE_BYTES);
        public static readonly PerformanceMetricDefinition SERIALIZER_STAT_SNAPSHOT_READ_BYTES = SerializerBytes(PerformanceMetricIds.NETWORK_SERIALIZER_STAT_SNAPSHOT_READ_BYTES);

        /// <summary>Unsupported without public transport framing counters; use <see cref="TRANSPORT_ON_WIRE_BYTES_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_ON_WIRE_BYTES = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_ON_WIRE_BYTES, PerformanceMetricUnit.Bytes);
        /// <summary>Unsupported without public retransmission counters; use <see cref="TRANSPORT_RETRANSMISSION_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_RETRANSMISSION_COUNT = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_RETRANSMISSION_COUNT, PerformanceMetricUnit.Count);
        /// <summary>Unsupported without public packet-loss counters; use <see cref="TRANSPORT_PACKET_LOSS_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_PACKET_LOSS_RATIO = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_PACKET_LOSS_RATIO, PerformanceMetricUnit.Ratio);
        /// <summary>Unsupported without public backlog counters; use <see cref="TRANSPORT_BACKLOG_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_BACKLOG_COUNT = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_BACKLOG_COUNT, PerformanceMetricUnit.Count);
        /// <summary>Unsupported without public latency samples; use <see cref="TRANSPORT_LATENCY_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_LATENCY = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_LATENCY, PerformanceMetricUnit.Milliseconds);

        public static void RecordVisibility(bool accepted)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(VISIBILITY_EVALUATION_COUNT, 1);
            PerformanceTelemetry.TryRecord(accepted ? VISIBILITY_ACCEPTED_COUNT : VISIBILITY_REJECTED_COUNT, 1);
        }

        public static void RecordChunkSubscriptionChange()
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(CHUNK_SUBSCRIPTION_CHANGE_COUNT, 1);
        }

        public static void RecordRelevantRecipient(int recipientCount = 1)
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(RELEVANT_RECIPIENT_COUNT, recipientCount);
        }

        public static void RecordRpc(PerformanceMetricDefinition category, int recipientCount = -1)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(RPC_EMISSION_COUNT, 1);
            PerformanceTelemetry.TryRecord(category, 1);
            if (recipientCount >= 0)
                PerformanceTelemetry.TryRecord(RPC_RECIPIENT_COUNT, recipientCount);
        }

        public static void RecordSerializer(
            PerformanceMetricDefinition encodedBytes,
            int applicationPayloadBytes,
            int encodedByteCount)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(SERIALIZER_OPERATION_COUNT, 1);
            PerformanceTelemetry.TryRecord(SERIALIZER_APPLICATION_PAYLOAD_ESTIMATED_BYTES, applicationPayloadBytes);
            PerformanceTelemetry.TryRecord(encodedBytes, encodedByteCount);
        }

        public static void RecordEncodedSerializer(
            PerformanceMetricDefinition encodedBytes,
            int encodedByteCount)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(SERIALIZER_OPERATION_COUNT, 1);
            PerformanceTelemetry.TryRecord(encodedBytes, encodedByteCount);
        }

        public static int EstimateWorldPositionPayloadBytes()
        {
            return sizeof(int) * 2 + sizeof(float) * 3;
        }

        public static int EstimateStatEntryPayloadBytes(StatSnapshotEntry value)
        {
            return (value.StatID == null ? 0 : Encoding.UTF8.GetByteCount(value.StatID))
                + sizeof(float) * 2;
        }

        public static int EstimateStatSnapshotPayloadBytes(StatSnapshot value)
        {
            int result = sizeof(int);
            if (value.Stats == null)
                return result;

            for (int i = 0; i < value.Stats.Length; i++)
                result += EstimateStatEntryPayloadBytes(value.Stats[i]);
            return result;
        }

        private static PerformanceMetricDefinition RpcCategory(string metricId)
        {
            return Metric(metricId, PerformanceMetricUnit.Count, PerformanceMetricSide.Shared, "rpc", "network_message");
        }

        private static PerformanceMetricDefinition SerializerBytes(string metricId)
        {
            return Metric(metricId, PerformanceMetricUnit.Bytes, PerformanceMetricSide.Shared, "purrnet_serializer", "network_message");
        }

        private static PerformanceMetricDefinition Unsupported(string metricId, PerformanceMetricUnit unit)
        {
            return Metric(metricId, unit, PerformanceMetricSide.Shared, "transport_on_wire", "network_interval");
        }

        private static PerformanceMetricDefinition Metric(
            string metricId,
            PerformanceMetricUnit unit,
            PerformanceMetricSide side,
            string subsystem,
            string scope)
        {
            return new PerformanceMetricDefinition(metricId, unit, side, subsystem, scope);
        }
    }
}
