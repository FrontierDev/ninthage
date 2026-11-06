using UnityEngine;

namespace Game.Client.Rendering
{
    /// <summary>Binds one Terrain heightmap to the architectural renderers in this hierarchy.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ArchitectureTerrainBlend : MonoBehaviour
    {
        private const string ARCHITECTURE_LIT_SHADER_NAME = "Ninth Age/Architecture Lit Weathering";
        private const string ARCHITECTURE_MASONRY_SHADER_NAME = "Ninth Age/Architecture Masonry Weathering";
        private static readonly int TERRAIN_HEIGHTMAP_ID = Shader.PropertyToID("_NATerrainHeightmap");
        private static readonly int OBJECT_TO_TERRAIN_ID = Shader.PropertyToID("_NATerrainObjectToTerrain");
        private static readonly int TERRAIN_SIZE_ID = Shader.PropertyToID("_NATerrainSize");
        private static readonly int BLEND_ENABLED_ID = Shader.PropertyToID("_NATerrainBlendEnabled");
        private static readonly int WEATHERING_OFFSET_ID = Shader.PropertyToID("_NAWeatheringOffset");

        [SerializeField] private Terrain _terrain;

        private void OnEnable()
        {
            ApplyBindings();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
                ApplyBindings();
            else
                DisableBindings();
        }

        private void OnDisable()
        {
            DisableBindings();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
                ApplyBindings();
        }
#endif

        private void ApplyBindings()
        {
            TerrainData terrainData = _terrain != null ? _terrain.terrainData : null;
            RenderTexture heightmap = terrainData != null ? terrainData.heightmapTexture : null;
            if (heightmap == null)
            {
                DisableBindings();
                return;
            }

            var properties = new MaterialPropertyBlock();
            Vector3 size = terrainData.size;
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!UsesArchitectureShader(renderer))
                    continue;
                Vector3 weatheringOffset = _terrain.transform.InverseTransformPoint(renderer.transform.position);
                renderer.GetPropertyBlock(properties);
                properties.SetVector(WEATHERING_OFFSET_ID, new Vector4(weatheringOffset.x, weatheringOffset.y, weatheringOffset.z, 0f));
                properties.SetTexture(TERRAIN_HEIGHTMAP_ID, heightmap);
                properties.SetMatrix(OBJECT_TO_TERRAIN_ID, _terrain.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix);
                properties.SetVector(TERRAIN_SIZE_ID, new Vector4(size.x, size.y, size.z, 0f));
                properties.SetFloat(BLEND_ENABLED_ID, 1f);
                renderer.SetPropertyBlock(properties);
            }
        }

        private void DisableBindings()
        {
            var properties = new MaterialPropertyBlock();
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!UsesArchitectureShader(renderer))
                    continue;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(BLEND_ENABLED_ID, 0f);
                properties.SetVector(WEATHERING_OFFSET_ID, Vector4.zero);
                renderer.SetPropertyBlock(properties);
            }
        }

        private static bool UsesArchitectureShader(MeshRenderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.shader != null &&
                    (material.shader.name == ARCHITECTURE_LIT_SHADER_NAME ||
                     material.shader.name == ARCHITECTURE_MASONRY_SHADER_NAME))
                    return true;
            return false;
        }
    }
}
