#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class Camera
    {
        /// <summary>
        /// ビュー行列。
        /// </summary>
        public Matrix View;

        /// <summary>
        /// 射影行列。
        /// </summary>
        public Matrix Projection;

        public Camera()
        {
            View = Matrix.Identity;
            Projection = Matrix.Identity;
        }
    }
}
