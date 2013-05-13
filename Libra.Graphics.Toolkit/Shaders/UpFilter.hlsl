Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    return Texture.Sample(TextureSampler, texCoord);
}
