#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;

#endregion

namespace Samples.LensFlare
{
    public sealed class LensFlareComponent : DrawableGameComponent
    {
        const float glowSize = 400;

        const float querySize = 100;

        public Matrix View;

        public Matrix Projection;

        public Vector3 LightDirection = Vector3.Normalize(new Vector3(-1, -0.1f, 0.3f));

        Vector2 lightPosition;
        
        bool lightBehindCamera;

        Texture2D glowSprite;

        ShaderResourceView glowSpriteView;

        SpriteBatch spriteBatch;
        
        BasicEffect basicEffect;
        
        VertexBuffer vertexBuffer;

        static readonly BlendState ColorWriteDisable = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None
        };

        OcclusionQuery occlusionQuery;

        bool occlusionQueryActive;
        
        float occlusionAlpha;

        #region Flare

        class Flare
        {
            public Flare(float position, float scale, Color color, string textureName)
            {
                Position = position;
                Scale = scale;
                Color = color;
                TextureName = textureName;
            }

            public float Position;

            public float Scale;
            
            public Color Color;
            
            public string TextureName;

            public Texture2D Texture;

            public ShaderResourceView TextureView;
        }

        #endregion

        Flare[] flares =
        {
            new Flare(-0.5f, 0.7f, new Color( 50,  25,  50), "flare1"),
            new Flare( 0.3f, 0.4f, new Color(100, 255, 200), "flare1"),
            new Flare( 1.2f, 1.0f, new Color(100,  50,  50), "flare1"),
            new Flare( 1.5f, 1.5f, new Color( 50, 100,  50), "flare1"),

            new Flare(-0.3f, 0.7f, new Color(200,  50,  50), "flare2"),
            new Flare( 0.6f, 0.9f, new Color( 50, 100,  50), "flare2"),
            new Flare( 0.7f, 0.4f, new Color( 50, 200, 200), "flare2"),

            new Flare(-0.7f, 0.7f, new Color( 50, 100,  25), "flare3"),
            new Flare( 0.0f, 0.6f, new Color( 25,  25,  25), "flare3"),
            new Flare( 2.0f, 1.4f, new Color( 25,  50, 100), "flare3"),
        };

        public LensFlareComponent(Game game)
            : base(game)
        {
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device);

            glowSprite = (Game as MainGame).Content.Load<Texture2D>("glow");
            glowSpriteView = Device.CreateShaderResourceView();
            glowSpriteView.Initialize(glowSprite);

            foreach (var flare in flares)
            {
                flare.Texture = (Game as MainGame).Content.Load<Texture2D>(flare.TextureName);
                flare.TextureView = Device.CreateShaderResourceView();
                flare.TextureView.Initialize(flare.Texture);
            }

            basicEffect = new BasicEffect(Device);
            basicEffect.View = Matrix.Identity;
            basicEffect.VertexColorEnabled = true;

            var vertices = new VertexPositionColor[4];
            vertices[0].Position = new Vector3(-querySize / 2, -querySize / 2, -1);
            vertices[1].Position = new Vector3( querySize / 2, -querySize / 2, -1);
            vertices[2].Position = new Vector3(-querySize / 2,  querySize / 2, -1);
            vertices[3].Position = new Vector3( querySize / 2,  querySize / 2, -1);

            vertexBuffer = Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Immutable;
            vertexBuffer.Initialize(vertices);

            occlusionQuery = Device.CreateOcclusionQuery();
            occlusionQuery.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            UpdateOcclusion();

            DrawGlow();
            DrawFlares();

            RestoreRenderStates();
        }

        public void UpdateOcclusion()
        {
            var context = Device.ImmediateContext;

            var infiniteView = View;
            infiniteView.Translation = Vector3.Zero;

            var viewport = context.Viewport;
            var projectedPosition = viewport.Project(-LightDirection, Projection, infiniteView, Matrix.Identity);

            if ((projectedPosition.Z < 0) || (projectedPosition.Z > 1))
            {
                lightBehindCamera = true;
                return;
            }

            lightPosition = new Vector2(projectedPosition.X, projectedPosition.Y);
            lightBehindCamera = false;

            if (occlusionQueryActive)
            {
                if (!occlusionQuery.IsComplete)
                    return;

                const float queryArea = querySize * querySize;

                occlusionAlpha = Math.Min(occlusionQuery.PixelCount / queryArea, 1);
            }

            context.BlendState = ColorWriteDisable;
            context.DepthStencilState = DepthStencilState.DepthRead;

            context.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            context.SetVertexBuffer(0, vertexBuffer);

            basicEffect.World = Matrix.CreateTranslation(lightPosition.X, lightPosition.Y, 0);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);

            basicEffect.Apply(context);

            occlusionQuery.Begin();

            // TriangleStrip で 3 * 2 の三角形を描画。
            context.Draw(6);

            occlusionQuery.End();

            occlusionQueryActive = true;
        }

        public void DrawGlow()
        {
            if (lightBehindCamera || occlusionAlpha <= 0)
                return;

            var color = Color.White * occlusionAlpha;
            var origin = new Vector2(glowSprite.Width, glowSprite.Height) / 2;
            float scale = glowSize * 2 / glowSprite.Width;

            spriteBatch.Begin();
            spriteBatch.Draw(glowSpriteView, lightPosition, null, color, 0, origin, scale);
            spriteBatch.End();
        }

        public void DrawFlares()
        {
            if (lightBehindCamera || occlusionAlpha <= 0)
                return;

            var context = Device.ImmediateContext;
            var viewport = context.Viewport;
            var screenCenter = new Vector2(viewport.Width, viewport.Height) / 2;

            var flareVector = screenCenter - lightPosition;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            foreach (var flare in flares)
            {
                var flarePosition = lightPosition + flareVector * flare.Position;

                var flareColor = flare.Color.ToVector4();
                flareColor.W *= occlusionAlpha;

                var flareOrigin = new Vector2(flare.Texture.Width, flare.Texture.Height) / 2;
                
                spriteBatch.Draw(flare.TextureView, flarePosition, null, new Color(flareColor), 1, flareOrigin, flare.Scale);
            }

            spriteBatch.End();
        }

        void RestoreRenderStates()
        {
            var context = Device.ImmediateContext;

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;
            context.PixelShaderSamplers[0] = SamplerState.LinearWrap;
        }
    }
}
