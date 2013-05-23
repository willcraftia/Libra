cbuffer Parameters : register(b0)
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
    float4 Position             : SV_Position;
    float2 TexCoord             : TEXCOORD0;
    float4 ReflectionPosition   : TEXCOORD1;
    float4 RefractionPosition   : TEXCOORD2;
};

Output VS(Input input)
{
    Output output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.TexCoord = input.TexCoord;
    output.ReflectionPosition = mul(input.Position, WorldReflectionProjection);
    output.RefractionPosition = output.Position;

    return output;
}
