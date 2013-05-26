cbuffer PerObject : register(b0)
{
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
    float4 PositionWVP  : TEXCOORD1;
    float4 PositionWRP  : TEXCOORD2;
};

Output VS(Input input)
{
    Output output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;
    output.PositionWVP = output.Position;
    output.PositionWRP = mul(input.Position, WorldReflectionProjection);

    return output;
}
