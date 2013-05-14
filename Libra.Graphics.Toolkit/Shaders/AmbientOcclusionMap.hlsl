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

// ポストプロセス規約による定義 (シェーダ内未使用)。
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

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
        depthNormal = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord);
    }
    else
    {
        depthNormal.x = DepthMap.Sample(DepthMapSampler, texCoord);
        depthNormal.yzw = NormalMap.Sample(NormalMapSampler, texCoord);
    }

    depthNormal.yzw = normalize(depthNormal.yzw * 2.0f - 1.0f);

    return depthNormal;
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // ランダムなレイを算出するための法線。
    float3 randomNormal = RandomNormalMap.Sample(RandomNormalMapSampler, texCoord + RandomOffset);

    // 現在対象とする位置での法線と深度。
    float4 depthNormal = SampleDepthNormal(texCoord);
    float depth = depthNormal.x;
    float3 normal = depthNormal.yzw;

    // 遠方である程にサンプリングの半径を小さくする。
    float adjustedRadius = Radius * (1.0f - depth);

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // サンプルの座標を決定するためのレイ。
        float3 ray = adjustedRadius * reflect(SampleSphere[i], randomNormal);

        // レイを半球内に収めるため sign で全て正へ。
        float3 direction = sign(dot(ray, normal)) * ray;

        // サンプルの座標。
        float2 occluderTexCoord = texCoord + direction.xy;

        // サンプルの法線と深度。
        float4 occluderNormalDepth = SampleDepthNormal(occluderTexCoord);
        float occluderDepth = occluderNormalDepth.x;
        float3 occluderNormal = occluderNormalDepth.yzw;

        // 深度差。
        // deltaDepth < 0 は、サンプルがより奥にある状態。
        float deltaDepth = depth - occluderDepth;

        // 法線のなす角。
        float dotNormals = dot(occluderNormal, normal);

        // 法線のなす角が大きい程に影響が大きい。
        float occlustion = 1.0f - (dotNormals * 0.5f + 0.5f);

        // 深度差が Falloff 以下ならば法線の影響が無いものとする。
        occlustion *= step(Falloff, deltaDepth);

        // [Falloff, Strength] の間で深度差による影響の度合いを変える。
        // より深度差が小さい程に影響が大きく、より深度差が大きい程に影響が小さい。
        occlustion *= (1.0f - smoothstep(Falloff, Strength, deltaDepth));

        // このサンプルでの遮蔽の度合いを足す。
        totalOcclusion += occlustion;
    }

    // サンプル数で除算し、TotalStrength で強さを調整。
    float ao = 1.0f - totalOcclusion / SampleCount * TotalStrength;
    return float4(ao, 0, 0, 0);
}
