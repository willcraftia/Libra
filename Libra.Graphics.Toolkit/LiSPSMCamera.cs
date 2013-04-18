#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LiSPSMCamera : LightCamera
    {
        /// <summary>
        /// LSPSM の新しい式が有効かどうかを示します。
        /// true (新しい式が有効な場合)、false (それ以外の場合)。
        /// </summary>
        public bool NewNFormulaEnabled;

        /// <summary>
        /// 明示した N 値を使用するかどうかを示します。
        /// true (明示した N 値を使用する場合)、false (それ以外の場合)。
        /// </summary>
        public bool ExplicitNEnabled;

        /// <summary>
        /// 明示する N 値です。
        /// </summary>
        public float N;

        public override void Update()
        {


            base.Update();
        }
    }
}
