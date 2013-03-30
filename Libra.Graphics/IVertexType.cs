#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics
{
    public interface IVertexType
    {
        VertexDeclaration VertexDeclaration { get; }
    }
}
