#define KERNEL_SIZE 4

cbuffer Parameters : register(b0)
{
    float2 Kernels[KERNEL_SIZE];
};

// ポストプロセス規約による定義 (シェーダ内未使用)。
Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

// 法線マップは _SNORM フォーマット。
Texture2D<float3> NormalMap : register(t1);
SamplerState NormalMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float3 source = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);
    source = normalize(source);

    float3 sum = 0;

    [unroll]
    for (int i = 0; i < KERNEL_SIZE; i++)
    {
        float3 sample = NormalMap.SampleLevel(NormalMapSampler, texCoord + Kernels[i], 0);
        sample = normalize(sample);

        sum += saturate(1 - dot(source, sample));
    }

    return float4(sum, 1);
}
