cbuffer Parameters : register(b0)
{
    float2 EdgeOffset           : packoffset(c0);
    float  EdgeIntensity        : packoffset(c0.z);
    float  DepthThreshold       : packoffset(c1.x);
    float  DepthSensitivity     : packoffset(c1.y);
    float  NormalThreshold      : packoffset(c1.z);
    float  NormalSensitivity    : packoffset(c1.w);
    float3 EdgeColor            : packoffset(c2);
    float  EdgeAttenuation      : packoffset(c2.w);
};

Texture2D<float4> Texture           : register(t0);
Texture2D<float4> DepthNormalMap    : register(t1);

SamplerState TextureSampler         : register(s0);
SamplerState DepthNormalMapSampler  : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 source = Texture.Sample(TextureSampler, texCoord);

    float4 s  = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord);
    float4 s1 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2(-1, -1) * EdgeOffset);
    float4 s2 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2( 1,  1) * EdgeOffset);
    float4 s3 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2(-1,  1) * EdgeOffset);
    float4 s4 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2( 1, -1) * EdgeOffset);

    float4 deltaSample = abs(s1 - s2) + abs(s3 - s4);

    float deltaDepth = deltaSample.x;
    deltaDepth = saturate((deltaDepth - DepthThreshold) * DepthSensitivity);

    float deltaNormal = dot(deltaSample.yzw, 1);
    deltaNormal = saturate((deltaNormal - NormalThreshold) * NormalSensitivity);

    // �Q�l�ɂ��� XNA �T���v���ł� saturate(deltaDepth + deltaNormal)�B
    // �x�����̘a�Ƃ���Ӗ����Ȃ��Ɣ��f���A�ő�l���o�֕ύX�B
    float amount = saturate(max(deltaDepth, deltaNormal));

    // EdgeAttenuation ���z����[�x���猸�����n�߂�B
    // EdgeAttenuation < 1 �Ȃ�ΐ[�x 1 �� amount = 0�B
    // ���Ȃ킿�A���N���b�v�ʂɋ߂Â����ɃG�b�W�Ȃ��ɋ߂Â��B
    // 1 <= EdgeAttenuation �Ȃ�Ό����Ȃ��B
    // �Ȃ��AXNA �T���v���ł͌��������Ȃ��B
    float attenuation = 1 - s.x * (EdgeAttenuation < s.x);

    amount *= EdgeIntensity * attenuation;

    source.rgb = lerp(source.rgb, source.rgb * EdgeColor, saturate(amount));

    return source;
}
