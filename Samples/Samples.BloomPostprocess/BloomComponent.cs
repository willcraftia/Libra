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

        GaussianBlurCore gaussianBlur;

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

            // ガウシアン ブラーの設定。
            // XNA の BlurAamount はガウス関数の sigma そのものに一致。
            // Libra の amount は一般的に用いられる例のごとく sigma = radius / amount。
            // 一般的には amount を減らす程にぼかしを強くし、amount を増やす程にぼかしを弱くする。
            // しかし、amount を減らす程にぼかしが強くなるという設定は混乱を招きやすいため、
            // XNA では amount を増やす程にぼかしが強くなる設定にしていると思われる。
            gaussianBlur.Amount = 1.0f / Settings.BlurAmount;

            // ガウシアン ブラーの水平パス。
            // SpriteBatch でビューはスロット #0 に設定されるが、
            // FullscreenQuad との互換性のために明示的に設定する必要がある。
            gaussianBlur.Texture = bloomMapRenderTarget.GetShaderResourceView();
            gaussianBlur.Pass = GaussianBlurPass.Horizon;
            context.SetRenderTarget(interBlurRenderTarget.GetRenderTargetView());
            DrawFullscreenQuad(bloomMapRenderTarget, interBlurRenderTarget.Width, interBlurRenderTarget.Height, gaussianBlur.Apply);
            context.SetRenderTarget(null);

            if (showBuffer < IntermediateBuffer.BlurredBothWays)
            {
                DrawFullscreenQuad(interBlurRenderTarget, (int) viewport.Width, (int) viewport.Height, null);
                return;
            }

            // ガウシアン ブラーの垂直パス。
            // SpriteBatch でビューはスロット #0 に設定されるが、
            // FullscreenQuad との互換性のために明示的に設定する必要がある。
            gaussianBlur.Texture = interBlurRenderTarget.GetShaderResourceView();
            gaussianBlur.Pass = GaussianBlurPass.Vertical;
            context.SetRenderTarget(bloomMapRenderTarget.GetRenderTargetView());
            DrawFullscreenQuad(interBlurRenderTarget, bloomMapRenderTarget.Width, bloomMapRenderTarget.Height, gaussianBlur.Apply);
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

            // XNA サンプルはブルーム マップを主体としているため、
            // ブルーム マップに対するポストプロセスとしてブラーと合成が処理されている。
            // このため、通常シーンのテクスチャはレジスタ #0 ではなく #1。
            // #0 はブルーム マップ。
            // Libra では通常シーンのテクスチャを主体としているため、
            // 通常シーンのテクスチャはレジスタ #0。
            // #1 はブルーム マップ。
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
