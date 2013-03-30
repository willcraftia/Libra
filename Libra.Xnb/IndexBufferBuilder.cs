#region Using

using System;
using Felis.Xnb;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class IndexBufferBuilder : IndexBufferBuilderBase<IndexBuffer>
    {
        IGraphicsService graphicsService;

        IndexBuffer instance;

        protected override void Initialize(ContentManager contentManager)
        {
            graphicsService = contentManager.ServiceProvider.GetRequiredService<IGraphicsService>();

            base.Initialize(contentManager);
        }

        protected override void SetIsSixteenBits(bool value)
        {
            instance.Format = (value) ? IndexFormat.SixteenBits : IndexFormat.ThirtyTwoBits;
        }

        protected override void SetDataSize(uint value)
        {
        }

        protected override void SetIndexData(byte[] value)
        {
            instance.Initialize(value);
        }

        protected override void Begin(object deviceContext)
        {
            instance = graphicsService.Device.CreateIndexBuffer();
        }

        protected override object End()
        {
            return instance;
        }
    }
}
