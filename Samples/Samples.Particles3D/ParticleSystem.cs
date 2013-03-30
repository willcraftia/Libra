#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.SharpDX.Compiler;
using Libra.PackedVector;
using Libra.Xnb;

#endregion

namespace Samples.Particles3D
{
    public abstract class ParticleSystem : DrawableGameComponent
    {
        #region ConstantsPerShader

        // 16 バイト倍数の制限に合わせるためレイアウトを明示。
        // オフセットはシェーダの cbuffer に一致させる。
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

        // 16 バイト倍数の制限に合わせるためレイアウトを明示。
        [StructLayout(LayoutKind.Explicit, Size = 144)]
        struct ConstantsPerFrame
        {
            [FieldOffset(0)]
            public Matrix View;

            [FieldOffset(64)]
            public Matrix Projection;

            [FieldOffset(128)]
            public Vector2 ViewportScale;

            [FieldOffset(136)]
            public float CurrentTime;
        }

        #endregion

        ParticleSettings settings = new ParticleSettings();

        XnbManager content;

        VertexShader vertexShader;

        PixelShader pixelShader;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerFrame;

        ShaderResourceView textureView;

        ParticleVertex[] particles;

        VertexBuffer vertexBuffer;

        IndexBuffer indexBuffer;

        ConstantsPerFrame constantsPerFrame;

        int firstActiveParticle;
        
        int firstNewParticle;
        
        int firstFreeParticle;
        
        int firstRetiredParticle;

        float currentTime;

        int drawCounter;

        static Random random = new Random();

        protected ParticleSystem(Game game, XnbManager content)
            : base(game)
        {
            this.content = content;
        }

        public override void Initialize()
        {
            InitializeSettings(settings);

            particles = new ParticleVertex[settings.MaxParticles * 4];

            for (int i = 0; i < settings.MaxParticles; i++)
            {
                particles[i * 4 + 0].Corner = new Short2(-1, -1);
                particles[i * 4 + 1].Corner = new Short2(1, -1);
                particles[i * 4 + 2].Corner = new Short2(1, 1);
                particles[i * 4 + 3].Corner = new Short2(-1, 1);
            }

            base.Initialize();
        }

        protected abstract void InitializeSettings(ParticleSettings settings);

        protected override void LoadContent()
        {
            LoadParticleEffect();

            vertexBuffer = Device.CreateVertexBuffer();
            vertexBuffer.Usage = ResourceUsage.Dynamic;
            vertexBuffer.Initialize(ParticleVertex.VertexDeclaration, settings.MaxParticles * 4);

            ushort[] indices = new ushort[settings.MaxParticles * 6];

            for (int i = 0; i < settings.MaxParticles; i++)
            {
                indices[i * 6 + 0] = (ushort) (i * 4 + 0);
                indices[i * 6 + 1] = (ushort) (i * 4 + 1);
                indices[i * 6 + 2] = (ushort) (i * 4 + 2);

                indices[i * 6 + 3] = (ushort) (i * 4 + 0);
                indices[i * 6 + 4] = (ushort) (i * 4 + 2);
                indices[i * 6 + 5] = (ushort) (i * 4 + 3);
            }

            indexBuffer = Device.CreateIndexBuffer();
            indexBuffer.Usage = ResourceUsage.Immutable;
            indexBuffer.Initialize(indices);
        }

        void LoadParticleEffect()
        {
            // TODO
            //
            // シェーダは全てのパーティクル システムで共通であるため、
            // 外部から指定するか、あるいは、共有リソースとして定義すべき。

            var compiler = new ShaderCompiler();
            compiler.RootPath = "Shaders";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var vsBytecode = compiler.CompileVertexShader("ParticleEffect.fx", "ParticleVertexShader");
            var psBytecode = compiler.CompilePixelShader("ParticleEffect.fx", "ParticlePixelShader");

            vertexShader = Device.CreateVertexShader();
            vertexShader.Initialize(vsBytecode);

            pixelShader = Device.CreatePixelShader();
            pixelShader.Initialize(psBytecode);

            var constantsPerShader = new ConstantsPerShader
            {
                Duration = (float) settings.Duration.TotalSeconds,
                DurationRandomness = settings.DurationRandomness,
                Gravity = settings.Gravity,
                EndVelocity = settings.EndVelocity,
                MinColor = settings.MinColor.ToVector4(),
                MaxColor = settings.MaxColor.ToVector4(),
                RotateSpeed = new Vector2(settings.MinRotateSpeed, settings.MaxRotateSpeed),
                StartSize = new Vector2(settings.MinStartSize, settings.MaxStartSize),
                EndSize = new Vector2(settings.MinEndSize, settings.MaxEndSize)
            };

            constantBufferPerShader = Device.CreateConstantBuffer();
            constantBufferPerShader.Usage = ResourceUsage.Immutable;
            constantBufferPerShader.Initialize(constantsPerShader);

            constantBufferPerFrame = Device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ConstantsPerFrame>();

            var texture = content.Load<Texture2D>(settings.TextureName);
            textureView = Device.CreateShaderResourceView();
            textureView.Initialize(texture);
        }

        public override void Update(GameTime gameTime)
        {
            if (gameTime == null)
                throw new ArgumentNullException("gameTime");

            currentTime += (float) gameTime.ElapsedGameTime.TotalSeconds;

            RetireActiveParticles();
            FreeRetiredParticles();

            if (firstActiveParticle == firstFreeParticle)
                currentTime = 0;

            if (firstRetiredParticle == firstActiveParticle)
                drawCounter = 0;
        }

        void RetireActiveParticles()
        {
            float particleDuration = (float) settings.Duration.TotalSeconds;

            while (firstActiveParticle != firstNewParticle)
            {
                float particleAge = currentTime - particles[firstActiveParticle * 4].Time;

                if (particleAge < particleDuration)
                    break;

                particles[firstActiveParticle * 4].Time = drawCounter;

                firstActiveParticle++;

                if (firstActiveParticle >= settings.MaxParticles)
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

                if (firstRetiredParticle >= settings.MaxParticles)
                    firstRetiredParticle = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            if (firstNewParticle != firstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }

            if (firstActiveParticle != firstFreeParticle)
            {
                context.BlendState = settings.BlendState;
                context.DepthStencilState = DepthStencilState.DepthRead;

                constantsPerFrame.ViewportScale = new Vector2(0.5f / context.Viewport.AspectRatio, -0.5f);
                constantsPerFrame.CurrentTime = currentTime;
                constantBufferPerFrame.SetData(Device.ImmediateContext, constantsPerFrame);

                context.VertexShader = vertexShader;
                context.PixelShader = pixelShader;

                context.VertexShaderConstantBuffers[0] = constantBufferPerShader;
                context.VertexShaderConstantBuffers[1] = constantBufferPerFrame;

                context.PixelShaderResources[0] = textureView;
                context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

                context.PrimitiveTopology = PrimitiveTopology.TriangleList;
                context.SetVertexBuffer(0, vertexBuffer);
                context.IndexBuffer = indexBuffer;

                if (firstActiveParticle < firstFreeParticle)
                {
                    context.DrawIndexed((firstFreeParticle - firstActiveParticle) * 6, firstActiveParticle * 6);
                }
                else
                {
                    context.DrawIndexed((settings.MaxParticles - firstActiveParticle) * 6, firstActiveParticle * 6);

                    if (firstFreeParticle > 0)
                    {
                        context.DrawIndexed(firstFreeParticle * 6);
                    }
                }

                context.DepthStencilState = DepthStencilState.Default;
            }

            drawCounter++;
        }

        void AddNewParticlesToVertexBuffer()
        {
            int stride = ParticleVertex.VertexDeclaration.Stride;

            if (firstNewParticle < firstFreeParticle)
            {
                vertexBuffer.SetData(Device.ImmediateContext,
                    firstNewParticle * stride * 4,
                    particles,
                    firstNewParticle * 4,
                    (firstFreeParticle - firstNewParticle) * 4,
                    SetDataOptions.NoOverwrite);
            }
            else
            {
                vertexBuffer.SetData(Device.ImmediateContext,
                    firstNewParticle * stride * 4,
                    particles,
                    firstNewParticle * 4,
                    (settings.MaxParticles - firstNewParticle) * 4,
                    SetDataOptions.NoOverwrite);

                if (firstFreeParticle > 0)
                {
                    vertexBuffer.SetData(Device.ImmediateContext,
                        firstNewParticle * stride * 4,
                        particles,
                        0,
                        firstFreeParticle * 4,
                        SetDataOptions.NoOverwrite);
                }
            }

            firstNewParticle = firstFreeParticle;
        }

        public void SetCamera(Matrix view, Matrix projection)
        {
            Matrix.Transpose(ref view, out constantsPerFrame.View);
            Matrix.Transpose(ref projection, out constantsPerFrame.Projection);
        }

        public void AddParticle(Vector3 position, Vector3 velocity)
        {
            int nextFreeParticle = firstFreeParticle + 1;

            if (nextFreeParticle >= settings.MaxParticles)
                nextFreeParticle = 0;

            if (nextFreeParticle == firstRetiredParticle)
                return;

            velocity *= settings.EmitterVelocitySensitivity;

            float horizontalVelocity = MathHelper.Lerp(settings.MinHorizontalVelocity,
                                                       settings.MaxHorizontalVelocity,
                                                       (float) random.NextDouble());

            double horizontalAngle = random.NextDouble() * MathHelper.TwoPi;

            velocity.X += horizontalVelocity * (float) Math.Cos(horizontalAngle);
            velocity.Z += horizontalVelocity * (float) Math.Sin(horizontalAngle);

            velocity.Y += MathHelper.Lerp(settings.MinVerticalVelocity,
                                          settings.MaxVerticalVelocity,
                                          (float) random.NextDouble());

            Color randomValues = new Color((byte) random.Next(255),
                                           (byte) random.Next(255),
                                           (byte) random.Next(255),
                                           (byte) random.Next(255));

            for (int i = 0; i < 4; i++)
            {
                particles[firstFreeParticle * 4 + i].Position = position;
                particles[firstFreeParticle * 4 + i].Velocity = velocity;
                particles[firstFreeParticle * 4 + i].Random = randomValues;
                particles[firstFreeParticle * 4 + i].Time = currentTime;
            }

            firstFreeParticle = nextFreeParticle;
        }
    }
}
