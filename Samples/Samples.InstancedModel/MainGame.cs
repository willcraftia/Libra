#region Using

using System;
using System.Collections.Generic;
using Libra;
using Libra.Games;
using Libra.Games.SharpDX;
using Libra.Graphics;
using Libra.Graphics.SharpDX.Compiler;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.InstancedModel
{
    public sealed class MainGame : Game
    {
        #region InstancingTechnique

        public enum InstancingTechnique
        {
            HardwareInstancing,
            NoInstancing,
            NoInstancingOrStateBatching
        }

        #endregion

        #region Constants

        struct Constants
        {
            public Matrix World;

            public Matrix View;

            public Matrix Projection;

            // シェーダでは float3 だがバイトの並びを合わせるために Vector4。
            public Vector4 LightDirection;

            // シェーダでは float3 だがバイトの並びを合わせるために Vector4。
            public Vector4 DiffuseLight;

            // シェーダでは float3 だがバイトの並びを合わせるために Vector4。
            public Vector4 AmbientLight;
        }

        #endregion

        IGamePlatform platform;

        GraphicsManager graphics;

        XnbManager content;

        VertexShader instanceVertexShader;

        VertexShader vertexShader;

        PixelShader pixelShader;

        ConstantBuffer constantBuffer;

        Constants constants;

        SpriteBatch spriteBatch;

        SpriteFont spriteFont;

        InstancingTechnique instancingTechnique = InstancingTechnique.HardwareInstancing;

        const int InitialInstanceCount = 1000;

        List<SpinningInstance> instances;
        
        Matrix[] instanceTransforms;
        
        Model instancedModel;
        
        Matrix[] instancedModelBones;
        
        VertexBuffer instanceVertexBuffer;

        static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration(
            new VertexElement("TRANSFORM", 0, InputElementFormat.Vector4,  0),
            new VertexElement("TRANSFORM", 1, InputElementFormat.Vector4, 16),
            new VertexElement("TRANSFORM", 2, InputElementFormat.Vector4, 32),
            new VertexElement("TRANSFORM", 3, InputElementFormat.Vector4, 48)
            );

        InputLayout instanceInputLayout;

        int frameRate;
        
        int frameCounter;
        
        TimeSpan elapsedTime;

        IKeyboard keyboard;

        IJoystick joystick;

        KeyboardState lastKeyboardState;
        
        JoystickState lastGamePadState;
        
        KeyboardState currentKeyboardState;
        
        JoystickState currentGamePadState;

        public MainGame()
        {
            platform = new SdxFormGamePlatform(this)
            {
                DirectInputEnabled = true
            };
            graphics = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            IsFixedTimeStep = false;

            graphics.SynchronizeWithVerticalRetrace = false;

            instances = new List<SpinningInstance>();

            for (int i = 0; i < InitialInstanceCount; i++)
                instances.Add(new SpinningInstance());
        }

        protected override void LoadContent()
        {
            var compiler = new ShaderCompiler();
            compiler.RootPath = "../../Shaders/";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var instanceVsBytecode = compiler.CompileVertexShader("InstancedModel.fx", "HWInstancingVS");
            var vsBytecode = compiler.CompileVertexShader("InstancedModel.fx", "NoInstancingVS");
            var psBytecode = compiler.CompilePixelShader("InstancedModel.fx", "PS");

            instanceVertexShader = Device.CreateVertexShader();
            instanceVertexShader.Initialize(instanceVsBytecode);

            vertexShader = Device.CreateVertexShader();
            vertexShader.Initialize(vsBytecode);

            pixelShader = Device.CreatePixelShader();
            pixelShader.Initialize(psBytecode);

            constantBuffer = Device.CreateConstantBuffer();
            constantBuffer.Usage = ResourceUsage.Dynamic;
            constantBuffer.Initialize<Constants>();

            constants.LightDirection = Vector3.Normalize(new Vector3(-1, -1, -1)).ToVector4();
            constants.DiffuseLight = new Vector4(1.25f, 1.25f, 1.25f, 0);
            constants.AmbientLight = new Vector4(0.25f, 0.25f, 0.25f, 0);

            spriteBatch = new SpriteBatch(Device.ImmediateContext);
            
            spriteFont = content.Load<SpriteFont>("Font");

            instancedModel = content.Load<Model>("Cats");
            instancedModelBones = new Matrix[instancedModel.Bones.Count];
            instancedModel.CopyAbsoluteBoneTransformsTo(instancedModelBones);

            instanceInputLayout = Device.CreateInputLayout();
            instanceInputLayout.Initialize(instanceVertexShader,
                new VertexDeclarationBinding(instancedModel.Meshes[0].MeshParts[0].VertexBuffer.VertexDeclaration),
                new VertexDeclarationBinding(instanceVertexDeclaration, 1, true, 1));

            keyboard = platform.CreateKeyboard();
            joystick = platform.CreateJoystick();
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            foreach (var instance in instances)
            {
                instance.Update(gameTime);
            }

            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            } 
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            var view = Matrix.CreateLookAt(new Vector3(0, 0, 15), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, context.Viewport.AspectRatio, 1,  100);

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;
            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.PixelShader = pixelShader;
            context.VertexShaderConstantBuffers[0] = constantBuffer;

            Array.Resize(ref instanceTransforms, instances.Count);

            for (int i = 0; i < instances.Count; i++)
            {
                instanceTransforms[i] = instances[i].Transform;
            }

            switch (instancingTechnique)
            {
                case InstancingTechnique.HardwareInstancing:
                    DrawModelHardwareInstancing(instancedModel, instancedModelBones, instanceTransforms, view, projection);
                    break;

                case InstancingTechnique.NoInstancing:
                    DrawModelNoInstancing(instancedModel, instancedModelBones, instanceTransforms, view, projection);
                    break;

                case InstancingTechnique.NoInstancingOrStateBatching:
                    DrawModelNoInstancingOrStateBatching(instancedModel, instancedModelBones, instanceTransforms, view, projection);
                    break;
            }

            DrawOverlayText();
            
            frameCounter++;

            base.Draw(gameTime);
        }

        void DrawModelHardwareInstancing(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
            var context = Device.ImmediateContext;

            if (instances.Length == 0)
                return;

            if ((instanceVertexBuffer == null) || (instances.Length > instanceVertexBuffer.VertexCount))
            {
                if (instanceVertexBuffer != null)
                    instanceVertexBuffer.Dispose();

                instanceVertexBuffer = Device.CreateVertexBuffer();
                instanceVertexBuffer.Usage = ResourceUsage.Dynamic;
                instanceVertexBuffer.Initialize(instanceVertexDeclaration, instances.Length);
            }

            instanceVertexBuffer.SetData(context, instances, 0, instances.Length, SetDataOptions.Discard);

            context.AutoResolveInputLayout = false;
            context.InputLayout = instanceInputLayout;
            context.VertexShader = instanceVertexShader;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffers(
                        new VertexBufferBinding(meshPart.VertexBuffer),
                        new VertexBufferBinding(instanceVertexBuffer));
                    
                    context.IndexBuffer = meshPart.IndexBuffer;
                    context.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                    Matrix.Transpose(ref modelBones[mesh.ParentBone.Index], out constants.World);
                    Matrix.Transpose(ref view, out constants.View);
                    Matrix.Transpose(ref projection, out constants.Projection);
                    constantBuffer.SetData(context, constants);

                    context.DrawIndexedInstanced(
                        meshPart.IndexCount, instances.Length, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void DrawModelNoInstancing(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
            var context = Device.ImmediateContext;

            context.AutoResolveInputLayout = true;
            context.VertexShader = vertexShader;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    context.SetVertexBuffer(meshPart.VertexBuffer);
                    context.IndexBuffer = meshPart.IndexBuffer;
                    context.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                    Matrix.Transpose(ref view, out constants.View);
                    Matrix.Transpose(ref projection, out constants.Projection);

                    for (int i = 0; i < instances.Length; i++)
                    {
                        Matrix world;
                        Matrix.Multiply(ref modelBones[mesh.ParentBone.Index], ref instances[i], out world);
                        Matrix.Transpose(ref world, out constants.World);
                        constantBuffer.SetData(context, constants);

                        context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                    }
                }
            }
        }

        void DrawModelNoInstancingOrStateBatching(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
            var context = Device.ImmediateContext;

            context.AutoResolveInputLayout = true;
            context.VertexShader = vertexShader;

            for (int i = 0; i < instances.Length; i++)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var meshPart in mesh.MeshParts)
                    {
                        context.SetVertexBuffer(meshPart.VertexBuffer);
                        context.IndexBuffer = meshPart.IndexBuffer;
                        context.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                        Matrix world;
                        Matrix.Multiply(ref modelBones[mesh.ParentBone.Index], ref instances[i], out world);
                        Matrix.Transpose(ref world, out constants.World);
                        Matrix.Transpose(ref view, out constants.View);
                        Matrix.Transpose(ref projection, out constants.Projection);
                        constantBuffer.SetData(context, constants);

                        context.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                    }
                }
            }
        }

        void DrawOverlayText()
        {
            var text = string.Format("Frames per second: {0}\n" +
                                     "Instances: {1}\n" +
                                     "Technique: {2}\n\n" +
                                     "A = Change technique\n" +
                                     "X = Add instances\n" +
                                     "Y = Remove instances\n",
                                     frameRate,
                                     instances.Count,
                                     instancingTechnique);

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 65), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 64), Color.White);

            spriteBatch.End();
        }

        void HandleInput()
        {
            lastKeyboardState = currentKeyboardState;
            lastGamePadState = currentGamePadState;

            currentKeyboardState = keyboard.GetState();
            currentGamePadState = joystick.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            int instanceChangeRate = Math.Max(instances.Count / 100, 1);

            if (currentKeyboardState.IsKeyDown(Keys.X) ||
                currentGamePadState.Buttons.X == ButtonState.Pressed)
            {
                for (int i = 0; i < instanceChangeRate; i++)
                {
                    instances.Add(new SpinningInstance());
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.Y) ||
                currentGamePadState.Buttons.Y == ButtonState.Pressed)
            {
                for (int i = 0; i < instanceChangeRate; i++)
                {
                    if (instances.Count == 0)
                        break;

                    instances.RemoveAt(instances.Count - 1);
                }
            }

            if ((currentKeyboardState.IsKeyDown(Keys.A) &&
                 lastKeyboardState.IsKeyUp(Keys.A)) ||
                (currentGamePadState.Buttons.A == ButtonState.Pressed &&
                 lastGamePadState.Buttons.A == ButtonState.Released))
            {
                instancingTechnique++;

                if (instancingTechnique > InstancingTechnique.NoInstancingOrStateBatching)
                    instancingTechnique = 0;
            }
        }
    }

    #region Program

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new MainGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
