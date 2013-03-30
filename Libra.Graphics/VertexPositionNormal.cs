#region Using

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormal : IVertexType, IEquatable<VertexPositionNormal>
    {
        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexElement.SVPosition, VertexElement.Normal);

        public Vector3 Position;

        public Vector3 Normal;

        public VertexPositionNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionNormal value1, VertexPositionNormal value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionNormal value1, VertexPositionNormal value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionNormal other)
        {
            return Position == other.Position && Normal == other.Normal;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionNormal) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Normal.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Normal:" + Normal + "}";
        }

        #endregion
    }
}
