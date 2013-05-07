cbuffer Parameters : register(b0)
{
    float FocusScale            : packoffset(c0.x);
    float FocusDistance         : packoffset(c0.y);
    float4x4 InvertProjection   : packoffset(c1);
};

Texture2D<float4> NormalSceneMap    : register(t0);
Texture2D<float4> BluredSceneMap    : register(t1);
Texture2D<float>  DepthMap          : register(t2);

SamplerState SceneMapSampler        : register(s0);
SamplerState BluredSceneMapSampler  : register(s1);
SamplerState DepthMapSampler        : register(s2);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 normalScene = NormalSceneMap.Sample(SceneMapSampler, texCoord);
    float4 bluredScene = BluredSceneMap.Sample(BluredSceneMapSampler, texCoord);
    float depth = DepthMap.Sample(DepthMapSampler, texCoord);

    float4 depthSample = mul(float4(texCoord, depth, 1), InvertProjection);
    float sceneDistance = depthSample.z / depthSample.w;

    float blurFactor = saturate(abs(sceneDistance - FocusDistance) * FocusScale);

    return lerp(normalScene, bluredScene, blurFactor);
}
