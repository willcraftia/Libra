cbuffer Parameters : register(b0)
{
    float2 EdgeOffset               : packoffset(c0);
    float  EdgeIntensity            : packoffset(c0.z);
    float  DepthThreshold           : packoffset(c1.x);
    float  DepthSensitivity         : packoffset(c1.y);
    float  NormalThreshold          : packoffset(c1.z);
    float  NormalSensitivity        : packoffset(c1.w);
    float3 EdgeColor                : packoffset(c2);
    float  EdgeAttenuation          : packoffset(c2.w);
};

// 法線マップは _SNORM フォーマット。
Texture2D<float4> Texture           : register(t0);
Texture2D<float>  DepthMap          : register(t1);
Texture2D<float3> NormalMap         : register(t2);

SamplerState TextureSampler         : register(s0);
SamplerState DepthMapSampler        : register(s1);
SamplerState NormalMapSampler       : register(s2);

float4 SampleDepthNormal(float2 texCoord)
{
    float4 depthNormal;

    depthNormal.x = DepthMap.SampleLevel(DepthMapSampler, texCoord, 0);
    depthNormal.yzw = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    return depthNormal;
}

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 source = Texture.Sample(TextureSampler, texCoord);

    float4 s  = SampleDepthNormal(texCoord);
    float4 s1 = SampleDepthNormal(texCoord + float2(-1, -1) * EdgeOffset);
    float4 s2 = SampleDepthNormal(texCoord + float2( 1,  1) * EdgeOffset);
    float4 s3 = SampleDepthNormal(texCoord + float2(-1,  1) * EdgeOffset);
    float4 s4 = SampleDepthNormal(texCoord + float2( 1, -1) * EdgeOffset);

    float4 deltaSample = abs(s1 - s2) + abs(s3 - s4);

    float deltaDepth = deltaSample.x;
    deltaDepth = saturate((deltaDepth - DepthThreshold) * DepthSensitivity);

    float deltaNormal = dot(deltaSample.yzw, 1);
    deltaNormal = saturate((deltaNormal - NormalThreshold) * NormalSensitivity);

    // 参考にした XNA サンプルでは saturate(deltaDepth + deltaNormal)。
    // 度合いの和とする意味がないと判断し、最大値抽出へ変更。
    float amount = saturate(max(deltaDepth, deltaNormal));

    // EdgeAttenuation を越える深度から減衰を始める。
    // EdgeAttenuation < 1 ならば深度 1 で amount = 0。
    // すなわち、遠クリップ面に近づく程にエッジなしに近づく。
    // 1 <= EdgeAttenuation ならば減衰なし。
    // なお、XNA サンプルでは減衰処理なし。
    float attenuation = 1 - s.x * (EdgeAttenuation < s.x);

    amount *= EdgeIntensity * attenuation;

    source.rgb = lerp(source.rgb, source.rgb * EdgeColor, saturate(amount));

    return source;
}
