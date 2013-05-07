#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// 被写界深度を考慮したシーンを描画するクラスです。
    /// </summary>
    public sealed class DepthOfField : IDisposable
    {
        /// <summary>
        /// 被写界深度エフェクト。
        /// </summary>
        DepthOfFieldEffect effect;

        /// <summary>
        /// フルスクリーン クアッド。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// デバイス。
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// 焦点距離を取得または設定します。
        /// </summary>
        public float FocusDistance
        {
            get { return effect.FocusDistance; }
            set { effect.FocusDistance = value; }
        }

        /// <summary>
        /// 焦点範囲を取得または設定します。
        /// </summary>
        public float FocusRange
        {
            get { return effect.FocusRange; }
            set { effect.FocusRange = value; }
        }

        /// <summary>
        /// 表示カメラの射影行列を取得または設定します。
        /// </summary>
        public Matrix Projection
        {
            get { return effect.Projection; }
            set { effect.Projection = value; }
        }

        public DepthOfField(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            effect = new DepthOfFieldEffect(device);

            fullScreenQuad = new FullScreenQuad(Device);
        }

        /// <summary>
        /// 深度マップを参照して、通常シーンとブラー済みシーンを合成し、
        /// 現在設定されているレンダ ターゲットへ結果を描画します。
        /// </summary>
        /// <param name="context">デバイス コンテキスト。</param>
        /// <param name="normalSceneMap">通常シーン。</param>
        /// <param name="bluredSceneMap">ブラー済みシーン。</param>
        /// <param name="depthMap">深度マップ。</param>
        public void Draw(
            DeviceContext context,
            ShaderResourceView normalSceneMap,
            ShaderResourceView bluredSceneMap,
            ShaderResourceView depthMap)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (normalSceneMap == null) throw new ArgumentNullException("normalSceneMap");
            if (bluredSceneMap == null) throw new ArgumentNullException("bluredSceneMap");
            if (depthMap == null) throw new ArgumentNullException("depthMap");

            // ステートの記録。
            var previousBlendState = context.BlendState;
            var previousDepthStencilState = context.DepthStencilState;
            var previousRasterizerState = context.RasterizerState;
            var previousSamplerState0 = context.PixelShaderSamplers[0];
            var previousSamplerState1 = context.PixelShaderSamplers[1];
            var previousSamplerState2 = context.PixelShaderSamplers[2];

            // ステートの設定。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.None;
            context.RasterizerState = RasterizerState.CullBack;
            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[2] = SamplerState.PointClamp;

            // エフェクトの設定。
            effect.NormalSceneMap = normalSceneMap;
            effect.BluredSceneMap = bluredSceneMap;
            effect.DepthMap = depthMap;
            effect.Apply(context);

            // 描画。
            fullScreenQuad.Draw(context);

            // ステートを以前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
            context.PixelShaderSamplers[0] = previousSamplerState0;
            context.PixelShaderSamplers[1] = previousSamplerState1;
            context.PixelShaderSamplers[2] = previousSamplerState2;
        }

        #region IDisposable

        bool disposed;

        ~DepthOfField()
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
                effect.Dispose();
                fullScreenQuad.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
