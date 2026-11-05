using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.Rendering
{
    /// <summary>Stores a chunk's baked architecture weathering atlas and renderer bindings.</summary>
    public sealed class ArchitectureWeatheringAtlasData : MonoBehaviour
    {
        /// <summary>Associates a renderer with its rectangle in the chunk atlas.</summary>
        [Serializable]
        public struct RendererBinding
        {
            [SerializeField] private MeshRenderer _renderer;
            [SerializeField] private Vector4 _atlasST;

            /// <summary>Gets the bound renderer.</summary>
            public MeshRenderer Renderer => _renderer;

            /// <summary>Gets the atlas scale and offset.</summary>
            public Vector4 AtlasST => _atlasST;

#if UNITY_EDITOR
            /// <summary>Creates a renderer binding for the Editor baker.</summary>
            public RendererBinding(MeshRenderer renderer, Vector4 atlasST)
            {
                if (renderer == null)
                    throw new ArgumentNullException(nameof(renderer));
                _renderer = renderer;
                _atlasST = atlasST;
            }
#endif
        }

        [SerializeField] private Vector2Int _chunkCoordinate;
        [SerializeField] private Texture2D _atlas;
        [SerializeField] private RendererBinding[] _bindings = Array.Empty<RendererBinding>();

        /// <summary>Gets the absolute chunk coordinate.</summary>
        public Vector2Int ChunkCoordinate => _chunkCoordinate;

        /// <summary>Gets the chunk's baked atlas.</summary>
        public Texture2D Atlas => _atlas;

        /// <summary>Gets the renderer-to-atlas bindings.</summary>
        public IReadOnlyList<RendererBinding> Bindings => _bindings;

#if UNITY_EDITOR
        /// <summary>Assigns a baked atlas and its renderer bindings.</summary>
        public void Configure(Vector2Int chunkCoordinate, Texture2D atlas, RendererBinding[] bindings)
        {
            if (atlas == null)
                throw new ArgumentNullException(nameof(atlas));
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            foreach (RendererBinding binding in bindings)
                if (binding.Renderer == null)
                    throw new ArgumentException("Every atlas binding must have a renderer.", nameof(bindings));
            _chunkCoordinate = chunkCoordinate;
            _atlas = atlas;
            _bindings = (RendererBinding[])bindings.Clone();
        }
#endif
    }
}
