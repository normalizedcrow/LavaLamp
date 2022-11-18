#ifndef LAVA_LAMP_LIGHTING_HELPER_INCLUDED
#define LAVA_LAMP_LIGHTING_HELPER_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "UnityGlobalIllumination.cginc"

float3 ClampBrightness(float3 color, float maxLuminance)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    return color / max(1.0, luminance / maxLuminance);
}

//GGX formulas via: http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html

//Schlick approximation
inline float Fresnel(float vDotH, float reflectiveness)
{
    return saturate(lerp(reflectiveness, 1.0, pow(1.0 - vDotH, 5.0)));
}

//isotropic Trowbridge-Reitz distribution
inline float GGXDistribution(float nDotH, float roughness)
{
    float a2 = roughness * roughness;
    float denom = (nDotH * nDotH * (a2 - 1.0)) + 1.0;
    denom = UNITY_PI * denom * denom;
    return max(0.0, a2 / denom);
}

//Schlick-Beckmann approximation remapped for GGX and pre-combined with the BRDF denominator
inline float GeometrySmithGGX(float nDotL, float nDotV, float roughness)
{
    float incoming = (nDotL * (2.0 - roughness)) + roughness;
    float outgoing = (nDotV * (2.0 - roughness)) + roughness;

    return saturate(1.0 / (incoming * outgoing));
}

inline float GGXCookTorrance(float3 surfaceNormal, float3 viewDirection, float3 lightDirection, float roughness, float reflectiveness)
{
    float3 halfNormal = normalize(-viewDirection + lightDirection);

    float nDotL = saturate(dot(lightDirection, surfaceNormal));
    float nDotV = saturate(-dot(viewDirection, surfaceNormal));

    float nDotH = saturate(dot(surfaceNormal, halfNormal));
    float vDotH = saturate(-dot(viewDirection, halfNormal));

    float specular = GGXDistribution(nDotH, roughness)
                   * GeometrySmithGGX(nDotL, nDotV, roughness)
                   * Fresnel(vDotH, reflectiveness);

    return max(0.0, specular) * nDotL;
}

//get direct light from the curent forward pass
float3 GetDirectSpecularLighting(float3 worldPos, float3 normal, float3 viewDirection, float roughness, float reflectiveness, float attenuation)
{
    //when _WorldSpaceLightPos0.w == 0 then _WorldSpaceLightPos0.xyz is a directional light direction
    float3 lightDirection = (_WorldSpaceLightPos0.w < 0.5) ? normalize(_WorldSpaceLightPos0.xyz) : normalize(_WorldSpaceLightPos0.xyz - worldPos.xyz);

    //multiply by PI because Unity dosn't use the 1/PI normalization term for diffuse, so the specular brightness is instead increased to match it
    return _LightColor0.rgb * attenuation * GGXCookTorrance(normal, viewDirection, lightDirection, roughness, reflectiveness) * UNITY_PI;
}

//sample unity's built in reflection probes
float3 SampleBuiltInReflectionProbes(float3 worldPos, float3 normal, float3 viewDirection, float roughness)
{
    UnityGIInput d;
    d.worldPos = worldPos;
    d.worldViewDir = -viewDirection;

    //unity internal values
    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
    d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
    d.boxMax[0] = unity_SpecCube0_BoxMax;
    d.probePosition[0] = unity_SpecCube0_ProbePosition;
    d.boxMax[1] = unity_SpecCube1_BoxMax;
    d.boxMin[1] = unity_SpecCube1_BoxMin;
    d.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif

    float smoothness = 1.0 - RoughnessToPerceptualRoughness(roughness);
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, d.worldViewDir, normal, 1.0);

    return UnityGI_IndirectSpecular(d, 1.0, g);
}

//sample a specified reflection probe
float3 SampleReflectionProbe(TextureCube reflectionProbe, SamplerState reflectionProbeSampler, float3 normal, float3 viewDirection, float roughness)
{
    float3 reflectionVector = reflect(viewDirection, normal);

    //match the behavior in Unity_GlossyEnvironment
    float perceptualRoughness = RoughnessToPerceptualRoughness(roughness);
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
    float mipLevel = perceptualRoughnessToMipmapLevel(perceptualRoughness);
    
    return reflectionProbe.SampleLevel(reflectionProbeSampler, reflectionVector, mipLevel).rgb;
}

//gets the reflection strength for cubemaps (reduce fresnel strength on rough materials)
inline float ReflectionProbeFresnel(float3 surfaceNormal, float3 viewDirection, float reflectiveness, float roughness)
{
    float vDotN = saturate(-dot(surfaceNormal, viewDirection));
    return saturate(lerp(reflectiveness, 1.0, pow(1.0 - vDotN, 5.0) * (1.0 - roughness)));
}

#endif