#region Using

using System;
using System.Text;

#endregion

namespace Libra.Graphics
{
    public sealed class VertexDeclaration
    {
        const int InputSlotCount = D3D11Constants.IAVertexInputResourceSlotCount;

        internal VertexElement[] Elements;

        public int Stride { get; private set; }

        public VertexDeclaration(params VertexElement[] elements)
        {
            if (elements == null) throw new ArgumentNullException("elements");
            if (elements.Length == 0) throw new ArgumentException("elements is empty", "elements");

            this.Elements = elements;

            foreach (var element in elements)
            {
                Stride += element.SizeInBytes;
            }
        }

        public VertexDeclaration(int stride, params VertexElement[] elements)
        {
            if (stride < 1) throw new ArgumentOutOfRangeException("stride");
            if (elements == null) throw new ArgumentNullException("elements");
            if (elements.Length == 0) throw new ArgumentException("elements is empty", "elements");

            Stride = stride;
            this.Elements = elements;
        }

        public VertexElement[] GetVertexElements()
        {
            return (VertexElement[]) Elements.Clone();
        }
    }
}
