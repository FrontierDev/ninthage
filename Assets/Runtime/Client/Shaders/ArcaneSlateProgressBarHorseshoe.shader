Shader "NinthAge/UI/ArcaneSlate/ProgressBarHorseshoe" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0.1254902,0.1568627,0.2,1)
        _MainColor ("Main Color", Color) = (0.38,0.74,0.54,1)
        _FlashColor ("Flash Color", Color) = (1,0.95,0.7,1)
        _FlashWidth ("Flash Width (px)", Range(0,32)) = 4.0
        _BorderColor ("Border Color", Color) = (0.22745098,0.2745098,0.3372549,1)
        _BorderWidth ("Border Width (px)", Range(0,16)) = 1.5
        _ArcRadius ("Arc Radius (px)", Range(4,512)) = 60.0
        _ArcThickness ("Arc Thickness (px)", Range(2,100)) = 14.0
        _FillAmount ("Fill Amount", Range(0,1)) = 0.5
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Strength", Range(0,1)) = 0.3
        _InnerBorderColor ("Inner Border Color", Color) = (1,1,1,1)
        _InnerBorderWidth ("Inner Border Width (px)", Range(0,8)) = 1.0
        _InnerBorderStrength ("Inner Border Strength", Range(0,1)) = 0.25
        _SelectedColor ("Selected Color", Color) = (1,1,0,1)
        _SelectedAmount ("Selected Amount", Range(0,1)) = 0.0
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

            #define ARC_PI 3.14159265
            #define ARC_TWO_PI 6.28318530

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BackgroundColor;
            float4 _MainColor;
            float4 _FlashColor;
            float _FlashWidth;
            float4 _BorderColor;
            float _BorderWidth;
            float _ArcRadius;
            float _ArcThickness;
            float _FillAmount;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
            float4 _InnerBorderColor;
            float _InnerBorderWidth;
            float _InnerBorderStrength;
            float4 _SelectedColor;
            float _SelectedAmount;
            float _Opacity;
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

            // SDF for a 270-degree arc ring with rounded end caps.
            // p          – pixel position from arc center
            // radius     – ring centreline radius
            // halfThick  – half of ring thickness
            // startAng   – arc start angle (radians)
            // span       – arc angular span (radians, positive = CCW)
            // rel (out)  – clockwise angular progress from start [0, 2π)
            static float sdArcRing(float2 p, float radius, float halfThick,
                                   float startAng, float span, out float rel) {
                float angle = atan2(p.y, p.x);
                float dist  = length(p);

                // Clockwise progress from start, wrapped to [0, 2π)
                rel = startAng - angle;
                rel = rel - floor(rel / ARC_TWO_PI) * ARC_TWO_PI;

                float onTrack = step(rel, span); // 1 when inside the arc

                // Ring distance (for on-arc points)
                float dRing = abs(dist - radius);

                // End-cap centres (for points in the gap)
                float2 capStart = radius * float2(cos(startAng), sin(startAng));
                float endAng    = startAng - span;
                float2 capEnd   = radius * float2(cos(endAng), sin(endAng));
                float dCaps     = min(length(p - capStart), length(p - capEnd));

                // Blend smoothly: on-arc → ring distance, gap → nearest cap
                float d = lerp(dCaps, dRing, onTrack);
                return d - halfThick;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // ---- element size in pixels ----
                float2 rectSizePx;
                if (_RectSize.x > 1.0 && _RectSize.y > 1.0) {
                    rectSizePx = _RectSize.xy;
                } else {
                    float2 gdx = ddx(uv);
                    float2 gdy = ddy(uv);
                    float det    = gdx.x * gdy.y - gdx.y * gdy.x;
                    float invDet = 1.0 / (abs(det) + 1e-6);
                    float wPx    = length(float2( gdy.y, -gdx.y)) * invDet;
                    float hPx    = length(float2(-gdy.x,  gdx.x)) * invDet;
                    rectSizePx   = float2(max(wPx, 1.0), max(hPx, 1.0));
                }

                float2 p = (uv - 0.5) * rectSizePx; // pixel coords from centre

                // ---- arc configuration ----
                // 270° C-shape, gap at bottom, 0 % at bottom-left
                float startAng = -0.75 * ARC_PI;   // −135 °
                float arcSpan  =  1.5  * ARC_PI;   //  270 °

                float radiusPx = max(4.0, _ArcRadius);
                float thickPx  = max(2.0, _ArcThickness);
                float halfThick = thickPx * 0.5;

                // ---- track SDF (full 270° ring with round caps) ----
                float rel;
                float sdf = sdArcRing(p, radiusPx, halfThick, startAng, arcSpan, rel);
                float aa  = max(1.0, fwidth(sdf));

                // ---- border masks ----
                float borderPx  = max(0.0, _BorderWidth <= 1.0
                                      ? _BorderWidth * 100.0 : _BorderWidth);
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // ---- fill determination ----
                float onTrack  = step(rel, arcSpan);
                float fillSpan = _FillAmount * arcSpan;
                float aaAngle  = fwidth(rel) * 2.0;
                float isFilled = smoothstep(aaAngle, -aaAngle, rel - fillSpan) * onTrack;

                // ---- fill gradient (vertical: top bright, bottom dark) ----
                float gradT = pow(saturate(uv.y), 2.0);
                float3 fillColorBottom = _MainColor.rgb * 0.72;
                float3 fillColorTop    = saturate(_MainColor.rgb * 1.5);
                float3 fillGradient    = lerp(fillColorBottom, fillColorTop, gradT);

                // ---- flash at fill leading edge ----
                float arcLenToEdge = abs(rel - fillSpan) * radiusPx;
                float flashMask = smoothstep(_FlashWidth + 1.0, _FlashWidth - 1.0,
                                             arcLenToEdge) * isFilled * onTrack;

                // ---- compose fill / background / flash ----
                float3 baseColor = lerp(_BackgroundColor.rgb, fillGradient, isFilled);
                float3 withFlash = lerp(baseColor, _FlashColor.rgb, flashMask);

                // ---- inner shadow (edge darkening) ----
                float dInside       = max(-sdf, 0.0);
                float innerShadowPx = _InnerShadowStrength * thickPx * 0.4;
                float shadowMask    = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                float3 withShadow   = withFlash * (1.0 - shadowMask * 0.6);

                // ---- inner border (fill region only) ----
                float innerBorderPx = max(0.0, _InnerBorderWidth <= 1.0
                                         ? _InnerBorderWidth * 100.0 : _InnerBorderWidth);
                float maskInner2    = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2) * isFilled;

                // ---- final composition ----
                float3 finalBorderColor = lerp(_BorderColor.rgb, _SelectedColor.rgb,
                                               _SelectedAmount);
                float3 finalColor = lerp(withShadow, finalBorderColor, borderMask);
                finalColor = lerp(finalColor, _InnerBorderColor.rgb,
                                  innerBorderMask * _InnerBorderStrength);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
