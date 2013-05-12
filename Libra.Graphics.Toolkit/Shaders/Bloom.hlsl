cbuffer Parameters : register(b0)
{
    float BaseIntensity     : packoffset(c0.x);
    float BaseSaturation    : packoffset(c0.y);
    float BloomIntensity    : packoffset(c0.z);
    float BloomSaturation   : packoffset(c0.w);
};

Texture2D Texture  : register(t0);
Texture2D BaseTexture   : register(t1);

SamplerState TextureSampler : register(s0);
SamplerState BaseTextureSampler  : register(s1);

float4 AdjustSaturation(float4 color, float saturation)
{
    float grey = dot(color, float4(0.3, 0.59, 0.11, 0));

    return lerp(grey, color, saturation);
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 bloom = Texture.Sample(TextureSampler, texCoord);
    float4 base = BaseTexture.Sample(BaseTextureSampler, texCoord);

    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;

    base *= (1 - saturate(bloom));

    return base + bloom;
}
