cbuffer Parameters : register(b0)
{
    float NearClipDistance  : packoffset(c0.x);
    float FarClipDistance   : packoffset(c0.y);
};

// ダミー。
Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

Texture2D<float> LinearDepthMap : register(t1);
SamplerState LinearDepthMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float z = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);

    z = (z - NearClipDistance) / (FarClipDistance - NearClipDistance);

    return float4(z, z, z, 1);
}

