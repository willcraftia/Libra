#region Using

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColorTexture : IVertexType, IEquatable<VertexPositionColorTexture>
    {
        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexElement.SVPosition, VertexElement.Color, VertexElement.TexCoord);

        public Vector3 Position;

        public Color Color;

        public Vector2 TexCoord;

        public VertexPositionColorTexture(Vector3 position, Color color, Vector2 texCoord)
        {
            Position = position;
            Color = color;
            TexCoord = texCoord;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionColorTexture value1, VertexPositionColorTexture value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionColorTexture value1, VertexPositionColorTexture value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionColorTexture other)
        {
            return Position == other.Position && Color == other.Color && TexCoord == other.TexCoord;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionColorTexture) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Color.GetHashCode() ^ TexCoord.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Color:" + Color + " TexCoord:" + TexCoord + "}";
        }

        #endregion
    }
}
