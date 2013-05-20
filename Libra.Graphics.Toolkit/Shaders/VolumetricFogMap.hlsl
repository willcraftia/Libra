cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection    : packoffset(c0);
    float4x4 WorldView              : packoffset(c4);
    // 線形深度を用いている場合、深度 1 に対する密度を表す。
    float    Density                : packoffset(c8);
};

Texture2D<float> FrontFogDepthMap;
Texture2D<float> BackFogDepthMap;

SamplerState FrontFogDepthMapSampler;
SamplerState BackFogDepthMapSampler;

struct VSOutput
{
    float4 Position     : SV_Position;
    float4 PositionWVP  : POSITION_WVP;
};

VSOutput VS(float4 position : SV_Position)
{
    VSOutput output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWVP = output.Position;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    float2 texCoord = input.PositionWVP.xy / input.PositionWVP.w * float2(0.5, -0.5) + float2(0.5, 0.5);

    float front = FrontFogDepthMap.Sample(FrontFogDepthMapSampler, texCoord);
    float back = BackFogDepthMap.Sample(BackFogDepthMapSampler, texCoord);

    float level = saturate((front - back) * Density);

    return float4(level, 0, 0, 0);
}
