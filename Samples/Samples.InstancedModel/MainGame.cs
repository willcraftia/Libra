#region Using

using System;
using System.Collections.Generic;
using Libra;
using Libra.Games;
using Libra.Games.Debugging;
using Libra.Graphics;
using Libra.Graphics.Compiler;
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
            new VertexElement("TRANSFORM", 0, VertexFormat.Vector4,  0),
            new VertexElement("TRANSFORM", 1, VertexFormat.Vector4, 16),
            new VertexElement("TRANSFORM", 2, VertexFormat.Vector4, 32),
            new VertexElement("TRANSFORM", 3, VertexFormat.Vector4, 48)
            );

        InputLayout instanceInputLayout;

        FrameRateMeasure frameRateMeasure;

        KeyboardState lastKeyboardState;
        
        JoystickState lastGamePadState;
        
        KeyboardState currentKeyboardState;
        
        JoystickState currentGamePadState;

        public MainGame()
        {
            graphics = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            IsFixedTimeStep = false;

            graphics.SynchronizeWithVerticalRetrace = false;

            instances = new List<SpinningInstance>();

            for (int i = 0; i < InitialInstanceCount; i++)
                instances.Add(new SpinningInstance());
        }

        protected override void Initialize()
        {
            frameRateMeasure = new FrameRateMeasure(this);
            Components.Add(frameRateMeasure);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            var compiler = ShaderCompiler.CreateShaderCompiler();
            compiler.RootPath = "../../Shaders/";
            compiler.EnableStrictness = true;
            compiler.OptimizationLevel = OptimizationLevels.Level3;
            compiler.WarningsAreErrors = true;

            var instanceVsBytecode = compiler.CompileVertexShader("InstancedModel.fx", "HWInstancingVS");
            var vsBytecode = compiler.CompileVertexShader("InstancedModel.fx", "NoInstancingVS");
            var psBytecode = compiler.CompilePixelShader("InstancedModel.fx");

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

            spriteBatch = new SpriteBatch(DeviceContext);
            spriteFont = content.Load<SpriteFont>("Font");

            instancedModel = content.Load<Model>("Cats");
            instancedModelBones = new Matrix[instancedModel.Bones.Count];
            instancedModel.CopyAbsoluteBoneTransformsTo(instancedModelBones);

            instanceInputLayout = Device.CreateInputLayout();
            instanceInputLayout.Initialize(instanceVertexShader,
                new VertexDeclarationBinding(instancedModel.Meshes[0].MeshParts[0].VertexBuffer.VertexDeclaration),
                new VertexDeclarationBinding(instanceVertexDeclaration, 1, true, 1));
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            foreach (var instance in instances)
            {
                instance.Update(gameTime);
            }
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DeviceContext.Clear(Color.CornflowerBlue);

            var view = Matrix.CreateLookAt(new Vector3(0, 0, 15), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, DeviceContext.Viewport.AspectRatio, 1, 100);

            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.Default;
            DeviceContext.PrimitiveTopology = PrimitiveTopology.TriangleList;
            DeviceContext.PixelShader = pixelShader;
            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;

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

            base.Draw(gameTime);
        }

        void DrawModelHardwareInstancing(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
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

            DeviceContext.SetData(instanceVertexBuffer, instances, 0, instances.Length, SetDataOptions.Discard);

            DeviceContext.AutoResolveInputLayout = false;
            DeviceContext.InputLayout = instanceInputLayout;
            DeviceContext.VertexShader = instanceVertexShader;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    DeviceContext.SetVertexBuffers(
                        new VertexBufferBinding(meshPart.VertexBuffer),
                        new VertexBufferBinding(instanceVertexBuffer));

                    DeviceContext.IndexBuffer = meshPart.IndexBuffer;
                    DeviceContext.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                    Matrix.Transpose(ref modelBones[mesh.ParentBone.Index], out constants.World);
                    Matrix.Transpose(ref view, out constants.View);
                    Matrix.Transpose(ref projection, out constants.Projection);
                    DeviceContext.SetData(constantBuffer, constants);

                    DeviceContext.DrawIndexedInstanced(
                        meshPart.IndexCount, instances.Length, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                }
            }
        }

        void DrawModelNoInstancing(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
            DeviceContext.AutoResolveInputLayout = true;
            DeviceContext.VertexShader = vertexShader;

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    DeviceContext.SetVertexBuffer(meshPart.VertexBuffer);
                    DeviceContext.IndexBuffer = meshPart.IndexBuffer;
                    DeviceContext.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                    Matrix.Transpose(ref view, out constants.View);
                    Matrix.Transpose(ref projection, out constants.Projection);

                    for (int i = 0; i < instances.Length; i++)
                    {
                        Matrix world;
                        Matrix.Multiply(ref modelBones[mesh.ParentBone.Index], ref instances[i], out world);
                        Matrix.Transpose(ref world, out constants.World);
                        DeviceContext.SetData(constantBuffer, constants);

                        DeviceContext.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
                    }
                }
            }
        }

        void DrawModelNoInstancingOrStateBatching(Model model, Matrix[] modelBones, Matrix[] instances, Matrix view, Matrix projection)
        {
            DeviceContext.AutoResolveInputLayout = true;
            DeviceContext.VertexShader = vertexShader;

            for (int i = 0; i < instances.Length; i++)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var meshPart in mesh.MeshParts)
                    {
                        DeviceContext.SetVertexBuffer(meshPart.VertexBuffer);
                        DeviceContext.IndexBuffer = meshPart.IndexBuffer;
                        DeviceContext.PixelShaderResources[0] = (meshPart.Effect as BasicEffect).Texture;

                        Matrix world;
                        Matrix.Multiply(ref modelBones[mesh.ParentBone.Index], ref instances[i], out world);
                        Matrix.Transpose(ref world, out constants.World);
                        Matrix.Transpose(ref view, out constants.View);
                        Matrix.Transpose(ref projection, out constants.Projection);
                        DeviceContext.SetData(constantBuffer, constants);

                        DeviceContext.DrawIndexed(meshPart.IndexCount, meshPart.StartIndexLocation, meshPart.BaseVertexLocation);
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
                                     frameRateMeasure.FrameRate,
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

            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = Joystick.GetState();

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
