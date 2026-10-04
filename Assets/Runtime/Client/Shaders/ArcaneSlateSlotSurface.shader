Shader "NinthAge/UI/ArcaneSlate/SlotSurface" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillTop ("Fill Top", Color) = (0.1294118,0.1647059,0.2078431,1)
        _FillBottom ("Fill Bottom", Color) = (0.09411765,0.1254902,0.1647059,1)
        _BorderColor ("Border Color", Color) = (0.2666667,0.3137255,0.3921569,1)
        _BorderWidth ("Border Width (normalized)", Range(0,0.2)) = 0.012
        _InnerBorderColor ("Inner Border Color", Color) = (0,0,0,1)
        _InnerBorderWidth ("Inner Border Width (normalized)", Range(0,0.2)) = 0.02
        _CornerRadius ("Corner Radius (normalized)", Range(0,0.5)) = 0.06
        _TopHighlightColor ("Top Highlight Color", Color) = (1,1,1,1)
        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.08
        _TopHighlightAngle ("Top Highlight Angle (deg)", Range(-60,60)) = 0.0
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Width (normalized)", Range(0,0.3)) = 0.06
        _InnerDarkenStrength ("Inner Darken Strength", Range(0,1)) = 0.9
        [Toggle]_HoverAmount ("Hover", Float) = 0
        [Toggle]_PressedAmount ("Pressed", Float) = 0
        [Toggle]_SelectedAmount ("Selected", Float) = 0
        _SelectedColor ("Selected Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _RarityColor ("Rarity Color", Color) = (0.7098039,0.6039216,0.4156863,1)
        _RarityAmount ("Rarity Amount", Range(0,1)) = 0
        _RarityPulseStrength ("Rarity Pulse Strength", Range(0,2)) = 0.8
        _RarityPulsePeriod ("Rarity Pulse Period (s)", Float) = 4.0
        _RarityPulseWidth ("Rarity Pulse Width (fraction)", Range(0.01,0.5)) = 0.12
        _RarityPulsePhase ("Rarity Pulse Phase (deg)", Range(0,360)) = 0.0
        _SelectedPulseBoost ("Selected Pulse Boost", Range(0,3)) = 1.0
        [Toggle]_DisabledAmount ("Disabled", Float) = 0
        _SelectedInnerShadowStrength ("Selected Inner Shadow Strength", Range(0,1)) = 0.7
        _AccentColor ("Accent Color", Color) = (0.43137255,0.48235294,0.8509804,1)
        _AccentAmount ("Accent Amount", Range(0,1)) = 0
        
        _IconTex ("Icon Texture", 2D) = "white" {}
        _IconTint ("Icon Tint", Color) = (1,1,1,1)
        _IconAmount ("Icon Amount", Range(0,1)) = 1
        _IconScale ("Icon Scale (relative)", Range(0.05,2)) = 0.6
        _IconOffset ("Icon Offset (normalized)", Vector) = (0,0,0,0)
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
            float _BorderWidth;
            float4 _InnerBorderColor;
            float _InnerBorderWidth;
            float _CornerRadius;
            float4 _TopHighlightColor;
            float _TopHighlightStrength;
            float _TopHighlightAngle;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float _InnerDarkenStrength;
            float _HoverAmount;
            float _PressedAmount;
            float _SelectedAmount;
            float4 _SelectedColor;
            float4 _RarityColor;
            float _RarityAmount;
            float _RarityPulseStrength;
            float _RarityPulsePeriod;
            float _RarityPulseWidth;
            float _RarityPulsePhase;
            float _SelectedPulseBoost;
            float _SelectedInnerShadowStrength;
            float _DisabledAmount;
            float4 _AccentColor;
            float _AccentAmount;
            float _VignetteStrength;
            float _Opacity;
            float4 _RectSize;
            sampler2D _IconTex;
            float4 _IconTex_ST;
            float4 _IconTint;
            float _IconAmount;
            float _IconScale;
            float4 _IconOffset;

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 position : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata_t v) {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            static float sdRoundRect(float2 p, float2 b, float r) {
                float2 q = abs(p) - b;
                float outside = length(max(q, 0.0));
                float inside = min(max(q.x, q.y), 0.0);
                return outside + inside - r;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // compute element size in pixels (explicit or derivative fallback)
                float2 rectSizePx;
                if (_RectSize.x > 1.0 && _RectSize.y > 1.0) {
                    rectSizePx = _RectSize.xy;
                } else {
                    float2 gdx = ddx(uv);
                    float2 gdy = ddy(uv);
                    float a = gdx.x; float c = gdx.y; float b = gdy.x; float d = gdy.y;
                    float det = a * d - b * c;
                    float invDet = 1.0 / (abs(det) + 1e-6);
                    float widthPx = length(float2(d, -c)) * invDet;
                    float heightPx = length(float2(-b, a)) * invDet;
                    rectSizePx = float2(max(widthPx, 1.0), max(heightPx, 1.0));
                }

                float minDim = min(rectSizePx.x, rectSizePx.y);

                // pixel-space coords and SDF
                float2 p = (uv - 0.5) * rectSizePx;
                float rPx = saturate(_CornerRadius) * minDim;
                float2 b = rectSizePx * 0.5 - rPx;
                float sdf = sdRoundRect(p, b, rPx);
                float aa = max(1.0, fwidth(sdf));

                // border masks
                float borderPx = _BorderWidth * minDim;
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // body gradient and slightly darker lower half
                float3 body = lerp(_FillBottom.rgb, _FillTop.rgb, uv.y);
                body = lerp(body, body * 0.85, smoothstep(0.0, 0.9, 1.0 - uv.y) * 0.6);

                // treat hover/pressed/selected/disabled as booleans (0 or 1)
                float hoverOn = (_HoverAmount >= 0.5) ? 1.0 : 0.0;
                float pressedOn = (_PressedAmount >= 0.5) ? 1.0 : 0.0;
                float selectedOn = (_SelectedAmount >= 0.5) ? 1.0 : 0.0;
                float disabledOn = (_DisabledAmount >= 0.5) ? 1.0 : 0.0;

                // inner shadow (recess) - produce a darkening overlay toward the shadow color
                // when selected, tint the inner shadow toward the rarity color
                float innerShadowPx = _InnerShadowStrength * minDim;
                float dInside = max(-sdf, 0.0);
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                float innerDarken = saturate(_InnerDarkenStrength);
                float selBlend = selectedOn * _SelectedInnerShadowStrength;
                float3 shadowColor = lerp(_InnerShadowColor.rgb, _RarityColor.rgb, selBlend);
                float3 shadowOverlay = lerp(body, shadowColor, innerDarken);
                body = lerp(body, shadowOverlay, shadowMask);

                // hover/pressed (now boolean)
                body += hoverOn * 0.05;
                body *= lerp(1.0, 0.86, pressedOn);

                // angled top highlight on body (small band)
                float angleRad = radians(_TopHighlightAngle);
                float cosA = cos(angleRad); float sinA = sin(angleRad);
                float2 centered = uv - 0.5;
                float2 rotated = float2(centered.x * cosA - centered.y * sinA, centered.x * sinA + centered.y * cosA);
                float ruvY = rotated.y + 0.5;
                float topStripStart = 1.0 - clamp(_TopHighlightStrength * 2.5 + 0.04, 0.03, 0.3);
                float topBand = smoothstep(topStripStart, 1.0 - 0.01, ruvY) * maskInner;
                body = lerp(body, body + _TopHighlightColor.rgb * 0.14, topBand * _TopHighlightStrength * (1.0 - pressedOn));

                // apply accent/rarity to main body only
                float bodyAccentMask = maskInner * smoothstep(0.78, 0.98, uv.y) * _AccentAmount;
                body = lerp(body, lerp(body, _AccentColor.rgb, 0.5), bodyAccentMask);

                // border shading using SDF normal
                float2 grad = float2(ddx(sdf), ddy(sdf));
                float gradLen = length(grad) + 1e-6;
                float2 n = grad / gradLen;
                float topFactor = smoothstep(0.15, 0.9, n.y);
                float bottomFactor = smoothstep(0.15, 0.9, -n.y);

                float3 borderBase = _BorderColor.rgb;
                borderBase += _TopHighlightColor.rgb * (0.18 * topFactor * _TopHighlightStrength);
                borderBase = lerp(borderBase, borderBase * 0.82, bottomFactor * saturate((-sdf) / max(borderPx, 1.0)));

                // rarity tint on border: static base with a clockwise blackening pulse
                float time = _Time.y;
                float phaseRad = radians(_RarityPulsePhase);
                float rot = - (time * 2.0 * UNITY_PI / max(_RarityPulsePeriod, 0.0001)) + phaseRad;
                float ang = atan2(p.y, p.x);
                float angDiff = abs(atan2(sin(ang - rot), cos(ang - rot)));
                float halfWidth = _RarityPulseWidth * UNITY_PI;
                float edgeW = max(0.001, halfWidth * 0.35);
                float band = 1.0 - smoothstep(halfWidth - edgeW, halfWidth + edgeW, angDiff);
                float ampOsc = 0.5 + 0.5 * sin(time * 2.0 * UNITY_PI / max(_RarityPulsePeriod, 0.0001));
                float pulseAmp = band * _RarityPulseStrength * ampOsc * borderMask;
                // selection no longer boosts the pulse amplitude to avoid extra darkening on select
                float pulseNorm = saturate(pulseAmp);

                // Separate rarity contribution from base border so the pulse darkens only the rarity color
                float baseRarity = _RarityAmount * borderMask;
                float3 rarityPart = _RarityColor.rgb * baseRarity;
                float3 nonRarityPart = borderBase * (1.0 - baseRarity);
                float3 rarityPartDark = rarityPart * (1.0 - pulseNorm);
                borderBase = nonRarityPart + rarityPartDark;

                // compose body and border
                float3 bodyWithVignette = lerp(body, body * (1.0 - _VignetteStrength), smoothstep(0.4, 0.75, length((uv - 0.5))) * maskInner);

                // Icon compositing (centered, scalable). Icon is applied only to main body (maskInner).
                float3 bodyWithIcon = bodyWithVignette;
                if (_IconAmount > 0.001) {
                    float iconScale = max(0.001, _IconScale);
                    float2 iconOffset = _IconOffset.xy;
                    float2 iconUV = (uv - 0.5 - iconOffset) / iconScale + 0.5;
                    fixed4 iconSample = tex2D(_IconTex, iconUV);

                    // Base icon alpha, restricted to inside the main body
                    float iconAlpha = iconSample.a * _IconAmount * maskInner;

                    // Icon responds to hover/pressed/disabled as booleans
                    iconAlpha *= (1.0 + hoverOn * 0.08);
                    iconAlpha *= lerp(1.0, 0.82, pressedOn);
                    iconAlpha *= lerp(1.0, 0.5, disabledOn);

                    // Icon colour with tint
                    float3 iconCol = _IconTint.rgb * iconSample.rgb;
                    // Slight brighten on hover
                    iconCol += hoverOn * 0.08;
                    // Darken on press
                    iconCol *= lerp(1.0, 0.82, pressedOn);
                    // Desaturate when disabled
                    float iconLum = dot(iconCol, float3(0.299,0.587,0.114));
                    float3 iconGray = float3(iconLum, iconLum, iconLum);
                    iconCol = lerp(iconCol, iconGray * 0.9, disabledOn);

                    // If selected, apply an inner-shadow-like tint to the icon using the selected shadow color
                    if (selectedOn > 0.5) {
                        float selOverlay = saturate(shadowMask * maskInner * _SelectedInnerShadowStrength);
                        float3 rarityShadow = shadowColor * 0.85;
                        iconCol = lerp(iconCol, rarityShadow, selOverlay);
                    }

                    bodyWithIcon = lerp(bodyWithVignette, iconCol, saturate(iconAlpha));
                }

                float3 finalColor = lerp(bodyWithIcon, borderBase, borderMask * 0.98);

                // inner solid border overlay
                float innerBorderPx = _InnerBorderWidth * minDim;
                float maskInner2 = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2);
                finalColor = lerp(finalColor, _InnerBorderColor.rgb, innerBorderMask);

                // selection applies a tinted inner-shadow when active; pulse amplitude not changed by selection

                // disabled desaturation (boolean)
                if (disabledOn > 0.5) {
                    float lum = dot(finalColor, float3(0.299,0.587,0.114));
                    float3 gray = float3(lum,lum,lum);
                    finalColor = lerp(finalColor, gray * 0.9, 1.0);
                }

                // sample sprite alpha and return
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;
                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
