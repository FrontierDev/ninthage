Shader "NinthAge/UI/ArcaneSlate/ProgressBar" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0.1254902,0.1568627,0.2,1)
        _MainColor ("Main Color", Color) = (0.08,0.35,0.2,1)
        _FlashColor ("Flash Color", Color) = (0.7,1,0.8,1)
        _FlashWidth ("Flash Width (px)", Range(0,32)) = 4.0
        _BorderWidth ("Border Width (px)", Range(0,16)) = 1.0
        _BevelAmount ("Bevel Amount (px)", Range(0,16)) = 2.0
        _FillAmount ("Fill Amount", Range(0,1)) = 0.5
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,1)
        _InnerShadowStrength ("Inner Shadow Strength", Range(0,1)) = 0.3
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
            float _BorderWidth;
            float _BevelAmount;
            float _FillAmount;
            float4 _InnerShadowColor;
            float _InnerShadowStrength;
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

            static float sdRoundRect(float2 p, float2 b, float r) {
                float2 q = abs(p) - b;
                float outside = length(max(q, 0.0));
                float inside = min(max(q.x, q.y), 0.0);
                return outside + inside - r;
            }

            // Simple rectangle (sharp corners)
            static float sdRect(float2 p, float2 b) {
                float2 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            // Beveled rectangle: chamfered edges
            static float sdBeveledRect(float2 p, float2 b, float bevel) {
                float2 q = abs(p) - b + bevel;
                float d = length(max(q, 0.0)) - bevel;
                return d;
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

                // Distance to beveled rectangle
                float bevelPx = max(0.0, _BevelAmount);
                float sdf = sdBeveledRect(p, b, bevelPx);
                float aa = max(1.0, fwidth(sdf));

                // Border masks
                float borderPx = max(0.0, _BorderWidth <= 1.0 ? _BorderWidth * 100.0 : _BorderWidth);
                float maskOuter = smoothstep(aa, -aa, sdf);
                float maskInner = smoothstep(aa, -aa, sdf + borderPx);
                float borderMask = saturate(maskOuter - maskInner);

                // Derive colors from main color
                float3 fillColorBottom = _MainColor.rgb * 0.7;  // 30% darker
                float3 fillColorTop = lerp(_MainColor.rgb, float3(1.0, 1.0, 1.0), 0.2);  // 20% towards white
                float3 borderColor = _MainColor.rgb * 0.5;  // 50% darker

                // Determine if pixel is in filled region (left to right based on _FillAmount)
                float fillThreshold = (uv.x - 0.5) * rectSizePx.x;
                float fillEdge = (_FillAmount - 0.5) * rectSizePx.x;
                float isFilled = smoothstep(-1.0, 1.0, fillEdge - fillThreshold);

                // Fill gradient (horizontal): left to right
                float3 fillGradient = lerp(fillColorBottom, fillColorTop, uv.x);
                
                // Flash at fill edge: narrow bright band
                float distToEdge = abs(fillThreshold - fillEdge);
                float flashMask = smoothstep(_FlashWidth + 1.0, _FlashWidth - 1.0, distToEdge);
                flashMask *= isFilled; // only show flash where fill ends

                // Base colors: filled or background
                float3 baseColor = lerp(_BackgroundColor.rgb, fillGradient, isFilled);
                
                // Apply flash
                float3 withFlash = lerp(baseColor, _FlashColor.rgb, flashMask);

                // Inner shadow: darkens from edges inward
                float dInside = max(-sdf, 0.0);
                float innerShadowPx = _InnerShadowStrength * minDim * 0.2;
                float shadowMask = 1.0 - smoothstep(0.0, innerShadowPx + aa, dInside);
                float3 withShadow = withFlash * (1.0 - shadowMask * 0.6);

                // Final color: no glow
                float3 finalColor = lerp(withShadow, borderColor, borderMask);

                // Final alpha: bar only
                fixed4 tex = tex2D(_MainTex, i.uv);
                float outAlpha = _Opacity * maskOuter * tex.a * i.color.a;

                return float4(finalColor, outAlpha);
            }

            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
