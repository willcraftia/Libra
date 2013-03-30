#region Using

using System;

using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;

#endregion

namespace Libra.Graphics.SharpDX
{
    public static class ResourceHelper
    {
        public static D3D11CpuAccessFlags GetD3D11CpuAccessFlags(D3D11ResourceUsage usage)
        {
            // TODO
            //
            // Staging はリソースのコピーなどで用いるためのもの？
            // 頂点バッファなどで指定するとエラーになる。
            // MonoGame のコードなどを見る限りは、DataContext の CopySubresourceRegion などで、
            // GPU へ転送したリソースを staging のリソースへコピーし、
            // そこからデータを取得したりなどしている。

            //if (usage == D3D11ResourceUsage.Staging)
            //    return D3D11CpuAccessFlags.Read | D3D11CpuAccessFlags.Write;

            if (usage == D3D11ResourceUsage.Dynamic)
                return D3D11CpuAccessFlags.Write;

            return D3D11CpuAccessFlags.None;
        }
    }
}
