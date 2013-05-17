#define MAX_RADIUS 7
#define MAX_KERNEL_SIZE (MAX_RADIUS * 2 + 1)

cbuffer Parameters : register(b0)
{
    // 深度の重み
    float DepthSigma                : packoffset(c0);
    // カーネル サイズ
    float KernelSize                : packoffset(c0.y);
    // xy = テクセル オフセット
    // z  = 空間の重み
    float3 Kernel[MAX_KERNEL_SIZE]  : packoffset(c1);
};

// 法線マップは _SNORM フォーマット。
Texture2D<float>  Texture           : register(t0);
Texture2D<float>  LinearDepthMap    : register(t1);
Texture2D<float3> NormalMap         : register(t2);

SamplerState TextureSampler         : register(s0);
SamplerState LinearDepthMapSampler  : register(s1);
SamplerState NormalMapSampler       : register(s2);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float centerDepth = LinearDepthMap.Sample(LinearDepthMapSampler, texCoord);

    float4 totalColor = 0;
    float totalWeight = 0;

    for (int i = 0; i < KernelSize; i++)
    {
        float3 kernel = Kernel[i];

        float2 offset = kernel.xy;
        float spaceWeight = kernel.z;

        float2 sampleTexCoord = texCoord + offset;

        float sampleColor = Texture.Sample(TextureSampler, sampleTexCoord);
        float sampleDepth = LinearDepthMap.Sample(LinearDepthMapSampler, sampleTexCoord);

        // 深度の差から深度の重みを算出。
        float depthCloseness = abs(centerDepth - sampleDepth);
        float depthWeight = exp(-depthCloseness * depthCloseness / (2 * DepthSigma * DepthSigma));

        // 重み = 空間の重み * 深度の重み
        float sampleWeight = spaceWeight * depthWeight;

        totalColor += sampleColor * sampleWeight;
        totalWeight += sampleWeight;
    }

    // 正規化して最終的な色を決定。
    return totalColor / totalWeight;
}
