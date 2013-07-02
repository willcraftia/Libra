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

namespace Samples.SceneVolumetricFog
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
        /// 前面フォグ深度マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget frontFogDepthMapRenderTarget;

        /// <summary>
        /// 背面フォグ深度マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget backFogDepthMapRenderTarget;

        /// <summary>
        /// ボリューム フォグ マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget volumetricFogMapRenderTarget;

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
        /// ボリューム フォグ合成フィルタ。
        /// </summary>
        VolumetricFogCombineFilter volumetricFogCombineFilter;

        /// <summary>
        /// 線形深度マップ可視化フィルタ。
        /// </summary>
        LinearDepthMapColorFilter depthMapColorFilter;

        /// <summary>
        /// 前面フォグ深度マップ可視化フィルタ。
        /// </summary>
        LinearDepthMapColorFilter frontFogDepthMapColorFilter;
        
        /// <summary>
        /// 背面フォグ深度マップ可視化フィルタ。
        /// </summary>
        LinearDepthMapColorFilter backFogDepthMapColorFilter;

        /// <summary>
        /// 線形深度マップ エフェクト。
        /// </summary>
        LinearDepthMapEffect depthMapEffect;

        /// <summary>
        /// 線形フォグ深度マップ エフェクト。
        /// </summary>
        LinearFogDepthMapEffect fogDepthMapEffect;

        /// <summary>
        /// ボリューム フォグ マップ エフェクト。
        /// </summary>
        VolumetricFogMapEffect volumetricFogMapEffect;

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
        /// フォグ領域メッシュ。
        /// </summary>
        CubeMesh volumetricFogMesh;

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
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(DeviceContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMapRenderTarget = Device.CreateRenderTarget();
            depthMapRenderTarget.Width = WindowWidth;
            depthMapRenderTarget.Height = WindowHeight;
            depthMapRenderTarget.Format = SurfaceFormat.Single;
            depthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            depthMapRenderTarget.Initialize();

            frontFogDepthMapRenderTarget = Device.CreateRenderTarget();
            frontFogDepthMapRenderTarget.Width = WindowWidth;
            frontFogDepthMapRenderTarget.Height = WindowHeight;
            frontFogDepthMapRenderTarget.Format = SurfaceFormat.Single;
            frontFogDepthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            frontFogDepthMapRenderTarget.Initialize();

            backFogDepthMapRenderTarget = Device.CreateRenderTarget();
            backFogDepthMapRenderTarget.Width = WindowWidth;
            backFogDepthMapRenderTarget.Height = WindowHeight;
            backFogDepthMapRenderTarget.Format = SurfaceFormat.Single;
            backFogDepthMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            backFogDepthMapRenderTarget.Initialize();

            volumetricFogMapRenderTarget = Device.CreateRenderTarget();
            volumetricFogMapRenderTarget.Width = WindowWidth;
            volumetricFogMapRenderTarget.Height = WindowHeight;
            volumetricFogMapRenderTarget.Format = SurfaceFormat.Single;
            volumetricFogMapRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            volumetricFogMapRenderTarget.Initialize();

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            postprocess = new Postprocess(DeviceContext);
            postprocess.Width = WindowWidth;
            postprocess.Height = WindowHeight;

            downFilter = new DownFilter(DeviceContext);
            upFilter = new UpFilter(DeviceContext);
            volumetricFogCombineFilter = new VolumetricFogCombineFilter(DeviceContext);
            volumetricFogCombineFilter.FogColor = Color.White.ToVector3();
            depthMapColorFilter = new LinearDepthMapColorFilter(DeviceContext);
            depthMapColorFilter.Enabled = false;
            frontFogDepthMapColorFilter = new LinearDepthMapColorFilter(DeviceContext);
            frontFogDepthMapColorFilter.Enabled = false;
            backFogDepthMapColorFilter = new LinearDepthMapColorFilter(DeviceContext);
            backFogDepthMapColorFilter.Enabled = false;
            
            postprocess.Filters.Add(volumetricFogCombineFilter);
            postprocess.Filters.Add(depthMapColorFilter);
            postprocess.Filters.Add(frontFogDepthMapColorFilter);
            postprocess.Filters.Add(backFogDepthMapColorFilter);

            depthMapEffect = new LinearDepthMapEffect(DeviceContext);
            fogDepthMapEffect = new LinearFogDepthMapEffect(DeviceContext);
            volumetricFogMapEffect = new VolumetricFogMapEffect(DeviceContext);

            basicEffect = new BasicEffect(DeviceContext);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

            cubeMesh = new CubeMesh(DeviceContext, 20);
            sphereMesh = new SphereMesh(DeviceContext, 20, 32);
            cylinderMesh = new CylinderMesh(DeviceContext, 80, 20, 32);
            squareMesh = new SquareMesh(DeviceContext, 400);
            volumetricFogMesh = new CubeMesh(DeviceContext, 400);
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
            // 念のため状態を初期状態へ。
            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.Default;

            // 深度マップを描画。
            CreateDepthMap();

            // ボリューム フォグ マップを描画。
            CreateVolumetricFogMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // ポストプロセスを実行。
            ApplyPostprocess();

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

            // エフェクトへ設定。
            fogDepthMapEffect.LinearDepthMap = depthMapRenderTarget;
            // フィルタへ設定。
            depthMapColorFilter.LinearDepthMap = depthMapRenderTarget;
        }

        void CreateVolumetricFogMap()
        {
            // 基本的に、ボリューム フォグはフォグ領域の深度のみを必要とするため、
            // フォグ深度マップの初期値は深度 0 で良い。
            // ただし、前面フォグ深度マップに限っては、初期値を深度 0 とする事が必須である。
            // 例えば、カメラがフォグ領域内に存在する場合には前面が描画されなくなるが、
            // 状態としては前面がカメラの背後にある事を示さねばならず、
            // すなわち深度 0 を初期値とする必要がある。

            DeviceContext.SetRenderTarget(frontFogDepthMapRenderTarget);
            DeviceContext.Clear(Vector4.Zero);

            DeviceContext.RasterizerState = RasterizerState.CullBack;

            DrawVolumetricFog(fogDepthMapEffect);

            DeviceContext.SetRenderTarget(backFogDepthMapRenderTarget);
            DeviceContext.Clear(Vector4.Zero);

            DeviceContext.RasterizerState = RasterizerState.CullFront;

            DrawVolumetricFog(fogDepthMapEffect);

            DeviceContext.SetRenderTarget(null);

            // エフェクトへ設定。
            volumetricFogMapEffect.FrontFogDepthMap = frontFogDepthMapRenderTarget;
            volumetricFogMapEffect.BackFogDepthMap = backFogDepthMapRenderTarget;

            // フィルタへ設定。
            frontFogDepthMapColorFilter.LinearDepthMap = frontFogDepthMapRenderTarget;
            backFogDepthMapColorFilter.LinearDepthMap = backFogDepthMapRenderTarget;

            DeviceContext.SetRenderTarget(volumetricFogMapRenderTarget);
            // ボリューム フォグ マップの値はフォグの度合いを示すため、
            // 0 (フォグ無し) で埋めてから描画する。
            DeviceContext.Clear(Vector4.Zero);

            DeviceContext.RasterizerState = RasterizerState.CullNone;

            DrawVolumetricFog(volumetricFogMapEffect);

            DeviceContext.SetRenderTarget(null);

            DeviceContext.RasterizerState = RasterizerState.CullBack;

            // フィルタへ設定。
            volumetricFogCombineFilter.VolumetricFogMap = volumetricFogMapRenderTarget;

            // 中間マップ表示。
            textureDisplay.Textures.Add(volumetricFogMapRenderTarget);
        }

        void CreateNormalSceneMap()
        {
            DeviceContext.SetRenderTarget(normalSceneRenderTarget);
            DeviceContext.Clear(Color.CornflowerBlue);

            DrawScene(basicEffect);

            DeviceContext.SetRenderTarget(null);

            // 中間マップ表示。
            textureDisplay.Textures.Add(normalSceneRenderTarget);
        }

        void ApplyPostprocess()
        {
            finalSceneTexture = postprocess.Draw(normalSceneRenderTarget);
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

            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-40, 10, 40), new Vector3(0, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-85, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-60, 10, -20), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(cubeMesh, Matrix.CreateTranslation(-40, 10, 0), new Vector3(1, 0, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(10, 10, -60), new Vector3(0, 1, 0), effect);
            DrawPrimitiveMesh(sphereMesh, Matrix.CreateTranslation(0, 10, -40), new Vector3(0, 1, 0), effect);
            for (float z = -180; z <= 180; z += 40)
            {
                DrawPrimitiveMesh(cylinderMesh, Matrix.CreateTranslation(-180, 40, z), new Vector3(0, 0, 1), effect);
            }
            DrawPrimitiveMesh(squareMesh, Matrix.Identity, new Vector3(0.5f), effect);
        }

        void DrawVolumetricFog(IEffect effect)
        {
            var effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.View = camera.View;
                effectMatrices.Projection = camera.Projection;
            }

            DrawPrimitiveMesh(volumetricFogMesh, Matrix.CreateTranslation(0, -200 + 40, 0), new Vector3(0, 0, 0), effect);
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

        void CreateFinalSceneMap()
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(finalSceneTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text = "";

            string basicText =
                "[F1] HUD on/off\n" +
                "[F2] Inter-maps on/off\n" +
                "[F3] Show/Hide Depth Map (" + depthMapColorFilter.Enabled + ")\n" +
                "[F4] Show/Hide Front Fog Depth Map (" + frontFogDepthMapColorFilter.Enabled + ")\n" +
                "[F5] Show/Hide Back Fog Depth Map (" + backFogDepthMapColorFilter.Enabled + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 280), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 280 - 1), Color.Yellow);

            spriteBatch.DrawString(spriteFont, basicText, new Vector2(449, 340), Color.Black);
            spriteBatch.DrawString(spriteFont, basicText, new Vector2(448, 340 - 1), Color.Yellow);

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
            {
                depthMapColorFilter.Enabled = !depthMapColorFilter.Enabled;
                frontFogDepthMapColorFilter.Enabled = false;
                backFogDepthMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F4) && lastKeyboardState.IsKeyDown(Keys.F4))
            {
                frontFogDepthMapColorFilter.Enabled = !frontFogDepthMapColorFilter.Enabled;
                depthMapColorFilter.Enabled = false;
                backFogDepthMapColorFilter.Enabled = false;
            }

            if (currentKeyboardState.IsKeyUp(Keys.F5) && lastKeyboardState.IsKeyDown(Keys.F5))
            {
                backFogDepthMapColorFilter.Enabled = !backFogDepthMapColorFilter.Enabled;
                depthMapColorFilter.Enabled = false;
                frontFogDepthMapColorFilter.Enabled = false;
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
