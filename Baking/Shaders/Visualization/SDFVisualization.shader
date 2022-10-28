Shader "Hidden/normalizedcrow/Mesh Processing/Visualization/SDF Visualization"
{
    SubShader
    {
        Cull Front

        Pass
        {
            HLSLPROGRAM

            #pragma vertex VisualizationVertexShader
            #pragma fragment VisualizationPixelShader

            #include "UnityCG.cginc"
			#include "../Helpers/RaymarchingHelper.hlsl"

			float _Dilation;
			bool _Invert;

            struct VertexInput
            {
                float3 position : POSITION;
            };

            struct PixelInput
            {
                float4 clipPos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
            };

			struct PixelOutput
			{
				float4 color : SV_Target;
				float depth : SV_Depth; //need to manually write out depth
			};

			PixelInput VisualizationVertexShader(VertexInput vertex)
            {
				PixelInput output;

				output.worldPos = mul(unity_ObjectToWorld, float4(vertex.position, 1.0)).xyz;
				output.clipPos = UnityWorldToClipPos(output.worldPos);

                return output;
            }

			PixelOutput VisualizationPixelShader(PixelInput input)
            {
				//do the raymarch
				float3 marchStart;
				float3 marchDirection;
				float maxDistance;
				GetSDFTraceParameters(_WorldSpaceCameraPos, input.worldPos, marchStart, marchDirection, maxDistance);

				float distance = RayMarchSDF(marchStart, marchDirection, maxDistance, _Dilation, _Invert, 1024);

				//if there was no hit don't draw anything
				if (distance <= 0.0 || distance >= maxDistance)
				{
					discard;
				}
				
				//get the normal of the SDF at the hit point
				float3 uvw = marchStart + marchDirection * distance;
				float3 normal = normalize(mul(float4(GetSDFNormal(uvw), 0.0), unity_WorldToObject).xyz);
				normal = _Invert ? -normal : normal;

				//convert back from SDF space to clip space
				float3 position = mul(_SDFToObj, float4(uvw, 1.0)).xyz;
				float4 clipPos = UnityObjectToClipPos(position);

				PixelOutput o;

				o.color = float4((normal * 0.5) + 0.5, 1.0); //remap normal into 0-1 range
				o.depth = clipPos.z / clipPos.w; //get the projected depth

				return o;
            }

            ENDHLSL
        }
    }
}
