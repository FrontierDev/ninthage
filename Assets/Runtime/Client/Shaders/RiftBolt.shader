Shader "NinthAge/VFX/RiftBolt"
{
    // No properties block — colour is driven entirely by LineRenderer vertex colours.

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "RiftBolt"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                // When LineRenderer.useWorldSpace = true Unity builds the mesh with an
                // identity model matrix, so positionOS is already in world space.
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Use TransformWorldToHClip because the LineRenderer with useWorldSpace=true
                // renders with an identity transform, so positionOS == positionWS.
                OUT.positionCS = TransformWorldToHClip(IN.positionOS.xyz);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
