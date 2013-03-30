#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelMeshPartCollection : ReadOnlyCollection<ModelMeshPart>
    {
        public ModelMeshPartCollection(IList<ModelMeshPart> list)
            : base(list)
        {
        }
    }
}
