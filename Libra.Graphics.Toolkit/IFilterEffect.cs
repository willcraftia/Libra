#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IFilterEffect : IEffect
    {
        /// <summary>
        /// フィルタが有効であるか否かを示す値を取得します。
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// フィルタ対象テクスチャを取得または設定します。
        /// </summary>
        /// <remarks>
        /// 殆どのフィルタは、フィルタ対象テクスチャをレジスタ #0 へ設定しますが、
        /// 実際のレジスタはシェーダ コードに依存します。
        /// 
        /// フィルタ チェーンへフィルタを登録して利用する場合、
        /// フィルタ チェーンは対象のテクスチャを Texture プロパティに対して設定します。
        /// 
        /// なお、フィルタ対象テクスチャを必要としない特殊なフィルタも一部に例外として存在し、
        /// そのようなフィルタでは Texture プロパティは無効です。
        /// </remarks>
        ShaderResourceView Texture { get;  set; }

        /// <summary>
        /// フィルタ対象テクスチャに対するサンプラ ステートを取得または設定します。
        /// </summary>
        /// <remarks>
        /// 殆どのフィルタは、フィルタ対象テクスチャに対するサンプラ ステートをレジスタ #0 へ設定しますが、
        /// 実際のレジスタはシェーダ コードに依存します。
        /// 
        /// なお、null を指定した場合、デバイス コンテキストのデフォルト (LinearClamp) に従います。
        /// </remarks>
        SamplerState TextureSampler { get; set; }
    }
}
