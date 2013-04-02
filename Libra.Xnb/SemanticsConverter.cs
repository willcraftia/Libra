#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    internal static class SemanticsConverter
    {
        static readonly string[] Mapping =
        {
            Semantics.SVPosition,
            Semantics.Color,
            Semantics.TexCoord,
            Semantics.Normal,
            Semantics.Binormal,
            Semantics.Tangent,
            Semantics.BlendIndices,
            Semantics.BlendWeight,
            Semantics.SVDepth,
            Semantics.Fog,
            Semantics.PSize,
            // TODO
            // これがよく分からない。合ってる？
            Semantics.SVSampleIndex,
            Semantics.SVTessFactor
        };

        public static string ToSemanticsName(int xnbValue)
        {
            if ((uint) (Mapping.Length - 1) < (uint) xnbValue)
                throw new NotSupportedException("Unknown vertex element usage: " + xnbValue);

            return Mapping[xnbValue];
        }
    }
}
