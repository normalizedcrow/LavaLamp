Shader "Hidden/normalizedcrow/Mesh Processing/Visualization/Bind Mask Visualization"
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
            #include "../Helpers/MeshBakingHelper.hlsl"

            static const uint cMaxMaskColors = 16;

            float4x4 _VisualizationMatrix;

            uint _MaskColorCount;
            float3 _InvalidMaskColor;
            float3 _MaskColors[cMaxMaskColors];

            Texture2D<float4> _MaskTexture;
            SamplerState sampler_MaskTexture;

            struct VertexInput
            {
                float3 position : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct PixelInput
            {
                float4 position : SV_POSITION;
                nointerpolation float3 color : TEXCOORD0; //use a flat color across the triangle
            };

            PixelInput VisualizationVertexShader(VertexInput vertex)
            {
                float3 sampledMaskColor = _MaskTexture.SampleLevel(sampler_MaskTexture, vertex.uv, 0);

                //start with the invalid color
                bool isInvalid = true;
                float3 closestMaskColor = _InvalidMaskColor;
                float closestMaskDistance = length(sampledMaskColor - _InvalidMaskColor);

                //iterate through all the mask colors and find which one is the closest to the sampled color
                for (uint i = 0; i < _MaskColorCount; i++)
                {
                    float maskDistance = length(sampledMaskColor - _MaskColors[i]);

                    if (maskDistance < closestMaskDistance)
                    {
                        isInvalid = false;
                        closestMaskDistance = maskDistance;
                        closestMaskColor = _MaskColors[i];
                    }
                }

                //render the vertex at the bake position
                float3 bindPosition = GetBakePosition(vertex.position);
                float3 worldPos = mul(_VisualizationMatrix, float4(bindPosition, 1.0)).xyz;
                float4 clipPos = UnityWorldToClipPos(worldPos);

                PixelInput output;
                output.position = isInvalid ? asfloat(~0) : clipPos; //discard vertex if it is invalid by setting it to NaN
                output.color = closestMaskColor;

                return output;
            }

            float4 VisualizationPixelShader(PixelInput input) : SV_Target
            {
                return float4(input.color, 1.0);
            }

            ENDHLSL
        }
    }
}

