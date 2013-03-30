#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

#endregion

// MonoGame の SpriteFont から移植。
// ただし、MonoGame では、SpriteEffect の適用に関するロジックにおいて、
// 行列演算による非効率な変換を行なっているため、
// DirectX Tool Kit を参考に行列演算を用いないロジックへ修正。

namespace Libra.Graphics
{
    public sealed class SpriteFont : IDisposable
    {
        #region Glyph

        struct Glyph
        {
            public static readonly Glyph Empty = new Glyph();

            public char Character;

            public Rectangle Bounds;
            
            public Rectangle Cropping;
            
            public float LeftSideBearing;
            
            public float RightSideBearing;
            
            public float Width;
            
            public float WidthIncludingBearings;

            #region ToString

            public override string ToString()
            {
                return "{Character:" + Character +
                    " Bounds:" + Bounds +
                    " Cropping:" + Cropping +
                    " LeftSideBearing:" + LeftSideBearing +
                    " RightSideBearing:" + RightSideBearing +
                    " Width:" + Width +
                    "}";
            }

            #endregion
        }

        #endregion

        #region CharacterSource

        struct CharacterSource
        {
            public readonly int Length;

            string stringObject;

            StringBuilder stringBuilder;

            public char this[int index]
            {
                get
                {
                    if (stringObject != null)
                    {
                        return stringObject[index];
                    }
                    else
                    {
                        return stringBuilder[index];
                    }
                }
            }

            public CharacterSource(string stringObject)
            {
                this.stringObject = stringObject;
                stringBuilder = null;
                Length = stringObject.Length;
            }

            public CharacterSource(StringBuilder stringBuilder)
            {
                this.stringBuilder = stringBuilder;
                stringObject = null;
                Length = stringBuilder.Length;
            }
        }

        #endregion

        ShaderResourceView texture;

        Dictionary<char, Glyph> glyphMap;

        public ReadOnlyCollection<char> Characters { get; private set; }

        public int LineSpacing { get; set; }

        public float Spacing { get; set; }

        public char? DefaultCharacter { get; set; }

        public SpriteFont(
            ShaderResourceView texture,
            IList<Rectangle> bounds,
            IList<Rectangle> cropping,
            IList<char> characters,
            int lineSpacing,
            float spacing,
            IList<Vector3> kerning,
            char? defaultCharacter)
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if (bounds == null) throw new ArgumentNullException("bounds");
            if (cropping == null) throw new ArgumentNullException("cropping");
            if (characters == null) throw new ArgumentNullException("characters");
            if (kerning == null) throw new ArgumentNullException("kerning");

            this.texture = texture;
            Characters = new ReadOnlyCollection<char>(characters);
            LineSpacing = lineSpacing;
            Spacing = spacing;
            DefaultCharacter = defaultCharacter;

            glyphMap = new Dictionary<char, Glyph>(characters.Count);
            for (int i = 0; i < characters.Count; i++)
            {
                var glyph = new Glyph
                {
                    Bounds = bounds[i],
                    Cropping = cropping[i],
                    Character = characters[i],
                    LeftSideBearing = kerning[i].X,
                    Width = kerning[i].Y,
                    RightSideBearing = kerning[i].Z,
                    WidthIncludingBearings = kerning[i].X + kerning[i].Y + kerning[i].Z
                };

                glyphMap[glyph.Character] = glyph;
            }
        }

        public Vector2 MeasureString(string text)
        {
            var source = new CharacterSource(text);
            Vector2 size;
            MeasureString(ref source, out size);
            return size;
        }

        public Vector2 MeasureString(StringBuilder text)
        {
            var source = new CharacterSource(text);
            Vector2 size;
            MeasureString(ref source, out size);
            return size;
        }

        void MeasureString(ref CharacterSource text, out Vector2 size)
        {
            if (text.Length == 0)
            {
                size = Vector2.Zero;
                return;
            }

            Glyph? defaultGlyph = null;
            if (DefaultCharacter.HasValue)
            {
                defaultGlyph = glyphMap[DefaultCharacter.Value];
            }

            var width = 0.0f;
            var finalLineHeight = (float) LineSpacing;
            var fullLineCount = 0;
            var currentGlyph = Glyph.Empty;
            var offset = Vector2.Zero;
            var hasCurrentGlyph = false;

            for (var i = 0; i < text.Length; ++i)
            {
                var c = text[i];
                if (c == '\r')
                {
                    hasCurrentGlyph = false;
                    continue;
                }

                if (c == '\n')
                {
                    fullLineCount++;
                    finalLineHeight = LineSpacing;

                    offset.X = 0;
                    offset.Y = LineSpacing * fullLineCount;
                    hasCurrentGlyph = false;
                    continue;
                }

                if (hasCurrentGlyph)
                    offset.X += Spacing + currentGlyph.WidthIncludingBearings;

                hasCurrentGlyph = glyphMap.TryGetValue(c, out currentGlyph);
                if (!hasCurrentGlyph)
                {
                    if (!defaultGlyph.HasValue)
                        throw new InvalidOperationException("Can not resolve a character.");

                    currentGlyph = defaultGlyph.Value;
                    hasCurrentGlyph = true;
                }

                var proposedWidth = offset.X + currentGlyph.WidthIncludingBearings;
                if (proposedWidth > width)
                    width = proposedWidth;

                if (currentGlyph.Cropping.Height > finalLineHeight)
                    finalLineHeight = currentGlyph.Cropping.Height;
            }

            size.X = width;
            size.Y = fullLineCount * LineSpacing + finalLineHeight;
        }

        static readonly Vector2[] AxisDirections =
        {
            new Vector2(-1, -1),
            new Vector2( 1, -1),
            new Vector2(-1,  1),
            new Vector2( 1,  1)
        };

        static readonly Vector2[] AxisIsMirrored =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        internal void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth)
        {
            var sharacterSource = new CharacterSource(text);
            DrawString(spriteBatch, ref sharacterSource, ref position, ref color, rotation, ref origin, ref scale, effects, depth);
        }

        internal void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color,
            float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth)
        {
            var sharacterSource = new CharacterSource(text);
            DrawString(spriteBatch, ref sharacterSource, ref position, ref color, rotation, ref origin, ref scale, effects, depth);
        }

        void DrawString(
            SpriteBatch spriteBatch,
            ref CharacterSource text, ref Vector2 position, ref Color color,
            float rotation, ref Vector2 origin, ref Vector2 scale, SpriteEffects effects, float depth)
        {
            // MonoGame のコードとは SpriteBatch の設計が異なるため、
            // ここは MonoGame と DirectX TK の両方を参考に自作した結果。

            var baseOffset = origin;
            int mirrorBits = (int) effects & 3;

            if (effects != SpriteEffects.None)
            {
                Vector2 size;
                MeasureString(ref text, out size);

                baseOffset -= size * AxisIsMirrored[mirrorBits];
            }

            Glyph? defaultGlyph = null;
            if (DefaultCharacter.HasValue)
                defaultGlyph = glyphMap[DefaultCharacter.Value];

            var currentGlyph = Glyph.Empty;
            var offset = Vector2.Zero;
            var hasCurrentGlyph = false;
            for (var i = 0; i < text.Length; ++i)
            {
                var c = text[i];
                if (c == '\r')
                {
                    hasCurrentGlyph = false;
                    continue;
                }

                if (c == '\n')
                {
                    offset.X = 0;
                    offset.Y += LineSpacing;
                    hasCurrentGlyph = false;
                    continue;
                }

                if (hasCurrentGlyph)
                    offset.X += Spacing + currentGlyph.Width + currentGlyph.RightSideBearing;

                hasCurrentGlyph = glyphMap.TryGetValue(c, out currentGlyph);
                if (!hasCurrentGlyph)
                {
                    if (!defaultGlyph.HasValue)
                        throw new InvalidOperationException("Can not resolve a character.");

                    currentGlyph = defaultGlyph.Value;
                    hasCurrentGlyph = true;
                }
                offset.X += currentGlyph.LeftSideBearing;
                
                var finalOffset = offset;

                finalOffset.X += currentGlyph.Cropping.X;
                finalOffset.Y += currentGlyph.Cropping.Y;

                var axisDirection = AxisDirections[mirrorBits];
                finalOffset.X *= axisDirection.X;
                finalOffset.Y *= axisDirection.Y;

                finalOffset += baseOffset;

                if (effects != SpriteEffects.None)
                {
                    var glyphSize = new Vector2(currentGlyph.Bounds.Width, currentGlyph.Bounds.Height);

                    finalOffset += glyphSize * AxisIsMirrored[mirrorBits];
                }

                spriteBatch.Draw(texture, position, currentGlyph.Bounds, color, rotation, finalOffset, scale, effects, depth);
            }
        }

        #region IDisposable

        bool disposed;

        ~SpriteFont()
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
            }

            disposed = true;
        }

        #endregion
    }
}
