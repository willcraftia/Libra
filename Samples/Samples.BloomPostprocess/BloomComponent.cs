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

        Bloom bloom;

        GaussianBlurEffect gaussianBlurEffect;

        RenderTarget sceneRenderTarget;

        RenderTarget bloomMapRenderTarget;
        
        RenderTarget interBlurRenderTarget;

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

            bloomExtract = new BloomExtract(Device);
            bloom = new Bloom(Device);
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

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

            // ブルーム マップの生成。
            bloomExtract.Threshold = Settings.BloomThreshold;
            context.SetRenderTarget(bloomMapRenderTarget.GetRenderTargetView());
            DrawFullscreenQuad(sceneRenderTarget, bloomMapRenderTarget.Width, bloomMapRenderTarget.Height, bloomExtract.Apply);
            context.SetRenderTarget(null);

            if (showBuffer < IntermediateBuffer.BlurredHorizontally)
            {
                DrawFullscreenQuad(bloomMapRenderTarget, (int) viewport.Width, (int) viewport.Height, null);
                return;
            }

            // GaussianBlurEffect の設定。
            // XNA の BlurAamount はガウス関数の sigma そのものに一致。
            // Libra の amount は一般的に用いられる例のごとく sigma = radius / amount。
            // 一般的には amount を減らす程にぼかしを強くし、amount を増やす程にぼかしを弱くする。
            // しかし、amount を減らす程にぼかしが強くなるという設定は混乱を招きやすいため、
            // XNA では amount を増やす程にぼかしが強くなる設定にしていると思われる。
            gaussianBlurEffect.Width = bloomMapRenderTarget.Width;
            gaussianBlurEffect.Height = bloomMapRenderTarget.Height;
            gaussianBlurEffect.Amount = 1.0f / Settings.BlurAmount;

            // GaussianBlurEffect Horizon パス。
            gaussianBlurEffect.Pass = GaussianBlurEffectPass.Horizon;
            context.SetRenderTarget(interBlurRenderTarget.GetRenderTargetView());
            DrawFullscreenQuad(bloomMapRenderTarget, interBlurRenderTarget.Width, interBlurRenderTarget.Height, gaussianBlurEffect.Apply);
            context.SetRenderTarget(null);

            if (showBuffer < IntermediateBuffer.BlurredBothWays)
            {
                DrawFullscreenQuad(interBlurRenderTarget, (int) viewport.Width, (int) viewport.Height, null);
                return;
            }

            // GaussianBlurEffect Vertical パス。
            gaussianBlurEffect.Pass = GaussianBlurEffectPass.Vertical;
            context.SetRenderTarget(bloomMapRenderTarget.GetRenderTargetView());
            DrawFullscreenQuad(interBlurRenderTarget, bloomMapRenderTarget.Width, bloomMapRenderTarget.Height, gaussianBlurEffect.Apply);
            context.SetRenderTarget(null);

            if (showBuffer < IntermediateBuffer.FinalResult)
            {
                DrawFullscreenQuad(bloomMapRenderTarget, (int) viewport.Width, (int) viewport.Height, null);
                return;
            }

            // ブルーム マップとシーンを合成。
            bloom.BaseIntensity = Settings.BaseIntensity;
            bloom.BaseSaturation = Settings.BaseSaturation;
            bloom.BloomIntensity = Settings.BloomIntensity;
            bloom.BloomSaturation = Settings.BloomSaturation;
            bloom.BloomTexture = bloomMapRenderTarget.GetShaderResourceView();

            // XNA サンプルでは、中間レンダ ターゲットを表示するために、
            // シーンのテクスチャをレジスタ #0 ではなく #1 に設定していると思われる。
            // しかし、このサンプルに限定せずに考えた場合、
            // ポストプロセス毎に対象とする通常シーンの設定先が異なることは混乱を招きやすい。
            // Libra ではポストプロセスの対象となる通常シーンのテクスチャを #0 に統一して設定する。
            DrawFullscreenQuad(sceneRenderTarget, (int) viewport.Width, (int) viewport.Height, bloom.Apply);
        }

        void DrawFullscreenQuad(Texture2D texture, int width, int height, Action<DeviceContext> applyShader)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, applyShader);
            spriteBatch.Draw(texture.GetShaderResourceView(), new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
        }
    }
}
