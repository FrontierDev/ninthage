// Hidden utility shader used by TerrainAutoPainterHelper to blit a single
// slice from a Texture2DArray into a flat RenderTexture.
//
// Using a graphics Blit (rather than a compute shader load) means the hardware
// applies the standard sRGB decode on Sample and the sRGB encode on RT write,
// giving correct colour preservation on all platforms (D3D11/12, Vulkan, Metal).

Shader "Hidden/TerrainArraySliceCopy"
{
    Properties
    {
        _SourceArray ("Source Array", 2DArray) = "" {}
        _SliceIndex  ("Slice Index", Float) = 0
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

            UNITY_DECLARE_TEX2DARRAY(_SourceArray);
            float _SliceIndex;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            // appdata_img gives us vertex + texcoord, already UV-flipped
            // correctly for the current platform by the Blit quad mesh.
            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Sample (not load) — hardware applies sRGB decode automatically
                // when the source array is an sRGB texture.
                return UNITY_SAMPLE_TEX2DARRAY(_SourceArray, float3(i.uv, _SliceIndex));
            }
            ENDCG
        }
    }
}
