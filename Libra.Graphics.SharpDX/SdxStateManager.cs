#region Using

using System;
using System.Collections.Generic;

using D3D11DeviceChild = SharpDX.Direct3D11.DeviceChild;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxStateManager<TState, TD3D11State> : IDisposable
        where TState : State
        where TD3D11State : D3D11DeviceChild
    {
        public delegate TD3D11State CreateD3D11StateFunction(TState state);

        CreateD3D11StateFunction createFunction;

        Dictionary<TState, TD3D11State> stateMap;

        public SdxDevice Device { get; private set; }

        public TD3D11State this[TState state]
        {
            get
            {
                lock (stateMap)
                {
                    if (!stateMap.ContainsKey(state))
                        Register(state);

                    return stateMap[state];
                }
            }
        }

        public SdxStateManager(SdxDevice device, CreateD3D11StateFunction createFunction)
        {
            Device = device;
            this.createFunction = createFunction;

            stateMap = new Dictionary<TState, TD3D11State>();
        }

        /// <summary>
        /// ステートを追加します。
        /// </summary>
        /// <remarks>
        /// ステートを追加すると、対応する ID3D11DeviceChild が内部で生成されます。
        /// そして、生成された ID3D11DeviceChild は、ステートをキーとするディクショナリで管理されます。
        /// なお、追加されたステートはプロパティ凍結状態に設定され、
        /// 以後、プロパティを変更しようとすると例外が発生するようになります。
        /// </remarks>
        /// <param name="state">追加するステート。</param>
        public void Register(TState state)
        {
            if (stateMap.ContainsKey(state))
                return;

            stateMap[state] = createFunction(state);

            state.Freeze();
        }

        /// <summary>
        /// ステートを削除します。
        /// </summary>
        /// <remarks>
        /// ステートの削除では、対応する ID3D11DeviceChild の Dipose() が呼び出されます。
        /// また、削除したステートはプロパティ凍結状態が維持されます。
        /// </remarks>
        /// <param name="state">削除するステート。</param>
        public void Deregister(TState state)
        {
            if (!stateMap.ContainsKey(state))
                return;

            var core = stateMap[state];
            if (!core.IsDisposed)
                core.Dispose();

            stateMap.Remove(state);
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~SdxStateManager()
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
            if (IsDisposed) return;

            if (disposing)
            {
                foreach (var d3d11DeviceChild in stateMap.Values)
                    d3d11DeviceChild.Dispose();
            }

            IsDisposed = true;
        }

        #endregion
    }
}
