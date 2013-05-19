// �_�~�[�B
Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

Texture2D<float> SSAOMap : register(t1);
SamplerState SSAOMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float occlusion = SSAOMap.SampleLevel(SSAOMapSampler, texCoord, 0);

    return float4(occlusion, occlusion, occlusion, 1);
}

