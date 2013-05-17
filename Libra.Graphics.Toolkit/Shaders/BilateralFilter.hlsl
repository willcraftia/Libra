//
// メモ
//
// Y  =  0.29900 * R + 0.58700 * G + 0.11400 * B
// Cb = -0.16874 * R - 0.33126 * G + 0.50000 * B
// Cr =  0.50000 * R - 0.41869 * G - 0.08131 * B
//
// Y is luminance component
// Cb is the blue-defference chroma component
// Cr is the red-difference chroma component

#define MAX_RADIUS 7
#define MAX_KERNEL_SIZE (MAX_RADIUS * 2 + 1)

cbuffer Parameters : register(b0)
{
    // 色の重み
    float ColorSigma                : packoffset(c0);
    // カーネル サイズ
    float KernelSize                : packoffset(c0.y);
    // xy = テクセル オフセット
    // z  = 空間の重み
    float3 Kernel[MAX_KERNEL_SIZE]  : packoffset(c1);
};

Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float4 center = Texture.Sample(TextureSampler, texCoord);

    float4 totalColor = 0;
    float totalWeight = 0;

    for (int i = 0; i < KernelSize; i++)
    {
        float3 kernel = Kernel[i];

        float2 offset = kernel.xy;
        float spaceWeight = kernel.z;

        float4 sample = Texture.Sample(TextureSampler, texCoord + offset);

        // 色の差から色の重みを算出。
        // 色差の式は状況により変更しても良い。
        float closeness = distance(center, sample);
        float colorWeight = exp(-closeness * closeness / (2 * ColorSigma * ColorSigma));

        // 重み = 空間の重み * 色の重み
        float weight = spaceWeight * colorWeight;

        totalColor += sample * weight;
        totalWeight += weight;
    }

    // 正規化して最終的な色を決定。
    return totalColor / totalWeight;
}
