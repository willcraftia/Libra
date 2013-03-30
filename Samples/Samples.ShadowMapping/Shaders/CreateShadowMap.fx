//-----------------------------------------------------------------------------
// DrawModel.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// XNA の DrawModel.fx からシャドウマップ生成のためのシェーダを分離。
// および、シェーダ モデル 4.0 へ変更。

cbuffer Parameters : register(b0)
{
    float4x4 World          : packoffset(c0);
    float4x4 LightViewProj  : packoffset(c4);
};

struct VSOutput
{
    float4 Position : SV_Position;
    float  Depth    : TEXCOORD0;
};

VSOutput VS(float4 Position : SV_Position)
{
    VSOutput output;

    output.Position = mul(Position, mul(World, LightViewProj));
    output.Depth = output.Position.z / output.Position.w;

    return output;
}

float4 PS(VSOutput input) : SV_Target
{ 
    return float4(input.Depth, 0, 0, 0);
}
