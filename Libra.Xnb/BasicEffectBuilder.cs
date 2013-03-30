#region Using

using System;
using Felis.Xnb;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class BasicEffectBuilder : BasicEffectBuilderBase<BasicEffect>
    {
        ContentManager contentManager;

        IGraphicsService graphicsService;

        BasicEffect instance;

        protected override void Initialize(ContentManager contentManager)
        {
            this.contentManager = contentManager;
            graphicsService = contentManager.ServiceProvider.GetRequiredService<IGraphicsService>();

            base.Initialize(contentManager);
        }

        protected override void Begin(object deviceContext)
        {
            instance = new BasicEffect(graphicsService.Device);
        }

        protected override void SetTexture(string value)
        {
            if (value != null)
            {
                var texture = contentManager.Load<Texture2D>(value);

                var shaderResourceView = graphicsService.Device.CreateShaderResourceView();
                shaderResourceView.Initialize(texture);

                instance.Texture = shaderResourceView;
                instance.TextureEnabled = true;
            }
        }

        protected override void SetDiffuseColor(object value)
        {
            instance.DiffuseColor = (Vector3) value;
        }

        protected override void SetEmissiveColor(object value)
        {
            instance.EmissiveColor = (Vector3) value;
        }

        protected override void SetSpecularColor(object value)
        {
            instance.SpecularColor = (Vector3) value;
        }

        protected override void SetSpecularPower(float value)
        {
            instance.SpecularPower = value;
        }

        protected override void SetAlpha(float value)
        {
            instance.Alpha = value;
        }

        protected override void SetVertexColorEnabled(bool value)
        {
            instance.VertexColorEnabled = value;
        }

        protected override object End()
        {
            return instance;
        }
    }
}
