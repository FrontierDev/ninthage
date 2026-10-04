Shader "NinthAge/VFX/RiftSurface"
{
    Properties
    {
        [Header(Colours)]
        [HDR] _EdgeGlowColor        ("Edge Glow",           Color)  = (0.5,  0.1,  1.0,  1.0)
        [HDR] _CoreGlowColor        ("Core Glow",           Color)  = (0.9,  0.4,  1.0,  1.0)
        [HDR] _VoidColor            ("Void (Interior)",     Color)  = (0.02, 0.0,  0.06, 1.0)
        [HDR] _TurbulenceColor      ("Turbulence Tint",     Color)  = (0.6,  0.1,  1.0,  1.0)

        [Header(Glow)]
        _EmissionIntensity          ("Emission Intensity",  Range(0.0, 20.0)) = 4.0
        _FresnelPower               ("Fresnel Power",       Range(0.1,  8.0)) = 2.5
        _AlphaFalloff               ("Tip Alpha Falloff",   Range(0.5,  4.0)) = 1.5

        [Header(Parallax Interior)]
        _ParallaxStrength           ("Parallax Depth",      Range(0.0,  1.0)) = 0.25

        [Header(Turbulence)]
        _NoiseScale                 ("Noise Scale",         Range(0.5, 20.0)) = 5.0
        _NoiseSpeed                 ("Noise Speed",         Range(0.0,  5.0)) = 0.8
        _TurbulenceStrength         ("Turbulence Strength", Range(0.0,  1.0)) = 0.35

        [Header(Pulse)]
        _PulseSpeed                 ("Pulse Speed",         Range(0.0,  5.0)) = 1.1
        _PulseMinIntensity          ("Pulse Min Intensity", Range(0.0,  1.0)) = 0.55
        _DisplaceStrength           ("Displace Strength",   Range(0.0,  0.5)) = 0.06
        _DisplaceSpeed              ("Displace Speed",      Range(0.0,  5.0)) = 0.4
        _DisplaceScale              ("Displace Scale",      Range(0.5, 10.0)) = 3.0

        [Header(Halo)]
        [HDR] _HaloColor            ("Halo Color",          Color)  = (0.7, 0.3, 1.0, 1.0)
        _HaloIntensity              ("Halo Intensity",      Range(0.0, 20.0)) = 4.0
        _HaloWidth                  ("Halo Width (m)",      Range(0.01, 2.0)) = 0.3
        _HaloFalloff                ("Halo Falloff",        Range(0.5,  8.0)) = 2.0

        [Header(Lifetime)]
        _OpenAmount                 ("Open Amount",         Range(0.0,  1.0)) = 1.0
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
            Name "RiftForward"
            Tags { "LightMode" = "UniversalForward" }

            // Soft-additive: src colour is pre-weighted by alpha, then added to scene.
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeGlowColor;
                half4 _CoreGlowColor;
                half4 _VoidColor;
                half4 _TurbulenceColor;
                half  _EmissionIntensity;
                half  _FresnelPower;
                half  _AlphaFalloff;
                half  _ParallaxStrength;
                half  _NoiseScale;
                half  _NoiseSpeed;
                half  _TurbulenceStrength;
                half  _PulseSpeed;
                half  _PulseMinIntensity;
                half  _DisplaceStrength;
                half  _DisplaceSpeed;
                half  _DisplaceScale;
                half4 _HaloColor;
                half  _HaloIntensity;
                half  _HaloWidth;
                half  _HaloFalloff;
                half  _OpenAmount;
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

            // 4-octave fBm.
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

                // ── Vertex displacement ───────────────────────────────────────────
                // Sample a slow noise field in object space and push the vertex
                // along its normal, making the whole surface writhe over time.
                // Tip vertices (uv.y ≈ 0 or 1) are pinned by the sin profile so
                // the tips don't drift open.
                float tipPin      = sin(IN.uv.y * 3.14159265);
                float2 dispUV     = IN.positionOS.xy * _DisplaceScale
                                  + float2(_Time.y * _DisplaceSpeed,
                                           _Time.y * _DisplaceSpeed * 0.6);
                float  dispNoise  = ValueNoise(dispUV) * 2.0 - 1.0; // [-1, 1]
                float3 displaced  = IN.positionOS.xyz
                                  + IN.normalOS * dispNoise * _DisplaceStrength * tipPin;

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

                // NdotV: 1 = face-on, 0 = grazing edge.
                float NdotV  = saturate(abs(dot(N, V)));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // ── Parallax interior ─────────────────────────────────────────────
                // Build a tangent frame on the fly from the surface normal so that
                // the view direction offsets the interior UVs — like peering through
                // a hole whose contents are fixed in world space.
                float3 up       = abs(N.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 tangent  = normalize(cross(up, N));
                float3 binormal = cross(N, tangent);

                float2 parallax = float2(dot(V, tangent), dot(V, binormal)) * _ParallaxStrength;
                float2 intUV    = IN.uv + parallax;

                // ── Animated turbulence ───────────────────────────────────────────
                // Primary noise layer — scrolls gently.
                float2 noiseUV  = intUV * _NoiseScale
                                + float2( _Time.y * _NoiseSpeed,
                                          _Time.y * _NoiseSpeed * 0.7);
                float  noise    = FBM(noiseUV);

                // Secondary warp layer — uses first layer to distort itself,
                // creating swirling, non-repeating motion.
                float2 warpUV   = intUV * _NoiseScale * 1.5
                                + float2(-_Time.y * _NoiseSpeed * 0.4,
                                          _Time.y * _NoiseSpeed * 0.6);
                float  warp     = FBM(warpUV + noise * 0.4);

                // ── Alpha: tip fade ───────────────────────────────────────────────
                // uv.y runs 0 → 1 from bottom to top tip. sin pushes it to 0 at both ends.
                float tipFade = pow(sin(IN.uv.y * 3.14159265), _AlphaFalloff);

                // ── Colour composition ────────────────────────────────────────────
                // 1. Interior: void → core glow driven by warped noise.
                half3 interior = lerp(_VoidColor.rgb, _CoreGlowColor.rgb, saturate(warp * 1.4));

                // 2. Overlay turbulence tint on bright patches.
                interior = lerp(interior, _TurbulenceColor.rgb, noise * _TurbulenceStrength);

                // 3. Blend toward edge glow at grazing angles (Fresnel).
                half3 col = lerp(interior, _EdgeGlowColor.rgb, fresnel);

                // ── Emission pulse ────────────────────────────────────────────────
                // Two overlapping sine waves at coprime frequencies produce a
                // compound beat that never feels mechanical.
                float pulse = sin(_Time.y * _PulseSpeed)       * 0.55
                            + sin(_Time.y * _PulseSpeed * 1.7) * 0.25
                            + sin(_Time.y * _PulseSpeed * 0.4) * 0.20;
                // Remap [-1,1] → [_PulseMinIntensity, 1].
                float pulseT  = pulse * 0.5 + 0.5;
                float pulseMul = lerp(_PulseMinIntensity, 1.0, pulseT);

                // 4. Emission boost, modulated by pulse.
                col *= _EmissionIntensity * pulseMul;

                // Alpha accumulates from: edge brightness, face-on interior, noise variation.
                float alpha = saturate(fresnel * 0.6 + NdotV * 0.35 + warp * 0.25) * tipFade;

                // Open/close mask: reveals from the vertical centre outward.
                // normalizedDist = 0 at centre (uv.y=0.5), 1 at both tips.
                float normalizedDist = abs(IN.uv.y - 0.5) * 2.0;
                float feather = 0.06;
                float openMask = saturate((_OpenAmount * (1.0 + feather) - normalizedDist) / feather);
                alpha *= openMask;

                return half4(col, alpha);
            }
            ENDHLSL
        }

        // ── Halo glow pass ────────────────────────────────────────────────────
        // Extrudes vertices along world normals by _HaloWidth, then renders only
        // back faces (Cull Front).  Back faces are visible only where the inflated
        // shell protrudes past the rift body — i.e. the silhouette rim — producing
        // a smooth glow outline without any secondary mesh or component.
        Pass
        {
            Name "RiftHalo"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            Cull Front
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   HaloVert
            #pragma fragment HaloFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Must be identical to the forward pass CBUFFER for SRP Batcher.
            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeGlowColor;
                half4 _CoreGlowColor;
                half4 _VoidColor;
                half4 _TurbulenceColor;
                half  _EmissionIntensity;
                half  _FresnelPower;
                half  _AlphaFalloff;
                half  _ParallaxStrength;
                half  _NoiseScale;
                half  _NoiseSpeed;
                half  _TurbulenceStrength;
                half  _PulseSpeed;
                half  _PulseMinIntensity;
                half  _DisplaceStrength;
                half  _DisplaceSpeed;
                half  _DisplaceScale;
                half4 _HaloColor;
                half  _HaloIntensity;
                half  _HaloWidth;
                half  _HaloFalloff;
                half  _OpenAmount;
            CBUFFER_END

            struct HaloAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct HaloVaryings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            HaloVaryings HaloVert(HaloAttributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                HaloVaryings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);

                // Extrude along world normal.
                posWS += normalize(normWS) * _HaloWidth;

                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.worldPos    = posWS;
                OUT.worldNormal = normWS;
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 HaloFrag(HaloVaryings IN) : SV_Target
            {
                float3 N = normalize(IN.worldNormal);
                float3 V = normalize(GetWorldSpaceViewDir(IN.worldPos));

                // Cull Front → we see back faces only.  abs(NdotV) is:
                //   ~1 where the face points directly away from camera → full glow
                //   ~0 at the silhouette tangent → fades to nothing
                // The extruded shell protrudes past the rift body only at the rim,
                // so the concentrated glow there produces the outline effect.
                float rim = pow(saturate(abs(dot(N, V))), _HaloFalloff);

                // Fade to zero at both pointed tips.
                float tipFade = sin(IN.uv.y * 3.14159265);

                // Compound-sine pulse — reuses the surface pulse parameters.
                float pulse = sin(_Time.y * _PulseSpeed)       * 0.55
                            + sin(_Time.y * _PulseSpeed * 1.7) * 0.25
                            + sin(_Time.y * _PulseSpeed * 0.4) * 0.20;
                float pulseMul = lerp(_PulseMinIntensity, 1.0, pulse * 0.5 + 0.5);

                // Match the open/close mask from the surface pass.
                float normalizedDist = abs(IN.uv.y - 0.5) * 2.0;
                float feather = 0.06;
                float openMask = saturate((_OpenAmount * (1.0 + feather) - normalizedDist) / feather);

                half3 col = _HaloColor.rgb * _HaloIntensity * pulseMul * rim * tipFade * openMask;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
