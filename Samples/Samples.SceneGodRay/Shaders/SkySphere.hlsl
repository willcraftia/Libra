cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float3 SkyColor;
    // ���_���猩�����z�̕���
    float3 SunDirection;
    float3 SunColor;
    // ���z�̏ꏊ�𔻒肷�邽�߂�臒l (0.999 �ȏオ�Ó�)
    float SunThreshold;
    // 0: ���z��`�悵�Ȃ�
    // 1: ���z��`�悷��
    float SunVisible;
};

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : TEXCOORD0;
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
    amount *= 1 / (1 - SunThreshold);

    // ���z�̐F���u�����h
    color.rgb += SunColor * amount;

    return color;
}
