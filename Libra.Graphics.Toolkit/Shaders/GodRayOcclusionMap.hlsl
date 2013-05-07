cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
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
    return float4(0, 0, 0, 1);
}
