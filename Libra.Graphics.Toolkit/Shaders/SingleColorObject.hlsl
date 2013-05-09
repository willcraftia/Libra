cbuffer VSParameters : register(b0)
{
    float4x4 WorldViewProjection;
};

cbuffer PSParameters : register(b0)
{
    float4 Color;
};

struct VSOutput
{
    float4 Position : SV_Position;
};

VSOutput VS(float4 position : SV_Position)
{
    VSOutput output;

    output.Position = mul(position, WorldViewProjection);

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    return Color;
}
