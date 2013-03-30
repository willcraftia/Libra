#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct VertexDeclarationBinding
    {
        public VertexDeclaration VertexDeclaration;

        public int Slot;

        public bool PerInstance;

        public int InstanceDataStepRate;

        public VertexDeclarationBinding(VertexDeclaration vertexDeclaration, int slot = 0,
            bool perInstance = false, int instanceDataStepRate = 0)
        {
            VertexDeclaration = vertexDeclaration;
            Slot = slot;
            PerInstance = perInstance;
            InstanceDataStepRate = instanceDataStepRate;
        }
    }
}
