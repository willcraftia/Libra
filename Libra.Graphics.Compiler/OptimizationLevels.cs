#region Using

using System;

#endregion

namespace Libra.Graphics.Compiler
{
    /// <summary>
    /// 最適化レベルを定義する列挙型です。
    /// </summary>
    /// <remarks>
    /// D3DCompiler.h:
    /// D3DCOMPILE_OPTIMIZATION_LEVEL0
    /// D3DCOMPILE_OPTIMIZATION_LEVEL1
    /// D3DCOMPILE_OPTIMIZATION_LEVEL2
    /// D3DCOMPILE_OPTIMIZATION_LEVEL3
    /// </remarks>
    public enum OptimizationLevels
    {
        /// <summary>
        /// 最も低い最適化を行います。
        /// </summary>
        /// <remarks>
        /// このレベルを指定すると、コンパイルは高速になりますが、
        /// 低速なコードが生成されます。
        /// このレベルは、シェーダの開発時に指定します。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_OPTIMIZATION_LEVEL0
        /// </remarks>
        Level0 = (1 << 14),

        /// <summary>
        /// 二番目に低い最適化を行います。
        /// </summary>
        /// <remarks>
        /// デフォルトでは Level1 になります。
        /// 
        /// D3DCompiler.h: D3DCOMPILE_OPTIMIZATION_LEVEL1
        /// </remarks>
        Level1 = 0,

        /// <summary>
        /// 二番目に高い最適化を行います。
        /// </summary>
        /// <remarks>
        /// D3DCompiler.h: D3DCOMPILE_OPTIMIZATION_LEVEL2
        /// </remarks>
        Level2 = ((1 << 14) | (1 << 15)),

        /// <summary>
        /// 最も高い最適化を行います。
        /// </summary>
        /// <remarks>
        /// このレベルを指定すると、最も最適なコードを生成しますが、
        /// コンパイルは低速になります。
        /// このレベルは、パフォーマンスが最も重要な要因となる場合に、
        /// アプリケーションの最終ビルドで指定します。
        /// </remarks>
        Level3 = (1 << 15)
    }
}
