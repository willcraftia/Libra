cbuffer VSParameters : register(b0)
{
    float4x4 WorldViewProjection;
};

cbuffer PSParameters : register(b0)
{
    float3 SkyColor     : packoffset(c0);
    // ‹“_‚©‚çŒ©‚½‘¾—z‚Ì•ûŒü
    float3 SunDirection : packoffset(c1);
    float3 SunColor     : packoffset(c2);
    // ‘¾—z‚ÌêŠ‚ğ”»’è‚·‚é‚½‚ß‚Ìè‡’l (0.999 ˆÈã‚ª‘Ã“–)
    float SunThreshold  : packoffset(c3.x);
    // 0: ‘¾—z‚ğ•`‰æ‚µ‚È‚¢
    // 1: ‘¾—z‚ğ•`‰æ‚·‚é
    float SunVisible    : packoffset(c3.y);
};

struct VSInput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL0;
};

VSOutput VS(VSInput input)
{
    VSOutput output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Normal = input.Normal;

    return output;
}

float4 PS(VSOutput input) : SV_Target0
{
    float4 color = float4(SkyColor, 1);

    // –@ü‚ª‚Ç‚Ì’ö“x‘¾—z‚ÌŒü‚«‚Éˆê’v‚µ‚Ä‚¢‚é‚©‚ğZo
    // ‘¾—z‚Ì‹t•ûŒü‚Í 0 ‚Æ‚µ‚Ä”jŠü
    float amount = saturate(dot(normalize(input.Normal), SunDirection)) * SunVisible;

    // SunThreshold ‚©‚ç‘¾—z‚Ì”ÍˆÍ‚ğZo
    amount -= SunThreshold;
    amount = saturate(amount);
    amount /= (1 - SunThreshold);

    // ‘¾—z‚ÌF‚ğƒuƒŒƒ“ƒh
    color.rgb += SunColor * amount;

    return color;
}
