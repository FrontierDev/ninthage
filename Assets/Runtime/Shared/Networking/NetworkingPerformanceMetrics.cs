using System;
using System.Text;

using Game.Shared.Performance;
using PurrNet;

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
        public const string TRANSPORT_CONNECTION_ROUND_TRIP_TIME_UNAVAILABLE_REASON =
            "The selected PurrNet transport reports measuresRoundTripTime == false; no per-connection RTT sample is available.";
        public const string TRANSPORT_UDP_NATIVE_STATISTICS_UNAVAILABLE_REASON =
            "The selected transport is not PurrNet UDPTransport; UDP-native statistics are unavailable.";
        public const string TRANSPORT_LATENCY_UNAVAILABLE_REASON =
            "PurrNet exposes transport-dependent RTT samples through ITransport.GetRoundTripTime; this generic metric remains unavailable because server per-connection RTT is recorded separately.";

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
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "purrnet_payload",
            "connection_interval");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "purrnet_payload",
            "connection_interval");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "purrnet_transport",
            "connection_interval");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "purrnet_udp_transport",
            "network_interval");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "purrnet_udp_transport",
            "network_interval");
        public static readonly PerformanceMetricDefinition SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT = Metric(
            PerformanceMetricIds.SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "purrnet_udp_transport",
            "network_interval");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_OWNER_PRIVATE_PUBLICATION_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_OWNER_PRIVATE_PUBLICATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_OWNER_PRIVATE_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_OWNER_PRIVATE_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_COSMETIC_TRANSIENT_PUBLICATION_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_COSMETIC_TRANSIENT_PUBLICATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
        public static readonly PerformanceMetricDefinition SERVER_REPLICATION_COSMETIC_TRANSIENT_RECIPIENT_COUNT = Metric(
            PerformanceMetricIds.SERVER_REPLICATION_COSMETIC_TRANSIENT_RECIPIENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "replication_source",
            "server_publication");
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
        public static readonly PerformanceMetricDefinition VISIBILITY_CANDIDATE_COUNT = Metric(
            PerformanceMetricIds.NETWORK_INTEREST_VISIBILITY_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "interest",
            "visibility_evaluation");
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

        public static readonly PerformanceMetricDefinition RPC_SERVER_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_SERVER_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Server);
        public static readonly PerformanceMetricDefinition RPC_CLIENT_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_CLIENT_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Server);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Server);
        public static readonly PerformanceMetricDefinition RPC_ACCOUNT_CLIENT_ENTER_WORLD_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACCOUNT_CLIENT_ENTER_WORLD_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Server);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_TARGET_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_TARGET_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_SERVER_SPELL_CAST_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_SERVER_SPELL_CAST_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Server);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_SPELL_CAST_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_SPELL_CAST_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_COOLDOWN_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_COOLDOWN_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_ACTOR_CLIENT_SFX_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_ACTOR_CLIENT_SFX_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);
        public static readonly PerformanceMetricDefinition RPC_STAT_CLIENT_SNAPSHOT_HANDLER_INVOCATION_COUNT = RpcHandlerMetric(
            PerformanceMetricIds.NETWORK_RPC_STAT_CLIENT_SNAPSHOT_HANDLER_INVOCATION_COUNT,
            PerformanceMetricSide.Client);

        public static readonly PerformanceMetricDefinition SERVER_NETWORK_PAYLOAD_BYTES = Metric(
            PerformanceMetricIds.SERVER_NETWORK_PAYLOAD_BYTES,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "transport_payload",
            "aggregate");

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
        /// <summary>Generic latency metric remains unavailable; use <see cref="TRANSPORT_LATENCY_UNAVAILABLE_REASON"/>.</summary>
        public static readonly PerformanceMetricDefinition TRANSPORT_LATENCY = Unsupported(PerformanceMetricIds.NETWORK_TRANSPORT_LATENCY, PerformanceMetricUnit.Milliseconds);

        /// <summary>
        /// Registers the networking metrics that can be observed on the server.
        /// The benchmark runner calls this after starting its telemetry session.
        /// </summary>
        /// <param name="measuresRoundTripTime">Whether the active transport supports per-connection RTT samples.</param>
        /// <param name="supportsUdpNativeStatistics">Whether the active transport is PurrNet UDPTransport.</param>
        public static void RegisterServerMetrics(bool measuresRoundTripTime, bool supportsUdpNativeStatistics)
        {
            RegisterUnavailable(
                SERVER_CONNECTION_ATTEMPT_COUNT,
                SERVER_CONNECTION_ATTEMPT_UNAVAILABLE_REASON);
            RegisterUnavailable(
                SERVER_NETWORK_PAYLOAD_BYTES,
                "The server exposes directional transport payload bytes, not a separate aggregate payload producer.");
            RegisterMeasured(SERVER_TRANSPORT_CONNECTED_COUNT);
            RegisterMeasured(SERVER_AUTHENTICATED_CLIENT_COUNT);
            RegisterMeasured(SERVER_IN_WORLD_CLIENT_COUNT);
            RegisterMeasured(SERVER_DISCONNECTED_COUNT);
            RegisterMeasured(SERVER_LIVE_CONNECTION_COUNT);
            RegisterMeasured(SERVER_TRANSPORT_PAYLOAD_BYTES_SENT);
            RegisterMeasured(SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED);
            RegisterMeasured(SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT);
            RegisterMeasured(SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT);
            if (measuresRoundTripTime)
                RegisterMeasured(SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME);
            else
                RegisterUnavailable(
                    SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME,
                    TRANSPORT_CONNECTION_ROUND_TRIP_TIME_UNAVAILABLE_REASON);
            if (supportsUdpNativeStatistics)
            {
                RegisterMeasured(SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT);
                RegisterMeasured(SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT);
                RegisterMeasured(SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT);
            }
            else
            {
                RegisterUnavailable(
                    SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT,
                    TRANSPORT_UDP_NATIVE_STATISTICS_UNAVAILABLE_REASON);
                RegisterUnavailable(
                    SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT,
                    TRANSPORT_UDP_NATIVE_STATISTICS_UNAVAILABLE_REASON);
                RegisterUnavailable(
                    SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT,
                    TRANSPORT_UDP_NATIVE_STATISTICS_UNAVAILABLE_REASON);
            }
            RegisterMeasured(SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT);
            RegisterMeasured(SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT);
            RegisterMeasured(SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT);
            RegisterMeasured(SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT);
            RegisterMeasured(SERVER_REPLICATION_OWNER_PRIVATE_PUBLICATION_COUNT);
            RegisterMeasured(SERVER_REPLICATION_OWNER_PRIVATE_RECIPIENT_COUNT);
            RegisterMeasured(SERVER_REPLICATION_COSMETIC_TRANSIENT_PUBLICATION_COUNT);
            RegisterMeasured(SERVER_REPLICATION_COSMETIC_TRANSIENT_RECIPIENT_COUNT);

            RegisterMeasured(VISIBILITY_EVALUATION_COUNT);
            RegisterMeasured(VISIBILITY_ACCEPTED_COUNT);
            RegisterMeasured(VISIBILITY_REJECTED_COUNT);
            RegisterMeasured(CHUNK_SUBSCRIPTION_CHANGE_COUNT);
            RegisterMeasured(VISIBILITY_CANDIDATE_COUNT);
            RegisterMeasured(VISIBILITY_EVALUATION_DURATION);
            RegisterMeasured(INTEREST_REQUEST_DURATION);
            RegisterMeasured(INTEREST_VISIBILITY_EVALUATION_COUNT);
            RegisterMeasured(INTEREST_CHANGED_SUBSCRIPTION_COUNT);

            RegisterMeasured(RPC_SERVER_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACCOUNT_SERVER_TRY_CREATE_CHARACTER_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACCOUNT_SERVER_REQUEST_ENTER_WORLD_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_MOVEMENT_SERVER_REQUEST_CHUNK_INTEREST_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_SERVER_SPELL_CAST_HANDLER_INVOCATION_COUNT);

            RegisterSharedMetrics();
        }

        /// <summary>
        /// Registers the networking metrics that can be observed on the client.
        /// The benchmark runner calls this after starting its telemetry session.
        /// </summary>
        public static void RegisterClientMetrics()
        {
            RegisterMeasured(CLIENT_CONNECTION_ATTEMPT_COUNT);
            RegisterMeasured(CLIENT_CONNECTED_COUNT);
            RegisterMeasured(CLIENT_AUTHENTICATED_COUNT);
            RegisterMeasured(CLIENT_IN_WORLD_COUNT);
            RegisterMeasured(CLIENT_DISCONNECTED_COUNT);
            RegisterMeasured(CLIENT_LIVE_CONNECTION_COUNT);
            RegisterMeasured(CLIENT_TRANSPORT_PAYLOAD_BYTES_SENT);
            RegisterMeasured(CLIENT_TRANSPORT_PAYLOAD_BYTES_RECEIVED);

            RegisterMeasured(RPC_CLIENT_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACCOUNT_CLIENT_UPDATE_CHARACTER_LIST_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACCOUNT_CLIENT_NOTIFY_CHARACTER_CREATION_RESULT_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACCOUNT_CLIENT_ENTER_WORLD_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_TARGET_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_SPELL_CAST_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_COOLDOWN_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_ACTOR_CLIENT_SFX_HANDLER_INVOCATION_COUNT);
            RegisterMeasured(RPC_STAT_CLIENT_SNAPSHOT_HANDLER_INVOCATION_COUNT);

            RegisterSharedMetrics();
        }

        private static void RegisterSharedMetrics()
        {
            RegisterMeasured(SERIALIZER_OPERATION_COUNT);
            RegisterEstimated(SERIALIZER_APPLICATION_PAYLOAD_ESTIMATED_BYTES);
            RegisterMeasured(SERIALIZER_WORLD_POSITION_WRITE_DURATION);
            RegisterMeasured(SERIALIZER_WORLD_POSITION_READ_DURATION);
            RegisterMeasured(SERIALIZER_STAT_ENTRY_WRITE_DURATION);
            RegisterMeasured(SERIALIZER_STAT_ENTRY_READ_DURATION);
            RegisterMeasured(SERIALIZER_STAT_SNAPSHOT_WRITE_DURATION);
            RegisterMeasured(SERIALIZER_STAT_SNAPSHOT_READ_DURATION);
            RegisterMeasured(SERIALIZER_WORLD_POSITION_WRITE_BYTES);
            RegisterMeasured(SERIALIZER_WORLD_POSITION_READ_BYTES);
            RegisterMeasured(SERIALIZER_STAT_ENTRY_WRITE_BYTES);
            RegisterMeasured(SERIALIZER_STAT_ENTRY_READ_BYTES);
            RegisterMeasured(SERIALIZER_STAT_SNAPSHOT_WRITE_BYTES);
            RegisterMeasured(SERIALIZER_STAT_SNAPSHOT_READ_BYTES);

            RegisterUnavailable(TRANSPORT_ON_WIRE_BYTES, TRANSPORT_ON_WIRE_BYTES_UNAVAILABLE_REASON);
            RegisterUnavailable(TRANSPORT_RETRANSMISSION_COUNT, TRANSPORT_RETRANSMISSION_UNAVAILABLE_REASON);
            RegisterUnavailable(TRANSPORT_PACKET_LOSS_RATIO, TRANSPORT_PACKET_LOSS_UNAVAILABLE_REASON);
            RegisterUnavailable(TRANSPORT_BACKLOG_COUNT, TRANSPORT_BACKLOG_UNAVAILABLE_REASON);
            RegisterUnavailable(TRANSPORT_LATENCY, TRANSPORT_LATENCY_UNAVAILABLE_REASON);
        }

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

        public static void RecordVisibilityCandidates(int candidateCount)
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(VISIBILITY_CANDIDATE_COUNT, candidateCount);
        }

        /// <summary>Records one combat-critical observer publication and its intended observer fan-out.</summary>
        public static void RecordCombatCriticalObserverPublication(NetworkIdentity identity)
        {
            RecordObserverPublication(
                SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT,
                SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT,
                identity);
        }

        /// <summary>Records one combat-critical targeted publication.</summary>
        public static void RecordCombatCriticalTargetPublication()
        {
            RecordTargetPublication(
                SERVER_REPLICATION_COMBAT_CRITICAL_PUBLICATION_COUNT,
                SERVER_REPLICATION_COMBAT_CRITICAL_RECIPIENT_COUNT);
        }

        /// <summary>Records one movement-presentation observer publication and its intended observer fan-out.</summary>
        public static void RecordMovementPresentationObserverPublication(NetworkIdentity identity)
        {
            RecordObserverPublication(
                SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT,
                SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT,
                identity);
        }

        /// <summary>Records one movement-presentation targeted publication.</summary>
        public static void RecordMovementPresentationTargetPublication()
        {
            RecordTargetPublication(
                SERVER_REPLICATION_MOVEMENT_PRESENTATION_PUBLICATION_COUNT,
                SERVER_REPLICATION_MOVEMENT_PRESENTATION_RECIPIENT_COUNT);
        }

        /// <summary>Records one owner-private targeted publication.</summary>
        public static void RecordOwnerPrivateTargetPublication()
        {
            RecordTargetPublication(
                SERVER_REPLICATION_OWNER_PRIVATE_PUBLICATION_COUNT,
                SERVER_REPLICATION_OWNER_PRIVATE_RECIPIENT_COUNT);
        }

        /// <summary>Records one cosmetic observer publication and its intended observer fan-out.</summary>
        public static void RecordCosmeticObserverPublication(NetworkIdentity identity)
        {
            RecordObserverPublication(
                SERVER_REPLICATION_COSMETIC_TRANSIENT_PUBLICATION_COUNT,
                SERVER_REPLICATION_COSMETIC_TRANSIENT_RECIPIENT_COUNT,
                identity);
        }

        private static void RecordObserverPublication(
            PerformanceMetricDefinition publicationMetric,
            PerformanceMetricDefinition recipientMetric,
            NetworkIdentity identity)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            PerformanceTelemetry.TryRecord(publicationMetric, 1);
            PerformanceTelemetry.TryRecord(recipientMetric, identity.observers.Count);
        }

        private static void RecordTargetPublication(
            PerformanceMetricDefinition publicationMetric,
            PerformanceMetricDefinition recipientMetric)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(publicationMetric, 1);
            PerformanceTelemetry.TryRecord(recipientMetric, 1);
        }

        /// <summary>
        /// Records execution of an RPC handler on the side that ran its body.
        /// This is not an outbound emission or a recipient-fan-out measurement.
        /// </summary>
        public static void RecordRpcHandlerInvocation(PerformanceMetricDefinition category)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(category, 1);
            PerformanceMetricDefinition sideMetric = category.Side switch
            {
                PerformanceMetricSide.Server => RPC_SERVER_HANDLER_INVOCATION_COUNT,
                PerformanceMetricSide.Client => RPC_CLIENT_HANDLER_INVOCATION_COUNT,
                _ => throw new ArgumentException("RPC handler metrics must identify a server or client execution side.", nameof(category))
            };
            PerformanceTelemetry.TryRecord(sideMetric, 1);
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
                + sizeof(float) * 4;
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

        /// <summary>Converts a serializer bit-position delta to encoded payload bytes without touching the packer.</summary>
        public static int GetEncodedByteCount(int startPositionInBits, int endPositionInBits)
        {
            if (startPositionInBits < 0 || endPositionInBits < startPositionInBits)
                throw new ArgumentOutOfRangeException(nameof(endPositionInBits), "Serializer positions must be monotonic.");

            return (endPositionInBits - startPositionInBits + 7) / 8;
        }

        private static PerformanceMetricDefinition RpcHandlerMetric(string metricId, PerformanceMetricSide side)
        {
            return Metric(metricId, PerformanceMetricUnit.Count, side, "rpc_handler", "local_invocation");
        }

        private static PerformanceMetricDefinition SerializerBytes(string metricId)
        {
            return Metric(metricId, PerformanceMetricUnit.Bytes, PerformanceMetricSide.Shared, "purrnet_serializer", "encoded_payload");
        }

        private static PerformanceMetricDefinition Unsupported(string metricId, PerformanceMetricUnit unit)
        {
            return Metric(metricId, unit, PerformanceMetricSide.Shared, "transport", "network_interval");
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

        private static void RegisterMeasured(PerformanceMetricDefinition definition)
        {
            PerformanceTelemetry.RegisterMetric(definition, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private static void RegisterEstimated(PerformanceMetricDefinition definition)
        {
            PerformanceTelemetry.RegisterMetric(
                definition,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC,
                PerformanceMetricObservation.Estimated,
                "Application payload size is estimated from serialized values and excludes PurrNet framing and transport overhead.");
        }

        private static void RegisterUnavailable(PerformanceMetricDefinition definition, string reason)
        {
            PerformanceTelemetry.RegisterMetric(
                definition,
                0,
                PerformanceMetricObservation.Unavailable,
                reason);
        }

    }
}
