#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    int SampleCount             : packoffset(c0);
    float2 ScreenLightPosition  : packoffset(c1);
    float Density               : packoffset(c2.x);
    float Decay                 : packoffset(c2.y);
    float Weight                : packoffset(c2.z);
    float Exposure              : packoffset(c2.w);
};

Texture2D<float3> SceneMap : register(t0);
SamplerState SceneMapSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float2 deltaTexCoord = (texCoord - ScreenLightPosition);
    deltaTexCoord *= 1.0f / SampleCount * Density;

    float3 sceneColor = SceneMap.Sample(SceneMapSampler, texCoord);

    float illuminationDecay = 1;

    [unroll(MAX_SAMPLE_COUNT)]
    for (int i = 0; i < SampleCount; i++)
    {
        texCoord -= deltaTexCoord;

        float3 sample = SceneMap.Sample(SceneMapSampler, texCoord);

        sample *= illuminationDecay * Weight;
        sceneColor += sample;
        illuminationDecay *= Decay;
    }

    return float4(sceneColor * Exposure, 1);
}
