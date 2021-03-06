cbuffer PerObject : register(b0)
{
    float4x4 WorldView;
    float4x4 WorldViewProjection;
    float4x4 WorldReflectionProjection;
};

struct Input
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

struct Output
{
    float4 Position     : SV_Position;
    float2 TexCoord     : TEXCOORD0;
    float4 PositionWV   : TEXCOORD1;
    float4 PositionWVP  : TEXCOORD2;
    float4 PositionWRP  : TEXCOORD3;
};

Output VS(Input input)
{
    Output output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;
    output.PositionWV = mul(input.Position, WorldView);
    output.PositionWVP = output.Position;
    output.PositionWRP = mul(input.Position, WorldReflectionProjection);

    return output;
}
