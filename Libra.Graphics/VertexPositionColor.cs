#region Using

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColor : IVertexType, IEquatable<VertexPositionColor>
    {
        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexElement.SVPosition, VertexElement.Color);

        public Vector3 Position;

        public Color Color;

        public VertexPositionColor(Vector3 position, Color color)
        {
            Position = position;
            Color = color;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        #region Equatable

        public static bool operator ==(VertexPositionColor value1, VertexPositionColor value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexPositionColor value1, VertexPositionColor value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexPositionColor other)
        {
            return Position == other.Position && Color == other.Color;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexPositionColor) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Color.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Color:" + Color + "}";
        }

        #endregion
    }
}
