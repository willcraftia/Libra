#region Using

using System;
using System.Windows.Forms;
using Libra.Graphics;
using Libra.Graphics.SharpDX;
using Libra.Input;
using Libra.Input.Forms;
using Libra.Input.SharpDX;

using SDXWRenderForm = SharpDX.Windows.RenderForm;
using SDXWRenderLoop = SharpDX.Windows.RenderLoop;

#endregion

namespace Libra.Games.Forms.SharpDX
{
    public sealed class SdxFormGamePlatform : FormGamePlatform
    {
        SdxDirectInput sdxDirectInput;

        SdxJoystick sdxJoystick;

        public bool DirectInputEnabled { get; set; }

        protected override IJoystick Joystick
        {
            get { return sdxJoystick; }
        }

        protected override void Initialize()
        {
            base.Initialize();

            sdxDirectInput = new SdxDirectInput();
            sdxJoystick = sdxDirectInput.CreateJoystick();
        }

        protected override void Run(TickCallback tick)
        {
            SDXWRenderLoop.Run(Form, new SDXWRenderLoop.RenderCallback(tick));
        }

        protected override Form CreateForm()
        {
            return new SDXWRenderForm();
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (sdxJoystick != null)
                    sdxJoystick.Dispose();

                if (sdxDirectInput != null)
                    sdxDirectInput.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
