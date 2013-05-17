#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン ブラー シェーダをポストプロセス パスとして構築するアダプタ クラスです。
    /// </summary>
    public sealed class GaussianFilter : IPostprocessPass
    {
        public GaussianFilterCore Core { get; private set; }

        public GaussianFilterPass Pass { get; private set; }

        public bool Enabled { get; set; }

        public GaussianFilter(GaussianFilterCore core, GaussianFilterPass pass)
        {
            if (core == null) throw new ArgumentNullException("core");

            Core = core;
            Pass = pass;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Core.Pass = Pass;
            Core.Apply(context);
        }
    }
}
