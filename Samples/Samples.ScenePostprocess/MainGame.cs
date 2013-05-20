#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.ScenePostprocess
{
    public sealed class MainGame : Game
    {
        #region PostprocessType

        enum PostprocessType
        {
            None,
            DepthOfField,
            Bloom,
            Blur,
            BilateralFilter
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
        /// 法線マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ポストプロセス適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// ポストプロセス。
        /// </summary>
        Postprocess postprocess;

        /// <summary>
        /// ダウン フィルタ。
        /// </summary>
        DownFilter downFilter;

        /// <summary>
        /// アップ フィルタ。
        /// </summary>
        UpFilter upFilter;

        /// <summary>
        /// ガウシアン フィルタ。
        /// </summary>
        GaussianFilter gaussianFilter;

        /// <summary>
        /// ガウシアン フィルタ 水平パス。
        /// </summary>
        GaussianFilterPass gaussianFilterH;

        /// <summary>
        /// ガウシアン フィルタ 垂直パス。
        /// </summary>
        GaussianFilterPass gaussianFilterV;

        /// <summary>
        /// バイラテラル フィルタ。
        /// </summary>
        BilateralFilter bilateralFilter;

        /// <summary>
        /// バイラテラル フィルタ パス (水平)。
        /// </summary>
        GaussianFilterPass bilateralFilterH;

        /// <summary>
        /// バイラテラル フィルタ パス (垂直)。
        /// </summary>
        GaussianFilterPass bilateralFilterV;

        /// <summary>
        /// ブルーム抽出フィルタ。
        /// </summary>
        BloomExtractFilter bloomExtractFilter;

        /// <summary>
        /// ブルーム合成フィルタ。
        /// </summary>
        BloomCombineFilter bloomCombineFilter;

        /// <summary>
        /// 被写界深度合成フィルタ。
        /// </summary>
        DofCombineFilter dofCombineFilter;

        /// <summary>
        /// モノクローム フィルタ。
        /// </summary>
        MonochromeFilter monochromeFilter;

        /// <summary>
        /// 走査線フィルタ。
        /// </summary>
        ScanlineFilter scanlineFilter;

        /// <summary>
        /// エッジ強調フィルタ。
        /// </summary>
        EdgeFilter edgeFilter;

        /// <summary>
        /// ネガティブ フィルタ。
        /// </summary>
        NegativeFilter negativeFilter;

        /// <summary>
        /// 放射フィルタ。
        /// </summary>
        RadialFilter radialFilter;

        /// <summary>
        /// 法線エッジ検出フィルタ。
        /// </summary>
        NormalEdgeDetectFilter normalEdgeDetectFilter;

        /// <summary>
        /// 線形深度マップ エフェクト。
        /// </summary>
        LinearDepthMapEffect depthMapEffect;

        /// <summary>
        /// 法線マップ エフェクト。
        /// </summary>
        NormalMapEffect normalMapEffect;

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
        /// 選択されているポストプロセスの種類。
        /// </summary>
        PostprocessType postprocessType;

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

            textureDisplay = new TextureDisplay(this);
            Components.Add(textureDisplay);

            postprocessType = PostprocessType.None;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMapRenderTarget = Device.CreateRenderTarget();
            depthMapRenderTarget.Width = WindowWidth;
            depthMapRenderTarget.Height = WindowHeight;
            depthMapRenderTarget.Format = SurfaceFormat.Single;
            depthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            depthMapRenderTarget.Initialize();

            normalMapRenderTarget = Device.CreateRenderTarget();
            normalMapRenderTarget.Width = WindowWidth;
            normalMapRenderTarget.Height = WindowHeight;
            normalMapRenderTarget.Format = SurfaceFormat.NormalizedByte4;
            normalMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            postprocess = new Postprocess(Device.ImmediateContext);
            postprocess.Width = WindowWidth;
            postprocess.Height = WindowHeight;

            downFilter = new DownFilter(Device);
            upFilter = new UpFilter(Device);

            gaussianFilter = new GaussianFilter(Device);
            gaussianFilterH = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Horizon);
            gaussianFilterV = new GaussianFilterPass(gaussianFilter, GaussianFilterDirection.Vertical);

            bilateralFilter = new BilateralFilter(Device);
            bilateralFilterH = new GaussianFilterPass(bilateralFilter, GaussianFilterDirection.Horizon);
            bilateralFilterV = new GaussianFilterPass(bilateralFilter, GaussianFilterDirection.Vertical);

            bloomExtractFilter = new BloomExtractFilter(Device);
            bloomCombineFilter = new BloomCombineFilter(Device);

            dofCombineFilter = new DofCombineFilter(Device);

            monochromeFilter = new MonochromeFilter(Device);
            monochromeFilter.Enabled = false;

            scanlineFilter = new ScanlineFilter(Device);
            scanlineFilter.Enabled = false;
            scanlineFilter.Density = WindowHeight * MathHelper.PiOver2;

            edgeFilter = new EdgeFilter(Device);
            edgeFilter.Enabled = false;
            edgeFilter.NearClipDistance = camera.NearClipDistance;
            edgeFilter.FarClipDistance = camera.FarClipDistance;

            negativeFilter = new NegativeFilter(Device);
            negativeFilter.Enabled = false;

            radialFilter = new RadialFilter(Device);
            radialFilter.Enabled = false;

            normalEdgeDetectFilter = new NormalEdgeDetectFilter(Device);
            normalEdgeDetectFilter.Enabled = false;

            depthMapEffect = new LinearDepthMapEffect(Device);
            normalMapEffect = new NormalMapEffect(Device);

            basicEffect = new BasicEffect(Device);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

            cubeMesh = new CubeMesh(Device, 20);
            sphereMesh = new SphereMesh(Device, 20, 32);
            cylinderMesh = new CylinderMesh(Device, 80, 20, 32);
            squareMesh = new SquareMesh(Device, 400);
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // ポストプロセスの更新。
            UpdatePostprocess();

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            const float scale = 0.2f;
            textureDisplay.TextureWidth = (int) (WindowWidth * scale);
            textureDisplay.TextureHeight = (int) (WindowHeight * scale);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            // 念のため状態を初期状態へ。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            // 深度マップを描画。
            CreateDepthMap(context);

            // 法線マップを描画。
            CreateNormalMap(context);

            // 通常シーンを描画。
            CreateNormalSceneMap(context);

            // ポストプロセスを適用。
            finalSceneTexture = postprocess.Draw(normalSceneRenderTarget.GetShaderResourceView());

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap(context);

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateDepthMap(DeviceContext context)
        {
            context.SetRenderTarget(depthMapRenderTarget.GetRenderTargetView());
            //context.Clear(Vector4.One);
            context.Clear(new Vector4(float.MaxValue));

            DrawScene(context, depthMapEffect);

            context.SetRenderTarget(null);

            // フィルタへ設定。
            dofCombineFilter.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
            edgeFilter.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(depthMapRenderTarget.GetShaderResourceView());
        }

        void CreateNormalMap(DeviceContext context)
        {
            context.SetRenderTarget(normalMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            DrawScene(context, normalMapEffect);

            context.SetRenderTarget(null);

            // フィルタへ設定。
            normalEdgeDetectFilter.NormalMap = normalMapRenderTarget.GetShaderResourceView();
            edgeFilter.NormalMap = normalMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalMapRenderTarget.GetShaderResourceView());
        }

        void CreateNormalSceneMap(DeviceContext context)
        {
            context.SetRenderTarget(normalSceneRenderTarget.GetRenderTargetView());
            context.Clear(Color.CornflowerBlue);

            DrawScene(context, basicEffect);

            context.SetRenderTarget(null);

            // フィルタへ設定。
            bloomCombineFilter.BaseTexture = normalSceneRenderTarget.GetShaderResourceView();
            dofCombineFilter.BaseTexture = normalSceneRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalSceneRenderTarget.GetShaderResourceView());
        }

        void DrawPrimitiveMesh(DeviceContext context, PrimitiveMesh mesh, Matrix world, Vector3 color)
        {
            basicEffect.DiffuseColor = color;

            DrawPrimitiveMesh(context, mesh, world, color, basicEffect);
        }

        void DrawScene(DeviceContext context, IEffect effect)
        {
            var effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.View = camera.View;
                effectMatrices.Projection = camera.Projection;
            }

            DrawPrimitiveMesh(context, cubeMesh, Matrix.CreateTranslation(-40, 10, 40), new Vector3(0, 0, 0), effect);
            DrawPrimitiveMesh(context, cubeMesh, Matrix.CreateTranslation(-85, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(context, cubeMesh, Matrix.CreateTranslation(-60, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(context, cubeMesh, Matrix.CreateTranslation(-40, 10, 0), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(context, sphereMesh, Matrix.CreateTranslation(10, 10, -60), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(context, sphereMesh, Matrix.CreateTranslation(0, 10, -40), new Vector3(0, 1, 0), effect);
            for (float z = -180; z <= 180; z += 40)
            {
                DrawPrimitiveMesh(context, cylinderMesh, Matrix.CreateTranslation(-180, 40, z), new Vector3(0, 0, 1), effect);
            }
            DrawPrimitiveMesh(context, squareMesh, Matrix.Identity, new Vector3(0.5f), effect);
        }

        void DrawPrimitiveMesh(DeviceContext context, PrimitiveMesh mesh, Matrix world, Vector3 color, IEffect effect)
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
            mesh.Draw(context);
        }

        void CreateFinalSceneMap(DeviceContext context)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(finalSceneTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text =
                "Current postprocess: " + postprocessType + "\n" +
                "[F1] None [F2] Depth of Field [F3] Bloom [F4] Blur\n" +
                "[F5] Bilateral Filter\n" +
                "[1] Monochrome (" + monochromeFilter.Enabled + ")\n" +
                "[2] Scanline (" + scanlineFilter.Enabled + ")\n" +
                "[3] Edge (" + edgeFilter.Enabled + ")\n" +
                "[4] Negative Filter (" + negativeFilter.Enabled + ")\n" +
                "[5] Radial Blur (" + radialFilter.Enabled + ")\n" +
                "[6] Normal Edge Detect (" + normalEdgeDetectFilter.Enabled + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 280), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 280 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void UpdatePostprocess()
        {
            postprocess.Filters.Clear();

            switch (postprocessType)
            {
                case PostprocessType.DepthOfField:
                    SetupDepthOfField();
                    break;
                case PostprocessType.Bloom:
                    SetupBloom();
                    break;
                case PostprocessType.Blur:
                    SetupBlur();
                    break;
                case PostprocessType.BilateralFilter:
                    SetupBilateralFilter();
                    break;
                case PostprocessType.None:
                default:
                    SetupNone();
                    break;
            }
        }

        void SetupNone()
        {
            AddCommonPasses();
        }

        void SetupDepthOfField()
        {
            postprocess.Filters.Add(downFilter);
            postprocess.Filters.Add(gaussianFilterH);
            postprocess.Filters.Add(gaussianFilterV);
            postprocess.Filters.Add(upFilter);
            postprocess.Filters.Add(dofCombineFilter);

            AddCommonPasses();
        }

        void SetupBloom()
        {
            postprocess.Filters.Add(bloomExtractFilter);
            postprocess.Filters.Add(downFilter);
            postprocess.Filters.Add(gaussianFilterH);
            postprocess.Filters.Add(gaussianFilterV);
            postprocess.Filters.Add(upFilter);
            postprocess.Filters.Add(bloomCombineFilter);

            AddCommonPasses();
        }

        void SetupBlur()
        {
            postprocess.Filters.Add(downFilter);
            postprocess.Filters.Add(gaussianFilterH);
            postprocess.Filters.Add(gaussianFilterV);
            postprocess.Filters.Add(upFilter);

            AddCommonPasses();
        }

        void SetupBilateralFilter()
        {
            // ダウン/アップ フィルタは用いない (ぼかしが雑になるのみ)。

            const int iteration = 6;
            for (int i = 0; i < iteration; i++)
            {
                postprocess.Filters.Add(bilateralFilterH);
                postprocess.Filters.Add(bilateralFilterV);
            }

            AddCommonPasses();
        }

        void AddCommonPasses()
        {
            postprocess.Filters.Add(monochromeFilter);
            postprocess.Filters.Add(scanlineFilter);
            postprocess.Filters.Add(negativeFilter);
            postprocess.Filters.Add(edgeFilter);
            postprocess.Filters.Add(radialFilter);
            postprocess.Filters.Add(normalEdgeDetectFilter);
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyUp(Keys.F1) && lastKeyboardState.IsKeyDown(Keys.F1))
                postprocessType = PostprocessType.None;

            if (currentKeyboardState.IsKeyUp(Keys.F2) && lastKeyboardState.IsKeyDown(Keys.F2))
                postprocessType = PostprocessType.DepthOfField;

            if (currentKeyboardState.IsKeyUp(Keys.F3) && lastKeyboardState.IsKeyDown(Keys.F3))
                postprocessType = PostprocessType.Bloom;

            if (currentKeyboardState.IsKeyUp(Keys.F4) && lastKeyboardState.IsKeyDown(Keys.F4))
                postprocessType = PostprocessType.Blur;

            if (currentKeyboardState.IsKeyUp(Keys.F5) && lastKeyboardState.IsKeyDown(Keys.F5))
                postprocessType = PostprocessType.BilateralFilter;

            if (currentKeyboardState.IsKeyUp(Keys.D1) && lastKeyboardState.IsKeyDown(Keys.D1))
                monochromeFilter.Enabled = !monochromeFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D2) && lastKeyboardState.IsKeyDown(Keys.D2))
                scanlineFilter.Enabled = !scanlineFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D3) && lastKeyboardState.IsKeyDown(Keys.D3))
                edgeFilter.Enabled = !edgeFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D4) && lastKeyboardState.IsKeyDown(Keys.D4))
                negativeFilter.Enabled = !negativeFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D5) && lastKeyboardState.IsKeyDown(Keys.D5))
                radialFilter.Enabled = !radialFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D6) && lastKeyboardState.IsKeyDown(Keys.D6))
                normalEdgeDetectFilter.Enabled = !normalEdgeDetectFilter.Enabled;

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
