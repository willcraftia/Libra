#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public abstract class LightCamera : Camera
    {
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
        protected Matrix invertEyeView;

        /// <summary>
        /// 表示カメラの射影行列の種類。
        /// </summary>
        protected ProjectionType eyeProjectionType;

        /// <summary>
        /// 表示カメラの射影行列の左クリップ面位置。
        /// </summary>
        protected float eyeProjectionLeft;

        /// <summary>
        /// 表示カメラの射影行列の右クリップ面位置。
        /// </summary>
        protected float eyeProjectionRight;

        /// <summary>
        /// 表示カメラの射影行列の下クリップ面位置。
        /// </summary>
        protected float eyeProjectionBottom;

        /// <summary>
        /// 表示カメラの射影行列の上クリップ面位置。
        /// </summary>
        protected float eyeProjectionTop;

        /// <summary>
        /// 表示カメラの射影行列の近クリップ面位置。
        /// </summary>
        protected float eyeProjectionNear;

        /// <summary>
        /// 表示カメラの射影行列の遠クリップ面位置。
        /// </summary>
        protected float eyeProjectionFar;

        /// <summary>
        /// 表示カメラの射影行列の視野角。
        /// </summary>
        protected float eyeProjectionFov;

        /// <summary>
        /// 表示カメラの射影行列のアスペクト比。
        /// </summary>
        protected float eyeProjectionAspectRatio;

        /// <summary>
        /// 表示カメラの視錐台。
        /// </summary>
        protected BoundingFrustum eyeFrustum;

        // ライトの方向 (進行方向)
        protected Vector3 lightDirection;

        /// <summary>
        /// ライトの方向 (進行方向) を取得または設定します。
        /// </summary>
        /// <remarks>
        /// ライト方向は単位ベクトルで指定します。
        /// </remarks>
        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set { lightDirection = value; }
        }

        protected LightCamera()
        {
            eyeView = Matrix.Identity;
            eyePosition = Vector3.Zero;
            eyeDirection = Vector3.Forward;
            eyeUp = Vector3.Up;
            eyeFrustum = new BoundingFrustum(Matrix.Identity);
            lightDirection = Vector3.Down;
        }

        public void Update(Matrix eyeView, Matrix eyeProjection, BoundingBox sceneBox)
        {
            this.eyeView = eyeView;
            this.eyeProjection = eyeProjection;
            this.sceneBox = sceneBox;

            Matrix.Invert(ref eyeView, out invertEyeView);

            eyePosition = invertEyeView.Translation;
            eyeDirection = invertEyeView.Forward;
            eyeDirection.Normalize();
            eyeUp = invertEyeView.Up;
            eyeUp.Normalize();

            // eyeProjection に正しい射影行列が設定されている事を仮定。
            if (eyeProjection.M44 == 1.0f)
            {
                // 正射影。
                eyeProjectionType = ProjectionType.Orthographic;
                eyeProjection.ExtractOrthographic(
                    out eyeProjectionLeft,
                    out eyeProjectionRight,
                    out eyeProjectionBottom,
                    out eyeProjectionTop,
                    out eyeProjectionNear,
                    out eyeProjectionFar);

                eyeProjectionFov = float.NaN;
                eyeProjectionAspectRatio = float.NaN;
            }
            else
            {
                // 透視射影。
                eyeProjectionType = ProjectionType.Perspective;
                eyeProjection.ExtractPerspective(
                    out eyeProjectionFov,
                    out eyeProjectionAspectRatio,
                    out eyeProjectionLeft,
                    out eyeProjectionRight,
                    out eyeProjectionBottom,
                    out eyeProjectionTop,
                    out eyeProjectionNear,
                    out eyeProjectionFar);
            }

            Matrix eyeViewProjection;
            Matrix.Multiply(ref eyeView, ref eyeProjection, out eyeViewProjection);

            eyeFrustum.Matrix = eyeViewProjection;

            Update();
        }

        protected abstract void Update();
    }
}
