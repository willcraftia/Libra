#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.ShadowMapping
{
    public sealed class MainGame : Game
    {
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

            public PixelShader StandardPixelShader { get; private set; }

            public PixelShader VariancePixelShader { get; private set; }

            public ConstantBuffer ConstantBuffer { get; private set; }

            public ShadowMapEffectForm ShadowMapEffectForm { get; set; }

            public DrawModelShader(Device device, ShaderCompiler compiler)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(compiler.CompileVertexShader("DrawModel.fx"));

                StandardPixelShader = device.CreatePixelShader();
                StandardPixelShader.Initialize(compiler.CompilePixelShader("DrawModel.fx", "BasicPS"));

                VariancePixelShader = device.CreatePixelShader();
                VariancePixelShader.Initialize(compiler.CompilePixelShader("DrawModel.fx", "VariancePS"));

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
                if (ShadowMapEffectForm == ShadowMapEffectForm.Variance)
                {
                    context.PixelShader = VariancePixelShader;
                }
                else
                {
                    context.PixelShader = StandardPixelShader;
                }
            }
        }

        #endregion

        const int shadowMapWidthHeight = 2048;

        const int windowWidth = 800;

        const int windowHeight = 480;

        GraphicsManager graphicsManager;

        XnbManager content;

        SpriteBatch spriteBatch;

        SpriteFont spriteFont;

        Vector3 cameraPosition = new Vector3(0, 70, 100);
        
        Vector3 cameraForward = new Vector3(0, -0.4472136f, -0.8944272f);
        
        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);
 
        Vector3 lightDir = new Vector3(-0.3333333f, 0.6666667f, 0.6666667f);

        KeyboardState lastKeyboardState = new KeyboardState();

        JoystickState lastJoystickState = new JoystickState();

        KeyboardState currentKeyboardState;
        
        JoystickState currentJoystickState;

        ShadowMapEffect shadowMapEffect;

        DrawModelShader drawModelShader;

        Model gridModel;

        Model dudeModel;

        float rotateDude = 0.0f;

        RenderTarget bsmRenderTarget;

        RenderTarget vsmRenderTarget;

        RenderTarget currentShadowRenderTarget;

        Matrix world;
        
        Matrix view;
        
        Matrix projection;

        Matrix lightViewProjection;

        GaussianBlur gaussianBlur;

        ShadowMapEffectForm shadowMapEffectForm;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = windowWidth;
            graphicsManager.PreferredBackBufferHeight = windowHeight;

            var aspectRatio = (float) windowWidth / (float) windowHeight;
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio,  1.0f, 1000.0f);

            shadowMapEffectForm = ShadowMapEffectForm.Variance;
        }

        protected override void LoadContent()
        {
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "../../Shaders/";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            shadowMapEffect = new ShadowMapEffect(Device);
            drawModelShader = new DrawModelShader(Device, compiler);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");

            bsmRenderTarget = Device.CreateRenderTarget();
            bsmRenderTarget.Width = shadowMapWidthHeight;
            bsmRenderTarget.Height = shadowMapWidthHeight;
            bsmRenderTarget.Format = SurfaceFormat.Single;
            bsmRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            bsmRenderTarget.Initialize();

            vsmRenderTarget = Device.CreateRenderTarget();
            vsmRenderTarget.Width = shadowMapWidthHeight;
            vsmRenderTarget.Height = shadowMapWidthHeight;
            vsmRenderTarget.Format = SurfaceFormat.Vector2;
            vsmRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            vsmRenderTarget.Initialize();

            gaussianBlur = new GaussianBlur(Device.ImmediateContext, vsmRenderTarget.Width, vsmRenderTarget.Height, SurfaceFormat.Vector2);
            gaussianBlur.Radius = 2;
            gaussianBlur.Amount = 8;
            gaussianBlur.Enabled = true;
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

            switch (shadowMapEffectForm)
            {
                case ShadowMapEffectForm.Basic:
                    currentShadowRenderTarget = bsmRenderTarget;
                    break;
                case ShadowMapEffectForm.Variance:
                    currentShadowRenderTarget = vsmRenderTarget;
                    break;
            }

            CreateShadowMap();

            DrawWithShadowMap();

            DrawShadowMapToScreen();

            DrawOverlayText();

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

            context.SetRenderTarget(currentShadowRenderTarget.GetRenderTargetView());

            context.Clear(Color.White);

            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModel(dudeModel, true);

            context.SetRenderTarget(null);

            if (shadowMapEffectForm == ShadowMapEffectForm.Variance)
            {
                gaussianBlur.Filter(currentShadowRenderTarget.GetShaderResourceView(),
                                    currentShadowRenderTarget.GetRenderTargetView());
            }
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
                shadowMapEffect.World = world;
                shadowMapEffect.LightViewProjection = lightViewProjection;
                shadowMapEffect.Form = shadowMapEffectForm;
                shadowMapEffect.Apply(context);
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
                drawModelShader.ShadowMapEffectForm = shadowMapEffectForm;
                drawModelShader.Apply(context);

                context.PixelShaderResources[1] = currentShadowRenderTarget.GetShaderResourceView();
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
        }

        void DrawShadowMapToScreen()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(currentShadowRenderTarget.GetShaderResourceView(), new Rectangle(0, 0, 128, 128), Color.White);
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            var text = "X = Shadow map form (" + shadowMapEffectForm + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 300), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 299), Color.White);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            lastJoystickState = currentJoystickState;

            currentKeyboardState = Keyboard.GetState();
            currentJoystickState = Joystick.GetState();

            rotateDude += currentJoystickState.Triggers.Right * time * 0.2f;
            rotateDude -= currentJoystickState.Triggers.Left * time * 0.2f;
            
            if (currentKeyboardState.IsKeyDown(Keys.Q))
                rotateDude -= time * 0.2f;
            if (currentKeyboardState.IsKeyDown(Keys.E))
                rotateDude += time * 0.2f;

            if (currentKeyboardState.IsKeyUp(Keys.X) && lastKeyboardState.IsKeyDown(Keys.X) ||
                currentJoystickState.IsButtonUp(Buttons.X) && lastJoystickState.IsButtonDown(Buttons.X))
            {
                if (shadowMapEffectForm == ShadowMapEffectForm.Basic)
                {
                    shadowMapEffectForm = ShadowMapEffectForm.Variance;
                }
                else
                {
                    shadowMapEffectForm = ShadowMapEffectForm.Basic;
                }
            }

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
