#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class FluidRippleFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.FluidRippleFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerObject
        {
            public float Stiffness;
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        public struct ParametersPerRenderTarget
        {
            // XY: テクセル オフセット
            // ZW: 整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Offsets;
        }

        #endregion

        #region ConstantsPerFrame

        public struct ParametersPerFrame
        {
            public Vector2 NewPosition;

            public float NewRadius;

            public float NewVelocity;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject         = (1 << 0),
            ConstantBufferPerRenderTarget   = (1 << 1),
            ConstantBufferPerFrame          = (1 << 2),
            Offsets                         = (1 << 3),
        }

        #endregion

        const int KernelSize = 4;

        static readonly Vector2[] PixelOffsets =
        {
            new Vector2( 1.5f,  0.0f),
            new Vector2(-1.5f,  0.0f),
            new Vector2( 0.0f,  1.5f),
            new Vector2( 0.0f, -1.5f),
        };

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerRenderTarget;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerObject parametersPerObject;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ParametersPerFrame parametersPerFrame;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float Stiffness
        {
            get { return parametersPerObject.Stiffness; }
            set
            {
                parametersPerObject.Stiffness = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public FluidRippleFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<FluidRippleFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            constantBufferPerFrame = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            parametersPerObject.Stiffness = 0.5f;

            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

            parametersPerFrame.NewPosition = Vector2.Zero;
            parametersPerFrame.NewRadius = 0.0f;
            parametersPerFrame.NewVelocity = 0.0f;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.ConstantBufferPerFrame |
                DirtyFlags.Offsets;
        }

        public void AddRipple(Vector2 position, float radius, float velocity)
        {
            if (position.X < 0.0f || 1.0f < position.X ||
                position.Y < 0.0f || 1.0f < position.Y)
                throw new ArgumentOutOfRangeException("position");
            if (radius <= 0.0f) throw new ArgumentOutOfRangeException("radius");

            parametersPerFrame.NewPosition = position;
            parametersPerFrame.NewRadius = radius;
            parametersPerFrame.NewVelocity = velocity;

            dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
        }

        public void Apply()
        {
            var viewport = DeviceContext.Viewport;
            int currentWidth = (int) viewport.Width;
            int currentHeight = (int) viewport.Height;

            if (currentWidth != viewportWidth || currentHeight != viewportHeight)
            {
                viewportWidth = currentWidth;
                viewportHeight = currentHeight;

                dirtyFlags |= DirtyFlags.Offsets;
            }

            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                for (int i = 0; i < KernelSize; i++)
                {
                    parametersPerRenderTarget.Offsets[i].X = PixelOffsets[i].X / (float) viewportWidth;
                    parametersPerRenderTarget.Offsets[i].Y = PixelOffsets[i].Y / (float) viewportHeight;
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                DeviceContext.SetData(constantBufferPerObject, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                DeviceContext.SetData(constantBufferPerRenderTarget, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                DeviceContext.SetData(constantBufferPerFrame, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderConstantBuffers[2] = constantBufferPerFrame;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~FluidRippleFilter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                sharedDeviceResource = null;
                constantBufferPerObject.Dispose();
                constantBufferPerRenderTarget.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
