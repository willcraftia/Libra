#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;

#endregion

namespace Samples.BloomPostprocess
{
    public sealed class BloomComponent : DrawableGameComponent
    {
        SpriteBatch spriteBatch;

        PixelShader bloomExtractPixelShader;

        PixelShader bloomCombinePixelShader;

        PixelShader gaussianBlurPixelShader;

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

            bloomExtractPixelShader = Device.CreatePixelShader();
            bloomExtractPixelShader.Initialize(compiler.CompilePixelShader("BloomExtract.fx"));

            bloomCombinePixelShader = Device.CreatePixelShader();
            bloomCombinePixelShader.Initialize(compiler.CompilePixelShader("BloomCombine.fx"));

            gaussianBlurPixelShader = Device.CreatePixelShader();
            gaussianBlurPixelShader.Initialize(compiler.CompilePixelShader("GaussianBlur.fx"));

            //PresentationParameters pp = GraphicsDevice.PresentationParameters;

            //int width = pp.BackBufferWidth;
            //int height = pp.BackBufferHeight;

            //SurfaceFormat format = pp.BackBufferFormat;

            //sceneRenderTarget = Device.CreateRenderTarget();
            //sceneRenderTarget.Width = width;
            //sceneRenderTarget.Height = height;

            //sceneRenderTarget = new RenderTarget2D(GraphicsDevice, width, height, false,
            //                                       format, pp.DepthStencilFormat, pp.MultiSampleCount,
            //                                       RenderTargetUsage.DiscardContents);

            //width /= 2;
            //height /= 2;

            //renderTarget1 = new RenderTarget2D(GraphicsDevice, width, height, false, format, DepthFormat.None);
            //renderTarget2 = new RenderTarget2D(GraphicsDevice, width, height, false, format, DepthFormat.None);
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

            //bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);

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

        //void DrawFullscreenQuad(Texture2D texture, RenderTargetView renderTarget, Effect effect, IntermediateBuffer currentBuffer)
        //{
        //    GraphicsDevice.SetRenderTarget(renderTarget);

        //    DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect, currentBuffer);
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
