Shader "Ninth Age/Architecture Masonry Weathering"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Face", Float) = 2
        [Toggle(_METALLICSPECGLOSSMAP)] _UseMetallicMap("Use Metallic Map", Float) = 0
        _MetallicGlossMap("Metallic Map (R), Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        [Toggle(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)] _SmoothnessFromAlbedo("Smoothness from Base Alpha", Float) = 0
        [Toggle(_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        [Toggle(_PARALLAXMAP)] _UseParallaxMap("Use Height Map", Float) = 0
        _ParallaxMap("Height Map", 2D) = "black" {}
        _Parallax("Height Scale", Range(0.005,0.08)) = 0.005
        [Toggle(_OCCLUSIONMAP)] _UseOcclusionMap("Use Occlusion Map", Float) = 0
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        [Toggle(_EMISSION)] _UseEmission("Use Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        [Toggle(_DETAIL_SCALED)] _UseDetail("Use Detail", Float) = 0
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Base Map", 2D) = "linearGrey" {}
        _DetailAlbedoMapScale("Detail Base Scale", Range(0,2)) = 1
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Range(0,2)) = 1
        [Toggle(_AGING_ON)] _UseAging("Use Aging", Float) = 0
        _SurfaceMask("Surface Mask (R Damage, G Cavity, B Biology, A Grime)", 2D) = "black" {}
        _DamageColor("Damage Color", Color) = (1,1,1,1)
        [Normal] _DamageNormalMap("Damage Normal", 2D) = "bump" {}
        _DamageNormalScale("Damage Normal Scale", Float) = 1
        _DamageStrength("Damage Strength", Range(0,1)) = 0
        _DamageSmoothnessOffset("Damage Smoothness Offset", Range(-1,1)) = -0.15
        _MacroMap("Macro Map (R Albedo, G Smoothness, B Dirt, A Moss)", 2D) = "gray" {}
        _MacroTileSize("Macro Tile Size", Float) = 12
        _MacroBlendSharpness("Macro Blend Sharpness", Float) = 4
        _MacroAlbedoVariation("Macro Albedo Variation", Range(0,1)) = 0.08
        _MacroSmoothnessVariation("Macro Smoothness Variation", Range(0,1)) = 0.10
        [Toggle(_DIRT_ON)] _UseDirt("Use Dirt", Float) = 0
        _DirtBaseMap("Dirt Base Map", 2D) = "white" {}
        _DirtColor("Dirt Color", Color) = (1,1,1,1)
        [ToggleUI] _DirtUseMaskMap("Use Dirt Mask", Float) = 0
        _DirtMaskMap("Dirt Mask (R Metallic, G AO, A Smoothness)", 2D) = "white" {}
        [Normal] _DirtNormalMap("Dirt Normal", 2D) = "bump" {}
        _DirtNormalScale("Dirt Normal Scale", Float) = 1
        _DirtSmoothness("Dirt Smoothness", Range(0,1)) = 0.2
        _DirtTiling("Dirt Tiling", Vector) = (1,1,0,0)
        _DirtOffset("Dirt Offset", Vector) = (0,0,0,0)
        _DirtStrength("Dirt Strength", Range(0,1)) = 0
        _GroundDirtStrength("Ground Dirt Strength", Range(0,1)) = 1
        _GroundDirtFullHeight("Ground Dirt Full Height", Float) = 0.10
        _GroundDirtFadeHeight("Ground Dirt Fade Height", Float) = 1.50
        _CavityDirtStrength("Cavity Dirt Strength", Range(0,1)) = 0
        _RunoffDirtStrength("Runoff Dirt Strength", Range(0,1)) = 0
        _GrimeDirtStrength("Grime Strength", Range(0,1)) = 0
        _RunoffMap("Runoff Map (R Coverage)", 2D) = "white" {}
        _RunoffTiling("Runoff Tiling", Vector) = (1,1,0,0)
        _RunoffOffset("Runoff Offset", Vector) = (0,0,0,0)
        _RunoffVerticalFullNormalY("Runoff Full Normal Y", Range(0,1)) = 0.15
        _RunoffVerticalFadeNormalY("Runoff Fade Normal Y", Range(0,1)) = 0.55
        [Toggle(_WETNESS_ON)] _UseWetness("Use Wetness", Float) = 0
        _WetnessStrength("Wetness Strength", Range(0,1)) = 0
        _WetGroundInfluence("Wet Ground Influence", Range(0,1)) = 1
        _WetRunoffInfluence("Wet Runoff Influence", Range(0,1)) = 1
        _WetCavityInfluence("Wet Cavity Influence", Range(0,1)) = 0.35
        _WetGroundFullHeight("Wet Ground Full Height", Float) = 0.05
        _WetGroundFadeHeight("Wet Ground Fade Height", Float) = 0.75
        _WetAlbedoMultiplier("Wet Albedo Multiplier", Range(0,1)) = 0.72
        _WetSmoothness("Wet Smoothness", Range(0,1)) = 0.75
        _WetNormalFlattening("Wet Normal Flattening", Range(0,1)) = 0.20
        [Toggle(_MOSS_ON)] _UseMoss("Use Moss", Float) = 0
        _MossBaseMap("Moss Base Map (A Coverage)", 2D) = "white" {}
        _MossColor("Moss Color", Color) = (1,1,1,1)
        [ToggleUI] _MossUseMaskMap("Use Moss Mask", Float) = 0
        _MossMaskMap("Moss Mask (R Metallic, G AO, A Smoothness)", 2D) = "white" {}
        [Normal] _MossNormalMap("Moss Normal", 2D) = "bump" {}
        _MossNormalScale("Moss Normal Scale", Float) = 1
        _MossTiling("Moss Tiling", Vector) = (1,1,0,0)
        _MossOffset("Moss Offset", Vector) = (0,0,0,0)
        _MossStrength("Moss Strength", Range(0,1)) = 0
        _MossStartNormalY("Moss Start Normal Y", Range(0,1)) = 0.55
        _MossFullNormalY("Moss Full Normal Y", Range(0,1)) = 0.90
        _MossMoistureInfluence("Moss Moisture Influence", Range(0,1)) = 0.75
        _MossMinimumMoisture("Moss Minimum Moisture", Range(0,1)) = 0.35
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" }
        LOD 300
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ArchitectureVertex
            #pragma fragment ArchitectureFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _DIRT_ON
            #pragma shader_feature_local_fragment _MOSS_ON
            #pragma shader_feature_local_fragment _AGING_ON
            #pragma shader_feature_local_fragment _WETNESS_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #define ARCHITECTURE_FORWARD_PASS
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        // Depth normals retain the persistent authored damage.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex MasonryDepthNormalsVertex
            #pragma fragment MasonryDepthNormalsFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _AGING_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #define MASONRY_DEPTH_NORMALS_PASS
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ArchitectureMetaVertex
            #pragma fragment ArchitectureMetaFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _AGING_ON
            #define ARCHITECTURE_META_PASS
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode"="MotionVectors" }
            ColorMask RG
            HLSLPROGRAM
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY
            #include "ArchitectureMasonryWeatheringInput.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
