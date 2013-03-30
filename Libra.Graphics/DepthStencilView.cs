#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class DepthStencilView : IDisposable
    {
        bool initialized;

        public IDevice Device { get; private set; }

        public DepthStencil DepthStencil { get; private set; }

        protected DepthStencilView(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize(DepthStencil depthStencil)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (depthStencil == null) throw new ArgumentNullException("depthStencil");

            DepthStencil = depthStencil;

            InitializeCore();

            initialized = true;
        }

        protected abstract void InitializeCore();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~DepthStencilView()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
