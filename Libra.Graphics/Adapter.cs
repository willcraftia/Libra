#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public abstract class Adapter : IDisposable
    {
        #region Output

        public abstract class Output
        {
            public abstract string DeviceName { get; }

            public abstract Rectangle DesktopCoordinates { get; }

            public abstract bool AttachedToDesktop { get; }

            public abstract IntPtr Monitor { get; }

            public abstract DisplayMode[] GetModes(SurfaceFormat format, EnumerateDisplayModes flags);

            public abstract void GetClosestMatchingMode(Device device, ref DisplayMode preferredMode, out DisplayMode result);
        }

        #endregion

        #region OutputCollection

        public sealed class OutputCollection : IEnumerable<Output>
        {
            List<Output> outputs;

            public Output this[int index]
            {
                get { return outputs[index]; }
            }

            internal OutputCollection()
            {
                outputs = new List<Output>();
            }

            internal void Add(Output output)
            {
                outputs.Add(output);
            }

            public IEnumerator<Output> GetEnumerator()
            {
                return outputs.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        /// <summary>
        /// DXGI_ADAPTER_DESC.Description。
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.VendorId。
        /// </summary>
        public abstract int VendorId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.DeviceId。
        /// </summary>
        public abstract int DeviceId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.SubSysId。
        /// </summary>
        public abstract int SubSystemId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.Revision。
        /// </summary>
        public abstract int Revision { get; }

        public OutputCollection Outputs { get; private set; }

        public abstract Output PrimaryOutput { get; }

        public bool IsDefaultAdapter { get; private set; }

        protected Adapter(bool isDefaultAdapter)
        {
            IsDefaultAdapter = isDefaultAdapter;

            Outputs = new OutputCollection();
        }

        protected void AddOutput(Output output)
        {
            Outputs.Add(output);
        }

        #region ToString

        public override string ToString()
        {
            return Description;
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~Adapter()
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
