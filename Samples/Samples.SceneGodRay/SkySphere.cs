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
        SkySphereEffect skySphereEffect;

        SphereMesh sphereMesh;

        Vector3 skyColor;

        bool sunVisible = true;

        float sunThreshold = 0.999f;

        Vector3[] frustumCorners = new Vector3[8];

        public Device Device { get; private set; }

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

        public SkySphere(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            skySphereEffect = new SkySphereEffect(device);
            skySphereEffect.World = Matrix.Identity;
            sphereMesh = new SphereMesh(device);
        }

        public void Draw(DeviceContext context, Matrix view, Matrix projection)
        {
            var localView = view;
            view.Translation = Vector3.Zero;

            // z = w を強制してファー クリップ面に描画。
            var localProjection = projection;
            localProjection.M13 = localProjection.M14;
            localProjection.M23 = localProjection.M24;
            localProjection.M33 = localProjection.M34;
            localProjection.M43 = localProjection.M44;

            skySphereEffect.View = localView;
            skySphereEffect.Projection = localProjection;

            // 深度は読み取り専用。
            // スカイ スフィアは最後に描画する前提。
            context.DepthStencilState = DepthStencilState.DepthRead;
            context.RasterizerState = RasterizerState.CullFront;

            skySphereEffect.Apply(context);
            sphereMesh.Draw(context);

            // デフォルトへ戻す。
            context.DepthStencilState = DepthStencilState.Default;
            context.RasterizerState = RasterizerState.CullBack;
        }
    }
}
