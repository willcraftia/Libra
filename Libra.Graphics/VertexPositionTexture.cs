#region Using

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionTexture : IVertexType, IEquatable<VertexPositionTexture>
    {
        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexElement.SVPosition, VertexElement.TexCoord);

        public Vector3 Position;

        public Vector2 TexCoord;

        public VertexPositionTexture(Vector3 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionTexture value1, VertexPositionTexture value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionTexture value1, VertexPositionTexture value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionTexture other)
        {
            return Position == other.Position && TexCoord == other.TexCoord;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionTexture) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ TexCoord.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " TexCoord:" + TexCoord + "}";
        }

        #endregion
    }
}
