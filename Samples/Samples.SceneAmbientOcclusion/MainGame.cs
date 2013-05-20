﻿#region Using

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
        /// 法線マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalMapRenderTarget;

        /// <summary>
        /// 環境光閉塞マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget ssaoMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ポストプロセス適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// 環境光閉塞マップ シェーダ。
        /// </summary>
        SSAOMap ssaoMap;

        /// <summary>
        /// 環境光閉塞マップ用ポストプロセス。
        /// </summary>
        Postprocess postprocessSSAOMap;

        /// <summary>
        /// 表示シーン用ポストプロセス。
        /// </summary>
        Postprocess postprocessScene;

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
        SSAOBlur ssaoBlur;

        /// <summary>
        /// 環境光閉塞ブラー フィルタ 水平パス。
        /// </summary>
        GaussianFilterPass ssaoBlurH;

        /// <summary>
        /// 環境光閉塞ブラー フィルタ 垂直パス。
        /// </summary>
        GaussianFilterPass ssaoBlurV;

        /// <summary>
        /// 環境光閉塞マップ合成フィルタ。
        /// </summary>
        SSAOCombine ssaoCombine;

        /// <summary>
        /// 線形深度マップ可視化フィルタ。
        /// </summary>
        LinearDepthMapVisualize linearDepthMapVisualize;

        /// <summary>
        /// 環境光閉塞マップ可視化フィルタ。
        /// </summary>
        SSAOMapVisualize ssaoMapVisualize;

        /// <summary>
        /// 最終環境光閉塞マップ可視化フィルタ。
        /// </summary>
        SSAOMapVisualize finalSsaoMapVisualize;

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
        /// ブラー適用回数。
        /// </summary>
        int blurIteration = 3;

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

            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);

            //IsFixedTimeStep = false;

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

            ssaoMapRenderTarget = Device.CreateRenderTarget();
            ssaoMapRenderTarget.Width = WindowWidth / 1;
            ssaoMapRenderTarget.Height = WindowHeight / 1;
            ssaoMapRenderTarget.Format = SurfaceFormat.Single;
            ssaoMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            ssaoMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            ssaoMap = new SSAOMap(Device);
            ssaoMap.Projection = camera.Projection;

            postprocessSSAOMap = new Postprocess(Device.ImmediateContext);
            postprocessSSAOMap.Width = ssaoMapRenderTarget.Width;
            postprocessSSAOMap.Height = ssaoMapRenderTarget.Height;
            postprocessSSAOMap.Format = SurfaceFormat.Single;

            postprocessScene = new Postprocess(Device.ImmediateContext);
            postprocessScene.Width = WindowWidth;
            postprocessScene.Height = WindowHeight;

            downFilter = new DownFilter(Device);
            upFilter = new UpFilter(Device);

            ssaoBlur = new SSAOBlur(Device);
            ssaoBlurH = new GaussianFilterPass(ssaoBlur, GaussianFilterDirection.Horizon);
            ssaoBlurV = new GaussianFilterPass(ssaoBlur, GaussianFilterDirection.Vertical);

            const int blurIteration = 3;
            for (int i = 0; i < blurIteration; i++)
            {
                postprocessSSAOMap.Filters.Add(ssaoBlurH);
                postprocessSSAOMap.Filters.Add(ssaoBlurV);
            }

            ssaoCombine = new SSAOCombine(Device);
            linearDepthMapVisualize = new LinearDepthMapVisualize(Device);
            linearDepthMapVisualize.NearClipDistance = camera.NearClipDistance;
            linearDepthMapVisualize.FarClipDistance = camera.FarClipDistance;
            linearDepthMapVisualize.Enabled = false;
            ssaoMapVisualize = new SSAOMapVisualize(Device);
            ssaoMapVisualize.Enabled = false;
            finalSsaoMapVisualize = new SSAOMapVisualize(Device);
            finalSsaoMapVisualize.Enabled = false;

            postprocessScene.Filters.Add(ssaoCombine);
            postprocessScene.Filters.Add(linearDepthMapVisualize);
            postprocessScene.Filters.Add(ssaoMapVisualize);
            postprocessScene.Filters.Add(finalSsaoMapVisualize);

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

            titleBuilder.Length = 0;
            titleBuilder.Append("FPS: ");
            titleBuilder.AppendNumber(frameRateMeasure.FrameRate);
            Window.Title = titleBuilder.ToString();

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
            CreateSSAOMap(context);

            // 通常シーンを描画。
            CreateNormalSceneMap(context);

            // 環境光閉塞マップへポストプロセスを適用。
            DoPostprocessSSAOMap();

            // 通常シーンへポストプロセスを適用。
            DoPostprocessScene();

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
            ssaoMap.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
            // 環境光閉塞マップ ブラー フィルタへ設定。
            ssaoBlur.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
            // 可視化フィルタへ設定。
            linearDepthMapVisualize.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
        }

        void CreateNormalMap(DeviceContext context)
        {
            context.SetRenderTarget(normalMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            DrawScene(context, normalMapEffect);

            context.SetRenderTarget(null);

            // 環境光閉塞マップ シェーダへ設定。
            ssaoMap.NormalMap = normalMapRenderTarget.GetShaderResourceView();
            // 環境光閉塞マップ ブラー フィルタへ設定。
            ssaoBlur.NormalMap = normalMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalMapRenderTarget.GetShaderResourceView());
        }

        void CreateSSAOMap(DeviceContext context)
        {
            context.SetRenderTarget(ssaoMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            ssaoMap.Draw(context);

            context.SetRenderTarget(null);

            // 環境光閉塞マップ可視化フィルタへ設定。
            ssaoMapVisualize.SSAOMap = ssaoMapRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(ssaoMapRenderTarget.GetShaderResourceView());
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

        void DoPostprocessSSAOMap()
        {
            postprocessSSAOMap.Filters.Clear();
            for (int i = 0; i < blurIteration; i++)
            {
                postprocessSSAOMap.Filters.Add(ssaoBlurH);
                postprocessSSAOMap.Filters.Add(ssaoBlurV);
            }

            var finalSSAOMap = postprocessSSAOMap.Draw(ssaoMapRenderTarget.GetShaderResourceView());

            // 環境光閉塞マップ合成フィルタへ設定。
            ssaoCombine.SSAOMap = finalSSAOMap;
            // 最終環境光閉塞マップ可視化フィルタへ設定。
            finalSsaoMapVisualize.SSAOMap = finalSSAOMap;

            textureDisplay.Textures.Add(finalSSAOMap);
        }

        void DoPostprocessScene()
        {
            finalSceneTexture = postprocessScene.Draw(normalSceneRenderTarget.GetShaderResourceView());
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
                    "[T/G] Strength (" + ssaoMap.Strength.ToString("F1") + ")\n" +
                    "[Y/H] Attenuation (" + ssaoMap.Attenuation.ToString("F2") + ")\n" +
                    "[U/J] Radius (" + ssaoMap.Radius.ToString("F1") + ")\n" +
                    "[I/K] SampleCount (" + ssaoMap.SampleCount + ")";
            }
            else
            {
                text =
                    "Blur Settings\n" +
                    "[T/G] Radius (" + ssaoBlur.Radius + ")\n" +
                    "[Y/H] Space Sigma (" + ssaoBlur.SpaceSigma.ToString("F1") + ")\n" +
                    "[U/J] Depth Sigma (" + ssaoBlur.DepthSigma.ToString("F2") + ")\n" +
                    "[I/K] Normal Sigma (" + ssaoBlur.NormalSigma.ToString("F2") + ")";
            }

            string basicText =
                "[F1] HUD on/off\n" +
                "[F2] Inter-maps on/off\n" +
                "[F3] SSAO on/off\n" +
                "[F4] Show/Hide Depth Map (" + linearDepthMapVisualize.Enabled + ")\n" +
                "[F5] Show/Hide SSAO Map (" + linearDepthMapVisualize.Enabled + ")\n" +
                "[F6] Show/Hide Final SSAO Map (" + linearDepthMapVisualize.Enabled + ")\n" +
                "[PageUp/Down] Blur Iteration (" + blurIteration + ")";

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
                    ssaoMap.Strength += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.G))
                    ssaoMap.Strength = Math.Max(0.0f, ssaoMap.Strength - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.Y))
                    ssaoMap.Attenuation += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.H))
                    ssaoMap.Attenuation = Math.Max(0.0f, ssaoMap.Attenuation - 0.01f);

                if (currentKeyboardState.IsKeyDown(Keys.U))
                    ssaoMap.Radius += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.J))
                    ssaoMap.Radius = Math.Max(0.1f, ssaoMap.Radius - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.I))
                    ssaoMap.SampleCount = Math.Min(128, ssaoMap.SampleCount + 1);
                if (currentKeyboardState.IsKeyDown(Keys.K))
                    ssaoMap.SampleCount = Math.Max(1, ssaoMap.SampleCount - 1);
            }
            else
            {
                if (currentKeyboardState.IsKeyDown(Keys.T))
                    ssaoBlur.Radius = Math.Min(7, ssaoBlur.Radius + 1);
                if (currentKeyboardState.IsKeyDown(Keys.G))
                    ssaoBlur.Radius = Math.Max(1, ssaoBlur.Radius - 1);

                if (currentKeyboardState.IsKeyDown(Keys.Y))
                    ssaoBlur.SpaceSigma += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.H))
                    ssaoBlur.SpaceSigma = Math.Max(0.1f, ssaoBlur.SpaceSigma - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.U))
                    ssaoBlur.DepthSigma += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.J))
                    ssaoBlur.DepthSigma = Math.Max(0.1f, ssaoBlur.DepthSigma - 0.01f);

                if (currentKeyboardState.IsKeyDown(Keys.I))
                    ssaoBlur.NormalSigma += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.K))
                    ssaoBlur.NormalSigma = Math.Max(0.1f, ssaoBlur.NormalSigma - 0.01f);
            }

            if (currentKeyboardState.IsKeyUp(Keys.F1) && lastKeyboardState.IsKeyDown(Keys.F1))
                hudVisible = !hudVisible;

            if (currentKeyboardState.IsKeyUp(Keys.F2) && lastKeyboardState.IsKeyDown(Keys.F2))
                textureDisplay.Visible = !textureDisplay.Visible;

            if (currentKeyboardState.IsKeyUp(Keys.F3) && lastKeyboardState.IsKeyDown(Keys.F3))
                ssaoCombine.Enabled = !ssaoCombine.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.F4) && lastKeyboardState.IsKeyDown(Keys.F4))
            {
                linearDepthMapVisualize.Enabled = !linearDepthMapVisualize.Enabled;
                ssaoMapVisualize.Enabled = false;
                finalSsaoMapVisualize.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F5) && lastKeyboardState.IsKeyDown(Keys.F5))
            {
                ssaoMapVisualize.Enabled = !ssaoMapVisualize.Enabled;
                linearDepthMapVisualize.Enabled = false;
                finalSsaoMapVisualize.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F6) && lastKeyboardState.IsKeyDown(Keys.F6))
            {
                finalSsaoMapVisualize.Enabled = !finalSsaoMapVisualize.Enabled;
                linearDepthMapVisualize.Enabled = false;
                ssaoMapVisualize.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.PageUp) && lastKeyboardState.IsKeyDown(Keys.PageUp))
                blurIteration = Math.Min(10, blurIteration + 1);

            if (currentKeyboardState.IsKeyUp(Keys.PageDown) && lastKeyboardState.IsKeyDown(Keys.PageDown))
                blurIteration = Math.Max(0, blurIteration - 1);

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
