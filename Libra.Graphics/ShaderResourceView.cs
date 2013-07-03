#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class ShaderResourceView : IDisposable
    {
        bool initialized;

        public Device Device { get; private set; }

        public Resource Resource { get; private set; }

        protected ShaderResourceView(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize(Resource resource)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (resource == null) throw new ArgumentNullException("resource");

            Resource = resource;

            InitializeCore();

            initialized = true;
        }

        protected abstract void InitializeCore();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~ShaderResourceView()
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
