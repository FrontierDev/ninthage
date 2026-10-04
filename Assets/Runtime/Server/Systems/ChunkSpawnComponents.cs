using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using PurrNet;
using PurrNet.Packing;
using UnityEngine;
using System;

namespace Game.Server.ECS
{
    public struct SpawnPointComponent : IComponentData
    {
        public Guid SpawnPointGuid;
        public FixedString128Bytes SceneName;
        public float3 Position;
        public int PrefabIndex;    // index into a managed prefab list
        public int SceneIndex;     // maps back to tile coord for MoveToScene
        public bool Pending;       // true = needs to spawn this tick
    }

    public struct SpawnCommand : IComponentData
    {
        public Guid SpawnPointGuid;
        public FixedString128Bytes SceneName;
        public float3 Position;
        public int PrefabIndex;
        public int SceneIndex;
        public PlayerID Owner;
        public int2 ChunkCoord; // Chunk coordinate derived from SceneName
    }
}