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

        // Server callbacks.
        /// <summary>Raised when a player requests a chunk-interest update.</summary>
        public static Action<PlayerID, Vector2Int> onInterestRequest;

        // Server correction callback (cheat detection only).
        /// <summary>Raised when the server corrects a client's position.</summary>
        public static Action<Vector2Int, Vector3> onServerCorrection;

        /// <summary>Requests a chunk-interest update from the server.</summary>
        [ServerRpc]
        public static void Server_RequestChunkInterest(Vector2Int centerChunk, RPCInfo rpcInfo = default)
        {
            onInterestRequest?.Invoke(rpcInfo.sender, centerChunk);
        }

        /// <summary>Notifies a client of its current chunk interest.</summary>
        [TargetRpc]
        public static void Client_UpdateInterest(PlayerID target, Vector2Int[] newInterest, RPCInfo rpcInfo = default)
        {
            onInterestUpdated?.Invoke(newInterest);
        }

        /// <summary>
        /// Sent by the server only when it detects an illegal player position.
        /// The client must snap to this corrected position.
        /// </summary>
        [TargetRpc]
        public static void Client_ServerCorrection(PlayerID target, Vector2Int chunk, Vector3 localPosition, RPCInfo rpcInfo = default)
        {
            onServerCorrection?.Invoke(chunk, localPosition);
        }
    }
}
