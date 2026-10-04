Shader "NinthAge/UI/ArcaneSlate/TargetPanel" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillTop ("Fill Top", Color) = (0.1254902,0.1568627,0.2,1)
        _FillBottom ("Fill Bottom", Color) = (0.0901961,0.1176471,0.1529412,1)
        _BorderColor ("Border Color", Color) = (0.22745098,0.2745098,0.3372549,1)
        _BorderWidth ("Border Width (px)", Range(0,32)) = 2.0
        _CornerInset ("Corner Inset (px)", Range(0,128)) = 6.0
        _VerticalBorderColor ("Vertical Border Color", Color) = (0.878,0.749,0.447,1)
        _VerticalBorderStrength ("Vertical Border Strength", Range(0,1)) = 0.4
        _VerticalBorderWidth ("Vertical Border Width (px)", Range(0,32)) = 3.0
        _TopHighlightColor ("Top Highlight Color", Color) = (1,1,1,1)
        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.06
        _TopHighlightAngle ("Top Highlight Angle (deg)", Range(-60,60)) = 0.0
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Width (normalized)", Range(0,0.3)) = 0.06
        _InnerDarkenStrength ("Inner Darken Strength", Range(0,1)) = 0.8
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.1
        _BorderNoiseScale ("Border Noise Scale", Range(0.001,0.5)) = 0.05
        _BorderNoiseStrength ("Border Noise Strength", Range(0,1)) = 0.4
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
            float _BorderWidth;
            float _CornerInset;
            float4 _VerticalBorderColor;
            float _VerticalBorderStrength;
            float _VerticalBorderWidth;
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float _TopHighlightAngle;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float _InnerDarkenStrength;
            float _VignetteStrength;
            float _BorderNoiseScale;
            float _BorderNoiseStrength;
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

            // Rectangle with concave circular notches at each corner.
            // A circle of radius notchRadius is centered at each corner and subtracted from the rect.
            static float sdConcaveCornerRect(float2 p, float2 b, float notchRadius) {
                float2 q = abs(p);
                // Plain rectangle SDF (negative inside)
                float mainDist = max(q.x - b.x, q.y - b.y);
                // Notch circle centered at corner (b.x, b.y) in first quadrant.
                // Positive when inside the notch circle (within notchRadius of corner).
                float notchDist = notchRadius - length(q - b);
                // Subtract notch from rect: outside shape if outside rect OR inside notch circle
                return max(mainDist, notchDist);
            }

            static float resolveBorderWidthPx(float widthValue) {
                return widthValue <= 1.0 ? widthValue * 100.0 : widthValue;
            }

            // Simple hash-based pseudo-random function
            static float hash(float2 q) {
                return frac(sin(dot(q, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth value noise (bilinear interpolation of hashes)
            static float valueNoise(float2 q) {
                float2 i = floor(q);
                float2 f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep
                return lerp(
                    lerp(hash(i + float2(0,0)), hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x),
                    u.y
                );
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

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
                float2 p = (uv - 0.5) * rectSizePx;
                float2 b = rectSizePx * 0.5;

                // Corner notch radius in pixels
                float notchPx = max(0.0, _CornerInset <= 1.0 ? _CornerInset * 100.0 : _CornerInset);
                notchPx = min(notchPx, minDim * 0.45);

                float sdf = sdConcaveCornerRect(p, b, notchPx);
                float aa = max(1.0, fwidth(sdf));

                float borderPx = resolveBorderWidthPx(_BorderWidth);
                float vertBorderPx = resolveBorderWidthPx(_VerticalBorderWidth);

                // Main masks
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // Base vertical gradient
                float3 baseFill = lerp(_FillBottom.rgb, _FillTop.rgb, uv.y);

                // Inner shadow
                float innerShadowPx = _InnerShadowStrength * minDim;
                float dInside = max(-sdf, 0.0);
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                baseFill = baseFill * (1.0 - _InnerShadowColor.rgb * (shadowMask * _InnerShadowColor.a * _InnerDarkenStrength));

                // Surface normal from SDF gradient
                float2 grad = float2(ddx(sdf), ddy(sdf));
                float gradLen = length(grad) + 1e-6;
                float2 n = grad / gradLen;

                // Top highlight (same approach as ArcaneSlatePanelSurface)
                float topStripStart = 1.0 - clamp(_TopHighlightStrength * 2.5 + 0.04, 0.03, 0.3);
                float angleRad = radians(_TopHighlightAngle);
                float cosA = cos(angleRad);
                float sinA = sin(angleRad);
                float2 centered = uv - 0.5;
                float2 rotated = float2(centered.x * cosA - centered.y * sinA, centered.x * sinA + centered.y * cosA);
                float ruvY = rotated.y + 0.5;
                float topHighlightNorm = smoothstep(topStripStart, 0.99, ruvY);

                float borderDenom = max(borderPx, 0.0001);
                float borderT = saturate((-sdf) / borderDenom);
                float outerEdge = 1.0 - borderT;
                float topFactor = smoothstep(0.15, 0.9, n.y);

                float3 borderColorBase = _BorderColor.rgb;
                float3 hl = _TopHighlightColor.rgb * 0.22;
                float highlightInfluence = topHighlightNorm * lerp(0.6, 1.0, topFactor);
                borderColorBase += hl * highlightInfluence * outerEdge * _TopHighlightStrength * 1.8;

                // Deteriorated border: erode border mask with value noise
                float borderNoise = valueNoise(p * _BorderNoiseScale);
                // Second octave for finer detail
                borderNoise = borderNoise * 0.65 + valueNoise(p * _BorderNoiseScale * 3.1) * 0.35;
                // Erode: shrink the border where noise is low, creating worn gaps
                float erodedBorderMask = borderMask * smoothstep(
                    _BorderNoiseStrength * 0.5,
                    _BorderNoiseStrength * 0.5 + 0.3,
                    borderNoise
                );

                // Inner border with noisy gradient based on distance from center
                float distFromCenter = length(p);
                float distFromInnerEdge = -(sdf + borderPx); // positive = inside the inner boundary
                float innerBorderStrip = smoothstep(0.0, vertBorderPx + aa, distFromInnerEdge)
                                       * smoothstep(vertBorderPx + aa + 2.0, 0.0, distFromInnerEdge);
                
                // Radial gradient from center
                float centerGradient = distFromCenter / length(b);
                // Noise based on pixel position
                float noise = hash(p * 0.05) * 0.3 + 0.7; // noise in range [0.7, 1.0]
                float noisyGradient = centerGradient * noise;

                // Erode inner border with same two-octave noise as outer border
                float innerBorderNoise = valueNoise(p * _BorderNoiseScale * 1.3);
                innerBorderNoise = innerBorderNoise * 0.65 + valueNoise(p * _BorderNoiseScale * 3.9) * 0.35;
                float innerBorderErode = smoothstep(
                    _BorderNoiseStrength * 0.5,
                    _BorderNoiseStrength * 0.5 + 0.3,
                    innerBorderNoise
                );
                
                float innerBorderMask = innerBorderStrip * noisyGradient * maskInner * innerBorderErode;

                // Final composition
                float vign = length((uv - 0.5) * float2(1.2, 0.8));
                float vignNorm = smoothstep(0.35, 0.75, vign);
                float3 bodyWithVignette = lerp(baseFill, baseFill * (1.0 - _VignetteStrength), vignNorm * maskInner);

                float3 finalColor = lerp(bodyWithVignette, borderColorBase, erodedBorderMask);
                // Overlay inner border on top
                finalColor = lerp(finalColor, _VerticalBorderColor.rgb, innerBorderMask * _VerticalBorderStrength);

                // Sample sprite alpha
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
