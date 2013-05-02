﻿#region Using

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

        /// <summary>
        /// モデル描画シェーダの簡易管理クラス。
        /// </summary>
        sealed class DrawModelEffect
        {
            #region Constants

            /// <summary>
            /// 定数バッファの定義。
            /// </summary>
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

            #endregion

            public Matrix World;

            public Matrix View;

            public Matrix Projection;

            public Matrix LightView;

            public Matrix LightProjection;

            public Vector3 LightDirection;

            public float DepthBias;

            public Vector4 AmbientColor;

            Constants constants;

            VertexShader vertexShader;

            PixelShader basicPixelShader;

            PixelShader variancePixelShader;

            ConstantBuffer constantBuffer;

            public ShadowMapForm ShadowMapEffectForm { get; set; }

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
                Matrix lightViewProjection;
                Matrix.Multiply(ref LightView, ref LightProjection, out lightViewProjection);

                Matrix.Transpose(ref World, out constants.World);
                Matrix.Transpose(ref View, out constants.View);
                Matrix.Transpose(ref Projection, out constants.Projection);
                Matrix.Transpose(ref lightViewProjection, out constants.LightViewProjection);
                constants.LightDirection = LightDirection;
                constants.DepthBias = DepthBias;
                constants.AmbientColor = AmbientColor;

                constantBuffer.SetData(context, constants);

                context.VertexShaderConstantBuffers[0] = constantBuffer;
                context.PixelShaderConstantBuffers[0] = constantBuffer;
                context.VertexShader = vertexShader;
                if (ShadowMapEffectForm == ShadowMapForm.Variance)
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

        #region LightCameraType

        enum LightCameraType
        {
            /// <summary>
            /// LiSPSMLightCamera を用いる。
            /// </summary>
            LiSPSM  = 0,
            
            /// <summary>
            /// FocusedLightCamera を用いる。
            /// </summary>
            Focused = 1,

            /// <summary>
            /// BasicLightCamera を用いる。
            /// </summary>
            Basic   = 2,
        }

        #endregion

        /// <summary>
        /// シャドウ マップのサイズ (正方形)。
        /// </summary>
        const int shadowMapSize = 2048;

        /// <summary>
        /// ウィンドウの幅。
        /// </summary>
        const int windowWidth = 800;

        /// <summary>
        /// ウィンドウの高さ。
        /// </summary>
        const int windowHeight = 480;

        /// <summary>
        /// Libra のグラフィックス マネージャ。
        /// </summary>
        GraphicsManager graphicsManager;

        /// <summary>
        /// Libra の XNA コンテント マネージャ。
        /// </summary>
        XnbManager content;

        /// <summary>
        /// スプライト バッチ。
        /// </summary>
        SpriteBatch spriteBatch;

        /// <summary>
        /// フォント。
        /// </summary>
        SpriteFont spriteFont;

        /// <summary>
        /// 表示カメラ。
        /// </summary>
        BasicCamera camera;
        
        /// <summary>
        /// 表示カメラの視錐台。
        /// </summary>
        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);

        /// <summary>
        /// 前回の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState lastKeyboardState = new KeyboardState();

        /// <summary>
        /// 前回の更新処理におけるジョイスティック状態。
        /// </summary>
        JoystickState lastJoystickState = new JoystickState();

        /// <summary>
        /// 現在の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState currentKeyboardState;
        
        /// <summary>
        /// 現在の更新処理におけるジョイスティック状態。
        /// </summary>
        JoystickState currentJoystickState;

        /// <summary>
        /// シャドウ マップ エフェクト。
        /// </summary>
        ShadowMapEffect shadowMapEffect;

        /// <summary>
        /// モデル描画エフェクト。
        /// </summary>
        DrawModelEffect drawModelEffect;

        /// <summary>
        /// グリッド モデル (格子状の床)。
        /// </summary>
        Model gridModel;

        /// <summary>
        /// デュード モデル (人)。
        /// </summary>
        Model dudeModel;

        /// <summary>
        /// デュード モデルに適用するワールド行列。
        /// </summary>
        Matrix world;

        /// <summary>
        /// 明示するシーン領域。
        /// </summary>
        BoundingBox sceneBox;

        /// <summary>
        /// 表示カメラの視錐台をシーン領域として用いるか否かを示す値。
        /// true (表示カメラの視錐台をシーン領域として用いる場合)、
        /// false (sceneBox で明示した領域をシーン領域として用いる場合)。
        /// </summary>
        bool useCameraFrustumSceneBox;

        /// <summary>
        /// 視錐台や境界ボックスの頂点を得るための一時作業配列。
        /// </summary>
        Vector3[] corners;

        /// <summary>
        /// デュード モデルの回転量。
        /// </summary>
        float rotateDude = 0.0f;

        /// <summary>
        /// 基礎的なシャドウ マップのレンダ ターゲット。
        /// </summary>
        RenderTarget bsmRenderTarget;

        /// <summary>
        /// VSM のレンダ ターゲット。
        /// </summary>
        RenderTarget vsmRenderTarget;

        /// <summary>
        /// 現在選択されているシャドウ マップのレンダ ターゲット。
        /// </summary>
        RenderTarget currentShadowRenderTarget;

        /// <summary>
        /// 基礎的な簡易ライト カメラ。
        /// シーン領域へ焦点を合わせない。
        /// XNA Shadow Mapping のライト カメラ算出と同程度の品質。
        /// </summary>
        BasicLightCamera basicLightCamera;

        /// <summary>
        /// シーン領域へ焦点を合わせるライト カメラ。
        /// 焦点合わせにより、BasicLightCamera よりも高品質となる。
        /// </summary>
        FocusedLightCamera focusedLightCamera;

        /// <summary>
        /// LiSPSM ライト カメラ。
        /// 焦点合わせおよび LiSPSM による補正により、
        /// FocusedLightCamera よりも高品質となる。
        /// </summary>
        LiSPSMLightCamera lispsmLightCamera;

        /// <summary>
        /// 現在選択されているライト カメラの種類。
        /// </summary>
        LightCameraType currentLightCameraType;

        /// <summary>
        /// 現在選択されているライト カメラ。
        /// </summary>
        LightCamera currentLightCamera;

        /// <summary>
        /// VSM で用いるガウシアン ブラー。
        /// </summary>
        GaussianBlur gaussianBlur;

        /// <summary>
        /// 現在選択されているシャドウ マップの種類。
        /// </summary>
        ShadowMapForm shadowMapEffectForm;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = windowWidth;
            graphicsManager.PreferredBackBufferHeight = windowHeight;

            camera = new BasicCamera
            {
                Position = new Vector3(0, 70, 100),
                Direction = new Vector3(0, -0.4472136f, -0.8944272f),
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) windowWidth / (float) windowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };

            corners = new Vector3[8];

            // gridModel が半径約 183 であるため、
            // これを含むように簡易シーン AABB を決定。
            // なお、広大な世界を扱う場合には、表示カメラの視錐台に含まれるオブジェクト、
            // および、それらに投影しうるオブジェクトを動的に選択および決定し、
            // 適切な最小シーン領域を算出して利用する。
            sceneBox = new BoundingBox(new Vector3(-200), new Vector3(200));

            useCameraFrustumSceneBox = true;

            // ライト カメラの初期化。

            // ライトの進行方向 (XNA Shadow Mapping では原点から見たライトの方向)。
            // 単位ベクトル。
            var lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

            // ライトによる投影を処理する距離。
            float lightFar = 500.0f;

            basicLightCamera = new BasicLightCamera();
            basicLightCamera.LightDirection = lightDirection;

            focusedLightCamera = new FocusedLightCamera();
            focusedLightCamera.LightDirection = lightDirection;
            focusedLightCamera.LightFarClipDistance = lightFar;
            
            lispsmLightCamera = new LiSPSMLightCamera();
            lispsmLightCamera.LightDirection = lightDirection;
            lispsmLightCamera.LightFarClipDistance = lightFar;

            currentLightCameraType = LightCameraType.LiSPSM;

            shadowMapEffectForm = ShadowMapForm.Variance;
        }

        protected override void LoadContent()
        {
            shadowMapEffect = new ShadowMapEffect(Device);
            drawModelEffect = new DrawModelEffect(Device);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");

            // 基礎的なシャドウ マップは R 値のみを用いる。
            bsmRenderTarget = Device.CreateRenderTarget();
            bsmRenderTarget.Width = shadowMapSize;
            bsmRenderTarget.Height = shadowMapSize;
            bsmRenderTarget.Format = SurfaceFormat.Single;
            bsmRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            bsmRenderTarget.Initialize();

            // VSM は RG 値の二つを用いる。
            vsmRenderTarget = Device.CreateRenderTarget();
            vsmRenderTarget.Width = shadowMapSize;
            vsmRenderTarget.Height = shadowMapSize;
            vsmRenderTarget.Format = SurfaceFormat.Vector2;
            vsmRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            vsmRenderTarget.Initialize();

            // ガウシアン ブラーは VSM で用いるため、
            // 内部で使用するレンダ ターゲットは VSM に合わせる。
            gaussianBlur = new GaussianBlur(Device, vsmRenderTarget.Width, vsmRenderTarget.Height, vsmRenderTarget.Format);
            gaussianBlur.Radius = 4;
            gaussianBlur.Amount = 16;
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            // ライト カメラの更新。
            UpdateLightCamera();

            // 念のため状態を初期状態へ。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            // 選択されているシャドウ マップの種類に応じてレンダ ターゲットを切り替え。
            switch (shadowMapEffectForm)
            {
                case ShadowMapForm.Basic:
                    currentShadowRenderTarget = bsmRenderTarget;
                    break;
                case ShadowMapForm.Variance:
                    currentShadowRenderTarget = vsmRenderTarget;
                    break;
            }

            // シャドウ マップの描画。
            CreateShadowMap();

            // シャドウ マップを用いたシーンの描画。
            DrawWithShadowMap();

            // シャドウ マップを画面左上に表示。
            DrawShadowMapToScreen();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void UpdateLightCamera()
        {
            // ライト カメラへ指定するシーン領域。
            BoundingBox actualSceneBox;

            if (useCameraFrustumSceneBox)
            {
                // 視錐台全体とする場合。
                cameraFrustum.GetCorners(corners);
                actualSceneBox = BoundingBox.CreateFromPoints(corners);

            }
            else
            {
                // 明示する場合。
                actualSceneBox = sceneBox;
                actualSceneBox.Merge(camera.Position);
            }

            // 利用するライト カメラの選択。
            switch (currentLightCameraType)
            {
                case LightCameraType.LiSPSM:
                    currentLightCamera = lispsmLightCamera;
                    break;
                case LightCameraType.Focused:
                    currentLightCamera = focusedLightCamera;
                    break;
                default:
                    currentLightCamera = basicLightCamera;
                    break;
            }

            // カメラの行列を更新。
            currentLightCamera.Update(camera.View, camera.Projection, actualSceneBox);
        }

        void CreateShadowMap()
        {
            var context = Device.ImmediateContext;

            // 選択中のシャドウ マップ レンダ ターゲットを設定。
            context.SetRenderTarget(currentShadowRenderTarget.GetRenderTargetView());

            // レンダ ターゲットの R あるいは RG を 1 で埋める (1 は最遠の深度)。
            // 同時に、深度ステンシルの深度も 1 へ。
            context.Clear(Color.White);

            // デュード モデルのワールド行列。
            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            
            // 投影オブジェクトとしてデュード モデルを描画。
            // グリッド モデルは非投影オブジェクト。
            DrawModel(dudeModel, true);

            // レンダ ターゲットをデフォルトへ戻す。
            context.SetRenderTarget(null);

            // VSM を選択している場合はシャドウ マップへブラーを適用。
            if (shadowMapEffectForm == ShadowMapForm.Variance)
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

            // シャドウ マップに対するサンプラ。
            context.PixelShaderSamplers[1] = SamplerState.PointClamp;

            // シャドウ マップと共にグリッド モデルを描画。
            world = Matrix.Identity;
            DrawModel(gridModel, false);

            // シャドウ マップと共にデュード モデルを描画。
            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModel(dudeModel, false);
        }

        void DrawModel(Model model, bool createShadowMap)
        {
            var context = Device.ImmediateContext;

            if (createShadowMap)
            {
                // シャドウ マップ エフェクトの準備。
                shadowMapEffect.World = world;
                shadowMapEffect.View = currentLightCamera.View;
                shadowMapEffect.Projection = currentLightCamera.Projection;
                shadowMapEffect.Form = shadowMapEffectForm;
                shadowMapEffect.Apply(context);
            }
            else
            {
                // モデル描画エフェクトの準備。
                drawModelEffect.World = world;
                drawModelEffect.View = camera.View;
                drawModelEffect.Projection = camera.Projection;
                drawModelEffect.LightView = currentLightCamera.View;
                drawModelEffect.LightProjection = currentLightCamera.Projection;
                drawModelEffect.LightDirection = currentLightCamera.LightDirection;
                drawModelEffect.DepthBias = 0.001f;
                drawModelEffect.AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                drawModelEffect.ShadowMapEffectForm = shadowMapEffectForm;
                drawModelEffect.Apply(context);

                context.PixelShaderResources[1] = currentShadowRenderTarget.GetShaderResourceView();
            }

            // モデルを描画。
            // モデルは XNB 標準状態で読み込んでいるため、
            // メッシュ パートのエフェクトには BasicEffect が設定されている。

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffer(0, meshPart.VertexBuffer);
                    context.IndexBuffer = meshPart.IndexBuffer;

                    if (!createShadowMap)
                    {
                        // BasicEffect に設定されているモデルのテクスチャを取得して設定。
                        context.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;
                    }

                    context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void DrawShadowMapToScreen()
        {
            // 現在のフレームで生成したシャドウ マップを画面左上に表示。
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(currentShadowRenderTarget.GetShaderResourceView(), new Rectangle(0, 0, 128, 128), Color.White);
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text = "B = Light camera type (" + currentLightCameraType + ")\n" +
                "X = Shadow map form (" + shadowMapEffectForm + ")\n" +
                "Y = Use camera frustum as scene box (" + useCameraFrustumSceneBox + ")\n" +
                "L = Adjust LiSPSM optimal N (" + lispsmLightCamera.AdjustOptimalN + ")";

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

            if (currentKeyboardState.IsKeyUp(Keys.B) && lastKeyboardState.IsKeyDown(Keys.B) ||
                currentJoystickState.IsButtonUp(Buttons.B) && lastJoystickState.IsButtonDown(Buttons.B))
            {
                currentLightCameraType++;

                if (LightCameraType.Basic < currentLightCameraType)
                    currentLightCameraType = LightCameraType.LiSPSM;
            }

            if (currentKeyboardState.IsKeyUp(Keys.Y) && lastKeyboardState.IsKeyDown(Keys.Y) ||
                currentJoystickState.IsButtonUp(Buttons.Y) && lastJoystickState.IsButtonDown(Buttons.Y))
            {
                useCameraFrustumSceneBox = !useCameraFrustumSceneBox;
            }

            if (currentKeyboardState.IsKeyUp(Keys.X) && lastKeyboardState.IsKeyDown(Keys.X) ||
                currentJoystickState.IsButtonUp(Buttons.X) && lastJoystickState.IsButtonDown(Buttons.X))
            {
                if (shadowMapEffectForm == ShadowMapForm.Basic)
                {
                    shadowMapEffectForm = ShadowMapForm.Variance;
                }
                else
                {
                    shadowMapEffectForm = ShadowMapForm.Basic;
                }
            }

            if (currentKeyboardState.IsKeyUp(Keys.L) && lastKeyboardState.IsKeyDown(Keys.L) ||
                currentJoystickState.IsButtonUp(Buttons.LeftShoulder) && lastJoystickState.IsButtonDown(Buttons.LeftShoulder))
            {
                lispsmLightCamera.AdjustOptimalN = !lispsmLightCamera.AdjustOptimalN;
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
            float yaw = -currentJoystickState.ThumbSticks.Right.X * time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
                pitch += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Down))
                pitch -= time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Left))
                yaw += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
                yaw -= time * 0.001f;

            camera.Pitch(pitch);
            camera.Yaw(yaw);

            var movement = new Vector3();

            if (currentKeyboardState.IsKeyDown(Keys.W))
                movement.Z -= time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.S))
                movement.Z += time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.A))
                movement.X -= time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.D))
                movement.X += time * 0.1f;

            movement.Z -= currentJoystickState.ThumbSticks.Left.Y * time * 0.1f;
            movement.X += currentJoystickState.ThumbSticks.Left.X * time * 0.1f;

            camera.MoveRelative(ref movement);

            if (currentJoystickState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                camera.Position = new Vector3(0, 50, 50);
                camera.Direction = Vector3.Forward;
            }

            camera.Update();

            cameraFrustum.Matrix = camera.View * camera.Projection;
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
