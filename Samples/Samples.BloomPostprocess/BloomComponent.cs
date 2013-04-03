#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Graphics.Toolkit;

#endregion

namespace Samples.BloomPostprocess
{
    public sealed class BloomComponent : DrawableGameComponent
    {
        #region BloomExtractShader

        sealed class BloomExtractShader
        {
            public float BloomThreshold;

            ConstantBuffer constantBuffer;

            PixelShader pixelShader;

            public BloomExtractShader(Device device, byte[] bytecode)
            {
                pixelShader = device.CreatePixelShader();
                pixelShader.Initialize(bytecode);

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Initialize(16);
            }

            public void Apply(DeviceContext context)
            {
                constantBuffer.SetData(context, BloomThreshold);
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.PixelShader = pixelShader;
            }
        }

        #endregion

        #region BloomCombineShader

        sealed class BloomCombineShader
        {
            public float BloomIntensity;

            public float BaseIntensity;

            public float BloomSaturation;

            public float BaseSaturation;

            ConstantBuffer constantBuffer;

            PixelShader pixelShader;

            public BloomCombineShader(Device device, byte[] bytecode)
            {
                pixelShader = device.CreatePixelShader();
                pixelShader.Initialize(bytecode);

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Initialize(16);
            }

            public void Apply(DeviceContext context)
            {
                var constatns = new Vector4
                {
                    X = BloomIntensity,
                    Y = BaseIntensity,
                    Z = BloomSaturation,
                    W = BaseSaturation
                };

                constantBuffer.SetData(context, constatns);
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.PixelShader = pixelShader;
            }
        }

        #endregion

        const int MaxSampleCount = 15;

        SpriteBatch spriteBatch;

        BloomExtractShader bloomExtractShader;

        BloomCombineShader bloomCombineShader;

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

            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "../../Shaders";

            bloomExtractShader = new BloomExtractShader(Device, compiler.CompilePixelShader("BloomExtract.fx"));
            bloomCombineShader = new BloomCombineShader(Device, compiler.CompilePixelShader("BloomCombine.fx"));

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

            bloomExtractShader.BloomThreshold = Settings.BloomThreshold;

            DrawFullscreenQuad(sceneRenderTarget, renderTarget1, bloomExtractShader.Apply, IntermediateBuffer.PreBloom);

            // ガウス ブラー シェーダへのレンダ ターゲットのサイズの設定。
            gaussianBlurShader.Width = renderTarget1.Width;
            gaussianBlurShader.Height = renderTarget1.Height;

            // 水平方向のブラー。
            gaussianBlurShader.Direction = BlurDirection.Horizon;
            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurShader.Apply, IntermediateBuffer.BlurredHorizontally);

            // 垂直方向のブラー。
            gaussianBlurShader.Direction = BlurDirection.Vertical;
            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurShader.Apply, IntermediateBuffer.BlurredBothWays);

            context.SetRenderTarget(null);

            bloomCombineShader.BloomIntensity = Settings.BloomIntensity;
            bloomCombineShader.BaseIntensity = Settings.BaseIntensity;
            bloomCombineShader.BloomSaturation = Settings.BloomSaturation;
            bloomCombineShader.BaseSaturation = Settings.BaseSaturation;

            context.PixelShaderResources[1] = sceneRenderTarget.GetShaderResourceView();

            var viewport = context.Viewport;

            DrawFullscreenQuad(renderTarget1, (int) viewport.Width, (int) viewport.Height,
                bloomCombineShader.Apply, IntermediateBuffer.FinalResult);

            // レンダ ターゲットをシェーダ リソースとして設定しているため、
            // 必ず明示的にシェーダから解除しなければならない。
            context.PixelShaderResources[1] = null;
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
