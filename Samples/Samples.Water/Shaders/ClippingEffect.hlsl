// BasicEffect ÇÃàÍïîÇ…ëŒÇµÇƒ SV_ClipDistance ÇÃã@î\Çí«â¡ÇµÇΩèÛë‘ÅB

Texture2D<float4> Texture : register(t0);
sampler Sampler : register(s0);

cbuffer Parameters : register(b0)
{
    float4 DiffuseColor             : packoffset(c0);
    float3 EmissiveColor            : packoffset(c1);
    float3 SpecularColor            : packoffset(c2);
    float  SpecularPower            : packoffset(c2.w);

    float3 LightDirection[3]        : packoffset(c3);
    float3 LightDiffuseColor[3]     : packoffset(c6);
    float3 LightSpecularColor[3]    : packoffset(c9);

    float3 EyePosition              : packoffset(c12);

    float3 FogColor                 : packoffset(c13);
    float4 FogVector                : packoffset(c14);

    float4x4 World                  : packoffset(c15);
    float3x3 WorldInverseTranspose  : packoffset(c19);
    float4x4 WorldViewProj          : packoffset(c22);
};

cbuffer ClippingParameters : register(b1)
{
    bool   ClippingEnabled  : packoffset(c0);
    float4 ClipPlanes[3]    : packoffset(c1);
};

struct VSInputNm
{
    float4 Position : SV_Position;
    float3 Normal   : NORMAL;
};

struct VSOutputPixelLighting
{
    float4 PositionWS   : TEXCOORD0;
    float3 NormalWS     : TEXCOORD1;
    float4 Diffuse      : COLOR0;
    float4 PositionPS   : SV_Position;
    float3 ClipDistance : SV_ClipDistance0;
};

struct PSInputPixelLighting
{
    float4 PositionWS : TEXCOORD0;
    float3 NormalWS   : TEXCOORD1;
    float4 Diffuse    : COLOR0;
};

struct CommonVSOutputPixelLighting
{
    float4 Pos_ps;
    float3 Pos_ws;
    float3 Normal_ws;
    float  FogFactor;
};

struct ColorPair
{
    float3 Diffuse;
    float3 Specular;
};

float ComputeFogFactor(float4 position)
{
    return saturate(dot(position, FogVector));
}

void ApplyFog(inout float4 color, float fogFactor)
{
    color.rgb = lerp(color.rgb, FogColor * color.a, fogFactor);
}

void AddSpecular(inout float4 color, float3 specular)
{
    color.rgb += specular * color.a;
}

CommonVSOutputPixelLighting ComputeCommonVSOutputPixelLighting(float4 position, float3 normal)
{
    CommonVSOutputPixelLighting vout;
    
    vout.Pos_ps = mul(position, WorldViewProj);
    vout.Pos_ws = mul(position, World).xyz;
    vout.Normal_ws = normalize(mul(normal, WorldInverseTranspose));
    vout.FogFactor = ComputeFogFactor(position);
    
    return vout;
}

ColorPair ComputeLights(float3 eyeVector, float3 worldNormal, uniform int numLights)
{
    float3x3 lightDirections = 0;
    float3x3 lightDiffuse = 0;
    float3x3 lightSpecular = 0;
    float3x3 halfVectors = 0;
    
    [unroll]
    for (int i = 0; i < numLights; i++)
    {
        lightDirections[i] = LightDirection[i];
        lightDiffuse[i]    = LightDiffuseColor[i];
        lightSpecular[i]   = LightSpecularColor[i];
        
        halfVectors[i] = normalize(eyeVector - lightDirections[i]);
    }

    float3 dotL = mul(-lightDirections, worldNormal);
    float3 dotH = mul(halfVectors, worldNormal);
    
    float3 zeroL = step(0, dotL);

    float3 diffuse  = zeroL * dotL;
    float3 specular = pow(max(dotH, 0) * zeroL, SpecularPower);

    ColorPair result;
    
    result.Diffuse  = mul(diffuse,  lightDiffuse)  * DiffuseColor.rgb + EmissiveColor;
    result.Specular = mul(specular, lightSpecular) * SpecularColor;

    return result;
}

#define SetCommonVSOutputParamsPixelLighting \
    vout.PositionPS = cout.Pos_ps; \
    vout.PositionWS = float4(cout.Pos_ws, cout.FogFactor); \
    vout.NormalWS = cout.Normal_ws;

// VSBasicPixelLighting
//[clipplanes(ClipPlanes[0])]
VSOutputPixelLighting VS(VSInputNm vin)
{
    VSOutputPixelLighting vout;

    CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(vin.Position, vin.Normal);
    SetCommonVSOutputParamsPixelLighting;

    vout.Diffuse = float4(1, 1, 1, DiffuseColor.a);

    if (ClippingEnabled)
    {
//        float4 posW = mul(vin.Position, World);
        float4 posW = float4(cout.Pos_ws, 1);
        vout.ClipDistance.x = dot(posW, ClipPlanes[0]);
        vout.ClipDistance.y = dot(posW, ClipPlanes[1]);
        vout.ClipDistance.z = dot(posW, ClipPlanes[2]);
//        vout.ClipDistance.z = dot(mul(vin.Position, World), ClipPlanes[2]);
    }
    else
    {
        vout.ClipDistance.x = 1;
        vout.ClipDistance.y = 1;
        vout.ClipDistance.z = 1;
    }

    return vout;
}

// PSBasicPixelLighting
float4 PS(PSInputPixelLighting pin) : SV_Target0
{
    float4 color = pin.Diffuse;

    float3 eyeVector = normalize(EyePosition - pin.PositionWS.xyz);
    float3 worldNormal = normalize(pin.NormalWS);

    ColorPair lightResult = ComputeLights(eyeVector, worldNormal, 3);

    color.rgb *= lightResult.Diffuse;

    AddSpecular(color, lightResult.Specular);
    ApplyFog(color, pin.PositionWS.w);

    return color;
}

