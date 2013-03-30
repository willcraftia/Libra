#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelMesh
    {
        public string Name { get; set; }

        public ModelBone ParentBone { get; set; }

        public ModelMeshPartCollection MeshParts { get; set; }

        public BoundingSphere BoundingSphere { get; set; }

        public ModelEffectCollection Effects { get; set; }

        public void Draw(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            for (int i = 0; i < MeshParts.Count; i++)
            {
                var part = MeshParts[i];

                if (part.IndexCount != 0)
                {
                    context.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    context.SetVertexBuffer(part.VertexBuffer);
                    context.IndexBuffer = part.IndexBuffer;

                    part.Effect.Apply(context);

                    context.DrawIndexed(part.IndexCount, part.StartIndexLocation, part.BaseVertexLocation);
                }
            }
        }
    }
}
