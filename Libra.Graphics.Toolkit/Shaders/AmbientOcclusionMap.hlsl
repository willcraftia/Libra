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

// SpriteBatch でスプライト エフェクトとして利用するためのダミー定義。
// スプライト エフェクトとして用いる場合には、
// SpriteBatch のソース テクスチャへ LinearDepthMap や NormalMap などを
// ダミーとして指定する。
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

// 法線マップは _SNORM フォーマット。
Texture2D<float>  LinearDepthMap    : register(t1);
Texture2D<float3> NormalMap         : register(t2);
Texture2D<float3> RandomNormalMap   : register(t3);

SamplerState LinearDepthMapSampler  : register(s1);
SamplerState NormalMapSampler       : register(s2);
SamplerState RandomNormalMapSampler : register(s3);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // ランダムなレイを算出するための法線。
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    // 現在対象とする位置での法線と深度。
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);
    float3 normal = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    // 遠クリップ面の除去。
    if (FarClipDistance <= depth)
    {
        return float4(1, 0, 0, 0);
    }

    float3 position = float3((texCoord - float2(0.5, 0.5)) * float2(2.0, -2.0) * depth, depth);

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // サンプルの座標を決定するためのレイ。
        float3 ray = SampleSphere[i];

        // ランダム法線との反射によりランダム化。
        ray = reflect(ray, randomNormal);

        // 面上の半球内に収めるため、面の法線との内積 0 未満は反転。
        ray *= sign(dot(ray, normal));

        // サンプル位置
        float3 occluderPosition = position + ray * Radius;

        // サンプル テクセル位置
        float2 occluderTexCoord = occluderPosition.xy / occluderPosition.z;
        occluderTexCoord = occluderTexCoord * float2(0.5, -0.5) + float2(0.5, 0.5);

        // 閉塞物候補の深度。
        float occluderDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, occluderTexCoord, 0);

        // 深度差。
        // deltaDepth < 0: 閉塞物候補が基点よりも奥。
        float deltaDepth = depth - occluderDepth;

        // 閉塞物候補が基点と同じ深度、あるいは、奥にある場合、
        // その候補は閉塞物ではないとみなす。
        // これは、手前にある面には影を落とさず、
        // その背景へ影を落とすための調整。
        if (deltaDepth <= 0)
            continue;

        // 同一平面である程に閉塞度を下げる。
        float3 occluderNormal = NormalMap.SampleLevel(NormalMapSampler, occluderTexCoord, 0);
        float occlusion = 1.0f - (dot(occluderNormal, normal) + 1) * 0.5;

        // レイの長さに応じて閉塞度を減衰。
        occlusion *= saturate((Radius - Attenuation * deltaDepth) / Radius);

        // 対象とした閉塞物における遮蔽度を足す。
        totalOcclusion += occlusion;
    }

    // サンプル数で除算し、Strength で強さを調整。
    float ao = 1.0f - totalOcclusion / SampleCount * Strength;

    return float4(ao, 0, 0, 0);
}
