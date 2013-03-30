//-----------------------------------------------------------------------------
// DrawModel.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// XNA の DrawModel.fx をシェーダ モデル 4.0 へ変更。
// シャドウマップ生成シェーダは CreateShadowMap.fx へ分離。

cbuffer Parameters : register(b0)
{
    float4x4 World          : packoffset(c0);
    float4x4 View           : packoffset(c4);
    float4x4 Projection     : packoffset(c8);
    float4x4 LightViewProj  : packoffset(c12);
    float3   LightDirection : packoffset(c16);
    float    DepthBias      : packoffset(c16.w);
    float4   AmbientColor   : packoffset(c17);
};

Texture2D<float4> Texture   : register(t0);
Texture2D<float>  ShadowMap : register(t1);

SamplerState TextureSampler     : register(s0);
SamplerState ShadowMapSampler   : register(s1);

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 WorldPos : TEXCOORD2;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    float4x4 WorldViewProj = mul(mul(World, View), Projection);

    output.Position = mul(input.Position, WorldViewProj);
    output.Normal =  normalize(mul(float4(input.Normal, 0), World)).xyz;
    output.TexCoord = input.TexCoord;

    output.WorldPos = mul(input.Position, World);

    return output;
}

float4 PS(VSOutput input) : SV_Target
{ 
    float4 diffuseColor = Texture.Sample(TextureSampler, input.TexCoord);

    float diffuseIntensity = saturate(dot(LightDirection, input.Normal));
    float4 diffuse = diffuseIntensity * diffuseColor + AmbientColor;

    float4 lightingPosition = mul(input.WorldPos, LightViewProj);

    float2 ShadowTexCoord = 0.5 * lightingPosition.xy / lightingPosition.w + float2( 0.5, 0.5 );
    ShadowTexCoord.y = 1.0f - ShadowTexCoord.y;

    float shadowdepth = ShadowMap.Sample(ShadowMapSampler, ShadowTexCoord);

    float ourdepth = (lightingPosition.z / lightingPosition.w) - DepthBias;

    if (shadowdepth < ourdepth)
    {
        diffuse *= float4(0.5,0.5,0.5,0);
    };

    return diffuse;
}
