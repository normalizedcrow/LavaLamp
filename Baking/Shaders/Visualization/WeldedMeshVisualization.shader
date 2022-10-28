Shader "Hidden/normalizedcrow/Mesh Processing/Visualization/Welded Mesh Visualization"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma vertex VisualizationVertexShader
            #pragma fragment VisualizationPixelShader

            #pragma target 5.0

            #include "UnityCG.cginc"

            StructuredBuffer<float3> _WeldedVertexPositions;
            float4x4 _VisualizationMatrix;

            struct PixelInput
            {
                float4 position : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            PixelInput VisualizationVertexShader(uint id : SV_VertexID)
            {
                PixelInput output;

                float3 position = _WeldedVertexPositions[id];
                output.worldPos = mul(_VisualizationMatrix, float4(position, 1.0)).xyz;
                output.position = UnityWorldToClipPos(output.worldPos);

                return output;
            }

            float4 VisualizationPixelShader(PixelInput input) : SV_Target
            {
                float3 normal = normalize(cross(ddy(input.worldPos), ddx(input.worldPos))); //get the triangle normal
                return float4((normal * 0.5) + 0.5, 1.0); //remap normal into the 0-1 range
            }

            ENDHLSL
        }
    }
}
