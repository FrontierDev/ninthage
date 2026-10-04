Shader "NinthAge/VFX/VFXQuadRippleField"
{
    // Screen-space circular ripple distortion for a flat ground quad.
    // Ripple rings radiate outward from the centre of the quad.
    // No colour is added - pure distortion only.
    //
    // REQUIREMENT: Enable "Opaque Texture" on your Universal Renderer Data asset.
    //
    // Recommended setup:
    //   - Unity built-in Quad, rotated 90 degrees on X so it lies flat on the ground
    //   - Scale XZ to match the ripple field radius
    //   - ZWrite Off, Transparent queue
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.1))  = 0.025
        _RippleFrequency    ("Ripple Frequency",    Range(1.0, 30.0)) = 8.0
        _RippleSpeed        ("Ripple Speed",        Range(0.0, 10.0)) = 2.5
        _EdgeFade           ("Edge Fade",           Range(0.0, 1.0))  = 0.2
        _Opacity            ("Opacity [0-1]",       Range(0.0, 1.0))  = 1.0
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
            Name "RippleField"
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
                half _EdgeFade;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float4 centerPos  : TEXCOORD1;  // projected quad centre
                float2 uv         : TEXCOORD2;  // object-space UV [0,1]
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
                OUT.uv         = IN.uv;

                float4 centerCS = TransformObjectToHClip(float3(0.0, 0.0, 0.0));
                OUT.centerPos   = ComputeScreenPos(centerCS);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                if (_Opacity < 0.005) discard;

                // Object-space radial distance from quad centre [0, ~0.7 at corner].
                float2 uvCentered = IN.uv - 0.5;   // [-0.5, 0.5]
                float  radialDist = length(uvCentered);
                float  normDist   = saturate(radialDist * 2.0); // 0 = centre, 1 = edge

                // Discard outside the inscribed circle.
                if (normDist > 1.0) discard;

                // Outward-travelling concentric ripple.
                float ripple = sin(normDist * _RippleFrequency - _Time.y * _RippleSpeed);

                // Fade: zero at dead centre (no direction) and at edge.
                float centreFade = smoothstep(0.0, 0.15, normDist);
                float edgeFade   = smoothstep(1.0, 1.0 - _EdgeFade, normDist);
                float envelope   = centreFade * edgeFade;

                // Displacement direction in screen space: outward from projected centre.
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 centerUV = IN.centerPos.xy / IN.centerPos.w;
                float2 outDir   = normalize(screenUV - centerUV + 0.0001);

                float2 disp     = outDir * ripple * envelope * _DistortionStrength * _Opacity;
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
