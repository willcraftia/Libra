#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // VertexElement と InputElement の違いは、入力スロットを明示しているか否か。
    // 頂点バッファはコンテキストへの設定段階で入力スロットが確定するため、
    // 頂点バッファで管理する頂点宣言は InputElement ではなく VertexElement で宣言する。

    public struct VertexElement : IEquatable<VertexElement>
    {
        public static readonly VertexElement SVPosition = new VertexElement(InputElement.SemanticSVPosition, InputElementFormat.Vector3);

        public static readonly VertexElement Normal = new VertexElement(InputElement.SemanticNormal, InputElementFormat.Vector3);

        public static readonly VertexElement Color = new VertexElement(InputElement.SemanticColor, InputElementFormat.Color);

        public static readonly VertexElement TexCoord = new VertexElement(InputElement.SemanticTexCoord, InputElementFormat.Vector2);

        public string SemanticName;

        public int SemanticIndex;

        public InputElementFormat Format;

        public int AlignedByteOffset;

        public int SizeInBytes
        {
            get { return FormatHelper.SizeInBytes(Format); }
        }

        public VertexElement(string semanticName, InputElementFormat format,
            int alignedByteOffset = InputElement.AppendAlignedElement)
        {
            SemanticName = semanticName;
            SemanticIndex = 0;
            Format = format;
            AlignedByteOffset = alignedByteOffset;
        }

        public VertexElement(string semanticName, int semanticIndex, InputElementFormat format,
            int alignedByteOffset = InputElement.AppendAlignedElement)
        {
            SemanticName = semanticName;
            SemanticIndex = semanticIndex;
            Format = format;
            AlignedByteOffset = alignedByteOffset;
        }

        #region Equatable

        public static bool operator ==(VertexElement value1, VertexElement value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(VertexElement value1, VertexElement value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(VertexElement other)
        {
            return SemanticName == other.SemanticName && SemanticIndex == other.SemanticIndex &&
                Format == other.Format && AlignedByteOffset == other.AlignedByteOffset;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((VertexElement) obj);
        }

        public override int GetHashCode()
        {
            return SemanticName.GetHashCode() ^ SemanticIndex.GetHashCode() ^
                Format.GetHashCode() ^ AlignedByteOffset.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{SemanticName:" + SemanticName + " SemanticIndex:" + SemanticIndex +
                " Format:" + Format + " AlignedByteOffset:" + AlignedByteOffset +
                "]";
        }

        #endregion
    }
}
