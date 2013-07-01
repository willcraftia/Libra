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

        DeviceContext context;

        SpriteBatch spriteBatch;

        Postprocess postprocess;

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
            context = Device.ImmediateContext;

            spriteBatch = new SpriteBatch(context);

            bloomExtractFilter = new BloomExtractFilter(Device);
            bloomCombineFilter = new BloomCombineFilter(Device);
            gaussianFilter = new GaussianFilter(Device);
            gaussianFilterH = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Horizon);
            gaussianFilterV = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Vertical);
            downFilter = new DownFilter(Device);
            downFilter.WidthScale = 0.5f;
            downFilter.HeightScale = 0.5f;
            upFilter = new UpFilter(Device);
            upFilter.WidthScale = 2.0f;
            upFilter.HeightScale = 2.0f;

            var backBuffer = Device.BackBuffer;
            var width = backBuffer.Width;
            var height = backBuffer.Height;
            var format = backBuffer.Format;
            var multisampleCount = backBuffer.MultisampleCount;
            var depthFormat = backBuffer.DepthFormat;

            postprocess = new Postprocess(context);
            postprocess.Width = width;
            postprocess.Height = height;
            postprocess.Format = format;
            postprocess.MultisampleCount = multisampleCount;
            postprocess.Filters.Add(downFilter);
            postprocess.Filters.Add(bloomExtractFilter);
            postprocess.Filters.Add(gaussianFilterH);
            postprocess.Filters.Add(gaussianFilterV);
            postprocess.Filters.Add(upFilter);
            postprocess.Filters.Add(bloomCombineFilter);

            sceneRenderTarget = Device.CreateRenderTarget();
            sceneRenderTarget.Width = width;
            sceneRenderTarget.Height = height;
            sceneRenderTarget.PreferredMultisampleCount = multisampleCount;
            sceneRenderTarget.Format = format;
            sceneRenderTarget.DepthFormat = depthFormat;
            sceneRenderTarget.Initialize();
        }

        public void BeginDraw()
        {
            if (Visible)
            {
                context.SetRenderTarget(sceneRenderTarget);
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

            finalSceneTexture = postprocess.Draw(sceneRenderTarget);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(finalSceneTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
