// 参考元:
// http://www.gamerendering.com/2009/01/14/ssao/

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

// SpriteBatch でスプライト エフェクトとして利用するためのダミー定義。
// スプライト エフェクトとして用いる場合には、
// SpriteBatch のソース テクスチャへ DepthMap や DepthNormalMap などを
// ダミーとして指定する。
Texture2D<float4> Texture           : register(t0);
SamplerState TextureSampler         : register(s0);

// 法線マップは _SNORM フォーマット。
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
        depthNormal = DepthNormalMap.SampleLevel(DepthNormalMapSampler, texCoord, 0);
    }
    else
    {
        depthNormal.x = DepthMap.SampleLevel(DepthMapSampler, texCoord, 0);
        depthNormal.yzw = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);
    }

    depthNormal.yzw = normalize(depthNormal.yzw);

    return depthNormal;
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    // ランダムなレイを算出するための法線。
    float3 randomNormal = RandomNormalMap.SampleLevel(RandomNormalMapSampler, texCoord * RandomOffset, 0);
    randomNormal = normalize(randomNormal);

    // 現在対象とする位置での法線と深度。
    float4 depthNormal = SampleDepthNormal(texCoord);
    float depth = depthNormal.x;
    float3 normal = depthNormal.yzw;

    // 遠方である程にサンプリングの半径を小さくする。
    float adjustedRadius = Radius / depth;

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // サンプルの座標を決定するためのレイ。
        float3 ray = reflect(SampleSphere[i], randomNormal);

        // レイを半球内に収めるため、内積 0 未満はレイを反転。
        ray *= sign(dot(ray, normal));

        // 閉塞物候補 (サンプル) の座標。
        float2 occluderTexCoord = texCoord + ray.xy * adjustedRadius;

        // 閉塞物候補の法線と深度。
        float4 occluderNormalDepth = SampleDepthNormal(occluderTexCoord);
        float occluderDepth = occluderNormalDepth.x;
        float3 occluderNormal = occluderNormalDepth.yzw;

        // 深度差。
        // deltaDepth < 0: 閉塞物候補が基点よりも奥。
        float deltaDepth = depth - occluderDepth;

        // 深度差が Falloff 以下ならば閉塞無しとする。
        // つまり、閉塞物候補が基点よりも奥にある場合、
        // その候補は閉塞物ではないとみなす。
        // これは主に、手前にある面には影を落とさず、
        // その背景へ影を落とすための調整であると思われる。
        float occlusion = step(Falloff, deltaDepth);

        // 深度差が半球内に収まっていない場合は閉塞物ではないとみなす。
        // TODO
        // ビュー空間での判定でなければ意味が無いと思われる。
//        occlusion *= (deltaDepth < adjustedRadius);

        // 法線のなす角による閉塞度合いの決定。
        // 閉塞物候補が閉塞物であると仮定した場合 (凹状態と仮定した場合)、
        // 内積 -1 は真逆に隣接する状態であり完全な閉塞。
        // 内積 1 は同一平面であり閉塞なし。
        // TODO
        // 実際には、ここで閉塞物であるとの仮定が成立しない。
        // つまり、凸状態も閉塞とみなす事になるため、
        // 期待する閉塞度を得られない。
        occlusion *= 1.0f - dot(occluderNormal, normal);

        // [Falloff, Strength] の間で深度差による影響の度合いを変える。
        occlusion *= (1.0f - smoothstep(Falloff, Strength, deltaDepth));

        // 対象とした閉塞物における遮蔽度を足す。
        totalOcclusion += occlusion;
    }

    // サンプル数で除算し、TotalStrength で強さを調整。
    float ao = 1.0f - totalOcclusion / SampleCount * TotalStrength;
    return float4(ao, 0, 0, 0);
}
