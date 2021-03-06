﻿#region Using

using System;
using Felis;

#endregion

namespace Libra.Xnb
{
    public sealed class RectangleBuilder : RectangleBuilderBase<Rectangle>
    {
        Rectangle instance;

        protected override void SetValues(int x, int y, int width, int height)
        {
            instance = new Rectangle(x, y, width, height);
        }

        protected override void Begin() { }

        protected override object End()
        {
            return instance;
        }
    }
}
