#ifndef HELPERFUNCTIONS
#define HELPERFUNCTIONS


float inverseLerp(float a, float b, float v)
{
    return (v - a) / (b - a);
}

float inverseLerp2(float2 a, float2 b, float2 v)
{
    return (v - a) / (b - a);
}

float inverseLerp3(float3 a, float3 b, float3 v)
{
    return (v - a) / (b - a);
}

float inverseLerp3(float4 a, float4 b, float4 v)
{
    return (v - a) / (b - a);
}

float remap(float iMin, float iMax, float oMin, float oMax, float v)
{
    float t = inverseLerp(iMin, iMax, v);
    return lerp(oMin, oMax, t);
}

float radialAlpha(float2 UV)
{

    float2 remappedUV = (UV * 2 - 1);
    float mask = 1 - dot(remappedUV, remappedUV);
    return mask;
}

float radialAlphaSmooth(float2 UV, float intensity = 0.01f)
{

    float2 remappedUV = (UV * 2 - 1);
    float mask = 1 - dot(remappedUV, remappedUV);
    float shiftAdjust = intensity / (1 + intensity);
    float scaleBack = 1 + (pow((mask - 1.0f), 2) + intensity) / (1 + intensity);
    float smoothMask = saturate((intensity / (pow((mask - 1.0f), 2) + intensity) - shiftAdjust) * scaleBack);
    return smoothMask;
}

float2 tileAndCenter(float2 UV, float tileX, float tileY)
{

    float2 offsetToCenter = float2(1 - tileX, 1 - tileY) / 2;
    float2 remappedUV = float2(UV.x * tileX, UV.y * tileY) + offsetToCenter;
    return remappedUV;
}

#endif