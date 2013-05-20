cbuffer Parameters : register(b0)
{
    float Density       : packoffset(c0.x);
    float Brightness    : packoffset(c0.y);
};

Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // Y ���W�ɉ����� sin �ŋP�x��������B
    float amount = (sin(texCoord.y * Density) + 1) * 0.5 * (1 - Brightness) + Brightness;

    return Texture.Sample(TextureSampler, texCoord) * amount;
}