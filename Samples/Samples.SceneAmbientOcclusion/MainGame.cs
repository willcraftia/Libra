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

namespace Samples.SceneAmbientOcclusion
{
    public sealed class MainGame : Game
    {
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
        /// 環境光閉塞マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget ambientOcclusionMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ポストプロセス適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// ランダム法線マップ。
        /// </summary>
        Texture2D randomNormalMap;

        /// <summary>
        /// 環境光閉塞マップ シェーダ。
        /// </summary>
        AmbientOcclusionMap ambientOcclusionMap;

        /// <summary>
        /// 環境光閉塞マップ用ポストプロセス。
        /// </summary>
        Postprocess postprocessAO;

        /// <summary>
        /// 表示シーン用ポストプロセス。
        /// </summary>
        Postprocess postprocess;

        /// <summary>
        /// ダウン フィルタ パス。
        /// </summary>
        DownFilter downFilter;

        /// <summary>
        /// アップ フィルタ パス。
        /// </summary>
        UpFilter upFilter;

        /// <summary>
        /// 環境光閉塞ブラー フィルタ。
        /// </summary>
        AmbientOcclusionBlur ambientOcclusionBlur;

        /// <summary>
        /// 環境光閉塞ブラー フィルタ 水平パス。
        /// </summary>
        GaussianFilterPass ambientOcclusionBlurH;

        /// <summary>
        /// 環境光閉塞ブラー フィルタ 垂直パス。
        /// </summary>
        GaussianFilterPass ambientOcclusionBlurV;

        /// <summary>
        /// 環境光閉塞マップ合成フィルタ。
        /// </summary>
        AmbientOcclusionCombine ambientOcclusionCombine;

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
        /// HUD テキストを表示するか否かを示す値。
        /// </summary>
        bool hudVisible;

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

            hudVisible = true;
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

            ambientOcclusionMapRenderTarget = Device.CreateRenderTarget();
            ambientOcclusionMapRenderTarget.Width = WindowWidth / 2;
            ambientOcclusionMapRenderTarget.Height = WindowHeight / 2;
            ambientOcclusionMapRenderTarget.Format = SurfaceFormat.Single;
            ambientOcclusionMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            ambientOcclusionMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            randomNormalMap = RandomNormalMap.CreateAsR8G8B8A8SNorm(Device.ImmediateContext, new Random(0), 64, 64);

            ambientOcclusionMap = new AmbientOcclusionMap(Device);
            ambientOcclusionMap.Width = WindowWidth / 2;
            ambientOcclusionMap.Height = WindowHeight / 2;
            ambientOcclusionMap.FarClipDistance = camera.FarClipDistance;
            ambientOcclusionMap.RandomNormalMap = randomNormalMap.GetShaderResourceView();

            postprocessAO = new Postprocess(Device.ImmediateContext);
            postprocessAO.Width = ambientOcclusionMap.Width;
            postprocessAO.Height = ambientOcclusionMap.Height;
            postprocessAO.Format = SurfaceFormat.Single;

            postprocess = new Postprocess(Device.ImmediateContext);
            postprocess.Width = WindowWidth;
            postprocess.Height = WindowHeight;

            downFilter = new DownFilter(Device);
            upFilter = new UpFilter(Device);

            ambientOcclusionBlur = new AmbientOcclusionBlur(Device);
            ambientOcclusionBlurH = new GaussianFilterPass(ambientOcclusionBlur, GaussianFilterDirection.Horizon);
            ambientOcclusionBlurV = new GaussianFilterPass(ambientOcclusionBlur, GaussianFilterDirection.Vertical);

            ambientOcclusionCombine = new AmbientOcclusionCombine(Device);

            const int blurIteration = 4;
            for (int i = 0; i < blurIteration; i++)
            {
                postprocessAO.Filters.Add(ambientOcclusionBlurH);
                postprocessAO.Filters.Add(ambientOcclusionBlurV);
            }

            postprocess.Filters.Add(ambientOcclusionCombine);

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

            // 環境光閉塞マップを描画。
            CreateAmbientOcclusionMap(context);

            // 通常シーンを描画。
            CreateNormalSceneMap(context);

            // 環境光閉塞マップへポストプロセスを適用。
            var finalAmbientOcclusionMap = postprocessAO.Draw(ambientOcclusionMapRenderTarget.GetShaderResourceView());
            textureDisplay.Textures.Add(finalAmbientOcclusionMap);

            // 環境光閉塞マップ合成フィルタへ設定。
            ambientOcclusionCombine.AmbientOcclusionMap = finalAmbientOcclusionMap;

            // 通常シーンへポストプロセスを適用。
            finalSceneTexture = postprocess.Draw(normalSceneRenderTarget.GetShaderResourceView());

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap(context);

            // HUD のテキストを描画。
            if (hudVisible)
                DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateDepthMap(DeviceContext context)
        {
            context.SetRenderTarget(depthMapRenderTarget.GetRenderTargetView());
            context.Clear(new Vector4(float.MaxValue));

            DrawScene(context, depthMapEffect);

            context.SetRenderTarget(null);

            // 環境光閉塞マップ シェーダへ設定。
            ambientOcclusionMap.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
            // 環境光閉塞マップ ブラー フィルタへ設定。
            ambientOcclusionBlur.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(depthMapRenderTarget.GetShaderResourceView());
        }

        void CreateNormalMap(DeviceContext context)
        {
            context.SetRenderTarget(normalMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            DrawScene(context, normalMapEffect);

            context.SetRenderTarget(null);

            // 環境光閉塞マップ シェーダへ設定。
            ambientOcclusionMap.NormalMap = normalMapRenderTarget.GetShaderResourceView();
            // 環境光閉塞マップ ブラー フィルタへ設定。
            ambientOcclusionBlur.NormalMap = normalMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalMapRenderTarget.GetShaderResourceView());
        }

        void CreateAmbientOcclusionMap(DeviceContext context)
        {
            context.SetRenderTarget(ambientOcclusionMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, ambientOcclusionMap.Apply);
            spriteBatch.Draw(depthMapRenderTarget.GetShaderResourceView(), ambientOcclusionMapRenderTarget.Bounds, Color.White);
            spriteBatch.End();

            context.SetRenderTarget(null);

            // 中間マップ表示。
            textureDisplay.Textures.Add(ambientOcclusionMapRenderTarget.GetShaderResourceView());
        }

        void CreateNormalSceneMap(DeviceContext context)
        {
            context.SetRenderTarget(normalSceneRenderTarget.GetRenderTargetView());
            context.Clear(Color.CornflowerBlue);

            DrawScene(context, basicEffect);

            context.SetRenderTarget(null);

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
            string text;
            if (currentKeyboardState.IsKeyUp(Keys.ControlKey))
            {
                text =
                    "AO Settings ([Control] Blur Settings)\n" +
                    "[T/G] Strength (" + ambientOcclusionMap.Strength.ToString("F1") + ")\n" +
                    "[Y/H] Attenuation (" + ambientOcclusionMap.Attenuation.ToString("F2") + ")\n" +
                    "[U/J] Radius (" + ambientOcclusionMap.Radius.ToString("F1") + ")\n" +
                    "[I/K] SampleCount (" + ambientOcclusionMap.SampleCount + ")";
            }
            else
            {
                text =
                    "Blur Settings\n" +
                    "[T/G] Radius (" + ambientOcclusionBlur.Radius + ")\n" +
                    "[Y/H] Space Sigma (" + ambientOcclusionBlur.SpaceSigma.ToString("F1") + ")\n" +
                    "[U/J] Depth Sigma (" + ambientOcclusionBlur.DepthSigma.ToString("F1") + ")\n" +
                    "[I/K] Normal Sigma (" + ambientOcclusionBlur.NormalSigma.ToString("F1") + ")";
            }

            string basicText =
                "[F1] HUD on/off\n" +
                "[F2] Inter-maps on/off";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 340), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 340 - 1), Color.Yellow);

            spriteBatch.DrawString(spriteFont, basicText, new Vector2(449, 340), Color.Black);
            spriteBatch.DrawString(spriteFont, basicText, new Vector2(448, 340 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyUp(Keys.ControlKey))
            {
                if (currentKeyboardState.IsKeyDown(Keys.T))
                    ambientOcclusionMap.Strength += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.G))
                    ambientOcclusionMap.Strength = Math.Max(0.0f, ambientOcclusionMap.Strength - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.Y))
                    ambientOcclusionMap.Attenuation += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.H))
                    ambientOcclusionMap.Attenuation = Math.Max(0.0f, ambientOcclusionMap.Attenuation - 0.01f);

                if (currentKeyboardState.IsKeyDown(Keys.U))
                    ambientOcclusionMap.Radius += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.J))
                    ambientOcclusionMap.Radius = Math.Max(0.1f, ambientOcclusionMap.Radius - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.I))
                    ambientOcclusionMap.SampleCount = Math.Min(128, ambientOcclusionMap.SampleCount + 1);
                if (currentKeyboardState.IsKeyDown(Keys.K))
                    ambientOcclusionMap.SampleCount = Math.Max(1, ambientOcclusionMap.SampleCount - 1);
            }
            else
            {
                if (currentKeyboardState.IsKeyDown(Keys.T))
                    ambientOcclusionBlur.Radius = Math.Min(7, ambientOcclusionBlur.Radius + 1);
                if (currentKeyboardState.IsKeyDown(Keys.G))
                    ambientOcclusionBlur.Radius = Math.Max(1, ambientOcclusionBlur.Radius - 1);

                if (currentKeyboardState.IsKeyDown(Keys.Y))
                    ambientOcclusionBlur.SpaceSigma += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.H))
                    ambientOcclusionBlur.SpaceSigma = Math.Max(0.1f, ambientOcclusionBlur.SpaceSigma - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.U))
                    ambientOcclusionBlur.DepthSigma += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.J))
                    ambientOcclusionBlur.DepthSigma = Math.Max(0.1f, ambientOcclusionBlur.DepthSigma - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.I))
                    ambientOcclusionBlur.NormalSigma += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.K))
                    ambientOcclusionBlur.NormalSigma = Math.Max(0.1f, ambientOcclusionBlur.NormalSigma - 0.1f);
            }

            if (currentKeyboardState.IsKeyUp(Keys.F1) && lastKeyboardState.IsKeyDown(Keys.F1))
                hudVisible = !hudVisible;

            if (currentKeyboardState.IsKeyUp(Keys.F2) && lastKeyboardState.IsKeyDown(Keys.F2))
                textureDisplay.Visible = !textureDisplay.Visible;

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
