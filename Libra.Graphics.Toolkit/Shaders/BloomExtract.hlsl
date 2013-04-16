cbuffer Parameters : register(b0)
{
    float BloomThreshold;
};

Texture2D<float4> Texture : register(t0);
sampler TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = Texture.Sample(TextureSampler, texCoord);

    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}
