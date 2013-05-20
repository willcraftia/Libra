cbuffer Parameters : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 WorldView;
};

Texture2D<float> LinearDepthMap : register(t0);
SamplerState LinearDepthMapSampler : register(s0);

struct VSOutput
{
    float4 Position     : SV_Position;
    float4 PositionWV   : POSITION_WV;
    float4 PositionWVP  : POSITION_WVP;
};

VSOutput VS(float4 position : SV_Position)
{
    VSOutput output;

    output.Position = mul(position, WorldViewProjection);
    output.PositionWV = mul(position, WorldView);
    output.PositionWVP = output.Position;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    float depth = -input.PositionWV.z;

    float2 texCoord = input.PositionWVP.xy / input.PositionWVP.w * float2(0.5, -0.5) + float2(0.5, 0.5);
    float sceneDepth = LinearDepthMap.Sample(LinearDepthMapSampler, texCoord);

    float fogDepth = min(depth, sceneDepth);

    return float4(fogDepth, 0, 0, 0);
}
