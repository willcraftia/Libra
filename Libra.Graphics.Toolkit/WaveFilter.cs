#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class WaveFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.WaveFilterPS);
            }
        }

        #endregion

        #region ParametersPerShader

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerShader
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
            public Vector2 NewWavePosition;

            public float NewWaveRadius;

            public float NewWaveVelocity;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader         = (1 << 0),
            ConstantBufferPerRenderTarget   = (1 << 1),
            ConstantBufferPerFrame          = (1 << 2),
            Offsets                         = (1 << 3),
            NewWave                         = (1 << 4),
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

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerRenderTarget;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerShader parametersPerShader;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ParametersPerFrame parametersPerFrame;

        Vector2 newWavePosition;

        float newWaveRadius;

        float newWaveVelocity;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public float Stiffness
        {
            get { return parametersPerShader.Stiffness; }
            set
            {
                parametersPerShader.Stiffness = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public bool Enabled { get; set; }

        public WaveFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<WaveFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            constantBufferPerRenderTarget = device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            constantBufferPerFrame = device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            parametersPerShader.Stiffness = 0.5f;

            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

            parametersPerFrame.NewWavePosition = Vector2.Zero;
            parametersPerFrame.NewWaveRadius = 0.0f;
            parametersPerFrame.NewWaveVelocity = 0.0f;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerShader |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.ConstantBufferPerFrame |
                DirtyFlags.Offsets;
        }

        public void AddWave(Vector2 position, float radius, float velocity)
        {
            if (position.X < 0.0f || 1.0f < position.X ||
                position.Y < 0.0f || 1.0f < position.Y)
                throw new ArgumentOutOfRangeException("position");
            if (radius <= 0.0f) throw new ArgumentOutOfRangeException("radius");

            newWavePosition = position;
            newWaveRadius = radius;
            newWaveVelocity = velocity;

            dirtyFlags |= DirtyFlags.NewWave;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var viewport = context.Viewport;
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

            if ((dirtyFlags & DirtyFlags.NewWave) != 0)
            {
                parametersPerFrame.NewWavePosition = newWavePosition;
                parametersPerFrame.NewWaveRadius = newWaveRadius;
                parametersPerFrame.NewWaveVelocity = newWaveVelocity;

                dirtyFlags &= ~DirtyFlags.NewWave;
                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                constantBufferPerRenderTarget.SetData(context, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(context, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShaderConstantBuffers[1] = constantBufferPerRenderTarget;
            context.PixelShaderConstantBuffers[2] = constantBufferPerFrame;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~WaveFilter()
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
                constantBufferPerShader.Dispose();
                constantBufferPerRenderTarget.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
