//
// See
// http://www.altdevblogaday.com/2011/08/08/interesting-vertex-shader-trick/
//

// 本来、COLOR0 は不要であるが、SpriteBatch の VS 出力との互換のために定義。
struct VSOutput
{
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
    float4 Position : SV_Position;
};

VSOutput VS(uint id : SV_VertexID)
{
    VSOutput output;

    output.Color = float4(0, 0, 0, 0);
    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2, -2) + float2(-1, 1), 0, 1);

    return output;
}
