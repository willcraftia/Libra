#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float  Strength         : packoffset(c0.x);
    float  Attenuation      : packoffset(c0.y);
    float  Radius           : packoffset(c0.z);
    float  FarClipDistance  : packoffset(c0.w);

    float2 RandomOffset     : packoffset(c1);
    float  SampleCount      : packoffset(c1.z);

    float3 SampleSphere     : packoffset(c2);
};

// SpriteBatch �ŃX�v���C�g �G�t�F�N�g�Ƃ��ė��p���邽�߂̃_�~�[��`�B
// �X�v���C�g �G�t�F�N�g�Ƃ��ėp����ꍇ�ɂ́A
// SpriteBatch �̃\�[�X �e�N�X�`���� LinearDepthMap �� NormalMap �Ȃǂ�
// �_�~�[�Ƃ��Ďw�肷��B
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

// �@���}�b�v�� _SNORM �t�H�[�}�b�g�B
Texture2D<float>  LinearDepthMap    : register(t1);
Texture2D<float3> NormalMap         : register(t2);
Texture2D<float3> RandomNormalMap   : register(t3);

SamplerState LinearDepthMapSampler  : register(s1);
SamplerState NormalMapSampler       : register(s2);
SamplerState RandomNormalMapSampler : register(s3);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // �����_���ȃ��C���Z�o���邽�߂̖@���B
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    // ���ݑΏۂƂ���ʒu�ł̖@���Ɛ[�x�B
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);
    float3 normal = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    // ���N���b�v�ʂ̏����B
    if (FarClipDistance <= depth)
    {
        return float4(1, 0, 0, 0);
    }

    float3 position = float3((texCoord - float2(0.5, 0.5)) * float2(2.0, -2.0) * depth, depth);

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // �T���v���̍��W�����肷�邽�߂̃��C�B
        float3 ray = SampleSphere[i];

        // �����_���@���Ƃ̔��˂ɂ�胉���_�����B
        ray = reflect(ray, randomNormal);

        // �ʏ�̔������Ɏ��߂邽�߁A�ʂ̖@���Ƃ̓��� 0 �����͔��]�B
        ray *= sign(dot(ray, normal));

        // �T���v���ʒu
        float3 occluderPosition = position + ray * Radius;

        // �T���v�� �e�N�Z���ʒu
        float2 occluderTexCoord = occluderPosition.xy / occluderPosition.z;
        occluderTexCoord = occluderTexCoord * float2(0.5, -0.5) + float2(0.5, 0.5);

        // �Ǖ����̐[�x�B
        float occluderDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, occluderTexCoord, 0);

        // �[�x���B
        // deltaDepth < 0: �Ǖ���₪��_�������B
        float deltaDepth = depth - occluderDepth;

        // �Ǖ���₪��_�Ɠ����[�x�A���邢�́A���ɂ���ꍇ�A
        // ���̌��͕Ǖ��ł͂Ȃ��Ƃ݂Ȃ��B
        // ����́A��O�ɂ���ʂɂ͉e�𗎂Ƃ����A
        // ���̔w�i�։e�𗎂Ƃ����߂̒����B
        if (deltaDepth <= 0)
            continue;

        // ���ꕽ�ʂł�����ɕǓx��������B
        float3 occluderNormal = NormalMap.SampleLevel(NormalMapSampler, occluderTexCoord, 0);
        float occlusion = 1.0f - (dot(occluderNormal, normal) + 1) * 0.5;

        // ���C�̒����ɉ����ĕǓx�������B
        occlusion *= saturate((Radius - Attenuation * deltaDepth) / Radius);

        // �ΏۂƂ����Ǖ��ɂ�����Օ��x�𑫂��B
        totalOcclusion += occlusion;
    }

    // �T���v�����ŏ��Z���AStrength �ŋ����𒲐��B
    float ao = 1.0f - totalOcclusion / SampleCount * Strength;

    return float4(ao, 0, 0, 0);
}
