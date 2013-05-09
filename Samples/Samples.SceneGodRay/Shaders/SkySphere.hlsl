cbuffer VSParameters : register(b0)
{
    float4x4 WorldViewProjection;
};

cbuffer PSParameters : register(b0)
{
    float3 SkyColor     : packoffset(c0);
    // ���_���猩�����z�̕���
    float3 SunDirection : packoffset(c1);
    float3 SunColor     : packoffset(c2);
    // ���z�̏ꏊ�𔻒肷�邽�߂�臒l (0.999 �ȏオ�Ó�)
    float SunThreshold  : packoffset(c3.x);
    // 0: ���z��`�悵�Ȃ�
    // 1: ���z��`�悷��
    float SunVisible    : packoffset(c3.y);
};

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = input.Normal;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    float4 color = float4(SkyColor, 1);

    // �@�����ǂ̒��x���z�̌����Ɉ�v���Ă��邩���Z�o
    // ���z�̋t������ 0 �Ƃ��Ĕj��
    float amount = saturate(dot(normalize(input.Normal), SunDirection)) * SunVisible;

    // SunThreshold ���瑾�z�͈̔͂��Z�o
    amount -= SunThreshold;
    amount = saturate(amount);
    amount /= (1 - SunThreshold);

    // ���z�̐F���u�����h
    color.rgb += SunColor * amount;

    return color;
}