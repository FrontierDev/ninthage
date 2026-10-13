using PurrNet;
using PurrNet.Packing;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Server.ECS
{
    /// <summary>Describes a spawn point available to the server world-spawn system.</summary>
    public struct SpawnPointComponent : IComponentData
    {
        /// <summary>The unique spawn-point identifier.</summary>
        public Guid SpawnPointGuid;
        /// <summary>The scene containing the spawn point.</summary>
        public FixedString128Bytes SceneName;
        /// <summary>The spawn position.</summary>
        public float3 Position;
        /// <summary>The managed prefab registry index.</summary>
        public int PrefabIndex;    // index into a managed prefab list
        /// <summary>The scene index used for scene movement.</summary>
        public int SceneIndex;     // maps back to tile coord for MoveToScene
        /// <summary>Whether this spawn point should be processed this tick.</summary>
        public bool Pending;       // true = needs to spawn this tick
    }

    /// <summary>Describes a spawn command handed to the server spawn manager.</summary>
    public struct SpawnCommand : IComponentData
    {
        /// <summary>The source spawn-point identifier.</summary>
        public Guid SpawnPointGuid;
        /// <summary>The scene containing the spawn point.</summary>
        public FixedString128Bytes SceneName;
        /// <summary>The spawn position.</summary>
        public float3 Position;
        /// <summary>The managed prefab registry index.</summary>
        public int PrefabIndex;
        /// <summary>The scene index used for scene movement.</summary>
        public int SceneIndex;
        /// <summary>The player that owns the spawned actor.</summary>
        public PlayerID Owner;
        /// <summary>The chunk coordinate derived from the scene name.</summary>
        public int2 ChunkCoord; // Chunk coordinate derived from SceneName
    }
}
