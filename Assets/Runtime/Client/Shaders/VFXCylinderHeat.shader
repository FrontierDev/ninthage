Shader "NinthAge/VFX/VFXCylinderHeat"
{
    // Screen-space heat distortion for a cylinder mesh.
    // Waves travel upward over time, displacing whatever is visible through the cylinder.
    // No colour is added - pure distortion only.
    //
    // REQUIREMENT: Enable "Opaque Texture" on your Universal Renderer Data asset.
    //
    // Recommended mesh setup:
    //   - Unity built-in Cylinder, Cull Off (both sides visible)
    //   - ZWrite Off, Transparent queue
    //   - Scale XZ to match the heat column width, Y to match its height
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.1))  = 0.02
        _WaveFrequency      ("Wave Frequency",      Range(1.0, 30.0)) = 10.0
        _WaveSpeed          ("Wave Speed",          Range(0.0, 10.0)) = 3.0
        _WaveCount          ("Wave Count",          Range(1.0, 8.0))  = 2.0
        _TopFade            ("Top Fade",            Range(0.0, 1.0))  = 0.3
        _BottomFade         ("Bottom Fade",         Range(0.0, 1.0))  = 0.1
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
            Name "HeatDistortion"
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
                half _WaveFrequency;
                half _WaveSpeed;
                half _WaveCount;
                half _TopFade;
                half _BottomFade;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float2 uv         : TEXCOORD1;  // .y = 0 at bottom, 1 at top of cylinder side
                float  normalDotV : TEXCOORD2;  // fade out where cylinder faces away from camera
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

                // Fade effect where the cylinder surface grazes the view angle.
                float3 normalWS  = TransformObjectToWorldNormal(IN.normalOS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - pos.positionWS);
                OUT.normalDotV   = abs(dot(normalWS, viewDirWS));

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                if (_Opacity < 0.005) discard;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float  uvY      = IN.uv.y;  // 0 = bottom, 1 = top

                // Upward-travelling wave: multiple frequencies for organic look.
                float wave  = sin(uvY * _WaveFrequency       - _Time.y * _WaveSpeed)
                            + sin(uvY * _WaveFrequency * 1.7 - _Time.y * _WaveSpeed * 0.8) * 0.5;
                wave /= 1.5; // normalise combined amplitude

                // Vertical fade: dissipates toward top, softer at bottom.
                float topMask    = smoothstep(1.0, 1.0 - _TopFade,    uvY);
                float bottomMask = smoothstep(0.0, _BottomFade,        uvY);
                float vertFade   = topMask * bottomMask;

                // Edge fade: reduce distortion where cylinder curves sharply away.
                float edgeFade = smoothstep(0.0, 0.3, IN.normalDotV);

                float dispX = wave * _DistortionStrength * vertFade * edgeFade * _Opacity;

                // Displace only horizontally (heat shimmer is primarily lateral).
                float2 sampleUV = screenUV + float2(dispX, 0.0);
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
