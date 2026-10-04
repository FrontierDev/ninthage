using UnityEngine;
using PurrNet;
using System;

namespace Game.Shared.Networking
{
    public sealed class MovementService
    {
        // Client callbacks.
        public static Action<Vector2Int[]> onInterestUpdated;
        public static Action<Vector2Int> onClientInterestRequest;

        // Server callbacks.
        public static Action<PlayerID, Vector2Int> onInterestRequest;

        // Server correction callback (cheat detection only).
        public static Action<Vector2Int, Vector3> onServerCorrection;

        [ServerRpc]
        public static void Server_RequestChunkInterest(Vector2Int centerChunk, RPCInfo rpcInfo = default)
        {
            onInterestRequest?.Invoke(rpcInfo.sender, centerChunk);
        }

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
