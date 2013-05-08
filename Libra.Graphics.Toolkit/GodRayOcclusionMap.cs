#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GodRayOcclusionMap : IDisposable
    {
        /// <summary>
        /// オブジェクトを描画する際に呼び出されるコールバック デリゲートです。
        /// コールバックを受けたクラスは、閉塞マップ エフェクトを用いてオブジェクトを描画します。
        /// 描画するオブジェクトの選択は、コールバックを受けたクラスが決定します。
        /// </summary>
        /// <param name="view">現在の表示カメラのビュー行列。</param>
        /// <param name="projection">現在の表示カメラの射影行列。</param>
        /// <param name="effect">閉塞マップ エフェクト。</param>
        public delegate void DrawObjectsCallback(Matrix view, Matrix projection, GodRayOcclusionMapEffect effect);

        /// <summary>
        /// 閉塞マップ エフェクト。
        /// </summary>
        GodRayOcclusionMapEffect depthMapEffect;

        /// <summary>
        /// 閉塞マップの幅。
        /// </summary>
        int width;

        /// <summary>
        /// 閉塞マップの高さ。
        /// </summary>
        int height;

        /// <summary>
        /// デバイスを取得します。
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// 閉塞マップの幅を取得または設定します。
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;

                width = value;

                InvalidateRenderTarget();
            }
        }

        /// <summary>
        /// 閉塞マップの高さを取得または設定します。
        /// </summary>
        public int Height
        {
            get { return height; }
            set
            {
                if (height == value) return;

                height = value;

                InvalidateRenderTarget();
            }
        }

        /// <summary>
        /// 閉塞マップが描画されるレンダ ターゲットを取得します。
        /// </summary>
        /// <remarks>
        /// レンダ ターゲットは、初回の Draw メソッドが呼び出されるまで生成されません。
        /// </remarks>
        public RenderTarget RenderTarget { get; private set; }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="device">デバイス。</param>
        public GodRayOcclusionMap(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            depthMapEffect = new GodRayOcclusionMapEffect(device);
        }

        /// <summary>
        /// 閉塞マップを描画します。
        /// </summary>
        /// <param name="context">デバイス コンテキスト。</param>
        /// <param name="view">表示カメラのビュー行列。</param>
        /// <param name="projection">表示カメラの射影行列。</param>
        /// <param name="drawObjects">オブジェクト描画コールバック。</param>
        public void Draw(DeviceContext context, Matrix view, Matrix projection, DrawObjectsCallback drawObjects)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (drawObjects == null) throw new ArgumentNullException("drawObjects");

            PrepareRenderTarget();

            context.DepthStencilState = DepthStencilState.Default;
            context.BlendState = BlendState.Opaque;

            // エフェクトを設定。
            depthMapEffect.View = view;
            depthMapEffect.Projection = projection;

            context.SetRenderTarget(RenderTarget.GetRenderTargetView());
            context.Clear(Color.White);

            // 描画をコールバック。
            // 描画するオブジェクトの選別は、コールバックされる側のクラスで決定。
            drawObjects(view, projection, depthMapEffect);

            context.SetRenderTarget(null);
        }

        void PrepareRenderTarget()
        {
            if (RenderTarget == null)
            {
                RenderTarget = Device.CreateRenderTarget();
                RenderTarget.Width = width;
                RenderTarget.Height = height;
                RenderTarget.Format = SurfaceFormat.Color;
                RenderTarget.RenderTargetUsage = RenderTargetUsage.Preserve;
                RenderTarget.Initialize();
            }
        }

        void InvalidateRenderTarget()
        {
            if (RenderTarget != null)
            {
                RenderTarget.Dispose();
                RenderTarget = null;
            }
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~GodRayOcclusionMap()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                depthMapEffect.Dispose();

                if (RenderTarget != null)
                    RenderTarget.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
