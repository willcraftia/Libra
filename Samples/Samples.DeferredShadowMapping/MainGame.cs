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
        /// 最大分割数。
        /// </summary>
        const int MaxSplitCount = 3;

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
        /// 描画に使用するコンテキスト。
        /// </summary>
        DeviceContext context;

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
        /// シャドウ閉塞マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget occlusionMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ポストプロセス適用後の最終閉塞マップ。
        /// </summary>
        ShaderResourceView finalOcclusionMap;

        /// <summary>
        /// ポストプロセス適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// 閉塞マップ用ポストプロセス。
        /// </summary>
        Postprocess occlusionPostprocess;

        /// <summary>
        /// 表示シーン用ポストプロセス。
        /// </summary>
        Postprocess scenePostprocess;

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
        /// VSM 用ガウシアン フィルタ。
        /// </summary>
        GaussianFilterSuite vsmGaussianFilterSuite;

        /// <summary>
        /// 閉塞マップのブラー後に行うアップ サンプリング。
        /// </summary>
        UpFilter upFilter;

        /// <summary>
        /// 閉塞マップのブラー前に行うダウン サンプリング。
        /// </summary>
        DownFilter downFilter;

        /// <summary>
        /// 閉塞マップのブラー用ガウシアン フィルタ。
        /// </summary>
        GaussianFilter occlusionGaussianFilter;

        /// <summary>
        /// 閉塞マップのブラー用ガウシアン フィルタ水平パス。
        /// </summary>
        GaussianFilterPass occlusionGaussianFilterPassH;

        /// <summary>
        /// 閉塞マップのブラー用ガウシアン フィルタ垂直パス。
        /// </summary>
        GaussianFilterPass occlusionGaussianFilterPassV;

        /// <summary>
        /// 線形深度マップ エフェクト。
        /// </summary>
        LinearDepthMapEffect depthMapEffect;

        /// <summary>
        /// メッシュ描画のための基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        /// <summary>
        /// シャドウ閉塞マップ機能。
        /// </summary>
        ShadowOcclusionMap shadowOcclusionMap;

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
        /// 分割数。
        /// </summary>
        int splitCount = MaxSplitCount;

        /// <summary>
        /// PSSM 分割機能。
        /// </summary>
        PSSM pssm = new PSSM();

        /// <summary>
        /// 分割された距離の配列。
        /// </summary>
        float[] splitDistances = new float[MaxSplitCount + 1];

        /// <summary>
        /// 分割された射影行列の配列。
        /// </summary>
        Matrix[] splitProjections = new Matrix[MaxSplitCount];

        /// <summary>
        /// 分割されたシャドウ マップの配列。
        /// </summary>
        ShadowMap[] shadowMaps = new ShadowMap[MaxSplitCount];

        /// <summary>
        /// 分割されたライト カメラ空間行列の配列。
        /// </summary>
        Matrix[] lightViewProjections = new Matrix[MaxSplitCount];

        /// <summary>
        /// シャドウ マップ形式。
        /// </summary>
        ShadowMapForm shadowMapForm = ShadowMapForm.Basic;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        Vector3 lightDirection = new Vector3(0.3333333f, -0.6666667f, -0.6666667f);

        /// <summary>
        /// ライトによる投影を処理する距離。
        /// </summary>
        float lightFar = 1000.0f;

        /// <summary>
        /// 現在の表示カメラの境界錐台。
        /// </summary>
        BoundingFrustum currentFrustum = new BoundingFrustum(Matrix.Identity);

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

            pssm.Fov = camera.Fov;
            pssm.AspectRatio = camera.AspectRatio;
            pssm.NearClipDistance = camera.NearClipDistance;
            pssm.FarClipDistance = camera.FarClipDistance;

            textureDisplay = new TextureDisplay(this);
            textureDisplay.Visible = false;
            Components.Add(textureDisplay);

            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);

            //IsFixedTimeStep = false;
        }

        protected override void LoadContent()
        {
            context = Device.ImmediateContext;

            spriteBatch = new SpriteBatch(context);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMapRenderTarget = Device.CreateRenderTarget();
            depthMapRenderTarget.Width = WindowWidth;
            depthMapRenderTarget.Height = WindowHeight;
            depthMapRenderTarget.Format = SurfaceFormat.Single;
            depthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            depthMapRenderTarget.Initialize();

            occlusionMapRenderTarget = Device.CreateRenderTarget();
            occlusionMapRenderTarget.Width = WindowWidth / 1;
            occlusionMapRenderTarget.Height = WindowHeight / 1;
            occlusionMapRenderTarget.Format = SurfaceFormat.Single;
            occlusionMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            occlusionPostprocess = new Postprocess(context);
            occlusionPostprocess.Width = occlusionMapRenderTarget.Width;
            occlusionPostprocess.Height = occlusionMapRenderTarget.Height;

            scenePostprocess = new Postprocess(context);
            scenePostprocess.Width = normalSceneRenderTarget.Width;
            scenePostprocess.Height = normalSceneRenderTarget.Height;

            occlusionCombineFilter = new OcclusionCombineFilter(Device);
            occlusionCombineFilter.ShadowColor = new Vector3(0.5f, 0.5f, 0.5f);
            
            linearDepthMapColorFilter = new LinearDepthMapColorFilter(Device);
            linearDepthMapColorFilter.NearClipDistance = camera.NearClipDistance;
            linearDepthMapColorFilter.FarClipDistance = camera.FarClipDistance;
            linearDepthMapColorFilter.Enabled = false;

            occlusionMapColorFilter = new OcclusionMapColorFilter(Device);
            occlusionMapColorFilter.Enabled = false;

            scenePostprocess.Filters.Add(occlusionCombineFilter);
            scenePostprocess.Filters.Add(linearDepthMapColorFilter);
            scenePostprocess.Filters.Add(occlusionMapColorFilter);

            upFilter = new UpFilter(Device);
            downFilter = new DownFilter(Device);

            // 範囲と標準偏差に適した値は、アップ/ダウン サンプリングを伴うか否かで大きく異なる。
            occlusionGaussianFilter = new GaussianFilter(Device);
            occlusionGaussianFilter.Radius = 3;
            occlusionGaussianFilter.Sigma = 1;
            occlusionGaussianFilterPassH = new GaussianFilterPass(occlusionGaussianFilter, GaussianFilterDirection.Horizon);
            occlusionGaussianFilterPassV = new GaussianFilterPass(occlusionGaussianFilter, GaussianFilterDirection.Vertical);

            occlusionPostprocess.Filters.Add(downFilter);
            occlusionPostprocess.Filters.Add(occlusionGaussianFilterPassH);
            occlusionPostprocess.Filters.Add(occlusionGaussianFilterPassV);
            occlusionPostprocess.Filters.Add(upFilter);
            occlusionPostprocess.Enabled = true;

            depthMapEffect = new LinearDepthMapEffect(Device);

            basicEffect = new BasicEffect(Device);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();
            basicEffect.DirectionalLights[0].Direction = lightDirection;

            for (int i = 0; i < shadowMaps.Length; i++)
            {
                shadowMaps[i] = new ShadowMap(context);
            }

            // 深度バイアスは、主に PCF の際に重要となる。
            // VSM の場合、あまり意味は無い。
            shadowOcclusionMap = new ShadowOcclusionMap(context);
            shadowOcclusionMap.SplitCount = splitCount;
            shadowOcclusionMap.PcfEnabled = false;

            cubeMesh = new CubeMesh(context, 20);
            sphereMesh = new SphereMesh(context, 20, 32);
            cylinderMesh = new CylinderMesh(context, 80, 20, 32);
            squareMesh = new SquareMesh(context, 400);
            torusMesh = new TorusMesh(context, 20, 10);
            teapotMesh = new TeapotMesh(context, 40);
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
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            // シャドウ マップの描画。
            CreateShadowMap();

            // 深度マップを描画。
            CreateDepthMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // シャドウ閉塞マップを描画。
            CreateShadowOcclusionMap();

            // 閉塞マップへポストプロセスを適用。
            ApplyOcclusionMapPostprocess();

            // 通常シーンへポストプロセスを適用。
            ApplyScenePostprocess();

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
                    if (vsmGaussianFilterSuite == null)
                    {
                        var shadowMapSize = ShadowMapSizes[currentShadowMapSizeIndex];
                        vsmGaussianFilterSuite = new GaussianFilterSuite(
                            Device.ImmediateContext,
                            shadowMapSize,
                            shadowMapSize,
                            SurfaceFormat.Vector2);
                        vsmGaussianFilterSuite.Radius = 3;
                        vsmGaussianFilterSuite.Sigma = 1;
                    }

                    vsmGaussianFilterSuite.Filter(shadowMaps[i].RenderTarget, shadowMaps[i].RenderTarget);
                }

                // 生成されたシャドウ マップを一覧表示機能へ追加。
                textureDisplay.Textures.Add(shadowMaps[i].RenderTarget);
            }
        }

        // コールバック。
        void DrawShadowCasters(Matrix eyeView, Matrix eyeProjection, ShadowMapEffect effect)
        {
            Matrix viewProjection;
            Matrix.Multiply(ref eyeView, ref eyeProjection, out viewProjection);

            currentFrustum.Matrix = viewProjection;

            DrawShadowCasters(effect);
        }

        void CreateDepthMap()
        {
            context.SetRenderTarget(depthMapRenderTarget);
            context.Clear(new Vector4(float.MaxValue));

            DrawScene(depthMapEffect);

            context.SetRenderTarget(null);
        }

        void CreateNormalSceneMap()
        {
            context.SetRenderTarget(normalSceneRenderTarget);
            context.Clear(Color.CornflowerBlue);

            DrawScene(basicEffect);

            context.SetRenderTarget(null);

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalSceneRenderTarget);
        }

        void CreateShadowOcclusionMap()
        {
            context.SetRenderTarget(occlusionMapRenderTarget);
            context.Clear(Vector4.Zero);

            shadowOcclusionMap.ShadowMapForm = shadowMapForm;
            shadowOcclusionMap.View = camera.View;
            shadowOcclusionMap.Projection = camera.Projection;

            for (int i = 0; i < MaxSplitCount; i++)
            {
                shadowOcclusionMap.SetSplitDistance(i, splitDistances[i]);
                shadowOcclusionMap.SetLightViewProjection(i, lightViewProjections[i]);
                shadowOcclusionMap.SetShadowMap(i, shadowMaps[i].RenderTarget);
            }
            shadowOcclusionMap.SetSplitDistance(MaxSplitCount, splitDistances[MaxSplitCount]);
            shadowOcclusionMap.LinearDepthMap = depthMapRenderTarget;

            shadowOcclusionMap.Draw();

            context.SetRenderTarget(null);

            // フィルタへ設定。

            // 中間マップ表示。
            textureDisplay.Textures.Add(occlusionMapRenderTarget);
        }
        void ApplyOcclusionMapPostprocess()
        {
            finalOcclusionMap = occlusionPostprocess.Draw(occlusionMapRenderTarget);
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

            context.RasterizerState = RasterizerState.CullNone;
            DrawPrimitiveMesh(teapotMesh, Matrix.CreateTranslation(100, 10, -100), new Vector3(0, 1, 1), effect);
            context.RasterizerState = null;
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

            effect.Apply(context);
            mesh.Draw();
        }

        void ApplyScenePostprocess()
        {
            linearDepthMapColorFilter.LinearDepthMap = depthMapRenderTarget;
            occlusionCombineFilter.OcclusionMap = finalOcclusionMap;
            occlusionMapColorFilter.OcclusionMap = finalOcclusionMap;

            finalSceneTexture = scenePostprocess.Draw(normalSceneRenderTarget);
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
                    "[2] Shadow Map Form (" + shadowMapForm + ")\n" +
                    "[3] Camera Frustum as Scene Box (" + useCameraFrustumSceneBox + ")\n" +
                    "[4] Split count (" + splitCount + ")\n" +
                    "[5] Shadow map size (" + ShadowMapSizes[currentShadowMapSizeIndex] + "x" + ShadowMapSizes[currentShadowMapSizeIndex] + ")\n" +
                    "[T/G] Depth Bias (" + shadowOcclusionMap.DepthBias.ToString("F5") + ")\n" +
                    "[Y/H] PCF Radius (" + shadowOcclusionMap.PcfRadius + ")";
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
                "[F6] Use Pcf (" + shadowOcclusionMap.PcfEnabled + ")\n" +
                "[F7] Use Screen Space Blur (" + occlusionPostprocess.Enabled + ")";

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
                shadowOcclusionMap.PcfEnabled = !shadowOcclusionMap.PcfEnabled;

                if (shadowOcclusionMap.PcfEnabled)
                    occlusionPostprocess.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F7) && lastKeyboardState.IsKeyDown(Keys.F7))
            {
                occlusionPostprocess.Enabled = !occlusionPostprocess.Enabled;

                if (occlusionPostprocess.Enabled)
                    shadowOcclusionMap.PcfEnabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D1) && lastKeyboardState.IsKeyDown(Keys.D1))
            {
                currentLightCameraType++;

                if (LightCameraType.Basic < currentLightCameraType)
                    currentLightCameraType = LightCameraType.LiSPSM;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D2) && lastKeyboardState.IsKeyDown(Keys.D2))
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

            if (currentKeyboardState.IsKeyUp(Keys.D3) && lastKeyboardState.IsKeyDown(Keys.D3))
            {
                useCameraFrustumSceneBox = !useCameraFrustumSceneBox;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D4) && lastKeyboardState.IsKeyDown(Keys.D4))
            {
                splitCount++;
                if (MaxSplitCount < splitCount)
                    splitCount = 1;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D5) && lastKeyboardState.IsKeyDown(Keys.D5))
            {
                currentShadowMapSizeIndex++;
                if (ShadowMapSizes.Length <= currentShadowMapSizeIndex)
                    currentShadowMapSizeIndex = 0;

                if (vsmGaussianFilterSuite != null)
                {
                    vsmGaussianFilterSuite.Dispose();
                    vsmGaussianFilterSuite = null;
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.T))
            {
                shadowOcclusionMap.DepthBias += 0.00001f;
            }
            if (currentKeyboardState.IsKeyDown(Keys.G))
            {
                shadowOcclusionMap.DepthBias = Math.Max(shadowOcclusionMap.DepthBias - 0.00001f, 0.0f);
            }

            if (currentKeyboardState.IsKeyUp(Keys.Y) && lastKeyboardState.IsKeyDown(Keys.Y))
            {
                shadowOcclusionMap.PcfRadius = Math.Min(ShadowOcclusionMap.MaxPcfRadius, shadowOcclusionMap.PcfRadius + 1);
            }

            if (currentKeyboardState.IsKeyUp(Keys.H) && lastKeyboardState.IsKeyDown(Keys.H))
            {
                shadowOcclusionMap.PcfRadius = Math.Max(2, shadowOcclusionMap.PcfRadius - 1);
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
