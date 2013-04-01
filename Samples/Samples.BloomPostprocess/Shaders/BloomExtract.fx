cbuffer Parameters : register(b1)
{
    float BloomThreshold;
};

Texture2D Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = Texture.Sample(TextureSampler, texCoord);

    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}
