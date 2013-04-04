//-----------------------------------------------------------------------------
// DrawModel.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// XNA �� DrawModel.fx ���V�F�[�_ ���f�� 4.0 �֕ύX�B
// �V���h�E�}�b�v�����V�F�[�_�� Toolkit �N���X���g�p�B
// ���U�V���h�E �}�b�v (VSM: Variance Shadow Map) �ɑΉ��B

cbuffer Parameters : register(b0)
{
    float4x4 World          : packoffset(c0);
    float4x4 View           : packoffset(c4);
    float4x4 Projection     : packoffset(c8);
    float4x4 LightViewProj  : packoffset(c12);
    float3   LightDirection : packoffset(c16);
    float    DepthBias      : packoffset(c16.w);
    float4   AmbientColor   : packoffset(c17);
};

Texture2D<float4> Texture           : register(t0);
Texture2D<float>  BasicShadowMap    : register(t1);
Texture2D<float2> VarianceShadowMap : register(t1);

SamplerState TextureSampler     : register(s0);
SamplerState ShadowMapSampler   : register(s1);

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 WorldPos : TEXCOORD2;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    float4x4 WorldViewProj = mul(mul(World, View), Projection);

    output.Position = mul(input.Position, WorldViewProj);
    output.Normal =  normalize(mul(float4(input.Normal, 0), World)).xyz;
    output.TexCoord = input.TexCoord;

    output.WorldPos = mul(input.Position, World);

    return output;
}

float4 BasicPS(VSOutput input) : SV_Target0
{ 
    float4 diffuseColor = Texture.Sample(TextureSampler, input.TexCoord);

    float diffuseIntensity = saturate(dot(LightDirection, input.Normal));
    float4 diffuse = diffuseIntensity * diffuseColor + AmbientColor;

    float4 lightingPosition = mul(input.WorldPos, LightViewProj);

    float2 shadowTexCoord = 0.5 * lightingPosition.xy / lightingPosition.w + float2( 0.5, 0.5 );
    shadowTexCoord.y = 1.0f - shadowTexCoord.y;

    float shadowdepth = BasicShadowMap.Sample(ShadowMapSampler, shadowTexCoord);

    float ourdepth = (lightingPosition.z / lightingPosition.w) - DepthBias;

    if (shadowdepth < ourdepth)
    {
        diffuse *= float4(0.5,0.5,0.5,0);
    };

    return diffuse;
}

float TestVSM(float4 position, float2 shadowTexCoord)
{
    float2 moments = VarianceShadowMap.Sample(ShadowMapSampler, shadowTexCoord);

    float Ex = moments.x;
    float E_x2 = moments.y;
    float Vx = E_x2 - Ex * Ex;
    Vx = min(1, max(0, Vx + 0.00001f));
    float t = position.z / position.w - DepthBias;
    float tMinusM = t - Ex;
    float p = Vx / (Vx + tMinusM * tMinusM);

    // �`�F�r�V�F�t�̕s�����ɂ�� t > Ex �� p ���L���B
    // t <= Ex �ł� p = 1�A�܂�A�e���Ȃ��B
    return saturate(max(p, t <= Ex));
}

float4 VariancePS(VSOutput input) : SV_Target0
{ 
    float4 diffuseColor = Texture.Sample(TextureSampler, input.TexCoord);

    float diffuseIntensity = saturate(dot(LightDirection, input.Normal));
    float4 diffuse = diffuseIntensity * diffuseColor + AmbientColor;

    float4 lightingPosition = mul(input.WorldPos, LightViewProj);

    float2 shadowTexCoord = 0.5 * lightingPosition.xy / lightingPosition.w + float2( 0.5, 0.5 );
    shadowTexCoord.y = 1.0f - shadowTexCoord.y;

    float shadow = TestVSM(lightingPosition, shadowTexCoord);

    // �ł��e�ȕ����� 0.5 �ɂ��邽�߂̒����B
    shadow *= 0.5f;
    shadow += 0.5f;

    diffuse *= shadow;

    return diffuse;
}
