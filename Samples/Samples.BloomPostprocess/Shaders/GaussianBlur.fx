#define SAMPLE_COUNT 15

cbuffer Parameters : register(b0)
{
    // xy = SampleOffsets
    // z  = SampleWeights
    float4 Samples[SAMPLE_COUNT];
};

Texture2D<float4> Texture : register(t0);
sampler TextureSampler : register(s0);

// SpriteEffect �� PS �V�O�l�`���ɍ��킹�� COLOR0 ���w�肵�Ȃ��Ə�肭�ғ����Ȃ��B
float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = 0;

    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        c += Texture.Sample(TextureSampler, texCoord + Samples[i].xy) * Samples[i].z;
    }

    return c;
}
