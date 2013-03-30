//-----------------------------------------------------------------------------
// InstancedModel.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// XNA の InstancedModel.fx を移植。
// シェーダ モデル 4.0 へ改変。

cbuffer Parameters : register(b0)
{
    float4x4 World          : packoffset(c0);
    float4x4 View           : packoffset(c4);
    float4x4 Projection     : packoffset(c8);
    float3   LightDirection : packoffset(c12);
    float3   DiffuseLight   : packoffset(c13);
    float3   AmbientLight   : packoffset(c14);
};

Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSCommon(VSInput input, float4x4 instanceTransform)
{
    VSOutput output;

    float4 worldPosition = mul(input.Position, instanceTransform);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    float3 worldNormal = mul(float4(input.Normal, 0), instanceTransform).xyz;

    float diffuseAmount = max(-dot(worldNormal, LightDirection), 0);

    float3 lightingResult = saturate(diffuseAmount * DiffuseLight + AmbientLight);

    output.Color = float4(lightingResult, 1);

    output.TexCoord = input.TexCoord;

    return output;
}

VSOutput HWInstancingVS(VSInput input, float4x4 instanceTransform : TRANSFORM)
{
    return VSCommon(input, mul(World, transpose(instanceTransform)));
}

VSOutput NoInstancingVS(VSInput input)
{
    return VSCommon(input, World);
}

float4 PS(VSOutput input) : SV_Target
{
    return Texture.Sample(Sampler, input.TexCoord) * input.Color;
}
