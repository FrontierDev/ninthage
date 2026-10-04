Shader "NinthAge/UI/ArcaneSlate/VerticalLineDivider" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (0.22745098,0.2745098,0.3372549,1)
        _LineWidth ("Line Width (px)", Range(0,16)) = 1.0
        _EdgeFadeStart ("Edge Fade Start", Range(0,0.5)) = 0.1
        _EdgeFadeEnd ("Edge Fade End", Range(0,0.5)) = 0.05
        _NoiseScale ("Noise Scale", Range(0.1,10)) = 2.0
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.15
        _Opacity ("Opacity", Range(0,1)) = 1.0
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _LineColor;
            float _LineWidth;
            float _EdgeFadeStart;
            float _EdgeFadeEnd;
            float _NoiseScale;
            float _NoiseStrength;
            float _Opacity;

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 position : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            v2f vert(appdata_t v) {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            // Pseudo-Perlin noise
            static float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            static float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                float ab = lerp(a, b, f.x);
                float cd = lerp(c, d, f.x);
                return lerp(ab, cd, f.y);
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // Distance from center (uv.x = 0.5 is center)
                float distFromCenterPx = abs((uv.x - 0.5) * max(1.0, 1.0 / fwidth(uv.x)));

                // Line mask: thin vertical band centered on uv.x = 0.5
                float aa = 1.0;
                float lineMask = smoothstep(_LineWidth + aa, _LineWidth - aa, distFromCenterPx);

                // Fade opacity at top and bottom
                float bottomFade = smoothstep(_EdgeFadeEnd, _EdgeFadeStart, uv.y);
                float topFade = smoothstep(1.0 - _EdgeFadeEnd, 1.0 - _EdgeFadeStart, uv.y);
                float edgeMask = bottomFade * topFade;

                // Subtle noise along the line
                float noiseSample = noise(float2(uv.x * _NoiseScale, uv.y * _NoiseScale * 4.0));
                float noiseAlpha = lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noiseSample);

                // Final output
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = lineMask * edgeMask * noiseAlpha * _Opacity * tex.a * i.color.a;

                return float4(_LineColor.rgb, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
