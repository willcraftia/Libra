#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTargetView : IDisposable
    {
        bool initialized;

        public Device Device { get; private set; }

        public RenderTarget RenderTarget { get; private set; }

        public DepthStencilView DepthStencilView
        {
            get
            {
                if (RenderTarget.DepthStencil == null)
                {
                    return null;
                }
                else
                {
                    return RenderTarget.DepthStencil.GetDepthStencilView();
                }
            }
        }

        protected RenderTargetView(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize(RenderTarget renderTarget)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            RenderTarget = renderTarget;

            InitializeRenderTargetView();

            initialized = true;
        }

        protected abstract void InitializeRenderTargetView();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~RenderTargetView()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (DepthStencilView != null)
                    DepthStencilView.Dispose();
            }
        }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
