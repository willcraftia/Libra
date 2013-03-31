#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Games.SharpDX;
using Libra.Graphics;
using Libra.Graphics.Compiler;

#endregion

namespace Samples.MiniCube
{
    public sealed class MainGame : Game
    {
        #region Vertices

        static readonly VertexPositionColor[] Vertices =
        {
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(255, 0, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(255, 0, 0, 255)),

            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(0, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 255, 0, 255)),

            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(0, 0, 255, 255)),

            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(255, 255, 0, 255)),
            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(255, 255, 0, 255)),

            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1, -1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1, -1, -1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1,  1,  1), new Color(255, 0, 255, 255)),
            new VertexPositionColor(new Vector3(-1, -1,  1), new Color(255, 0, 255, 255)),

            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1, -1,  1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1, -1, -1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1,  1), new Color(0, 255, 255, 255)),
            new VertexPositionColor(new Vector3( 1,  1, -1), new Color(0, 255, 255, 255)),
        };

        #endregion

        IGamePlatform platform;

        GraphicsManager graphicsManager;

        VertexShader vertexShader;

        PixelShader pixelShader;

        InputLayout inputLayout;

        VertexBuffer vertexBuffer;

        ConstantBuffer constantBuffer;

        public MainGame()
        {
            platform = new SdxFormGamePlatform(this);
            graphicsManager = new GraphicsManager(this);
        }

        protected override void LoadContent()
        {
            // ここではテストのために行優先でコンパイル。
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "Shaders";
            compiler.PackMatrixRowMajor = true;
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var vsBytecode = compiler.CompileVertexShader("MiniCube.fx", "VS");
            var psBytecode = compiler.CompilePixelShader("MiniCube.fx", "PS");

            vertexShader = Device.CreateVertexShader();
            vertexShader.Initialize(vsBytecode);

            pixelShader = Device.CreatePixelShader();
            pixelShader.Initialize(psBytecode);

            inputLayout = Device.CreateInputLayout();
            inputLayout.Initialize<VertexPositionColor>(vertexShader);

            vertexBuffer = Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Immutable;
            vertexBuffer.Initialize(Vertices);

            // 以下は頂点構造体を用いず、バイト配列を直接設定する場合のテスト。
            //var stream = new System.IO.MemoryStream();
            //var writer = new System.IO.BinaryWriter(stream);
            //var reader = new System.IO.BinaryReader(stream);
            //foreach (var vertex in Vertices)
            //{
            //    writer.Write(vertex.Position.X);
            //    writer.Write(vertex.Position.Y);
            //    writer.Write(vertex.Position.Z);
            //    writer.Write(vertex.Color.R);
            //    writer.Write(vertex.Color.G);
            //    writer.Write(vertex.Color.B);
            //    writer.Write(vertex.Color.A);
            //}
            //stream.Flush();
            //stream.Position = 0;
            //var byteData = reader.ReadBytes(VertexPositionColor.VertexDeclaration.Stride * Vertices.Length);
            //writer.Close();
            //reader.Close();
            //stream.Close();
            //vertexBuffer.Initialize(VertexPositionColor.VertexDeclaration, byteData);

            constantBuffer = Device.CreateConstantBuffer();
            constantBuffer.Initialize<Matrix>();

            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            // 入力レイアウト自動解決 OFF。
            context.AutoResolveInputLayout = false;
            context.InputLayout = inputLayout;
            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.SetVertexBuffer(vertexBuffer);

            context.VertexShader = vertexShader;
            context.PixelShader = pixelShader;
            context.VertexShaderConstantBuffers[0] = constantBuffer;

            float aspect = context.Viewport.AspectRatio;
            float time = (float) gameTime.TotalGameTime.TotalSeconds;

            var world = Matrix.CreateRotationX(time) * Matrix.CreateRotationY(time * 2) * Matrix.CreateRotationZ(time * .7f);
            var view = Matrix.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect, 0.1f, 100.0f);

            // メモ
            //
            // HLSL デフォルトは列優先 (column_major)。
            // http://msdn.microsoft.com/en-us/library/bb509706(v=vs.85).aspx
            //
            // Matrix 構造体は、SharpDX も XNA も自作 Matrix も、
            // フィールドの並びが行優先 (HLSL では row_major)。
            //
            // このため、HLSL デフォルトでは列優先に対応させる必要があり、
            // このためには行列を転置させてから設定すれば良い。
            //
            // あるいは、シェーダをコンパイルする際に、オプション指定により
            // 行優先と列優先を設定できる。
            //
            // なお、列優先の方が効率的であるらしい。
            // http://maverickproj.web.fc2.com/d3d11_02.html
            //
            // 基本的には、デフォルトに従う事を重視し、
            // 転置してから設定という手順を踏むが、
            // ここではテストのためにコンパイル時に行優先としている。

            var worldViewProjection = world * view * projection;
            //constantBuffer.SetData(context, Matrix.Transpose(worldViewProjection));
            constantBuffer.SetData(context, worldViewProjection);

            context.Draw(36);

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
