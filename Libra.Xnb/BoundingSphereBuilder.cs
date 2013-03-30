#region Using

using System;
using Felis.Xnb;

#endregion

namespace Libra.Xnb
{
    public sealed class BoundingSphereBuilder : BoundingSphereBuilderBase<BoundingSphere>
    {
        BoundingSphere instance;

        protected override void SetCenter(object value)
        {
            instance.Center = (Vector3) value;
        }

        protected override void SetRadius(float value)
        {
            instance.Radius = value;
        }

        protected override void Begin(object deviceContext)
        {
            instance = new BoundingSphere();
        }

        protected override object End()
        {
            return instance;
        }
    }
}
