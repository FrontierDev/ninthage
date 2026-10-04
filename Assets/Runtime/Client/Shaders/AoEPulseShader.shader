Shader "NinthAge/VFX/AoEPulseShader"
{
    // Expanding cylinder rendered as real geometry. Cull Off shows both sides.
    // Side normals (normalY ~ 0) = the 3D shock ring.
    // Cap normals  (normalY < 0) = the ground-level trail disc (bottom cap).
    // Reusable for all area-of-effect pulse wave effects (rift, explosions, etc).
    //
    // Ground ripple uses the distortion-delta trick with Blend One One:
    //   output = (distortedBg - undistortedBg) + emission
    //   final  = undistortedBg + output = distortedBg + emission
    // REQUIREMENT for ripple: Enable "Opaque Texture" on Universal Renderer Data.
    Properties
    {
        [HDR] _Color           ("Ring Color",              Color)            = (0.7, 0.3, 1.0, 1.0)
        _Intensity             ("Emission Intensity",      Range(0.0, 50.0)) = 12.0
        _TrailStrength         ("Trail Brightness",        Range(0.0,  1.0)) = 0.35
        _TrailFalloffPower     ("Trail Falloff Power",     Range(1.0,  4.0)) = 2.0
        _Progress              ("Progress [0-1]",          Range(0.0,  1.0)) = 0.0
        _FalloffDistance       ("Falloff Distance",        Range(0.1,  2.0)) = 0.5
        _FalloffPower          ("Falloff Power",           Range(1.0,  4.0)) = 2.0
        _JitterAmount          ("Shock Jitter",            Range(0.0,  1.0)) = 0.2
        _StreakCount           ("Trail Streak Count",      Range(1.0, 16.0)) = 8.0
        _StreakSharpness       ("Trail Streak Sharp",      Range(1.0,  4.0)) = 2.0
        _RippleStrength        ("Ground Ripple Strength",  Range(0.0, 0.05)) = 0.015
        _RippleFrequency       ("Ground Ripple Frequency", Range(1.0, 20.0)) = 6.0
        _RippleSpeed           ("Ground Ripple Speed",     Range(0.0, 10.0)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AoEPulseShader"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half  _Intensity;
                half  _TrailStrength;
                half  _TrailFalloffPower;
                half  _Progress;
                half  _FalloffDistance;
                half  _FalloffPower;
                half  _JitterAmount;
                half  _StreakCount;
                half  _StreakSharpness;
                half  _RippleStrength;
                half  _RippleFrequency;
                half  _RippleSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  normalY    : TEXCOORD0;
                float  xzRadius   : TEXCOORD1;
                float  posY       : TEXCOORD2;
                float2 posXZ      : TEXCOORD3;
                float4 screenPos  : TEXCOORD4;
                float4 centerPos  : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalY    = normalize(IN.normalOS).y;
                OUT.xzRadius   = saturate(length(IN.positionOS.xz) * 2.0);
                OUT.posY       = IN.positionOS.y;
                OUT.posXZ      = IN.positionOS.xz;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                float4 centerCS = TransformObjectToHClip(float3(0.0, 0.0, 0.0));
                OUT.centerPos   = ComputeScreenPos(centerCS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float lifeFade   = 1.0 - _Progress;
                float absNormalY = abs(IN.normalY);

                if (absNormalY > 0.5)
                {
                    if (IN.normalY < 0.0)
                    {
                        // Bottom cap: ground trail disc + ground ripple distortion.
                        float angle    = atan2(IN.posXZ.y, IN.posXZ.x);
                        float streak1  = sin(angle * _StreakCount);
                        float streak2  = sin(angle * _StreakCount * 0.618);
                        float streakPattern = abs(streak1) * abs(streak2);
                        streakPattern = pow(max(streakPattern, 0.0), 1.0 / max(_StreakSharpness, 0.01));
                        streakPattern = streakPattern * 0.5 + 0.5;

                        float radialFade = pow(max(IN.xzRadius, 0.0), _TrailFalloffPower);
                        radialFade = lerp(radialFade, radialFade * streakPattern, 0.75);
                        half emitAlpha = radialFade * lifeFade * _TrailStrength;
                        half3 emission = _Color.rgb * _Intensity * lifeFade * emitAlpha;

                        // Distortion delta: sample opaque texture at displaced and undisplaced UV.
                        // With Blend One One: final = undistorted + (distorted - undistorted) + emission
                        //                          = distorted + emission
                        float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                        float2 centerUV = IN.centerPos.xy / IN.centerPos.w;
                        float2 outDir   = normalize(screenUV - centerUV + 0.0001);

                        float normDist   = IN.xzRadius;
                        float ripple     = sin(normDist * _RippleFrequency - _Time.y * _RippleSpeed);
                        float centreFade = smoothstep(0.0, 0.1, normDist);
                        float edgeFade   = smoothstep(1.0, 0.8, normDist);
                        float envelope   = centreFade * edgeFade * lifeFade;
                        float2 disp      = outDir * ripple * envelope * _RippleStrength;

                        half3 distorted   = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + disp).rgb;
                        half3 undistorted = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;
                        half3 delta       = distorted - undistorted;

                        if (emitAlpha < 0.001 && dot(abs(delta), 1.0) < 0.001) discard;
                        return half4(delta + emission, emitAlpha);
                    }
                    else
                    {
                        discard;
                    }
                }
                else
                {
                    // Side face: 3D shock ring.
                    float angle  = atan2(IN.posXZ.y, IN.posXZ.x);
                    float jitter = sin(angle * 4.0) * 0.5 + 0.5;
                    jitter = lerp(1.0, jitter, _JitterAmount);

                    float effectiveFalloff = _FalloffDistance * lifeFade;
                    float vertFrac = saturate((IN.posY + 0.5) / max(effectiveFalloff, 0.01));
                    float vertFade = pow(max(1.0 - vertFrac, 0.0), _FalloffPower);
                    half alpha = vertFade * lifeFade * jitter;
                    if (alpha < 0.001) discard;
                    return half4(_Color.rgb * _Intensity * lifeFade * alpha, alpha);
                }
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}