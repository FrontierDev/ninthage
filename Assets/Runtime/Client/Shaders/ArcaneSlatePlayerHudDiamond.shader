Shader "NinthAge/UI/ArcaneSlate/PlayerHudDiamond" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OuterFrameColor ("Outer Frame Color", Color) = (0.03529412,0.03921569,0.04705882,1)
        _AccentColor ("Accent Color", Color) = (0.79607844,0.6901961,0.47058824,1)
        _InnerFillTop ("Inner Fill Top", Color) = (0.1254902,0.15686275,0.2,1)
        _InnerFillBottom ("Inner Fill Bottom", Color) = (0.06666667,0.07450981,0.08235294,1)
        _OuterFrameWidth ("Outer Frame Width (px)", Range(1,6)) = 3.0
        _AccentWidth ("Accent Width (px)", Range(0,4)) = 2.0
        _InnerInset ("Inner Inset (px)", Range(2,10)) = 5.0
        _TopHighlightColor ("Top Highlight Color", Color) = (1,1,1,1)
        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.10
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Strength", Range(0,1)) = 0.30
        _Opacity ("Opacity", Range(0,1)) = 1.0
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
            #include "ArcaneSlateCommon.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OuterFrameColor;
            float4 _AccentColor;
            float4 _InnerFillTop;
            float4 _InnerFillBottom;
            float _OuterFrameWidth;
            float _AccentWidth;
            float _InnerInset;
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float _Opacity;
            float4 _RectSize;

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            float ArcaneSlateSdDiamond(float2 p, float size)
            {
                return abs(p.x) + abs(p.y) - size;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 rectSizePx = ArcaneSlateResolveRectSizePx(_RectSize, uv);
                float2 p = (uv - 0.5) * rectSizePx;
                float minDim = min(rectSizePx.x, rectSizePx.y);
                float outerSize = max(0.5, minDim * 0.5);

                float outerSdf = ArcaneSlateSdDiamond(p, outerSize);
                float outerAa = ArcaneSlateAntiAliasWidth(outerSdf);
                float outerMask = smoothstep(outerAa, -outerAa, outerSdf);

                float frameWidth = max(0.0, _OuterFrameWidth);
                float accentWidth = max(0.0, _AccentWidth);
                float accentStart = frameWidth;
                float accentEnd = frameWidth + accentWidth;
                float innerStart = accentEnd + max(0.0, _InnerInset);

                float frameInnerMask = smoothstep(outerAa, -outerAa, outerSdf + accentStart);
                float accentInnerMask = smoothstep(outerAa, -outerAa, outerSdf + accentEnd);
                float innerAreaMask = smoothstep(outerAa, -outerAa, outerSdf + accentEnd);
                float innerFillSdf = outerSdf + innerStart;
                float innerFillAa = ArcaneSlateAntiAliasWidth(innerFillSdf);
                float innerFillMask = smoothstep(innerFillAa, -innerFillAa, innerFillSdf);

                float innerGradient = saturate((p.y / max(outerSize - innerStart, 0.5)) * 0.5 + 0.5);
                float3 innerColor = lerp(_InnerFillBottom.rgb, _InnerFillTop.rgb, innerGradient);
                innerColor = lerp(_InnerFillBottom.rgb, innerColor, innerFillMask);

                float topEdge = 1.0 - smoothstep(
                    0.25, 1.5, abs(p.y - (outerSize - innerStart)));
                innerColor = lerp(innerColor, _TopHighlightColor.rgb,
                    topEdge * innerFillMask * _TopHighlightStrength);

                float innerDistance = max(-innerFillSdf, 0.0);
                float innerShadowWidth = max(1.0, _InnerShadowStrength * minDim * 0.18);
                float innerShadow = 1.0 - smoothstep(
                    0.0, innerShadowWidth + innerFillAa, innerDistance);
                innerColor = lerp(innerColor, _InnerShadowColor.rgb,
                    innerShadow * _InnerShadowStrength);

                float3 surfaceColor = _OuterFrameColor.rgb;
                float accentMask = saturate(frameInnerMask - accentInnerMask);
                surfaceColor = lerp(surfaceColor, _AccentColor.rgb, accentMask);
                surfaceColor = lerp(surfaceColor, innerColor, innerAreaMask);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * outerMask * tex.a * i.color.a;
                return float4(surfaceColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
