//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

//Based on kuwahara effect here: https://danielilett.com/2019-05-18-tut1-6-smo-painting/
//Uses Linear Depth Fade code found in SCPostProcessing: https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/sc-post-effects-pack-108753

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X(_BlitTexture);
float2 _BlitTexture_TexelSize;

struct region
{
	float3 mean;
	float variance;
};

struct Sums {
    float3 sum;
    float3 squareSum;
};

Sums calculateSums(int2 lowerBound, int2 upperBound, float2 uv) {
    float3 sum = 0.0;
    float3 squareSum = 0.0;
    for (int x = lowerBound.x; x <= upperBound.x; ++x)
    {
        for (int y = lowerBound.y; y <= upperBound.y; ++y)
        {
            float2 offset = float2(_BlitTexture_TexelSize.x * x, _BlitTexture_TexelSize.y * y);
            int2 texelCords = int2((uv + offset) * _ScreenSize.xy);
            float3 tex = LOAD_TEXTURE2D_X_LOD(_BlitTexture, texelCords, 0).rgb;
            sum += tex;
            squareSum += tex * tex;
        }
    }
    Sums sums;
    sums.sum = sum;
    sums.squareSum = squareSum;
    return sums;
}

region calcRegion(uint2 lower, uint2 upper, int samples, float2 uv) {
    Sums sums = calculateSums(lower, upper, uv);
    float3 sum = sums.sum;
    float3 squareSum = sums.squareSum;

    region r;
    r.mean = sum / samples;
    float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
    r.variance = length(variance);

    return r;
}
#define NEAR_PLANE _ProjectionParams.y
#define FAR_PLANE _ProjectionParams.z

void LinearDepthFade_float(float linearDepth, float start, float end, float invert, float enable, out float Out)
{
    if(enable == 0.0) Out = 1.0;
	
    float rawDepth = (linearDepth * FAR_PLANE) - NEAR_PLANE;
    float eyeDepth = FAR_PLANE - ((_ZBufferParams.z * (1.0 - rawDepth) + _ZBufferParams.w) * _ProjectionParams.w);

    float perspDist = rawDepth;
    float orthoDist = eyeDepth;

    //Non-linear depth values
    float dist = lerp(perspDist, orthoDist, unity_OrthoParams.w);
	
    float fadeFactor = saturate((end - dist) / (end-start));

    //OpenGL + Vulkan
    #if !defined(UNITY_REVERSED_Z)
    fadeFactor = 1-fadeFactor;
    #endif
	
    if (invert == 1.0) fadeFactor = 1-fadeFactor;

    Out = fadeFactor;
}

void Kuwahara_float(float2 screenUV, uint _KernelSize, out float4 Out)
{
    int upper = (_KernelSize - 1) / 2;
    int lower = -upper;

    int samples = (upper + 1) * (upper + 1);

    // Calculate the four regional parameters as discussed.
    region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, screenUV);
    region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, screenUV);
    region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, screenUV);
    region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, screenUV);

    float3 col = regionA.mean;
    float minVar = regionA.variance;

    /*	Cascade through each region and compare variances - the end
        result will be the that the correct mean is picked for col.
    */
    float testVal;

    testVal = step(regionB.variance, minVar);
    col = lerp(col, regionB.mean, testVal);
    minVar = lerp(minVar, regionB.variance, testVal);

    testVal = step(regionC.variance, minVar);
    col = lerp(col, regionC.mean, testVal);
    minVar = lerp(minVar, regionC.variance, testVal);

    testVal = step(regionD.variance, minVar);
    col = lerp(col, regionD.mean, testVal);

    Out = float4(col, 1.0);
}

#endif //MYHLSLINCLUDE_INCLUDED