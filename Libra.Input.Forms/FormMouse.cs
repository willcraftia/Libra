#region Using

using System;
using System.Windows.Forms;

#endregion

namespace Libra.Input.Forms
{
    public sealed class FormMouse : IMouse
    {
        public MouseState State;

        Form form;

        public FormMouse(Form form)
        {
            this.form = form;
        }

        public MouseState GetState()
        {
            return State;
        }

        public void SetPosition(int x, int y)
        {
            var clientPoint = new System.Drawing.Point(x, y);
            var screenPoint = form.PointToScreen(clientPoint);

            Cursor.Position = screenPoint;
        }
    }
}
