#region Using

using System;
using Libra;
using Libra.Graphics;
using Libra.Graphics.Toolkit;

#endregion

namespace Samples.SceneGodRay
{
    public sealed class SkySphere
    {
        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            LocalView       = (1 << 0),
            LocalProjection = (1 << 1)
        }

        #endregion

        /// <summary>
        /// DepthFunction に LessEqual を用いる読み取り専用深度ステンシル ステート。
        /// Libra の DepthRead は Less (D3D11 デフォルト) を深度比較に用いるため、
        /// DepthRead では SkySphere の射影を遠クリップ面に強制する際に
        /// SkySphere の頂点深度が深度ステンシルのデフォルト深度 1 と等価となり、描画から除外されてしまう。
        /// そこで、深度比較を LessEqual とし、深度 1 に等しいなら描画対象にする。
        /// </summary>
        static readonly DepthStencilState DepthReadWithLessEqual = new DepthStencilState
        {
            DepthEnable = true,
            DepthWriteEnable = false,
            DepthFunction = ComparisonFunction.LessEqual,
            Name = "SkySphere.DepthReadWithLessEqual"
        };

        DeviceContext context;

        SkySphereEffect skySphereEffect;

        SphereMesh sphereMesh;

        Matrix view;

        Matrix projection;

        DirtyFlags dirtyFlags;

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.LocalView;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.LocalProjection;
            }
        }

        public Vector3 SkyColor
        {
            get { return skySphereEffect.SkyColor; }
            set { skySphereEffect.SkyColor = value; }
        }

        public Vector3 SunDirection
        {
            get { return skySphereEffect.SunDirection; }
            set { skySphereEffect.SunDirection = value; }
        }

        public Vector3 SunColor
        {
            get { return skySphereEffect.SunColor; }
            set { skySphereEffect.SunColor = value; }
        }

        public float SunThreshold
        {
            get { return skySphereEffect.SunThreshold; }
            set { skySphereEffect.SunThreshold = value; }
        }

        public bool SunVisible
        {
            get { return skySphereEffect.SunVisible; }
            set { skySphereEffect.SunVisible = value; }
        }

        public SkySphere(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            skySphereEffect = new SkySphereEffect(context.Device);
            skySphereEffect.World = Matrix.Identity;
            sphereMesh = new SphereMesh(context, 1, 32);

            dirtyFlags |= DirtyFlags.LocalView | DirtyFlags.LocalProjection;
        }

        public void Draw()
        {
            if ((dirtyFlags & DirtyFlags.LocalView) != 0)
            {
                var localView = view;
                localView.Translation = Vector3.Zero;

                skySphereEffect.View = localView;

                dirtyFlags &= ~DirtyFlags.LocalView;
            }

            if ((dirtyFlags & DirtyFlags.LocalProjection) != 0)
            {
                // z = w を強制して遠クリップ面に描画。
                var localProjection = projection;
                localProjection.M13 = localProjection.M14;
                localProjection.M23 = localProjection.M24;
                localProjection.M33 = localProjection.M34;
                localProjection.M43 = localProjection.M44;

                skySphereEffect.Projection = localProjection;

                dirtyFlags &= ~DirtyFlags.LocalProjection;
            }

            // 読み取り専用深度かつ深度比較 LessEqual。
            context.DepthStencilState = DepthReadWithLessEqual;
            // 内側 (背面) を描画。
            context.RasterizerState = RasterizerState.CullFront;

            skySphereEffect.Apply(context);
            sphereMesh.Draw();

            // デフォルトへ戻す。
            context.DepthStencilState = DepthStencilState.Default;
            context.RasterizerState = RasterizerState.CullBack;
        }
    }
}
