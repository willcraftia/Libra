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

        public SdxFormGamePlatform(Game game)
            : base(game, new SDXWRenderForm(game.GetType().Name))
        {
        }

        public SdxFormGamePlatform(Game game, Form form)
            : base(game, form)
        {
        }

        public override void Run(TickCallback tick)
        {
            SDXWRenderLoop.Run(Form, new SDXWRenderLoop.RenderCallback(tick));
        }

        protected override IGraphicsFactory CreateGraphicsFactory()
        {
            return new SdxGraphicsFactory();
        }

        protected override IKeyboard CreateKeyboard()
        {
            return FormKeyboard.Instance;
        }

        protected override IMouse CreateMouse()
        {
            return FormMouse.Instance;
        }

        protected override IJoystick CreateJoystick()
        {
            if (DirectInputEnabled)
            {
                sdxDirectInput = new SdxDirectInput();
                sdxJoystick = sdxDirectInput.CreateJoystick();
                return sdxJoystick;
            }

            return null;
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
