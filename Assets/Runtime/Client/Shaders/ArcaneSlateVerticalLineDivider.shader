Shader "NinthAge/UI/ArcaneSlate/VerticalLineDivider" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (0.29411765,0.28235295,0.2627451,1)
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
            #include "ArcaneSlateCommon.cginc"

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

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                float lineMask = ArcaneSlateCenterLineMask(uv.x, _LineWidth);
                float edgeMask = ArcaneSlateEdgeFade(uv.y, _EdgeFadeStart, _EdgeFadeEnd);

                // Subtle noise along the line
                float noiseSample = ArcaneSlateValueNoise(float2(uv.x * _NoiseScale, uv.y * _NoiseScale * 4.0));
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
