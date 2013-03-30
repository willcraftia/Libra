#region Using

using System;

using DIDeviceInstance = SharpDX.DirectInput.DeviceInstance;
using DIDirectInput = SharpDX.DirectInput.DirectInput;
using DIRawJoystickState = SharpDX.DirectInput.RawJoystickState;
using DIJoystickUpdate = SharpDX.DirectInput.JoystickUpdate;
using SDXUtilities = SharpDX.Utilities;

#endregion

namespace Libra.Input.SharpDX
{
    public sealed class SdxJoystick : IJoystick, IDisposable
    {
        #region Bridge

        sealed class Bridge : global::SharpDX.DirectInput.CustomDevice<StateBridge, DIRawJoystickState, DIJoystickUpdate>
        {
            public Bridge(DIDirectInput diDirectInput, Guid deviceGuid)
                : base(diDirectInput, deviceGuid)
            {
            }
        }

        #endregion

        #region StateBridge

        sealed class StateBridge : global::SharpDX.DirectInput.IDeviceState<DIRawJoystickState, DIJoystickUpdate>
        {
            public JoystickState State;

            // 左スティック
            //      X 軸     X
            //      Y 軸     Y
            //
            //      ※パッド設定画面をイメージすると分かるが、X と Y はそのまま 2D 平面にマップしている。
            //      ※倒し具合を見るには、その平面上での位置で判断する事になる。
            //
            // 右スティック
            //      ※右スティックについては曖昧だが、Z と RotationZ の組である事は確定。
            //
            //      X 軸     Z
            //      Y 軸     RotationZ
            //
            //      ※別名、Z はスロットル、RotationZ は方向舵らしいが、逆に思えてならない。
            //

            public void MarshalFrom(ref DIRawJoystickState value)
            {
                State = new JoystickState();

                unsafe
                {
                    // POV の角度をボタンへ対応付ける。
                    fixed (int* povs = value.PointOfViewControllers)
                    {
                        int pov = povs[0];
                        if (0 <= pov && pov <= 4500)        State.DPad.Up = ButtonState.Pressed;
                        if (4500 <= pov && pov <= 13500)    State.DPad.Right = ButtonState.Pressed;
                        if (13500 <= pov && pov <= 22500)   State.DPad.Down = ButtonState.Pressed;
                        if (21500 <= pov && pov <= 31500)   State.DPad.Left = ButtonState.Pressed;
                        if (31500 <= pov)                   State.DPad.Up = ButtonState.Pressed;
                    }

                    // ボタンの対応付け。
                    // ただし、第二ショルダーは擬似トリガー値へ対応。
                    fixed (byte* buttons = value.Buttons)
                    {
                        if (buttons[0] != 0)    State.Buttons.X = ButtonState.Pressed;
                        if (buttons[1] != 0)    State.Buttons.A = ButtonState.Pressed;
                        if (buttons[2] != 0)    State.Buttons.B = ButtonState.Pressed;
                        if (buttons[3] != 0)    State.Buttons.Y = ButtonState.Pressed;
                        if (buttons[4] != 0)    State.Buttons.LeftShoulder = ButtonState.Pressed;
                        if (buttons[5] != 0)    State.Buttons.RightShoulder = ButtonState.Pressed;
                        if (buttons[6] != 0)    State.Triggers.Left = 1;
                        if (buttons[7] != 0)    State.Triggers.Right = 1;
                        if (buttons[8] != 0)    State.Buttons.Back = ButtonState.Pressed;
                        if (buttons[9] != 0)    State.Buttons.Start = ButtonState.Pressed;
                        if (buttons[10] != 0)   State.Buttons.LeftStick = ButtonState.Pressed;
                        if (buttons[11] != 0)   State.Buttons.RightStick= ButtonState.Pressed;
                    }

                    // ゼロとみなす最大の値 0.01。
                    // 人間の操作では 0.01 の度合いでスティックを倒す事は不可能である。
                    float zeroTolerance = 0.01f;

                    float max = (float) ushort.MaxValue;
                    // [0, ushort.MaxValue] を [0, 1] へ。
                    // ここで計算誤差が発生。
                    float x = (float) value.X / max;
                    float y = (float) value.Y / max;
                    // [0, 1] を [-1, 1] へ。
                    x = x * 2 - 1;
                    y = y * 2 - 1;
                    // 計算誤差、および、スティックは正確に中心には戻らない事から、
                    // ゼロに関して補正。
                    // 最小値と最大値は越えないため、境界での補正は不要。
                    if (-zeroTolerance <= x && x <= zeroTolerance) x = 0;
                    if (-zeroTolerance <= y && y <= zeroTolerance) y = 0;
                    State.ThumbSticks.Left.X = x;
                    State.ThumbSticks.Left.Y = y;

                    // 右スティックも同様に。
                    x = (float) value.Z / max;
                    y = (float) value.RotationZ / max;
                    x = x * 2 - 1;
                    y = y * 2 - 1;
                    if (-zeroTolerance <= x && x <= zeroTolerance) x = 0;
                    if (-zeroTolerance <= y && y <= zeroTolerance) y = 0;
                    State.ThumbSticks.Right.X = x;
                    State.ThumbSticks.Right.Y = y;
                }
            }

            // 不要。
            public void Update(DIJoystickUpdate update) { throw new NotImplementedException(); }
        }

        #endregion

        DIDeviceInstance diDevice;

        Bridge bridge;

        StateBridge stateBridge;

        public SdxJoystick(DIDirectInput diDirectInput, DIDeviceInstance diDevice)
        {
            if (diDirectInput == null) throw new ArgumentNullException("diDirectInput");

            this.diDevice = diDevice;

            if (diDevice != null)
            {
                bridge = new Bridge(diDirectInput, diDevice.InstanceGuid);
                bridge.Acquire();
                stateBridge = new StateBridge();
            }
        }

        public JoystickState GetState()
        {
            if (diDevice == null)
                return new JoystickState();

            lock (this)
            {
                bridge.GetCurrentState(ref stateBridge);
                stateBridge.State.IsConnected = true;
                return stateBridge.State;
            }
        }

        #region IDisposable

        bool disposed;

        ~SdxJoystick()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                if (bridge != null)
                    bridge.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
