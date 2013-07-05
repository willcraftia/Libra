#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LuminanceExtractor : IDisposable
    {
        enum DirtyFlags
        {
            RenderTargets = (1 << 0)
        }

        int width = 1;

        int height = 1;

        FullScreenQuad fullScreenQuad;

        RenderTarget[] luminanceRenderTargetChain;

        RenderTarget currentLuminanceRenderTarget;

        RenderTarget currentAdaptedLuminanceRenderTarget;

        RenderTarget lastAdaptedLuminanceRenderTarget;

        DownFilter downFilter;

        LuminanceLogFilter luminanceLogFilter;

        LuminanceAverageFilter luminanceAverageFilter;

        LuminanceAdaptFilter luminanceAdaptFilter;

        DirtyFlags dirtyFlags = DirtyFlags.RenderTargets;

        public DeviceContext DeviceContext { get; private set; }

        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (width == value) return;

                width = value;

                dirtyFlags |= DirtyFlags.RenderTargets;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (height == value) return;

                height = value;

                dirtyFlags |= DirtyFlags.RenderTargets;
            }
        }

        public SurfaceFormat Format { get; set; }

        public ShaderResourceView LuminanceAverageMap
        {
            get { return currentAdaptedLuminanceRenderTarget; }
        }

        public LuminanceExtractor(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            fullScreenQuad = new FullScreenQuad(deviceContext);

            downFilter = new DownFilter(deviceContext);
            luminanceLogFilter = new LuminanceLogFilter(deviceContext);
            luminanceAverageFilter = new LuminanceAverageFilter(deviceContext);
            luminanceAdaptFilter = new LuminanceAdaptFilter(deviceContext);
        }

        //
        // ダウンスケール済みテクスチャを指定する想定。
        // ダウンスケール済みテクスチャは、
        // 輝度抽出の後に行われるであろうブライト パス合成フィルタでも用いる。
        //

        public void Extract(GameTime gameTime, ShaderResourceView texture)
        {
            if (texture == null) throw new ArgumentNullException("texture");

            // レンダ ターゲットの準備。
            PrepareRenderTargets();

            // 現フレームと前フレームの平均輝度レンダ ターゲットを交換。
            SwapAdaptedLuminanceRenderTargets();

            // 平均輝度の算出。
            CalculateAverageLuminance(gameTime, texture);
        }

        void PrepareRenderTargets()
        {
            if ((dirtyFlags & DirtyFlags.RenderTargets) != 0)
            {
                DisposeRenderTargets();
                
                int chainCount = 1;
                int size = 1;
                for (; size <= width || size <= height; size *= 4)
                    chainCount++;

                luminanceRenderTargetChain = new RenderTarget[chainCount];

                size /= 4;
                for (int i = 0; i < luminanceRenderTargetChain.Length; i++)
                {
                    luminanceRenderTargetChain[i] = DeviceContext.Device.CreateRenderTarget();
                    luminanceRenderTargetChain[i].Width = size;
                    luminanceRenderTargetChain[i].Height = size;
                    luminanceRenderTargetChain[i].Format = SurfaceFormat.Single;
                    luminanceRenderTargetChain[i].Initialize();
                    size /= 4;
                }

                currentLuminanceRenderTarget = DeviceContext.Device.CreateRenderTarget();
                currentLuminanceRenderTarget.Width = 1;
                currentLuminanceRenderTarget.Height = 1;
                currentLuminanceRenderTarget.Format = SurfaceFormat.Single;
                currentLuminanceRenderTarget.Initialize();

                currentAdaptedLuminanceRenderTarget = DeviceContext.Device.CreateRenderTarget();
                currentAdaptedLuminanceRenderTarget.Width = 1;
                currentAdaptedLuminanceRenderTarget.Height = 1;
                currentAdaptedLuminanceRenderTarget.Format = SurfaceFormat.Single;
                currentAdaptedLuminanceRenderTarget.Initialize();

                lastAdaptedLuminanceRenderTarget = DeviceContext.Device.CreateRenderTarget();
                lastAdaptedLuminanceRenderTarget.Width = 1;
                lastAdaptedLuminanceRenderTarget.Height = 1;
                lastAdaptedLuminanceRenderTarget.Format = SurfaceFormat.Single;
                lastAdaptedLuminanceRenderTarget.Initialize();

                dirtyFlags &= ~DirtyFlags.RenderTargets;
            }
        }

        void SwapAdaptedLuminanceRenderTargets()
        {
            var temp = lastAdaptedLuminanceRenderTarget;
            lastAdaptedLuminanceRenderTarget = currentAdaptedLuminanceRenderTarget;
            currentAdaptedLuminanceRenderTarget = temp;
        }

        void CalculateAverageLuminance(GameTime gameTime, ShaderResourceView texture)
        {
            //
            // downscaleRenderTarget から輝度を抽出し、luminanceRenderTargetChain[0] へ格納。
            // レンダ ターゲットには log(delta + luminance) が描画される。
            //

            DeviceContext.SetRenderTarget(luminanceRenderTargetChain[0]);

            luminanceLogFilter.Texture = texture;
            luminanceLogFilter.Apply();
            fullScreenQuad.Draw();

            DeviceContext.SetRenderTarget(null);

            //
            // luminanceRenderTargetChain[0] のダウンスケールを順に処理。
            // log(delta + luminance) の平均化。
            //

            for (int i = 1; i < luminanceRenderTargetChain.Length; i++)
            {
                DeviceContext.SetRenderTarget(luminanceRenderTargetChain[i]);

                downFilter.WidthScale = 0.25f;
                downFilter.HeightScale = 0.25f;
                downFilter.Texture = luminanceRenderTargetChain[i - 1];
                downFilter.Apply();
                fullScreenQuad.Draw();

                DeviceContext.SetRenderTarget(null);
            }

            //
            // 1x1 の luminanceRenderTargetChain から最終的な平均値を算出。
            // exp(sigma(log(delta + luminance)))。
            //

            DeviceContext.SetRenderTarget(currentLuminanceRenderTarget);

            luminanceAverageFilter.Texture = luminanceRenderTargetChain[luminanceRenderTargetChain.Length - 1];
            luminanceAverageFilter.Apply();
            fullScreenQuad.Draw();

            DeviceContext.SetRenderTarget(null);

            //
            // currentLuminanceRenderTarget と lastAdaptedLuminanceRenderTarget の差異から
            // currentAdaptedLuminanceRenderTarget を生成。
            //

            DeviceContext.SetRenderTarget(currentAdaptedLuminanceRenderTarget);

            luminanceAdaptFilter.Texture = currentLuminanceRenderTarget;
            luminanceAdaptFilter.LastTexture = lastAdaptedLuminanceRenderTarget;
            luminanceAdaptFilter.DeltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            luminanceAdaptFilter.Apply();
            fullScreenQuad.Draw();

            DeviceContext.SetRenderTarget(null);
        }

        void DisposeRenderTargets()
        {
            if (luminanceRenderTargetChain != null)
            {
                foreach (var renderTarget in luminanceRenderTargetChain)
                    renderTarget.Dispose();
            }

            if (currentLuminanceRenderTarget != null)
                currentLuminanceRenderTarget.Dispose();

            if (currentAdaptedLuminanceRenderTarget != null)
                currentAdaptedLuminanceRenderTarget.Dispose();

            if (lastAdaptedLuminanceRenderTarget != null)
                lastAdaptedLuminanceRenderTarget.Dispose();
        }

        #region IDisposable

        bool disposed;

        ~LuminanceExtractor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                fullScreenQuad.Dispose();
                downFilter.Dispose();
                luminanceLogFilter.Dispose();
                luminanceAverageFilter.Dispose();
                luminanceAdaptFilter.Dispose();

                DisposeRenderTargets();
            }

            disposed = true;
        }

        #endregion
    }
}
