#region Using

using System;
using Libra;
using Libra.Graphics;

#endregion

namespace Samples.Audio3D
{
    public sealed class QuadDrawer
    {
        Device device;

        AlphaTestEffect effect;
        
        VertexPositionTexture[] vertices;

        VertexBuffer vertexBuffer;

        public QuadDrawer(Device device)
        {
            this.device = device;

            effect = new AlphaTestEffect(device);

            effect.AlphaFunction = ComparisonFunction.Greater;
            effect.ReferenceAlpha = 128;

            vertices = new VertexPositionTexture[4];
            vertices[0].Position = new Vector3( 1,  1, 0);
            vertices[1].Position = new Vector3(-1,  1, 0);
            vertices[2].Position = new Vector3( 1, -1, 0);
            vertices[3].Position = new Vector3(-1, -1, 0);

            vertexBuffer = device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Dynamic;
            vertexBuffer.Initialize<VertexPositionTexture>(vertices.Length);
        }

        public void DrawQuad(DeviceContext context,
            ShaderResourceView texture, float textureRepeats, Matrix world, Matrix view, Matrix projection)
        {
            effect.Texture = texture;

            effect.World = world;
            effect.View = view;
            effect.Projection = projection;

            vertices[0].TexCoord = new Vector2(0, 0);
            vertices[1].TexCoord = new Vector2(textureRepeats, 0);
            vertices[2].TexCoord = new Vector2(0, textureRepeats);
            vertices[3].TexCoord = new Vector2(textureRepeats, textureRepeats);

            vertexBuffer.SetData(context, vertices);
            context.SetVertexBuffer(0, vertexBuffer);

            context.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            context.PixelShaderSamplers[0] = SamplerState.LinearWrap;

            effect.Apply(context);

            context.Draw(4);
        }
    }
}
