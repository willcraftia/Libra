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

        public void Draw()
        {
            for (int i = 0; i < MeshParts.Count; i++)
            {
                var meshPart = MeshParts[i];

                if (meshPart.IndexCount != 0)
                {
                    meshPart.Effect.Apply();
                    DrawMeshPart(meshPart.Effect.DeviceContext, meshPart);
                }
            }
        }

        public void Draw(IEffect effect)
        {
            if (effect == null) throw new ArgumentNullException("effect");

            effect.Apply();

            for (int i = 0; i < MeshParts.Count; i++)
            {
                var meshPart = MeshParts[i];

                if (meshPart.IndexCount != 0)
                {
                    DrawMeshPart(effect.DeviceContext, meshPart);
                }
            }
        }

        void DrawMeshPart(DeviceContext context, ModelMeshPart meshPart)
        {
            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.SetVertexBuffer(meshPart.VertexBuffer);
            context.IndexBuffer = meshPart.IndexBuffer;

            context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
        }
    }
}
