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

        BloomExtract bloomExtract;

        BloomCombine bloomCombine;

        GaussianBlurCore gaussianBlur;

        RenderTarget sceneRenderTarget;

        RenderTarget bloomMapRenderTarget;
        
        RenderTarget interBlurRenderTarget;

        BloomSettings settings = BloomSettings.PresetSettings[0];

        public BloomSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

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

            bloomExtract = new BloomExtract(Device);
            bloomCombine = new BloomCombine(Device);
            gaussianBlur = new GaussianBlurCore(Device);

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

            bloomMapRenderTarget = Device.CreateRenderTarget();
            bloomMapRenderTarget.Width = width;
            bloomMapRenderTarget.Height = height;
            bloomMapRenderTarget.Format = format;
            bloomMapRenderTarget.Initialize();

            interBlurRenderTarget = Device.CreateRenderTarget();
            interBlurRenderTarget.Width = width;
            interBlurRenderTarget.Height = height;
            interBlurRenderTarget.Format = format;
            interBlurRenderTarget.Initialize();
        }

        protected override void UnloadContent()
        {
            sceneRenderTarget.Dispose();
            bloomMapRenderTarget.Dispose();
            interBlurRenderTarget.Dispose();
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
            var viewport = context.Viewport;

            // ブルーム マップの生成。
            bloomExtract.Threshold = Settings.BloomThreshold;
            DrawFullscreenQuad(
                sceneRenderTarget,
                bloomMapRenderTarget,
                bloomExtract.Apply,
                IntermediateBuffer.PreBloom);

            // ガウシアン ブラーの設定。
            // XNA の BlurAamount はガウス関数の sigma そのものに一致。
            gaussianBlur.Sigma = Settings.BlurAmount;

            // ガウシアン ブラーの水平パス。
            gaussianBlur.Pass = GaussianBlurPass.Horizon;
            DrawFullscreenQuad(
                bloomMapRenderTarget,
                interBlurRenderTarget,
                gaussianBlur.Apply,
                IntermediateBuffer.BlurredHorizontally);

            // ガウシアン ブラーの垂直パス。
            gaussianBlur.Pass = GaussianBlurPass.Vertical;
            DrawFullscreenQuad(
                interBlurRenderTarget,
                bloomMapRenderTarget,
                gaussianBlur.Apply,
                IntermediateBuffer.BlurredBothWays);
            
            context.SetRenderTarget(null);

            // ブルーム マップとシーンを合成。
            bloomCombine.BaseIntensity = Settings.BaseIntensity;
            bloomCombine.BaseSaturation = Settings.BaseSaturation;
            bloomCombine.BloomIntensity = Settings.BloomIntensity;
            bloomCombine.BloomSaturation = Settings.BloomSaturation;
            bloomCombine.BaseTexture = sceneRenderTarget.GetShaderResourceView();
            DrawFullscreenQuad(
                bloomMapRenderTarget,
                (int) viewport.Width,
                (int) viewport.Height,
                bloomCombine.Apply,
                IntermediateBuffer.FinalResult);
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
