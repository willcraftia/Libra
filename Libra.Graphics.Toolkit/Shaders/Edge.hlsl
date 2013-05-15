cbuffer Parameters : register(b0)
{
    float2 EdgeOffset           : packoffset(c0);
    float  EdgeIntensity        : packoffset(c0.z);
    float  EdgeAttenuation      : packoffset(c0.w);
    float3 EdgeColor            : packoffset(c1);
    float  DepthThreshold       : packoffset(c2.x);
    float  DepthSensitivity     : packoffset(c2.y);
    float  NormalThreshold      : packoffset(c2.z);
    float  NormalSensitivity    : packoffset(c2.w);
    float  NearClipDistance     : packoffset(c3.x);
    float  FarClipDistance      : packoffset(c3.y);
};

// 法線マップは _SNORM フォーマット。
Texture2D<float4> Texture           : register(t0);
Texture2D<float>  LinearDepthMap    : register(t1);
Texture2D<float3> NormalMap         : register(t2);

SamplerState TextureSampler         : register(s0);
SamplerState LinearDepthMapSampler  : register(s1);
SamplerState NormalMapSampler       : register(s2);

float4 SampleDepthNormal(float2 texCoord)
{
    float4 depthNormal;

    depthNormal.x = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);
    depthNormal.yzw = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    return depthNormal;
}

static float2 Kernel[4] =
{
    float2(-1, -1),
    float2( 1,  1),
    float2(-1,  1),
    float2( 1, -1)
};

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 source = Texture.Sample(TextureSampler, texCoord);

    float depth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, texCoord, 0);
    float3 normal = NormalMap.SampleLevel(NormalMapSampler, texCoord, 0);

    float deltaDepth = 0;
    float deltaNormal = 0;

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        // TODO
        // 定数バッファで事前計算して設定。
        float2 sampleTexCoord = texCoord + Kernel[i] * EdgeOffset;

        float sampleDepth = LinearDepthMap.SampleLevel(LinearDepthMapSampler, sampleTexCoord, 0);
        float3 sampleNormal = NormalMap.SampleLevel(NormalMapSampler, sampleTexCoord, 0);

        deltaDepth += abs(depth - sampleDepth);
        deltaNormal += 1.0f - dot(normal, sampleNormal);
    }

    deltaDepth /= 4;
    deltaNormal /= 4;

    deltaDepth = saturate(deltaDepth - DepthThreshold);
    deltaNormal = saturate(deltaNormal - NormalThreshold);

    deltaDepth = saturate(deltaDepth * DepthSensitivity);
    deltaNormal = saturate(deltaNormal * NormalSensitivity);

    float amount = max(deltaDepth, deltaNormal);

    amount *= EdgeIntensity;

    // EdgeAttenuation を越える深度 (射影空間) から減衰を始める。
    // EdgeAttenuation < 1 ならば遠クリップ面 で amount = 0。
    // 1 <= EdgeAttenuation ならば減衰なし。
    float projectedDepth = (depth - NearClipDistance) / (FarClipDistance - NearClipDistance);
    float attenuation = 1 - projectedDepth * step(EdgeAttenuation, projectedDepth);
    amount *= attenuation;

    source.rgb = lerp(source.rgb, source.rgb * EdgeColor, saturate(amount));

    return source;
}
