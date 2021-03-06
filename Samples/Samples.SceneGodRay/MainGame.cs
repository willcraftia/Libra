﻿#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
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
        /// 光芒フィルタ。
        /// </summary>
        LightScatteringFilter lightScatteringFilter;

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

            textureDisplay = new TextureDisplay(this);
            const float scale = 0.2f;
            textureDisplay.TextureWidth = (int) (WindowWidth * scale);
            textureDisplay.TextureHeight = (int) (WindowHeight * scale);
            Components.Add(textureDisplay);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(DeviceContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthStencilEnabled = true;
            normalSceneRenderTarget.Name = "Normal";
            normalSceneRenderTarget.Initialize();

            basicEffect = new BasicEffect(DeviceContext);
            basicEffect.Projection = camera.Projection;
            basicEffect.DiffuseColor = Color.Black.ToVector3();
            basicEffect.DirectionalLights[0].Direction = lightDirection;
            basicEffect.EnableDefaultLighting();

            singleColorObjectEffect = new SingleColorObjectEffect(DeviceContext);
            singleColorObjectEffect.Projection = camera.Projection;

            lightScatteringFilter = new LightScatteringFilter(DeviceContext);
            lightScatteringFilter.Density = 2.0f;
            lightScatteringFilter.Exposure = 2.0f;

            fullScreenQuad = new FullScreenQuad(DeviceContext);

            cubeMesh = new CubeMesh(DeviceContext, 10.0f);

            skySphere = new SkySphere(DeviceContext);
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
            singleColorObjectEffect.View = camera.View;
            skySphere.View = camera.View;
            basicEffect.View = camera.View;

            // ライト位置の算出。

            var infiniteView = camera.View;
            infiniteView.Translation = Vector3.Zero;

            // 参考にした XNA Lens Flare サンプルでは調整無しだが、
            // それは near = 0.1 であるが故であり、
            // それなりの距離 (near = 1 など) を置くと、
            // 単位ベクトルであるライト方向の射影が射影領域の外に出てしまう (0 から near の間に射影されてしまう)。
            // このため、常にカメラの外にライトがあると見なされ、レンズ フレアは描画されない。
            // そこで、near = 0.001 とした射影行列を構築し、これに基づいてライトの射影を行う。
            // near = 0 とした場合、真逆を向いた場合にもライトが射影領域に入ってしまう点に注意。

            // 射影行列から情報を抽出。
            float fov = camera.Projection.PerspectiveFieldOfView;
            float aspectRatio = camera.Projection.PerspectiveAspectRatio;
            float far = camera.Projection.PerspectiveFarClipDistance;

            // near = 0.001 の射影行列を再構築。
            Matrix localProjection;
            Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, 0.001f, far, out localProjection);

            var lightPosition = -lightDirection;
            
            var viewport = DeviceContext.Viewport;
            var projectedPosition = viewport.Project(lightPosition, localProjection, infiniteView, Matrix.Identity);

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
            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.Default;

            // 閉塞マップを作成。
            if (!lightBehindCamera)
            {
                CreateOcclusionMap();
            }

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateOcclusionMap()
        {
            if (occlusionRenderTarget == null)
            {
                occlusionRenderTarget = Device.CreateRenderTarget();
                occlusionRenderTarget.Width = (int) (WindowWidth * MapScales[currentMapScaleIndex]);
                occlusionRenderTarget.Height = (int) (WindowHeight * MapScales[currentMapScaleIndex]);
                occlusionRenderTarget.DepthStencilEnabled = true;
                occlusionRenderTarget.Name = "Occlusion";
                occlusionRenderTarget.Initialize();
            }

            DeviceContext.SetRenderTarget(occlusionRenderTarget);

            DeviceContext.Clear(Color.Black);

            DrawCubeMeshes(singleColorObjectEffect);

            skySphere.SkyColor = Color.Black.ToVector3();
            skySphere.Draw();

            textureDisplay.Textures.Add(occlusionRenderTarget);
        }

        void CreateNormalSceneMap()
        {
            DeviceContext.SetRenderTarget(normalSceneRenderTarget);

            DeviceContext.Clear(Color.CornflowerBlue);

            DrawCubeMeshes(basicEffect);

            skySphere.SkyColor = Color.CornflowerBlue.ToVector3();
            skySphere.Draw();

            DeviceContext.SetRenderTarget(null);

            textureDisplay.Textures.Add(normalSceneRenderTarget);
        }

        void DrawCubeMeshes(IEffect effect)
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

                        effect.Apply();
                        cubeMesh.Draw();
                    }
                }
            }
        }

        void CreateFinalSceneMap()
        {
            if (lightBehindCamera)
            {
                // ライト位置がカメラの後方ならば、光芒効果を適用しない。
                spriteBatch.Begin();
                spriteBatch.Draw(normalSceneRenderTarget, Vector2.Zero, Color.White);
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
            DeviceContext.SetRenderTarget(lightScatteringRenderTarget);

            // テクスチャ座標としてライト位置を設定。
            lightScatteringFilter.ScreenLightPosition = screenLightPosition;
            lightScatteringFilter.Texture = occlusionRenderTarget;
            lightScatteringFilter.Apply();

            fullScreenQuad.Draw();

            // 光芒マップと通常シーンを加算混合。
            DeviceContext.SetRenderTarget(null);

            DeviceContext.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            spriteBatch.Draw(normalSceneRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.Draw(lightScatteringRenderTarget, new Rectangle(0, 0, WindowWidth, WindowHeight), Color.White);
            spriteBatch.End();

            textureDisplay.Textures.Add(lightScatteringRenderTarget);
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
                "T/G: Exposure (" + lightScatteringFilter.Exposure + ")\n" +
                "Y/H: Density (" + lightScatteringFilter.Density + ")\n" +
                "U/J: Decay (" + lightScatteringFilter.Decay + ")\n" +
                "I/K: Weight (" + lightScatteringFilter.Weight + ")\n" +
                "PageUp/Down: Sample count (" + lightScatteringFilter.SampleCount + ")\n" +
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
                lightScatteringFilter.Exposure += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.G))
                lightScatteringFilter.Exposure = Math.Max(0.0f, lightScatteringFilter.Exposure - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.Y))
                lightScatteringFilter.Density += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.H))
                lightScatteringFilter.Density = Math.Max(0.0f, lightScatteringFilter.Density - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.U))
                lightScatteringFilter.Decay += 0.001f;
            if (currentKeyboardState.IsKeyDown(Keys.J))
                lightScatteringFilter.Decay = Math.Max(0.0f, lightScatteringFilter.Decay - 0.001f);

            if (currentKeyboardState.IsKeyDown(Keys.I))
                lightScatteringFilter.Weight += 0.01f;
            if (currentKeyboardState.IsKeyDown(Keys.K))
                lightScatteringFilter.Weight = Math.Max(0.0f, lightScatteringFilter.Weight - 0.01f);

            if (currentKeyboardState.IsKeyDown(Keys.PageUp))
                lightScatteringFilter.SampleCount = Math.Min(LightScatteringFilter.MaxSampleCount, lightScatteringFilter.SampleCount + 1);
            if (currentKeyboardState.IsKeyDown(Keys.PageDown))
                lightScatteringFilter.SampleCount = Math.Max(0, lightScatteringFilter.SampleCount - 1);

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
