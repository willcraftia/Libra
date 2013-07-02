#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LensFlare : IDisposable
    {
        #region Flare

        public struct Flare
        {
            public float Position;

            public float Scale;

            public Color Color;

            public ShaderResourceView Texture;

            public Flare(float position, float scale, Color color, ShaderResourceView texture)
            {
                Position = position;
                Scale = scale;
                Color = color;
                Texture = texture;
            }
        }

        #endregion

        #region FlareCollection

        public sealed class FlareCollection : Collection<Flare>
        {
            internal FlareCollection() { }
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            VertexBuffer    = (1 << 0),
            Glow            = (1 << 1)
        }

        #endregion

        SpriteBatch spriteBatch;

        BasicEffect basicEffect;

        VertexBuffer vertexBuffer;

        VertexPositionColor[] vertices;

        OcclusionQuery occlusionQuery;

        float querySize;

        bool occlusionQueryActive;

        float occlusionAlpha;

        Matrix view;

        Matrix projection;

        Vector3 lightDirection;

        Vector2 screenLightPosition;

        ShaderResourceView glowTexture;

        float glowSize;

        Vector2 glowOrigin;

        float glowScale;

        bool lightBehindCamera;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float QuerySize
        {
            get { return querySize; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                querySize = value;

                dirtyFlags |= DirtyFlags.VertexBuffer;
            }
        }

        public FlareCollection Flares { get; private set; }

        public ShaderResourceView GlowTexture
        {
            get { return glowTexture; }
            set
            {
                if (glowTexture == value) return;

                glowTexture = value;

                dirtyFlags |= DirtyFlags.Glow;
            }
        }

        public float GlowSize
        {
            get { return glowSize; }
            set
            {
                if (glowSize == value) return;

                glowSize = value;

                dirtyFlags |= DirtyFlags.Glow;
            }
        }

        public Matrix View
        {
            get { return view; }
            set { view = value; }
        }

        public Matrix Projection
        {
            get { return projection; }
            set { projection = value; }
        }

        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set
            {
                lightDirection = value;
                lightDirection.Normalize();
            }
        }

        public bool Enabled { get; set; }

        public LensFlare(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            spriteBatch = new SpriteBatch(deviceContext);

            basicEffect = new BasicEffect(deviceContext);
            basicEffect.View = Matrix.Identity;
            basicEffect.VertexColorEnabled = true;

            vertexBuffer = deviceContext.Device.CreateVertexBuffer();
            vertexBuffer.Initialize(VertexPositionColor.VertexDeclaration, 4);

            occlusionQuery = deviceContext.Device.CreateOcclusionQuery();
            occlusionQuery.Initialize();

            vertices = new VertexPositionColor[4];
            querySize = 100;
            glowSize = 400;
            Flares = new FlareCollection();

            dirtyFlags = DirtyFlags.VertexBuffer;

            Enabled = true;
        }

        public void Draw()
        {
            if (!Enabled)
                return;

            SetVertices();

            var infiniteView = view;
            infiniteView.Translation = Vector3.Zero;

            // 参考にした XNA Lens Flare サンプルでは調整無しだが、
            // それは near = 0.1 であるが故であり、
            // それなりの距離 (near = 1 など) を置くと、
            // 単位ベクトルであるライト方向の射影が射影領域の外に出てしまう (0 から near の間に射影されてしまう)。
            // このため、常にカメラの外にライトがあると見なされ、レンズ フレアは描画されない。
            // そこで、near = 0.001 とした射影行列を構築し、これに基づいてライトの射影を行う。
            // near = 0 とした場合、真逆を向いた場合にもライトが射影領域に入ってしまう点に注意。

            var lightPosition = -lightDirection;

            // 射影行列から情報を抽出。
            float fov = projection.PerspectiveFieldOfView;
            float aspectRatio = projection.PerspectiveAspectRatio;
            float far = projection.PerspectiveFarClipDistance;

            // near = 0.001 の射影行列を再構築。
            Matrix localProjection;
            Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, 0.001f, far, out localProjection);

            // near = 0.001 射影行列でライト位置をスクリーン座標へ射影。
            var viewport = DeviceContext.Viewport;
            var projectedPosition = viewport.Project(lightPosition, localProjection, infiniteView, Matrix.Identity);

            if (projectedPosition.Z < 0 || 1 < projectedPosition.Z)
            {
                lightBehindCamera = true;
                return;
            }

            screenLightPosition = new Vector2(projectedPosition.X, projectedPosition.Y);
            lightBehindCamera = false;

            UpdateOcclusion();

            DrawGlow();
            DrawFlares();

            RestoreRenderStates();
        }
        
        void SetVertices()
        {
            if ((dirtyFlags & DirtyFlags.VertexBuffer) != 0)
            {
                vertices[0].Position = new Vector3(-querySize * 0.5f, -querySize * 0.5f, -1.0f);
                vertices[1].Position = new Vector3( querySize * 0.5f, -querySize * 0.5f, -1.0f);
                vertices[2].Position = new Vector3(-querySize * 0.5f,  querySize * 0.5f, -1.0f);
                vertices[3].Position = new Vector3( querySize * 0.5f,  querySize * 0.5f, -1.0f);

                vertexBuffer.SetData(DeviceContext, vertices);

                dirtyFlags &= ~DirtyFlags.VertexBuffer;
            }
        }

        void UpdateOcclusion()
        {
            if (lightBehindCamera)
                return;

            if (occlusionQueryActive)
            {
                if (!occlusionQuery.IsComplete)
                    return;

                float queryArea = querySize * querySize;
                occlusionAlpha = Math.Min(occlusionQuery.PixelCount / queryArea, 1);
            }

            var viewport = DeviceContext.Viewport;

            Matrix world;
            Matrix.CreateTranslation(screenLightPosition.X, screenLightPosition.Y, 0, out world);

            Matrix projection;
            Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1, out projection);

            // レンダ ターゲットには書き込まない。
            DeviceContext.BlendState = BlendState.ColorWriteDisable;
            // 遠クリップ面にクエリのためのメッシュを描画するため、
            // LessEqual でなければ描画されない。
            DeviceContext.DepthStencilState = DepthStencilState.DepthReadLessEqual;
            DeviceContext.SetVertexBuffer(vertexBuffer);
            DeviceContext.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            basicEffect.World = world;
            basicEffect.Projection = projection;
            basicEffect.Apply();

            occlusionQuery.Begin(DeviceContext);

            // TriangleStrip の場合でも単に利用する頂点数を指定するのみ。
            DeviceContext.Draw(4);

            occlusionQuery.End();

            occlusionQueryActive = true;
        }

        void DrawGlow()
        {
            if (GlowTexture == null)
                return;
            
            if (lightBehindCamera || occlusionAlpha <= 0)
                return;

            if ((dirtyFlags & DirtyFlags.Glow) != 0)
            {
                var texture2d = GetTexture2D(GlowTexture);

                glowOrigin = new Vector2((float) texture2d.Width / 2.0f, (float) texture2d.Height / 2.0f);
                glowScale = glowSize * 2.0f / (float) texture2d.Width;

                dirtyFlags &= ~DirtyFlags.Glow;
            }

            var color = Color.White * occlusionAlpha;

            spriteBatch.Begin(SpriteSortMode.Immediate);
            spriteBatch.Draw(GlowTexture, screenLightPosition, null, color, 0, glowOrigin, glowScale);
            spriteBatch.End();
        }

        void DrawFlares()
        {
            if (lightBehindCamera || occlusionAlpha <= 0)
                return;

            var viewport = DeviceContext.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2.0f, viewport.Height / 2.0f);

            var flareVector = screenCenter - screenLightPosition;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            for (int i = 0; i < Flares.Count; i++)
            {
                var flare = Flares[i];
                var flarePosition = screenLightPosition + flareVector * flare.Position;

                var flareColor = flare.Color.ToVector4();
                flareColor.W *= occlusionAlpha;

                var texture2d = GetTexture2D(flare.Texture);

                var flareOrigin = new Vector2((float) texture2d.Width / 2.0f, (float) texture2d.Height / 2.0f);

                spriteBatch.Draw(flare.Texture, flarePosition, null, new Color(flareColor), 1, flareOrigin, flare.Scale);
            }

            spriteBatch.End();
        }

        void RestoreRenderStates()
        {
            DeviceContext.BlendState = null;
            DeviceContext.DepthStencilState = null;
            DeviceContext.RasterizerState = null;
            DeviceContext.PixelShaderSamplers[0] = null;
        }

        Texture2D GetTexture2D(ShaderResourceView texture)
        {
            var textureSource = texture.Resource as Texture2D;
            if (textureSource == null)
                throw new InvalidOperationException("ShaderResourceView is not for Texture2D.");

            return textureSource;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~LensFlare()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                basicEffect.Dispose();
                spriteBatch.Dispose();
                occlusionQuery.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
