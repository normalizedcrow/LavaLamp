#ifndef RAYMARCHING_HELPER_INCLUDED
#define RAYMARCHING_HELPER_INCLUDED

#include "UnityCG.cginc"

Texture3D _SDFTex;
SamplerState sampler_SDFTex;

float4x4 _ObjToSDF;
float4x4 _SDFToObj;

float SampleSDF(float3 uvw)
{
    return _SDFTex.SampleLevel(sampler_SDFTex, uvw, 0.0).r;
}

void GetSDFTraceParameters(float3 cameraPos, float3 worldPos, out float3 startPosition, out float3 marchDirection, out float marchDistance)
{
    //convert the camera pos into the SDF coordinate space
    startPosition = mul(unity_WorldToObject, float4(cameraPos, 1.0)).xyz;
    startPosition = mul(_ObjToSDF, float4(startPosition, 1.0)).xyz;

    //convert the view direction into the SDF coordinate space
    marchDirection = mul(unity_WorldToObject, float4(worldPos - cameraPos, 0.0)).xyz;
    marchDirection = mul(_ObjToSDF, float4(normalize(marchDirection), 0.0)).xyz;

    //find the distance to enter the box and how far the march in the box will be
    float3 distanceToHitLowerSides = -startPosition / marchDirection;
    float3 distanceToHitUpperSides = (-startPosition + 1.0) / marchDirection;

    float3 minDistancePerAxis = min(distanceToHitLowerSides, distanceToHitUpperSides);
    float3 maxDistancePerAxis = max(distanceToHitLowerSides, distanceToHitUpperSides);

    float entryDistance = max(max(0.0, minDistancePerAxis.x), max(minDistancePerAxis.y, minDistancePerAxis.z));
    marchDistance = min(maxDistancePerAxis.x, min(maxDistancePerAxis.y, maxDistancePerAxis.z)) - entryDistance;
    
    //move the start position to the start of the box
    startPosition += marchDirection * entryDistance;
}

float RayMarchSDF(float3 startPosition, float3 marchDirection, float maxDistance, float surfaceDilation = 0.0, bool invert = false, uint steps = 32)
{
    float finalDistance = 0.0;

    for (uint i = 0; i < steps; i++)
    {
        //if we have exited the SDF
        if (finalDistance > maxDistance + 0.00001)
        {
            return -1.0; //return an invalid distance
        }
        
        ///sample the SDF
        float3 uvw = startPosition + marchDirection * finalDistance;
        float signedDistance = SampleSDF(uvw);
        signedDistance = invert ? -signedDistance : signedDistance;
        signedDistance -= surfaceDilation;
        
        //if we are close enough to the surface
        if (signedDistance < 0.001)
        {
            return finalDistance;
        }

        //march by the SDF distance
        finalDistance += signedDistance;
    }

    //if we ran out of steps, just return how far we got 
    return finalDistance;
}

float3 GetSDFNormal(float3 uvw)
{
    uint3 sdfDimensions;
    _SDFTex.GetDimensions(sdfDimensions.x, sdfDimensions.y, sdfDimensions.z);

    //get the derivative of the SDF by getting the difference in distances at a 1 pixel offset along each axis
    float3 pixelOffst = 1.0 / sdfDimensions;
    float3 normal = float3(SampleSDF(uvw + float3(pixelOffst.x, 0.0, 0.0)),
                           SampleSDF(uvw + float3(0.0, pixelOffst.y, 0.0)),
                           SampleSDF(uvw + float3(0.0, 0.0, pixelOffst.z)))
                  - float3(SampleSDF(uvw - float3(pixelOffst.x, 0.0, 0.0)),
                           SampleSDF(uvw - float3(0.0, pixelOffst.y, 0.0)),
                           SampleSDF(uvw - float3(0.0, 0.0, pixelOffst.z)));

    return normalize(normal);
}

#endif