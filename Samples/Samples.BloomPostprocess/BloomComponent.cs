#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Toolkit;

#endregion

namespace Samples.BloomPostprocess
{
    public sealed class BloomComponent : DrawableGameComponent
    {
        public enum IntermediateBuffer
        {
            PreBloom,
            BlurredHorizontally,
            BlurredBothWays,
            FinalResult,
        }

        DeviceContext deviceContext;

        SpriteBatch spriteBatch;

        FilterChain filterChain;

        BloomExtractFilter bloomExtractFilter;

        BloomCombineFilter bloomCombineFilter;

        GaussianFilter gaussianFilter;

        GaussianFilterPass gaussianFilterH;

        GaussianFilterPass gaussianFilterV;

        DownFilter downFilter;

        UpFilter upFilter;

        RenderTarget sceneRenderTarget;

        ShaderResourceView finalSceneTexture;

        BloomSettings settings = BloomSettings.PresetSettings[0];

        IntermediateBuffer showBuffer = IntermediateBuffer.FinalResult;

        public BloomSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        public IntermediateBuffer ShowBuffer
        {
            get { return showBuffer; }
            set { showBuffer = value; }
        }

        public BloomComponent(Game game)
            : base(game)
        {
            if (game == null)
                throw new ArgumentNullException("game");
        }

        protected override void LoadContent()
        {
            deviceContext = Device.ImmediateContext;

            spriteBatch = new SpriteBatch(deviceContext);

            bloomExtractFilter = new BloomExtractFilter(deviceContext);
            bloomCombineFilter = new BloomCombineFilter(deviceContext);
            gaussianFilter = new GaussianFilter(deviceContext);
            gaussianFilterH = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Horizon);
            gaussianFilterV = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Vertical);
            downFilter = new DownFilter(deviceContext);
            downFilter.WidthScale = 0.5f;
            downFilter.HeightScale = 0.5f;
            upFilter = new UpFilter(deviceContext);
            upFilter.WidthScale = 2.0f;
            upFilter.HeightScale = 2.0f;

            filterChain = new FilterChain(deviceContext);
            filterChain.Width = Device.BackBufferWidth;
            filterChain.Height = Device.BackBufferHeight;
            filterChain.Format = Device.BackBufferFormat;
            filterChain.PreferredMultisampleCount = Device.BackBufferMultisampleCount;
            filterChain.Filters.Add(downFilter);
            filterChain.Filters.Add(bloomExtractFilter);
            filterChain.Filters.Add(gaussianFilterH);
            filterChain.Filters.Add(gaussianFilterV);
            filterChain.Filters.Add(upFilter);
            filterChain.Filters.Add(bloomCombineFilter);

            sceneRenderTarget = Device.CreateRenderTarget();
            sceneRenderTarget.Width = Device.BackBufferWidth;
            sceneRenderTarget.Height = Device.BackBufferHeight;
            sceneRenderTarget.Format = Device.BackBufferFormat;
            sceneRenderTarget.PreferredMultisampleCount = Device.BackBufferMultisampleCount;
            sceneRenderTarget.DepthStencilEnabled = true;
            sceneRenderTarget.DepthStencilFormat = Device.BackBufferDepthStencilFormat;
            sceneRenderTarget.Initialize();
        }

        public void BeginDraw()
        {
            if (Visible)
            {
                deviceContext.SetRenderTarget(sceneRenderTarget);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            gaussianFilterH.Enabled = false;
            gaussianFilterV.Enabled = false;
            bloomCombineFilter.Enabled = false;

            if (IntermediateBuffer.BlurredHorizontally <= showBuffer)
                gaussianFilterH.Enabled = true;

            if (IntermediateBuffer.BlurredBothWays <= showBuffer)
                gaussianFilterV.Enabled = true;

            if (IntermediateBuffer.FinalResult == showBuffer)
                bloomCombineFilter.Enabled = true;

            bloomCombineFilter.BaseIntensity = Settings.BaseIntensity;
            bloomCombineFilter.BaseSaturation = Settings.BaseSaturation;
            bloomCombineFilter.BloomIntensity = Settings.BloomIntensity;
            bloomCombineFilter.BloomSaturation = Settings.BloomSaturation;
            bloomCombineFilter.BaseTexture = sceneRenderTarget;

            // XNA の BlurAamount はガウス関数の sigma そのものに一致。
            gaussianFilter.Sigma = Settings.BlurAmount;

            finalSceneTexture = filterChain.Draw(sceneRenderTarget);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(finalSceneTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
