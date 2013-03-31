﻿#region Using

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#endregion

namespace Libra.Input.Forms
{
    // フォームから直接イベントを受信しない理由。
    //      イベント ハンドラを登録しなければ、
    //      イベント オブジェクト生成を避けられるのではないかと・・・。

    public sealed class MessageFilter : IMessageFilter
    {
        struct MouseEvent
        {
            public Int32 structureSize;

            public Int32 flags;
            
            public IntPtr trackWindowHandle;
            
            public Int32 hoverTime;
        }

        enum WindowMessages : int
        {
            GETDLGCODE       = 0x0087,
            INPUT            = 0x00FF,
            KEYDOWN          = 0x0100,
            KEYUP            = 0x0101,
            CHAR             = 0x0102,
            UNICHAR          = 0x0109,
            MOUSEMOVE        = 0x0200,
            LBUTTONDOWN      = 0x0201,
            LBUTTONUP        = 0x0202,
            LBUTTONDBLCLK    = 0x0203,
            RBUTTONDOWN      = 0x0204,
            RBUTTONUP        = 0x0205,
            RBUTTONDBLCLK    = 0x0206,
            MBUTTONDOWN      = 0x0207,
            MBUTTONUP        = 0x0208,
            MBUTTONDBLCLK    = 0x0209,
            MOUSEHWHEEL      = 0x020A,
            XBUTTONDOWN      = 0x020B,
            XBUTTONUP        = 0x020C,
            XBUTTONDBLCLK    = 0x020D,
            MOUSEHWHEEL_TILT = 0x020E,
            MOUSELEAVE       = 0x02A3
        }

        const int TME_LEAVE = 0x00000002;

        FormKeyboard keyboard;

        FormMouse mouse;

        MouseEvent mouseEvent;

        bool trackingMouse;

        public MessageFilter(IntPtr windowHandle, FormKeyboard keyboard, FormMouse mouse)
        {
            this.keyboard = keyboard;
            this.mouse = mouse;

            mouseEvent = new MouseEvent();
            mouseEvent.structureSize = Marshal.SizeOf(this.mouseEvent);
            mouseEvent.flags = TME_LEAVE;
            mouseEvent.trackWindowHandle = windowHandle;
        }

        [DllImport("user32")]
        static extern int TrackMouseEvent(ref MouseEvent eventTrack);

        public bool PreFilterMessage(ref Message message)
        {
            switch ((WindowMessages) message.Msg)
            {
                case WindowMessages.KEYDOWN:
                    {
                        int key = message.WParam.ToInt32();
                        keyboard.State[(Keys) key] = KeyState.Down;
                        break;
                    }
                case WindowMessages.KEYUP:
                    {
                        int key = message.WParam.ToInt32();
                        keyboard.State[(Keys) key] = KeyState.Up;
                        break;
                    }

                case WindowMessages.MOUSEMOVE:
                    {
                        if (!trackingMouse)
                        {
                            int result = TrackMouseEvent(ref mouseEvent);
                            trackingMouse = (result != 0);
                        }

                        short x = (short) (message.LParam.ToInt32() & 0xFFFF);
                        short y = (short) (message.LParam.ToInt32() >> 16);

                        mouse.State.X = x;
                        mouse.State.Y = y;
                        break;
                    }

                case WindowMessages.LBUTTONDOWN:
                case WindowMessages.LBUTTONDBLCLK:
                    {
                        mouse.State.LeftButton = ButtonState.Pressed;
                        break;
                    }
                case WindowMessages.LBUTTONUP:
                    {
                        mouse.State.LeftButton = ButtonState.Released;
                        break;
                    }

                case WindowMessages.RBUTTONDOWN:
                case WindowMessages.RBUTTONDBLCLK:
                    {
                        mouse.State.RightButton = ButtonState.Pressed;
                        break;
                    }
                case WindowMessages.RBUTTONUP:
                    {
                        mouse.State.RightButton = ButtonState.Released;
                        break;
                    }

                case WindowMessages.MBUTTONDOWN:
                case WindowMessages.MBUTTONDBLCLK:
                    {
                        mouse.State.MiddleButton = ButtonState.Pressed;
                        break;
                    }
                case WindowMessages.MBUTTONUP:
                    {
                        mouse.State.MiddleButton = ButtonState.Released;
                        break;
                    }

                case WindowMessages.XBUTTONDOWN:
                case WindowMessages.XBUTTONDBLCLK:
                    {
                        short button = (short) (message.WParam.ToInt32() >> 16);
                        if (button == 1)
                        {
                            mouse.State.XButton1 = ButtonState.Pressed;
                        }
                        if (button == 2)
                        {
                            mouse.State.XButton2 = ButtonState.Pressed;
                        }

                        break;
                    }
                case WindowMessages.XBUTTONUP:
                    {
                        short button = (short) (message.WParam.ToInt32() >> 16);
                        if (button == 1)
                        {
                            mouse.State.XButton1 = ButtonState.Released;
                        }
                        if (button == 2)
                        {
                            mouse.State.XButton2 = ButtonState.Released;
                        }

                        break;
                    }

                case WindowMessages.MOUSEHWHEEL:
                    {
                        short ticks = (short) (message.WParam.ToInt32() >> 16);
                        mouse.State.ScrollWheelValue = ticks;
                        break;
                    }

                case WindowMessages.MOUSELEAVE:
                    {
                        mouse.State.X = -1;
                        mouse.State.Y = -1;
                        this.trackingMouse = false;
                        break;
                    }
            }

            return false;
        }
    }
}
