using Game.Shared.Utility;
using PurrNet;
using System;
using UnityEngine;

namespace Game.Shared.Networking
{
    /// <summary>Defines RPCs and callbacks for streamed movement interest.</summary>
    public sealed class MovementEndpoint
    {
        // Client callbacks.
        /// <summary>Raised when the client's chunk interest changes.</summary>
        public static Action<Vector2Int[]> onInterestUpdated;
        /// <summary>Raised when the client requests an interest update.</summary>
        public static Action<Vector2Int> onClientInterestRequest;

        /// <summary>Raised when an owner submits an origin-qualified movement proposal.</summary>
        public static Action<PlayerID, WorldPosition, long, long> onServerPositionProposal;

        // Server correction callback (cheat detection only).
        /// <summary>Raised when the server corrects a client's position.</summary>
        public static Action<Vector2Int, Vector3> onServerCorrection;

        /// <summary>
        /// Receives an owner's movement proposal. Server-side validation and
        /// authoritative commit are handled by the movement validation system.
        /// </summary>
        [ServerRpc]
        public static void Server_SubmitPositionProposal(
            WorldPosition position,
            long ownershipEpoch,
            long proposalSequence,
            RPCInfo rpcInfo = default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FormattedDebug.Log(
                $"[Movement/Receive] sender={rpcInfo.sender} epoch={ownershipEpoch} "
                + $"sequence={proposalSequence} "
                + $"absolute=({position.x:F6},{position.y:F6},{position.z:F6}) receipt only.");
#endif
            onServerPositionProposal?.Invoke(
                rpcInfo.sender,
                position,
                ownershipEpoch,
                proposalSequence);
        }

        /// <summary>Notifies a client of its current chunk interest.</summary>
        [TargetRpc]
        public static void Client_UpdateInterest(PlayerID target, Vector2Int[] newInterest, RPCInfo rpcInfo = default)
        {
            NetworkingPerformanceMetrics.RecordRpcHandlerInvocation(
                NetworkingPerformanceMetrics.RPC_MOVEMENT_CLIENT_UPDATE_INTEREST_HANDLER_INVOCATION_COUNT);
            onInterestUpdated?.Invoke(newInterest);
        }

        /// <summary>
        /// Sent by the server only when it detects an illegal player position.
        /// The client must snap to this corrected position.
        /// </summary>
        [TargetRpc]
        public static void Client_ServerCorrection(PlayerID target, Vector2Int chunk, Vector3 localPosition, RPCInfo rpcInfo = default)
        {
            NetworkingPerformanceMetrics.RecordRpcHandlerInvocation(
                NetworkingPerformanceMetrics.RPC_MOVEMENT_CLIENT_SERVER_CORRECTION_HANDLER_INVOCATION_COUNT);
            onServerCorrection?.Invoke(chunk, localPosition);
        }
    }
}
