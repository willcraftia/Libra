#region Using

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class DeviceContext : IDisposable
    {
        #region MappedSubresource

        internal protected struct MappedSubresource
        {
            public IntPtr Pointer;

            public int RowPitch;

            public int DepthPitch;

            public MappedSubresource(IntPtr pointer, int rowPitch, int depthPitch)
            {
                Pointer = pointer;
                RowPitch = rowPitch;
                DepthPitch = depthPitch;
            }
        }

        #endregion

        #region MapMode

        internal protected enum MapMode
        {
            Read                = 1,
            Write               = 2,
            ReadWrite           = 3,
            WriteDiscard        = 4,
            WriteNoOverwrite    = 5,
        }

        #endregion

        #region ShaderStage

        protected internal enum ShaderStage
        {
            Vertex      = 0,
            Hull        = 1,
            Domain      = 2,
            Geometry    = 3,
            Pixel       = 4
        }

        #endregion

        #region ConstantBufferCollection

        public sealed class ConstantBufferCollection
        {
            public int Count = D3D11Constants.CommnonShaderConstantBufferApiSlotCount;

            DeviceContext context;

            ShaderStage shaderStage;

            ConstantBuffer[] buffers;

            int dirtyFlags;

            public ConstantBuffer this[int index]
            {
                get
                {
                    if ((uint) buffers.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    return buffers[index];
                }
                set
                {
                    if ((uint) buffers.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    if (buffers[index] == value)
                        return;

                    buffers[index] = value;

                    dirtyFlags |= 1 << index;
                }
            }

            internal ConstantBufferCollection(DeviceContext context, ShaderStage shaderStage)
            {
                this.context = context;
                this.shaderStage = shaderStage;

                buffers = new ConstantBuffer[Count];
            }

            internal void Apply()
            {
                if (dirtyFlags == 0)
                    return;

                for (int i = 0; i < buffers.Length; i++)
                {
                    int flag = 1 << i;
                    if ((dirtyFlags & flag) != 0)
                    {
                        context.SetConstantBufferCore(shaderStage, i, buffers[i]);

                        dirtyFlags &= ~flag;
                    }

                    if (dirtyFlags == 0)
                        break;
                }
            }
        }

        #endregion

        #region SamplerStateCollection

        public sealed class SamplerStateCollection
        {
            public int Count = D3D11Constants.CommnonShaderSamplerSlotCount;

            DeviceContext context;

            ShaderStage shaderStage;

            SamplerState[] samplers;

            int dirtyFlags;

            public SamplerState this[int index]
            {
                get
                {
                    if ((uint) samplers.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    return samplers[index];
                }
                set
                {
                    if ((uint) samplers.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    if (samplers[index] == value)
                        return;

                    samplers[index] = value;

                    dirtyFlags |= 1 << index;
                }
            }

            internal SamplerStateCollection(DeviceContext context, ShaderStage shaderStage)
            {
                this.context = context;
                this.shaderStage = shaderStage;

                samplers = new SamplerState[Count];
            }

            internal void Apply()
            {
                if (dirtyFlags == 0)
                    return;

                for (int i = 0; i < samplers.Length; i++)
                {
                    int flag = 1 << i;
                    if ((dirtyFlags & flag) != 0)
                    {
                        context.SetSamplerCore(shaderStage, i, samplers[i]);

                        dirtyFlags &= ~flag;
                    }

                    if (dirtyFlags == 0)
                        break;
                }
            }
        }

        #endregion

        #region ShaderResourceCollection

        public sealed class ShaderResourceCollection
        {
            /// <summary>
            /// D3D11 の上限は 128 ですが、ここではサンプラの最大スロット数に合わせて 16 とします。
            /// </summary>
            /// <remarks>
            /// D3D11.h: D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT ( 128 )
            /// </remarks>
            public int Count = 16;

            DeviceContext context;

            ShaderStage shaderStage;

            IShaderResourceView[] resources;

            int dirtyFlags;

            public IShaderResourceView this[int index]
            {
                get
                {
                    if ((uint) resources.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    return resources[index];
                }
                set
                {
                    if ((uint) resources.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    if (resources[index] == value)
                        return;

                    resources[index] = value;

                    dirtyFlags |= 1 << index;
                }
            }

            internal ShaderResourceCollection(DeviceContext context, ShaderStage shaderStage)
            {
                this.context = context;
                this.shaderStage = shaderStage;

                resources = new IShaderResourceView[Count];
            }

            internal void Apply()
            {
                if (dirtyFlags == 0)
                    return;

                for (int i = 0; i < resources.Length; i++)
                {
                    int flag = 1 << i;
                    if ((dirtyFlags & flag) != 0)
                    {
                        context.SetShaderResourceCore(shaderStage, i, resources[i]);

                        dirtyFlags &= ~flag;
                    }

                    if (dirtyFlags == 0)
                        break;
                }
            }
        }

        #endregion

        /// <summary>
        /// 頂点入力スロットの数。
        /// </summary>
        protected const int VertexInputResourceSlotCount = D3D11Constants.IAVertexInputResourceSlotCount;

        /// <summary>
        /// 同時利用が可能なレンダ ターゲットの数。
        /// </summary>
        protected const int RenderTargetCount = D3D11Constants.SimultaneousRenderTargetCount;

        static readonly Color DiscardColor = Color.Purple;

        InputLayout inputLayout;

        PrimitiveTopology primitiveTopology;

        VertexBufferBinding[] vertexBufferBindings;

        IndexBuffer indexBuffer;

        RasterizerState rasterizerState;

        Viewport viewport;

        Rectangle scissorRectangle;

        BlendState blendState;

        DepthStencilState depthStencilState;

        RenderTargetView[] activeRenderTargetViews;

        VertexShader vertexShader;

        PixelShader pixelShader;

        public event EventHandler Disposing;

        public IDevice Device { get; private set; }

        public abstract bool Deferred { get; }

        public bool AutoResolveInputLayout { get; set; }

        public InputLayout InputLayout
        {
            get { return inputLayout; }
            set
            {
                if (inputLayout == value) return;

                inputLayout = value;

                OnInputLayoutChanged();
            }
        }

        public PrimitiveTopology PrimitiveTopology
        {
            get { return primitiveTopology; }
            set
            {
                if (primitiveTopology == value) return;

                primitiveTopology = value;

                OnPrimitiveTopologyChanged();
            }
        }

        // offset 指定はひとまず無視する。
        // インデックス配列内のオフセットを指定する事が当面ないため。

        public IndexBuffer IndexBuffer
        {
            get { return indexBuffer; }
            set
            {
                if (indexBuffer == value) return;

                indexBuffer = value;

                OnIndexBufferChanged();
            }
        }

        public RasterizerState RasterizerState
        {
            get { return rasterizerState; }
            set
            {
                if (rasterizerState == value) return;

                rasterizerState = value;

                OnRasterizerStateChanged();
            }
        }

        public Viewport Viewport
        {
            get { return viewport; }
            set
            {
                viewport = value;

                OnViewportChanged();
            }
        }

        public Rectangle ScissorRectangle
        {
            get { return scissorRectangle; }
            set
            {
                scissorRectangle = value;

                OnScissorRectangleChanged();
            }
        }

        public Color BlendFactor { get; set; }

        public BlendState BlendState
        {
            get { return blendState; }
            set
            {
                if (blendState == value) return;

                blendState = value;

                OnBlendStateChanged();
            }
        }

        public DepthStencilState DepthStencilState
        {
            get { return depthStencilState; }
            set
            {
                if (depthStencilState == value) return;

                depthStencilState = value;

                OnDepthStencilStateChanged();
            }
        }

        public VertexShader VertexShader
        {
            get { return vertexShader; }
            set
            {
                if (vertexShader == value) return;

                vertexShader = value;

                OnVertexShaderChanged();
            }
        }

        public PixelShader PixelShader
        {
            get { return pixelShader; }
            set
            {
                if (pixelShader == value) return;

                pixelShader = value;

                OnPixelShaderChanged();
            }
        }

        public ConstantBufferCollection VertexShaderConstantBuffers { get; private set; }

        public ConstantBufferCollection PixelShaderConstantBuffers { get; private set; }

        public SamplerStateCollection PixelShaderSamplers { get; private set; }

        public ShaderResourceCollection PixelShaderResources { get; private set; }

        protected DeviceContext(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            // TODO
            //
            // 描画中にバック バッファのリサイズが発生したらどうするの？
            //Device.BackBuffersResetting += OnDeviceBackBuffersResetting;
            //Device.BackBuffersReset += OnDeviceBackBuffersReset;

            vertexBufferBindings = new VertexBufferBinding[VertexInputResourceSlotCount];

            activeRenderTargetViews = new RenderTargetView[RenderTargetCount];

            VertexShaderConstantBuffers = new ConstantBufferCollection(this, ShaderStage.Vertex);
            PixelShaderConstantBuffers = new ConstantBufferCollection(this, ShaderStage.Pixel);

            PixelShaderSamplers = new SamplerStateCollection(this, ShaderStage.Pixel);

            PixelShaderResources = new ShaderResourceCollection(this, ShaderStage.Pixel);

            AutoResolveInputLayout = true;
        }

        //void OnDeviceBackBuffersReset(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //void OnDeviceBackBuffersResetting(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        public VertexBufferBinding GetVertexBuffer(int slot)
        {
            if ((uint) VertexInputResourceSlotCount < (uint) slot) throw new ArgumentOutOfRangeException("slot");

            return vertexBufferBindings[slot];
        }

        public VertexBufferBinding[] GetVertexBuffers()
        {
            return (VertexBufferBinding[]) vertexBufferBindings.Clone();
        }

        public void SetVertexBuffer(VertexBuffer buffer, int offset = 0)
        {
            SetVertexBuffer(0, new VertexBufferBinding(buffer, offset));
        }

        public void SetVertexBuffer(int slot, VertexBuffer buffer, int offset = 0)
        {
            SetVertexBuffer(slot, new VertexBufferBinding(buffer, offset));
        }

        public void SetVertexBuffer(int slot, VertexBufferBinding binding)
        {
            if ((uint) VertexInputResourceSlotCount < (uint) slot) throw new ArgumentOutOfRangeException("slot");

            vertexBufferBindings[slot] = binding;

            SetVertexBufferCore(slot, ref binding);
        }

        public void SetVertexBuffers(params VertexBufferBinding[] bindings)
        {
            if (bindings == null) throw new ArgumentNullException("bindings");
            if ((uint) VertexInputResourceSlotCount < (uint) bindings.Length) throw new ArgumentOutOfRangeException("bindings.Length");

            Array.Copy(bindings, 0, vertexBufferBindings, 0, bindings.Length);

            SetVertexBuffersCore(bindings);
        }

        protected abstract void OnInputLayoutChanged();

        protected abstract void OnPrimitiveTopologyChanged();

        protected abstract void OnIndexBufferChanged();

        protected abstract void SetVertexBufferCore(int slot, ref VertexBufferBinding binding);

        protected abstract void SetVertexBuffersCore(VertexBufferBinding[] bindings);

        protected abstract void OnRasterizerStateChanged();

        protected abstract void OnViewportChanged();

        protected abstract void OnScissorRectangleChanged();

        public RenderTargetView GetRenderTarget()
        {
            return activeRenderTargetViews[0];
        }

        public void GetRenderTargets(RenderTargetView[] result)
        {
            if (result == null) throw new ArgumentNullException("result");

            Array.Copy(activeRenderTargetViews, result, Math.Min(activeRenderTargetViews.Length, result.Length));
        }

        public void SetRenderTarget(RenderTargetView renderTargetView)
        {
            if (renderTargetView == null)
            {
                // アクティブなレンダ ターゲットをクリア。
                Array.Clear(activeRenderTargetViews, 0, activeRenderTargetViews.Length);

                // #0 にバック バッファ レンダ ターゲットを設定。
                activeRenderTargetViews[0] = Device.BackBufferRenderTargetView;

                SetRenderTargetsCore(null);

                ClearRenderTargetView(Device.BackBufferRenderTargetView, DiscardColor);
            }
            else
            {
                activeRenderTargetViews[0] = renderTargetView;

                SetRenderTargetsCore(activeRenderTargetViews);

                if (renderTargetView.RenderTarget.RenderTargetUsage == RenderTargetUsage.Discard)
                {
                    ClearRenderTargetView(renderTargetView, DiscardColor);
                }
            }
        }

        public void SetRenderTargets(params RenderTargetView[] renderTargetViews)
        {
            if (renderTargetViews == null) throw new ArgumentNullException("renderTargetViews");
            if (RenderTargetCount < renderTargetViews.Length)
                throw new ArgumentOutOfRangeException("renderTargetViews");
            if (renderTargetViews.Length == 0)
                throw new ArgumentException("renderTargetViews is empty", "renderTargets");
            if (renderTargetViews[0] == null)
                throw new ArgumentException(string.Format("renderTargetViews[{0}] is null.", 0), "renderTargetViews");

            Array.Copy(renderTargetViews, activeRenderTargetViews, renderTargetViews.Length);
            if (renderTargetViews.Length < RenderTargetCount)
                Array.Clear(activeRenderTargetViews, renderTargetViews.Length, (RenderTargetCount - renderTargetViews.Length));

            SetRenderTargetsCore(renderTargetViews);

            if (renderTargetViews[0].RenderTarget.RenderTargetUsage == RenderTargetUsage.Discard)
            {
                foreach (var renderTargetView in renderTargetViews)
                {
                    ClearRenderTargetView(renderTargetView, DiscardColor);
                }
            }
        }

        protected abstract void SetRenderTargetsCore(RenderTargetView[] renderTargets);

        protected abstract void OnBlendStateChanged();

        protected abstract void OnDepthStencilStateChanged();

        protected abstract void OnVertexShaderChanged();

        protected abstract void OnPixelShaderChanged();

        protected abstract void SetConstantBufferCore(ShaderStage shaderStage, int slot, ConstantBuffer buffer);

        protected abstract void SetSamplerCore(ShaderStage shaderStage, int slot, SamplerState state);

        protected abstract void SetShaderResourceCore(ShaderStage shaderStage, int slot, IShaderResourceView view);

        public void ClearRenderTargetView(RenderTargetView renderTarget, Color color)
        {
            ClearRenderTargetView(renderTarget, ClearOptions.Target | ClearOptions.Depth | ClearOptions.Stencil, color, Viewport.MaxDepth);
        }

        public void ClearRenderTargetView(RenderTargetView renderTarget, Vector4 color)
        {
            ClearRenderTargetView(renderTarget, ClearOptions.Target | ClearOptions.Depth | ClearOptions.Stencil, color, Viewport.MaxDepth);
        }

        public void ClearRenderTargetView(
            RenderTargetView renderTarget, ClearOptions options, Color color, float depth, byte stencil = 0)
        {
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            ClearRenderTargetView(renderTarget, options, color.ToVector4(), depth, stencil);
        }

        public void ClearRenderTargetView(
            RenderTargetView renderTarget, ClearOptions options, Vector4 color, float depth, byte stencil = 0)
        {
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            ClearRenderTargetCore(renderTarget, options, ref color, depth, stencil);
        }

        protected abstract void ClearRenderTargetCore(
            RenderTargetView renderTarget, ClearOptions options, ref Vector4 color, float depth, byte stencil);

        public void Clear(Color color)
        {
            Clear(color.ToVector4());
        }

        public void Clear(Vector4 color)
        {
            Clear(ClearOptions.Target | ClearOptions.Depth | ClearOptions.Stencil, color, Viewport.MaxDepth);
        }

        public void Clear(ClearOptions options, Color color, float depth = 1f, byte stencil = 0)
        {
            Clear(options, color.ToVector4(), depth, stencil);
        }

        public void Clear(ClearOptions options, Vector4 color, float depth = 1f, byte stencil = 0)
        {
            // アクティブな全レンダ ターゲットをクリア。
            for (int i = 0; i < activeRenderTargetViews.Length; i++)
            {
                var renderTarget = activeRenderTargetViews[i];
                if (renderTarget != null)
                {
                    ClearRenderTargetCore(renderTarget, options, ref color, depth, stencil);
                }
            }
        }

        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            ApplyState();

            DrawCore(vertexCount, startVertexLocation);
        }

        public void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0)
        {
            ApplyState();

            DrawIndexedCore(indexCount, startIndexLocation, baseVertexLocation);
        }

        public void DrawInstanced(int vertexCountPerInstance, int instanceCount,
            int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            ApplyState();

            DrawInstancedCore(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
        }

        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount,
            int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            ApplyState();

            DrawIndexedInstancedCore(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        protected abstract void DrawCore(int vertexCount, int startVertexLocation);

        protected abstract void DrawIndexedCore(int indexCount, int startIndexLocation, int baseVertexLocation);

        protected abstract void DrawInstancedCore(int vertexCountPerInstance, int instanceCount,
            int startVertexLocation, int startInstanceLocation);

        protected abstract void DrawIndexedInstancedCore(int indexCountPerInstance, int instanceCount,
            int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0);

        internal protected abstract MappedSubresource Map(Resource resource, int subresource, MapMode mapMode);

        internal protected abstract void Unmap(Resource resource, int subresource);

        internal protected abstract void UpdateSubresource(
            Resource destinationResource, int destinationSubresource, Box? destinationBox,
            IntPtr sourcePointer, int sourceRowPitch, int sourceDepthPitch);

        void ApplyState()
        {
            if (AutoResolveInputLayout)
            {
                // 入力レイアウト自動解決は、入力スロット #0 の頂点バッファの頂点宣言を参照。
                var vertexBuffer = vertexBufferBindings[0].VertexBuffer;
                if (vertexBuffer != null)
                {
                    var inputLayout = vertexShader.GetInputLayout(vertexBuffer.VertexDeclaration);
                    InputLayout = inputLayout;
                }
            }

            // 状態検査。
            if (InputLayout == null) throw new InvalidOperationException("InputLayout is null.");

            // 定数バッファの反映。
            VertexShaderConstantBuffers.Apply();
            PixelShaderConstantBuffers.Apply();

            // サンプラ ステートの反映。
            PixelShaderSamplers.Apply();

            // シェーダ リソースの反映。
            PixelShaderResources.Apply();
        }

        protected virtual void OnDisposing(object sender, EventArgs e)
        {
            if (Disposing != null)
                Disposing(sender, e);
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~DeviceContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            OnDisposing(this, EventArgs.Empty);

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
