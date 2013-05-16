#define KERNEL_SIZE 16

cbuffer Parameters : register(b0)
{
    float2 Kernel[KERNEL_SIZE];
};

Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = 0;

    [unroll]
    for (int i = 0; i < KERNEL_SIZE; i++)
    {
        c += Texture.Sample(TextureSampler, texCoord + Kernel[i]);
    }

    return c / KERNEL_SIZE;
}
