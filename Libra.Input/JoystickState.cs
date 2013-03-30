#region Using

using System;

#endregion

namespace Libra.Input
{
    // Direct Input のジョイスティックを XInput に矯正する。
    // 故に、Velocity などの依存は除外してしまう。

    public struct JoystickState
    {
        public JoystickButtons Buttons;

        public JoystickThumbSticks ThumbSticks;

        public JoystickTriggers Triggers;

        public JoystickDPad DPad;

        public bool IsConnected;

        public bool IsButtonDown(Buttons button)
        {
            if ((button & Input.Buttons.A) != 0 && Buttons.A != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.B) != 0 && Buttons.B != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.X) != 0 && Buttons.X != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.Y) != 0 && Buttons.Y != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.LeftShoulder) != 0 && Buttons.LeftShoulder != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.RightShoulder) != 0 && Buttons.RightShoulder != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.LeftStick) != 0 && Buttons.LeftStick != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.RightStick) != 0 && Buttons.RightStick != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.Back) != 0 && Buttons.Back != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.Start) != 0 && Buttons.Start != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.LeftTrigger) != 0 && 0 < Triggers.Left)
                return false;

            if ((button & Input.Buttons.RightTrigger) != 0 && 0 < Triggers.Left)
                return false;

            if ((button & Input.Buttons.DPadUp) != 0 && DPad.Up != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.DPadDown) != 0 && DPad.Down != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.DPadLeft) != 0 && DPad.Left != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.DPadRight) != 0 && DPad.Right != ButtonState.Pressed)
                return false;

            if ((button & Input.Buttons.LeftThumbstickUp) != 0 && ThumbSticks.Left.Y < 0)
                return false;

            if ((button & Input.Buttons.LeftThumbstickDown) != 0 && 0 < ThumbSticks.Left.Y)
                return false;

            if ((button & Input.Buttons.LeftThumbstickLeft) != 0 && ThumbSticks.Left.X < 0)
                return false;

            if ((button & Input.Buttons.LeftThumbstickRight) != 0 && 0 < ThumbSticks.Left.X)
                return false;

            if ((button & Input.Buttons.RightThumbstickUp) != 0 && ThumbSticks.Right.Y < 0)
                return false;

            if ((button & Input.Buttons.RightThumbstickDown) != 0 && 0 < ThumbSticks.Right.Y)
                return false;

            if ((button & Input.Buttons.RightThumbstickLeft) != 0 && ThumbSticks.Right.X < 0)
                return false;

            if ((button & Input.Buttons.RightThumbstickRight) != 0 && 0 < ThumbSticks.Right.X)
                return false;

            return true;
        }

        public bool IsButtonUp(Buttons button)
        {
            if ((button & Input.Buttons.A) != 0 && Buttons.A != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.B) != 0 && Buttons.B != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.X) != 0 && Buttons.X != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.Y) != 0 && Buttons.Y != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.LeftShoulder) != 0 && Buttons.LeftShoulder != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.RightShoulder) != 0 && Buttons.RightShoulder != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.LeftStick) != 0 && Buttons.LeftStick != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.RightStick) != 0 && Buttons.RightStick != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.Back) != 0 && Buttons.Back != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.Start) != 0 && Buttons.Start != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.LeftTrigger) != 0 && Triggers.Left != 0)
                return false;

            if ((button & Input.Buttons.RightTrigger) != 0 && Triggers.Left != 0)
                return false;

            if ((button & Input.Buttons.DPadUp) != 0 && DPad.Up != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.DPadDown) != 0 && DPad.Down != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.DPadLeft) != 0 && DPad.Left != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.DPadRight) != 0 && DPad.Right != ButtonState.Released)
                return false;

            if ((button & Input.Buttons.LeftThumbstickUp) != 0 && 0 <= ThumbSticks.Left.Y)
                return false;

            if ((button & Input.Buttons.LeftThumbstickDown) != 0 && ThumbSticks.Left.Y <= 0)
                return false;

            if ((button & Input.Buttons.LeftThumbstickLeft) != 0 && 0 <= ThumbSticks.Left.X)
                return false;

            if ((button & Input.Buttons.LeftThumbstickRight) != 0 && ThumbSticks.Left.X <= 0)
                return false;

            if ((button & Input.Buttons.RightThumbstickUp) != 0 && 0 <= ThumbSticks.Right.Y)
                return false;

            if ((button & Input.Buttons.RightThumbstickDown) != 0 && ThumbSticks.Right.Y <= 0)
                return false;

            if ((button & Input.Buttons.RightThumbstickLeft) != 0 && 0 <= ThumbSticks.Right.X)
                return false;

            if ((button & Input.Buttons.RightThumbstickRight) != 0 && ThumbSticks.Right.X <= 0)
                return false;

            return true;
        }
    }
}
