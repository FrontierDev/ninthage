Shader "NinthAge/UI/ArcaneSlate/TabSurface" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.88,0.75,0.45,1)
        _UnderlineWidth ("Underline Width (px)", Range(0,8)) = 2.0
        _UnderlineInset ("Underline Inset (px)", Range(0,64)) = 8.0
        _GlowRadius ("Glow Radius (px)", Range(0,256)) = 48.0
        _GlowStrength ("Glow Strength", Range(0,1)) = 0.5
        [Toggle] _IsActive ("Is Active", Float) = 0
        _RectSize ("Rect Size (px)", Vector) = (0,0,0,0)
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
            float4 _GlowColor;
            float _UnderlineWidth;
            float _UnderlineInset;
            float _GlowRadius;
            float _GlowStrength;
            float _IsActive;
            float4 _RectSize;

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
                float isActive = saturate(_IsActive);

                // Derive element size in pixels
                float2 rectSizePx;
                if (_RectSize.x > 1.0 && _RectSize.y > 1.0) {
                    rectSizePx = _RectSize.xy;
                } else {
                    float2 gdx = ddx(uv);
                    float2 gdy = ddy(uv);
                    float det = gdx.x * gdy.y - gdx.y * gdy.x;
                    float invDet = 1.0 / (abs(det) + 1e-6);
                    float widthPx  = length(float2(gdy.y, -gdx.y)) * invDet;
                    float heightPx = length(float2(-gdy.x, gdx.x)) * invDet;
                    rectSizePx = float2(max(widthPx, 1.0), max(heightPx, 1.0));
                }

                // Pixel-space coordinates: center of tab = (0,0), bottom = (x, -halfH)
                float2 p = (uv - 0.5) * rectSizePx;
                float halfW = rectSizePx.x * 0.5;
                float halfH = rectSizePx.y * 0.5;

                // Underline: short horizontal bar at the bottom, inset from left/right edges
                float aa = 1.0;
                float distFromBottomPx = uv.y * rectSizePx.y;
                
                // Vertical band at the bottom
                float underlineV = smoothstep(_UnderlineWidth, 0.0, distFromBottomPx);
                
                // Horizontal inset: fade out near the left/right edges
                float distFromEdgeH = halfW - abs(p.x);
                float underlineH = smoothstep(_UnderlineInset - aa, _UnderlineInset + aa, distFromEdgeH);
                
                float underlineMask = underlineV * underlineH;

                // Glow: surrounds the underline (halo that extends around underline edges)
                // Horizontal bloom: extends on both sides of the underline horizontal extent
                float glowH = smoothstep(_UnderlineInset - aa, _UnderlineInset + _GlowRadius, distFromEdgeH);
                // Vertical bloom: extends above and below the underline center
                float glowV = smoothstep(_GlowRadius, 0.0, abs(distFromBottomPx - _UnderlineWidth * 0.5));
                float glowMask = glowH * glowV * (1.0 - underlineMask);

                // Combine
                float underlineAlpha = underlineMask * isActive;
                float glowAlpha      = glowMask * _GlowStrength * isActive * (1.0 - underlineMask);
                float outAlpha       = max(underlineAlpha, glowAlpha) * i.color.a;

                return float4(_GlowColor.rgb, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
