#define MAX_SPLIT_COUNT 3

cbuffer Parameters : register(b0)
{
    float4x4 World                                  : packoffset(c0);
    float4x4 View                                   : packoffset(c4);
    float4x4 Projection                             : packoffset(c8);
    float4   AmbientColor                           : packoffset(c12);
    float    DepthBias                              : packoffset(c13);
    int      SplitCount                             : packoffset(c14);
    float3   LightDirection                         : packoffset(c15);
    float3   ShadowColor                            : packoffset(c16);
    float    SplitDistances[MAX_SPLIT_COUNT + 1]    : packoffset(c17);
    float4x4 LightViewProjections[MAX_SPLIT_COUNT]  : packoffset(c21);
};

Texture2D<float4> Texture                               : register(t0);
Texture2D<float>  BasicShadowMap[MAX_SPLIT_COUNT]       : register(t1);
Texture2D<float2> VarianceShadowMap[MAX_SPLIT_COUNT]    : register(t1);

SamplerState TextureSampler     : register(s0);
SamplerState ShadowMapSampler   : register(s1);

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position   : SV_Position;
    float3 Normal     : TEXCOORD0;
    float2 TexCoord   : TEXCOORD1;
    float4 PositionWS : TEXCOORD2;
    float4 PositionVS : TEXCOORD3;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    float4 positionWS = mul(input.Position, World);
    float4 positionVS = mul(positionWS, View);

    output.Position = mul(positionVS, Projection);
    output.Normal =  normalize(mul(float4(input.Normal, 0), World)).xyz;
    output.TexCoord = input.TexCoord;

    output.PositionWS = positionWS;
    output.PositionVS = positionVS;

    return output;
}

float4 BasicPS(VSOutput input) : SV_Target0
{ 
    float4 diffuseColor = Texture.Sample(TextureSampler, input.TexCoord);

    float diffuseIntensity = saturate(dot(-LightDirection, input.Normal));
    float4 diffuse = diffuseIntensity * diffuseColor + AmbientColor;

    float distance = abs(input.PositionVS.z);

    float shadow = 0;

    [unroll]
    for (int i = 0; i < SplitCount; i++)
    {
        float depthLS = 0;
        float depthShadowMap = 0;

        if (SplitDistances[i] <= distance && distance < SplitDistances[i + 1])
        {
            float4 positionLS = mul(input.PositionWS, LightViewProjections[i]);
            depthLS = (positionLS.z / positionLS.w) - DepthBias;

            float2 shadowMapTexCoord = 0.5 * positionLS.xy / positionLS.w + float2(0.5, 0.5);
            shadowMapTexCoord.y = 1 - shadowMapTexCoord.y;

            // Sample() では gradient-based operation に関する警告が発生。
            // これは SampleLevel() で LOD を明示することで解決可能。
            //
            // http://www.gamedev.net/topic/533200-dx10-hlsl-gradient-based-operation-error/
            // MJP の回答から。
            //
            // ---
            // Sample() を用いると、スクリーン空間でのテクスチャ座標の偏導関数を用いて勾配が計算される。
            // これら導関数は、利用するミップマップ レベルを決定する。
            // シェーダは、テクスチャ座標の導関数を動的分岐の中では自動的に計算できない。
            // これは、4 隣接ピクセルが同じ分岐の中にあるとは限らないためである。
            //
            // 簡単な解決方法は二通りある。
            //
            // 1. 分岐の外で明示的に導関数を計算して SampleGrad() へ渡す。
            // 2. SampleLevel() を用いてミップマップ レベルを明示する。
            // ---
            depthShadowMap = BasicShadowMap[i].SampleLevel(ShadowMapSampler, shadowMapTexCoord, 0);

            shadow = (depthShadowMap < depthLS);

            break;
        }
    }

    float3 blendShadowColor = lerp(float3(1, 1, 1), ShadowColor, shadow);

    diffuse.xyz *= blendShadowColor;

    return diffuse;
}

float TestVSM(float4 position, float2 moments)
{
    float Ex = moments.x;
    float E_x2 = moments.y;
    float Vx = E_x2 - Ex * Ex;
    Vx = min(1, max(0, Vx + 0.00001f));
    float t = position.z / position.w - DepthBias;
    float tMinusM = t - Ex;
    float p = Vx / (Vx + tMinusM * tMinusM);

    // チェビシェフの不等式により t > Ex で p が有効。
    // t <= Ex では p = 1、つまり、影がない。
    return saturate(max(p, t <= Ex));
}

float4 VariancePS(VSOutput input) : SV_Target0
{ 
    float4 diffuseColor = Texture.Sample(TextureSampler, input.TexCoord);

    float diffuseIntensity = saturate(dot(-LightDirection, input.Normal));
    float4 diffuse = diffuseIntensity * diffuseColor + AmbientColor;

    float distance = abs(input.PositionVS.z);

    float shadow = 0;

    [unroll]
    for (int i = 0; i < SplitCount; i++)
    {
        if (SplitDistances[i] <= distance && distance < SplitDistances[i + 1])
        {
            float4 positionLS = mul(input.PositionWS, LightViewProjections[i]);

            float2 shadowMapTexCoord = 0.5 * positionLS.xy / positionLS.w + float2( 0.5, 0.5 );
            shadowMapTexCoord.y = 1 - shadowMapTexCoord.y;

            // Sample() では gradient-based operation に関する警告が発生。
            // これは SampleLevel() で LOD を明示することで解決可能。
            float2 moments = VarianceShadowMap[i].SampleLevel(ShadowMapSampler, shadowMapTexCoord, 0);
            float test = TestVSM(positionLS, moments);

            shadow = (1 - test);

            break;
        }
    }

    float3 blendShadowColor = lerp(float3(1, 1, 1), ShadowColor, shadow);

    diffuse.xyz *= blendShadowColor;

    return diffuse;
}
