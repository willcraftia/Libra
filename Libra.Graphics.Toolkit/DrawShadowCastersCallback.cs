#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// 投影オブジェクトを描画する際に呼び出されるコールバック デリゲートです。
    /// コールバックを受けたクラスは、シャドウ マップ エフェクトを用いて投影オブジェクトを描画します。
    /// 描画する投影オブジェクトの選択は、コールバックを受けたクラスが決定します。
    /// </summary>
    /// <param name="effect">シャドウ マップ エフェクト。</param>
    public delegate void DrawShadowCastersCallback(IEffect effect);
}
