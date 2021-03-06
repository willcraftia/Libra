﻿#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Input;

#endregion

namespace Samples.MiniCubeTexture
{
    public sealed class MainGame : Game
    {
        #region Vertices

        static readonly VertexPositionTexture[] Vertices =
        {
            new VertexPositionTexture(new Vector3(-1, -1,  1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-1,  1,  1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1,  1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3( 1, -1,  1), new Vector2(1, 1)),

            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3( 1,  1, -1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(-1,  1, -1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3( 1, -1, -1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3( 1,  1, -1), new Vector2(0, 0)),

            new VertexPositionTexture(new Vector3(-1,  1, -1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1,  1, -1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1,  1, -1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-1,  1,  1), new Vector2(0, 1)),

            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-1, -1,  1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1, -1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3( 1, -1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3( 1, -1, -1), new Vector2(1, 1)),

            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-1,  1, -1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(-1,  1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1, -1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-1,  1,  1), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1,  1), new Vector2(1, 1)),

            new VertexPositionTexture(new Vector3( 1, -1, -1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3( 1, -1,  1), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1, -1, -1), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3( 1,  1,  1), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 1,  1, -1), new Vector2(1, 0)),
        };

        #endregion

        GraphicsManager graphicsManager;

        VertexShader vertexShader;

        PixelShader pixelShader;

        InputLayout inputLayout;

        VertexBuffer vertexBuffer;

        ConstantBuffer constantBuffer;

        Texture2D texture;

        ShaderResourceView textureView;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);
        }

        protected override void LoadContent()
        {
            // 行優先でコンパイル。
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "Shaders";
            compiler.PackMatrixRowMajor = true;
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var vsBytecode = compiler.CompileVertexShader("MiniCubeTexture.fx");
            var psBytecode = compiler.CompilePixelShader("MiniCubeTexture.fx");

            vertexShader = Device.CreateVertexShader();
            vertexShader.Initialize(vsBytecode);

            pixelShader = Device.CreatePixelShader();
            pixelShader.Initialize(psBytecode);

            inputLayout = Device.CreateInputLayout();
            inputLayout.Initialize<VertexPositionTexture>(vertexShader);

            vertexBuffer = Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Immutable;
            vertexBuffer.Initialize(Vertices);

            constantBuffer = Device.CreateConstantBuffer();
            constantBuffer.Initialize<Matrix>();

            texture = Device.CreateTexture2D();
            texture.Initialize("Textures/GeneticaMortarlessBlocks.jpg");

            textureView = Device.CreateShaderResourceView();
            textureView.Initialize(texture);

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DeviceContext.Clear(Color.CornflowerBlue);

            // 入力レイアウト自動解決 OFF。
            DeviceContext.AutoResolveInputLayout = false;
            DeviceContext.InputLayout = inputLayout;
            DeviceContext.PrimitiveTopology = PrimitiveTopology.TriangleList;
            DeviceContext.SetVertexBuffer(vertexBuffer);

            DeviceContext.VertexShader = vertexShader;
            DeviceContext.PixelShader = pixelShader;
            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;
            DeviceContext.PixelShaderResources[0] = textureView;

            float aspect = DeviceContext.Viewport.AspectRatio;
            float time = (float) gameTime.TotalGameTime.TotalSeconds;

            var world = Matrix.CreateRotationX(time) * Matrix.CreateRotationY(time * 2) * Matrix.CreateRotationZ(time * .7f);
            var view = Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100.0f);

            var worldViewProjection = world * view * projection;
            DeviceContext.SetData(constantBuffer, worldViewProjection);

            DeviceContext.Draw(36);

            base.Draw(gameTime);
        }
    }

    #region Program

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new MainGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
