#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.ShadowMapping
{
    public sealed class MainGame : Game
    {
        #region CreateShadowMapShader

        sealed class CreateShadowMapShader
        {
            public struct CreateShadowMapShaderConstants
            {
                public Matrix World;

                public Matrix LightViewProjection;
            }

            public CreateShadowMapShaderConstants Constants;

            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public ConstantBuffer ConstantBuffer { get; private set; }

            public CreateShadowMapShader(Device device, ShaderCompiler compiler)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(compiler.CompileVertexShader("CreateShadowMap.fx"));

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(compiler.CompilePixelShader("CreateShadowMap.fx"));

                ConstantBuffer = device.CreateConstantBuffer();
                ConstantBuffer.Usage = ResourceUsage.Dynamic;
                ConstantBuffer.Initialize<CreateShadowMapShaderConstants>();
            }

            public void Apply(DeviceContext context)
            {
                ConstantBuffer.SetData(context, Constants);

                context.VertexShaderConstantBuffers[0] = ConstantBuffer;
                context.VertexShader = VertexShader;
                context.PixelShader = PixelShader;
            }
        }

        #endregion

        #region DrawModelShader

        sealed class DrawModelShader
        {
            public struct DrawModelShaderConstants
            {
                public Matrix World;

                public Matrix View;

                public Matrix Projection;

                public Matrix LightViewProjection;

                public Vector3 LightDirection;

                public float DepthBias;

                public Vector4 AmbientColor;
            }

            public DrawModelShaderConstants Constants;
            
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public ConstantBuffer ConstantBuffer { get; private set; }

            public DrawModelShader(Device device, ShaderCompiler compiler)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(compiler.CompileVertexShader("DrawModel.fx"));

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(compiler.CompilePixelShader("DrawModel.fx"));

                ConstantBuffer = device.CreateConstantBuffer();
                ConstantBuffer.Usage = ResourceUsage.Dynamic;
                ConstantBuffer.Initialize<DrawModelShaderConstants>();
            }

            public void Apply(DeviceContext context)
            {
                ConstantBuffer.SetData(context, Constants);

                context.VertexShaderConstantBuffers[0] = ConstantBuffer;
                context.PixelShaderConstantBuffers[0] = ConstantBuffer;
                context.VertexShader = VertexShader;
                context.PixelShader = PixelShader;
            }
        }

        #endregion

        const int shadowMapWidthHeight = 2048;

        const int windowWidth = 800;

        const int windowHeight = 480;

        GraphicsManager graphicsManager;

        XnbManager content;

        SpriteBatch spriteBatch;

        Vector3 cameraPosition = new Vector3(0, 70, 100);
        
        Vector3 cameraForward = new Vector3(0, -0.4472136f, -0.8944272f);
        
        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);
 
        Vector3 lightDir = new Vector3(-0.3333333f, 0.6666667f, 0.6666667f);

        KeyboardState currentKeyboardState;
        
        JoystickState currentJoystickState;

        CreateShadowMapShader createShadowMapShader;

        DrawModelShader drawModelShader;

        Model gridModel;

        Model dudeModel;

        float rotateDude = 0.0f;

        RenderTarget shadowRenderTarget;

        Matrix world;
        
        Matrix view;
        
        Matrix projection;

        Matrix lightViewProjection;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = windowWidth;
            graphicsManager.PreferredBackBufferHeight = windowHeight;

            var aspectRatio = (float) windowWidth / (float) windowHeight;
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio,  1.0f, 1000.0f);
        }

        protected override void LoadContent()
        {
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "../../Shaders/";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            createShadowMapShader = new CreateShadowMapShader(Device, compiler);
            drawModelShader = new DrawModelShader(Device, compiler);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");

            shadowRenderTarget = Device.CreateRenderTarget();
            shadowRenderTarget.Width = shadowMapWidthHeight;
            shadowRenderTarget.Height = shadowMapWidthHeight;
            shadowRenderTarget.MipLevels = 1;
            shadowRenderTarget.Format = SurfaceFormat.Single;
            shadowRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            shadowRenderTarget.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput(gameTime);

            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            lightViewProjection = CreateLightViewProjectionMatrix();

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            CreateShadowMap();

            DrawWithShadowMap();

            DrawShadowMapToScreen();

            base.Draw(gameTime);
        }

        Matrix CreateLightViewProjectionMatrix()
        {
            var lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDir, Vector3.Up);

            var frustumCorners = cameraFrustum.GetCorners();

            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], lightRotation);
            }

            var lightBox = BoundingBox.CreateFromPoints(frustumCorners);

            var boxSize = lightBox.Max - lightBox.Min;
            var halfBoxSize = boxSize * 0.5f;

            var lightPosition = lightBox.Min + halfBoxSize;
            lightPosition.Z = lightBox.Min.Z;

            lightPosition = Vector3.Transform(lightPosition, Matrix.Invert(lightRotation));

            var lightView = Matrix.CreateLookAt(lightPosition, lightPosition - lightDir, Vector3.Up);
            
            var lightProjection = Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z);

            return lightView * lightProjection;
        }

        void CreateShadowMap()
        {
            var context = Device.ImmediateContext;

            context.SetRenderTarget(shadowRenderTarget.GetRenderTargetView());

            context.Clear(Color.White);

            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModel(dudeModel, true);

            context.SetRenderTarget(null);
        }

        void DrawWithShadowMap()
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            context.PixelShaderSamplers[1] = SamplerState.PointClamp;

            world = Matrix.Identity;
            DrawModel(gridModel, false);

            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModel(dudeModel, false);
        }

        void DrawModel(Model model, bool createShadowMap)
        {
            var context = Device.ImmediateContext;

            if (createShadowMap)
            {
                Matrix.Transpose(ref world, out createShadowMapShader.Constants.World);
                Matrix.Transpose(ref lightViewProjection, out createShadowMapShader.Constants.LightViewProjection);
                createShadowMapShader.Apply(context);
            }
            else
            {
                Matrix.Transpose(ref world, out drawModelShader.Constants.World);
                Matrix.Transpose(ref view, out drawModelShader.Constants.View);
                Matrix.Transpose(ref projection, out drawModelShader.Constants.Projection);
                Matrix.Transpose(ref lightViewProjection, out drawModelShader.Constants.LightViewProjection);
                drawModelShader.Constants.LightDirection = lightDir;
                drawModelShader.Constants.DepthBias = 0.001f;
                drawModelShader.Constants.AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                drawModelShader.Apply(context);

                context.PixelShaderResources[1] = shadowRenderTarget.GetShaderResourceView();
            }

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffer(0, meshPart.VertexBuffer);
                    context.IndexBuffer = meshPart.IndexBuffer;

                    if (!createShadowMap)
                    {
                        context.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;
                    }

                    context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }

            if (!createShadowMap)
            {
                // レンダ ターゲットの場合、明示的に参照を外す必要がある。
                // レンダ ターゲットの Texture2D を、RenderTargetView と ShaderResourceView の
                // 両方で同時に参照された状態となる時、
                // 入力側 (ShaderResourceView) には 0 で埋められたテクスチャが強制的に設定される。
                //
                // 恐らく、ここでレンダ ターゲットの ShaderResourceView を外さなかった場合、
                // 次の描画で深度マップを描画する際に RenderTargetView として設定するが、
                // シェーダ ステージには ShaderResourceView として残っているため、
                // そのタイミングで ShaderResourceView が強制リセットされるのではないか、と。
                context.PixelShaderResources[1] = null;
            }
        }

        void DrawShadowMapToScreen()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(shadowRenderTarget.GetShaderResourceView(), new Rectangle(0, 0, 128, 128), Color.White);
            spriteBatch.End();

            var context = Device.ImmediateContext;
            context.PixelShaderResources[0] = null;
            context.PixelShaderSamplers[0] = SamplerState.LinearWrap;
        }

        /// </summary>
        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            currentKeyboardState = Keyboard.GetState();
            currentJoystickState = Joystick.GetState();

            rotateDude += currentJoystickState.Triggers.Right * time * 0.2f;
            rotateDude -= currentJoystickState.Triggers.Left * time * 0.2f;
            
            if (currentKeyboardState.IsKeyDown(Keys.Q))
                rotateDude -= time * 0.2f;
            if (currentKeyboardState.IsKeyDown(Keys.E))
                rotateDude += time * 0.2f;

            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentJoystickState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
        }

        void UpdateCamera(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;
 
            float pitch = -currentJoystickState.ThumbSticks.Right.Y * time * 0.001f;
            float turn = -currentJoystickState.ThumbSticks.Right.X * time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
                pitch += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Down))
                pitch -= time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Left))
                turn += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
                turn -= time * 0.001f;

            var cameraRight = Vector3.Cross(Vector3.Up, cameraForward);
            var flatFront = Vector3.Cross(cameraRight, Vector3.Up);

            var pitchMatrix = Matrix.CreateFromAxisAngle(cameraRight, pitch);
            var turnMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, turn);

            var tiltedFront = Vector3.TransformNormal(cameraForward, pitchMatrix * turnMatrix);

            if (Vector3.Dot(tiltedFront, flatFront) > 0.001f)
            {
                cameraForward = Vector3.Normalize(tiltedFront);
            }

            if (currentKeyboardState.IsKeyDown(Keys.W))
                cameraPosition += cameraForward * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.S))
                cameraPosition -= cameraForward * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.A))
                cameraPosition += cameraRight * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.D))
                cameraPosition -= cameraRight * time * 0.1f;

            cameraPosition += cameraForward * currentJoystickState.ThumbSticks.Left.Y * time * 0.1f;
            cameraPosition -= cameraRight * currentJoystickState.ThumbSticks.Left.X * time * 0.1f;

            if (currentJoystickState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraPosition = new Vector3(0, 50, 50);
                cameraForward = new Vector3(0, 0, -1);
            }

            cameraForward.Normalize();

            view = Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraForward, Vector3.Up);

            cameraFrustum.Matrix = view * projection;
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
