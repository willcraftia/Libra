cbuffer Parameters : register(b0)
{
    float3 ShadowColor;
};

// Texture �͒ʏ�V�[��
Texture2D<float4> Texture               : register(t0);
Texture2D<float>  AmbientOcclusionMap   : register(t1);

SamplerState TextureSampler             : register(s0);
SamplerState AmbientOcclusionMapSampler : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float4 scene = Texture.SampleLevel(TextureSampler, texCoord, 0);
    float occlusion = AmbientOcclusionMap.SampleLevel(AmbientOcclusionMapSampler, texCoord, 0);

    float3 c = lerp(scene.rgb * ShadowColor, scene.rgb, occlusion);

    return float4(c, scene.a);
}