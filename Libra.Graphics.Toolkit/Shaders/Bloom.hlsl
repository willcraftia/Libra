cbuffer Parameters : register(b0)
{
    float BaseIntensity     : packoffset(c0.x);
    float BaseSaturation    : packoffset(c0.y);
    float BloomIntensity    : packoffset(c0.z);
    float BloomSaturation   : packoffset(c0.w);
};

Texture2D Texture   : register(t0);
Texture2D BloomMap  : register(t1);

sampler TextureSampler  : register(s0);
sampler BloomMapSampler : register(s1);

float4 AdjustSaturation(float4 color, float saturation)
{
    float grey = dot(color, float4(0.3, 0.59, 0.11, 0));

    return lerp(grey, color, saturation);
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 base = Texture.Sample(TextureSampler, texCoord);
    float4 bloom = BloomMap.Sample(BloomMapSampler, texCoord);

    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;
    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;

    base *= (1 - saturate(bloom));

    return base + bloom;
}
