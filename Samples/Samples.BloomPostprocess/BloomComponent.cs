#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;

#endregion

namespace Samples.BloomPostprocess
{
    public sealed class BloomComponent : DrawableGameComponent
    {
        #region BloomExtractConstants

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct BloomExtractConstants
        {
            public float BloomThreshold;
        }

        #endregion

        #region GaussianBlurSample

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct GaussianBlurSample
        {
            public Vector2 Offset;

            public float Weight;
        }

        #endregion

        #region BloomCombineConstants

        struct BloomCombineConstants
        {
            public float BloomIntensity;

            public float BaseIntensity;

            public float BloomSaturation;

            public float BaseSaturation;
        }

        #endregion

        #region CustomShader

        sealed class CustomShader<T> where T : struct
        {
            public T Constants;

            ConstantBuffer constantBuffer;

            PixelShader pixelShader;

            public CustomShader(IDevice device, byte[] bytecode)
            {
                pixelShader = device.CreatePixelShader();
                pixelShader.Initialize(bytecode);

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Initialize<T>();
            }

            public void Apply(DeviceContext context)
            {
                constantBuffer.SetData(context, Constants);
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.PixelShader = pixelShader;
            }
        }

        #endregion

        sealed class GaussianBlurShader
        {
            public GaussianBlurSample[] Samples;

            ConstantBuffer constantBuffer;

            PixelShader pixelShader;

            public GaussianBlurShader(IDevice device, byte[] bytecode)
            {
                Samples = new GaussianBlurSample[15];

                pixelShader = device.CreatePixelShader();
                pixelShader.Initialize(bytecode);

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Initialize(Marshal.SizeOf(typeof(GaussianBlurSample)) * 15);
            }

            public void Apply(DeviceContext context)
            {
                constantBuffer.SetData(context, Samples);
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.PixelShader = pixelShader;
            }
        }

        SpriteBatch spriteBatch;

        CustomShader<BloomExtractConstants> bloomExtractShader;

        GaussianBlurShader gaussianBlurShader;

        CustomShader<BloomCombineConstants> bloomCombineShader;

        ConstantBuffer bloomExtractConstantBuffer;

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

            bloomExtractShader = new CustomShader<BloomExtractConstants>(
                Device, compiler.CompilePixelShader("BloomExtract.fx"));

            bloomCombineShader = new CustomShader<BloomCombineConstants>(
                Device, compiler.CompilePixelShader("BloomCombine.fx"));

            gaussianBlurShader = new GaussianBlurShader(
                Device, compiler.CompilePixelShader("GaussianBlur.fx"));

            bloomExtractConstantBuffer = Device.CreateConstantBuffer();
            bloomExtractConstantBuffer.Initialize<BloomExtractConstants>();

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

            bloomExtractShader.Constants.BloomThreshold = Settings.BloomThreshold;

            DrawFullscreenQuad(sceneRenderTarget, renderTarget1, bloomExtractShader.Apply, IntermediateBuffer.PreBloom);

            SetBlurEffectParameters(1.0f / (float) renderTarget1.Width, 0);

            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurShader.Apply, IntermediateBuffer.BlurredHorizontally);

            SetBlurEffectParameters(0, 1.0f / (float) renderTarget1.Height);

            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurShader.Apply, IntermediateBuffer.BlurredBothWays);

            context.SetRenderTarget(null);

            bloomCombineShader.Constants.BloomIntensity = Settings.BloomIntensity;
            bloomCombineShader.Constants.BaseIntensity = Settings.BaseIntensity;
            bloomCombineShader.Constants.BloomSaturation = Settings.BloomSaturation;
            bloomCombineShader.Constants.BaseSaturation = Settings.BaseSaturation;

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

        void SetBlurEffectParameters(float dx, float dy)
        {
            int sampleCount = gaussianBlurShader.Samples.Length;

            float totalWeights = ComputeGaussian(0);

            gaussianBlurShader.Samples[0].Weight = totalWeights;
            gaussianBlurShader.Samples[0].Offset = Vector2.Zero;

            for (int i = 0; i < sampleCount / 2; i++)
            {
                float weight = ComputeGaussian(i + 1);

                gaussianBlurShader.Samples[i * 2 + 1].Weight = weight;
                gaussianBlurShader.Samples[i * 2 + 2].Weight = weight;

                totalWeights += weight * 2;

                float sampleOffset = i * 2 + 1.5f;

                Vector2 delta = new Vector2(dx, dy) * sampleOffset;

                gaussianBlurShader.Samples[i * 2 + 1].Offset = delta;
                gaussianBlurShader.Samples[i * 2 + 2].Offset = -delta;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                gaussianBlurShader.Samples[i].Weight /= totalWeights;
            }
        }

        float ComputeGaussian(float n)
        {
            float theta = Settings.BlurAmount;

            // メモ
            // ガウス関数になっていない気がするのだが。

            return (float) ((1.0 / Math.Sqrt(2 * Math.PI * theta)) * Math.Exp(-(n * n) / (2 * theta * theta)));
        }
    }
}
