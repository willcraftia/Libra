Texture2D Texture  : register(t0);
Texture2D BaseTexture   : register(t1);

SamplerState TextureSampler : register(s0);
SamplerState BaseTextureSampler  : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = Texture.Sample(TextureSampler, texCoord);

    return c + BaseTexture.Sample(BaseTextureSampler, texCoord);
}
