#region Using

using System;

#endregion

namespace Libra.Input
{
    public struct KeyboardState
    {
        int flags32;

        int flags64;

        int flags96;

        int flags128;

        int flags160;

        int flags192;

        int flags224;

        int flags256;

        public KeyState this[Keys key]
        {
            get
            {
                int position = (int) key;
                if (position <= 0) throw new ArgumentOutOfRangeException("key");

                if (position <= 32)
                {
                    return GetKeyState(ref flags32, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags64, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags96, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags128, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags160, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags192, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags224, position);
                }

                position -= 32;
                if (position <= 32)
                {
                    return GetKeyState(ref flags256, position);
                }

                throw new ArgumentOutOfRangeException("key");
            }
            set
            {
                int position = (int) key;
                if (position <= 0) throw new ArgumentOutOfRangeException("key");

                if (position <= 32)
                {
                    SetKeyState(ref flags32, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags64, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags96, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags128, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags160, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags192, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags224, position, value);
                    return;
                }

                position -= 32;
                if (position <= 32)
                {
                    SetKeyState(ref flags256, position, value);
                    return;
                }

                throw new ArgumentOutOfRangeException("key");
            }
        }

        public bool IsKeyDown(Keys key)
        {
            return this[key] == KeyState.Down;
        }

        public bool IsKeyUp(Keys key)
        {
            return this[key] == KeyState.Up;
        }

        KeyState GetKeyState(ref int flags, int position)
        {
            position--;
            var flag = 1 << position;
            if ((flags & flag) == 0)
            {
                return KeyState.Up;
            }
            else
            {
                return KeyState.Down;
            }
        }

        void SetKeyState(ref int flags, int position, KeyState state)
        {
            position--;
            var flag = 1 << position;
            if (state == KeyState.Up)
            {
                flags &= ~flag;
            }
            else
            {
                flags |= flag;
            }
        }
    }
}
