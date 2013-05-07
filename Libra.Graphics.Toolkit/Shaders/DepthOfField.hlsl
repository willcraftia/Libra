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

    // 射影行列の逆行列を掛けてビュー空間における座標を算出。
    float4 depthVS = mul(float4(texCoord, depth, 1), InvertProjection);

    // z 距離 (符号反転)。
    float sceneDistance = -depthVS.z / depthVS.w;

    // ブラー係数 (FocusDistance に近い程に 0、離れている程に 1)。
    float blurFactor = saturate(abs(sceneDistance - FocusDistance) * FocusScale);

    // 通常シーンとブラー済みシーンを線形補間。
    return lerp(normalScene, bluredScene, blurFactor);
}
