cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
};

float4 VS(float4 position : SV_Position) : SV_Position
{
    return mul(position, WorldViewProjection);
}
