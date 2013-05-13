#define KERNEL_SIZE 4

cbuffer Parameters : register(b0)
{
    float2 Kernels[KERNEL_SIZE];
};

// �|�X�g�v���Z�X�K��ɂ���` (�V�F�[�_�����g�p)�B
Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

Texture2D<float3> NormalMap : register(t1);
SamplerState NormalMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float3 source = NormalMap.Sample(NormalMapSampler, texCoord);
    source = normalize(source * 2.0f - 1.0f);

    float3 sum = 0;

    [unroll]
    for (int i = 0; i < KERNEL_SIZE; i++)
    {
        float3 sample = NormalMap.Sample(NormalMapSampler, texCoord + Kernels[i]);
        sample = normalize(sample * 2.0f - 1.0f);

        sum += saturate(1 - dot(source, sample));
    }

    return float4(sum, 1);
}
