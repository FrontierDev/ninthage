Shader "NinthAge/VFX/VFXQuadVortex"
{
    // Screen-space vortex/whirlpool distortion on a flat quad.
    // Pixels near the centre are rotated further than pixels at the edge,
    // creating a swirling drain effect. No colour is added - pure distortion only.
    //
    // REQUIREMENT: Enable "Opaque Texture" on your Universal Renderer Data asset.
    //
    // Recommended setup:
    //   - Unity built-in Quad (or rotated flat for ground use)
    //   - ZWrite Off, Transparent queue
    //   - Scale to match desired vortex diameter
    Properties
    {
        _VortexStrength  ("Vortex Strength",   Range(0.0, 15.0)) = 4.0
        _VortexSpeed     ("Vortex Speed",      Range(-10.0, 10.0)) = 2.0
        _FalloffPower    ("Falloff Power",     Range(0.5, 4.0))  = 1.5
        _EdgeFade        ("Edge Fade",         Range(0.0, 1.0))  = 0.15
        _CentreHole      ("Centre Hole",       Range(0.0, 0.8))  = 0.0
        _Opacity         ("Opacity [0-1]",     Range(0.0, 1.0))  = 1.0
        [HDR] _TintColor ("Tint Color",        Color)            = (0.0, 0.0, 0.0, 0.0)
        _TintStrength    ("Tint Strength",     Range(0.0, 1.0))  = 0.0
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
            Name "VortexDistortion"
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
                half _VortexStrength;
                half _VortexSpeed;
                half _FalloffPower;
                half _EdgeFade;
                half _CentreHole;
                half _Opacity;
                half4 _TintColor;
                half _TintStrength;
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
                float4 centerPos  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
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

                // Radial distance from quad centre in object space [0, 1].
                float2 uvCentered = IN.uv - 0.5;
                float  normDist   = saturate(length(uvCentered) * 2.0);

                // Discard outside the inscribed circle and optional centre hole.
                if (normDist > 1.0)          discard;
                if (normDist < _CentreHole)  discard;

                // Rotation angle: strongest at centre, zero at edge.
                // pow gives control over the falloff curve.
                float falloff     = pow(max(1.0 - normDist, 0.0), _FalloffPower);
                float rotAngle    = falloff * _VortexStrength + _Time.y * _VortexSpeed;

                // Rotate screen-space delta around the projected quad centre.
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 centerUV = IN.centerPos.xy / IN.centerPos.w;
                float2 delta    = screenUV - centerUV;

                float sinA = sin(rotAngle);
                float cosA = cos(rotAngle);
                float2 rotatedDelta = float2(
                    delta.x * cosA - delta.y * sinA,
                    delta.x * sinA + delta.y * cosA
                );

                float2 sampleUV = centerUV + rotatedDelta;

                // Soft fade at the disc edge.
                float edgeMask = smoothstep(1.0, 1.0 - _EdgeFade, normDist);

                // Blend between undistorted and vortexed based on edge fade and opacity.
                float blend = edgeMask * _Opacity;
                half3 distorted   = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sampleUV).rgb;
                half3 undistorted = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;
                half3 color       = lerp(undistorted, distorted, blend);

                // Optional additive tint — stronger at the vortex core.
                color += _TintColor.rgb * _TintStrength * falloff * _Opacity;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
