cbuffer Parameters : register(b0)
{
    float FocusScale    : packoffset(c0.x);
    float FocusDistance : packoffset(c0.y);
};

Texture2D<float4> Texture           : register(t0);
Texture2D<float4> BaseTexture       : register(t1);
Texture2D<float>  LinearDepthMap    : register(t2);

SamplerState TextureSampler         : register(s0);
SamplerState BaseTextureSampler     : register(s1);
SamplerState LinearDepthMapSampler  : register(s2);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 bluredScene = Texture.Sample(TextureSampler, texCoord);
    float4 normalScene = BaseTexture.Sample(BaseTextureSampler, texCoord);
    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);

    // ブラー係数 (FocusDistance に近い程に 0、離れている程に 1)。
    float blurFactor = saturate(abs(depth - FocusDistance) * FocusScale);

    // 通常シーンとブラー済みシーンを線形補間。
    return lerp(normalScene, bluredScene, blurFactor);
}
