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

        #region ConstantsPerShader

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        struct ConstantsPerShader
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

        #region ConstantsPerFrame

        [StructLayout(LayoutKind.Explicit, Size = 144)]
        public struct ConstantsPerFrame
        {
            [FieldOffset(0)]
            public Matrix ViewProjection;

            [FieldOffset(64)]
            public Matrix Projection;

            [FieldOffset(128)]
            public Vector2 ViewportScale;

            [FieldOffset(136)]
            public float CurrentTime;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ViewProjection      = (1 << 0),
            Projection          = (1 << 1),
            ViewportScale       = (1 << 2),
            ConstantsPerShader  = (1 << 3),
            ConstantsPerFrame   = (1 << 4)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerFrame;

        ConstantsPerShader constantsPerShader;

        ConstantsPerFrame constantsPerFrame;

        Matrix view;

        Matrix projection;

        Matrix viewProjection;

        DirtyFlags dirtyFlags;

        /// <summary>
        /// パーティクル存続期間 (秒) を取得または設定します。
        /// </summary>
        public float Duration
        {
            get { return constantsPerShader.Duration; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.Duration = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル存続期間のランダム性を取得または設定します。
        /// 0 より大きな値を指定した場合、存続期間がランダムに変化します。
        /// </summary>
        public float DurationRandomness
        {
            get { return constantsPerShader.DurationRandomness; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.DurationRandomness = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクルへ与える重力効果の方向と強さを取得または設定します。
        /// </summary>
        public Vector3 Gravity
        {
            get { return constantsPerShader.Gravity; }
            set
            {
                constantsPerShader.Gravity = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
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
            get { return constantsPerShader.EndVelocity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.EndVelocity = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル色の最小値を取得または設定します。
        /// </summary>
        public Vector4 MinColor
        {
            get { return constantsPerShader.MinColor; }
            set
            {
                constantsPerShader.MinColor = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル色の最大値を取得または設定します。
        /// </summary>
        public Vector4 MaxColor
        {
            get { return constantsPerShader.MaxColor; }
            set
            {
                constantsPerShader.MaxColor = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル回転速度の最小値を取得または設定します。
        /// </summary>
        public float MinRotateSpeed
        {
            get { return constantsPerShader.RotateSpeed.X; }
            set
            {
                constantsPerShader.RotateSpeed.X = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル回転速度の最大値を取得または設定します。
        /// </summary>
        public float MaxRotateSpeed
        {
            get { return constantsPerShader.RotateSpeed.Y; }
            set
            {
                constantsPerShader.RotateSpeed.Y = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル生成時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinStartSize
        {
            get { return constantsPerShader.StartSize.X; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.StartSize.X = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル生成時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxStartSize
        {
            get { return constantsPerShader.StartSize.Y; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.StartSize.Y = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinEndSize
        {
            get { return constantsPerShader.EndSize.X; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.EndSize.X = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxEndSize
        {
            get { return constantsPerShader.EndSize.Y; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPerShader.EndSize.Y = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
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
            get { return constantsPerFrame.CurrentTime; }
            set
            {
                constantsPerFrame.CurrentTime = value;

                dirtyFlags |= DirtyFlags.ConstantsPerFrame;
            }
        }

        public ParticleEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<ParticleEffect, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ConstantsPerShader>();
            constantBufferPerFrame = device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ConstantsPerFrame>();

            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            constantsPerShader.Duration = 1.0f;
            constantsPerShader.DurationRandomness = 0.0f;
            constantsPerShader.Gravity = Vector3.Zero;
            constantsPerShader.EndVelocity = 1.0f;
            constantsPerShader.MinColor = Vector4.One;
            constantsPerShader.MaxColor = Vector4.One;
            constantsPerShader.RotateSpeed = Vector2.Zero;
            constantsPerShader.StartSize = new Vector2(100.0f, 100.0f);
            constantsPerShader.EndSize = new Vector2(100.0f, 100.0f);

            constantsPerFrame.ViewProjection = Matrix.Identity;
            constantsPerFrame.Projection = Matrix.Identity;
            constantsPerFrame.ViewportScale = Vector2.One;
            constantsPerFrame.CurrentTime = 0.0f;

            dirtyFlags = DirtyFlags.ViewportScale | DirtyFlags.ConstantsPerShader | DirtyFlags.ConstantsPerFrame;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ViewProjection) != 0)
            {
                Matrix.Multiply(ref view, ref projection, out viewProjection);
                Matrix.Transpose(ref viewProjection, out constantsPerFrame.ViewProjection);

                dirtyFlags &= ~DirtyFlags.ViewProjection;
                dirtyFlags |= DirtyFlags.ConstantsPerFrame;
            }

            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                Matrix.Transpose(ref projection, out constantsPerFrame.Projection);

                dirtyFlags &= ~DirtyFlags.Projection;
                dirtyFlags |= DirtyFlags.ConstantsPerFrame;
            }

            if ((dirtyFlags & DirtyFlags.ViewportScale) != 0)
            {
                constantsPerFrame.ViewportScale = new Vector2(0.5f / projection.PerspectiveAspectRatio, -0.5f);

                dirtyFlags &= ~DirtyFlags.ViewportScale;
                dirtyFlags |= DirtyFlags.ConstantsPerFrame;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, constantsPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantsPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(context, constantsPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantsPerFrame;
            }

            context.VertexShaderConstantBuffers[0] = constantBufferPerShader;
            context.VertexShaderConstantBuffers[1] = constantBufferPerFrame;
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
                constantBufferPerShader.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
