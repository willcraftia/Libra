#region Using

using System;
using Felis.Xnb;

#endregion

namespace Libra.Xnb
{
    public sealed class MatrixBuilder : MatrixBuilderBase<Matrix>
    {
        Matrix instance;

        protected override void SetValues(
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            instance = new Matrix(
                m11, m12, m13, m14,
                m21, m22, m23, m24,
                m31, m32, m33, m34,
                m41, m42, m43, m44);
        }

        protected override void Begin(object deviceContext) { }

        protected override object End()
        {
            return instance;
        }
    }
}
