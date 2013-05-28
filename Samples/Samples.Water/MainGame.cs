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
        enum FluidType
        {
            Ripple,
            Flow
        }

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

        /// <summary>
        /// Ripple 用の流体面の頂点バッファ。
        /// </summary>
        VertexBuffer rippleVertexBuffer;

        /// <summary>
        /// Flow 用の流体面の頂点バッファ。
        /// </summary>
        VertexBuffer flowVertexBuffer;

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

        SeamlessNoiseMap noiseHeightMap0 = new SeamlessNoiseMap(128, 128);

        SeamlessNoiseMap noiseHeightMap1 = new SeamlessNoiseMap(128, 128);

        NoiseMapBuilder noiseMapBuilder = new NoiseMapBuilder();

        Texture2D flowNormalMap0;

        Texture2D flowNormalMap1;

        FluidType fluidType = FluidType.Ripple;

        Vector2 normalOffset0;

        Vector2 normalOffset1;

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


            Vector3 v1 = FlowVertices[0].Position;
            Vector3 v2 = FlowVertices[1].Position;
            Vector3 v3 = FlowVertices[2].Position;

            Vector2 w1 = FlowVertices[0].TexCoord;
            Vector2 w2 = FlowVertices[1].TexCoord;
            Vector2 w3 = FlowVertices[2].TexCoord;

            float x1 = v2.X - v1.X;
            float x2 = v3.X - v1.X;

            float y1 = v2.Y - v1.Y;
            float y2 = v3.Y - v1.Y;

            float z1 = v2.Z - v1.Z;
            float z2 = v3.Z - v1.Z;

            float s1 = w2.X - w1.X;
            float s2 = w3.X - w1.X;

            float t1 = w2.Y - w1.Y;
            float t2 = w3.Y - w1.Y;

            float r = 1.0f / (s1 * t2 - s2 * t1);

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            Vector3 normal = Vector3.Up;

            Vector3 tangent = sdir - normal * Vector3.Dot(normal, sdir);
            tangent.Normalize();

            float tangentdir = (Vector3.Dot(Vector3.Cross(normal, sdir), tdir) >= 0.0f) ? 1.0f : -1.0f;

            Vector3 binormal = Vector3.Cross(normal, tangent) * tangentdir;

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

            rippleRenderTargetChain = new RenderTargetChain(Device);
            rippleRenderTargetChain.Width = 512;
            rippleRenderTargetChain.Height = 512;
            rippleRenderTargetChain.Format = SurfaceFormat.Vector2;

            rippleNormalMapRenderTarget = Device.CreateRenderTarget();
            rippleNormalMapRenderTarget.Width = rippleRenderTargetChain.Width;
            rippleNormalMapRenderTarget.Height = rippleRenderTargetChain.Height;
            rippleNormalMapRenderTarget.Format = SurfaceFormat.Vector4;
            rippleNormalMapRenderTarget.Initialize();

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

            fluidRippleFilter = new FluidRippleFilter(Device);
            fluidRippleFilter.TextureSampler = SamplerState.LinearClamp;

            heightToNormalConverter = new HeightToNormalConverter(Device);
            heightToNormalConverter.HeightMapSampler = SamplerState.LinearClamp;

            fullScreenQuad = new FullScreenQuad(context);

            fluidEffect = new FluidEffect(Device);
            fluidEffect.FluidColorBlendDistance = 50.0f;
            fluidEffect.FluidColorBlendEnabled = true;

            clippingEffect = new ClippingEffect(Device);
            clippingEffect.AmbientLightColor = new Vector3(0.15f, 0.15f, 0.15f);
            clippingEffect.EnableDefaultLighting();

            rippleVertexBuffer = Device.CreateVertexBuffer();
            rippleVertexBuffer.Initialize(RippleVertices);
            flowVertexBuffer = Device.CreateVertexBuffer();
            flowVertexBuffer.Initialize(FlowVertices);

            fluidIndexBuffer = Device.CreateIndexBuffer();
            fluidIndexBuffer.Initialize(FluidIndices);

            cubeMesh = new CubeMesh(context, 20);
            sphereMesh = new SphereMesh(context, 20, 32);
            cylinderMesh = new CylinderMesh(context, 80, 20, 32);
            squareMesh = new SquareMesh(context, 400);

            flowNormalMap0 = CreateNoiseNormalMap(noiseHeightMap0, new Bounds(0, 0, 5, 5));
            flowNormalMap1 = CreateNoiseNormalMap(noiseHeightMap1, new Bounds(2, 2, 7, 7));
        }

        Texture2D CreateNoiseNormalMap(SeamlessNoiseMap noiseHeightMap, Bounds bounds)
        {
            // ノイズによる流体ハイトマップの生成。
            noiseMapBuilder.Bounds = bounds;
            noiseMapBuilder.Build(noiseHeightMap.Values, noiseHeightMap.Width, noiseHeightMap.Height);

            // 流体ハイトマップから法線マップを生成。
            Vector4[] noiseNormals = new Vector4[noiseHeightMap.Values.Length];
            for (int y = 0; y < noiseHeightMap.Height; y++)
            {
                for (int x = 0; x < noiseHeightMap.Width; x++)
                {
                    float h0 = noiseHeightMap[x - 1, y    ];
                    float h1 = noiseHeightMap[x + 1, y    ];
                    float h2 = noiseHeightMap[x,     y - 1];
                    float h3 = noiseHeightMap[x,     y + 1];

                    Vector3 u = new Vector3(1.0f, (h1 - h0) * 0.5f, 0.0f);
                    Vector3 v = new Vector3(0.0f, (h3 - h2) * 0.5f, 1.0f);

                    Vector3 normal;
                    Vector3.Cross(ref v, ref u, out normal);
                    normal.Normalize();

                    noiseNormals[x + y * noiseHeightMap.Width] = new Vector4(normal, 0.0f);
                }
            }

            var texture = Device.CreateTexture2D();
            texture.Width = noiseHeightMap.Width;
            texture.Height = noiseHeightMap.Height;
            texture.Format = SurfaceFormat.Vector4;
            texture.Initialize();
            texture.SetData(context, noiseNormals);

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
                normalOffset0.X += elapsedTime * 1.0f;
                normalOffset0.Y += elapsedTime * 1.0f;
                normalOffset0.X %= 1.0f;
                normalOffset0.Y %= 1.0f;
                normalOffset1.X += elapsedTime * 0.6f;
                normalOffset1.Y += elapsedTime * 0.7f;
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
            context.IndexBuffer = fluidIndexBuffer;

            context.RasterizerState = RasterizerState.CullNone;
            context.BlendState = BlendState.AlphaBlend;
            context.DrawIndexed(fluidIndexBuffer.IndexCount);
            context.RasterizerState = null;
            context.BlendState = null;

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
