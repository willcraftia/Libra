#region Using

using System;
using System.Runtime.InteropServices;
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
            [StructLayout(LayoutKind.Explicit, Size = 256 + (16 + 64) * PSSMCameras.MaxSplitCount)]
            struct Constants
            {
                [FieldOffset(0)]
                public Matrix World;

                [FieldOffset(64)]
                public Matrix View;

                [FieldOffset(128)]
                public Matrix Projection;

                [FieldOffset(192)]
                public Vector4 AmbientColor;

                [FieldOffset(208)]
                public float DepthBias;

                [FieldOffset(224)]
                public int SplitCount;

                [FieldOffset(240)]
                public Vector3 LightDirection;

                [FieldOffset(256), MarshalAs(UnmanagedType.ByValArray, SizeConst = PSSMCameras.MaxSplitCount)]
                public Vector4[] SplitDistances;

                [FieldOffset(320), MarshalAs(UnmanagedType.ByValArray, SizeConst = PSSMCameras.MaxSplitCount)]
                public Matrix[] LightViewProjection;
            }

            #endregion

            public Matrix World;

            public Matrix View;

            public Matrix Projection;

            public Vector4 AmbientColor;

            public float DepthBias;

            public ShadowMap ShadowMap;

            public ShaderResourceView Texture;

            Constants constants;

            VertexShader vertexShader;

            PixelShader basicPixelShader;

            PixelShader variancePixelShader;

            ConstantBuffer constantBuffer;

            float[] splitDistances;

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

                // splitDistances は (PSSMCameras.MaxSplitCount + 1) の容量。

                constants.SplitDistances = new Vector4[PSSMCameras.MaxSplitCount + 1];
                constants.LightViewProjection = new Matrix[PSSMCameras.MaxSplitCount];

                splitDistances = new float[PSSMCameras.MaxSplitCount + 1];
            }

            public void Apply(DeviceContext context)
            {
                Matrix.Transpose(ref World, out constants.World);
                Matrix.Transpose(ref View, out constants.View);
                Matrix.Transpose(ref Projection, out constants.Projection);

                constants.AmbientColor = AmbientColor;
                constants.DepthBias = DepthBias;
                constants.SplitCount = ShadowMap.SplitCount;
                constants.LightDirection = ShadowMap.LightDirection;

                for (int i = 0; i < PSSMCameras.MaxSplitCount; i++)
                {
                    if (i < ShadowMap.SplitCount)
                    {
                        var lightCamera = ShadowMap.GetLightCamera(i);
                        Matrix lightViewProjection;
                        Matrix.Multiply(ref lightCamera.View, ref lightCamera.Projection, out lightViewProjection);

                        Matrix.Transpose(ref lightViewProjection, out constants.LightViewProjection[i]);

                        context.PixelShaderResources[i + 1] = ShadowMap.GetTexture(i).GetShaderResourceView();
                    }
                    else
                    {
                        constants.SplitDistances[i].X = 0.0f;
                        constants.LightViewProjection[i] = Matrix.Identity;
                        context.PixelShaderResources[i + 1] = null;
                    }
                }

                Array.Clear(splitDistances, 0, splitDistances.Length);
                ShadowMap.GetSplitDistances(splitDistances);
                for (int i = 0; i < splitDistances.Length; i++)
                {
                    constants.SplitDistances[i].X = splitDistances[i];
                }

                constantBuffer.SetData(context, constants);

                context.VertexShaderConstantBuffers[0] = constantBuffer;
                context.PixelShaderConstantBuffers[0] = constantBuffer;

                context.PixelShaderResources[0] = Texture;

                context.VertexShader = vertexShader;

                if (ShadowMap.Form == ShadowMapForm.Variance)
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
        /// デュード モデルのローカル空間境界ボックス。
        /// </summary>
        BoundingBox dudeBoxLocal;

        /// <summary>
        /// デュード モデルのワールド空間境界ボックス。
        /// </summary>
        BoundingBox dudeBoxWorld;

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
        /// シャドウ マップ生成機能。
        /// </summary>
        ShadowMap shadowMap;

        // ライトの進行方向 (XNA Shadow Mapping では原点から見たライトの方向)。
        // 単位ベクトル。
        Vector3 lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

        // ライトによる投影を処理する距離。
        float lightFar = 500.0f;

        /// <summary>
        /// 現在選択されているライト カメラの種類。
        /// </summary>
        LightCameraType currentLightCameraType;

        BoundingFrustum splitFrustum;

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

            currentLightCameraType = LightCameraType.LiSPSM;

            splitFrustum = new BoundingFrustum(Matrix.Identity);
        }

        LightCamera CreateBasicLightCamera()
        {
            return new BasicLightCamera();
        }

        LightCamera CreateFocusedLightCamera()
        {
            var result = new FocusedLightCamera();
            result.LightFarClipDistance = lightFar;
            return result;
        }

        LightCamera CreateLiSPSMLightCamera()
        {
            var result = new LiSPSMLightCamera();
            result.LightFarClipDistance = lightFar;
            return result;
        }

        protected override void LoadContent()
        {
            drawModelEffect = new DrawModelEffect(Device);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");

            dudeBoxLocal = BoundingBox.Empty;
            foreach (var mesh in dudeModel.Meshes)
            {
                dudeBoxLocal.Merge(BoundingBox.CreateFromSphere(mesh.BoundingSphere));
            }

            shadowMap = new ShadowMap(Device);
            shadowMap.Size = shadowMapSize;
            shadowMap.SplitCount = 3;
            shadowMap.Form = ShadowMapForm.Variance;
            shadowMap.BlurRadius = 7;
            // TODO
            // ブラーを強くすると影の弱い部分が大きくなってしまう・・・
            shadowMap.BlurAmount = 7;
            shadowMap.LightDirection = lightDirection;
            shadowMap.CreateLightCamera = CreateLiSPSMLightCamera;
            shadowMap.DrawShadowCasters = DrawShadowCasters;
            shadowMap.Form = ShadowMapForm.Variance;
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

            // 念のため状態を初期状態へ。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

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

        void PrepareShadowMap()
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
                    shadowMap.CreateLightCamera = CreateLiSPSMLightCamera;
                    break;
                case LightCameraType.Focused:
                    shadowMap.CreateLightCamera = CreateFocusedLightCamera;
                    break;
                default:
                    shadowMap.CreateLightCamera = CreateBasicLightCamera;
                    break;
            }

            // シャドウ マッピング機能の準備。
            shadowMap.Prepare(camera.View, camera.Projection, actualSceneBox);
        }

        void CreateShadowMap()
        {
            // デュード モデルのワールド行列。
            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));

            dudeBoxLocal.GetCorners(corners);
            dudeBoxWorld = BoundingBox.Empty;
            foreach (var corner in corners)
            {
                Vector3 cornerLocal = corner;
                Vector3 cornerWorld;
                Vector3.Transform(ref cornerLocal, ref world, out cornerWorld);

                dudeBoxWorld.Merge(ref cornerWorld);
            }

            PrepareShadowMap();

            shadowMap.Draw(Device.ImmediateContext);
        }

        void DrawWithShadowMap()
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            // シャドウ マップに対するサンプラ。
            context.PixelShaderSamplers[1] = SamplerState.PointClamp;

            // シャドウ マップと共にグリッド モデルを描画。
            world = Matrix.Identity;
            DrawModelWithShadowMap(gridModel);

            // シャドウ マップと共にデュード モデルを描画。
            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModelWithShadowMap(dudeModel);
        }

        // コールバック。
        void DrawShadowCasters(Camera camera, ShadowMapEffect effect)
        {
            Matrix viewProjection;
            Matrix.Multiply(ref camera.View, ref camera.Projection, out viewProjection);

            splitFrustum.Matrix = viewProjection;

            ContainmentType containment;
            splitFrustum.Contains(ref dudeBoxWorld, out containment);
            if (containment != ContainmentType.Disjoint)
            {
                DrawShadowCaster(camera, effect, dudeModel);
            }
        }

        void DrawShadowCaster(Camera camera, ShadowMapEffect effect, Model model)
        {
            var context = Device.ImmediateContext;

            // シャドウ マップ エフェクトの準備。
            effect.World = world;
            effect.Apply(context);

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffer(0, meshPart.VertexBuffer);
                    context.IndexBuffer = meshPart.IndexBuffer;
                    context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void DrawModelWithShadowMap(Model model)
        {
            var context = Device.ImmediateContext;

            drawModelEffect.World = world;
            drawModelEffect.View = camera.View;
            drawModelEffect.Projection = camera.Projection;
            drawModelEffect.AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            drawModelEffect.DepthBias = 0.0001f;
            drawModelEffect.ShadowMap = shadowMap;

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

                    // BasicEffect に設定されているモデルのテクスチャを取得して設定。
                    drawModelEffect.Texture = (meshPart.Effect as BasicEffect).Texture;

                    drawModelEffect.Apply(context);

                    context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void DrawShadowMapToScreen()
        {
            // 現在のフレームで生成したシャドウ マップを画面左上に表示。
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            for (int i = 0; i < shadowMap.SplitCount; i++)
            {
                var x = i * 128;
                spriteBatch.Draw(shadowMap.GetTexture(i).GetShaderResourceView(), new Rectangle(x, 0, 128, 128), Color.White);
            }
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            //var text = "B = Light camera type (" + currentLightCameraType + ")\n" +
            //    "X = Shadow map form (" + shadowMapEffectForm + ")\n" +
            //    "Y = Use camera frustum as scene box (" + useCameraFrustumSceneBox + ")\n" +
            //    "L = Adjust LiSPSM optimal N (" + lispsmLightCamera.AdjustOptimalN + ")";
            var text = "B = Light camera type (" + currentLightCameraType + ")\n" +
                "X = Shadow map form (" + shadowMap.Form + ")\n" +
                "Y = Use camera frustum as scene box (" + useCameraFrustumSceneBox + ")";

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
                if (shadowMap.Form == ShadowMapForm.Basic)
                {
                    shadowMap.Form = ShadowMapForm.Variance;
                }
                else
                {
                    shadowMap.Form = ShadowMapForm.Basic;
                }
            }

            //if (currentKeyboardState.IsKeyUp(Keys.L) && lastKeyboardState.IsKeyDown(Keys.L) ||
            //    currentJoystickState.IsButtonUp(Buttons.LeftShoulder) && lastJoystickState.IsButtonDown(Buttons.LeftShoulder))
            //{
            //    lispsmLightCamera.AdjustOptimalN = !lispsmLightCamera.AdjustOptimalN;
            //}

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
