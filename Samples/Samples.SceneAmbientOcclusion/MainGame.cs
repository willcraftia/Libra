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
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ポストプロセス適用後の最終シーン。
        /// </summary>
        ShaderResourceView finalSceneTexture;

        /// <summary>
        /// 環境光閉塞マップ。
        /// </summary>
        SSAOMap ssaoMap;

        /// <summary>
        /// 表示シーン用ポストプロセス。
        /// </summary>
        Postprocess postprocessScene;

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
        /// 最終閉塞マップ可視化フィルタ。
        /// </summary>
        OcclusionMapColorFilter finalOcclusionMapColorFilter;

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
        /// 輪環体メッシュ。
        /// </summary>
        TorusMesh torusMesh;

        /// <summary>
        /// ティーポット メッシュ。
        /// </summary>
        TeapotMesh teapotMesh;

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

            normalMapRenderTarget = Device.CreateRenderTarget();
            normalMapRenderTarget.Width = WindowWidth;
            normalMapRenderTarget.Height = WindowHeight;
            normalMapRenderTarget.Format = SurfaceFormat.NormalizedByte4;
            normalMapRenderTarget.DepthStencilEnabled = true;
            normalMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.PreferredMultisampleCount = Device.BackBufferMultisampleCount;
            normalSceneRenderTarget.DepthStencilEnabled = true;
            normalSceneRenderTarget.Initialize();

            postprocessScene = new Postprocess(DeviceContext);
            postprocessScene.Width = normalSceneRenderTarget.Width;
            postprocessScene.Height = normalSceneRenderTarget.Height;
            postprocessScene.Format = normalSceneRenderTarget.Format;

            ssaoMap = new SSAOMap(DeviceContext);
            ssaoMap.RenderTargetWidth = WindowWidth;
            ssaoMap.RenderTargetHeight = WindowHeight;

            occlusionCombineFilter = new OcclusionCombineFilter(DeviceContext);
            linearDepthMapColorFilter = new LinearDepthMapColorFilter(DeviceContext);
            linearDepthMapColorFilter.NearClipDistance = camera.NearClipDistance;
            linearDepthMapColorFilter.FarClipDistance = camera.FarClipDistance;
            linearDepthMapColorFilter.Enabled = false;
            occlusionMapColorFilter = new OcclusionMapColorFilter(DeviceContext);
            occlusionMapColorFilter.Enabled = false;
            finalOcclusionMapColorFilter = new OcclusionMapColorFilter(DeviceContext);
            finalOcclusionMapColorFilter.Enabled = false;

            postprocessScene.Filters.Add(occlusionCombineFilter);
            postprocessScene.Filters.Add(linearDepthMapColorFilter);
            postprocessScene.Filters.Add(occlusionMapColorFilter);
            postprocessScene.Filters.Add(finalOcclusionMapColorFilter);

            depthMapEffect = new LinearDepthMapEffect(DeviceContext);
            normalMapEffect = new NormalMapEffect(DeviceContext);

            basicEffect = new BasicEffect(DeviceContext);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

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

            // 法線マップを描画。
            CreateNormalMap();

            // 環境光閉塞マップを描画。
            CreateSSAOMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // 通常シーンへポストプロセスを適用。
            ApplyPostprocessScene();

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }
    
        void CreateDepthMap()
        {
            DeviceContext.SetRenderTarget(depthMapRenderTarget);
            DeviceContext.Clear(new Vector4(float.MaxValue));

            DrawScene(depthMapEffect);

            DeviceContext.SetRenderTarget(null);

            ssaoMap.LinearDepthMap = depthMapRenderTarget;
            linearDepthMapColorFilter.LinearDepthMap = depthMapRenderTarget;
        }

        void CreateNormalMap()
        {
            DeviceContext.SetRenderTarget(normalMapRenderTarget);
            DeviceContext.Clear(Vector4.One);

            DrawScene(normalMapEffect);

            DeviceContext.SetRenderTarget(null);

            ssaoMap.NormalMap = normalMapRenderTarget;

            textureDisplay.Textures.Add(normalMapRenderTarget);
        }

        void CreateSSAOMap()
        {
            ssaoMap.Projection = camera.Projection;
            ssaoMap.Draw();

            occlusionMapColorFilter.OcclusionMap = ssaoMap.BaseTexture;
            occlusionCombineFilter.OcclusionMap = ssaoMap.FinalTexture;
            finalOcclusionMapColorFilter.OcclusionMap = ssaoMap.FinalTexture;

            textureDisplay.Textures.Add(ssaoMap.BaseTexture);
            textureDisplay.Textures.Add(ssaoMap.FinalTexture);
        }

        void CreateNormalSceneMap()
        {
            DeviceContext.SetRenderTarget(normalSceneRenderTarget);
            DeviceContext.Clear(Color.CornflowerBlue);

            DrawScene(basicEffect);

            DeviceContext.SetRenderTarget(null);

            textureDisplay.Textures.Add(normalSceneRenderTarget);
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

            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-85, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-60, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-40, 10, 0), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(10, 10, -60), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(0, 10, -40), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(torusMesh, Matrix.CreateTranslation(40, 5, -40), new Vector3(1, 1, 0), effect);
            DrawPrimitiveMesh(teapotMesh, Matrix.CreateTranslation(100, 10, -100), new Vector3(0, 1, 1), effect);
            for (float z = -180; z <= 180; z += 40)
            {
                DrawPrimitiveMesh(cylinderMesh, Matrix.CreateTranslation(-180, 40, z), new Vector3(0, 0, 1), effect);
            }
            DrawPrimitiveMesh(squareMesh, Matrix.Identity, new Vector3(0.5f), effect);
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

        void ApplyPostprocessScene()
        {
            finalSceneTexture = postprocessScene.Draw(normalSceneRenderTarget);
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
                    "[T/G] Radius (" + ssaoMap.BlurRadius + ")\n" +
                    "[Y/H] Space Sigma (" + ssaoMap.BlurSpaceSigma.ToString("F1") + ")\n" +
                    "[U/J] Depth Sigma (" + ssaoMap.BlurDepthSigma.ToString("F2") + ")\n" +
                    "[I/K] Normal Sigma (" + ssaoMap.BlurNormalSigma.ToString("F2") + ")";
            }

            string basicText =
                "[F1] HUD on/off\n" +
                "[F2] Inter-maps on/off\n" +
                "[F3] Combine Occlusion Map (" + occlusionCombineFilter.Enabled + ")\n" +
                "[F4] Depth Map " + (linearDepthMapColorFilter.Enabled ? "(Current)" : "") + "\n" +
                "[F5] Occlusion Map " + (occlusionMapColorFilter.Enabled ? "(Current)" : "") + "\n" +
                "[F6] Final Occlusion Map " + (finalOcclusionMapColorFilter.Enabled ? "(Current)" : "") + "\n" +
                "[PageUp/Down] Blur Iteration (" + ssaoMap.BlurIteration + ")";

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
                    ssaoMap.BlurRadius = Math.Min(7, ssaoMap.BlurRadius + 1);
                if (currentKeyboardState.IsKeyDown(Keys.G))
                    ssaoMap.BlurRadius = Math.Max(1, ssaoMap.BlurRadius - 1);

                if (currentKeyboardState.IsKeyDown(Keys.Y))
                    ssaoMap.BlurSpaceSigma += 0.1f;
                if (currentKeyboardState.IsKeyDown(Keys.H))
                    ssaoMap.BlurSpaceSigma = Math.Max(0.1f, ssaoMap.BlurSpaceSigma - 0.1f);

                if (currentKeyboardState.IsKeyDown(Keys.U))
                    ssaoMap.BlurDepthSigma += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.J))
                    ssaoMap.BlurDepthSigma = Math.Max(0.1f, ssaoMap.BlurDepthSigma - 0.01f);

                if (currentKeyboardState.IsKeyDown(Keys.I))
                    ssaoMap.BlurNormalSigma += 0.01f;
                if (currentKeyboardState.IsKeyDown(Keys.K))
                    ssaoMap.BlurNormalSigma = Math.Max(0.1f, ssaoMap.BlurNormalSigma - 0.01f);
            }

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
                finalOcclusionMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F5) && lastKeyboardState.IsKeyDown(Keys.F5))
            {
                occlusionMapColorFilter.Enabled = !occlusionMapColorFilter.Enabled;
                linearDepthMapColorFilter.Enabled = false;
                finalOcclusionMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F6) && lastKeyboardState.IsKeyDown(Keys.F6))
            {
                finalOcclusionMapColorFilter.Enabled = !finalOcclusionMapColorFilter.Enabled;
                linearDepthMapColorFilter.Enabled = false;
                occlusionMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.PageUp) && lastKeyboardState.IsKeyDown(Keys.PageUp))
                ssaoMap.BlurIteration = Math.Min(10, ssaoMap.BlurIteration + 1);

            if (currentKeyboardState.IsKeyUp(Keys.PageDown) && lastKeyboardState.IsKeyDown(Keys.PageDown))
                ssaoMap.BlurIteration = Math.Max(0, ssaoMap.BlurIteration - 1);

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
