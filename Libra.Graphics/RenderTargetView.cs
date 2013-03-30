#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTargetView : IShaderResourceView, IDisposable
    {
        bool initialized;

        public IDevice Device { get; private set; }

        public RenderTarget RenderTarget { get; private set; }

        public DepthStencilView DepthStencilView { get; private set; }

        Resource IShaderResourceView.Resource
        {
            get { return RenderTarget; }
        }

        protected RenderTargetView(IDevice device)
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

            if (RenderTarget.DepthStencil != null)
                DepthStencilView = InitializeDepthStencilView();

            initialized = true;
        }

        protected abstract void InitializeRenderTargetView();

        protected abstract DepthStencilView InitializeDepthStencilView();

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
