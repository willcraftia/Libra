#region Using

using System;

using DXGIAdapter1 = SharpDX.DXGI.Adapter1;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxAdapter : IAdapter
    {
        SdxOutputCollection outputs;

        public DXGIAdapter1 DXGIAdapter { get; private set; }

        public string Description { get; private set; }

        public int VendorId { get; private set; }

        public int DeviceId { get; private set; }

        public int SubSystemId { get; private set; }

        public int Revision { get; private set; }

        public IOutputCollection Outputs
        {
            get { return outputs; }
        }

        public IOutput PrimaryOutput { get; private set; }

        public bool IsDefaultAdapter { get; private set; }

        public SdxAdapter(DXGIAdapter1 dxgiAdapter, bool isDefaultAdapter)
        {
            DXGIAdapter = dxgiAdapter;
            IsDefaultAdapter = isDefaultAdapter;

            outputs = new SdxOutputCollection(dxgiAdapter.Outputs.Length);

            var adapterDescription = dxgiAdapter.Description1;

            Description = adapterDescription.Description;
            VendorId = adapterDescription.VendorId;
            DeviceId = adapterDescription.DeviceId;
            SubSystemId = adapterDescription.SubsystemId;
            Revision = adapterDescription.Revision;

            for (int i = 0; i < dxgiAdapter.Outputs.Length; i++)
            {
                var output = new SdxOutput(dxgiAdapter.Outputs[i]);
                outputs.Add(output);
            }

            if (0 < dxgiAdapter.Outputs.Length)
                PrimaryOutput = Outputs[0];
        }

        #region ToString

        public override string ToString()
        {
            return Description;
        }

        #endregion
    }
}
