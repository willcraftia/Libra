#define MAX_KERNEL_SIZE 32

cbuffer Parameters : register(b0)
{
    float2 Center                   : packoffset(c0);
    float  Strength                 : packoffset(c0.z);
    int    KernelSize               : packoffset(c0.w);
    // xy = offset
    // z  = weight
    float3 Kernels[MAX_KERNEL_SIZE] : packoffset(c1);
};

Texture2D Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    // 放射中心からのベクトル。
    float2 direction = Center - texCoord;

    // 放射中心から距離。
    float distance = length(direction);

    // 正規化。
    direction /= distance;

    // 距離と Strength に応じて基本サンプル位置を広げる。
    float2 baseOffset = direction * distance * Strength;

    float4 c = 0;

    for (int i = 0; i < KernelSize; i++)
    {
        float3 kernel = Kernels[i];
        float2 offset = baseOffset * kernel.xy;
        float weight = kernel.z;
        c += Texture.Sample(TextureSampler, texCoord + offset) * weight;
    }

    return c;
}
