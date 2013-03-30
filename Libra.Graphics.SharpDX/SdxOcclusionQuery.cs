#region Using

using System;

using D3D11AsynchronousFlags = SharpDX.Direct3D11.AsynchronousFlags;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11Query = SharpDX.Direct3D11.Query;
using D3D11QueryDescription = SharpDX.Direct3D11.QueryDescription;
using D3D11QueryFlags = SharpDX.Direct3D11.QueryFlags;
using D3D11QueryType = SharpDX.Direct3D11.QueryType;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxOcclusionQuery : OcclusionQuery
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11Query D3D11Query { get; private set; }

        public SdxOcclusionQuery(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            var description = new D3D11QueryDescription
            {
                Type = D3D11QueryType.Occlusion,
                Flags = D3D11QueryFlags.None
            };

            D3D11Query = new D3D11Query(D3D11Device, description);
        }

        protected override void BeginCore(DeviceContext context)
        {
            (context as SdxDeviceContext).D3D11DeviceContext.Begin(D3D11Query);
        }

        protected override void EndCore(DeviceContext context)
        {
            (context as SdxDeviceContext).D3D11DeviceContext.End(D3D11Query);
        }

        protected override bool PullQueryResult(DeviceContext context, out ulong result)
        {
            var d3d11DeviceContext = (context as SdxDeviceContext).D3D11DeviceContext;
            return d3d11DeviceContext.GetData(D3D11Query, D3D11AsynchronousFlags.None, out result);
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11Query != null)
                    D3D11Query.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
