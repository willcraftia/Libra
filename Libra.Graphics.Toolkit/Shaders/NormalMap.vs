cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 WorldView;
};

struct Input
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
};

struct Output
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
};

Output VS(Input input)
{
    Output output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = mul(float4(input.Normal, 0), WorldView).xyz;

    return output;
}
