cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
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
    output.Normal = mul(float4(input.Normal, 1), World).xyz;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    // �@��: [-1, 1] ���� [0, 1] �֕ϊ����Đݒ�B
    float3 normal = normalize(input.Normal) * 0.5f + 0.5f;

    return float4(normal, 1);
}
