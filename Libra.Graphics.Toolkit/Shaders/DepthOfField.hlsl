cbuffer Parameters : register(b0)
{
    float    FocusScale         : packoffset(c0.x);
    float    FocusDistance      : packoffset(c0.y);
    float4x4 InvertProjection   : packoffset(c1);
};

Texture2D<float4> NormalSceneMap    : register(t0);
Texture2D<float4> BluredSceneMap    : register(t1);
Texture2D<float>  DepthMap          : register(t2);

SamplerState SceneMapSampler        : register(s0);
SamplerState BluredSceneMapSampler  : register(s1);
SamplerState DepthMapSampler        : register(s2);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 normalScene = NormalSceneMap.Sample(SceneMapSampler, texCoord);
    float4 bluredScene = BluredSceneMap.Sample(BluredSceneMapSampler, texCoord);
    float depth = DepthMap.Sample(DepthMapSampler, texCoord);

    // �ˉe�s��̋t�s����|���ăr���[��Ԃɂ�������W���Z�o�B
    float4 depthVS = mul(float4(texCoord, depth, 1), InvertProjection);

    // z ���� (�������])�B
    float sceneDistance = -depthVS.z / depthVS.w;

    // �u���[�W�� (FocusDistance �ɋ߂����� 0�A����Ă������ 1)�B
    float blurFactor = saturate(abs(sceneDistance - FocusDistance) * FocusScale);

    // �ʏ�V�[���ƃu���[�ς݃V�[������`��ԁB
    return lerp(normalScene, bluredScene, blurFactor);
}
