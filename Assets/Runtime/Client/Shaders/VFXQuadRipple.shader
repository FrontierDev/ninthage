Shader "NinthAge/VFX/VFXQuadRipple"
{
    // Pure screen-space distortion quad for URP.
    // Displaces the scene texture behind it - no colour is added.
    // REQUIREMENT: Enable "Opaque Texture" on your Universal Renderer Data asset.
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.15)) = 0.04
        _RippleFrequency    ("Ripple Frequency",    Range(1.0, 20.0)) = 6.0
        _RippleSpeed        ("Ripple Speed",        Range(0.0, 10.0)) = 2.0
        _Progress           ("Progress [0-1]",      Range(0.0, 1.0))  = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "RiftDistortion"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One Zero
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                half _DistortionStrength;
                half _RippleFrequency;
                half _RippleSpeed;
                half _Progress;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float4 centerPos  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.screenPos  = ComputeScreenPos(pos.positionCS);

                float4 centerCS = TransformObjectToHClip(float3(0.0, 0.0, 0.0));
                OUT.centerPos   = ComputeScreenPos(centerCS);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float fade = _Progress;
                if (fade < 0.005) discard;

                // Perspective-correct screen UVs.
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 centerUV = IN.centerPos.xy / IN.centerPos.w;

                float2 delta   = screenUV - centerUV;
                float  dist    = length(delta);

                // Normalise to [0,1] across the quad radius.
                float normDist = saturate(dist * 3.5);

                // Discard quad corners so the effect is circular.
                if (normDist > 1.0) discard;

                float2 dir = delta / (dist + 0.0001);

                // Animated radial ripple: sine wave travelling outward from centre.
                float ripple = sin(normDist * _RippleFrequency - _Time.y * _RippleSpeed);

                // Envelope: displacement peaks at mid-radius, zero at centre and edge.
                float envelope = sin(normDist * 3.14159);
                float2 disp = dir * ripple * envelope * _DistortionStrength * fade;

                float2 sampleUV = screenUV + disp;
                half3 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                                               sampler_CameraOpaqueTexture,
                                               sampleUV).rgb;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}