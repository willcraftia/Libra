#region Using

using System;

#endregion

namespace Libra.Input
{
    // 実際にはトリガーは存在しない。
    // 第二ショルダー ボタンをトリガーへ強引に対応させる。
    // トリガー値は、ボタンが押下されていれば 1、それ以外は 0 として扱う。

    public struct JoystickTriggers
    {
        public float Left;

        public float Right;
    }
}
