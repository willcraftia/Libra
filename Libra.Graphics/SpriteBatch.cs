#region Using

using System;
using System.Collections.Generic;
using System.Text;

#endregion

// DirectX Tool Kit より移植。

namespace Libra.Graphics
{
    public sealed partial class SpriteBatch : IDisposable
    {
        #region DeviceResources

        sealed class DeviceResources
        {
            IDevice device;

            public InputLayout InputLayout { get; private set; }

            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public IndexBuffer IndexBuffer { get; private set; }

            public DeviceResources(IDevice device)
            {
                this.device = device;

                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(SpriteVertexShader);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(SpritePixelShader);

                InputLayout = device.CreateInputLayout();
                InputLayout.Initialize(SpriteVertexShader, VertexPositionColorTexture.VertexDeclaration);

                IndexBuffer = device.CreateIndexBuffer();
                IndexBuffer.Usage = ResourceUsage.Immutable;
                IndexBuffer.Initialize(CreateIndexValues());
            }

            static ushort[] CreateIndexValues()
            {
                var indices = new ushort[MaxBatchSize * IndicesPerSprite];

                int cursor = 0;
                for (ushort i = 0; i < MaxBatchSize * VerticesPerSprite; i += VerticesPerSprite)
                {
                    indices[cursor++] = i;
                    indices[cursor++] = (ushort) (i + 1);
                    indices[cursor++] = (ushort) (i + 2);

                    indices[cursor++] = (ushort) (i + 1);
                    indices[cursor++] = (ushort) (i + 3);
                    indices[cursor++] = (ushort) (i + 2);
                }

                return indices;
            }
        }

        #endregion

        #region ContextResoruces

        sealed class ContextResoruces
        {
            public int VertexBufferPosition;

            DeviceContext context;

            public VertexBuffer VertexBuffer { get; private set; }

            public ConstantBuffer ConstantBuffer { get; private set; }

            public bool InImmediateMode { get; internal set; }

            public ContextResoruces(DeviceContext context)
            {
                this.context = context;

                var device = context.Device;

                VertexBuffer = device.CreateVertexBuffer();
                VertexBuffer.Usage = ResourceUsage.Dynamic;
                VertexBuffer.Initialize(VertexPositionColorTexture.VertexDeclaration, MaxBatchSize * VerticesPerSprite);

                ConstantBuffer = device.CreateConstantBuffer();
                ConstantBuffer.Initialize<Matrix>();
            }
        }

        #endregion

        #region RectangleF

        struct RectangleF
        {
            public float X;

            public float Y;

            public float Width;

            public float Height;

            public RectangleF(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public RectangleF(Rectangle rectangle)
            {
                X = rectangle.X;
                Y = rectangle.Y;
                Width = rectangle.Width;
                Height = rectangle.Height;
            }

            #region ToString

            public override string ToString()
            {
                return "{X:" + X + ", Y:" + Y + ", Width:" + Width + ", Height:" + Height + "}";
            }

            #endregion
        }

        #endregion

        #region SpriteInfo

        struct SpriteInfo
        {
            public RectangleF Source;

            public RectangleF Destination;

            public Color Color;

            public float Rotation;

            public Vector2 Origin;

            public float Depth;

            public IShaderResourceView Texture;

            public int Flags;
        }

        #endregion

        #region ComparerForTexture

        sealed class ComparerForTexture : IComparer<SpriteInfo>
        {
            public static ComparerForTexture Instance = new ComparerForTexture();

            ComparerForTexture() { }

            public int Compare(SpriteInfo x, SpriteInfo y)
            {
                // テクスチャの参照が等しい場合に等しい。
                if (x.Texture == y.Texture) return 0;

                // テクスチャの参照で大小を決められないため、
                // ソートを成功させるために、ハッシュコードで代替する。
                // ここでの比較は、重要ではない。
                return x.GetHashCode().CompareTo(y.GetHashCode());
            }
        }

        #endregion

        #region ComparerForBackToFront

        sealed class ComparerForBackToFront : IComparer<SpriteInfo>
        {
            public static ComparerForBackToFront Instance = new ComparerForBackToFront();

            ComparerForBackToFront() { }

            public int Compare(SpriteInfo x, SpriteInfo y)
            {
                // 深度で比較。最前面 0、最背面 1。
                // より W 値の大きい物が、より奥にある。

                if (y.Depth < x.Depth)
                    return -1;

                if (x.Depth < y.Depth)
                    return 1;

                return 0;
            }
        }

        #endregion

        #region ComparerForFrontToBack

        sealed class ComparerForFrontToBack : IComparer<SpriteInfo>
        {
            public static ComparerForFrontToBack Instance = new ComparerForFrontToBack();

            ComparerForFrontToBack() { }

            public int Compare(SpriteInfo x, SpriteInfo y)
            {
                // ComparerForBackToFront の逆。

                if (y.Depth < x.Depth)
                    return 1;

                if (x.Depth < y.Depth)
                    return -1;

                return 0;
            }
        }

        #endregion

        static readonly SharedResourcePool<IDevice, DeviceResources> DeviceResourcesPool;

        static readonly SharedResourcePool<DeviceContext, ContextResoruces> ContextResourcesPool;

        const int MaxBatchSize = 2048;

        const int MinBatchSize = 128;

        const int VerticesPerSprite = 4;

        const int IndicesPerSprite = 6;

        const int SourceInTexels = 4;

        const int DestSizeInPixels = 8;

        static readonly Vector2[] CornerOffsets =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        DeviceContext context;

        DeviceResources deviceResources;

        ContextResoruces contextResoruces;

        SpriteSortMode sortMode;

        BlendState blendState;

        SamplerState samplerState;

        DepthStencilState depthStencilState;

        RasterizerState rasterizerState;

        Action setCustomShaders;

        Matrix transformMatrix;

        bool inBeginEndPair;

        List<SpriteInfo> sprites;

        static SpriteBatch()
        {
            DeviceResourcesPool = new SharedResourcePool<IDevice, DeviceResources>(CreateDeviceResources);
            ContextResourcesPool = new SharedResourcePool<DeviceContext, ContextResoruces>(CreateContextResoruces);
        }

        // コンテキスト毎で縛る。
        public SpriteBatch(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            deviceResources = DeviceResourcesPool.Get(context.Device);
            contextResoruces = ContextResourcesPool.Get(context);

            sprites = new List<SpriteInfo>();
        }

        static DeviceResources CreateDeviceResources(IDevice device)
        {
            return new DeviceResources(device);
        }

        static ContextResoruces CreateContextResoruces(DeviceContext context)
        {
            return new ContextResoruces(context);
        }

        // メモ
        //
        // ひにけにGD - SpriteBatch と Effect
        // http://blogs.msdn.com/b/ito/archive/2010/03/30/spritebatch-and-effect.aspx
        //
        // ここでは DirectXTK 同様にシェーダ指定をデリゲートとしているが、
        // 使い方の概念については上記サイトを参照。

        public void Begin(
            SpriteSortMode sortMode = SpriteSortMode.Deferred,
            BlendState blendState = null,
            SamplerState samplerState = null,
            DepthStencilState depthStencilState = null,
            RasterizerState rasterizerState = null,
            Action setCustomShaders = null)
        {
            Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, setCustomShaders, Matrix.Identity);
        }

        public void Begin(
            SpriteSortMode sortMode,
            BlendState blendState,
            SamplerState samplerState,
            DepthStencilState depthStencilState,
            RasterizerState rasterizerState,
            Action setCustomShaders,
            Matrix transformMatrix)
        {
            if (inBeginEndPair)
                throw new InvalidOperationException("Cannot nest Begin calls on a single SpriteBatch");

            this.sortMode = sortMode;
            this.blendState = blendState ?? BlendState.AlphaBlend;
            this.samplerState = samplerState ?? SamplerState.LinearClamp;
            this.depthStencilState = depthStencilState ?? DepthStencilState.None;
            this.rasterizerState = rasterizerState ?? RasterizerState.CullBack;
            this.setCustomShaders = setCustomShaders;
            this.transformMatrix = transformMatrix;

            if (sortMode == SpriteSortMode.Immediate)
            {
                if (contextResoruces.InImmediateMode)
                    throw new InvalidOperationException("Only one SpriteBatch at a time can use SpriteSortMode.Immediate");

                PrepareForRendering();

                contextResoruces.InImmediateMode = true;
            }

            inBeginEndPair = true;
        }

        public void End()
        {
            if (!inBeginEndPair)
                throw new InvalidOperationException("Begin must be called before End");

            if (sortMode == SpriteSortMode.Immediate)
            {
                contextResoruces.InImmediateMode = false;
            }
            else
            {
                if (contextResoruces.InImmediateMode)
                    throw new InvalidOperationException("Cannot end one SpriteBatch while another is using SpriteSortMode.Immediate");

                PrepareForRendering();
                FlushBatch();
            }

            setCustomShaders = null;

            inBeginEndPair = false;
        }

        public void Draw(IShaderResourceView texture, Vector2 position, Color color)
        {
            var destination = new RectangleF(position.X, position.Y, 1, 1);

            Draw(texture, destination, null, color, 0, Vector2.Zero, 0, 0);
        }

        public void Draw(IShaderResourceView texture, Vector2 position, Rectangle? sourceRectangle, Color color)
        {
            var destination = new RectangleF(position.X, position.Y, 1, 1);

            Draw(texture, destination, sourceRectangle, color, 0, Vector2.Zero, 0, 0);
        }

        public void Draw(IShaderResourceView texture, Vector2 position, Rectangle? sourceRectangle, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects = SpriteEffects.None, float depth = 0)
        {
            var destination = new RectangleF(position.X, position.Y, scale, scale);

            Draw(texture, destination, sourceRectangle, color, rotation, origin, depth, (int) effects);
        }

        public void Draw(IShaderResourceView texture, Vector2 position, Rectangle? sourceRectangle, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None, float depth = 0)
        {
            var destination = new RectangleF(position.X, position.Y, scale.X, scale.Y);

            Draw(texture, destination, sourceRectangle, color, rotation, origin, depth, (int) effects);
        }

        public void Draw(IShaderResourceView texture, Rectangle destinationRectangle, Color color)
        {
            var destination = new RectangleF(destinationRectangle);

            Draw(texture, destination, null, color, 0, Vector2.Zero, 0, DestSizeInPixels);
        }

        public void Draw(IShaderResourceView texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
        {
            var destination = new RectangleF(destinationRectangle);

            Draw(texture, destination, sourceRectangle, color, 0, Vector2.Zero, 0, DestSizeInPixels);
        }

        public void Draw(IShaderResourceView texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color,
            float rotation, Vector2 origin, SpriteEffects effects = SpriteEffects.None, float depth = 0)
        {
            var destination = new RectangleF(destinationRectangle);

            Draw(texture, destination, sourceRectangle, color, rotation, origin, depth, ((int) effects) | DestSizeInPixels);
        }

        void Draw(IShaderResourceView texture, RectangleF destination, Rectangle? sourceRectangle, Color color,
            float rotation, Vector2 origin, float depth, int flags)
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if (!(texture.Resource is Texture2D))
                throw new ArgumentException("SpriteBatch can only draw Texture2D resources");
            if (!inBeginEndPair)
                throw new InvalidOperationException("Begin must be called before Draw");

            RectangleF source;
            if (sourceRectangle.HasValue)
            {
                var rect = sourceRectangle.Value;
                source = new RectangleF(rect);

                if ((flags & DestSizeInPixels) == 0)
                {
                    destination.Width *= source.Width;
                    destination.Height *= source.Height;
                }

                flags |= SourceInTexels | DestSizeInPixels;
            }
            else
            {
                source = new RectangleF(0, 0, 1, 1);
            }

            var sprite = new SpriteInfo
            {
                Source = source,
                Destination = destination,
                Color = color,
                Rotation = rotation,
                Origin = origin,
                Depth = depth,
                Texture = texture,
                Flags = flags
            };

            if (sortMode == SpriteSortMode.Immediate)
            {
                sprites.Add(sprite);
                RenderBatch(sprite.Texture, 0, 1);
            }
            else
            {
                sprites.Add(sprite);
            }
        }

        void PrepareForRendering()
        {
            context.BlendState = blendState;
            context.DepthStencilState = depthStencilState;
            context.RasterizerState = rasterizerState;
            context.PixelShaderSamplers[0] = samplerState;

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputLayout = deviceResources.InputLayout;
            context.VertexShader = deviceResources.VertexShader;
            context.PixelShader = deviceResources.PixelShader;

            context.SetVertexBuffer(0, contextResoruces.VertexBuffer);
            context.IndexBuffer = deviceResources.IndexBuffer;

            Matrix viewportTransform;
            GetViewportTransform(out viewportTransform);

            Matrix finalTransformMatrix;
            Matrix.Multiply(ref transformMatrix, ref viewportTransform, out finalTransformMatrix);

            var constantBuffer = contextResoruces.ConstantBuffer;
            constantBuffer.SetData(context, finalTransformMatrix);

            context.VertexShaderConstantBuffers[0] = constantBuffer;

            if (context.Deferred)
            {
                contextResoruces.VertexBufferPosition = 0;
            }

            if (setCustomShaders != null)
            {
                setCustomShaders();
            }
        }

        void FlushBatch()
        {
            if (sprites.Count == 0)
                return;

            SortSprites();

            IShaderResourceView batchTexture = null;
            int batchStart = 0;

            // 同一テクスチャの処理を纏めながら描画。
            for (int i = 0; i < sprites.Count; i++)
            {
                var texture = sprites[i].Texture;

                // 異なるテクスチャに切り替わったら、
                // そこまでにあった処理を纏めて反映。
                if (texture != batchTexture)
                {
                    // (i == 0) 回避
                    if (batchStart < i)
                    {
                        RenderBatch(batchTexture, batchStart, i - batchStart);
                    }

                    batchTexture = texture;
                    batchStart = i;
                }
            }

            // 最後の一纏めを処理。
            RenderBatch(batchTexture, batchStart, sprites.Count - batchStart);

            // 描画処理をクリア。
            sprites.Clear();
        }

        void SortSprites()
        {
            switch (sortMode)
            {
                case SpriteSortMode.Texture:
                    sprites.Sort(ComparerForTexture.Instance);
                    break;
                case SpriteSortMode.BackToFront:
                    sprites.Sort(ComparerForBackToFront.Instance);
                    break;
                case SpriteSortMode.FrontToBack:
                    sprites.Sort(ComparerForFrontToBack.Instance);
                    break;
            }
        }

        void RenderBatch(IShaderResourceView texture, int startIndex, int spriteCount)
        {
            context.PixelShaderResources[0] = texture;

            Vector2 textureSize;
            GetTextureSize(texture, out textureSize);

            var inverseTextureSize = new Vector2(1.0f / textureSize.X, 1.0f / textureSize.Y);

            int count = spriteCount;
            int baseSpriteIndex = startIndex;

            while (0 < count)
            {
                int batchSize = count;
                int remainingSpace = MaxBatchSize - contextResoruces.VertexBufferPosition;

                if (remainingSpace < batchSize)
                {
                    if (remainingSpace < MinBatchSize)
                    {
                        contextResoruces.VertexBufferPosition = 0;
                        batchSize = Math.Min(count, MaxBatchSize);
                    }
                    else
                    {
                        batchSize = remainingSpace;
                    }
                }

                var mapMode = (contextResoruces.VertexBufferPosition == 0) ?
                    DeviceContext.MapMode.WriteDiscard : DeviceContext.MapMode.WriteNoOverwrite;

                // TODO
                //
                // SharpDX ToolKit によると、x64 では、
                // Map されたリソースへ直接書き込むと極端に低速になるとのこと。
                // 要調査。
                // 恐らく、64 ビット ポインタであるから、ではないだろうか？

                var mappedResource = context.Map(contextResoruces.VertexBuffer, 0, mapMode);
                unsafe
                {
                    var vertices = (VertexPositionColorTexture*) mappedResource.Pointer +
                        contextResoruces.VertexBufferPosition * VerticesPerSprite;

                    for (int i = 0; i < batchSize; i++)
                    {
                        RenderSprite(baseSpriteIndex + i, ref vertices, ref textureSize, ref inverseTextureSize);

                        vertices += VerticesPerSprite;
                    }
                }
                context.Unmap(contextResoruces.VertexBuffer, 0);

                int startIndexLocation = contextResoruces.VertexBufferPosition * IndicesPerSprite;
                int indexCount = batchSize * IndicesPerSprite;

                context.DrawIndexed(indexCount, startIndexLocation);

                contextResoruces.VertexBufferPosition += batchSize;
                baseSpriteIndex += batchSize;
                count -= batchSize;
            }
        }

        unsafe void RenderSprite(
            int spriteIndex, ref VertexPositionColorTexture* vertices, ref Vector2 textureSize, ref Vector2 inverseTextureSize)
        {
            var sprite = sprites[spriteIndex];

            var source = sprite.Source;
            var destination = sprite.Destination;
            var origin = sprite.Origin;

            if (source.Width == 0) source.Width = float.Epsilon;
            if (source.Height == 0) source.Height = float.Epsilon;

            origin.X /= source.Width;
            origin.Y /= source.Height;

            if ((sprite.Flags & SourceInTexels) != 0)
            {
                source.X *= inverseTextureSize.X;
                source.Y *= inverseTextureSize.Y;
                source.Width *= inverseTextureSize.X;
                source.Height *= inverseTextureSize.Y;
            }
            else
            {
                origin *= inverseTextureSize;
            }

            if ((sprite.Flags & DestSizeInPixels) == 0)
            {
                destination.Width *= textureSize.X;
                destination.Height *= textureSize.Y;
            }

            Vector2 rotation;
            if (sprite.Rotation == 0)
            {
                rotation = new Vector2(1, 0);
            }
            else
            {
                rotation = new Vector2
                {
                    X = (float) Math.Cos(sprite.Rotation),
                    Y = (float) Math.Sin(sprite.Rotation)
                };
            }

            // 本来の SpritEffects のビット値を取得。
            int mirrorBits = (int) sprite.Flags & 3;

            for (int i = 0; i < VerticesPerSprite; i++)
            {
                var cornerOffset = (CornerOffsets[i] - origin);
                cornerOffset.X *= destination.Width;
                cornerOffset.Y *= destination.Height;

                vertices[i].Position = new Vector3
                {
                    X = destination.X + (cornerOffset.X * rotation.X) - (cornerOffset.Y * rotation.Y),
                    Y = destination.Y + (cornerOffset.X * rotation.Y) + (cornerOffset.Y * rotation.X),
                    Z = sprite.Depth
                };

                vertices[i].Color = sprite.Color;

                cornerOffset = CornerOffsets[i ^ mirrorBits];

                vertices[i].TexCoord = new Vector2
                {
                    X = (source.X + cornerOffset.X * source.Width),
                    Y = (source.Y + cornerOffset.Y * source.Height)
                };
            }
        }

        void GetTextureSize(IShaderResourceView texture, out Vector2 result)
        {
            var texture2D = texture.Resource as Texture2D;
            if (texture2D == null)
                throw new InvalidOperationException("SpriteBatch can only draw Texture2D resources");

            result.X = texture2D.Width;
            result.Y = texture2D.Height;
        }

        void GetViewportTransform(out Matrix result)
        {
            var viewport = context.Viewport;

            float xScale = (0 < viewport.Width) ? 2.0f / viewport.Width : 0.0f;
            float yScale = (0 < viewport.Height) ? 2.0f / viewport.Height : 0.0f;

            result = Matrix.Identity;
            result.M11 = xScale;
            result.M22 = -yScale;
            result.M41 = -1;
            result.M42 = 1;
        }

        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
        {
            DrawString(spriteFont, text, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
        }

        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
        {
            DrawString(spriteFont, text, position, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
        }

        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects = SpriteEffects.None, float depth = 0.0f)
        {
            DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale), effects, depth);
        }

        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects = SpriteEffects.None, float depth = 0.0f)
        {
            DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale), effects, depth);
        }

        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None, float depth = 0.0f)
        {
            if (spriteFont == null) throw new ArgumentNullException("spriteFont");
            if (text == null) throw new ArgumentNullException("text");

            spriteFont.DrawString(this, text, position, color, rotation, origin, scale, effects, depth);
        }

        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth)
        {
            if (spriteFont == null) throw new ArgumentNullException("spriteFont");
            if (text == null) throw new ArgumentNullException("text");

            spriteFont.DrawString(this, text, position, color, rotation, origin, scale, effects, depth);
        }

        #region IDisposable

        bool disposed;

        ~SpriteBatch()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                deviceResources = null;
                contextResoruces = null;
            }

            disposed = true;
        }

        #endregion
    }
}
