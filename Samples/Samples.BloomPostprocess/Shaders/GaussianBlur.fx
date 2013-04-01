#define SAMPLE_COUNT 15

cbuffer Parameters : register(b1)
{
    float2 SampleOffsets[SAMPLE_COUNT]  : packoffset(c0);
    float SampleWeights[SAMPLE_COUNT]   : packoffset(c9);
};

Texture2D Texture : register(t0)
SamplerState TextureSampler : register(s0);

float4 PS(float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = 0;

    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        c += tex2D(TextureSampler, texCoord + SampleOffsets[i]) * SampleWeights[i];
    }

    return c;
}
