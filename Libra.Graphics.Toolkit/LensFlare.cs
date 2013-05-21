#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LensFlare : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public IndexBuffer IndexBuffer { get; private set; }

            public SharedDeviceResource(Device device)
            {
                IndexBuffer = device.CreateIndexBuffer();
                IndexBuffer.Usage = ResourceUsage.Immutable;
                IndexBuffer.Initialize(Indices);
            }
        }

        #endregion

        #region Flare

        class Flare
        {
            public float Position;

            public float Scale;

            public Color Color;

            public int TextureIndex;

            public Texture2D Texture;

            public Flare(float position, float scale, Color color, int textureIndex)
            {
                Position = position;
                Scale = scale;
                Color = color;
                TextureIndex = textureIndex;
            }
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            VertexBuffer
        }

        #endregion

        const float GlowSize = 400;

        static readonly ushort[] Indices =
        {
            0, 1, 2,
            0, 2, 3
        };

        static readonly BlendState ColorWriteDisable = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None
        };

        SharedDeviceResource sharedDeviceResource;

        SpriteBatch spriteBatch;

        Texture2D glowSprite;

        Flare[] flares =
        {
            new Flare(-0.5f, 0.7f, new Color( 50,  25,  50), 0),
            new Flare( 0.3f, 0.4f, new Color(100, 255, 200), 0),
            new Flare( 1.2f, 1.0f, new Color(100,  50,  50), 0),
            new Flare( 1.5f, 1.5f, new Color( 50, 100,  50), 0),

            new Flare(-0.3f, 0.7f, new Color(200,  50,  50), 1),
            new Flare( 0.6f, 0.9f, new Color( 50, 100,  50), 1),
            new Flare( 0.7f, 0.4f, new Color( 50, 200, 200), 1),

            new Flare(-0.7f, 0.7f, new Color( 50, 100,  25), 2),
            new Flare( 0.0f, 0.6f, new Color( 25,  25,  25), 2),
            new Flare( 2.0f, 1.4f, new Color( 25,  50, 100), 2),
        };

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

        Vector2 lightPosition;

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

        public LensFlare(DeviceContext context, Texture2D glowSprite, Texture2D[] flareSprites)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (glowSprite == null) throw new ArgumentNullException("glowSprite");
            if (flareSprites == null) throw new ArgumentNullException("flareSprites");

            Context = context;
            this.glowSprite = glowSprite;

            sharedDeviceResource = context.Device.GetSharedResource<LensFlare, SharedDeviceResource>();

            spriteBatch = new SpriteBatch(context);

            for (int i = 0; i < flares.Length; i++)
            {
                var index = flares[i].TextureIndex;
                if (index < 0 || flareSprites.Length <= index)
                    throw new InvalidOperationException("Invalid index of flare sprite: " + index);

                flares[i].Texture = flareSprites[index];
            }

            basicEffect = new BasicEffect(context.Device);
            basicEffect.View = Matrix.Identity;
            basicEffect.VertexColorEnabled = true;

            vertexBuffer = context.Device.CreateVertexBuffer();
            occlusionQuery = context.Device.CreateOcclusionQuery();

            vertices = new VertexPositionColor[4];
            querySize = 100;

            dirtyFlags = DirtyFlags.VertexBuffer;

            Enabled = true;
        }

        void SetVertices()
        {
            if ((dirtyFlags & DirtyFlags.VertexBuffer) != 0)
            {
                vertices[0].Position = new Vector3(-querySize * 0.5f,  querySize * 0.5f, -1.0f);
                vertices[1].Position = new Vector3(-querySize * 0.5f, -querySize * 0.5f, -1.0f);
                vertices[2].Position = new Vector3( querySize * 0.5f, -querySize * 0.5f, -1.0f);
                vertices[3].Position = new Vector3( querySize * 0.5f,  querySize * 0.5f, -1.0f);
                vertexBuffer.SetData(Context, vertices);

                dirtyFlags &= ~DirtyFlags.VertexBuffer;
            }
        }

        public void Draw()
        {
            SetVertices();

            var infiniteView = view;
            infiniteView.Translation = Vector3.Zero;

            var viewport = Context.Viewport;
            var projectedPosition = viewport.Project(-lightDirection, projection, infiniteView, Matrix.Identity);

            if (projectedPosition.Z < 0 || 1 < projectedPosition.Z)
            {
                lightBehindCamera = true;
                return;
            }

            lightPosition = new Vector2(projectedPosition.X, projectedPosition.Y);
            lightBehindCamera = false;

            UpdateOcclusion();

            DrawGlow();
            DrawFlares();
        }

        void UpdateOcclusion()
        {
            if (lightBehindCamera) return;

            if (occlusionQueryActive)
            {
                if (!occlusionQuery.IsComplete) return;

                float queryArea = querySize * querySize;
                occlusionAlpha = Math.Min(occlusionQuery.PixelCount / queryArea, 1);
            }

            var viewport = Context.Viewport;

            Matrix world;
            Matrix.CreateTranslation(lightPosition.X, lightPosition.Y, 0, out world);

            Matrix projection;
            Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1, out projection);

            basicEffect.World = world;
            basicEffect.Projection = projection;
            basicEffect.Apply(Context);

            Context.BlendState = ColorWriteDisable;
            Context.DepthStencilState = DepthStencilState.DepthRead;

            occlusionQuery.Begin(Context);

            Context.SetVertexBuffer(vertexBuffer);
            Context.IndexBuffer = sharedDeviceResource.IndexBuffer;
            Context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Context.DrawIndexed(sharedDeviceResource.IndexBuffer.IndexCount);

            occlusionQuery.End();

            occlusionQueryActive = true;
        }

        void DrawGlow()
        {
            if (lightBehindCamera || occlusionAlpha <= 0) return;

            var color = Color.White * occlusionAlpha;
            var origin = new Vector2(glowSprite.Width / 2.0f, glowSprite.Height / 2.0f);
            var scale = GlowSize * 2.0f / (float) glowSprite.Width;

            spriteBatch.Begin();
            spriteBatch.Draw(glowSprite, lightPosition, null, color, 0, origin, scale);
            spriteBatch.End();
        }

        void DrawFlares()
        {
            if (lightBehindCamera || occlusionAlpha <= 0) return;

            var viewport = Context.Viewport;

            var screenCenter = new Vector2(viewport.Width / 2.0f, viewport.Height / 2.0f);

            var flareVector = screenCenter - lightPosition;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            foreach (var flare in flares)
            {
                var flarePosition = lightPosition + flareVector * flare.Position;

                var flareColor = flare.Color.ToVector4();
                flareColor.W *= occlusionAlpha;

                var flareOrigin = new Vector2(flare.Texture.Width / 2.0f, flare.Texture.Height / 2.0f);

                spriteBatch.Draw(flare.Texture, flarePosition, null, new Color(flareColor), 1, flareOrigin, flare.Scale);
            }

            spriteBatch.End();
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
                occlusionQuery.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
