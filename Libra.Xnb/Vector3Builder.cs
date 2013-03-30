#region Using

using System;
using Felis.Xnb;

#endregion

namespace Libra.Xnb
{
    public sealed class Vector3Builder : Vector3BuilderBase<Vector3>
    {
        Vector3 instance;

        protected override void SetValues(float x, float y, float z)
        {
            instance = new Vector3(x, y, z);
        }

        protected override void Begin(object deviceContext) { }

        protected override object End()
        {
            return instance;
        }
    }
}
