﻿#region Using

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

        SkySphereEffect skySphereEffect;

        SphereMesh sphereMesh;

        Matrix view;

        Matrix projection;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

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

        public SkySphere(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            skySphereEffect = new SkySphereEffect(deviceContext);
            skySphereEffect.World = Matrix.Identity;
            sphereMesh = new SphereMesh(deviceContext, 1, 32);

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

            // 遠クリップ面への描画であるため、深度比較は LessEqual とする。
            DeviceContext.DepthStencilState = DepthStencilState.DepthReadLessEqual;
            // 内側 (背面) を描画。
            DeviceContext.RasterizerState = RasterizerState.CullFront;

            skySphereEffect.Apply();
            sphereMesh.Draw();

            // デフォルトへ戻す。
            DeviceContext.DepthStencilState = DepthStencilState.Default;
            DeviceContext.RasterizerState = RasterizerState.CullBack;
        }
    }
}
