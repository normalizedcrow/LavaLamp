Shader "Hidden/normalizedcrow/Mesh Processing/Weld Mesh"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma vertex WeldingVertexShader
            #pragma geometry WeldingGeometryShader
            #pragma fragment WeldingPixelShader

            #pragma target 5.0

            #include "UnityCG.cginc"
            #include "Helpers/MeshBakingHelper.hlsl"

            RWStructuredBuffer<float3> _WeldedVertexPositions : register(u1);
            RWStructuredBuffer<uint> _VertexCounter : register(u2);

            struct VertexInput
            {
                float3 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct GeometryInput
            {
                float3 bakePosition : TEXCOORD0;
            };

            GeometryInput WeldingVertexShader(VertexInput vertex)
            {
                GeometryInput output;

                //get the position of the verts relative to the root position
                output.bakePosition = GetBakePosition(vertex.position);
                output.bakePosition = ExpandBakePosition(output.bakePosition, vertex.normal, vertex.uv);

                return output;
            }

            [maxvertexcount(1)]
            void WeldingGeometryShader(triangle GeometryInput input[3]) //don't actually output any verts
            {
                //add these new verts to the counter and get the number already added
                uint triangleOffset;
                InterlockedAdd(_VertexCounter[0], 3, triangleOffset);

                //actually put all 3 vertices into the buffer
                for (uint currentVert = 0; currentVert < 3; currentVert++)
                {
                    _WeldedVertexPositions[triangleOffset + currentVert] = input[currentVert].bakePosition;
                }
            }

            void WeldingPixelShader() { } //just here to make the compiler happy

            ENDHLSL
        }
    }
}
