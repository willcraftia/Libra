#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ParticleEffect : IEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.ParticleVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.ParticlePS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit)]
        struct ParametersPerObject
        {
            [FieldOffset(0)]
            public float Duration;

            [FieldOffset(4)]
            public float DurationRandomness;

            [FieldOffset(16)]
            public Vector3 Gravity;

            [FieldOffset(28)]
            public float EndVelocity;

            [FieldOffset(32)]
            public Vector4 MinColor;

            [FieldOffset(48)]
            public Vector4 MaxColor;

            [FieldOffset(64)]
            public Vector2 RotateSpeed;

            [FieldOffset(80)]
            public Vector2 StartSize;

            [FieldOffset(88)]
            public Vector2 EndSize;
        }

        #endregion

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Explicit, Size = 144)]
        public struct ParametersPerCamera
        {
            [FieldOffset(0)]
            public Matrix ViewProjection;

            [FieldOffset(64)]
            public Matrix Projection;

            [FieldOffset(128)]
            public Vector2 ViewportScale;
        }

        #endregion

        #region ParametersPerFrame

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerFrame
        {
            public float CurrentTime;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0),
            ConstantBufferPerCamera = (1 << 1),
            ConstantBufferPerFrame  = (1 << 2),
            ViewProjection          = (1 << 3),
            Projection              = (1 << 4),
            ViewportScale           = (1 << 5)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerCamera;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerObject parametersPerObject;

        ParametersPerCamera parametersPerCamera;

        ParametersPerFrame parametersPerFrame;

        Matrix view;

        Matrix projection;

        Matrix viewProjection;

        DirtyFlags dirtyFlags;

        /// <summary>
        /// パーティクル存続期間 (秒) を取得または設定します。
        /// </summary>
        public float Duration
        {
            get { return parametersPerObject.Duration; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Duration = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル存続期間のランダム性を取得または設定します。
        /// 0 より大きな値を指定した場合、存続期間がランダムに変化します。
        /// </summary>
        public float DurationRandomness
        {
            get { return parametersPerObject.DurationRandomness; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.DurationRandomness = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクルへ与える重力効果の方向と強さを取得または設定します。
        /// </summary>
        public Vector3 Gravity
        {
            get { return parametersPerObject.Gravity; }
            set
            {
                parametersPerObject.Gravity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// 消滅時のパーティクル速度を取得または設定します。
        /// 1 に設定すると、パーティクルは作成時と同じ速度を維持します。
        /// 0 に設定すると、パーティクルは存続期間の終了時に完全に停止します。
        /// 1 よりも大きい値では、パーティクルの速度は時間経過とともに上昇します。
        /// </summary>
        public float EndVelocity
        {
            get { return parametersPerObject.EndVelocity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.EndVelocity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル色の最小値を取得または設定します。
        /// </summary>
        public Vector4 MinColor
        {
            get { return parametersPerObject.MinColor; }
            set
            {
                parametersPerObject.MinColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル色の最大値を取得または設定します。
        /// </summary>
        public Vector4 MaxColor
        {
            get { return parametersPerObject.MaxColor; }
            set
            {
                parametersPerObject.MaxColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル回転速度の最小値を取得または設定します。
        /// </summary>
        public float MinRotateSpeed
        {
            get { return parametersPerObject.RotateSpeed.X; }
            set
            {
                parametersPerObject.RotateSpeed.X = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル回転速度の最大値を取得または設定します。
        /// </summary>
        public float MaxRotateSpeed
        {
            get { return parametersPerObject.RotateSpeed.Y; }
            set
            {
                parametersPerObject.RotateSpeed.Y = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル生成時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinStartSize
        {
            get { return parametersPerObject.StartSize.X; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.StartSize.X = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル生成時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxStartSize
        {
            get { return parametersPerObject.StartSize.Y; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.StartSize.Y = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinEndSize
        {
            get { return parametersPerObject.EndSize.X; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.EndSize.X = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxEndSize
        {
            get { return parametersPerObject.EndSize.Y; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.EndSize.Y = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// カメラのビュー行列を取得または設定します。
        /// </summary>
        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
            }
        }

        /// <summary>
        /// カメラの射影行列を取得または設定します。
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.ViewProjection | DirtyFlags.Projection | DirtyFlags.ViewportScale;
            }
        }

        /// <summary>
        /// 現在時刻を取得または設定します。
        /// </summary>
        public float CurrentTime
        {
            get { return parametersPerFrame.CurrentTime; }
            set
            {
                parametersPerFrame.CurrentTime = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }
        }

        public ParticleEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<ParticleEffect, SharedDeviceResource>();

            constantBufferPerObject = device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();
            constantBufferPerCamera = device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();
            constantBufferPerFrame = device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            parametersPerObject.Duration = 1.0f;
            parametersPerObject.DurationRandomness = 0.0f;
            parametersPerObject.Gravity = Vector3.Zero;
            parametersPerObject.EndVelocity = 1.0f;
            parametersPerObject.MinColor = Vector4.One;
            parametersPerObject.MaxColor = Vector4.One;
            parametersPerObject.RotateSpeed = Vector2.Zero;
            parametersPerObject.StartSize = new Vector2(100.0f, 100.0f);
            parametersPerObject.EndSize = new Vector2(100.0f, 100.0f);

            parametersPerCamera.ViewProjection = Matrix.Identity;
            parametersPerCamera.Projection = Matrix.Identity;
            parametersPerCamera.ViewportScale = Vector2.One;

            parametersPerFrame.CurrentTime = 0.0f;

            dirtyFlags = DirtyFlags.ViewportScale | DirtyFlags.ConstantBufferPerObject | DirtyFlags.ConstantBufferPerFrame;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ViewProjection) != 0)
            {
                Matrix.Multiply(ref view, ref projection, out viewProjection);
                Matrix.Transpose(ref viewProjection, out parametersPerCamera.ViewProjection);

                dirtyFlags &= ~DirtyFlags.ViewProjection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                Matrix.Transpose(ref projection, out parametersPerCamera.Projection);

                dirtyFlags &= ~DirtyFlags.Projection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.ViewportScale) != 0)
            {
                parametersPerCamera.ViewportScale = new Vector2(0.5f / projection.PerspectiveAspectRatio, -0.5f);

                dirtyFlags &= ~DirtyFlags.ViewportScale;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(context, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(context, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(context, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            context.VertexShaderConstantBuffers[0] = constantBufferPerObject;
            context.VertexShaderConstantBuffers[1] = constantBufferPerCamera;
            context.VertexShaderConstantBuffers[2] = constantBufferPerFrame;
            context.VertexShader = sharedDeviceResource.VertexShader;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~ParticleEffect()
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
                constantBufferPerCamera.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
