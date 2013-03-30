#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelMeshCollection : Collection<ModelMesh>
    {
        public ModelMesh this[string name]
        {
            get
            {
                ModelMesh result;
                if (!TryGetValue(name, out result))
                    throw new KeyNotFoundException("Mesh not found: " + name);

                return result;
            }
        }

        public ModelMeshCollection(IList<ModelMesh> list)
            : base(list)
        {
        }

        public bool TryGetValue(string name, out ModelMesh value)
        {
            for (int i = 0; i < Count; i++)
            {
                var mesh = Items[i];
                if (mesh.Name == name)
                {
                    value = mesh;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
