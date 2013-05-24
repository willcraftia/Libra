// Xbox LIVE Indie Games - Particles 3D ���ڐA�B
// http://xbox.create.msdn.com/en-US/education/catalog/sample/particle_3d

cbuffer PerObject : register(b0)
{
    float  Duration             : packoffset(c0);
    float  DurationRandomness   : packoffset(c0.y);

    float3 Gravity              : packoffset(c1);
    float  EndVelocity          : packoffset(c1.w);

    float4 MinColor             : packoffset(c2);
    float4 MaxColor             : packoffset(c3);

    float2 RotateSpeed          : packoffset(c4);

    float2 StartSize            : packoffset(c5);
    float2 EndSize              : packoffset(c5.z);
};

cbuffer PerCamera : register(b1)
{
    float4x4 ViewProjection     : packoffset(c0);
    float4x4 Projection         : packoffset(c4);
    float2   ViewportScale      : packoffset(c8);
};

cbuffer PerFrame : register(b2)
{
    float CurrentTime;
};

struct Input
{
    // D3D11 �ł� float2 �� �p�b�N�^ Short2 ���󂯎�鎖���ł��Ȃ��B
    // int2 (�Ȃ����� vector<int, 2>) �Ȃ�ΐ������󂯎���B
    // �Ȃ��AD3DX �ɂ͕ϊ��p�� D3DX_R16G16_SINT_to_INT2 �֐�������B
    int2   Corner   : CORNER;
    float3 Position : POSITION;
    float3 Velocity : VELOCITY;
    float4 Random   : RANDOM;
    float  Time     : TIME;
};

struct Output
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
    float2 TexCoord : TEXCOORD;
};

float4 ComputeParticlePosition(float3 position, float3 velocity, float age, float normalizedAge)
{
    float startVelocity = length(velocity);

    float endVelocity = startVelocity * EndVelocity;

    float velocityIntegral = startVelocity * normalizedAge +
                             (endVelocity - startVelocity) * normalizedAge * normalizedAge / 2;

    position += normalize(velocity) * velocityIntegral * Duration;

    position += Gravity * age * normalizedAge;

    return mul(float4(position, 1), ViewProjection);
}

float ComputeParticleSize(float randomValue, float normalizedAge)
{
    float startSize = lerp(StartSize.x, StartSize.y, randomValue);
    float endSize = lerp(EndSize.x, EndSize.y, randomValue);

    float size = lerp(startSize, endSize, normalizedAge);

    return size * Projection._m11;
}

float4 ComputeParticleColor(float4 projectedPosition, float randomValue, float normalizedAge)
{
    float4 color = lerp(MinColor, MaxColor, randomValue);

    color.a *= normalizedAge * (1-normalizedAge) * (1-normalizedAge) * 6.7;

    return color;
}

float2x2 ComputeParticleRotation(float randomValue, float age)
{
    float rotateSpeed = lerp(RotateSpeed.x, RotateSpeed.y, randomValue);

    float rotation = rotateSpeed * age;

    float c = cos(rotation);
    float s = sin(rotation);

    return float2x2(c, -s, s, c);
}

Output VS(Input input)
{
    Output output;

    float age = CurrentTime - input.Time;

    age *= 1 + input.Random.x * DurationRandomness;

    float normalizedAge = saturate(age / Duration);

    output.Position = ComputeParticlePosition(input.Position, input.Velocity, age, normalizedAge);

    float size = ComputeParticleSize(input.Random.y, normalizedAge);
    float2x2 rotation = ComputeParticleRotation(input.Random.w, age);

    output.Position.xy += mul(input.Corner, rotation) * size * ViewportScale;

    output.Color = ComputeParticleColor(output.Position, input.Random.z, normalizedAge);
    output.TexCoord = ((float2) input.Corner + 1.0f) / 2.0f;

    return output;
}
