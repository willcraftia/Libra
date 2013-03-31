#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct VertexPositionNormalColorTexture : IVertexType, IEquatable<VertexPositionNormalColorTexture>
    {
        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            VertexElement.SVPosition, VertexElement.Normal, VertexElement.Color, VertexElement.TexCoord);

        public Vector3 Position;

        public Vector3 Normal;

        public Color Color;

        public Vector2 TexCoord;

        public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color color, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texCoord;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionNormalColorTexture value1, VertexPositionNormalColorTexture value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionNormalColorTexture value1, VertexPositionNormalColorTexture value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionNormalColorTexture other)
        {
            return Position == other.Position && Normal == other.Normal &&
                Color == other.Color && TexCoord == other.TexCoord;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionNormalColorTexture) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Normal.GetHashCode() ^
                Color.GetHashCode() ^ TexCoord.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Normal:" + Normal +
                " Color:" + Color + " TexCoord:" + TexCoord + "}";
        }

        #endregion
    }
}
