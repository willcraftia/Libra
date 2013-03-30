#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelBone
    {
        public Matrix Transform;

        public string Name { get; set; }

        public int Index { get; set; }

        public ModelBone Parent { get; set; }

        public ModelBoneCollection Children { get; set; }
    }
}
