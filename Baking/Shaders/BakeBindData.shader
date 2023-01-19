Shader "Hidden/normalizedcrow/Mesh Processing/Bake Bind Data"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma vertex BakingVertexShader
            #pragma fragment BakingPixelShader

            #pragma target 5.0

            #include "UnityCG.cginc"
            #include "Helpers/MeshBakingHelper.hlsl"

            static const uint cMaxMaskColors = 16;
            
            uint _MaskColorCount;
            float3 _InvalidMaskColor;
            float3 _MaskColors[cMaxMaskColors];

            Texture2D<float4> _MaskTexture;
            SamplerState sampler_MaskTexture;

            RWBuffer<float4> _PositionsOutputBuffer : register(u1);
            RWBuffer<float4> _NormalsOutputBuffer : register(u2);
            RWBuffer<float4> _TangentsOutputBuffer : register(u3);

            struct VertexInput
            {
                float3 position : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float3 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            float4 BakingVertexShader(VertexInput vertex) : SV_Position
            {
                float3 sampledMaskColor = _MaskTexture.SampleLevel(sampler_MaskTexture, vertex.uv, 0);

                //start with the invalid color and an invalid mask index
                uint closestMask = 1000;
                float closestMaskDistance = length(sampledMaskColor - _InvalidMaskColor);

                //iterate through all the mask colors and find which one is the closest to the sampled color
                for (uint i = 0; i < _MaskColorCount; i++)
                {
                    float maskDistance = length(sampledMaskColor - _MaskColors[i]);

                    if (maskDistance < closestMaskDistance)
                    {
                        closestMaskDistance = maskDistance;
                        closestMask = i;
                    }
                }

                //get the position relative to the root pos and the world space tangent frame
                float3 bindPosition = GetBakePosition(vertex.position);
                float3 bindNormal = normalize(mul(float4(vertex.normal, 0.0), unity_WorldToObject).xyz);
                float3 bindTangent = normalize(mul(unity_ObjectToWorld, float4(vertex.tangent.xyz, 0.0)).xyz);

                //write out all the values to buffers
                _PositionsOutputBuffer[vertex.id] = float4(bindPosition, closestMask);
                _NormalsOutputBuffer[vertex.id] = float4(bindNormal, 0.5);
                _TangentsOutputBuffer[vertex.id] = float4(bindTangent, vertex.tangent.w * unity_WorldTransformParams.w);

                return 0.0;
            }

            void BakingPixelShader() { } //just here to make the compiler happy

            ENDHLSL
        }
    }
}

