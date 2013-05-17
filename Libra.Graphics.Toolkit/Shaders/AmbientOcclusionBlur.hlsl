#define MAX_RADIUS 7
#define MAX_KERNEL_SIZE (MAX_RADIUS * 2 + 1)

cbuffer Parameters : register(b0)
{
    // �[�x�̏d��
    float DepthSigma                : packoffset(c0);
    // �J�[�l�� �T�C�Y
    float KernelSize                : packoffset(c0.y);
    // xy = �e�N�Z�� �I�t�Z�b�g
    // z  = ��Ԃ̏d��
    float3 Kernel[MAX_KERNEL_SIZE]  : packoffset(c1);
};

// �@���}�b�v�� _SNORM �t�H�[�}�b�g�B
Texture2D<float>  Texture           : register(t0);
Texture2D<float>  LinearDepthMap    : register(t1);
Texture2D<float3> NormalMap         : register(t2);

SamplerState TextureSampler         : register(s0);
SamplerState LinearDepthMapSampler  : register(s1);
SamplerState NormalMapSampler       : register(s2);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float centerDepth = LinearDepthMap.Sample(LinearDepthMapSampler, texCoord);

    float4 totalColor = 0;
    float totalWeight = 0;

    for (int i = 0; i < KernelSize; i++)
    {
        float3 kernel = Kernel[i];

        float2 offset = kernel.xy;
        float spaceWeight = kernel.z;

        float2 sampleTexCoord = texCoord + offset;

        float sampleColor = Texture.Sample(TextureSampler, sampleTexCoord);
        float sampleDepth = LinearDepthMap.Sample(LinearDepthMapSampler, sampleTexCoord);

        // �[�x�̍�����[�x�̏d�݂��Z�o�B
        float depthCloseness = abs(centerDepth - sampleDepth);
        float depthWeight = exp(-depthCloseness * depthCloseness / (2 * DepthSigma * DepthSigma));

        // �d�� = ��Ԃ̏d�� * �[�x�̏d��
        float sampleWeight = spaceWeight * depthWeight;

        totalColor += sampleColor * sampleWeight;
        totalWeight += sampleWeight;
    }

    // ���K�����čŏI�I�ȐF������B
    return totalColor / totalWeight;
}
