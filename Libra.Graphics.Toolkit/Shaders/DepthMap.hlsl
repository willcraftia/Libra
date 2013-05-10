cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
};

struct VSOutput
{
    float4 Position     : SV_Position;
    float4 PositionWVP  : POSITION_WVP;
};

VSOutput VS(float4 position : SV_Position)
{
    VSOutput output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWVP = output.Position;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    float depth = input.PositionWVP.z / input.PositionWVP.w;
    return float4(depth, 0, 0, 0);
}
