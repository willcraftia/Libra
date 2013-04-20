#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public abstract class LightCamera
    {
        // 光源カメラのビュー行列
        public Matrix LightView;

        // 光源カメラの射影行列
        public Matrix LightProjection;

        // 光源カメラのビュー×射影行列
        public Matrix LightViewProjection;

        protected LightCamera()
        {
            LightView = Matrix.Identity;
            LightProjection = Matrix.Identity;
            LightViewProjection = Matrix.Identity;
        }

        public abstract void Update();
    }
}
