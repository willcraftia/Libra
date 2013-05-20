cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection    : packoffset(c0);
    float4x4 WorldView              : packoffset(c4);
    // ���`�[�x��p���Ă���ꍇ�A�[�x 1 �ɑ΂��閧�x��\���B
    float    Density                : packoffset(c8);
};

Texture2D<float> FrontFogDepthMap   : register(t0);
Texture2D<float> BackFogDepthMap    : register(t1);

SamplerState FrontFogDepthMapSampler    : register(s0);
SamplerState BackFogDepthMapSampler     : register(s1);

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

    float level = saturate((back - front) * Density);

    return float4(level, 0, 0, 0);
}
