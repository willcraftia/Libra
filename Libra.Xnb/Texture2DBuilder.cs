#region Using

using System;
using Felis;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class Texture2DBuilder : Texture2DBuilderBase<Texture2D>
    {
        IGraphicsService graphicsService;

        Texture2D instance;

        int currentMipLevel;

        protected override void Initialize(ContentManager contentManager)
        {
            graphicsService = contentManager.ServiceProvider.GetRequiredService<IGraphicsService>();

            base.Initialize(contentManager);
        }

        protected override void SetSurfaceFormat(int value)
        {
            instance.Format = SurfaceFormatConverter.ToSurfaceFormat(value);
        }

        protected override void SetWidth(uint value)
        {
            instance.Width = (int) value;
        }

        protected override void SetHeight(uint value)
        {
            instance.Height = (int) value;
        }

        protected override void SetMipCount(uint value)
        {
            instance.MipLevels = (int) value;
        }

        protected override void BeginMips()
        {
            instance.Initialize();

            base.BeginMips();
        }

        protected override void BeginMip(int index)
        {
            currentMipLevel = index;
        }

        protected override void SetMipDataSize(uint value)
        {
        }

        protected override void SetMipImageData(byte[] value)
        {
            // TODO
            //
            // 当面は ImmidiateContext 固定。
            // DeferredContext が必要となるならば、
            // ContentManager のサブクラスで DeviceContext を管理するなど。
            instance.SetData(graphicsService.Device.ImmediateContext, 0, currentMipLevel, value, 0, value.Length);
        }

        protected override void Begin()
        {
            instance = graphicsService.Device.CreateTexture2D();
        }

        protected override object End()
        {
            return instance;
        }
    }
}
