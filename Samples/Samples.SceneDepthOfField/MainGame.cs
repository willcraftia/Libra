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
        /// 深度マップ、および、ブラー済みシーンのスケール。
        /// </summary>
        float mapScale = 0.5f;

        /// <summary>
        /// 深度マップ。
        /// </summary>
        DepthMap depthMap;

        /// <summary>
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        /// <summary>
        /// ブラー済みシーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget bluredSceneRenderTarget;

        /// <summary>
        /// シーンに適用するブラー。
        /// </summary>
        GaussianBlur gaussianBlur;

        /// <summary>
        /// 被写界深度。
        /// </summary>
        DepthOfField depthOfField;

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
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            spriteFont = content.Load<SpriteFont>("hudFont");

            depthMap = new DepthMap(Device);

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            bluredSceneRenderTarget = Device.CreateRenderTarget();
            bluredSceneRenderTarget.Width = (int) (WindowWidth * mapScale);
            bluredSceneRenderTarget.Height = (int) (WindowWidth * mapScale);
            bluredSceneRenderTarget.Initialize();

            gaussianBlur = new GaussianBlur(
                Device, bluredSceneRenderTarget.Width, bluredSceneRenderTarget.Height, bluredSceneRenderTarget.Format);

            depthOfField = new DepthOfField(Device);

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

            // 深度マップを作成。
            CreateDepthMap();

            // 通常シーンを描画。
            CreateNormalSceneMap();

            // ブラー済みシーンを作成。
            CreateBluredSceneMap();

            // 最終的なシーンをバック バッファへ描画。
            CreateFinalSceneMap();

            // 中間マップを描画。
            DrawInterMapsToScreen();

            // HUD のテキストを描画。
            DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateDepthMap()
        {
            var context = Device.ImmediateContext;

            depthMap.Width = (int) (WindowWidth * mapScale);
            depthMap.Height = (int) (WindowHeight * mapScale);

            depthMap.Draw(context, camera.View, camera.Projection, DrawDepthMapObjects);
        }

        void DrawDepthMapObjects(Matrix view, Matrix projection, DepthMapEffect effect)
        {
            DrawModel(gridModel, Matrix.Identity, effect);
            DrawModel(dudeModel, Matrix.CreateRotationY(MathHelper.ToRadians(rotateDude)), effect);
        }

        void DrawModel(Model model, Matrix world, DepthMapEffect effect)
        {
            var context = Device.ImmediateContext;

            // 深度マップ エフェクトの準備。
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
                    effect.World = Matrix.Identity;
                    effect.View = camera.View;
                    effect.Projection = camera.Projection;
                    effect.EnableDefaultLighting();
                }

                mesh.Draw(context);
            }
        }

        void CreateBluredSceneMap()
        {
            var context = Device.ImmediateContext;

            gaussianBlur.Filter(
                context,
                normalSceneRenderTarget.GetShaderResourceView(),
                bluredSceneRenderTarget.GetRenderTargetView());
        }

        void CreateFinalSceneMap()
        {
            var context = Device.ImmediateContext;

            depthOfField.Draw(
                context,
                normalSceneRenderTarget.GetShaderResourceView(),
                bluredSceneRenderTarget.GetShaderResourceView(),
                depthMap.RenderTarget.GetShaderResourceView());
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
            spriteBatch.Draw(depthMap.RenderTarget.GetShaderResourceView(), new Rectangle(x, 0, w, h), Color.White);

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
            var text = "";

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
