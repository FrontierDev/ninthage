Shader "NinthAge/UI/ArcaneSlate/LineDivider" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (0.29411765,0.33333334,0.4,1)
        _LineWidth ("Line Width (px)", Range(0,16)) = 1.0
        _EdgeFadeStart ("Edge Fade Start", Range(0,0.5)) = 0.1
        _EdgeFadeEnd ("Edge Fade End", Range(0,0.5)) = 0.05
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

                float lineMask = ArcaneSlateCenterLineMask(uv.y, _LineWidth);
                float edgeMask = ArcaneSlateEdgeFade(uv.x, _EdgeFadeStart, _EdgeFadeEnd);

                // Sample sprite alpha
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = lineMask * edgeMask * _Opacity * tex.a * i.color.a;

                return float4(_LineColor.rgb, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
