﻿#region Using

using System;
using System.IO;

using D3D11BindFlags = SharpDX.Direct3D11.BindFlags;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11ResourceOptionFlags = SharpDX.Direct3D11.ResourceOptionFlags;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using D3D11Texture2DDescription = SharpDX.Direct3D11.Texture2DDescription;
using DXGIFormat = SharpDX.DXGI.Format;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxDepthStencil : DepthStencil
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11Texture2D D3D11Texture2D { get; private set; }

        public SdxDepthStencil(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            AssertValidState();

            D3D11Texture2DDescription description;
            CreateD3D11Texture2DDescription(out description);

            D3D11Texture2D = new D3D11Texture2D(D3D11Device, description);
        }

        protected override void InitializeCore(Stream stream)
        {
            throw new NotImplementedException();
        }

        void AssertValidState()
        {
            switch (Format)
            {
                case SurfaceFormat.Depth24Stencil8:
                case SurfaceFormat.Depth16:
                    break;
                default:
                    throw new InvalidOperationException(
                        string.Format("The depth stencil does not support the specfied format '{0}'.", Format));
            }

            if (1 < ArraySize)
                throw new InvalidOperationException("The depth stencil does not support the array of textures.");
        }

        void CreateD3D11Texture2DDescription(out D3D11Texture2DDescription result)
        {
            result = new D3D11Texture2DDescription
            {
                Width = Width,
                Height = Height,
                // ミップ レベルは 1 で固定。
                MipLevels = 1,
                ArraySize = 1,
                //Format = (DXGIFormat) Format,
                Format = ResolveTextureFormat(),
                SampleDescription =
                {
                    Count = MultisampleCount,
                    Quality = MultisampleQuality
                },
                Usage = (D3D11ResourceUsage) Usage,
                BindFlags = D3D11BindFlags.ShaderResource | D3D11BindFlags.DepthStencil,
                CpuAccessFlags = ResourceHelper.GetD3D11CpuAccessFlags((D3D11ResourceUsage) Usage),
                // ミップ マップ生成無しで固定。
                OptionFlags = D3D11ResourceOptionFlags.None
            };
        }

        DXGIFormat ResolveTextureFormat()
        {
            switch (Format)
            {
                case SurfaceFormat.Depth16:
                    return DXGIFormat.R16_Typeless;
                case SurfaceFormat.Depth24Stencil8:
                    return DXGIFormat.R24G8_Typeless;
                default:
                    throw new InvalidOperationException(string.Format("Unexpected format '{0}'", Format));
            }
        }
    }
}
