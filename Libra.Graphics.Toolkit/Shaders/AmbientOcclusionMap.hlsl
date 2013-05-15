// �Q�l��:
// http://www.gamerendering.com/2009/01/14/ssao/

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

// SpriteBatch �ŃX�v���C�g �G�t�F�N�g�Ƃ��ė��p���邽�߂̃_�~�[��`�B
// �X�v���C�g �G�t�F�N�g�Ƃ��ėp����ꍇ�ɂ́A
// SpriteBatch �̃\�[�X �e�N�X�`���� DepthMap �� DepthNormalMap �Ȃǂ�
// �_�~�[�Ƃ��Ďw�肷��B
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

// �@���}�b�v�� _SNORM �t�H�[�}�b�g�B
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
        depthNormal = DepthNormalMap.SampleLevel(DepthNormalMapSampler, texCoord, 0);
    }
    else
    {
        depthNormal.x = DepthMap.SampleLevel(DepthMapSampler, texCoord, 0);
        depthNormal.yzw = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);
    }

    depthNormal.yzw = normalize(depthNormal.yzw);

    return depthNormal;
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // �����_���ȃ��C���Z�o���邽�߂̖@���B
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    // ���ݑΏۂƂ���ʒu�ł̖@���Ɛ[�x�B
    float4 depthNormal = SampleDepthNormal(texCoord);
    float depth = depthNormal.x;
    float3 normal = depthNormal.yzw;

    // �����ł�����ɃT���v�����O�̔��a������������B
    float adjustedRadius = Radius / depth;

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // �T���v���̍��W�����肷�邽�߂̃��C�B
        float3 ray = reflect(SampleSphere[i], randomNormal);

        // ���C�𔼋����Ɏ��߂邽�߁A���� 0 �����̓��C�𔽓]�B
        ray *= sign(dot(ray, normal));

        // �Ǖ���� (�T���v��) �̍��W�B
        float2 occluderTexCoord = texCoord + ray.xy * adjustedRadius;

        // �Ǖ����̖@���Ɛ[�x�B
        float4 occluderNormalDepth = SampleDepthNormal(occluderTexCoord);
        float occluderDepth = occluderNormalDepth.x;
        float3 occluderNormal = occluderNormalDepth.yzw;

        // �[�x���B
        // deltaDepth < 0: �Ǖ���₪��_�������B
        float deltaDepth = depth - occluderDepth;

        // �[�x���� Falloff �ȉ��Ȃ�Εǖ����Ƃ���B
        // �܂�A�Ǖ���₪��_�������ɂ���ꍇ�A
        // ���̌��͕Ǖ��ł͂Ȃ��Ƃ݂Ȃ��B
        // ����͎�ɁA��O�ɂ���ʂɂ͉e�𗎂Ƃ����A
        // ���̔w�i�։e�𗎂Ƃ����߂̒����ł���Ǝv����B
        float occlusion = step(Falloff, deltaDepth);

        // �[�x�����������Ɏ��܂��Ă��Ȃ��ꍇ�͕Ǖ��ł͂Ȃ��Ƃ݂Ȃ��B
        // TODO
        // �r���[��Ԃł̔���łȂ���ΈӖ��������Ǝv����B
//        occlusion *= (deltaDepth < adjustedRadius);

        // �@���̂Ȃ��p�ɂ��Ǔx�����̌���B
        // �Ǖ���₪�Ǖ��ł���Ɖ��肵���ꍇ (����ԂƉ��肵���ꍇ)�A
        // ���� -1 �͐^�t�ɗאڂ����Ԃł��芮�S�ȕǁB
        // ���� 1 �͓��ꕽ�ʂł���ǂȂ��B
        // TODO
        // ���ۂɂ́A�����ŕǕ��ł���Ƃ̉��肪�������Ȃ��B
        // �܂�A�ʏ�Ԃ��ǂƂ݂Ȃ����ɂȂ邽�߁A
        // ���҂���Ǔx�𓾂��Ȃ��B
        occlusion *= 1.0f - dot(occluderNormal, normal);

        // [Falloff, Strength] �̊ԂŐ[�x���ɂ��e���̓x������ς���B
        occlusion *= (1.0f - smoothstep(Falloff, Strength, deltaDepth));

        // �ΏۂƂ����Ǖ��ɂ�����Օ��x�𑫂��B
        totalOcclusion += occlusion;
    }

    // �T���v�����ŏ��Z���ATotalStrength �ŋ����𒲐��B
    float ao = 1.0f - totalOcclusion / SampleCount * TotalStrength;
    return float4(ao, 0, 0, 0);
}
