#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    internal static class GraphicsHelper
    {
        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr destinationPointer, IntPtr sourcePointer, int size);
    }
}
