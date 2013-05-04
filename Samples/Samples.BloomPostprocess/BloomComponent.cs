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

        BloomEffect bloomEffect;

        GaussianBlurEffect gaussianBlurEffect;

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

            bloomEffect = new BloomEffect(Device);
            gaussianBlurEffect = new GaussianBlurEffect(Device);

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

            // BloomEffect Extract パス。
            bloomEffect.BloomThreshold = Settings.BloomThreshold;
            bloomEffect.Pass = BloomEffectPass.Extract;
            DrawFullscreenQuad(sceneRenderTarget, renderTarget1, bloomEffect.Apply, IntermediateBuffer.PreBloom);

            // GaussianBlurEffect の設定。
            gaussianBlurEffect.Width = renderTarget1.Width;
            gaussianBlurEffect.Height = renderTarget1.Height;
            // XNA の BlurAamount はガウス関数の sigma そのものに一致。
            // Libra の amount は一般的に用いられる例のごとく sigma = radius / amount。
            // 一般的には amount を減らす程にぼかしを強くし、amount を増やす程にぼかしを弱くする。
            // しかし、amount を減らす程にぼかしが強くなるという設定はこれは混乱を招きやすいため、
            // XNA では amount を増やす程にぼかしが強くなる設定にしていると思われる。
            gaussianBlurEffect.Amount = 1.0f / Settings.BlurAmount;

            // GaussianBlurEffect Horizon パス。
            gaussianBlurEffect.Pass = GaussianBlurEffectPass.Horizon;
            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurEffect.Apply, IntermediateBuffer.BlurredHorizontally);

            // GaussianBlurEffect Vertical パス。
            gaussianBlurEffect.Pass = GaussianBlurEffectPass.Vertical;
            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurEffect.Apply, IntermediateBuffer.BlurredBothWays);

            context.SetRenderTarget(null);

            // BloomEffect Combine パス。
            bloomEffect.BloomIntensity = Settings.BloomIntensity;
            bloomEffect.BaseIntensity = Settings.BaseIntensity;
            bloomEffect.BloomSaturation = Settings.BloomSaturation;
            bloomEffect.BaseSaturation = Settings.BaseSaturation;
            bloomEffect.BloomTexture = renderTarget1.GetShaderResourceView();
            bloomEffect.BaseTexture = sceneRenderTarget.GetShaderResourceView();
            bloomEffect.Pass = BloomEffectPass.Combine;

            var viewport = context.Viewport;
            DrawFullscreenQuad(renderTarget1, (int) viewport.Width, (int) viewport.Height,
                bloomEffect.Apply, IntermediateBuffer.FinalResult);
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
