#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.SceneDepthOfField
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
        /// ブラー済みシーンのスケール。
        /// </summary>
        float mapScale = 0.5f;

        /// <summary>
        /// 深度マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget depthMapRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ブラー済みシーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget bluredSceneRenderTarget;

        /// <summary>
        /// 線形深度マップ エフェクト。
        /// </summary>
        LinearDepthMapEffect depthMapEffect;

        /// <summary>
        /// シーンに適用するブラー。
        /// </summary>
        GaussianBlurSuite gaussianBlur;

        /// <summary>
        /// FullScreenQuad。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// 被写界深度合成ポストプロセス。
        /// </summary>
        DofCombine dofCombine;

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
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMapRenderTarget = Device.CreateRenderTarget();
            depthMapRenderTarget.Width = WindowWidth;
            depthMapRenderTarget.Height = WindowWidth;
            depthMapRenderTarget.Format = SurfaceFormat.Single;
            depthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            depthMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            bluredSceneRenderTarget = Device.CreateRenderTarget();
            bluredSceneRenderTarget.Width = (int) (WindowWidth * mapScale);
            bluredSceneRenderTarget.Height = (int) (WindowWidth * mapScale);
            bluredSceneRenderTarget.Initialize();

            depthMapEffect = new LinearDepthMapEffect(Device);

            gaussianBlur = new GaussianBlurSuite(
                Device, bluredSceneRenderTarget.Width, bluredSceneRenderTarget.Height, bluredSceneRenderTarget.Format);

            dofCombine = new DofCombine(Device);

            fullScreenQuad = new FullScreenQuad(Device);

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

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            // 念のため状態を初期状態へ。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            // 深度マップを作成。
            CreateDepthMap(context);

            // 通常シーンを描画。
            CreateNormalSceneMap(context);

            // ブラー済みシーンを作成。
            CreateBluredSceneMap(context);

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap(context);

            // 中間マップを描画。
            DrawInterMapsToScreen();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateDepthMap(DeviceContext context)
        {
            context.SetRenderTarget(depthMapRenderTarget.GetRenderTargetView());
            context.Clear(Vector4.One);

            DrawScene(context, depthMapEffect);

            context.SetRenderTarget(null);

            // 被写界深度合成ポストプロセスへ深度マップを設定。
            dofCombine.LinearDepthMap = depthMapRenderTarget.GetShaderResourceView();
        }

        void CreateNormalSceneMap(DeviceContext context)
        {
            context.SetRenderTarget(normalSceneRenderTarget.GetRenderTargetView());
            context.Clear(Color.CornflowerBlue);

            DrawScene(context, basicEffect);

            context.SetRenderTarget(null);

            // 被写界深度合成ポストプロセスへ通常シーンを設定。
            dofCombine.BaseTexture = normalSceneRenderTarget.GetShaderResourceView();
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

        void CreateBluredSceneMap(DeviceContext context)
        {
            gaussianBlur.Filter(
                context,
                normalSceneRenderTarget.GetShaderResourceView(),
                bluredSceneRenderTarget.GetRenderTargetView());
        }

        void CreateFinalSceneMap(DeviceContext context)
        {
            context.PixelShaderResources[0] = bluredSceneRenderTarget.GetShaderResourceView();
            dofCombine.Apply(context);
            fullScreenQuad.Draw(context);
        }

        void DrawInterMapsToScreen()
        {
            // 中間マップを画面左上に表示。

            const float scale = 0.2f;

            int w = (int) (WindowWidth * scale);
            int h = (int) (WindowHeight * scale);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);

            int index;
            int x;

            index = 0;
            x = index * w;
            spriteBatch.Draw(depthMapRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            index = 1;
            x = index * w;
            spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            index = 2;
            x = index * w;
            spriteBatch.Draw(bluredSceneRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);
            
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text =
                "PageUp/Down: Focus distance (" + dofCombine.FocusDistance + ")\n" +
                "Home/End: Focus range (" + dofCombine.FocusRange + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 380), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 380 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;

            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.PageUp))
            {
                dofCombine.FocusDistance += 1;
                if (camera.FarClipDistance < dofCombine.FocusDistance)
                    dofCombine.FocusDistance = camera.FarClipDistance;
            }

            if (currentKeyboardState.IsKeyDown(Keys.PageDown))
            {
                dofCombine.FocusDistance -= 1;
                if (dofCombine.FocusDistance < camera.NearClipDistance)
                    dofCombine.FocusDistance = camera.NearClipDistance;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Home))
            {
                dofCombine.FocusRange += 1;
                if (500.0f < dofCombine.FocusRange)
                    dofCombine.FocusRange = 500.0f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.R))
            {
                // カメラ位置と同じ操作で焦点もリセット。
                dofCombine.FocusDistance = 10;
                dofCombine.FocusRange = 200;
            }

            if (currentKeyboardState.IsKeyDown(Keys.End))
            {
                dofCombine.FocusRange -= 1;
                if (dofCombine.FocusRange < 10.0f)
                    dofCombine.FocusRange = 10.0f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }
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
