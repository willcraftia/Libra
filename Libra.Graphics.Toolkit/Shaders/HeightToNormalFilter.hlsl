cbuffer Parameters : register(b0)
{
    // xy = テクセル オフセット
    float2 Kernel[4];
};

// ダミー。
Texture2D<float> Texture : register(t0);
SamplerState TextureSampler : register(s0);

// 高さマップ。
Texture2D<float> HeightMap : register(t1);
SamplerState HeightMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    // *  - s2 - *
    // |    |    |
    // s0 - c  - s1
    // |    |    |
    // *  - s3 - *
    float s0 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[0]);
    float s1 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[1]);
    float s2 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[2]);
    float s3 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[3]);

    // 以下、右手系で処理。

    float3 u = float3(1.0, (s0 - s1) * 0.5, 0.0);
    float3 v = float3(0.0, (s3 - s2) * 0.5, 1.0);

    float3 normal = normalize(cross(u, v));

    return float4(normal, 0.0);
}
