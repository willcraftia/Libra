#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float2 FocalLength                      : packoffset(c0);

    float  Strength                         : packoffset(c1.x);
    float  Attenuation                      : packoffset(c1.y);
    float  Radius                           : packoffset(c1.z);
    float  FarClipDistance                  : packoffset(c1.w);

    float2 RandomOffset                     : packoffset(c2);
    float  SampleCount                      : packoffset(c2.z);

    float4 SampleSphere[MAX_SAMPLE_COUNT]   : packoffset(c3);
};

// �@���}�b�v�� _SNORM �t�H�[�}�b�g�B
Texture2D<float>  LinearDepthMap    : register(t0);
Texture2D<float3> NormalMap         : register(t1);
Texture2D<float3> RandomNormalMap   : register(t2);

SamplerState LinearDepthMapSampler  : register(s0);
SamplerState NormalMapSampler       : register(s1);
SamplerState RandomNormalMapSampler : register(s2);

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 ViewRay  : TEXCOORD1;
};

VSOutput VS(uint id : SV_VertexID)
{
    VSOutput output;

    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    output.ViewRay = float3(output.Position.xy / FocalLength, 1);

    return output;
}

float4 PS(VSOutput input) : SV_Target
{
    float2 texCoord = input.TexCoord;
    float3 viewRay = input.ViewRay;

    // ���ݑΏۂƂ���ʒu�ł̖@���Ɛ[�x�B
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);

    // ���N���b�v�ʂ̏����B
    if (FarClipDistance <= depth)
    {
        return float4(1, 0, 0, 0);
    }

    // �����_���ȃ��C���Z�o���邽�߂̖@���B
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    float3 normal = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    float3 position = viewRay * depth;

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // �T���v���̍��W�����肷�邽�߂̃��C�B
        float3 ray = SampleSphere[i].xyz;

        // �����_���@���Ƃ̔��˂ɂ�胉���_�����B
        ray = reflect(ray, randomNormal);

        // �ʏ�̔������Ɏ��߂邽�߁A�ʂ̖@���Ƃ̓��� 0 �����͔��]�B
        ray *= sign(dot(ray, normal));

        // �T���v���ʒu
        float3 samplePosition = position + ray * Radius;

        // �T���v�� �e�N�Z���ʒu
        float2 sampleTexCoord = samplePosition.xy / samplePosition.z;
        sampleTexCoord = sampleTexCoord * FocalLength * float2(0.5, -0.5) + float2(0.5, 0.5);

        // �T���v���ʒu�̐[�x�B
        float sampleDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, sampleTexCoord, 0);

        // �T���v���ʒu�ɑΉ�����[�x���T���v���_�����ɂ���ꍇ�A
        // �[�x���������̂̓T���v���_���Օ����Ȃ��B
        if (samplePosition.z < sampleDepth)
            continue;

        // �[�x���B
        float deltaDepth = depth - sampleDepth;

        // �[�x�������a�𒴂���ꍇ�͊��S�Ɍ����ƌ��􂷁B
        if (Radius < abs(deltaDepth))
            continue;

        float occlusion = 1.0;

        // ���ꕽ�ʂł�����ɕǓx��������B
        float3 occluderNormal = NormalMap.SampleLevel(NormalMapSampler, sampleTexCoord, 0);
        occluderNormal = normalize(occluderNormal);
        occlusion = 1.0f - (dot(occluderNormal, normal) + 1) * 0.5;

        // ���C�̒����ɉ����ĕǓx�������B
        occlusion *= saturate((Radius - Attenuation * deltaDepth) / Radius);

        // �ΏۂƂ����Ǖ��ɂ�����Օ��x�𑫂��B
        totalOcclusion += occlusion;
    }

    // �T���v�����ŏ��Z���AStrength �ŋ����𒲐��B
    float ao = 1.0f - totalOcclusion / SampleCount * Strength;

    return float4(ao, 0, 0, 0);
}
