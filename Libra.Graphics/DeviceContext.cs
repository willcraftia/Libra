#region Using

using System;
using System.Collections.Generic;
using System.IO;
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

            ShaderResourceView[] views;

            int dirtyFlags;

            public ShaderResourceView this[int index]
            {
                get
                {
                    if ((uint) views.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    return views[index];
                }
                set
                {
                    if ((uint) views.Length <= (uint) index) throw new ArgumentOutOfRangeException("index");

                    if (views[index] == value)
                        return;

                    views[index] = value;

                    dirtyFlags |= 1 << index;
                }
            }

            internal ShaderResourceCollection(DeviceContext context, ShaderStage shaderStage)
            {
                this.context = context;
                this.shaderStage = shaderStage;

                views = new ShaderResourceView[Count];
            }

            internal void Remove(Resource resource)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    var view = views[i];

                    if (view != null && view.Resource == resource)
                    {
                        views[i] = null;

                        dirtyFlags |= 1 << i;
                    }
                }
            }

            internal void Apply()
            {
                if (dirtyFlags == 0)
                    return;

                for (int i = 0; i < views.Length; i++)
                {
                    int flag = 1 << i;
                    if ((dirtyFlags & flag) != 0)
                    {
                        context.SetShaderResourceCore(shaderStage, i, views[i]);

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

        Dictionary<Type, WeakReference> sharedResourceMap;

        InputLayout inputLayout;

        PrimitiveTopology primitiveTopology;

        VertexBufferBinding[] vertexBufferBindings;

        IndexBuffer indexBuffer;

        RasterizerState rasterizerState;

        Viewport viewport;

        Rectangle scissorRectangle;

        BlendState blendState;

        DepthStencilState depthStencilState;

        DepthStencilView activeDepthStencilView;

        RenderTargetView[] activeRenderTargetViews;

        bool backBufferActive;

        VertexShader vertexShader;

        PixelShader pixelShader;

        public event EventHandler Disposing;

        public Device Device { get; private set; }

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

        public DepthStencilView DepthStencil
        {
            get { return activeDepthStencilView; }
        }

        protected RenderTargetView BackBufferView
        {
            get { return Device.BackBufferView; }
        }

        protected DeviceContext(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            sharedResourceMap = new Dictionary<Type, WeakReference>();

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

        public TSharedResource GetSharedResource<TKey, TSharedResource>() where TSharedResource : class
        {
            return GetSharedResource(typeof(TKey), typeof(TSharedResource)) as TSharedResource;
        }

        /// <summary>
        /// デバイス コンテキスト単位で共有するリソースを共有リソース キャッシュより取得します。
        /// 共有リソース キャッシュに指定のリソースが存在しない場合には、
        /// 新たに生成するインスタンスをキャッシュに追加してから返却します。
        /// 
        /// キャッシュする共有リソースのクラスは、
        /// DeviceContext を引数とする公開コンストラクタを定義しなければなりません。
        /// 
        /// 共有リソースの利用側クラスは、このメソッドにより取得した共有リソースへの参照を
        /// インスタンス フィールドで保持する必要があります。
        /// これは、デバイス コンテキストは共有リソースを弱参照でキャッシュしているためです。
        /// 
        /// 共有リソースを弱参照で管理しているため、
        /// どのインスタンスからも参照されなくなった共有リソースはデバイス コンテキストから自動的に削除されます。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sharedResourceType"></param>
        /// <returns></returns>
        public object GetSharedResource(Type key, Type sharedResourceType)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (sharedResourceType == null) throw new ArgumentNullException("sharedResourceType");

            lock (sharedResourceMap)
            {
                object sharedResource = null;
                WeakReference reference;
                if (sharedResourceMap.TryGetValue(key, out reference))
                {
                    sharedResource = reference.Target;
                }
                else
                {
                    reference = new WeakReference(null);
                    sharedResourceMap[key] = reference;
                }

                if (sharedResource == null)
                {
                    sharedResource = Activator.CreateInstance(sharedResourceType, this);
                    reference.Target = sharedResource;
                }

                return sharedResource;
            }
        }

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

        // ※※※※※注意※※※※※
        //
        // D3D の振る舞いとして、
        // RenderTarget の RenderTargetView が OMSetRenderTargetView に渡された時点で、
        // RenderTarget の ShaderResourceView が設定されているスロットが自動的に null に設定される。
        // これは、RenderTarget に対する同時読み書きを避けるための振る舞いである。
        //
        // このため、このクラスにおいても同様の振る舞いをエミュレートする必要がある。
        // RenderTarget の RenderTargetView が OMSetRenderTargetView に渡される直前に、
        // ShaderResourceView として登録されている RenderTarget を解除しなければならない。
        //
        // 仮に、D3D の振る舞いをエミュレートしなかった場合、
        // このクラスには D3D 内部では null にされているスロットに ShaderResourceView が残り、
        // 次回の ShaderResourceView では状態変更無しと判定される可能性があり、
        // ShaderResourceView が正しく反映されないまま描画が行われる問題が発生する。

        public void SetRenderTarget(RenderTargetView renderTargetView)
        {
            if (renderTargetView == null)
            {
                SetRenderTarget(null, null);
            }
            else
            {
                SetRenderTarget(renderTargetView.DepthStencilView, renderTargetView);
            }
        }

        public void SetRenderTarget(DepthStencilView depthStencilView, RenderTargetView renderTargetView)
        {
            backBufferActive = false;

            if (renderTargetView == null)
            {
                if (depthStencilView != null)
                    throw new ArgumentException("A depth stencil must be set along with a render target.", "depthStencilView");

                // アクティブな深度ステンシルとレンダ ターゲットをクリア。
                activeDepthStencilView = null;
                Array.Clear(activeRenderTargetViews, 0, activeRenderTargetViews.Length);

                backBufferActive = true;

                SetRenderTargetsCore(null, null);

                ClearRenderTarget(BackBufferView, DiscardColor);

                // ビューポートを更新。
                var renderTarget = BackBufferView.RenderTarget;
                Viewport = new Viewport(0, 0, renderTarget.Width, renderTarget.Height);
            }
            else
            {
                // シェーダ リソースとして設定されているものを解除。
                if (depthStencilView != null)
                    PixelShaderResources.Remove(depthStencilView.DepthStencil);

                PixelShaderResources.Remove(renderTargetView.RenderTarget);

                activeDepthStencilView = depthStencilView;
                activeRenderTargetViews[0] = renderTargetView;

                SetRenderTargetsCore(activeDepthStencilView, activeRenderTargetViews);

                if (renderTargetView.RenderTarget.RenderTargetUsage == RenderTargetUsage.Discard)
                {
                    ClearRenderTarget(renderTargetView, DiscardColor);
                }

                // ビューポートを更新。
                var renderTarget = renderTargetView.RenderTarget;
                Viewport = new Viewport(0, 0, renderTarget.Width, renderTarget.Height);
            }
        }

        public void SetRenderTargets(params RenderTargetView[] renderTargetViews)
        {
            if (renderTargetViews == null) throw new ArgumentNullException("renderTargetViews");
            if (renderTargetViews.Length == 0) throw new ArgumentException("renderTargetViews is empty", "renderTargets");
            if (renderTargetViews[0] == null)
                throw new ArgumentException(string.Format("renderTargetViews[{0}] is null.", 0), "renderTargetViews");

            // 先頭要素のレンダ ターゲットが保持する深度ステンシルの設定を試行。
            SetRenderTargets(renderTargetViews[0].DepthStencilView, renderTargetViews);
        }

        public void SetRenderTargets(DepthStencilView depthStencilView, params RenderTargetView[] renderTargetViews)
        {
            if (renderTargetViews == null) throw new ArgumentNullException("renderTargetViews");
            if (renderTargetViews.Length == 0) throw new ArgumentException("renderTargetViews is empty", "renderTargets");
            if (RenderTargetCount < renderTargetViews.Length) throw new ArgumentOutOfRangeException("renderTargetViews");
            if (renderTargetViews[0] == null)
                throw new ArgumentException(string.Format("renderTargetViews[{0}] is null.", 0), "renderTargetViews");

            backBufferActive = false;

            if (depthStencilView != null)
                PixelShaderResources.Remove(depthStencilView.DepthStencil);

            activeDepthStencilView = depthStencilView;

            for (int i = 0; i < activeRenderTargetViews.Length; i++)
            {
                if (i < renderTargetViews.Length)
                {
                    var renderTargetView = renderTargetViews[i];
                    if (renderTargetView != null)
                        PixelShaderResources.Remove(renderTargetView.RenderTarget);

                    activeRenderTargetViews[i] = renderTargetView;
                }
                else
                {
                    activeRenderTargetViews[i] = null;
                }
            }

            SetRenderTargetsCore(depthStencilView, renderTargetViews);

            for (int i = 0; i < renderTargetViews.Length; i++)
            {
                var renderTargetView = renderTargetViews[i];

                if (renderTargetView != null)
                {
                    if (renderTargetView.RenderTarget.RenderTargetUsage == RenderTargetUsage.Discard)
                    {
                        ClearRenderTarget(renderTargetView, DiscardColor);
                    }
                }
            }
        }

        protected abstract void SetRenderTargetsCore(DepthStencilView depthStencilView, RenderTargetView[] renderTargets);

        protected abstract void OnBlendStateChanged();

        protected abstract void OnDepthStencilStateChanged();

        protected abstract void OnVertexShaderChanged();

        protected abstract void OnPixelShaderChanged();

        protected abstract void SetConstantBufferCore(ShaderStage shaderStage, int slot, ConstantBuffer buffer);

        protected abstract void SetSamplerCore(ShaderStage shaderStage, int slot, SamplerState state);

        protected abstract void SetShaderResourceCore(ShaderStage shaderStage, int slot, ShaderResourceView view);

        public void ClearDepthStencil(DepthStencilView depthStencil, float depth = 1, byte stencil = 0)
        {
            ClearDepthStencil(depthStencil, ClearOptions.Depth | ClearOptions.Stencil, depth, stencil);
        }

        public void ClearDepthStencil(DepthStencilView depthStencil, ClearOptions options, float depth = 1, byte stencil = 0)
        {
            if (depthStencil == null) throw new ArgumentNullException("depthStencil");

            ClearDepthStencilCore(depthStencil, options, depth, stencil);
        }

        public void ClearRenderTarget(RenderTargetView renderTarget, Color color)
        {
            ClearRenderTarget(renderTarget, ClearOptions.Target | ClearOptions.Depth | ClearOptions.Stencil, color, Viewport.MaxDepth);
        }

        public void ClearRenderTarget(RenderTargetView renderTarget, Vector4 color)
        {
            ClearRenderTarget(renderTarget, ClearOptions.Target | ClearOptions.Depth | ClearOptions.Stencil, color, Viewport.MaxDepth);
        }

        public void ClearRenderTarget(RenderTargetView renderTarget, ClearOptions options, Color color, float depth = 1, byte stencil = 0)
        {
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            ClearRenderTarget(renderTarget, options, color.ToVector4(), depth, stencil);
        }

        public void ClearRenderTarget(RenderTargetView renderTarget, ClearOptions options, Vector4 color, float depth = 1, byte stencil = 0)
        {
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            ClearRenderTargetCore(renderTarget, options, ref color, depth, stencil);
        }

        protected abstract void ClearDepthStencilCore(DepthStencilView depthStencilView, ClearOptions options, float depth, byte stencil);

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

        public void Clear(ClearOptions options, Color color, float depth = 1, byte stencil = 0)
        {
            Clear(options, color.ToVector4(), depth, stencil);
        }

        public void Clear(ClearOptions options, Vector4 color, float depth = 1, byte stencil = 0)
        {
            if (backBufferActive)
            {
                ClearRenderTargetCore(BackBufferView, options, ref color, depth, stencil);
            }
            else
            {
                if (activeDepthStencilView != null)
                    ClearDepthStencilCore(activeDepthStencilView, options, depth, stencil);

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

        internal void GetData<T>(
            Texture2D texture,
            int arrayIndex,
            int mipLevel,
            Rectangle? rectangle,
            T[] data,
            int startIndex,
            int elementCount) where T : struct
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if ((uint) (D3D11Constants.ReqTexture2dArrayAxisDimension - 1) < (uint) arrayIndex)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (mipLevel < 0) throw new ArgumentOutOfRangeException("mipLevel");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            GetDataCore(texture, arrayIndex, mipLevel, rectangle, data, startIndex, elementCount);
        }

        internal void SetData<T>(
            Texture2D texture,
            int arrayIndex,
            int mipLevel,
            T[] data,
            int startIndex,
            int elementCount) where T : struct
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if ((uint) (D3D11Constants.ReqTexture2dArrayAxisDimension - 1) < (uint) arrayIndex)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (mipLevel < 0) throw new ArgumentOutOfRangeException("mipLevel");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            if (texture.Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("The specified texture is immutable.");

            int levelWidth = texture.Width >> mipLevel;

            // ブロック圧縮ならばブロック サイズで調整。
            // この場合、FormatHelper.SizeInBytes で測る値は、
            // 1 ブロック (4x4 テクセル) に対するバイト数である点に注意。
            if (FormatHelper.IsBlockCompression(texture.Format))
            {
                levelWidth /= 4;
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var dataPointer = gcHandle.AddrOfPinnedObject();
                var sizeOfT = Marshal.SizeOf(typeof(T));

                var sourcePointer = (IntPtr) (dataPointer + startIndex * sizeOfT);

                if (texture.Usage == ResourceUsage.Default)
                {
                    // Immutable と Dynamic 以外は UpdateSubresource で更新可能。
                    // Staging は内部利用にとどめるため Default でのみ UpdateSubresource で更新。
                    int rowPitch = FormatHelper.SizeInBytes(texture.Format) * levelWidth;
                    UpdateSubresource(texture, mipLevel, null, sourcePointer, rowPitch, 0);
                }
                else
                {
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    // ポインタの移動に用いるため、フォーマットから測れる要素サイズで算出しなければならない。
                    // SizeOf(typeof(T)) では、例えばバイト配列などを渡した場合に、
                    // そのサイズは元配列の要素の移動となり、リソース要素の移動にはならない。
                    var rowSpan = FormatHelper.SizeInBytes(texture.Format) * levelWidth;

                    // TODO
                    //
                    // Dynamic だと D3D11MapMode.Write はエラーになる。
                    // 対応関係を MSDN から把握できないが、どうすべきか。
                    // ひとまず WriteDiscard とする。

                    var subresourceIndex = Resource.CalculateSubresource(mipLevel, arrayIndex, texture.MipLevels);
                    var mappedResource = Map(texture, subresourceIndex, DeviceContext.MapMode.WriteDiscard);
                    try
                    {
                        var rowSourcePointer = sourcePointer;
                        var destinationPointer = mappedResource.Pointer;

                        for (int i = 0; i < texture.Height; i++)
                        {
                            GraphicsHelper.CopyMemory(destinationPointer, rowSourcePointer, rowSpan);
                            destinationPointer += mappedResource.RowPitch;
                            rowSourcePointer += rowSpan;
                        }
                    }
                    finally
                    {
                        Unmap(texture, subresourceIndex);
                    }
                }
            }
            finally
            {
                gcHandle.Free();
            }
        }

        internal void SetData<T>(
            Texture2D texture,
            int arrayIndex,
            int mipLevel,
            Rectangle? rectangle,
            T[] data,
            int startIndex,
            int elementCount) where T : struct
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if ((uint) (D3D11Constants.ReqTexture2dArrayAxisDimension - 1) < (uint) arrayIndex)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (mipLevel < 0) throw new ArgumentOutOfRangeException("mipLevel");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");
            
            if (texture.Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("The specified texture is immutable.");

            // 領域指定は UpdateSubresource でなければ実装が面倒であるし、
            // 仮に実装したとしても常に全書き換えを GPU へ命令するため Dynamic の利点も失われるため、
            // 非サポートとして除外する。
            if (texture.Usage == ResourceUsage.Dynamic)
                throw new NotSupportedException("Dynamic texture does not support to write data into the specified bounds.");

            int levelWidth = texture.Width >> mipLevel;

            // ブロック圧縮ならばブロック サイズで調整。
            // この場合、FormatHelper.SizeInBytes で測る値は、
            // 1 ブロック (4x4 テクセル) に対するバイト数である点に注意。
            if (FormatHelper.IsBlockCompression(texture.Format))
            {
                levelWidth /= 4;
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var dataPointer = gcHandle.AddrOfPinnedObject();
                var sizeOfT = Marshal.SizeOf(typeof(T));

                var sourcePointer = (IntPtr) (dataPointer + startIndex * sizeOfT);

                int sourceRowPitch;

                Box? destinationBox = null;
                if (rectangle.HasValue)
                {
                    destinationBox = new Box(
                        rectangle.Value.Left,
                        rectangle.Value.Top,
                        0,
                        rectangle.Value.Right,
                        rectangle.Value.Bottom,
                        1);

                    sourceRowPitch = FormatHelper.SizeInBytes(texture.Format) * rectangle.Value.Width;
                }
                else
                {
                    sourceRowPitch = FormatHelper.SizeInBytes(texture.Format) * levelWidth;
                }

                if (FormatHelper.IsBlockCompression(texture.Format))
                {
                    sourceRowPitch /= 4;
                }

                var subresourceIndex = Resource.CalculateSubresource(mipLevel, arrayIndex, texture.MipLevels);
                UpdateSubresource(texture, subresourceIndex, destinationBox, sourcePointer, sourceRowPitch, 0);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        internal void Save(Texture2D texture, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            if (texture == null) throw new ArgumentNullException("texture");
            if (stream == null) throw new ArgumentNullException("stream");

            SaveCore(texture, stream, format);
        }

        protected abstract void DrawCore(int vertexCount, int startVertexLocation);

        protected abstract void DrawIndexedCore(int indexCount, int startIndexLocation, int baseVertexLocation);

        protected abstract void DrawInstancedCore(
            int vertexCountPerInstance,
            int instanceCount,
            int startVertexLocation,
            int startInstanceLocation);

        protected abstract void DrawIndexedInstancedCore(
            int indexCountPerInstance,
            int instanceCount,
            int startIndexLocation = 0,
            int baseVertexLocation = 0,
            int startInstanceLocation = 0);

        protected abstract void GetDataCore<T>(
            Texture2D texture,
            int arrayIndex,
            int level,
            Rectangle? rectangle,
            T[] data,
            int startIndex,
            int elementCount) where T : struct;

        protected abstract void SaveCore(Texture2D texture, Stream stream, ImageFileFormat format);

        internal protected abstract MappedSubresource Map(Resource resource, int subresource, MapMode mapMode);

        internal protected abstract void Unmap(Resource resource, int subresource);

        internal protected abstract void UpdateSubresource(
            Resource destinationResource,
            int destinationSubresource,
            Box? destinationBox,
            IntPtr sourcePointer,
            int sourceRowPitch,
            int sourceDepthPitch);

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
