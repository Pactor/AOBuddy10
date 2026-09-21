using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class String_c
    {
        [DllImport("Utils.dll", EntryPoint = "??0String@@QAE@PBDH@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, byte[] str, int len);

        [DllImport("Utils.dll", EntryPoint = "??1String@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);
    }
}
