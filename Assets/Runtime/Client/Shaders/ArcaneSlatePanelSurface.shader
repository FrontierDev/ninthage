Shader "NinthAge/UI/ArcaneSlate/PanelSurface" {
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
        _AccentColor ("Accent Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _InnerBorderColor ("Inner Border Color", Color) = (0,0,0,1)
        _InnerBorderWidth ("Inner Border Width (px)", Range(0,32)) = 2.0
        _AccentAmount ("Accent Amount", Range(0,1)) = 0
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.06
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

            float4 _FillTop;
            float4 _FillBottom;
            float4 _BorderColor;
            float _BorderWidth; // pixel width; values <= 1 are treated as legacy normalized inputs
            float _CornerRadius; // pixel radius; values <= 1 are treated as legacy normalized inputs
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float _TopHighlightAngle;
            float4 _InnerShadowColor;
            float _InnerShadowStrength; // fraction of min dimension
            float _InnerDarkenStrength;
            float4 _InnerBorderColor;
            float _InnerBorderWidth; // pixel width; values <= 1 are treated as legacy normalized inputs
            float4 _AccentColor;
            float _AccentAmount;
            float _VignetteStrength;
            float _Opacity;

            float4 _RectSize; // x = width in pixels, y = height in pixels

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
                // Backward compatibility: old materials often used 0.01, 0.02, etc.
                // Treat values <= 1 as a 0..100px scale so 0.02 becomes ~2px.
                return widthValue <= 1.0 ? widthValue * 100.0 : widthValue;
            }

            static float resolveCornerRadiusPx(float radiusValue, float minDim) {
                float radiusPx = radiusValue <= 1.0 ? radiusValue * 100.0 : radiusValue;
                return min(radiusPx, minDim * 0.5 - 1.0);
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // Determine element size in pixels. Prefer explicit _RectSize when provided,
                // otherwise derive size from GPU derivatives (ddx/ddy) so the shader is self-sufficient.
                float2 rectSizePx;
                if (_RectSize.x > 1.0 && _RectSize.y > 1.0) {
                    rectSizePx = _RectSize.xy;
                } else {
                    float2 gdx = ddx(uv);
                    float2 gdy = ddy(uv);
                    float a = gdx.x; // du/dx
                    float c = gdx.y; // dv/dx
                    float b = gdy.x; // du/dy
                    float d = gdy.y; // dv/dy
                    float det = a * d - b * c;
                    float invDet = 1.0 / (abs(det) + 1e-6);
                    float widthPx = length(float2(d, -c)) * invDet;
                    float heightPx = length(float2(-b, a)) * invDet;
                    widthPx = max(widthPx, 1.0);
                    heightPx = max(heightPx, 1.0);
                    rectSizePx = float2(widthPx, heightPx);
                }

                float minDim = min(rectSizePx.x, rectSizePx.y);

                // convert to pixel-space coordinates centered at 0
                float2 p = (uv - 0.5) * rectSizePx;

                // compute pixel-space rounded-rect parameters
                float rPx = max(0.0, resolveCornerRadiusPx(_CornerRadius, minDim));
                float2 b = rectSizePx * 0.5 - rPx;

                float sdf = sdRoundRect(p, b, rPx);
                float aa = max(1.0, fwidth(sdf)); // anti-alias width in pixel units

                // border thickness in pixel units.
                float borderPx = resolveBorderWidthPx(_BorderWidth);

                // masks (pixel-space SDF -> consistent border on all edges)
                float maskOuter = smoothstep(aa, -aa, sdf); // 1 inside
                float maskInner = smoothstep(aa, -aa, sdf + borderPx); // inner area shrunk by border width
                float borderMask = saturate(maskOuter - maskInner);

                // base vertical gradient (use normalized uv for smooth vertical gradient)
                float3 baseFill = lerp(_FillBottom.rgb, _FillTop.rgb, uv.y);

                // inner shadow - darker near inner edges (convert configured strength to pixels)
                float innerShadowPx = _InnerShadowStrength * minDim;
                float dInside = max(-sdf, 0.0);
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                baseFill = baseFill * (1.0 - _InnerShadowColor.rgb * (shadowMask * _InnerShadowColor.a * _InnerDarkenStrength));

                // apply accent only to main body (exclude border)
                float accentNorm = smoothstep(0.86, 0.98, uv.y);
                float bodyAccentMask = maskInner * accentNorm * _AccentAmount;
                baseFill = lerp(baseFill, lerp(baseFill, _AccentColor.rgb, 0.5), bodyAccentMask);

                // compute top highlight and accent masks (will be applied only to the border)
                float topStripStart = 1.0 - clamp(_TopHighlightStrength * 2.5 + 0.04, 0.03, 0.3);
                // rotate the UV space around the center by _TopHighlightAngle so the highlight band can be angled
                float angleRad = radians(_TopHighlightAngle);
                float cosA = cos(angleRad);
                float sinA = sin(angleRad);
                float2 centered = uv - 0.5;
                float2 rotated = float2(centered.x * cosA - centered.y * sinA, centered.x * sinA + centered.y * cosA);
                float ruvY = rotated.y + 0.5;
                float topHighlightNorm = smoothstep(topStripStart, 1.0 - 0.01, ruvY);

                // Border shading: compute relative position inside the border (0 outer -> 1 inner)
                float borderDenom = max(borderPx, 0.0001);
                float borderT = saturate((-sdf) / borderDenom);
                float outerEdge = 1.0 - borderT;
                float innerEdge = borderT;

                // inner border (solid color) in pixel units
                float innerBorderPx = resolveBorderWidthPx(_InnerBorderWidth);
                float maskInner2 = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2);

                // approximate local normal from SDF derivatives (points outward)
                float2 grad = float2(ddx(sdf), ddy(sdf));
                float gradLen = length(grad) + 1e-6;
                float2 n = grad / gradLen;

                // facing factors for top and bottom shading
                float topFactor = smoothstep(0.15, 0.9, n.y);
                float bottomFactor = smoothstep(0.15, 0.9, -n.y);

                // base border color and subtle edge variations
                float3 borderBase = _BorderColor.rgb;

                // subtle top rim highlight on outer edge of the border (angle-controlled)
                float3 hl = _TopHighlightColor.rgb * 0.22;
                // Make the angled band visible on sides by using the rotated-band mask as the base,
                // then boost it where the surface normal faces upwards.
                float highlightInfluence = topHighlightNorm * lerp(0.6, 1.0, topFactor);
                borderBase += hl * highlightInfluence * outerEdge * _TopHighlightStrength * 1.8;

                // darker lower rim toward the inner edge
                borderBase = lerp(borderBase, borderBase * (1.0 - 0.14), bottomFactor * innerEdge);

                // small specular-like rim on corners/curves using a fixed light direction
                float2 lightDir = normalize(float2(-0.35, 0.85));
                float nl = saturate(dot(n, lightDir));
                float spec = pow(nl, 20.0) * 0.06 * outerEdge;
                borderBase += spec;

                // blend border over body (body is baseFill with accent applied)
                // subtle vignette toward corners - apply only to main body (maskInner)
                float vign = length((uv - 0.5) * float2(1.0, 1.0));
                float vignNorm = smoothstep(0.4, 0.75, vign);
                float3 bodyWithVignette = lerp(baseFill, baseFill * (1.0 - _VignetteStrength), vignNorm * maskInner);

                float3 finalColor = lerp(bodyWithVignette, borderBase, borderMask * 0.98);

                // overlay inner border (solid color) on top of blended result
                finalColor = lerp(finalColor, _InnerBorderColor.rgb, innerBorderMask);

                // sample sprite alpha (so you can use a white rounded mask or simple white square)
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
