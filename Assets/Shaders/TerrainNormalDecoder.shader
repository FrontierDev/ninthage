// Hidden/TerrainNormalDecoder
//
// Decodes a Unity NormalMap-imported texture from its platform-specific
// compressed format (BC5 on PC: X in R, Y in G; or DXT5nm on older/mobile:
// X in A, Y in G) to a standard tangent-space normal map with XYZ in
// RGB [0, 1]. The output PNG can be re-imported with
// TextureImporterType.NormalMap and will produce correct normals at runtime.
//
// Used by Texture2DArrayBuilder when building normal-map arrays.

Shader "Hidden/TerrainNormalDecoder"
{
    Properties
    {
        _MainTex ("Source Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 raw = tex2D(_MainTex, i.uv);

                // Explicit UnpackNormalmapRGorAG logic — no UNITY_NO_DXT5nm conditional.
                // Works for both GPU formats Unity uses for normal maps:
                //   DXT5nm  : A=X, G=Y, R≈1.0  → R * A = 1 * X = X
                //   BC5     : R=X, G=Y, A=1.0   → R * A = X * 1 = X  (no-op)
                raw.x *= raw.w;
                float3 n;
                n.xy = raw.xy * 2.0 - 1.0;
                n.z  = sqrt(max(0.0, 1.0 - dot(n.xy, n.xy)));

                // Remap XYZ from [-1,1] to [0,1]. The PNG is saved with these
                // values and reimported as NormalMap, which re-encodes correctly.
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
}
