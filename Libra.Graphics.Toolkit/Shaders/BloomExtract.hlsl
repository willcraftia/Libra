cbuffer Parameters : register(b0)
{
    float BloomThreshold;
};

Texture2D<float4> Texture : register(t0);
sampler TextureSampler : register(s0);

// SpriteEffect �� PS �V�O�l�`���ɍ��킹�� COLOR0 ���w�肵�Ȃ��Ə�肭�ғ����Ȃ��B
float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 c = Texture.Sample(TextureSampler, texCoord);

    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}
