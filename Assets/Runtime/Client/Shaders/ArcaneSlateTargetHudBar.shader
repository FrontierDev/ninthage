Shader "NinthAge/UI/ArcaneSlate/TargetHudBar" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (0.59607846,0.13725491,0.13725491,1)
        _FillAmount ("Fill Amount", Range(0,1)) = 1.0
        _FrameColor ("Frame Color", Color) = (0.06666667,0.07450981,0.08235294,1)
        _AccentColor ("Accent Color", Color) = (0.79607844,0.6901961,0.47058824,1)
        _TroughColor ("Trough Color", Color) = (0.03529412,0.03921569,0.04705882,1)
        _EndPointDepth ("End Point Depth (px)", Range(0,64)) = 12.0
        _EndShoulderInset ("End Shoulder Inset (px)", Range(0,32)) = 3.0
        _OuterBorderWidth ("Outer Border Width (px)", Range(0,8)) = 2.0
        _AccentWidth ("Accent Width (px)", Range(0,4)) = 1.0
        _TroughInset ("Trough Inset (px)", Range(0,8)) = 3.0
        _FillInset ("Fill Inset (px)", Range(0,4)) = 1.0
        _FillTopStrength ("Fill Top Strength", Range(0,1)) = 0.18
        _FillBottomStrength ("Fill Bottom Strength", Range(0,1)) = 0.25
        _InnerHighlightColor ("Inner Highlight Color", Color) = (1,1,1,1)
        _InnerHighlightStrength ("Inner Highlight Strength", Range(0,1)) = 0.16
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Strength", Range(0,1)) = 0.30
        _FillEdgeColor ("Fill Edge Color", Color) = (1,1,1,1)
        _FillEdgeStrength ("Fill Edge Strength", Range(0,1)) = 0.20
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
            float4 _MainColor;
            float _FillAmount;
            float4 _FrameColor;
            float4 _AccentColor;
            float4 _TroughColor;
            float _EndPointDepth;
            float _EndShoulderInset;
            float _OuterBorderWidth;
            float _AccentWidth;
            float _TroughInset;
            float _FillInset;
            float _FillTopStrength;
            float _FillBottomStrength;
            float4 _InnerHighlightColor;
            float _InnerHighlightStrength;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float4 _FillEdgeColor;
            float _FillEdgeStrength;
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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 rectSizePx = ArcaneSlateResolveRectSizePx(_RectSize, uv);
                float2 p = (uv - 0.5) * rectSizePx;
                float2 outerHalf = max(rectSizePx * 0.5, float2(0.5, 0.5));
                float minDim = min(rectSizePx.x, rectSizePx.y);

                float outerSdf = ArcaneSlateSdPointedBar(
                    p, outerHalf, _EndPointDepth, _EndShoulderInset);
                float outerAa = ArcaneSlateAntiAliasWidth(outerSdf);
                float outerMask = smoothstep(outerAa, -outerAa, outerSdf);

                float outerBorderWidth = max(0.0, _OuterBorderWidth);
                float borderInnerMask = smoothstep(outerAa, -outerAa, outerSdf + outerBorderWidth);
                float borderMask = saturate(outerMask - borderInnerMask);

                float accentWidth = max(0.0, _AccentWidth);
                float accentInnerMask = smoothstep(
                    outerAa, -outerAa, outerSdf + outerBorderWidth + accentWidth);
                float accentMask = saturate(borderInnerMask - accentInnerMask);

                float troughInset = max(0.0, _TroughInset);
                float2 troughHalf = max(outerHalf - troughInset, float2(0.5, 0.5));
                float troughDepth = max(0.0, _EndPointDepth - troughInset);
                float troughShoulder = max(0.0, _EndShoulderInset - troughInset);
                float troughSdf = ArcaneSlateSdPointedBar(
                    p, troughHalf, troughDepth, troughShoulder);
                float troughAa = ArcaneSlateAntiAliasWidth(troughSdf);
                float troughMask = smoothstep(troughAa, -troughAa, troughSdf);

                float fillInset = max(0.0, _FillInset);
                float2 fillHalf = max(troughHalf - fillInset, float2(0.5, 0.5));
                float fillDepth = max(0.0, troughDepth - fillInset);
                float fillShoulder = max(0.0, troughShoulder - fillInset);
                float fillSdf = ArcaneSlateSdPointedBar(
                    p, fillHalf, fillDepth, fillShoulder);
                float fillAa = ArcaneSlateAntiAliasWidth(fillSdf);
                float fillShapeMask = smoothstep(fillAa, -fillAa, fillSdf);

                float fillAmount = saturate(_FillAmount);
                float fillEdge = lerp(-fillHalf.x, fillHalf.x, fillAmount);
                float signedFillDistance = p.x - fillEdge;
                float directionalMask = smoothstep(fillAa, -fillAa, signedFillDistance);
                float fillPresence = smoothstep(0.0, 0.001, fillAmount);
                float fillMask = fillShapeMask * directionalMask * fillPresence;

                float upper = saturate((p.y / max(fillHalf.y, 0.5)) * 0.5 + 0.5);
                float frameUpper = smoothstep(0.15, 0.9, upper);
                float3 frameColor = lerp(_FrameColor.rgb, _AccentColor.rgb, frameUpper * 0.08);
                float3 surfaceColor = lerp(frameColor, _AccentColor.rgb, accentMask);
                surfaceColor = lerp(surfaceColor, _TroughColor.rgb, troughMask);

                float fillGradient = saturate((p.y / max(fillHalf.y, 0.5)) * 0.5 + 0.5);
                float fillValue = lerp(1.0 - _FillBottomStrength, 1.0 + _FillTopStrength, fillGradient);
                float3 fillColor = saturate(_MainColor.rgb * fillValue);

                float fillTopEdge = 1.0 - smoothstep(0.25, 1.5, abs(p.y - fillHalf.y));
                fillColor = lerp(fillColor, _InnerHighlightColor.rgb,
                    fillTopEdge * _InnerHighlightStrength);

                float fillInside = max(-fillSdf, 0.0);
                float fillShadowWidth = max(1.0, _InnerShadowStrength * minDim * 0.16);
                float fillShadow = 1.0 - smoothstep(0.0, fillShadowWidth + fillAa, fillInside);
                fillColor = lerp(fillColor, _InnerShadowColor.rgb,
                    fillShadow * _InnerShadowStrength * 0.55);
                surfaceColor = lerp(surfaceColor, fillColor, fillMask);

                float edgeDistance = abs(p.x - fillEdge);
                float activeFill = step(0.001, fillAmount) * (1.0 - step(0.999, fillAmount));
                float fillEdgeMask = (1.0 - smoothstep(0.25, 1.25, edgeDistance))
                    * directionalMask * fillShapeMask * activeFill;
                surfaceColor = lerp(surfaceColor, _FillEdgeColor.rgb,
                    fillEdgeMask * _FillEdgeStrength);
                surfaceColor = lerp(surfaceColor, _FrameColor.rgb, borderMask);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * outerMask * tex.a * i.color.a;
                return float4(surfaceColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
