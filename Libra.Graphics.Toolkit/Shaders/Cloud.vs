cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
};

struct Input
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

struct Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

Output VS(Input input)
{
    Output output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;

    return output;
}
