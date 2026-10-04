Shader "NinthAge/UI/ArcaneSlate/CheckboxDiamond" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BorderColor ("Border Color", Color) = (0.22745098,0.2745098,0.3372549,1)
        _BorderWidth ("Border Width (px)", Range(0,16)) = 2.0
        _InnerBorderColor ("Inner Border Color", Color) = (0,0,0,1)
        _InnerBorderWidth ("Inner Border Width (px)", Range(0,16)) = 1.0
        _EmptyFillColor ("Empty Fill Color", Color) = (0.0901961,0.1176471,0.1529412,1)
        _CheckedFillColor ("Checked Fill Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _TopHighlightColor ("Top Highlight Color", Color) = (1,1,1,1)
        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.06
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Width (normalized)", Range(0,0.3)) = 0.06
        _InnerDarkenStrength ("Inner Darken Strength", Range(0,1)) = 0.8
        [Toggle] _IsChecked ("Is Checked", Float) = 0
        [Toggle] _HoverAmount ("Hover", Float) = 0
        [Toggle] _DisabledAmount ("Disabled", Float) = 0
        _CheckFillAmount ("Check Fill Amount", Range(0,1)) = 0
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _BorderColor;
            float _BorderWidth;
            float4 _InnerBorderColor;
            float _InnerBorderWidth;
            float4 _EmptyFillColor;
            float4 _CheckedFillColor;
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float _InnerDarkenStrength;
            float _IsChecked;
            float _HoverAmount;
            float _DisabledAmount;
            float _CheckFillAmount;
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

            v2f vert(appdata_t v) {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            // Signed distance to diamond (rotated square) centered at (0,0) with half-extents s
            static float sdDiamond(float2 p, float s) {
                float q = abs(p.x) + abs(p.y) - s;
                return q;
            }

            // Distance to center of diamond (for fill animation)
            static float distToCenter(float2 p) {
                return length(p);
            }

            static float resolveBorderWidthPx(float widthValue) {
                return widthValue <= 1.0 ? widthValue * 100.0 : widthValue;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // Determine element size in pixels
                float2 rectSizePx;
                if (_RectSize.x > 1.0 && _RectSize.y > 1.0) {
                    rectSizePx = _RectSize.xy;
                } else {
                    float2 gdx = ddx(uv);
                    float2 gdy = ddy(uv);
                    float a = gdx.x;
                    float c = gdx.y;
                    float b = gdy.x;
                    float d = gdy.y;
                    float det = a * d - b * c;
                    float invDet = 1.0 / (abs(det) + 1e-6);
                    float widthPx = length(float2(d, -c)) * invDet;
                    float heightPx = length(float2(-b, a)) * invDet;
                    widthPx = max(widthPx, 1.0);
                    heightPx = max(heightPx, 1.0);
                    rectSizePx = float2(widthPx, heightPx);
                }

                float minDim = min(rectSizePx.x, rectSizePx.y);

                // Convert to pixel-space coordinates centered at 0
                float2 p = (uv - 0.5) * rectSizePx;

                // Border thickness in pixel units
                float borderPx = resolveBorderWidthPx(_BorderWidth);
                float innerBorderPx = resolveBorderWidthPx(_InnerBorderWidth);

                // Diamond SDF - size adjusted so border sits at element edge
                float diamondSize = minDim * 0.5 - borderPx * 0.5;
                float sdf = sdDiamond(p, diamondSize);
                float aa = max(1.0, fwidth(sdf));

                // Masks for outer and inner edges
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // Parse state toggles
                float isChecked = (_IsChecked >= 0.5) ? 1.0 : 0.0;
                float hoverOn = (_HoverAmount >= 0.5) ? 1.0 : 0.0;
                float disabledOn = (_DisabledAmount >= 0.5) ? 1.0 : 0.0;

                // Base fill - blend between empty and checked colors based on fill amount
                float3 fillColor = lerp(_EmptyFillColor.rgb, _CheckedFillColor.rgb, _CheckFillAmount);

                // Inner shadow - darker near inner edges (only apply to inner fill area)
                float innerShadowPx = _InnerShadowStrength * minDim;
                float dInside = max(-sdf, 0.0);
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                float3 innerFill = fillColor * (1.0 - _InnerShadowColor.rgb * (shadowMask * _InnerShadowColor.a * _InnerDarkenStrength));

                // Apply hover brightening to inner fill only
                innerFill += hoverOn * 0.05 * maskInner;

                // Border base color
                float3 borderBase = _BorderColor.rgb;

                // Top highlight on border (diagonal accent)
                float topAccent = max(0.0, -p.y - abs(p.x)) / diamondSize;
                topAccent = smoothstep(0.5, 1.0, topAccent);
                float3 hl = _TopHighlightColor.rgb * 0.18;
                borderBase += hl * topAccent * _TopHighlightStrength;

                // Bottom shadow on border
                float bottomAccent = max(0.0, p.y - abs(p.x)) / diamondSize;
                bottomAccent = smoothstep(0.5, 1.0, bottomAccent);
                borderBase *= lerp(1.0, 0.85, bottomAccent * 0.3);

                // Blend: use innerFill in the fill area, borderBase in the border area
                float3 finalColor = lerp(innerFill, borderBase, borderMask);

                // Inner border overlay
                float maskInner2 = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2);
                finalColor = lerp(finalColor, _InnerBorderColor.rgb, innerBorderMask);

                // Disabled state: desaturate
                if (disabledOn > 0.5) {
                    float gray = dot(finalColor, float3(0.299, 0.587, 0.114));
                    finalColor = lerp(finalColor, float3(gray, gray, gray) * 0.75, 1.0);
                }

                // Center fill animation - grows from center when checked
                float distCenter = distToCenter(p);
                float maxFillRadius = diamondSize * 0.85;
                float fillRadius = maxFillRadius * _CheckFillAmount;
                float centerFillMask = smoothstep(fillRadius + aa, fillRadius - aa, distCenter);
                float3 centerFill = _CheckedFillColor.rgb * 0.85;
                finalColor = lerp(finalColor, centerFill, centerFillMask * maskInner * isChecked);

                // Sample sprite alpha and apply opacity
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
