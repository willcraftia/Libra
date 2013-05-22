cbuffer Parameters : register(b0)
{
    // xy = テクセル オフセット
    float2 Kernel[4];
};

// ダミー。
Texture2D<float> Texture : register(t0);
SamplerState TextureSampler : register(s0);

Texture2D<float> HeightMap : register(t1);
SamplerState HeightMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    // *  - h2 - *
    // |    |    |
    // h0 - c  - h1
    // |    |    |
    // *  - h3 - *
    float h0 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[0]);
    float h1 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[1]);
    float h2 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[2]);
    float h3 = HeightMap.Sample(HeightMapSampler, texCoord + Kernel[3]);

    float u = (h1 - h0) * 0.5;
    float v = (h3 - h2) * 0.5;

    return float4(u, v, 0, 0);
}
