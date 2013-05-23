cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection    : packoffset(c0);
    float4x4 WorldView              : packoffset(c4);
    // ���`�[�x��p���Ă���ꍇ�A�[�x 1 �ɑ΂��閧�x��\���B
    float    Density                : packoffset(c8);
};

struct Output
{
    float4 Position     : SV_Position;
    float4 PositionWVP  : POSITION_WVP;
};

Output VS(float4 position : SV_Position)
{
    Output output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWVP = output.Position;

    return output;
}
