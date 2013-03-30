#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionNormalTexture: IVertexType, IEquatable<VertexPositionNormalTexture>
    {
        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexElement.SVPosition, VertexElement.Normal, VertexElement.TexCoord);

        public Vector3 Position;

        public Vector3 Normal;

        public Vector2 TexCoord;

        public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionNormalTexture value1, VertexPositionNormalTexture value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionNormalTexture value1, VertexPositionNormalTexture value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionNormalTexture other)
        {
            return Position == other.Position && Normal == other.Normal && TexCoord == other.TexCoord;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionNormalTexture) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Normal.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Normal:" + Normal + " TexCoord:" + TexCoord + "}";
        }

        #endregion
    }
}
