cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 WorldView;
};

struct Output
{
    float4 Position     : SV_Position;
    float4 PositionWV   : TEXCOORD0;
    float4 PositionWVP  : TEXCOORD1;
};

Output VS(float4 position : SV_Position)
{
    Output output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWV = mul(position, WorldView);
    output.PositionWVP = output.Position;

    return output;
}
