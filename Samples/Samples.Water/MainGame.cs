#region Using

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
using Musca;
using Musca.Toolkit;

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

        /// <summary>
        /// 乱数生成器。
        /// </summary>
        static readonly Random Random = new Random();

        // 流体面メッシュの頂点。
        static readonly VertexPositionTexture[] FluidVertices =
        {
            new VertexPositionTexture(new Vector3(-200, 0,  200), new Vector2(0, 8)),
            new VertexPositionTexture(new Vector3(-200, 0, -200), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 200, 0, -200), new Vector2(8, 0)),
            new VertexPositionTexture(new Vector3( 200, 0,  200), new Vector2(8, 8)),
        };

        // 流体面メッシュのインデックス。
        static readonly ushort[] FluidIndices =
        {
            0, 1, 2,
            0, 2, 3
        };

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

        /// <summary>
        /// 波法線マップの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget waveNormalMapRenderTarget;

        /// <summary>
        /// 反射シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget reflectionSceneRenderTarget;

        /// <summary>
        /// 屈折シーンの描画先レンダ ターゲット。
        /// </summary>
        RenderTarget refractionSceneRenderTarget;

        /// <summary>
        /// 波マップ ポストプロセスのためのレンダ ターゲット チェイン。
        /// </summary>
        RenderTargetChain waveRenderTargetChain;

        /// <summary>
        /// FullScreenQuad。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// メッシュ描画のための基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        /// <summary>
        /// 波エフェクト。
        /// </summary>
        WaveFilter waveFilter;

        /// <summary>
        /// 高低マップから法線マップへの変換器エフェクト。
        /// </summary>
        HeightToNormalConverter heightToNormalConverter;

        /// <summary>
        /// 流体エフェクト。
        /// </summary>
        FluidEffect fluidEffect;

        /// <summary>
        /// クリッピング エフェクト。
        /// </summary>
        ClippingEffect clippingEffect;

        /// <summary>
        /// 流体面の頂点バッファ。
        /// </summary>
        VertexBuffer fluidVertexBuffer;

        /// <summary>
        /// 流体面のインデックス バッファ。
        /// </summary>
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

        /// <summary>
        /// 流体面の移動行列。
        /// </summary>
        Matrix fluidTranslation = Matrix.CreateTranslation(0, 10, 0);

        /// <summary>
        /// 流体面のワールド行列。
        /// </summary>
        Matrix fluidWorld;
        
        /// <summary>
        /// ローカル座標系における流体面の表側を表す平面。
        /// </summary>
        Plane localFluidFrontPlane = new Plane(Vector3.Up, 0.0f);

        /// <summary>
        /// ローカル座標系における流体面の裏側を表す平面。
        /// </summary>
        Plane localeFluidBackPlane = new Plane(Vector3.Down, 0.0f);

        /// <summary>
        /// ワールド座標系における流体面の表側を表す平面。
        /// </summary>
        Plane fluidFrontPlane;

        /// <summary>
        /// ワールド座標系における流体面の裏側を表す平面。
        /// </summary>
        Plane fluidBackPlane;

        /// <summary>
        /// 反射シーンのビュー行列。
        /// </summary>
        Matrix reflectionView = Matrix.Identity;

        /// <summary>
        /// 表示カメラ位置が流体面の表側にあるか否かを示す値。
        /// </summary>
        bool eyeInFront;

        /// <summary>
        /// 新しい波を生成する間隔 (秒)。
        /// </summary>
        float newWaveInterval = 3.0f;

        /// <summary>
        /// 新しい波を生成してからの経過時間 (秒)。
        /// </summary>
        float elapsedNewWaveTime;

        /// <summary>
        /// 流体面のロール (z 軸周りの回転)。
        /// </summary>
        float fluidRoll;

        Perlin perlin = new Perlin();

        SumFractal sumFractal = new SumFractal();

        Heterofractal heterofractal = new Heterofractal();

        Multifractal multifractal = new Multifractal();

        RidgedMultifractal ridgedMultifractal = new RidgedMultifractal();

        SinFractal sinFractal = new SinFractal();

        NoiseMap noiseMap = new NoiseMap(128, 128);

        NoiseMapBuilder noiseMapBuilder = new NoiseMapBuilder();

        Texture2D noiseWaveHeightMap;

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
                Position = InitialCameraPosition,
                Fov = MathHelper.PiOver4,
                AspectRatio = (float) WindowWidth / (float) WindowHeight,
                NearClipDistance = 1.0f,
                FarClipDistance = 1000.0f
            };
            camera.LookAt(InitialCameraLookAt);
            camera.Update();

            fluidWorld = fluidTranslation;
            Plane.Transform(ref localFluidFrontPlane, ref fluidWorld, out fluidFrontPlane);
            Plane.Transform(ref localeFluidBackPlane, ref fluidWorld, out fluidBackPlane);

            // ノイズ設定。
            perlin.Seed = 999;
            
            sumFractal.Source = perlin;
            heterofractal.Source = perlin;
            multifractal.Source = perlin;
            ridgedMultifractal.Source = perlin;
            sinFractal.Source = perlin;

            noiseMapBuilder.Source = sumFractal;
            //noiseMapBuilder.Source = heterofractal;
            //noiseMapBuilder.Source = multifractal;
            //noiseMapBuilder.Source = ridgedMultifractal;
            //noiseMapBuilder.Source = sinFractal;
            noiseMapBuilder.Destination = noiseMap;
            noiseMapBuilder.Bounds = new Bounds(0.0f, 0.0f, 8.0f, 8.0f);
            noiseMapBuilder.SeamlessEnabled = true;

            textureDisplay = new TextureDisplay(this);
            const float scale = 0.2f;
            textureDisplay.TextureWidth = (int) (WindowWidth * scale);
            textureDisplay.TextureHeight = (int) (WindowHeight * scale);
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
            waveFilter.TextureSampler = SamplerState.LinearWrap;

            heightToNormalConverter = new HeightToNormalConverter(Device);
            heightToNormalConverter.HeightMapSampler = SamplerState.LinearWrap;

            fullScreenQuad = new FullScreenQuad(context);

            fluidEffect = new FluidEffect(Device);
            fluidEffect.FluidColorBlendDistance = 50.0f;
            fluidEffect.FluidColorBlendEnabled = true;

            clippingEffect = new ClippingEffect(Device);
            clippingEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            clippingEffect.EnableDefaultLighting();

            fluidVertexBuffer = Device.CreateVertexBuffer();
            fluidVertexBuffer.Initialize(FluidVertices);

            fluidIndexBuffer = Device.CreateIndexBuffer();
            fluidIndexBuffer.Initialize(FluidIndices);

            cubeMesh = new CubeMesh(context, 20);
            sphereMesh = new SphereMesh(context, 20, 32);
            cylinderMesh = new CylinderMesh(context, 80, 20, 32);
            squareMesh = new SquareMesh(context, 400);

            // ノイズによる波ハイトマップの生成。
            noiseMapBuilder.Build();

            noiseWaveHeightMap = Device.CreateTexture2D();
            noiseWaveHeightMap.Width = noiseMap.Width;
            noiseWaveHeightMap.Height = noiseMap.Height;
            noiseWaveHeightMap.Format = SurfaceFormat.Single;
            noiseWaveHeightMap.Initialize();
            noiseWaveHeightMap.SetData(context, noiseMap.Values);
        }

        Vector2 normalOffset0;
        Vector2 normalOffset1;

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            float elapsedTime = (float) gameTime.ElapsedGameTime.TotalSeconds;

            // 時間経過に応じて流体面のテクスチャを移動。
            normalOffset0.X += elapsedTime * 1.0f;
            normalOffset0.Y += elapsedTime * 1.0f;
            normalOffset0.X %= 1.0f;
            normalOffset0.Y %= 1.0f;
            normalOffset1.X += elapsedTime * 0.6f;
            normalOffset1.Y += elapsedTime * 0.6f;
            //normalOffset1.X += elapsedTime * (float) Random.NextDouble();
            //normalOffset1.Y += elapsedTime * (float) Random.NextDouble();
            normalOffset1.X %= 1.0f;
            normalOffset1.Y %= 1.0f;

            fluidEffect.Offset0 = normalOffset0;
            fluidEffect.Offset1 = normalOffset1;

            // 一定時間が経過したらランダムな新しい波を追加。
            elapsedNewWaveTime += elapsedTime;
            if (newWaveInterval <= elapsedNewWaveTime)
            {
                var position = new Vector2((float) Random.NextDouble(), (float) Random.NextDouble());
                var radius = Random.Next(1, 20) / 128.0f;
                var velocity = (float) Random.NextDouble() * 0.05f;

                waveFilter.AddWave(position, radius, velocity);

                elapsedNewWaveTime -= newWaveInterval;
            }

            // 表示カメラが流体面の表側にあるか否か。
            eyeInFront = (0 <= fluidFrontPlane.DotCoordinate(camera.Position));

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

            // 波マップを描画。
            CreateWaveMap();

            // 波マップから法線マップを生成。
            CreateWaveNormalMap();

            // 反射シーンを描画。
            CreateReflectionMap();

            // 屈折シーンを描画。
            CreateRefractionMap();

            // シーンを描画。
            DrawScene();

            // HUD のテキストを描画。
            if (hudVisible)
                DrawOverlayText();

            textureDisplay.Textures.Add(noiseWaveHeightMap);

            base.Draw(gameTime);
        }

        void CreateWaveMap()
        {
            waveRenderTargetChain.Next();

            context.SetRenderTarget(waveRenderTargetChain.Current);
            context.Clear(Vector4.Zero);

            context.DepthStencilState = DepthStencilState.None;

            waveFilter.Texture = waveRenderTargetChain.Last;
            waveFilter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            //heightToNormalConverter.HeightMap = waveRenderTargetChain.Current;
            heightToNormalConverter.HeightMap = noiseWaveHeightMap;

            textureDisplay.Textures.Add(waveRenderTargetChain.Current);
        }

        void CreateWaveNormalMap()
        {
            context.SetRenderTarget(waveNormalMapRenderTarget);
            context.Clear(Vector3.Up.ToVector4());

            context.DepthStencilState = DepthStencilState.None;

            heightToNormalConverter.HeightMapSampler = SamplerState.LinearWrap;
            heightToNormalConverter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            context.SetRenderTarget(null);

            fluidEffect.NormalMap = waveNormalMapRenderTarget;

            textureDisplay.Textures.Add(waveNormalMapRenderTarget);
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
            //var clipPlane = (eyeInFront) ? fluidFrontPlane : fluidBackPlane;
            var clipPlane = fluidFrontPlane;

            if (eyeInFront)
            {
                CreateReflectionView(ref camera.View, ref clipPlane, out reflectionView);
            }
            else
            {
                reflectionView = camera.View;
            }

            context.SetRenderTarget(reflectionSceneRenderTarget);
            context.Clear(Color.CornflowerBlue);

            clippingEffect.ClipPlane0 = clipPlane.ToVector4();

            DrawSceneWithoutFluid(clippingEffect, ref reflectionView);

            context.SetRenderTarget(null);

            fluidEffect.ReflectionMap = reflectionSceneRenderTarget;

            textureDisplay.Textures.Add(reflectionSceneRenderTarget);
        }

        void CreateRefractionMap()
        {
            context.SetRenderTarget(refractionSceneRenderTarget);
            context.Clear(Color.CornflowerBlue);

            var clipPlane = (eyeInFront) ? fluidBackPlane : fluidFrontPlane;
            clippingEffect.ClipPlane0 = clipPlane.ToVector4();

            DrawSceneWithoutFluid(clippingEffect, ref camera.View);

            context.SetRenderTarget(null);

            fluidEffect.RefractionMap = refractionSceneRenderTarget;

            textureDisplay.Textures.Add(refractionSceneRenderTarget);
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

            if (eyeInFront)
            {
                fluidEffect.RefractiveIndex1 = FluidEffect.RefractiveIndexAir;
                fluidEffect.RefractiveIndex2 = FluidEffect.RefracticeIndexWater;
            }
            else
            {
                fluidEffect.RefractiveIndex1 = FluidEffect.RefracticeIndexWater;
                fluidEffect.RefractiveIndex2 = FluidEffect.RefractiveIndexAir;
            }

            fluidEffect.World = fluidWorld;
            fluidEffect.View = camera.View;
            fluidEffect.Projection = camera.Projection;
            fluidEffect.ReflectionView = reflectionView;
            fluidEffect.Apply(context);

            context.SetVertexBuffer(fluidVertexBuffer);
            context.IndexBuffer = fluidIndexBuffer;

            context.RasterizerState = RasterizerState.CullNone;
            context.DrawIndexed(fluidIndexBuffer.IndexCount);
            context.RasterizerState = null;

            DrawSceneWithoutFluid(basicEffect, ref camera.View);
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

            if (currentKeyboardState.IsKeyDown(Keys.PageUp))
            {
                fluidRoll += 0.005f;
                fluidRoll %= MathHelper.TwoPi;
            }

            if (currentKeyboardState.IsKeyDown(Keys.PageDown))
            {
                fluidRoll -= 0.005f;
                fluidRoll %= MathHelper.TwoPi;
            }

            Matrix fluidRotation;
            Matrix.CreateFromYawPitchRoll(0, 0, fluidRoll, out fluidRotation);

            Matrix.Multiply(ref fluidRotation, ref fluidTranslation, out fluidWorld);

            Plane.Transform(ref localFluidFrontPlane, ref fluidWorld, out fluidFrontPlane);
            Plane.Transform(ref localeFluidBackPlane, ref fluidWorld, out fluidBackPlane);

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
