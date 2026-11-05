#ifndef NINTH_AGE_ARCHITECTURE_LIT_WEATHERING_INPUT_INCLUDED
#define NINTH_AGE_ARCHITECTURE_LIT_WEATHERING_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

// The layout is identical in every pass, including passes supplied by URP.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _EmissionColor;
half4 _DirtColor;
float4 _DirtTiling;
float4 _DirtOffset;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Parallax;
half _OcclusionStrength;
half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _DirtNormalScale;
half _DirtStrength;
float _DirtFullHeight;
float _DirtFadeHeight;
half _DirtSmoothness;
half _DirtUseMaskMap;
half _Surface;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_ParallaxMap); SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DetailMask); SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap); SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_DirtBaseMap); SAMPLER(sampler_DirtBaseMap);
TEXTURE2D(_DirtMaskMap); SAMPLER(sampler_DirtMaskMap);
TEXTURE2D(_DirtNormalMap); SAMPLER(sampler_DirtNormalMap);

#if defined(ARCHITECTURE_FORWARD_PASS)
TEXTURE2D(_NATerrainHeightmap);
float4x4 _NATerrainObjectToTerrain;
float4 _NATerrainSize;
float _NATerrainBlendEnabled;
#endif

void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
#if defined(_PARALLAXMAP)
    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
#endif
}

half3 ApplyDetailNormal(float2 uv, half3 normalTS, half mask)
{
#if defined(_DETAIL)
    half3 detail = normalize(UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, uv), _DetailNormalMapScale));
    return lerp(normalTS, BlendNormalRNM(normalTS, detail), mask);
#else
    return normalTS;
#endif
}

void InitializeArchitectureSurface(float2 uv, float2 dirtUV, float3 positionOS, out SurfaceData surface)
{
    surface = (SurfaceData)0;
    half4 baseSample = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    surface.alpha = Alpha(baseSample.a, _BaseColor, _Cutoff);
    surface.albedo = AlphaModulate(baseSample.rgb * _BaseColor.rgb, surface.alpha);
#if defined(_METALLICSPECGLOSSMAP)
    half4 metallic = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    surface.metallic = metallic.r;
    surface.smoothness = metallic.a * _Smoothness;
#else
    surface.metallic = _Metallic;
    surface.smoothness = _Smoothness;
#endif
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    surface.smoothness = baseSample.a * _Smoothness;
#endif
    surface.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
#if defined(_OCCLUSIONMAP)
    surface.occlusion = LerpWhiteTo(SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g, _OcclusionStrength);
#else
    surface.occlusion = 1;
#endif
    surface.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUV = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    half3 detail = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUV).rgb;
#if defined(_DETAIL_SCALED)
    detail = 2.0h * detail * _DetailAlbedoMapScale - _DetailAlbedoMapScale + 1.0h;
#else
    detail *= 2.0h;
#endif
    surface.albedo *= LerpWhiteTo(detail, detailMask);
    surface.normalTS = ApplyDetailNormal(detailUV, surface.normalTS, detailMask);
#endif
#if defined(_DIRT_ON) && defined(ARCHITECTURE_FORWARD_PASS)
    // The renderer binding identifies its assigned Terrain.
    [branch] if (_DirtStrength > 0.0h)
    {
        if (_NATerrainBlendEnabled < 0.5 || any(_NATerrainSize.xyz <= 0.0))
            return;
        float3 terrainLocalPosition = mul(_NATerrainObjectToTerrain, float4(positionOS, 1.0)).xyz;
        float2 terrainUV = terrainLocalPosition.xz / _NATerrainSize.xz;
        if (any(terrainUV < 0.0) || any(terrainUV > 1.0))
            return;
        float4 packedHeight = SAMPLE_TEXTURE2D_LOD(_NATerrainHeightmap, sampler_LinearClamp, terrainUV, 0);
        // Unity stores normalized terrain heights in the 0..32766/65535 range.
        float normalizedHeight = UnpackHeightmap(packedHeight) * (65535.0 / 32766.0);
        float terrainLocalHeight = normalizedHeight * _NATerrainSize.y;
        float verticalDistance = max(0.0, terrainLocalPosition.y - terrainLocalHeight);
        float fullHeight = max(0.0, _DirtFullHeight);
        float fadeHeight = max(fullHeight + 0.001, _DirtFadeHeight);
        float contactMask = 1.0 - smoothstep(fullHeight, fadeHeight, verticalDistance);
        half mask = saturate(contactMask * _DirtStrength);
        float2 uvDirt = dirtUV * _DirtTiling.xy + _DirtOffset.xy;
        half3 dirtAlbedo = SAMPLE_TEXTURE2D(_DirtBaseMap, sampler_DirtBaseMap, uvDirt).rgb * _DirtColor.rgb;
        half4 dirtPacked = half4(0, 1, 0, _DirtSmoothness);
        [branch] if (_DirtUseMaskMap > 0.5h)
            dirtPacked = SAMPLE_TEXTURE2D(_DirtMaskMap, sampler_DirtMaskMap, uvDirt);
        half3 dirtNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_DirtNormalMap, sampler_DirtNormalMap, uvDirt), _DirtNormalScale);
        surface.albedo = lerp(surface.albedo, dirtAlbedo, mask);
        surface.metallic = lerp(surface.metallic, dirtPacked.r, mask);
        surface.occlusion = lerp(surface.occlusion, dirtPacked.g, mask);
        surface.smoothness = lerp(surface.smoothness, dirtPacked.a, mask);
        surface.normalTS = normalize(lerp(surface.normalTS, dirtNormal, mask));
    }
#endif
}

#if defined(ARCHITECTURE_FORWARD_PASS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct ArchitectureAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ArchitectureVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    half4 tangentWS : TEXCOORD2;
    float4 uv : TEXCOORD3;
    half4 fogAndVertexLight : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
#if defined(DYNAMICLIGHTMAP_ON)
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD7;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
    float4 probeOcclusion : TEXCOORD8;
#endif
    float3 positionOS : TEXCOORD9;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ArchitectureVaryings ArchitectureVertex(ArchitectureAttributes input)
{
    ArchitectureVaryings output = (ArchitectureVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = position.positionCS;
    output.positionOS = input.positionOS.xyz;
    output.positionWS = position.positionWS;
    output.normalWS = normal.normalWS;
    output.tangentWS = half4(normal.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.uv = float4(TRANSFORM_TEX(input.uv, _BaseMap), input.uv);
    half fog = 0;
#if !defined(_FOG_FRAGMENT)
    fog = ComputeFogFactor(position.positionCS.z);
#endif
    output.fogAndVertexLight = half4(fog, VertexLighting(position.positionWS, normal.normalWS));
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
#if defined(DYNAMICLIGHTMAP_ON)
    output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(position.positionWS, normal.normalWS, GetWorldSpaceNormalizeViewDir(position.positionWS), output.vertexSH, output.probeOcclusion);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(position);
#endif
    return output;
}

void ArchitectureFragment(ArchitectureVaryings input, out half4 outColor : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
    float2 uv = input.uv.xy;
#if defined(_PARALLAXMAP)
    ApplyPerPixelDisplacement(GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDir), uv);
#endif
    SurfaceData surface;
    InitializeArchitectureSurface(uv, input.uv.zw, input.positionOS, surface);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    InputData data = (InputData)0;
    data.positionWS = input.positionWS;
    data.positionCS = input.positionCS;
    data.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surface.normalTS, tangentToWorld));
    data.tangentToWorld = tangentToWorld;
    data.viewDirectionWS = viewDir;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    data.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    data.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1), input.fogAndVertexLight.x);
    data.vertexLighting = input.fogAndVertexLight.yzw;
    data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
#if defined(_SCREEN_SPACE_IRRADIANCE)
    data.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
    data.bakedGI = SAMPLE_GI(input.lightmapUV, input.dynamicLightmapUV, input.vertexSH, data.normalWS);
    data.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    data.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(input.positionWS), data.normalWS, viewDir, input.positionCS.xy, input.probeOcclusion, data.shadowMask);
#else
    data.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, data.normalWS);
    data.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#endif
#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surface, data);
#endif
    half4 color = UniversalFragmentPBR(data, surface);
    color.rgb = MixFog(color.rgb, data.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
    outColor = color;
#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
#endif

#if defined(ARCHITECTURE_META_PASS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
struct ArchitectureMetaAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
};
struct ArchitectureMetaVaryings
{
    float4 positionCS : SV_POSITION;
    float4 uv : TEXCOORD0;
};
ArchitectureMetaVaryings ArchitectureMetaVertex(ArchitectureMetaAttributes input)
{
    ArchitectureMetaVaryings output;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.lightmapUV, input.dynamicLightmapUV);
    output.uv = float4(TRANSFORM_TEX(input.uv, _BaseMap), input.uv);
    return output;
}
half4 ArchitectureMetaFragment(ArchitectureMetaVaryings input) : SV_Target
{
    SurfaceData surface;
    InitializeArchitectureSurface(input.uv.xy, input.uv.zw, float3(0, 0, 0), surface);
    BRDFData brdf;
    InitializeBRDFData(surface.albedo, surface.metallic, surface.specular, surface.smoothness, surface.alpha, brdf);
    MetaInput meta = (MetaInput)0;
    meta.Albedo = brdf.diffuse + brdf.specular * brdf.roughness * 0.5h;
    meta.Emission = surface.emission;
    return UnityMetaFragment(meta);
}
#endif

#endif // NINTH_AGE_ARCHITECTURE_LIT_WEATHERING_INPUT_INCLUDED
