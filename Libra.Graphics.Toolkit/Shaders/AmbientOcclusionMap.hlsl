#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float  TotalStrength            : packoffset(c0.x);
    float  Strength                 : packoffset(c0.y);
    float  Falloff                  : packoffset(c0.z);
    float  Radius                   : packoffset(c0.w);

    float2 RandomOffset             : packoffset(c1);
    int    SampleCount              : packoffset(c1.z);
    bool   DepthNormalMapEnabled    : packoffset(c1.w);

    float3 SampleSphere             : packoffset(c2);
};

// �|�X�g�v���Z�X�K��ɂ���` (�V�F�[�_�����g�p)�B
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

Texture2D<float>  DepthMap          : register(t1);
Texture2D<float3> NormalMap         : register(t2);
Texture2D<float3> RandomNormalMap   : register(t3);
Texture2D<float4> DepthNormalMap    : register(t4);

SamplerState DepthMapSampler        : register(s1);
SamplerState NormalMapSampler       : register(s2);
SamplerState RandomNormalMapSampler : register(s3);
SamplerState DepthNormalMapSampler  : register(s4);

float4 SampleDepthNormal(float2 texCoord)
{
    float4 depthNormal;

    if (DepthNormalMapEnabled)
    {
        depthNormal = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord);
    }
    else
    {
        depthNormal.x = DepthMap.Sample(DepthMapSampler, texCoord);
        depthNormal.yzw = NormalMap.Sample(NormalMapSampler, texCoord);
    }

    depthNormal.yzw = normalize(depthNormal.yzw * 2.0f - 1.0f);

    return depthNormal;
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // �����_���ȃ��C���Z�o���邽�߂̖@���B
    float3 randomNormal = RandomNormalMap.Sample(RandomNormalMapSampler, texCoord + RandomOffset);

    // ���ݑΏۂƂ���ʒu�ł̖@���Ɛ[�x�B
    float4 depthNormal = SampleDepthNormal(texCoord);
    float depth = depthNormal.x;
    float3 normal = depthNormal.yzw;

    // �����ł�����ɃT���v�����O�̔��a������������B
    float adjustedRadius = Radius * (1.0f - depth);

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // �T���v���̍��W�����肷�邽�߂̃��C�B
        float3 ray = adjustedRadius * reflect(SampleSphere[i], randomNormal);

        // ���C�𔼋����Ɏ��߂邽�� sign �őS�Đ��ցB
        float3 direction = sign(dot(ray, normal)) * ray;

        // �T���v���̍��W�B
        float2 occluderTexCoord = texCoord + direction.xy;

        // �T���v���̖@���Ɛ[�x�B
        float4 occluderNormalDepth = SampleDepthNormal(occluderTexCoord);
        float occluderDepth = occluderNormalDepth.x;
        float3 occluderNormal = occluderNormalDepth.yzw;

        // �[�x���B
        // deltaDepth < 0 �́A�T���v������艜�ɂ����ԁB
        float deltaDepth = depth - occluderDepth;

        // �@���̂Ȃ��p�B
        float dotNormals = dot(occluderNormal, normal);

        // �@���̂Ȃ��p���傫�����ɉe�����傫���B
        float occlustion = 1.0f - (dotNormals * 0.5f + 0.5f);

        // �[�x���� Falloff �ȉ��Ȃ�Ζ@���̉e�����������̂Ƃ���B
        occlustion *= step(Falloff, deltaDepth);

        // [Falloff, Strength] �̊ԂŐ[�x���ɂ��e���̓x������ς���B
        // ���[�x�������������ɉe�����傫���A���[�x�����傫�����ɉe�����������B
        occlustion *= (1.0f - smoothstep(Falloff, Strength, deltaDepth));

        // ���̃T���v���ł̎Օ��̓x�����𑫂��B
        totalOcclusion += occlustion;
    }

    // �T���v�����ŏ��Z���ATotalStrength �ŋ����𒲐��B
    float ao = 1.0f - totalOcclusion / SampleCount * TotalStrength;
    return float4(ao, 0, 0, 0);
}
