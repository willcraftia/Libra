// �I���W�i���͉��L�T�C�g:
// http://www.gamerendering.com/2009/01/14/ssao/
//
// ��L�V�F�[�_�Ƃ̍��ق͈ȉ�:
// �E�[�x�}�b�v�͐��`�[�x�}�b�v (�I���W�i���͎ˉe�[�x�}�b�v)�B
// �E����̃����_���_�̓V�F�[�_�O���Ő��� (�I���W�i���̓n�[�h�R�[�h)�B
// �EFalloff �ɂ��[�x���ق̒��������Ȃ� (�P����r�ŏ\���Ɣ��f)�B
// �E�Ǖ��܂ł̋����ɂ�錸�������̒ǉ��B

#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float2 FocalLength                      : packoffset(c0);
    float  SampleCount                      : packoffset(c0.z);

    float  Strength                         : packoffset(c1.x);
    float  Attenuation                      : packoffset(c1.y);
    float  Radius                           : packoffset(c1.z);
    float  FarClipDistance                  : packoffset(c1.w);

    float4 SampleSphere[MAX_SAMPLE_COUNT]   : packoffset(c2);
};

struct Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 ViewRay  : TEXCOORD1;
};

Output VS(uint id : SV_VertexID)
{
    Output output;

    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    output.ViewRay = float3(output.Position.xy / FocalLength, 1);

    return output;
}
