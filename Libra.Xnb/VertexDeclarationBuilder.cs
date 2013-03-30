#region Using

using System;
using Felis.Xnb;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class VertexDeclarationBuilder : VertexDeclarationBuilderBase<VertexDeclaration>
    {
        int vertexStride;

        VertexElement[] elements;

        int currentElementIndex;

        VertexElement currentElement;

        protected override void SetVertexStride(uint value)
        {
            vertexStride = (int) value;
        }

        protected override void SetElementCount(uint value)
        {
            elements = new VertexElement[value];
        }

        protected override void BeginElement(int index)
        {
            currentElement = new VertexElement();
            currentElementIndex = index;
        }

        protected override void SetElementOffset(uint value)
        {
            currentElement.AlignedByteOffset = (int) value;
        }

        protected override void SetElementFormat(int value)
        {
            currentElement.Format = InputElementFormatConverter.ToInputElement(value);
        }

        protected override void SetElementUsage(int value)
        {
            currentElement.SemanticName = SemanticNameConverter.ToSemanticsName(value);
        }

        protected override void SetElementUsageIndex(uint value)
        {
            currentElement.SemanticIndex = (int) value;
        }

        protected override void EndElement()
        {
            elements[currentElementIndex] = currentElement;

            base.EndElement();
        }

        protected override void Begin(object deviceContext)
        {
        }

        protected override object End()
        {
            return new VertexDeclaration(vertexStride, elements);
        }
    }
}
