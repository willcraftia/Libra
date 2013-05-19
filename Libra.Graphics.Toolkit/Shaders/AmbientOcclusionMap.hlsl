// オリジナルは下記サイト:
// http://www.gamerendering.com/2009/01/14/ssao/
//
// 上記シェーダとの差異は以下:
// ・深度マップは線形深度マップ (オリジナルは射影深度マップ)。
// ・球状のランダム点はシェーダ外部で生成 (オリジナルはハードコード)。
// ・Falloff による深度差異の調整をしない (単純比較で十分と判断)。
// ・閉塞物までの距離による減衰処理の追加。

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

// 法線マップは _SNORM フォーマット。
Texture2D<float>  LinearDepthMap    : register(t0);
Texture2D<float3> NormalMap         : register(t1);
Texture2D<float3> RandomNormalMap   : register(t2);

SamplerState LinearDepthMapSampler  : register(s0);
SamplerState NormalMapSampler       : register(s1);

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
    // 深度。
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, input.TexCoord, 0);

    // 遠クリップ面以降の除去。
    if (FarClipDistance <= depth)
    {
        return float4(1, 0, 0, 0);
    }

    // 座標 (ビュー空間)
    float3 position = input.ViewRay * depth;

    // 法線。
    float3 normal = NormalMap.SampleLevel(NormalMapSampler, input.TexCoord, 0);

    // ランダム法線。
    int3 randomLocation = int3((int) input.Position.x & 63, (int) input.Position.y & 63, 0);
    float3 randomNormal = RandomNormalMap.Load(randomLocation);
    randomNormal = normalize(randomNormal);

    float totalOcclusion = 0;
    for (int i = 0; i < SampleCount; i++)
    {
        // サンプル点を決定するためのレイ。
        float3 ray = SampleSphere[i].xyz;

        // ランダム法線との反射によりランダム化。
        ray = reflect(ray, randomNormal);

        // 面上の半球内に収めるため、面の法線との内積 0 未満は反転。
        ray *= sign(dot(ray, normal));

        // サンプル点
        float3 samplePosition = position + ray * Radius;

        // サンプル テクセル位置
        float2 sampleTexCoord = samplePosition.xy / samplePosition.z;
        sampleTexCoord = sampleTexCoord * FocalLength * float2(0.5, -0.5) + float2(0.5, 0.5);

        // サンプル テクセル位置の深度。
        float sampleDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, sampleTexCoord, 0);

        // サンプル点よりも奥にある場合、
        // 深度が示す物体はサンプル点を遮蔽しないものと見做す。
        if (samplePosition.z < sampleDepth)
            continue;

        // 遮蔽物の位置。
        float3 occluderPosition = float3(samplePosition.xy, sampleDepth);

        // 遮蔽物までの距離が半径を超えるならば完全に減衰と見做す。
        float d = distance(position, occluderPosition);
        if (Radius < d)
            continue;

        float occlusion = 1.0;

        // 遮蔽物までの距離に応じて閉塞度を減衰。
        occlusion *= saturate((Radius - Attenuation * d) / Radius);

        // 法線が同じである程に閉塞度を下げる。
        // 法線による調整は、主に、連続する平面における期待しない遮蔽の回避が目的。
        // 実際には、法線が同一であっても大きな遮蔽である状態も有り得る。
        // なお、基本はカメラからの深度による遮蔽であるため、
        // 法線同士の内積が 0 未満である状態は無いものと見做せる。
        float3 occluderNormal = NormalMap.SampleLevel(NormalMapSampler, sampleTexCoord, 0);
        occluderNormal = normalize(occluderNormal);
        occlusion = 1.0f - dot(occluderNormal, normal);

        // 対象とした閉塞物における遮蔽度を足す。
        totalOcclusion += occlusion;
    }

    // サンプル数で除算し、Strength で強さを調整。
    float ao = 1.0f - totalOcclusion / SampleCount * Strength;

    return float4(ao, 0, 0, 0);
}
