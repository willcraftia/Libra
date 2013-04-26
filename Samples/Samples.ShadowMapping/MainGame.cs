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
        #region DrawModelEffect

        sealed class DrawModelEffect
        {
            struct Constants
            {
                public Matrix World;

                public Matrix View;

                public Matrix Projection;

                public Matrix LightViewProjection;

                public Vector3 LightDirection;

                public float DepthBias;

                public Vector4 AmbientColor;
            }

            public Matrix World;

            public Matrix View;

            public Matrix Projection;

            public Matrix LightViewProjection;

            public Vector3 LightDirection;

            public float DepthBias;

            public Vector4 AmbientColor;

            Constants constants;

            VertexShader vertexShader;

            PixelShader basicPixelShader;

            PixelShader variancePixelShader;

            ConstantBuffer constantBuffer;

            public ShadowMapEffectForm ShadowMapEffectForm { get; set; }

            public DrawModelEffect(Device device)
            {
                var compiler = ShaderCompiler.CreateShaderCompiler();
                compiler.RootPath = "../../Shaders";
                compiler.OptimizationLevel = OptimizationLevels.Level3;
                compiler.EnableStrictness = true;
                compiler.WarningsAreErrors = true;

                vertexShader = device.CreateVertexShader();
                vertexShader.Initialize(compiler.CompileVertexShader("DrawModel.fx"));

                basicPixelShader = device.CreatePixelShader();
                basicPixelShader.Initialize(compiler.CompilePixelShader("DrawModel.fx", "BasicPS"));

                variancePixelShader = device.CreatePixelShader();
                variancePixelShader.Initialize(compiler.CompilePixelShader("DrawModel.fx", "VariancePS"));

                constantBuffer = device.CreateConstantBuffer();
                constantBuffer.Usage = ResourceUsage.Dynamic;
                constantBuffer.Initialize<Constants>();
            }

            public void Apply(DeviceContext context)
            {
                Matrix.Transpose(ref World, out constants.World);
                Matrix.Transpose(ref View, out constants.View);
                Matrix.Transpose(ref Projection, out constants.Projection);
                Matrix.Transpose(ref LightViewProjection, out constants.LightViewProjection);
                constants.LightDirection = LightDirection;
                constants.DepthBias = DepthBias;
                constants.AmbientColor = AmbientColor;

                constantBuffer.SetData(context, constants);

                context.VertexShaderConstantBuffers[0] = constantBuffer;
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.VertexShader = vertexShader;
                if (ShadowMapEffectForm == ShadowMapEffectForm.Variance)
                {
                    context.PixelShader = variancePixelShader;
                }
                else
                {
                    context.PixelShader = basicPixelShader;
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

        DrawModelEffect drawModelShader;

        Model gridModel;

        Model dudeModel;

        ConvexBody bodyB;

        BoundingBox sceneBox;

        Vector3[] corners;

        float rotateDude = 0.0f;

        RenderTarget bsmRenderTarget;

        RenderTarget vsmRenderTarget;

        RenderTarget currentShadowRenderTarget;

        Matrix world;
        
        Matrix view;
        
        Matrix projection;

        BasicLightCamera basicLightCamera;

        FocusedLightCamera focusedLightCamera;

        LiSPSMCamera lispsmCamera;

        OldLiSPSMCamera oldLispsmCamera;

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

            basicLightCamera = new BasicLightCamera();
            focusedLightCamera = new FocusedLightCamera();
            lispsmCamera = new LiSPSMCamera();
            lispsmCamera.EyeNearPlaneDistance = 1.0f;
            oldLispsmCamera = new OldLiSPSMCamera();
            oldLispsmCamera.EyeNearPlaneDistance = 1.0f;

            shadowMapEffectForm = ShadowMapEffectForm.Variance;
        }

        protected override void LoadContent()
        {
            shadowMapEffect = new ShadowMapEffect(Device);
            drawModelShader = new DrawModelEffect(Device);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");

            corners = new Vector3[8];
            bodyB = new ConvexBody();

            // gridModel が半径約 183 であるため、
            // これを含むように簡易シーン AABB を決定。
            sceneBox = new BoundingBox(new Vector3(-200), new Vector3(200));

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

            gaussianBlur = new GaussianBlur(Device, vsmRenderTarget.Width, vsmRenderTarget.Height, SurfaceFormat.Vector2);
            gaussianBlur.Radius = 2;
            gaussianBlur.Amount = 8;
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
            basicLightCamera.LightDirection = -lightDir;
            focusedLightCamera.LightDirection = -lightDir;
            lispsmCamera.LightDirection = -lightDir;
            oldLispsmCamera.LightDirection = -lightDir;

            basicLightCamera.Update(view, projection, sceneBox);
            focusedLightCamera.Update(view, projection, sceneBox);
            lispsmCamera.Update(view, projection, sceneBox);
            oldLispsmCamera.Update(view, projection, sceneBox);

            Matrix lightViewProjection;
            //Matrix.Multiply(ref basicLightCamera.LightView, ref basicLightCamera.LightProjection, out lightViewProjection);
            //Matrix.Multiply(ref focusedLightCamera.LightView, ref focusedLightCamera.LightProjection, out lightViewProjection);
            Matrix.Multiply(ref lispsmCamera.LightView, ref lispsmCamera.LightProjection, out lightViewProjection);
            //Matrix.Multiply(ref oldLispsmCamera.LightView, ref oldLispsmCamera.LightProjection, out lightViewProjection);

            return lightViewProjection;
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
                gaussianBlur.Filter(
                    context,
                    currentShadowRenderTarget.GetShaderResourceView(),
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
                drawModelShader.World = world;
                drawModelShader.View = view;
                drawModelShader.Projection = projection;
                drawModelShader.LightViewProjection = lightViewProjection;
                drawModelShader.LightDirection = lightDir;
                drawModelShader.DepthBias = 0.001f;
                drawModelShader.AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
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

            text += "\r\nCamera position: " + cameraPosition;

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
