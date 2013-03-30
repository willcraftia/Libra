#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class OcclusionQuery : IDisposable
    {
        bool initialized;

        DeviceContext currentContext;

        bool inBeginEndPair;

        bool isQueryResultStillOutstanding;

        public IDevice Device { get; private set; }

        public bool IsComplete
        {
            get
            {
                AssertInitialized();
                if (isQueryResultStillOutstanding)
                {
                    ulong result;
                    if (!PullQueryResult(currentContext, out result))
                    {
                        return false;
                    }
                    else
                    {
                        PixelCount = result;
                    }

                    currentContext = null;
                    isQueryResultStillOutstanding = false;
                }

                return true;
            }
        }

        public ulong PixelCount { get; private set; }

        protected OcclusionQuery(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public void Initialize()
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");

            InitializeCore();

            initialized = true;
        }

        public void Begin(DeviceContext context = null)
        {
            AssertInitialized();
            if (inBeginEndPair)
                throw new InvalidOperationException("Cannot nest Begin calls on a single OcclusionQuery");

            currentContext = context ?? Device.ImmediateContext;

            BeginCore(currentContext);

            isQueryResultStillOutstanding = true;
            PixelCount = 0;

            inBeginEndPair = true;
        }

        public void End()
        {
            AssertInitialized();
            if (!inBeginEndPair)
                throw new InvalidOperationException("Begin must be called before End");

            EndCore(currentContext);

            inBeginEndPair = false;
        }

        protected abstract void InitializeCore();

        protected abstract void BeginCore(DeviceContext context);

        protected abstract void EndCore(DeviceContext context);

        protected abstract bool PullQueryResult(DeviceContext context, out ulong result);

        void AssertInitialized()
        {
            if (!initialized) throw new InvalidOperationException("Not initialized.");
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~OcclusionQuery()
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
