#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class ShaderResourceView : IDisposable
    {
        bool initialized;

        public IDevice Device { get; private set; }

        public Resource Resource { get; private set; }

        protected ShaderResourceView(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize(Texture2D texture)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (texture == null) throw new ArgumentNullException("texture");

            Resource = texture;

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
