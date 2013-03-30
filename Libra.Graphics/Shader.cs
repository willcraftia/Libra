#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class Shader : IDisposable
    {
        internal bool initialized;

        public IDevice Device { get; private set; }

        public string Name { get; set; }

        protected internal byte[] ShaderBytecode { get; private set; }

        protected Shader(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize(byte[] shaderBytecode)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (shaderBytecode == null) throw new ArgumentNullException("shaderBytecode");
            if (shaderBytecode.Length == 0) throw new ArgumentOutOfRangeException("shaderBytecode.Length");

            ShaderBytecode = shaderBytecode;

            InitializeCore();

            initialized = true;
        }

        protected abstract void InitializeCore();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~Shader()
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
