#region Using

using System;
using Libra.PackedVector;

#endregion

// Xbox LIVE Indie Games - Particles 3D より移植。
// http://xbox.create.msdn.com/en-US/education/catalog/sample/particle_3d

namespace Libra.Graphics.Toolkit
{
    public sealed class ParticleSystem : IDisposable
    {
        #region ParticleVertex

        /// <summary>
        /// パーティクルを描画するためのカスタム頂点構造体。
        /// </summary>
        struct ParticleVertex
        {
            /// <summary>
            /// パーティクルのコーナー。
            /// </summary>
            public Short2 Corner;

            /// <summary>
            /// パーティクルの開始位置。
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// パーティクルの開始速度。
            /// </summary>
            public Vector3 Velocity;

            /// <summary>
            /// パーティクルの外観を変化させるための 4 つのランダム値。
            /// </summary>
            public Color Random;

            /// <summary>
            /// パーティクルの作成時刻 (秒単位)。
            /// </summary>
            public float Time;

            /// <summary>
            /// 頂点宣言。
            /// </summary>
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement("CORNER", 0, VertexFormat.Short2),
                new VertexElement("POSITION", 0, VertexFormat.Vector3),
                new VertexElement("VELOCITY", 0, VertexFormat.Vector3),
                new VertexElement("RANDOM", 0, VertexFormat.Color),
                new VertexElement("TIME", 0, VertexFormat.Single)
                );
        }

        #endregion

        static readonly Random Random = new Random();

        ParticleEffect particleEffect;

        ParticleVertex[] particles;

        VertexBuffer vertexBuffer;

        IndexBuffer indexBuffer;

        int firstActiveParticle;

        int firstNewParticle;

        int firstFreeParticle;

        int firstRetiredParticle;

        float currentTime;

        int drawCounter;

        /// <summary>
        /// デバイス コンテキストを取得します。
        /// </summary>
        public DeviceContext DeviceContext { get; private set; }

        /// <summary>
        /// 一度に表示可能なパーティクルの最大数を取得または設定します。
        /// </summary>
        public int MaxParticleCount { get; private set; }

        /// <summary>
        /// システムの更新が有効であるか否かを示す値を取得または設定します。
        /// </summary>
        /// <value>
        /// true (システムの更新が有効である場合)、false (それ以外の場合)。
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// システムの描画が有効であるか否かを示す値を取得または設定します。
        /// </summary>
        /// <value>
        /// true (システムの描画が有効である場合)、false (それ以外の場合)。
        /// </value>
        public bool Visible { get; set; }

        /// <summary>
        /// システム名を取得または設定します。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// テクスチャを取得または設定します。
        /// </summary>
        public ShaderResourceView Texture { get; set; }

        /// <summary>
        /// 作成元であるオブジェクトの速度に影響を受けるパーティクルの数を制御します。
        /// この動作は、爆発エフェクトとともに見ることができます。
        /// その際、炎はソースの発射体と同じ方向への移動を続けます。
        /// 一方、発射体のトレール パーティクルはこの値を極めて低く設定するので、発射体の速度による影響は少なくなります。
        /// </summary>
        public float EmitterVelocitySensitivity { get; set; }

        /// <summary>
        /// パーティクルに与える X-Z 軸の速度の最小値を取得または設定します。
        /// </summary>
        public float MinHorizontalVelocity { get; set; }

        /// <summary>
        /// パーティクルに与える X-Z 軸の速度の最大値を取得または設定します。
        /// </summary>
        public float MaxHorizontalVelocity { get; set; }

        /// <summary>
        /// パーティクルに与える Y 軸の速度の最小値を取得または設定します。
        /// </summary>
        public float MinVerticalVelocity { get; set; }

        /// <summary>
        /// パーティクルに与える Y 軸の速度の最大値を取得または設定します。
        /// </summary>
        public float MaxVerticalVelocity { get; set; }

        /// <summary>
        /// ブレンド ステートを取得または設定します。
        /// </summary>
        public BlendState BlendState { get; set; }

        /// <summary>
        /// パーティクル存続期間 (秒) を取得または設定します。
        /// </summary>
        public float Duration
        {
            get { return particleEffect.Duration; }
            set { particleEffect.Duration = value; }
        }

        /// <summary>
        /// パーティクル存続期間のランダム性を取得または設定します。
        /// 0 より大きな値を指定した場合、存続期間がランダムに変化します。
        /// </summary>
        public float DurationRandomness
        {
            get { return particleEffect.DurationRandomness; }
            set { particleEffect.DurationRandomness = value; }
        }

        /// <summary>
        /// パーティクルへ与える重力効果の方向と強さを取得または設定します。
        /// </summary>
        public Vector3 Gravity
        {
            get { return particleEffect.Gravity; }
            set { particleEffect.Gravity = value; }
        }

        /// <summary>
        /// 消滅時のパーティクル速度を取得または設定します。
        /// 1 に設定すると、パーティクルは作成時と同じ速度を維持します。
        /// 0 に設定すると、パーティクルは存続期間の終了時に完全に停止します。
        /// 1 よりも大きい値では、パーティクルの速度は時間経過とともに上昇します。
        /// </summary>
        public float EndVelocity
        {
            get { return particleEffect.EndVelocity; }
            set { particleEffect.EndVelocity = value; }
        }

        /// <summary>
        /// パーティクル色の最小値を取得または設定します。
        /// </summary>
        public Vector4 MinColor
        {
            get { return particleEffect.MinColor; }
            set { particleEffect.MinColor = value; }
        }

        /// <summary>
        /// パーティクル色の最大値を取得または設定します。
        /// </summary>
        public Vector4 MaxColor
        {
            get { return particleEffect.MaxColor; }
            set { particleEffect.MaxColor = value; }
        }

        /// <summary>
        /// パーティクル回転速度の最小値を取得または設定します。
        /// </summary>
        public float MinRotateSpeed
        {
            get { return particleEffect.MinRotateSpeed; }
            set { particleEffect.MinRotateSpeed = value; }
        }

        /// <summary>
        /// パーティクル回転速度の最大値を取得または設定します。
        /// </summary>
        public float MaxRotateSpeed
        {
            get { return particleEffect.MaxRotateSpeed; }
            set { particleEffect.MaxRotateSpeed = value; }
        }

        /// <summary>
        /// パーティクル生成時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinStartSize
        {
            get { return particleEffect.MinStartSize; }
            set { particleEffect.MinStartSize = value; }
        }

        /// <summary>
        /// パーティクル生成時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxStartSize
        {
            get { return particleEffect.MaxStartSize; }
            set { particleEffect.MaxStartSize = value; }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最小値を取得または設定します。
        /// </summary>
        public float MinEndSize
        {
            get { return particleEffect.MinEndSize; }
            set { particleEffect.MinEndSize = value; }
        }

        /// <summary>
        /// パーティクル消滅時サイズの最大値を取得または設定します。
        /// </summary>
        public float MaxEndSize
        {
            get { return particleEffect.MaxEndSize; }
            set { particleEffect.MaxEndSize = value; }
        }

        /// <summary>
        /// カメラのビュー行列を取得または設定します。
        /// </summary>
        public Matrix View
        {
            get { return particleEffect.View; }
            set { particleEffect.View = value; }
        }

        /// <summary>
        /// カメラの射影行列を取得または設定します。
        /// </summary>
        public Matrix Projection
        {
            get { return particleEffect.Projection; }
            set { particleEffect.Projection = value; }
        }

        public ParticleSystem(DeviceContext deviceContext, int maxParticleCount)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");
            if (maxParticleCount < 1) throw new ArgumentOutOfRangeException("maxParticleCount");

            DeviceContext = deviceContext;
            MaxParticleCount = maxParticleCount;

            EmitterVelocitySensitivity = 1;
            MinHorizontalVelocity = 0;
            MaxHorizontalVelocity = 0;
            MinVerticalVelocity = 0;
            MaxVerticalVelocity = 0;
            BlendState = BlendState.NonPremultiplied;

            particleEffect = new ParticleEffect(deviceContext.Device);

            particles = new ParticleVertex[MaxParticleCount * 4];

            for (int i = 0; i < MaxParticleCount; i++)
            {
                particles[i * 4 + 0].Corner = new Short2(-1, -1);
                particles[i * 4 + 1].Corner = new Short2(1, -1);
                particles[i * 4 + 2].Corner = new Short2(1, 1);
                particles[i * 4 + 3].Corner = new Short2(-1, 1);
            }

            vertexBuffer = deviceContext.Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Dynamic;
            vertexBuffer.Initialize(ParticleVertex.VertexDeclaration, MaxParticleCount * 4);

            ushort[] indices = new ushort[MaxParticleCount * 6];

            for (int i = 0; i < MaxParticleCount; i++)
            {
                indices[i * 6 + 0] = (ushort) (i * 4 + 0);
                indices[i * 6 + 1] = (ushort) (i * 4 + 1);
                indices[i * 6 + 2] = (ushort) (i * 4 + 2);

                indices[i * 6 + 3] = (ushort) (i * 4 + 0);
                indices[i * 6 + 4] = (ushort) (i * 4 + 2);
                indices[i * 6 + 5] = (ushort) (i * 4 + 3);
            }

            indexBuffer = deviceContext.Device.CreateIndexBuffer();
            indexBuffer.Usage = ResourceUsage.Immutable;
            indexBuffer.Initialize(indices);

            Enabled = true;
            Visible = true;
        }

        public void Update(TimeSpan elapsedGameTime)
        {
            if (!Enabled)
                return;

            currentTime += (float) elapsedGameTime.TotalSeconds;

            RetireActiveParticles();
            FreeRetiredParticles();

            if (firstActiveParticle == firstFreeParticle)
                currentTime = 0;

            if (firstRetiredParticle == firstActiveParticle)
                drawCounter = 0;
        }

        public void Draw()
        {
            if (!Visible)
                return;

            if (firstNewParticle != firstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }

            if (firstActiveParticle != firstFreeParticle)
            {
                DeviceContext.BlendState = BlendState;
                DeviceContext.DepthStencilState = DepthStencilState.DepthRead;

                DeviceContext.PixelShaderResources[0] = Texture;
                DeviceContext.PixelShaderSamplers[0] = SamplerState.LinearClamp;

                DeviceContext.PrimitiveTopology = PrimitiveTopology.TriangleList;
                DeviceContext.SetVertexBuffer(0, vertexBuffer);
                DeviceContext.IndexBuffer = indexBuffer;

                particleEffect.CurrentTime = currentTime;
                particleEffect.Apply(DeviceContext);

                if (firstActiveParticle < firstFreeParticle)
                {
                    DeviceContext.DrawIndexed((firstFreeParticle - firstActiveParticle) * 6, firstActiveParticle * 6);
                }
                else
                {
                    DeviceContext.DrawIndexed((MaxParticleCount - firstActiveParticle) * 6, firstActiveParticle * 6);

                    if (firstFreeParticle > 0)
                    {
                        DeviceContext.DrawIndexed(firstFreeParticle * 6);
                    }
                }

                DeviceContext.BlendState = null;
                DeviceContext.DepthStencilState = null;
            }

            drawCounter++;
        }

        void AddNewParticlesToVertexBuffer()
        {
            int stride = ParticleVertex.VertexDeclaration.Stride;

            if (firstNewParticle < firstFreeParticle)
            {
                vertexBuffer.SetData(DeviceContext,
                    firstNewParticle * stride * 4,
                    particles,
                    firstNewParticle * 4,
                    (firstFreeParticle - firstNewParticle) * 4,
                    SetDataOptions.NoOverwrite);
            }
            else
            {
                vertexBuffer.SetData(DeviceContext,
                    firstNewParticle * stride * 4,
                    particles,
                    firstNewParticle * 4,
                    (MaxParticleCount - firstNewParticle) * 4,
                    SetDataOptions.NoOverwrite);

                if (firstFreeParticle > 0)
                {
                    vertexBuffer.SetData(DeviceContext,
                        firstNewParticle * stride * 4,
                        particles,
                        0,
                        firstFreeParticle * 4,
                        SetDataOptions.NoOverwrite);
                }
            }

            firstNewParticle = firstFreeParticle;
        }

        public void AddParticle(Vector3 position, Vector3 velocity)
        {
            int nextFreeParticle = firstFreeParticle + 1;

            if (nextFreeParticle >= MaxParticleCount)
                nextFreeParticle = 0;

            if (nextFreeParticle == firstRetiredParticle)
                return;

            velocity *= EmitterVelocitySensitivity;

            float horizontalVelocity = MathHelper.Lerp(
                MinHorizontalVelocity,
                MaxHorizontalVelocity,
                (float) Random.NextDouble());

            double horizontalAngle = Random.NextDouble() * MathHelper.TwoPi;

            velocity.X += horizontalVelocity * (float) Math.Cos(horizontalAngle);
            velocity.Z += horizontalVelocity * (float) Math.Sin(horizontalAngle);

            velocity.Y += MathHelper.Lerp(
                MinVerticalVelocity,
                MaxVerticalVelocity,
                (float) Random.NextDouble());

            Color randomValues = new Color(
                (byte) Random.Next(255),
                (byte) Random.Next(255),
                (byte) Random.Next(255),
                (byte) Random.Next(255));

            for (int i = 0; i < 4; i++)
            {
                particles[firstFreeParticle * 4 + i].Position = position;
                particles[firstFreeParticle * 4 + i].Velocity = velocity;
                particles[firstFreeParticle * 4 + i].Random = randomValues;
                particles[firstFreeParticle * 4 + i].Time = currentTime;
            }

            firstFreeParticle = nextFreeParticle;
        }

        void RetireActiveParticles()
        {
            float particleDuration = Duration;

            while (firstActiveParticle != firstNewParticle)
            {
                float particleAge = currentTime - particles[firstActiveParticle * 4].Time;

                if (particleAge < particleDuration)
                    break;

                particles[firstActiveParticle * 4].Time = drawCounter;

                firstActiveParticle++;

                if (firstActiveParticle >= MaxParticleCount)
                    firstActiveParticle = 0;
            }
        }

        void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                int age = drawCounter - (int) particles[firstRetiredParticle * 4].Time;

                if (age < 3)
                    break;

                firstRetiredParticle++;

                if (firstRetiredParticle >= MaxParticleCount)
                    firstRetiredParticle = 0;
            }
        }

        #region IDisposable

        bool disposed;

        ~ParticleSystem()
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
                particleEffect.Dispose();
                vertexBuffer.Dispose();
                indexBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
