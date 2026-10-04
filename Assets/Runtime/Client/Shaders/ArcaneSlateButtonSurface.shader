Shader "NinthAge/UI/ArcaneSlate/ButtonSurface" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillTop ("Fill Top", Color) = (0.1254902,0.1568627,0.2,1)
        _FillBottom ("Fill Bottom", Color) = (0.0901961,0.1176471,0.1529412,1)
        _BorderColor ("Border Color", Color) = (0.22745098,0.2745098,0.3372549,1)
        _BorderWidth ("Border Width (px)", Range(0,32)) = 2.0
        _CornerRadius ("Corner Radius (px)", Range(0,128)) = 4.0
        _TopHighlightColor ("Top Highlight Color", Color) = (1,1,1,1)
        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.06
        _TopHighlightAngle ("Top Highlight Angle (deg)", Range(-60,60)) = 0.0
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Width (normalized)", Range(0,0.3)) = 0.06
        _InnerDarkenStrength ("Inner Darken Strength", Range(0,1)) = 0.8
        _InnerBorderColor ("Inner Border Color", Color) = (0,0,0,1)
        _InnerBorderWidth ("Inner Border Width (px)", Range(0,32)) = 2.0
        _AccentColor ("Accent Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _AccentAmount ("Accent Amount", Range(0,1)) = 0.0
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.06
        _Opacity ("Opacity", Range(0,1)) = 1.0
        _RectSize ("Rect Size (px)", Vector) = (0,0,0,0)
        
        [Toggle] _HoverAmount ("Hover", Float) = 0
        [Toggle] _PressedAmount ("Pressed", Float) = 0
        [Toggle] _FocusedAmount ("Focused", Float) = 0
        [Toggle] _DisabledAmount ("Disabled", Float) = 0
        
        _HoverBrightness ("Hover Brightness Boost", Range(0,0.3)) = 0.08
        _PressedDarkness ("Pressed Darkness", Range(0,0.3)) = 0.15
        _FocusedGlowColor ("Focused Glow Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _FocusedGlowStrength ("Focused Glow Strength", Range(0,1)) = 0.3
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

            float4 _FillTop;
            float4 _FillBottom;
            float4 _BorderColor;
            float _BorderWidth;
            float _CornerRadius;
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float _TopHighlightAngle;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float _InnerDarkenStrength;
            float4 _InnerBorderColor;
            float _InnerBorderWidth;
            float4 _AccentColor;
            float _AccentAmount;
            float _VignetteStrength;
            float _Opacity;
            float4 _RectSize;
            float _HoverAmount;
            float _PressedAmount;
            float _FocusedAmount;
            float _DisabledAmount;
            float _HoverBrightness;
            float _PressedDarkness;
            float4 _FocusedGlowColor;
            float _FocusedGlowStrength;

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

            // Signed distance to rounded rectangle centered at (0,0) with half-extents b and radius r.
            static float sdRoundRect(float2 p, float2 b, float r) {
                float2 q = abs(p) - b;
                float outside = length(max(q, 0.0));
                float inside = min(max(q.x, q.y), 0.0);
                return outside + inside - r;
            }

            static float resolveBorderWidthPx(float widthValue) {
                return widthValue <= 1.0 ? widthValue * 100.0 : widthValue;
            }

            static float resolveCornerRadiusPx(float radiusValue, float minDim) {
                float radiusPx = radiusValue <= 1.0 ? radiusValue * 100.0 : radiusValue;
                return min(radiusPx, minDim * 0.5 - 1.0);
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

                // Compute pixel-space rounded-rect parameters
                float rPx = max(0.0, resolveCornerRadiusPx(_CornerRadius, minDim));
                float2 b = rectSizePx * 0.5 - rPx;

                float sdf = sdRoundRect(p, b, rPx);
                float aa = max(1.0, fwidth(sdf));

                // Border thickness in pixel units
                float borderPx = resolveBorderWidthPx(_BorderWidth);

                // Masks (pixel-space SDF -> consistent border on all edges)
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // Parse state toggles as booleans
                float hoverOn = (_HoverAmount >= 0.5) ? 1.0 : 0.0;
                float pressedOn = (_PressedAmount >= 0.5) ? 1.0 : 0.0;
                float focusedOn = (_FocusedAmount >= 0.5) ? 1.0 : 0.0;
                float disabledOn = (_DisabledAmount >= 0.5) ? 1.0 : 0.0;

                // Base vertical gradient
                float3 baseFill = lerp(_FillBottom.rgb, _FillTop.rgb, uv.y);

                // Inner shadow - darker near inner edges
                float innerShadowPx = _InnerShadowStrength * minDim;
                float dInside = max(-sdf, 0.0);
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                baseFill = baseFill * (1.0 - _InnerShadowColor.rgb * (shadowMask * _InnerShadowColor.a * _InnerDarkenStrength));

                // Apply accent to main body (exclude border)
                float accentNorm = smoothstep(0.86, 0.98, uv.y);
                float bodyAccentMask = maskInner * accentNorm * _AccentAmount;
                baseFill = lerp(baseFill, lerp(baseFill, _AccentColor.rgb, 0.5), bodyAccentMask);

                // Compute top highlight and accent masks
                float topStripStart = 1.0 - clamp(_TopHighlightStrength * 2.5 + 0.04, 0.03, 0.3);
                float angleRad = radians(_TopHighlightAngle);
                float cosA = cos(angleRad);
                float sinA = sin(angleRad);
                float2 centered = uv - 0.5;
                float2 rotated = float2(centered.x * cosA - centered.y * sinA, centered.x * sinA + centered.y * cosA);
                float ruvY = rotated.y + 0.5;
                float topHighlightNorm = smoothstep(topStripStart, 1.0 - 0.01, ruvY);

                // Border shading: compute relative position inside the border
                float borderDenom = max(borderPx, 0.0001);
                float borderT = saturate((-sdf) / borderDenom);
                float outerEdge = 1.0 - borderT;
                float innerEdge = borderT;

                // Inner border (solid color) in pixel units
                float innerBorderPx = resolveBorderWidthPx(_InnerBorderWidth);
                float maskInner2 = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2);

                // Approximate local normal from SDF derivatives (points outward)
                float2 grad = float2(ddx(sdf), ddy(sdf));
                float gradLen = length(grad) + 1e-6;
                float2 n = grad / gradLen;

                // Facing factors for top and bottom shading
                float topFactor = smoothstep(0.15, 0.9, n.y);
                float bottomFactor = smoothstep(0.15, 0.9, -n.y);

                // Base border color and subtle edge variations
                float3 borderBase = _BorderColor.rgb;

                // Subtle top rim highlight on outer edge (reduced when pressed)
                float3 hl = _TopHighlightColor.rgb * 0.22;
                float highlightInfluence = topHighlightNorm * lerp(0.6, 1.0, topFactor);
                borderBase += hl * highlightInfluence * outerEdge * _TopHighlightStrength * 1.8 * (1.0 - pressedOn * 0.6);

                // Darker lower rim toward the inner edge (enhanced when pressed)
                borderBase = lerp(borderBase, borderBase * (1.0 - 0.14), bottomFactor * innerEdge);
                borderBase *= lerp(1.0, 0.92, pressedOn * 0.5);

                // Small specular-like rim on corners/curves
                float2 lightDir = normalize(float2(-0.35, 0.85));
                float nl = saturate(dot(n, lightDir));
                float spec = pow(nl, 20.0) * 0.06 * outerEdge;
                borderBase += spec * (1.0 - pressedOn * 0.4);

                // Angled top highlight band on body
                float topBand = smoothstep(topStripStart, 1.0 - 0.01, ruvY) * maskInner;
                float3 bodyFinal = baseFill;
                bodyFinal = lerp(bodyFinal, bodyFinal + _TopHighlightColor.rgb * 0.14, topBand * _TopHighlightStrength * (1.0 - pressedOn));

                // Apply button state modifiers
                // Hover: brighten slightly
                bodyFinal += hoverOn * _HoverBrightness;

                // Pressed: darken and compress the visual
                bodyFinal *= lerp(1.0, 1.0 - _PressedDarkness, pressedOn);

                // Focused/Glow: add a subtle glow contribution to the border and body
                float3 borderFinal = borderBase;
                if (focusedOn > 0.5) {
                    // Add glow to border edges
                    borderFinal = lerp(borderFinal, _FocusedGlowColor.rgb, _FocusedGlowStrength * 0.5);
                    // Glow also influences body slightly
                    bodyFinal = lerp(bodyFinal, bodyFinal + _FocusedGlowColor.rgb * 0.1, focusedOn * _FocusedGlowStrength * 0.3);
                }

                // Vignette toward corners
                float vign = length((uv - 0.5) * float2(1.0, 1.0));
                float vignNorm = smoothstep(0.4, 0.75, vign);
                float3 bodyWithVignette = lerp(bodyFinal, bodyFinal * (1.0 - _VignetteStrength), vignNorm * maskInner);

                // Blend border over body
                float3 finalColor = lerp(bodyWithVignette, borderFinal, borderMask * 0.98);

                // Overlay inner border (solid color)
                finalColor = lerp(finalColor, _InnerBorderColor.rgb, innerBorderMask);

                // Disabled state: desaturate
                if (disabledOn > 0.5) {
                    float gray = dot(finalColor, float3(0.299, 0.587, 0.114));
                    finalColor = lerp(finalColor, float3(gray, gray, gray), 0.5);
                    // Also darken disabled button
                    finalColor *= 0.75;
                }

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
