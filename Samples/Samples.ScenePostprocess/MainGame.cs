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
        /// 深度法線マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget depthNormalRenderTarget;

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
        /// モノクローム パス。
        /// </summary>
        Monochrome monochrome;

        /// <summary>
        /// 走査線パス。
        /// </summary>
        Scanline scanline;

        /// <summary>
        /// エッジ強調パス。
        /// </summary>
        Edge edge;

        /// <summary>
        /// ネガティブ フィルタ パス。
        /// </summary>
        NegativeFilter negativeFilter;

        /// <summary>
        /// ガウシアン ブラー シェーダ。
        /// </summary>
        GaussianBlurCore gaussianBlurCore;

        /// <summary>
        /// ガウシアン ブラー パス (水平)。
        /// </summary>
        GaussianBlur gaussianBlurH;

        /// <summary>
        /// ガウシアン ブラー パス (垂直)。
        /// </summary>
        GaussianBlur gaussianBlurV;

        /// <summary>
        /// 放射ブラー パス。
        /// </summary>
        RadialBlur radialBlur;

        /// <summary>
        /// 深度法線マップ エフェクト。
        /// </summary>
        DepthNormalMapEffect depthNormalMapEffect;

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
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthNormalRenderTarget = Device.CreateRenderTarget();
            depthNormalRenderTarget.Width = WindowWidth;
            depthNormalRenderTarget.Height = WindowHeight;
            depthNormalRenderTarget.Format = SurfaceFormat.Vector4;
            depthNormalRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            depthNormalRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            postprocess = new Postprocess(Device.ImmediateContext);
            postprocess.Width = WindowWidth;
            postprocess.Height = WindowHeight;

            monochrome = new Monochrome(Device);
            monochrome.Enabled = false;
            postprocess.Passes.Add(monochrome);

            scanline = new Scanline(Device);
            scanline.Enabled = false;
            scanline.Density = WindowHeight * MathHelper.PiOver2;
            postprocess.Passes.Add(scanline);

            edge = new Edge(Device);
            edge.Enabled = false;
            postprocess.Passes.Add(edge);

            negativeFilter = new NegativeFilter(Device);
            negativeFilter.Enabled = false;
            postprocess.Passes.Add(negativeFilter);

            gaussianBlurCore = new GaussianBlurCore(Device);
            gaussianBlurH = new GaussianBlur(gaussianBlurCore, GaussianBlurPass.Horizon);
            gaussianBlurV = new GaussianBlur(gaussianBlurCore, GaussianBlurPass.Vertical);
            gaussianBlurH.Enabled = false;
            gaussianBlurV.Enabled = false;
            postprocess.Passes.Add(gaussianBlurH);
            postprocess.Passes.Add(gaussianBlurV);

            radialBlur = new RadialBlur(Device);
            radialBlur.Enabled = false;
            postprocess.Passes.Add(radialBlur);

            depthNormalMapEffect = new DepthNormalMapEffect(Device);

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

            // 深度法線マップを描画。
            CreateDepthNormalMap(context);

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

        void CreateDepthNormalMap(DeviceContext context)
        {
            if (!edge.Enabled)
                return;

            context.SetRenderTarget(depthNormalRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            DrawScene(context, depthNormalMapEffect);

            context.SetRenderTarget(null);

            // エッジ強調エフェクトへ深度法線マップを設定。
            edge.DepthNormalMap = depthNormalRenderTarget.GetShaderResourceView();

            // 中間マップ表示。
            textureDisplay.Textures.Add(depthNormalRenderTarget.GetShaderResourceView());
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

            DrawPrimitiveMesh(context, cubeMesh, Matrix.CreateTranslation(-40, 10, 0), new Vector3(1, 0, 0), effect);
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
                "1: Monochrome (" + monochrome.Enabled + ")\n" +
                "2: Scanline (" + scanline.Enabled + ")\n" +
                "3: Edge (" + edge.Enabled + ")\n" +
                "4: Negative filter (" + negativeFilter.Enabled + ")\n" +
                "5: Gaussian blur (" + gaussianBlurH.Enabled + ")\n" +
                "6: Radial blur (" + radialBlur.Enabled + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 340), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 340 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyUp(Keys.D1) && lastKeyboardState.IsKeyDown(Keys.D1))
                monochrome.Enabled = !monochrome.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D2) && lastKeyboardState.IsKeyDown(Keys.D2))
                scanline.Enabled = !scanline.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D3) && lastKeyboardState.IsKeyDown(Keys.D3))
                edge.Enabled = !edge.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D4) && lastKeyboardState.IsKeyDown(Keys.D4))
                negativeFilter.Enabled = !negativeFilter.Enabled;

            if (currentKeyboardState.IsKeyUp(Keys.D5) && lastKeyboardState.IsKeyDown(Keys.D5))
            {
                gaussianBlurH.Enabled = !gaussianBlurH.Enabled;
                gaussianBlurV.Enabled = !gaussianBlurV.Enabled;
            }

            if (currentKeyboardState.IsKeyUp(Keys.D6) && lastKeyboardState.IsKeyDown(Keys.D6))
                radialBlur.Enabled = !radialBlur.Enabled;

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
