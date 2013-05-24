cbuffer PerCamera : register(b0)
{
    float2 FocalLength;
};

struct Output
{
    float2 TexCoord : TEXCOORD0;
    float3 ViewRay  : TEXCOORD1;
    float4 Position : SV_Position;
};

Output VS(uint id : SV_VertexID)
{
    Output output;

    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    output.ViewRay = float3(output.Position.xy / FocalLength, 1);

    return output;
}
