#region Using

using System;
using System.Diagnostics;

#endregion

namespace Libra.Games.Debugging
{
    /// <summary>
    /// フレーム レートを計測するコンポーネントです。
    /// </summary>
    public sealed class FrameRateMeasure : DrawableGameComponent
    {
        double lastUpdateTime;

        int frameCount;

        float frameRate;

        /// <summary>
        /// フレーム レートを取得します。
        /// </summary>
        public float FrameRate
        {
            get { return frameRate; }
        }

        public FrameRateMeasure(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            frameRate = 0;
            frameCount = 0;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            double totalGameTime = gameTime.TotalGameTime.TotalSeconds;
            double elapsedTime = totalGameTime - lastUpdateTime;

            if (1.0 < elapsedTime)
            {
                frameRate = (float) (frameCount / elapsedTime);
                lastUpdateTime = totalGameTime;
                frameCount = 0;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            frameCount++;

            base.Draw(gameTime);
        }
    }
}
