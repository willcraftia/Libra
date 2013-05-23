// オリジナルは下記サイト:
// http://www.gamerendering.com/2009/01/14/ssao/
//
// 上記シェーダとの差異は以下:
// ・深度マップは線形深度マップ (オリジナルは射影深度マップ)。
// ・球状のランダム点はシェーダ外部で生成 (オリジナルはハードコード)。
// ・Falloff による深度差異の調整をしない (単純比較で十分と判断)。
// ・閉塞物までの距離による減衰処理の追加。

#define MAX_SAMPLE_COUNT 128

cbuffer Parameters : register(b0)
{
    float2 FocalLength                      : packoffset(c0);
    float  SampleCount                      : packoffset(c0.z);

    float  Strength                         : packoffset(c1.x);
    float  Attenuation                      : packoffset(c1.y);
    float  Radius                           : packoffset(c1.z);
    float  FarClipDistance                  : packoffset(c1.w);

    float4 SampleSphere[MAX_SAMPLE_COUNT]   : packoffset(c2);
};

struct Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 ViewRay  : TEXCOORD1;
};

Output VS(uint id : SV_VertexID)
{
    Output output;

    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);
    output.ViewRay = float3(output.Position.xy / FocalLength, 1);

    return output;
}
