#region Using

using System;
using System.Text;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.DeferredShadowMapping
{
    public sealed class MainGame : Game
    {
        #region LightCameraType

        /// <summary>
        /// ライト カメラの種類を表します。
        /// </summary>
        enum LightCameraType
        {
            /// <summary>
            /// LiSPSMLightCamera を用いる。
            /// </summary>
            LiSPSM = 0,

            /// <summary>
            /// UniformLightCamera を用いる。
            /// </summary>
            Uniform = 1,

            /// <summary>
            /// BasicLightCamera を用いる。
            /// </summary>
            Basic = 2,
        }

        #endregion

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
        /// 表示カメラの初期位置。
        /// </summary>
        static readonly Vector3 initialCameraPosition = new Vector3(0, 70, 100);

        /// <summary>
        /// 表示カメラの初期注視点。
        /// </summary>
        static readonly Vector3 initialCameraLookAt = Vector3.Zero;

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
        /// 中間マップ表示。
        /// </summary>
        TextureDisplay textureDisplay;

        /// <summary>
        /// フレーム レート計測器。
        /// </summary>
        FrameRateMeasure frameRateMeasure;

        /// <summary>
        /// ウィンドウ タイトル文字列ビルダ。
        /// </summary>
        StringBuilder titleBuilder = new StringBuilder();

        /// <summary>
        /// 表示カメラ。
        /// </summary>
        BasicCamera camera;

        /// <summary>
        /// 前回の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState lastKeyboardState;

        /// <summary>
        /// 現在の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState currentKeyboardState;

        /// <summary>
        /// 深度マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget depthMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// フィルタ チェーン適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// フィルタ チェーン。
        /// </summary>
        FilterChain filterChain;

        /// <summary>
        /// 閉塞マップ合成フィルタ。
        /// </summary>
        OcclusionCombineFilter occlusionCombineFilter;

        /// <summary>
        /// 線形深度マップ可視化フィルタ。
        /// </summary>
        LinearDepthMapColorFilter linearDepthMapColorFilter;

        /// <summary>
        /// 閉塞マップ可視化フィルタ。
        /// </summary>
        OcclusionMapColorFilter occlusionMapColorFilter;

        /// <summary>
        /// 線形深度マップ エフェクト。
        /// </summary>
        LinearDepthMapEffect depthMapEffect;

        /// <summary>
        /// メッシュ描画のための基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        /// <summary>
        /// 立方体メッシュ。
        /// </summary>
        CubeMesh cubeMesh;

        /// <summary>
        /// 球メッシュ。
        /// </summary>
        SphereMesh sphereMesh;

        /// <summary>
        /// 円柱メッシュ。
        /// </summary>
        CylinderMesh cylinderMesh;

        /// <summary>
        /// 正方形メッシュ。
        /// </summary>
        SquareMesh squareMesh;

        /// <summary>
        /// 輪環体メッシュ。
        /// </summary>
        TorusMesh torusMesh;

        /// <summary>
        /// ティーポット メッシュ。
        /// </summary>
        TeapotMesh teapotMesh;

        /// <summary>
        /// 表示カメラの視錐台。
        /// </summary>
        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);

        /// <summary>
        /// 表示カメラの視錐台をシーン領域として用いるか否かを示す値。
        /// true (表示カメラの視錐台をシーン領域として用いる場合)、
        /// false (sceneBox で明示した領域をシーン領域として用いる場合)。
        /// </summary>
        bool useCameraFrustumSceneBox = true;

        /// <summary>
        /// 視錐台や境界ボックスの頂点を得るための一時作業配列。
        /// </summary>
        Vector3[] corners = new Vector3[8];

        /// <summary>
        /// 明示するシーン領域。
        /// </summary>
        BoundingBox sceneBox;

        /// <summary>
        /// 現在選択されているライト カメラの種類。
        /// </summary>
        LightCameraType currentLightCameraType = LightCameraType.Basic;

        /// <summary>
        /// 簡易ライト カメラ ビルダ。
        /// </summary>
        BasicLightCameraBuilder basicLightCameraBuilder = new BasicLightCameraBuilder();

        /// <summary>
        /// USM ライト カメラ ビルダ。
        /// </summary>
        UniformLightCameraBuilder uniformLightCameraBuilder = new UniformLightCameraBuilder();

        /// <summary>
        /// LiSPSM ライト カメラ ビルダ。
        /// </summary>
        LiSPSMLightCameraBuilder liSPSMLightCameraBuilder = new LiSPSMLightCameraBuilder();

        /// <summary>
        /// カスケード シャドウ マップ。
        /// </summary>
        CascadeShadowMap cascadeShadowMap;

        /// <summary>
        /// シャドウ シーン マップ。
        /// </summary>
        ShadowSceneMap shadowSceneMap;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        Vector3 lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

        /// <summary>
        /// ライトによる投影を処理する距離。
        /// </summary>
        float lightFar = 1000.0f;

        /// <summary>
        /// 現在のシャドウ マップ サイズのインデックス。
        /// </summary>
        int currentShadowMapSizeIndex = 1;

        /// <summary>
        /// HUD テキストを表示するか否かを示す値。
        /// </summary>
        bool hudVisible = true;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = WindowWidth;
            graphicsManager.PreferredBackBufferHeight = WindowHeight;

            camera = new BasicCamera
            {
                Position = initialCameraPosition,
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) WindowWidth / (float) WindowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };
            camera.LookAt(initialCameraLookAt);
            camera.Update();

            // 簡易シーン AABB を決定。
            // 広大な世界を扱う場合には、表示カメラの視錐台に含まれるオブジェクト、
            // および、それらに投影しうるオブジェクトを動的に選択および決定し、
            // 適切な最小シーン領域を算出して利用する。
            sceneBox = new BoundingBox(new Vector3(-300), new Vector3(300));

            uniformLightCameraBuilder.LightFarClipDistance = lightFar;
            liSPSMLightCameraBuilder.LightFarClipDistance = lightFar;

            textureDisplay = new TextureDisplay(this);
            textureDisplay.Visible = false;
            Components.Add(textureDisplay);

            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(DeviceContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMapRenderTarget = Device.CreateRenderTarget();
            depthMapRenderTarget.Width = WindowWidth;
            depthMapRenderTarget.Height = WindowHeight;
            depthMapRenderTarget.Format = SurfaceFormat.Single;
            depthMapRenderTarget.DepthStencilEnabled = true;
            depthMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.PreferredMultisampleCount = Device.BackBufferMultisampleCount;
            normalSceneRenderTarget.DepthStencilEnabled = true;
            normalSceneRenderTarget.Initialize();

            filterChain = new FilterChain(DeviceContext);
            filterChain.Width = normalSceneRenderTarget.Width;
            filterChain.Height = normalSceneRenderTarget.Height;

            occlusionCombineFilter = new OcclusionCombineFilter(DeviceContext);
            occlusionCombineFilter.ShadowColor = new Vector3(0.5f, 0.5f, 0.5f);

            linearDepthMapColorFilter = new LinearDepthMapColorFilter(DeviceContext);
            linearDepthMapColorFilter.NearClipDistance = camera.NearClipDistance;
            linearDepthMapColorFilter.FarClipDistance = camera.FarClipDistance;
            linearDepthMapColorFilter.Enabled = false;

            occlusionMapColorFilter = new OcclusionMapColorFilter(DeviceContext);
            occlusionMapColorFilter.Enabled = false;

            filterChain.Filters.Add(occlusionCombineFilter);
            filterChain.Filters.Add(linearDepthMapColorFilter);
            filterChain.Filters.Add(occlusionMapColorFilter);

            depthMapEffect = new LinearDepthMapEffect(DeviceContext);

            basicEffect = new BasicEffect(DeviceContext);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();
            basicEffect.DirectionalLights[0].Direction = lightDirection;

            cascadeShadowMap = new CascadeShadowMap(DeviceContext);
            cascadeShadowMap.Fov = camera.Fov;
            cascadeShadowMap.AspectRatio = camera.AspectRatio;
            cascadeShadowMap.NearClipDistance = camera.NearClipDistance;
            cascadeShadowMap.FarClipDistance = camera.FarClipDistance;
            cascadeShadowMap.DrawShadowCastersCallback = DrawShadowCasters;

            shadowSceneMap = new ShadowSceneMap(DeviceContext);
            shadowSceneMap.RenderTargetWidth = WindowWidth;
            shadowSceneMap.RenderTargetHeight = WindowHeight;
            shadowSceneMap.PreferredRenderTargetMultisampleCount = Device.BackBufferMultisampleCount;
            shadowSceneMap.SplitCount = cascadeShadowMap.SplitCount;

            cubeMesh = new CubeMesh(DeviceContext, 20);
            sphereMesh = new SphereMesh(DeviceContext, 20, 32);
            cylinderMesh = new CylinderMesh(DeviceContext, 80, 20, 32);
            squareMesh = new SquareMesh(DeviceContext, 400);
            torusMesh = new TorusMesh(DeviceContext, 20, 10);
            teapotMesh = new TeapotMesh(DeviceContext, 40);
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            const float scale = 0.2f;
            textureDisplay.TextureWidth = (int) (WindowWidth * scale);
            textureDisplay.TextureHeight = (int) (WindowHeight * scale);

            titleBuilder.Length = 0;
            titleBuilder.Append("FPS: ");
            titleBuilder.AppendNumber(frameRateMeasure.FrameRate);
            Window.Title = titleBuilder.ToString();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // 念のため状態を初期状態へ。
            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.Default;

            // 深度マップを描画。
            CreateDepthMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // シャドウ マップの描画。
            CreateShadowMap();

            // シャドウ シーン マップを描画。
            CreateShadowSceneMap();

            // 通常シーンへフィルタ チェーンを適用。
            ApplyFilterChain();

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }
        
        void CreateShadowMap()
        {
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

            cascadeShadowMap.View = camera.View;
            cascadeShadowMap.LightDirection = lightDirection;
            cascadeShadowMap.ShadowMapSize = ShadowMapSizes[currentShadowMapSizeIndex];
            cascadeShadowMap.SceneBox = actualSceneBox;
            cascadeShadowMap.LightCameraBuilder = currentLightCameraBuilder;

            cascadeShadowMap.Draw();

            for (int i = 0; i < cascadeShadowMap.SplitCount; i++)
                textureDisplay.Textures.Add(cascadeShadowMap.GetTexture(i));
        }

        void CreateDepthMap()
        {
            DeviceContext.SetRenderTarget(depthMapRenderTarget);
            DeviceContext.Clear(new Vector4(float.MaxValue));

            DrawScene(depthMapEffect);

            DeviceContext.SetRenderTarget(null);
        }

        void CreateNormalSceneMap()
        {
            DeviceContext.SetRenderTarget(normalSceneRenderTarget);
            DeviceContext.Clear(Color.CornflowerBlue);

            DrawScene(basicEffect);

            DeviceContext.SetRenderTarget(null);

            textureDisplay.Textures.Add(normalSceneRenderTarget);
        }

        void CreateShadowSceneMap()
        {
            shadowSceneMap.View = camera.View;
            shadowSceneMap.Projection = camera.Projection;
            shadowSceneMap.LinearDepthMap = depthMapRenderTarget;
            shadowSceneMap.UpdateShadowMapSettings(cascadeShadowMap);

            shadowSceneMap.Draw();

            textureDisplay.Textures.Add(shadowSceneMap.BaseTexture);
            textureDisplay.Textures.Add(shadowSceneMap.FinalTexture);
        }

        void DrawPrimitiveMesh(PrimitiveMesh mesh, Matrix world, Vector3 color)
        {
            basicEffect.DiffuseColor = color;

            DrawPrimitiveMesh(mesh, world, color, basicEffect);
        }

        void DrawScene(IEffect effect)
        {
            var effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.View = camera.View;
                effectMatrices.Projection = camera.Projection;
            }

            DrawShadowReveivers(effect);
        }

        void DrawShadowReveivers(IEffect effect)
        {
            DrawShadowCasters(effect);

            DrawPrimitiveMesh(squareMesh, Matrix.Identity, new Vector3(0.5f), effect);
        }

        void DrawShadowCasters(IEffect effect)
        {
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-85, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-60, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-40, 10, 0), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(10, 10, -60), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(0, 10, -40), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(torusMesh, Matrix.CreateTranslation(40, 5, -40), new Vector3(1, 1, 0), effect);
            for (float z = -180; z <= 180; z += 40)
            {
                DrawPrimitiveMesh(cylinderMesh, Matrix.CreateTranslation(-180, 40, z), new Vector3(0, 0, 1), effect);
            }

            DeviceContext.RasterizerState = RasterizerState.CullNone;
            DrawPrimitiveMesh(teapotMesh, Matrix.CreateTranslation(100, 10, -100), new Vector3(0, 1, 1), effect);
            DeviceContext.RasterizerState = null;
        }

        void DrawPrimitiveMesh(PrimitiveMesh mesh, Matrix world, Vector3 color, IEffect effect)
        {
            var effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.World = world;
            }

            var basicEffect = effect as BasicEffect;
            if (basicEffect != null)
            {
                basicEffect.DiffuseColor = color;
            }

            effect.Apply();
            mesh.Draw();
        }

        void ApplyFilterChain()
        {
            linearDepthMapColorFilter.LinearDepthMap = depthMapRenderTarget;
            occlusionCombineFilter.OcclusionMap = shadowSceneMap.FinalTexture;
            occlusionMapColorFilter.OcclusionMap = shadowSceneMap.FinalTexture;

            finalSceneTexture = filterChain.Draw(normalSceneRenderTarget);
        }

        void CreateFinalSceneMap()
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(finalSceneTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            if (!hudVisible)
                return;

            // HUD のテキストを表示。
            string text0;
            if (currentKeyboardState.IsKeyUp(Keys.ControlKey))
            {
                text0 =
                    "[1] Light Camera Type (" + currentLightCameraType + ")\n" +
                    "[2] Shadow Map Form (" + cascadeShadowMap.ShadowMapForm + ")\n" +
                    "[3] Camera Frustum as Scene Box (" + useCameraFrustumSceneBox + ")\n" +
                    "[4] Split count (" + cascadeShadowMap.SplitCount + ")\n" +
                    "[5] Shadow map size (" + ShadowMapSizes[currentShadowMapSizeIndex] + "x" + ShadowMapSizes[currentShadowMapSizeIndex] + ")\n" +
                    "[T/G] Depth Bias (" + shadowSceneMap.DepthBias.ToString("F5") + ")\n" +
                    "[Y/H] PCF Radius (" + shadowSceneMap.PcfRadius + ")";
            }
            else
            {
                text0 =
                    "";
            }

            string text1 =
                "[F1] HUD On/Off\n" +
                "[F2] Inter-maps On/Off\n" +
                "[F3] Combine Occlusion (" + occlusionCombineFilter.Enabled + ")\n" +
                "[F4] Depth Map (" + linearDepthMapColorFilter.Enabled + ")\n" +
                "[F5] Occlusion Map " + (occlusionMapColorFilter.Enabled ? "(Current)" : "") + "\n" +
                "[F6] Use Pcf (" + shadowSceneMap.PcfEnabled + ")\n" +
                "[F7] Use Screen Space Blur (" + shadowSceneMap.BlurEnabled + ")";

            spriteBatch.Begin();

            const int locationY = 310;

            const int locationX0 = 65;
            const int locationX1 = 449;

            spriteBatch.DrawString(spriteFont, text0, new Vector2(locationX0, locationY), Color.Black);
            spriteBatch.DrawString(spriteFont, text0, new Vector2(locationX0 - 1, locationY - 1), Color.Yellow);

            spriteBatch.DrawString(spriteFont, text1, new Vector2(locationX1, locationY), Color.Black);
            spriteBatch.DrawString(spriteFont, text1, new Vector2(locationX1 - 1, locationY - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyUp(Keys.F1) && lastKeyboardState.IsKeyDown(Keys.F1))
                hudVisible = !hudVisible;

            if (currentKeyboardState.IsKeyUp(Keys.F2) && lastKeyboardState.IsKeyDown(Keys.F2))
                textureDisplay.Visible = !textureDisplay.Visible;

            if (currentKeyboardState.IsKeyUp(Keys.F3) && lastKeyboardState.IsKeyDown(Keys.F3))
                occlusionCombineFilter.Enabled = !occlusionCombineFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.F4) && lastKeyboardState.IsKeyDown(Keys.F4))
            {
                linearDepthMapColorFilter.Enabled = !linearDepthMapColorFilter.Enabled;
                occlusionMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F5) && lastKeyboardState.IsKeyDown(Keys.F5))
            {
                occlusionMapColorFilter.Enabled = !occlusionMapColorFilter.Enabled;
                linearDepthMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F6) && lastKeyboardState.IsKeyDown(Keys.F6))
            {
                shadowSceneMap.PcfEnabled = !shadowSceneMap.PcfEnabled;

                if (shadowSceneMap.PcfEnabled)
                    shadowSceneMap.BlurEnabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F7) && lastKeyboardState.IsKeyDown(Keys.F7))
            {
                shadowSceneMap.BlurEnabled = !shadowSceneMap.BlurEnabled;

                if (shadowSceneMap.BlurEnabled)
                    shadowSceneMap.PcfEnabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D1) && lastKeyboardState.IsKeyDown(Keys.D1))
            {
                currentLightCameraType++;

                if (LightCameraType.Basic < currentLightCameraType)
                    currentLightCameraType = LightCameraType.LiSPSM;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D2) && lastKeyboardState.IsKeyDown(Keys.D2))
            {
                if (cascadeShadowMap.ShadowMapForm == ShadowMapForm.Basic)
                {
                    cascadeShadowMap.ShadowMapForm = ShadowMapForm.Variance;
                }
                else
                {
                    cascadeShadowMap.ShadowMapForm = ShadowMapForm.Basic;
                }
            }

            if (currentKeyboardState.IsKeyUp(Keys.D3) && lastKeyboardState.IsKeyDown(Keys.D3))
            {
                useCameraFrustumSceneBox = !useCameraFrustumSceneBox;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D4) && lastKeyboardState.IsKeyDown(Keys.D4))
            {
                var count = cascadeShadowMap.SplitCount + 1;
                if (CascadeShadowMap.MaxSplitCount < count) count = 1;
                cascadeShadowMap.SplitCount = count;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D5) && lastKeyboardState.IsKeyDown(Keys.D5))
            {
                currentShadowMapSizeIndex++;
                if (ShadowMapSizes.Length <= currentShadowMapSizeIndex)
                    currentShadowMapSizeIndex = 0;
            }

            if (currentKeyboardState.IsKeyDown(Keys.T))
            {
                shadowSceneMap.DepthBias += 0.00001f;
            }
            if (currentKeyboardState.IsKeyDown(Keys.G))
            {
                shadowSceneMap.DepthBias = Math.Max(shadowSceneMap.DepthBias - 0.00001f, 0.0f);
            }

            if (currentKeyboardState.IsKeyUp(Keys.Y) && lastKeyboardState.IsKeyDown(Keys.Y))
            {
                shadowSceneMap.PcfRadius = Math.Min(ShadowSceneMapEffect.MaxPcfRadius, shadowSceneMap.PcfRadius + 1);
            }

            if (currentKeyboardState.IsKeyUp(Keys.H) && lastKeyboardState.IsKeyDown(Keys.H))
            {
                shadowSceneMap.PcfRadius = Math.Max(2, shadowSceneMap.PcfRadius - 1);
            }

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();
        }

        void UpdateCamera(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            float pitch = 0.0f;
            float yaw = 0.0f;

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

            camera.MoveRelative(ref movement);

            if (currentKeyboardState.IsKeyDown(Keys.R))
            {
                camera.Position = initialCameraPosition;
                camera.LookAt(initialCameraLookAt);
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
