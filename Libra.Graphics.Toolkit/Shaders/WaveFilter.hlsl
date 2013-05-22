cbuffer PerShader : register(b0)
{
    float  Stiffness;

    // xy = �e�N�Z�� �I�t�Z�b�g
    float2 Kernel[4];
}

cbuffer PerFrame : register(b1)
{
    float2 NewWavePosition;
    float  NewWaveRadius;
    float  NewWaveVeclocity;
};

// TODO
// MRT �ł���Ă݂�H

// �O�t���[���ɂ�����g�}�b�v�B
// x: ����
// y: ���x
// ���Ȃ킿�Ax �݂̂������ꍇ�ɂ͍����}�b�v (heightmap) �ƂȂ�B
Texture2D<float2> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target0
{
    float2 c  = Texture.Sample(TextureSampler, texCoord);
    float2 s0 = Texture.Sample(TextureSampler, texCoord + Kernel[0]);
    float2 s1 = Texture.Sample(TextureSampler, texCoord + Kernel[1]);
    float2 s2 = Texture.Sample(TextureSampler, texCoord + Kernel[2]);
    float2 s3 = Texture.Sample(TextureSampler, texCoord + Kernel[3]);

    // �t�b�N�̖@���B
    float x = s0.x + s1.x + s2.x + s3.x - 4.0 * c.x;
    float f = Stiffness * x;

    float velocity = c.y + f;
    float height = c.x + velocity;

    if (distance(NewWavePosition, texCoord) <= NewWaveRadius)
    {
        velocity += NewWaveVeclocity;
    }

    return float4(height, velocity, 0.0, 0.0);
}
