cbuffer Parameters : register(b0)
{
    float3 FogColor;
};

// Texture ÇÕí èÌÉVÅ[Éì
Texture2D<float4> Texture               : register(t0);
Texture2D<float> LinearVolumetricFogMap : register(t1);

SamplerState TextureSampler                 : register(s0);
SamplerState LinearVolumetricFogMapSampler  : register(s1);


float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float4 scene = Texture.SampleLevel(TextureSampler, texCoord, 0);
    float fog = LinearVolumetricFogMap.SampleLevel(LinearVolumetricFogMapSampler, texCoord, 0);

    float3 c = lerp(scene.rgb, FogColor, fog);

    return float4(c, scene.a);
}

