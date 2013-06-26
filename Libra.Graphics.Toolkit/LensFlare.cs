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

        static readonly BlendState ColorWriteDisable = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None
        };

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

        public DeviceContext Context { get; private set; }

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

        public LensFlare(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Context = context;

            spriteBatch = new SpriteBatch(context);

            basicEffect = new BasicEffect(context.Device);
            basicEffect.View = Matrix.Identity;
            basicEffect.VertexColorEnabled = true;

            vertexBuffer = context.Device.CreateVertexBuffer();
            vertexBuffer.Initialize(VertexPositionColor.VertexDeclaration, 4);

            occlusionQuery = context.Device.CreateOcclusionQuery();
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

            // 参考にした XNA Lens Flare サンプルでは調整無しだが、それは near = 0.1 であるが故であり、
            // それなりの距離 (near = 1 など) を置くと、単位ベクトルであるライト方向を射影した場合に
            // 射影空間の外に出てしまう (0 から near の間に射影されてしまう)。
            // このため、near だけカメラ奥へ押し出した後に射影する (射影空間に収まる位置で射影する)。
            var lightPosition = -lightDirection;
            lightPosition.Z -= projection.PerspectiveNearClipDistance;

            var viewport = Context.Viewport;
            var projectedPosition = viewport.Project(lightPosition, projection, infiniteView, Matrix.Identity);

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
                vertexBuffer.SetData(Context, vertices);

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

            var viewport = Context.Viewport;

            Matrix world;
            Matrix.CreateTranslation(screenLightPosition.X, screenLightPosition.Y, 0, out world);

            Matrix projection;
            Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1, out projection);

            Context.BlendState = ColorWriteDisable;
            Context.DepthStencilState = DepthStencilState.DepthRead;
            Context.RasterizerState = RasterizerState.CullNone;
            Context.SetVertexBuffer(vertexBuffer);
            Context.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            basicEffect.World = world;
            basicEffect.Projection = projection;
            basicEffect.Apply(Context);

            occlusionQuery.Begin(Context);

            // TriangleStrip で 3 * 2 の三角形を描画。
            Context.Draw(6);

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

            spriteBatch.Begin();
            spriteBatch.Draw(GlowTexture, screenLightPosition, null, color, 0, glowOrigin, glowScale);
            spriteBatch.End();
        }

        void DrawFlares()
        {
            if (lightBehindCamera || occlusionAlpha <= 0)
                return;

            var viewport = Context.Viewport;
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
            Context.BlendState = null;
            Context.DepthStencilState = null;
            Context.RasterizerState = null;
            Context.PixelShaderSamplers[0] = null;
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
