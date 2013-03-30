#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class Model
    {
        Matrix[] boneTransforms;

        public ModelBone Root { get; set; }

        public ModelBoneCollection Bones { get; set; }

        public ModelMeshCollection Meshes { get; set; }

        public void Draw(DeviceContext context, Matrix world, Matrix view, Matrix projection)
        {
            if (boneTransforms == null)
            {
                boneTransforms = new Matrix[Bones.Count];
            }

            CopyAbsoluteBoneTransformsTo(boneTransforms);

            for (int i = 0; i < Meshes.Count; i++)
            {
                var mesh = Meshes[i];
                
                for (int j = 0; j < mesh.Effects.Count; j++)
                {
                    var effect = mesh.Effects[j];

                    var effectMatrices = effect as IEffectMatrices;
                    if (effectMatrices != null)
                    {
                        Matrix finalWorld;
                        Matrix.Multiply(ref boneTransforms[mesh.ParentBone.Index], ref world, out finalWorld);

                        effectMatrices.World = finalWorld;
                        effectMatrices.View = view;
                        effectMatrices.Projection = projection;
                    }
                }

                mesh.Draw(context);
            }
        }

        public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
        {
            if (destinationBoneTransforms == null) throw new ArgumentNullException("destinationBoneTransforms");
            if (destinationBoneTransforms.Length != Bones.Count)
                throw new ArgumentOutOfRangeException("destinationBoneTransforms");

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];

                if (bone.Parent == null)
                {
                    destinationBoneTransforms[i] = bone.Transform;
                }
                else
                {
                    Matrix.Multiply(ref bone.Transform, ref bone.Parent.Transform, out destinationBoneTransforms[i]);
                }
            }
        }

        // TODO
        //
        // Transform を public フィールドにしたので要らないのでは？

        //public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
        //{
        //    if (sourceBoneTransforms == null) throw new ArgumentNullException("sourceBoneTransforms");
        //    if (sourceBoneTransforms.Length != Bones.Count)
        //        throw new ArgumentOutOfRangeException("sourceBoneTransforms");

        //    for (int i = 0; i < sourceBoneTransforms.Length; i++)
        //    {
        //        Bones[i].Transform = sourceBoneTransforms[i];
        //    }
        //}

        //public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
        //{
        //    if (destinationBoneTransforms == null) throw new ArgumentNullException("destinationBoneTransforms");
        //    if (destinationBoneTransforms.Length != Bones.Count)
        //        throw new ArgumentOutOfRangeException("destinationBoneTransforms");

        //    for (int i = 0; i < destinationBoneTransforms.Length; i++)
        //    {
        //        destinationBoneTransforms[i] = Bones[i].Transform;
        //    }
        //}
    }
}
