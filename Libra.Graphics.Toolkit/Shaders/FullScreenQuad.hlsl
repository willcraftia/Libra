//
// See
// http://www.altdevblogaday.com/2011/08/08/interesting-vertex-shader-trick/
//

// �{���ACOLOR0 �͕s�v�ł��邪�ASpriteBatch �� VS �o�͂Ƃ̌݊��̂��߂ɒ�`�B
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
