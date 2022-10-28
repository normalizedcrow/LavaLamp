#ifndef MESH_BAKING_HELPER_INCLUDED
#define MESH_BAKING_HELPER_INCLUDED

#include "UnityCG.cginc"

float3 _BakeMeshRootPosition;

float _MeshExpansion;
Texture2D _MeshExpansionTexture;
SamplerState sampler_MeshExpansionTexture;

//get the position relative to the root
float3 GetBakePosition(float3 position)
{
    return mul(unity_ObjectToWorld, float4(position, 1.0)).xyz - _BakeMeshRootPosition;
}

//expand the mesh by its normals by an amount scaled by a heightfield
float3 ExpandBakePosition(float3 bakePosition, float3 normal, float2 uv)
{
    normal = normalize(mul(float4(normal, 0.0), unity_WorldToObject).xyz);
    bakePosition += normal * _MeshExpansion * _MeshExpansionTexture.SampleLevel(sampler_MeshExpansionTexture, uv, 0).r;
    
    return bakePosition;
}

#endif