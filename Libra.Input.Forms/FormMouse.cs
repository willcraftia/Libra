#region Using

using System;
using System.Windows.Forms;

#endregion

namespace Libra.Input.Forms
{
    public sealed class FormMouse : IMouse
    {
        internal MouseState State;

        public MouseState GetState()
        {
            return State;
        }

        public void SetPosition(int x, int y)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
        }
    }
}
