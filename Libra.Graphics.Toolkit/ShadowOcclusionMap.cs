#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ShadowOcclusionMap : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader BasicPixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                // TODO
                // 遅延ロード。
                BasicPixelShader = device.CreatePixelShader();
                BasicPixelShader.Initialize(Resources.ShadowOcclusionMapBasicPS);
            }
        }

        #endregion

        #region ParametersPerLight

        [StructLayout(LayoutKind.Explicit, Size = 16 + (16 * MaxSplitDistanceCount) + (64 * MaxSplitCount))]
        public struct ParametersPerLight
        {
            [FieldOffset(0)]
            public int SplitCount;

            [FieldOffset(4)]
            public float DepthBias;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSplitDistanceCount)]
            public Vector4[] SplitDistances;

            [FieldOffset(16 + (16 * MaxSplitDistanceCount)), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSplitCount)]
            public Matrix[] LightViewProjections;
        }

        #endregion

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        public struct ParametersPerCamera
        {
            [FieldOffset(0)]
            public Vector2 FocalLength;

            [FieldOffset(8)]
            public float FarClipDistance;


            [FieldOffset(16)]
            public Matrix InverseView;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerLight  = (1 << 0),
            ConstantBufferPerCamera = (1 << 1),
            InverseView             = (1 << 2),
            Projection              = (1 << 3)
        }

        #endregion

        /// <summary>
        /// 最大分割数。
        /// </summary>
        const int MaxSplitCount = 3;

        /// <summary>
        /// 最大分割距離数。
        /// </summary>
        const int MaxSplitDistanceCount = MaxSplitCount + 1;

        SharedDeviceResource sharedDeviceResource;

        FullScreenQuad fullScreenQuad;

        ConstantBuffer constantBufferPerLight;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerLight parametersPerLight;

        ParametersPerCamera parametersPerCamera;

        Matrix view;

        Matrix projection;

        ShaderResourceView[] shadowMaps;

        DirtyFlags dirtyFlags;

        public DeviceContext Context { get; private set; }

        public int SplitCount
        {
            get { return parametersPerLight.SplitCount; }
            set
            {
                if (value < 1 || MaxSplitCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerLight.SplitCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerLight;
            }
        }

        public float DepthBias
        {
            get { return parametersPerLight.DepthBias; }
            set
            {
                if (value < 1 || MaxSplitCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerLight.DepthBias = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerLight;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.InverseView;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState ShadowMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShadowOcclusionMap(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Context = context;

            sharedDeviceResource = context.Device.GetSharedResource<ShadowOcclusionMap, SharedDeviceResource>();

            fullScreenQuad = new FullScreenQuad(context);
            fullScreenQuad.ViewRayEnabled = true;

            constantBufferPerLight = context.Device.CreateConstantBuffer();
            constantBufferPerLight.Initialize<ParametersPerLight>();

            constantBufferPerCamera = context.Device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            parametersPerLight.SplitCount = MaxSplitCount;
            parametersPerLight.DepthBias = 0.001f;
            parametersPerLight.SplitDistances = new Vector4[MaxSplitDistanceCount];
            parametersPerLight.LightViewProjections = new Matrix[MaxSplitCount];

            parametersPerCamera.FocalLength = Vector2.One;
            parametersPerCamera.FarClipDistance = 1000.0f;

            shadowMaps = new ShaderResourceView[MaxSplitCount];

            LinearDepthMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerLight |
                DirtyFlags.ConstantBufferPerCamera;
        }

        public float GetSplitDistance(int index)
        {
            if ((uint) MaxSplitDistanceCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return parametersPerLight.SplitDistances[index].X;
        }

        public void SetSplitDistance(int index, float value)
        {
            if ((uint) MaxSplitDistanceCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            parametersPerLight.SplitDistances[index].X = value;

            dirtyFlags |= DirtyFlags.ConstantBufferPerLight;
        }

        public Matrix GetLightViewProjection(int index)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            Matrix result;
            Matrix.Transpose(ref parametersPerLight.LightViewProjections[index], out result);

            return result;
        }

        public void SetLightViewProjection(int index, Matrix value)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            Matrix.Transpose(ref value, out parametersPerLight.LightViewProjections[index]);

            dirtyFlags |= DirtyFlags.ConstantBufferPerLight;
        }

        public ShaderResourceView GetShadowMap(int index)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return shadowMaps[index];
        }

        public void SetShadowMap(int index, ShaderResourceView value)
        {
            if ((uint) MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            shadowMaps[index] = value;
        }

        public void Draw()
        {
            Apply();

            fullScreenQuad.Draw();
        }

        void Apply()
        {
            if ((dirtyFlags & DirtyFlags.InverseView) != 0)
            {
                Matrix inverseView;
                Matrix.Invert(ref view, out inverseView);

                Matrix.Transpose(ref inverseView, out parametersPerCamera.InverseView);

                dirtyFlags &= ~DirtyFlags.InverseView;
                dirtyFlags |= DirtyFlags.ConstantBufferPerLight;
            }

            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                fullScreenQuad.Projection = projection;
                parametersPerCamera.FocalLength.X = projection.M11;
                parametersPerCamera.FocalLength.Y = projection.M22;
                parametersPerCamera.FarClipDistance = projection.PerspectiveFarClipDistance;

                dirtyFlags &= ~DirtyFlags.Projection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerLight) != 0)
            {
                constantBufferPerLight.SetData(Context, parametersPerLight);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerLight;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(Context, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            // TODO
            // 他のシェーダも。
            Context.PixelShader = sharedDeviceResource.BasicPixelShader;

            Context.PixelShaderConstantBuffers[0] = constantBufferPerLight;
            Context.PixelShaderConstantBuffers[1] = constantBufferPerCamera;
            Context.PixelShaderResources[0] = LinearDepthMap;
            Context.PixelShaderSamplers[0] = LinearDepthMapSampler;
            Context.PixelShaderSamplers[1] = ShadowMapSampler;

            for (int i = 0; i < shadowMaps.Length; i++)
            {
                Context.PixelShaderResources[1 + i] = shadowMaps[i];
            }
        }

        #region IDisposable

        bool disposed;

        ~ShadowOcclusionMap()
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
                constantBufferPerLight.Dispose();
                constantBufferPerCamera.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
