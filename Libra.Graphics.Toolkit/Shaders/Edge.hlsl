cbuffer Parameters : register(b0)
{
    float2 EdgeOffset           : packoffset(c0);
    float  EdgeIntensity        : packoffset(c0.z);
    float  DepthThreshold       : packoffset(c1.x);
    float  DepthSensitivity     : packoffset(c1.y);
    float  NormalThreshold      : packoffset(c1.z);
    float  NormalSensitivity    : packoffset(c1.w);
    float3 EdgeColor            : packoffset(c2);
};

Texture2D<float4> Texture           : register(t0);
Texture2D<float4> DepthNormalMap    : register(t1);

SamplerState TextureSampler         : register(s0);
SamplerState DepthNormalMapSampler  : register(s1);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 source = Texture.Sample(TextureSampler, texCoord);

    float4 s  = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord);
    float4 s1 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2(-1, -1) * EdgeOffset);
    float4 s2 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2( 1,  1) * EdgeOffset);
    float4 s3 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2(-1,  1) * EdgeOffset);
    float4 s4 = DepthNormalMap.Sample(DepthNormalMapSampler, texCoord + float2( 1, -1) * EdgeOffset);

    float4 deltaSample = abs(s1 - s2) + abs(s3 - s4);

    float deltaDepth = deltaSample.x;
    deltaDepth = saturate((deltaDepth - DepthThreshold) * DepthSensitivity);

    float deltaNormal = dot(deltaSample.yzw, 1);
    deltaNormal = saturate((deltaNormal - NormalThreshold) * NormalSensitivity);

    float amount = saturate(deltaDepth + deltaNormal);

    // XNA サンプルとは異なり、遠方に行く程に影響を少なくする。
    // これにより、遠クリップ面での不正なエッジ描画が無くなる。
    amount *= EdgeIntensity * (1 - s.w);

    source.rgb = lerp(source.rgb, source.rgb * EdgeColor, saturate(amount));

    return source;
}
