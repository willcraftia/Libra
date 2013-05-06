#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DepthOfField : IDisposable
    {
        DepthOfFieldEffect effect;

        FullScreenQuad fullScreenQuad;

        public Device Device { get; private set; }

        public float FocusDistance
        {
            get { return effect.FocusDistance; }
            set { effect.FocusDistance = value; }
        }

        public float FocusRange
        {
            get { return effect.FocusRange; }
            set { effect.FocusRange = value; }
        }

        public float NearClipDistance
        {
            get { return effect.NearClipDistance; }
            set { effect.NearClipDistance = value; }
        }

        public float FarClipDistance
        {
            get { return effect.FarClipDistance; }
            set { effect.FarClipDistance = value; }
        }

        public DepthOfField(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            effect = new DepthOfFieldEffect(device);

            fullScreenQuad = new FullScreenQuad(Device);
        }

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
            var previousSamplerState = context.PixelShaderSamplers[0];

            // ステートの設定。
            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.None;
            context.RasterizerState = RasterizerState.CullBack;
            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

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
            context.PixelShaderSamplers[0] = previousSamplerState;
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
