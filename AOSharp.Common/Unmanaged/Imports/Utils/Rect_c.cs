using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Rect_c
    {
        [DllImport("Utils.dll", EntryPoint = "??0Rect@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "??1Rect@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        public static IntPtr Create()
        {
            return Constructor(MSVCR100.New(0x10));
        }
    }
}
