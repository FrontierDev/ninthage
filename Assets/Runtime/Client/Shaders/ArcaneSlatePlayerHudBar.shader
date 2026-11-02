Shader "NinthAge/UI/ArcaneSlate/PlayerHudBar" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (0.7254902,0.25490198,0.25490198,1)
        _FillAmount ("Fill Amount", Range(0,1)) = 1.0
        _FillDirection ("Fill Direction", Range(0,1)) = 0.0
        _FrameColor ("Frame Color", Color) = (0.11372549,0.11764706,0.12156863,1)
        _FrameHighlightColor ("Frame Highlight Color", Color) = (0.29411765,0.28235295,0.2627451,1)
        _TroughColor ("Trough Color", Color) = (0.06666667,0.07450981,0.08235294,1)
        _BorderColor ("Border Color", Color) = (0.03529412,0.03921569,0.04705882,1)
        _BorderWidth ("Border Width (px)", Range(0,4)) = 1.0
        _FrameInset ("Frame Inset (px)", Range(1,6)) = 2.0
        _FillInset ("Fill Inset (px)", Range(0,3)) = 1.0
        _LeftChamfer ("Left Chamfer (px)", Range(0,16)) = 6.0
        _RightChamfer ("Right Chamfer (px)", Range(0,16)) = 6.0
        _FillTopStrength ("Fill Top Strength", Range(0,1)) = 0.18
        _FillBottomStrength ("Fill Bottom Strength", Range(0,1)) = 0.25
        _InnerHighlightColor ("Inner Highlight Color", Color) = (1,1,1,1)
        _InnerHighlightStrength ("Inner Highlight Strength", Range(0,1)) = 0.18
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
            float _FillDirection;
            float4 _FrameColor;
            float4 _FrameHighlightColor;
            float4 _TroughColor;
            float4 _BorderColor;
            float _BorderWidth;
            float _FrameInset;
            float _FillInset;
            float _LeftChamfer;
            float _RightChamfer;
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

                float outerSdf = ArcaneSlateSdAsymmetricChamferRect(
                    p, outerHalf, _LeftChamfer, _RightChamfer);
                float outerAa = ArcaneSlateAntiAliasWidth(outerSdf);
                float outerMask = smoothstep(outerAa, -outerAa, outerSdf);

                float borderWidth = max(0.0, _BorderWidth);
                float borderInnerMask = smoothstep(outerAa, -outerAa, outerSdf + borderWidth);
                float borderMask = saturate(outerMask - borderInnerMask);

                float frameInset = max(0.0, _FrameInset);
                float2 troughHalf = max(outerHalf - frameInset, float2(0.5, 0.5));
                float troughSdf = ArcaneSlateSdAsymmetricChamferRect(
                    p, troughHalf, max(0.0, _LeftChamfer - frameInset),
                    max(0.0, _RightChamfer - frameInset));
                float troughAa = ArcaneSlateAntiAliasWidth(troughSdf);
                float troughMask = smoothstep(troughAa, -troughAa, troughSdf);

                float fillInset = max(0.0, _FillInset);
                float2 fillHalf = max(troughHalf - fillInset, float2(0.5, 0.5));
                float fillSdf = ArcaneSlateSdAsymmetricChamferRect(
                    p, fillHalf, max(0.0, _LeftChamfer - frameInset - fillInset),
                    max(0.0, _RightChamfer - frameInset - fillInset));
                float fillAa = ArcaneSlateAntiAliasWidth(fillSdf);
                float fillShapeMask = smoothstep(fillAa, -fillAa, fillSdf);

                float fillAmount = saturate(_FillAmount);
                float fillDirection = step(0.5, _FillDirection);
                float leftToRightEdge = lerp(-fillHalf.x, fillHalf.x, fillAmount);
                float rightToLeftEdge = lerp(fillHalf.x, -fillHalf.x, fillAmount);
                float fillEdge = lerp(leftToRightEdge, rightToLeftEdge, fillDirection);
                float signedFillDistance = p.x - fillEdge;
                float leftToRightMask = smoothstep(fillAa, -fillAa, signedFillDistance);
                float rightToLeftMask = smoothstep(-fillAa, fillAa, signedFillDistance);
                float directionalMask = lerp(leftToRightMask, rightToLeftMask, fillDirection);
                float fillPresence = smoothstep(0.0, 0.001, fillAmount);
                float fillMask = fillShapeMask * directionalMask * fillPresence;

                float upper = saturate((p.y / max(fillHalf.y, 0.5)) * 0.5 + 0.5);
                float frameUpper = smoothstep(0.15, 0.9, upper);
                float3 frameColor = lerp(_FrameColor.rgb, _FrameHighlightColor.rgb, frameUpper * 0.45);

                float troughInside = max(-troughSdf, 0.0);
                float troughShadowWidth = max(1.0, _InnerShadowStrength * minDim * 0.20);
                float troughShadow = 1.0 - smoothstep(0.0, troughShadowWidth + troughAa, troughInside);
                float3 troughColor = lerp(
                    _TroughColor.rgb,
                    _InnerShadowColor.rgb,
                    troughShadow * _InnerShadowStrength * 0.65);

                float3 surfaceColor = lerp(frameColor, troughColor, troughMask);

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
                surfaceColor = lerp(surfaceColor, _BorderColor.rgb, borderMask);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * outerMask * tex.a * i.color.a;
                return float4(surfaceColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
