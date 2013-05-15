cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 WorldView;
};

struct VSOutput
{
    float4 Position     : SV_Position;
    float4 PositionWV   : POSITION_WV;
};

VSOutput VS(float4 position : SV_Position)
{
    VSOutput output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWV = mul(position, WorldView);

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    return float4(-input.PositionWV.z, 0, 0, 0);
}
