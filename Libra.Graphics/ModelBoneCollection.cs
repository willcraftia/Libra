#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelBoneCollection : Collection<ModelBone>
    {
        public ModelBone this[string name]
        {
            get
            {
                ModelBone result;
                if (!TryGetValue(name, out result))
                    throw new KeyNotFoundException("Bone not found: " + name);

                return result;
            }
        }

        public ModelBoneCollection(IList<ModelBone> list)
            : base(list)
        {
        }

        public bool TryGetValue(string name, out ModelBone value)
        {
            for (int i = 0; i < Count; i++)
            {
                var bone = Items[i];
                if (bone.Name == name)
                {
                    value = bone;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
