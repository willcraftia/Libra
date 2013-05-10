#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.SceneGodRay
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
        /// マップ スケール配列。
        /// </summary>
        static readonly float[] MapScales = { 1.0f, 0.5f, 0.25f };

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
        /// 閉塞マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget occlusionRenderTarget;

        /// <summary>
        /// 光芒マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget lightScatteringRenderTarget;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// 現在選択されているマップ スケールのインデックス。
        /// </summary>
        int currentMapScaleIndex = 0;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        Vector3 lightDirection = Vector3.Backward;

        /// <summary>
        /// 基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        /// <summary>
        /// 単色オブジェクト描画エフェクト。
        /// </summary>
        SingleColorObjectEffect singleColorObjectEffect;

        /// <summary>
        /// 光芒ポストプロセス。
        /// </summary>
        LightScattering lightScattering;

        /// <summary>
        /// FullScreenQuad。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// 立方体メッシュ。
        /// </summary>
        CubeMesh cubeMesh;

        /// <summary>
        /// スカイ スフィア。
        /// </summary>
        SkySphere skySphere;

        /// <summary>
        /// ライト位置がカメラの後方であるか否かを示す値。
        /// </summary>
        bool lightBehindCamera;

        /// <summary>
        /// スクリーン空間におけるライト位置 (テクスチャ座標)。
        /// </summary>
        Vector2 screenLightPosition;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = WindowWidth;
            graphicsManager.PreferredBackBufferHeight = WindowHeight;

            camera = new BasicCamera
            {
                Position = new Vector3(0, 0, 300),
                Direction = Vector3.Forward,
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) WindowWidth / (float) WindowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };
            camera.Update();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Name = "Normal";
            normalSceneRenderTarget.Initialize();

            basicEffect = new BasicEffect(Device);
            basicEffect.Projection = camera.Projection;
            basicEffect.DiffuseColor = Color.Black.ToVector3();
            basicEffect.DirectionalLights[0].Direction = lightDirection;
            basicEffect.EnableDefaultLighting();

            singleColorObjectEffect = new SingleColorObjectEffect(Device);
            singleColorObjectEffect.Projection = camera.Projection;

            lightScattering = new LightScattering(Device);
            lightScattering.Density = 2.0f;
            lightScattering.Exposure = 2.0f;

            fullScreenQuad = new FullScreenQuad(Device);

            cubeMesh = new CubeMesh(Device, 10.0f);

            skySphere = new SkySphere(Device);
            skySphere.Projection = camera.Projection;
            skySphere.SunDirection = -lightDirection;
            skySphere.SunThreshold = 0.99f;
            skySphere.SkyColor = Color.CornflowerBlue.ToVector3();
            skySphere.SunColor = Color.White.ToVector3();
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

            singleColorObjectEffect.View = camera.View;
            skySphere.View = camera.View;
            basicEffect.View = camera.View;

            // ライト位置の算出。

            var infiniteView = camera.View;
            infiniteView.Translation = Vector3.Zero;

            var lightPosition = -lightDirection;

            // 参考にした XNA Lens Flare サンプルでは調整無しだが、それは near = 0.1 であるが故であり、
            // それなりの距離 (near = 1 など) を置くと、単位ベクトルであるライト方向を射影した場合に
            // 射影空間の外に出てしまう (0 から near の間に射影されてしまう)。
            // このため、near だけカメラ奥へ押し出した後に射影する (射影空間に収まる位置で射影する)。
            lightPosition.Z -= camera.NearClipDistance;

            var viewport = context.Viewport;
            var projectedPosition = viewport.Project(lightPosition, camera.Projection, infiniteView, Matrix.Identity);

            if (projectedPosition.Z < 0 || projectedPosition.Z > 1)
            {
                lightBehindCamera = true;
            }
            else
            {
                lightBehindCamera = false;
                screenLightPosition = new Vector2(projectedPosition.X / viewport.Width, projectedPosition.Y / viewport.Height);
            }

            // 描画。

            // 念のため状態を初期状態へ。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            // 閉塞マップを作成。
            if (!lightBehindCamera)
            {
                CreateOcclusionMap(context);
            }

            // 通常シーンを描画。
            CreateNormalSceneMap(context);

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap(context);

            // 中間マップを描画。
            DrawInterMapsToScreen();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateOcclusionMap(DeviceContext context)
        {
            if (occlusionRenderTarget == null)
            {
                occlusionRenderTarget = Device.CreateRenderTarget();
                occlusionRenderTarget.Width = (int) (WindowWidth * MapScales[currentMapScaleIndex]);
                occlusionRenderTarget.Height = (int) (WindowHeight * MapScales[currentMapScaleIndex]);
                occlusionRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
                occlusionRenderTarget.Name = "Occlusion";
                occlusionRenderTarget.Initialize();
            }

            context.SetRenderTarget(occlusionRenderTarget.GetRenderTargetView());

            context.Clear(Color.Black);

            DrawCubeMeshes(context, singleColorObjectEffect);

            skySphere.SkyColor = Color.Black.ToVector3();
            skySphere.Draw(context);
        }

        void CreateNormalSceneMap(DeviceContext context)
        {
            context.SetRenderTarget(normalSceneRenderTarget.GetRenderTargetView());

            context.Clear(Color.CornflowerBlue);

            DrawCubeMeshes(context, basicEffect);

            skySphere.SkyColor = Color.CornflowerBlue.ToVector3();
            skySphere.Draw(context);

            context.SetRenderTarget(null);
        }

        void DrawCubeMeshes(DeviceContext context, IEffect effect)
        {
            var effectMatrices = effect as IEffectMatrices;

            const float distance = 30.0f;

            Vector3 position;
            Matrix world;

            for (int x = -2; x < 3; x++)
            {
                for (int y = -2; y < 3; y++)
                {
                    for (int z = -2; z < 3; z++)
                    {
                        if (effectMatrices != null)
                        {
                            position.X = x * distance;
                            position.Y = y * distance;
                            position.Z = z * distance;

                            Matrix.CreateTranslation(ref position, out world);

                            effectMatrices.World = world;
                        }

                        effect.Apply(context);
                        cubeMesh.Draw(context);
                    }
                }
            }
        }

        void CreateFinalSceneMap(DeviceContext context)
        {
            if (lightBehindCamera)
            {
                // ライト位置がカメラの後方ならば、光芒効果を適用しない。
                spriteBatch.Begin();
                spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), Vector2.Zero, Color.White);
                spriteBatch.End();
                return;
            }

            if (lightScatteringRenderTarget == null)
            {
                lightScatteringRenderTarget = Device.CreateRenderTarget();
                lightScatteringRenderTarget.Width = (int) (WindowWidth * MapScales[currentMapScaleIndex]);
                lightScatteringRenderTarget.Height = (int) (WindowHeight * MapScales[currentMapScaleIndex]);
                lightScatteringRenderTarget.Name = "LightScattering";
                lightScatteringRenderTarget.Initialize();
            }

            // 閉塞マップから光芒マップを生成。
            context.SetRenderTarget(lightScatteringRenderTarget.GetRenderTargetView());

            // テクスチャ座標としてライト位置を設定。
            lightScattering.ScreenLightPosition = screenLightPosition;
            lightScattering.Texture = occlusionRenderTarget.GetShaderResourceView();
            lightScattering.Apply(context);

            fullScreenQuad.Draw(context);

            // 光芒マップと通常シーンを加算混合。
            context.SetRenderTarget(null);

            context.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), Vector2.Zero, Color.White);
            spriteBatch.Draw(lightScatteringRenderTarget.GetShaderResourceView(), new Rectangle(0, 0, WindowWidth, WindowHeight), Color.White);
            spriteBatch.End();
        }

        void DrawInterMapsToScreen()
        {
            // 中間マップを画面左上に表示。

            const float scale = 0.2f;

            int w = (int) (WindowWidth * scale);
            int h = (int) (WindowHeight * scale);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);

            int index;
            int x;

            index = 0;
            x = index * w;
            spriteBatch.Draw(occlusionRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            index = 1;
            x = index * w;
            spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            index = 2;
            x = index * w;
            spriteBatch.Draw(lightScatteringRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            spriteBatch.End();
        }

        void InvalidateRenderTargets()
        {
            if (occlusionRenderTarget != null)
            {
                occlusionRenderTarget.Dispose();
                occlusionRenderTarget = null;
            }
            if (lightScatteringRenderTarget != null)
            {
                lightScatteringRenderTarget.Dispose();
                lightScatteringRenderTarget = null;
            }
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text =
                "T/G: Exposure (" + lightScattering.Exposure + ")\n" +
                "Y/H: Density (" + lightScattering.Density + ")\n" +
                "U/J: Decay (" + lightScattering.Decay + ")\n" +
                "I/K: Weight (" + lightScattering.Weight + ")\n" +
                "PageUp/Down: Sample count (" + lightScattering.SampleCount + ")\n" +
                "Home/End: Light occlusion & scattering map scale (x " + MapScales[currentMapScaleIndex] + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(33, 352), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(32, 352 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;

            currentKeyboardState = Keyboard.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            if (currentKeyboardState.IsKeyDown(Keys.T))
                lightScattering.Exposure += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.G))
                lightScattering.Exposure = Math.Max(0.0f, lightScattering.Exposure - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.Y))
                lightScattering.Density += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.H))
                lightScattering.Density = Math.Max(0.0f, lightScattering.Density - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.U))
                lightScattering.Decay += 0.001f;
            if (currentKeyboardState.IsKeyDown(Keys.J))
                lightScattering.Decay = Math.Max(0.0f, lightScattering.Decay - 0.001f);

            if (currentKeyboardState.IsKeyDown(Keys.I))
                lightScattering.Weight += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.K))
                lightScattering.Weight = Math.Max(0.0f, lightScattering.Weight - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.PageUp))
                lightScattering.SampleCount = Math.Min(LightScattering.MaxSampleCount, lightScattering.SampleCount + 1);
            if (currentKeyboardState.IsKeyDown(Keys.PageDown))
                lightScattering.SampleCount = Math.Max(0, lightScattering.SampleCount - 1);

            if (currentKeyboardState.IsKeyUp(Keys.Home) && lastKeyboardState.IsKeyDown(Keys.Home))
            {
                if (0 < currentMapScaleIndex)
                {
                    currentMapScaleIndex--;
                    InvalidateRenderTargets();
                }
            }
            if (currentKeyboardState.IsKeyUp(Keys.End) && lastKeyboardState.IsKeyDown(Keys.End))
            {
                if (currentMapScaleIndex < MapScales.Length - 1)
                {
                    currentMapScaleIndex++;
                    InvalidateRenderTargets();
                }
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
                camera.Position = new Vector3(0, 0, 300);
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
