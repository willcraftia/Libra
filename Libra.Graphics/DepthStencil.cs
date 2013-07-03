#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class DepthStencil : Texture2D
    {
        DepthStencilView depthStencilView;

        protected DepthStencil(Device device)
            : base(device)
        {
            Format = SurfaceFormat.Depth24Stencil8;
        }

        /// <summary>
        /// 暗黙的に GetDepthStencilView() を呼び出して DepthStencilView 型とします。
        /// </summary>
        /// <param name="depthStencil">DepthStencil。</param>
        /// <returns>DepthStencil 内部で管理する DepthStencilView。</returns>
        public static implicit operator DepthStencilView(DepthStencil depthStencil)
        {
            if (depthStencil == null) return null;

            return depthStencil.GetDepthStencilView();
        }

        public DepthStencilView GetDepthStencilView()
        {
            if (depthStencilView == null)
            {
                depthStencilView = Device.CreateDepthStencilView();
                depthStencilView.Initialize(this);
            }
            return depthStencilView;
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (depthStencilView != null)
                    depthStencilView.Dispose();
            }

            base.DisposeOverride(disposing);
        }
    }
}
