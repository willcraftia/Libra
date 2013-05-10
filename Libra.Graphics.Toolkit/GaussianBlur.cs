#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン ブラー シェーダをポストプロセスとして構築するアダプタ クラスです。
    /// </summary>
    public sealed class GaussianBlur : IPostprocessor
    {
        public GaussianBlurCore Core { get; private set; }

        public GaussianBlurPass Pass { get; private set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public GaussianBlur(GaussianBlurCore core, GaussianBlurPass pass)
        {
            if (core == null) throw new ArgumentNullException("core");

            Core = core;
            Pass = pass;

            TextureSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Core.Texture = Texture;
            Core.TextureSampler = TextureSampler;
            Core.Pass = Pass;
            Core.Apply(context);
        }
    }
}
