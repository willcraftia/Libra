#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Libra.Graphics;

#endregion

namespace Libra.Games.Debugging
{
    /// <summary>
    /// テクスチャを画面へ一覧表示するゲーム コンポーネントです。
    /// 描画の最中に生成する中間テクスチャの確認のためなどに利用します。
    /// </summary>
    public sealed class TextureDisplay : DrawableGameComponent
    {
        #region TextureCollection

        public sealed class TextureCollection : Collection<ShaderResourceView>
        {
            internal TextureCollection(int capacity)
                : base(new List<ShaderResourceView>(capacity))
            {
            }
        }

        #endregion

        SpriteBatch spriteBatch;

        int textureWidth;

        int textureHeight;

        Point offset;

        /// <summary>
        /// テクスチャの描画幅を取得または設定します。
        /// </summary>
        public int TextureWidth
        {
            get { return textureWidth; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                textureWidth = value;
            }
        }

        /// <summary>
        /// テクスチャの描画高さを取得または設定します。
        /// </summary>
        public int TextureHeight
        {
            get { return textureHeight; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                textureHeight = value;
            }
        }

        /// <summary>
        /// 一覧表示のオフセット位置を取得または設定します。
        /// </summary>
        public Point Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        /// <summary>
        /// 一覧表示するテクスチャのコレクションを取得します。
        /// </summary>
        public TextureCollection Textures { get; private set; }

        public TextureDisplay(Game game)
            : base(game)
        {
            textureWidth = 128;
            textureHeight = 128;
            Textures = new TextureCollection(10);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // 登録されているテクスチャを破棄。
            Textures.Clear();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;
            var viewport = context.Viewport;
            var rect = new Rectangle(offset.X, offset.Y, textureWidth, textureHeight);

            for (int i = 0; i < Textures.Count; i++)
            {
                var texture = Textures[i];

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                spriteBatch.Draw(texture, rect, Color.White);
                spriteBatch.End();

                rect.X += offset.X + textureWidth;

                if (viewport.Width < rect.X + textureWidth)
                {
                    rect.X = offset.X;
                    rect.Y += offset.Y + textureHeight;
                }
            }

            base.Draw(gameTime);
        }
    }
}
