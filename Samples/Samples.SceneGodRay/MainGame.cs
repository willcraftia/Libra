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
        /// 前回の更新処理におけるジョイスティック状態。
        /// </summary>
        JoystickState lastJoystickState;

        /// <summary>
        /// 現在の更新処理におけるキーボード状態。
        /// </summary>
        KeyboardState currentKeyboardState;

        /// <summary>
        /// 現在の更新処理におけるジョイスティック状態。
        /// </summary>
        JoystickState currentJoystickState;

        /// <summary>
        /// 閉塞マップのスケール。
        /// </summary>
        //float mapScale = 0.5f;
        float mapScale = 1.0f;

        /// <summary>
        /// 閉塞マップ。
        /// </summary>
        GodRayOcclusionMap occlusionMap;

        GodRayEffect godRayEffect;

        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// グリッド モデル (格子状の床)。
        /// </summary>
        Model gridModel;

        /// <summary>
        /// デュード モデル (人)。
        /// </summary>
        Model dudeModel;

        /// <summary>
        /// デュード モデルの回転量。
        /// </summary>
        float rotateDude;

        /// <summary>
        /// ライトの進行方向。
        /// </summary>
        //Vector3 lightDirection = Vector3.Normalize(new Vector3(0.3333333f, -0.6666667f, 0.6666667f));
        Vector3 lightDirection = Vector3.Normalize(new Vector3(-1, -0.1f, 0.3f));

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = WindowWidth;
            graphicsManager.PreferredBackBufferHeight = WindowHeight;

            camera = new BasicCamera
            {
                Position = new Vector3(0, 70, 100),
                Direction = new Vector3(0, -0.4472136f, -0.8944272f),
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

            occlusionMap = new GodRayOcclusionMap(Device);

            godRayEffect = new GodRayEffect(Device);

            fullScreenQuad = new FullScreenQuad(Device);

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            gridModel = content.Load<Model>("grid");
            dudeModel = content.Load<Model>("dude");
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

            // 閉塞マップを作成。
            CreateOcclusionMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap();

            // 中間マップを描画。
            DrawInterMapsToScreen();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateOcclusionMap()
        {
            var context = Device.ImmediateContext;

            occlusionMap.Width = (int) (WindowWidth * mapScale);
            occlusionMap.Height = (int) (WindowHeight * mapScale);

            occlusionMap.Draw(context, camera.View, camera.Projection, DrawOcclusionMapObjects);
        }

        void DrawOcclusionMapObjects(Matrix view, Matrix projection, GodRayOcclusionMapEffect effect)
        {
            DrawModel(gridModel, Matrix.Identity, effect);
            DrawModel(dudeModel, Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude)), effect);
        }

        void DrawModel(Model model, Matrix world, GodRayOcclusionMapEffect effect)
        {
            var context = Device.ImmediateContext;

            // 閉塞マップ エフェクトの準備。
            effect.World = world;
            effect.Apply(context);

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffer(0, meshPart.VertexBuffer);
                    context.IndexBuffer = meshPart.IndexBuffer;
                    context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void CreateNormalSceneMap()
        {
            var context = Device.ImmediateContext;

            context.SetRenderTarget(normalSceneRenderTarget.GetRenderTargetView());

            context.Clear(Color.CornflowerBlue);

            DrawModel(gridModel, Matrix.Identity);
            DrawModel(dudeModel, Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude)));

            context.SetRenderTarget(null);
        }

        void DrawModel(Model model, Matrix world)
        {
            var context = Device.ImmediateContext;

            foreach (var mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = camera.View;
                    effect.Projection = camera.Projection;
                    effect.EnableDefaultLighting();
                }

                mesh.Draw(context);
            }
        }

        void CreateFinalSceneMap()
        {
            var context = Device.ImmediateContext;

            var infiniteView = camera.View;
            infiniteView.Translation = Vector3.Zero;

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, context.Viewport.AspectRatio, 0.1f, 500.0f);

            var viewport = context.Viewport;
            var projectedPosition = viewport.Project(-lightDirection, projection, infiniteView, Matrix.Identity);

            if (/*projectedPosition.X < 0 || projectedPosition.X > viewport.Width ||
                projectedPosition.Y < 0 || projectedPosition.Y > viewport.Height ||*/
                projectedPosition.Z < 0 || projectedPosition.Z > 1)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), normalSceneRenderTarget.Bounds, Color.White);
                spriteBatch.End();
                return;
            }

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;

            //godRayEffect.ScreenLightPosition = new Vector2(400, 240);
            godRayEffect.ScreenLightPosition = new Vector2(projectedPosition.X / context.Viewport.Width, projectedPosition.Y / context.Viewport.Height);
            godRayEffect.SceneMap = normalSceneRenderTarget.GetShaderResourceView();
            godRayEffect.OcclusionMap = occlusionMap.RenderTarget.GetShaderResourceView();
            godRayEffect.Apply(context);

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
            spriteBatch.Draw(occlusionMap.RenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            index = 1;
            x = index * w;
            spriteBatch.Draw(normalSceneRenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

            spriteBatch.End();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            var text = "";
            //var text = "PageUp/Down = Focus distance (" + depthOfField.FocusDistance + ")\n" +
            //    "Home/End = Focus range (" + depthOfField.FocusRange + ")";

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 350), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 350 - 1), Color.Yellow);

            spriteBatch.End();
        }

        void HandleInput(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            lastKeyboardState = currentKeyboardState;
            lastJoystickState = currentJoystickState;

            currentKeyboardState = Keyboard.GetState();
            currentJoystickState = Joystick.GetState();

            rotateDude += currentJoystickState.Triggers.Right * time * 0.2f;
            rotateDude -= currentJoystickState.Triggers.Left * time * 0.2f;

            if (currentKeyboardState.IsKeyDown(Keys.Q))
                rotateDude -= time * 0.2f;
            if (currentKeyboardState.IsKeyDown(Keys.E))
                rotateDude += time * 0.2f;

            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentJoystickState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
        }

        void UpdateCamera(GameTime gameTime)
        {
            float time = (float) gameTime.ElapsedGameTime.TotalMilliseconds;

            float pitch = -currentJoystickState.ThumbSticks.Right.Y * time * 0.001f;
            float yaw = -currentJoystickState.ThumbSticks.Right.X * time * 0.001f;

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

            movement.Z -= currentJoystickState.ThumbSticks.Left.Y * time * 0.1f;
            movement.X += currentJoystickState.ThumbSticks.Left.X * time * 0.1f;

            camera.MoveRelative(ref movement);

            if (currentJoystickState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                camera.Position = new Vector3(0, 50, 50);
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
