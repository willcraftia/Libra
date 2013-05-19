#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float2 FocalLength                      : packoffset(c0);

    float  Strength                         : packoffset(c1.x);
    float  Attenuation                      : packoffset(c1.y);
    float  Radius                           : packoffset(c1.z);
    float  FarClipDistance                  : packoffset(c1.w);

    float2 RandomOffset                     : packoffset(c2);
    float  SampleCount                      : packoffset(c2.z);

    float4 SampleSphere[MAX_SAMPLE_COUNT]   : packoffset(c3);
};

// 法線マップは _SNORM フォーマット。
Texture2D<float>  LinearDepthMap    : register(t0);
Texture2D<float3> NormalMap         : register(t1);
Texture2D<float3> RandomNormalMap   : register(t2);

SamplerState LinearDepthMapSampler  : register(s0);
SamplerState NormalMapSampler       : register(s1);
SamplerState RandomNormalMapSampler : register(s2);

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 ViewRay  : TEXCOORD1;
};

VSOutput VS(uint id : SV_VertexID)
{
    VSOutput output;

    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    output.ViewRay = float3(output.Position.xy / FocalLength, 1);

    return output;
}

float4 PS(VSOutput input) : SV_Target
{
    float2 texCoord = input.TexCoord;
    float3 viewRay = input.ViewRay;

    // 現在対象とする位置での法線と深度。
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);

    // 遠クリップ面の除去。
    if (FarClipDistance <= depth)
    {
        return float4(1, 0, 0, 0);
    }

    // ランダムなレイを算出するための法線。
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    float3 normal = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    float3 position = viewRay * depth;

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // サンプルの座標を決定するためのレイ。
        float3 ray = SampleSphere[i].xyz;

        // ランダム法線との反射によりランダム化。
        ray = reflect(ray, randomNormal);

        // 面上の半球内に収めるため、面の法線との内積 0 未満は反転。
        ray *= sign(dot(ray, normal));

        // サンプル位置
        float3 samplePosition = position + ray * Radius;

        // サンプル テクセル位置
        float2 sampleTexCoord = samplePosition.xy / samplePosition.z;
        sampleTexCoord = sampleTexCoord * FocalLength * float2(0.5, -0.5) + float2(0.5, 0.5);

        // サンプル位置の深度。
        float sampleDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, sampleTexCoord, 0);

        // サンプル位置に対応する深度がサンプル点も奥にある場合、
        // 深度が示す物体はサンプル点を遮蔽しない。
        if (samplePosition.z < sampleDepth)
            continue;

        // 深度差。
        float deltaDepth = depth - sampleDepth;

        // 深度差が半径を超える場合は完全に減衰と見做す。
        if (Radius < abs(deltaDepth))
            continue;

        float occlusion = 1.0;

        // 同一平面である程に閉塞度を下げる。
        float3 occluderNormal = NormalMap.SampleLevel(NormalMapSampler, sampleTexCoord, 0);
        occluderNormal = normalize(occluderNormal);
        occlusion = 1.0f - (dot(occluderNormal, normal) + 1) * 0.5;

        // レイの長さに応じて閉塞度を減衰。
        occlusion *= saturate((Radius - Attenuation * deltaDepth) / Radius);

        // 対象とした閉塞物における遮蔽度を足す。
        totalOcclusion += occlusion;
    }

    // サンプル数で除算し、Strength で強さを調整。
    float ao = 1.0f - totalOcclusion / SampleCount * Strength;

    return float4(ao, 0, 0, 0);
}
