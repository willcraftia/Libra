cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 WorldView;
};

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = mul(float4(input.Normal, 0), WorldView).xyz;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    return float4(normalize(input.Normal), 1);
}
