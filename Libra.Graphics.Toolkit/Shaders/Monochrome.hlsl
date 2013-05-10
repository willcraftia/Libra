//
// ����
//
// Y  =  0.29900 * R + 0.58700 * G + 0.11400 * B
// Cb = -0.16874 * R - 0.33126 * G + 0.50000 * B
// Cr =  0.50000 * R - 0.41869 * G - 0.08131 * B
//
// Y is luminance component
// Cb is the blue-defference chroma component
// Cr is the red-difference chroma component
//
// R = Y                + 1.40200 * Cr
// G = Y - 0.34414 * Cb - 0.71414 * Cr
// B = Y + 1.77200 * Cb

//
// ����
//
// Grayscale:  Cb = 0, Cr = 0
// Sepia Tone: Cb = -0.1, Cr = 0.1
//
cbuffer Parameters : register(b0)
{
    float Cb : packoffset(c0.x);
    float Cr : packoffset(c0.y);
};

Texture2D<float4> Texture : register(t0);
SamplerState TextureSampler : register(s0);

float4 PS(float4 color    : COLOR0,
          float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 source = Texture.Sample(TextureSampler, texCoord);

    float y = dot(float3(0.299f, 0.587f, 0.114f), source.rgb);

    return float4(
        y               + 1.402f * Cr,
        y - 0.344f * Cb - 0.714f * Cr,
        y + 1.772f * Cb,
        source.a);
}