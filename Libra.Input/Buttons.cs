#region Using

using System;

#endregion

namespace Libra.Input
{
    // Direct Input のジョイスティックを XInput に矯正する。
    // そもそも、ボタンの番号による管理は、コードの可読性を極端に低下させる。
    // 制約として、POV ボタンはボタンとしてマップするため、
    // Direct Input で得られるはずの POV 角度は、もはや得られない。
    //
    // ボタン番号との対応
    //      0   X
    //      1   A
    //      2   B
    //      3   Y
    //      4   LeftShoulder
    //      5   RightShoulder
    //      6   LeftTrigger
    //      7   RightTrigger
    //      8   Back
    //      9   Start
    //      10  LeftStick
    //      11  RightStick
    //
    // なお、第二ショルダーボタンをトリガーへ対応させるが、
    // 実際にはボタンであり、トリガーとしての機能を持たない。

    [Flags]
    public enum Buttons
    {
        DPadUp                  = 0x1,
        DPadDown                = 0x2,
        DPadLeft                = 0x4,
        DPadRight               = 0x8,
        Start                   = 0x10,
        Back                    = 0x20,
        LeftStick               = 0x40,
        RightStick              = 0x80,
        LeftShoulder            = 0x100,
        RightShoulder           = 0x200,

        // 非 XInput では考えなくて良い。
        //BigButton               = 0x800,

        A                       = 0x1000,
        B                       = 0x2000,
        X                       = 0x4000,
        Y                       = 0x8000,
        
        LeftThumbstickLeft      = 0x200000,
        RightTrigger            = 0x400000,
        LeftTrigger             = 0x800000,

        RightThumbstickUp       = 0x1000000,
        RightThumbstickDown     = 0x2000000,
        RightThumbstickRight    = 0x4000000,
        RightThumbstickLeft     = 0x8000000,
        
        LeftThumbstickUp        = 0x10000000,
        LeftThumbstickDown      = 0x20000000,
        LeftThumbstickRight     = 0x40000000,
    }
}
