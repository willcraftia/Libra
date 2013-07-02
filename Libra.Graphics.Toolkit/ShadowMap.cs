#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// シャドウ マップを描画するクラスです。
    /// </summary>
    /// <remarks>
    /// このクラスはシャドウ マップの描画に専念するため、
    /// VSM 形式でシャドウ マップを生成する場合、
    /// 別途、このクラスで描画したシャドウ マップに対してブラーを適用する必要があります。
    /// </remarks>
    public sealed class ShadowMap : IDisposable
    {
        #region DirtyFlags

        enum DirtyFlags
        {
            RenderTarget = (1 << 0)
        }

        #endregion

        /// <summary>
        /// シャドウ マップ エフェクト。
        /// </summary>
        ShadowMapEffect shadowMapEffect;

        /// <summary>
        /// シャドウ マップのサイズ。
        /// </summary>
        int size = 1024;

        /// <summary>
        /// ダーティ フラグ。
        /// </summary>
        DirtyFlags dirtyFlags = DirtyFlags.RenderTarget;

        /// <summary>
        /// デバイス コンテキストを取得します。
        /// </summary>
        public DeviceContext DeviceContext { get; private set; }

        /// <summary>
        /// シャドウ マップ形式を取得または設定します。
        /// </summary>
        public ShadowMapForm Form
        {
            get { return shadowMapEffect.Form; }
            set
            {
                if (shadowMapEffect.Form == value) return;

                var previous = shadowMapEffect.Form;

                shadowMapEffect.Form = value;

                // VSM に関する切り替えならばレンダ ターゲットの再作成が必要。
                if (previous == ShadowMapForm.Variance ||
                    shadowMapEffect.Form == ShadowMapForm.Variance)
                {
                    dirtyFlags |= DirtyFlags.RenderTarget;
                }
            }
        }

        /// <summary>
        /// シャドウ マップのサイズを取得または設定します。
        /// </summary>
        public int Size
        {
            get { return size; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (size == value) return;

                size = value;

                dirtyFlags |= DirtyFlags.RenderTarget;
            }
        }

        /// <summary>
        /// ライト カメラのビュー行列を取得または設定します。
        /// </summary>
        public Matrix View
        {
            get { return shadowMapEffect.View; }
            set { shadowMapEffect.View = value; }
        }

        /// <summary>
        /// ライト カメラの射影行列を取得または設定します。
        /// </summary>
        public Matrix Projection
        {
            get { return shadowMapEffect.Projection; }
            set { shadowMapEffect.Projection = value; }
        }

        /// <summary>
        /// 投影オブジェクト描画コールバックを取得または設定します。
        /// </summary>
        public DrawShadowCastersCallback DrawShadowCastersCallback { get; set; }

        /// <summary>
        /// シャドウ マップが描画されるレンダ ターゲットを取得します。
        /// 分散シャドウ マップを用いる場合、このクラスで深度を描画した後に、
        /// 別途、生成されたシャドウ マップに対してブラーを適用する必要があります。
        /// </summary>
        /// <remarks>
        /// レンダ ターゲットは、初回の Draw メソッドが呼び出されるまで生成されません。
        /// また、シャドウ マップ形式を変更した場合、
        /// Draw メソッドでレンダ ターゲットが再生成される可能性があります。
        /// </remarks>
        public RenderTarget RenderTarget { get; private set; }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="device">デバイス コンテキスト。</param>
        public ShadowMap(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            shadowMapEffect = new ShadowMapEffect(deviceContext);
        }

        /// <summary>
        /// シャドウ マップを描画します。
        /// </summary>
        public void Draw()
        {
            PrepareRenderTarget();
            DrawDepth();
        }

        void PrepareRenderTarget()
        {
            if ((dirtyFlags & DirtyFlags.RenderTarget) != 0)
            {
                if (RenderTarget != null)
                    RenderTarget.Dispose();

                var format = SurfaceFormat.Single;
                if (shadowMapEffect.Form == ShadowMapForm.Variance)
                    format = SurfaceFormat.Vector2;

                RenderTarget = DeviceContext.Device.CreateRenderTarget();
                RenderTarget.Width = size;
                RenderTarget.Height = size;
                RenderTarget.Format = format;
                RenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
                RenderTarget.RenderTargetUsage = RenderTargetUsage.Preserve;
                RenderTarget.Initialize();

                dirtyFlags &= ~DirtyFlags.RenderTarget;
            }
        }

        void DrawDepth()
        {
            DeviceContext.RasterizerState = RasterizerState.CullNone;
            DeviceContext.DepthStencilState = null;
            DeviceContext.BlendState = null;

            DeviceContext.SetRenderTarget(RenderTarget);
            DeviceContext.Clear(Color.White);

            // 描画をコールバック。
            // 描画する投影オブジェクトの選別は、コールバックされる側のクラスで決定。
            if (DrawShadowCastersCallback != null)
                DrawShadowCastersCallback(shadowMapEffect);

            DeviceContext.SetRenderTarget(null);

            DeviceContext.RasterizerState = null;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~ShadowMap()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                shadowMapEffect.Dispose();

                if (RenderTarget != null)
                    RenderTarget.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
