using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client.Rendering
{
    /// <summary>Owns the atlas array and renderer bindings for loaded world scenes.</summary>
    public sealed class ClientArchitectureWeatheringManager : MonoBehaviour
    {
        private const int ATLAS_RESOLUTION = 2048;
        private const int MAX_ACTIVE_ATLASES = 64;
        private static readonly int ATLAS_ARRAY_ID = Shader.PropertyToID("_NAArchitectureWeatheringAtlasArray");
        private static readonly int ATLAS_ST_ID = Shader.PropertyToID("_NAArchitectureWeatheringAtlasST");
        private static readonly int ATLAS_SLICE_ID = Shader.PropertyToID("_NAArchitectureWeatheringAtlasSlice");
        private static readonly int ENABLED_ID = Shader.PropertyToID("_NAArchitectureWeatheringEnabled");

        private readonly List<MeshRenderer> _boundRenderers = new();
        private MaterialPropertyBlock _propertyBlock;
        private Texture2DArray _atlasArray;

        /// <summary>Gets the live weathering manager.</summary>
        public static ClientArchitectureWeatheringManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("A client architecture weathering manager already exists.");
            Instance = this;
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;
            DisableBindings();
            Shader.SetGlobalTexture(ATLAS_ARRAY_ID, null);
            if (_atlasArray != null)
                Destroy(_atlasArray);
            Instance = null;
        }

        /// <summary>Refreshes the atlas array and bindings after world-scene membership changes.</summary>
        public void RefreshFromWorldScenes(IReadOnlyList<Scene> worldScenes)
        {
            if (worldScenes == null)
                throw new ArgumentNullException(nameof(worldScenes));

            var atlases = new List<ArchitectureWeatheringAtlasData>();
            var coordinates = new HashSet<Vector2Int>();
            var renderers = new HashSet<MeshRenderer>();
            foreach (Scene scene in worldScenes)
            {
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new ArgumentException("World scenes must be valid and loaded.", nameof(worldScenes));
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (ArchitectureWeatheringAtlasData data in root.GetComponentsInChildren<ArchitectureWeatheringAtlasData>(true))
                    {
                        Texture2D atlas = data.Atlas;
                        if (atlas == null || atlas.width != ATLAS_RESOLUTION || atlas.height != ATLAS_RESOLUTION
                            || atlas.format != TextureFormat.R8 || !coordinates.Add(data.ChunkCoordinate))
                        {
                            DisableBindings();
                            Shader.SetGlobalTexture(ATLAS_ARRAY_ID, null);
                            Debug.LogError($"Invalid or duplicate architecture weathering atlas in {scene.name} for chunk {data.ChunkCoordinate}.", data);
                            return;
                        }
                        foreach (ArchitectureWeatheringAtlasData.RendererBinding binding in data.Bindings)
                        {
                            if (binding.Renderer == null || !renderers.Add(binding.Renderer))
                            {
                                DisableBindings();
                                Shader.SetGlobalTexture(ATLAS_ARRAY_ID, null);
                                Debug.LogError($"Invalid or duplicate weathering renderer binding in {scene.name}.", data);
                                return;
                            }
                        }
                        atlases.Add(data);
                    }
                }
            }
            if (atlases.Count > MAX_ACTIVE_ATLASES)
            {
                DisableBindings();
                Shader.SetGlobalTexture(ATLAS_ARRAY_ID, null);
                Debug.LogError($"Architecture weathering has {atlases.Count} atlases; the maximum is {MAX_ACTIVE_ATLASES}.");
                return;
            }
            atlases.Sort((a, b) =>
            {
                int x = a.ChunkCoordinate.x.CompareTo(b.ChunkCoordinate.x);
                return x != 0 ? x : a.ChunkCoordinate.y.CompareTo(b.ChunkCoordinate.y);
            });

            foreach (MeshRenderer previous in _boundRenderers)
                if (previous != null && !renderers.Contains(previous))
                    SetDisabled(previous);
            _boundRenderers.Clear();
            if (atlases.Count == 0)
            {
                Shader.SetGlobalTexture(ATLAS_ARRAY_ID, null);
                return;
            }
            if (_atlasArray == null || _atlasArray.depth != atlases.Count)
            {
                if (_atlasArray != null)
                    Destroy(_atlasArray);
                _atlasArray = new Texture2DArray(ATLAS_RESOLUTION, ATLAS_RESOLUTION, atlases.Count, TextureFormat.R8, false, true)
                {
                    name = "Loaded architecture weathering atlases",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            for (int slice = 0; slice < atlases.Count; slice++)
                Graphics.CopyTexture(atlases[slice].Atlas, 0, 0, _atlasArray, slice, 0);
            Shader.SetGlobalTexture(ATLAS_ARRAY_ID, _atlasArray);
            for (int slice = 0; slice < atlases.Count; slice++)
            {
                foreach (ArchitectureWeatheringAtlasData.RendererBinding binding in atlases[slice].Bindings)
                {
                    MeshRenderer renderer = binding.Renderer;
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetVector(ATLAS_ST_ID, binding.AtlasST);
                    _propertyBlock.SetFloat(ATLAS_SLICE_ID, slice);
                    _propertyBlock.SetFloat(ENABLED_ID, 1f);
                    renderer.SetPropertyBlock(_propertyBlock);
                    _boundRenderers.Add(renderer);
                }
            }
        }

        private void DisableBindings()
        {
            foreach (MeshRenderer renderer in _boundRenderers)
                if (renderer != null)
                    SetDisabled(renderer);
            _boundRenderers.Clear();
        }

        private void SetDisabled(MeshRenderer renderer)
        {
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ENABLED_ID, 0f);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
