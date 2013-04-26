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

        /// <summary>
        /// 表示カメラのビュー行列。
        /// </summary>
        protected Matrix eyeView;

        /// <summary>
        /// 表示カメラの射影行列。
        /// </summary>
        protected Matrix eyeProjection;

        /// <summary>
        /// 表示シーン領域。
        /// </summary>
        protected BoundingBox sceneBox;

        /// <summary>
        /// 表示カメラの位置。
        /// </summary>
        protected Vector3 eyePosition;

        /// <summary>
        /// 表示カメラの方向。
        /// </summary>
        protected Vector3 eyeDirection;

        /// <summary>
        /// 表示カメラの UP ベクトル。
        /// </summary>
        protected Vector3 eyeUp;

        /// <summary>
        /// 表示カメラのビュー行列の逆行列。
        /// </summary>
        protected Matrix inverseEyeView;

        // ライトの方向 (進行方向)
        protected Vector3 lightDirection;

        /// <summary>
        /// ライトの進行方向を取得または設定します。
        /// </summary>
        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set { lightDirection = value; }
        }

        protected LightCamera()
        {
            LightView = Matrix.Identity;
            LightProjection = Matrix.Identity;
            eyeView = Matrix.Identity;
            eyePosition = Vector3.Zero;
            eyeDirection = Vector3.Forward;
            eyeUp = Vector3.Up;
            lightDirection = Vector3.Down;
        }

        public void Update(Matrix eyeView, Matrix eyeProjection, BoundingBox sceneBox)
        {
            this.eyeView = eyeView;
            this.eyeProjection = eyeProjection;
            this.sceneBox = sceneBox;

            Matrix.Invert(ref eyeView, out inverseEyeView);

            eyePosition = inverseEyeView.Translation;
            eyeDirection = inverseEyeView.Forward;
            eyeUp = inverseEyeView.Up;

            Update();
        }

        protected abstract void Update();
    }
}
