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
        #region FluidType

        enum FluidType
        {
            Ripple,
            Flow
        }

        #endregion

        #region SeamlessNoiseMap

        public sealed class SeamlessNoiseMap
        {
            public readonly float[] Values;

            int width;

            int height;

            public int Width
            {
                get { return width; }
            }

            public int Height
            {
                get { return height; }
            }

            public float this[int x, int y]
            {
                get
                {
                    x %= width;
                    y %= height;

                    if (x < 0) x += width;
                    if (width <= x) x -= width;
                    if (y < 0) y += height;
                    if (height <= y) y -= height;

                    return Values[x + y * width];
                }
                set
                {
                    x %= width;
                    y %= height;

                    if (x < 0) x += width;
                    if (width <= x) x -= width;
                    if (y < 0) y += height;
                    if (height <= y) y -= height;

                    Values[x + y * width] = value;
                }
            }

            public SeamlessNoiseMap(int width, int height)
            {
                if (width < 1) throw new ArgumentOutOfRangeException("width");
                if (height < 1) throw new ArgumentOutOfRangeException("height");

                this.width = width;
                this.height = height;

                Values = new float[width * height];
            }

            public void Clear()
            {
                Array.Clear(Values, 0, Values.Length);
            }
        }

        #endregion

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

        // Ripple 用の流体面メッシュの頂点。
        static readonly VertexPositionTexture[] RippleVertices =
        {
            new VertexPositionTexture(new Vector3(-200, 0,  200), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-200, 0, -200), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 200, 0, -200), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3( 200, 0,  200), new Vector2(1, 1)),
        };

        // Flow 用の流体面メッシュの頂点。
        static readonly VertexPositionTexture[] FlowVertices =
        {
            new VertexPositionTexture(new Vector3(-200, 0,  200), new Vector2(0, 8)),
            new VertexPositionTexture(new Vector3(-200, 0, -200), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3( 200, 0, -200), new Vector2(8, 0)),
            new VertexPositionTexture(new Vector3( 200, 0,  200), new Vector2(8, 8)),
        };

        static readonly VertexPositionNormalTexture[] CloudVertices =
        {
            new VertexPositionNormalTexture(new Vector3(-1500, 0,  1500), Vector3.Down, new Vector2(0, 4)),
            new VertexPositionNormalTexture(new Vector3(-1500, 0, -1500), Vector3.Down, new Vector2(0, 0)),
            new VertexPositionNormalTexture(new Vector3( 1500, 0, -1500), Vector3.Down, new Vector2(4, 0)),
            new VertexPositionNormalTexture(new Vector3( 1500, 0,  1500), Vector3.Down, new Vector2(4, 4)),
        };

        // 矩形メッシュのインデックス。
        static readonly ushort[] QuadIndices =
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
        RenderTarget rippleNormalMapRenderTarget;

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
        RenderTargetChain rippleRenderTargetChain;

        /// <summary>
        /// FullScreenQuad。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// メッシュ描画のための基礎エフェクト。
        /// </summary>
        BasicEffect basicEffect;

        /// <summary>
        /// 波紋生成フィルタ。
        /// </summary>
        FluidRippleFilter fluidRippleFilter;

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

        Postprocess cloudPostprocess;

        CloudLayerFilter cloudLayerFilter;

        CloudLayerFilterPass cloudLayerFilterPass1;

        CloudLayerFilterPass cloudLayerFilterPass2;

        /// <summary>
        /// Ripple 用の流体面の頂点バッファ。
        /// </summary>
        VertexBuffer rippleVertexBuffer;

        /// <summary>
        /// Flow 用の流体面の頂点バッファ。
        /// </summary>
        VertexBuffer flowVertexBuffer;

        VertexBuffer cloudVertexBuffer;

        /// <summary>
        /// 矩形のインデックス バッファ。
        /// </summary>
        IndexBuffer quadIndexBuffer;

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

        Matrix cloudWorld = Matrix.CreateTranslation(0, 100, 0);

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

        INoiseSource fluidHeightNoise;

        INoiseSource cloudDensityNoise;

        SeamlessNoiseMap fluidNoiseBuffer0 = new SeamlessNoiseMap(128, 128);

        SeamlessNoiseMap fluidNoiseBuffer1 = new SeamlessNoiseMap(128, 128);

        SeamlessNoiseMap cloudNoiseBuffer = new SeamlessNoiseMap(128, 128);

        NoiseMapBuilder noiseMapBuilder = new NoiseMapBuilder();

        Texture2D flowNormalMap0;

        Texture2D flowNormalMap1;

        Texture2D cloudMap;

        ShaderResourceView finalCloudMap;

        FluidType fluidType = FluidType.Flow;

        Vector2 normalOffset0;

        Vector2 normalOffset1;

        Vector3 ambientLightColor = new Vector3(0.15f, 0.15f, 0.15f);

        Vector3 fluidDiffuseColor = new Vector3(0.5f, 0.85f, 0.815f);

        Vector3 fluidSpecularColor = new Vector3(0.8f, 0.8f, 0.8f);

        float fluidSpecularPower = 32;

        Vector3 lightDirection = new Vector3(0, -1, 1);

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

            lightDirection.Normalize();

            fluidWorld = fluidTranslation;
            Plane.Transform(ref localFluidFrontPlane, ref fluidWorld, out fluidFrontPlane);
            Plane.Transform(ref localeFluidBackPlane, ref fluidWorld, out fluidBackPlane);

            // ノイズ設定。
            fluidHeightNoise = new ScaleBias
            {
                Scale = 2.0f,
                Bias = -1.0f,
                Source = new SumFractal
                {
                    Source = new Perlin { Seed = 100 }
                }
            };

            cloudDensityNoise = new ScaleBias
            {
                //Bias = 1.0f,
                //Source = new Billow
                //{
                //    Source = new Perlin { Seed = 200 }
                //}
                Bias = 0.0f,
                Source = new SumFractal
                {
                    Source = new Perlin { Seed = 200 }
                }
            };

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

            rippleNormalMapRenderTarget = Device.CreateRenderTarget();
            rippleNormalMapRenderTarget.Width = 512;
            rippleNormalMapRenderTarget.Height = 512;
            rippleNormalMapRenderTarget.Format = SurfaceFormat.Vector4;
            rippleNormalMapRenderTarget.Initialize();

            rippleRenderTargetChain = new RenderTargetChain(Device);
            rippleRenderTargetChain.Width = rippleNormalMapRenderTarget.Width;
            rippleRenderTargetChain.Height = rippleNormalMapRenderTarget.Height;
            rippleRenderTargetChain.Format = SurfaceFormat.Vector2;

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
            basicEffect.AmbientLightColor = ambientLightColor;
            basicEffect.PerPixelLighting = true;
            basicEffect.EnableDefaultLighting();
            basicEffect.DirectionalLights[0].Direction = lightDirection;

            fluidRippleFilter = new FluidRippleFilter(Device);
            fluidRippleFilter.TextureSampler = SamplerState.LinearClamp;

            heightToNormalConverter = new HeightToNormalConverter(Device);
            heightToNormalConverter.HeightMapSampler = SamplerState.LinearClamp;

            fullScreenQuad = new FullScreenQuad(context);

            fluidEffect = new FluidEffect(Device);
            fluidEffect.RippleScale = 0.05f;
            fluidEffect.AmbientLightColor = ambientLightColor;
            fluidEffect.DiffuseColor = fluidDiffuseColor;
            fluidEffect.SpecularColor = fluidSpecularColor;
            fluidEffect.SpecularPower = fluidSpecularPower;
            fluidEffect.LightDirection = lightDirection;

            clippingEffect = new ClippingEffect(Device);
            clippingEffect.AmbientLightColor = ambientLightColor;
            clippingEffect.EnableDefaultLighting();

            cloudPostprocess = new Postprocess(context);
            cloudPostprocess.Width = cloudNoiseBuffer.Width;
            cloudPostprocess.Height = cloudNoiseBuffer.Height;

            cloudLayerFilter = new CloudLayerFilter(Device);
            cloudLayerFilterPass1 = new CloudLayerFilterPass(cloudLayerFilter);
            cloudLayerFilterPass2 = new CloudLayerFilterPass(cloudLayerFilter);
            cloudLayerFilterPass1.PixelOffset = new Vector2(2.5f, 2.5f);
            cloudLayerFilterPass2.PixelOffset = new Vector2(4.5f, 4.5f);
            cloudLayerFilterPass1.TextureSampler = SamplerState.LinearWrap;
            cloudLayerFilterPass2.TextureSampler = SamplerState.LinearWrap;
            cloudPostprocess.Filters.Add(cloudLayerFilterPass1);
            cloudPostprocess.Filters.Add(cloudLayerFilterPass2);

            rippleVertexBuffer = Device.CreateVertexBuffer();
            rippleVertexBuffer.Initialize(RippleVertices);
            flowVertexBuffer = Device.CreateVertexBuffer();
            flowVertexBuffer.Initialize(FlowVertices);

            cloudVertexBuffer = Device.CreateVertexBuffer();
            cloudVertexBuffer.Initialize(CloudVertices);

            quadIndexBuffer = Device.CreateIndexBuffer();
            quadIndexBuffer.Initialize(QuadIndices);

            cubeMesh = new CubeMesh(context, 20);
            sphereMesh = new SphereMesh(context, 20, 32);
            cylinderMesh = new CylinderMesh(context, 80, 20, 32);
            squareMesh = new SquareMesh(context, 400);

            flowNormalMap0 = CreateFluidNormalMap(fluidNoiseBuffer0, new Bounds(0, 0, 5, 5));
            flowNormalMap1 = CreateFluidNormalMap(fluidNoiseBuffer1, new Bounds(2, 2, 7, 7));
            cloudMap = CreateCloudMap(cloudNoiseBuffer, new Bounds(0, 0, 8, 8)); 
        }

        Texture2D CreateFluidNormalMap(SeamlessNoiseMap noiseBuffer, Bounds bounds)
        {
            // ノイズによる流体ハイトマップの生成。
            noiseMapBuilder.Source = fluidHeightNoise;
            noiseMapBuilder.Bounds = bounds;
            noiseMapBuilder.SeamlessEnabled = true;
            noiseMapBuilder.Build(noiseBuffer.Values, noiseBuffer.Width, noiseBuffer.Height);

            // 流体ハイトマップから法線マップを生成。
            Vector4[] noiseNormals = new Vector4[noiseBuffer.Values.Length];
            for (int y = 0; y < noiseBuffer.Height; y++)
            {
                for (int x = 0; x < noiseBuffer.Width; x++)
                {
                    float h0 = noiseBuffer[x - 1, y    ];
                    float h1 = noiseBuffer[x + 1, y    ];
                    float h2 = noiseBuffer[x,     y - 1];
                    float h3 = noiseBuffer[x,     y + 1];

                    Vector3 u = new Vector3(1.0f, (h1 - h0) * 0.5f, 0.0f);
                    Vector3 v = new Vector3(0.0f, (h3 - h2) * 0.5f, 1.0f);

                    Vector3 normal;
                    Vector3.Cross(ref v, ref u, out normal);
                    normal.Normalize();

                    noiseNormals[x + y * noiseBuffer.Width] = new Vector4(normal, 0.0f);
                }
            }

            var texture = Device.CreateTexture2D();
            texture.Width = noiseBuffer.Width;
            texture.Height = noiseBuffer.Height;
            texture.Format = SurfaceFormat.Vector4;
            texture.Initialize();
            texture.SetData(context, noiseNormals);

            return texture;
        }

        Texture2D CreateCloudMap(SeamlessNoiseMap noiseBuffer, Bounds bounds)
        {
            // ノイズによる雲マップの生成。
            noiseMapBuilder.Source = cloudDensityNoise;
            noiseMapBuilder.Bounds = bounds;
            noiseMapBuilder.SeamlessEnabled = true;
            noiseMapBuilder.Build(noiseBuffer.Values, noiseBuffer.Width, noiseBuffer.Height);

            Vector3 cloudColor = Vector3.One;

            Color[] colors = new Color[noiseBuffer.Values.Length];
            for (int y = 0; y < noiseBuffer.Height; y++)
            {
                for (int x = 0; x < noiseBuffer.Width; x++)
                {
                    var index = x + y * noiseBuffer.Width;

                    float density = MathHelper.Clamp(noiseBuffer.Values[index], 0, 1);

                    // 乗算済みアルファで生成。
                    colors[index] = new Color(
                        cloudColor.X * density,
                        cloudColor.Y * density,
                        cloudColor.Z * density,
                        density);
                }
            }

            var texture = Device.CreateTexture2D();
            texture.Width = noiseBuffer.Width;
            texture.Height = noiseBuffer.Height;
            texture.Initialize();
            texture.SetData(context, colors);

            return texture;
        }

        protected override void Update(GameTime gameTime)
        {
            // キーボード状態およびジョイスティック状態のハンドリング。
            HandleInput(gameTime);

            // 表示カメラの更新。
            UpdateCamera(gameTime);

            float elapsedTime = (float) gameTime.ElapsedGameTime.TotalSeconds;

            if (fluidType == FluidType.Flow)
            {
                // 時間経過に応じて流体面のテクスチャを移動。
                normalOffset0.X += elapsedTime * 0.1f;
                normalOffset0.Y += elapsedTime * 0.1f;
                normalOffset0.X %= 1.0f;
                normalOffset0.Y %= 1.0f;
                normalOffset1.X += elapsedTime * 0.06f;
                normalOffset1.Y += elapsedTime * 0.07f;
                normalOffset1.X %= 1.0f;
                normalOffset1.Y %= 1.0f;

                fluidEffect.Offset0 = normalOffset0;
                fluidEffect.Offset1 = normalOffset1;
            }

            if (fluidType == FluidType.Ripple)
            {
                // 一定時間が経過したらランダムな新しい波を追加。
                elapsedNewWaveTime += elapsedTime;
                if (newWaveInterval <= elapsedNewWaveTime)
                {
                    var position = new Vector2((float) Random.NextDouble(), (float) Random.NextDouble());
                    var radius = Random.Next(1, 20) / 512.0f;
                    var velocity = (float) Random.NextDouble() * 0.05f;

                    fluidRippleFilter.AddRipple(position, radius, velocity);

                    elapsedNewWaveTime -= newWaveInterval;
                }
            }

            //cloudLayerFilterPass1.PixelOffset += Vector2.One * 0.1f;
            //cloudLayerFilterPass2.PixelOffset += Vector2.One * 0.2f;
            //cloudLayerFilterPass3.PixelOffset += Vector2.One * 0.3f;

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

            if (fluidType == FluidType.Ripple)
            {
                // 波紋マップを描画。
                CreateFluidRippleMap();

                // 波紋マップから法線マップを生成。
                CreateFluidNormalMap();

                // 波紋マップから生成された法線マップを設定。
                fluidEffect.NormalMap0 = rippleNormalMapRenderTarget;
                fluidEffect.NormalMap1 = rippleNormalMapRenderTarget;
            }
            else if (fluidType == FluidType.Flow)
            {
                fluidEffect.NormalMap0 = flowNormalMap0;
                fluidEffect.NormalMap1 = flowNormalMap1;
                fluidEffect.NormalMapSampler = SamplerState.LinearWrap;

                textureDisplay.Textures.Add(flowNormalMap0);
                textureDisplay.Textures.Add(flowNormalMap1);
            }

            textureDisplay.Textures.Add(cloudMap);

            CreateFinalCloudMap();

            // 反射シーンを描画。
            CreateReflectionMap();

            // 屈折シーンを描画。
            CreateRefractionMap();

            // シーンを描画。
            DrawScene();

            // HUD のテキストを描画。
            if (hudVisible)
                DrawOverlayText();

            base.Draw(gameTime);
        }

        void CreateFinalCloudMap()
        {
            //finalCloudMap = cloudPostprocess.Draw(cloudMap);

            //textureDisplay.Textures.Add(finalCloudMap);

            finalCloudMap = cloudMap;
        }

        void CreateFluidRippleMap()
        {
            rippleRenderTargetChain.Next();

            context.SetRenderTarget(rippleRenderTargetChain.Current);
            context.Clear(Vector4.Zero);

            context.DepthStencilState = DepthStencilState.None;

            fluidRippleFilter.Texture = rippleRenderTargetChain.Last;
            fluidRippleFilter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            heightToNormalConverter.HeightMap = rippleRenderTargetChain.Current;

            textureDisplay.Textures.Add(rippleRenderTargetChain.Current);
        }

        void CreateFluidNormalMap()
        {
            context.SetRenderTarget(rippleNormalMapRenderTarget);
            context.Clear(Vector3.Up.ToVector4());

            context.DepthStencilState = DepthStencilState.None;

            heightToNormalConverter.HeightMapSampler = SamplerState.LinearWrap;
            heightToNormalConverter.Apply(context);

            fullScreenQuad.Draw();

            context.SetRenderTarget(null);

            context.DepthStencilState = null;
            context.PixelShaderResources[0] = null;

            context.SetRenderTarget(null);

            textureDisplay.Textures.Add(rippleNormalMapRenderTarget);
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

            switch (fluidType)
            {
                case FluidType.Ripple:
                    context.SetVertexBuffer(rippleVertexBuffer);
                    break;
                case FluidType.Flow:
                    context.SetVertexBuffer(flowVertexBuffer);
                    break;
            }
            context.IndexBuffer = quadIndexBuffer;

            context.RasterizerState = RasterizerState.CullNone;
            context.BlendState = BlendState.AlphaBlend;
            context.DrawIndexed(quadIndexBuffer.IndexCount);
            context.RasterizerState = null;
            context.BlendState = null;

            basicEffect.LightingEnabled = true;
            DrawSceneWithoutFluid(basicEffect, ref camera.View);

            // TODO
            context.SetVertexBuffer(cloudVertexBuffer);
            context.IndexBuffer = quadIndexBuffer;
            context.RasterizerState = RasterizerState.CullNone;
            context.BlendState = BlendState.AlphaBlend;
            basicEffect.World = cloudWorld;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.Texture = finalCloudMap;
            basicEffect.TextureEnabled = true;
            basicEffect.DiffuseColor = Vector3.One;
            basicEffect.LightingEnabled = false;
            basicEffect.Apply(context);
            context.PixelShaderSamplers[0] = SamplerState.LinearWrap;
            context.DrawIndexed(quadIndexBuffer.IndexCount);
            basicEffect.Texture = null;
            basicEffect.TextureEnabled = false;
            context.RasterizerState = null;
            context.BlendState = null;
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
