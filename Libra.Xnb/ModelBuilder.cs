#region Using

using System;
using System.Collections.Generic;
using Felis.Xnb;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class ModelBuilder : ModelBuilderBase<Model>
    {
        #region VertexBufferFixup

        struct VertexBufferFixup
        {
            ModelMeshPart meshPart;

            public VertexBufferFixup(ModelMeshPart meshPart)
            {
                this.meshPart = meshPart;
            }

            public void Fixup(object vertexBuffer)
            {
                meshPart.VertexBuffer = vertexBuffer as VertexBuffer;
            }
        }

        #endregion

        #region IndexBufferFixup

        struct IndexBufferFixup
        {
            ModelMeshPart meshPart;

            public IndexBufferFixup(ModelMeshPart meshPart)
            {
                this.meshPart = meshPart;
            }

            public void Fixup(object indexBuffer)
            {
                meshPart.IndexBuffer = indexBuffer as IndexBuffer;
            }
        }

        #endregion

        #region EffectFixup

        struct EffectFixup
        {
            ModelMesh mesh;

            ModelMeshPart meshPart;

            public EffectFixup(ModelMesh mesh, ModelMeshPart meshPart)
            {
                this.mesh = mesh;
                this.meshPart = meshPart;
            }

            public void Fixup(object effect)
            {
                meshPart.Effect = effect as IEffect;

                if (mesh.Effects == null)
                    mesh.Effects = new ModelEffectCollection();

                mesh.Effects.Add(meshPart.Effect);
            }
        }

        #endregion

        Model instance;

        List<ModelBone> bones;

        ModelBone currentBone;

        List<ModelBone> currentChildBones;

        List<ModelMesh> meshes;

        ModelMesh currentMesh;

        List<ModelMeshPart> meshParts;

        ModelMeshPart currentMeshPart;

        protected override void Initialize(ContentManager contentManager)
        {
            base.Initialize(contentManager);
        }

        protected override void SetBoneCount(uint value)
        {
            bones = new List<ModelBone>((int) value);
        }

        protected override void BeginBone(int index)
        {
            currentBone = new ModelBone();
            bones.Add(currentBone);
        }

        protected override void SetBoneName(string value)
        {
            currentBone.Name = value;
        }

        protected override void SetBoneTransform(object value)
        {
            currentBone.Transform = (Matrix) value;
        }

        protected override void EndBones()
        {
            instance.Bones = new ModelBoneCollection(bones);

            base.EndBones();
        }

        protected override void BeginBoneHierarchy(int index)
        {
            currentBone = bones[index];
        }

        protected override void SetBoneHierarchyParentBone(int value)
        {
            if (value != 0)
            {
                currentBone.Parent = bones[value - 1];
            }
        }

        protected override void SetBoneHierarchyChildBoneCount(uint value)
        {
            currentChildBones = new List<ModelBone>((int) value);
        }

        protected override void BeginBoneHierarchyChildBone(int index)
        {
        }

        protected override void SetBoneHierarchyChildBone(int value)
        {
            currentChildBones.Add(bones[value - 1]);
        }

        protected override void EndBoneHierarchy()
        {
            currentBone.Children = new ModelBoneCollection(currentChildBones);

            base.EndBoneHierarchy();
        }

        protected override void SetMeshCount(uint value)
        {
            meshes = new List<ModelMesh>((int) value);
        }

        protected override void BeginMesh(int index)
        {
            currentMesh = new ModelMesh();
            meshes.Add(currentMesh);
        }

        protected override void SetMeshName(string value)
        {
            currentMesh.Name = value;
        }

        protected override void SetMeshParentBone(int value)
        {
            currentMesh.ParentBone = bones[value - 1];
        }

        protected override void SetMeshBoundingSphere(object value)
        {
            currentMesh.BoundingSphere = (BoundingSphere) value;
        }

        protected override void SetMeshTag(object value)
        {
        }

        protected override void SetMeshPartCount(uint value)
        {
            meshParts = new List<ModelMeshPart>((int) value);
        }

        protected override void BeginMeshPart(int index)
        {
            currentMeshPart = new ModelMeshPart();
            meshParts.Add(currentMeshPart);
        }

        protected override void SetMeshPartVertexOffset(uint value)
        {
            currentMeshPart.BaseVertexLocation = (int) value;
        }

        protected override void SetMeshPartNumVertices(uint value)
        {
            currentMeshPart.VertexCount = (int) value;
        }

        protected override void SetMeshPartStartIndex(uint value)
        {
            currentMeshPart.StartIndexLocation = (int) value;
        }

        protected override void SetMeshPartPrimitiveCount(uint value)
        {
            currentMeshPart.IndexCount = (int) value * 3;
        }

        protected override void SetMeshPartTag(object value)
        {
        }

        protected override Action<object> GetMeshPartVertexBufferCallback()
        {
            return new VertexBufferFixup(currentMeshPart).Fixup;
        }

        protected override Action<object> GetMeshPartIndexBufferCallback()
        {
            return new IndexBufferFixup(currentMeshPart).Fixup;
        }

        protected override Action<object> GetMeshPartEffectCallback()
        {
            return new EffectFixup(currentMesh, currentMeshPart).Fixup;
        }

        protected override void EndMeshParts()
        {
            currentMesh.MeshParts = new ModelMeshPartCollection(meshParts);

            base.EndMeshParts();
        }

        protected override void EndMeshes()
        {
            instance.Meshes = new ModelMeshCollection(meshes);

            base.EndMeshes();
        }

        protected override void SetRootBone(int value)
        {
            instance.Root = bones[value - 1];
        }

        protected override void SetTag(object value)
        {
        }

        protected override void Begin(object deviceContext)
        {
            instance = new Model();
        }

        protected override object End()
        {
            return instance;
        }
    }
}
