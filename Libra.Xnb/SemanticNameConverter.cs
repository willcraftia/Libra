#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    internal static class SemanticNameConverter
    {
        #region XnbVertexElementUsage

        enum XnbVertexElementUsage
        {
            Position            = 0,
            Color               = 1,
            TextureCoordinate   = 2,
            Normal              = 3,
            Binormal            = 4,
            Tangent             = 5,
            BlendIndices        = 6,
            BlendWeight         = 7,
            Depth               = 8,
            Fog                 = 9,
            PointSize           = 10,
            Sample              = 11,
            TessellateFactor    = 12,
        }

        #endregion

        static readonly string[] Mapping =
        {
            InputElement.SemanticSVPosition,
            InputElement.SemanticColor,
            InputElement.SemanticTexCoord,
            InputElement.SemanticNormal,
            InputElement.SemanticBinormal,
            InputElement.SemanticTangent,
            InputElement.SemanticBlendIndices,
            InputElement.SemanticBlendWeight,
            InputElement.SemanticDepth,
            InputElement.SemanticFog,
            InputElement.SemanticPSize,
            // TODO
            // これがよく分からない。合ってる？
            InputElement.SemanticSampleIndex,
            InputElement.SemanticTessFactor
        };

        public static string ToSemanticsName(int xnbValue)
        {
            if ((uint) (Mapping.Length - 1) < (uint) xnbValue)
                throw new NotSupportedException("Unknown vertex element usage: " + xnbValue);


            switch ((XnbVertexElementUsage) xnbValue)
            {
                case XnbVertexElementUsage.Position:
                    return InputElement.SemanticSVPosition;
                case XnbVertexElementUsage.Color:
                    return InputElement.SemanticColor;
                case XnbVertexElementUsage.TextureCoordinate:
                    return InputElement.SemanticTexCoord;
                case XnbVertexElementUsage.Normal:
                    return InputElement.SemanticNormal;
                case XnbVertexElementUsage.Binormal:
                    return InputElement.SemanticBinormal;
                case XnbVertexElementUsage.Tangent:
                    return InputElement.SemanticTangent;
                case XnbVertexElementUsage.BlendIndices:
                    return InputElement.SemanticBlendIndices;
                case XnbVertexElementUsage.BlendWeight:
                    return InputElement.SemanticBlendWeight;
                case XnbVertexElementUsage.Depth:
                    return InputElement.SemanticDepth;
                case XnbVertexElementUsage.Fog:
                    return InputElement.SemanticFog;
                case XnbVertexElementUsage.PointSize:
                    return InputElement.SemanticPSize;
                case XnbVertexElementUsage.Sample:
                    // TODO
                    // これがよく分からない。合ってる？
                    return InputElement.SemanticSampleIndex;
                case XnbVertexElementUsage.TessellateFactor:
                    return InputElement.SemanticTessFactor;
            }

            throw new NotSupportedException("Unknown vertex element usage: " + xnbValue);
        }
    }
}
