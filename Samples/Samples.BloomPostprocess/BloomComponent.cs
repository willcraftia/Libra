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
        SpriteBatch spriteBatch;

        BloomShader bloomShader;

        GaussianBlurShader gaussianBlurShader;

        RenderTarget sceneRenderTarget;

        RenderTarget renderTarget1;
        
        RenderTarget renderTarget2;

        public BloomSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        BloomSettings settings = BloomSettings.PresetSettings[0];

        public enum IntermediateBuffer
        {
            PreBloom,
            BlurredHorizontally,
            BlurredBothWays,
            FinalResult,
        }

        public IntermediateBuffer ShowBuffer
        {
            get { return showBuffer; }
            set { showBuffer = value; }
        }

        IntermediateBuffer showBuffer = IntermediateBuffer.FinalResult;

        public BloomComponent(Game game)
            : base(game)
        {
            if (game == null)
                throw new ArgumentNullException("game");
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);

            bloomShader = new BloomShader(Device);
            gaussianBlurShader = new GaussianBlurShader(Device);

            var backBuffer = Device.BackBuffer;
            var width = backBuffer.Width;
            var height = backBuffer.Height;
            var format = backBuffer.Format;

            sceneRenderTarget = Device.CreateRenderTarget();
            sceneRenderTarget.Width = width;
            sceneRenderTarget.Height = height;
            sceneRenderTarget.MultisampleCount = backBuffer.MultisampleCount;
            sceneRenderTarget.Format = format;
            sceneRenderTarget.DepthFormat = backBuffer.DepthFormat;
            sceneRenderTarget.Initialize();

            width /= 2;
            height /= 2;

            renderTarget1 = Device.CreateRenderTarget();
            renderTarget1.Width = width;
            renderTarget1.Height = height;
            renderTarget1.Format = format;
            renderTarget1.Initialize();

            renderTarget2 = Device.CreateRenderTarget();
            renderTarget2.Width = width;
            renderTarget2.Height = height;
            renderTarget2.Format = format;
            renderTarget2.Initialize();
        }

        protected override void UnloadContent()
        {
            sceneRenderTarget.Dispose();
            renderTarget1.Dispose();
            renderTarget2.Dispose();
        }

        public void BeginDraw()
        {
            if (Visible)
            {
                Device.ImmediateContext.SetRenderTarget(sceneRenderTarget.GetRenderTargetView());
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

            // ブルーム シェーダの Extract パス。
            bloomShader.BloomThreshold = Settings.BloomThreshold;
            bloomShader.Pass = BloomShaderPass.Extract;
            DrawFullscreenQuad(sceneRenderTarget, renderTarget1, bloomShader.Apply, IntermediateBuffer.PreBloom);

            // ガウス ブラー シェーダへのサイズの設定。
            gaussianBlurShader.Width = renderTarget1.Width;
            gaussianBlurShader.Height = renderTarget1.Height;

            // ガウス ブラー シェーダ の Horizon パス。
            gaussianBlurShader.Pass = GaussianBlurShaderPass.Horizon;
            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurShader.Apply, IntermediateBuffer.BlurredHorizontally);

            // ガウス ブラー シェーダ の Vertical パス。
            gaussianBlurShader.Pass = GaussianBlurShaderPass.Vertical;
            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurShader.Apply, IntermediateBuffer.BlurredBothWays);

            context.SetRenderTarget(null);

            // ブルーム シェーダの Combine パス。
            bloomShader.BloomIntensity = Settings.BloomIntensity;
            bloomShader.BaseIntensity = Settings.BaseIntensity;
            bloomShader.BloomSaturation = Settings.BloomSaturation;
            bloomShader.BaseSaturation = Settings.BaseSaturation;
            bloomShader.BloomTexture = renderTarget1.GetShaderResourceView();
            bloomShader.BaseTexture = sceneRenderTarget.GetShaderResourceView();
            bloomShader.Pass = BloomShaderPass.Combine;

            var viewport = context.Viewport;
            DrawFullscreenQuad(renderTarget1, (int) viewport.Width, (int) viewport.Height,
                bloomShader.Apply, IntermediateBuffer.FinalResult);
        }

        void DrawFullscreenQuad(Texture2D texture, RenderTarget renderTarget,
            Action<DeviceContext> applyShader, IntermediateBuffer currentBuffer)
        {
            Device.ImmediateContext.SetRenderTarget(renderTarget.GetRenderTargetView());

            DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, applyShader, currentBuffer);
        }

        void DrawFullscreenQuad(Texture2D texture, int width, int height,
            Action<DeviceContext> applyShader, IntermediateBuffer currentBuffer)
        {
            if (showBuffer < currentBuffer)
            {
                applyShader = null;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, applyShader);
            spriteBatch.Draw(texture.GetShaderResourceView(), new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
        }
    }
}
