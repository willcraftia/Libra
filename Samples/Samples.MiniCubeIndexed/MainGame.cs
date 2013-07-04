#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Input;

#endregion

namespace Samples.MiniCubeIndexed
{
    public sealed class MainGame : Game
    {
        #region Vertices

        static readonly VertexPositionColor[] Vertices =
        {
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(255, 0, 0, 255)),

            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(0, 255, 0, 255)),

            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 0, 255, 255)),

            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 255, 0, 255)),

            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 0, 255, 255)),

            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(0, 255, 255, 255)),
        };

        #endregion

        #region Indices

        static readonly ushort[] Indices =
        {
            0,  1,  2,  0,  2,  3,
            4,  5,  6,  4,  6,  7,
            8,  9, 10,  8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23,
        };

        #endregion

        GraphicsManager graphicsManager;

        VertexShader vertexShader;

        PixelShader pixelShader;

        InputLayout inputLayout;

        VertexBuffer vertexBuffer;

        IndexBuffer indexBuffer;

        ConstantBuffer constantBuffer;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);
        }

        protected override void LoadContent()
        {
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "Shaders";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var vsBytecode = compiler.CompileVertexShader("MiniCube.fx");
            var psBytecode = compiler.CompilePixelShader("MiniCube.fx");

            vertexShader = Device.CreateVertexShader();
            vertexShader.Initialize(vsBytecode);

            pixelShader = Device.CreatePixelShader();
            pixelShader.Initialize(psBytecode);
            
            inputLayout = Device.CreateInputLayout();
            inputLayout.Initialize<VertexPositionColor>(vertexShader);

            vertexBuffer = Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Immutable;
            vertexBuffer.Initialize(Vertices);

            indexBuffer = Device.CreateIndexBuffer();
            indexBuffer.Usage = ResourceUsage.Immutable;
            indexBuffer.Initialize(Indices);

            constantBuffer = Device.CreateConstantBuffer();
            constantBuffer.Initialize<Matrix>();

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
            DeviceContext.IndexBuffer = indexBuffer;

            DeviceContext.VertexShader = vertexShader;
            DeviceContext.PixelShader = pixelShader;
            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;

            float aspect = DeviceContext.Viewport.AspectRatio;
            float time = (float) gameTime.TotalGameTime.TotalSeconds;

            var world = Matrix.CreateRotationX(time) * Matrix.CreateRotationY(time * 2) * Matrix.CreateRotationZ(time * .7f);
            var view = Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100.0f);

            var worldViewProjection = world * view * projection;
            // 列優先としているので転置してから設定。
            DeviceContext.SetData(constantBuffer, Matrix.Transpose(worldViewProjection));

            DeviceContext.DrawIndexed(indexBuffer.IndexCount);

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
