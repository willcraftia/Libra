#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IAdapter
    {
        /// <summary>
        /// DXGI_ADAPTER_DESC.Description。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.VendorId。
        /// </summary>
        int VendorId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.DeviceId。
        /// </summary>
        int DeviceId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.SubSysId。
        /// </summary>
        int SubSystemId { get; }

        /// <summary>
        /// DXGI_ADAPTER_DESC.Revision。
        /// </summary>
        int Revision { get; }

        IOutputCollection Outputs { get; }

        IOutput PrimaryOutput { get; }

        bool IsDefaultAdapter { get; }
    }
}
