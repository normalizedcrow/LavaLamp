#ifndef LAVA_LAMP_CORE_HELPER_INCLUDED
#define LAVA_LAMP_CORE_HELPER_INCLUDED

static const float3 cHaltonSequence[4] = { float3(0.5, 0.0, 0.333), float3(0.25, 0.0, 0.666), float3(0.75, 0.0, 0.111), float3(0.125, 0.0, 0.444) };
static const float cBlobLayerJitterScale = 0.6;
static const float cLavaRaymarchMaxSteps = 100;
static const float cLavaRaymarchMinStepLength = 0.01;
static const float cNormalEPS = 0.05;

#include "LavaLampLightingHelper.hlsl"

//Global Constants --------------------------------------------------------------------------------

float _LavaPadding;
float _LavaSmoothingFactor;
float _LavaVerticalSeparation;
float _LavaSkipChance;
float _LavaMinSize;
float _LavaMaxSize;
float _LavaSizeDistribution;
float _LavaMinSpeed;
float _LavaMaxSpeed;
float _LavaMinDriftSpeed;
float _LavaMaxDriftSpeed;
float _LavaReflectiveness;
float _LavaPerceptualRoughness;
float _LavaSoftDepthSize;
float _LavaTouchingSideBlendSize;

//Structs -----------------------------------------------------------------------------------------

struct LavaSurfaceParameters
{
    float3 position;
    float3 normal;
    float thickness;
    float reservoirFactor;
    float touchingSideFactor;
};

struct LavaLampShapeParameters
{
    float scale;
    float topReservoirHeight;
    float bottomReservoirHeight;
    float3 flowDirection;
};

struct LavaLampMaterialParameters
{
    float3 coreColor;
    float3 edgeColor;
    float colorThicknessScale;
    float3 waterHazeColor;
    float waterHazeStrength;
    float3 waterTintColor;
    float waterTintStrength;
    float3 topLightColor;
    float topLightHeight;
    float3 bottomLightColor;
    float bottomLightHeight;
    float lightFalloff;
};

//forward declare these since different shaders might choose to fill these differently
LavaLampShapeParameters GetLavaLampShapeParameters(int index);
LavaLampMaterialParameters GetLavaLampMaterialParameters(int index);

//Helper Functions --------------------------------------------------------------------------------

//via https://www.reddit.com/r/RNG/comments/jqnq20/the_wang_and_jenkins_integer_hash_functions_just/
//randomly generates a float in the 0-1 range along with a new seed
float RandomNormalized(inout uint seed)
{
    seed ^= seed >> 16;
    seed *= 0xa812d533;
    seed ^= seed >> 15;
    seed *= 0xb278e4ad;
    seed ^= seed >> 17;

    return seed / 4294967295.0f;
}

//via https://iquilezles.org/articles/smin/

//blends together two distance field surfaces with smooth c1 continuity
float LavaSmoothMin(float distanceA, float distanceB, bool restrictToOriginalRadius)
{
    //shrink the source shapes so the blended shape dosn't extend past the original bounds
    distanceA += restrictToOriginalRadius ? _LavaSmoothingFactor / 4.0 : 0.0;
    distanceB += restrictToOriginalRadius ? _LavaSmoothingFactor / 4.0 : 0.0;

    float blendFactor = max(0.0, _LavaSmoothingFactor - abs(distanceA - distanceB)) / _LavaSmoothingFactor;
    return min(distanceA, distanceB) - (_LavaSmoothingFactor * blendFactor * blendFactor / 4.0);
}

//does the same as above and also smoothly interpolates an additional value
float2 LavaSmoothMinWithValueBlend(float2 distanceAndValueA, float2 distanceAndValueB, bool restrictToOriginalRadius)
{
    //shrink the source shapes so the blended shape dosn't extend past the original bounds
    distanceAndValueA.x += restrictToOriginalRadius ? _LavaSmoothingFactor / 4.0 : 0.0;
    distanceAndValueB.x += restrictToOriginalRadius ? _LavaSmoothingFactor / 4.0 : 0.0;

    float blendFactor = max(0.0, _LavaSmoothingFactor - abs(distanceAndValueA.x - distanceAndValueB.x)) / _LavaSmoothingFactor;
    float blendedDistance = min(distanceAndValueA.x, distanceAndValueB.x) - (_LavaSmoothingFactor * blendFactor * blendFactor / 4.0);

    float valueBlend = blendFactor * blendFactor / 2.0;
    valueBlend = distanceAndValueA.x < distanceAndValueB.x ? valueBlend : 1.0 - valueBlend;

    return float2(blendedDistance, lerp(distanceAndValueA.y, distanceAndValueB.y, valueBlend));
}

void ApplyLavaFlowRotation(float3 flowDirection, inout float3 startPosition, inout float3 traceDirection)
{
    //normalize the flow direction, just make it go up if the vector is too small
    float flowLength = length(flowDirection);
    flowDirection = flowLength > 0.0001 ? flowDirection / flowLength : float3(0.0, 1.0, 0.0);

    //get a perpendicular direction, just use the x axis if the flow direction is straight up
    float3 lavaHorizontalDirection = cross(flowDirection, float3(0.0, 1.0, 0.0));
    float horizontalLength = length(lavaHorizontalDirection);
    lavaHorizontalDirection = horizontalLength > 0.0001 ? lavaHorizontalDirection / horizontalLength : float3(1.0, 0.0, 0.0);

    //get the rotation matrix for the lava flow
    float3x3 lavaToWorldTransform = float3x3(lavaHorizontalDirection, flowDirection, normalize(cross(flowDirection, lavaHorizontalDirection)));

    startPosition = mul(lavaToWorldTransform, startPosition);
    traceDirection = normalize(mul(lavaToWorldTransform, traceDirection));
}

//falloff that starts at 1.0 and converges to inverse square distance, based on http://www.cemyuksel.com/research/pointlightattenuation/
float2 GetLavaLampLightAttenuations(float height, LavaLampMaterialParameters materialParameters)
{
    float2 lightDistances = float2(height - materialParameters.bottomLightHeight, materialParameters.topLightHeight - height);
    lightDistances = max(0.0, lightDistances * materialParameters.lightFalloff); //rescale distance

    return 1.0 - (lightDistances / sqrt((lightDistances * lightDistances) + 2.0));
}

//Raymarching Functions ---------------------------------------------------------------------------

float2 GetDistanceAndRadiusOfLavaBlob(float3 blobCoord, uint layerIndex)
{
    //jitter each layer of blobs
    blobCoord += cHaltonSequence[layerIndex % 4] * cBlobLayerJitterScale;

    //Per Column Properties

    int2 columnIndex = floor(blobCoord.xz);
    uint columnSeed = columnIndex.x ^ (columnIndex.y << 16) ^ (layerIndex << 30);

    //get the blob's vertical speed
    float scrollSpeed = lerp(_LavaMinSpeed, _LavaMaxSpeed, RandomNormalized(columnSeed));
    scrollSpeed *= ((columnSeed & 1) == 0) ? 1.0 : -1.0; //make half of the blobs flow up and half flow down

    //scroll the columns vertically
    blobCoord.y -= scrollSpeed * _Time.y;
    blobCoord.y -= (columnSeed % 1024) / 1024.0; //add some extra starting variation for if the lava speed is 0
    blobCoord.y /= _LavaVerticalSeparation;

    //Per Blob Properties

    int3 blobIndex = floor(blobCoord);
    uint blobSeed = blobIndex.y ^ (blobIndex.z << 10) ^ (blobIndex.x << 20) ^ (layerIndex << 30);

    //get the blob's radius
    float blobRadius = RandomNormalized(blobSeed);

    //skip a certain percentage of blobs, just return arbitrarily far distance instead
    if (blobRadius < _LavaSkipChance)
    {
        return float2(100000000.0, 0.0);
    }

    blobRadius = saturate((blobRadius - _LavaSkipChance) / (1.0 - _LavaSkipChance)); //renormalize probability for non-skipped blobs
    blobRadius = pow(blobRadius, exp(-_LavaSizeDistribution)); //adjust the curve of the size distribution
    blobRadius = lerp(_LavaMinSize, _LavaMaxSize, blobRadius); //remap the 0-1 range to the specified min and max

    float maxBlobRadius = 0.5 - _LavaPadding; //leave some space for padding
    float minBlobRadius = min(_LavaSmoothingFactor / 2.0, maxBlobRadius); //blobs will be shrunk by (_LavaSmoothingFactor / 2.0) during blending, so compensate
    blobRadius = lerp(minBlobRadius, maxBlobRadius, blobRadius);

    //get the position of the blob
    float minDistanceFromSide = blobRadius + _LavaPadding; //can't offset the blob any closer than this to the edge of its bounding box
    float blobVerticalPosition = lerp(minDistanceFromSide, _LavaVerticalSeparation - minDistanceFromSide, RandomNormalized(blobSeed));
    float blobRadialDistance = (0.5 - minDistanceFromSide) * RandomNormalized(blobSeed);
    float blobAngularPosition = 2.0 * UNITY_PI * RandomNormalized(blobSeed);
    
    //add spiral drift
    float driftSpeed = lerp(_LavaMinDriftSpeed, _LavaMaxDriftSpeed, RandomNormalized(blobSeed)) * scrollSpeed;
    driftSpeed *= ((blobSeed & 1) == 0) ? 1.0 : -1.0; //make half of the blobs spin clockwise and half spin counterclockwise
    blobAngularPosition += driftSpeed * 2.0 * _Time.y; //multiply by 2 because the max radius of motion is 0.5

    float3 blobCenter = float3((cos(blobAngularPosition) * blobRadialDistance) + 0.5,
                               blobVerticalPosition,
                               (sin(blobAngularPosition) * blobRadialDistance) + 0.5);

    //calculate distance to the blob
    float3 blobRelativePosition = frac(blobCoord);
    blobRelativePosition.y *= _LavaVerticalSeparation;
    blobRelativePosition -= blobCenter;

    float distanceToBlobSurface = length(blobRelativePosition) - blobRadius;

    return float2(distanceToBlobSurface, blobRadius);
}

float GetDistanceToLavaSurace(float3 lavaPosition, LavaLampShapeParameters shapeParameters)
{
    //get the nearest blob on each layer and blend them together while keeping the resulting surface within the original bounds
    float distance = LavaSmoothMin(
        LavaSmoothMin(GetDistanceAndRadiusOfLavaBlob(lavaPosition, 0).x, GetDistanceAndRadiusOfLavaBlob(lavaPosition, 1).x, true),
        LavaSmoothMin(GetDistanceAndRadiusOfLavaBlob(lavaPosition, 2).x, GetDistanceAndRadiusOfLavaBlob(lavaPosition, 3).x, true),
        true);

    //get the distance to the closest lava reservoir
    float reservoirDistance = min(lavaPosition.y - shapeParameters.bottomReservoirHeight , shapeParameters.topReservoirHeight - lavaPosition.y);

    //blend the blobs into the reservoir (no need to keep the surface contained within the original bounds here since the reservoir isn't tiled like the blobs)
    return LavaSmoothMin(distance, reservoirDistance, false);
}

float RaymarchLavaSurface(float3 startPosition, float3 marchDirection, float maxDistance, LavaLampShapeParameters shapeParameters)
{
    //do all the tracing relative to the lava scale
    startPosition /= shapeParameters.scale;
    maxDistance /= shapeParameters.scale;

    float totalDistance = 0.0;

    [loop]
    for (int step = 0; (step < cLavaRaymarchMaxSteps) && (totalDistance < maxDistance); step++)
    {
        float distanceToSurface = GetDistanceToLavaSurace(startPosition + (marchDirection * totalDistance), shapeParameters);

        //if we are close enough to the surface
        [branch]
        if (distanceToSurface <= 0.0)
        {
            //make the final step to return to the surface since we are inside of it
            totalDistance += distanceToSurface;
            return max(0.0, totalDistance * shapeParameters.scale);
        }

        //don't step further than _LavaPadding because we might overstep if the closest blob changes
        //(unless _LavaPadding is somehow less than the min step length, always step at least that far for the sake of performance)
        totalDistance += max(min(distanceToSurface, _LavaPadding), cLavaRaymarchMinStepLength);
    }

    return totalDistance < maxDistance ? totalDistance * shapeParameters.scale : -1.0;
}

//Rendering Functions -----------------------------------------------------------------------------

LavaSurfaceParameters GetLavaSurfaceParameters(float3 startPosition, float3 marchDirection, float hitDistance, LavaLampShapeParameters shapeParameters)
{
    float3 position = startPosition + (marchDirection * hitDistance);
    float3 lavaPosition = position / shapeParameters.scale; //do everything relative to the lava scale

    //get normal by empirically evaluating the derivative
    float3 distanceFieldDerivative = float3(
        GetDistanceToLavaSurace(lavaPosition + float3(cNormalEPS, 0.0, 0.0), shapeParameters) - GetDistanceToLavaSurace(lavaPosition - float3(cNormalEPS, 0.0, 0.0), shapeParameters),
        GetDistanceToLavaSurace(lavaPosition + float3(0.0, cNormalEPS, 0.0), shapeParameters) - GetDistanceToLavaSurace(lavaPosition - float3(0.0, cNormalEPS, 0.0), shapeParameters),
        GetDistanceToLavaSurace(lavaPosition + float3(0.0, 0.0, cNormalEPS), shapeParameters) - GetDistanceToLavaSurace(lavaPosition - float3(0.0, 0.0, cNormalEPS), shapeParameters));
    
    //calculate the thickness of the part where two blobs barely touch eachother based on the length of the  of the distance function
    float thicknessBlendScale = length(distanceFieldDerivative) / (cNormalEPS * 2.0);
    thicknessBlendScale = saturate(thicknessBlendScale / 0.95); //the derivative never perfectly reaches a full length of 1, so bias it a bit

    //normalize with NaN prevention
    float3 normal = clamp(normalize(distanceFieldDerivative), -1.0, 1.0);

    //get the distance to, and thickness of, the lava blobs at the hit position
    float2 lavaDistanceAndRadius = LavaSmoothMinWithValueBlend(
        LavaSmoothMinWithValueBlend(GetDistanceAndRadiusOfLavaBlob(lavaPosition, 0), GetDistanceAndRadiusOfLavaBlob(lavaPosition, 1), true),
        LavaSmoothMinWithValueBlend(GetDistanceAndRadiusOfLavaBlob(lavaPosition, 2), GetDistanceAndRadiusOfLavaBlob(lavaPosition, 3), true),
        true);

    //blobs will be shrunk by (_LavaSmoothingFactor / 2.0) during blending from what they say their radius is
    float radius = max(0.0, lavaDistanceAndRadius.y - (_LavaSmoothingFactor / 2.0));

    //account for the rounded shape of the blob in the thickness, just assume the thickness is the radius when the blob touches the side
    float sphereThickness = saturate(dot(normal, -marchDirection)) * 2.0;
    float touchingSideFactor = smoothstep(1.0, 0.0, saturate(hitDistance / _LavaTouchingSideBlendSize));
    float thickness = radius * lerp(sphereThickness * thicknessBlendScale, 1.0, touchingSideFactor);

    //determine how much the blobs are blending into the reservoirs
    float reservoirDistance = min(lavaPosition.y - shapeParameters.bottomReservoirHeight, shapeParameters.topReservoirHeight  - lavaPosition.y);
    float reservoirFactor = LavaSmoothMinWithValueBlend(float2(lavaDistanceAndRadius.x, 0.0), float2(reservoirDistance, 1.0), false).y;

    LavaSurfaceParameters surfaceParameters;
    surfaceParameters.position = position;
    surfaceParameters.normal = normal;
    surfaceParameters.thickness = thickness;
    surfaceParameters.reservoirFactor = reservoirFactor;
    surfaceParameters.touchingSideFactor = touchingSideFactor;

    return surfaceParameters;
}

float3 GetLavaLighting(float3 viewDirection, LavaSurfaceParameters surfaceParameters, LavaLampMaterialParameters materialParameters)
{
    //get the illuminance of each light
    float2 lightAttenuations = GetLavaLampLightAttenuations(surfaceParameters.position.y, materialParameters);
    float3 bottomIlluminance = materialParameters.bottomLightColor * lightAttenuations.x;
    float3 topIlluminance = materialParameters.topLightColor * lightAttenuations.y;

    //scattering (non directional lighting)
    float thicknessFactor = saturate(exp(-surfaceParameters.thickness * materialParameters.colorThicknessScale)); //expodentially blend colors based on thickness
    thicknessFactor = lerp(thicknessFactor, 0.0, surfaceParameters.reservoirFactor); //reservoir is always using the core color

    float3 scatteringColor = lerp(materialParameters.coreColor, materialParameters.edgeColor, thicknessFactor);
    scatteringColor *= bottomIlluminance + topIlluminance; //non directional

    //specular lighting
    float roughness = _LavaPerceptualRoughness * _LavaPerceptualRoughness; //unity parameterizes roughness as sqrt of the actual BRDF roughness, for a more linear change in appearence
    float3 specularColor = GGXCookTorrance(surfaceParameters.normal, viewDirection, float3(0.0, -1.0, 0.0), roughness, _LavaReflectiveness) * bottomIlluminance;
    specularColor += GGXCookTorrance(surfaceParameters.normal, viewDirection, float3(0.0, 1.0, 0.0), roughness, _LavaReflectiveness) * topIlluminance;

    specularColor *= (4.0 * UNITY_PI); //scattering normalization term, multiply here instead for energy normalization with more intuitive brightness levels for the user
    specularColor *= saturate(1.0 - surfaceParameters.touchingSideFactor); //no specular on surfaces touching the sides

    //combine with energy conservation
    return (scatteringColor * (1.0 - _LavaReflectiveness)) + specularColor;
}

float3 ApplyLampLiquidParticipation(float surfaceHeight, float3 viewDirection,
                                    float3 lavaColor, float lavaOpacityFactor, float lavaDistance,
                                    float3 backgroundColor, float backgroundDistance,
                                    LavaLampMaterialParameters materialParameters)
{
    //get water extinction
    float3 combinedExtinction = materialParameters.waterHazeStrength + (float3(1.0 - materialParameters.waterTintColor) * materialParameters.waterTintStrength);

    //get the illuminance of each light, just assume the lighting is the same across the entire ray
    float2 waterLightAttenuations = GetLavaLampLightAttenuations(surfaceHeight, materialParameters);
    float3 totalIlluminance = (materialParameters.bottomLightColor * waterLightAttenuations.x) + (materialParameters.topLightColor * waterLightAttenuations.y);

    //get the ammount of inscattered light, non directional
    float3 waterInScatter = max(0.0, totalIlluminance * materialParameters.waterHazeColor * materialParameters.waterHazeStrength / combinedExtinction);

    //integrate transmittance and in-scatter for lava and background
    float3 transmittanceToLava = saturate(exp(-lavaDistance * combinedExtinction));
    lavaColor = lerp(waterInScatter, lavaColor, transmittanceToLava);

    float3 transmittanceToBackground = saturate(exp(-backgroundDistance * combinedExtinction));
    backgroundColor = lerp(waterInScatter, backgroundColor, transmittanceToBackground);

    //alpha blend between lava color and background color
    return lerp(backgroundColor, lavaColor, lavaOpacityFactor);
}

float3 GetLavaLampColor(float3 startPosition, float3 marchDirection, float maxDistance, float3 backgroundColor, int subregionIndex)
{
    marchDirection = normalize(marchDirection);
    maxDistance = max(0.0, maxDistance);

    //get subregion shape parameters for raymarching
    LavaLampShapeParameters shapeParameters = GetLavaLampShapeParameters(subregionIndex);
    shapeParameters.bottomReservoirHeight /= shapeParameters.scale; //rescale reservoir height here for simplicity
    shapeParameters.topReservoirHeight /= shapeParameters.scale;

    //rotate the local raymarching space based on our lava flow direction 
    ApplyLavaFlowRotation(shapeParameters.flowDirection, startPosition, marchDirection);
    
    //raymarch to test for a lava surface intersection
    float distance = RaymarchLavaSurface(startPosition, marchDirection, maxDistance, shapeParameters);
    
    //get the opacity of the lava based on if we hit it at all as well as soft depth
    float lavaOpacityFactor = saturate((maxDistance - distance) / min(_LavaSoftDepthSize, maxDistance));
    lavaOpacityFactor = smoothstep(0.0, 1.0, lavaOpacityFactor);
    lavaOpacityFactor = distance >= 0.0 ? lavaOpacityFactor : 0.0; //if distance is negative then we didn't hit lava
    
    //get the subregion material parameters for lighting
    LavaLampMaterialParameters materialParameters = GetLavaLampMaterialParameters(subregionIndex);

    //get the lava color, if we actually hit it
    float3 lavaColor = 0.0;

    [branch]
    if (lavaOpacityFactor > 0.0)
    {
        LavaSurfaceParameters surfaceParameters = GetLavaSurfaceParameters(startPosition, marchDirection, distance, shapeParameters);
        lavaColor = GetLavaLighting(marchDirection, surfaceParameters, materialParameters);
    }
    
    //apply participating media effects from the lamp liquid and composite lava with the background
    return ApplyLampLiquidParticipation(startPosition.y, marchDirection, lavaColor, lavaOpacityFactor, distance, backgroundColor, maxDistance, materialParameters);
}

//Constant Helper Macros --------------------------------------------------------------------------

#define DECLARE_LAVA_LAMP_CONSTANTS(id) \
float _LavaScale##id; \
float _LavaTopReservoirHeight##id; \
float _LavaBottomReservoirHeight##id; \
float3 _LavaFlowDirection##id; \
float3 _LavaCoreColor##id; \
float3 _LavaEdgeColor##id; \
float _LavaColorThicknessScale##id; \
float3 _LavaWaterHazeColor##id; \
float _LavaWaterHazeStrength##id; \
float3 _LavaWaterTintColor##id; \
float _LavaWaterTintStrength##id; \
float3 _LavaTopLightColor##id; \
float _LavaTopLightHeight##id; \
float3 _LavaBottomLightColor##id; \
float _LavaBottomLightHeight##id; \
float _LavaLightFalloff##id;

#define FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParametersStruct, id) \
{ \
    shapeParametersStruct.scale = _LavaScale##id; \
    shapeParametersStruct.topReservoirHeight = _LavaTopReservoirHeight##id; \
    shapeParametersStruct.bottomReservoirHeight = _LavaBottomReservoirHeight##id; \
    shapeParametersStruct.flowDirection = _LavaFlowDirection##id; \
}

#define FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParametersStruct, id) \
{ \
    materialParametersStruct.coreColor = _LavaCoreColor##id; \
    materialParametersStruct.edgeColor = _LavaEdgeColor##id; \
    materialParametersStruct.colorThicknessScale = _LavaColorThicknessScale##id; \
    materialParametersStruct.waterHazeColor = _LavaWaterHazeColor##id; \
    materialParametersStruct.waterHazeStrength = _LavaWaterHazeStrength##id; \
    materialParametersStruct.waterTintColor = _LavaWaterTintColor##id; \
    materialParametersStruct.waterTintStrength = _LavaWaterTintStrength##id; \
    materialParametersStruct.topLightColor = _LavaTopLightColor##id; \
    materialParametersStruct.topLightHeight = _LavaTopLightHeight##id; \
    materialParametersStruct.bottomLightColor = _LavaBottomLightColor##id; \
    materialParametersStruct.bottomLightHeight = _LavaBottomLightHeight##id; \
    materialParametersStruct.lightFalloff = _LavaLightFalloff##id; \
}

#endif