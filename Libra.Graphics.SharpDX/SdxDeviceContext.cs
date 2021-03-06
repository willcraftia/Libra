﻿#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using D3D11BindFlags = SharpDX.Direct3D11.BindFlags;
using D3D11Buffer = SharpDX.Direct3D11.Buffer;
using D3D11BufferDescription = SharpDX.Direct3D11.BufferDescription;
using D3D11CommonShaderStage = SharpDX.Direct3D11.CommonShaderStage;
using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11DepthStencilClearFlags = SharpDX.Direct3D11.DepthStencilClearFlags;
using D3D11DepthStencilView = SharpDX.Direct3D11.DepthStencilView;
using D3D11DeviceContext = SharpDX.Direct3D11.DeviceContext;
using D3D11DeviceContextType = SharpDX.Direct3D11.DeviceContextType;
using D3D11ImageFileFormat = SharpDX.Direct3D11.ImageFileFormat;
using D3D11InputLayout = SharpDX.Direct3D11.InputLayout;
using D3D11MapFlags = SharpDX.Direct3D11.MapFlags;
using D3D11MapMode = SharpDX.Direct3D11.MapMode;
using D3D11PixelShader = SharpDX.Direct3D11.PixelShader;
using D3D11PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology;
using D3D11RasterizerState = SharpDX.Direct3D11.RasterizerState;
using D3D11RenderTargetView = SharpDX.Direct3D11.RenderTargetView;
using D3D11Resource = SharpDX.Direct3D11.Resource;
using D3D11ResourceOptionFlags = SharpDX.Direct3D11.ResourceOptionFlags;
using D3D11ResourceRegion = SharpDX.Direct3D11.ResourceRegion;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;
using D3D11SamplerState = SharpDX.Direct3D11.SamplerState;
using D3D11ShaderResourceView = SharpDX.Direct3D11.ShaderResourceView;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using D3D11Texture2DDescription = SharpDX.Direct3D11.Texture2DDescription;
using D3D11VertexBufferBinding = SharpDX.Direct3D11.VertexBufferBinding;
using D3D11VertexShader = SharpDX.Direct3D11.VertexShader;
using DXGIFormat = SharpDX.DXGI.Format;
using SDXColor4 = SharpDX.Color4;
using SDXDataBox = SharpDX.DataBox;
using SDXRectangle = SharpDX.Rectangle;
using SDXUtilities = SharpDX.Utilities;
using SDXViewportF = SharpDX.ViewportF;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxDeviceContext : DeviceContext
    {
        SdxDevice device;

        bool deferred;

        // 作業用配列。
        D3D11RenderTargetView[] d3d11RenderTargetViews;

        D3D11VertexBufferBinding[] d3d11VertexBufferBindings;

        public override bool Deferred
        {
            get { return deferred; }
        }

        public D3D11DeviceContext D3D11DeviceContext { get; private set; }

        public SdxDeviceContext(SdxDevice device, D3D11DeviceContext d3d11DeviceContext)
            : base(device)
        {
            if (d3d11DeviceContext == null) throw new ArgumentNullException("d3d11DeviceContext");

            this.device = device;
            D3D11DeviceContext = d3d11DeviceContext;

            deferred = (d3d11DeviceContext.TypeInfo == D3D11DeviceContextType.Deferred);

            d3d11RenderTargetViews = new D3D11RenderTargetView[RenderTargetCount];
            d3d11VertexBufferBindings = new D3D11VertexBufferBinding[VertexInputResourceSlotCount];
        }

        protected override void OnInputLayoutChanged()
        {
            D3D11InputLayout d3d11InputLayout = null;
            if (InputLayout != null) d3d11InputLayout = (InputLayout as SdxInputLayout).D3D11InputLayout;

            D3D11DeviceContext.InputAssembler.InputLayout = d3d11InputLayout;
        }

        protected override void OnPrimitiveTopologyChanged()
        {
            D3D11DeviceContext.InputAssembler.PrimitiveTopology = (D3D11PrimitiveTopology) PrimitiveTopology;
        }

        protected override void OnIndexBufferChanged()
        {
            D3D11Buffer d3d11Buffer = null;
            DXGIFormat dxgiFormat = DXGIFormat.Unknown;

            if (IndexBuffer != null)
            {
                d3d11Buffer = (IndexBuffer as SdxIndexBuffer).D3D11Buffer;
                dxgiFormat = (DXGIFormat) IndexBuffer.Format;
            }

            D3D11DeviceContext.InputAssembler.SetIndexBuffer(d3d11Buffer, dxgiFormat, 0);
        }

        protected override void SetVertexBufferCore(int slot, ref VertexBufferBinding binding)
        {
            D3D11Buffer d3d11Buffer = null;
            int stride = 0;

            // 頂点バッファに null が指定される場合もある。
            if (binding.VertexBuffer != null)
            {
                d3d11Buffer = (binding.VertexBuffer as SdxVertexBuffer).D3D11Buffer;
                stride = binding.VertexBuffer.VertexDeclaration.Stride;
            }

            var d3d11VertexBufferBinding = new D3D11VertexBufferBinding
            {
                Buffer = d3d11Buffer,
                Offset = binding.Offset,
                Stride = stride
            };

            D3D11DeviceContext.InputAssembler.SetVertexBuffers(slot, d3d11VertexBufferBinding);
        }

        protected override void SetVertexBuffersCore(VertexBufferBinding[] bindings)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                D3D11Buffer d3d11Buffer = null;
                int stride = 0;

                // 頂点バッファに null が指定される場合もある。
                if (bindings[i].VertexBuffer != null)
                {
                    d3d11Buffer = (bindings[i].VertexBuffer as SdxVertexBuffer).D3D11Buffer;
                    stride = bindings[i].VertexBuffer.VertexDeclaration.Stride;
                }

                d3d11VertexBufferBindings[i] = new D3D11VertexBufferBinding
                {
                    Buffer = d3d11Buffer,
                    Offset = bindings[i].Offset,
                    Stride = stride
                };
            }

            D3D11DeviceContext.InputAssembler.SetVertexBuffers(0, d3d11VertexBufferBindings);
        }

        protected override void OnRasterizerStateChanged()
        {
            D3D11RasterizerState d3d11RasterizerState = null;
            if (RasterizerState != null) d3d11RasterizerState = (Device as SdxDevice).RasterizerStateManager[RasterizerState];

            D3D11DeviceContext.Rasterizer.State = d3d11RasterizerState;
        }

        protected override void OnViewportChanged()
        {
            var viewport = Viewport;
            var sdxViewportF = new SDXViewportF(
                viewport.X, viewport.Y,
                viewport.Width, viewport.Height,
                viewport.MinDepth, viewport.MaxDepth);

            D3D11DeviceContext.Rasterizer.SetViewports(sdxViewportF);
        }

        protected override void OnScissorRectangleChanged()
        {
            var rectangle = ScissorRectangle;
            var sdxRectangle = new SDXRectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);

            D3D11DeviceContext.Rasterizer.SetScissorRectangles(sdxRectangle);
        }

        protected override void OnBlendStateChanged()
        {
            if (BlendState == null)
            {
                D3D11DeviceContext.OutputMerger.SetBlendState(null, BlendFactor.ToSDXColor4(), -1);
            }
            else
            {
                var d3d11BlendState = (Device as SdxDevice).BlendStateManager[BlendState];
                D3D11DeviceContext.OutputMerger.SetBlendState(
                    d3d11BlendState, BlendState.BlendFactor.ToSDXColor4(), BlendState.MultiSampleMask);
            }
        }

        protected override void OnDepthStencilStateChanged()
        {
            if (DepthStencilState == null)
            {
                D3D11DeviceContext.OutputMerger.SetDepthStencilState(null);
            }
            else
            {
                var d3d11DepthStancilState = (Device as SdxDevice).DepthStencilStateManager[DepthStencilState];

                D3D11DeviceContext.OutputMerger.SetDepthStencilState(
                    d3d11DepthStancilState, DepthStencilState.ReferenceStencil);
            }
        }

        protected override void SetRenderTargetsCore(DepthStencilView depthStencilView, RenderTargetView[] renderTargetViews)
        {
            // renderTargetViews == null の場合は depthStencilView == null である。
            // renderTargetViews != null の場合は depthStencilView は null かもしれないし非 null かもしれない。

            if (renderTargetViews == null)
            {
                // レンダ ターゲットと深度ステンシルの解除。
                D3D11DeviceContext.OutputMerger.SetTargets((D3D11DepthStencilView) null, (D3D11RenderTargetView[]) null);

                var d3d11RenderTargetView = (BackBufferView as SdxRenderTargetView).D3D11RenderTargetView;
                D3D11DepthStencilView d3d11DepthStencilView = null;
                if (BackBufferView.DepthStencilView != null)
                {
                    d3d11DepthStencilView = (BackBufferView.DepthStencilView as SdxDepthStencilView).D3D11DepthStencilView;
                }

                // バック バッファのレンダ ターゲットと深度ステンシルの設定。
                D3D11DeviceContext.OutputMerger.SetTargets(d3d11DepthStencilView, d3d11RenderTargetView);
            }
            else
            {
                D3D11DepthStencilView d3d11DepthStencilView = null;
                if (depthStencilView != null)
                {
                    d3d11DepthStencilView = (depthStencilView as SdxDepthStencilView).D3D11DepthStencilView;
                }

                // TODO
                //
                // MRT の場合に各レンダ ターゲット間の整合性 (サイズ等) を確認すべき。

                // インタフェース差異のため、D3D 実体参照を作業配列へコピー。
                int renderTargetCount = renderTargetViews.Length;
                for (int i = 0; i < d3d11RenderTargetViews.Length; i++)
                {
                    var sdxRenderTargetView = renderTargetViews[i] as SdxRenderTargetView;
                    if (sdxRenderTargetView != null)
                    {
                        d3d11RenderTargetViews[i] = sdxRenderTargetView.D3D11RenderTargetView;
                    }
                    else
                    {
                        d3d11RenderTargetViews[i] = null;
                    }
                }

                D3D11DeviceContext.OutputMerger.SetTargets(d3d11DepthStencilView, d3d11RenderTargetViews);

                // 参照を残さないために作業配列をクリア。
                Array.Clear(d3d11RenderTargetViews, 0, renderTargetCount);
            }
        }

        protected override void OnVertexShaderChanged()
        {
            D3D11VertexShader d3d11VertexShader = null;
            if (VertexShader != null) d3d11VertexShader = (VertexShader as SdxVertexShader).D3D11VertexShader;

            D3D11DeviceContext.VertexShader.Set(d3d11VertexShader);
        }

        protected override void OnPixelShaderChanged()
        {
            D3D11PixelShader d3d11PixelShader = null;
            if (PixelShader != null) d3d11PixelShader = (PixelShader as SdxPixelShader).D3D11PixelShader;

            D3D11DeviceContext.PixelShader.Set(d3d11PixelShader);
        }

        protected override void SetConstantBufferCore(ShaderStage shaderStage, int slot, ConstantBuffer buffer)
        {
            D3D11Buffer d3d11Buffer = null;
            if (buffer != null) d3d11Buffer = (buffer as SdxConstantBuffer).D3D11Buffer;

            GetD3D11CommonShaderStage(shaderStage).SetConstantBuffer(slot, d3d11Buffer);
        }

        protected override void SetSamplerCore(ShaderStage shaderStage, int slot, SamplerState state)
        {
            D3D11SamplerState d3dSamplerState = null;
            if (state != null) d3dSamplerState = device.SamplerStateManager[state];

            GetD3D11CommonShaderStage(shaderStage).SetSampler(slot, d3dSamplerState);
        }

        protected override void SetShaderResourceCore(ShaderStage shaderStage, int slot, ShaderResourceView view)
        {
            D3D11ShaderResourceView d3d11ShaderResourceView = null;
            if (view != null)
            {
                d3d11ShaderResourceView = (view as SdxShaderResourceView).D3D11ShaderResourceView;
            }
            GetD3D11CommonShaderStage(shaderStage).SetShaderResource(slot, d3d11ShaderResourceView);
        }

        protected override void GetDataCore<T>(ConstantBuffer constantBuffer, out T data)
        {
            var stagingDescription = new D3D11BufferDescription
            {
                SizeInBytes = constantBuffer.ByteWidth,
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            using (var staging = new D3D11Buffer(device.D3D11Device, stagingDescription))
            {
                D3D11DeviceContext.CopyResource((constantBuffer as SdxConstantBuffer).D3D11Buffer, staging);

                var mappedResource = D3D11DeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                try
                {
                    // data はスタックに作る事になるので、CopyMemory ではない。
                    data = (T) Marshal.PtrToStructure(mappedResource.DataPointer, typeof(T));
                }
                finally
                {
                    D3D11DeviceContext.UnmapSubresource(staging, 0);
                }
            }
        }

        protected override void GetDataCore<T>(
            Texture2D texture,
            int arrayIndex,
            int mipLevel,
            Rectangle? rectangle,
            T[] data,
            int startIndex,
            int elementCount)
        {
            int w;
            int h;

            if (rectangle.HasValue)
            {
                // 矩形が設定されているならば、これにサイズを合わせる。
                w = rectangle.Value.Width;
                h = rectangle.Value.Height;
            }
            else
            {
                // ミップマップのサイズ。
                w = texture.Width >> mipLevel;
                h = texture.Height >> mipLevel;
            }

            var stagingDescription = new D3D11Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = (DXGIFormat) texture.Format,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None
            };

            D3D11ResourceRegion? d3d11ResourceRegion = null;
            if (rectangle.HasValue)
            {
                d3d11ResourceRegion = new D3D11ResourceRegion
                {
                    Left = rectangle.Value.Left,
                    Top = rectangle.Value.Top,
                    Front = 0,
                    Right = rectangle.Value.Right,
                    Bottom = rectangle.Value.Bottom,
                    Back = 1
                };
            }

            using (var staging = new D3D11Texture2D(device.D3D11Device, stagingDescription))
            {
                var d3d11Texture2D = GetD3D11Texture2D(texture);

                var subresourceIndex = Resource.CalculateSubresource(mipLevel, arrayIndex, texture.MipLevels);
                D3D11DeviceContext.CopySubresourceRegion(d3d11Texture2D, subresourceIndex, d3d11ResourceRegion, staging, 0);

                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPtr = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var destinationRowPitch = sizeOfT * w;

                    var dataBox = D3D11DeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        // Texture2D に格納されたデータの整列 (マップで得られる DataBox の RowPitch) は、
                        // 必ずしも頭の中で期待する状態であるとは限らない。
                        // データ取得先の RowPitch と異なる場合には、メモリの一括複製では済まず、
                        // 行ごとにポインタを動かしながら複製する必要がある。

                        if (dataBox.RowPitch == destinationRowPitch)
                        {
                            SDXUtilities.CopyMemory(destinationPtr, dataBox.DataPointer, sizeInBytes);
                        }
                        else
                        {
                            var sourcePtr = dataBox.DataPointer;
                            for (int i = 0; i < h; i++)
                            {
                                SDXUtilities.CopyMemory(destinationPtr, sourcePtr, destinationRowPitch);
                                destinationPtr += destinationRowPitch;
                                sourcePtr += dataBox.RowPitch;
                            }
                        }
                    }
                    finally
                    {
                        D3D11DeviceContext.UnmapSubresource(staging, 0);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }

        protected override void SaveCore(Texture2D texture, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            var d3d11Texture2D = GetD3D11Texture2D(texture);

            D3D11Resource.ToStream(D3D11DeviceContext, d3d11Texture2D, (D3D11ImageFileFormat) format, stream);
        }

        protected override void GetDataCore<T>(VertexBuffer vertexBuffer, T[] data, int startIndex, int elementCount)
        {
            var stagingDescription = new D3D11BufferDescription
            {
                SizeInBytes = vertexBuffer.ByteWidth,
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            using (var staging = new D3D11Buffer(device.D3D11Device, stagingDescription))
            {
                D3D11DeviceContext.CopyResource((vertexBuffer as SdxVertexBuffer).D3D11Buffer, staging);

                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPtr = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var mappedResource = D3D11DeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        SDXUtilities.CopyMemory(destinationPtr, mappedResource.DataPointer, sizeInBytes);
                    }
                    finally
                    {
                        D3D11DeviceContext.UnmapSubresource(staging, 0);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }

        protected override void GetDataCore<T>(IndexBuffer indexBuffer, T[] data, int startIndex, int elementCount)
        {
            var stagingDescription = new D3D11BufferDescription
            {
                SizeInBytes = indexBuffer.ByteWidth,
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            using (var staging = new D3D11Buffer(device.D3D11Device, stagingDescription))
            {
                D3D11DeviceContext.CopyResource((indexBuffer as SdxIndexBuffer).D3D11Buffer, staging);

                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPtr = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var mappedResource = D3D11DeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        SDXUtilities.CopyMemory(destinationPtr, mappedResource.DataPointer, sizeInBytes);
                    }
                    finally
                    {
                        D3D11DeviceContext.UnmapSubresource(staging, 0);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }

        protected override void GenerateMipsCore(ShaderResourceView shaderResourceView)
        {
            D3D11DeviceContext.GenerateMips((shaderResourceView as SdxShaderResourceView).D3D11ShaderResourceView);
        }

        D3D11Texture2D GetD3D11Texture2D(Texture2D texture)
        {
            if (texture is SdxTexture2D)
            {
                return (texture as SdxTexture2D).D3D11Texture2D;
            }
            else if (texture is SdxRenderTarget)
            {
                return (texture as SdxRenderTarget).D3D11Texture2D;
            }
            else if (texture is SdxDepthStencil)
            {
                return (texture as SdxDepthStencil).D3D11Texture2D;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        D3D11CommonShaderStage GetD3D11CommonShaderStage(ShaderStage shaderStage)
        {
            switch (shaderStage)
            {
                case ShaderStage.Vertex:
                    return D3D11DeviceContext.VertexShader;
                case ShaderStage.Hull:
                    return D3D11DeviceContext.HullShader;
                case ShaderStage.Domain:
                    return D3D11DeviceContext.DomainShader;
                case ShaderStage.Geometry:
                    return D3D11DeviceContext.GeometryShader;
                case ShaderStage.Pixel:
                    return D3D11DeviceContext.PixelShader;
            }

            throw new ArgumentException("Unknown shader stage: " + shaderStage, "shaderStage");
        }

        protected override void ClearDepthStencilCore(DepthStencilView depthStencilView, ClearOptions options, float depth, byte stencil)
        {
            D3D11DepthStencilClearFlags flags = 0;

            if ((options & ClearOptions.Depth) != 0)
                flags |= D3D11DepthStencilClearFlags.Depth;

            if ((options & ClearOptions.Stencil) != 0)
                flags |= D3D11DepthStencilClearFlags.Stencil;

            if (flags != 0)
            {
                var d3d11DepthStencilView = (depthStencilView as SdxDepthStencilView).D3D11DepthStencilView;

                D3D11DeviceContext.ClearDepthStencilView(d3d11DepthStencilView, flags, depth, stencil);
            }
        }

        protected override void ClearRenderTargetCore(
            RenderTargetView renderTarget, ClearOptions options, ref Vector4 color, float depth, byte stencil)
        {
            if ((options & ClearOptions.Target) != 0)
            {
                var d3d11RenderTargetView = (renderTarget as SdxRenderTargetView).D3D11RenderTargetView;

                D3D11DeviceContext.ClearRenderTargetView(
                    d3d11RenderTargetView, new SDXColor4(color.X, color.Y, color.Z, color.W));
            }

            var depthStencilView = renderTarget.DepthStencilView;
            if (depthStencilView == null)
                return;

            D3D11DepthStencilClearFlags flags = 0;

            if ((options & ClearOptions.Depth) != 0)
                flags |= D3D11DepthStencilClearFlags.Depth;

            if ((options & ClearOptions.Stencil) != 0)
                flags |= D3D11DepthStencilClearFlags.Stencil;

            if (flags != 0)
            {
                var d3d11DepthStencilView = (depthStencilView as SdxDepthStencilView).D3D11DepthStencilView;

                D3D11DeviceContext.ClearDepthStencilView(d3d11DepthStencilView, flags, depth, stencil);
            }
        }

        protected override void DrawCore(int vertexCount, int startVertexLocation)
        {
            D3D11DeviceContext.Draw(vertexCount, startVertexLocation);
        }

        protected override void DrawIndexedCore(int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            D3D11DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
        }

        protected override void DrawInstancedCore(int vertexCountPerInstance, int instanceCount,
            int startVertexLocation, int startInstanceLocation)
        {
            D3D11DeviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
        }

        protected override void DrawIndexedInstancedCore(int indexCountPerInstance, int instanceCount,
            int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            D3D11DeviceContext.DrawIndexedInstanced(
                indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        protected override MappedSubresource Map(Resource resource, int subresource, MapMode mapMode)
        {
            var d3d11Resource = GetD3D11Resource(resource);
            var dataBox = D3D11DeviceContext.MapSubresource(d3d11Resource, subresource, (D3D11MapMode) mapMode, D3D11MapFlags.None);
            return new MappedSubresource(dataBox.DataPointer, dataBox.RowPitch, dataBox.SlicePitch);
        }

        protected override void Unmap(Resource resource, int subresource)
        {
            var d3d11Resource = GetD3D11Resource(resource);
            D3D11DeviceContext.UnmapSubresource(d3d11Resource, subresource);
        }

        protected override void UpdateSubresource(
            Resource destinationResource, int destinationSubresource, Box? destinationBox,
            IntPtr sourcePointer, int sourceRowPitch, int sourceDepthPitch)
        {
            var d3d11Resource = GetD3D11Resource(destinationResource);
            D3D11ResourceRegion? d3d11ResourceRegion = null;
            if (destinationBox.HasValue)
            {
                d3d11ResourceRegion = new D3D11ResourceRegion
                {
                    Left = destinationBox.Value.Left,
                    Top = destinationBox.Value.Top,
                    Front = destinationBox.Value.Front,
                    Right = destinationBox.Value.Right,
                    Bottom = destinationBox.Value.Bottom,
                    Back = destinationBox.Value.Back
                };
            }
            D3D11DeviceContext.UpdateSubresource(
                d3d11Resource, destinationSubresource, d3d11ResourceRegion, sourcePointer, sourceRowPitch, sourceDepthPitch);
        }

        D3D11Resource GetD3D11Resource(Resource resource)
        {
            var constantBuffer = resource as SdxConstantBuffer;
            if (constantBuffer != null)
                return constantBuffer.D3D11Buffer;

            var vertexBuffer = resource as SdxVertexBuffer;
            if (vertexBuffer != null)
                return vertexBuffer.D3D11Buffer;

            var indexBuffer = resource as SdxIndexBuffer;
            if (indexBuffer != null)
                return indexBuffer.D3D11Buffer;

            var texture2D = resource as SdxTexture2D;
            if (texture2D != null)
                return texture2D.D3D11Texture2D;

            throw new ArgumentException("Unknown resource specified: " + resource.GetType(), "resource");
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                D3D11DeviceContext.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
