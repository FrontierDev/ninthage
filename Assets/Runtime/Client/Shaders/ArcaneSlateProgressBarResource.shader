Shader "NinthAge/UI/ArcaneSlate/ProgressBarResource" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0.1254902,0.1568627,0.2,1)
        _MainColor ("Main Color", Color) = (0.58,0.42,0.08,1)
        _FlashColor ("Flash Color", Color) = (1,0.95,0.7,1)
        _FlashWidth ("Flash Width (px)", Range(0,32)) = 4.0
        _BorderColor ("Border Color", Color) = (0.55,0.42,0.12,1)
        _BorderWidth ("Border Width (px)", Range(0,16)) = 1.5
        _CornerRadius ("Corner Radius (px)", Range(0,64)) = 4.0
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BackgroundColor;
            float4 _MainColor;
            float4 _FlashColor;
            float _FlashWidth;
            float4 _BorderColor;
            float _BorderWidth;
            float _CornerRadius;
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

            // Rounded rectangle SDF
            static float sdRoundRect(float2 p, float2 b, float r) {
                float2 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            static float hash(float2 q) {
                return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.uv;

                // Determine bar size
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

                float minDim = min(rectSizePx.x, rectSizePx.y);
                float2 p = (uv - 0.5) * rectSizePx;
                float2 b = rectSizePx * 0.5;

                // Corner radius in pixels
                float rPx = max(0.0, _CornerRadius <= 1.0 ? _CornerRadius * 100.0 : _CornerRadius);
                rPx = min(rPx, minDim * 0.5 - 1.0);
                float2 bInner = b - rPx;

                float sdf = sdRoundRect(p, bInner, rPx);
                float aa = max(1.0, fwidth(sdf));

                // Border masks
                float borderPx = max(0.0, _BorderWidth <= 1.0 ? _BorderWidth * 100.0 : _BorderWidth);
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // Fill gradient: natural color mid-body, dark at bottom, bright top rim
                // pow(y, 2) keeps most of bar at natural color, brightens only near top
                float gradT = pow(uv.y, 2.0);
                float3 fillColorBottom = _MainColor.rgb * 0.72;
                float3 fillColorTop    = saturate(_MainColor.rgb * 1.5); // brighten without desaturating
                float3 fillGradient = lerp(fillColorBottom, fillColorTop, gradT);

                // Filled region (left to right)
                float fillThreshold = (uv.x - 0.5) * rectSizePx.x;
                float fillEdge = (_FillAmount - 0.5) * rectSizePx.x;
                float isFilled = smoothstep(-1.0, 1.0, fillEdge - fillThreshold);

                // Flash at fill edge
                float distToEdge = abs(fillThreshold - fillEdge);
                float flashMask = smoothstep(_FlashWidth + 1.0, _FlashWidth - 1.0, distToEdge) * isFilled;

                // Base color
                float3 baseColor = lerp(_BackgroundColor.rgb, fillGradient, isFilled);
                float3 withFlash = lerp(baseColor, _FlashColor.rgb, flashMask);

                // Inner shadow
                float dInside = max(-sdf, 0.0);
                float innerShadowPx = _InnerShadowStrength * minDim * 0.2;
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                float3 withShadow = withFlash * (1.0 - shadowMask * 0.6);

                // Inner border: thin rim just inside the outer border, only on filled region
                float innerBorderPx = max(0.0, _InnerBorderWidth <= 1.0 ? _InnerBorderWidth * 100.0 : _InnerBorderWidth);
                float maskInner2 = smoothstep(aa, -aa, sdf + borderPx + innerBorderPx);
                float innerBorderMask = saturate(maskInner - maskInner2) * isFilled;

                // Compose border with selected color blend
                float3 finalBorderColor = lerp(_BorderColor.rgb, _SelectedColor.rgb, _SelectedAmount);
                float3 finalColor = lerp(withShadow, finalBorderColor, borderMask);
                finalColor = lerp(finalColor, _InnerBorderColor.rgb, innerBorderMask * _InnerBorderStrength);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
