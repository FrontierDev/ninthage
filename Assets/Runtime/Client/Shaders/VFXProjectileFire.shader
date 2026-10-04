Shader "NinthAge/VFX/VFXProjectileFire"
{
    Properties
    {
        [Header(CoreFlame)]
        [HDR] _HotCoreColor          ("Hot Core",            Color)  = (1.0,  0.9,  0.3,  1.0)
        [HDR] _FlameOrangeColor      ("Flame Orange",        Color)  = (1.0,  0.4,  0.0,  1.0)
        [HDR] _DarkRedColor          ("Dark Red Edge",       Color)  = (0.6,  0.1,  0.0,  1.0)

        [Header(Glow)]
        _EmissionIntensity           ("Emission Intensity",  Range(0.5, 5.0)) = 2.0
        _FresnelPower                ("Fresnel Power",       Range(0.5,  5.0)) = 2.0
        _AlphaFalloff                ("Alpha Falloff",       Range(0.5,  3.0)) = 1.2

        [Header(FireAnimation)]
        _NoiseScale                  ("Turbulence Scale",    Range(2.0, 15.0)) = 8.0
        _NoiseSpeed                  ("Turbulence Speed",    Range(0.5,  3.0)) = 1.8
        _TurbulenceStrength          ("Turbulence Strength", Range(0.3,  1.0)) = 0.7

        [Header(VertexWrithe)]
        _DisplaceStrength            ("Flame Writhe",        Range(0.05, 0.3)) = 0.12
        _DisplaceSpeed               ("Writhe Speed",        Range(0.5,  3.0)) = 1.2
        _DisplaceScale               ("Writhe Scale",        Range(2.0,  8.0)) = 5.0

        [Header(PulseFlicker)]
        _FlameFlickerSpeed           ("Flicker Speed",       Range(2.0, 10.0)) = 5.5
        _FlameFlickerIntensity       ("Flicker Intensity",   Range(0.05, 0.3)) = 0.15
        _CorePulseSpeed              ("Core Pulse Speed",    Range(1.0,  4.0)) = 2.2

        [Header(Lifetime)]
        _DeathFade                   ("Death Fade",          Range(0.0,  1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FireballForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotCoreColor;
                half4 _FlameOrangeColor;
                half4 _DarkRedColor;
                half  _EmissionIntensity;
                half  _FresnelPower;
                half  _AlphaFalloff;
                half  _NoiseScale;
                half  _NoiseSpeed;
                half  _TurbulenceStrength;
                half  _DisplaceStrength;
                half  _DisplaceSpeed;
                half  _DisplaceScale;
                half  _FlameFlickerSpeed;
                half  _FlameFlickerIntensity;
                half  _CorePulseSpeed;
                half  _DeathFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Procedural value noise ────────────────────────────────────────────
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = frac(sin(dot(i,               float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1,0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0,1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1,1), float2(127.1, 311.7))) * 43758.5453);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 4-octave fBm for organic flame turbulence
            float FBM(float2 p)
            {
                float v = 0.0, amp = 0.5;
                UNITY_UNROLL
                for (int k = 0; k < 4; k++)
                {
                    v   += amp * ValueNoise(p);
                    p   *= 2.1;
                    amp *= 0.5;
                }
                return v;
            }

            // ── Vertex ───────────────────────────────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Subtle flame writhe
                float2 dispUV = IN.positionOS.xy * _DisplaceScale
                              + float2(_Time.y * _DisplaceSpeed,
                                       _Time.y * _DisplaceSpeed * 0.8);
                float dispNoise = ValueNoise(dispUV) * 2.0 - 1.0;
                
                float pinStrength = sin(abs(IN.uv.y - 0.5) * 3.14159) * 0.8 + 0.2;
                
                float3 displaced = IN.positionOS.xyz
                                 + IN.normalOS * dispNoise * _DisplaceStrength * pinStrength;

                VertexPositionInputs vpi = GetVertexPositionInputs(displaced);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = vni.normalWS;
                OUT.uv          = IN.uv;
                return OUT;
            }

            // ── Fragment ─────────────────────────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.worldNormal);
                float3 V = normalize(GetWorldSpaceViewDir(IN.worldPos));

                float NdotV = saturate(abs(dot(N, V)));
                float rimLight = pow(1.0 - NdotV, _FresnelPower);

                // ── Turbulent flame pattern ───────────────────────────────────────
                float2 flameUV = IN.uv * _NoiseScale
                               + float2(_Time.y * _NoiseSpeed * 1.1,
                                        _Time.y * _NoiseSpeed * 0.7);
                float flameTurbulence = FBM(flameUV);

                float2 warpUV = IN.uv * _NoiseScale * 1.4
                              + float2(-_Time.y * _NoiseSpeed * 0.5,
                                        _Time.y * _NoiseSpeed * 0.8);
                float flameWarp = FBM(warpUV + flameTurbulence * 0.5);

                // ── Flame colour gradient ─────────────────────────────────────────
                float flameGradient = saturate(flameWarp * 1.6);
                
                half3 col = lerp(_DarkRedColor.rgb, _FlameOrangeColor.rgb, flameGradient * 0.5);
                col = lerp(col, _HotCoreColor.rgb, flameGradient * 0.7);

                // Rim glow
                col += _HotCoreColor.rgb * rimLight * 0.3;

                // ── Flickering ────────────────────────────────────────────────────
                float flicker = sin(_Time.y * _FlameFlickerSpeed)       * 0.4
                              + sin(_Time.y * _FlameFlickerSpeed * 1.3) * 0.3
                              + sin(_Time.y * _FlameFlickerSpeed * 2.1) * 0.3;
                float flickerMul = 1.0 + flicker * _FlameFlickerIntensity;

                // ── Core pulse ─────────────────────────────────────────────────────
                float pulse = sin(_Time.y * _CorePulseSpeed) * 0.5 + 0.5;
                float pulseMul = lerp(0.85, 1.15, pulse);

                float emission = _EmissionIntensity * flickerMul * pulseMul;
                col *= emission;

                // ── Alpha composition ─────────────────────────────────────────────
                float alpha = saturate(flameGradient * 0.6 + rimLight * 0.4);
                alpha *= _DeathFade;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
