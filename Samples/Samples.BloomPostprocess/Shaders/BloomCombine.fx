
cbuffer Parameters : register(b0)
{
    float BloomIntensity    : packoffset(c0.x);
    float BaseIntensity     : packoffset(c0.y);
    float BloomSaturation   : packoffset(c0.z);
    float BaseSaturation    : packoffset(c0.w);
};

Texture2D Bloom : register(t0);
Texture2D Base : register(t1);

SamplerState BloomSampler : register(s0);
SamplerState BaseSampler : register(s1);

float4 AdjustSaturation(float4 color, float saturation)
{
    float grey = dot(color, float3(0.3, 0.59, 0.11));

    return lerp(grey, color, saturation);
}


float4 PS(float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 bloom = Bloom.Sample(BloomSampler, texCoord);
    float4 base = Base.Sample(BaseSampler, texCoord);

    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;

    base *= (1 - saturate(bloom));

    return base + bloom;
}
