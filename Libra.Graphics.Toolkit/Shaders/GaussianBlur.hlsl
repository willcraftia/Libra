#define MAX_RADIUS 7
#define KERNEL_SIZE (MAX_RADIUS * 2 + 1)

float KernelSize = KERNEL_SIZE;
float Weights[KERNEL_SIZE];
float2 OffsetsH[KERNEL_SIZE];
float2 OffsetsV[KERNEL_SIZE];

cbuffer Parameters : register(b0)
{
    float KernelSize;
    
};

Texture2D Texture : register(t0);
SamplerState TextureSampler : register(s0)
{
    Filter = MIN_MAG_MIP_POINT
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 HorizontalPS(float4 color    : COLOR0,
                    float2 texCoord : TEXCOORD0) : SV_Target0
{
    float4 c = 0;
    for (int i = 0; i < KernelSize; i++)
    {
        c += Texture.Sample(TextureSampler, texCoord + OffsetsH[i]) * Weights[i];
    }
    return c;
}

float4 VerticalPS(float4 color    : COLOR0,
                  float2 texCoord : TEXCOORD0) : SV_Target0
{
    float4 c = 0;
    for (int i = 0; i < KernelSize; i++)
    {
        c += Texture.Sample(TextureSampler, texCoord + OffsetsV[i]) * Weights[i];
    }
    return c;
}
