#region Using

using System;
using System.Collections.Generic;
using Felis.Xnb;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class SpriteFontBuilder : SpriteFontBuilderBase<SpriteFont>
    {
        IGraphicsService graphicsService;

        ShaderResourceView texture;

        List<Rectangle> glyphs;

        List<Rectangle> cropping;

        List<char> characters;

        int lineSpacing;

        float spacing;

        List<Vector3> kerning;

        char? defaultCharacter;

        protected override void Initialize(ContentManager contentManager)
        {
            graphicsService = contentManager.ServiceProvider.GetRequiredService<IGraphicsService>();

            base.Initialize(contentManager);
        }

        protected override void SetTexture(object value)
        {
            texture = graphicsService.Device.CreateShaderResourceView();
            texture.Initialize(value as Texture2D);
        }

        protected override void SetGlyphs(object value)
        {
            glyphs = value as List<Rectangle>;
        }

        protected override void SetCropping(object value)
        {
            cropping = value as List<Rectangle>;
        }

        protected override void SetCharacterMap(object value)
        {
            characters = value as List<char>;
        }

        protected override void SetVerticalLineSpacing(int value)
        {
            lineSpacing = value;
        }

        protected override void SetHorizontalSpacing(float value)
        {
            spacing = value;
        }

        protected override void SetKering(object value)
        {
            kerning = value as List<Vector3>;
        }

        protected override void SetDefaultCharacter(object value)
        {
            if (value == null)
            {
                defaultCharacter = null;
            }
            else
            {
                defaultCharacter = value as char?;
            }
        }

        protected override void Begin(object deviceContext)
        {
        }

        protected override object End()
        {
            return new SpriteFont(texture, glyphs, cropping, characters, lineSpacing, spacing, kerning, defaultCharacter);
        }
    }
}
