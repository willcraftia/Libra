﻿#region Using

using System;
using System.Text;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
using Libra.Graphics;
using Libra.Graphics.Compiler;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.Water
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
        static readonly Vector3 InitialCameraPosition = new Vector3(0, 70, 100);

        /// <summary>
        /// 表示カメラの初期注視点。
        /// </summary>
        static readonly Vector3 InitialCameraLookAt = Vector3.Zero;

        static readonly Random Random = new Random();

        /// <summary>
        /// Libra のグラフィックス マネージャ。
        /// </summary>
        GraphicsManager graphicsManager;

        /// <summary>
        /// Libra の XNA コンテント マネージャ。
        /// </summary>
        XnbManager content;

        /// <summary>
        /// 描画に使用するコンテキスト。
        /// </summary>
        DeviceContext context;

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
        /// 通常シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget normalSceneRenderTarget;

        RenderTargetChain waveRenderTargetChain;

        RenderTarget waveNormalMapRenderTarget;

        RenderTarget waveGradientMapRenderTarget;

        RenderTarget reflectionSceneRenderTarget;

        RenderTarget refractionSceneRenderTarget;

        /// <summary>
        /// メッシュ描画のための基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        WaveFilter waveFilter;

        HeightToNormalFilter heightToNormalFilter;

        HeightToGradientFilter heightToGradientFilter;

        FullScreenQuad fullScreenQuad;

        FluidEffect fluidEffect;

        ClippingEffect clippingEffect;

        // 流体面メッシュの頂点。
        VertexPositionTexture[] fluidVertices =
        {
            new VertexPositionTexture(new Vector3(-100, 0,  100), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-100, 0, -100), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 100, 0, -100), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3( 100, 0,  100), new Vector2(1, 1)),
        };

        // 流体面メッシュのインデックス。
        ushort[] fluidIndices =
        {
            0, 1, 2,
            0, 2, 3
        };

        VertexBuffer fluidVertexBuffer;

        IndexBuffer fluidIndexBuffer;

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

        Matrix fluidWorld = Matrix.CreateTranslation(0, 5, 0);

        Plane fluidPlane;

        Plane clipPlane;

        Matrix reflectionView = Matrix.Identity;

        float newWaveInterval = 3.0f;

        float elapsedNewWaveTime;

        /// <summary>
        /// HUD テキストを表示するか否かを示す値。
        /// </summary>
        bool hudVisible;

        Texture2D pseudoReflectionMap;

        Texture2D pseudoRefractionMap;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            graphicsManager.PreferredBackBufferWidth = WindowWidth;
            graphicsManager.PreferredBackBufferHeight = WindowHeight;

            camera = new BasicCamera
            {
                Position = InitialCameraPosition,
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) WindowWidth / (float) WindowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };
            camera.LookAt(InitialCameraLookAt);
            camera.Update();

            // 流体面。
            Plane localFluidPlane = new Plane(Vector3.Up, 0.0f);
            Plane.Transform(ref localFluidPlane, ref fluidWorld, out fluidPlane);

            Plane localeClipPlane = new Plane(Vector3.Down, 0.0f);
            Plane.Transform(ref localeClipPlane, ref fluidWorld, out clipPlane);

            textureDisplay = new TextureDisplay(this);
            Components.Add(textureDisplay);

            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);

            //IsFixedTimeStep = false;

            hudVisible = true;
        }

        protected override void LoadContent()
        {
            context = Device.ImmediateContext;

            spriteBatch = new SpriteBatch(context);
            spriteFont = content.Load<SpriteFont>("hudFont");

            normalSceneRenderTarget = Device.CreateRenderTarget();
            normalSceneRenderTarget.Width = WindowWidth;
            normalSceneRenderTarget.Height = WindowHeight;
            normalSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            normalSceneRenderTarget.Initialize();

            waveRenderTargetChain = new RenderTargetChain(Device);
            waveRenderTargetChain.Width = 128;
            waveRenderTargetChain.Height = 128;
            waveRenderTargetChain.Format = SurfaceFormat.Vector2;

            waveNormalMapRenderTarget = Device.CreateRenderTarget();
            waveNormalMapRenderTarget.Width = waveRenderTargetChain.Width;
            waveNormalMapRenderTarget.Height = waveRenderTargetChain.Height;
            waveNormalMapRenderTarget.Format = SurfaceFormat.Vector4;
            waveNormalMapRenderTarget.Initialize();

            waveGradientMapRenderTarget = Device.CreateRenderTarget();
            waveGradientMapRenderTarget.Width = waveRenderTargetChain.Width;
            waveGradientMapRenderTarget.Height = waveRenderTargetChain.Height;
            waveGradientMapRenderTarget.Format = SurfaceFormat.Vector2;
            waveGradientMapRenderTarget.Initialize();

            reflectionSceneRenderTarget = Device.CreateRenderTarget();
            reflectionSceneRenderTarget.Width = WindowWidth;
            reflectionSceneRenderTarget.Height = WindowHeight;
            reflectionSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            reflectionSceneRenderTarget.Initialize();

            refractionSceneRenderTarget = Device.CreateRenderTarget();
            refractionSceneRenderTarget.Width = WindowWidth;
            refractionSceneRenderTarget.Height = WindowHeight;
            refractionSceneRenderTarget.DepthFormat = DepthFormat.Depth24Stencil8;
            refractionSceneRenderTarget.Initialize();

            basicEffect = new BasicEffect(Device);
            basicEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

            waveFilter = new WaveFilter(Device);
            heightToNormalFilter = new HeightToNormalFilter(Device);
            heightToGradientFilter = new HeightToGradientFilter(Device);

            fullScreenQuad = new FullScreenQuad(context);

            fluidEffect = new FluidEffect(Device);

            clippingEffect = new ClippingEffect(Device);
            clippingEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            clippingEffect.EnableDefaultLighting();
            //clippingEffect.ClipPlane0 = new Vector4(-fluidPlane.Normal, fluidPlane.D);
            clippingEffect.ClipPlane0 = clipPlane.ToVector4();

            fluidVertexBuffer = Device.CreateVertexBuffer();
            fluidVertexBuffer.Initialize(fluidVertices);

            fluidIndexBuffer = Device.CreateIndexBuffer();
            fluidIndexBuffer.Initialize(fluidIndices);

            cubeMesh = new CubeMesh(context, 20);
            sphereMesh = new SphereMesh(context, 20, 32);
            cylinderMesh = new CylinderMesh(context, 80, 20, 32);
            squareMesh = new SquareMesh(context, 400);

            // TODO
            pseudoReflectionMap = Device.CreateTexture2D();
            pseudoReflectionMap.Width = 2;
            pseudoReflectionMap.Height = 2;
            pseudoReflectionMap.Initialize();
            pseudoReflectionMap.SetData(context, Color.White, Color.Red, Color.Red, Color.White);

            pseudoRefractionMap = Device.CreateTexture2D();
            pseudoRefractionMap.Width = 2;
            pseudoRefractionMap.Height = 2;
            pseudoRefractionMap.Initialize();
            pseudoRefractionMap.SetData(context, Color.Green, Color.Blue, Color.Blue, Color.Green);
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            elapsedNewWaveTime += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (newWaveInterval <= elapsedNewWaveTime)
            {
                var position = new Vector2(0.6f, 0.3f);
                //var position = new Vector2((float) Random.NextDouble(), (float) Random.NextDouble());
                var radius = Random.Next(1, 20) / 128.0f;
                var velocity = (float) Random.NextDouble();
                //var velocity = 0.01f;

                waveFilter.AddWave(position, radius, velocity);

                elapsedNewWaveTime -= newWaveInterval;
            }

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
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;

            CreateWaveMap();
            CreateWaveNormalMap();
            CreateWaveGradientMap();

            CreateReflectionMap();
            CreateRefractionMap();

            // シーンを描画。
            DrawScene();

            // HUD のテキストを描画。
            if (hudVisible)
                DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateWaveMap()
        {
            waveRenderTargetChain.Next();

            context.SetRenderTarget(waveRenderTargetChain.Current);
            context.Clear(Vector4.Zero);

            context.DepthStencilState = DepthStencilState.None;
            context.PixelShaderResources[0] = waveRenderTargetChain.Last;

            waveFilter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            heightToNormalFilter.HeightMap = waveRenderTargetChain.Current;
            heightToGradientFilter.HeightMap = waveRenderTargetChain.Current;

            textureDisplay.Textures.Add(waveRenderTargetChain.Current);
        }

        void CreateWaveNormalMap()
        {
            context.SetRenderTarget(waveNormalMapRenderTarget);
            context.Clear(Vector3.Up.ToVector4());

            context.DepthStencilState = DepthStencilState.None;
            context.PixelShaderResources[0] = waveRenderTargetChain.Current;

            heightToNormalFilter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            context.SetRenderTarget(null);

            fluidEffect.NormalMap = waveNormalMapRenderTarget;

            textureDisplay.Textures.Add(waveNormalMapRenderTarget);
        }

        void CreateWaveGradientMap()
        {
            context.SetRenderTarget(waveGradientMapRenderTarget);
            context.Clear(Vector3.Up.ToVector4());

            context.DepthStencilState = DepthStencilState.None;
            context.PixelShaderResources[0] = waveRenderTargetChain.Current;

            heightToGradientFilter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;
            
            context.SetRenderTarget(null);

            textureDisplay.Textures.Add(waveGradientMapRenderTarget);
        }

        static void CreateReflectionView(ref Matrix eyeView, ref Plane plane, out Matrix result)
        {
            // 流体面に対して裏側に位置する仮想カメラを算出し、
            // 反射される空間を描画するためのカメラとして用いる。

            // 表示カメラのワールド行列。
            Matrix eyeWorld;
            Matrix.Invert(ref eyeView, out eyeWorld);

            // 表示カメラ位置。
            Vector3 eyePosition = eyeWorld.Translation;

            // 反射仮想カメラ位置。
            Vector3 position;
            CalculateVirtualEyePosition(ref plane, ref eyePosition, out position);

            // 表示カメラ方向。
            Vector3 eyeDirection = eyeWorld.Forward;

            // 反射仮想カメラ方向。
            Vector3 direction;
            CalculateVirtualEyeDirection(ref plane, ref eyeDirection, out direction);

            // 反射仮想カメラ up ベクトル。
            Vector3 up = Vector3.Up;
            if (1.0f - MathHelper.ZeroTolerance < Math.Abs(Vector3.Dot(up, direction)))
            {
                // カメラ方向と並行になるならば z 方向を設定。
                up = Vector3.Forward;
            }

            // 反射仮想カメラのビュー行列。
            Matrix.CreateLook(ref position, ref direction, ref up, out result);
        }

        static void CalculateVirtualEyePosition(ref Plane plane, ref Vector3 eyePosition, out Vector3 result)
        {
            // v  : eyePosition
            // v' : result
            // n  : plane の法線
            // d  : v から p までの距離
            //
            // v' = v - 2 * d * n
            //
            // つまり v を n の逆方向へ (2 * d) の距離を移動させた点が v'。

            float distance;
            plane.DotCoordinate(ref eyePosition, out distance);

            result = eyePosition - 2.0f * distance * plane.Normal;
        }

        static void CalculateVirtualEyeDirection(ref Plane plane, ref Vector3 eyeDirection, out Vector3 result)
        {
            // f  : eyeDirection
            // f' : result
            // n  : plane の法線
            // d  : f から p までの距離 (負値)
            //
            // f' = f - 2 * d * n
            //
            // d は負値であるため、f' = f + 2 * (abs(d)) * n と考えても良い。
            // f は単位ベクトルであるため、距離算出では plane.D を考慮しなくて良い。

            float distance;
            Vector3.Dot(ref eyeDirection, ref plane.Normal, out distance);

            result = eyeDirection - 2.0f * distance * plane.Normal;
        }

        void CreateReflectionMap()
        {
            CreateReflectionView(ref camera.View, ref fluidPlane, out reflectionView);

            context.SetRenderTarget(reflectionSceneRenderTarget);
            context.Clear(Color.CornflowerBlue);

            DrawSceneWithoutFluid(basicEffect, ref reflectionView);

            context.SetRenderTarget(null);

            textureDisplay.Textures.Add(reflectionSceneRenderTarget);

            // TODO
            //fluidEffect.ReflectionMap = pseudoReflectionMap;
            fluidEffect.ReflectionMap = reflectionSceneRenderTarget;
        }

        void CreateRefractionMap()
        {
            context.SetRenderTarget(refractionSceneRenderTarget);
            context.Clear(Color.CornflowerBlue);

            DrawSceneWithoutFluid(clippingEffect, ref camera.View);

            context.SetRenderTarget(null);

            textureDisplay.Textures.Add(refractionSceneRenderTarget);

            // TODO
            //fluidEffect.RefractionMap = pseudoRefractionMap;
            fluidEffect.RefractionMap = refractionSceneRenderTarget;
        }

        void DrawSceneWithoutFluid(IEffect effect, ref Matrix view)
        {
            var effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.View = view;
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

        void DrawScene()
        {
            context.Clear(Color.CornflowerBlue);

            fluidEffect.World = fluidWorld;
            fluidEffect.View = camera.View;
            fluidEffect.Projection = camera.Projection;
            fluidEffect.ReflectionView = reflectionView;
            fluidEffect.Apply(context);

            context.SetVertexBuffer(fluidVertexBuffer);
            context.IndexBuffer = fluidIndexBuffer;

            context.DrawIndexed(fluidIndexBuffer.IndexCount);

            //DrawPrimitiveMesh(squareMesh, Matrix.Identity, new Vector3(0.5f), fluidEffect);
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

            var clippingEffect = effect as ClippingEffect;
            if (clippingEffect != null)
            {
                clippingEffect.DiffuseColor = color;
            }

            effect.Apply(context);
            mesh.Draw();
        }

        void DrawOverlayText()
        {
            // HUD のテキストを表示。
            string text = "";

            string basicText =
                "[F1] HUD on/off\n" +
                "[F2] Inter-maps on/off\n";

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
                camera.Position = InitialCameraPosition;
                camera.LookAt(InitialCameraLookAt);
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
