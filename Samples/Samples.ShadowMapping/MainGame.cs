#region Using

using System;
using System.Runtime.InteropServices;
using System.Text;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
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
        /// モデル描画シェーダの簡易管理クラスです。
        /// </summary>
        sealed class DrawModelEffect
        {
            #region Constants

            /// <summary>
            /// 定数バッファの定義。
            /// </summary>
            [StructLayout(LayoutKind.Explicit, Size = 272 + (16 * MaxSplitDistanceCount) + (64 * MaxSplitCount))]
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

                [FieldOffset(256)]
                public Vector3 ShadowColor;

                [FieldOffset(272), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSplitDistanceCount)]
                public Vector4[] SplitDistances;

                [FieldOffset(272 + (16 * MaxSplitDistanceCount)), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSplitCount)]
                public Matrix[] LightViewProjections;
            }

            #endregion

            public Matrix World;

            public Matrix View;

            public Matrix Projection;

            public Vector4 AmbientColor;

            public float DepthBias;

            public int SplitCount;

            public Vector3 LightDirection;

            public Vector3 ShadowColor;

            public float[] SplitDistances;

            public ShadowMap[] ShadowMaps;

            public Matrix[] LightViewProjections;

            public ShadowMapForm ShadowMapForm;

            public ShaderResourceView Texture;

            Constants constants;

            VertexShader vertexShader;

            PixelShader basicPixelShader;

            PixelShader variancePixelShader;

            ConstantBuffer constantBuffer;

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

                constants.SplitDistances = new Vector4[MaxSplitDistanceCount];
                constants.LightViewProjections = new Matrix[MaxSplitCount];
            }

            public void Apply(DeviceContext context)
            {
                Matrix.Transpose(ref World, out constants.World);
                Matrix.Transpose(ref View, out constants.View);
                Matrix.Transpose(ref Projection, out constants.Projection);

                constants.AmbientColor = AmbientColor;
                constants.DepthBias = DepthBias;
                constants.SplitCount = SplitCount;
                constants.LightDirection = LightDirection;
                constants.ShadowColor = ShadowColor;

                Array.Clear(constants.LightViewProjections, 0, constants.LightViewProjections.Length);
                for (int i = 0; i < MaxSplitCount; i++)
                {
                    if (i < SplitCount)
                    {
                        Matrix.Transpose(ref LightViewProjections[i], out constants.LightViewProjections[i]);

                        context.PixelShaderResources[i + 1] = ShadowMaps[i].RenderTarget.GetShaderResourceView();
                    }
                    else
                    {
                        context.PixelShaderResources[i + 1] = null;
                    }
                }

                Array.Clear(constants.SplitDistances, 0, constants.SplitDistances.Length);
                for (int i = 0; i < MaxSplitDistanceCount; i++)
                {
                    constants.SplitDistances[i].X = SplitDistances[i];
                }

                constantBuffer.SetData(context, constants);

                context.VertexShaderConstantBuffers[0] = constantBuffer;
                context.PixelShaderConstantBuffers[0] = constantBuffer;

                context.PixelShaderResources[0] = Texture;

                context.VertexShader = vertexShader;

                if (ShadowMapForm == ShadowMapForm.Variance)
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

        /// <summary>
        /// ライト カメラの種類を表します。
        /// </summary>
        enum LightCameraType
        {
            /// <summary>
            /// LiSPSMLightCamera を用いる。
            /// </summary>
            LiSPSM  = 0,
            
            /// <summary>
            /// UniformLightCamera を用いる。
            /// </summary>
            Uniform = 1,

            /// <summary>
            /// BasicLightCamera を用いる。
            /// </summary>
            Basic   = 2,
        }

        #endregion

        /// <summary>
        /// 最大分割数。
        /// </summary>
        const int MaxSplitCount = 3;

        /// <summary>
        /// 最大分割距離数。
        /// </summary>
        const int MaxSplitDistanceCount = MaxSplitCount + 1;

        /// <summary>
        /// ウィンドウの幅。
        /// </summary>
        const int WindowWidth = 800;

        /// <summary>
        /// ウィンドウの高さ。
        /// </summary>
        const int WindowHeight = 480;

        /// <summary>
        /// シャドウ マップのサイズ。
        /// </summary>
        static readonly int[] ShadowMapSizes = { 512, 1024, 2048 };

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
        /// 中間マップ一覧表示機能。
        /// </summary>
        TextureDisplay textureDisplay;

        /// <summary>
        /// 表示カメラ。
        /// </summary>
        BasicCamera camera;
        
        /// <summary>
        /// 表示カメラの視錐台。
        /// </summary>
        BoundingFrustum cameraFrustum;

        /// <summary>
        /// 前回の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState lastKeyboardState;

        /// <summary>
        /// 前回の更新処理におけるジョイスティック状態。
        /// </summary>
        JoystickState lastJoystickState;

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
        /// デュード モデルの回転量。
        /// </summary>
        float rotateDude;

        /// <summary>
        /// モデルに適用するワールド行列。
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
        /// 現在選択されているライト カメラの種類。
        /// </summary>
        LightCameraType currentLightCameraType;

        /// <summary>
        /// 簡易ライト カメラ ビルダ。
        /// </summary>
        BasicLightCameraBuilder basicLightCameraBuilder;

        /// <summary>
        /// USM ライト カメラ ビルダ。
        /// </summary>
        UniformLightCameraBuilder uniformLightCameraBuilder;

        /// <summary>
        /// LiSPSM ライト カメラ ビルダ。
        /// </summary>
        LiSPSMLightCameraBuilder liSPSMLightCameraBuilder;

        /// <summary>
        /// 分割数。
        /// </summary>
        int splitCount;

        /// <summary>
        /// PSSM 分割機能。
        /// </summary>
        PSSM pssm;

        /// <summary>
        /// 分割された距離の配列。
        /// </summary>
        float[] splitDistances;

        /// <summary>
        /// 分割された射影行列の配列。
        /// </summary>
        Matrix[] splitProjections;

        /// <summary>
        /// 分割されたシャドウ マップの配列。
        /// </summary>
        ShadowMap[] shadowMaps;

        /// <summary>
        /// 分割されたライト カメラ空間行列の配列。
        /// </summary>
        Matrix[] lightViewProjections;

        /// <summary>
        /// シャドウ マップ形式。
        /// </summary>
        ShadowMapForm shadowMapForm;

        /// <summary>
        /// ガウシアン ブラー。
        /// </summary>
        GaussianBlurSuite gaussianBlur;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        /// <remarks>
        /// XNA Shadow Mapping では原点から見たライトの方向であり、
        /// ここでの方向の定義が異なる点に注意が必要です。
        /// </remarks>
        Vector3 lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

        /// <summary>
        /// ライトによる投影を処理する距離。
        /// </summary>
        float lightFar = 500.0f;

        /// <summary>
        /// 現在の表示カメラの境界錐台。
        /// </summary>
        BoundingFrustum currentFrustum;

        /// <summary>
        /// 現在のシャドウ マップ サイズのインデックス。
        /// </summary>
        int currentShadowMapSizeIndex = 1;

        /// <summary>
        /// フレーム レート計測器。
        /// </summary>
        FrameRateMeasure frameRateMeasure;

        /// <summary>
        /// ウィンドウ タイトル作成用文字列バッファ。
        /// </summary>
        StringBuilder stringBuilder;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = WindowWidth;
            graphicsManager.PreferredBackBufferHeight = WindowHeight;

            camera = new BasicCamera
            {
                Position = new Vector3(0, 70, 100),
                Direction = new Vector3(0, -0.4472136f, -0.8944272f),
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) WindowWidth / (float) WindowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };

            cameraFrustum = new BoundingFrustum(Matrix.Identity);

            corners = new Vector3[8];

            // gridModel が半径約 183 であるため、
            // これを含むように簡易シーン AABB を決定。
            // なお、広大な世界を扱う場合には、表示カメラの視錐台に含まれるオブジェクト、
            // および、それらに投影しうるオブジェクトを動的に選択および決定し、
            // 適切な最小シーン領域を算出して利用する。
            sceneBox = new BoundingBox(new Vector3(-200), new Vector3(200));

            useCameraFrustumSceneBox = true;

            currentLightCameraType = LightCameraType.LiSPSM;

            basicLightCameraBuilder = new BasicLightCameraBuilder();
            uniformLightCameraBuilder = new UniformLightCameraBuilder();
            uniformLightCameraBuilder.LightFarClipDistance = lightFar;
            liSPSMLightCameraBuilder = new LiSPSMLightCameraBuilder();
            liSPSMLightCameraBuilder.LightFarClipDistance = lightFar;

            splitCount = MaxSplitCount;

            pssm = new PSSM();
            pssm.Fov = camera.Fov;
            pssm.AspectRatio = camera.AspectRatio;
            pssm.NearClipDistance = camera.NearClipDistance;
            pssm.FarClipDistance = camera.FarClipDistance;

            splitDistances = new float[MaxSplitCount + 1];
            splitProjections = new Matrix[MaxSplitCount];
            shadowMaps = new ShadowMap[MaxSplitCount];
            lightViewProjections = new Matrix[MaxSplitCount];

            shadowMapForm = ShadowMapForm.Basic;

            // 単位ベクトル。
            lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

            currentFrustum = new BoundingFrustum(Matrix.Identity);

            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);

            textureDisplay = new TextureDisplay(this);
            textureDisplay.TextureWidth = 96;
            textureDisplay.TextureHeight = 96;
            Components.Add(textureDisplay);

            stringBuilder = new StringBuilder();
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

            for (int i = 0; i < shadowMaps.Length; i++)
            {
                shadowMaps[i] = new ShadowMap(Device);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            stringBuilder.Length = 0;
            stringBuilder.Append("FPS: ");
            stringBuilder.AppendNumber(frameRateMeasure.FrameRate);
            Window.Title = stringBuilder.ToString();

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

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
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

            // ライト カメラへ指定するシーン領域。
            BoundingBox actualSceneBox;
            if (useCameraFrustumSceneBox)
            {
                // 視錐台全体とする場合。
                cameraFrustum.Matrix = camera.View * camera.Projection;
                cameraFrustum.GetCorners(corners);
                actualSceneBox = BoundingBox.CreateFromPoints(corners);

            }
            else
            {
                // 明示する場合。
                actualSceneBox = sceneBox;
                actualSceneBox.Merge(camera.Position);
            }

            // 表示カメラの分割。
            // デフォルトのラムダ値 0.5f ではカメラ手前が少し狭すぎるか？
            // ここは表示カメラの far の値に応じて調整する。
            pssm.Count = splitCount;
            pssm.Lambda = 0.4f;
            pssm.View = camera.View;
            pssm.SceneBox = actualSceneBox;
            pssm.Split(splitDistances, splitProjections);

            // 使用するライト カメラ ビルダの選択。
            LightCameraBuilder currentLightCameraBuilder;
            switch (currentLightCameraType)
            {
                case LightCameraType.LiSPSM:
                    currentLightCameraBuilder = liSPSMLightCameraBuilder;
                    break;
                case LightCameraType.Uniform:
                    currentLightCameraBuilder = uniformLightCameraBuilder;
                    break;
                default:
                    currentLightCameraBuilder = basicLightCameraBuilder;
                    break;
            }

            // 各分割で共通のビルダ プロパティを設定。
            currentLightCameraBuilder.EyeView = camera.View;
            currentLightCameraBuilder.LightDirection = lightDirection;
            currentLightCameraBuilder.SceneBox = sceneBox;

            var context = Device.ImmediateContext;

            for (int i = 0; i < splitCount; i++)
            {
                // 射影行列は分割毎に異なる。
                currentLightCameraBuilder.EyeProjection = splitProjections[i];

                // ライトのビューおよび射影行列の算出。
                Matrix lightView;
                Matrix lightProjection;
                currentLightCameraBuilder.Build(out lightView, out lightProjection);

                // 後のモデル描画用にライト空間行列を算出。
                Matrix.Multiply(ref lightView, ref lightProjection, out lightViewProjections[i]);

                // シャドウ マップを描画。
                shadowMaps[i].Form = shadowMapForm;
                shadowMaps[i].Size = ShadowMapSizes[currentShadowMapSizeIndex];
                shadowMaps[i].Draw(context, camera.View, splitProjections[i], lightView, lightProjection, DrawShadowCasters);

                // VSM の場合は生成したシャドウ マップへブラーを適用。
                if (shadowMapForm == ShadowMapForm.Variance)
                {
                    if (gaussianBlur == null)
                    {
                        var shadowMapSize = ShadowMapSizes[currentShadowMapSizeIndex];
                        gaussianBlur = new GaussianBlurSuite(Device, shadowMapSize, shadowMapSize, SurfaceFormat.Vector2);
                        gaussianBlur.Radius = 7;
                        gaussianBlur.Amount = 7;
                    }

                    gaussianBlur.Filter(
                        context,
                        shadowMaps[i].RenderTarget.GetShaderResourceView(),
                        shadowMaps[i].RenderTarget.GetRenderTargetView());
                }

                // 生成されたシャドウ マップを一覧表示機能へ追加。
                textureDisplay.Textures.Add(shadowMaps[i].RenderTarget.GetShaderResourceView());
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
            DrawModelWithShadowMap(gridModel);

            // シャドウ マップと共にデュード モデルを描画。
            world = Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude));
            DrawModelWithShadowMap(dudeModel);
        }

        // コールバック。
        void DrawShadowCasters(Matrix eyeView, Matrix eyeProjection, ShadowMapEffect effect)
        {
            Matrix viewProjection;
            Matrix.Multiply(ref eyeView, ref eyeProjection, out viewProjection);

            currentFrustum.Matrix = viewProjection;

            ContainmentType containment;
            currentFrustum.Contains(ref dudeBoxWorld, out containment);
            if (containment != ContainmentType.Disjoint)
            {
                DrawShadowCaster(effect, dudeModel);
            }
        }

        void DrawShadowCaster(ShadowMapEffect effect, Model model)
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
            drawModelEffect.DepthBias = 0.001f;
            drawModelEffect.LightDirection = lightDirection;
            drawModelEffect.ShadowColor = new Vector3(0.5f, 0.5f, 0.5f);
            drawModelEffect.SplitCount = splitCount;
            drawModelEffect.SplitDistances = splitDistances;
            drawModelEffect.ShadowMaps = shadowMaps;
            drawModelEffect.ShadowMapForm = shadowMapForm;
            drawModelEffect.LightViewProjections = lightViewProjections;

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

        void DrawOverlayText()
        {
            var currentShadowMapSize = ShadowMapSizes[currentShadowMapSizeIndex];

            // HUD のテキストを表示。
            var text = "B = Light camera type (" + currentLightCameraType + ")\n" +
                "X = Shadow map form (" + shadowMapForm + ")\n" +
                "Y = Camera frustum as scene box (" + useCameraFrustumSceneBox + ")\n" +
                "K = Split count (" + splitCount + ")\n" +
                "L = Shadow map size (" + currentShadowMapSize + "x" + currentShadowMapSize + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 350), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 350 - 1), Color.Yellow);

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
                if (shadowMapForm == ShadowMapForm.Basic)
                {
                    shadowMapForm = ShadowMapForm.Variance;
                }
                else
                {
                    shadowMapForm = ShadowMapForm.Basic;
                }
            }

            if (currentKeyboardState.IsKeyUp(Keys.K) && lastKeyboardState.IsKeyDown(Keys.K) ||
                currentJoystickState.IsButtonUp(Buttons.RightShoulder) && lastJoystickState.IsButtonDown(Buttons.RightShoulder))
            {
                splitCount++;
                if (MaxSplitCount < splitCount)
                    splitCount = 1;
            }

            if (currentKeyboardState.IsKeyUp(Keys.L) && lastKeyboardState.IsKeyDown(Keys.L) ||
                currentJoystickState.IsButtonUp(Buttons.LeftShoulder) && lastJoystickState.IsButtonDown(Buttons.LeftShoulder))
            {
                currentShadowMapSizeIndex++;
                if (ShadowMapSizes.Length <= currentShadowMapSizeIndex)
                    currentShadowMapSizeIndex = 0;

                if (gaussianBlur != null)
                {
                    gaussianBlur.Dispose();
                    gaussianBlur = null;
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
