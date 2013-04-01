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

        #region BloomExtractShader

        sealed class BloomExtractShader
        {
            public BloomExtractConstants Constants;

            ConstantBuffer constantBuffer;

            PixelShader pixelShader;

            public BloomExtractShader(IDevice device, byte[] bytecode)
            {
                pixelShader = device.CreatePixelShader();
                pixelShader.Initialize(bytecode);

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Initialize<BloomExtractConstants>();
            }

            public void ApplyConstants(DeviceContext context)
            {
                constantBuffer.SetData(context, Constants);
                context.PixelShaderConstantBuffers[0] = constantBuffer;
            }

            public void ApplyShader(DeviceContext context)
            {
                context.PixelShader = pixelShader;
            }
        }

        #endregion

        SpriteBatch spriteBatch;

        BloomExtractShader bloomExtractShader;

        PixelShader bloomCombinePixelShader;

        PixelShader gaussianBlurPixelShader;

        ConstantBuffer bloomExtractConstantBuffer;

        RenderTarget sceneRenderTarget;

        RenderTargetView sceneRenderTargetView;

        RenderTarget renderTarget1;

        RenderTargetView renderTarget1View;
        
        RenderTarget renderTarget2;

        RenderTargetView renderTarget2View;

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

            bloomCombinePixelShader = Device.CreatePixelShader();
            bloomCombinePixelShader.Initialize(compiler.CompilePixelShader("BloomCombine.fx"));

            gaussianBlurPixelShader = Device.CreatePixelShader();
            gaussianBlurPixelShader.Initialize(compiler.CompilePixelShader("GaussianBlur.fx"));

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
                Device.ImmediateContext.SetRenderTarget(sceneRenderTargetView);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;

            bloomExtractShader.Constants.BloomThreshold = Settings.BloomThreshold;
            bloomExtractShader.ApplyConstants(context);

            //DrawFullscreenQuad(sceneRenderTarget, renderTarget1, bloomExtractEffect, IntermediateBuffer.PreBloom);

            //SetBlurEffectParameters(1.0f / (float) renderTarget1.Width, 0);

            //DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurEffect, IntermediateBuffer.BlurredHorizontally);

            //SetBlurEffectParameters(0, 1.0f / (float) renderTarget1.Height);

            //DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurEffect, IntermediateBuffer.BlurredBothWays);

            //GraphicsDevice.SetRenderTarget(null);

            //EffectParameterCollection parameters = bloomCombineEffect.Parameters;

            //parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
            //parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
            //parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
            //parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);

            //GraphicsDevice.Textures[1] = sceneRenderTarget;

            //Viewport viewport = GraphicsDevice.Viewport;

            //DrawFullscreenQuad(renderTarget1, viewport.Width, viewport.Height, bloomCombineEffect, IntermediateBuffer.FinalResult);
        }

        //void DrawFullscreenQuad(Texture2D texture, RenderTargetView renderTargetView, Effect effect, IntermediateBuffer currentBuffer)
        //{
        //    Device.ImmediateContext.SetRenderTarget(renderTargetView);

        //    DrawFullscreenQuad(texture, renderTargetView.Width, renderTargetView.Height, effect, currentBuffer);
        //}

        //void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect, IntermediateBuffer currentBuffer)
        //{
        //    if (showBuffer < currentBuffer)
        //    {
        //        effect = null;
        //    }

        //    spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
        //    spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
        //    spriteBatch.End();
        //}

        //void SetBlurEffectParameters(float dx, float dy)
        //{
        //    EffectParameter weightsParameter, offsetsParameter;

        //    weightsParameter = gaussianBlurEffect.Parameters["SampleWeights"];
        //    offsetsParameter = gaussianBlurEffect.Parameters["SampleOffsets"];

        //    int sampleCount = weightsParameter.Elements.Count;

        //    float[] sampleWeights = new float[sampleCount];
        //    Vector2[] sampleOffsets = new Vector2[sampleCount];

        //    sampleWeights[0] = ComputeGaussian(0);
        //    sampleOffsets[0] = new Vector2(0);

        //    float totalWeights = sampleWeights[0];

        //    for (int i = 0; i < sampleCount / 2; i++)
        //    {
        //        float weight = ComputeGaussian(i + 1);

        //        sampleWeights[i * 2 + 1] = weight;
        //        sampleWeights[i * 2 + 2] = weight;

        //        totalWeights += weight * 2;

        //        float sampleOffset = i * 2 + 1.5f;

        //        Vector2 delta = new Vector2(dx, dy) * sampleOffset;

        //        sampleOffsets[i * 2 + 1] = delta;
        //        sampleOffsets[i * 2 + 2] = -delta;
        //    }

        //    for (int i = 0; i < sampleWeights.Length; i++)
        //    {
        //        sampleWeights[i] /= totalWeights;
        //    }

        //    weightsParameter.SetValue(sampleWeights);
        //    offsetsParameter.SetValue(sampleOffsets);
        //}

        float ComputeGaussian(float n)
        {
            float theta = Settings.BlurAmount;

            // メモ
            // ガウス関数になっていない気がするのだが。

            return (float) ((1.0 / Math.Sqrt(2 * Math.PI * theta)) * Math.Exp(-(n * n) / (2 * theta * theta)));
        }
    }
}
