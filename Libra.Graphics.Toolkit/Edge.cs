﻿#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Edge : IPostprocessor, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.EdgePS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct Constants
        {
            [FieldOffset(0)]
            public Vector2 EdgeOffset;

            [FieldOffset(8)]
            public float EdgeIntensity;

            [FieldOffset(16)]
            public float DepthThreshold;

            [FieldOffset(20)]
            public float DepthSensitivity;

            [FieldOffset(24)]
            public float NormalThreshold;

            [FieldOffset(28)]
            public float NormalSensitivity;

            [FieldOffset(32)]
            public Vector3 EdgeColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            EdgeOffset  = (1 << 0),
            Constants   = (1 << 1)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        float edgeWidth;

        int lastTextureWidth;

        int lastTextureHeight;

        DirtyFlags dirtyFlags;

        public float EdgeWidth
        {
            get { return edgeWidth; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                edgeWidth = value;

                dirtyFlags |= DirtyFlags.EdgeOffset;
            }
        }

        public float EdgeIntensity
        {
            get { return constants.EdgeIntensity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.EdgeIntensity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NormalThreshold
        {
            get { return constants.NormalThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.NormalThreshold = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float DepthThreshold
        {
            get { return constants.DepthThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.DepthThreshold = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NormalSensitivity
        {
            get { return constants.NormalSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.NormalSensitivity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float DepthSensitivity
        {
            get { return constants.DepthSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.DepthSensitivity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public Vector3 EdgeOffset
        {
            get { return constants.EdgeColor; }
            set
            {
                constants.EdgeColor = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public ShaderResourceView DepthNormalMap { get; set; }

        public Edge(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<Edge, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            edgeWidth = 1;
            constants.EdgeOffset = Vector2.Zero;
            constants.EdgeIntensity = 200.0f;
            constants.DepthThreshold = 0.0f;
            constants.DepthSensitivity = 1.0f;
            constants.NormalThreshold = 0.5f;
            constants.NormalSensitivity = 1.0f;
            constants.EdgeColor = Vector3.Zero;

            Enabled = true;

            dirtyFlags = DirtyFlags.EdgeOffset | DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            int textureWidth;
            int textureHeight;
            GetTextureSize(out textureWidth, out textureHeight);

            if (textureWidth != lastTextureWidth || textureHeight != lastTextureHeight)
            {
                lastTextureWidth = textureWidth;
                lastTextureHeight = textureHeight;

                dirtyFlags |= DirtyFlags.EdgeOffset;
            }

            if ((dirtyFlags & DirtyFlags.EdgeOffset) != 0)
            {
                var offset = new Vector2(edgeWidth, edgeWidth);
                offset.X /= (float) textureWidth;
                offset.Y /= (float) textureHeight;

                constants.EdgeOffset = offset;

                dirtyFlags &= ~DirtyFlags.EdgeOffset;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShaderResources[0] = Texture;
            context.PixelShaderResources[1] = DepthNormalMap;
            context.PixelShaderSamplers[0] = SamplerState.PointClamp;
            context.PixelShaderSamplers[1] = SamplerState.PointClamp;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        void GetTextureSize(out int width, out int height)
        {
            if (Texture == null)
                throw new InvalidOperationException("Texture is null.");

            var texture2D = Texture.Resource as Texture2D;
            if (texture2D == null)
                throw new InvalidOperationException("Texture is not a view for Texture2D.");

            width = texture2D.Width;
            height = texture2D.Height;
        }

        #region IDisposable

        bool disposed;

        ~Edge()
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
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
